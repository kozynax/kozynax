using System;
using System.Reflection;
using ZXMAK2.Host.Entities;
using ZXMAK2.Host.WinForms.Lib;
using Timer = ZXMAK2.Host.WinForms.Lib.Timer;

namespace Kozui.Abstract
{
    /// <summary>
    /// Convention binding for Kozui dialogs: <c>Root</c>, <c>CloseRequested</c>,
    /// optional <c>DialogResult</c> / <c>UpdateTimer</c>, and <see cref="KozuiDialogAttribute"/>.
    /// </summary>
    public sealed class KozuiDialogBinding
    {
        private readonly object _ui;
        private readonly PropertyInfo _root;
        private readonly EventInfo _closeRequested;
        private readonly PropertyInfo _dialogResult;
        private readonly PropertyInfo _updateTimer;

        public KozuiDialogAttribute Options { get; }

        public KozuiDialogBinding(object ui)
        {
            _ui = ui ?? throw new ArgumentNullException(nameof(ui));
            var type = ui.GetType();
            Options = type.GetCustomAttribute<KozuiDialogAttribute>(inherit: true)
                      ?? new KozuiDialogAttribute();

            _root = type.GetProperty("Root", BindingFlags.Instance | BindingFlags.Public);
            if (_root == null || !typeof(KozuiControl).IsAssignableFrom(_root.PropertyType))
            {
                throw new InvalidOperationException(
                    type.FullName + " must expose a public Root KozuiControl property.");
            }

            _closeRequested = type.GetEvent("CloseRequested", BindingFlags.Instance | BindingFlags.Public);
            if (_closeRequested == null)
            {
                throw new InvalidOperationException(
                    type.FullName + " must expose a public CloseRequested event.");
            }

            _dialogResult = type.GetProperty("DialogResult", BindingFlags.Instance | BindingFlags.Public);
            if (_dialogResult != null && _dialogResult.PropertyType != typeof(DlgResult))
                _dialogResult = null;

            _updateTimer = type.GetProperty("UpdateTimer", BindingFlags.Instance | BindingFlags.Public);
            if (_updateTimer != null && _updateTimer.PropertyType != typeof(Timer))
                _updateTimer = null;
        }

        public KozuiControl Root => (KozuiControl)_root.GetValue(_ui);

        public Timer UpdateTimer
            => _updateTimer != null ? (Timer)_updateTimer.GetValue(_ui) : null;

        public DlgResult DialogResult
        {
            get
            {
                if (_dialogResult != null)
                    return (DlgResult)_dialogResult.GetValue(_ui);
                return DlgResult.OK;
            }
        }

        public void AddCloseHandler(EventHandler handler)
            => _closeRequested.AddEventHandler(_ui, handler);

        public void RemoveCloseHandler(EventHandler handler)
            => _closeRequested.RemoveEventHandler(_ui, handler);
    }
}
