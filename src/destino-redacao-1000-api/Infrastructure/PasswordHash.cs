using System;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;

namespace destino_redacao_1000_api
{
    public static class PasswordHash
    {
        public static string Hash(string password, string salt)
        {
            String[] tempAry = salt.Split('-');
            byte[] decBytes = new byte[tempAry.Length];

            for (int i = 0; i < tempAry.Length; i++)
                decBytes[i] = Convert.ToByte(tempAry[i], 16);

            // derive a 256-bit subkey (use HMACSHA1 with 10,000 iterations)
            string hashed = Convert.ToBase64String(KeyDerivation.Pbkdf2(
                password: password,
                salt: decBytes,
                prf: KeyDerivationPrf.HMACSHA1,
                iterationCount: 10000,
                numBytesRequested: 256 / 8));

            
            return hashed;
        }

        public static string GenerateSalt()
        {
            // generate a 128-bit salt using a secure PRNG
            byte[] salt = new byte[128 / 8];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }

            return BitConverter.ToString(salt);
        }
    }
}