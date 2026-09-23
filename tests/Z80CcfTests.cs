using System.Text;
using Kozynax.Tests.Infrastructure;
using Xunit;
using Xunit.Abstractions;
using ZXMAK2.Engine;
using ZXMAK2.Engine.Cpu;
using ZXMAK2.Engine.Interfaces;

namespace Kozynax.Tests;

/// <summary>
/// Runs Patrik Rak's z80ccf.tap (SCF/CCF Q-flag suite) with RST #10 trapped
/// to capture channel output. Requires Zilog CPU type for a clean pass.
/// </summary>
public sealed class Z80CcfTests : IClassFixture<EmulatorServicesFixture>
{
    private const string TapFileName = "z80ccf.tap";
    private const ushort CodeLoadAddress = 0x8000;
    private const ushort Rst10Address = 0x0010;
    private const ushort BasicErrNr = 0x5C3A;
    private const byte RetOpcode = 0xC9;
    private const int TimeoutFrames = 50 * 600; // plenty for the full suite (~12s wall)

    private readonly ITestOutputHelper _output;
    private int _controlArgsRemaining;

    public Z80CcfTests(EmulatorServicesFixture _, ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void Z80ccf_all_tests_passed_on_zilog_nmos()
    {
        using var machine = TestMachineFactory.CreateSpectrum48(CpuType.ZILOG_NMOS);
        Assert.Equal(CpuType.ZILOG_NMOS, machine.CPU.Type);

        machine.IsRunning = true;
        machine.DebugReset();
        for (var i = 0; i < 50; i++)
            machine.ExecuteFrame();
        machine.IsRunning = false;

        LoadTapCode(machine, TestMachineFactory.GetFixturePath(TapFileName), CodeLoadAddress);
        Assert.Equal(0xF3, machine.DebugReadMemory(CodeLoadAddress)); // DI at entry

        // Skip CHAN-OPEN; stream 2 is already current after BASIC init.
        machine.DebugWriteMemory(0x815A, RetOpcode);
        PatchPrintchrEiToNop(machine);

        // z80ccf self-check expects idle port #FE == #BF.
        ((IBusManager)machine.BusManager).Events.SubscribeRdIo(
            0x00FF,
            0x00FE,
            (ushort _, ref byte value, ref bool handled) =>
            {
                value = 0xBF;
                handled = true;
            });

        var text = new StringBuilder();
        ((IBusManager)machine.BusManager).Events.SubscribeRdMemM1(
            0xFFFF,
            Rst10Address,
            (ushort _, ref byte value) =>
            {
                AppendChannelChar(text, machine.CPU.regs.A);
                value = RetOpcode;
            });

        var cpu = machine.CPU;
        cpu.IFF1 = false;
        cpu.IFF2 = false;
        cpu.IM = 1;
        cpu.regs.IY = BasicErrNr;
        cpu.regs.SP = 0xFFFD;
        machine.DebugWriteMemory(0xFFFD, 0x00);
        machine.DebugWriteMemory(0xFFFE, 0x00);
        cpu.regs.PC = CodeLoadAddress;

        machine.IsRunning = true;
        for (var frame = 0; frame < TimeoutFrames; frame++)
        {
            machine.ExecuteFrame();
            var output = text.ToString();
            if (output.Contains("all tests passed.") || output.Contains("tests failed."))
                break;

            if (frame == TimeoutFrames - 1)
            {
                WriteOutput(output);
                Assert.Fail($"Timed out after {TimeoutFrames} frames. PC=#{cpu.regs.PC:X4}");
            }
        }

        machine.IsRunning = false;
        var result = text.ToString();
        WriteOutput(result);
        machine.BusManager.Disconnect();

        Assert.Contains("all tests passed.", result);
        Assert.DoesNotContain("tests failed.", result);
    }

    private void WriteOutput(string result)
    {
        foreach (var line in result.Split('\n'))
        {
            if (!string.IsNullOrWhiteSpace(line))
                _output.WriteLine(line);
        }
    }

    private static void PatchPrintchrEiToNop(Spectrum machine)
    {
        for (var addr = CodeLoadAddress; addr < CodeLoadAddress + 0x300; addr++)
        {
            if (machine.DebugReadMemory((ushort)addr) == 0xFB &&
                machine.DebugReadMemory((ushort)(addr + 1)) == 0xD7 &&
                machine.DebugReadMemory((ushort)(addr + 2)) == 0xF3)
            {
                machine.DebugWriteMemory((ushort)addr, 0x00);
                return;
            }
        }

        throw new InvalidOperationException("printchr EI/RST10/DI sequence not found.");
    }

    private static void LoadTapCode(Spectrum machine, string tapPath, ushort loadAddress)
    {
        var data = File.ReadAllBytes(tapPath);
        var offset = 0;
        for (var block = 0; block < 3; block++)
        {
            if (offset + 2 > data.Length)
                throw new InvalidDataException("Unexpected end of TAP while skipping headers.");
            var length = data[offset] | (data[offset + 1] << 8);
            offset += 2 + length;
        }

        if (offset + 2 > data.Length)
            throw new InvalidDataException("CODE data block missing.");
        var codeBlockLength = data[offset] | (data[offset + 1] << 8);
        offset += 2;
        if (offset + codeBlockLength > data.Length)
            throw new InvalidDataException("CODE data block truncated.");

        var payloadLength = codeBlockLength - 2;
        var payloadStart = offset + 1;
        for (var i = 0; i < payloadLength; i++)
            machine.DebugWriteMemory((ushort)(loadAddress + i), data[payloadStart + i]);
    }

    private void AppendChannelChar(StringBuilder text, byte value)
    {
        if (_controlArgsRemaining > 0)
        {
            _controlArgsRemaining--;
            return;
        }

        // z80ccf uses CHR$ 23 with two bytes as a column-align helper before OK/FAILED.
        var args = value switch
        {
            0x10 or 0x11 or 0x12 or 0x13 or 0x14 or 0x15 => 1,
            0x16 or 0x17 => 2,
            _ => -1,
        };
        if (args >= 0)
        {
            _controlArgsRemaining = args;
            return;
        }

        if (value == 0x0D)
        {
            text.Append('\n');
            return;
        }

        if (value >= 0x20 && value < 0x7F)
            text.Append((char)value);
    }
}
