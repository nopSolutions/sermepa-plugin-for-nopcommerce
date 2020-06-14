using System;
using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Core.Domain.Orders;
using Nop.Plugin.Payments.Sermepa.Models;
using Nop.Plugin.Payments.Sermepa.Redsys;
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
        private readonly IPaymentPluginManager _paymentPluginManager;

        public PaymentSermepaController(ISettingService settingService,
            IPaymentService paymentService, IOrderService orderService,
            IOrderProcessingService orderProcessingService,
            ILogger logger, SermepaPaymentSettings sermepaPaymentSettings,
            IWebHelper webHelper,
            IPermissionService permissionService,
            IPaymentPluginManager paymentPluginManager)
        {
            this._settingService = settingService;
            this._paymentService = paymentService;
            this._orderService = orderService;
            this._orderProcessingService = orderProcessingService;
            this._logger = logger;
            this._sermepaPaymentSettings = sermepaPaymentSettings;
            this._webHelper = webHelper;
            this._permissionService = permissionService;
            _paymentPluginManager = paymentPluginManager;
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

        /// <summary>
        /// <see cref="https://pagosonline.redsys.es/conexion-redireccion.html#envio-peticionRedireccion"/>
        /// </summary>
        /// <param name="ipn"></param>
        /// <returns></returns>
        public IActionResult Return(IpnModel ipn)
        {
            if (!(_paymentPluginManager.LoadPluginBySystemName("Payments.Sermepa") is SermepaPaymentProcessor processor) || !_paymentPluginManager.IsPluginActive(processor))
                throw new NopException("Sermepa module cannot be loaded");

            var isTestMode = _sermepaPaymentSettings.Pruebas;
            var key = isTestMode ? _sermepaPaymentSettings.ClavePruebas : _sermepaPaymentSettings.ClaveReal;

            var redsysApi = new RedsysAPI();

            if (string.IsNullOrEmpty(ipn.Ds_SignatureVersion) ||
                string.IsNullOrEmpty(ipn.Ds_MerchantParameters) ||
                string.IsNullOrEmpty(ipn.Ds_Signature))
            {
                _logger.Error("TPV SERMEPA: Missing data.");

                return RedirectToAction("Index", "Home", new { area = "" });
            }

            // Decode Base 64 data
            var decodedMerchantParameters = redsysApi.decodeMerchantParameters(ipn.Ds_MerchantParameters);

            // Get Signature notificacion
            var signatureNotif = redsysApi.createMerchantSignatureNotif(key, ipn.Ds_MerchantParameters);

            // Check if signature received is the same than signature notificacion previously calculated
            if (signatureNotif != ipn.Ds_Signature)
            {
                _logger.Error("TPV SERMEPA: Clave incorrecta. Las claves enviada y generada no coinciden: " + ipn.Ds_Signature + " != " + signatureNotif);

                return RedirectToAction("Index", "Home", new { area = "" });
            }

            //ID de Pedido
            var orderId = redsysApi.GetParameter("Ds_Order");

            //Pedido
            var order = _orderService.GetOrderById(Convert.ToInt32(orderId));
            if (order == null)
                throw new NopException($"El pedido de ID {orderId} no existe");

            //Actualizamos el pedido
            var dsResponse = Convert.ToInt32(redsysApi.GetParameter("Ds_Response"));
            if (dsResponse > -1 && dsResponse < 100)
            {
                //Lo marcamos como pagado
                if (_orderProcessingService.CanMarkOrderAsPaid(order))
                {
                    _orderProcessingService.MarkOrderAsPaid(order);
                }

                //order note
                _orderService.InsertOrderNote(new OrderNote
                {
                    OrderId = order.Id,
                    Note = "Información del pago: " + decodedMerchantParameters,
                    DisplayToCustomer = false,
                    CreatedOnUtc = DateTime.UtcNow
                });

                return RedirectToRoute("CheckoutCompleted", new { orderId = order.Id });
            }

            _logger.Error("TPV SERMEPA: Pago no autorizado con ERROR: " + dsResponse);

            //order note
            _orderService.InsertOrderNote(new OrderNote
            {
                OrderId = order.Id,
                Note = "!!! PAGO DENEGADO !!! " + decodedMerchantParameters,
                DisplayToCustomer = false,
                CreatedOnUtc = DateTime.UtcNow
            });

            return RedirectToAction("Index", "Home", new { area = "" });
        }

        public IActionResult Error()
        {
            if (!(_paymentPluginManager.LoadPluginBySystemName("Payments.Sermepa") is SermepaPaymentProcessor processor) || !_paymentPluginManager.IsPluginActive(processor))
                throw new NopException("Sermepa module cannot be loaded");

            return RedirectToAction("Index", "Home", new { area = "" });
        }
    }
}