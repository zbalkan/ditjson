using ditjson.Extractors;

namespace ditjson.Tests.Extractors;

[TestClass]
public class ObjectClassifierTests
{
    [DataTestMethod]
    [DataRow(0x30000000)]
    [DataRow(0x30000002)]
    public void IsUserObject_RecognizesUserAndTrustAccounts(int value) =>
        Assert.IsTrue(ObjectClassifier.IsUserObject(value));

    [DataTestMethod]
    [DataRow(0x10000000)]
    [DataRow(0x10000001)]
    [DataRow(0x20000000)]
    [DataRow(0x20000001)]
    public void IsGroupObject_RecognizesAllGroupAndAliasTypes(int value) =>
        Assert.IsTrue(ObjectClassifier.IsGroupObject(value));

    [TestMethod]
    public void IsComputerObject_RecognizesMachineAccounts() =>
        Assert.IsTrue(ObjectClassifier.IsComputerObject(0x30000001));

    [TestMethod]
    public void Classifiers_RejectUnrelatedValues()
    {
        Assert.IsFalse(ObjectClassifier.IsUserObject(0x7d));
        Assert.IsFalse(ObjectClassifier.IsGroupObject(0x73));
        Assert.IsFalse(ObjectClassifier.IsComputerObject(0x6c));
        Assert.IsFalse(ObjectClassifier.IsUserObject(0));
    }
}
