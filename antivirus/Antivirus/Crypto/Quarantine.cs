using Antivirus.Scan;
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Antivirus.Crypto
{
    public class Quarantine
    {
        private string Path { get; }
        private string Key { get; }

        private RNGCryptoServiceProvider random = new RNGCryptoServiceProvider();
        private byte[] keyBytes;
        private int keySize = 256;

        public Quarantine(string path, string key)
        {
            this.Path = path;
            this.Key = key;
            this.keyBytes = this.GetKeyBytes(key, this.keySize);

            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }

        public async Task LockToQuarantine(FileScan scan)
        {
            var path = this.GeneratePath();

            try
            {
                if (scan.InQuarantine)
                {
                    throw new Exception($"Scan {scan.Path} is already in quarantine");
                }
                if (!File.Exists(scan.Path))
                {
                    throw new Exception($"Scan file {scan.Path} not found");
                }

                using (FileStream outStream = new FileStream(path, FileMode.Create))
                using (FileStream inStream = new FileStream(scan.Path, FileMode.Open))
                using (RijndaelManaged aes = this.CreateAES())
                {
                    File.SetAttributes(path, File.GetAttributes(scan.Path));

                    aes.GenerateIV();
                    scan.IV = aes.IV.ToArray();

                    var encryptor = aes.CreateEncryptor();
                    using (CryptoStream encryptStream = new CryptoStream(outStream, encryptor, CryptoStreamMode.Write))
                    {
                        await inStream.CopyToAsync(encryptStream);
                    }
                }

                scan.QuarantinePath = path;
                scan.InQuarantine = true;

                File.Delete(scan.Path);
            }
            catch (Exception e)
            {
                try
                {
                    File.Delete(path);
                }
                catch (IOException)
                {

                }
                throw e;
            }
        }
        public async Task UnlockFromQuarantine(FileScan scan)
        {
            try
            {
                if (!scan.InQuarantine)
                {
                    throw new Exception($"Scan {scan.Path} is not in quarantine");
                }
                if (!File.Exists(scan.QuarantinePath))
                {
                    throw new Exception($"Quarantine file {scan.QuarantinePath} not found");
                }

                using (FileStream outStream = new FileStream(scan.Path, FileMode.Create))
                using (FileStream inStream = new FileStream(scan.QuarantinePath, FileMode.Open))
                using (RijndaelManaged aes = this.CreateAES())
                {
                    File.SetAttributes(scan.Path, File.GetAttributes(scan.QuarantinePath));

                    aes.IV = scan.IV;

                    var decryptor = aes.CreateDecryptor();
                    using (CryptoStream encryptStream = new CryptoStream(inStream, decryptor, CryptoStreamMode.Read))
                    {
                        await encryptStream.CopyToAsync(outStream);
                    }

                    outStream.SetLength(scan.Size);
                }

                File.Delete(scan.QuarantinePath);

                scan.InQuarantine = false;
                scan.QuarantinePath = "";
            }
            catch (Exception e)
            {
                try
                {
                    File.Delete(scan.Path);
                }
                catch (IOException)
                {

                }
                throw e;
            }
        }

        private RijndaelManaged CreateAES()
        {
            var aes = new RijndaelManaged();
            aes.Padding = PaddingMode.Zeros;
            aes.Mode = CipherMode.ECB;
            aes.KeySize = this.keySize;
            aes.Key = this.keyBytes.ToArray();
            return aes;
        }

        private string GeneratePath()
        {
            while (true)
            {
                var name = this.CreateRandomString(8);
                var path = System.IO.Path.Combine(this.Path, name);
                if (!File.Exists(path))
                {
                    return path;
                }
            }
        }
        private string CreateRandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
              .Select(s => s[this.NextInt(s.Length)]).ToArray());
        }
        private int NextInt(int max)
        {
            byte[] bytes = new byte[4];
            this.random.GetNonZeroBytes(bytes);

            return Math.Abs(BitConverter.ToInt32(bytes, 0)) % max;
        }

        private byte[] GetKeyBytes(string key, int keySize)
        {
            var keyBytes = new byte[keySize / 8];
            Encoding.UTF8.GetBytes(key).CopyTo(keyBytes, 0);
            return keyBytes;
        }
    }
}
