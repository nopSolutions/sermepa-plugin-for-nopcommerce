//Contributor: Noel Revuelta
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Web.Mvc;
using Nop.Core;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Payments;
using Nop.Plugin.Payments.Sermepa.Models;
using Nop.Services.Configuration;
using Nop.Services.Logging;
using Nop.Services.Orders;
using Nop.Services.Payments;
using Nop.Web.Framework.Controllers;

namespace Nop.Plugin.Payments.Sermepa.Controllers
{
    public class PaymentSermepaController : BasePaymentController
    {
        private readonly ISettingService _settingService;
        private readonly IPaymentService _paymentService;
        private readonly IOrderService _orderService;
        private readonly IOrderProcessingService _orderProcessingService;
        private readonly ILogger _logger;
        private readonly SermepaPaymentSettings _sermepaPaymentSettings;
        private readonly PaymentSettings _paymentSettings;

        public PaymentSermepaController(ISettingService settingService, 
            IPaymentService paymentService, IOrderService orderService, 
            IOrderProcessingService orderProcessingService, 
            ILogger logger, SermepaPaymentSettings sermepaPaymentSettings,
            PaymentSettings paymentSettings)
        {
            this._settingService = settingService;
            this._paymentService = paymentService;
            this._orderService = orderService;
            this._orderProcessingService = orderProcessingService;
            this._logger = logger;
            this._sermepaPaymentSettings = sermepaPaymentSettings;
            this._paymentSettings = paymentSettings;
        }
        
        [AdminAuthorize]
        [ChildActionOnly]
        public ActionResult Configure()
        {
            var model = new ConfigurationModel
            {
                NombreComercio = _sermepaPaymentSettings.NombreComercio,
                Titular = _sermepaPaymentSettings.Titular,
                Producto = _sermepaPaymentSettings.Producto,
                FUC = _sermepaPaymentSettings.FUC,
                Terminal = _sermepaPaymentSettings.Terminal,
                Moneda = _sermepaPaymentSettings.Moneda,
                ClaveReal = _sermepaPaymentSettings.ClaveReal,
                ClavePruebas = _sermepaPaymentSettings.ClavePruebas,
                Pruebas = _sermepaPaymentSettings.Pruebas,
                AdditionalFee = _sermepaPaymentSettings.AdditionalFee
            };

            return View("~/Plugins/Payments.Sermepa/Views/Configure.cshtml", model);
        }

        [HttpPost]
        [AdminAuthorize]
        [ChildActionOnly]
        public ActionResult Configure(ConfigurationModel model)
        {
            if (!ModelState.IsValid)
                return Configure();

            //save settings
            _sermepaPaymentSettings.NombreComercio = model.NombreComercio;
            _sermepaPaymentSettings.Titular = model.Titular;
            _sermepaPaymentSettings.Producto = model.Producto;
            _sermepaPaymentSettings.FUC = model.FUC;
            _sermepaPaymentSettings.Terminal = model.Terminal;
            _sermepaPaymentSettings.Moneda = model.Moneda;
            _sermepaPaymentSettings.ClaveReal = model.ClaveReal;
            _sermepaPaymentSettings.ClavePruebas = model.ClavePruebas;
            _sermepaPaymentSettings.Pruebas = model.Pruebas;
            _sermepaPaymentSettings.AdditionalFee = model.AdditionalFee;
            _settingService.SaveSetting(_sermepaPaymentSettings);

            return View("~/Plugins/Payments.Sermepa/Views/Configure.cshtml", model);
        }

        [ChildActionOnly]
        public ActionResult PaymentInfo()
        {
            return View("~/Plugins/Payments.Sermepa/Views/PaymentInfo.cshtml");
        }

        [NonAction]
        public override IList<string> ValidatePaymentForm(FormCollection form)
        {
            var warnings = new List<string>();
            return warnings;
        }

        [NonAction]
        public override ProcessPaymentRequest GetPaymentInfo(FormCollection form)
        {
            var paymentInfo = new ProcessPaymentRequest();
            return paymentInfo;
        }

        [ValidateInput(false)]
        public ActionResult Return(FormCollection form)
        {
            var processor = _paymentService.LoadPaymentMethodBySystemName("Payments.Sermepa") as SermepaPaymentProcessor;
            if (processor == null ||
                !processor.IsPaymentMethodActive(_paymentSettings) || !processor.PluginDescriptor.Installed)
                throw new NopException("Sermepa module cannot be loaded");

            //_logger.Information("TPV SERMEPA: Host " + Request.UserHostName);

            //ID de Pedido
            var orderId = Request["Ds_Order"];
            var strDs_Merchant_Order = Request["Ds_Order"];

            var strDs_Merchant_Amount = Request["Ds_Amount"];
            var strDs_Merchant_MerchantCode = Request["Ds_MerchantCode"];
            var strDs_Merchant_Currency = Request["Ds_Currency"];

            //Respuesta del TPV
            var str_Merchant_Response = Request["Ds_Response"];
            var dsResponse = Convert.ToInt32(Request["Ds_Response"]);

            //Clave
            var pruebas = _sermepaPaymentSettings.Pruebas;
            var clave = pruebas ? _sermepaPaymentSettings.ClavePruebas : _sermepaPaymentSettings.ClaveReal;

            //Calculo de la firma
            var sha = string.Format("{0}{1}{2}{3}{4}{5}",
                strDs_Merchant_Amount,
                strDs_Merchant_Order,
                strDs_Merchant_MerchantCode,
                strDs_Merchant_Currency,
                str_Merchant_Response,
                clave);

            SHA1 shaM = new SHA1Managed();
            var shaResult = shaM.ComputeHash(Encoding.Default.GetBytes(sha));
            var shaResultStr = BitConverter.ToString(shaResult).Replace("-", "");

            //Firma enviada
            var signature = CommonHelper.EnsureNotNull(Request["Ds_Signature"]);

            //Comprobamos la integridad de las comunicaciones con las claves
            //LogManager.InsertLog(LogTypeEnum.OrderError, "TPV SERMEPA: Clave generada", "CLAVE GENERADA: " + SHAresultStr);
            //LogManager.InsertLog(LogTypeEnum.OrderError, "TPV SERMEPA: Clave obtenida", "CLAVE OBTENIDA: " + signature);
            if (!signature.Equals(shaResultStr))
            {
                _logger.Error("TPV SERMEPA: Clave incorrecta. Las claves enviada y generada no coinciden: " + shaResultStr + " != " + signature);

                return RedirectToAction("Index", "Home", new { area = "" });
            }

            //Pedido
            var order = _orderService.GetOrderById(Convert.ToInt32(orderId));
            if (order == null)
                throw new NopException(string.Format("El pedido de ID {0} no existe", orderId));

            //Actualizamos el pedido
            if (dsResponse > -1 && dsResponse < 100)
            {
                //Lo marcamos como pagado
                if (_orderProcessingService.CanMarkOrderAsPaid(order))
                {
                    _orderProcessingService.MarkOrderAsPaid(order);
                }

                //order note
                order.OrderNotes.Add(new OrderNote
                {
                    Note = "Información del pago: " + Request.Form,
                    DisplayToCustomer = false,
                    CreatedOnUtc = DateTime.UtcNow
                });
                _orderService.UpdateOrder(order);
                return RedirectToRoute("CheckoutCompleted", new { orderId = order.Id });
            }

            _logger.Error("TPV SERMEPA: Pago no autorizado con ERROR: " + dsResponse);

            //order note
            order.OrderNotes.Add(new OrderNote
            {
                Note = "!!! PAGO DENEGADO !!! " + Request.Form,
                DisplayToCustomer = false,
                CreatedOnUtc = DateTime.UtcNow
            });
            _orderService.UpdateOrder(order);
            return RedirectToAction("Index", "Home", new { area = "" });
        }

        [ValidateInput(false)]
        public ActionResult Error()
        {
            var processor = _paymentService.LoadPaymentMethodBySystemName("Payments.Sermepa") as SermepaPaymentProcessor;
            if (processor == null ||
                !processor.IsPaymentMethodActive(_paymentSettings) || !processor.PluginDescriptor.Installed)
                throw new NopException("Sermepa module cannot be loaded");

            return RedirectToAction("Index", "Home", new { area = "" });
        }
    }
}