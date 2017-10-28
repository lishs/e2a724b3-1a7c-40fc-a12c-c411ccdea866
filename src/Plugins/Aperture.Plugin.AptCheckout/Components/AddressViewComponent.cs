using Microsoft.AspNetCore.Mvc;
using Nop.Web.Framework.Components;

namespace Aperture.Plugin.AptCheckout.Components
{
    public class AddressViewComponent : NopViewComponent
    {
        public IViewComponentResult Invoke()
        {

            return View("~/Plugins/Aperture.AptCheckout/Views/Partials/_address.cshtml");
        }
    }
}