using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Soft.SourceGenerator.NgTable.Helpers;
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

            List<SoftClass> projectClasses = Helper.GetSoftClasses(classes);

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
    public class {{referencedProjectEntityGroupedClasses.Key}}BaseController : SoftControllerBase
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

                result.Add($$"""
        #region {{referencedProjectEntityClass.Name}}

        #region Read

        [HttpPost]
        [AuthGuard]
        public virtual async Task<TableResponseDTO<{{referencedProjectEntityClass.Name}}DTO>> Load{{referencedProjectEntityClass.Name}}TableData(TableFilterDTO tableFilterDTO)
        {
            return await _{{businessServiceName.FirstCharToLower()}}.Load{{referencedProjectEntityClass.Name}}TableData(tableFilterDTO, _context.DbSet<{{referencedProjectEntityClass.Name}}>(), false);
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
            return await _{{businessServiceName.FirstCharToLower()}}.Load{{referencedProjectEntityClass.Name}}DTOList(_context.DbSet<{{referencedProjectEntityClass.Name}}>(), false);
        }

        [HttpGet]
        [AuthGuard]
        public virtual async Task<{{referencedProjectEntityClass.Name}}DTO> Get{{referencedProjectEntityClass.Name}}(int id)
        {
            return await _{{businessServiceName.FirstCharToLower()}}.Get{{referencedProjectEntityClass.Name}}DTOAsync(id, false);
        }

{{string.Join("\n\n", GetOneToManyControllerMethods(referencedProjectEntityClass, referencedProjectEntityClasses, businessServiceName))}}

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

        private static List<string> GetOneToManyControllerMethods(SoftClass referencedProjectEntityClass, List<SoftClass> referencedProjectEntityClasses, string businessServiceName)
        {
            List<string> result = new List<string>();

            foreach (SoftProperty manyToOneProperty in referencedProjectEntityClass.Properties.Where(x => x.Type.IsManyToOneType()))
            {
                SoftClass manyToOnePropertyClass = referencedProjectEntityClasses.Where(x => x.Name == manyToOneProperty.Type).SingleOrDefault();
                string manyToOnePropertyIdType = Helper.GetIdType(manyToOnePropertyClass, referencedProjectEntityClasses);

                //if (manyToOneProperty.IsAutocomplete())
                //{
                result.Add($$"""
        [HttpGet]
        [AuthGuard]
        public virtual async Task<List<NamebookDTO<{{manyToOnePropertyIdType}}>>> Load{{manyToOneProperty.IdentifierText}}ListForAutocomplete(int limit, string query)
        {
            return await _{{businessServiceName.FirstCharToLower()}}.Load{{manyToOneProperty.Type}}ListForAutocomplete(limit, query, _context.DbSet<{{manyToOneProperty.Type}}>());
        }
""");
                //}

                //if (manyToOneProperty.IsDropdown())
                //{
                result.Add($$"""
        [HttpGet]
        [AuthGuard]
        public virtual async Task<List<NamebookDTO<{{manyToOnePropertyIdType}}>>> Load{{manyToOneProperty.IdentifierText}}ListForDropdown()
        {
            return await _{{businessServiceName.FirstCharToLower()}}.Load{{manyToOneProperty.Type}}ListForDropdown(_context.DbSet<{{manyToOneProperty.Type}}>(), false);
        }
""");
                //}
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
        public virtual async Task<{{entity.Name}}DTO> Save{{entity.Name}}({{entity.Name}}DTO {{entity.Name.FirstCharToLower()}}DTO)
        {
            return await _{{businessServiceName.FirstCharToLower()}}.Save{{entity.Name}}AndReturnDTOAsync({{entity.Name.FirstCharToLower()}}DTO, false, false);
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
        public async Task<string> Upload{{entity.Name}}{{property.IdentifierText}}([FromForm] IFormFile file) // FT: It doesn't work without interface
        {
            return await _{{businessServiceName.FirstCharToLower()}}.Upload{{entity.Name}}{{property.IdentifierText}}Async(file); // TODO: Make authorization in business service with override
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
