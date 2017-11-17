//Contributor: Noel Revuelta
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;
using Nop.Core;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Payments;
using Nop.Core.Plugins;
using Nop.Plugin.Payments.Sermepa.Controllers;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Payments;
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
            //Notificación On-Line
            var strDs_Merchant_MerchantURL = _webHelper.GetStoreLocation(false) + "Plugins/PaymentSermepa/Return";

            //URL OK
            var strDs_Merchant_UrlOK = _webHelper.GetStoreLocation(false) + "checkout/completed";

            //URL KO
            var strDs_Merchant_UrlKO = _webHelper.GetStoreLocation(false) + "Plugins/PaymentSermepa/Error";

            //Numero de pedido
            //You have to change the id of the orders table to begin with a number of at least 4 digits.
            var strDs_Merchant_Order = postProcessPaymentRequest.Order.Id.ToString("0000");

            //Nombre del comercio
            var strDs_Merchant_MerchantName = _sermepaPaymentSettings.NombreComercio;

            //Importe
            var amount = ((int)Convert.ToInt64(postProcessPaymentRequest.Order.OrderTotal * 100)).ToString();
            var strDs_Merchant_Amount = amount;

            //Código de comercio
            var strDs_Merchant_MerchantCode = _sermepaPaymentSettings.FUC;

            //Moneda
            var strDs_Merchant_Currency = _sermepaPaymentSettings.Moneda;

            //Terminal
            var strDs_Merchant_Terminal = _sermepaPaymentSettings.Terminal;

            //Tipo de transaccion (0 - Autorización)
            var strDs_Merchant_TransactionType = "0";

            //Clave
            var clave = _sermepaPaymentSettings.Pruebas ? _sermepaPaymentSettings.ClavePruebas : _sermepaPaymentSettings.ClaveReal;

            //Calculo de la firma
            var sha = $"{strDs_Merchant_Amount}{strDs_Merchant_Order}{strDs_Merchant_MerchantCode}{strDs_Merchant_Currency}{strDs_Merchant_TransactionType}{strDs_Merchant_MerchantURL}{clave}";

            SHA1 shaM = new SHA1Managed();
            var shaResult = shaM.ComputeHash(Encoding.Default.GetBytes(sha));
            var shaResultStr = BitConverter.ToString(shaResult).Replace("-", "");

            //Creamos el POST
            var remotePostHelper = new RemotePost
            {
                FormName = "form1",
                Url = GetSermepaUrl()
            };

            remotePostHelper.Add("Ds_Merchant_Amount", strDs_Merchant_Amount);
            remotePostHelper.Add("Ds_Merchant_Currency", strDs_Merchant_Currency);
            remotePostHelper.Add("Ds_Merchant_Order", strDs_Merchant_Order);
            remotePostHelper.Add("Ds_Merchant_MerchantCode", strDs_Merchant_MerchantCode);
            remotePostHelper.Add("Ds_Merchant_TransactionType", strDs_Merchant_TransactionType);
            remotePostHelper.Add("Ds_Merchant_MerchantURL", strDs_Merchant_MerchantURL);
            remotePostHelper.Add("Ds_Merchant_MerchantSignature", shaResultStr);
            remotePostHelper.Add("Ds_Merchant_Terminal", strDs_Merchant_Terminal);
            remotePostHelper.Add("Ds_Merchant_MerchantName", strDs_Merchant_MerchantName);
            remotePostHelper.Add("Ds_Merchant_UrlOK", strDs_Merchant_UrlOK);
            remotePostHelper.Add("Ds_Merchant_UrlKO", strDs_Merchant_UrlKO);

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

        public void GetPublicViewComponent(out string viewComponentName)
        {
            viewComponentName = "PaymentSermepa";
        }

        public Type GetControllerType()
        {
            return typeof(PaymentSermepaController);
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

            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.Sermepa.NombreComercio", "Nombre del comercio");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.Sermepa.Titular", "Nombre y Apellidos del titular");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.Sermepa.Producto", "Descripción del producto");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.Sermepa.FUC", "FUC comercio");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.Sermepa.Terminal", "Terminal");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.Sermepa.Moneda", "Moneda");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.Sermepa.ClaveReal", "Clave Real");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.Sermepa.ClavePruebas", "Clave Pruebas");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.Sermepa.Pruebas", "En pruebas");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.Sermepa.AdditionalFee", "Additional fee"); 
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.Sermepa.RedirectionTip", "You will be redirected to Sermepa site to complete the order.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.Sermepa.PaymentMethodDescription", "You will be redirected to Sermepa site to complete the order.");

            base.Install();
        }

        public override void Uninstall()
        {
            this.DeletePluginLocaleResource("Plugins.Payments.Sermepa.NombreComercio");
            this.DeletePluginLocaleResource("Plugins.Payments.Sermepa.Titular");
            this.DeletePluginLocaleResource("Plugins.Payments.Sermepa.Producto");
            this.DeletePluginLocaleResource("Plugins.Payments.Sermepa.FUC");
            this.DeletePluginLocaleResource("Plugins.Payments.Sermepa.Terminal");
            this.DeletePluginLocaleResource("Plugins.Payments.Sermepa.Moneda");
            this.DeletePluginLocaleResource("Plugins.Payments.Sermepa.ClaveReal");
            this.DeletePluginLocaleResource("Plugins.Payments.Sermepa.ClavePruebas");
            this.DeletePluginLocaleResource("Plugins.Payments.Sermepa.Pruebas");
            this.DeletePluginLocaleResource("Plugins.Payments.Sermepa.AdditionalFee");
            this.DeletePluginLocaleResource("Plugins.Payments.Sermepa.RedirectionTip");
            this.DeletePluginLocaleResource("Plugins.Payments.Sermepa.PaymentMethodDescription");

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
