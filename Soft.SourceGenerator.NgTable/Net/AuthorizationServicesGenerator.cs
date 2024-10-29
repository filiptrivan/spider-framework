using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Soft.SourceGenerator.NgTable.Helpers;
using Soft.SourceGenerators.Helpers;
using Soft.SourceGenerators.Models;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Soft.SourceGenerator.NgTable.Net
{
    [Generator]
    public class AuthorizationServicesGenerator : IIncrementalGenerator
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

            IncrementalValueProvider<IEnumerable<INamedTypeSymbol>> referencedProjectClasses = Helper.GetReferencedProjectsSymbolsEntities(context);

            var allClasses = classDeclarations.Collect()
                .Combine(referencedProjectClasses);

            context.RegisterImplementationSourceOutput(allClasses, static (spc, source) => Execute(source.Left, source.Right, spc));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="classes">Only EF classes</param>
        /// <param name="context"></param>
        private static void Execute(IList<ClassDeclarationSyntax> classes, IEnumerable<INamedTypeSymbol> referencedClassesEntities, SourceProductionContext context)
        {
            if (classes.Count <= 1) return;
            List<ClassDeclarationSyntax> entityClasses = Helper.GetEntityClasses(classes);

            StringBuilder sb = new StringBuilder();

            string[] namespacePartsWithoutLastElement = Helper.GetNamespacePartsWithoutLastElement(entityClasses[0]);

            string basePartOfNamespace = string.Join(".", namespacePartsWithoutLastElement); // eg. Soft.Generator.Security
            string projectName = namespacePartsWithoutLastElement[namespacePartsWithoutLastElement.Length - 1]; // eg. Security

            bool generateAuthorizationMethods = projectName != "Security";

            sb.AppendLine($$"""
using {{basePartOfNamespace}}.Entities;
using {{basePartOfNamespace}}.Enums;
using {{basePartOfNamespace}}.DTO;
using Azure.Storage.Blobs;
using Soft.Generator.Security.Services;
using Soft.Generator.Shared.Extensions;
using Soft.Generator.Shared.Interfaces;

namespace {{basePartOfNamespace}}.Services
{
    public class AuthorizationBusinessServiceGenerated : AuthorizationService
    {
        private readonly IApplicationDbContext _context;
        private readonly AuthenticationService _authenticationService;

        public AuthorizationBusinessServiceGenerated(IApplicationDbContext context, AuthenticationService authenticationService, BlobContainerClient blobContainerClient)
            : base(context, authenticationService, blobContainerClient)
        {
            _context = context;
            _authenticationService = authenticationService;
        }
""");
            foreach (ClassDeclarationSyntax entityClass in entityClasses)
            {
                string baseType = entityClass.GetBaseType();

                if (baseType == null) // FT: Handling many to many, maybe you should do something else in the future
                    continue;

                string nameOfTheEntityClass = entityClass.Identifier.Text;
                string nameOfTheEntityClassFirstLower = entityClass.Identifier.Text.FirstCharToLower();
                string idTypeOfTheEntityClass = Helper.GetGenericIdType(entityClass, entityClasses);

                sb.AppendLine($$"""
        public virtual async Task {{nameOfTheEntityClass}}SingleReadAuthorize({{idTypeOfTheEntityClass}} {{nameOfTheEntityClassFirstLower}}Id)
        {
{{(generateAuthorizationMethods ? $$"""
            await _context.WithTransactionAsync(async () =>
            {
                await AuthorizeAndThrowAsync<UserExtended>(PermissionCodes.Read{{nameOfTheEntityClass}});
            });
"""
            : "")}}
        }

        public virtual async Task {{nameOfTheEntityClass}}SingleUpdateAuthorize({{nameOfTheEntityClass}}DTO {{nameOfTheEntityClassFirstLower}}DTO) // FT: Save
        {
{{(generateAuthorizationMethods ? $$"""
            await _context.WithTransactionAsync(async () =>
            {
                await AuthorizeAndThrowAsync<UserExtended>(PermissionCodes.Edit{{nameOfTheEntityClass}});
            });
"""
            : "")}}
        }

        public virtual async Task {{nameOfTheEntityClass}}SingleUpdateAuthorize({{idTypeOfTheEntityClass}} {{nameOfTheEntityClassFirstLower}}Id) // FT: Blob
        {
{{(generateAuthorizationMethods ? $$"""
            await _context.WithTransactionAsync(async () =>
            {
                await AuthorizeAndThrowAsync<UserExtended>(PermissionCodes.Edit{{nameOfTheEntityClass}});
            });
"""
            : "")}}
        }

        public virtual async Task {{nameOfTheEntityClass}}SingleInsertAuthorize({{nameOfTheEntityClass}}DTO {{nameOfTheEntityClassFirstLower}}DTO) // FT: Save
        {
{{(generateAuthorizationMethods ? $$"""
            await _context.WithTransactionAsync(async () =>
            {
                await AuthorizeAndThrowAsync<UserExtended>(PermissionCodes.Insert{{nameOfTheEntityClass}});
            });
"""
            : "")}}
        }

        public virtual async Task {{nameOfTheEntityClass}}SingleInsertAuthorize() // FT: Blob, the id will always be 0, so we don't need to pass it.
        {
{{(generateAuthorizationMethods ? $$"""
            await _context.WithTransactionAsync(async () =>
            {
                await AuthorizeAndThrowAsync<UserExtended>(PermissionCodes.Insert{{nameOfTheEntityClass}});
            });
"""
            : "")}}
        }

        public virtual async Task {{nameOfTheEntityClass}}ListReadAuthorize() // FT: Same for table, excel, autocomplete, dropdown
        {
{{(generateAuthorizationMethods ? $$"""
            await _context.WithTransactionAsync(async () =>
            {
                await AuthorizeAndThrowAsync<UserExtended>(PermissionCodes.Read{{nameOfTheEntityClass}});
            });
"""
            : "")}}
        }

        public virtual async Task {{nameOfTheEntityClass}}DeleteAuthorize({{idTypeOfTheEntityClass}} {{nameOfTheEntityClassFirstLower}}Id)
        {
{{(generateAuthorizationMethods ? $$"""
            await _context.WithTransactionAsync(async () =>
            {
                await AuthorizeAndThrowAsync<UserExtended>(PermissionCodes.Delete{{nameOfTheEntityClass}});
            });
"""
            : "")}}
        }
""");
            }

            sb.AppendLine($$"""
    }
}
""");

            context.AddSource($"AuthorizationBusinessService.generated", SourceText.From(sb.ToString(), Encoding.UTF8));
        }

    }
}