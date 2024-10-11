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
            List<ClassDeclarationSyntax> uninheritedEntityClasses = Helper.GetUninheritedClasses(entityClasses);

            StringBuilder sb = new StringBuilder();

            string[] namespacePartsWithoutLastElement = Helper.GetNamespacePartsWithoutLastElement(entityClasses[0]);

            string basePartOfNamespace = string.Join(".", namespacePartsWithoutLastElement); // eg. Soft.Generator.Security
            string projectName = namespacePartsWithoutLastElement[namespacePartsWithoutLastElement.Length - 1]; // eg. Security

            bool generateAuthorizationMethods = projectName != "Security";

            sb.AppendLine($$"""
using {{basePartOfNamespace}}.ValidationRules;
using {{basePartOfNamespace}}.DataMappers;
using {{basePartOfNamespace}}.DTO;
using {{basePartOfNamespace}}.Entities;
using {{basePartOfNamespace}}.Enums;
using {{basePartOfNamespace}}.ExcelProperties;
using {{basePartOfNamespace}}.TableFiltering;
using Microsoft.EntityFrameworkCore;
using System.Data;
using FluentValidation;
using Soft.Generator.Security.Services;
using Soft.Generator.Shared.Excel;
using Soft.Generator.Shared.Interfaces;
using Soft.Generator.Shared.Services;
using Soft.Generator.Shared.DTO;
using Soft.Generator.Shared.Extensions;
using Mapster;

namespace {{basePartOfNamespace}}.Services
{
    public class {{projectName}}BusinessServiceGenerated : BusinessServiceBase
    {
        private readonly IApplicationDbContext _context;
        private readonly ExcelService _excelService;
        private readonly AuthorizationService _authorizationService;

        public {{projectName}}BusinessServiceGenerated(IApplicationDbContext context, ExcelService excelService, AuthorizationService authorizationService)
        : base(context)
        {
            _context=context;
            _excelService = excelService;
            _authorizationService = authorizationService;
        }
""");
            foreach (ClassDeclarationSyntax c in entityClasses)
            {
                if (c.GetBaseType() == null) // FT: Handling many to many, maybe you should do something else in the future
                    continue;
                string nameOfTheEntityClass = c.Identifier.Text;
                string nameOfTheEntityClassFirstLower = c.Identifier.Text.FirstCharToLower();
                string idTypeOfTheEntityClass = Helper.GetGenericIdType(c, entityClasses);
                string displayNameProperty = Helper.GetDisplayNamePropForClass(c, entityClasses);

                sb.AppendLine($$"""
        public async Task<{{nameOfTheEntityClass}}DTO> Get{{nameOfTheEntityClass}}DTOAsync({{idTypeOfTheEntityClass}} id, bool authorize = true)
        {
            return await _context.WithTransactionAsync(async () =>
            {
                if (authorize) 
                {
                    {{(generateAuthorizationMethods ? $"await _authorizationService.AuthorizeAndThrowAsync<UserExtended>(PermissionCodes.Read{nameOfTheEntityClass});" : "")}}
                }

                return await _context.DbSet<{{nameOfTheEntityClass}}>().AsNoTracking().Where(x => x.Id == id).ProjectToType<{{nameOfTheEntityClass}}DTO>(Mapper.{{c.Identifier.Text}}ProjectToConfig()).FirstOrDefaultAsync();
            });
        }

        private async Task<PaginationResult<{{nameOfTheEntityClass}}>> Load{{nameOfTheEntityClass}}ListForPagination(TableFilterDTO tableFilterPayload, IQueryable<{{nameOfTheEntityClass}}> query)
        {
            return await _context.WithTransactionAsync(async () =>
            {
                return await TableFilterQueryable.Build(query.AsNoTracking(), tableFilterPayload);
            });
        }

        public async virtual Task<TableResponseDTO<{{nameOfTheEntityClass}}DTO>> Load{{nameOfTheEntityClass}}ListForTable(TableFilterDTO tableFilterPayload, IQueryable<{{nameOfTheEntityClass}}> query, bool authorize = true)
        {
            PaginationResult<{{nameOfTheEntityClass}}> paginationResult = new PaginationResult<{{nameOfTheEntityClass}}>();
            List<{{nameOfTheEntityClass}}DTO> data = null;

            await _context.WithTransactionAsync(async () =>
            {
                if (authorize) 
                {
                    {{(generateAuthorizationMethods ? $"await _authorizationService.AuthorizeAndThrowAsync<UserExtended>(PermissionCodes.Read{nameOfTheEntityClass});" : "")}}
                }

                paginationResult = await Load{{nameOfTheEntityClass}}ListForPagination(tableFilterPayload, query);

                data = await paginationResult.Query
                    .Skip(tableFilterPayload.First)
                    .Take(tableFilterPayload.Rows)
                    .ProjectToType<{{nameOfTheEntityClass}}DTO>(Mapper.{{c.Identifier.Text}}ProjectToConfig())
                    .ToListAsync();
            });

            return new TableResponseDTO<{{nameOfTheEntityClass}}DTO> { Data = data, TotalRecords = paginationResult.TotalRecords };
        }

        public async Task<byte[]> Export{{nameOfTheEntityClass}}ListToExcel(TableFilterDTO tableFilterPayload, IQueryable<{{nameOfTheEntityClass}}> query, bool authorize = true)
        {
            PaginationResult<{{nameOfTheEntityClass}}> paginationResult = new PaginationResult<{{nameOfTheEntityClass}}>();
            List<{{nameOfTheEntityClass}}DTO> data = null;

            await _context.WithTransactionAsync(async () =>
            {
                if (authorize)
                {
                    {{(generateAuthorizationMethods ? $"await _authorizationService.AuthorizeAndThrowAsync<UserExtended>(PermissionCodes.Read{nameOfTheEntityClass});" : "")}}
                }

                paginationResult = await Load{{nameOfTheEntityClass}}ListForPagination(tableFilterPayload, query);

                data = await paginationResult.Query.ProjectToType<{{nameOfTheEntityClass}}DTO>(Mapper.{{c.Identifier.Text}}ExcelProjectToConfig()).ToListAsync();
            });

            string[] excelPropertiesToExclude = ExcelPropertiesToExclude.GetHeadersToExclude(new {{nameOfTheEntityClass}}DTO());
            return _excelService.FillReportTemplate<{{nameOfTheEntityClass}}DTO>(data, paginationResult.TotalRecords, excelPropertiesToExclude).ToArray();
        }

        {{(c.IsAbstract() ? "" : GetSavingData(nameOfTheEntityClass, idTypeOfTheEntityClass, c, entityClasses, generateAuthorizationMethods))}}
        
        public async virtual Task<List<NamebookDTO<{{idTypeOfTheEntityClass}}>>> Load{{nameOfTheEntityClass}}ListForAutocomplete(int limit, string query, IQueryable<{{nameOfTheEntityClass}}> {{nameOfTheEntityClassFirstLower}}Query, bool authorize = true)
        {
            return await _context.WithTransactionAsync(async () =>
            {
                if (authorize)
                {
                    {{(generateAuthorizationMethods ? $"await _authorizationService.AuthorizeAndThrowAsync<UserExtended>(PermissionCodes.Read{nameOfTheEntityClass});" : "")}}
                }

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

        public async virtual Task<List<NamebookDTO<{{idTypeOfTheEntityClass}}>>> Load{{nameOfTheEntityClass}}ListForDropdown(IQueryable<{{nameOfTheEntityClass}}> {{nameOfTheEntityClassFirstLower}}Query, bool authorize = true)
        {
            return await _context.WithTransactionAsync(async () =>
            {
                if (authorize)
                {
                    {{(generateAuthorizationMethods ? $"await _authorizationService.AuthorizeAndThrowAsync<UserExtended>(PermissionCodes.Read{nameOfTheEntityClass});" : "")}}
                }

                return await {{nameOfTheEntityClassFirstLower}}Query
                    .Select(x => new NamebookDTO<{{idTypeOfTheEntityClass}}>
                    {
                        Id = x.Id,
                        DisplayName = x.{{displayNameProperty}},
                    })
                    .ToListAsync();
            });
        }

        public async Task<List<{{nameOfTheEntityClass}}>> Load{{nameOfTheEntityClass}}List(IQueryable<{{nameOfTheEntityClass}}> {{nameOfTheEntityClassFirstLower}}Query, bool authorize = true)
        {
            return await _context.WithTransactionAsync(async () =>
            {
                if (authorize)
                {
                    {{(generateAuthorizationMethods ? $"await _authorizationService.AuthorizeAndThrowAsync<UserExtended>(PermissionCodes.Read{nameOfTheEntityClass});" : "")}}
                }

                return await {{nameOfTheEntityClassFirstLower}}Query
                    .ToListAsync();
            });
        }

{{string.Join("\n", GetEnumerableGeneratedMethods(c, entityClasses, generateAuthorizationMethods, referencedClassesEntities))}}

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
            List<SoftProperty> properties = c.Members.OfType<PropertyDeclarationSyntax>()
                .Select(prop => new SoftProperty()
                {
                    Type = prop.Type.ToString(),
                    IdentifierText = prop.Identifier.Text
                })
                .Where(prop => prop.Type.PropTypeIsManyToOne())
                .ToList();

            foreach (SoftProperty prop in properties)
            {
                ClassDeclarationSyntax classOfManyToOneProperty = GetClassOfManyToOneProperty(prop.Type, classes);

                if (classOfManyToOneProperty == null)
                    continue;

                if (classOfManyToOneProperty.IsEntityBusinessObject() || classOfManyToOneProperty.IsEntityReadonlyObject() == false)
                {
                    result.Add($$"""
            if (dto.{{prop.IdentifierText}}Id > 0)
                poco.{{prop.IdentifierText}} = await LoadInstanceAsync<{{prop.Type}}, {{GetIdTypeOfManyToOneProperty(prop.Type, classes)}}>(dto.{{prop.IdentifierText}}Id.Value, null);
""");
                }
                else
                {
                    result.Add($$"""
            if (dto.{{prop.IdentifierText}}Id > 0)
                poco.{{prop.IdentifierText}} = await LoadInstanceAsync<{{prop.Type}}, {{GetIdTypeOfManyToOneProperty(prop.Type, classes)}}>(dto.{{prop.IdentifierText}}Id.Value);
""");
                }
            }

            return result;
        }

        static List<string> GetEnumerableGeneratedMethods(ClassDeclarationSyntax entityClass, IList<ClassDeclarationSyntax> classes, bool generateAuthorizationMethods, IEnumerable<INamedTypeSymbol> referencedClassesEntities)
        {
            string nameOfTheEntityClass = entityClass.Identifier.Text; // User
            string nameOfTheEntityClassFirstLower = entityClass.Identifier.Text.FirstCharToLower(); // user
            string idTypeOfTheEntityClass = Helper.GetGenericIdType(entityClass, classes); // long

            List<string> result = new List<string>();
            List<SoftProperty> propertiesEntityClass = entityClass.Members.OfType<PropertyDeclarationSyntax>()
                .Select(prop => new SoftProperty()
                {
                    Type = prop.Type.ToString(),
                    IdentifierText = prop.Identifier.Text
                })
                .Where(prop => prop.Type.IsEnumerable())
                .ToList();

            foreach (SoftProperty prop in propertiesEntityClass) // List<Role> Roles
            {
                string classNameFromTheList = GetClassNameFromTheList(prop.Type); // Role
                string classNameFromTheListFirstLower = classNameFromTheList.FirstCharToLower(); // role
                ClassDeclarationSyntax classFromTheList = classes.Where(x => x.Identifier.Text == classNameFromTheList).SingleOrDefault(); // Role

                INamedTypeSymbol classFromTheListFromTheReferencedProjects = referencedClassesEntities.Where(x => x.Name == classNameFromTheList).SingleOrDefault();
                if (classFromTheList == null) // && classFromTheListFromTheReferencedProjects == null  // TODO FT: Continue to do this...
                {
                    continue;
                    //result.Add("INVALID ENTITY CLASS, YOU CAN'T MAKE LIST OF NO ENTITY CLASS"); // FT: It can, if the class is from another project.
                }
                string idTypeOfTheClassFromTheList = Helper.GetGenericIdType(classFromTheList, classes); // int
                string classNameFromTheListDisplayNameProp = Helper.GetDisplayNamePropForClass(classFromTheList, classes); // Name
                //classNameFromTheListDisplayNameProp = classNameFromTheListDisplayNameProp ?? classFromTheListFromTheReferencedProjects.Att.Where(x => ).FirstOrDefault() // TODO FT: Continue to do this...
                List<SoftProperty> classFromTheListProperties = classFromTheList.Members.OfType<PropertyDeclarationSyntax>()
                    .Select(prop => new SoftProperty()
                    {
                        Type = prop.Type.ToString(),
                        IdentifierText = prop.Identifier.Text
                    })
                    .ToList();

                SoftProperty manyToManyPropFromTheListProperties = classFromTheListProperties.Where(x => x.Type.IsEnumerable() && GetClassNameFromTheList(x.Type) == nameOfTheEntityClass).SingleOrDefault(); // List<User> Users
                SoftProperty manyToOneProp = classFromTheListProperties.Where(x => x.Type.PropTypeIsManyToOne() && prop.Type == nameOfTheEntityClass).SingleOrDefault(); // User / Userando

                if (manyToOneProp != null)
                {
                    result.Add($$"""
        public async virtual Task<List<NamebookDTO<{{idTypeOfTheClassFromTheList}}>>> Load{{classNameFromTheList}}NamebookListFor{{nameOfTheEntityClass}}({{idTypeOfTheEntityClass}} {{nameOfTheEntityClassFirstLower}}Id, bool authorize = true)
        {
            return await _context.WithTransactionAsync(async () =>
            {
                if (authorize)
                {
                    {{(generateAuthorizationMethods ? $"await _authorizationService.AuthorizeAndThrowAsync<UserExtended>(PermissionCodes.Read{nameOfTheEntityClass});" : "")}}
                }

                return await _context.DbSet<{{classNameFromTheList}}>()
                    .AsNoTracking()
                    .Where(x => x.{{manyToOneProp.IdentifierText}} == {{nameOfTheEntityClassFirstLower}}Id)
                    .Select(x => new NamebookDTO<{{idTypeOfTheClassFromTheList}}>
                    {
                        Id = x.Id,
                        DisplayName = x.{{classNameFromTheListDisplayNameProp}},
                    })
                    .ToListAsync();
            });
        }

        public async Task<List<{{classNameFromTheList}}>> Load{{classNameFromTheList}}ListFor{{nameOfTheEntityClass}}({{idTypeOfTheEntityClass}} {{nameOfTheEntityClassFirstLower}}Id, bool authorize = true)
        {
            return await _context.WithTransactionAsync(async () =>
            {
                if (authorize)
                {
                    {{(generateAuthorizationMethods ? $"await _authorizationService.AuthorizeAndThrowAsync<UserExtended>(PermissionCodes.Read{nameOfTheEntityClass});" : "")}}
                }

                return await _context.DbSet<{{classNameFromTheList}}>()
                    .Where(x => x.{{manyToOneProp.IdentifierText}} == {{nameOfTheEntityClassFirstLower}}Id)
                    .ToListAsync();
            });
        }
""");
                }
                else if (manyToManyPropFromTheListProperties != null)
                {
                    result.Add($$"""
        public async virtual Task<List<NamebookDTO<{{idTypeOfTheClassFromTheList}}>>> Load{{classNameFromTheList}}NamebookListFor{{nameOfTheEntityClass}}({{idTypeOfTheEntityClass}} {{nameOfTheEntityClassFirstLower}}Id, bool authorize = true)
        {
            return await _context.WithTransactionAsync(async () =>
            {
                if (authorize)
                {
                    {{(generateAuthorizationMethods ? $"await _authorizationService.AuthorizeAndThrowAsync<UserExtended>(PermissionCodes.Read{nameOfTheEntityClass});" : "")}}
                }

                return await _context.DbSet<{{classNameFromTheList}}>()
                    .AsNoTracking()
                    .Where(x => x.{{manyToManyPropFromTheListProperties.IdentifierText}}.Any(x => x.Id == {{nameOfTheEntityClassFirstLower}}Id))
                    .Select(x => new NamebookDTO<{{idTypeOfTheClassFromTheList}}>
                    {
                        Id = x.Id,
                        DisplayName = x.{{classNameFromTheListDisplayNameProp}},
                    })
                    .ToListAsync();
            });
        }

        public async Task<List<{{classNameFromTheList}}>> Load{{classNameFromTheList}}ListFor{{nameOfTheEntityClass}}({{idTypeOfTheEntityClass}} {{nameOfTheEntityClassFirstLower}}Id, bool authorize = true)
        {
            return await _context.WithTransactionAsync(async () =>
            {
                if (authorize)
                {
                    {{(generateAuthorizationMethods ? $"await _authorizationService.AuthorizeAndThrowAsync<UserExtended>(PermissionCodes.Read{nameOfTheEntityClass});" : "")}}
                }

                return await _context.DbSet<{{classNameFromTheList}}>()
                    .Where(x => x.{{manyToManyPropFromTheListProperties.IdentifierText}}.Any(x => x.Id == {{nameOfTheEntityClassFirstLower}}Id))
                    .ToListAsync();
            });
        }

        public async Task Update{{classNameFromTheList}}ListFor{{nameOfTheEntityClass}}({{idTypeOfTheEntityClass}} {{nameOfTheEntityClassFirstLower}}Id, List<{{idTypeOfTheClassFromTheList}}> selected{{classNameFromTheList}}Ids)
        {
            if (selected{{classNameFromTheList}}Ids == null)
                return;

            List<{{idTypeOfTheClassFromTheList}}> selectedIdsHelper = selected{{classNameFromTheList}}Ids.ToList();

            await _context.WithTransactionAsync(async () =>
            {
                // FT: Not doing authorization here, because we can not figure out here if we are updating while inserting object (eg. User), or updating object, we will always get the id which is not 0 here.

                {{((entityClass.IsEntityBusinessObject() || entityClass.IsEntityReadonlyObject() == false)
                ? $"{nameOfTheEntityClass} {nameOfTheEntityClassFirstLower} = await LoadInstanceAsync<{nameOfTheEntityClass}, {idTypeOfTheEntityClass}>({nameOfTheEntityClassFirstLower}Id, null); // FT: Version will always be checked before or after this method"
                : $"{nameOfTheEntityClass} {nameOfTheEntityClassFirstLower} = await LoadInstanceAsync<{nameOfTheEntityClass}, {idTypeOfTheEntityClass}>({nameOfTheEntityClassFirstLower}Id);"
                )}}

                if ({{nameOfTheEntityClassFirstLower}}.{{prop.IdentifierText}} != null)
                {
                    foreach ({{classNameFromTheList}} {{classNameFromTheListFirstLower}} in {{nameOfTheEntityClassFirstLower}}.{{prop.IdentifierText}}.ToList())
                    {
                        if (selectedIdsHelper.Contains({{classNameFromTheListFirstLower}}.Id))
                            selectedIdsHelper.Remove({{classNameFromTheListFirstLower}}.Id);
                        else
                            {{nameOfTheEntityClassFirstLower}}.{{prop.IdentifierText}}.Remove({{classNameFromTheListFirstLower}});
                    }
                }
                else
                {
                    {{nameOfTheEntityClassFirstLower}}.{{prop.IdentifierText}} = new {{prop.Type}}();
                }

                List<{{classNameFromTheList}}> {{classNameFromTheListFirstLower}}ListToInsert = await _context.DbSet<{{classNameFromTheList}}>().Where(x => selectedIdsHelper.Contains(x.Id)).ToListAsync();

                {{nameOfTheEntityClassFirstLower}}.{{prop.IdentifierText}}.AddRange({{classNameFromTheListFirstLower}}ListToInsert);
                await _context.SaveChangesAsync();
            });
        }

        public async Task Update{{classNameFromTheList}}ListFor{{nameOfTheEntityClass}}TableSelection(IQueryable<{{classNameFromTheList}}> {{classNameFromTheListFirstLower}}Query, {{idTypeOfTheEntityClass}} {{nameOfTheEntityClassFirstLower}}Id, TableSelectionDTO<{{idTypeOfTheClassFromTheList}}> tableSelectionDTO)
        {
            await _context.WithTransactionAsync(async () =>
            {
                List<{{idTypeOfTheClassFromTheList}}> {{classNameFromTheListFirstLower}}ListToInsert = null;

                if (tableSelectionDTO.IsAllSelected == true)
                {
                    {{classNameFromTheListFirstLower}}ListToInsert = await {{classNameFromTheListFirstLower}}Query.Where(x => tableSelectionDTO.UnselectedIds.Contains(x.Id) == false).Select(x => x.Id).ToListAsync();
                }
                else if (tableSelectionDTO.IsAllSelected == false)
                {
                    {{classNameFromTheListFirstLower}}ListToInsert = await {{classNameFromTheListFirstLower}}Query.Where(x => tableSelectionDTO.SelectedIds.Contains(x.Id) == true).Select(x => x.Id).ToListAsync();
                }
                else if (tableSelectionDTO.IsAllSelected == null)
                {
                    {{((entityClass.IsEntityBusinessObject() || entityClass.IsEntityReadonlyObject() == false)
                    ? $"{nameOfTheEntityClass} {nameOfTheEntityClassFirstLower} = await LoadInstanceAsync<{nameOfTheEntityClass}, {idTypeOfTheEntityClass}>({nameOfTheEntityClassFirstLower}Id, null); // FT: Version will always be checked before or after this method"
                    : $"{nameOfTheEntityClass} {nameOfTheEntityClassFirstLower} = await LoadInstanceAsync<{nameOfTheEntityClass}, {idTypeOfTheEntityClass}>({nameOfTheEntityClassFirstLower}Id);"
                    )}}

                    List<{{idTypeOfTheClassFromTheList}}> alreadySelected = {{nameOfTheEntityClassFirstLower}}.{{prop.IdentifierText}} == null ? new List<{{idTypeOfTheClassFromTheList}}>() : {{nameOfTheEntityClassFirstLower}}.{{prop.IdentifierText}}.Select(x => x.Id).ToList();

                    {{classNameFromTheListFirstLower}}ListToInsert = alreadySelected
                        .Union(tableSelectionDTO.SelectedIds)
                        .Except(tableSelectionDTO.UnselectedIds)
                        .ToList();
                }

                await Update{{classNameFromTheList}}ListFor{{nameOfTheEntityClass}}({{nameOfTheEntityClassFirstLower}}Id, {{classNameFromTheListFirstLower}}ListToInsert);
            });
        }
""");
                }
                else if (classFromTheList == null)
                {
                    result.Add("Invalid entity class, you can't have List<Entity> without List<AssociationEntity> or AssociationEntity on the other side."); // He can (User/Role example, many to many on the one side)
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

        static ClassDeclarationSyntax GetClassOfManyToOneProperty(string propType, IList<ClassDeclarationSyntax> classes)
        {
            ClassDeclarationSyntax manyToOneclass = classes.Where(x => x.Identifier.Text == propType).SingleOrDefault();

            if (manyToOneclass == null)
                return null;

            return manyToOneclass;
        }

        static string GetClassNameFromTheList(string propType)
        {
            string[] parts = propType.Split('<');
            return parts[parts.Length-1].Replace(">", "");
        }

        static string GetSavingData(string nameOfTheEntityClass, string idTypeOfTheEntityClass, ClassDeclarationSyntax c, IList<ClassDeclarationSyntax> entityClasses, bool generateAuthorizationMethods)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append($$"""
        protected virtual void OnBefore{{nameOfTheEntityClass}}IsMapped({{nameOfTheEntityClass}}DTO dto) { }

        public async Task<{{nameOfTheEntityClass}}> Save{{nameOfTheEntityClass}}AndReturnDomainAsync({{nameOfTheEntityClass}}DTO dto, bool authorizeUpdate = true, bool authorizeInsert = true)
        {
            {{nameOfTheEntityClass}}DTOValidationRules validationRules = new {{nameOfTheEntityClass}}DTOValidationRules();
            validationRules.ValidateAndThrow(dto);

            {{nameOfTheEntityClass}} poco = null;
            await _context.WithTransactionAsync(async () =>
            {
                OnBefore{{nameOfTheEntityClass}}IsMapped(dto);
                DbSet<{{nameOfTheEntityClass}}> dbSet = _context.DbSet<{{nameOfTheEntityClass}}>();
""");
            if (c.IsEntityReadonlyObject())
            {
                sb.AppendLine($$"""

                poco = dto.Adapt<{{nameOfTheEntityClass}}>();
                await dbSet.AddAsync(poco);
""");
            }
            else
            {
                sb.AppendLine($$"""

                if (dto.Id > 0)
                {
                    if (authorizeUpdate)
                    {
                        {{(generateAuthorizationMethods ? $"await _authorizationService.AuthorizeAndThrowAsync<UserExtended>(PermissionCodes.Edit{nameOfTheEntityClass});" : "")}}
                    }

                    poco = await LoadInstanceAsync<{{nameOfTheEntityClass}}, {{idTypeOfTheEntityClass}}>(dto.Id, dto.Version);
                    dto.Adapt(poco);
                    dbSet.Update(poco);
                }
                else
                {
                    if (authorizeInsert)
                    {
                        {{(generateAuthorizationMethods ? $"await _authorizationService.AuthorizeAndThrowAsync<UserExtended>(PermissionCodes.Insert{nameOfTheEntityClass});" : "")}}
                    }

                    poco = dto.Adapt<{{nameOfTheEntityClass}}>();
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

        public async Task<{{nameOfTheEntityClass}}DTO> Save{{nameOfTheEntityClass}}AndReturnDTOAsync({{nameOfTheEntityClass}}DTO dto, bool authorizeUpdate = true, bool authorizeInsert = true)
        {
            return await _context.WithTransactionAsync(async () =>
            {
                {{nameOfTheEntityClass}} poco = await Save{{nameOfTheEntityClass}}AndReturnDomainAsync(dto, authorizeUpdate, authorizeInsert);

                return poco.Adapt<{{nameOfTheEntityClass}}DTO>(Mapper.{{c.Identifier.Text}}ToDTOConfig());
            });
        }
""");
            return sb.ToString();
        }
    }
}