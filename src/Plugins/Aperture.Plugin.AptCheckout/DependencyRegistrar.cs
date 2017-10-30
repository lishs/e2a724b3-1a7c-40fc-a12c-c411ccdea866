using Aperture.Plugin.AptCheckout.Factories;
using Autofac;
using Nop.Core.Configuration;
using Nop.Core.Infrastructure;
using Nop.Core.Infrastructure.DependencyManagement;
using Nop.Web.Factories;

namespace Aperture.Plugin.AptCheckout
{
    public class DependencyRegistrar : IDependencyRegistrar
    {
        public void Register(ContainerBuilder builder, ITypeFinder typeFinder, NopConfig config)
        {
            //throw new NotImplementedException();
            builder.RegisterType<CustomCheckoutModelFactory>().As<ICheckoutModelFactory>();
           // builder.RegisterType<CustomShoppingCartModelFactory>().As<IShoppingCartModelFactory>();
        }

        public int Order => 100;
    }
}
