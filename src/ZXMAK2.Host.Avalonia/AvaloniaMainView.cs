using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Kozynax.UI;
using Kozynax.UI.Helpers;
using Silk.NET.SDL;
using ZXMAK2.Dependency;
using ZXMAK2.Engine.Interfaces;
using ZXMAK2.Host.AvaloniaBackend.Views;
using ZXMAK2.Host.Interfaces;
using ZXMAK2.Host.Presentation;
using ZXMAK2.Host.Presentation.Interfaces;
using ZXMAK2.Host.SdlBackend;
using ZXMAK2.Host.Services;
using ZXMAK2.Mvvm;
using AvaloniaSize = Avalonia.Size;
using ICommand = ZXMAK2.Mvvm.ICommand;
using Key = Avalonia.Input.Key;
using Cursor = Avalonia.Input.Cursor;
using DrawingSize = System.Drawing.Size;

namespace ZXMAK2.Host.AvaloniaBackend
{
    /// <summary>
    /// <see cref="IMainView"/> on Avalonia: owns the window, the host devices and the
    /// UI-thread presenter. Audio and joysticks come from SDL (Avalonia has neither).
    /// </summary>
    public sealed class AvaloniaMainView : IMainView, ICommandManager, ISynchronizeInvoke
    {
        private const int ResizeSettleMs = 100;

        private readonly IResolver _resolver;
        private readonly List<ICommand> _commands = new List<ICommand>();

        private ClassicDesktopStyleApplicationLifetime _lifetime;
        private MainWindow _window;
        private AvaloniaRuntimeContext _runtime;

        private AvaloniaVideo _video;
        private SdlSound _sound;
        private AvaloniaKeyboard _keyboard;
        private AvaloniaMouse _mouse;
        private AvaloniaJoystick _joystick;
        private IHostService _host;
        private Sdl _sdl;
        private bool _sdlInitialized;

        private int _presentPending;
        private bool _applyingRenderSize;
        private bool _syncingFromWindow;
        private DispatcherTimer _resizeTimer;
        private bool _menuRebuildPending;
        private bool _quit;

        public AvaloniaMainView(IResolver resolver)
        {
            _resolver = resolver;
        }

        public object DataContext { get; set; }
        public IHostService Host => _host;
        public ICommandManager CommandManager => this;

        public event EventHandler ViewOpened;
        public event EventHandler ViewClosed;
        public event EventHandler RequestFrame;

        private MainViewModel ViewModel => DataContext as MainViewModel;

        #region IMainView

        public void Run()
        {
            _runtime = _resolver.Resolve<AvaloniaRuntimeContext>();
            var settings = _resolver.Resolve<ISettingService>();

            _lifetime = new ClassicDesktopStyleApplicationLifetime
            {
                ShutdownMode = ShutdownMode.OnMainWindowClose,
            };
            var builder = AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .LogToTrace();
            // KOZYNAX_SOFTWARE_RENDER=1 forces the CPU rasterizer (broken GL/XWayland setups).
            if (Environment.GetEnvironmentVariable("KOZYNAX_SOFTWARE_RENDER") == "1")
            {
                builder = builder
                    .With(new X11PlatformOptions { RenderingMode = new[] { X11RenderingMode.Software } })
                    .With(new Win32PlatformOptions { RenderingMode = new[] { Win32RenderingMode.Software } });
            }
            builder.SetupWithLifetime(_lifetime);

            _window = new MainWindow();
            _runtime.MainWindow = _window;

            CreateHost();
            WireWindow();

            _window.Surface.ScaleMode = settings.RenderScaleMode;
            _window.Surface.Smooth = settings.RenderSmooth;

            _lifetime.MainWindow = _window;
            _lifetime.Start(Array.Empty<string>());

            _quit = true;
            SyncWindowSizeToViewModel();
            ViewClosed?.Invoke(this, EventArgs.Empty);
            CleanupHost();
        }

        public void Close()
        {
            _quit = true;
            Dispatcher.UIThread.Post(() => _window?.Close());
        }

        public void Activate()
            => Dispatcher.UIThread.Post(() => _window?.Activate());

        public void Dispose()
        {
            _quit = true;
            CleanupHost();
        }

        #endregion

        #region ICommandManager

        public void Clear()
        {
            _commands.Clear();
            ScheduleMenuRebuild();
        }

        public void Add(ICommand command)
        {
            _commands.Add(command);
            ScheduleMenuRebuild();
        }

        #endregion

        #region ISynchronizeInvoke

        public bool InvokeRequired => !Dispatcher.UIThread.CheckAccess();

        public IAsyncResult BeginInvoke(Delegate method, object[] args)
        {
            Dispatcher.UIThread.Post(() => method.DynamicInvoke(args));
            return null;
        }

        public object EndInvoke(IAsyncResult result) => null;

        public object Invoke(Delegate method, object[] args)
        {
            if (!InvokeRequired)
                return method.DynamicInvoke(args);
            return Dispatcher.UIThread.Invoke(() => method.DynamicInvoke(args));
        }

        #endregion

        #region Host

        private void CreateHost()
        {
            _sdl = _resolver.Resolve<Sdl>();
            if (_sdl.InitSubSystem(Sdl.InitAudio | Sdl.InitJoystick | Sdl.InitGamecontroller) != 0)
                Logger.Warn("SDL_InitSubSystem(audio|joystick) failed: {0}", _sdl.GetErrorS());
            else
                _sdlInitialized = true;

            _video = new AvaloniaVideo();
            _video.FrameReady += OnFrameReady;

            try
            {
                _sound = _sdlInitialized ? new SdlSound(_sdl) : null;
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                _sound = null;
            }

            _keyboard = new AvaloniaKeyboard();
            _mouse = new AvaloniaMouse();
            _mouse.CaptureChanged += OnMouseCaptureChanged;
            _joystick = _sdlInitialized ? new AvaloniaJoystick(_sdl) : null;
            _host = new HostService(_video, _sound, _keyboard, _mouse, _joystick);

            _runtime.PrepareUiInput = () =>
            {
                _keyboard.Reset();
                _mouse.Uncapture();
            };
            _runtime.EndUiInput = () => _keyboard.Reset();
        }

        private void CleanupHost()
        {
            if (_video != null)
                _video.FrameReady -= OnFrameReady;
            _host?.Dispose();
            _host = null;
            // HostService does not dispose video.
            _video?.Dispose();
            _video = null;
            _sound = null;
            _keyboard = null;
            _mouse = null;
            _joystick = null;
            _window?.Surface.DisposeBitmap();

            if (_runtime != null)
            {
                _runtime.MainWindow = null;
                _runtime.PrepareUiInput = null;
                _runtime.EndUiInput = null;
            }

            if (_sdlInitialized)
            {
                _sdl.Quit();
                _sdlInitialized = false;
            }
        }

        #endregion

        #region Window wiring

        private void WireWindow()
        {
            _window.Opened += Window_Opened;
            _window.Loaded += (s, e) => ApplyRenderSize();
            _window.Closing += (s, e) => SyncWindowSizeToViewModel();
            _window.Deactivated += (s, e) =>
            {
                _keyboard?.Reset();
                _mouse?.Uncapture();
            };

            _window.AddHandler(InputElement.KeyDownEvent, Window_KeyDown, RoutingStrategies.Tunnel);
            _window.AddHandler(InputElement.KeyUpEvent, Window_KeyUp, RoutingStrategies.Tunnel);

            var surface = _window.Surface;
            surface.PointerPressed += Surface_PointerPressed;
            surface.PointerReleased += Surface_PointerReleased;
            surface.PointerMoved += Surface_PointerMoved;
            surface.SizeChanged += (s, e) => NoteUserResize();
            surface.Presented += () => _video?.NotifyPresented();

            _resizeTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(ResizeSettleMs),
            };
            _resizeTimer.Tick += (s, e) =>
            {
                _resizeTimer.Stop();
                SyncWindowSizeToViewModel();
            };
        }

        private void Window_Opened(object sender, EventArgs e)
        {
            HookDataContext();
            ViewOpened?.Invoke(this, EventArgs.Empty);
            RebuildMenu();
            _window.Surface.Focus();
        }

        private void HookDataContext()
        {
            var vm = ViewModel;
            if (vm == null)
                return;
            vm.PropertyChanged += DataContext_PropertyChanged;
            vm.CommandViewSmooth.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == null || e.PropertyName == "Checked")
                    _window.Surface.Smooth = vm.CommandViewSmooth.Checked;
            };
            ApplyTitle();
            ApplyScaleMode();
        }

        private void DataContext_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (!Dispatcher.UIThread.CheckAccess())
            {
                Dispatcher.UIThread.Post(() => DataContext_PropertyChanged(sender, e));
                return;
            }

            if (e.PropertyName == null || e.PropertyName == "Title" || e.PropertyName == "IsRunning")
                ApplyTitle();
            if (e.PropertyName == null || e.PropertyName == "IsFullScreen")
                ApplyFullScreen();
            if (!_syncingFromWindow && (e.PropertyName == null || e.PropertyName == "RenderSize"))
                ApplyRenderSize();
            if (e.PropertyName == null || e.PropertyName == "RenderScaleMode")
                ApplyScaleMode();
        }

        private void ApplyTitle()
        {
            var vm = ViewModel;
            if (vm == null || _window == null)
                return;
            _window.Title = MainWindowTitle.Format(vm.Title ?? string.Empty, vm.IsRunning);
        }

        private void ApplyScaleMode()
        {
            var vm = ViewModel;
            if (vm != null && _window != null)
                _window.Surface.ScaleMode = vm.RenderScaleMode;
        }

        private void ApplyFullScreen()
        {
            var vm = ViewModel;
            if (vm == null || _window == null)
                return;

            if (vm.IsFullScreen)
            {
                _window.Menu.IsVisible = false;
                _window.WindowState = WindowState.FullScreen;
            }
            else
            {
                _window.WindowState = WindowState.Normal;
                _window.Menu.IsVisible = true;
                // Size commands set RenderSize then clear fullscreen — apply once windowed.
                Dispatcher.UIThread.Post(ApplyRenderSize, DispatcherPriority.Loaded);
            }
        }

        /// <summary>ViewModel.RenderSize (pixels) → client area, leaving room for the menu.</summary>
        private void ApplyRenderSize()
        {
            var vm = ViewModel;
            if (vm == null || _window == null || vm.IsFullScreen)
                return;
            if (_window.WindowState == WindowState.Maximized)
                return;

            var size = vm.RenderSize;
            if (size.Width <= 0 || size.Height <= 0)
                return;

            var scaling = _window.RenderScaling > 0 ? _window.RenderScaling : 1.0;
            var menuHeight = _window.Menu.IsVisible ? _window.Menu.Bounds.Height : 0;
            var client = new AvaloniaSize(size.Width / scaling, size.Height / scaling + menuHeight);

            var current = _window.ClientSize;
            if (Math.Abs(current.Width - client.Width) < 0.5 && Math.Abs(current.Height - client.Height) < 0.5)
                return;

            _applyingRenderSize = true;
            try
            {
                // Window.Width/Height are the client area in DIPs.
                _window.Width = client.Width;
                _window.Height = client.Height;
            }
            finally
            {
                _applyingRenderSize = false;
            }
        }

        private void NoteUserResize()
        {
            if (_applyingRenderSize || _quit)
                return;
            // Compositors emit a flood of size events while dragging; settle first.
            _resizeTimer?.Stop();
            _resizeTimer?.Start();
        }

        /// <summary>Surface size (pixels) → ViewModel.RenderSize, which also persists it.</summary>
        private void SyncWindowSizeToViewModel()
        {
            var vm = ViewModel;
            if (vm == null || _window == null || vm.IsFullScreen || _applyingRenderSize)
                return;
            if (_window.WindowState != WindowState.Normal)
                return;

            var bounds = _window.Surface.Bounds;
            var scaling = _window.RenderScaling > 0 ? _window.RenderScaling : 1.0;
            var w = (int)Math.Round(bounds.Width * scaling);
            var h = (int)Math.Round(bounds.Height * scaling);
            if (w <= 0 || h <= 0)
                return;

            var logical = new DrawingSize(w, h);
            if (vm.RenderSize == logical)
                return;

            _syncingFromWindow = true;
            try
            {
                vm.RenderSize = logical;
            }
            finally
            {
                _syncingFromWindow = false;
            }
        }

        #endregion

        #region Menu

        private void ScheduleMenuRebuild()
        {
            if (_menuRebuildPending)
                return;
            _menuRebuildPending = true;
            Dispatcher.UIThread.Post(() =>
            {
                _menuRebuildPending = false;
                RebuildMenu();
            });
        }

        private void RebuildMenu()
        {
            var vm = ViewModel;
            if (vm == null || _window == null || _quit)
                return;
            MenuBuilder.Populate(
                _window.Menu,
                () => MainMenuFactory.BuildRoot(vm, _commands),
                this);
        }

        private void TryExecuteCommand(ICommand command)
        {
            if (command != null && command.CanExecute(this))
                command.Execute(this);
        }

        #endregion

        #region Input

        private bool IsEmulatorInputActive
            => _window != null && _window.IsActive && !_window.Menu.IsOpen;

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (_keyboard == null || !IsEmulatorInputActive)
                return;

            if (HandleHotKey(e))
            {
                e.Handled = true;
                return;
            }

            if (_keyboard.IsMapped(e.PhysicalKey))
            {
                _keyboard.OnKeyEvent(e.PhysicalKey, true);
                e.Handled = true;
            }
        }

        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            if (_keyboard == null)
                return;
            if (IsHostHotKey(e))
            {
                e.Handled = true;
                return;
            }
            if (_keyboard.IsMapped(e.PhysicalKey))
            {
                _keyboard.OnKeyEvent(e.PhysicalKey, false);
                e.Handled = true;
            }
        }

        private bool HandleHotKey(KeyEventArgs e)
        {
            var vm = ViewModel;
            if (vm == null)
                return false;

            var ctrl = (e.KeyModifiers & KeyModifiers.Control) != 0;
            if (ctrl && e.Key == Key.O)
            {
                _keyboard.Reset();
                TryExecuteCommand(vm.CommandFileOpen);
                _keyboard.Reset();
                return true;
            }
            if (e.Key == Key.F11)
            {
                TryExecuteCommand(vm.CommandViewFullScreen);
                return true;
            }
            if (e.Key == Key.F5)
            {
                TryExecuteCommand(vm.CommandVmWarmReset);
                return true;
            }
            if (e.Key == Key.F8)
            {
                TryExecuteCommand(vm.CommandVmPause);
                return true;
            }
            if (e.Key == Key.Escape)
            {
                if (vm.IsFullScreen)
                {
                    TryExecuteCommand(vm.CommandViewFullScreen);
                    return true;
                }
                if (_mouse.IsCaptured)
                {
                    _mouse.Uncapture();
                    return true;
                }
            }
            return false;
        }

        private bool IsHostHotKey(KeyEventArgs e)
        {
            var ctrl = (e.KeyModifiers & KeyModifiers.Control) != 0;
            if (ctrl && e.Key == Key.O)
                return true;
            return e.Key == Key.F11 || e.Key == Key.F5 || e.Key == Key.F8;
        }

        private void Surface_PointerPressed(object sender, PointerPressedEventArgs e)
        {
            if (_mouse == null)
                return;
            _window.Surface.Focus();

            var props = e.GetCurrentPoint(_window.Surface).Properties;
            if (!_mouse.IsCaptured)
            {
                if (props.IsLeftButtonPressed)
                    _mouse.Capture();
                _mouse.ResetOrigin(e.GetPosition(_window.Surface));
                e.Handled = true;
                return;
            }

            e.Pointer.Capture(_window.Surface);
            _mouse.OnButton(ButtonBit(e.GetCurrentPoint(_window.Surface).Properties.PointerUpdateKind), true);
            e.Handled = true;
        }

        private void Surface_PointerReleased(object sender, PointerReleasedEventArgs e)
        {
            if (_mouse == null || !_mouse.IsCaptured)
                return;
            _mouse.OnButton(ButtonBit(e.GetCurrentPoint(_window.Surface).Properties.PointerUpdateKind), false);
            e.Handled = true;
        }

        private void Surface_PointerMoved(object sender, PointerEventArgs e)
        {
            if (_mouse == null || !_mouse.IsCaptured)
                return;
            _mouse.OnPointerMoved(e.GetPosition(_window.Surface));
            e.Handled = true;
        }

        private static int ButtonBit(PointerUpdateKind kind)
        {
            switch (kind)
            {
                case PointerUpdateKind.LeftButtonPressed:
                case PointerUpdateKind.LeftButtonReleased:
                    return 1;
                case PointerUpdateKind.RightButtonPressed:
                case PointerUpdateKind.RightButtonReleased:
                    return 2;
                case PointerUpdateKind.MiddleButtonPressed:
                case PointerUpdateKind.MiddleButtonReleased:
                    return 4;
                default:
                    return 0;
            }
        }

        private void OnMouseCaptureChanged(bool captured)
        {
            if (_window == null)
                return;
            Dispatcher.UIThread.Post(() =>
            {
                if (_window == null)
                    return;
                _window.Surface.Cursor = captured ? new Cursor(StandardCursorType.None) : Cursor.Default;
            });
        }

        #endregion

        #region Presenter

        private void OnFrameReady()
        {
            if (Interlocked.Exchange(ref _presentPending, 1) != 0)
                return;
            Dispatcher.UIThread.Post(PresentFrame, DispatcherPriority.Render);
        }

        private void PresentFrame()
        {
            Interlocked.Exchange(ref _presentPending, 0);
            var video = _video;
            if (video == null || _window == null || _quit)
                return;
            if (!video.TryConsumeFrame(out var buffer, out var size, out var ratio))
                return;
            _window.Surface.UpdateFrame(buffer, size.Width, size.Height, ratio);
        }

        #endregion
    }
}
