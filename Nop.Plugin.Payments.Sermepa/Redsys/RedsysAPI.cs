using System;
using System.Collections.Generic;
using System.Web;
using Newtonsoft.Json;

namespace Nop.Plugin.Payments.Sermepa.Redsys
{
    /// <summary>
    /// NOTE: Extracted from Redsys help libraries
    /// </summary>
    /// <see cref="https://pagosonline.redsys.es/descargas.html"/>
    public class RedsysAPI
    {

        private Dictionary<string, string> m_keyvalues;

        private Cryptogra cryp;

        public RedsysAPI()
        {
            m_keyvalues = new Dictionary<string, string>();
            cryp = new Cryptogra();
        }

        /// <summary>
        /// Get parameter by key
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public string GetParameter(string key)
        {
            if (m_keyvalues.ContainsKey(key))
            {
                return m_keyvalues[key];
            }
            return null;
        }

        /// <summary>
        /// Set the value of a key
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public void SetParameter(string key, string value)
        {
            m_keyvalues.Add(key, value);
        }



        /// <summary>
        ///  Convert Dictionary to JSON string
        /// </summary>
        /// <param name="keyvalues"></param>
        /// <returns></returns>
        private string ToJson(Dictionary<string, string> keyvalues)

        {

            try
            {
                string res = JsonConvert.SerializeObject(keyvalues, Formatting.None);
                return res;

            }
            catch (JsonSerializationException ex)
            {
                throw new JsonSerializationException(ex.Message);

            }

        }

        /// <summary>
        ///  Encode string to Base64
        /// </summary>
        /// <param name="toEncode"></param>
        /// <returns></returns>
        private string Base64Encode(string toEncode)
        {
            try
            {
                byte[] toEncodeAsBytes
                 = System.Text.Encoding.UTF8.GetBytes(toEncode);
                string returnValue
                      = System.Convert.ToBase64String(toEncodeAsBytes);
                return returnValue;
            }
            catch (FormatException ex)
            {
                throw new FormatException(ex.Message);
            }
        }

        /// <summary>
        /// Encode url string to Base64
        /// </summary>
        /// <param name="toEncode"></param>
        /// <returns></returns>
        private string Base64Encode_url(string toEncode)
        {
            try
            {
                byte[] toEncodeAsBytes
                 = System.Text.Encoding.UTF8.GetBytes(toEncode);
                string returnValue
                      = System.Convert.ToBase64String(toEncodeAsBytes).Replace('+', '-').Replace('/', '_');
                return returnValue;
            }
            catch (FormatException ex)
            {
                throw new FormatException(ex.Message);

            }
        }
        /// <summary>
        /// Encode byte[] to base64 string
        /// </summary>
        /// <param name="toEncode"></param>
        /// <returns></returns>
        private string Base64Encode2(byte[] toEncode)
        {
            try
            {
                string returnValue
                      = System.Convert.ToBase64String(toEncode);
                return returnValue;
            }
            catch (FormatException ex)
            {

                throw new FormatException(ex.Message);
            }
        }

        /// <summary>
        ///  Decode base64 url to string 
        /// </summary>
        /// <param name="encodedData"></param>
        /// <returns></returns>
        private string Base64Decode_url(string encodedData)
        {
            try
            {
                byte[] encodedDataAsBytes
              = System.Convert.FromBase64String(encodedData.Replace('-', '+').Replace('_', '/'));
                string returnValue =
                   System.Text.Encoding.UTF8.GetString(encodedDataAsBytes);
                return HttpUtility.UrlDecode(returnValue);
            }
            catch (FormatException ex)
            {

                throw new FormatException(ex.Message);

            }
        }

        /// <summary>
        /// Decode Base64 string to byte[]
        /// </summary>
        /// <param name="encodedData"></param>
        /// <returns></returns>
        private byte[] Base64Decode(string encodedData)
        {

            try
            {
                byte[] encodedDataAsBytes
              = System.Convert.FromBase64String(encodedData);

                return encodedDataAsBytes;
            }
            catch (FormatException ex)
            {
                throw new FormatException(ex.Message);
            }
        }

        /// <summary>
        /// Calculate Encoded base64 JSON string with Merchant Parameters 
        /// </summary>
        /// <returns></returns>
        public string createMerchantParameters()
        {

            string json = ToJson(m_keyvalues);

            string j = Base64Encode(json);
            return j;

        }


        /// <summary>
        ///  Get Merchant Order
        /// </summary>
        /// <returns></returns>
        private string getOrder()
        {
            string numOrder = "";

            if (m_keyvalues.ContainsKey("DS_MERCHANT_ORDER"))
            {
                numOrder = m_keyvalues["DS_MERCHANT_ORDER"];

            }
            if (m_keyvalues.ContainsKey("Ds_Merchant_Order"))
            {


                numOrder = m_keyvalues["Ds_Merchant_Order"];
            }
            return numOrder;

        }
        /// <summary>
        /// Calculate  Merchant Signature 
        /// </summary>
        /// <param name="key"> Encoded base64 Key </param>
        /// <returns></returns>
        public string createMerchantSignature(string key)
        {
            // Decode key to byte[]
            byte[] k = Base64Decode(key);

            // Calculate Encoded base64 JSON string with Merchant Parameters 
            string ent = createMerchantParameters();


            // Calculate derivated key by encrypting with 3DES the "DS_MERCHANT_ORDER" with decoded key 
            byte[] kk = cryp.Encrypt3DES(getOrder(), k);


            /// Calculate HMAC SHA256 with Encoded base64 JSON string using derivated key calculated previously
            byte[] res = cryp.GetHMACSHA256(ent, kk);


            // Encode byte[] res to Base64 String
            string result = Base64Encode2(res);

            return result;

        }

        /// <summary>
        /// Decode base64 url to string  (Get the parameters)
        /// </summary>
        /// <param name="dat"></param>
        /// <returns></returns>
        public string decodeMerchantParameters(string dat)
        {

            return Base64Decode_url(dat);

        }
        /// <summary>
        /// Convert JSON string to Dictionary
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        private Dictionary<string, string> FromJson(string json)
        {

            try
            {
                Dictionary<string, string> res = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
                return res;
            }
            catch (JsonSerializationException ex)
            {
                throw new JsonSerializationException(ex.Message);
            }
        }

        /// <summary>
        /// Get Order Notification
        /// </summary>
        /// <returns>"DS_ORDER"</returns>
        private string GetOrderNotif()
        {
            string numOrder = "";
            // "DS_ORDER"
            if (m_keyvalues.ContainsKey("Ds_Order"))
            {
                numOrder = m_keyvalues["Ds_Order"];
            }

            if (m_keyvalues.ContainsKey("DS_ORDER"))
            {
                numOrder = m_keyvalues["DS_ORDER"];
            }
            return numOrder;
        }
        /// <summary>
        /// Calculate Signature Notificacion to comparate with original Signature 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public string createMerchantSignatureNotif(string key, string data)
        {
            // Decode key to byte[]
            var k = Base64Decode(key);

            //Decode base64 url to string  (Get the parameters)
            var deco = Base64Decode_url(data);

            // Convert JSON string to Dictionary
            m_keyvalues = FromJson(deco);

            string result = "";

            // If dictionary has values
            if (m_keyvalues != null)
            {
                // Calculate derivated key by encrypting with 3DES the "DS_ORDER" with decoded key 
                var derivatekey = cryp.Encrypt3DES(GetOrderNotif(), k);

                // Calculate HMAC SHA256 with Encoded base64 JSON string using derivated key calculated previously
                var res = cryp.GetHMACSHA256(data, derivatekey);

                // Encode byte[] res to Base64 String
                string res2 = Base64Encode2(res);

                // Convert the result to be compatible with url
                return res2.Replace('+', '-').Replace('/', '_');
            }
            //If dictionary has not value 
            return result;
        }

    }

}
