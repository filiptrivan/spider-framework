using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Spider.SourceGenerators.Shared;
using Spider.SourceGenerators.Enums;
using Spider.SourceGenerators.Models;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Spider.SourceGenerators.Net
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
            IncrementalValuesProvider<ClassDeclarationSyntax> classDeclarations = Helpers.GetClassIncrementalValuesProvider(context.SyntaxProvider, new List<NamespaceExtensionCodes>
                {
                    NamespaceExtensionCodes.Entities,
                });
            
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
            if (classes.Count <= 1) 
                return;

            List<SpiderClass> currentProjectEntities = Helpers.GetSpiderClasses(classes, referencedProjectEntityClasses);
            List<SpiderClass> allEntities = currentProjectEntities.Concat(referencedProjectEntityClasses).ToList();

            StringBuilder sb = new();

            string namespaceValue = currentProjectEntities[0].Namespace;
            string basePartOfNamespace = Helpers.GetBasePartOfNamespace(namespaceValue);
            string projectName = Helpers.GetProjectName(namespaceValue);

            bool generateAuthorizationMethods = projectName != "Security";

            sb.AppendLine($$"""
{{GetUsings(basePartOfNamespace)}}

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
            foreach (SpiderClass entity in currentProjectEntities)
            {
                if (entity.BaseType == null) // FT: Handling many to many, maybe you should do something else in the future
                    continue;

                string idTypeOfTheEntityClass = entity.GetIdType(allEntities);

                sb.AppendLine($$"""
        #region {{entity.Name}}

""");

                sb.AppendLine($$"""
        public virtual async Task {{entity.Name}}SingleReadAuthorize({{idTypeOfTheEntityClass}} {{entity.Name.FirstCharToLower()}}Id)
        {
{{(generateAuthorizationMethods ? $$"""
            await _context.WithTransactionAsync(async () =>
            {
                await AuthorizeAndThrowAsync<UserExtended>(PermissionCodes.Read{{entity.Name}});
            });
"""
            : "")}}
        }

        public virtual async Task {{entity.Name}}SingleUpdateAuthorize({{entity.Name}}DTO {{entity.Name.FirstCharToLower()}}DTO) // FT: Save
        {
{{(generateAuthorizationMethods ? $$"""
            await _context.WithTransactionAsync(async () =>
            {
                await AuthorizeAndThrowAsync<UserExtended>(PermissionCodes.Edit{{entity.Name}});
            });
"""
            : "")}}
        }

        public virtual async Task {{entity.Name}}SingleUpdateAuthorize({{idTypeOfTheEntityClass}} {{entity.Name.FirstCharToLower()}}Id) // FT: Blob
        {
{{(generateAuthorizationMethods ? $$"""
            await _context.WithTransactionAsync(async () =>
            {
                await AuthorizeAndThrowAsync<UserExtended>(PermissionCodes.Edit{{entity.Name}});
            });
"""
            : "")}}
        }

        public virtual async Task {{entity.Name}}SingleInsertAuthorize({{entity.Name}}DTO {{entity.Name.FirstCharToLower()}}DTO) // FT: Save
        {
{{(generateAuthorizationMethods ? $$"""
            await _context.WithTransactionAsync(async () =>
            {
                await AuthorizeAndThrowAsync<UserExtended>(PermissionCodes.Insert{{entity.Name}});
            });
"""
            : "")}}
        }

        public virtual async Task {{entity.Name}}SingleInsertAuthorize() // FT: Blob, the id will always be 0, so we don't need to pass it.
        {
{{(generateAuthorizationMethods ? $$"""
            await _context.WithTransactionAsync(async () =>
            {
                await AuthorizeAndThrowAsync<UserExtended>(PermissionCodes.Insert{{entity.Name}});
            });
"""
            : "")}}
        }

        public virtual async Task {{entity.Name}}ListReadAuthorize() // FT: Same for table, excel, autocomplete, dropdown
        {
{{(generateAuthorizationMethods ? $$"""
            await _context.WithTransactionAsync(async () =>
            {
                await AuthorizeAndThrowAsync<UserExtended>(PermissionCodes.Read{{entity.Name}});
            });
"""
            : "")}}
        }

        public virtual async Task {{entity.Name}}DeleteAuthorize({{idTypeOfTheEntityClass}} {{entity.Name.FirstCharToLower()}}Id)
        {
{{(generateAuthorizationMethods ? $$"""
            await _context.WithTransactionAsync(async () =>
            {
                await AuthorizeAndThrowAsync<UserExtended>(PermissionCodes.Delete{{entity.Name}});
            });
"""
            : "")}}
        }

        public virtual async Task {{entity.Name}}ListDeleteAuthorize(List<{{idTypeOfTheEntityClass}}> {{entity.Name.FirstCharToLower()}}ListToDelete)
        {
{{(generateAuthorizationMethods ? $$"""
            await _context.WithTransactionAsync(async () =>
            {
                await AuthorizeAndThrowAsync<UserExtended>(PermissionCodes.Delete{{entity.Name}});
            });
"""
            : "")}}
        }

""");

                sb.AppendLine($$"""
        #endregion

""");
            }

            sb.AppendLine($$"""
    }
}
""");

            context.AddSource($"AuthorizationBusinessService.generated", SourceText.From(sb.ToString(), Encoding.UTF8));
        }

        private static string GetUsings(string basePartOfTheNamespace)
        {
            return $$"""
using {{basePartOfTheNamespace}}.Entities;
using {{basePartOfTheNamespace}}.Enums;
using {{basePartOfTheNamespace}}.DTO;
using Azure.Storage.Blobs;
using Spider.Security.Services;
using Spider.Shared.Extensions;
using Spider.Shared.Interfaces;
""";
        }
    }
}