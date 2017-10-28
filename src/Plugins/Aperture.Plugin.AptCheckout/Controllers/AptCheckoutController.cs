using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using Aperture.Plugin.AptCheckout.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Primitives;
using Nop.Core;
using Nop.Core.Caching;
using Nop.Core.Domain.Common;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Payments;
using Nop.Core.Domain.Shipping;
using Nop.Core.Plugins;
using Nop.Services.Catalog;
using Nop.Services.Common;
using Nop.Services.Customers;
using Nop.Services.Directory;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Services.Orders;
using Nop.Services.Payments;
using Nop.Services.Shipping;
using Nop.Web.Controllers;
using Nop.Web.Extensions;
using Nop.Web.Factories;
using Nop.Web.Models.Checkout;
using Nop.Web.Models.Common;
using Nop.Web.Models.ShoppingCart;

namespace Aperture.Plugin.AptCheckout.Controllers
{
    public class AptCheckoutController : CheckoutController
    {
        private readonly ICheckoutModelFactory _checkoutModelFactory;
        private readonly IWorkContext _workContext;
        private readonly OrderSettings _orderSettings;
        private readonly IStoreContext _storeContext;
        private readonly IShoppingCartModelFactory _shoppingCartModelFactory;
        private readonly IAddressService _addressService;
        private readonly ICustomerService _customerService;
        private readonly IAddressAttributeParser _addressAttributeParser;
        private readonly IAddressAttributeService _addressAttributeService;
        private readonly IStaticCacheManager _staticCacheManager;
        private readonly AptCheckoutSettings _aptCheckoutSettings;

        public AptCheckoutController(IShoppingCartModelFactory shoppingCartModelFactory, ICheckoutModelFactory checkoutModelFactory, IWorkContext workContext, IStoreContext storeContext, IShoppingCartService shoppingCartService, ILocalizationService localizationService, IProductAttributeParser productAttributeParser, IProductService productService, IOrderProcessingService orderProcessingService, ICustomerService customerService, IGenericAttributeService genericAttributeService, ICountryService countryService, IStateProvinceService stateProvinceService, IShippingService shippingService, IPaymentService paymentService, IPluginFinder pluginFinder, ILogger logger, IOrderService orderService, IWebHelper webHelper, IAddressAttributeParser addressAttributeParser, IAddressAttributeService addressAttributeService, OrderSettings orderSettings, RewardPointsSettings rewardPointsSettings, PaymentSettings paymentSettings, ShippingSettings shippingSettings, AddressSettings addressSettings, CustomerSettings customerSettings, IStaticCacheManager staticCacheManager, IAddressService addressService, AptCheckoutSettings aptCheckoutSettings) : base(checkoutModelFactory, workContext, storeContext, shoppingCartService, localizationService, productAttributeParser, productService, orderProcessingService, customerService, genericAttributeService, countryService, stateProvinceService, shippingService, paymentService, pluginFinder, logger, orderService, webHelper, addressAttributeParser, addressAttributeService, orderSettings, rewardPointsSettings, paymentSettings, shippingSettings, addressSettings, customerSettings)
        {
            _checkoutModelFactory = checkoutModelFactory;
            _workContext = workContext;
            _storeContext = storeContext;
            _orderSettings = orderSettings;
            _staticCacheManager = staticCacheManager;
            _addressService = addressService;
            _aptCheckoutSettings = aptCheckoutSettings;
            _customerService = customerService;
            _addressAttributeParser = addressAttributeParser;
            _addressAttributeService = addressAttributeService;
            _shoppingCartModelFactory = shoppingCartModelFactory;
        }

        [HttpPost]
        public ActionResult OrderSummary()
        {
            //var model = new ShoppingCartModel();
            //model = _shoppingCartModelFactory.PrepareShoppingCartModel(model, cart,
            //    isEditable: false,
            //    prepareAndDisplayOrderReviewData: true);

            //return View(model);

            return null;
        }


        [ResponseCache(Location = ResponseCacheLocation.Any, Duration = int.MaxValue)]
        public ActionResult Checkout()
        {
                
            var cart = _workContext.CurrentCustomer.ShoppingCartItems
                .Where(sci => sci.ShoppingCartType == ShoppingCartType.ShoppingCart)
                .LimitPerStore(_storeContext.CurrentStore.Id)
                .ToList();

            if (!cart.Any())
                return RedirectToRoute("ShoppingCart");

            if (!_orderSettings.OnePageCheckoutEnabled)
                return RedirectToRoute("Checkout");

            if (_workContext.CurrentCustomer.IsGuest() && !_orderSettings.AnonymousCheckoutAllowed)
                return Challenge();

            var model = _checkoutModelFactory.PrepareOnePageCheckoutModel(cart);
            model.CustomProperties["ShippingAddress"] = _checkoutModelFactory.PrepareShippingAddressModel();

            var shoppingCartModel = new ShoppingCartModel();
            model.CustomProperties["OrderSummary"] = _shoppingCartModelFactory.PrepareOrderTotalsModel(cart, false);
           

            model.CustomProperties["ShoppingCart"] = _shoppingCartModelFactory.PrepareShoppingCartModel(shoppingCartModel, cart,
            isEditable: false,
            prepareAndDisplayOrderReviewData: true);
            
            return View("~/Plugins/Aperture.AptCheckout/Views/Checkout.cshtml", model);
        }


        [HttpPost]
        public ActionResult CreateOrUpdateAddress(AddressModel address, string formstr)
        {
            if (!ModelState.IsValid)
            {
                string errors = string.Join(", ", ModelState.Values
                    .SelectMany(x => x.Errors)
                    .Select(x => x.ErrorMessage));

                return this.Error(errors);
            }

            var customer = _workContext.CurrentCustomer;

            var nvc = HttpUtility.ParseQueryString(formstr).ToDictionary().ToDictionary(q=>q.Key, q=>new StringValues(q.Value));
            var form = new FormCollection(nvc);

            var customAttributes = form.ParseCustomAddressAttributes(_addressAttributeParser, _addressAttributeService);
            var customAttributeWarnings = _addressAttributeParser.GetAttributeWarnings(customAttributes);
            foreach (var error in customAttributeWarnings)
            {
                ModelState.AddModelError("", error);
            }

            if (address.Id > 0) //new addr
            {
                var existingAddress = _addressService.GetAddressById(address.Id);
                var updatedAddress = address.ToEntity(existingAddress);
                _addressService.UpdateAddress(updatedAddress);
            }
            else //existing addr
            {
                var newAddress = address.ToEntity();
                newAddress.CreatedOnUtc = DateTime.UtcNow;
                customer.Addresses.Add(newAddress);
                _customerService.UpdateCustomer(customer);
            }

            var cart = _workContext.CurrentCustomer.ShoppingCartItems
                .Where(sci => sci.ShoppingCartType == ShoppingCartType.ShoppingCart)
                .LimitPerStore(_storeContext.CurrentStore.Id)
                .ToList();

            var billingAddressModel = _checkoutModelFactory.PrepareBillingAddressModel(cart);
            var shippingAddressModel = _checkoutModelFactory.PrepareShippingAddressModel();
            
            var billingStoredAddressMarkup =
                RenderPartialViewToString("~/Plugins/Aperture.AptCheckout/Views/Partials/_billingStoredAddresses.cshtml", billingAddressModel);
            
            var shippingStoredAddressMarkup = RenderPartialViewToString(
                "~/Plugins/Aperture.AptCheckout/Views/Partials/_shippingStoredAddresses.cshtml",
                shippingAddressModel);
            
            return Json(new
            {
                sam = shippingStoredAddressMarkup,
                bam = billingStoredAddressMarkup
            });
        }

        
    }
}
