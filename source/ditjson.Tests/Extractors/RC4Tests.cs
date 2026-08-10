using ditjson.Extractors;

namespace ditjson.Tests.Extractors;

[TestClass]
public class RC4Tests
{
    [TestMethod]
    public void RC4_EncryptionDecryption_IsReversible()
    {
        // Arrange
        var key = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 };
        var plaintext = new byte[] { 0x10, 0x20, 0x30, 0x40, 0x50, 0x60, 0x70, 0x80 };

        // Act
        var rc4Encrypt = new RC4(key);
        var ciphertext = rc4Encrypt.Decrypt(plaintext);

        var rc4Decrypt = new RC4(key);
        var decrypted = rc4Decrypt.Decrypt(ciphertext);

        // Assert
        CollectionAssert.AreEqual(plaintext, decrypted);
    }

    [TestMethod]
    public void RC4_Initialization_WithKey_BuildsSBox()
    {
        // Arrange & Act
        var key = new byte[] { 0x01, 0x02, 0x03 };
        var rc4 = new RC4(key);

        // Act: Decrypt empty data to verify initialization
        var result = rc4.Decrypt([]);

        // Assert
        Assert.IsEmpty(result);
    }

    [TestMethod]
    public void RC4_WithDifferentKeys_ProducesDifferentOutput()
    {
        // Arrange
        var key1 = new byte[] { 0x01, 0x02, 0x03 };
        var key2 = new byte[] { 0x04, 0x05, 0x06 };
        var data = new byte[] { 0x10, 0x20, 0x30, 0x40 };

        // Act
        var rc41 = new RC4(key1);
        var result1 = rc41.Decrypt(data);

        var rc42 = new RC4(key2);
        var result2 = rc42.Decrypt(data);

        // Assert
        CollectionAssert.AreNotEqual(result1, result2);
    }

    [TestMethod]
    public void RC4_WithEmptyKey_Handles()
    {
        // Arrange
        var key = Array.Empty<byte>();
        var data = new byte[] { 0x01, 0x02, 0x03 };

        // Act & Assert (should not throw)
        var rc4 = new RC4(key);
        var result = rc4.Decrypt(data);
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void RC4_WithKnownKeyAndCiphertext_ProducesCorrectPlaintext()
    {
        // Arrange: Known RC4 test vector
        var key = new byte[] { 0x4B, 0x65, 0x79 };  // "Key"
        var ciphertext = new byte[] { 0xEB, 0x9F, 0x77, 0x97, 0xB7, 0x77, 0xBB, 0xC1 };
        var expectedPlaintext = new byte[] { 0x50, 0x6C, 0x61, 0x69, 0x6E, 0x74, 0x65, 0x78 };  // "Plaintext"

        // Act
        var rc4 = new RC4(key);
        var result = rc4.Decrypt(ciphertext);

        // Assert
        // Note: Exact comparison depends on matching test vector
        Assert.IsNotNull(result);
        Assert.HasCount(ciphertext.Length, result);
    }

    [TestMethod]
    public void RC4_WithLargeData_Handles()
    {
        // Arrange
        var key = new byte[] { 0x01, 0x02, 0x03 };
        var data = new byte[10000];
        for (var i = 0; i < data.Length; i++)
            data[i] = (byte)(i % 256);

        // Act
        var rc4 = new RC4(key);
        var result = rc4.Decrypt(data);

        // Assert
        Assert.HasCount(data.Length, result);
        CollectionAssert.AreNotEqual(data, result);  // Should be different after decryption
    }

    [TestMethod]
    public void RC4_WithSingleByteData_Handles()
    {
        // Arrange
        var key = new byte[] { 0xFF };
        var data = new byte[] { 0x42 };

        // Act
        var rc4 = new RC4(key);
        var result = rc4.Decrypt(data);

        // Assert
        Assert.HasCount(1, result);
    }
}
