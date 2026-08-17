using System;
using System.ComponentModel;
using Kozynax.UI;
using ZXMAK2.Hardware;
using ZXMAK2.Hardware.Circuits.Fdd;
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
}
