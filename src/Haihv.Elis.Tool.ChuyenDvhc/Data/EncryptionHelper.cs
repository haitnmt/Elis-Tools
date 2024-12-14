using System;
using System.IO;
using System.Security.Cryptography;

namespace Haihv.Elis.Tool.ChuyenDvhc.Data;

public static class EncryptionHelper
{
    private static readonly byte[] Key = "iHK2DThdy8ZJw4E753V5n8a7gYXSn9sU"u8.ToArray(); // Must be 16, 24, or 32 bytes long
    private static readonly byte[] Iv = "WDZKEVFjsM3q8F5D"u8.ToArray(); // Must be 16 bytes long

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