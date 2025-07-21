using System.IO;
using System.Security.Cryptography;
using System.Text;
namespace Backup.Service.Services;


public static class AesEncryptionService
{
    // Encrypt a plain text with a password
    public static string Encrypt(string plainText, string password)
    {
        var salt = GenerateRandomBytes(16); // 128-bit salt
        var keyIv = DeriveKeyAndIV(password, salt);

        using var aes = Aes.Create();
        aes.Key = keyIv.Key;
        aes.IV = keyIv.IV;

        using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
        using var ms = new MemoryStream();
        using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
        using (var sw = new StreamWriter(cs))
        {
            sw.Write(plainText);
        }

        var encryptedBytes = ms.ToArray();

        var result = new byte[salt.Length + encryptedBytes.Length];
        Array.Copy(salt, 0, result, 0, salt.Length);
        Array.Copy(encryptedBytes, 0, result, salt.Length, encryptedBytes.Length);

        return Convert.ToBase64String(result);
    }

    public static string Decrypt(string encryptedText, string password)
    {
        var fullBytes = Convert.FromBase64String(encryptedText);

        var salt = new byte[16];
        var cipherBytes = new byte[fullBytes.Length - salt.Length];
        Array.Copy(fullBytes, 0, salt, 0, salt.Length);
        Array.Copy(fullBytes, salt.Length, cipherBytes, 0, cipherBytes.Length);

        var keyIv = DeriveKeyAndIV(password, salt);

        using var aes = Aes.Create();
        aes.Key = keyIv.Key;
        aes.IV = keyIv.IV;

        using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
        using var ms = new MemoryStream(cipherBytes);
        using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
        using var sr = new StreamReader(cs);

        return sr.ReadToEnd();
    }

    private static byte[] GenerateRandomBytes(int length)
    {
        var bytes = new byte[length];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return bytes;
    }

    private static (byte[] Key, byte[] IV) DeriveKeyAndIV(string password, byte[] salt)
    {
        using var keyGenerator = new Rfc2898DeriveBytes(password, salt, 100_000, HashAlgorithmName.SHA256);
        var key = keyGenerator.GetBytes(32); // 256-bit key
        var iv = keyGenerator.GetBytes(16);  // 128-bit IV
        return (key, iv);
    }
}