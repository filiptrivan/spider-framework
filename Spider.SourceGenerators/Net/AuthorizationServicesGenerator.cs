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

        private static void Execute(IList<ClassDeclarationSyntax> classes, List<SpiderClass> referencedProjectEntities, SourceProductionContext context)
        {
            if (classes.Count <= 1)
                return;

            List<SpiderClass> currentProjectEntities = Helpers.GetSpiderClasses(classes, referencedProjectEntities);
            List<SpiderClass> allEntities = currentProjectEntities.Concat(referencedProjectEntities).ToList();

            string namespaceValue = currentProjectEntities[0].Namespace;
            string basePartOfNamespace = Helpers.GetBasePartOfNamespace(namespaceValue);
            string projectName = Helpers.GetProjectName(namespaceValue);

            bool isSecurityProject = projectName == "Security";

            string result = $$"""
{{GetUsings(basePartOfNamespace)}}

namespace {{basePartOfNamespace}}.Services
{
    {{(isSecurityProject ? $"public class AuthorizationBusinessServiceGenerated<TUser> : AuthorizationService where TUser : class, IUser, new()" : $"public class AuthorizationBusinessServiceGenerated : AuthorizationService")}}
    {
        private readonly IApplicationDbContext _context;
        private readonly AuthenticationService _authenticationService;

        public AuthorizationBusinessServiceGenerated(IApplicationDbContext context, AuthenticationService authenticationService, BlobContainerClient blobContainerClient)
            : base(context, authenticationService, blobContainerClient)
        {
            _context = context;
            _authenticationService = authenticationService;
        }

{{GetAuthorizeRegions(currentProjectEntities, allEntities, projectName, isSecurityProject)}}

    }
}
""";

            context.AddSource($"AuthorizationBusinessService.generated", SourceText.From(result, Encoding.UTF8));
        }

        public static string GetAuthorizeRegions(List<SpiderClass> currentProjectEntities, List<SpiderClass> allEntities, string projectName, bool isSecurityProject)
        {
            StringBuilder sb = new();

            foreach (SpiderClass entity in currentProjectEntities)
            {
                if (entity.BaseType == null) // FT: Handling many to many, maybe you should do something else in the future
                    continue;

                string idTypeOfTheEntityClass = entity.GetIdType(allEntities);

                sb.AppendLine($$"""
        #region {{entity.Name}}

{{GetAuthorizeEntityMethod(entity.Name, CrudCodes.Read, $"{idTypeOfTheEntityClass} {entity.Name.FirstCharToLower()}Id", projectName, isSecurityProject)}}

        // FT: Same for table, excel, autocomplete, dropdown
{{GetAuthorizeEntityListMethod(entity.Name, CrudCodes.Read, "", projectName, isSecurityProject)}}

{{GetAuthorizeEntityMethod(entity.Name, CrudCodes.Update, $"{entity.Name}DTO {entity.Name.FirstCharToLower()}DTO", projectName, isSecurityProject)}}

{{GetAuthorizeEntityMethod(entity.Name, CrudCodes.Update, $"{idTypeOfTheEntityClass} {entity.Name.FirstCharToLower()}Id", projectName, isSecurityProject)}}

{{GetAuthorizeEntityMethod(entity.Name, CrudCodes.Insert, $"{entity.Name}DTO {entity.Name.FirstCharToLower()}DTO", projectName, isSecurityProject)}}

        // FT: Blob, the id will always be 0, so we don't need to pass it.
{{GetAuthorizeEntityMethod(entity.Name, CrudCodes.Insert, "", projectName, isSecurityProject)}}

{{GetAuthorizeEntityMethod(entity.Name, CrudCodes.Delete, $"{idTypeOfTheEntityClass} {entity.Name.FirstCharToLower()}Id", projectName, isSecurityProject)}}

{{GetAuthorizeEntityListMethod(entity.Name, CrudCodes.Delete, $"List<{idTypeOfTheEntityClass}> {entity.Name.FirstCharToLower()}ListToDelete", projectName, isSecurityProject)}}

        #endregion

""");
            }

            return sb.ToString();
        }

        private static string GetAuthorizeEntityMethod(string entityName, CrudCodes crudCode, string parametersBody, string projectName, bool isSecurityProject)
        {
            return GetAuthorizeMethod(Helpers.GetAuthorizeEntityMethodName(entityName, crudCode), parametersBody, crudCode, entityName, projectName, isSecurityProject);
        }

        private static string GetAuthorizeEntityListMethod(string entityName, CrudCodes crudCode, string parametersBody, string projectName, bool isSecurityProject)
        {
            return GetAuthorizeMethod(Helpers.GetAuthorizeEntityListMethodName(entityName, crudCode), parametersBody, crudCode, entityName, projectName, isSecurityProject);
        }

        private static string GetAuthorizeMethod(string methodName, string parametersBody, CrudCodes permissionCodePrefix, string entityName, string projectName, bool isSecurityProject)
        {
            return $$"""
        public virtual async Task {{methodName}}({{parametersBody}})
        {
            await _context.WithTransactionAsync(async () =>
            {
                await AuthorizeAndThrowAsync<{{(isSecurityProject ? "TUser" : "UserExtended")}}>({{projectName}}PermissionCodes.{{permissionCodePrefix}}{{entityName}});
            });
        }
""";
        }

        private static string GetUsings(string basePartOfTheNamespace)
        {
            return $$"""
using {{basePartOfTheNamespace}}.Entities;
using {{basePartOfTheNamespace}}.Enums;
using {{basePartOfTheNamespace}}.DTO;
using Azure.Storage.Blobs;
using Spider.Security.Services;
using Spider.Security.Interfaces;
using Spider.Shared.Extensions;
using Spider.Shared.Interfaces;
""";
        }
    }
}