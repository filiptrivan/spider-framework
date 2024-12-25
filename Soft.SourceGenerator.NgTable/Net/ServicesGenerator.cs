using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Soft.SourceGenerator.NgTable.Helpers;
using Soft.SourceGenerators;
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
    public class ServicesGenerator : IIncrementalGenerator
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

            IncrementalValueProvider<List<SoftClass>> referencedProjectEntityClasses = Helper.GetEntityClassesFromReferencedAssemblies(context);

            var allClasses = classDeclarations.Collect()
                .Combine(referencedProjectEntityClasses);

            context.RegisterImplementationSourceOutput(allClasses, static (spc, source) => Execute(source.Left, source.Right, spc));
        }

        private static void Execute(IList<ClassDeclarationSyntax> classes, List<SoftClass> referencedProjectEntityClasses, SourceProductionContext context)
        {
            if (classes.Count <= 1) 
                return;

            List<SoftClass> entityClasses = Helper.GetSoftEntityClasses(classes);
            List<SoftClass> allEntityClasses = entityClasses.Concat(referencedProjectEntityClasses).ToList();

            StringBuilder sb = new StringBuilder();

            string[] namespacePartsWithoutLastElement = Helper.GetNamespacePartsWithoutLastElement(entityClasses[0].Namespace);

            string basePartOfTheNamespace = string.Join(".", namespacePartsWithoutLastElement); // eg. Soft.Generator.Security
            string projectName = namespacePartsWithoutLastElement[namespacePartsWithoutLastElement.Length - 1]; // eg. Security

            bool isSecurityProject = projectName == "Security";

            sb.AppendLine($$"""
{{GetUsings(basePartOfTheNamespace)}}
{{(isSecurityProject ? "using Soft.Generator.Security.Interface;" : "")}}

namespace {{basePartOfTheNamespace}}.Services
{
    {{(isSecurityProject ? $"public class BusinessServiceGenerated<TUser> : BusinessServiceBase where TUser : class, IUser, new()" : $"public class BusinessServiceGenerated : BusinessServiceBase")}}
    {
        private readonly IApplicationDbContext _context;
        private readonly ExcelService _excelService;
        {{(isSecurityProject ? "private readonly AuthorizationBusinessService<TUser> _authorizationService;" : "private readonly AuthorizationBusinessService _authorizationService;")}}
        private readonly BlobContainerClient _blobContainerClient;

        public BusinessServiceGenerated(IApplicationDbContext context, ExcelService excelService, {{(isSecurityProject ? "AuthorizationBusinessService<TUser> authorizationService" : "AuthorizationBusinessService authorizationService")}}, BlobContainerClient blobContainerClient)
        : base(context, blobContainerClient)
        {
            _context = context;
            _excelService = excelService;
            _authorizationService = authorizationService;
            _blobContainerClient = blobContainerClient;
        }

""");

            foreach (SoftClass entityClass in entityClasses)
            {
                string baseType = entityClass.BaseType;

                string nameOfTheEntityClass = entityClass.Name;

                if (baseType == null) // FT: Handling many to many
                {
                    sb.AppendLine($$"""
        #region {{nameOfTheEntityClass}}

{{HandleManyToManyData(entityClass, allEntityClasses)}}

        #endregion

""");

                    continue;
                }

                string nameOfTheEntityClassFirstLower = entityClass.Name.FirstCharToLower();
                string idTypeOfTheEntityClass = Helper.GetGenericIdType(entityClass, allEntityClasses);
                string displayNameProperty = Helper.GetDisplayNamePropForClass(entityClass);

                List<SoftProperty> entityProperties = entityClass.Properties;

                sb.AppendLine($$"""
        #region {{nameOfTheEntityClass}}

        #region Read

        public async Task<{{nameOfTheEntityClass}}DTO> Get{{nameOfTheEntityClass}}DTOAsync({{idTypeOfTheEntityClass}} id, bool authorize = true)
        {
            return await _context.WithTransactionAsync(async () =>
            {
                if (authorize) 
                {
                    await _authorizationService.{{nameOfTheEntityClass}}SingleReadAuthorize(id);
                }

                {{nameOfTheEntityClass}}DTO dto = await _context.DbSet<{{nameOfTheEntityClass}}>().AsNoTracking().Where(x => x.Id == id).ProjectToType<{{nameOfTheEntityClass}}DTO>(Mapper.{{entityClass.Name}}ProjectToConfig()).SingleOrDefaultAsync();

                if (dto == null)
                    throw new BusinessException(SharedTerms.EntityDoesNotExistInDatabase);

{{GetPopulateDTOWithBlobPartsForDTO(entityClass, entityProperties)}}

                return dto;
            });
        }

        public async Task<PaginationResult<{{nameOfTheEntityClass}}>> Load{{nameOfTheEntityClass}}ListForPagination(TableFilterDTO tableFilterPayload, IQueryable<{{nameOfTheEntityClass}}> query)
        {
            return await _context.WithTransactionAsync(async () =>
            {
                return await TableFilterQueryable.Build(query.AsNoTracking(), tableFilterPayload);
            });
        }

        public async virtual Task<TableResponseDTO<{{nameOfTheEntityClass}}DTO>> Load{{nameOfTheEntityClass}}TableData(TableFilterDTO tableFilterPayload, IQueryable<{{nameOfTheEntityClass}}> query, bool authorize = true)
        {
            PaginationResult<{{nameOfTheEntityClass}}> paginationResult = new PaginationResult<{{nameOfTheEntityClass}}>();
            List<{{nameOfTheEntityClass}}DTO> data = null;

            await _context.WithTransactionAsync(async () =>
            {
                if (authorize) 
                {
                    await _authorizationService.{{nameOfTheEntityClass}}ListReadAuthorize();
                }

                paginationResult = await Load{{nameOfTheEntityClass}}ListForPagination(tableFilterPayload, query);

                data = await paginationResult.Query
                    .Skip(tableFilterPayload.First)
                    .Take(tableFilterPayload.Rows)
                    .ProjectToType<{{nameOfTheEntityClass}}DTO>(Mapper.{{entityClass.Name}}ProjectToConfig())
                    .ToListAsync();
            });

            return new TableResponseDTO<{{nameOfTheEntityClass}}DTO> { Data = data, TotalRecords = paginationResult.TotalRecords };
        }

        public async virtual Task<List<NamebookDTO<{{idTypeOfTheEntityClass}}>>> Load{{nameOfTheEntityClass}}ListForAutocomplete(int limit, string query, IQueryable<{{nameOfTheEntityClass}}> {{nameOfTheEntityClassFirstLower}}Query, bool authorize = true)
        {
            return await _context.WithTransactionAsync(async () =>
            {
                if (authorize)
                {
                    await _authorizationService.{{nameOfTheEntityClass}}ListReadAuthorize();
                }

                if (!string.IsNullOrEmpty(query))
                    {{nameOfTheEntityClassFirstLower}}Query = {{nameOfTheEntityClassFirstLower}}Query.Where(x => x.{{displayNameProperty}}.Contains(query));

                return await {{nameOfTheEntityClassFirstLower}}Query
                    .AsNoTracking()
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
                    await _authorizationService.{{nameOfTheEntityClass}}ListReadAuthorize();
                }

                return await {{nameOfTheEntityClassFirstLower}}Query
                    .AsNoTracking()
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
                    await _authorizationService.{{nameOfTheEntityClass}}ListReadAuthorize();
                }

                return await {{nameOfTheEntityClassFirstLower}}Query
                    .ToListAsync();
            });
        }

        public async Task<List<{{nameOfTheEntityClass}}DTO>> Load{{nameOfTheEntityClass}}DTOList(IQueryable<{{nameOfTheEntityClass}}> {{nameOfTheEntityClassFirstLower}}Query, bool authorize = true)
        {
            return await _context.WithTransactionAsync(async () =>
            {
                if (authorize)
                {
                    await _authorizationService.{{nameOfTheEntityClass}}ListReadAuthorize();
                }

                List<{{nameOfTheEntityClass}}DTO> dtoList = await {{nameOfTheEntityClassFirstLower}}Query
                    .AsNoTracking()
                    .ProjectToType<{{nameOfTheEntityClass}}DTO>(Mapper.{{entityClass.Name}}ToDTOConfig())
                    .ToListAsync();

{{GetPopulateDTOWithBlobPartsForDTOList(entityClass, entityProperties)}}

                return dtoList;
            });
        }

        #endregion

        #region Save

{{(entityClass.IsAbstract || entityClass.IsEntityReadonlyObject() ? "" : GetSavingData(entityClass, idTypeOfTheEntityClass, allEntityClasses, entityProperties))}}

{{string.Join("\n", GetUploadBlobMethods(entityClass, idTypeOfTheEntityClass, entityProperties))}}
        
        #endregion

{{(entityClass.IsAbstract || entityClass.IsEntityReadonlyObject() ? "" : GetDeletingData(entityClass, idTypeOfTheEntityClass, allEntityClasses))}}

{{string.Join("\n", GetEnumerableGeneratedMethods(entityClass, allEntityClasses))}}

        public async Task<byte[]> Export{{nameOfTheEntityClass}}TableDataToExcel(TableFilterDTO tableFilterPayload, IQueryable<{{nameOfTheEntityClass}}> query, bool authorize = true)
        {
            PaginationResult<{{nameOfTheEntityClass}}> paginationResult = new PaginationResult<{{nameOfTheEntityClass}}>();
            List<{{nameOfTheEntityClass}}DTO> data = null;

            await _context.WithTransactionAsync(async () =>
            {
                if (authorize)
                {
                    await _authorizationService.{{nameOfTheEntityClass}}ListReadAuthorize();
                }

                paginationResult = await Load{{nameOfTheEntityClass}}ListForPagination(tableFilterPayload, query);

                data = await paginationResult.Query.ProjectToType<{{nameOfTheEntityClass}}DTO>(Mapper.{{entityClass.Name}}ExcelProjectToConfig()).ToListAsync();
            });

            string[] excelPropertiesToExclude = ExcelPropertiesToExclude.GetHeadersToExclude(new {{nameOfTheEntityClass}}DTO());
            return _excelService.FillReportTemplate<{{nameOfTheEntityClass}}DTO>(data, paginationResult.TotalRecords, excelPropertiesToExclude).ToArray();
        }

        #endregion

""");
            }

            sb.AppendLine($$"""
    }
}
""");

            context.AddSource($"BusinessService.generated", SourceText.From(sb.ToString(), Encoding.UTF8));
        }

        #region Save

        static string GetSavingData(SoftClass entityClass, string idTypeOfTheEntityClass, List<SoftClass> allEntityClasses, List<SoftProperty> propertiesEntityClass)
        {
            string nameOfTheEntityClass = entityClass.Name;

            StringBuilder sb = new StringBuilder();

            sb.Append($$"""
        protected virtual async Task OnBefore{{nameOfTheEntityClass}}IsMapped({{nameOfTheEntityClass}}DTO {{nameOfTheEntityClass.FirstCharToLower()}}DTO) { }

        protected virtual async Task OnBefore{{nameOfTheEntityClass}}Update({{nameOfTheEntityClass}} {{nameOfTheEntityClass.FirstCharToLower()}}, {{nameOfTheEntityClass}}DTO {{nameOfTheEntityClass.FirstCharToLower()}}DTO) { }

        public async Task<{{nameOfTheEntityClass}}> Save{{nameOfTheEntityClass}}AndReturnDomainAsync({{nameOfTheEntityClass}}DTO dto, bool authorizeUpdate = true, bool authorizeInsert = true)
        {
            {{nameOfTheEntityClass}}DTOValidationRules validationRules = new {{nameOfTheEntityClass}}DTOValidationRules();
            validationRules.ValidateAndThrow(dto);

            {{nameOfTheEntityClass}} poco = null;
            await _context.WithTransactionAsync(async () =>
            {
                await OnBefore{{nameOfTheEntityClass}}IsMapped(dto);
                DbSet<{{nameOfTheEntityClass}}> dbSet = _context.DbSet<{{nameOfTheEntityClass}}>();
""");
            if (entityClass.IsEntityReadonlyObject())
            {
                sb.AppendLine($$"""

                poco = dto.Adapt<{{nameOfTheEntityClass}}>(Mapper.{{nameOfTheEntityClass}}DTOToEntityConfig());
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
                        await _authorizationService.{{nameOfTheEntityClass}}SingleUpdateAuthorize(dto);
                    }

                    poco = await LoadInstanceAsync<{{nameOfTheEntityClass}}, {{idTypeOfTheEntityClass}}>(dto.Id, dto.Version);
                    await OnBefore{{nameOfTheEntityClass}}Update(poco, dto);
                    dto.Adapt(poco, Mapper.{{nameOfTheEntityClass}}DTOToEntityConfig());
                    dbSet.Update(poco);
                }
                else
                {
                    if (authorizeInsert)
                    {
                        await _authorizationService.{{nameOfTheEntityClass}}SingleInsertAuthorize(dto);
                    }

                    poco = dto.Adapt<{{nameOfTheEntityClass}}>(Mapper.{{nameOfTheEntityClass}}DTOToEntityConfig());
                    await dbSet.AddAsync(poco);
                }
""");
            }
            sb.AppendLine($$"""

{{string.Join("\n", GetManyToOneInstancesForSave(entityClass, allEntityClasses))}}

                await _context.SaveChangesAsync();

                {{string.Join("\n\t\t\t\t", GetNonActiveDeleteBlobMethods(entityClass, propertiesEntityClass))}}
            });

            return poco;
        }

        public async Task<{{nameOfTheEntityClass}}DTO> Save{{nameOfTheEntityClass}}AndReturnDTOAsync({{nameOfTheEntityClass}}DTO {{nameOfTheEntityClass.FirstCharToLower()}}DTO, bool authorizeUpdate = true, bool authorizeInsert = true)
        {
            return await _context.WithTransactionAsync(async () =>
            {
                {{nameOfTheEntityClass}} poco = await Save{{nameOfTheEntityClass}}AndReturnDomainAsync({{nameOfTheEntityClass.FirstCharToLower()}}DTO, authorizeUpdate, authorizeInsert);

                return poco.Adapt<{{nameOfTheEntityClass}}DTO>(Mapper.{{entityClass.Name}}ToDTOConfig());
            });
        }
""");
            return sb.ToString();
        }

        static List<string> GetManyToOneInstancesForSave(SoftClass entityClass, List<SoftClass> allEntityClasses)
        {
            List<string> result = new List<string>();

            List<SoftProperty> properties = entityClass.Properties
                .Where(prop => prop.Type.PropTypeIsManyToOne())
                .ToList();

            foreach (SoftProperty prop in properties)
            {
                SoftClass classOfManyToOneProperty = GetClassOfManyToOneProperty(prop.Type, allEntityClasses);

                if (classOfManyToOneProperty == null)
                    continue;

                if (classOfManyToOneProperty.IsEntityBusinessObject() || classOfManyToOneProperty.IsEntityReadonlyObject() == false)
                {
                    result.Add($$"""
            if (dto.{{prop.IdentifierText}}Id > 0)
                poco.{{prop.IdentifierText}} = await LoadInstanceAsync<{{prop.Type}}, {{Helper.GetGenericIdType(classOfManyToOneProperty, allEntityClasses)}}>(dto.{{prop.IdentifierText}}Id.Value, null);
            else
                poco.{{prop.IdentifierText}} = null;
""");
                }
                else
                {
                    result.Add($$"""
            if (dto.{{prop.IdentifierText}}Id > 0)
                poco.{{prop.IdentifierText}} = await LoadInstanceAsync<{{prop.Type}}, {{Helper.GetGenericIdType(classOfManyToOneProperty, allEntityClasses)}}>(dto.{{prop.IdentifierText}}Id.Value);
            else
                poco.{{prop.IdentifierText}} = null;
""");
                }
            }

            return result;
        }

        static SoftClass GetClassOfManyToOneProperty(string propType, List<SoftClass> allEntityClasses)
        {
            SoftClass manyToOneclass = allEntityClasses.Where(x => x.Name == propType).SingleOrDefault();

            if (manyToOneclass == null)
                return null;

            return manyToOneclass;
        }

        private static List<string> GetNonActiveDeleteBlobMethods(SoftClass entityClass, List<SoftProperty> propertiesEntityClass)
        {
            List<string> result = new List<string>();

            List<SoftProperty> blobProperies = Helper.GetBlobProperties(propertiesEntityClass);

            foreach (SoftProperty property in blobProperies)
                result.Add($"await DeleteNonActiveBlobs(dto.{property.IdentifierText}, nameof({entityClass.Name}), nameof({entityClass.Name}.{property.IdentifierText}), poco.Id.ToString());");

            return result;
        }

        private static List<string> GetUploadBlobMethods(SoftClass entityClass, string idTypeOfTheEntityClass, List<SoftProperty> entityProperties)
        {
            List<string> result = new List<string>();

            List<SoftProperty> blobProperies = Helper.GetBlobProperties(entityProperties);

            string nameOfTheEntityClass = entityClass.Name;
            string nameOfTheEntityClassFirstLower = entityClass.Name.FirstCharToLower();

            foreach (SoftProperty property in blobProperies)
            {
                result.Add($$"""
        public async Task<string> Upload{{nameOfTheEntityClass}}{{property.IdentifierText}}Async(IFormFile file, bool authorizeUpdate = true, bool authorizeInsert = true) // FT: It doesn't work without interface
        {
            using Stream stream = file.OpenReadStream();

            {{idTypeOfTheEntityClass}} {{nameOfTheEntityClassFirstLower}}Id = GetObjectIdFromFileName<{{idTypeOfTheEntityClass}}>(file.FileName);

            if ({{nameOfTheEntityClassFirstLower}}Id > 0)
            {
                if (authorizeUpdate)
                {
                    await _authorizationService.{{nameOfTheEntityClass}}SingleUpdateAuthorize({{nameOfTheEntityClassFirstLower}}Id);
                }
            }
            else
            {
                if (authorizeInsert)
                {
                    await _authorizationService.{{nameOfTheEntityClass}}SingleInsertAuthorize();
                }
            }

            OnBefore{{nameOfTheEntityClass}}{{property.IdentifierText}}BlobIsUploaded({{nameOfTheEntityClassFirstLower}}Id); // FT: Authorize access for this id...

            string fileName = await UploadFileAsync(file.FileName, nameof({{nameOfTheEntityClass}}), nameof({{nameOfTheEntityClass}}.{{property.IdentifierText}}), {{nameOfTheEntityClassFirstLower}}Id.ToString(), stream);

            return fileName;
        }

        public virtual async Task OnBefore{{nameOfTheEntityClass}}{{property.IdentifierText}}BlobIsUploaded ({{idTypeOfTheEntityClass}} {{nameOfTheEntityClassFirstLower}}Id) { }
"""
);
            }

            return result;
        }

        #endregion

        #region Delete

        private static string GetDeletingData(SoftClass entityClass, string idTypeOfTheEntityClass, List<SoftClass> allEntityClasses)
        {
            string nameOfTheEntityClass = entityClass.Name;
            string nameOfTheEntityClassFirstLower = nameOfTheEntityClass.FirstCharToLower();

            StringBuilder sb = new StringBuilder();

            sb.Append($$"""
        public virtual async Task OnBefore{{nameOfTheEntityClass}}AsyncDelete({{idTypeOfTheEntityClass}} {{nameOfTheEntityClassFirstLower}}Id) { }

        public async Task Delete{{nameOfTheEntityClass}}Async({{idTypeOfTheEntityClass}} {{nameOfTheEntityClassFirstLower}}Id, bool authorize = true)
        {
            await _context.WithTransactionAsync(async () =>
            {
                if (authorize)
                {
                    await _authorizationService.{{nameOfTheEntityClass}}DeleteAuthorize({{nameOfTheEntityClassFirstLower}}Id);
                }

                await OnBefore{{nameOfTheEntityClass}}AsyncDelete({{nameOfTheEntityClassFirstLower}}Id);

{{string.Join("\n", GetManyToOneDeleteQueries(entityClass, allEntityClasses, null, 0))}}

                await DeleteEntityAsync<{{nameOfTheEntityClass}}, {{idTypeOfTheEntityClass}}>({{nameOfTheEntityClassFirstLower}}Id);
            });
        }

        public virtual async Task OnBefore{{nameOfTheEntityClass}}ListAsyncDelete(List<{{idTypeOfTheEntityClass}}> {{nameOfTheEntityClassFirstLower}}ListToDelete) { }

        public async Task Delete{{nameOfTheEntityClass}}ListAsync(List<{{idTypeOfTheEntityClass}}> {{nameOfTheEntityClassFirstLower}}ListToDelete, bool authorize = true)
        {
            await _context.WithTransactionAsync(async () =>
            {
                if (authorize)
                {
                    await _authorizationService.{{nameOfTheEntityClass}}ListDeleteAuthorize({{nameOfTheEntityClassFirstLower}}ListToDelete);
                }

                await OnBefore{{nameOfTheEntityClass}}ListAsyncDelete({{nameOfTheEntityClassFirstLower}}ListToDelete);

{{string.Join("\n", GetManyToOneDeleteQueriesForList(entityClass, allEntityClasses, null, 0))}}

                await DeleteEntitiesAsync<{{nameOfTheEntityClass}}, {{idTypeOfTheEntityClass}}>({{nameOfTheEntityClassFirstLower}}ListToDelete);
            });
        }
""");

            return sb.ToString();
        }

        private static List<string> GetManyToOneDeleteQueries(SoftClass entityClass, List<SoftClass> allEntityClasses, string parentNameOfTheEntityClass, int recursiveIteration)
        {
            if (recursiveIteration > 5000)
            {
                GetManyToOneDeleteQueries(null, null, null, int.MaxValue);
                return new List<string> { "You made cascade delete infinite loop." };
            }

            List<string> result = new List<string>();

            string nameOfTheEntityClass = entityClass.Name;
            string nameOfTheEntityClassFirstLower = nameOfTheEntityClass.FirstCharToLower();

            List<SoftProperty> manyToOneRequiredProperties = Helper.GetManyToOneRequiredProperties(nameOfTheEntityClass, allEntityClasses);

            foreach (SoftProperty prop in manyToOneRequiredProperties)
            {
                SoftClass nestedEntityClass = allEntityClasses.Where(x => x.Name == prop.ClassIdentifierText).SingleOrDefault();
                string nestedEntityClassName = nestedEntityClass.Name;
                string nestedEntityClassNameLowerCase = nestedEntityClassName.FirstCharToLower();
                string nestedEntityClassIdType = Helper.GetGenericIdType(nestedEntityClass, allEntityClasses);

                if (recursiveIteration == 0)
                {
                    result.Add($$"""
                List<{{nestedEntityClassIdType}}> {{nameOfTheEntityClassFirstLower}}{{nestedEntityClassName}}ListToDelete = await _context.DbSet<{{nestedEntityClassName}}>().Where(x => x.{{prop.IdentifierText}}.Id == {{nameOfTheEntityClassFirstLower}}Id).Select(x => x.Id).ToListAsync();
""");
                }
                else
                {
                    result.Add($$"""
                List<{{nestedEntityClassIdType}}> {{nameOfTheEntityClassFirstLower}}{{nestedEntityClassName}}ListToDelete = await _context.DbSet<{{nestedEntityClassName}}>().Where(x => {{parentNameOfTheEntityClass.FirstCharToLower()}}{{nameOfTheEntityClass}}ListToDelete.Contains(x.{{prop.IdentifierText}}.Id)).Select(x => x.Id).ToListAsync();
""");
                }

                result.AddRange(GetManyToOneDeleteQueries(nestedEntityClass, allEntityClasses, nameOfTheEntityClass, recursiveIteration + 1));

                result.Add($$"""
                await _context.DbSet<{{nestedEntityClassName}}>().Where(x => {{nameOfTheEntityClassFirstLower}}{{nestedEntityClassName}}ListToDelete.Contains(x.Id)).ExecuteDeleteAsync();
""");
            }

            return result;
        }

        private static List<string> GetManyToOneDeleteQueriesForList(SoftClass entityClass, List<SoftClass> allEntityClasses, string parentNameOfTheEntityClass, int recursiveIteration)
        {
            if (recursiveIteration > 5000)
            {
                GetManyToOneDeleteQueries(null, null, null, int.MaxValue);
                return new List<string> { "You made cascade delete infinite loop." };
            }

            List<string> result = new List<string>();

            string nameOfTheEntityClass = entityClass.Name;
            string nameOfTheEntityClassFirstLower = nameOfTheEntityClass.FirstCharToLower();

            List<SoftProperty> manyToOneRequiredProperties = Helper.GetManyToOneRequiredProperties(nameOfTheEntityClass, allEntityClasses);

            foreach (SoftProperty prop in manyToOneRequiredProperties)
            {
                SoftClass nestedEntityClass = allEntityClasses.Where(x => x.Name == prop.ClassIdentifierText).SingleOrDefault();
                string nestedEntityClassName = nestedEntityClass.Name;
                string nestedEntityClassNameLowerCase = nestedEntityClassName.FirstCharToLower();
                string nestedEntityClassIdType = Helper.GetGenericIdType(nestedEntityClass, allEntityClasses);

                if (recursiveIteration == 0)
                {
                    result.Add($$"""
                List<{{nestedEntityClassIdType}}> {{nameOfTheEntityClassFirstLower}}{{nestedEntityClassName}}ListToDelete = await _context.DbSet<{{nestedEntityClassName}}>().Where(x => {{nameOfTheEntityClassFirstLower}}ListToDelete.Contains(x.{{prop.IdentifierText}}.Id)).Select(x => x.Id).ToListAsync();
""");
                }
                else
                {
                    result.Add($$"""
                List<{{nestedEntityClassIdType}}> {{nameOfTheEntityClassFirstLower}}{{nestedEntityClassName}}ListToDelete = await _context.DbSet<{{nestedEntityClassName}}>().Where(x => {{parentNameOfTheEntityClass.FirstCharToLower()}}{{nameOfTheEntityClass}}ListToDelete.Contains(x.{{prop.IdentifierText}}.Id)).Select(x => x.Id).ToListAsync();
""");
                }

                result.AddRange(GetManyToOneDeleteQueries(nestedEntityClass, allEntityClasses, nameOfTheEntityClass, recursiveIteration + 1));

                result.Add($$"""
                await _context.DbSet<{{nestedEntityClassName}}>().Where(x => {{nameOfTheEntityClassFirstLower}}{{nestedEntityClassName}}ListToDelete.Contains(x.Id)).ExecuteDeleteAsync();
""");
            }

            return result;
        }

        #endregion

        #region Read

        private static string GetPopulateDTOWithBlobPartsForDTO(SoftClass entityClass, List<SoftProperty> propertiesEntityClass)
        {
            List<string> blobParts = GetPopulateDTOWithBlobParts(propertiesEntityClass);

            if (blobParts.Count == 0)
                return null;

            return $$"""
                {{string.Join("\n", blobParts)}}
""";
        }

        private static string GetPopulateDTOWithBlobPartsForDTOList(SoftClass entityClass, List<SoftProperty> propertiesEntityClass)
        {
            List<string> blobParts = GetPopulateDTOWithBlobParts(propertiesEntityClass);

            if (blobParts.Count == 0)
                return null;

            return $$"""
                foreach ({{entityClass.Name}}DTO dto in dtoList)
                {
                    {{string.Join("\n", blobParts)}}
                }
""";
        }

        private static List<string> GetPopulateDTOWithBlobParts(List<SoftProperty> propertiesEntityClass)
        {
            List<string> blobParts = new List<string>();

            List<SoftProperty> blobProperies = Helper.GetBlobProperties(propertiesEntityClass);

            foreach (SoftProperty property in blobProperies)
            {
                blobParts.Add($$"""
                    if (!string.IsNullOrEmpty(dto.{{property.IdentifierText}}))
                    {
                        dto.{{property.IdentifierText}}Data = await GetFileDataAsync(dto.{{property.IdentifierText}});
                    }
""");
            }

            return blobParts;
        }

        #endregion

        #region Enumerable

        static List<string> GetEnumerableGeneratedMethods(SoftClass entityClass, List<SoftClass> allEntityClasses)
        {
            string nameOfTheEntityClass = entityClass.Name; // User
            string nameOfTheEntityClassFirstLower = entityClass.Name.FirstCharToLower(); // user
            string idTypeOfTheEntityClass = Helper.GetGenericIdType(entityClass, allEntityClasses); // long

            List<SoftProperty> enumerablePropertiesOfTheEntityClass = entityClass.Properties
                .Where(prop => prop.Type.IsEnumerable())
                .ToList();

            List<string> result = new List<string>();

            foreach (SoftProperty prop in enumerablePropertiesOfTheEntityClass) // List<Role> Roles
            {
                string classNameFromTheList = GetClassNameFromTheList(prop.Type); // Role
                string classNameFromTheListFirstLower = classNameFromTheList.FirstCharToLower(); // role
                SoftClass classFromTheList = allEntityClasses.Where(x => x.Name == classNameFromTheList).Single(); // Role

                //ClassDeclarationSyntax classFromTheListFromTheReferencedProjects = referencedClassesEntities.Where(x => x.Identifier.Text == classNameFromTheList).SingleOrDefault();
                //if (classFromTheList == null) // && classFromTheListFromTheReferencedProjects == null  // TODO FT: Continue to do this...
                //{
                    //continue;
                    //result.Add("INVALID ENTITY CLASS, YOU CAN'T MAKE LIST OF NO ENTITY CLASS"); // FT: It can, if the class is from another project.
                //}

                string idTypeOfTheClassFromTheList = Helper.GetGenericIdType(classFromTheList, allEntityClasses); // int

                if (idTypeOfTheClassFromTheList == null) // FT: M2M List, maybe do something else in the future.
                    continue;

                string classNameFromTheListDisplayNameProp = Helper.GetDisplayNamePropForClass(classFromTheList); // Name
                //classNameFromTheListDisplayNameProp = classNameFromTheListDisplayNameProp ?? classFromTheListFromTheReferencedProjects.Att.Where(x => ).FirstOrDefault() // TODO FT: Continue to do this...
                List<SoftProperty> classFromTheListProperties = classFromTheList.Properties;

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
                    await _authorizationService.{{nameOfTheEntityClass}}SingleReadAuthorize({{nameOfTheEntityClassFirstLower}}Id);
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
                    await _authorizationService.{{nameOfTheEntityClass}}SingleReadAuthorize({{nameOfTheEntityClassFirstLower}}Id);
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
                    await _authorizationService.{{nameOfTheEntityClass}}SingleReadAuthorize({{nameOfTheEntityClassFirstLower}}Id);
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
                    await _authorizationService.{{nameOfTheEntityClass}}SingleReadAuthorize({{nameOfTheEntityClassFirstLower}}Id);
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
                
                foreach ({{classNameFromTheList}} {{classNameFromTheListFirstLower}} in {{nameOfTheEntityClassFirstLower}}.{{prop.IdentifierText}}.ToList())
                {
                    if (selectedIdsHelper.Contains({{classNameFromTheListFirstLower}}.Id))
                        selectedIdsHelper.Remove({{classNameFromTheListFirstLower}}.Id);
                    else
                        {{nameOfTheEntityClassFirstLower}}.{{prop.IdentifierText}}.Remove({{classNameFromTheListFirstLower}});
                }

                List<{{classNameFromTheList}}> {{classNameFromTheListFirstLower}}ListToInsert = await _context.DbSet<{{classNameFromTheList}}>().Where(x => selectedIdsHelper.Contains(x.Id)).ToListAsync();

                {{nameOfTheEntityClassFirstLower}}.{{prop.IdentifierText}}.AddRange({{classNameFromTheListFirstLower}}ListToInsert);
                await _context.SaveChangesAsync();
            });
        }

        public async Task Update{{classNameFromTheList}}ListFor{{nameOfTheEntityClass}}TableSelection(IQueryable<{{classNameFromTheList}}> {{classNameFromTheListFirstLower}}Query, {{idTypeOfTheEntityClass}} {{nameOfTheEntityClassFirstLower}}Id, LazyTableSelectionDTO<{{idTypeOfTheClassFromTheList}}> tableSelectionDTO)
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

        static string GetClassNameFromTheList(string propType)
        {
            string[] parts = propType.Split('<');
            return parts[parts.Length-1].Replace(">", "");
        }

        #endregion

        #region M2M

        private static string HandleManyToManyData(SoftClass entityClass, List<SoftClass> allEntityClasses)
        {
            if (entityClass.Properties.Count == Settings.NumberOfPropertiesWithoutAdditionalManyToManyProperties)
                return null;

            string nameOfTheEntityClass = entityClass.Name;
            string nameOfTheEntityClassFirstLower = entityClass.Name.FirstCharToLower();

            List<SoftProperty> manyToManyProperties = entityClass.Properties;

            SoftProperty mainEntityProperty = manyToManyProperties
                .Where(x => x.Attributes.Any(x => x.Name == "M2MMaintanceEntity"))
                .SingleOrDefault(); // eg. Category
            SoftAttribute mainEntityAttribute = mainEntityProperty.Attributes.Where(x => x.Name == "M2MMaintanceEntity").SingleOrDefault();

            SoftProperty extendEntityProperty = manyToManyProperties
                .Where(x => x.Attributes.Any(x => x.Name == "M2MExtendEntity"))
                .SingleOrDefault();
            SoftAttribute extendEntityAttribute = extendEntityProperty.Attributes.Where(x => x.Name == "M2MExtendEntity").SingleOrDefault();

            if (mainEntityProperty == null)
                return null;

            if (extendEntityProperty == null)
                return "YouNeedToDefineExtendEntityAlso";

            if (mainEntityAttribute?.Value != extendEntityAttribute?.Value) // FT HACK, FT TODO: For now, when we migrate UserNotification and PartnerUserPartnerNotification, we should change this.
                return null;

            string mainEntityPropertyName = mainEntityProperty.IdentifierText; // eg. "DiscountProductGroup"
            string mainEntityClassName = mainEntityProperty.Type; // eg. Category
            SoftClass mainEntityClass = allEntityClasses.Where(x => x.Name == mainEntityClassName).Single();
            string mainEntityIdType = Helper.GetGenericIdType(mainEntityClass, allEntityClasses);

            string extendEntityPropertyName = extendEntityProperty.IdentifierText;
            string extendEntityClassName = extendEntityProperty.Type; 
            SoftClass extendEntityClass = allEntityClasses.Where(x => x.Name == extendEntityClassName).Single();
            string extendEntityIdType = Helper.GetGenericIdType(extendEntityClass, allEntityClasses);

            return $$"""
        /// <summary>
        /// Call this method when you have additional fields in M2M association
        /// </summary>
        public async Task Update{{extendEntityClassName}}ListFor{{mainEntityClassName}}({{mainEntityIdType}} {{mainEntityPropertyName.FirstCharToLower()}}Id, List<{{nameOfTheEntityClass}}DTO> selected{{nameOfTheEntityClass}}DTOList)
        {
            if (selected{{nameOfTheEntityClass}}DTOList == null)
                return;

            List<{{nameOfTheEntityClass}}DTO> selectedDTOListHelper = selected{{nameOfTheEntityClass}}DTOList.ToList();

            await _context.WithTransactionAsync(async () =>
            {
                // FT: Not doing authorization here, because we can not figure out here if we are updating while inserting object (eg. User), or updating object, we will always get the id which is not 0 here.

                DbSet<{{nameOfTheEntityClass}}> dbSet = _context.DbSet<{{nameOfTheEntityClass}}>();
                List<{{nameOfTheEntityClass}}> {{nameOfTheEntityClassFirstLower}}List = await dbSet.Where(x => x.{{mainEntityPropertyName}}.Id == {{mainEntityPropertyName.FirstCharToLower()}}Id).ToListAsync();

                foreach ({{nameOfTheEntityClass}}DTO selected{{nameOfTheEntityClass}}DTO in selectedDTOListHelper)
                {
                    {{nameOfTheEntityClass}}DTOValidationRules validationRules = new {{nameOfTheEntityClass}}DTOValidationRules();
                    DefaultValidatorExtensions.ValidateAndThrow(validationRules, selected{{nameOfTheEntityClass}}DTO);

                    {{nameOfTheEntityClass}} {{nameOfTheEntityClassFirstLower}} = {{nameOfTheEntityClassFirstLower}}List.Where(x => x.{{extendEntityPropertyName}}.Id == selected{{nameOfTheEntityClass}}DTO.{{extendEntityPropertyName}}Id).SingleOrDefault();

                    if ({{nameOfTheEntityClassFirstLower}} == null)
                    {
                        {{nameOfTheEntityClassFirstLower}} = TypeAdapter.Adapt<{{nameOfTheEntityClass}}>(selected{{nameOfTheEntityClass}}DTO, Mapper.{{nameOfTheEntityClass}}DTOToEntityConfig());
                        {{nameOfTheEntityClassFirstLower}}.{{mainEntityPropertyName}} = await LoadInstanceAsync<{{mainEntityClassName}}, {{mainEntityIdType}}>({{mainEntityPropertyName.FirstCharToLower()}}Id, null);
                        {{nameOfTheEntityClassFirstLower}}.{{extendEntityPropertyName}} = await LoadInstanceAsync<{{extendEntityClassName}}, {{extendEntityIdType}}>(selected{{nameOfTheEntityClass}}DTO.{{extendEntityPropertyName}}Id.Value, null);
                        dbSet.Add({{nameOfTheEntityClassFirstLower}});
                    }
                    else
                    {
                        selected{{nameOfTheEntityClass}}DTO.Adapt({{nameOfTheEntityClassFirstLower}}, Mapper.{{nameOfTheEntityClass}}DTOToEntityConfig());
                        dbSet.Update({{nameOfTheEntityClassFirstLower}});

                        {{nameOfTheEntityClassFirstLower}}List.Remove({{nameOfTheEntityClassFirstLower}});
                    }
                }

                dbSet.RemoveRange({{nameOfTheEntityClassFirstLower}}List);

                await _context.SaveChangesAsync();
            });
        }
""";
        }

        #endregion

        #region Helpers

        private static string GetUsings(string basePartOfTheNamespace)
        {
            return $$"""
using {{basePartOfTheNamespace}}.ValidationRules;
using {{basePartOfTheNamespace}}.DataMappers;
using {{basePartOfTheNamespace}}.DTO;
using {{basePartOfTheNamespace}}.Entities;
using {{basePartOfTheNamespace}}.Enums;
using {{basePartOfTheNamespace}}.ExcelProperties;
using {{basePartOfTheNamespace}}.TableFiltering;
using Microsoft.EntityFrameworkCore;
using System.Data;
using FluentValidation;
using Soft.Generator.Security.Services;
using Soft.Generator.Shared.Excel;
using Soft.Generator.Shared.Interfaces;
using Soft.Generator.Shared.Services;
using Soft.Generator.Shared.DTO;
using Soft.Generator.Shared.Entities;
using Soft.Generator.Shared.Extensions;
using Soft.Generator.Shared.SoftExceptions;
using Soft.Generator.Shared.Terms;
using Mapster;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.Http;
""";
        }

        #endregion

    }
}