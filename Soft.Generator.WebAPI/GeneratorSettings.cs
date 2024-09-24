using Soft.Generator.Shared.Attributes;

namespace Soft.Generator.Security.GeneratorSettings
{
    public class GeneratorSettings
    {
        [Output(@"E:\Projects\Soft.Generator\Source\Soft.Generator.SPA\src\app\business\services\api\api.service.generated.ts")]
        public string NgControllersGenerator { get; set; }
    }
}