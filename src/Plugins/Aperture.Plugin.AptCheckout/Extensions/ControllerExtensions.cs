using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Nop.Core.Infrastructure;

namespace Aperture.Plugin.AptCheckout.Extensions
{
    public static class ControllerExtensions
    {
        public static ActionResult Error(this Controller controller, string message, HttpContextBase httpContext = null)
        {
            httpContext = httpContext ?? EngineContext.Current.Resolve<HttpContextBase>();
            httpContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            httpContext.Response.TrySkipIisCustomErrors = true;

            return new ContentResult
            {
                Content = message,
            };
        }
    }
}
