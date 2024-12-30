using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Soft.SourceGenerator.NgTable.Helpers;
using Soft.SourceGenerators.Helpers;
using Soft.SourceGenerators.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Soft.SourceGenerators.Net
{
    [Generator]
    public class ControllerGenerator
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

            IncrementalValueProvider<List<SoftClass>> referencedProjectEntityClasses = Helper.GetEntityClassesFromReferencedAssemblies(context);

            var allClasses = classDeclarations.Collect()
                .Combine(referencedProjectEntityClasses);

            context.RegisterImplementationSourceOutput(allClasses, static (spc, source) => Execute(source.Left, source.Right, spc));
        }

        private static void Execute(IList<ClassDeclarationSyntax> classes, List<SoftClass> referencedProjectEntityClasses, SourceProductionContext context)
        {
            if (classes.Count < 1)
                return;

            bool shouldGenerateController = Helper.ShouldStartGenerator(nameof(ControllerGenerator), classes);

            if (shouldGenerateController == false)
                return;

            List<SoftClass> projectClasses = Helper.GetSoftClasses(classes);

            List<SoftClass> customControllers = projectClasses.Where(x => x.Namespace.EndsWith(".Controllers")).ToList();

            List<SoftClass> allEntityClasses = projectClasses.Concat(referencedProjectEntityClasses).ToList();

            string[] namespacePartsWithoutLastElement = Helper.GetNamespacePartsWithoutLastElement(projectClasses[0].Namespace);

            string basePartOfTheNamespace = string.Join(".", namespacePartsWithoutLastElement); // eg. Playerty.Loyals.Infrastructure
            //string projectName = namespacePartsWithoutLastElement[namespacePartsWithoutLastElement.Length - 1]; // eg. Infrastructure
            string projectName = namespacePartsWithoutLastElement[0]; // eg. Playerty

            string result = $$"""
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Soft.Generator.Infrastructure;
using System.Data;
{{string.Join("\n", Helper.GetEntityClassesUsings(referencedProjectEntityClasses))}}
{{string.Join("\n", Helper.GetDTOClassesUsings(referencedProjectEntityClasses))}}

namespace {{basePartOfTheNamespace}}
{
    {{string.Join("\n\n", GetControllerClasses(referencedProjectEntityClasses))}}
}
""";

            context.AddSource($"{projectName}BaseControllers.generated", SourceText.From(result, Encoding.UTF8));
        }

        public static List<string> GetControllerClasses(List<SoftClass> referencedProjectEntityClasses)
        {
            List<string> result = new List<string>();

            foreach (SoftClass referencedProjectEntityClass in referencedProjectEntityClasses)
            {
                string servicesNamespace = referencedProjectEntityClass.Namespace.Replace(".Entities", ".Services");

                result.Add($$"""
    [ApiController]
    [Route("/api/[controller]/[action]")]
    public class {{referencedProjectEntityClass.Name}}BaseController : SoftControllerBase
    {
        private readonly IApplicationDbContext _context;
        private readonly {{servicesNamespace}}.BusinessService _businessService;
        private readonly BlobContainerClient _blobContainerClient;

        public PartnerController(IApplicationDbContext context, {{servicesNamespace}}BusinessService businessService, BlobContainerClient blobContainerClient)
        {
            _context = context;
            _businessService = businessService;
            _blobContainerClient = blobContainerClient;
        }

        [HttpPost]
        [AuthGuard]
        public async Task<TableResponseDTO<{{referencedProjectEntityClass.Name}}DTO>> Load{{referencedProjectEntityClass.Name}}TableData(TableFilterDTO tableFilterDTO)
        {
            return await _businessService.Load{{referencedProjectEntityClass.Name}}TableData(tableFilterDTO, _context.DbSet<{{referencedProjectEntityClass.Name}}>(), false);
        }

        [HttpPost]
        [AuthGuard]
        public async Task<IActionResult> Export{{referencedProjectEntityClass.Name}}TableDataToExcel(TableFilterDTO tableFilterDTO)
        {
            byte[] fileContent = await _businessService.Export{{referencedProjectEntityClass.Name}}TableDataToExcel(tableFilterDTO, _context.DbSet<{{referencedProjectEntityClass.Name}}>(), false);
            return File(fileContent, SettingsProvider.Current.ExcelContentType, Uri.EscapeDataString($"{Terms.{{referencedProjectEntityClass.Name}}ExcelExportName}.xlsx"));
        }

        [HttpDelete]
        [AuthGuard]
        public async Task Delete{{referencedProjectEntityClass.Name}}(int id)
        {
            await _businessService.Delete{{referencedProjectEntityClass.Name}}Async(id, false);
        }

        [HttpGet]
        [AuthGuard]
        public async Task<{{referencedProjectEntityClass.Name}}DTO> Get{{referencedProjectEntityClass.Name}}(int id)
        {
            return await _businessService.Get{{referencedProjectEntityClass.Name}}DTOAsync(id, false);
        }

        [HttpPut]
        [AuthGuard]
        public async Task<{{referencedProjectEntityClass.Name}}DTO> Save{{referencedProjectEntityClass.Name}}({{referencedProjectEntityClass.Name}}DTO {{referencedProjectEntityClass.Name.FirstCharToLower()}}DTO)
        {
            return await _businessService.Save{{referencedProjectEntityClass.Name}}AndReturnDTOAsync({{referencedProjectEntityClass.Name.FirstCharToLower()}}DTO, false, false);
        }

        [HttpGet]
        [AuthGuard]
        public async Task<List<{{referencedProjectEntityClass.Name}}DTO>> Get{{referencedProjectEntityClass.Name}}List()
        {
            return await _businessService.Load{{referencedProjectEntityClass.Name}}DTOList(_context.DbSet<{{referencedProjectEntityClass.Name}}>(), false);
        }

{{string.Join("\n", GetOneToManyControllerMethods(referencedProjectEntityClass, referencedProjectEntityClasses))}}

    }
""");
            }

            return result;
        }

        private static List<string> GetOneToManyControllerMethods(SoftClass referencedProjectEntityClass, List<SoftClass> referencedProjectEntityClasses)
        {
            List<string> result = new List<string>();

            foreach (SoftProperty manyToOneProperty in referencedProjectEntityClass.Properties.Where(x => x.Type.IsManyToOneType()))
            {
                SoftClass manyToOnePropertyClass = referencedProjectEntityClasses.Where(x => x.Name == manyToOneProperty.Type).SingleOrDefault();
                string manyToOnePropertyIdType = Helper.GetGenericIdType(manyToOnePropertyClass, referencedProjectEntityClasses);

                //if (manyToOneProperty.IsAutocomplete())
                //{
                    result.Add($$"""
        [HttpGet]
        [AuthGuard]
        public async Task<List<NamebookDTO<{{manyToOnePropertyIdType}}>>> Load{{manyToOneProperty.IdentifierText}}ListForAutocomplete(int limit, string query)
        {
            return await _businessService.Load{{manyToOneProperty.Type}}ListForAutocomplete(limit, query, _context.DbSet<{{manyToOneProperty.Type}}>());
        }
""");
                //}

                //if (manyToOneProperty.IsDropdown())
                //{
                    result.Add($$"""
        [HttpGet]
        [AuthGuard]
        public async Task<List<NamebookDTO<{{manyToOnePropertyIdType}}>>> Load{{manyToOneProperty.IdentifierText}}ListForDropdown()
        {
            return await _businessService.Load{{manyToOneProperty.Type}}ListForDropdown(_context.DbSet<{{manyToOneProperty.Type}}>(), false);
        }
""");
                //}
            }

            return result;
        }

    }
}
