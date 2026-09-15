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

    public static string LoadFixtureText(string fileName)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Fixtures", fileName);
        return File.ReadAllText(path);
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
