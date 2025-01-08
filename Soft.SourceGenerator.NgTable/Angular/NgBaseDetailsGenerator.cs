using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Soft.SourceGenerator.NgTable.Angular;
using Soft.SourceGenerator.NgTable.Helpers;
using Soft.SourceGenerators.Enums;
using Soft.SourceGenerators.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Soft.SourceGenerators.Angular
{
    [Generator]
    public class NgBaseDetailsGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            //#if DEBUG
            //            if (!Debugger.IsAttached)
            //            {
            //                Debugger.Launch();
            //            }
            //#endif
            IncrementalValuesProvider<ClassDeclarationSyntax> classDeclarations = context.SyntaxProvider
                .CreateSyntaxProvider(
                   predicate: static (s, _) => Helper.IsClassSyntaxTargetForGeneration(s, new List<NamespaceExtensionCodes>
                   {
                       NamespaceExtensionCodes.Entities,
                       NamespaceExtensionCodes.DTO
                   }),
                   transform: static (ctx, _) => Helper.GetClassSemanticTargetForGeneration(ctx, new List<NamespaceExtensionCodes>
                   {
                       NamespaceExtensionCodes.Entities,
                       NamespaceExtensionCodes.DTO
                   }))
                .Where(static c => c is not null);

            IncrementalValueProvider<List<SoftClass>> referencedProjectClasses = Helper.GetIncrementalValueProviderClassesFromReferencedAssemblies(context,
                new List<NamespaceExtensionCodes>
                {
                    NamespaceExtensionCodes.Entities,
                    NamespaceExtensionCodes.DTO,
                });

            var allClasses = classDeclarations.Collect()
                .Combine(referencedProjectClasses);

            context.RegisterImplementationSourceOutput(allClasses, static (spc, source) => Execute(source.Left, source.Right, spc));
        }

        private static void Execute(IList<ClassDeclarationSyntax> classes, List<SoftClass> referencedProjectClasses, SourceProductionContext context)
        {
            if (classes.Count <= 1)
                return; // FT: one because of config settings

            string outputPath = Helper.GetGeneratorOutputPath(nameof(NgControllersGenerator), classes);

            if (outputPath == null)
                return;

            List<SoftClass> softClasses = Helper.GetSoftClasses(classes);

            List<SoftClass> controllerClasses = softClasses
                .Where(x => x.Namespace.EndsWith($".{NamespaceExtensionCodes.Controllers}"))
                .ToList();

            List<SoftClass> referencedDTOClasses = referencedProjectClasses
                .Where(x => x.Namespace.EndsWith($".{NamespaceExtensionCodes.DTO}"))
                .ToList();

            List<SoftClass> referencedEntityClasses = referencedProjectClasses
                .Where(x => x.Namespace.EndsWith($".{NamespaceExtensionCodes.Entities}"))
                .ToList();

            string result = $$"""
{{string.Join("\n", GetImports(referencedDTOClasses))}}

@Injectable()
export class ApiGeneratedService extends ApiSecurityService {

    constructor(protected override http: HttpClient) {
        super(http);
    }

{{string.Join("\n\n", GetAngularHttpMethods(controllerClasses, referencedEntityClasses))}}

}
""";

            Helper.WriteToTheFile(result, outputPath);
        }

    }
}
