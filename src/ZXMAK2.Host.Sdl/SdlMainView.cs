using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using Kozynax.UI.Helpers;
using Silk.NET.SDL;
using Thread = System.Threading.Thread;
using ManualResetEvent = System.Threading.ManualResetEvent;
using SendOrPostCallback = System.Threading.SendOrPostCallback;
using ZXMAK2.Dependency;
using ZXMAK2.Engine.Interfaces;
using ZXMAK2.Host.Interfaces;
using ZXMAK2.Host.Presentation;
using ZXMAK2.Host.Presentation.Interfaces;
using RenderScaleMode = ZXMAK2.Host.Presentation.Interfaces.ScaleMode;
using ZXMAK2.Host.SdlBackend.Views;
using ZXMAK2.Host.Services;
using ZXMAK2.Host.Terminal;
using ZXMAK2.Mvvm;
using Event = Silk.NET.SDL.Event;

namespace ZXMAK2.Host.SdlBackend
{
    public sealed unsafe class SdlMainView : IMainView, ICommandManager, ISynchronizeInvoke
    {
        private readonly IResolver _resolver;
        private readonly Sdl _sdl;
        private readonly ConcurrentQueue<(SendOrPostCallback, object)> _invokeQueue =
            new ConcurrentQueue<(SendOrPostCallback, object)>();

        private Window* _window;
        private Renderer* _renderer;
        private Texture* _texture;
        private int _textureWidth;
        private int _textureHeight;
        private int _frameWidth;
        private int _frameHeight;
        private float _frameRatio = 1f;
        private bool _hasTexture;
        private bool _applyingRenderSize;

        private SdlVideo _video;
        private SdlIconOverlay _icons;
        private SdlDebugOverlay _debugOsd;
        private SdlSound _sound;
        private SdlKeyboard _keyboard;
        private SdlMouse _mouse;
        private SdlJoystick _joystick;
        private IHostService _host;

        private bool _quit;
        private bool _running;
        private int _uiThreadId;
        private string _fileTitle = string.Empty;
        private readonly List<ICommand> _commands = new List<ICommand>();
        /// <summary>
        /// When true, Spectrum keyboard/mouse SDL events are ignored while a Terminal UI
        /// session is active (SdlTerminal shares the SDL event queue with the emulator).
        /// Stdio Kozui reads stdin instead, so the SDL window can keep driving the Spectrum.
        /// </summary>
        private bool _muteEmulatorInputDuringUi;

        public SdlMainView(IResolver resolver)
        {
            _resolver = resolver;
            _sdl = Sdl.GetApi();
        }

        public object DataContext { get; set; }
        public IHostService Host => _host;
        public ICommandManager CommandManager => this;

        public event EventHandler ViewOpened;
        public event EventHandler ViewClosed;
        public event EventHandler RequestFrame;

        public void Run()
        {
            // Prefer Wayland when available so the window appears on the user's session
            // instead of a secondary X11 DISPLAY that may not be visible.
            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("SDL_VIDEODRIVER"))
                && !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WAYLAND_DISPLAY")))
            {
                Environment.SetEnvironmentVariable("SDL_VIDEODRIVER", "wayland");
            }

            if (_sdl.Init(Sdl.InitVideo | Sdl.InitAudio | Sdl.InitJoystick | Sdl.InitGamecontroller) != 0)
                throw new InvalidOperationException($"SDL_Init failed: {_sdl.GetErrorS()}");

            Console.WriteLine(
                "SDL video driver: {0} (displays={1})",
                _sdl.GetCurrentVideoDriverS(),
                _sdl.GetNumVideoDisplays());

            var settings = _resolver.Resolve<ISettingService>();
            _window = _sdl.CreateWindow(
                MainWindowTitle.ProductName,
                Sdl.WindowposCentered,
                Sdl.WindowposCentered,
                settings.WindowWidth,
                settings.WindowHeight,
                (uint)(WindowFlags.Shown | WindowFlags.Resizable | WindowFlags.AllowHighdpi));

            if (_window == null)
                throw new InvalidOperationException($"SDL_CreateWindow failed: {_sdl.GetErrorS()}");

            SdlWindowIcon.Apply(_sdl, _window);
            _sdl.ShowWindow(_window);
            _sdl.RaiseWindow(_window);

            _renderer = _sdl.CreateRenderer(_window, -1, (uint)RendererFlags.Accelerated);
            if (_renderer == null)
                _renderer = _sdl.CreateRenderer(_window, -1, (uint)RendererFlags.Software);
            if (_renderer == null)
                throw new InvalidOperationException($"SDL_CreateRenderer failed: {_sdl.GetErrorS()}");

            _sdl.SetHint(Sdl.HintRenderScaleQuality, settings.RenderSmooth ? "1" : "0");

            // Wayland compositors typically do not map a window until the first present.
            _sdl.SetRenderDrawColor(_renderer, 0, 0, 0, 255);
            _sdl.RenderClear(_renderer);
            _sdl.RenderPresent(_renderer);

            var runtime = _resolver.Resolve<SdlRuntimeContext>();
            runtime.Window = _window;
            runtime.Renderer = _renderer;

            _uiThreadId = Thread.CurrentThread.ManagedThreadId;
            _video = new SdlVideo();
            _icons = new SdlIconOverlay(_sdl);
            _debugOsd = new SdlDebugOverlay();
            _sound = new SdlSound(_sdl);
            _keyboard = new SdlKeyboard();
            _mouse = new SdlMouse(_sdl);
            _joystick = new SdlJoystick(_sdl);
            _host = new HostService(_video, _sound, _keyboard, _mouse, _joystick);

            var terminal = _resolver.Resolve<ITerminal>();
            // Stdio Kozui uses stdin; keep SDL window keys/mouse for the Spectrum.
            _muteEmulatorInputDuringUi = !(terminal is StdioTerminal);

            // Clear Spectrum keys on UI enter/leave. When Terminal shares the SDL event
            // queue (SdlTerminal), also release mouse capture for the overlay.
            Action prepareUi = () =>
            {
                _keyboard.Reset();
                if (_muteEmulatorInputDuringUi)
                    _mouse.SuspendForUi();
            };
            Action endUi = () =>
            {
                _keyboard.Reset();
                if (_muteEmulatorInputDuringUi)
                    _mouse.ResumeAfterUi();
            };
            runtime.PrepareUiInput = prepareUi;
            runtime.EndUiInput = endUi;
            TerminalUiSession.Entered = prepareUi;
            TerminalUiSession.Left = endUi;

            if (terminal is StdioTerminal)
            {
                // Keep the SDL window updating while Kozui runs on stdout.
                runtime.IdlePump = IdlePumpFrame;
                TerminalUiSession.IdlePump = IdlePumpFrame;
            }

            _running = true;
            ViewOpened?.Invoke(this, EventArgs.Empty);
            HookDataContext();

            // Interactive TTY: show the main menu in the console immediately.
            if (terminal is StdioTerminal)
                ShowMainMenu();

            while (!_quit)
            {
                PumpInvokes();
                ProcessEvents();
                PresentFrame();
            }

            ViewClosed?.Invoke(this, EventArgs.Empty);
            CleanupHost();
            CleanupSdl();
            TerminalUiSession.IdlePump = null;
            TerminalUiSession.Entered = null;
            TerminalUiSession.Left = null;
            runtime.IdlePump = null;
            if (terminal is IDisposable disposableTerminal)
                disposableTerminal.Dispose();
        }

        public void Close()
            => _quit = true;

        public void Activate()
        {
            if (_window != null)
                _sdl.RaiseWindow(_window);
        }

        public void Clear()
            => _commands.Clear();

        public void Add(ICommand command)
            => _commands.Add(command);

        public void Dispose()
        {
            _quit = true;
            CleanupHost();
            CleanupSdl();
            _sdl.Dispose();
        }

        #region ISynchronizeInvoke

        public bool InvokeRequired
            => Thread.CurrentThread.ManagedThreadId != _uiThreadId;

        public IAsyncResult BeginInvoke(Delegate method, object[] args)
        {
            var callback = new SendOrPostCallback(_ => method.DynamicInvoke(args));
            _invokeQueue.Enqueue((callback, null));
            return null;
        }

        public object EndInvoke(IAsyncResult result) => null;

        public object Invoke(Delegate method, object[] args)
        {
            if (!InvokeRequired)
                return method.DynamicInvoke(args);

            object ret = null;
            using (var done = new ManualResetEvent(false))
            {
                _invokeQueue.Enqueue((new SendOrPostCallback(_ =>
                {
                    ret = method.DynamicInvoke(args);
                    done.Set();
                }), null));
                // Avoid deadlock if Invoke is used before the UI pump starts.
                if (!_running && !done.WaitOne(0))
                    PumpInvokes();
                done.WaitOne();
            }
            return ret;
        }

        #endregion

        private void HookDataContext()
        {
            if (DataContext is INotifyPropertyChanged npc)
                npc.PropertyChanged += DataContext_PropertyChanged;
            ApplyTitle();
        }

        private void DataContext_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == null || e.PropertyName == "Title" || e.PropertyName == "IsRunning")
                ApplyTitle();
            if (e.PropertyName == null || e.PropertyName == "IsFullScreen")
                ApplyFullScreen();
            if (e.PropertyName == null || e.PropertyName == "RenderSize")
                ApplyRenderSize();
        }

        private void ApplyTitle()
        {
            var titleProp = DataContext?.GetType().GetProperty("Title");
            _fileTitle = titleProp?.GetValue(DataContext) as string ?? string.Empty;

            var isRunning = true;
            var runningProp = DataContext?.GetType().GetProperty("IsRunning");
            if (runningProp?.GetValue(DataContext) is bool running)
                isRunning = running;

            if (_window != null)
                _sdl.SetWindowTitle(_window, MainWindowTitle.Format(_fileTitle, isRunning));
        }

        private void ApplyFullScreen()
        {
            if (_window == null || DataContext == null)
                return;
            var prop = DataContext.GetType().GetProperty("IsFullScreen");
            if (prop == null)
                return;
            var full = (bool)prop.GetValue(DataContext);
            _sdl.SetWindowFullscreen(_window, full ? (uint)WindowFlags.FullscreenDesktop : 0);
            // Size commands set RenderSize then clear fullscreen — apply size once windowed.
            if (!full)
                ApplyRenderSize();
        }

        private void ApplyRenderSize()
        {
            if (_window == null || DataContext == null)
                return;

            var fsProp = DataContext.GetType().GetProperty("IsFullScreen");
            if (fsProp != null && (bool)fsProp.GetValue(DataContext))
                return;

            var prop = DataContext.GetType().GetProperty("RenderSize");
            if (prop == null)
                return;

            var size = prop.GetValue(DataContext);
            if (!(size is Size renderSize) || renderSize.Width <= 0 || renderSize.Height <= 0)
                return;

            _applyingRenderSize = true;
            try
            {
                _sdl.SetWindowSize(_window, renderSize.Width, renderSize.Height);
            }
            finally
            {
                _applyingRenderSize = false;
            }
        }

        private void SyncWindowSizeToViewModel()
        {
            if (_applyingRenderSize || _window == null || DataContext == null)
                return;

            var fsProp = DataContext.GetType().GetProperty("IsFullScreen");
            if (fsProp != null && (bool)fsProp.GetValue(DataContext))
                return;

            var prop = DataContext.GetType().GetProperty("RenderSize");
            if (prop == null || !prop.CanWrite)
                return;

            int w, h;
            _sdl.GetWindowSize(_window, &w, &h);
            if (w <= 0 || h <= 0)
                return;

            var current = prop.GetValue(DataContext);
            if (current is Size existing && existing.Width == w && existing.Height == h)
                return;

            prop.SetValue(DataContext, new Size(w, h));
        }

        private void PumpInvokes()
        {
            while (_invokeQueue.TryDequeue(out var item))
                item.Item1(item.Item2);
        }

        /// <summary>Drain BeginInvoke queue while a Terminal UI session owns the main loop.</summary>
        public void PumpUiCallbacks()
            => PumpInvokes();

        private void IdlePumpFrame()
        {
            PumpInvokes();
            ProcessEvents();
            PresentFrame();
            if (_quit)
                (_resolver.Resolve<ITerminal>() as StdioTerminal)?.RequestQuit();
        }

        private void ProcessEvents()
        {
            // Mute only when Terminal UI consumes the same SDL event stream (SdlTerminal).
            var muteInput = _muteEmulatorInputDuringUi && TerminalUiSession.IsUiActive;
            Event e;
            while (_sdl.PollEvent(&e) != 0)
            {
                switch ((EventType)e.Type)
                {
                    case EventType.Quit:
                        _quit = true;
                        break;
                    case EventType.Keydown:
                        // SdlTerminal shares this queue with Kozui — mute while UI is up.
                        // Stdio Kozui uses stdin; SDL keys still drive the Spectrum.
                        if (muteInput)
                            break;
                        if (TerminalUiSession.IsUiActive)
                        {
                            // Console UI already open: feed Spectrum keys only (no nested F9/Esc UI).
                            if (!IsHostHotKey((KeyCode)e.Key.Keysym.Sym))
                                _keyboard.OnKeyEvent((KeyCode)e.Key.Keysym.Sym, true);
                            break;
                        }
                        if (HandleHotKey((KeyCode)e.Key.Keysym.Sym, true))
                            break;
                        _keyboard.OnKeyEvent((KeyCode)e.Key.Keysym.Sym, true);
                        break;
                    case EventType.Keyup:
                        if (muteInput)
                            break;
                        // Drop host hotkey chords so they never stick in the Spectrum matrix.
                        if (IsHostHotKey((KeyCode)e.Key.Keysym.Sym))
                            break;
                        _keyboard.OnKeyEvent((KeyCode)e.Key.Keysym.Sym, false);
                        break;
                    case EventType.Mousemotion:
                        if (muteInput)
                            break;
                        _mouse.OnMouseMotion(e.Motion.Xrel, e.Motion.Yrel);
                        break;
                    case EventType.Mousebuttondown:
                        if (muteInput)
                            break;
                        // SDL: 1=left, 2=middle, 3=right — menu only while uncaptured.
                        if (e.Button.Button == 3 && !_mouse.IsCaptured)
                        {
                            if (TerminalUiSession.IsUiActive)
                                break;
                            _keyboard.Reset();
                            ShowMainMenu();
                            _keyboard.Reset();
                            break;
                        }
                        if (!_mouse.IsCaptured)
                            _mouse.Capture();
                        _mouse.OnMouseButton(e.Button.Button, true);
                        break;
                    case EventType.Mousebuttonup:
                        if (muteInput)
                            break;
                        _mouse.OnMouseButton(e.Button.Button, false);
                        break;
                    case EventType.Windowevent:
                        switch ((WindowEventID)e.Window.Event)
                        {
                            case WindowEventID.Close:
                                _quit = true;
                                break;
                            case WindowEventID.FocusLost:
                                _mouse.Uncapture();
                                break;
                            case WindowEventID.SizeChanged:
                            case WindowEventID.Resized:
                                SyncWindowSizeToViewModel();
                                break;
                        }
                        break;
                }
            }

            _keyboard.Scan();
            _mouse.Scan();
            _joystick.KeyboardState = _keyboard.State;
            _joystick.Scan();
        }

        private bool HandleHotKey(KeyCode key, bool pressed)
        {
            if (!pressed || DataContext == null)
                return false;

            var mods = (Keymod)_sdl.GetModState();
            var ctrl = (mods & (Keymod.Ctrl | Keymod.Lctrl | Keymod.Rctrl)) != 0;

            // Ctrl+O - open file screen (never feed this chord to the emulated keyboard)
            if (ctrl && key == KeyCode.KO)
            {
                _keyboard.Reset();
                TryExecuteCommand("CommandFileOpen");
                _keyboard.Reset();
                return true;
            }

            // F9 - horizontal menu bar on Terminal
            if (key == KeyCode.KF9)
            {
                _keyboard.Reset();
                ShowMainMenu();
                _keyboard.Reset();
                return true;
            }

            // F11 - fullscreen, Escape - exit fullscreen / quit, F5 - warm reset via commands when available
            if (key == KeyCode.KF11)
            {
                TryExecuteCommand("CommandViewFullScreen");
                return true;
            }
            if (key == KeyCode.KF5)
            {
                TryExecuteCommand("CommandVmWarmReset");
                return true;
            }
            if (key == KeyCode.KF8)
            {
                TryExecuteCommand("CommandVmPause");
                return true;
            }
            if (key == KeyCode.KEscape)
            {
                var fs = DataContext.GetType().GetProperty("IsFullScreen");
                if (fs != null && (bool)fs.GetValue(DataContext))
                {
                    TryExecuteCommand("CommandViewFullScreen");
                    return true;
                }

                // In the bare emulator window, Esc stops the CPU and opens the debugger
                // instead of quitting. Falls back to Exit if no debugger is available
                // (e.g. unsupported board).
                if (TryOpenDebuggerAndStop())
                    return true;

                TryExecuteCommand("CommandFileExit");
                return true;
            }

            return false;
        }

        private bool TryOpenDebuggerAndStop()
        {
            var debuggerCommand = _commands.Find(c => c.Text == "Debugger");
            if (debuggerCommand == null || !debuggerCommand.CanExecute(this))
                return false;

            var runningProp = DataContext?.GetType().GetProperty("IsRunning");
            var wasRunning = runningProp != null
                && runningProp.GetValue(DataContext) is bool running
                && running;
            if (wasRunning)
                TryExecuteCommand("CommandVmPause");

            _keyboard.Reset();
            debuggerCommand.Execute(this);
            _keyboard.Reset();

            // Debugger.Execute blocks until the dialog is closed. Resume emulation
            // if it was running before we paused it for the debugger, unless the
            // user already resumed it (e.g. pressed Run/F9) while inside.
            if (wasRunning
                && runningProp != null
                && runningProp.GetValue(DataContext) is bool nowRunning
                && !nowRunning)
            {
                TryExecuteCommand("CommandVmPause");
            }

            return true;
        }

        private bool IsHostHotKey(KeyCode key)
        {
            var mods = (Keymod)_sdl.GetModState();
            var ctrl = (mods & (Keymod.Ctrl | Keymod.Lctrl | Keymod.Rctrl)) != 0;
            if (ctrl && key == KeyCode.KO)
                return true;
            return key == KeyCode.KF9
                   || key == KeyCode.KF11
                   || key == KeyCode.KF5
                   || key == KeyCode.KF8
                   || key == KeyCode.KEscape;
        }

        private void ShowMainMenu()
        {
            if (_quit)
                return;
            var vm = DataContext as MainViewModel;
            if (vm == null)
                return;
            var terminal = _resolver.Resolve<ITerminal>();
            if (terminal is StdioTerminal)
            {
                TerminalMainMenuView.Show(terminal, vm, _commands, this, () => _quit);
                return;
            }

            // Menu loop blocks the main pump; keep draining synchronized VM→UI updates
            // (e.g. Pause/Resume label) while the overlay is open.
            var previousIdle = TerminalUiSession.IdlePump;
            TerminalUiSession.IdlePump = () =>
            {
                previousIdle?.Invoke();
                PumpInvokes();
            };
            SdlMenuImagePainter painter = null;
            try
            {
                painter = new SdlMenuImagePainter(_sdl, _renderer);
                SdlMenuBarView.Show(
                    terminal,
                    vm,
                    _commands,
                    this,
                    () => _quit,
                    DrawEmulatorUnderlay,
                    painter);
            }
            finally
            {
                painter?.Dispose();
                TerminalUiSession.IdlePump = previousIdle;
            }
        }

        private void TryExecuteCommand(string propertyName)
        {
            var prop = DataContext.GetType().GetProperty(propertyName);
            var command = prop?.GetValue(DataContext) as ICommand;
            if (command != null && command.CanExecute(this))
                command.Execute(this);
        }

        /// <summary>
        /// Draws the latest emulator frame into the renderer without presenting,
        /// then dims slightly so the menu chrome stays readable.
        /// </summary>
        private void DrawEmulatorUnderlay()
        {
            UpdateEmulatorTexture();

            _sdl.SetRenderDrawColor(_renderer, 0, 0, 0, 255);
            _sdl.RenderClear(_renderer);

            if (_hasTexture && _texture != null)
            {
                int winW, winH;
                _sdl.GetRendererOutputSize(_renderer, &winW, &winH);
                var dst = ComputeDestination(GetRenderScaleMode(), winW, winH, _frameWidth, _frameHeight, _frameRatio);
                _sdl.RenderCopy(_renderer, _texture, null, &dst);
                DrawOsdIcons(winW, winH);
                DrawDebugOsd(winW, winH);
            }

            // Soft dim so menu text stays readable over moving video.
            _sdl.SetRenderDrawBlendMode(_renderer, BlendMode.Blend);
            _sdl.SetRenderDrawColor(_renderer, 0, 0, 0, 100);
            int rw, rh;
            _sdl.GetRendererOutputSize(_renderer, &rw, &rh);
            var dim = new Silk.NET.Maths.Rectangle<int>(0, 0, rw, rh);
            _sdl.RenderFillRect(_renderer, &dim);
            _sdl.SetRenderDrawBlendMode(_renderer, BlendMode.None);
        }

        private void PresentFrame()
        {
            if (!UpdateEmulatorTexture() && !_hasTexture)
            {
                // No frame yet — keep the window mapped without busy-spinning.
                _sdl.Delay(1);
                return;
            }

            _sdl.SetRenderDrawColor(_renderer, 0, 0, 0, 255);
            _sdl.RenderClear(_renderer);

            int winW, winH;
            _sdl.GetRendererOutputSize(_renderer, &winW, &winH);
            var dst = ComputeDestination(GetRenderScaleMode(), winW, winH, _frameWidth, _frameHeight, _frameRatio);
            _sdl.RenderCopy(_renderer, _texture, null, &dst);
            DrawOsdIcons(winW, winH);
            DrawDebugOsd(winW, winH);
            _sdl.RenderPresent(_renderer);
            _debugOsd?.OnPresent();

            // Cap present rate; re-blitting the last texture avoids black flicker
            // when the UI loop outruns the ~50 Hz emulator.
            _sdl.Delay(1);
        }

        private void DrawOsdIcons(int winW, int winH)
        {
            if (_icons == null || _video == null || !IsDisplayIconEnabled())
                return;
            _icons.Draw(_renderer, winW, winH, _video.Icons);
        }

        private void DrawDebugOsd(int winW, int winH)
        {
            if (_debugOsd == null || !IsDebugInfoEnabled())
                return;

            SyncDebugRunningState();
            _debugOsd.Draw(_sdl, _renderer, winW, winH, GetDisplayRefreshRate());
        }

        private bool IsDisplayIconEnabled()
        {
            var settings = _resolver.TryResolve<ISettingService>();
            if (settings != null)
                return settings.RenderDisplayIcon;

            var prop = DataContext?.GetType().GetProperty("CommandViewDisplayIcon");
            var command = prop?.GetValue(DataContext) as ICommand;
            return command == null || command.Checked;
        }

        private bool IsDebugInfoEnabled()
        {
            var settings = _resolver.TryResolve<ISettingService>();
            if (settings != null)
                return settings.RenderDebugInfo;

            var prop = DataContext?.GetType().GetProperty("CommandViewDebugInfo");
            var command = prop?.GetValue(DataContext) as ICommand;
            return command != null && command.Checked;
        }

        private void SyncDebugRunningState()
        {
            if (_debugOsd == null)
                return;
            var prop = DataContext?.GetType().GetProperty("IsRunning");
            if (prop != null && prop.GetValue(DataContext) is bool running)
                _debugOsd.IsRunning = running;
        }

        private int GetDisplayRefreshRate()
        {
            DisplayMode mode;
            if (_sdl.GetCurrentDisplayMode(0, &mode) == 0 && mode.RefreshRate > 0)
                return mode.RefreshRate;
            return 0;
        }

        /// <returns>True when a new frame was consumed.</returns>
        private bool UpdateEmulatorTexture()
        {
            if (!_video.TryConsumeFrame(out var buffer, out var size, out var ratio, out var debug))
                return false;

            EnsureTexture(size.Width, size.Height);
            _frameWidth = size.Width;
            _frameHeight = size.Height;
            _frameRatio = ratio;
            _debugOsd?.OnFrame(debug, size, ratio);

            // Copy under the video lock lifetime: TryConsumeFrame returns the
            // shared buffer, so upload immediately before the next PushFrame.
            fixed (int* pBuffer = buffer)
            {
                _sdl.UpdateTexture(_texture, null, pBuffer, size.Width * 4);
            }

            _hasTexture = true;
            _video.NotifyPresented();
            return true;
        }

        private RenderScaleMode GetRenderScaleMode()
        {
            var prop = DataContext?.GetType().GetProperty("RenderScaleMode");
            if (prop != null && prop.GetValue(DataContext) is RenderScaleMode mode)
                return mode;
            return RenderScaleMode.KeepProportion;
        }

        private void EnsureTexture(int width, int height)
        {
            if (_texture != null && _textureWidth == width && _textureHeight == height)
                return;

            if (_texture != null)
            {
                _sdl.DestroyTexture(_texture);
                _texture = null;
            }

            _texture = _sdl.CreateTexture(
                _renderer,
                Sdl.PixelformatArgb8888,
                (int)TextureAccess.Streaming,
                width,
                height);
            if (_texture == null)
                throw new InvalidOperationException($"SDL_CreateTexture failed: {_sdl.GetErrorS()}");

            _textureWidth = width;
            _textureHeight = height;
        }

        /// <summary>
        /// Mirrors WinForms <c>ScaleHelper.GetDestinationRect</c> (ratio-normalized frame size).
        /// </summary>
        private static Silk.NET.Maths.Rectangle<int> ComputeDestination(
            RenderScaleMode scaleMode,
            int winW,
            int winH,
            int frameW,
            int frameH,
            float ratio)
        {
            if (frameW <= 0 || frameH <= 0)
                return new Silk.NET.Maths.Rectangle<int>(0, 0, winW, winH);

            var dstW = (float)frameW;
            var dstH = frameH * Math.Max(ratio, 0.01f);
            var rx = winW / dstW;
            var ry = winH / dstH;

            switch (scaleMode)
            {
                case RenderScaleMode.SquarePixelSize:
                {
                    var s = Math.Min(Math.Floor(rx), Math.Floor(ry));
                    s = s < 1 ? 1 : s;
                    rx = ry = (float)s;
                    break;
                }
                case RenderScaleMode.FixedPixelSize:
                    rx = (float)Math.Floor(rx);
                    ry = (float)Math.Floor(ry);
                    if (rx < 1)
                        rx = 1;
                    if (ry < 1)
                        ry = 1;
                    break;
                case RenderScaleMode.KeepProportion:
                    if (rx > ry)
                        rx = (winW * ry / rx) / dstW;
                    else if (rx < ry)
                        ry = (winH * rx / ry) / dstH;
                    break;
                case RenderScaleMode.Stretch:
                    break;
            }

            var outW = (int)Math.Floor(dstW * rx);
            var outH = (int)Math.Floor(dstH * ry);
            var x = (winW - outW) / 2;
            var y = (winH - outH) / 2;
            return new Silk.NET.Maths.Rectangle<int>(x, y, outW, outH);
        }

        private void CleanupHost()
        {
            _host?.Dispose();
            _host = null;
            // HostService does not dispose video.
            _icons?.Dispose();
            _icons = null;
            _video?.Dispose();
            _video = null;
            _sound = null;
            _keyboard = null;
            _mouse = null;
            _joystick = null;
        }

        private void CleanupSdl()
        {
            var runtime = _resolver.TryResolve<SdlRuntimeContext>();
            if (runtime != null)
            {
                runtime.Window = null;
                runtime.Renderer = null;
            }

            if (_texture != null)
            {
                _sdl.DestroyTexture(_texture);
                _texture = null;
            }
            if (_renderer != null)
            {
                _sdl.DestroyRenderer(_renderer);
                _renderer = null;
            }
            if (_window != null)
            {
                _sdl.DestroyWindow(_window);
                _window = null;
            }
            if (_running)
            {
                _sdl.Quit();
                _running = false;
            }
        }
    }
}
