using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Nop.Web.Framework.Mvc.Routing;

namespace Nop.Plugin.Payments.Sermepa
{
    public partial class RouteProvider : IRouteProvider
    {
        public void RegisterRoutes(IRouteBuilder routeBuilder)
        {
            //Return
            routeBuilder.MapRoute("Plugin.Payments.Sermepa.Return",
                 "Plugins/PaymentSermepa/Return",
                 new { controller = "PaymentSermepa", action = "Return" });

            //Error
            routeBuilder.MapRoute("Plugin.Payments.Sermepa.Error",
                 "Plugins/PaymentSermepa/Error",
                 new { controller = "PaymentSermepa", action = "Error" });
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
