using System.Text.Json;
using CommandLine;
using ditjson.Models;

namespace ditjson.Tests;

[TestClass]
[DoNotParallelize]
public class ProgramTests
{
    [TestMethod]
    [DataRow("-h")]
    [DataRow("--help")]
    [DataRow("-v")]
    [DataRow("--version")]
    public void Main_HelpAndVersion_ReturnSuccessWithoutStdout(string argument)
    {
        var stdout = new StringWriter();
        var stderr = new StringWriter();
        var originalOut = Console.Out;
        var originalError = Console.Error;
        try
        {
            Console.SetOut(stdout);
            Console.SetError(stderr);

            var exitCode = Program.Main([argument]);

            Assert.AreEqual(0, exitCode);
            Assert.AreEqual(string.Empty, stdout.ToString());
            Assert.IsGreaterThan(0, stderr.ToString().Length);
        }
        finally
        {
            Console.SetOut(originalOut);
            Console.SetError(originalError);
        }
    }

    [TestMethod]
    public void Main_InvalidArguments_ReturnsUsageErrorWithoutStdout()
    {
        var stdout = new StringWriter();
        var stderr = new StringWriter();
        var originalOut = Console.Out;
        var originalError = Console.Error;
        try
        {
            Console.SetOut(stdout);
            Console.SetError(stderr);

            var exitCode = Program.Main([]);

            Assert.AreEqual(2, exitCode);
            Assert.AreEqual(string.Empty, stdout.ToString());
            Assert.Contains("ntds.dit", stderr.ToString());
        }
        finally
        {
            Console.SetOut(originalOut);
            Console.SetError(originalError);
        }
    }

    [TestMethod]
    public void Main_MissingNtds_ReturnsRuntimeFailureWithoutStdout()
    {
        var stdout = new StringWriter();
        var stderr = new StringWriter();
        var originalOut = Console.Out;
        var originalError = Console.Error;
        try
        {
            Console.SetOut(stdout);
            Console.SetError(stderr);

            var exitCode = Program.Main([Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".dit")]);

            Assert.AreEqual(1, exitCode);
            Assert.AreEqual(string.Empty, stdout.ToString());
            Assert.Contains("NTDS file not found", stderr.ToString());
        }
        finally
        {
            Console.SetOut(originalOut);
            Console.SetError(originalError);
        }
    }

    [TestMethod]
    public void Main_MissingSystem_ReturnsRuntimeFailureWithoutStdout()
    {
        var stdout = new StringWriter();
        var stderr = new StringWriter();
        var originalOut = Console.Out;
        var originalError = Console.Error;
        var ntdsPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".dit");
        File.WriteAllText(ntdsPath, string.Empty);

        try
        {
            Console.SetOut(stdout);
            Console.SetError(stderr);

            var exitCode = Program.Main([ntdsPath, Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".hive")]);

            Assert.AreEqual(1, exitCode);
            Assert.AreEqual(string.Empty, stdout.ToString());
            Assert.Contains("SYSTEM hive not found", stderr.ToString());
        }
        finally
        {
            Console.SetOut(originalOut);
            Console.SetError(originalError);
            File.Delete(ntdsPath);
        }
    }

    [TestMethod]
    public void Parser_AcceptsNtdsPositionalArgument()
    {
        Options? parsed = null;

        new Parser(settings => settings.HelpWriter = TextWriter.Null)
            .ParseArguments<Options>(["ntds.dit"])
            .WithParsed(options => parsed = options);

        Assert.IsNotNull(parsed);
        Assert.AreEqual("ntds.dit", parsed.Ntds);
        Assert.IsNull(parsed.System);
        Assert.IsNull(parsed.Output);
        Assert.IsFalse(parsed.Timeline);
    }

    [TestMethod]
    public void Parser_AcceptsTimelineFlag()
    {
        Options? parsed = null;

        new Parser(settings => settings.HelpWriter = TextWriter.Null)
            .ParseArguments<Options>(["ntds.dit", "--timeline"])
            .WithParsed(options => parsed = options);

        Assert.IsNotNull(parsed);
        Assert.IsTrue(parsed.Timeline);
    }

    [TestMethod]
    public void Parser_AcceptsSystemAndOutputArguments()
    {
        Options? parsed = null;

        new Parser(settings => settings.HelpWriter = TextWriter.Null)
            .ParseArguments<Options>(["ntds.dit", "SYSTEM", "-o", "domain.json"])
            .WithParsed(options => parsed = options);

        Assert.IsNotNull(parsed);
        Assert.AreEqual("ntds.dit", parsed.Ntds);
        Assert.AreEqual("SYSTEM", parsed.System);
        Assert.AreEqual("domain.json", parsed.Output);
    }

    [TestMethod]
    public void WriteOutput_WithDestination_WritesFileInsteadOfStdout()
    {
        var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var output = Path.Combine(directory, "output.json");
        Directory.CreateDirectory(directory);
        var stdout = new StringWriter();
        var originalOut = Console.Out;
        try
        {
            Console.SetOut(stdout);
            Program.WriteOutput("{\"users\":[]}", output);

            Assert.AreEqual(string.Empty, stdout.ToString());
            Assert.AreEqual("{\"users\":[]}", File.ReadAllText(output));
        }
        finally
        {
            Console.SetOut(originalOut);
            Directory.Delete(directory, true);
        }
    }

    [TestMethod]
    public void WriteOutput_WithInvalidDestination_Throws()
    {
        var missingDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString(), "output.json");

        Assert.ThrowsExactly<DirectoryNotFoundException>(() =>
            Program.WriteOutput("{}", missingDirectory));
    }

    [TestMethod]
    public void WriteOutput_WithoutDestination_WritesOnlyValidJsonToStdout()
    {
        var stdout = new StringWriter();
        var originalOut = Console.Out;
        try
        {
            Console.SetOut(stdout);
            Program.WriteOutput("{\"users\":[]}", null);
        }
        finally
        {
            Console.SetOut(originalOut);
        }

        using var document = JsonDocument.Parse(stdout.ToString());
        Assert.AreEqual(JsonValueKind.Array, document.RootElement.GetProperty("users").ValueKind);
    }

    [TestMethod]
    public void ReportCredentialResults_ReportsRecoveredValuesAndExplainsPeks()
    {
        var stderr = new StringWriter();
        var originalError = Console.Error;
        try
        {
            Console.SetError(stderr);
            Program.ReportCredentialResults(
                [new User
                {
                    PasswordHashes = new PasswordHashes { NtHash = "hash" },
                    PasswordHistory = ["old-hash"],
                    SupplementalCredentials = new SupplementalCredentials
                    {
                        KerberosKeys = [new KerberosKey { Key = "key" }]
                    }
                }],
                []);
        }
        finally
        {
            Console.SetError(originalError);
        }

        var output = stderr.ToString();
        Assert.Contains("1 user hash set(s)", output);
        Assert.Contains("1 history hash(es)", output);
        Assert.Contains("1 Kerberos key(s)", output);
        Assert.Contains("used internally and are not exported", output);
        Assert.DoesNotContain("No account password hashes", output);
    }

    [TestMethod]
    public void ReportCredentialResults_WithNoHashes_ReportsActionableWarning()
    {
        var stderr = new StringWriter();
        var originalError = Console.Error;
        try
        {
            Console.SetError(stderr);
            Program.ReportCredentialResults([], []);
        }
        finally
        {
            Console.SetError(originalError);
        }

        Assert.Contains("verify that the NTDS.dit and SYSTEM hive are a matching pair", stderr.ToString());
    }
}
