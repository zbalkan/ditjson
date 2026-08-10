using ditjson.Decoders;

namespace ditjson.Tests.Decoders;

[TestClass]
public class GuidDecoderTests
{
    [TestMethod]
    public void Decode_WithAllZeros_ReturnsEmptyGuid()
    {
        // Arrange: All zeros (empty GUID)
        var guidData = new byte[16];

        // Act
        var result = GuidDecoder.Decode(guidData);

        // Assert
        Assert.AreEqual(Guid.Empty, result);
    }

    [TestMethod]
    public void Decode_WithEmptyArray_ReturnsEmptyGuid()
    {
        // Act
        var result = GuidDecoder.Decode([]);

        // Assert
        Assert.AreEqual(Guid.Empty, result);
    }

    [TestMethod]
    public void Decode_WithHexString_ReturnsGuid()
    {
        // Arrange: Hex string representation
        var hexValue = "01020304-0506-0708-090a-0b0c0d0e0f10";

        // Act
        var result = GuidDecoder.Decode(hexValue);

        // Assert
        Assert.AreNotEqual(Guid.Empty, result);
    }

    [TestMethod]
    public void Decode_WithInsufficientLength_ReturnsEmptyGuid()
    {
        // Arrange: Only 8 bytes instead of 16
        var guidData = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08 };

        // Act
        var result = GuidDecoder.Decode(guidData);

        // Assert
        Assert.AreEqual(Guid.Empty, result);
    }

    [TestMethod]
    public void Decode_WithInvalidHexString_ReturnsEmptyGuid()
    {
        // Arrange
        var hexValue = "not-a-valid-guid";

        // Act
        var result = GuidDecoder.Decode(hexValue);

        // Assert
        Assert.AreEqual(Guid.Empty, result);
    }

    [TestMethod]
    public void Decode_WithNullInput_ReturnsEmptyGuid()
    {
        // Act
        var result = GuidDecoder.Decode((byte[]?)null);

        // Assert
        Assert.AreEqual(Guid.Empty, result);
    }

    [TestMethod]
    public void Decode_WithValidGuid_ReturnsGuid()
    {
        // Arrange: Sample GUID bytes
        var guidData = new byte[]
        {
            0x01, 0x02, 0x03, 0x04,  // First 4 bytes
            0x05, 0x06,               // Next 2 bytes
            0x07, 0x08,               // Next 2 bytes
            0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F, 0x10  // Last 8 bytes
        };

        // Act
        var result = GuidDecoder.Decode(guidData);

        // Assert
        Assert.AreNotEqual(Guid.Empty, result);
    }
}
