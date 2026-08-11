namespace ZXMAK2.Host.Terminal
{
    /// <summary>
    /// Host-neutral keys for overlay screens (not the Spectrum matrix).
    /// </summary>
    public enum TerminalKey
    {
        Unknown = 0,
        Escape,
        Enter,
        Backspace,
        Up,
        Down,
        Left,
        Right,
        PageUp,
        PageDown,
        Tab,
        F3,
        F5,
        F7,
        F8,
        F9,
        // Letters / digits reserved for future Kozui text input
        A, B, C, D, E, F, G, H, I, J, K, L, M,
        N, O, P, Q, R, S, T, U, V, W, X, Y, Z,
    }
}
