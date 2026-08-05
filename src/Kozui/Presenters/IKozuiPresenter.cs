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
        public KozuiInput(KozuiInputKind kind, KozuiInputKey key = KozuiInputKey.None)
        {
            Kind = kind;
            Key = key;
        }

        public KozuiInputKind Kind { get; }
        public KozuiInputKey Key { get; }

        public static KozuiInput KeyDown(KozuiInputKey key) => new KozuiInput(KozuiInputKind.KeyDown, key);
    }

    public enum KozuiInputKind
    {
        None = 0,
        KeyDown,
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
    }
}
