using System.Collections.Generic;
using Kozynax.Tests.Infrastructure;
using ZXMAK2.Engine;
using ZXMAK2.Engine.Cpu;
using ZXMAK2.Engine.Entities;
using ZXMAK2.Engine.Interfaces;
using ZXMAK2.Hardware.Spectrum;
using Xunit;

namespace Kozynax.Tests;

/// <summary>
/// ULA contention timing sanity checks ported from the legacy <c>src/Test</c> console harness.
/// </summary>
public sealed class UlaContentionTests : IClassFixture<EmulatorServicesFixture>
{
    public UlaContentionTests(EmulatorServicesFixture _)
    {
    }

    public static IEnumerable<object[]> Cases()
    {
        yield return Case("NOP", new UlaSpectrum48_Early(), new byte[] { 0x00 }, UlaContentionPatterns.s_patternUla48_Early_NOP);
        yield return Case("DJNZ", new UlaSpectrum48_Early(), new byte[] { 0x10, 0x00 }, UlaContentionPatterns.s_patternUla48_Early_DJNZ);
        yield return Case("IN A,(#FE)", new UlaSpectrum48_Early(), new byte[] { 0xDB, 0xFE }, UlaContentionPatterns.s_patternUla48_Early_INAFE);
        yield return Case("OUT (#FE),A", new UlaSpectrum48_Early(), new byte[] { 0xD3, 0xFE }, UlaContentionPatterns.s_patternUla48_Early_OUTAFE);
        yield return Case("LD A,(HL)", new UlaSpectrum48_Early(), new byte[] { 0x7E }, UlaContentionPatterns.s_patternUla48_Early_LDAHL);
        yield return Case("IN A,(C)", new UlaSpectrum48_Early(), new byte[] { 0xED, 0x78 }, UlaContentionPatterns.s_patternUla48_Early_INAC);
        yield return Case("OUT (C),A", new UlaSpectrum48_Early(), new byte[] { 0xED, 0x79 }, UlaContentionPatterns.s_patternUla48_Early_OUTCA);
        yield return Case("BIT 7,A", new UlaSpectrum48_Early(), new byte[] { 0xCB, 0x7F }, UlaContentionPatterns.s_patternUla48_Early_BIT7A);
        yield return Case("BIT 7,(HL)", new UlaSpectrum48_Early(), new byte[] { 0xCB, 0x7E }, UlaContentionPatterns.s_patternUla48_Early_BIT7HL);
        yield return Case("SET 7,(HL)", new UlaSpectrum48_Early(), new byte[] { 0xCB, 0xFE }, UlaContentionPatterns.s_patternUla48_Early_SET7HL);

        yield return Case("NOP", new UlaSpectrum48(), new byte[] { 0x00 }, UlaContentionPatterns.s_patternUla48_Late_NOP);
        yield return Case("DJNZ", new UlaSpectrum48(), new byte[] { 0x10, 0x00 }, UlaContentionPatterns.s_patternUla48_Late_DJNZ);
        yield return Case("IN A,(#FE)", new UlaSpectrum48(), new byte[] { 0xDB, 0xFE }, UlaContentionPatterns.s_patternUla48_Late_INAFE);
        yield return Case("OUT (#FE),A", new UlaSpectrum48(), new byte[] { 0xD3, 0xFE }, UlaContentionPatterns.s_patternUla48_Late_OUTAFE);
        yield return Case("LD A,(HL)", new UlaSpectrum48(), new byte[] { 0x7E }, UlaContentionPatterns.s_patternUla48_Late_LDAHL);
        yield return Case("IN A,(C)", new UlaSpectrum48(), new byte[] { 0xED, 0x78 }, UlaContentionPatterns.s_patternUla48_Late_INAC);
        yield return Case("OUT (C),A", new UlaSpectrum48(), new byte[] { 0xED, 0x79 }, UlaContentionPatterns.s_patternUla48_Late_OUTCA);
        yield return Case("BIT 7,A", new UlaSpectrum48(), new byte[] { 0xCB, 0x7F }, UlaContentionPatterns.s_patternUla48_Late_BIT7A);
        yield return Case("BIT 7,(HL)", new UlaSpectrum48(), new byte[] { 0xCB, 0x7E }, UlaContentionPatterns.s_patternUla48_Late_BIT7HL);
        yield return Case("SET 7,(HL)", new UlaSpectrum48(), new byte[] { 0xCB, 0xFE }, UlaContentionPatterns.s_patternUla48_Late_SET7HL);

        yield return Case("NOP", new UlaSpectrum128(), new byte[] { 0x00 }, UlaContentionPatterns.s_patternUla128_NOP);
        yield return Case("INC HL", new UlaSpectrum128(), new byte[] { 0x23 }, UlaContentionPatterns.s_patternUla128_INCHL);
        yield return Case("LD A,(HL)", new UlaSpectrum128(), new byte[] { 0x7E }, UlaContentionPatterns.s_patternUla128_LDA_HL_);
        yield return Case("LD (HL),A", new UlaSpectrum128(), new byte[] { 0x77 }, UlaContentionPatterns.s_patternUla128_LDA_HL_);
        yield return Case("OUT (C),A", new UlaSpectrum128(), new byte[] { 0xED, 0x79, 0x03 }, UlaContentionPatterns.s_patternUla128_OUTCA);
        yield return Case("IN A,(C)", new UlaSpectrum128(), new byte[] { 0xED, 0x78, 0x03 }, UlaContentionPatterns.s_patternUla128_OUTCA);
    }

    private static object[] Case(string name, IUlaDevice ula, byte[] opcode, int[] pattern)
        => new object[] { name, ula, opcode, pattern };

    [Theory]
    [MemberData(nameof(Cases))]
    public void Opcode_matches_expected_frame_tacts(string name, IUlaDevice ula, byte[] opcode, int[] pattern)
    {
        IMemoryDevice mem = new MemorySpectrum48();
        using var machine = TestMachineFactory.CreateInitialized();
        machine.BusManager.Disconnect();
        machine.BusManager.Clear();
        machine.BusManager.Add((BusDeviceBase)mem);
        machine.BusManager.Add((BusDeviceBase)ula);
        machine.BusManager.Connect();
        machine.IsRunning = true;
        machine.DebugReset();
        machine.ExecuteFrame();
        machine.IsRunning = false;

        ushort offset = 0x4000;
        for (var i = 0; i < pattern.Length; i++)
        {
            for (var j = 0; j < opcode.Length; j++)
                mem.WRMEM_DBG(offset++, opcode[j]);
        }

        machine.CPU.regs.PC = 0x4000;
        machine.CPU.regs.IR = 0x4000;
        machine.CPU.regs.SP = 0x4000;
        machine.CPU.regs.AF = 0x4000;
        machine.CPU.regs.HL = 0x4000;
        machine.CPU.regs.DE = 0x4000;
        machine.CPU.regs.BC = 0x4000;
        machine.CPU.regs.IX = 0x4000;
        machine.CPU.regs.IY = 0x4000;
        machine.CPU.regs._AF = 0x4000;
        machine.CPU.regs._HL = 0x4000;
        machine.CPU.regs._DE = 0x4000;
        machine.CPU.regs._BC = 0x4000;
        machine.CPU.regs.MW = 0x4000;
        machine.CPU.IFF1 = machine.CPU.IFF2 = false;
        machine.CPU.IM = 2;
        machine.CPU.BINT = false;
        machine.CPU.FX = CpuModeIndex.None;
        machine.CPU.XFX = CpuModeEx.None;

        long needsTact = pattern[0];
        long frameTact = machine.CPU.Tact % ula.FrameTactCount;
        long deltaTact = needsTact - frameTact;
        if (deltaTact < 0)
            deltaTact += ula.FrameTactCount;
        machine.CPU.Tact += deltaTact;

        for (var i = 0; i < pattern.Length - 1; i++)
        {
            machine.DebugStepInto();
            frameTact = machine.CPU.Tact % ula.FrameTactCount;
            Assert.True(
                frameTact == pattern[i + 1],
                $"Sanity ULA {ula.GetType().Name} [{name}]: failed @ {pattern[i]}->{frameTact} (should be {pattern[i]}->{pattern[i + 1]})");
        }

        machine.BusManager.Disconnect();
    }
}
