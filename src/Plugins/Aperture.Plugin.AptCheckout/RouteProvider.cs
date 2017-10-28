using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Nop.Web.Framework.Mvc.Routing;

namespace Aperture.Plugin.AptCheckout
{
    public class RouteProvider : IRouteProvider
    {
        public void RegisterRoutes(IRouteBuilder routeBuilder)
        {
         //   routeBuilder.Map
            routeBuilder.MapRoute("Aperture.Plugin.AptCheckout", "onepagecheckout/",
                new { controller = "AptCheckout", action = "Checkout" });
        }

        public int Priority => int.MaxValue;
    }
}
