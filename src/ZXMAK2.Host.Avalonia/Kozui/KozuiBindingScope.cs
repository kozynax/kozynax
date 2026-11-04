using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace ZXMAK2.Host.AvaloniaBackend.Kozui
{
    /// <summary>
    /// Owns the event subscriptions created while mapping one Kozui subtree,
    /// so a <c>Placeholder.Content</c> swap can tear the old subtree down cleanly.
    /// </summary>
    internal sealed class KozuiBindingScope : IDisposable
    {
        private readonly List<Action> _disposers = new List<Action>();
        private bool _disposed;

        public void Track(Action dispose)
        {
            if (dispose != null)
                _disposers.Add(dispose);
        }

        /// <summary>Apply now and on every change of <paramref name="propertyName"/> (or a null/all-props notification).</summary>
        public void BindOneWay(INotifyPropertyChanged source, string propertyName, Action apply)
        {
            apply();
            PropertyChangedEventHandler handler = (s, e) =>
            {
                if (e.PropertyName == null || e.PropertyName == propertyName)
                    apply();
            };
            source.PropertyChanged += handler;
            Track(() => source.PropertyChanged -= handler);
        }

        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;
            for (var i = _disposers.Count - 1; i >= 0; i--)
            {
                try
                {
                    _disposers[i]();
                }
                catch (Exception ex)
                {
                    Logger.Error(ex);
                }
            }
            _disposers.Clear();
        }
    }
}
