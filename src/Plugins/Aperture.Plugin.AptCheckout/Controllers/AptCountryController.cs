
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Nop.Core.Domain.Directory;
using Nop.Services.Directory;
using Nop.Services.Localization;
using Nop.Web.Controllers;
using Nop.Web.Factories;
using Nop.Web.Framework.Mvc.Filters;

namespace Aperture.Plugin.AptCheckout.Controllers
{
    public class AptCountryController : CountryController
    {
        private readonly ICountryService _countryService;
        private readonly IStateProvinceService _stateProvinceService;
        private readonly ILocalizationService _localizationService;
        private readonly ICountryModelFactory _countryModelFactory;

        public AptCountryController(ICountryModelFactory countryModelFactory, ICountryService countryService, IStateProvinceService stateProvinceService, ILocalizationService localizationService, ICountryModelFactory countryModelFactory1) : base(countryModelFactory)
        {
            _countryService = countryService;
            _stateProvinceService = stateProvinceService;
            _localizationService = localizationService;
            _countryModelFactory = countryModelFactory1;
        }

        //available even when navigation is not allowed
        [CheckAccessPublicStore(true)]
        public override IActionResult GetStatesByCountryId(string countryId, bool addSelectStateItem)
        {
            var model = _countryModelFactory.GetStatesByCountryId(countryId, addSelectStateItem);

            return Json(model);
        }


    }
}