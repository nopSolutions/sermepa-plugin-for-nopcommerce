namespace Nop.Plugin.Payments.Sermepa.Models
{
    public class IpnModel
    {
        public string Ds_SignatureVersion { get; set; }

        public string Ds_MerchantParameters { get; set; }

        public string Ds_Signature { get; set; }
    }
}
