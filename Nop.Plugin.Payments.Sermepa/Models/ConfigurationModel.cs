using System.ComponentModel;
using Nop.Web.Framework.Mvc;

namespace Nop.Plugin.Payments.Sermepa.Models
{
    public class ConfigurationModel : BaseNopModel
    {
        [DisplayName("Nombre del comercio")]
        public string NombreComercio { get; set; }

        [DisplayName("Nombre y Apellidos del titular")]
        public string Titular { get; set; }

        [DisplayName("Descripción del producto")]
        public string Producto { get; set; }

        [DisplayName("FUC comercio")]
        public string FUC { get; set; }

        [DisplayName("Terminal")]
        public string Terminal { get; set; }

        [DisplayName("Moneda")]
        public string Moneda { get; set; }

        [DisplayName("Clave Real")]
        public string ClaveReal { get; set; }

        [DisplayName("Clave Pruebas")]
        public string ClavePruebas { get; set; }

        [DisplayName("En pruebas")]
        public bool Pruebas { get; set; }

        [DisplayName("Additional fee")]
        public decimal AdditionalFee { get; set; }
    }
}