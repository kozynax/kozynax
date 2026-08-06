using System;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace ZXMAK2.Host.Terminal
{
    /// <summary>
    /// Linux/macOS console implementation of <see cref="ITerminal"/> (ANSI + stdin).
    /// Exposes a pixel-sized surface (cols×8, rows×8) so <see cref="TerminalKozuiPresenter"/> cell math is unchanged.
    /// </summary>
    public sealed class StdioTerminal : TerminalBase, IDisposable
    {
        private const int CellPx = TerminalFont.GlyphWidth;
        private const byte DefaultFgR = 230, DefaultFgG = 230, DefaultFgB = 230;
        private const byte DefaultBgR = 16, DefaultBgG = 18, DefaultBgB = 28;

        private readonly ConcurrentQueue<TerminalEvent> _events = new ConcurrentQueue<TerminalEvent>();
        private readonly object _bufferLock = new object();
        private readonly StringBuilder _presentBuilder = new StringBuilder(4096);

        private Cell[] _cells = Array.Empty<Cell>();
        private Cell[] _backdropCells;
        private int _cols;
        private int _rows;
        private int _uiDepth;
        private bool _altScreen;
        private bool _disposed;
        private Thread _inputThread;
        private volatile bool _inputStop;

        public static bool IsInteractive
        {
            get
            {
                if (Console.IsInputRedirected || Console.IsOutputRedirected)
                    return false;
                if (!(OperatingSystem.IsLinux() || OperatingSystem.IsMacOS()))
                    return false;
                try
                {
                    return Isatty(STDIN_FILENO) != 0 && Isatty(STDOUT_FILENO) != 0;
                }
                catch
                {
                    return false;
                }
            }
        }

        public StdioTerminal()
        {
            RefreshSize();
            EnsureBuffer();
        }

        public override bool IsAvailable => !_disposed && IsInteractive;

        public override bool HasBackdrop => _backdropCells != null;

        public override int Width
        {
            get
            {
                RefreshSize();
                return Math.Max(1, _cols) * CellPx;
            }
        }

        public override int Height
        {
            get
            {
                RefreshSize();
                return Math.Max(1, _rows) * CellPx;
            }
        }

        public void RequestQuit()
            => _events.Enqueue(TerminalEvent.QuitEvent);

        public override void Clear(TerminalColor color)
        {
            lock (_bufferLock)
            {
                EnsureBuffer();
                if (_backdropCells != null && _backdropCells.Length == _cells.Length)
                {
                    Array.Copy(_backdropCells, _cells, _cells.Length);
                    // Dim: pull backgrounds toward black so the modal frame stands out.
                    for (var i = 0; i < _cells.Length; i++)
                    {
                        var c = _cells[i];
                        _cells[i] = new Cell(
                            c.Ch,
                            (byte)(c.FgR * 2 / 3), (byte)(c.FgG * 2 / 3), (byte)(c.FgB * 2 / 3),
                            (byte)(c.BgR / 3), (byte)(c.BgG / 3), (byte)(c.BgB / 3));
                    }
                    return;
                }

                var cell = new Cell(' ', DefaultFgR, DefaultFgG, DefaultFgB, color.R, color.G, color.B);
                for (var i = 0; i < _cells.Length; i++)
                    _cells[i] = cell;
            }
        }

        public override void CaptureBackdrop()
        {
            lock (_bufferLock)
            {
                EnsureBuffer();
                _backdropCells = new Cell[_cells.Length];
                Array.Copy(_cells, _backdropCells, _cells.Length);
            }
        }

        public override void ReleaseBackdrop()
            => _backdropCells = null;

        public override void FillRect(int x, int y, int width, int height, TerminalColor color)
        {
            if (width <= 0 || height <= 0)
                return;

            lock (_bufferLock)
            {
                EnsureBuffer();
                var x0 = Math.Max(0, x / CellPx);
                var y0 = Math.Max(0, y / CellPx);
                var x1 = Math.Min(_cols, (x + width + CellPx - 1) / CellPx);
                var y1 = Math.Min(_rows, (y + height + CellPx - 1) / CellPx);
                var fill = new Cell(' ', DefaultFgR, DefaultFgG, DefaultFgB, color.R, color.G, color.B);
                for (var row = y0; row < y1; row++)
                {
                    var rowOff = row * _cols;
                    for (var col = x0; col < x1; col++)
                        _cells[rowOff + col] = fill;
                }
            }
        }

        public override void DrawText(int x, int y, string text, int scale, TerminalColor color)
        {
            if (string.IsNullOrEmpty(text) || scale < 1)
                return;

            lock (_bufferLock)
            {
                EnsureBuffer();
                var col = x / CellPx;
                var row = y / CellPx;
                if (row < 0 || row >= _rows)
                    return;

                for (var i = 0; i < text.Length; i++)
                {
                    var c = col + i;
                    if (c < 0)
                        continue;
                    if (c >= _cols)
                        break;
                    var ch = text[i];
                    if (ch < 32 || ch > 126)
                        ch = '?';
                    var idx = row * _cols + c;
                    var bg = _cells[idx];
                    _cells[idx] = new Cell(ch, color.R, color.G, color.B, bg.BgR, bg.BgG, bg.BgB);
                }
            }
        }

        public override int MeasureTextWidth(string text, int scale)
            => (text?.Length ?? 0) * CellPx * Math.Max(scale, 1);

        public override void Present()
        {
            if (!IsAvailable)
                return;

            lock (_bufferLock)
            {
                EnsureBuffer();
                EnsureSessionStarted();

                _presentBuilder.Clear();
                _presentBuilder.Append("\x1b[H\x1b[?25l");

                byte lastFgR = 255, lastFgG = 255, lastFgB = 255;
                byte lastBgR = 255, lastBgG = 255, lastBgB = 255;
                var haveColor = false;

                for (var row = 0; row < _rows; row++)
                {
                    if (row > 0)
                        _presentBuilder.Append('\n');

                    var rowOff = row * _cols;
                    for (var col = 0; col < _cols; col++)
                    {
                        var cell = _cells[rowOff + col];
                        var ch = cell.Ch == '\0' ? ' ' : cell.Ch;
                        if (!haveColor
                            || cell.FgR != lastFgR || cell.FgG != lastFgG || cell.FgB != lastFgB
                            || cell.BgR != lastBgR || cell.BgG != lastBgG || cell.BgB != lastBgB)
                        {
                            AppendTrueColor(_presentBuilder, cell.FgR, cell.FgG, cell.FgB, cell.BgR, cell.BgG, cell.BgB);
                            lastFgR = cell.FgR; lastFgG = cell.FgG; lastFgB = cell.FgB;
                            lastBgR = cell.BgR; lastBgG = cell.BgG; lastBgB = cell.BgB;
                            haveColor = true;
                        }
                        _presentBuilder.Append(ch);
                    }
                }

                _presentBuilder.Append("\x1b[0m");
                Console.Out.Write(_presentBuilder.ToString());
                Console.Out.Flush();
            }
        }

        public override void Delay(int milliseconds)
            => Thread.Sleep(Math.Max(0, milliseconds));

        public override void PrepareForUiInput()
        {
            if (_uiDepth++ == 0)
            {
                EnsureSessionStarted();
                TerminalUiSession.NotifyEnter();
            }
        }

        public override void EndUiInput()
        {
            if (_uiDepth <= 0)
                return;
            if (--_uiDepth > 0)
                return;

            TerminalUiSession.NotifyLeave();
            // Restore the user's shell view when the outermost UI closes (Esc menu / dialogs).
            LeaveSession();
            StopInputThread();
        }

        public override bool PollEvent(out TerminalEvent terminalEvent)
            => _events.TryDequeue(out terminalEvent);

        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;
            StopInputThread();
            LeaveSession();
            ReleaseBackdrop();
            while (TerminalUiSession.UiDepth > 0)
                TerminalUiSession.NotifyLeave();
            _uiDepth = 0;
        }

        private void EnsureSessionStarted()
        {
            if (_altScreen)
                return;
            Console.Out.Write("\x1b[?1049h\x1b[H\x1b[2J\x1b[?25l");
            Console.Out.Flush();
            _altScreen = true;
            StartInputThread();
        }

        private void LeaveSession()
        {
            if (!_altScreen)
                return;
            Console.Out.Write("\x1b[?25h\x1b[?1049l\x1b[0m");
            Console.Out.Flush();
            _altScreen = false;
        }

        private void RefreshSize()
        {
            if (!GetWinSize(out var cols, out var rows) || cols < 1 || rows < 1)
            {
                try
                {
                    cols = Math.Max(1, Console.WindowWidth);
                    rows = Math.Max(1, Console.WindowHeight);
                }
                catch
                {
                    cols = 80;
                    rows = 24;
                }
            }
            _cols = Math.Max(20, cols);
            _rows = Math.Max(8, rows);
        }

        private void EnsureBuffer()
        {
            var need = _cols * _rows;
            if (_cells.Length == need)
                return;
            var next = new Cell[need];
            var fill = new Cell(' ', DefaultFgR, DefaultFgG, DefaultFgB, DefaultBgR, DefaultBgG, DefaultBgB);
            for (var i = 0; i < next.Length; i++)
                next[i] = fill;
            _cells = next;
        }

        private void StartInputThread()
        {
            if (_inputThread != null)
                return;
            _inputStop = false;
            _inputThread = new Thread(InputLoop)
            {
                IsBackground = true,
                Name = "StdioTerminal.Input",
            };
            _inputThread.Start();
        }

        private void StopInputThread()
        {
            _inputStop = true;
            var t = _inputThread;
            _inputThread = null;
            if (t != null && t.IsAlive)
                t.Join(200);
        }

        private void InputLoop()
        {
            try
            {
                while (!_inputStop && !_disposed)
                {
                    if (!Console.KeyAvailable)
                    {
                        Thread.Sleep(5);
                        continue;
                    }

                    var keyInfo = Console.ReadKey(intercept: true);
                    TryMapKey(keyInfo, out var terminalKey);
                    var ch = keyInfo.KeyChar;
                    if (char.IsControl(ch))
                        ch = '\0';
                    if (terminalKey == TerminalKey.Unknown && ch == '\0')
                        continue;

                    _events.Enqueue(TerminalEvent.KeyDown(terminalKey, ch));
                    if (terminalKey != TerminalKey.Unknown)
                        _events.Enqueue(TerminalEvent.KeyUp(terminalKey));
                }
            }
            catch
            {
                // stdin closed / disposed
            }
        }

        private static bool TryMapKey(ConsoleKeyInfo info, out TerminalKey key)
        {
            key = TerminalKey.Unknown;
            switch (info.Key)
            {
                case ConsoleKey.Escape: key = TerminalKey.Escape; return true;
                case ConsoleKey.Enter: key = TerminalKey.Enter; return true;
                case ConsoleKey.Backspace: key = TerminalKey.Backspace; return true;
                case ConsoleKey.UpArrow: key = TerminalKey.Up; return true;
                case ConsoleKey.DownArrow: key = TerminalKey.Down; return true;
                case ConsoleKey.LeftArrow: key = TerminalKey.Left; return true;
                case ConsoleKey.RightArrow: key = TerminalKey.Right; return true;
                case ConsoleKey.PageUp: key = TerminalKey.PageUp; return true;
                case ConsoleKey.PageDown: key = TerminalKey.PageDown; return true;
                case ConsoleKey.Tab: key = TerminalKey.Tab; return true;
                default:
                    var ch = char.ToUpperInvariant(info.KeyChar);
                    if (ch >= 'A' && ch <= 'Z')
                    {
                        key = (TerminalKey)((int)TerminalKey.A + (ch - 'A'));
                        return true;
                    }
                    return false;
            }
        }

        private static void AppendTrueColor(StringBuilder sb, byte fr, byte fg, byte fb, byte br, byte bg, byte bb)
        {
            sb.Append("\x1b[38;2;");
            sb.Append(fr); sb.Append(';'); sb.Append(fg); sb.Append(';'); sb.Append(fb);
            sb.Append("m\x1b[48;2;");
            sb.Append(br); sb.Append(';'); sb.Append(bg); sb.Append(';'); sb.Append(bb);
            sb.Append('m');
        }

        private static bool GetWinSize(out int cols, out int rows)
        {
            cols = 0;
            rows = 0;
            var ws = new Winsize();
            if (Ioctl(STDOUT_FILENO, TIOCGWINSZ, ref ws) != 0)
                return false;
            cols = ws.ws_col;
            rows = ws.ws_row;
            return cols > 0 && rows > 0;
        }

        private struct Cell
        {
            public readonly char Ch;
            public readonly byte FgR, FgG, FgB;
            public readonly byte BgR, BgG, BgB;

            public Cell(char ch, byte fgR, byte fgG, byte fgB, byte bgR, byte bgG, byte bgB)
            {
                Ch = ch;
                FgR = fgR; FgG = fgG; FgB = fgB;
                BgR = bgR; BgG = bgG; BgB = bgB;
            }
        }

        #region libc

        private const int STDIN_FILENO = 0;
        private const int STDOUT_FILENO = 1;
        private const uint TIOCGWINSZ = 0x5413;

        [StructLayout(LayoutKind.Sequential)]
        private struct Winsize
        {
            public ushort ws_row;
            public ushort ws_col;
            public ushort ws_xpixel;
            public ushort ws_ypixel;
        }

        [DllImport("libc", EntryPoint = "isatty")]
        private static extern int Isatty(int fd);

        [DllImport("libc", EntryPoint = "ioctl", SetLastError = true)]
        private static extern int Ioctl(int fd, uint request, ref Winsize winsize);

        #endregion
    }
}
