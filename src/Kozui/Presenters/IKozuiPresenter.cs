using ZXMAK2.Host.WinForms.Lib.Layout;

namespace ZXMAK2.Host.WinForms.Lib.Presenters
{
    /// <summary>
    /// Host bridge: attach a Kozui tree, measure/arrange in cell units, paint, route input.
    /// </summary>
    public interface IKozuiPresenter
    {
        void Attach(KozuiControl root);
        void MeasureArrange(LayoutSize availableCells);
        void Render();
        bool RouteInput(KozuiInput input);
    }

    public readonly struct KozuiInput
    {
        public KozuiInput(
            KozuiInputKind kind,
            KozuiInputKey key = KozuiInputKey.None,
            int x = 0,
            int y = 0,
            KozuiMouseButton button = KozuiMouseButton.None,
            int wheelDelta = 0)
        {
            Kind = kind;
            Key = key;
            X = x;
            Y = y;
            Button = button;
            WheelDelta = wheelDelta;
        }

        public KozuiInputKind Kind { get; }
        public KozuiInputKey Key { get; }
        public int X { get; }
        public int Y { get; }
        public KozuiMouseButton Button { get; }
        public int WheelDelta { get; }

        public static KozuiInput KeyDown(KozuiInputKey key)
            => new KozuiInput(KozuiInputKind.KeyDown, key);

        public static KozuiInput MouseDown(int x, int y, KozuiMouseButton button = KozuiMouseButton.Left)
            => new KozuiInput(KozuiInputKind.MouseDown, x: x, y: y, button: button);

        public static KozuiInput MouseUp(int x, int y, KozuiMouseButton button = KozuiMouseButton.Left)
            => new KozuiInput(KozuiInputKind.MouseUp, x: x, y: y, button: button);

        public static KozuiInput MouseMove(int x, int y)
            => new KozuiInput(KozuiInputKind.MouseMove, x: x, y: y);

        public static KozuiInput MouseWheel(int x, int y, int delta)
            => new KozuiInput(KozuiInputKind.MouseWheel, x: x, y: y, wheelDelta: delta);
    }

    public enum KozuiInputKind
    {
        None = 0,
        KeyDown,
        MouseDown,
        MouseUp,
        MouseMove,
        MouseWheel,
    }

    public enum KozuiMouseButton
    {
        None = 0,
        Left,
        Right,
        Middle,
    }

    public enum KozuiInputKey
    {
        None = 0,
        Escape,
        Enter,
        Tab,
        Left,
        Right,
        Up,
        Down,
        PageUp,
        PageDown,
    }
}
