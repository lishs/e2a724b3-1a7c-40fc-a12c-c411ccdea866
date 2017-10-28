using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nop.Web.Framework.Mvc.Models;

namespace Aperture.Plugin.AptCheckout.Extensions
{

    public static class BaseNopModelExtensions
    {
        public static T GetCustomProperty<T>(this BaseNopModel model, string key = null)
        {

            if (key == null) //convenience feature that gets first custom property of applicable type
            {
                foreach (var value in model.CustomProperties.Values)
                {

                    if (value.GetType() == typeof(T))
                    {
                        return (T)value;
                    }
                }
            }
            else if (model.CustomProperties.ContainsKey(key))
            {
                try
                {
                    var retVal = model.CustomProperties[key];
                    return (T)retVal;

                }
                catch
                {
                    return default(T);
                }
            }
            return default(T);
        }
    }
}
