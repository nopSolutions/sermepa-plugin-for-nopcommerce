using System.Web.Mvc;
using System.Web.Routing;
using Nop.Web.Framework.Mvc.Routes;

namespace Nop.Plugin.Payments.Sermepa
{
    public partial class RouteProvider : IRouteProvider
    {
        public void RegisterRoutes(RouteCollection routes)
        {
            //Return
            routes.MapRoute("Plugin.Payments.Sermepa.Return",
                 "Plugins/PaymentSermepa/Return",
                 new { controller = "PaymentSermepa", action = "Return" },
                 new[] { "Nop.Plugin.Payments.Sermepa.Controllers" }
            );

            //Error
            routes.MapRoute("Plugin.Payments.Sermepa.Error",
                 "Plugins/PaymentSermepa/Error",
                 new { controller = "PaymentSermepa", action = "Error" },
                 new[] { "Nop.Plugin.Payments.Sermepa.Controllers" }
            );
        }
        public int Priority
        {
            get
            {
                return 0;
            }
        }
    }
}
