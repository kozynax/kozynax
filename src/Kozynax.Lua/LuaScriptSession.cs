using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using MoonSharp.Interpreter;
using ZXMAK2.Engine;
using ZXMAK2.Engine.Entities;
using ZXMAK2.Engine.Interfaces;
using ZXMAK2.Host.Entities;

namespace Kozynax.Lua
{
    /// <summary>
    /// One script run. Headless runs own the machine. The windowed host passes
    /// the live <see cref="Spectrum"/> and keeps the frame loop on screen.
    /// </summary>
    public sealed class LuaScriptSession : IDisposable
    {
        private const int KeyDownFrames = 12;
        private const int WaitPollFrames = 100;
        private const int KeyUpFrames = 16;
        private const int EnterUpFrames = 30;

        private readonly string _scriptDirectory;
        private readonly bool _ownsMachine;
        private readonly IScriptHost _host;
        private readonly List<LuaBreakpoint> _breakpoints = new List<LuaBreakpoint>();
        private readonly ScriptJoystickState _joystick = new ScriptJoystickState();
        private Spectrum _machine;
        private int? _exitCode;
        private bool _maximumSpeed;
        private bool _stop;

        public LuaScriptSession(string scriptDirectory)
            : this(scriptDirectory, null, null)
        {
        }

        public LuaScriptSession(string scriptDirectory, Spectrum machine, IScriptHost host)
        {
            _scriptDirectory = scriptDirectory ?? "";
            _ownsMachine = machine == null;
            _host = host;
            _machine = machine;
            Keys = new ScriptKeyboardState();
            Cpu = new LuaCpu();
            Emulator = new EmulatorApi(this);
            Machine = new MachineApi(this);
            Tape = new TapeApi(this);
            Debugger = new DebuggerApi(this);
            HostPacesFrames = host != null;
            if (machine != null)
            {
                Cpu.Bind(machine.CPU);
                AttachInput();
            }
            MaximumSpeed = true;
        }

        internal ScriptKeyboardState Keys { get; }
        internal ScriptJoystickState Stick { get { return _joystick; } }
        public LuaCpu Cpu { get; }
        public EmulatorApi Emulator { get; }
        public MachineApi Machine { get; }
        public TapeApi Tape { get; }
        public DebuggerApi Debugger { get; }
        public bool HostPacesFrames { get; set; }
        public bool IsHeadless { get { return _host == null; } }

        public bool MaximumSpeed
        {
            get { return _maximumSpeed; }
            set
            {
                _maximumSpeed = value;
                if (_host != null)
                    _host.SetMaximumSpeed(value);
            }
        }
        public bool HasExit { get { return _exitCode.HasValue; } }
        public int ExitCode { get { return _exitCode.GetValueOrDefault(); } }

        public void Dispose()
        {
            if (_ownsMachine && _machine != null)
            {
                _machine.Dispose();
                _machine = null;
            }
        }

        /// <summary>
        /// Drop script keys and breakpoints so the windowed host can take the machine back.
        /// </summary>
        public void ReleaseControl()
        {
            if (_machine == null)
                return;
            _machine.DebugClearBreakpoints();
            _breakpoints.Clear();
            Keys.Clear();
            var keyboard = _machine.BusManager.FindDevice<IKeyboardDevice>();
            if (keyboard != null)
                keyboard.KeyboardState = Keys;
            _joystick.Clear();
            foreach (var joy in _machine.BusManager.FindDevices<IJoystickDevice>())
                joy.JoystickState = _joystick;
            if (_machine.IsRunning)
                _machine.IsRunning = false;
        }

        public void SetMachine(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ScriptRuntimeException("Machine.Set name is empty");

            var machines = new MachinesConfig();
            machines.Load();
            XmlNode match = null;
            foreach (var busName in machines.GetNames())
            {
                var node = machines.GetConfig(busName);
                if (node == null)
                    continue;
                string modelId = null;
                if (node.Attributes != null && node.Attributes["modelId"] != null)
                    modelId = node.Attributes["modelId"].InnerText;
                if (string.Equals(busName, name, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(modelId, name, StringComparison.OrdinalIgnoreCase))
                {
                    match = node;
                    break;
                }
            }
            if (match == null)
                throw new ScriptRuntimeException("unknown machine '" + name + "'");

            if (_ownsMachine)
            {
                if (_machine != null)
                    _machine.Dispose();
                var created = new Spectrum();
                created.Init();
                _machine = created;
            }

            if (_host != null)
                _host.LoadBus(match);
            else
                _machine.BusManager.LoadConfigXml(match);
            Cpu.Bind(_machine.CPU);
            AttachInput();
            ReapplyBreakpoints();
        }

        public void Reset()
        {
            var machine = RequireMachine();
            machine.IsRunning = true;
            machine.DebugReset();
        }

        public void Load(string path)
        {
            var machine = RequireMachine();
            var fullPath = ResolvePath(path);
            var ext = Path.GetExtension(fullPath);
            var serializer = machine.BusManager.LoadManager.GetSerializer(ext);
            if (serializer == null || !serializer.CanDeserialize)
                throw new ScriptRuntimeException("unsupported file '" + path + "'");

            using (var stream = File.OpenRead(fullPath))
                serializer.Deserialize(stream);

            var tape = machine.BusManager.FindDevice<ITapeDevice>();
            if (tape != null && tape.Blocks.Count > 0)
                tape.Rewind();
        }

        public ITapeDevice TapeDevice
        {
            get
            {
                var machine = RequireMachine();
                var tape = machine.BusManager.FindDevice<ITapeDevice>();
                if (tape == null)
                    throw new ScriptRuntimeException("this machine has no tape device");
                return tape;
            }
        }

        public void AddBreakpoint(int address, Closure callback)
        {
            if (callback == null)
                throw new ScriptRuntimeException("AddBreakpoint expects a function");
            if (address < 0 || address > 0xFFFF)
                throw new ScriptRuntimeException("breakpoint address out of range");

            var addr = (ushort)address;
            foreach (var existing in _breakpoints)
            {
                if (existing.Address != addr)
                    continue;
                existing.Callback = callback;
                return;
            }

            var entry = new LuaBreakpoint
            {
                Address = addr,
                Callback = callback,
                Engine = new Breakpoint(addr),
            };
            _breakpoints.Add(entry);
            if (_machine != null)
                _machine.DebugAddBreakpoint(entry.Engine);
        }

        public string ReadMemory(int address, int length)
        {
            if (length < 0)
                throw new ScriptRuntimeException("ReadMemory length is negative");
            var machine = RequireMachine();
            var memory = machine.BusManager.FindDevice<IMemoryDevice>();
            if (memory == null)
                throw new ScriptRuntimeException("this machine has no memory");

            var bytes = new char[length];
            for (var i = 0; i < length; i++)
                bytes[i] = (char)memory.RDMEM_DBG((ushort)(address + i));
            return new string(bytes);
        }

        public void Exit(int code)
        {
            _exitCode = code;
            throw new LuaExitException(code);
        }

        /// <summary>
        /// Returns from the current <see cref="Wait"/> without ending the process.
        /// </summary>
        public void Stop()
        {
            _stop = true;
        }

        public void Wait(int frames, Closure until)
        {
            if (frames < 0)
                throw new ScriptRuntimeException("Wait frame count is negative");
            if (until == null)
            {
                RunFrames(frames);
                return;
            }

            var left = frames;
            while (left > 0)
            {
                var step = left < WaitPollFrames ? left : WaitPollFrames;
                RunFrames(step);
                if (_stop)
                    return;
                left -= step;
                if (until.Call().CastToBool())
                    return;
            }
        }

        public void TapKey(Key key)
        {
            Keys.Press(key);
            RefreshKeyboard();
            RunFrames(KeyDownFrames);
            Keys.Unpress();
            RefreshKeyboard();
            RunFrames(key == Key.Return ? EnterUpFrames : KeyUpFrames);
        }

        internal void TapJoy(ScriptJoy direction)
        {
            _joystick.Press(direction);
            RefreshJoystick();
            RunFrames(KeyDownFrames);
            _joystick.Unpress();
            RefreshJoystick();
            RunFrames(KeyUpFrames);
        }

        public void RefreshJoystick()
        {
            var machine = RequireMachine();
            var devices = machine.BusManager.FindDevices<IJoystickDevice>();
            if (devices.Count == 0)
                throw new ScriptRuntimeException("this machine has no Kempston joystick");
            foreach (var joy in devices)
                joy.JoystickState = _joystick;
        }

        public void RefreshKeyboard()
        {
            var machine = RequireMachine();
            var keyboard = machine.BusManager.FindDevice<IKeyboardDevice>();
            if (keyboard == null)
                throw new ScriptRuntimeException("this machine has no keyboard");
            keyboard.KeyboardState = Keys;
        }

        public void RunFrames(int count)
        {
            var machine = RequireMachine();
            for (var i = 0; i < count; i++)
            {
                if (_stop)
                    return;
                if (!machine.IsRunning)
                    machine.IsRunning = true;
                machine.ExecuteFrame();
                if (!machine.IsRunning)
                {
                    DispatchBreakpoint();
                    if (_exitCode.HasValue)
                        throw new LuaExitException(_exitCode.Value);
                    if (_stop)
                        return;

                    machine.DebugClearBreakpoints();
                    machine.IsRunning = true;
                    machine.ExecuteFrame();
                    ReapplyBreakpoints();
                }

                if (!MaximumSpeed && !HostPacesFrames)
                    System.Threading.Thread.Sleep(20);
            }
        }

        private void DispatchBreakpoint()
        {
            var pc = _machine.CPU.regs.PC;
            foreach (var bp in _breakpoints)
            {
                if (bp.Address != pc || bp.Callback == null)
                    continue;
                bp.Callback.Call(Cpu);
                return;
            }
        }

        private void ReapplyBreakpoints()
        {
            if (_machine == null)
                return;
            _machine.DebugClearBreakpoints();
            foreach (var bp in _breakpoints)
                _machine.DebugAddBreakpoint(bp.Engine);
        }

        private void AttachInput()
        {
            var keyboard = _machine.BusManager.FindDevice<IKeyboardDevice>();
            if (keyboard != null)
                keyboard.KeyboardState = Keys;
            foreach (var joy in _machine.BusManager.FindDevices<IJoystickDevice>())
                joy.JoystickState = _joystick;
        }

        private Spectrum RequireMachine()
        {
            if (_machine == null)
                throw new ScriptRuntimeException("call Machine.Set first");
            return _machine;
        }

        private string ResolvePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ScriptRuntimeException("Emulator.Load path is empty");

            if (Path.IsPathRooted(path))
            {
                if (!File.Exists(path))
                    throw new ScriptRuntimeException("file not found: " + path);
                return path;
            }

            var besideScript = Path.GetFullPath(Path.Combine(_scriptDirectory, path));
            if (File.Exists(besideScript))
                return besideScript;
            var fromCwd = Path.GetFullPath(path);
            if (File.Exists(fromCwd))
                return fromCwd;
            throw new ScriptRuntimeException("file not found: " + path);
        }

        private sealed class LuaBreakpoint
        {
            public ushort Address;
            public Closure Callback;
            public Breakpoint Engine;
        }
    }
}
