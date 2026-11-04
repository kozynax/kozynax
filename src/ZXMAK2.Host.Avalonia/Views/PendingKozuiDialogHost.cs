using System;
using Kozui.Abstract;
using Kozui.Interfaces;
using ZXMAK2.Host.Entities;

namespace ZXMAK2.Host.AvaloniaBackend.Views
{
    /// <summary>
    /// Placeholder <see cref="IViewImplementation{T}"/> until the Kozui → Avalonia presenter
    /// exists. Validates the dialog contract, tells the user, and cancels.
    /// </summary>
    public sealed class PendingKozuiDialogHost<T> : IViewImplementation<T>
        where T : class
    {
        private readonly AvaloniaRuntimeContext _runtime;
        private T _ui;

        public PendingKozuiDialogHost(AvaloniaRuntimeContext runtime)
        {
            _runtime = runtime;
        }

        public void Init(T ui)
        {
            _ui = ui ?? throw new ArgumentNullException(nameof(ui));
            // Throws early if the dialog does not follow the Kozui convention.
            _ = new KozuiDialogBinding(ui);
        }

        public DlgResult ShowDialog(object owner)
        {
            var name = _ui?.GetType().Name ?? typeof(T).Name;
            Logger.Warn("Avalonia host: Kozui dialog {0} is not implemented yet", name);
            MessageWindow.Show(
                _runtime?.MainWindow,
                "Not available yet",
                name + " is not implemented in the Avalonia host yet.",
                DlgButtonSet.OK);
            return DlgResult.Cancel;
        }

        public void Dispose()
        {
            _ui = null;
        }
    }
}
