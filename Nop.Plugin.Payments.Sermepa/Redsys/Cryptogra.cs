using System;
using System.Security.Cryptography;
using System.Text;

namespace Nop.Plugin.Payments.Sermepa.Redsys
{
    public class Cryptogra
    {
        /// <summary>
        /// <see cref="https://pagosonline.redsys.es/conexion-redireccion.html#envio-peticionRedireccion"/>
        /// calculate HMAC SHA-256
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="key"></param>
        /// <returns>byte[] with the result</returns>
        public byte[] GetHMACSHA256(string msg, byte[] key)
        {
            try
            {
                /// Obtain byte[] from input string 
                byte[] msgBytes = Encoding.UTF8.GetBytes(msg);

                // Initialize the keyed hash object.
                using (HMACSHA256 hmac = new HMACSHA256(key))
                {

                    //Compute the hash of the input file.
                    byte[] hashValue = hmac.ComputeHash(msgBytes, 0, msgBytes.Length);
                    return hashValue;
                }
            }
            // Error in crytographic process  
            catch (CryptographicException ex)
            {
                throw new CryptographicException(ex.Message);
            }

        }

        /// <summary>
        /// Encrypt 3DES 
        /// </summary>
        /// <param name="plainText"></param>
        /// <param name="key"></param>
        /// <returns>byte[] with the result</returns>
        public byte[] Encrypt3DES(string plainText, byte[] key)
        {
            if (String.IsNullOrEmpty(plainText))
            {
                throw new FormatException();
            }


            byte[] toEncryptArray = Encoding.UTF8.GetBytes(plainText);
            TripleDESCryptoServiceProvider tdes = new TripleDESCryptoServiceProvider();


            try
            {

                /// <summary>
                /// SALT used in 3DES encryptation process.
                /// </summary>
                byte[] SALT = new byte[8] { 0, 0, 0, 0, 0, 0, 0, 0 };

                // Block size 64 bit (8 bytes)
                tdes.BlockSize = 64;

                // Key Size 192 bit (24 bytes)
                tdes.KeySize = 192;
                tdes.Mode = CipherMode.CBC;
                tdes.Padding = PaddingMode.Zeros;


                tdes.IV = SALT;
                tdes.Key = key;

                var cTransform = tdes.CreateEncryptor();

                //transform the specified region of bytes array to resultArray
                byte[] resultArray =
                  cTransform.TransformFinalBlock(toEncryptArray, 0,
                  toEncryptArray.Length);

                //Release resources held by TripleDes Encryptor
                tdes.Clear();

                return resultArray;

            }
            // Error in Cryptographic method
            catch (CryptographicException ex)
            {
                throw new CryptographicException(ex.Message);
            }


        }
    }

}
