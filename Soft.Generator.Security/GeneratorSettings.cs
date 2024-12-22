using Soft.Generator.Shared.Attributes;

namespace Soft.Generator.Security.GeneratorSettings
{
    public class GeneratorSettings
    {

        [Output(@"E:\Projects\Soft.Generator\Source\Soft.Generator.SPA\src\app\business\entities\generated")]
        public string NgEntitiesGenerator { get; set; }

        [Output(@"E:\Projects\Soft.Generator\Source\Soft.Generator.SPA\src\app\business\enums\generated")]
        public string NgEnumsGenerator { get; set; }
    }
}