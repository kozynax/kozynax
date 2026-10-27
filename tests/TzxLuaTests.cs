using System.Net.Http;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Kozynax.Lua;
using Kozynax.Tests.Infrastructure;
using Xunit;
using Xunit.Abstractions;

namespace Kozynax.Tests;

/// <summary>
/// Runs each tape script in <c>Fixtures/tzx</c> headless, in name order.
/// <c>load.lua</c> is the shared helper and is not a tape run.
/// Each other script names its tape by file name. The tape must match the
/// <c>TZX File MD5</c> comment. A missing or different file is downloaded from
/// the project wiki <c>test_data/tzx_v1_20</c> set.
/// Opt-in: the test project filter leaves this class out of <c>dotnet test</c>,
/// including the build server. Run it with
/// <c>--filter FullyQualifiedName~TzxLuaTests</c>.
/// </summary>
[Trait("Category", "Manual")]
public sealed class TzxLuaTests : IClassFixture<EmulatorServicesFixture>
{
    private const string TapeUrlPrefix =
        "https://raw.githubusercontent.com/wiki/kozynax/kozynax/test_data/tzx_v1_20/";

    private static readonly Regex Md5Line = new Regex(
        "^-- TZX File MD5: ([0-9a-fA-F]{32})\\s*$",
        RegexOptions.CultureInvariant);

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

        using var client = new HttpClient();
        client.Timeout = TimeSpan.FromMinutes(3);
        client.DefaultRequestHeaders.UserAgent.ParseAdd("KozynaxTests");

        var failures = new List<string>();
        foreach (var name in scripts)
        {
            if (!EnsureTape(client, directory, name, failures))
                continue;

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

    private bool EnsureTape(HttpClient client, string directory, string luaName, ICollection<string> failures)
    {
        var scriptPath = Path.Combine(directory, luaName);
        var expected = ExpectedMd5(scriptPath);
        if (expected == null)
        {
            var missing = luaName + ": missing TZX File MD5 comment";
            _output.WriteLine(missing);
            failures.Add(missing);
            return false;
        }

        var tapeName = Path.ChangeExtension(luaName, ".tzx");
        var tapePath = Path.Combine(directory, tapeName);
        if (File.Exists(tapePath) && string.Equals(Md5File(tapePath), expected, StringComparison.OrdinalIgnoreCase))
            return true;

        _output.WriteLine("{0}: download {1}", luaName, tapeName);
        var partial = tapePath + ".partial";
        try
        {
            var url = TapeUrlPrefix + Uri.EscapeDataString(tapeName);
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            using var response = client.Send(request);
            if (!response.IsSuccessStatusCode)
            {
                var failed = luaName + ": download failed (" + (int)response.StatusCode + ")";
                _output.WriteLine(failed);
                failures.Add(failed);
                return false;
            }

            using (var file = File.Create(partial))
                response.Content.ReadAsStream().CopyTo(file);

            var actual = Md5File(partial);
            if (!string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase))
            {
                var mismatch = luaName + ": checksum " + actual + " != " + expected;
                _output.WriteLine(mismatch);
                failures.Add(mismatch);
                return false;
            }

            File.Move(partial, tapePath, true);
            return true;
        }
        catch (Exception ex)
        {
            var failed = luaName + ": " + ex.Message;
            _output.WriteLine(failed);
            failures.Add(failed);
            return false;
        }
        finally
        {
            if (File.Exists(partial))
                File.Delete(partial);
        }
    }

    private static string? ExpectedMd5(string scriptPath)
    {
        using var reader = new StreamReader(scriptPath);
        var line = reader.ReadLine() ?? "";
        var match = Md5Line.Match(line);
        return match.Success ? match.Groups[1].Value : null;
    }

    private static string Md5File(string path)
    {
        using var md5 = MD5.Create();
        using var stream = File.OpenRead(path);
        return Convert.ToHexString(md5.ComputeHash(stream)).ToLowerInvariant();
    }

    private static string TzxDirectory()
    {
        var directory = Path.Combine(AppContext.BaseDirectory, "Fixtures", "tzx");
        if (!Directory.Exists(directory))
            throw new DirectoryNotFoundException("TZX directory not found in '" + directory + "'");
        return directory;
    }
}
