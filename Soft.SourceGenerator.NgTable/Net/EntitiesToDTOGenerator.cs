using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis.Text;
using Soft.SourceGenerator.NgTable.Helpers;
using System.Collections.Immutable;
using System.Linq;
using System.IO;
using Soft.SourceGenerators.Helpers;
using System.Diagnostics;
using Soft.SourceGenerator.NgTable.Angular;

namespace Soft.SourceGenerator.NgTable.Net
{
    [Generator]
    public class EntitiesToDTOGenerator : IIncrementalGenerator
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

            context.RegisterImplementationSourceOutput(classDeclarations.Collect(),
                static (spc, source) => Execute(source, spc));
        }

        private static void Execute(IList<ClassDeclarationSyntax> classes, SourceProductionContext context)
        {
            if (classes.Count <= 1) return;
            List<ClassDeclarationSyntax> entityClasses = Helper.GetEntityClasses(classes);
            List<ClassDeclarationSyntax> uninheritedEntityClasses = Helper.GetUninheritedClasses(entityClasses);

            string outputPath = Helper.GetGeneratorOutputPath(nameof(EntitiesToDTOGenerator), classes);

            StringBuilder sb = new StringBuilder();

            string[] namespacePartsWithoutLastElement = Helper.GetNamespacePartsWithoutLastElement(entityClasses[0]);

            string basePartOfNamespace = string.Join(".", namespacePartsWithoutLastElement); // eg. Soft.Generator.Security
            string projectName = namespacePartsWithoutLastElement[namespacePartsWithoutLastElement.Length - 1]; // eg. Security

            sb.AppendLine($$"""
using Soft.Generator.Shared.DTO;
using Soft.Generator.Security.DTO;

namespace {{basePartOfNamespace}}.DTO // FT: Don't change namespace in generator, it's mandatory for partial classes
{
""");
            foreach (ClassDeclarationSyntax c in entityClasses)
            {
                string baseClass = c.GetDTOBaseType();

                sb.AppendLine($$"""
    public partial class {{c.Identifier.Text}}DTO {{(baseClass == null ? "" : $": {baseClass}")}}
    {
        {{string.Join("\n\t\t", Helper.GetDTOWithoutBaseProps(c, entityClasses))}}
    }
""");
            }

            sb.AppendLine($$"""
}
""");

            //Helper.WriteToTheFile(sb.ToString(), $@"{outputPath}");

            // FT: does not generating because we make file on the disk, because mapping can't figure out something inside analyzers
            context.AddSource($"{projectName}DTOList.generated", SourceText.From(sb.ToString(), Encoding.UTF8));
        }

    }
}
