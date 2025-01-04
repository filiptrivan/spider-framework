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

            string[] namespacePartsWithoutLastElement = Helper.GetNamespacePartsWithoutLastElement(entityClasses[0].Namespace);

            string basePartOfTheNamespace = string.Join(".", namespacePartsWithoutLastElement); // eg. Soft.Generator.Security
            string projectName = namespacePartsWithoutLastElement[namespacePartsWithoutLastElement.Length - 1]; // eg. Security

            bool isSecurityProject = projectName == "Security";

            string result = $$"""
{{GetUsings(basePartOfTheNamespace)}}

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

{{string.Join("\n\n", GetBusinessServiceMethods(entityClasses, allEntityClasses))}}

    }
}
""";

            context.AddSource($"BusinessService.generated", SourceText.From(result, Encoding.UTF8));
        }

        private static List<string> GetBusinessServiceMethods(List<SoftClass> entityClasses, List<SoftClass> allEntityClasses)
        {
            List<string> result = new List<string>();

            foreach (SoftClass entity in entityClasses)
            {
                if (entity.IsManyToMany())
                {
                    result.Add($$"""
        #region {{entity.Name}} - M2M

{{GetManyToManyData(entity, allEntityClasses)}}

        #endregion
""");
                }
                else
                {
                result.Add($$"""
        #region {{entity.Name}}

        #region Read

{{GetReadBusinessServiceMethods(entity, allEntityClasses)}}

        #endregion

        #region Save

{{GetSavingData(entity, allEntityClasses)}}

{{string.Join("\n\n", GetUploadBlobMethods(entity, allEntityClasses))}}
        
        #endregion

        #region Delete

{{GetDeletingData(entity, allEntityClasses)}}

        #endregion

        #region Enumerable

{{string.Join("\n\n", GetEnumerableGeneratedMethods(entity, allEntityClasses))}}

        #endregion

        #endregion
""");
                }
            }

            return result;
        }

        #region Read

        private static string GetReadBusinessServiceMethods(SoftClass entity, List<SoftClass> allEntityClasses)
        {
            string entityIdType = Helper.GetIdType(entity, allEntityClasses);
            string entityDisplayNameProperty = Helper.GetDisplayNamePropForClass(entity);

            return $$"""
        public async Task<{{entity.Name}}DTO> Get{{entity.Name}}DTOAsync({{entityIdType}} id, bool authorize = true)
        {
            return await _context.WithTransactionAsync(async () =>
            {
                if (authorize) 
                {
                    await _authorizationService.{{entity.Name}}SingleReadAuthorize(id);
                }

                {{entity.Name}}DTO dto = await _context.DbSet<{{entity.Name}}>().AsNoTracking().Where(x => x.Id == id).ProjectToType<{{entity.Name}}DTO>(Mapper.{{entity.Name}}ProjectToConfig()).SingleOrDefaultAsync();

                if (dto == null)
                    throw new BusinessException(SharedTerms.EntityDoesNotExistInDatabase);

{{GetPopulateDTOWithBlobPartsForDTO(entity, entity.Properties)}}

                return dto;
            });
        }

        public async Task<PaginationResult<{{entity.Name}}>> Load{{entity.Name}}ListForPagination(TableFilterDTO tableFilterPayload, IQueryable<{{entity.Name}}> query)
        {
            return await _context.WithTransactionAsync(async () =>
            {
                return await TableFilterQueryable.Build(query.AsNoTracking(), tableFilterPayload);
            });
        }

        public async virtual Task<TableResponseDTO<{{entity.Name}}DTO>> Load{{entity.Name}}TableData(TableFilterDTO tableFilterPayload, IQueryable<{{entity.Name}}> query, bool authorize = true)
        {
            PaginationResult<{{entity.Name}}> paginationResult = new PaginationResult<{{entity.Name}}>();
            List<{{entity.Name}}DTO> data = null;

            await _context.WithTransactionAsync(async () =>
            {
                if (authorize) 
                {
                    await _authorizationService.{{entity.Name}}ListReadAuthorize();
                }

                paginationResult = await Load{{entity.Name}}ListForPagination(tableFilterPayload, query);

                data = await paginationResult.Query
                    .Skip(tableFilterPayload.First)
                    .Take(tableFilterPayload.Rows)
                    .ProjectToType<{{entity.Name}}DTO>(Mapper.{{entity.Name}}ProjectToConfig())
                    .ToListAsync();
            });

            return new TableResponseDTO<{{entity.Name}}DTO> { Data = data, TotalRecords = paginationResult.TotalRecords };
        }

        public async Task<byte[]> Export{{entity.Name}}TableDataToExcel(TableFilterDTO tableFilterPayload, IQueryable<{{entity.Name}}> query, bool authorize = true)
        {
            PaginationResult<{{entity.Name}}> paginationResult = new PaginationResult<{{entity.Name}}>();
            List<{{entity.Name}}DTO> data = null;

            await _context.WithTransactionAsync(async () =>
            {
                if (authorize)
                {
                    await _authorizationService.{{entity.Name}}ListReadAuthorize();
                }

                paginationResult = await Load{{entity.Name}}ListForPagination(tableFilterPayload, query);

                data = await paginationResult.Query.ProjectToType<{{entity.Name}}DTO>(Mapper.{{entity.Name}}ExcelProjectToConfig()).ToListAsync();
            });

            string[] excelPropertiesToExclude = ExcelPropertiesToExclude.GetHeadersToExclude(new {{entity.Name}}DTO());
            return _excelService.FillReportTemplate<{{entity.Name}}DTO>(data, paginationResult.TotalRecords, excelPropertiesToExclude).ToArray();
        }

        public async virtual Task<List<NamebookDTO<{{entityIdType}}>>> Load{{entity.Name}}ListForAutocomplete(int limit, string query, IQueryable<{{entity.Name}}> {{entity.Name.FirstCharToLower()}}Query, bool authorize = true)
        {
            return await _context.WithTransactionAsync(async () =>
            {
                if (authorize)
                {
                    await _authorizationService.{{entity.Name}}ListReadAuthorize();
                }

                if (!string.IsNullOrEmpty(query))
                    {{entity.Name.FirstCharToLower()}}Query = {{entity.Name.FirstCharToLower()}}Query.Where(x => x.{{entityDisplayNameProperty}}.Contains(query));

                return await {{entity.Name.FirstCharToLower()}}Query
                    .AsNoTracking()
                    .Take(limit)
                    .Select(x => new NamebookDTO<{{entityIdType}}>
                    {
                        Id = x.Id,
                        DisplayName = x.{{entityDisplayNameProperty}},
                    })
                    .ToListAsync();
            });
        }

        public async virtual Task<List<NamebookDTO<{{entityIdType}}>>> Load{{entity.Name}}ListForDropdown(IQueryable<{{entity.Name}}> {{entity.Name.FirstCharToLower()}}Query, bool authorize = true)
        {
            return await _context.WithTransactionAsync(async () =>
            {
                if (authorize)
                {
                    await _authorizationService.{{entity.Name}}ListReadAuthorize();
                }

                return await {{entity.Name.FirstCharToLower()}}Query
                    .AsNoTracking()
                    .Select(x => new NamebookDTO<{{entityIdType}}>
                    {
                        Id = x.Id,
                        DisplayName = x.{{entityDisplayNameProperty}},
                    })
                    .ToListAsync();
            });
        }

        public async Task<List<{{entity.Name}}>> Load{{entity.Name}}List(IQueryable<{{entity.Name}}> {{entity.Name.FirstCharToLower()}}Query, bool authorize = true)
        {
            return await _context.WithTransactionAsync(async () =>
            {
                if (authorize)
                {
                    await _authorizationService.{{entity.Name}}ListReadAuthorize();
                }

                return await {{entity.Name.FirstCharToLower()}}Query
                    .ToListAsync();
            });
        }

        public async Task<List<{{entity.Name}}DTO>> Load{{entity.Name}}DTOList(IQueryable<{{entity.Name}}> {{entity.Name.FirstCharToLower()}}Query, bool authorize = true)
        {
            return await _context.WithTransactionAsync(async () =>
            {
                if (authorize)
                {
                    await _authorizationService.{{entity.Name}}ListReadAuthorize();
                }

                List<{{entity.Name}}DTO> dtoList = await {{entity.Name.FirstCharToLower()}}Query
                    .AsNoTracking()
                    .ProjectToType<{{entity.Name}}DTO>(Mapper.{{entity.Name}}ToDTOConfig())
                    .ToListAsync();

{{GetPopulateDTOWithBlobPartsForDTOList(entity, entity.Properties)}}

                return dtoList;
            });
        }
""";
        }

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

        #region Save

        static string GetSavingData(SoftClass entity, List<SoftClass> allEntityClasses)
        {
            if (entity.IsAbstract || entity.IsReadonlyObject())
                return null;

            string entityIdType = Helper.GetIdType(entity, allEntityClasses);

            return $$"""
        public async Task<{{entity.Name}}DTO> Save{{entity.Name}}AndReturnDTOAsync({{entity.Name}}DTO {{entity.Name.FirstCharToLower()}}DTO, bool authorizeUpdate = true, bool authorizeInsert = true)
        {
            return await _context.WithTransactionAsync(async () =>
            {
                {{entity.Name}} poco = await Save{{entity.Name}}AndReturnDomainAsync({{entity.Name.FirstCharToLower()}}DTO, authorizeUpdate, authorizeInsert);

                return poco.Adapt<{{entity.Name}}DTO>(Mapper.{{entity.Name}}ToDTOConfig());
            });
        }

        public async Task<{{entity.Name}}> Save{{entity.Name}}AndReturnDomainAsync({{entity.Name}}DTO dto, bool authorizeUpdate = true, bool authorizeInsert = true)
        {
            {{entity.Name}}DTOValidationRules validationRules = new {{entity.Name}}DTOValidationRules();
            validationRules.ValidateAndThrow(dto);

            {{entity.Name}} poco = null;
            await _context.WithTransactionAsync(async () =>
            {
                await OnBefore{{entity.Name}}IsMapped(dto);
                DbSet<{{entity.Name}}> dbSet = _context.DbSet<{{entity.Name}}>();

                if (dto.Id > 0)
                {
                    if (authorizeUpdate)
                    {
                        await _authorizationService.{{entity.Name}}SingleUpdateAuthorize(dto);
                    }

                    poco = await LoadInstanceAsync<{{entity.Name}}, {{entityIdType}}>(dto.Id, dto.Version);
                    await OnBefore{{entity.Name}}Update(poco, dto);
                    dto.Adapt(poco, Mapper.{{entity.Name}}DTOToEntityConfig());
                    dbSet.Update(poco);
                }
                else
                {
                    if (authorizeInsert)
                    {
                        await _authorizationService.{{entity.Name}}SingleInsertAuthorize(dto);
                    }

                    poco = dto.Adapt<{{entity.Name}}>(Mapper.{{entity.Name}}DTOToEntityConfig());
                    await dbSet.AddAsync(poco);
                }

{{string.Join("\n", GetManyToOneInstancesForSave(entity, allEntityClasses))}}

                await _context.SaveChangesAsync();

{{string.Join("\n", GetNonActiveDeleteBlobMethods(entity))}}
            });

            return poco;
        }

        protected virtual async Task OnBefore{{entity.Name}}IsMapped({{entity.Name}}DTO {{entity.Name.FirstCharToLower()}}DTO) { }

        protected virtual async Task OnBefore{{entity.Name}}Update({{entity.Name}} {{entity.Name.FirstCharToLower()}}, {{entity.Name}}DTO {{entity.Name.FirstCharToLower()}}DTO) { }
""";
        }

        static List<string> GetManyToOneInstancesForSave(SoftClass entityClass, List<SoftClass> allEntityClasses)
        {
            List<string> result = new List<string>();

            List<SoftProperty> properties = entityClass.Properties
                .Where(prop => prop.Type.IsManyToOneType())
                .ToList();

            foreach (SoftProperty prop in properties)
            {
                SoftClass classOfManyToOneProperty = GetClassOfManyToOneProperty(prop.Type, allEntityClasses);

                if (classOfManyToOneProperty == null)
                    continue;

                if (classOfManyToOneProperty.IsBusinessObject() || classOfManyToOneProperty.IsReadonlyObject() == false)
                {
                    result.Add($$"""
            if (dto.{{prop.IdentifierText}}Id > 0)
                poco.{{prop.IdentifierText}} = await LoadInstanceAsync<{{prop.Type}}, {{Helper.GetIdType(classOfManyToOneProperty, allEntityClasses)}}>(dto.{{prop.IdentifierText}}Id.Value, null);
            else
                poco.{{prop.IdentifierText}} = null;
""");
                }
                else
                {
                    result.Add($$"""
            if (dto.{{prop.IdentifierText}}Id > 0)
                poco.{{prop.IdentifierText}} = await LoadInstanceAsync<{{prop.Type}}, {{Helper.GetIdType(classOfManyToOneProperty, allEntityClasses)}}>(dto.{{prop.IdentifierText}}Id.Value);
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

        private static List<string> GetNonActiveDeleteBlobMethods(SoftClass entity)
        {
            List<string> result = new List<string>();

            List<SoftProperty> blobProperies = Helper.GetBlobProperties(entity.Properties);

            foreach (SoftProperty property in blobProperies)
            {
                result.Add($$"""
                await DeleteNonActiveBlobs(dto.{{property.IdentifierText}}, nameof({{entity.Name}}), nameof({{entity.Name}}.{{property.IdentifierText}}), poco.Id.ToString());
""");
            }

            return result;
        }

        private static List<string> GetUploadBlobMethods(SoftClass entity, List<SoftClass> allEntityClasses)
        {
            List<string> result = new List<string>();

            string entityIdType = Helper.GetIdType(entity, allEntityClasses);

            List<SoftProperty> blobProperies = Helper.GetBlobProperties(entity.Properties);

            foreach (SoftProperty property in blobProperies)
            {
                result.Add($$"""
        public async Task<string> Upload{{entity.Name}}{{property.IdentifierText}}Async(IFormFile file, bool authorizeUpdate = true, bool authorizeInsert = true) // FT: It doesn't work without interface
        {
            using Stream stream = file.OpenReadStream();

            {{entityIdType}} {{entity.Name.FirstCharToLower()}}Id = GetObjectIdFromFileName<{{entityIdType}}>(file.FileName);

            if ({{entity.Name.FirstCharToLower()}}Id > 0)
            {
                if (authorizeUpdate)
                {
                    await _authorizationService.{{entity.Name}}SingleUpdateAuthorize({{entity.Name.FirstCharToLower()}}Id);
                }
            }
            else
            {
                if (authorizeInsert)
                {
                    await _authorizationService.{{entity.Name}}SingleInsertAuthorize();
                }
            }

            OnBefore{{entity.Name}}{{property.IdentifierText}}BlobIsUploaded({{entity.Name.FirstCharToLower()}}Id); // FT: Authorize access for this id...

            string fileName = await UploadFileAsync(file.FileName, nameof({{entity.Name}}), nameof({{entity.Name}}.{{property.IdentifierText}}), {{entity.Name.FirstCharToLower()}}Id.ToString(), stream);

            return fileName;
        }

        public virtual async Task OnBefore{{entity.Name}}{{property.IdentifierText}}BlobIsUploaded ({{entityIdType}} {{entity.Name.FirstCharToLower()}}Id) { }
"""
);
            }

            return result;
        }

        #endregion

        #region Delete

        private static string GetDeletingData(SoftClass entity, List<SoftClass> allEntityClasses)
        {
            if (entity.IsAbstract || entity.IsReadonlyObject())
                return null;

            string entityIdType = Helper.GetIdType(entity, allEntityClasses);

            return $$"""
        public virtual async Task OnBefore{{entity.Name}}AsyncDelete({{entityIdType}} {{entity.Name.FirstCharToLower()}}Id) { }

        public async Task Delete{{entity.Name}}Async({{entityIdType}} {{entity.Name.FirstCharToLower()}}Id, bool authorize = true)
        {
            await _context.WithTransactionAsync(async () =>
            {
                if (authorize)
                {
                    await _authorizationService.{{entity.Name}}DeleteAuthorize({{entity.Name.FirstCharToLower()}}Id);
                }

                await OnBefore{{entity.Name}}AsyncDelete({{entity.Name.FirstCharToLower()}}Id);

{{string.Join("\n", GetManyToOneDeleteQueries(entity, allEntityClasses, null, 0))}}

                await DeleteEntityAsync<{{entity.Name}}, {{entityIdType}}>({{entity.Name.FirstCharToLower()}}Id);
            });
        }

        public virtual async Task OnBefore{{entity.Name}}ListAsyncDelete(List<{{entityIdType}}> {{entity.Name.FirstCharToLower()}}ListToDelete) { }

        public async Task Delete{{entity.Name}}ListAsync(List<{{entityIdType}}> {{entity.Name.FirstCharToLower()}}ListToDelete, bool authorize = true)
        {
            await _context.WithTransactionAsync(async () =>
            {
                if (authorize)
                {
                    await _authorizationService.{{entity.Name}}ListDeleteAuthorize({{entity.Name.FirstCharToLower()}}ListToDelete);
                }

                await OnBefore{{entity.Name}}ListAsyncDelete({{entity.Name.FirstCharToLower()}}ListToDelete);

{{string.Join("\n", GetManyToOneDeleteQueriesForList(entity, allEntityClasses, null, 0))}}

                await DeleteEntitiesAsync<{{entity.Name}}, {{entityIdType}}>({{entity.Name.FirstCharToLower()}}ListToDelete);
            });
        }
""";
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
                string nestedEntityClassIdType = Helper.GetIdType(nestedEntityClass, allEntityClasses);

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
                string nestedEntityClassIdType = Helper.GetIdType(nestedEntityClass, allEntityClasses);

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

        #region Enumerable

        static List<string> GetEnumerableGeneratedMethods(SoftClass entityClass, List<SoftClass> allEntityClasses)
        {
            string nameOfTheEntityClass = entityClass.Name; // User
            string nameOfTheEntityClassFirstLower = entityClass.Name.FirstCharToLower(); // user
            string idTypeOfTheEntityClass = Helper.GetIdType(entityClass, allEntityClasses); // long

            List<SoftProperty> enumerablePropertiesOfTheEntityClass = entityClass.Properties
                .Where(prop => prop.Type.IsEnumerable())
                .ToList();

            List<string> result = new List<string>();

            foreach (SoftProperty prop in enumerablePropertiesOfTheEntityClass) // List<Role> Roles
            {
                string classNameFromTheList = GetClassNameFromTheEnumerableType(prop.Type); // Role
                string classNameFromTheListFirstLower = classNameFromTheList.FirstCharToLower(); // role
                SoftClass classFromTheList = allEntityClasses.Where(x => x.Name == classNameFromTheList).Single(); // Role

                //ClassDeclarationSyntax classFromTheListFromTheReferencedProjects = referencedClassesEntities.Where(x => x.Identifier.Text == classNameFromTheList).SingleOrDefault();
                //if (classFromTheList == null) // && classFromTheListFromTheReferencedProjects == null  // TODO FT: Continue to do this...
                //{
                    //continue;
                    //result.Add("INVALID ENTITY CLASS, YOU CAN'T MAKE LIST OF NO ENTITY CLASS"); // FT: It can, if the class is from another project.
                //}

                string idTypeOfTheClassFromTheList = Helper.GetIdType(classFromTheList, allEntityClasses); // int

                if (idTypeOfTheClassFromTheList == null) // FT: M2M List, maybe do something else in the future.
                    continue;

                string classNameFromTheListDisplayNameProp = Helper.GetDisplayNamePropForClass(classFromTheList); // Name
                //classNameFromTheListDisplayNameProp = classNameFromTheListDisplayNameProp ?? classFromTheListFromTheReferencedProjects.Att.Where(x => ).FirstOrDefault() // TODO FT: Continue to do this...
                List<SoftProperty> classFromTheListProperties = classFromTheList.Properties;

                SoftProperty manyToManyPropFromTheListProperties = classFromTheListProperties.Where(x => x.Type.IsEnumerable() && GetClassNameFromTheEnumerableType(x.Type) == nameOfTheEntityClass).SingleOrDefault(); // List<User> Users
                SoftProperty manyToOneProp = classFromTheListProperties.Where(x => x.Type.IsManyToOneType() && prop.Type == nameOfTheEntityClass).SingleOrDefault(); // User / Userando

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

                {{((entityClass.IsBusinessObject() || entityClass.IsReadonlyObject() == false)
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

        /// <summary>
        /// It's mandatory to pass queryable ordered by the same field as the table data
        /// </summary>
        public async Task<LazyLoadSelectedIdsResultDTO<{{idTypeOfTheClassFromTheList}}>> LazyLoadSelected{{classNameFromTheList}}IdsFor{{nameOfTheEntityClass}}(TableFilterDTO tableFilterDTO, IQueryable<{{classNameFromTheList}}> {{classNameFromTheListFirstLower}}Query)
        {
            LazyLoadSelectedIdsResultDTO<{{idTypeOfTheClassFromTheList}}> lazyLoadSelectedIdsResultDTO = new();

            {{classNameFromTheListFirstLower}}Query = {{classNameFromTheListFirstLower}}Query
                .Skip(tableFilterDTO.First)
                .Take(tableFilterDTO.Rows)
                .Where(x => x.{{manyToManyPropFromTheListProperties.IdentifierText}}
                    .Any(x => x.Id == tableFilterDTO.{{idTypeOfTheClassFromTheList.GetTableFilterAdditionalFilterPropertyName()}}));

            await _context.WithTransactionAsync(async () =>
            {
                PaginationResult<{{classNameFromTheList}}> paginationResult = await Load{{classNameFromTheList}}ListForPagination(tableFilterDTO, {{classNameFromTheListFirstLower}}Query);

                lazyLoadSelectedIdsResultDTO.SelectedIds = await paginationResult.Query
                    .Select(x => x.Id)
                    .ToListAsync();

                int count = await _context.DbSet<{{classNameFromTheList}}>()
                    .Where(x => x.{{manyToManyPropFromTheListProperties.IdentifierText}}
                        .Any(x => x.Id == tableFilterDTO.{{idTypeOfTheClassFromTheList.GetTableFilterAdditionalFilterPropertyName()}}))
                    .CountAsync();

                lazyLoadSelectedIdsResultDTO.TotalRecordsSelected = count;
            });

            return lazyLoadSelectedIdsResultDTO;
        }

        public async Task Update{{classNameFromTheList}}ListFor{{nameOfTheEntityClass}}WithLazyTableSelection(IQueryable<{{classNameFromTheList}}> {{classNameFromTheListFirstLower}}Query, {{idTypeOfTheEntityClass}} {{nameOfTheEntityClassFirstLower}}Id, LazyTableSelectionDTO<{{idTypeOfTheClassFromTheList}}> lazyTableSelectionDTO)
        {
            await _context.WithTransactionAsync(async () =>
            {
                List<{{idTypeOfTheClassFromTheList}}> {{classNameFromTheListFirstLower}}ListToInsert = null;

                if (lazyTableSelectionDTO.IsAllSelected == true)
                {
                    {{classNameFromTheListFirstLower}}ListToInsert = await {{classNameFromTheListFirstLower}}Query.Where(x => lazyTableSelectionDTO.UnselectedIds.Contains(x.Id) == false).Select(x => x.Id).ToListAsync();
                }
                else if (lazyTableSelectionDTO.IsAllSelected == false)
                {
                    {{classNameFromTheListFirstLower}}ListToInsert = await {{classNameFromTheListFirstLower}}Query.Where(x => lazyTableSelectionDTO.SelectedIds.Contains(x.Id) == true).Select(x => x.Id).ToListAsync();
                }
                else if (lazyTableSelectionDTO.IsAllSelected == null)
                {
                    {{((entityClass.IsBusinessObject() || entityClass.IsReadonlyObject() == false)
                    ? $"{nameOfTheEntityClass} {nameOfTheEntityClassFirstLower} = await LoadInstanceAsync<{nameOfTheEntityClass}, {idTypeOfTheEntityClass}>({nameOfTheEntityClassFirstLower}Id, null); // FT: Version will always be checked before or after this method"
                    : $"{nameOfTheEntityClass} {nameOfTheEntityClassFirstLower} = await LoadInstanceAsync<{nameOfTheEntityClass}, {idTypeOfTheEntityClass}>({nameOfTheEntityClassFirstLower}Id);"
                    )}}

                    List<{{idTypeOfTheClassFromTheList}}> alreadySelected = {{nameOfTheEntityClassFirstLower}}.{{prop.IdentifierText}} == null ? new List<{{idTypeOfTheClassFromTheList}}>() : {{nameOfTheEntityClassFirstLower}}.{{prop.IdentifierText}}.Select(x => x.Id).ToList();

                    {{classNameFromTheListFirstLower}}ListToInsert = alreadySelected
                        .Union(lazyTableSelectionDTO.SelectedIds)
                        .Except(lazyTableSelectionDTO.UnselectedIds)
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

        private static string GetClassNameFromTheEnumerableType(string propType)
        {
            string[] parts = propType.Split('<');
            return parts[parts.Length-1].Replace(">", "");
        }

        #endregion

        #region M2M

        private static string GetManyToManyData(SoftClass entity, List<SoftClass> allEntityClasses)
        {
            if (entity.Properties.Count == Settings.NumberOfPropertiesWithoutAdditionalManyToManyProperties)
                return null;

            List<SoftProperty> manyToManyProperties = entity.Properties;

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
            string mainEntityIdType = Helper.GetIdType(mainEntityClass, allEntityClasses);

            string extendEntityPropertyName = extendEntityProperty.IdentifierText;
            string extendEntityClassName = extendEntityProperty.Type; 
            SoftClass extendEntityClass = allEntityClasses.Where(x => x.Name == extendEntityClassName).Single();
            string extendEntityIdType = Helper.GetIdType(extendEntityClass, allEntityClasses);

            return $$"""
        /// <summary>
        /// Call this method when you have additional fields in M2M association
        /// </summary>
        public async Task Update{{extendEntityClassName}}ListFor{{mainEntityClassName}}({{mainEntityIdType}} {{mainEntityPropertyName.FirstCharToLower()}}Id, List<{{entity.Name}}DTO> selected{{entity.Name}}DTOList)
        {
            if (selected{{entity.Name}}DTOList == null)
                return;

            List<{{entity.Name}}DTO> selectedDTOListHelper = selected{{entity.Name}}DTOList.ToList();

            await _context.WithTransactionAsync(async () =>
            {
                // FT: Not doing authorization here, because we can not figure out here if we are updating while inserting object (eg. User), or updating object, we will always get the id which is not 0 here.

                DbSet<{{entity.Name}}> dbSet = _context.DbSet<{{entity.Name}}>();
                List<{{entity.Name}}> {{entity.Name.FirstCharToLower()}}List = await dbSet.Where(x => x.{{mainEntityPropertyName}}.Id == {{mainEntityPropertyName.FirstCharToLower()}}Id).ToListAsync();

                foreach ({{entity.Name}}DTO selected{{entity.Name}}DTO in selectedDTOListHelper)
                {
                    {{entity.Name}}DTOValidationRules validationRules = new {{entity.Name}}DTOValidationRules();
                    DefaultValidatorExtensions.ValidateAndThrow(validationRules, selected{{entity.Name}}DTO);

                    {{entity.Name}} {{entity.Name.FirstCharToLower()}} = {{entity.Name.FirstCharToLower()}}List.Where(x => x.{{extendEntityPropertyName}}.Id == selected{{entity.Name}}DTO.{{extendEntityPropertyName}}Id).SingleOrDefault();

                    if ({{entity.Name.FirstCharToLower()}} == null)
                    {
                        {{entity.Name.FirstCharToLower()}} = TypeAdapter.Adapt<{{entity.Name}}>(selected{{entity.Name}}DTO, Mapper.{{entity.Name}}DTOToEntityConfig());
                        {{entity.Name.FirstCharToLower()}}.{{mainEntityPropertyName}} = await LoadInstanceAsync<{{mainEntityClassName}}, {{mainEntityIdType}}>({{mainEntityPropertyName.FirstCharToLower()}}Id, null);
                        {{entity.Name.FirstCharToLower()}}.{{extendEntityPropertyName}} = await LoadInstanceAsync<{{extendEntityClassName}}, {{extendEntityIdType}}>(selected{{entity.Name}}DTO.{{extendEntityPropertyName}}Id.Value, null);
                        dbSet.Add({{entity.Name.FirstCharToLower()}});
                    }
                    else
                    {
                        selected{{entity.Name}}DTO.Adapt({{entity.Name.FirstCharToLower()}}, Mapper.{{entity.Name}}DTOToEntityConfig());
                        dbSet.Update({{entity.Name.FirstCharToLower()}});

                        {{entity.Name.FirstCharToLower()}}List.Remove({{entity.Name.FirstCharToLower()}});
                    }
                }

                dbSet.RemoveRange({{entity.Name.FirstCharToLower()}}List);

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
using Soft.Generator.Security.Interface;
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