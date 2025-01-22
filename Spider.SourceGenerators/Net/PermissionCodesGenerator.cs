using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis.Text;
using Spider.SourceGenerators.Shared;
using Spider.SourceGenerators.Enums;

namespace Spider.SourceGenerators.Net
{
    [Generator]
    public class PermissionCodesGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            //#if DEBUG
            //            if (!Debugger.IsAttached)
            //            {
            //                Debugger.Launch();
            //            }
            //#endif
            IncrementalValuesProvider<ClassDeclarationSyntax> classDeclarations = Helpers.GetClassInrementalValuesProvider(context.SyntaxProvider, new List<NamespaceExtensionCodes>
                {
                    NamespaceExtensionCodes.Entities,
                });

            context.RegisterImplementationSourceOutput(classDeclarations.Collect(),
                static (spc, source) => Execute(source, spc));
        }

        private static void Execute(IList<ClassDeclarationSyntax> classes, SourceProductionContext context)
        {
            if (classes.Count <= 1) return;

            List<ClassDeclarationSyntax> entities = Helpers.GetEntityClasses(classes);

            StringBuilder sb = new StringBuilder();

            string[] namespacePartsWithoutLastElement = Helpers.GetNamespacePartsWithoutLastElement(entities[0]);
            string basePartOfNamespace = string.Join(".", namespacePartsWithoutLastElement); // eg. Spider.Security
            string projectName = namespacePartsWithoutLastElement[namespacePartsWithoutLastElement.Length - 1]; // eg. Security

            List<string> enumHelper = Helpers.GetPermissionCodesForEntites(entities);

            sb.AppendLine($$"""
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace {{basePartOfNamespace}}.Enums
{
    public enum PermissionCodes
    {
        {{string.Join(",\n\t\t", enumHelper)}}
    }
}
""");

            context.AddSource($"{projectName}PermissionCodes.generated", SourceText.From(sb.ToString(), Encoding.UTF8));
        }
    }
}

