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

                string entityIdType = entity.GetIdType(allEntities);

                sb.AppendLine($$"""
        #region {{entity.Name}}

{{GetAuthorizeEntityMethod(entity.Name, entity, CrudCodes.Read, $"{entityIdType} {entity.Name.FirstCharToLower()}IdToRead", projectName, isSecurityProject)}}

{{GetAuthorizeEntityMethod(entity.Name, entity, CrudCodes.Read, $"List<{entityIdType}> {entity.Name.FirstCharToLower()}IdListToRead", projectName, isSecurityProject)}}

{{GetAuthorizeEntityMethod(entity.Name, entity, CrudCodes.Update, $"{entity.Name}DTO dto", projectName, isSecurityProject)}}

{{GetAuthorizeEntityMethod(entity.Name, entity, CrudCodes.Insert, $"{entity.Name}DTO dto", projectName, isSecurityProject)}}

{{GetAuthorizeEntityMethod(entity.Name, entity, CrudCodes.Delete, $"{entityIdType} {entity.Name.FirstCharToLower()}Id", projectName, isSecurityProject)}}

{{GetAuthorizeEntityMethod(entity.Name, entity, CrudCodes.Delete, $"List<{entityIdType}> {entity.Name.FirstCharToLower()}ListToDelete", projectName, isSecurityProject)}}

{{GetBloblAuthorizeEntityMethods(entity, entityIdType, projectName, isSecurityProject)}}

        #endregion

""");
            }

            return sb.ToString();
        }

        private static string GetBloblAuthorizeEntityMethods(SpiderClass entity, string entityIdType, string projectName, bool isSecurityProject)
        {
            StringBuilder sb = new();

            foreach (SpiderProperty property in Helpers.GetBlobProperties(entity.Properties))
            {
                sb.AppendLine($$"""
{{GetAuthorizeEntityMethod($"{property.Name}For{entity.Name}", entity, CrudCodes.Update, $"{entityIdType} {entity.Name.FirstCharToLower()}Id", projectName, isSecurityProject)}} // FT: Blob update

{{GetAuthorizeEntityMethod($"{property.Name}For{entity.Name}", entity, CrudCodes.Insert, $"", projectName, isSecurityProject)}} // FT: Blob insert, the id will always be 0, so we don't need to pass it.
""");
            }

            return sb.ToString();
        }

        private static string GetAuthorizeEntityMethod(string authorizeEntityMethodFirstPart, SpiderClass entity, CrudCodes crudCode, string parametersBody, string projectName, bool isSecurityProject)
        {
            string methodName = Helpers.GetAuthorizeEntityMethodName(authorizeEntityMethodFirstPart, crudCode);
            return GetAuthorizeMethod(methodName, parametersBody, crudCode, entity, projectName, isSecurityProject);
        }

        private static string GetAuthorizeMethod(string methodName, string parametersBody, CrudCodes permissionCodePrefix, SpiderClass entity, string projectName, bool isSecurityProject)
        {
            return $$"""
        public virtual async Task {{methodName}}({{parametersBody}})
        {
            await _context.WithTransactionAsync(async () =>
            {
                await AuthorizeAndThrowAsync<{{(isSecurityProject ? "TUser" : "UserExtended")}}>({{projectName}}PermissionCodes.{{permissionCodePrefix}}{{entity.Name}});
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