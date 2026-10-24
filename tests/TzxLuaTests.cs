using Kozynax.Lua;
using Kozynax.Tests.Infrastructure;
using Xunit;
using Xunit.Abstractions;

namespace Kozynax.Tests;

/// <summary>
/// Runs each tape script in <c>Fixtures/tzx</c> headless, in name order.
/// <c>load.lua</c> is the shared helper and is not a tape run.
/// Opt-in: the test project filter leaves this class out of <c>dotnet test</c>,
/// including the build server. Run it with
/// <c>--filter FullyQualifiedName~TzxLuaTests</c>.
/// </summary>
[Trait("Category", "Manual")]
public sealed class TzxLuaTests : IClassFixture<EmulatorServicesFixture>
{
    private readonly ITestOutputHelper _output;

    public TzxLuaTests(EmulatorServicesFixture _, ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void Tape_scripts_succeed()
    {
        var directory = TzxDirectory();
        var scripts = Directory.GetFiles(directory, "*.lua")
            .Select(Path.GetFileName)
            .Where(name => name != null && !string.Equals(name, "load.lua", StringComparison.OrdinalIgnoreCase))
            .Cast<string>()
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        Assert.NotEmpty(scripts);

        var failures = new List<string>();
        foreach (var name in scripts)
        {
            var path = Path.Combine(directory, name);
            using var session = new LuaScriptSession(directory);
            var code = LuaScriptRunner.Execute(session, path, out var error);
            if (code == 0)
            {
                _output.WriteLine("{0}: Success", name);
                continue;
            }

            var detail = string.IsNullOrEmpty(error) ? "exit " + code : error;
            _output.WriteLine("{0}: Fail ({1})", name, detail);
            failures.Add(name + ": " + detail);
        }

        Assert.True(
            failures.Count == 0,
            "Failed scripts:\n" + string.Join("\n", failures));
    }

    private static string TzxDirectory()
    {
        var directory = Path.Combine(AppContext.BaseDirectory, "Fixtures", "tzx");
        if (!Directory.Exists(directory))
            throw new DirectoryNotFoundException("TZX directory not found in '" + directory + "'");
        return directory;
    }
}
