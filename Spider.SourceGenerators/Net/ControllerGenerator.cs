using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Spider.SourceGenerators.Shared;
using Spider.SourceGenerators.Enums;
using Spider.SourceGenerators.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Spider.SourceGenerators.Net
{
    [Generator]
    public class ControllerGenerator : IIncrementalGenerator
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
                    NamespaceExtensionCodes.Services
                });

            IncrementalValueProvider<string> callingProjectDirectory = context.GetCallingPath();

            var combined = classDeclarations.Collect()
                .Combine(referencedProjectClasses)
                .Combine(callingProjectDirectory);

            context.RegisterImplementationSourceOutput(combined, static (spc, source) =>
            {
                var (classesAndEntities, callingPath) = source;
                var (classes, referencedClasses) = classesAndEntities;

                Execute(classes, referencedClasses, callingPath, spc);
            });
        }

        private static void Execute(IList<ClassDeclarationSyntax> classes, List<SpiderClass> referencedProjectEntitiesAndServices, string callingProjectDirectory, SourceProductionContext context)
        {
            if (classes.Count < 1)
                return;

            if (callingProjectDirectory.Contains(".WebAPI") == false)
                return;

            List<SpiderClass> currentProjectClasses = Helpers.GetSpiderClasses(classes, referencedProjectEntitiesAndServices);
            List<SpiderClass> customControllers = currentProjectClasses.Where(x => x.Namespace.EndsWith(".Controllers")).ToList();
            List<SpiderClass> referencedProjectEntities = referencedProjectEntitiesAndServices.Where(x => x.Namespace.EndsWith(".Entities")).ToList();
            List<SpiderClass> referencedProjectServices = referencedProjectEntitiesAndServices.Where(x => x.Namespace.EndsWith(".Services")).ToList();

            string namespaceValue = currentProjectClasses[0].Namespace;
            string basePartOfNamespace = Helpers.GetBasePartOfNamespace(namespaceValue);
            string appName = namespaceValue.Split('.')[0]; // eg. PlayertyLoyals

            string result = $$"""
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.AspNetCore.Mvc;
using Azure.Storage.Blobs;
using System.Data;
using Spider.Infrastructure;
using Spider.Shared.Helpers;
using Spider.Shared.Extensions;
using Spider.Shared.Attributes;
using Spider.Shared.Interfaces;
using {{appName}}.Shared.Resources;
using {{appName}}.Business.Entities;
using {{appName}}.Business.DTO;
{{string.Join("\n", Helpers.GetEntityClassesUsings(referencedProjectEntities))}}
{{string.Join("\n", Helpers.GetDTOClassesUsings(referencedProjectEntities))}}

namespace {{basePartOfNamespace}}.Controllers
{
{{string.Join("\n\n", GetControllerClasses(referencedProjectEntities, referencedProjectServices))}}
}
""";

            context.AddSource($"{appName}BaseControllers.generated", SourceText.From(result, Encoding.UTF8));
        }

        public static List<string> GetControllerClasses(List<SpiderClass> referencedProjectEntities, List<SpiderClass> referencedProjectServices)
        {
            List<string> result = new();

            foreach (IGrouping<string, SpiderClass> referencedProjectEntityGroupedClasses in referencedProjectEntities.GroupBy(x => x.ControllerName))
            {
                string servicesNamespace = referencedProjectEntityGroupedClasses.FirstOrDefault().Namespace.Replace(".Entities", ".Services");
                SpiderClass businessServiceClass = referencedProjectServices
                    .Where(x => x.BaseType != null &&
                                x.Namespace != null &&
                                x.Namespace == servicesNamespace &&
                                x.BaseType.Contains("BusinessServiceGenerated") &&
                                x.BaseType.Contains("AuthorizationBusinessServiceGenerated") == false)
                    .SingleOrDefault();

                if (businessServiceClass == null) // FT: Didn't make custom business service in the project.
                    continue;

                string businessServiceName = businessServiceClass.Name;

                result.Add($$"""
    public class {{referencedProjectEntityGroupedClasses.Key}}BaseController : SpiderBaseController
    {
        private readonly IApplicationDbContext _context;
        private readonly {{servicesNamespace}}.{{GetBusinessServiceClassName(businessServiceName)}} _{{businessServiceName.FirstCharToLower()}};
        private readonly BlobContainerClient _blobContainerClient;

        public {{referencedProjectEntityGroupedClasses.Key}}BaseController(IApplicationDbContext context, {{servicesNamespace}}.{{GetBusinessServiceClassName(businessServiceName)}} {{businessServiceName.FirstCharToLower()}}, BlobContainerClient blobContainerClient)
        {
            _context = context;
            _{{businessServiceName.FirstCharToLower()}} = {{businessServiceName.FirstCharToLower()}};
            _blobContainerClient = blobContainerClient;
        }

{{string.Join("\n\n", GetControllerMethods(referencedProjectEntityGroupedClasses.ToList(), referencedProjectEntities, businessServiceName))}}

    }
""");
            }

            return result;
        }

        private static List<string> GetControllerMethods(List<SpiderClass> groupedReferencedProjectEntities, List<SpiderClass> allEntities, string businessServiceName)
        {
            List<string> result = new();

            foreach (SpiderClass referencedProjectEntity in groupedReferencedProjectEntities)
            {
                if (referencedProjectEntity.IsManyToMany()) // TODO FT: Do something with M2M entities
                    continue;

                string referencedProjectEntityClassIdType = referencedProjectEntity.GetIdType(allEntities);

                result.Add($$"""
        #region {{referencedProjectEntity.Name}}

        #region Read

        [HttpPost]
        [AuthGuard]
        public virtual async Task<TableResponseDTO<{{referencedProjectEntity.Name}}DTO>> Get{{referencedProjectEntity.Name}}TableData(TableFilterDTO tableFilterDTO)
        {
            return await _{{businessServiceName.FirstCharToLower()}}.Get{{referencedProjectEntity.Name}}TableData(tableFilterDTO, _context.DbSet<{{referencedProjectEntity.Name}}>(), {{ShouldAuthorizeEntity(referencedProjectEntity)}});
        }

        [HttpPost]
        [AuthGuard]
        public virtual async Task<IActionResult> Export{{referencedProjectEntity.Name}}TableDataToExcel(TableFilterDTO tableFilterDTO)
        {
            byte[] fileContent = await _{{businessServiceName.FirstCharToLower()}}.Export{{referencedProjectEntity.Name}}TableDataToExcel(tableFilterDTO, _context.DbSet<{{referencedProjectEntity.Name}}>(), {{ShouldAuthorizeEntity(referencedProjectEntity)}});
            return File(fileContent, SettingsProvider.Current.ExcelContentType, Uri.EscapeDataString($"{TermsGenerated.ResourceManager.GetExcelTranslation("{{referencedProjectEntity.Name}}ExcelExportName", "{{referencedProjectEntity.Name}}List")}.xlsx"));
        }

        [HttpGet]
        [AuthGuard]
        public virtual async Task<List<{{referencedProjectEntity.Name}}DTO>> Get{{referencedProjectEntity.Name}}List()
        {
            return await _{{businessServiceName.FirstCharToLower()}}.Get{{referencedProjectEntity.Name}}DTOList(_context.DbSet<{{referencedProjectEntity.Name}}>(), {{ShouldAuthorizeEntity(referencedProjectEntity)}});
        }

        [HttpGet]
        [AuthGuard]
        public virtual async Task<{{referencedProjectEntity.Name}}MainUIFormDTO> Get{{referencedProjectEntity.Name}}MainUIFormDTO({{referencedProjectEntityClassIdType}} id)
        {
            return await _{{businessServiceName.FirstCharToLower()}}.Get{{referencedProjectEntity.Name}}MainUIFormDTO(id, {{ShouldAuthorizeEntity(referencedProjectEntity)}});
        }

        [HttpGet]
        [AuthGuard]
        public virtual async Task<{{referencedProjectEntity.Name}}DTO> Get{{referencedProjectEntity.Name}}({{referencedProjectEntityClassIdType}} id)
        {
            return await _{{businessServiceName.FirstCharToLower()}}.Get{{referencedProjectEntity.Name}}DTO(id, {{ShouldAuthorizeEntity(referencedProjectEntity)}});
        }

{{GetManyToOneReadMethods(referencedProjectEntity, allEntities, businessServiceName)}}

{{string.Join("\n\n", GetOrderedOneToManyControllerMethods(referencedProjectEntity, allEntities, businessServiceName))}}

{{string.Join("\n\n", GetManyToManyControllerMethods(referencedProjectEntity, allEntities, businessServiceName))}}

        #endregion

        #region Save

{{GetSaveControllerMethods(referencedProjectEntity, businessServiceName)}}

{{string.Join("\n\n", GetUploadBlobControllerMethods(referencedProjectEntity, allEntities, businessServiceName))}}

        #endregion

        #region Delete

{{GetDeleteControllerMethods(referencedProjectEntity, allEntities, businessServiceName)}}

        #endregion

        #endregion
""");
            }

            return result;
        }

        #region Many To One

        private static string GetManyToOneReadMethods(SpiderClass entity, List<SpiderClass> allEntities, string businessServiceName)
        {
            StringBuilder sb = new();

            foreach (SpiderProperty property in entity.Properties)
            {
                if (property.ShouldGenerateAutocompleteControllerMethod())
                {
                    sb.Append($$"""
{{GetAutocompleteMethod(property, entity, allEntities, businessServiceName)}}

""");
                }

                if (property.ShouldGenerateDropdownControllerMethod())
                {
                    sb.Append($$"""
{{GetDropdownMethod(property, entity, allEntities, businessServiceName)}}

""");
                }
            }

            return sb.ToString();
        }

        private static string GetAutocompleteMethod(SpiderProperty property, SpiderClass entity, List<SpiderClass> allEntities, string businessServiceName)
        {
            SpiderClass manyToOneEntity = allEntities.Where(x => x.Name == Helpers.ExtractTypeFromGenericType(property.Type)).Single();
            string manyToOneEntityIdType = manyToOneEntity.GetIdType(allEntities);
            string manyToOneDisplayName = Helpers.GetDisplayNameProperty(manyToOneEntity);

            return $$"""
        [HttpGet]
        [AuthGuard]
        public virtual async Task<List<NamebookDTO<{{manyToOneEntityIdType}}>>> Get{{property.Name}}AutocompleteListFor{{entity.Name}}(int limit, string query, {{entity.GetIdType(allEntities)}}? {{entity.Name.FirstCharToLower()}}Id)
        {
            return await _{{businessServiceName.FirstCharToLower()}}.Get{{property.Name}}AutocompleteListFor{{entity.Name}}(
                limit, 
                query, 
                _context.DbSet<{{manyToOneEntity.Name}}>(),
                {{ShouldAuthorizeEntity(entity)}},
                {{entity.Name.FirstCharToLower()}}Id
            );
        }
""";
        }

        private static string GetDropdownMethod(SpiderProperty property, SpiderClass entity, List<SpiderClass> allEntities, string businessServiceName)
        {
            SpiderClass manyToOneEntity = allEntities.Where(x => x.Name == Helpers.ExtractTypeFromGenericType(property.Type)).Single();
            string manyToOneEntityIdType = manyToOneEntity.GetIdType(allEntities);
            string manyToOneDisplayName = Helpers.GetDisplayNameProperty(manyToOneEntity);

            return $$"""
        [HttpGet]
        [AuthGuard]
        public virtual async Task<List<NamebookDTO<{{manyToOneEntityIdType}}>>> Get{{property.Name}}DropdownListFor{{entity.Name}}({{entity.GetIdType(allEntities)}}? {{entity.Name.FirstCharToLower()}}Id)
        {
            return await _{{businessServiceName.FirstCharToLower()}}.Get{{property.Name}}DropdownListFor{{entity.Name}}(
                _context.DbSet<{{manyToOneEntity.Name}}>(), 
                {{ShouldAuthorizeEntity(entity)}},
                {{entity.Name.FirstCharToLower()}}Id
            );
        }
""";
        }

        #endregion

        #region Many To Many

        private static List<string> GetManyToManyControllerMethods(SpiderClass referencedProjectEntityClass, List<SpiderClass> referencedProjectEntities, string businessServiceName)
        {
            List<string> result = new();

            foreach (SpiderProperty property in referencedProjectEntityClass.Properties)
            {
                if (property.IsMultiSelectControlType() ||
                    property.IsMultiAutocompleteControlType())
                {
                    result.Add(GetManyToManySelectedEntitiesControllerMethod(property, referencedProjectEntityClass, referencedProjectEntities, businessServiceName));
                }
                else if (property.HasSimpleManyToManyTableLazyLoadAttribute())
                {
                    result.Add(GetSimpleManyToManyTableLazyLoadControllerMethod(property, referencedProjectEntityClass, referencedProjectEntities, businessServiceName));
                }
            }

            return result;
        }

        private static string GetSimpleManyToManyTableLazyLoadControllerMethod(SpiderProperty property, SpiderClass entity, List<SpiderClass> entities, string businessServiceName)
        {
            SpiderClass extractedEntity = entities.Where(x => x.Name == Helpers.ExtractTypeFromGenericType(property.Type)).SingleOrDefault();
            string extractedEntityIdType = extractedEntity.GetIdType(entities);

            return $$"""
        [HttpPost]
        [AuthGuard]
        public virtual async Task<TableResponseDTO<{{extractedEntity.Name}}DTO>> Get{{property.Name}}TableDataFor{{entity.Name}}(TableFilterDTO tableFilterDTO)
        {
            return await _{{businessServiceName.FirstCharToLower()}}.Get{{extractedEntity.Name}}TableData(tableFilterDTO, _context.DbSet<{{extractedEntity.Name}}>().OrderBy(x => x.Id), false);
        }

        [HttpPost]
        [AuthGuard]
        public virtual async Task<IActionResult> Export{{property.Name}}TableDataToExcelFor{{entity.Name}}(TableFilterDTO tableFilterDTO)
        {
            byte[] fileContent = await _{{businessServiceName.FirstCharToLower()}}.Export{{extractedEntity.Name}}TableDataToExcel(tableFilterDTO, _context.DbSet<{{extractedEntity.Name}}>(), false);
            return File(fileContent, SettingsProvider.Current.ExcelContentType, Uri.EscapeDataString($"{TermsGenerated.{{extractedEntity.Name}}ExcelExportName}.xlsx"));
        }

        [HttpPost]
        [AuthGuard]
        public virtual async Task<LazyLoadSelectedIdsResultDTO<{{extractedEntityIdType}}>> LazyLoadSelected{{property.Name}}IdsFor{{entity.Name}}(TableFilterDTO tableFilterDTO)
        {
            return await _{{businessServiceName.FirstCharToLower()}}.LazyLoadSelected{{property.Name}}IdsFor{{entity.Name}}(tableFilterDTO, _context.DbSet<{{extractedEntity.Name}}>().OrderBy(x => x.Id), {{ShouldAuthorizeEntity(entity)}});
        }
""";
        }

        private static string GetManyToManySelectedEntitiesControllerMethod(SpiderProperty property, SpiderClass entity, List<SpiderClass> entities, string businessServiceName)
        {
            SpiderClass extractedEntity = entities.Where(x => x.Name == Helpers.ExtractTypeFromGenericType(property.Type)).SingleOrDefault();

            return $$"""
        [HttpGet]
        [AuthGuard]
        public virtual async Task<List<NamebookDTO<{{extractedEntity.GetIdType(entities)}}>>> Get{{property.Name}}NamebookListFor{{entity.Name}}({{entity.GetIdType(entities)}} id)
        {
            return await _{{businessServiceName.FirstCharToLower()}}.Get{{property.Name}}NamebookListFor{{entity.Name}}(id, false);
        }
""";
        }

        #endregion

        #region One To Many

        private static List<string> GetOrderedOneToManyControllerMethods(SpiderClass entity, List<SpiderClass> entities, string businessServiceName)
        {
            List<string> result = new();

            List<SpiderProperty> uiOrderedOneToManyProperties = Helpers.GetUIOrderedOneToManyProperties(entity);

            foreach (SpiderProperty property in uiOrderedOneToManyProperties)
            {
                result.Add($$"""
        [HttpGet]
        [AuthGuard]
        public virtual async Task<List<{{Helpers.ExtractTypeFromGenericType(property.Type)}}DTO>> GetOrdered{{property.Name}}For{{entity.Name}}({{entity.GetIdType(entities)}} id)
        {
            return await _{{businessServiceName.FirstCharToLower()}}.GetOrdered{{property.Name}}For{{entity.Name}}(id, false);
        }
""");
            }

            return result;
        }

        #endregion

        #region Delete

        private static string GetDeleteControllerMethods(SpiderClass entity, List<SpiderClass> entities, string businessServiceName)
        {
            if (entity.IsReadonlyObject())
                return null;

            return $$"""
        [HttpDelete]
        [AuthGuard]
        public virtual async Task Delete{{entity.Name}}({{entity.GetIdType(entities)}} id)
        {
            await _{{businessServiceName.FirstCharToLower()}}.Delete{{entity.Name}}(id, {{ShouldAuthorizeEntity(entity)}});
        }
""";
        }

        #endregion

        #region Save

        private static string GetSaveControllerMethods(SpiderClass entity, string businessServiceName)
        {
            if (entity.IsReadonlyObject())
                return null;

            return $$"""
        [HttpPut]
        [AuthGuard]
        public virtual async Task<{{entity.Name}}SaveBodyDTO> Save{{entity.Name}}({{entity.Name}}SaveBodyDTO saveBodyDTO)
        {
            return await _{{businessServiceName.FirstCharToLower()}}.Save{{entity.Name}}AndReturnSaveBodyDTO(saveBodyDTO, {{ShouldAuthorizeEntity(entity)}}, {{ShouldAuthorizeEntity(entity)}});
        }
""";
        }

        private static List<string> GetUploadBlobControllerMethods(SpiderClass entity, List<SpiderClass> entities, string businessServiceName)
        {
            List<string> result = new();

            List<SpiderProperty> blobProperies = Helpers.GetBlobProperties(entity.Properties);

            foreach (SpiderProperty property in blobProperies)
            {
                result.Add($$"""
        // FT: You can't upload and delete on every request because you can delete the old image for the user when he refreshes the page
        [HttpPost]
        [AuthGuard]
        public virtual async Task<string> Upload{{property.Name}}For{{entity.Name}}([FromForm] IFormFile file) // FT: It doesn't work without interface
        {
            return await _{{businessServiceName.FirstCharToLower()}}.Upload{{property.Name}}For{{entity.Name}}(file, {{ShouldAuthorizeEntity(entity)}}, {{ShouldAuthorizeEntity(entity)}}); // TODO: Make authorization in business service with override
        }
"""
);
            }

            return result;
        }

        #endregion

        #region Helpers

        private static string ShouldAuthorizeEntity(SpiderClass entity)
        {
            return (!entity.HasDoNotAuthorizeAttribute()).ToString().ToLower();
        }

        private static string GetBusinessServiceClassName(string businessServiceName)
        {
            if (businessServiceName.Contains("Security"))
                return $"{businessServiceName}<UserExtended>";
            else
                return businessServiceName;
        }

        #endregion
    }
}
