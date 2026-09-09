using System;
using System.ComponentModel;
using Kozynax.UI;
using ZXMAK2.Engine.Interfaces;
using ZXMAK2.Hardware;
using ZXMAK2.Hardware.Circuits.Fdd;
using ZXMAK2.Host.Entities;
using ZXMAK2.Host.Interfaces;
using ZXMAK2.Host.Presentation.Interfaces;

namespace ZXMAK2.Host.SdlBackend.Views
{
    /// <summary>
    /// Thin <see cref="IView"/> adapters so existing ViewHolder commands can open
    /// Kozui <c>ViewDescription</c> dialogs via <c>ShowDialog</c> (no Terminal loop here).
    /// </summary>
    public abstract class ModalKozuiToolView : IView
    {
        public event EventHandler ViewClosed;
        public event CancelEventHandler ViewClosing;

        public void Show(IMainView parent)
        {
            try
            {
                ShowDialogCore(parent);
            }
            finally
            {
                ViewClosed?.Invoke(this, EventArgs.Empty);
            }
        }

        protected abstract void ShowDialogCore(IMainView parent);

        public void Hide()
        {
        }

        public void Close()
        {
        }

        public void Dispose()
        {
        }

        protected void RaiseViewClosing(CancelEventArgs args)
            => ViewClosing?.Invoke(this, args);
    }

    public sealed class AboutToolView : ModalKozuiToolView, IAboutView
    {
        protected override void ShowDialogCore(IMainView parent)
            => new AboutDialog().ShowDialog(parent);
    }

    public sealed class KeyboardHelpToolView : ModalKozuiToolView, IKeyboardView
    {
        protected override void ShowDialogCore(IMainView parent)
            => new KeyboardHelpDialog().ShowDialog(parent);
    }

    public sealed class MemoryMapToolView : ModalKozuiToolView, IMemoryMapView
    {
        private MemoryBase _memory;

        public void Init(MemoryBase mem)
            => _memory = mem ?? throw new ArgumentNullException(nameof(mem));

        protected override void ShowDialogCore(IMainView parent)
        {
            if (_memory == null)
                return;
            new MemoryMap(_memory).ShowDialog(parent);
        }
    }

    public sealed class FddDebugToolView : ModalKozuiToolView, IFddDebugView
    {
        private Wd1793 _wd1793;

        public void Init(Wd1793 debugTarget)
            => _wd1793 = debugTarget;

        protected override void ShowDialogCore(IMainView parent)
            => new FddDebugDialog(_wd1793).ShowDialog(parent);
    }

    public sealed class TapeToolView : ModalKozuiToolView, ITapeView
    {
        private TapeSettings _settings;

        public void Init(TapeSettings tapeSettings)
            => _settings = tapeSettings ?? throw new ArgumentNullException(nameof(tapeSettings));

        public DlgResult ShowDialog(object owner)
        {
            Show(owner as IMainView);
            return DlgResult.OK;
        }

        protected override void ShowDialogCore(IMainView parent)
        {
            if (_settings == null)
                return;
            try
            {
                _settings.ShowDialog(parent);
            }
            finally
            {
                _settings.Close();
                _settings = null;
            }
        }
    }

    public sealed class MachineSettingsToolView : ModalKozuiToolView, IMachineSettingsView
    {
        private MachineSettings _settings;

        public void Init(MachineSettings machineSettings)
        {
            _settings = machineSettings ?? throw new ArgumentNullException(nameof(machineSettings));
            _settings.Init();
        }

        public void Init(IHostService host, IVirtualMachine vm)
        {
            if (_settings == null)
                throw new InvalidOperationException("Init(MachineSettings) must be called first.");
            _settings.Init(host, vm);
        }

        public DlgResult ShowDialog(object owner)
        {
            if (_settings == null)
                return DlgResult.Cancel;
            return _settings.ShowDialog(owner);
        }

        protected override void ShowDialogCore(IMainView parent)
            => ShowDialog(parent);
    }
}
