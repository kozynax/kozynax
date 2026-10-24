using Kozynax.UI;
using Xunit;

namespace Kozynax.Tests;

public sealed class MachineMenuGroupingTests
{
    [Fact]
    public void Groups_shared_name_prefix_into_submenu()
    {
        var grouped = MachineMenuGrouping.Group(Configs(
            "ATM Turbo [V4.50]",
            "ATM Turbo 2+ [V7.10]",
            "BYTE 48K",
            "BYTE 128K+TRDOS",
            "DELTA-C",
            "PENTAGON 128K",
            "PENTAGON 512K",
            "PENTAGON 1024K",
            "PENT EVO",
            "Scorpion ZS 256 PROF-ROM",
            "Scorpion ZS 256",
            "ZX Spectrum 48",
            "ZX Spectrum 128",
            "ZX Spectrum +3"));

        Assert.Equal(new[]
        {
            "ATM Turbo",
            "  [V4.50]",
            "  2+ [V7.10]",
            "BYTE",
            "  48K",
            "  128K+TRDOS",
            "DELTA-C",
            "PENTAGON",
            "  128K",
            "  512K",
            "  1024K",
            "PENT EVO",
            "Scorpion ZS",
            "  256 PROF-ROM",
            "  256",
            "ZX Spectrum",
            "  48",
            "  128",
            "  +3",
        }, Flatten(grouped));
    }

    [Fact]
    public void Single_machine_stays_a_leaf()
    {
        var machine = new MachineSettings.MachineConfiguration { Name = "ZX Spectrum 48" };
        var grouped = MachineMenuGrouping.Group(new[] { machine });

        var entry = Assert.Single(grouped);
        Assert.False(entry.IsGroup);
        Assert.Equal("ZX Spectrum 48", entry.Caption);
        Assert.Same(machine, entry.Machine);
    }

    [Fact]
    public void Submenu_items_keep_their_machine()
    {
        var spec48 = new MachineSettings.MachineConfiguration { Name = "ZX Spectrum 48" };
        var spec128 = new MachineSettings.MachineConfiguration { Name = "ZX Spectrum 128" };
        var grouped = MachineMenuGrouping.Group(new[] { spec48, spec128 });

        var group = Assert.Single(grouped);
        Assert.Equal("ZX Spectrum", group.Caption);
        Assert.Equal(spec48, group.Children[0].Machine);
        Assert.Equal(spec128, group.Children[1].Machine);
    }

    private static MachineSettings.MachineConfiguration[] Configs(params string[] names)
    {
        var list = new MachineSettings.MachineConfiguration[names.Length];
        for (var i = 0; i < names.Length; i++)
            list[i] = new MachineSettings.MachineConfiguration { Name = names[i] };
        return list;
    }

    private static string[] Flatten(System.Collections.Generic.IReadOnlyList<MachineMenuGrouping.Entry> entries)
    {
        var lines = new List<string>();
        foreach (var entry in entries)
        {
            lines.Add(entry.Caption);
            foreach (var child in entry.Children)
                lines.Add("  " + child.Caption);
        }
        return lines.ToArray();
    }
}
