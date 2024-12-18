using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Soft.SourceGenerator.NgTable.Angular;
using Soft.SourceGenerator.NgTable.Helpers;
using Soft.SourceGenerators.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Soft.SourceGenerators.Net
{
    [Generator]
    public class DbContextGenerator : IIncrementalGenerator
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
                    predicate: static (s, _) => Helper.IsSyntaxTargetForGenerationEntities(s),
                    transform: static (ctx, _) => Helper.GetSemanticTargetForGenerationEntities(ctx))
                .Where(static c => c is not null);

            IncrementalValueProvider<List<SoftClass>> referencedProjectEntityClasses = Helper.GetEntityClassesFromReferencedAssemblies(context);

            var allClasses = classDeclarations.Collect()
                .Combine(referencedProjectEntityClasses);

            context.RegisterImplementationSourceOutput(allClasses, static (spc, source) => Execute(source.Left, source.Right, spc));
        }

        private static void Execute(IList<ClassDeclarationSyntax> classes, List<SoftClass> referencedProjectEntityClasses, SourceProductionContext context)
        {
            if (classes.Count <= 1)
                return;

            bool shouldGenerateDbContext = Helper.ShouldGenerateDbContext(nameof(DbContextGenerator), classes);

            if (shouldGenerateDbContext == false)
                return;

            List<SoftClass> entityClasses = Helper.GetSoftClasses(classes);

            SoftClass customDbContextClass = entityClasses.Where(x => x.BaseType.Contains("ApplicationDbContext<")).SingleOrDefault();

            //if (customDbContextClass == null)
            //    return;

            List<SoftClass> allEntityClasses = entityClasses.Concat(referencedProjectEntityClasses).ToList();

            string[] namespacePartsWithoutLastElement = Helper.GetNamespacePartsWithoutLastElement(entityClasses[0].Namespace);

            string basePartOfTheNamespace = string.Join(".", namespacePartsWithoutLastElement); // eg. Playerty.Loyals.Infrastructure
            //string projectName = namespacePartsWithoutLastElement[namespacePartsWithoutLastElement.Length - 1]; // eg. Infrastructure
            string projectName = namespacePartsWithoutLastElement[0]; // eg. Playerty

            string result = $$"""
using Microsoft.EntityFrameworkCore;
{{string.Join("\n", GetUsingsOfReferencedProjects())}}

namespace {{basePartOfTheNamespace}}
{
    public partial class {{projectName}}ApplicationDbContext
    {
{{string.Join("\n", GetDbSetDefinitionsOfReferencedProjects())}}
    }
}
""";

            context.AddSource($"{projectName}ApplicationDbContext.generated", SourceText.From(result, Encoding.UTF8));
        }

    }
}
