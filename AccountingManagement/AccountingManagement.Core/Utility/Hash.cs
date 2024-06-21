using System;
using System.Security.Cryptography;
using System.Text;

namespace AccountingManagement.Core.Utility
{
    public static class Hash
    {
        public static string GetHash(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return string.Empty;
            }

            using (var shaHash = SHA256.Create())
            {
                var hashData = shaHash.ComputeHash(Encoding.UTF8.GetBytes(input));

                var sBuilder = new StringBuilder();

                // Loop through each byte of the hased data and format each one as a hexadecimal string
                for (int i = 0; i < hashData.Length; i++)
                {
                    sBuilder.Append(hashData[i].ToString("x2"));
                }

                return sBuilder.ToString();
            }
        }

        public static bool VerifyHash(string input, string hash)
        {
            var inputHash = GetHash(input);

            return inputHash.Equals(hash, StringComparison.OrdinalIgnoreCase);
        }
    }
}
