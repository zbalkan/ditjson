using ditjson.Decoders;

namespace ditjson.Tests.Decoders;

[TestClass]
public class TimestampDecoderTests
{
    [TestMethod]
    public void Decode_WithInvalidHexValue_ReturnsNull()
    {
        // Arrange
        var hexValue = "invalid";

        // Act
        var result = TimestampDecoder.Decode(hexValue);

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public void Decode_WithNullInput_ReturnsNull()
    {
        // Act
        var result = TimestampDecoder.Decode(null);

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public void Decode_WithValidHexTimestamp_ReturnsIso8601String()
    {
        // Arrange: Hex representation of timestamp
        var hexValue = "00d03f96a48dda01";

        // Act
        var result = TimestampDecoder.Decode(hexValue);

        // Assert
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void DecodeDsTime_WithDatabaseTimestamp_ReturnsIso8601String()
    {
        // These bytes were previously decoded as the corrupt UTF-16 string
        // "铃ඒ\u0003\u0000" instead of as a DSTIME integer.
        var result = TimestampDecoder.DecodeDsTime(13112612035L);

        Assert.AreEqual("2016-07-10T08:13:55.0000000Z", result);
    }

    [TestMethod]
    [DataRow(0L)]
    [DataRow(-1L)]
    [DataRow(long.MaxValue)]
    public void DecodeDsTime_WithUnsetOrInvalidValue_ReturnsNull(long value)
    {
        var result = TimestampDecoder.DecodeDsTime(value);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void DecodeFromInt64_WithMaxValue_ReturnsNull()
    {
        // Arrange: Max value (never expires)
        var filetime = long.MaxValue;

        // Act
        var result = TimestampDecoder.DecodeFromInt64(filetime);

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public void DecodeFromInt64_WithNeverSetValue_ReturnsNull()
    {
        // Arrange: Zero timestamp (never set)
        var filetime = 0L;

        // Act
        var result = TimestampDecoder.DecodeFromInt64(filetime);

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public void DecodeFromInt64_WithValidTimestamp_ReturnsIso8601String()
    {
        // Arrange: Windows FILETIME for 2024-01-15T10:30:00Z
        var filetime = 133480512000000000L;

        // Act
        var result = TimestampDecoder.DecodeFromInt64(filetime);

        // Assert
        Assert.IsNotNull(result);
    }
}
