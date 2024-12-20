using System.Security.Cryptography;

namespace Haihv.Elis.Tool.ChuyenDvhc.Extensions;

/// <summary>
/// Lớp chứa các phương thức mở rộng để mã hóa và giải mã chuỗi văn bản.
/// </summary>
public static class EncryptionHelper
{
    private static readonly byte[]
        Key = "iHK2DThdy8ZJw4E753V5n8a7gYXSn9sU"u8.ToArray(); // Must be 16, 24, or 32 bytes long

    private static readonly byte[] Iv = "WDZKEVFjsM3q8F5D"u8.ToArray(); // Must be 16 bytes long

    /// <summary>
    /// Mã hóa một chuỗi văn bản thành chuỗi Base64.
    /// </summary>
    /// <param name="plainText">Chuỗi văn bản cần mã hóa.</param>
    /// <returns>Chuỗi đã được mã hóa dưới dạng Base64.</returns>
    public static string Encrypt(this string plainText)
    {
        using var aes = Aes.Create();
        aes.Key = Key;
        aes.IV = Iv;
        var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
        using var ms = new MemoryStream();
        using var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write);
        using (var sw = new StreamWriter(cs))
        {
            sw.Write(plainText);
        }

        return Convert.ToBase64String(ms.ToArray());
    }

    /// <summary>
    /// Giải mã một chuỗi Base64 thành chuỗi văn bản.
    /// </summary>
    /// <param name="cipherText">Chuỗi Base64 cần giải mã.</param>
    /// <returns>Chuỗi văn bản đã được giải mã.</returns>
    public static string Decrypt(this string cipherText)
    {
        var buffer = Convert.FromBase64String(cipherText);
        using var aes = Aes.Create();
        aes.Key = Key;
        aes.IV = Iv;
        var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
        using var ms = new MemoryStream(buffer);
        using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
        using var sr = new StreamReader(cs);
        return sr.ReadToEnd();
    }
}