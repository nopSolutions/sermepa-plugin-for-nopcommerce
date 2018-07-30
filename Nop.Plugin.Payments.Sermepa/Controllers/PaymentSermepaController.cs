//Contributor: Noel Revuelta
using System;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Payments;
using Nop.Plugin.Payments.Sermepa.Models;
using Nop.Services.Configuration;
using Nop.Services.Logging;
using Nop.Services.Orders;
using Nop.Services.Payments;
using Nop.Services.Security;
using Nop.Web.Framework;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Mvc.Filters;

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
        private readonly IWebHelper _webHelper;
        private readonly IPermissionService _permissionService;

        public PaymentSermepaController(ISettingService settingService,
            IPaymentService paymentService, IOrderService orderService,
            IOrderProcessingService orderProcessingService,
            ILogger logger, SermepaPaymentSettings sermepaPaymentSettings,
            IWebHelper webHelper,
            IPermissionService permissionService)
        {
            this._settingService = settingService;
            this._paymentService = paymentService;
            this._orderService = orderService;
            this._orderProcessingService = orderProcessingService;
            this._logger = logger;
            this._sermepaPaymentSettings = sermepaPaymentSettings;
            this._webHelper = webHelper;
            this._permissionService = permissionService;
        }

        private string GetValue(string key, IFormCollection form)
        {
            return (form.Keys.Contains(key) ? form[key].ToString() : _webHelper.QueryString<string>(key)) ?? string.Empty;
        }

        [AuthorizeAdmin]
        [Area(AreaNames.Admin)]
        public IActionResult Configure()
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManagePaymentMethods))
                return AccessDeniedView();

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
        [AuthorizeAdmin]
        [Area(AreaNames.Admin)]
        public IActionResult Configure(ConfigurationModel model)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManagePaymentMethods))
                return AccessDeniedView();

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

        public IActionResult Return(IpnModel model)
        {
            var form = model.Form;
            var processor = _paymentService.LoadPaymentMethodBySystemName("Payments.Sermepa") as SermepaPaymentProcessor;
            if (processor == null ||
                !_paymentService.IsPaymentMethodActive(processor) || !processor.PluginDescriptor.Installed)
                throw new NopException("Sermepa module cannot be loaded");

            //_logger.Information("TPV SERMEPA: Host " + Request.UserHostName);

            //ID de Pedido
            var orderId = GetValue("Ds_Order", form);
            var strDsMerchantOrder = GetValue("Ds_Order", form);

            var strDsMerchantAmount = GetValue("Ds_Amount", form);
            var strDsMerchantMerchantCode = GetValue("Ds_MerchantCode", form);
            var strDsMerchantCurrency = GetValue("Ds_Currency", form);

            //Respuesta del TPV
            var strMerchantResponse = GetValue("Ds_Response", form);
            var dsResponse = Convert.ToInt32(GetValue("Ds_Response", form));

            //Clave
            var pruebas = _sermepaPaymentSettings.Pruebas;
            var clave = pruebas ? _sermepaPaymentSettings.ClavePruebas : _sermepaPaymentSettings.ClaveReal;

            //Calculo de la firma
            var sha = $"{strDsMerchantAmount}{strDsMerchantOrder}{strDsMerchantMerchantCode}{strDsMerchantCurrency}{strMerchantResponse}{clave}";

            SHA1 shaM = new SHA1Managed();
            var shaResult = shaM.ComputeHash(Encoding.Default.GetBytes(sha));
            var shaResultStr = BitConverter.ToString(shaResult).Replace("-", "");

            //Firma enviada
            var signature = CommonHelper.EnsureNotNull(GetValue("Ds_Signature", form));

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
                throw new NopException($"El pedido de ID {orderId} no existe");

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
                    Note = "Información del pago: " + model.Form,
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
                Note = "!!! PAGO DENEGADO !!! " + model.Form,
                DisplayToCustomer = false,
                CreatedOnUtc = DateTime.UtcNow
            });
            _orderService.UpdateOrder(order);
            return RedirectToAction("Index", "Home", new { area = "" });
        }

        public IActionResult Error()
        {
            var processor = _paymentService.LoadPaymentMethodBySystemName("Payments.Sermepa") as SermepaPaymentProcessor;
            if (processor == null ||
                !_paymentService.IsPaymentMethodActive(processor) || !processor.PluginDescriptor.Installed)
                throw new NopException("Sermepa module cannot be loaded");

            return RedirectToAction("Index", "Home", new { area = "" });
        }
    }
}