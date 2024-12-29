using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Soft.SourceGenerator.NgTable.Helpers;
using Soft.SourceGenerators.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Soft.SourceGenerators.Net
{
    [Generator]
    public class ControllerGenerator
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
                    predicate: static (s, _) => Helper.IsSyntaxTargetForGenerationEveryClass(s),
                    transform: static (ctx, _) => Helper.GetSemanticTargetForGenerationEveryClass(ctx))
                .Where(static c => c is not null);

            IncrementalValueProvider<List<SoftClass>> referencedProjectEntityClasses = Helper.GetEntityClassesFromReferencedAssemblies(context);

            var allClasses = classDeclarations.Collect()
                .Combine(referencedProjectEntityClasses);

            context.RegisterImplementationSourceOutput(allClasses, static (spc, source) => Execute(source.Left, source.Right, spc));
        }

        private static void Execute(IList<ClassDeclarationSyntax> classes, List<SoftClass> referencedProjectEntityClasses, SourceProductionContext context)
        {
            if (classes.Count < 1)
                return;

            bool shouldGenerateController = Helper.ShouldStartGenerator(nameof(ControllerGenerator), classes);

            if (shouldGenerateController == false)
                return;

            List<SoftClass> projectClasses = Helper.GetSoftClasses(classes);

            List<SoftClass> customControllers = projectClasses.Where(x => x.Namespace.EndsWith(".Controllers")).ToList();

            List<SoftClass> allEntityClasses = projectClasses.Concat(referencedProjectEntityClasses).ToList();

            string[] namespacePartsWithoutLastElement = Helper.GetNamespacePartsWithoutLastElement(projectClasses[0].Namespace);

            string basePartOfTheNamespace = string.Join(".", namespacePartsWithoutLastElement); // eg. Playerty.Loyals.Infrastructure
            //string projectName = namespacePartsWithoutLastElement[namespacePartsWithoutLastElement.Length - 1]; // eg. Infrastructure
            string projectName = namespacePartsWithoutLastElement[0]; // eg. Playerty

            string result = $$"""
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Soft.Generator.Infrastructure;
using System.Data;
{{string.Join("\n", Helper.GetEntityClassesUsings(referencedProjectEntityClasses))}}
{{string.Join("\n", Helper.GetDTOClassesUsings(referencedProjectEntityClasses))}}

namespace {{basePartOfTheNamespace}}
{
    public partial class {{projectName}}BaseController
    {

    }
}
""";

            context.AddSource($"{projectName}BaseController.generated", SourceText.From(result, Encoding.UTF8));
        }
    }
}
