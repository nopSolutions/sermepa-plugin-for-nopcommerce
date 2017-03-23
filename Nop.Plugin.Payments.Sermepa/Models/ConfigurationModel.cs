using Nop.Web.Framework;
using Nop.Web.Framework.Mvc;

namespace Nop.Plugin.Payments.Sermepa.Models
{
    public class ConfigurationModel : BaseNopModel
    {
        [NopResourceDisplayName("Plugins.Payments.Sermepa.NombreComercio")]
        public string NombreComercio { get; set; }

        [NopResourceDisplayName("Plugins.Payments.Sermepa.Titular")]
        public string Titular { get; set; }

        [NopResourceDisplayName("Plugins.Payments.Sermepa.Producto")]
        public string Producto { get; set; }

        [NopResourceDisplayName("Plugins.Payments.Sermepa.FUC")]
        public string FUC { get; set; }

        [NopResourceDisplayName("Plugins.Payments.Sermepa.Terminal")]
        public string Terminal { get; set; }

        [NopResourceDisplayName("Plugins.Payments.Sermepa.Moneda")]
        public string Moneda { get; set; }

        [NopResourceDisplayName("Plugins.Payments.Sermepa.ClaveReal")]
        public string ClaveReal { get; set; }

        [NopResourceDisplayName("Plugins.Payments.Sermepa.ClavePruebas")]
        public string ClavePruebas { get; set; }

        [NopResourceDisplayName("Plugins.Payments.Sermepa.Pruebas")]
        public bool Pruebas { get; set; }

        [NopResourceDisplayName("Plugins.Payments.Sermepa.AdditionalFee")]
        public decimal AdditionalFee { get; set; }
    }
}