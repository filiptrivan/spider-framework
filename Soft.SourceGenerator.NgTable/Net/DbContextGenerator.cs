using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Soft.SourceGenerator.NgTable.Angular;
using Soft.SourceGenerator.NgTable.Helpers;
using Soft.SourceGenerators.Enums;
using Soft.SourceGenerators.Helpers;
using Soft.SourceGenerators.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Soft.SourceGenerators.Net
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
                    predicate: static (s, _) => Helper.IsSyntaxTargetForGenerationEveryClass(s),
                    transform: static (ctx, _) => Helper.GetSemanticTargetForGenerationEveryClass(ctx))
                .Where(static c => c is not null);

            IncrementalValueProvider<List<SoftClass>> referencedProjectClasses = Helper.GetIncrementalValueProviderClassesFromReferencedAssemblies(context,
                new List<NamespaceExtensionCodes>
                {
                    NamespaceExtensionCodes.Entities,
                });

            var allClasses = classDeclarations.Collect()
                .Combine(referencedProjectClasses);

            context.RegisterImplementationSourceOutput(allClasses, static (spc, source) => Execute(source.Left, source.Right, spc));
        }

        private static void Execute(IList<ClassDeclarationSyntax> classes, List<SoftClass> referencedProjectEntityClasses, SourceProductionContext context)
        {
            return;

            if (classes.Count < 1)
                return;

            bool shouldGenerateDbContext = Helper.ShouldStartGenerator(nameof(DbContextGenerator), classes);

            if (shouldGenerateDbContext == false)
                return;

            List<SoftClass> projectClasses = Helper.GetSoftClasses(classes);

            SoftClass customDbContextClass = projectClasses.Where(x => x.BaseType != null && x.BaseType.Contains("ApplicationDbContext<")).SingleOrDefault();

            //if (customDbContextClass == null)
            //    return;

            List<SoftClass> allEntityClasses = projectClasses.Concat(referencedProjectEntityClasses).ToList();

            string[] namespacePartsWithoutLastElement = Helper.GetNamespacePartsWithoutLastElement(projectClasses[0].Namespace);

            string basePartOfTheNamespace = string.Join(".", namespacePartsWithoutLastElement); // eg. PlayertyLoyals.Infrastructure
            //string projectName = namespacePartsWithoutLastElement[namespacePartsWithoutLastElement.Length - 1]; // eg. Infrastructure
            string projectName = namespacePartsWithoutLastElement[0]; // eg. Playerty

            string result = $$"""
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Soft.Generator.Infrastructure;
using System.Data;
{{string.Join("\n", Helper.GetEntityClassesUsings(referencedProjectEntityClasses))}}

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

        private static List<string> GetDbSetDefinitionsOfReferencedProjects(List<SoftClass> referencedProjectEntityClasses)
        {
            List<string> result = new List<string>();

            foreach (SoftClass referencedProjectEntityClass in referencedProjectEntityClasses)
            {
                if (referencedProjectEntityClass.Namespace.StartsWith("Soft.Generator") ||
                    referencedProjectEntityClass.Name == "UserExtended")
                    continue;

                result.Add($$"""
        public DbSet<{{referencedProjectEntityClass.Name}}> {{GetDbContextPropertyName(referencedProjectEntityClass)}} { get; set; } {{GetDbContextPropertyComment(referencedProjectEntityClass)}}
""");
            }

            return result;
        }
        
        private static string GetDbContextPropertyName(SoftClass referencedProjectEntityClass)
        {
            if (referencedProjectEntityClass.BaseType == null)
                return referencedProjectEntityClass.Name;
            else
                return referencedProjectEntityClass.Name.Pluralize();
        }

        private static string GetDbContextPropertyComment(SoftClass referencedProjectEntityClass)
        {
            if (referencedProjectEntityClass.BaseType == null)
                return "// M2M";
            else
                return null;
        }

    }
}
