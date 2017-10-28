using Nop.Core.Configuration;

namespace Aperture.Plugin.AptCheckout
{
    /// <summary>
    /// Represents settings of manual payment plugin
    /// </summary>
    public class AptCheckoutSettings : ISettings
    {
         public int? DefaultCountryId { get; set; }
    }
}
