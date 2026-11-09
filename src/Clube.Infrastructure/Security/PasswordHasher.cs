using System;
using System.Security.Cryptography;
using System.Text;

namespace Clube.Infrastructure.Security;

public static class PasswordHasher
{
    public static string Hash(string senha)
    {
        if (string.IsNullOrEmpty(senha))
            return string.Empty;

        using var sha256 = SHA256.Create();
        var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(senha + "_salt_clube_2026"));
        return Convert.ToBase64String(hashedBytes);
    }

    public static bool Verificar(string senha, string hash)
    {
        if (string.IsNullOrEmpty(senha) || string.IsNullOrEmpty(hash))
            return false;

        return Hash(senha) == hash;
    }
}
