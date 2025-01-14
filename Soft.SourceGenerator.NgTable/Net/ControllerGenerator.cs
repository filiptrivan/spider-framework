using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Soft.SourceGenerator.NgTable.Helpers;
using Soft.SourceGenerators.Enums;
using Soft.SourceGenerators.Helpers;
using Soft.SourceGenerators.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Soft.SourceGenerators.Net
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
                    predicate: static (s, _) => Helper.IsSyntaxTargetForGenerationEveryClass(s),
                    transform: static (ctx, _) => Helper.GetSemanticTargetForGenerationEveryClass(ctx))
                .Where(static c => c is not null);

            IncrementalValueProvider<List<SoftClass>> referencedProjectClasses = Helper.GetIncrementalValueProviderClassesFromReferencedAssemblies(context,
                new List<NamespaceExtensionCodes>
                {
                    NamespaceExtensionCodes.Entities,
                    NamespaceExtensionCodes.Services
                });

            var allClasses = classDeclarations.Collect()
                .Combine(referencedProjectClasses);

            context.RegisterImplementationSourceOutput(allClasses, static (spc, source) => Execute(source.Left, source.Right, spc));
        }

        private static void Execute(IList<ClassDeclarationSyntax> classes, List<SoftClass> referencedProjectEntityClassesAndServices, SourceProductionContext context)
        {
            if (classes.Count < 1)
                return;

            bool shouldGenerate = Helper.ShouldStartGenerator(nameof(ControllerGenerator), classes);

            if (shouldGenerate == false)
                return;

            List<SoftClass> projectClasses = Helper.GetSoftClasses(classes, referencedProjectEntityClassesAndServices);

            List<SoftClass> customControllers = projectClasses.Where(x => x.Namespace.EndsWith(".Controllers")).ToList();

            List<SoftClass> referencedProjectEntityClasses = referencedProjectEntityClassesAndServices.Where(x => x.Namespace.EndsWith(".Entities")).ToList();

            List<SoftClass> referencedProjectServices = referencedProjectEntityClassesAndServices.Where(x => x.Namespace.EndsWith(".Services")).ToList();

            List<SoftClass> allEntityClasses = projectClasses.Concat(referencedProjectEntityClasses).ToList();

            string[] namespacePartsWithoutLastElement = Helper.GetNamespacePartsWithoutLastElement(projectClasses[0].Namespace);

            string basePartOfTheNamespace = string.Join(".", namespacePartsWithoutLastElement); // eg. PlayertyLoyals.Infrastructure
            //string projectName = namespacePartsWithoutLastElement[namespacePartsWithoutLastElement.Length - 1]; // eg. Infrastructure
            string projectName = namespacePartsWithoutLastElement[0]; // eg. PlayertyLoyals

            string result = $$"""
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.AspNetCore.Mvc;
using Azure.Storage.Blobs;
using System.Data;
using Soft.Generator.Infrastructure;
using Soft.Generator.Shared.Helpers;
using Soft.Generator.Shared.DTO;
using Soft.Generator.Shared.Attributes;
using Soft.Generator.Shared.Interfaces;
using {{projectName}}.Shared.Terms;
{{string.Join("\n", Helper.GetEntityClassesUsings(referencedProjectEntityClasses))}}
{{string.Join("\n", Helper.GetDTOClassesUsings(referencedProjectEntityClasses))}}

namespace {{basePartOfTheNamespace}}.Controllers
{
{{string.Join("\n\n", GetControllerClasses(referencedProjectEntityClasses, referencedProjectServices))}}
}
""";

            context.AddSource($"{projectName}BaseControllers.generated", SourceText.From(result, Encoding.UTF8));
        }

        public static List<string> GetControllerClasses(List<SoftClass> referencedProjectEntityClasses, List<SoftClass> referencedProjectServices)
        {
            List<string> result = new List<string>();

            foreach (IGrouping<string, SoftClass> referencedProjectEntityGroupedClasses in referencedProjectEntityClasses.GroupBy(x => x.ControllerName))
            {
                string servicesNamespace = referencedProjectEntityGroupedClasses.FirstOrDefault().Namespace.Replace(".Entities", ".Services");
                SoftClass businessServiceClass = referencedProjectServices
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
    public class {{referencedProjectEntityGroupedClasses.Key}}BaseController : SoftBaseController
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

        private static List<string> GetControllerMethods(List<SoftClass> referencedProjectEntityGroupedClasses, List<SoftClass> referencedProjectEntityClasses, string servicesNamespace, string businessServiceName)
        {
            List<string> result = new List<string>();

            foreach (SoftClass referencedProjectEntityClass in referencedProjectEntityGroupedClasses)
            {
                if (referencedProjectEntityClass.IsManyToMany()) // TODO FT: Do something with M2M entities
                    continue;

                string referencedProjectEntityClassIdType = Helper.GetIdType(referencedProjectEntityClass, referencedProjectEntityClasses);

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

{{GetDeleteControllerMethods(referencedProjectEntityClass, businessServiceName)}}

        #endregion

        #endregion
""");
            }

            return result;
        }

        private static List<string> GetManyToManyControllerMethods(SoftClass referencedProjectEntityClass, List<SoftClass> referencedProjectEntityClasses, string businessServiceName)
        {
            List<string> result = new List<string>();

            foreach (SoftProperty property in referencedProjectEntityClass.Properties)
            {
                if (property.IsMultiSelectControlType() ||
                    property.IsMultiAutocompleteControlType())
                {
                    result.Add(GetManyToManySelectedEntitiesControllerMethod(property, referencedProjectEntityClass, referencedProjectEntityClasses, businessServiceName));
                }
            }

            return result;
        }

        private static string GetManyToManySelectedEntitiesControllerMethod(SoftProperty property, SoftClass entity, List<SoftClass> entities, string businessServiceName)
        {
            SoftClass extractedEntity = entities.Where(x => x.Name == Helper.ExtractTypeFromGenericType(property.Type)).SingleOrDefault();

            return $$"""
        [HttpGet]
        [AuthGuard]
        public async Task<List<NamebookDTO<{{Helper.GetIdType(extractedEntity, entities)}}>>> Get{{property.Name}}NamebookListFor{{entity.Name}}({{Helper.GetIdType(entity, entities)}} id)
        {
            return await _{{businessServiceName.FirstCharToLower()}}.Get{{property.Name}}NamebookListFor{{entity.Name}}(id, false);
        }
""";
        }

        private static List<string> GetOrderedOneToManyControllerMethods(SoftClass entity, List<SoftClass> entities, string businessServiceName)
        {
            List<string> result = new List<string>();

            List<SoftProperty> uiOrderedOneToManyProperties = Helper.GetUIOrderedOneToManyProperties(entity);

            foreach (SoftProperty property in uiOrderedOneToManyProperties)
            {
                result.Add($$"""
        [HttpGet]
        [AuthGuard]
        public async Task<List<{{Helper.ExtractTypeFromGenericType(property.Type)}}DTO>> GetOrdered{{property.Name}}For{{entity.Name}}(int id)
        {
            return await _{{businessServiceName.FirstCharToLower()}}.GetOrdered{{property.Name}}For{{entity.Name}}(id, false);
        }
""");
            }

            return result;
        }

        private static string GetDeleteControllerMethods(SoftClass entity, string businessServiceName)
        {
            if (entity.IsReadonlyObject())
                return null;

            return $$"""
        [HttpDelete]
        [AuthGuard]
        public virtual async Task Delete{{entity.Name}}(int id)
        {
            await _{{businessServiceName.FirstCharToLower()}}.Delete{{entity.Name}}Async(id, false);
        }
""";
        }

        private static string GetSaveControllerMethods(SoftClass entity, string businessServiceName)
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

        private static List<string> GetUploadBlobControllerMethods(SoftClass entity, List<SoftClass> entities, string businessServiceName)
        {
            List<string> result = new List<string>();

            List<SoftProperty> blobProperies = Helper.GetBlobProperties(entity.Properties);

            foreach (SoftProperty property in blobProperies)
            {
                result.Add($$"""
        // FT: You can't upload and delete on every request because you can delete the old image for the user when he refreshes the page
        [HttpPost]
        [AuthGuard]
        public async Task<string> Upload{{property.Name}}For{{entity.Name}}([FromForm] IFormFile file) // FT: It doesn't work without interface
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
