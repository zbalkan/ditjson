using ditjson.Decoders;

namespace ditjson.Tests.Decoders;

[TestClass]
public class SidDecoderTests
{
    [TestMethod]
    [OSCondition(OperatingSystems.Windows)]
    public void Decode_WithValidUserSid_ReturnsCorrectFormat()
    {
        // Arrange: Common user SID (S-1-5-21-domain-domain-domain-500)
        var sid = new byte[]
        {
            0x01,                           // Revision
            0x02,                           // SubAuthority count
            0x00, 0x00, 0x00, 0x00, 0x00, 0x05,  // Authority (5)
            0xE4, 0x04, 0x00, 0x00,       // SubAuth 1
            0xF4, 0x01, 0x00, 0x00        // SubAuth 2
        };

        // Act
        var result = SidDecoder.Decode(sid);

        // Assert
        Assert.IsNotNull(result);
        Assert.StartsWith("S-1-5-", result);
    }

    [TestMethod]
    [OSCondition(OperatingSystems.Windows)]
    public void Decode_WithNullInput_ReturnsNull()
    {
        // Act
        var result = SidDecoder.Decode((byte[]?)null);

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    [OSCondition(OperatingSystems.Windows)]
    public void Decode_WithEmptyArray_ReturnsNull()
    {
        // Act
        var result = SidDecoder.Decode([]);

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    [OSCondition(OperatingSystems.Windows)]
    public void Decode_WithInvalidRevision_ReturnsNull()
    {
        // Arrange: Invalid revision number
        var sid = new byte[]
        {
            0xFF,  // Invalid revision
            0x01,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x05
        };

        // Act
        var result = SidDecoder.Decode(sid);

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    [OSCondition(OperatingSystems.Windows)]
    public void Decode_WithValidSid_ContainsAuthority()
    {
        // Arrange
        var sid = new byte[]
        {
            0x01,
            0x01,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x05,
            0xE4, 0x04, 0x00, 0x00
        };

        // Act
        var result = SidDecoder.Decode(sid);

        // Assert
        Assert.IsNotNull(result);
        Assert.Contains("5", result);
    }

    [TestMethod]
    [OSCondition(OperatingSystems.Windows)]
    public void Decode_WithHexString_ReturnsValidSid()
    {
        // Arrange: Hex representation of SID
        var hexSid = "010200000000000005000000e4040000f4010000";

        // Act
        var result = SidDecoder.Decode(hexSid);

        // Assert
        Assert.IsNotNull(result);
        Assert.StartsWith("S-1-5-", result);
    }
}
