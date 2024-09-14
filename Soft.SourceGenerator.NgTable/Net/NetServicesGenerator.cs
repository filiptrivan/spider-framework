using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Soft.SourceGenerator.NgTable.Helpers;
using Soft.SourceGenerators.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Soft.SourceGenerator.NgTable.Net
{
    [Generator]
    public class NetServicesGenerator : IIncrementalGenerator
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

            context.RegisterImplementationSourceOutput(classDeclarations.Collect(),
                static (spc, source) => Execute(source, spc));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="classes">Only EF classes</param>
        /// <param name="context"></param>
        private static void Execute(IList<ClassDeclarationSyntax> entityClasses, SourceProductionContext context)
        {
            if (entityClasses.Count() == 0) return;
            List<ClassDeclarationSyntax> uninheritedEntityClasses = Helper.GetUninheritedClasses(entityClasses);

            StringBuilder sb = new StringBuilder();

            string[] namespacePartsWithoutLastElement = Helper.GetNamespacePartsWithoutLastElement(entityClasses[0]);

            string basePartOfNamespace = string.Join(".", namespacePartsWithoutLastElement); // eg. Soft.Generator.Security
            string projectName = namespacePartsWithoutLastElement[namespacePartsWithoutLastElement.Length - 1]; // eg. Security

            sb.AppendLine($$"""
using {{basePartOfNamespace}}.ValidationRules;
using {{basePartOfNamespace}}.DataMappers;
using {{basePartOfNamespace}}.DTO;
using {{basePartOfNamespace}}.Entities;
using Microsoft.EntityFrameworkCore;
using Soft.NgTable.Models;
using System.Data;
using Soft.SourceGenerator.NgTable;
using Soft.SourceGenerator.ExcelProperties;
using Soft.Generator.Shared.Excel;
using Soft.Generator.Shared.Interfaces;
using FluentValidation;
using Soft.Generator.Shared.Services;
using Soft.Generator.Shared.DTO;
using Soft.Generator.Shared.Extensions;

namespace {{basePartOfNamespace}}.Services
{
    public class {{projectName}}BusinessServiceGenerated : BusinessServiceBase
    {
        private readonly IApplicationDbContext _context;
        private readonly ExcelService _excelService;

        public {{projectName}}BusinessServiceGenerated(IApplicationDbContext context, ExcelService excelService)
        : base(context)
        {
            _context=context;
            _excelService = excelService;
        }
""");
            foreach (ClassDeclarationSyntax c in uninheritedEntityClasses)
            {
                string nameOfTheEntityClass = c.Identifier.Text;
                string nameOfTheEntityClassFirstLower = c.Identifier.Text.FirstCharToLower();
                string idTypeOfTheEntityClass = Helper.GetGenericIdType(c, entityClasses);
                string displayNameProperty = Helper.GetDisplayNamePropForClass(c, entityClasses);

                sb.AppendLine($$"""
        public async Task<{{nameOfTheEntityClass}}DTO> Get{{nameOfTheEntityClass}}DTOAsync({{idTypeOfTheEntityClass}} id)
        {
            return await _context.WithTransactionAsync(async () =>
            {
                return await _context.DbSet<{{nameOfTheEntityClass}}>().AsNoTracking().Where(x => x.Id == id).ProjectTo().FirstOrDefaultAsync();
            });
        }

        public async Task<BasePaginationResult<{{nameOfTheEntityClass}}>> Load{{nameOfTheEntityClass}}ListForPagination(TableFilterDTO tableFilterPayload)
        {
            return await _context.WithTransactionAsync(async () =>
            {
                IQueryable<{{nameOfTheEntityClass}}> query = _context.DbSet<{{nameOfTheEntityClass}}>().AsNoTracking();
                return await TableFilterQueryable.Build(query, tableFilterPayload);
            });
        }

        public async Task<BaseTableResponseEntity<{{nameOfTheEntityClass}}DTO>> Load{{nameOfTheEntityClass}}ListForTable(TableFilterDTO tableFilterPayload)
        {
            BasePaginationResult<{{nameOfTheEntityClass}}> paginationResult = new BasePaginationResult<{{nameOfTheEntityClass}}>();
            List<{{nameOfTheEntityClass}}DTO> data = null;

            await _context.WithTransactionAsync(async () =>
            {
                paginationResult = await Load{{nameOfTheEntityClass}}ListForPagination(tableFilterPayload);

                data = await paginationResult.Query
                    .Skip(tableFilterPayload.First)
                    .Take(tableFilterPayload.Rows)
                    .ProjectTo()
                    .ToListAsync();
            });

            return new BaseTableResponseEntity<{{nameOfTheEntityClass}}DTO> { Data = data, TotalRecords = paginationResult.TotalRecords };
        }

        public async Task<byte[]> Export{{nameOfTheEntityClass}}ListToExcel(TableFilterDTO tableFilterPayload)
        {
            BasePaginationResult<{{nameOfTheEntityClass}}> paginationResult = new BasePaginationResult<{{nameOfTheEntityClass}}>();
            List<{{nameOfTheEntityClass}}DTO> data = null;

            await _context.WithTransactionAsync(async () =>
            {
                paginationResult = await Load{{nameOfTheEntityClass}}ListForPagination(tableFilterPayload);

                data = await paginationResult.Query.ExcelProjectTo().ToListAsync();
            });

            string[] excelPropertiesToExclude = ExcelPropertiesToExclude.GetHeadersToExclude(new {{nameOfTheEntityClass}}DTO());
            return _excelService.FillReportTemplate<{{nameOfTheEntityClass}}DTO>(data, paginationResult.TotalRecords, excelPropertiesToExclude).ToArray();
        }

        protected virtual void OnBefore{{nameOfTheEntityClass}}IsMapped({{nameOfTheEntityClass}}DTO dto) { }

        public async Task<{{nameOfTheEntityClass}}> Save{{nameOfTheEntityClass}}AndReturnDomainAsync({{nameOfTheEntityClass}}DTO dto)
        {
            {{nameOfTheEntityClass}}DTOValidationRules validationRules = new {{nameOfTheEntityClass}}DTOValidationRules();
            validationRules.ValidateAndThrow(dto);

            {{nameOfTheEntityClass}} poco = null;
            await _context.WithTransactionAsync(async () =>
            {
                OnBefore{{nameOfTheEntityClass}}IsMapped(dto);
                DbSet<{{nameOfTheEntityClass}}> dbSet = _context.DbSet<{{nameOfTheEntityClass}}>();
""");
                if (c.IsEntityBusinessObject() == false)
                {
                    sb.AppendLine($$"""
                poco = Mapper.Map(dto);
                await dbSet.AddAsync(poco);
""");
                }
                else
                {
                    sb.AppendLine($$"""
                if (dto.Id > 0)
                {
                    poco = await LoadInstanceAsync<{{nameOfTheEntityClass}}, {{idTypeOfTheEntityClass}}>(dto.Id, dto.Version);
                    Mapper.MergeMap(dto, poco);
                    dbSet.Update(poco);
                }
                else
                {
                    poco = Mapper.Map(dto);
                    await dbSet.AddAsync(poco);
                }
""");
                }
                sb.AppendLine($$"""
{{string.Join("\n", GetManyToOneInstancesForSave(c, entityClasses))}}

                await _context.SaveChangesAsync();
            });

            return poco;
        }

        public async Task<{{nameOfTheEntityClass}}DTO> Save{{nameOfTheEntityClass}}AndReturnDTOAsync({{nameOfTheEntityClass}}DTO dto)
        {
            return await _context.WithTransactionAsync(async () =>
            {
                {{nameOfTheEntityClass}} poco = await Save{{nameOfTheEntityClass}}AndReturnDomainAsync(dto);

                return Mapper.Map(poco);
            });
        }

        public async Task<List<NamebookDTO<{{idTypeOfTheEntityClass}}>>> Load{{nameOfTheEntityClass}}ListForAutocomplete(int limit, string query, IQueryable<{{nameOfTheEntityClass}}> {{nameOfTheEntityClassFirstLower}}Query)
        {
            return await _context.WithTransactionAsync(async () =>
            {
                if (!string.IsNullOrEmpty(query))
                    {{nameOfTheEntityClassFirstLower}}Query = {{nameOfTheEntityClassFirstLower}}Query.Where(x => x.{{displayNameProperty}}.Contains(query));

                return await {{nameOfTheEntityClassFirstLower}}Query
                    .Take(limit)
                    .Select(x => new NamebookDTO<{{idTypeOfTheEntityClass}}>
                    {
                        Id = x.Id,
                        DisplayName = x.{{displayNameProperty}},
                    })
                    .ToListAsync();
            });
        }

        public async Task<List<NamebookDTO<{{idTypeOfTheEntityClass}}>>> Load{{nameOfTheEntityClass}}ListForDropdown(IQueryable<{{nameOfTheEntityClass}}> {{nameOfTheEntityClassFirstLower}}Query)
        {
            return await _context.WithTransactionAsync(async () =>
            {
                return await {{nameOfTheEntityClassFirstLower}}Query
                    .Select(x => new NamebookDTO<{{idTypeOfTheEntityClass}}>
                    {
                        Id = x.Id,
                        DisplayName = x.{{displayNameProperty}},
                    })
                    .ToListAsync();
            });
        }

{{string.Join("\n", GetEnumerableGeneratedMethods(c, entityClasses))}}

""");
            }

            sb.AppendLine($$"""
    }
}
""");

            context.AddSource($"{projectName}BusinessService.generated", SourceText.From(sb.ToString(), Encoding.UTF8));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="c"></param>
        /// <param name="classes">Used just to pass to GetIdTypeOfManyToOneProperty method</param>
        /// <returns></returns>
        static List<string> GetManyToOneInstancesForSave(ClassDeclarationSyntax c, IList<ClassDeclarationSyntax> classes)
        {
            List<string> result = new List<string>();
            List<Prop> properties = c.Members.OfType<PropertyDeclarationSyntax>()
                .Select(prop => new Prop()
                {
                    Type = prop.Type.ToString(),
                    IdentifierText = prop.Identifier.Text
                })
                .Where(prop => prop.Type.PropTypeIsManyToOne())
                .ToList();

            foreach (Prop prop in properties)
            {
                result.Add($$"""
            if (dto.{{prop.IdentifierText}}Id > 0)
                poco.{{prop.IdentifierText}} = await LoadInstanceAsync<{{prop.Type}}, {{GetIdTypeOfManyToOneProperty(prop.Type, classes)}}>(dto.{{prop.IdentifierText}}Id.Value, null);
""");
            }

            return result;
        }

        static List<string> GetEnumerableGeneratedMethods(ClassDeclarationSyntax c, IList<ClassDeclarationSyntax> classes)
        {
            string nameOfTheEntityClass = c.Identifier.Text; // User
            string nameOfTheEntityClassFirstLower = c.Identifier.Text.FirstCharToLower(); // user
            string idTypeOfTheEntityClass = Helper.GetGenericIdType(c, classes); // long

            List<string> result = new List<string>();
            List<Prop> properties = c.Members.OfType<PropertyDeclarationSyntax>()
                .Select(prop => new Prop()
                {
                    Type = prop.Type.ToString(),
                    IdentifierText = prop.Identifier.Text
                })
                .Where(prop => prop.Type.IsEnumerable())
                .ToList();

            foreach (Prop prop in properties) // List<Role> / Roles
            {
                string classNameFromTheList = GetClassNameFromTheList(prop.Type); // Role
                ClassDeclarationSyntax classFromTheList = classes.Where(x => x.Identifier.Text == classNameFromTheList).SingleOrDefault(); // Role
                if (classFromTheList == null)
                    result.Add("INVALID ENTITY CLASS, YOU CAN'T MAKE LIST OF NO ENTITY CLASS");
                string classNameFromTheListDisplayNameProp = Helper.GetDisplayNamePropForClass(classFromTheList, classes); // Name
                List<Prop> classFromTheListProperties = classFromTheList.Members.OfType<PropertyDeclarationSyntax>()
                    .Select(prop => new Prop()
                    {
                        Type = prop.Type.ToString(),
                        IdentifierText = prop.Identifier.Text
                    })
                    .ToList();
                string classFromTheListIdType = Helper.GetGenericIdType(classFromTheList, classes); // long

                Prop manyToManyProp = classFromTheListProperties.Where(x => x.Type.IsEnumerable() && GetClassNameFromTheList(x.Type) == nameOfTheEntityClass).SingleOrDefault(); // List<User> / Users
                Prop manyToOneProp = classFromTheListProperties.Where(x => x.Type.PropTypeIsManyToOne() && prop.Type == nameOfTheEntityClass).SingleOrDefault(); // User / Userando

                if (manyToOneProp != null)
                {
                    result.Add($$"""
        public async Task<List<NamebookDTO<{{classFromTheListIdType}}>>> Load{{classNameFromTheList}}ListFor{{nameOfTheEntityClass}}({{idTypeOfTheEntityClass}} {{nameOfTheEntityClassFirstLower}}Id)
        {
            return await _context.WithTransactionAsync(async () =>
            {
                return await _context.DbSet<{{classNameFromTheList}}>()
                    .Where(x => x.{{manyToOneProp.IdentifierText}} == {{nameOfTheEntityClassFirstLower}}Id)
                    .Select(x => new NamebookDTO<{{classFromTheListIdType}}>
                    {
                        Id = x.Id,
                        DisplayName = x.{{classNameFromTheListDisplayNameProp}},
                    })
                    .ToListAsync();
            });
        }
""");
                }
                else if (manyToManyProp != null)
                {
                    result.Add($$"""
        public async Task<List<NamebookDTO<{{classFromTheListIdType}}>>> Load{{classNameFromTheList}}ListFor{{nameOfTheEntityClass}}({{idTypeOfTheEntityClass}} {{nameOfTheEntityClassFirstLower}}Id)
        {
            return await _context.WithTransactionAsync(async () =>
            {
                return await _context.DbSet<{{classNameFromTheList}}>()
                    .Where(x => x.{{manyToManyProp.IdentifierText}}.Any(x => x.Id == {{nameOfTheEntityClassFirstLower}}Id))
                    .Select(x => new NamebookDTO<{{classFromTheListIdType}}>
                    {
                        Id = x.Id,
                        DisplayName = x.{{classNameFromTheListDisplayNameProp}},
                    })
                    .ToListAsync();
            });
        }
""");
                }
                else
                {
                    if (classFromTheList == null)
                        result.Add("Invalid entity class, you can't have List<Entity> without List<AssociationEntity> or AssociationEntity on the other side.");
                }

            }

            return result;
        }

        static string GetIdTypeOfManyToOneProperty(string propType, IList<ClassDeclarationSyntax> classes)
        {
            ClassDeclarationSyntax manyToOneclass = classes.Where(x => x.Identifier.Text == propType).SingleOrDefault();
            if (manyToOneclass == null)
                return "THERE IS NO MANY TO ONE CLASS FOR THE PROPERTY";

            return Helper.GetGenericIdType(manyToOneclass, classes);
        }

        static string GetClassNameFromTheList(string propType)
        {
            string[] parts = propType.Split('<');
            return parts[parts.Length-1].Replace(">", "");
        }

    }
}