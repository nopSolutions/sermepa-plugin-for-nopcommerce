using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Nop.Core;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Payments;
using Nop.Plugin.Payments.Sermepa.Redsys;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Payments;
using Nop.Services.Plugins;
using Nop.Web.Framework;

namespace Nop.Plugin.Payments.Sermepa
{
    /// <summary>
    /// Sermepa payment processor
    /// </summary>
    public class SermepaPaymentProcessor : BasePlugin, IPaymentMethod
    {
        #region Fields

        private readonly SermepaPaymentSettings _sermepaPaymentSettings;
        private readonly ISettingService _settingService;
        private readonly IWebHelper _webHelper;
        private readonly ILocalizationService _localizationService;

        #endregion

        #region Ctor

        public SermepaPaymentProcessor(SermepaPaymentSettings sermepaPaymentSettings,
            ISettingService settingService, IWebHelper webHelper,
            ILocalizationService localizationService)
        {
            this._sermepaPaymentSettings = sermepaPaymentSettings;
            this._settingService = settingService;
            this._webHelper = webHelper;
            this._localizationService = localizationService;
        }

        #endregion

        #region Utilities

        /// <summary>
        /// Gets Sermepa URL
        /// </summary>
        /// <returns></returns>
        private string GetSermepaUrl()
        {
            return _sermepaPaymentSettings.Pruebas ? "https://sis-t.redsys.es:25443/sis/realizarPago" :
                "https://sis.redsys.es/sis/realizarPago";
        }

        #endregion

        #region Methods

        /// <summary>
        /// Process a payment
        /// </summary>
        /// <param name="processPaymentRequest">Payment info required for an order processing</param>
        /// <returns>Process payment result</returns>
        public ProcessPaymentResult ProcessPayment(ProcessPaymentRequest processPaymentRequest)
        {
            var result = new ProcessPaymentResult { NewPaymentStatus = PaymentStatus.Pending };
            return result;
        }

        /// <summary>
        /// Post process payment (used by payment gateways that require redirecting to a third-party URL)
        /// </summary>
        /// <param name="postProcessPaymentRequest">Payment info required for an order processing</param>
        public void PostProcessPayment(PostProcessPaymentRequest postProcessPaymentRequest)
        {
            var ds_Merchant_Amount = ((int)Convert.ToInt64(postProcessPaymentRequest.Order.OrderTotal * 100)).ToString();
            // You have to change the id of the orders table to begin with a number of at least 4 digits.
            var ds_Merchant_Order = postProcessPaymentRequest.Order.Id.ToString("0000");
            var ds_Merchant_MerchantCode = _sermepaPaymentSettings.FUC;
            var ds_Merchant_Currency = _sermepaPaymentSettings.Moneda;
            var ds_Merchant_TransactionType = "0";
            var ds_Merchant_Terminal = _sermepaPaymentSettings.Terminal;
            var ds_Merchant_MerchantURL = _webHelper.GetStoreLocation(false) + "Plugins/PaymentSermepa/Return";
            var ds_Merchant_UrlOK = _webHelper.GetStoreLocation(false) + "checkout/completed";
            var ds_Merchant_UrlKO = _webHelper.GetStoreLocation(false) + "Plugins/PaymentSermepa/Error";

            var key = _sermepaPaymentSettings.Pruebas ? _sermepaPaymentSettings.ClavePruebas : _sermepaPaymentSettings.ClaveReal;

            // New instance of RedysAPI
            var redsysApi = new RedsysAPI();

            // Main Key 
            // Fill Ds_MerchantParameters parameters
            redsysApi.SetParameter("DS_MERCHANT_AMOUNT", ds_Merchant_Amount);
            redsysApi.SetParameter("DS_MERCHANT_ORDER", ds_Merchant_Order);
            redsysApi.SetParameter("DS_MERCHANT_MERCHANTCODE", ds_Merchant_MerchantCode);
            redsysApi.SetParameter("DS_MERCHANT_CURRENCY", ds_Merchant_Currency);
            redsysApi.SetParameter("DS_MERCHANT_TRANSACTIONTYPE", ds_Merchant_TransactionType);
            redsysApi.SetParameter("DS_MERCHANT_TERMINAL", ds_Merchant_Terminal);
            redsysApi.SetParameter("DS_MERCHANT_MERCHANTURL", ds_Merchant_MerchantURL);
            redsysApi.SetParameter("DS_MERCHANT_URLOK", ds_Merchant_UrlOK);
            redsysApi.SetParameter("DS_MERCHANT_URLKO", ds_Merchant_UrlKO);

            // Calculate Ds_MerchantParameters
            var ds_MerchantParameters = redsysApi.createMerchantParameters();

            // Calculate Ds_Signature
            var ds_Signature = redsysApi.createMerchantSignature(key);

            var remotePostHelper = new RemotePost
            {
                FormName = "form1",
                Url = GetSermepaUrl()
            };

            remotePostHelper.Add("Ds_SignatureVersion", "HMAC_SHA256_V1");
            remotePostHelper.Add("Ds_MerchantParameters", ds_MerchantParameters);
            remotePostHelper.Add("Ds_Signature", ds_Signature);

            remotePostHelper.Post();
        }

        /// <summary>
        /// Returns a value indicating whether payment method should be hidden during checkout
        /// </summary>
        /// <param name="cart">Shoping cart</param>
        /// <returns>true - hide; false - display.</returns>
        public bool HidePaymentMethod(IList<ShoppingCartItem> cart)
        {
            //you can put any logic here
            //for example, hide this payment method if all products in the cart are downloadable
            //or hide this payment method if current customer is from certain country
            return false;
        }

        /// <summary>
        /// Gets additional handling fee
        /// </summary>
        /// <param name="cart">Shoping cart</param>
        /// <returns>Additional handling fee</returns>
        public decimal GetAdditionalHandlingFee(IList<ShoppingCartItem> cart)
        {
            return _sermepaPaymentSettings.AdditionalFee;
        }

        /// <summary>
        /// Captures payment
        /// </summary>
        /// <param name="capturePaymentRequest">Capture payment request</param>
        /// <returns>Capture payment result</returns>
        public CapturePaymentResult Capture(CapturePaymentRequest capturePaymentRequest)
        {
            var result = new CapturePaymentResult();
            result.AddError("Capture method not supported");
            return result;
        }

        /// <summary>
        /// Refunds a payment
        /// </summary>
        /// <param name="refundPaymentRequest">Request</param>
        /// <returns>Result</returns>
        public RefundPaymentResult Refund(RefundPaymentRequest refundPaymentRequest)
        {
            var result = new RefundPaymentResult();
            result.AddError("Refund method not supported");
            return result;
        }

        /// <summary>
        /// Voids a payment
        /// </summary>
        /// <param name="voidPaymentRequest">Request</param>
        /// <returns>Result</returns>
        public VoidPaymentResult Void(VoidPaymentRequest voidPaymentRequest)
        {
            var result = new VoidPaymentResult();
            result.AddError("Void method not supported");
            return result;
        }

        /// <summary>
        /// Process recurring payment
        /// </summary>
        /// <param name="processPaymentRequest">Payment info required for an order processing</param>
        /// <returns>Process payment result</returns>
        public ProcessPaymentResult ProcessRecurringPayment(ProcessPaymentRequest processPaymentRequest)
        {
            var result = new ProcessPaymentResult();
            result.AddError("Recurring payment not supported");
            return result;
        }

        /// <summary>
        /// Cancels a recurring payment
        /// </summary>
        /// <param name="cancelPaymentRequest">Request</param>
        /// <returns>Result</returns>
        public CancelRecurringPaymentResult CancelRecurringPayment(CancelRecurringPaymentRequest cancelPaymentRequest)
        {
            var result = new CancelRecurringPaymentResult();
            result.AddError("Recurring payment not supported");
            return result;
        }

        /// <summary>
        /// Gets a value indicating whether customers can complete a payment after order is placed but not completed (for redirection payment methods)
        /// </summary>
        /// <param name="order">Order</param>
        /// <returns>Result</returns>
        public bool CanRePostProcessPayment(Order order)
        {
            if (order == null)
                throw new ArgumentNullException(nameof(order));

            return false;
        }

        public IList<string> ValidatePaymentForm(IFormCollection form)
        {
            var warnings = new List<string>();
            return warnings;
        }


        public ProcessPaymentRequest GetPaymentInfo(IFormCollection form)
        {
            var paymentInfo = new ProcessPaymentRequest();
            return paymentInfo;
        }

        public override string GetConfigurationPageUrl()
        {
            return $"{_webHelper.GetStoreLocation()}Admin/PaymentSermepa/Configure";
        }

        public string GetPublicViewComponentName()
        {
            return "PaymentSermepa";
        }

        public override void Install()
        {
            var settings = new SermepaPaymentSettings
            {
                NombreComercio = "",
                Titular = "",
                Producto = "",
                FUC = "",
                Terminal = "",
                Moneda = "",
                ClaveReal = "",
                ClavePruebas = "",
                Pruebas = true,
                AdditionalFee = 0,
            };
            _settingService.SaveSetting(settings);

            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payments.Sermepa.NombreComercio", "Nombre del comercio");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payments.Sermepa.Titular", "Nombre y Apellidos del titular");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payments.Sermepa.Producto", "Descripción del producto");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payments.Sermepa.FUC", "FUC comercio");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payments.Sermepa.Terminal", "Terminal");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payments.Sermepa.Moneda", "Moneda");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payments.Sermepa.ClaveReal", "Clave Real");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payments.Sermepa.ClavePruebas", "Clave Pruebas");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payments.Sermepa.Pruebas", "En pruebas");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payments.Sermepa.AdditionalFee", "Additional fee"); 
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payments.Sermepa.RedirectionTip", "You will be redirected to Sermepa site to complete the order.");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payments.Sermepa.PaymentMethodDescription", "You will be redirected to Sermepa site to complete the order.");

            base.Install();
        }

        public override void Uninstall()
        {
            //settings
            _settingService.DeleteSetting<SermepaPaymentSettings>();

            _localizationService.DeletePluginLocaleResource("Plugins.Payments.Sermepa.NombreComercio");
            _localizationService.DeletePluginLocaleResource("Plugins.Payments.Sermepa.Titular");
            _localizationService.DeletePluginLocaleResource("Plugins.Payments.Sermepa.Producto");
            _localizationService.DeletePluginLocaleResource("Plugins.Payments.Sermepa.FUC");
            _localizationService.DeletePluginLocaleResource("Plugins.Payments.Sermepa.Terminal");
            _localizationService.DeletePluginLocaleResource("Plugins.Payments.Sermepa.Moneda");
            _localizationService.DeletePluginLocaleResource("Plugins.Payments.Sermepa.ClaveReal");
            _localizationService.DeletePluginLocaleResource("Plugins.Payments.Sermepa.ClavePruebas");
            _localizationService.DeletePluginLocaleResource("Plugins.Payments.Sermepa.Pruebas");
            _localizationService.DeletePluginLocaleResource("Plugins.Payments.Sermepa.AdditionalFee");
            _localizationService.DeletePluginLocaleResource("Plugins.Payments.Sermepa.RedirectionTip");
            _localizationService.DeletePluginLocaleResource("Plugins.Payments.Sermepa.PaymentMethodDescription");

            base.Uninstall();
        }

        #endregion

        #region Properies

        /// <summary>
        /// Gets a value indicating whether capture is supported
        /// </summary>
        public bool SupportCapture
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Gets a value indicating whether partial refund is supported
        /// </summary>
        public bool SupportPartiallyRefund
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Gets a value indicating whether refund is supported
        /// </summary>
        public bool SupportRefund
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Gets a value indicating whether void is supported
        /// </summary>
        public bool SupportVoid
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Gets a recurring payment type of payment method
        /// </summary>
        public RecurringPaymentType RecurringPaymentType
        {
            get
            {
                return RecurringPaymentType.NotSupported;
            }
        }

        /// <summary>
        /// Gets a payment method type
        /// </summary>
        public PaymentMethodType PaymentMethodType
        {
            get
            {
                return PaymentMethodType.Redirection;
            }
        }

        /// <summary>
        /// Gets a value indicating whether we should display a payment information page for this plugin
        /// </summary>
        public bool SkipPaymentInfo
        {
            get { return false; }
        }

        /// <summary>
        /// Gets a payment method description that will be displayed on checkout pages in the public store
        /// </summary>
        public string PaymentMethodDescription
        {
            get { return _localizationService.GetResource("Plugins.Payments.Sermepa.PaymentMethodDescription"); }
        }

        #endregion
    }
}
