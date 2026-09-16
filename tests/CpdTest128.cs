using System.Text;
using Kozynax.Tests.Infrastructure;
using Xunit;
using Xunit.Abstractions;
using ZXMAK2.Engine;
using ZXMAK2.Engine.Cpu;
using ZXMAK2.Engine.Interfaces;

namespace Kozynax.Tests;

/// <summary>
/// Runs the CPD 128K snapshot and captures CALL #A5FF print traps until PC=#8080.
/// https://zx-pk.ru/threads/36139-cpd-test-dlya-proverki-izmeneniya-registra-memptr-instruktsiyami-proverte-na-reale-plz.html?p=1213779&viewfull=1#post1213779
/// </summary>
public sealed class CpdTest128Tests : IClassFixture<EmulatorServicesFixture>
{
    private const string SnapshotFileName = "cpd-test-128-v0.777b.SZX";
    private const ushort PrintTrapAddress = 0xA5FF;
    private const ushort ExitAddress = 0x8080;
    private const long TimeoutTStates = 70000L * 50 * 20;
    private const byte RetOpcode = 0xC9;
    private const int MaxPrintChars = 4096;

    private readonly ITestOutputHelper _output;

    public CpdTest128Tests(EmulatorServicesFixture _, ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void Cpd_test_128_all_cases_pass()
    {
        using var machine = TestMachineFactory.CreateSpectrum128();
        machine.IsRunning = true;
        machine.DebugReset();
        machine.ExecuteFrame();

        machine.IsRunning = false;
        using (var stream = File.OpenRead(TestMachineFactory.GetFixturePath(SnapshotFileName)))
            machine.BusManager.LoadManager.GetSerializer(Path.GetExtension(SnapshotFileName)).Deserialize(stream);

        var line = new StringBuilder();
        string? lastLine = null;
        void FlushLine()
        {
            if (line.Length == 0)
                return;
            var text = line.ToString();
            Console.WriteLine();
            if (!string.IsNullOrWhiteSpace(text))
            {
                lastLine = text;
                _output.WriteLine(lastLine);
            }
            line.Clear();
        }

        void AppendFragment(string text)
        {
            if (text.Length == 0 || string.IsNullOrWhiteSpace(text))
                return;

            if (text != "." &&
                line.Length > 0 &&
                line[^1] != '.' &&
                !char.IsWhiteSpace(line[^1]) &&
                !char.IsWhiteSpace(text[0]))
            {
                line.Append(' ');
                Console.Write(' ');
            }

            line.Append(text);
            Console.Write(text);
        }

        ((IBusManager)machine.BusManager).Events.SubscribeRdMemM1(
            0xFFFF,
            PrintTrapAddress,
            (ushort _, ref byte value) =>
            {
                var text = ReadPrintable(machine, machine.CPU.regs.HL);
                if (text.Length == 0)
                {
                    value = RetOpcode;
                    return;
                }

                var parts = text.Split('\n');
                for (var i = 0; i < parts.Length; i++)
                {
                    AppendFragment(parts[i]);
                    if (i < parts.Length - 1)
                        FlushLine();
                }

                if (parts[^1] is "OK" or "FAIL")
                    FlushLine();

                value = RetOpcode;
            });

        var cpu = machine.CPU;
        var startTact = cpu.Tact;
        while (true)
        {
            var elapsed = cpu.Tact - startTact;
            if (elapsed > TimeoutTStates)
                Assert.Fail($"Timed out after {elapsed} t-states (limit {TimeoutTStates}). PC=#{cpu.regs.PC:X4}");

            if (cpu.FX == CpuModeIndex.None &&
                cpu.XFX == CpuModeEx.None &&
                cpu.regs.PC == ExitAddress)
            {
                break;
            }

            machine.BusManager.ExecCycle();
        }

        FlushLine();
        machine.BusManager.Disconnect();

        Assert.Equal("Failed 00 from 98 tests", lastLine);
    }

    /// <summary>
    /// Reads a ZX null-terminated PRINT string, dropping control sequences
    /// other than CR/LF. Spectrum CHR$ 13 is treated as a host newline.
    /// </summary>
    private static string ReadPrintable(Spectrum machine, ushort addr)
    {
        var sb = new StringBuilder(64);
        for (var i = 0; i < MaxPrintChars; i++)
        {
            var value = machine.DebugReadMemory((ushort)(addr + i));
            if (value == 0)
                break;

            var skip = ControlArgCount(value);
            if (skip >= 0)
            {
                i += skip;
                continue;
            }

            if (value == (byte)'\n')
            {
                sb.Append('\n');
                continue;
            }

            if (value == (byte)'\r')
            {
                sb.Append('\n');
                continue;
            }

            if (value >= 0x20)
                sb.Append((char)value);
        }

        return sb.ToString();
    }

    // Ignore all control sequences
    private static int ControlArgCount(byte value) => value switch
    {
        0x10 or 0x11 or 0x12 or 0x13 or 0x14 or 0x15 or 0x17 => 1, // INK/PAPER/FLASH/BRIGHT/INVERSE/OVER/TAB
        0x16 => 2, // AT y,x
        >= 0x20 or 0x0A or 0x0D => -1,
        _ => 0,
    };
}
