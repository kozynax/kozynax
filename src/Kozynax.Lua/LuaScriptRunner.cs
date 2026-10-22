using System;
using System.IO;
using Kozynax.Cli.Services;
using MoonSharp.Interpreter;
using ZXMAK2;
using ZXMAK2.Dependency;
using ZXMAK2.Engine;
using ZXMAK2.Engine.Cpu;
using ZXMAK2.Hardware.Circuits.Sound;
using ZXMAK2.Host.Interfaces;

namespace Kozynax.Lua
{
    /// <summary>
    /// Runs one Lua file. Without a host the machine is headless and the
    /// process exits with the script code. With an <see cref="IScriptHost"/>
    /// the script drives the machine already shown in the window.
    /// </summary>
    public static class LuaScriptRunner
    {
        public static int Run(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                Console.Error.WriteLine("script not found: " + path);
                return 1;
            }

            var fullPath = Path.GetFullPath(path);
            var resolver = new ResolverSimple();
            resolver.RegisterInstance<IResolver>(resolver);
            resolver.RegisterInstance<IUserMessage>(new ConsoleUserMessage());
            resolver.RegisterInstance<IUserQuery>(new ConsoleUserQuery());
            resolver.RegisterType<IPsgChip, PsgChip>();
            Locator.Init(resolver);

            LuaScriptSession session = null;
            try
            {
                session = new LuaScriptSession(Path.GetDirectoryName(fullPath));
                string error;
                return Execute(session, fullPath, out error);
            }
            finally
            {
                if (session != null)
                    session.Dispose();
                Locator.Shutdown();
            }
        }

        public static void Run(VirtualMachine vm, string path, IScriptHost host)
        {
            if (vm == null)
                return;
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                if (host != null)
                    host.ShowError("script not found: " + path);
                return;
            }

            var fullPath = Path.GetFullPath(path);
            try
            {
                vm.DoStop();
                var session = new LuaScriptSession(
                    Path.GetDirectoryName(fullPath),
                    vm.Spectrum,
                    host);
                string error;
                Execute(session, fullPath, out error);
                session.ReleaseControl();
                if (session.HasExit)
                {
                    Environment.ExitCode = session.ExitCode;
                    if (host != null)
                        host.Close();
                    return;
                }
                if (!string.IsNullOrEmpty(error) && host != null)
                    host.ShowError(error);
                vm.DoRun();
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                if (host != null)
                    host.ShowError(ex.Message);
                try
                {
                    if (!vm.IsRunning)
                        vm.DoRun();
                }
                catch (Exception resume)
                {
                    Logger.Error(resume);
                }
            }
        }

        /// <summary>
        /// Runs <paramref name="path"/> on an existing session. Locator must already be initialized.
        /// </summary>
        public static int Execute(LuaScriptSession session, string path, out string error)
        {
            error = null;
            if (session == null)
                throw new ArgumentNullException("session");
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                error = "script not found: " + path;
                Console.Error.WriteLine(error);
                return 1;
            }

            try
            {
                UserData.RegisterType<CpuRegs>();
                UserData.RegisterType<LuaCpu>();
                UserData.RegisterType<EmulatorApi>();
                UserData.RegisterType<MachineApi>();
                UserData.RegisterType<TapeApi>();
                UserData.RegisterType<DebuggerApi>();

                var script = new Script();
                script.Globals["Emulator"] = session.Emulator;
                script.Globals["Machine"] = session.Machine;
                script.Globals["Tape"] = session.Tape;
                script.Globals["Debugger"] = session.Debugger;
                script.Globals["Wait"] = DynValue.NewCallback((ctx, args) =>
                {
                    if (args.Count < 1 || args[0].Type != DataType.Number)
                        throw new ScriptRuntimeException("Wait(frames) expects a frame count");
                    session.Wait((int)args[0].Number);
                    return DynValue.Nil;
                });

                script.DoFile(path);
                return session.HasExit ? session.ExitCode : 0;
            }
            catch (Exception ex)
            {
                if (session.HasExit)
                    return session.ExitCode;

                var exit = FindExit(ex);
                if (exit != null)
                    return exit.Code;

                var interpreter = ex as InterpreterException;
                error = interpreter != null ? interpreter.DecoratedMessage : ex.Message;
                Console.Error.WriteLine(error);
                Logger.Error(ex);
                return 1;
            }
        }

        private static LuaExitException FindExit(Exception ex)
        {
            for (var current = ex; current != null; current = current.InnerException)
            {
                var exit = current as LuaExitException;
                if (exit != null)
                    return exit;
            }
            return null;
        }
    }
}
