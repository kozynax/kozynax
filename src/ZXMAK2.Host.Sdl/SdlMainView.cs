using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using Silk.NET.SDL;
using Thread = System.Threading.Thread;
using ManualResetEvent = System.Threading.ManualResetEvent;
using SendOrPostCallback = System.Threading.SendOrPostCallback;
using ZXMAK2.Dependency;
using ZXMAK2.Engine.Interfaces;
using ZXMAK2.Host.Entities;
using ZXMAK2.Host.Interfaces;
using ZXMAK2.Host.Presentation.Interfaces;
using ZXMAK2.Host.Services;
using ZXMAK2.Mvvm;
using Silk.NET.Maths;
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

        private SdlVideo _video;
        private SdlSound _sound;
        private SdlKeyboard _keyboard;
        private SdlMouse _mouse;
        private SdlJoystick _joystick;
        private IHostService _host;

        private bool _quit;
        private bool _running;
        private int _uiThreadId;
        private string _title = "ZXMAK2 (SDL)";
        private readonly List<ICommand> _commands = new List<ICommand>();

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
                _title,
                Sdl.WindowposCentered,
                Sdl.WindowposCentered,
                settings.WindowWidth,
                settings.WindowHeight,
                (uint)(WindowFlags.Shown | WindowFlags.Resizable | WindowFlags.AllowHighdpi));

            if (_window == null)
                throw new InvalidOperationException($"SDL_CreateWindow failed: {_sdl.GetErrorS()}");

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
            _sound = new SdlSound(_sdl);
            _keyboard = new SdlKeyboard();
            _mouse = new SdlMouse(_sdl);
            _joystick = new SdlJoystick(_sdl);
            _host = new HostService(_video, _sound, _keyboard, _mouse, _joystick);

            _running = true;
            ViewOpened?.Invoke(this, EventArgs.Empty);
            HookDataContext();

            while (!_quit)
            {
                PumpInvokes();
                ProcessEvents();
                PresentFrame();
            }

            ViewClosed?.Invoke(this, EventArgs.Empty);
            CleanupHost();
            CleanupSdl();
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
            if (e.PropertyName == null || e.PropertyName == "Title")
                ApplyTitle();
            if (e.PropertyName == null || e.PropertyName == "IsFullScreen")
                ApplyFullScreen();
        }

        private void ApplyTitle()
        {
            var titleProp = DataContext?.GetType().GetProperty("Title");
            var title = titleProp?.GetValue(DataContext) as string;
            if (!string.IsNullOrEmpty(title))
                _title = title;
            if (_window != null)
                _sdl.SetWindowTitle(_window, _title);
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
        }

        private void PumpInvokes()
        {
            while (_invokeQueue.TryDequeue(out var item))
                item.Item1(item.Item2);
        }

        private void ProcessEvents()
        {
            Event e;
            while (_sdl.PollEvent(&e) != 0)
            {
                switch ((EventType)e.Type)
                {
                    case EventType.Quit:
                        _quit = true;
                        break;
                    case EventType.Keydown:
                        if (HandleHotKey((KeyCode)e.Key.Keysym.Sym, true))
                            break;
                        _keyboard.OnKeyEvent((KeyCode)e.Key.Keysym.Sym, true);
                        break;
                    case EventType.Keyup:
                        // Drop host hotkey chords so they never stick in the Spectrum matrix.
                        if (IsHostHotKey((KeyCode)e.Key.Keysym.Sym))
                            break;
                        _keyboard.OnKeyEvent((KeyCode)e.Key.Keysym.Sym, false);
                        break;
                    case EventType.Mousemotion:
                        _mouse.OnMouseMotion(e.Motion.Xrel, e.Motion.Yrel);
                        break;
                    case EventType.Mousebuttondown:
                        _mouse.OnMouseButton(e.Button.Button, true);
                        break;
                    case EventType.Mousebuttonup:
                        _mouse.OnMouseButton(e.Button.Button, false);
                        break;
                    case EventType.Windowevent:
                        if ((WindowEventID)e.Window.Event == WindowEventID.Close)
                            _quit = true;
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

            // Ctrl+H - pilot Kozui ConfirmDialog on Terminal
            if (ctrl && key == KeyCode.KH)
            {
                _keyboard.Reset();
                var query = Locator.Resolve<IUserQuery>();
                query?.Show("Kozui layout pilot: Tab/arrows focus, Enter activates, Esc cancels.",
                    "ConfirmDialog",
                    DlgButtonSet.OKCancel,
                    DlgIcon.Information);
                _keyboard.Reset();
                return true;
            }

            // Ctrl+T - Tape Settings (Kozui tree on Terminal)
            if (ctrl && key == KeyCode.KT)
            {
                _keyboard.Reset();
                TryExecuteUiCommand("Tape");
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
                    TryExecuteCommand("CommandViewFullScreen");
                else
                    TryExecuteCommand("CommandFileExit");
                return true;
            }

            return false;
        }

        private bool IsHostHotKey(KeyCode key)
        {
            var mods = (Keymod)_sdl.GetModState();
            var ctrl = (mods & (Keymod.Ctrl | Keymod.Lctrl | Keymod.Rctrl)) != 0;
            if (ctrl && (key == KeyCode.KO || key == KeyCode.KH || key == KeyCode.KT))
                return true;
            return key == KeyCode.KF11 || key == KeyCode.KF5 || key == KeyCode.KF8 || key == KeyCode.KEscape;
        }

        private void TryExecuteCommand(string propertyName)
        {
            var prop = DataContext.GetType().GetProperty(propertyName);
            var command = prop?.GetValue(DataContext) as ICommand;
            if (command != null && command.CanExecute(this))
                command.Execute(this);
        }

        private void TryExecuteUiCommand(string commandText)
        {
            foreach (var command in _commands)
            {
                if (command != null
                    && string.Equals(command.Text, commandText, StringComparison.OrdinalIgnoreCase)
                    && command.CanExecute(this))
                {
                    command.Execute(this);
                    return;
                }
            }
        }

        private void PresentFrame()
        {
            if (_video.TryConsumeFrame(out var buffer, out var size, out var ratio))
            {
                EnsureTexture(size.Width, size.Height);
                _frameWidth = size.Width;
                _frameHeight = size.Height;
                _frameRatio = ratio;

                // Copy under the video lock lifetime: TryConsumeFrame returns the
                // shared buffer, so upload immediately before the next PushFrame.
                fixed (int* pBuffer = buffer)
                {
                    _sdl.UpdateTexture(_texture, null, pBuffer, size.Width * 4);
                }

                _hasTexture = true;
                _video.NotifyPresented();
            }
            else if (!_hasTexture)
            {
                // No frame yet — keep the window mapped without busy-spinning.
                _sdl.Delay(1);
                return;
            }

            _sdl.SetRenderDrawColor(_renderer, 0, 0, 0, 255);
            _sdl.RenderClear(_renderer);

            int winW, winH;
            _sdl.GetRendererOutputSize(_renderer, &winW, &winH);
            var dst = ComputeDestination(winW, winH, _frameWidth, _frameHeight, _frameRatio);
            _sdl.RenderCopy(_renderer, _texture, null, &dst);
            _sdl.RenderPresent(_renderer);

            // Cap present rate; re-blitting the last texture avoids black flicker
            // when the UI loop outruns the ~50 Hz emulator.
            _sdl.Delay(1);
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

        private static Silk.NET.Maths.Rectangle<int> ComputeDestination(int winW, int winH, int frameW, int frameH, float ratio)
        {
            if (frameW <= 0 || frameH <= 0)
                return new Silk.NET.Maths.Rectangle<int>(0, 0, winW, winH);

            var aspect = (frameW * Math.Max(ratio, 0.01f)) / frameH;
            var destH = winH;
            var destW = (int)(destH * aspect);
            if (destW > winW)
            {
                destW = winW;
                destH = (int)(destW / aspect);
            }

            return new Silk.NET.Maths.Rectangle<int>((winW - destW) / 2, (winH - destH) / 2, destW, destH);
        }

        private void CleanupHost()
        {
            _host?.Dispose();
            _host = null;
            // HostService does not dispose video.
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
