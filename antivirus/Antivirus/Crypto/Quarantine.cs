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
        public event Action<FileScan> OnScanUpdated;

        private string Directory { get; }
        private string Key { get; }

        private RNGCryptoServiceProvider random = new RNGCryptoServiceProvider();
        private byte[] keyBytes;
        private int keySize = 256;

        private object mutex = new object();

        public Quarantine(string directory, string key)
        {
            this.Directory = directory;
            this.Key = key;
            this.keyBytes = this.GetKeyBytes(key, this.keySize);

            if (!System.IO.Directory.Exists(directory))
            {
                System.IO.Directory.CreateDirectory(directory);
            }
        }

        public async Task LockToQuarantine(FileScan scan)
        {
            var name = this.GenerateName();
            var path = Path.Combine(this.Directory, name);

            try
            {
                if (scan.QuarantineState != QuarantineState.NotQuarantined)
                {
                    throw new Exception($"Scan {scan.Path} is already in quarantine or is being encrypted/decrypted");
                }
                if (!File.Exists(scan.Path))
                {
                    throw new Exception($"Scan file {scan.Path} not found");
                }

                scan.QuarantineState = QuarantineState.Encrypting;
                this.OnScanUpdated?.Invoke(scan);

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

                scan.QuarantinePath = name;
                scan.QuarantineState = QuarantineState.InQuarantine;

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

                scan.QuarantineState = QuarantineState.NotQuarantined;
                throw e;
            }
        }
        public async Task UnlockFromQuarantine(FileScan scan)
        {
            var path = Path.Combine(this.Directory, scan.QuarantinePath);

            try
            {
                if (scan.QuarantineState != QuarantineState.InQuarantine)
                {
                    throw new Exception($"Scan {scan.Path} is not in quarantine");
                }
                if (!File.Exists(path))
                {
                    throw new Exception($"Quarantine file {path} not found");
                }

                scan.QuarantineState = QuarantineState.Decrypting;
                this.OnScanUpdated?.Invoke(scan);

                using (FileStream outStream = new FileStream(scan.Path, FileMode.Create))
                using (FileStream inStream = new FileStream(path, FileMode.Open))
                using (RijndaelManaged aes = this.CreateAES())
                {
                    File.SetAttributes(scan.Path, File.GetAttributes(path));

                    aes.IV = scan.IV;

                    var decryptor = aes.CreateDecryptor();
                    using (CryptoStream encryptStream = new CryptoStream(inStream, decryptor, CryptoStreamMode.Read))
                    {
                        await encryptStream.CopyToAsync(outStream);
                    }

                    outStream.SetLength(scan.Size);
                }

                File.Delete(path);

                scan.QuarantineState = QuarantineState.NotQuarantined;
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

                scan.QuarantineState = QuarantineState.InQuarantine;
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

        private string GenerateName()
        {
            while (true)
            {
                var name = this.CreateRandomString(8);
                var path = System.IO.Path.Combine(this.Directory, name);
                if (!File.Exists(path))
                {
                    return name;
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
