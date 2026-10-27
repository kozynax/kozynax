using System.Collections.Generic;
using ZXMAK2.Host.Entities;
using ZXMAK2.Host.Interfaces;

namespace Kozynax.Lua
{
    /// <summary>
    /// Keys currently down. Held modifiers stay across taps.
    /// </summary>
    sealed class ScriptKeyboardState : IKeyboardState
    {
        private readonly HashSet<Key> _held = new HashSet<Key>();
        private readonly HashSet<Key> _down = new HashSet<Key>();

        public bool this[Key key]
        {
            get { return _held.Contains(key) || _down.Contains(key); }
        }

        public void Hold(Key key)
        {
            _held.Add(key);
        }

        public void Release(Key key)
        {
            _held.Remove(key);
        }

        public void Press(Key key)
        {
            _down.Add(key);
        }

        public void Unpress()
        {
            _down.Clear();
        }

        public void Clear()
        {
            _held.Clear();
            _down.Clear();
        }
    }

    enum ScriptJoy
    {
        Left,
        Right,
        Up,
        Down,
        Fire
    }

    /// <summary>
    /// Kempston directions currently down. Held directions stay across taps.
    /// </summary>
    sealed class ScriptJoystickState : IJoystickState
    {
        private readonly HashSet<ScriptJoy> _held = new HashSet<ScriptJoy>();
        private readonly HashSet<ScriptJoy> _down = new HashSet<ScriptJoy>();

        public bool IsLeft { get { return Pressed(ScriptJoy.Left); } }
        public bool IsRight { get { return Pressed(ScriptJoy.Right); } }
        public bool IsUp { get { return Pressed(ScriptJoy.Up); } }
        public bool IsDown { get { return Pressed(ScriptJoy.Down); } }
        public bool IsFire { get { return Pressed(ScriptJoy.Fire); } }

        public void Hold(ScriptJoy direction)
        {
            _held.Add(direction);
        }

        public void Release(ScriptJoy direction)
        {
            _held.Remove(direction);
        }

        public void Press(ScriptJoy direction)
        {
            _down.Add(direction);
        }

        public void Unpress()
        {
            _down.Clear();
        }

        public void Clear()
        {
            _held.Clear();
            _down.Clear();
        }

        private bool Pressed(ScriptJoy direction)
        {
            return _held.Contains(direction) || _down.Contains(direction);
        }
    }
}
