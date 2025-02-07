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

            var allClasses = classDeclarations.Collect()
                .Combine(referencedProjectClasses);

            context.RegisterImplementationSourceOutput(allClasses, static (spc, source) => Execute(source.Left, source.Right, spc));
        }

        private static void Execute(IList<ClassDeclarationSyntax> classes, List<SpiderClass> referencedProjectEntityClassesAndServices, SourceProductionContext context)
        {
            if (classes.Count < 1)
                return;

            bool shouldGenerate = Helpers.ShouldStartGenerator(nameof(ControllerGenerator), classes);

            if (shouldGenerate == false)
                return;

            List<SpiderClass> currentProjectClasses = Helpers.GetSpiderClasses(classes, referencedProjectEntityClassesAndServices);
            List<SpiderClass> customControllers = currentProjectClasses.Where(x => x.Namespace.EndsWith(".Controllers")).ToList();
            List<SpiderClass> referencedProjectEntities = referencedProjectEntityClassesAndServices.Where(x => x.Namespace.EndsWith(".Entities")).ToList();
            List<SpiderClass> referencedProjectServices = referencedProjectEntityClassesAndServices.Where(x => x.Namespace.EndsWith(".Services")).ToList();
            List<SpiderClass> allEntities = currentProjectClasses.Concat(referencedProjectEntities).ToList();

            string[] namespacePartsWithoutLastElement = Helpers.GetNamespacePartsWithoutLastElement(currentProjectClasses[0].Namespace);

            string basePartOfTheNamespace = string.Join(".", namespacePartsWithoutLastElement); // eg. PlayertyLoyals.Infrastructure
            //string projectName = namespacePartsWithoutLastElement[namespacePartsWithoutLastElement.Length - 1]; // eg. Infrastructure
            string projectName = namespacePartsWithoutLastElement[0]; // eg. PlayertyLoyals

            string result = $$"""
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.AspNetCore.Mvc;
using Azure.Storage.Blobs;
using System.Data;
using Spider.Infrastructure;
using Spider.Shared.Helpers;
using Spider.Shared.Attributes;
using Spider.Shared.Interfaces;
using {{projectName}}.Shared.Resources;
using {{projectName}}.Business.Entities;
using {{projectName}}.Business.DTO;
{{string.Join("\n", Helpers.GetEntityClassesUsings(allEntities))}}
{{string.Join("\n", Helpers.GetDTOClassesUsings(allEntities))}}

namespace {{basePartOfTheNamespace}}.Controllers
{
{{string.Join("\n\n", GetControllerClasses(referencedProjectEntities, referencedProjectServices))}}
}
""";

            context.AddSource($"{projectName}BaseControllers.generated", SourceText.From(result, Encoding.UTF8));
        }

        public static List<string> GetControllerClasses(List<SpiderClass> referencedProjectEntityClasses, List<SpiderClass> referencedProjectServices)
        {
            List<string> result = new List<string>();

            foreach (IGrouping<string, SpiderClass> referencedProjectEntityGroupedClasses in referencedProjectEntityClasses.GroupBy(x => x.ControllerName))
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

{{string.Join("\n\n", GetControllerMethods(referencedProjectEntityGroupedClasses.ToList(), referencedProjectEntityClasses, servicesNamespace, businessServiceName))}}

    }
""");
            }

            return result;
        }

        private static List<string> GetControllerMethods(List<SpiderClass> referencedProjectEntityGroupedClasses, List<SpiderClass> referencedProjectEntityClasses, string servicesNamespace, string businessServiceName)
        {
            List<string> result = new List<string>();

            foreach (SpiderClass referencedProjectEntityClass in referencedProjectEntityGroupedClasses)
            {
                if (referencedProjectEntityClass.IsManyToMany()) // TODO FT: Do something with M2M entities
                    continue;

                string referencedProjectEntityClassIdType = referencedProjectEntityClass.GetIdType(referencedProjectEntityClasses);

                result.Add($$"""
        #region {{referencedProjectEntityClass.Name}}

        #region Read

        [HttpPost]
        [AuthGuard]
        public virtual async Task<TableResponseDTO<{{referencedProjectEntityClass.Name}}DTO>> Get{{referencedProjectEntityClass.Name}}TableData(TableFilterDTO tableFilterDTO)
        {
            return await _{{businessServiceName.FirstCharToLower()}}.Get{{referencedProjectEntityClass.Name}}TableData(tableFilterDTO, _context.DbSet<{{referencedProjectEntityClass.Name}}>(), false);
        }

        [HttpPost]
        [AuthGuard]
        public virtual async Task<IActionResult> Export{{referencedProjectEntityClass.Name}}TableDataToExcel(TableFilterDTO tableFilterDTO)
        {
            byte[] fileContent = await _{{businessServiceName.FirstCharToLower()}}.Export{{referencedProjectEntityClass.Name}}TableDataToExcel(tableFilterDTO, _context.DbSet<{{referencedProjectEntityClass.Name}}>(), false);
            return File(fileContent, SettingsProvider.Current.ExcelContentType, Uri.EscapeDataString($"{TermsGenerated.{{referencedProjectEntityClass.Name}}ExcelExportName}.xlsx"));
        }

        [HttpGet]
        [AuthGuard]
        public virtual async Task<List<{{referencedProjectEntityClass.Name}}DTO>> Get{{referencedProjectEntityClass.Name}}List()
        {
            return await _{{businessServiceName.FirstCharToLower()}}.Get{{referencedProjectEntityClass.Name}}DTOList(_context.DbSet<{{referencedProjectEntityClass.Name}}>(), false);
        }

        [HttpGet]
        [AuthGuard]
        public virtual async Task<{{referencedProjectEntityClass.Name}}DTO> Get{{referencedProjectEntityClass.Name}}({{referencedProjectEntityClassIdType}} id)
        {
            return await _{{businessServiceName.FirstCharToLower()}}.Get{{referencedProjectEntityClass.Name}}DTOAsync(id, false);
        }

        [HttpGet]
        [AuthGuard]
        public virtual async Task<List<NamebookDTO<{{referencedProjectEntityClassIdType}}>>> Get{{referencedProjectEntityClass.Name}}ListForAutocomplete(int limit, string query)
        {
            return await _{{businessServiceName.FirstCharToLower()}}.Get{{referencedProjectEntityClass.Name}}ListForAutocomplete(limit, query, _context.DbSet<{{referencedProjectEntityClass.Name}}>());
        }

        [HttpGet]
        [AuthGuard]
        public virtual async Task<List<NamebookDTO<{{referencedProjectEntityClassIdType}}>>> Get{{referencedProjectEntityClass.Name}}ListForDropdown()
        {
            return await _{{businessServiceName.FirstCharToLower()}}.Get{{referencedProjectEntityClass.Name}}ListForDropdown(_context.DbSet<{{referencedProjectEntityClass.Name}}>(), false);
        }

{{string.Join("\n\n", GetOrderedOneToManyControllerMethods(referencedProjectEntityClass, referencedProjectEntityClasses, businessServiceName))}}

{{string.Join("\n\n", GetManyToManyControllerMethods(referencedProjectEntityClass, referencedProjectEntityClasses, businessServiceName))}}

        #endregion

        #region Save

{{GetSaveControllerMethods(referencedProjectEntityClass, businessServiceName)}}

{{string.Join("\n\n", GetUploadBlobControllerMethods(referencedProjectEntityClass, referencedProjectEntityClasses, businessServiceName))}}

        #endregion

        #region Delete

{{GetDeleteControllerMethods(referencedProjectEntityClass, referencedProjectEntityClasses, businessServiceName)}}

        #endregion

        #endregion
""");
            }

            return result;
        }

        private static List<string> GetManyToManyControllerMethods(SpiderClass referencedProjectEntityClass, List<SpiderClass> referencedProjectEntityClasses, string businessServiceName)
        {
            List<string> result = new List<string>();

            foreach (SpiderProperty property in referencedProjectEntityClass.Properties)
            {
                if (property.IsMultiSelectControlType() ||
                    property.IsMultiAutocompleteControlType())
                {
                    result.Add(GetManyToManySelectedEntitiesControllerMethod(property, referencedProjectEntityClass, referencedProjectEntityClasses, businessServiceName));
                }
                else if (property.HasSimpleManyToManyTableLazyLoadAttribute())
                {
                    result.Add(GetSimpleManyToManyTableLazyLoadControllerMethod(property, referencedProjectEntityClass, referencedProjectEntityClasses, businessServiceName));
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
            return await _{{businessServiceName.FirstCharToLower()}}.LazyLoadSelected{{property.Name}}IdsFor{{entity.Name}}(tableFilterDTO, _context.DbSet<{{extractedEntity.Name}}>().OrderBy(x => x.Id));
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

        private static List<string> GetOrderedOneToManyControllerMethods(SpiderClass entity, List<SpiderClass> entities, string businessServiceName)
        {
            List<string> result = new List<string>();

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

        private static string GetDeleteControllerMethods(SpiderClass entity, List<SpiderClass> entities, string businessServiceName)
        {
            if (entity.IsReadonlyObject())
                return null;

            return $$"""
        [HttpDelete]
        [AuthGuard]
        public virtual async Task Delete{{entity.Name}}({{entity.GetIdType(entities)}} id)
        {
            await _{{businessServiceName.FirstCharToLower()}}.Delete{{entity.Name}}Async(id, false);
        }
""";
        }

        private static string GetSaveControllerMethods(SpiderClass entity, string businessServiceName)
        {
            if (entity.IsReadonlyObject())
                return null;

            return $$"""
        [HttpPut]
        [AuthGuard]
        public virtual async Task<{{entity.Name}}SaveBodyDTO> Save{{entity.Name}}({{entity.Name}}SaveBodyDTO saveBodyDTO)
        {
            return await _{{businessServiceName.FirstCharToLower()}}.Save{{entity.Name}}AndReturnSaveBodyDTOAsync(saveBodyDTO, false, false);
        }
""";
        }

        private static List<string> GetUploadBlobControllerMethods(SpiderClass entity, List<SpiderClass> entities, string businessServiceName)
        {
            List<string> result = new List<string>();

            List<SpiderProperty> blobProperies = Helpers.GetBlobProperties(entity.Properties);

            foreach (SpiderProperty property in blobProperies)
            {
                result.Add($$"""
        // FT: You can't upload and delete on every request because you can delete the old image for the user when he refreshes the page
        [HttpPost]
        [AuthGuard]
        public virtual async Task<string> Upload{{property.Name}}For{{entity.Name}}([FromForm] IFormFile file) // FT: It doesn't work without interface
        {
            return await _{{businessServiceName.FirstCharToLower()}}.Upload{{property.Name}}For{{entity.Name}}Async(file); // TODO: Make authorization in business service with override
        }
"""
);
            }

            return result;
        }

        private static string GetBusinessServiceClassName(string businessServiceName)
        {
            if (businessServiceName.Contains("Security"))
                return $"{businessServiceName}<UserExtended>";
            else
                return businessServiceName;
        }
    }
}
