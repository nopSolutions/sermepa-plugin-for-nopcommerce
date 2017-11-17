using Microsoft.AspNetCore.Mvc;
using Nop.Web.Framework.Components;

namespace Nop.Plugin.Payments.Sermepa.Components
{
    [ViewComponent(Name = "PaymentSermepa")]
    public class PaymentSermepaViewComponent : NopViewComponent
    {
        public IViewComponentResult Invoke()
        {
            return View("~/Plugins/Payments.Sermepa/Views/PaymentInfo.cshtml");
        }
    }
}
