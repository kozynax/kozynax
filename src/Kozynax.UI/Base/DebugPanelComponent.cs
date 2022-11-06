using System;

namespace Kozynax.UI.Base
{
    public abstract class DebugPanelComponent
    {
        public event EventHandler Redraw;
        public void Update() => Redraw?.Invoke(this, EventArgs.Empty);
    }
}