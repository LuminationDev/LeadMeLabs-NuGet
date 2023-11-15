using System;
using System.Text;
using System.Security.Cryptography;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace LeadMeLabsLibrary
{
    public static class EncryptionHelper
    {
        private static readonly string OldEncryptionKey = CollectOldSecret();
        private static readonly string EncryptionKey = CollectSecret();

        // This constant is used to determine the KeySize of the encryption algorithm in bits.
        // We divide this by 8 within the code below to get the equivalent number of bytes.
        private const int KeySize = 128;

        // This constant determines the number of iterations for the password bytes generation function.
        private const int DerivationIterations = 1000;

        #region Encryption Detection
        /// <summary>
        /// Attempt to read the supplied file as a UTF-16 format. If the decryption method throws an error this means that
        /// UTF-8 was used as the encryption method.Upon error, decipher the text using the old method then encrypt using
        /// UTF-16, returning the decrypted data at the end.
        /// </summary>
        /// <param name="fileName">A string of the file (path) to check.</param>
        /// <returns></returns>
        public static string DetectFileEncryption(string fileName)
        {
            try
            {
                string text = File.ReadAllText(fileName, Encoding.Unicode);
                string decryptedText = UnicodeDecryptNode(text);
                return decryptedText;
            }
            catch (Exception)
            {
                string text = File.ReadAllText(fileName, Encoding.UTF8);
                string decryptedText = Utf8DecryptNode(text);

                //Encrypt as Unicode
                string encryptedText = UnicodeEncryptNode(decryptedText);
                File.WriteAllText(fileName, encryptedText, Encoding.Unicode); //Attempt to overwrite the old UTF-8 file
                string decryptedNodeText = UnicodeDecryptNode(encryptedText);
                return decryptedNodeText;
            }
        }
        #endregion

        #region UTF8 Encryption
        /// <summary>
        /// Encrypts the given plain text using a pass phrase.
        /// </summary>
        /// <param name="plainText">The plain text to encrypt.</param>
        /// <param name="passPhrase">The pass phrase used for encryption.</param>
        /// <returns>The encrypted string.</returns>
        public static string Encrypt(string plainText, string passPhrase)
        {
            if (string.IsNullOrEmpty(plainText)) throw new ArgumentException(nameof(plainText));
            if (string.IsNullOrEmpty(passPhrase)) throw new ArgumentException(nameof(passPhrase));

            string encrypted = "";
            if (plainText.Length % 32 != 0)
            {
                int requiredPadding = 32 - (plainText.Length % 32);
                for (int i = 0; i < requiredPadding; i++)
                {
                    plainText += "_";
                }
            }
            for (int i = 0; i < plainText.Length; i += 32)
            {
                int substringLength = 32;
                if (plainText.Length < i + 32)
                {
                    substringLength = plainText.Length - i;
                }
                encrypted += Utf8Encrypt32(plainText.Substring(i, substringLength), passPhrase);
            }

            return encrypted;
        }

        private static string Utf8Encrypt32(string plainText, string passPhrase)
        {
            // Salt and IV is randomly generated each time, but is prepended to encrypted cipher text
            // so that the same Salt and IV values can be used when decrypting.  
            var saltStringBytes = Generate128BitsOfRandomEntropy();
            var ivStringBytes = Generate128BitsOfRandomEntropy();
            var plainTextBytes = Encoding.UTF8.GetBytes(plainText);
            
            using var password = new Rfc2898DeriveBytes(passPhrase, saltStringBytes, DerivationIterations);
            var keyBytes = password.GetBytes(KeySize / 8);
            
            using var symmetricKey = Aes.Create("AesManaged");
            if (symmetricKey == null) return "";

            symmetricKey.BlockSize = 128;
            symmetricKey.Mode = CipherMode.CBC;
            symmetricKey.Padding = PaddingMode.PKCS7;
            
            using var encryptor = symmetricKey.CreateEncryptor(keyBytes, ivStringBytes);
            using var memoryStream = new MemoryStream();
            using var cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write);
            cryptoStream.Write(plainTextBytes, 0, plainTextBytes.Length);
            cryptoStream.FlushFinalBlock();
            
            // Create the final bytes as a concatenation of the random salt bytes, the random iv bytes and the cipher bytes.
            var cipherTextBytes = saltStringBytes;
            cipherTextBytes = cipherTextBytes.Concat(ivStringBytes).ToArray();
            cipherTextBytes = cipherTextBytes.Concat(memoryStream.ToArray()).ToArray();
            memoryStream.Close();
            cryptoStream.Close();
            return Convert.ToBase64String(cipherTextBytes);
        }
        
        /// <summary>
        /// Decrypts the given plain text in UTF8 using a pass phrase.
        /// </summary>
        /// <param name="cipherText">The plain text to decrypt.</param>
        /// <param name="passPhrase">The pass phrase used for encryption.</param>
        /// <returns>The decrypted data.</returns>
        public static string Decrypt(string cipherText, string passPhrase)
        {
            if (string.IsNullOrEmpty(cipherText)) throw new ArgumentException(nameof(cipherText));
            if (string.IsNullOrEmpty(passPhrase)) throw new ArgumentException(nameof(passPhrase));

            string decrypted = "";
            for (int i = 0; i < cipherText.Length; i += 108)
            {
                int substringLength = 108;
                if (cipherText.Length < i + 108)
                {
                    substringLength = cipherText.Length - i;
                }

                decrypted += Utf8Decrypt108(cipherText.Substring(i, substringLength), passPhrase);
            }

            return decrypted.Trim('_');
        }

        /// <summary>
        /// Decrypts the given cipher text using a pass phrase.
        /// </summary>
        /// <param name="cipherText">The cipher text to decrypt.</param>
        /// <param name="passPhrase">The pass phrase used for decryption.</param>
        /// <returns>The decrypted string.</returns>
        private static string? Utf8Decrypt108(string cipherText, string passPhrase)
        {
            // Get the complete stream of bytes that represent:
            // [32 bytes of Salt] + [32 bytes of IV] + [n bytes of CipherText]
            var cipherTextBytesWithSaltAndIv = Convert.FromBase64String(cipherText);
            // Get the saltbytes by extracting the first 32 bytes from the supplied cipherText bytes.
            var saltStringBytes = cipherTextBytesWithSaltAndIv.Take(KeySize / 8).ToArray();
            // Get the IV bytes by extracting the next 32 bytes from the supplied cipherText bytes.
            var ivStringBytes = cipherTextBytesWithSaltAndIv.Skip(KeySize / 8).Take(KeySize / 8).ToArray();
            // Get the actual cipher text bytes by removing the first 64 bytes from the cipherText string.
            var cipherTextBytes = cipherTextBytesWithSaltAndIv.Skip((KeySize / 8) * 2).Take(cipherTextBytesWithSaltAndIv.Length - ((KeySize / 8) * 2)).ToArray();

            using var password = new Rfc2898DeriveBytes(passPhrase, saltStringBytes, DerivationIterations);
            var keyBytes = password.GetBytes(KeySize / 8);
            using var symmetricKey = Aes.Create("AesManaged");
            if (symmetricKey == null) return null;

            symmetricKey.BlockSize = 128;
            symmetricKey.Mode = CipherMode.CBC;
            symmetricKey.Padding = PaddingMode.PKCS7;

            using var decryptor = symmetricKey.CreateDecryptor(keyBytes, ivStringBytes);
            using var memoryStream = new MemoryStream(cipherTextBytes);
            using var cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read);
            var plainTextBytes = new byte[cipherTextBytes.Length];
            var decryptedByteCount = cryptoStream.Read(plainTextBytes, 0, plainTextBytes.Length);
            memoryStream.Close();
            cryptoStream.Close();
            return Encoding.UTF8.GetString(plainTextBytes, 0, decryptedByteCount);
        }

        /// <summary>
        /// Decrypt the data when encrypted through the nodejs launcher program.
        /// </summary>
        /// <param name="encryptedData">A string of encrypted data to decipher.</param>
        /// <returns>A decrypted string.</returns>
        public static string Utf8DecryptNode(string encryptedData)
        {
            byte[] key = Encoding.UTF8.GetBytes(OldEncryptionKey);
            byte[] iv = HexStringToByteArray(encryptedData.Substring(0, 32));

            string encrypted = encryptedData.Substring(32);

            byte[] encryptedBytes = HexStringToByteArray(encrypted);
            byte[] decryptedBytes;

            using (Aes aes = Aes.Create())
            {
                aes.Key = key;
                aes.IV = iv;

                ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

                using MemoryStream ms = new MemoryStream(encryptedBytes);
                using CryptoStream cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
                using StreamReader reader = new StreamReader(cs);
                decryptedBytes = Encoding.UTF8.GetBytes(reader.ReadToEnd());
            }

            string decrypted = Encoding.UTF8.GetString(decryptedBytes);
            return decrypted;
        }
        #endregion

        #region Unicode Encryption
        public static string UnicodeEncrypt(string plainText, string passPhrase)
        {
            if (string.IsNullOrEmpty(plainText)) throw new ArgumentException(nameof(plainText));
            if (string.IsNullOrEmpty(passPhrase)) throw new ArgumentException(nameof(passPhrase));

            var saltStringBytes = Generate128BitsOfRandomEntropy();
            string ivHex = GenerateRandomIv(); // Get IV as a hexadecimal string

            // Get the bytes of the plain text using Unicode encoding
            byte[] plainTextBytes = Encoding.Unicode.GetBytes(plainText);

            using var password = new Rfc2898DeriveBytes(passPhrase, saltStringBytes, DerivationIterations);
            byte[] keyBytes = password.GetBytes(KeySize / 8);

            using var symmetricKey = Aes.Create();
            if (symmetricKey == null) throw new InvalidOperationException("AES encryption is not available.");

            symmetricKey.BlockSize = 128;
            symmetricKey.Mode = CipherMode.CBC;
            symmetricKey.Padding = PaddingMode.PKCS7;

            // Generate a random IV for each encryption operation
            byte[] ivBytes = HexStringToByteArray(ivHex);

            using var encryptor = symmetricKey.CreateEncryptor(keyBytes, ivBytes);
            using var memoryStream = new MemoryStream();
            using var cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write);
            cryptoStream.Write(plainTextBytes, 0, plainTextBytes.Length);
            cryptoStream.FlushFinalBlock();

            // Concatenate the salt, IV, and encrypted data into a single byte array
            byte[] cipherTextBytes = password.Salt;
            cipherTextBytes = cipherTextBytes.Concat(ivBytes).ToArray();
            cipherTextBytes = cipherTextBytes.Concat(memoryStream.ToArray()).ToArray();

            return Convert.ToBase64String(cipherTextBytes);
        }

        public static string UnicodeDecrypt(string cipherText, string passPhrase)
        {
            if (string.IsNullOrEmpty(cipherText)) throw new ArgumentException(nameof(cipherText));
            if (string.IsNullOrEmpty(passPhrase)) throw new ArgumentException(nameof(passPhrase));

            var cipherTextBytesWithSaltAndIv = Convert.FromBase64String(cipherText);
            var saltStringBytes = cipherTextBytesWithSaltAndIv.Take(KeySize / 8).ToArray();
            var ivStringBytes = cipherTextBytesWithSaltAndIv.Skip(KeySize / 8).Take(KeySize / 8).ToArray();
            var cipherTextBytes = cipherTextBytesWithSaltAndIv.Skip((KeySize / 8) * 2).Take(cipherTextBytesWithSaltAndIv.Length - ((KeySize / 8) * 2)).ToArray();

            using var password = new Rfc2898DeriveBytes(passPhrase, saltStringBytes, DerivationIterations);
            byte[] keyBytes = password.GetBytes(KeySize / 8);

            using var symmetricKey = Aes.Create();
            if (symmetricKey == null) throw new InvalidOperationException("AES encryption is not available.");

            symmetricKey.BlockSize = 128;
            symmetricKey.Mode = CipherMode.CBC;
            symmetricKey.Padding = PaddingMode.PKCS7;

            // The IV is automatically extracted from the cipherTextBytesWithSaltAndIv during decryption
            using var decryptor = symmetricKey.CreateDecryptor(keyBytes, ivStringBytes);
            using var memoryStream = new MemoryStream(cipherTextBytes);
            using var cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read);
            using var streamReader = new StreamReader(cryptoStream);
            string decryptedText = streamReader.ReadToEnd();

            string decryptedTextTrimmed = decryptedText.TrimEnd('\0'); // Trim any null characters at the end of the decrypted text
            string sanitizedInput = Regex.Replace(decryptedTextTrimmed, @"\p{C}", "");
            return sanitizedInput;
        }

        public static string UnicodeEncryptNode(string data)
        {
            byte[] key = Encoding.UTF8.GetBytes(EncryptionKey);
            string ivHex = GenerateRandomIv(); // Get IV as a hexadecimal string

            byte[] dataBytes = Encoding.Unicode.GetBytes(data); // Use Encoding.Unicode for UTF-16
            byte[] encryptedBytes;

            using (Aes aes = Aes.Create())
            {
                aes.Key = key;
                aes.IV = HexStringToByteArray(ivHex); // Convert the IV hexadecimal string to a byte array

                ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

                using MemoryStream ms = new MemoryStream();
                using CryptoStream cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write);
                cs.Write(dataBytes, 0, dataBytes.Length);
                cs.FlushFinalBlock();
                encryptedBytes = ms.ToArray();
            }

            string hexEncryptedData = BitConverter.ToString(encryptedBytes).Replace("-", "");
            string encryptedData = ivHex + hexEncryptedData; // Append IV to the encrypted data

            return encryptedData;
        }

        public static string UnicodeDecryptNode(string encryptedData)
        {
            byte[] key = Encoding.UTF8.GetBytes(EncryptionKey);
            byte[] iv = HexStringToByteArray(encryptedData.Substring(0, 32));

            string encrypted = encryptedData.Substring(32);

            byte[] encryptedBytes = HexStringToByteArray(encrypted);
            byte[] decryptedBytes;

            using (Aes aes = Aes.Create())
            {
                aes.Key = key;
                aes.IV = iv;

                ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

                using MemoryStream ms = new MemoryStream(encryptedBytes);
                using CryptoStream cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
                using StreamReader reader = new StreamReader(cs, Encoding.Unicode); // Use Encoding.Unicode here for UTF-16
                decryptedBytes = Encoding.Unicode.GetBytes(reader.ReadToEnd());
            }

            string decrypted = Encoding.Unicode.GetString(decryptedBytes); // Use Encoding.Unicode here for UTF-16
            return decrypted;
        }
        #endregion

        #region Helpers
        /// <summary>
        /// Generate a random byte array of characters of length 16.
        /// </summary>
        /// <returns>A randomised byte array of length 16</returns>
        private static byte[] Generate128BitsOfRandomEntropy()
        {
            var randomBytes = new byte[16]; // 16 Bytes will give us 128 bits.
            using (var rngCsp = RandomNumberGenerator.Create())
            {
                // Fill the array with cryptographically secure random bytes.
                rngCsp.GetBytes(randomBytes);
            }
            return randomBytes;
        }

        /// <summary>
        /// Generate a random string of characters of length 16.
        /// </summary>
        /// <returns>A randomised byte array of length 16</returns>
        private static string GenerateRandomIv()
        {
            byte[] iv = new byte[16]; // 16 bytes for a 128-bit IV
            using (var rngCsp = RandomNumberGenerator.Create())
            {
                rngCsp.GetBytes(iv);
            }

            return BitConverter.ToString(iv).Replace("-", ""); // Return IV as a hexadecimal string
        }

        /// <summary>
        /// Convert a string of hex characters into a byte array.
        /// </summary>
        /// <param name="hexString">A hexadecimal string</param>
        /// <returns>A byte array of characters</returns>
        private static byte[] HexStringToByteArray(string hexString)
        {
            try
            {
                int numBytes = hexString.Length / 2;
                byte[] bytes = new byte[numBytes];
                for (int i = 0; i < numBytes; i++)
                {
                    bytes[i] = Convert.ToByte(hexString.Substring(i * 2, 2), 16);
                }

                return bytes;
            }
            catch (Exception)
            {
                return Array.Empty<byte>();
            }
        }

        /// <summary>
        /// This key is used for decrypting the nodejs encryption only. The key is based off the unique product ID of the individual
        /// computer the software is running on. It cannot be accessed remotely and only visible with file and admin access.
        /// </summary>
        /// <returns>A string representing the secret for encryption, it has been modified to be 32 characters long.</returns>
        private static string CollectOldSecret()
        {
            using RegistryKey rk = Registry.LocalMachine;
            
            string? key = rk.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\")?.GetValue("ProductId")?.ToString();
            if (key == null)
            {
                return "";
            }

            key = key.Replace("-", "");

            string paddedKey = key;

            while (paddedKey.Length < 32)
            {
                paddedKey += "0";
            }

            return paddedKey;
        }
        
        private static string CollectSecret()
        {
            string? key = SystemInformation.GetMACAddress();
            if (key == null)
            {
                return "";
            }
            
            key = key.Replace("-", "");

            string paddedKey = key;

            while (paddedKey.Length < 32)
            {
                paddedKey += "0";
            }

            paddedKey = paddedKey.ToLower();
            return paddedKey;
        }
        #endregion
    }
}
