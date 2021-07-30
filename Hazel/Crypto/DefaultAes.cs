using System;
using System.Security.Cryptography;

namespace Hazel.Crypto
{
    /// <summary>
    /// AES provider using the default System.Security.Cryptography implementation
    /// </summary>
    public class DefaultAes : IAes
    {
        private readonly ICryptoTransform encryptor_;

        /// <summary>
        /// Create a new default instance of the AES block cipher
        /// </summary>
        /// <param name="keySizeInBits">Size of the AES key size (in bits)</param>
        /// <param name="key">Encryption key</param>
        public DefaultAes(int keySizeInBits, ByteSpan key)
        {
            // Create the AES block cipher
            using (Aes aes = Aes.Create())
            {
                aes.KeySize = keySizeInBits;
                aes.BlockSize = keySizeInBits;
                aes.Mode = CipherMode.ECB;
                aes.Padding = PaddingMode.Zeros;
                aes.Key = key.ToArray();

                this.encryptor_ = aes.CreateEncryptor();
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            this.encryptor_.Dispose();
        }

        /// <inheritdoc/>
        public int EncryptBlock(ByteSpan inputSpan, ByteSpan outputSpan)
        {
            if (inputSpan.Length != outputSpan.Length)
            {
                throw new ArgumentException($"ouputSpan length ({outputSpan.Length}) does not match inputSpan length ({inputSpan.Length})", nameof(outputSpan));
            }

            return this.encryptor_.TransformBlock(inputSpan.GetUnderlyingArray(), inputSpan.Offset, inputSpan.Length, outputSpan.GetUnderlyingArray(), outputSpan.Offset);
        }
    }
}
