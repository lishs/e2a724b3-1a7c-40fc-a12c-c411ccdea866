using System;
using System.Collections.Generic;
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
using Nop.Core.Http.Extensions;

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
        private readonly IGenericAttributeService _genericAttributeService;
        private readonly IShoppingCartService _shoppingCartService;
        private readonly ILocalizationService _localizationService;
        private readonly IProductAttributeParser _productAttributeParser;
        private readonly IProductService _productService;
        private readonly IOrderProcessingService _orderProcessingService;
        private readonly ICountryService _countryService;
        private readonly IStateProvinceService _stateProvinceService;
        private readonly IShippingService _shippingService;
        private readonly IPaymentService _paymentService;
        private readonly IPluginFinder _pluginFinder;
        private readonly ILogger _logger;
        private readonly IOrderService _orderService;
        private readonly IWebHelper _webHelper;
        private readonly RewardPointsSettings _rewardPointsSettings;
        private readonly PaymentSettings _paymentSettings;
        private readonly ShippingSettings _shippingSettings;
        private readonly AddressSettings _addressSettings;
        private readonly CustomerSettings _customerSettings;

        public AptCheckoutController(IShoppingCartModelFactory shoppingCartModelFactory, ICheckoutModelFactory checkoutModelFactory, IWorkContext workContext, IStoreContext storeContext, IShoppingCartService shoppingCartService, ILocalizationService localizationService, IProductAttributeParser productAttributeParser, IProductService productService, IOrderProcessingService orderProcessingService, ICustomerService customerService, IGenericAttributeService genericAttributeService, ICountryService countryService, IStateProvinceService stateProvinceService, IShippingService shippingService, IPaymentService paymentService, IPluginFinder pluginFinder, ILogger logger, IOrderService orderService, IWebHelper webHelper, IAddressAttributeParser addressAttributeParser, IAddressAttributeService addressAttributeService, OrderSettings orderSettings, RewardPointsSettings rewardPointsSettings, PaymentSettings paymentSettings, ShippingSettings shippingSettings, AddressSettings addressSettings, CustomerSettings customerSettings, IStaticCacheManager staticCacheManager, IAddressService addressService, AptCheckoutSettings aptCheckoutSettings) : base(checkoutModelFactory, workContext, storeContext, shoppingCartService, localizationService, productAttributeParser, productService, orderProcessingService, customerService, genericAttributeService, countryService, stateProvinceService, shippingService, paymentService, pluginFinder, logger, orderService, webHelper, addressAttributeParser, addressAttributeService, orderSettings, rewardPointsSettings, paymentSettings, shippingSettings, addressSettings, customerSettings)
        {
            this._checkoutModelFactory = checkoutModelFactory;
            this._workContext = workContext;
            this._storeContext = storeContext;
            this._shoppingCartService = shoppingCartService;
            this._localizationService = localizationService;
            this._productAttributeParser = productAttributeParser;
            this._productService = productService;
            this._orderProcessingService = orderProcessingService;
            this._customerService = customerService;
            this._genericAttributeService = genericAttributeService;
            this._countryService = countryService;
            this._stateProvinceService = stateProvinceService;
            this._shippingService = shippingService;
            this._paymentService = paymentService;
            this._pluginFinder = pluginFinder;
            this._logger = logger;
            this._orderService = orderService;
            this._webHelper = webHelper;
            this._addressAttributeParser = addressAttributeParser;
            this._addressAttributeService = addressAttributeService;

            this._orderSettings = orderSettings;
            this._rewardPointsSettings = rewardPointsSettings;
            this._paymentSettings = paymentSettings;
            this._shippingSettings = shippingSettings;
            this._addressSettings = addressSettings;
            this._customerSettings = customerSettings;
            this._addressService = addressService;
            this._aptCheckoutSettings = aptCheckoutSettings;
            this._shoppingCartModelFactory = shoppingCartModelFactory;
            this._staticCacheManager = staticCacheManager;
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

        //protected void PopulateCheckoutModel()
        //{

        //}


        [ResponseCache(Location = ResponseCacheLocation.Any, Duration = int.MaxValue)]
        public ActionResult Checkout()
        {
            var customer = _workContext.CurrentCustomer;
            var cart = customer.ShoppingCartItems
                .Where(sci => sci.ShoppingCartType == ShoppingCartType.ShoppingCart)
                .LimitPerStore(_storeContext.CurrentStore.Id)
                .ToList();

            if (!cart.Any())
                return RedirectToRoute("ShoppingCart");

            if (!_orderSettings.OnePageCheckoutEnabled)
                return RedirectToRoute("Checkout");

            if (customer.IsGuest() && !_orderSettings.AnonymousCheckoutAllowed)
                return Challenge();

            var model = _checkoutModelFactory.PrepareOnePageCheckoutModel(cart);
            model.CustomProperties["ShippingAddress"] = _checkoutModelFactory.PrepareShippingAddressModel();
            model.CustomProperties["OrderSummary"] = _shoppingCartModelFactory.PrepareOrderTotalsModel(cart, false);

            var shoppingCartModel = new ShoppingCartModel();
            model.CustomProperties["ShoppingCart"] = _shoppingCartModelFactory.PrepareShoppingCartModel(shoppingCartModel, cart,
            isEditable: false,
            prepareAndDisplayOrderReviewData: true);

            model.CustomProperties["ShippingMethods"] =
                _checkoutModelFactory.PrepareShippingMethodModel(cart, customer.ShippingAddress);

            var paymentMethods = _checkoutModelFactory.PreparePaymentMethodModel(cart, _aptCheckoutSettings.DefaultCountryId ?? 1); ;
            model.CustomProperties["PaymentMethods"] = paymentMethods;

            var selectedPaymentMethodModel = paymentMethods.PaymentMethods.FirstOrDefault(q => q.Selected) ?? paymentMethods.PaymentMethods.FirstOrDefault();

            if (selectedPaymentMethodModel != null)
            {
                var selectedPaymentMethod =
                    _paymentService.LoadPaymentMethodBySystemName(selectedPaymentMethodModel.PaymentMethodSystemName);
                model.CustomProperties["PaymentInfo"] = _checkoutModelFactory.PreparePaymentInfoModel(selectedPaymentMethod);

            }


            return View("~/Plugins/Aperture.AptCheckout/Views/Checkout.cshtml", model);
        }



        [HttpPost, ActionName("ShippingOption")]
        public IActionResult SelectShippingOption(string shippingoption)
        {
            //validation
            var cart = _workContext.CurrentCustomer.ShoppingCartItems
                .Where(sci => sci.ShoppingCartType == ShoppingCartType.ShoppingCart)
                .LimitPerStore(_storeContext.CurrentStore.Id)
                .ToList();

            if (!cart.Any())
                return RedirectToRoute("ShoppingCart");

            if (_workContext.CurrentCustomer.IsGuest() && !_orderSettings.AnonymousCheckoutAllowed)
                return Challenge();

            if (!cart.RequiresShipping(_productService, _productAttributeParser))
            {
                _genericAttributeService.SaveAttribute<ShippingOption>(_workContext.CurrentCustomer,
                    SystemCustomerAttributeNames.SelectedShippingOption, null, _storeContext.CurrentStore.Id);
                return RedirectToRoute("CheckoutPaymentMethod");
            }

            //parse selected method 
            if (string.IsNullOrEmpty(shippingoption))
                return ShippingMethod();
            var splittedOption = shippingoption.Split(new[] { "___" }, StringSplitOptions.RemoveEmptyEntries);
            if (splittedOption.Length != 2)
                return ShippingMethod();
            var selectedName = splittedOption[0];
            var shippingRateComputationMethodSystemName = splittedOption[1];

            //find it
            //performance optimization. try cache first
            var shippingOptions = _workContext.CurrentCustomer.GetAttribute<List<ShippingOption>>(SystemCustomerAttributeNames.OfferedShippingOptions, _storeContext.CurrentStore.Id);
            if (shippingOptions == null || !shippingOptions.Any())
            {
                //not found? let's load them using shipping service
                shippingOptions = _shippingService.GetShippingOptions(cart, _workContext.CurrentCustomer.ShippingAddress,
                    _workContext.CurrentCustomer, shippingRateComputationMethodSystemName, _storeContext.CurrentStore.Id).ShippingOptions.ToList();
            }
            else
            {
                //loaded cached results. let's filter result by a chosen shipping rate computation method
                shippingOptions = shippingOptions.Where(so => so.ShippingRateComputationMethodSystemName.Equals(shippingRateComputationMethodSystemName, StringComparison.InvariantCultureIgnoreCase))
                    .ToList();
            }

            var shippingOption = shippingOptions
                .Find(so => !string.IsNullOrEmpty(so.Name) && so.Name.Equals(selectedName, StringComparison.InvariantCultureIgnoreCase));
            if (shippingOption == null)
            {
                return ShippingMethod();
            }

            //save
            _genericAttributeService.SaveAttribute(_workContext.CurrentCustomer, SystemCustomerAttributeNames.SelectedShippingOption, shippingOption, _storeContext.CurrentStore.Id);

            var orderTotalsModel = _shoppingCartModelFactory.PrepareOrderTotalsModel(cart, false);
            var orderSummaryMarkup = RenderPartialViewToString("~/Plugins/Aperture.AptCheckout/Views/Partials/_orderSummary.cshtml", orderTotalsModel);

            return Json(new
            {
                osm = orderSummaryMarkup
            });
        }




        [HttpPost, ActionName("Address")]
        public ActionResult SelectStoredAddress(int addressId, bool isBilling)
        {
            var customer = _workContext.CurrentCustomer;

            var cart = customer.ShoppingCartItems
                .Where(sci => sci.ShoppingCartType == ShoppingCartType.ShoppingCart)
                .LimitPerStore(_storeContext.CurrentStore.Id)
                .ToList();

            var address = customer.Addresses.FirstOrDefault(q => q.Id == addressId);
            if (address == null)
            {
                return this.Error("Cannot find address");
            }

            if (isBilling)
            {
                customer.BillingAddress = address;
            }
            else
            {
                customer.ShippingAddress = address;
            }

            _customerService.UpdateCustomer(customer);

            string shippingMethodMarkup = null;

            if (!isBilling)
            {
                var shippingMethods = _checkoutModelFactory.PrepareShippingMethodModel(cart, address);
                shippingMethodMarkup = RenderPartialViewToString("~/Plugins/Aperture.AptCheckout/Views/Partials/_shippingMethods.cshtml", shippingMethods);
            }

            var orderTotalsModel = _shoppingCartModelFactory.PrepareOrderTotalsModel(cart, false);
            var orderSummaryMarkup = RenderPartialViewToString("~/Plugins/Aperture.AptCheckout/Views/Partials/_orderSummary.cshtml", orderTotalsModel);

            return Json(new
            {
                success = true,
                smm = shippingMethodMarkup,
                osm = orderSummaryMarkup
            });

        }


        [HttpPost, ActionName("ManageAddress")]
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

            var nvc = HttpUtility.ParseQueryString(formstr).ToDictionary().ToDictionary(q => q.Key, q => new StringValues(q.Value));
            var form = new FormCollection(nvc);

            var customAttributes = form.ParseCustomAddressAttributes(_addressAttributeParser, _addressAttributeService);
            var customAttributeWarnings = _addressAttributeParser.GetAttributeWarnings(customAttributes);
            foreach (var error in customAttributeWarnings)
            {
                ModelState.AddModelError("", error);
            }

            Address addressEntity;
            if (address.Id > 0) //new addr
            {
                addressEntity = _addressService.GetAddressById(address.Id);
                addressEntity = address.ToEntity(addressEntity);
                _addressService.UpdateAddress(addressEntity);
            }
            else //existing addr
            {
                addressEntity = address.ToEntity();
                addressEntity.CreatedOnUtc = DateTime.UtcNow;

                customer.Addresses.Add(addressEntity);
                var isBilling = Convert.ToBoolean(form["IsBilling"]);
                if (isBilling)
                {
                    customer.BillingAddress = addressEntity;
                }
                else
                {
                    customer.ShippingAddress = addressEntity;
                }

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


            var shippingMethods = _checkoutModelFactory.PrepareShippingMethodModel(cart, addressEntity);
            var shippingMethodMarkup = RenderPartialViewToString("~/Plugins/Aperture.AptCheckout/Views/Partials/_shippingMethods.cshtml", shippingMethods);

            var orderTotalsModel = _shoppingCartModelFactory.PrepareOrderTotalsModel(cart, false);
            var orderSummaryMarkup = RenderPartialViewToString("~/Plugins/Aperture.AptCheckout/Views/Partials/_orderSummary.cshtml", orderTotalsModel);


            return Json(new
            {
                sam = shippingStoredAddressMarkup,
                bam = billingStoredAddressMarkup,
                smm = shippingMethodMarkup,
                osm = orderSummaryMarkup
            });
        }




        public virtual IActionResult SavePaymentMethod(string paymentmethod, CheckoutPaymentMethodModel model)
        {
            try
            {
                //validation
                var cart = _workContext.CurrentCustomer.ShoppingCartItems
                    .Where(sci => sci.ShoppingCartType == ShoppingCartType.ShoppingCart)
                    .LimitPerStore(_storeContext.CurrentStore.Id)
                    .ToList();
                if (!cart.Any())
                    throw new Exception("Your cart is empty");

                if (!_orderSettings.OnePageCheckoutEnabled)
                    throw new Exception("One page checkout is disabled");

                if (_workContext.CurrentCustomer.IsGuest() && !_orderSettings.AnonymousCheckoutAllowed)
                    throw new Exception("Anonymous checkout is not allowed");

                //payment method 
                if (string.IsNullOrEmpty(paymentmethod))
                    throw new Exception("Selected payment method can't be parsed");

                //reward points
                if (_rewardPointsSettings.Enabled)
                {
                    _genericAttributeService.SaveAttribute(_workContext.CurrentCustomer,
                        SystemCustomerAttributeNames.UseRewardPointsDuringCheckout, model.UseRewardPoints,
                        _storeContext.CurrentStore.Id);
                }

                //Check whether payment workflow is required
                var isPaymentWorkflowRequired = _orderProcessingService.IsPaymentWorkflowRequired(cart);
                if (!isPaymentWorkflowRequired)
                {
                    //payment is not required
                    _genericAttributeService.SaveAttribute<string>(_workContext.CurrentCustomer,
                        SystemCustomerAttributeNames.SelectedPaymentMethod, null, _storeContext.CurrentStore.Id);

                    var confirmOrderModel = _checkoutModelFactory.PrepareConfirmOrderModel(cart);
                    return Json(new
                    {
                        update_section = new UpdateSectionJsonModel
                        {
                            name = "confirm-order",
                            html = RenderPartialViewToString("OpcConfirmOrder", confirmOrderModel)
                        },
                        goto_section = "confirm_order"
                    });
                }

                var paymentMethodInst = _paymentService.LoadPaymentMethodBySystemName(paymentmethod);
                if (paymentMethodInst == null ||
                    !paymentMethodInst.IsPaymentMethodActive(_paymentSettings) ||
                    !_pluginFinder.AuthenticateStore(paymentMethodInst.PluginDescriptor, _storeContext.CurrentStore.Id) ||
                    !_pluginFinder.AuthorizedForUser(paymentMethodInst.PluginDescriptor, _workContext.CurrentCustomer))
                    throw new Exception("Selected payment method can't be parsed");

                //save
                _genericAttributeService.SaveAttribute(_workContext.CurrentCustomer,
                    SystemCustomerAttributeNames.SelectedPaymentMethod, paymentmethod, _storeContext.CurrentStore.Id);

                return OpcLoadStepAfterPaymentMethod(paymentMethodInst, cart);
            }
            catch (Exception exc)
            {
                _logger.Warning(exc.Message, exc, _workContext.CurrentCustomer);
                return Json(new { error = 1, message = exc.Message });
            }
        }

        public virtual IActionResult SavePaymentInfo(IFormCollection form)
        {
            try
            {
                //validation
                var cart = _workContext.CurrentCustomer.ShoppingCartItems
                    .Where(sci => sci.ShoppingCartType == ShoppingCartType.ShoppingCart)
                    .LimitPerStore(_storeContext.CurrentStore.Id)
                    .ToList();
                if (!cart.Any())
                    throw new Exception("Your cart is empty");

                if (!_orderSettings.OnePageCheckoutEnabled)
                    throw new Exception("One page checkout is disabled");

                if (_workContext.CurrentCustomer.IsGuest() && !_orderSettings.AnonymousCheckoutAllowed)
                    throw new Exception("Anonymous checkout is not allowed");

                var paymentMethodSystemName = _workContext.CurrentCustomer
                    .GetAttribute<string>(SystemCustomerAttributeNames.SelectedPaymentMethod, _genericAttributeService, _storeContext.CurrentStore.Id);
                var paymentMethod = _paymentService.LoadPaymentMethodBySystemName(paymentMethodSystemName);
                if (paymentMethod == null)
                    throw new Exception("Payment method is not selected");

                var warnings = paymentMethod.ValidatePaymentForm(form);
                foreach (var warning in warnings)
                    ModelState.AddModelError("", warning);
                if (ModelState.IsValid)
                {
                    //get payment info
                    var paymentInfo = paymentMethod.GetPaymentInfo(form);

                    //session save
                    HttpContext.Session.Set("OrderPaymentInfo", paymentInfo);

                    var confirmOrderModel = _checkoutModelFactory.PrepareConfirmOrderModel(cart);
                    return Json(new
                    {
                        update_section = new UpdateSectionJsonModel
                        {
                            name = "confirm-order",
                            html = RenderPartialViewToString("OpcConfirmOrder", confirmOrderModel)
                        },
                        goto_section = "confirm_order"
                    });
                }

                //If we got this far, something failed, redisplay form
                var paymenInfoModel = _checkoutModelFactory.PreparePaymentInfoModel(paymentMethod);
                return Json(new
                {
                    update_section = new UpdateSectionJsonModel
                    {
                        name = "payment-info",
                        html = RenderPartialViewToString("OpcPaymentInfo", paymenInfoModel)
                    }
                });
            }
            catch (Exception exc)
            {
                _logger.Warning(exc.Message, exc, _workContext.CurrentCustomer);
                return Json(new { error = 1, message = exc.Message });
            }
        }

        public virtual IActionResult OpcConfirmOrder()
        {
            try
            {
                //validation
                var cart = _workContext.CurrentCustomer.ShoppingCartItems
                    .Where(sci => sci.ShoppingCartType == ShoppingCartType.ShoppingCart)
                    .LimitPerStore(_storeContext.CurrentStore.Id)
                    .ToList();
                if (!cart.Any())
                    throw new Exception("Your cart is empty");

                if (!_orderSettings.OnePageCheckoutEnabled)
                    throw new Exception("One page checkout is disabled");

                if (_workContext.CurrentCustomer.IsGuest() && !_orderSettings.AnonymousCheckoutAllowed)
                    throw new Exception("Anonymous checkout is not allowed");

                //prevent 2 orders being placed within an X seconds time frame
                if (!IsMinimumOrderPlacementIntervalValid(_workContext.CurrentCustomer))
                    throw new Exception(_localizationService.GetResource("Checkout.MinOrderPlacementInterval"));

                //place order
                var processPaymentRequest = HttpContext.Session.Get<ProcessPaymentRequest>("OrderPaymentInfo");
                if (processPaymentRequest == null)
                {
                    //Check whether payment workflow is required
                    if (_orderProcessingService.IsPaymentWorkflowRequired(cart))
                    {
                        throw new Exception("Payment information is not entered");
                    }

                    processPaymentRequest = new ProcessPaymentRequest();
                }

                processPaymentRequest.StoreId = _storeContext.CurrentStore.Id;
                processPaymentRequest.CustomerId = _workContext.CurrentCustomer.Id;
                processPaymentRequest.PaymentMethodSystemName = _workContext.CurrentCustomer.GetAttribute<string>(
                    SystemCustomerAttributeNames.SelectedPaymentMethod,
                    _genericAttributeService, _storeContext.CurrentStore.Id);
                var placeOrderResult = _orderProcessingService.PlaceOrder(processPaymentRequest);
                if (placeOrderResult.Success)
                {
                    HttpContext.Session.Set<ProcessPaymentRequest>("OrderPaymentInfo", null);
                    var postProcessPaymentRequest = new PostProcessPaymentRequest
                    {
                        Order = placeOrderResult.PlacedOrder
                    };

                    var paymentMethod = _paymentService.LoadPaymentMethodBySystemName(placeOrderResult.PlacedOrder.PaymentMethodSystemName);
                    if (paymentMethod == null)
                        //payment method could be null if order total is 0
                        //success
                        return Json(new { success = 1 });

                    if (paymentMethod.PaymentMethodType == PaymentMethodType.Redirection)
                    {
                        //Redirection will not work because it's AJAX request.
                        //That's why we don't process it here (we redirect a user to another page where he'll be redirected)

                        //redirect
                        return Json(new
                        {
                            redirect = $"{_webHelper.GetStoreLocation()}checkout/OpcCompleteRedirectionPayment"
                        });
                    }

                    _paymentService.PostProcessPayment(postProcessPaymentRequest);
                    //success
                    return Json(new { success = 1 });
                }

                //error
                var confirmOrderModel = new CheckoutConfirmModel();
                foreach (var error in placeOrderResult.Errors)
                    confirmOrderModel.Warnings.Add(error);

                return Json(new
                {
                    update_section = new UpdateSectionJsonModel
                    {
                        name = "confirm-order",
                        html = RenderPartialViewToString("OpcConfirmOrder", confirmOrderModel)
                    },
                    goto_section = "confirm_order"
                });
            }
            catch (Exception exc)
            {
                _logger.Warning(exc.Message, exc, _workContext.CurrentCustomer);
                return Json(new { error = 1, message = exc.Message });
            }
        }

    }
}
