using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace System
{
    /// <summary>
    /// 加密模式。
    /// </summary>
    public enum CryptoKind
    {
        /// <summary>
        /// 有效的 KEY 与 IV 长度，以英文字符为单位： KEY（Min:8 Max:8 Skip:0），IV（8）。
        /// </summary>
        DES,
        /// <summary>
        /// 有效的 KEY 与 IV 长度，以英文字符为单位： KEY（Min:16 Max:24 Skip:8），IV（8）。
        /// </summary>
        TripleDES,
        /// <summary>
        /// 有效的 KEY 与 IV 长度，以英文字符为单位： KEY（Min:5 Max:16 Skip:1），IV（8）。
        /// </summary>
        RC2,
        /// <summary>
        /// 有效的 KEY 与 IV 长度，以英文字符为单位： KEY（Min:16 Max:32 Skip:8），IV（16）。
        /// </summary>
        Rijndael,
        /// <summary>
        /// 有效的 KEY 与 IV 长度，以英文字符为单位： KEY（Min:16 Max:32 Skip:8），IV（16）。
        /// </summary>
        AES
    }

    /// <summary>
    /// 加密扩展。
    /// </summary>
    public static class CryptoExtentions
    {
        private static SymmetricAlgorithm GetSymmetricAlgorithm(CryptoKind kind)
        {
            SymmetricAlgorithm algorithm = null;

            switch (kind)
            {
                case CryptoKind.DES:
                    algorithm = DES.Create();
                    break;
                case CryptoKind.TripleDES:
                    algorithm = TripleDES.Create();
                    break;
                case CryptoKind.RC2:
                    algorithm = RC2.Create();
                    break;
                case CryptoKind.Rijndael:
                    algorithm = Rijndael.Create();
                    break;
                case CryptoKind.AES:
                    algorithm = Aes.Create();
                    break;
            }

            return algorithm;
        }

        /// <summary>
        /// 对称加密（<see cref="CipherMode.ECB"/>，<seealso cref="PaddingMode.PKCS7"/>）。
        /// </summary>
        /// <param name="data">内容。</param>
        /// <param name="key">键。</param>
        /// <param name="kind">加密方式。</param>
        /// <returns></returns>
        public static string Encrypt(this string data, string key, CryptoKind kind = CryptoKind.DES)
        {
            if (data is null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            if (key is null)
            {
                throw new ArgumentNullException(nameof(key));
            }


            ICryptoTransform crypto = null;

            using (var algorithm = GetSymmetricAlgorithm(kind))
            {
                var rgbKey = Encoding.ASCII.GetBytes(key);

                if (!algorithm.ValidKeySize(rgbKey.Length * 8))
                    throw new ArgumentOutOfRangeException(nameof(key));

                algorithm.Key = rgbKey;
                algorithm.Mode = CipherMode.ECB;
                algorithm.Padding = PaddingMode.PKCS7;

                crypto = algorithm.CreateEncryptor();
            }

            using (var ms = new MemoryStream())
            {
                using (var cs = new CryptoStream(ms, crypto, CryptoStreamMode.Write))
                {
                    var buffer = Encoding.UTF8.GetBytes(data);

                    cs.Write(buffer, 0, buffer.Length);

                    cs.FlushFinalBlock();

                    cs.Close();
                }

                return Convert.ToBase64String(ms.ToArray());
            }
        }

        /// <summary>
        /// 对称减密（<see cref="CipherMode.ECB"/>，<seealso cref="PaddingMode.PKCS7"/>）。
        /// </summary>
        /// <param name="data">内容。</param>
        /// <param name="key">键。</param>
        /// <param name="kind">减密方式。</param>
        /// <returns></returns>
        public static string Decrypt(this string data, string key, CryptoKind kind = CryptoKind.DES)
        {
            if (data is null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            if (key is null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            ICryptoTransform crypto = null;

            using (var algorithm = GetSymmetricAlgorithm(kind))
            {
                var rgbKey = Encoding.ASCII.GetBytes(key);

                if (!algorithm.ValidKeySize(rgbKey.Length * 8))
                    throw new ArgumentOutOfRangeException(nameof(key));

                algorithm.Key = rgbKey;
                algorithm.Mode = CipherMode.ECB;
                algorithm.Padding = PaddingMode.PKCS7;

                crypto = algorithm.CreateDecryptor();
            }

            using (var ms = new MemoryStream())
            {
                using (var cs = new CryptoStream(ms, crypto, CryptoStreamMode.Write))
                {
                    var buffer = Convert.FromBase64String(data);

                    cs.Write(buffer, 0, buffer.Length);

                    cs.FlushFinalBlock();

                    cs.Close();
                }

                return Encoding.UTF8.GetString(ms.ToArray());
            }
        }

        /// <summary>
        /// 对称加密。
        /// </summary>
        /// <param name="data">内容。</param>
        /// <param name="key">键。</param>
        /// <param name="iv">初始化向量。</param>
        /// <param name="kind">加密方式。</param>
        /// <returns></returns>
        public static string Encrypt(this string data, string key, string iv, CryptoKind kind = CryptoKind.DES)
        {
            if (data is null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            if (key is null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            if (iv is null)
            {
                throw new ArgumentNullException(nameof(iv));
            }

            ICryptoTransform crypto = null;

            using (var algorithm = GetSymmetricAlgorithm(kind))
            {
                var rgbKey = Encoding.ASCII.GetBytes(key);

                var rgbIV = Encoding.ASCII.GetBytes(iv);

                if (!algorithm.ValidKeySize(rgbKey.Length * 8))
                    throw new ArgumentOutOfRangeException(nameof(key));

                if (algorithm.IV.Length != rgbIV.Length)
                    throw new ArgumentOutOfRangeException(nameof(iv));

                crypto = algorithm.CreateEncryptor(rgbKey, rgbIV);
            }

            using (var ms = new MemoryStream())
            {
                using (var cs = new CryptoStream(ms, crypto, CryptoStreamMode.Write))
                {
                    var buffer = Encoding.UTF8.GetBytes(data);

                    cs.Write(buffer, 0, buffer.Length);

                    cs.FlushFinalBlock();

                    cs.Close();
                }

                return Convert.ToBase64String(ms.ToArray());
            }
        }

        /// <summary>
        /// 对称减密。
        /// </summary>
        /// <param name="data">内容。</param>
        /// <param name="key">键。</param>
        /// <param name="iv">初始化向量。</param>
        /// <param name="kind">减密方式。</param>
        /// <returns></returns>
        public static string Decrypt(this string data, string key, string iv, CryptoKind kind = CryptoKind.DES)
        {
            if (data is null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            if (key is null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            if (iv is null)
            {
                throw new ArgumentNullException(nameof(iv));
            }

            ICryptoTransform crypto = null;

            using (var algorithm = GetSymmetricAlgorithm(kind))
            {
                var rgbKey = Encoding.ASCII.GetBytes(key);

                var rgbIV = Encoding.ASCII.GetBytes(iv);

                if (!algorithm.ValidKeySize(rgbKey.Length * 8))
                    throw new ArgumentOutOfRangeException(nameof(key));

                if (algorithm.IV.Length != rgbIV.Length)
                    throw new ArgumentOutOfRangeException(nameof(iv));

                crypto = algorithm.CreateDecryptor(rgbKey, rgbIV);
            }

            using (var ms = new MemoryStream())
            {
                using (var cs = new CryptoStream(ms, crypto, CryptoStreamMode.Write))
                {
                    var buffer = Convert.FromBase64String(data);

                    cs.Write(buffer, 0, buffer.Length);

                    cs.FlushFinalBlock();

                    cs.Close();
                }

                return Encoding.UTF8.GetString(ms.ToArray());
            }
        }

        /// <summary>
        /// 使用 RSA 公钥加密。
        /// </summary>
        /// <param name="data">数据。</param>
        /// <param name="publicKey">公钥。</param>
        /// <returns></returns>
        public static string RsaEncrypt(this string data, string publicKey)
        {
            if (data is null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            if (publicKey is null)
            {
                throw new ArgumentNullException(nameof(publicKey));
            }

            using (var rsa = RSA.Create())
            {
                rsa.FromXmlString(publicKey);

#if NET40 || NET45

                return Convert.ToBase64String(rsa.EncryptValue(Encoding.UTF8.GetBytes(data)));
#else
                return Convert.ToBase64String(rsa.Encrypt(Encoding.UTF8.GetBytes(data), RSAEncryptionPadding.OaepSHA512));
#endif
            }
        }

        /// <summary>
        /// 使用 RSA 私钥解密。
        /// </summary>
        /// <param name="data">数据。</param>
        /// <param name="privateKey">私钥。</param>
        /// <returns></returns>
        public static string RsaDecrypt(this string data, string privateKey)
        {
            using (var rsa = RSA.Create())
            {
                rsa.FromXmlString(privateKey);

#if NET40 || NET45
                return Encoding.UTF8.GetString(rsa.DecryptValue(Convert.FromBase64String(data)));
#else
                return Encoding.UTF8.GetString(rsa.Decrypt(Convert.FromBase64String(data), RSAEncryptionPadding.OaepSHA512));
#endif
            }
        }

        /// <summary>
        /// MD5加密(32个字符)。
        /// </summary>
        /// <param name="data">数据。</param>
        /// <param name="encoding">编码，默认：UTF8。</param>
        /// <param name="toUpperCase">是否转为大小。</param>
        /// <returns></returns>
        public static string Md5(this string data, Encoding encoding = null, bool toUpperCase = true)
        {
            if (data is null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            byte[] buffer = null;

            using (var md5 = MD5.Create())
            {
                buffer = md5.ComputeHash((encoding ?? Encoding.UTF8).GetBytes(data));
            }

            var sb = new StringBuilder();

            foreach (var item in buffer)
            {
                sb.Append(item.ToString(toUpperCase ? "X2" : "x2"));
            }

            return sb.ToString();
        }
    }
}
