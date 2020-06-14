using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Nop.Web.Framework.Mvc.Routing;

namespace Nop.Plugin.Payments.Sermepa
{
    public partial class RouteProvider : IRouteProvider
    {
        public void RegisterRoutes(IEndpointRouteBuilder endpointRouteBuilder)
        {
            //Return
            endpointRouteBuilder.MapControllerRoute("Plugin.Payments.Sermepa.Return", "Plugins/PaymentSermepa/Return",
                 new { controller = "PaymentSermepa", action = "Return" });

            //Error
            endpointRouteBuilder.MapControllerRoute("Plugin.Payments.Sermepa.Error", "Plugins/PaymentSermepa/Error",
                 new { controller = "PaymentSermepa", action = "Error" });
        }

        public int Priority => -1;
    }
}
