using LightInject;
using Soft.Generator.Security.Interface;
using Soft.Generator.Shared.Excel;
using Soft.Generator.Security.Services;
using System.Resources;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Mvc;
using Soft.Generator.Shared.SoftFluentValidation;
using Soft.Generator.Shared.Emailing;
//using Soft.Generator.Infrastructure.Repositories.Security;

namespace Soft.Generator.WebAPI.DI
{
    public class CompositionRoot : ICompositionRoot
    {
        private void ComposeUnified(IServiceRegistry registry)
        {
            registry.Register<SecurityBusinessService>();
            registry.Register<SecurityBusinessServiceGenerated>();
            registry.Register<ExcelService>();
            registry.Register<EmailingService>();
            registry.RegisterSingleton<IConfigureOptions<MvcOptions>, TranslatePropertiesConfiguration>();
            registry.RegisterSingleton<IJwtAuthManager, JwtAuthManagerService>();
        }

        /// <summary>
        /// Place here all common services for solution. 
        /// </summary>
        /// <param name="registry"></param>
        public virtual void Compose(IServiceRegistry registry)
        {
            ComposeUnified(registry);
        }
    }
}
