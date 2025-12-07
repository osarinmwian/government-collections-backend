using System.Security.Cryptography;
using System.Text;

namespace GovernmentCollections.Service.Utilities;

public static class HashUtility
{
    public static string ComputeSHA512Hash(string input)
    {
        using var sha512 = SHA512.Create();
        var inputBytes = Encoding.UTF8.GetBytes(input);
        var hashBytes = sha512.ComputeHash(inputBytes);
        return Convert.ToHexString(hashBytes);
    }
}