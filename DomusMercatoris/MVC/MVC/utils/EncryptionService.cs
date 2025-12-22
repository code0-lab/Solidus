using System.Security.Cryptography;
using System.Text;

namespace DomusMercatorisDotnetMVC.Utils
{
    public class EncryptionService
    {
        private readonly string _key;

        public EncryptionService(IConfiguration configuration)
        {
            // Gerçek hayatta bu anahtar Environment Variable veya Key Vault'tan gelmelidir.
            // Şimdilik appsettings'den veya sabit bir değerden alıyoruz.
            _key = configuration["EncryptionKey"] ?? "b14ca5898a4e4133bbce2ea2315a1916"; // 32 chars for AES-256
        }

        public string Encrypt(string plainText)
        {
            if (string.IsNullOrEmpty(plainText)) return plainText;

            using var aes = Aes.Create();
            aes.Key = Encoding.UTF8.GetBytes(_key);
            aes.GenerateIV();

            using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
            using var ms = new MemoryStream();
            
            // IV'yi (Initialization Vector) şifreli verinin başına ekliyoruz
            ms.Write(aes.IV, 0, aes.IV.Length);

            using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
            using (var sw = new StreamWriter(cs))
            {
                sw.Write(plainText);
            }

            return Convert.ToBase64String(ms.ToArray());
        }

        public string Decrypt(string cipherText)
        {
            if (string.IsNullOrEmpty(cipherText)) return cipherText;

            try 
            {
                var fullCipher = Convert.FromBase64String(cipherText);

                using var aes = Aes.Create();
                aes.Key = Encoding.UTF8.GetBytes(_key);

                // IV'yi verinin başından okuyoruz
                var iv = new byte[aes.BlockSize / 8];
                Array.Copy(fullCipher, 0, iv, 0, iv.Length);
                aes.IV = iv;

                using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
                using var ms = new MemoryStream(fullCipher, iv.Length, fullCipher.Length - iv.Length);
                using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
                using var sr = new StreamReader(cs);

                return sr.ReadToEnd();
            }
            catch
            {
                // Eğer şifre çözülemezse (eski veri şifresiz olabilir), metni olduğu gibi döndür
                // Bu, geçiş süreci için bir güvenlik önlemidir.
                return cipherText;
            }
        }
    }
}
