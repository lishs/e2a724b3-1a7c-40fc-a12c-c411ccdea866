using Nop.Web.Framework.Mvc.Models;
using Nop.Web.Models.Checkout;

namespace Aperture.Plugin.AptCheckout.Models
{
    public class AptCheckoutModel : OnePageCheckoutModel
    {
        public CheckoutShippingAddressModel ShippingAddress { get; set; }
    }
}
