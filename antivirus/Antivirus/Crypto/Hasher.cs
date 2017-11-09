using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Antivirus.Crypto
{
    public class Hasher
    {
        private HashAlgorithm algorithm = SHA256.Create();

        public string HashSha256(string path)
        {
            using (var file = new FileStream(path, FileMode.Open))
            {
                var hash = this.algorithm.ComputeHash(file);
                return this.Stringify(hash);
            }
        }

        public byte[] HashSha256(byte[] bytes)
        {
            return this.algorithm.ComputeHash(bytes);
        }

        private string Stringify(byte[] hash)
        {
            StringBuilder sb = new StringBuilder();
            foreach (byte b in hash)
            {
                sb.Append(b.ToString("X2"));
            }

            return sb.ToString();
        }
    }
}
