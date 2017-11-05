

(function (aptCheckoutMgr, $, undefined) {

    //fields ////////////////////////////////
    var self = this;
    var globalEntries = {};

    self.isRunning = false;

    //private functions ///////////////////
    var isMobile = function () {
        var check = false;
        (function (a) { if (/(android|bb\d+|meego).+mobile|avantgo|bada\/|blackberry|blazer|compal|elaine|fennec|hiptop|iemobile|ip(hone|od)|iris|kindle|lge |maemo|midp|mmp|mobile.+firefox|netfront|opera m(ob|in)i|palm( os)?|phone|p(ixi|re)\/|plucker|pocket|psp|series(4|6)0|symbian|treo|up\.(browser|link)|vodafone|wap|windows ce|xda|xiino/i.test(a) || /1207|6310|6590|3gso|4thp|50[1-6]i|770s|802s|a wa|abac|ac(er|oo|s\-)|ai(ko|rn)|al(av|ca|co)|amoi|an(ex|ny|yw)|aptu|ar(ch|go)|as(te|us)|attw|au(di|\-m|r |s )|avan|be(ck|ll|nq)|bi(lb|rd)|bl(ac|az)|br(e|v)w|bumb|bw\-(n|u)|c55\/|capi|ccwa|cdm\-|cell|chtm|cldc|cmd\-|co(mp|nd)|craw|da(it|ll|ng)|dbte|dc\-s|devi|dica|dmob|do(c|p)o|ds(12|\-d)|el(49|ai)|em(l2|ul)|er(ic|k0)|esl8|ez([4-7]0|os|wa|ze)|fetc|fly(\-|_)|g1 u|g560|gene|gf\-5|g\-mo|go(\.w|od)|gr(ad|un)|haie|hcit|hd\-(m|p|t)|hei\-|hi(pt|ta)|hp( i|ip)|hs\-c|ht(c(\-| |_|a|g|p|s|t)|tp)|hu(aw|tc)|i\-(20|go|ma)|i230|iac( |\-|\/)|ibro|idea|ig01|ikom|im1k|inno|ipaq|iris|ja(t|v)a|jbro|jemu|jigs|kddi|keji|kgt( |\/)|klon|kpt |kwc\-|kyo(c|k)|le(no|xi)|lg( g|\/(k|l|u)|50|54|\-[a-w])|libw|lynx|m1\-w|m3ga|m50\/|ma(te|ui|xo)|mc(01|21|ca)|m\-cr|me(rc|ri)|mi(o8|oa|ts)|mmef|mo(01|02|bi|de|do|t(\-| |o|v)|zz)|mt(50|p1|v )|mwbp|mywa|n10[0-2]|n20[2-3]|n30(0|2)|n50(0|2|5)|n7(0(0|1)|10)|ne((c|m)\-|on|tf|wf|wg|wt)|nok(6|i)|nzph|o2im|op(ti|wv)|oran|owg1|p800|pan(a|d|t)|pdxg|pg(13|\-([1-8]|c))|phil|pire|pl(ay|uc)|pn\-2|po(ck|rt|se)|prox|psio|pt\-g|qa\-a|qc(07|12|21|32|60|\-[2-7]|i\-)|qtek|r380|r600|raks|rim9|ro(ve|zo)|s55\/|sa(ge|ma|mm|ms|ny|va)|sc(01|h\-|oo|p\-)|sdk\/|se(c(\-|0|1)|47|mc|nd|ri)|sgh\-|shar|sie(\-|m)|sk\-0|sl(45|id)|sm(al|ar|b3|it|t5)|so(ft|ny)|sp(01|h\-|v\-|v )|sy(01|mb)|t2(18|50)|t6(00|10|18)|ta(gt|lk)|tcl\-|tdg\-|tel(i|m)|tim\-|t\-mo|to(pl|sh)|ts(70|m\-|m3|m5)|tx\-9|up(\.b|g1|si)|utst|v400|v750|veri|vi(rg|te)|vk(40|5[0-3]|\-v)|vm40|voda|vulc|vx(52|53|60|61|70|80|81|83|85|98)|w3c(\-| )|webc|whit|wi(g |nc|nw)|wmlb|wonu|x700|yas\-|your|zeto|zte\-/i.test(a.substr(0, 4))) check = true; })(navigator.userAgent || navigator.vendor || window.opera);
        return check;
    };


    var busy = function (isRunning) {
        if (isRunning != undefined) {
            self.isRunning = isRunning === true;
        } else {
            return self.isRunning;
        }
    }

    function parseObj(obj) {
        var newObj = {};
        for (var i = 0; i < obj.length; ++i) {
            var curProp = obj[i];
            newObj[curProp.name] = curProp.value;
        }
        return newObj;
    }

    function bindEvents() {


        $(document).ajaxStart(function () {
            busy(true);

            $("._aptOrderTotal ._aptOrderSummaryValue").data('formervalue', $("._aptOrderTotal ._aptOrderSummaryValue").text());
            $("._aptOrderTotal ._aptOrderSummaryValue").text('--');
        });

        $(document).ajaxComplete(function (event, request, settings) {
            busy(false);

            if ($("._aptOrderTotal ._aptOrderSummaryValue").data('formervalue')) {
                $("._aptOrderTotal ._aptOrderSummaryValue").text($("._aptOrderTotal ._aptOrderSummaryValue").data('formervalue'));
            }
        });

        $(document).ajaxError(function(jqXhr) {
            console.log("checkout error:", jqXhr);
        });


        var $wrap = $("#_aptOrderSummary");

        var width = $wrap.width();
        $wrap.css("width", width + "px");

        var pos = $wrap.offset().top;
        if (!isMobile()) {
            $(document).on("scroll", // function (e) {
                $.debounce(5,
                    false,
                    function (e) {
                        var shouldUndock = $(window).scrollTop() > (pos - 20);
                        $wrap.toggleClass("orderTotalsFixed", shouldUndock);
                    }));

        };





        $(document).on('click', '._aptStoredAddress', function () {

            var $item = $(this);
            var addrId = $item.data('id');
            var isBilling = $item.data('isbilling');

            if ($item.hasClass("_aptSelected") || busy()) {
                return;
            }

            busy(true);

            $item.addClass('preselected');
          
            aptCheckoutMgr.selectStoredAddress({
                addrId: addrId,
                isBilling: isBilling
            }).done(function () {
                $item.closest('.storedAddresses').find('._aptSelected').removeClass('_aptSelected');
                $item.addClass('_aptSelected');
            }).fail(function () {

            }).always(function () {
                $('._aptStoredAddress').removeClass('preselected');
            });


        });

        $(document).on('click', '._aptOpenAddrModal', function (e) {
            var modalId = $(this).data('modalid');
            $("." + modalId).remodal().open(); //so modal class (if id won't work)
        });

        $(document).on('change', '._aptShippingMethod', function(e) {
            if (busy()) {
                e.preventDefault();
                return false;
            }
        });

        $(document).on('change', '._aptShippingMethod', function (e) {
            if (!$(this).prop("checked") || busy()) {
                return false;
            }

            var option = $(this).val();

            var $wrapper = $(this).closest("._aptShippingMethodWrap");
            var $item = $wrapper.find('.method-name');
            $item.addClass('preselected');

          //  $("#_aptOrderSummary").loading(true);
            aptCheckoutMgr.updateShippingMethod({
                shippingOption: option
            }).done(function (response) {
                if (response.osm) {
                    $("#_aptOrderSummary").html(response.osm);
                }


                $(".shipping-method").find('._aptSelected').removeClass("_aptSelected");
                $wrapper.find('.method-name').removeClass('preselected').addClass('_aptSelected');

            }).fail(function () {

                }).always(function () {
              //  $("#_aptOrderSummary").loading(false);
                $('.method-name').removeClass('preselected');
            });

        });

        $(document).on('click', '._aptPaymentMethod', function () {

       

            var $form = $(this).closest('form');
            var $wrapper = $(this).closest("._aptPaymentMethodWrap");

            var $item = $wrapper.find('.payment-logo').length
                ? $wrapper.find('.payment-logo')
                : $wrapper.find('.payment-method');
            
           $item.addClass('preselected');

            $.ajax({
                url: '/AptCheckout/PaymentMethod',
                data: $form.serialize(),
                type: 'post'
            }).done(function(response) {
                $(".payment-method").find('._aptSelected').removeClass("_aptSelected");
                $item.removeClass('preselected').addClass('_aptSelected');
                
                if (response.osm) {
                    $("#_aptOrderSummary").html(response.osm);
                }

                Checkout.setStepResponse(response);

            }).fail(function(xhr) {

            }).always(function(xhr) {

            });

        });

        $(document).on('click', "._aptAddrCommit", function (e) {

            var modalId = $(this).data('modalid');
            var $form = $("." + modalId).find('form');

            $form.validate();
            if (!$form.valid()) {
                console.log('invalid form');
                return false;
            }

            var addrObj = parseObj(JSON.parse(JSON.stringify($form.serializeArray()))); // store json object

            if (busy()) { return false; }

            aptCheckoutMgr.createOrUpdateAddress({
                formstr: $form.serialize(),
                address: addrObj,
                modalId: modalId
            }).done(function (response) {
                if (response.bam) {
                    $("#_aptStoredBillingAddresses").html(response.bam); //.find(".storedAddresses")
                }

                if (response.sam) {
                    $("#_aptStoredShippingAddresses").html(response.sam);
                }
                $("." + data.modalId).remodal().close();
            }).always(function () {
            });
        });
    }

    //////////////////////////////////


    //constructor
    aptCheckoutMgr.init = function () {
        bindEvents();
    }


    aptCheckoutMgr.selectStoredAddress = function (data) {

        data = data || {};

        return $.ajax({
            url: '/AptCheckout/Address',
            type: "POST",
            data: {
                addressId: data.addrId,
                isBilling: data.isBilling
            }
        });

    }

    aptCheckoutMgr.updateShippingMethod = function (data) {
        data = data || {};

        return $.ajax({
            type: "POST",
            url: "/AptCheckout/ShippingOption",
            data: { shippingoption: data.shippingOption },
            //  contentType: 'application/json'
        });
    }

    //TODO PASS IN PATH ON INIT
    aptCheckoutMgr.createOrUpdateAddress = function (data) {

        data = data || {};

        return $.ajax({
            type: "POST",
            url: "/AptCheckout/ManageAddress",
            dataType: 'json',
            data: { address: data.address, formstr: data.formstr },
            //  contentType: 'application/json'
        });

    }


    aptCheckoutMgr.globalEntries = function (key, value) {
        if (value != undefined) {
            globalEntries[key] = value;
            return true;
        }
        return globalEntries[key];
    };


    var loading = function (isLoading) {
        if (isLoading === true) {
            $("._aptOrderSummaryValue").data('formervalue', $("._aptOrderSummaryValue").text());
            $("._aptOrderSummaryValue").text('--');
        } else {
            if ($("._aptOrderSummaryValue").data('formervalue')) {
                $("._aptOrderSummaryValue").text($("._aptOrderSummaryValue").data('formervalue'));
            }
        }

    };


}(window.aptCheckoutMgr = window.aptCheckoutMgr || {}, jQuery));



