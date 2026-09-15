using System.Diagnostics;
using Kozynax.Tests.Infrastructure;
using Xunit;
using Xunit.Abstractions;
using ZXMAK2.Engine.Cpu;

namespace Kozynax.Tests;

/// <summary>
/// CPU and engine performance measurements from the legacy <c>src/Test</c> harness.
/// These assert that a run completes; timings are written to the test output.
/// </summary>
public sealed class CpuPerformanceTests : IClassFixture<EmulatorServicesFixture>
{
    private readonly ITestOutputHelper _output;

    public CpuPerformanceTests(EmulatorServicesFixture _, ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void Cpu_exec_cycle_benchmark()
    {
        const int frameCount = 50 * 10;

        var cpu = new CpuUnit();
        cpu.regs.PC = 0;
        cpu.RESET = () => { };
        cpu.NMIACK_M1 = () => { };
        cpu.INTACK_M1 = () => { };
        cpu.RDMEM_M1 = addr => (byte)addr;
        cpu.RDMEM = addr => (byte)addr;
        cpu.WRMEM = (addr, value) => { };
        cpu.RDPORT = addr => 0xFF;
        cpu.WRPORT = (addr, value) => { };
        cpu.RDNOMREQ = addr => { };
        cpu.WRNOMREQ = addr => { };

        var watch = Stopwatch.StartNew();
        for (var cycle = 0L; cycle < 71980L * frameCount; cycle++)
            cpu.ExecCycle();
        watch.Stop();

        _output.WriteLine("zexall.sna [cpu]:\t{0} [ms]", watch.ElapsedMilliseconds);
        Assert.True(watch.ElapsedMilliseconds >= 0);
    }

    [Theory]
    [InlineData("testVideo.z80")]
    [InlineData("zexall.sna")]
    [InlineData("testOutFe.z80")]
    public void Engine_full_machine_benchmark(string snapshotName)
    {
        const int frameCount = 50 * 10;
        var config = TestMachineFactory.LoadFixtureText("machines.test.config");
        using var machine = TestMachineFactory.CreateWithConfig(config);
        machine.IsRunning = true;
        machine.DebugReset();
        machine.ExecuteFrame();

        machine.IsRunning = false;
        using (var testStream = TestMachineFactory.OpenEmbeddedSnapshot(snapshotName))
            machine.BusManager.LoadManager.GetSerializer(Path.GetExtension(snapshotName)).Deserialize(testStream);
        machine.IsRunning = true;

        var watch = Stopwatch.StartNew();
        for (var frame = 0; frame < frameCount; frame++)
            machine.ExecuteFrame();
        watch.Stop();

        _output.WriteLine("{0}:\t{1} [ms]", snapshotName, watch.ElapsedMilliseconds);
        machine.BusManager.Disconnect();
        Assert.True(watch.ElapsedMilliseconds >= 0);
    }

    [Fact]
    public void Engine_light_machine_benchmark()
    {
        const int frameCount = 50 * 10;
        const string snapshotName = "zexall.sna";
        var config = TestMachineFactory.LoadFixtureText("machines.testLight.config");
        using var machine = TestMachineFactory.CreateWithConfig(config);
        machine.IsRunning = true;
        machine.DebugReset();
        machine.ExecuteFrame();

        machine.IsRunning = false;
        using (var testStream = TestMachineFactory.OpenEmbeddedSnapshot(snapshotName))
            machine.BusManager.LoadManager.GetSerializer(Path.GetExtension(snapshotName)).Deserialize(testStream);
        machine.IsRunning = true;

        var watch = Stopwatch.StartNew();
        for (var frame = 0; frame < frameCount; frame++)
            machine.ExecuteFrame();
        watch.Stop();

        _output.WriteLine("{0} [light]:\t{1} [ms]", snapshotName, watch.ElapsedMilliseconds);
        machine.BusManager.Disconnect();
        Assert.True(watch.ElapsedMilliseconds >= 0);
    }
}
