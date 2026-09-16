using System.Reflection;
using System.Xml;
using ZXMAK2.Engine;

namespace Kozynax.Tests.Infrastructure;

internal static class TestMachineFactory
{
    public static Spectrum CreateInitialized()
    {
        var machine = new Spectrum();
        machine.Init();
        return machine;
    }

    public static Spectrum CreateWithConfig(string configXml)
    {
        var machine = CreateInitialized();
        // LoadConfigXml disconnects, replaces devices, then connects.
        var config = new XmlDocument();
        config.LoadXml(configXml);
        machine.BusManager.LoadConfigXml(config.DocumentElement);
        return machine;
    }

    public static Spectrum CreateSpectrum128()
    {
        var machines = new MachinesConfig();
        machines.Load();
        var node = machines.GetConfig("ZX Spectrum 128");
        if (node == null)
            throw new InvalidOperationException("ZX Spectrum 128 machine config not found.");
        var machine = CreateInitialized();
        machine.BusManager.LoadConfigXml(node);
        return machine;
    }

    public static string GetFixturePath(string fileName)
        => Path.Combine(AppContext.BaseDirectory, "Fixtures", fileName);

    public static string LoadFixtureText(string fileName)
    {
        return File.ReadAllText(GetFixturePath(fileName));
    }

    public static Stream OpenEmbeddedSnapshot(string fileName)
    {
        var name = $"Kozynax.Tests.Fixtures.{fileName}";
        var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(name);
        if (stream == null)
        {
            throw new FileNotFoundException($"Embedded fixture not found: {name}");
        }
        return stream;
    }
}
