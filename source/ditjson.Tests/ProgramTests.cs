namespace ditjson.Tests;

[TestClass]
public class ProgramTests
{
    [TestMethod]
    public void ShouldApplyCrownJewels_WithDefaultOptions_ReturnsFalse()
    {
        Assert.IsFalse(Program.ShouldApplyCrownJewels(new Options()));
    }

    [TestMethod]
    public void ShouldApplyCrownJewels_WhenExplicitlyRequested_ReturnsTrue()
    {
        var options = new Options { CrownJewels = true };

        Assert.IsTrue(Program.ShouldApplyCrownJewels(options));
    }

    [TestMethod]
    public void ShouldApplyCrownJewels_WithAllDataCompatibilityFlag_ReturnsFalse()
    {
        var options = new Options { CrownJewels = true, AllData = true };

        Assert.IsFalse(Program.ShouldApplyCrownJewels(options));
    }
}
