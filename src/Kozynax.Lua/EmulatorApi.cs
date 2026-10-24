using System;
using MoonSharp.Interpreter;

namespace Kozynax.Lua
{
    /// <summary>
    /// Lua <c>Emulator</c> table.
    /// </summary>
    public sealed class EmulatorApi
    {
        private readonly LuaScriptSession _session;

        internal EmulatorApi(LuaScriptSession session)
        {
            _session = session;
        }

        public bool MaximumSpeed
        {
            get { return _session.MaximumSpeed; }
            set { _session.MaximumSpeed = value; }
        }

        public void Load(string path)
        {
            _session.Load(path);
        }

        public void WaitFrames(int frames)
        {
            _session.Wait(frames, null);
        }

        // Polled every 100 frames. Return true when the wait should stop.
        public void WaitFrames(int frames, Closure until)
        {
            _session.Wait(frames, until);
        }

        // j+<SS>pp-<SS><ENTER>
        //   J, hold symbol shift, P, P, release symbol shift, Enter.
        // <CS> is caps shift, <SPACE> is space.
        public void SendKeys(string sequence)
        {
            if (sequence == null)
                sequence = "";

            var i = 0;
            while (i < sequence.Length)
            {
                var mark = sequence[i];
                if (mark == '+' || mark == '-')
                {
                    i++;
                    var token = ReadToken(sequence, ref i);
                    var key = ParseKey(token);
                    if (mark == '+')
                        _session.Keys.Hold(key);
                    else
                        _session.Keys.Release(key);
                    _session.RefreshKeyboard();
                    continue;
                }

                _session.TapKey(ParseKey(ReadToken(sequence, ref i)));
            }
        }

        public void SendKempston(string sequence)
        {
            if (sequence == null)
                sequence = "";

            var i = 0;
            while (i < sequence.Length)
            {
                var mark = sequence[i];
                if (mark == '+' || mark == '-')
                {
                    i++;
                    var direction = ParseJoy(ReadToken(sequence, ref i));
                    if (mark == '+')
                        _session.Stick.Hold(direction);
                    else
                        _session.Stick.Release(direction);
                    _session.RefreshJoystick();
                    continue;
                }

                _session.TapJoy(ParseJoy(ReadToken(sequence, ref i)));
            }
        }

        public bool IsHeadless
        {
            get { return _session.IsHeadless; }
        }

        public void Stop()
        {
            _session.Stop();
        }

        public void Exit(int code)
        {
            _session.Exit(code);
        }

        private static string ReadToken(string sequence, ref int i)
        {
            if (i >= sequence.Length)
                throw new ScriptRuntimeException("key sequence ended unexpectedly");

            if (sequence[i] != '<')
            {
                var ch = sequence.Substring(i, 1);
                i++;
                return ch;
            }

            var end = sequence.IndexOf('>', i + 1);
            if (end < 0)
                throw new ScriptRuntimeException("missing '>' in key sequence");

            var token = sequence.Substring(i + 1, end - i - 1);
            i = end + 1;
            return token;
        }

        private static ZXMAK2.Host.Entities.Key ParseKey(string token)
        {
            if (string.IsNullOrEmpty(token))
                throw new ScriptRuntimeException("empty key in SendKeys");

            var name = token.ToUpperInvariant();
            switch (name)
            {
                case "SS":
                case "SYMBOLSHIFT":
                    return ZXMAK2.Host.Entities.Key.RightShift;
                case "CS":
                case "CAPSSHIFT":
                    return ZXMAK2.Host.Entities.Key.LeftShift;
                case "ENTER":
                case "RETURN":
                case "CR":
                    return ZXMAK2.Host.Entities.Key.Return;
                case "SPACE":
                    return ZXMAK2.Host.Entities.Key.Space;
            }

            if (name.Length == 1)
            {
                var c = name[0];
                ZXMAK2.Host.Entities.Key key;
                if (c >= 'A' && c <= 'Z' && Enum.TryParse(c.ToString(), out key))
                    return key;
                if (c >= '0' && c <= '9' && Enum.TryParse("D" + c, out key))
                    return key;
            }

            throw new ScriptRuntimeException("unknown key '" + token + "'");
        }

        private static ScriptJoy ParseJoy(string token)
        {
            if (string.IsNullOrEmpty(token))
                throw new ScriptRuntimeException("empty direction in SendKempston");

            switch (token.ToUpperInvariant())
            {
                case "LEFT":
                    return ScriptJoy.Left;
                case "RIGHT":
                    return ScriptJoy.Right;
                case "UP":
                    return ScriptJoy.Up;
                case "DOWN":
                    return ScriptJoy.Down;
                case "FIRE":
                    return ScriptJoy.Fire;
            }

            throw new ScriptRuntimeException("unknown Kempston direction '" + token + "'");
        }
    }
}
