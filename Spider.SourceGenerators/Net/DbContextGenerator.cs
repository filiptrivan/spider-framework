using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Spider.SourceGenerators.Angular;
using Spider.SourceGenerators.Shared;
using Spider.SourceGenerators.Enums;
using Spider.SourceGenerators.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Spider.SourceGenerators.Net
{
    [Generator]
    [Obsolete("We don't use this generator, because the reflection couldn't find the generated DbSets.")]
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
                    predicate: static (s, _) => Helpers.IsSyntaxTargetForGenerationEveryClass(s),
                    transform: static (ctx, _) => Helpers.GetSemanticTargetForGenerationEveryClass(ctx))
                .Where(static c => c is not null);

            IncrementalValueProvider<List<SpiderClass>> referencedProjectClasses = Helpers.GetIncrementalValueProviderClassesFromReferencedAssemblies(context,
                new List<NamespaceExtensionCodes>
                {
                    NamespaceExtensionCodes.Entities,
                });

            var allClasses = classDeclarations.Collect()
                .Combine(referencedProjectClasses);

            context.RegisterImplementationSourceOutput(allClasses, static (spc, source) => Execute(source.Left, source.Right, spc));
        }

        private static void Execute(IList<ClassDeclarationSyntax> classes, List<SpiderClass> referencedProjectEntityClasses, SourceProductionContext context)
        {
            return;

            if (classes.Count < 1)
                return;

            bool shouldGenerateDbContext = Helpers.ShouldStartGenerator(nameof(DbContextGenerator), classes);

            if (shouldGenerateDbContext == false)
                return;

            List<SpiderClass> projectClasses = Helpers.GetSpiderClasses(classes, referencedProjectEntityClasses);

            SpiderClass customDbContextClass = projectClasses.Where(x => x.BaseType != null && x.BaseType.Contains("ApplicationDbContext<")).SingleOrDefault();

            //if (customDbContextClass == null)
            //    return;

            List<SpiderClass> allEntityClasses = projectClasses.Concat(referencedProjectEntityClasses).ToList();

            string[] namespacePartsWithoutLastElement = Helpers.GetNamespacePartsWithoutLastElement(projectClasses[0].Namespace);

            string basePartOfTheNamespace = string.Join(".", namespacePartsWithoutLastElement); // eg. PlayertyLoyals.Infrastructure
            //string projectName = namespacePartsWithoutLastElement[namespacePartsWithoutLastElement.Length - 1]; // eg. Infrastructure
            string projectName = namespacePartsWithoutLastElement[0]; // eg. Playerty

            string result = $$"""
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Spider.Infrastructure;
using System.Data;
{{string.Join("\n", Helpers.GetEntityClassesUsings(referencedProjectEntityClasses))}}

namespace {{basePartOfTheNamespace}}
{
    public partial class {{projectName}}ApplicationDbContext
    {
{{string.Join("\n", GetDbSetDefinitionsOfReferencedProjects(referencedProjectEntityClasses))}}
    }
}
""";

            context.AddSource($"{projectName}ApplicationDbContext.generated", SourceText.From(result, Encoding.UTF8));
        }

        private static List<string> GetDbSetDefinitionsOfReferencedProjects(List<SpiderClass> referencedProjectEntityClasses)
        {
            List<string> result = new List<string>();

            foreach (SpiderClass referencedProjectEntityClass in referencedProjectEntityClasses)
            {
                if (referencedProjectEntityClass.Namespace.StartsWith("Spider") ||
                    referencedProjectEntityClass.Name == "UserExtended")
                    continue;

                result.Add($$"""
        public DbSet<{{referencedProjectEntityClass.Name}}> {{GetDbContextPropertyName(referencedProjectEntityClass)}} { get; set; } {{GetDbContextPropertyComment(referencedProjectEntityClass)}}
""");
            }

            return result;
        }
        
        private static string GetDbContextPropertyName(SpiderClass referencedProjectEntityClass)
        {
            if (referencedProjectEntityClass.BaseType == null)
                return referencedProjectEntityClass.Name;
            else
                return referencedProjectEntityClass.Name.Pluralize();
        }

        private static string GetDbContextPropertyComment(SpiderClass referencedProjectEntityClass)
        {
            if (referencedProjectEntityClass.BaseType == null)
                return "// M2M";
            else
                return null;
        }

    }
}
