using Nop.Core.Configuration;

namespace Nop.Plugin.Payments.Sermepa
{
    public class SermepaPaymentSettings : ISettings
    {
        public string NombreComercio { get; set; }
        public string Titular { get; set; }
        public string Producto { get; set; }
        public string FUC { get; set; }
        public string Terminal { get; set; }
        public string Moneda { get; set; }
        public string ClaveReal { get; set; }
        public string ClavePruebas { get; set; }
        public bool Pruebas { get; set; }
        public decimal AdditionalFee { get; set; }
    }
}
