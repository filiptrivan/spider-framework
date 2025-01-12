using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Soft.SourceGenerator.NgTable.Helpers;
using Soft.SourceGenerators;
using Soft.SourceGenerators.Enums;
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
            IncrementalValuesProvider<ClassDeclarationSyntax> classDeclarations = Helper.GetClassInrementalValuesProvider(context.SyntaxProvider, new List<NamespaceExtensionCodes>
                {
                    NamespaceExtensionCodes.Entities,
                });

            IncrementalValueProvider<List<SoftClass>> referencedProjectClasses = Helper.GetIncrementalValueProviderClassesFromReferencedAssemblies(context,
                new List<NamespaceExtensionCodes>
                {
                    NamespaceExtensionCodes.Entities,
                });

            var allClasses = classDeclarations.Collect()
                .Combine(referencedProjectClasses);

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

{{string.Join("\n\n", GetOneToManyMethods(entity, allEntityClasses))}}

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

        public async Task<PaginationResult<{{entity.Name}}>> Get{{entity.Name}}ListForPagination(TableFilterDTO tableFilterPayload, IQueryable<{{entity.Name}}> query)
        {
            return await _context.WithTransactionAsync(async () =>
            {
                return await TableFilterQueryable.Build(query.AsNoTracking(), tableFilterPayload);
            });
        }

        public async virtual Task<TableResponseDTO<{{entity.Name}}DTO>> Get{{entity.Name}}TableData(TableFilterDTO tableFilterPayload, IQueryable<{{entity.Name}}> query, bool authorize = true)
        {
            PaginationResult<{{entity.Name}}> paginationResult = new PaginationResult<{{entity.Name}}>();
            List<{{entity.Name}}DTO> data = null;

            await _context.WithTransactionAsync(async () =>
            {
                if (authorize) 
                {
                    await _authorizationService.{{entity.Name}}ListReadAuthorize();
                }

                paginationResult = await Get{{entity.Name}}ListForPagination(tableFilterPayload, query);

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

                paginationResult = await Get{{entity.Name}}ListForPagination(tableFilterPayload, query);

                data = await paginationResult.Query.ProjectToType<{{entity.Name}}DTO>(Mapper.{{entity.Name}}ExcelProjectToConfig()).ToListAsync();
            });

            string[] excelPropertiesToExclude = ExcelPropertiesToExclude.GetHeadersToExclude(new {{entity.Name}}DTO());
            return _excelService.FillReportTemplate<{{entity.Name}}DTO>(data, paginationResult.TotalRecords, excelPropertiesToExclude).ToArray();
        }

        public async virtual Task<List<NamebookDTO<{{entityIdType}}>>> Get{{entity.Name}}ListForAutocomplete(int limit, string query, IQueryable<{{entity.Name}}> {{entity.Name.FirstCharToLower()}}Query, bool authorize = true)
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

        public async virtual Task<List<NamebookDTO<{{entityIdType}}>>> Get{{entity.Name}}ListForDropdown(IQueryable<{{entity.Name}}> {{entity.Name.FirstCharToLower()}}Query, bool authorize = true)
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

        public async Task<List<{{entity.Name}}>> Get{{entity.Name}}List(IQueryable<{{entity.Name}}> {{entity.Name.FirstCharToLower()}}Query, bool authorize = true)
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

        public async Task<List<{{entity.Name}}DTO>> Get{{entity.Name}}DTOList(IQueryable<{{entity.Name}}> {{entity.Name.FirstCharToLower()}}Query, bool authorize = true)
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
                    if (!string.IsNullOrEmpty(dto.{{property.Name}}))
                    {
                        dto.{{property.Name}}Data = await GetFileDataAsync(dto.{{property.Name}});
                    }
""");
            }

            return blobParts;
        }

        #endregion

        #region Save

        static string GetSavingData(SoftClass entity, List<SoftClass> entities)
        {
            if (entity.IsAbstract || entity.IsReadonlyObject())
                return null;

            string entityIdType = Helper.GetIdType(entity, entities);

            return $$"""
{{GetSaveAndReturnSaveBodyDTOData(entity, entities)}}

        public async Task<{{entity.Name}}DTO> Save{{entity.Name}}AndReturnDTOAsync({{entity.Name}}DTO {{entity.Name.FirstCharToLower()}}DTO, bool authorizeUpdate = true, bool authorizeInsert = true)
        {
            return await _context.WithTransactionAsync(async () =>
            {
                var poco = await Save{{entity.Name}}AndReturnDomainAsync({{entity.Name.FirstCharToLower()}}DTO, authorizeUpdate, authorizeInsert);

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

                    poco = await GetInstanceAsync<{{entity.Name}}, {{entityIdType}}>(dto.Id, dto.Version);
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

{{string.Join("\n", GetManyToOneInstancesForSave(entity, entities))}}

                await _context.SaveChangesAsync();

{{string.Join("\n", GetNonActiveDeleteBlobMethods(entity))}}
            });

            return poco;
        }

        protected virtual async Task OnBefore{{entity.Name}}IsMapped({{entity.Name}}DTO {{entity.Name.FirstCharToLower()}}DTO) { }

        protected virtual async Task OnBefore{{entity.Name}}Update({{entity.Name}} {{entity.Name.FirstCharToLower()}}, {{entity.Name}}DTO {{entity.Name.FirstCharToLower()}}DTO) { }
""";
        }

        private static string GetSaveAndReturnSaveBodyDTOData(SoftClass entity, List<SoftClass> entities)
        {
            return $$"""
        public async Task<{{entity.Name}}SaveBodyDTO> Save{{entity.Name}}AndReturnSaveBodyDTOAsync({{entity.Name}}SaveBodyDTO saveBodyDTO, bool authorizeUpdate = true, bool authorizeInsert = true)
        {
            return await _context.WithTransactionAsync(async () =>
            {
                await OnBeforeSave{{entity.Name}}AndReturnSaveBodyDTO(saveBodyDTO);
                var saved{{entity.Name}}DTO = await Save{{entity.Name}}AndReturnDTOAsync(saveBodyDTO.{{entity.Name}}DTO, authorizeUpdate, authorizeInsert);
{{string.Join("\n", GetOrderedOneToManyUpdateVariables(entity, entities))}}

                var result = new {{entity.Name}}SaveBodyDTO
                {
                    {{entity.Name}}DTO = saved{{entity.Name}}DTO,
{{string.Join(",\n", GetOrderedOneToManySaveBodyDTOVariables(entity, entities))}}
                };

                return result;
            });
        }

{{string.Join("\n\n", GetOrderedOneToManyUpdateMethods(entity, entities))}}

        protected virtual async Task OnBeforeSave{{entity.Name}}AndReturnSaveBodyDTO({{entity.Name}}SaveBodyDTO {{entity.Name.FirstCharToLower()}}SaveBodyDTO) { }
""";
        }

        #region Ordered One To Many

        private static List<string> GetOrderedOneToManyUpdateVariables(SoftClass entity, List<SoftClass> entities)
        {
            List<string> result = new List<string>();

            foreach (SoftProperty property in entity.GetOrderedOneToManyProperties())
            {
                SoftClass extractedEntity = entities.Where(x => x.Name == Helper.ExtractTypeFromGenericType(property.Type)).SingleOrDefault();

                result.Add($$"""
                var saved{{property.Name}}DTO = await UpdateOrdered{{property.Name}}For{{entity.Name}}(saved{{entity.Name}}DTO.Id, saveBodyDTO.{{property.Name}}DTO);
""");
            }

            return result;
        }

        private static List<string> GetOrderedOneToManySaveBodyDTOVariables(SoftClass entity, List<SoftClass> entities)
        {
            List<string> result = new List<string>();

            foreach (SoftProperty property in entity.GetOrderedOneToManyProperties())
            {
                SoftClass extractedEntity = entities.Where(x => x.Name == Helper.ExtractTypeFromGenericType(property.Type)).SingleOrDefault();

                result.Add($$"""
                    {{property.Name}}DTO = saved{{property.Name}}DTO
""");
            }

            return result;
        }

        private static List<string> GetOrderedOneToManyUpdateMethods(SoftClass entity, List<SoftClass> entities)
        {
            List<string> result = new List<string>();

            foreach (SoftProperty property in entity.GetOrderedOneToManyProperties())
            {
                SoftClass extractedEntity = entities.Where(x => x.Name == Helper.ExtractTypeFromGenericType(property.Type)).SingleOrDefault();

                result.Add($$"""
        public async Task<List<{{extractedEntity.Name}}DTO>> UpdateOrdered{{property.Name}}For{{entity.Name}}({{Helper.GetIdType(entity, entities)}} id, List<{{extractedEntity.Name}}DTO> orderedItemsDTO)
        {
            List<{{Helper.GetIdType(extractedEntity, entities)}}> orderedItemIds = orderedItemsDTO.Select(x => x.Id).ToList();

{{GetOrderedOneToManyNonEmptyValidation(property, entity)}}

            return await _context.WithTransactionAsync(async () =>
            {
                await _context.DbSet<{{extractedEntity.Name}}>().Where(x => x.{{extractedEntity.GetManyToOnePropertyWithManyAttribute(entity.Name, property.Name)?.Name}}.Id == id && orderedItemIds.Contains(x.Id) == false).ExecuteDeleteAsync();

                List<{{extractedEntity.Name}}DTO> savedOrderedItemsDTO = new List<{{extractedEntity.Name}}DTO>();

                for (int i = 0; i < orderedItemsDTO.Count; i++)
                {
                    orderedItemsDTO[i].{{extractedEntity.GetManyToOnePropertyWithManyAttribute(entity.Name, property.Name)?.Name}}Id = id;
                    orderedItemsDTO[i].OrderNumber = i + 1;
                    savedOrderedItemsDTO.Add(await Save{{extractedEntity.Name}}AndReturnDTOAsync(orderedItemsDTO[i], false, false));
                }

                return savedOrderedItemsDTO;
            });
        } 
""");
            }

            return result;
        }

        private static string GetOrderedOneToManyNonEmptyValidation(SoftProperty property, SoftClass entity)
        {
            if (property.IsNonEmpty())
            {
                return $$"""
            if (orderedItemIds.Count == 0)
                throw new HackerException("The ordered {{property.Name}} for {{entity.Name}} can't be empty.");
""";

            }

            return null;
        }

        #endregion

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
            if (dto.{{prop.Name}}Id > 0)
                poco.{{prop.Name}} = await GetInstanceAsync<{{prop.Type}}, {{Helper.GetIdType(classOfManyToOneProperty, allEntityClasses)}}>(dto.{{prop.Name}}Id.Value, null);
            else
                poco.{{prop.Name}} = null;
""");
                }
                else
                {
                    result.Add($$"""
            if (dto.{{prop.Name}}Id > 0)
                poco.{{prop.Name}} = await GetInstanceAsync<{{prop.Type}}, {{Helper.GetIdType(classOfManyToOneProperty, allEntityClasses)}}>(dto.{{prop.Name}}Id.Value);
            else
                poco.{{prop.Name}} = null;
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
                await DeleteNonActiveBlobs(dto.{{property.Name}}, nameof({{entity.Name}}), nameof({{entity.Name}}.{{property.Name}}), poco.Id.ToString());
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
        public async Task<string> Upload{{property.Name}}For{{entity.Name}}Async(IFormFile file, bool authorizeUpdate = true, bool authorizeInsert = true) // FT: It doesn't work without interface
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

            OnBefore{{property.Name}}BlobFor{{entity.Name}}IsUploaded({{entity.Name.FirstCharToLower()}}Id); // FT: Authorize access for this id...

            string fileName = await UploadFileAsync(file.FileName, nameof({{entity.Name}}), nameof({{entity.Name}}.{{property.Name}}), {{entity.Name.FirstCharToLower()}}Id.ToString(), stream);

            return fileName;
        }

        public virtual async Task OnBefore{{property.Name}}BlobFor{{entity.Name}}IsUploaded ({{entityIdType}} {{entity.Name.FirstCharToLower()}}Id) { }
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
                SoftClass nestedEntityClass = allEntityClasses.Where(x => x.Name == prop.EntityName).SingleOrDefault();
                string nestedEntityClassName = nestedEntityClass.Name;
                string nestedEntityClassNameLowerCase = nestedEntityClassName.FirstCharToLower();
                string nestedEntityClassIdType = Helper.GetIdType(nestedEntityClass, allEntityClasses);

                if (recursiveIteration == 0)
                {
                    result.Add($$"""
                List<{{nestedEntityClassIdType}}> {{nameOfTheEntityClassFirstLower}}{{nestedEntityClassName}}ListToDelete = await _context.DbSet<{{nestedEntityClassName}}>().Where(x => x.{{prop.Name}}.Id == {{nameOfTheEntityClassFirstLower}}Id).Select(x => x.Id).ToListAsync();
""");
                }
                else
                {
                    result.Add($$"""
                List<{{nestedEntityClassIdType}}> {{nameOfTheEntityClassFirstLower}}{{nestedEntityClassName}}ListToDelete = await _context.DbSet<{{nestedEntityClassName}}>().Where(x => {{parentNameOfTheEntityClass.FirstCharToLower()}}{{nameOfTheEntityClass}}ListToDelete.Contains(x.{{prop.Name}}.Id)).Select(x => x.Id).ToListAsync();
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
                SoftClass nestedEntityClass = allEntityClasses.Where(x => x.Name == prop.EntityName).SingleOrDefault();
                string nestedEntityClassName = nestedEntityClass.Name;
                string nestedEntityClassNameLowerCase = nestedEntityClassName.FirstCharToLower();
                string nestedEntityClassIdType = Helper.GetIdType(nestedEntityClass, allEntityClasses);

                if (recursiveIteration == 0)
                {
                    result.Add($$"""
                List<{{nestedEntityClassIdType}}> {{nameOfTheEntityClassFirstLower}}{{nestedEntityClassName}}ListToDelete = await _context.DbSet<{{nestedEntityClassName}}>().Where(x => {{nameOfTheEntityClassFirstLower}}ListToDelete.Contains(x.{{prop.Name}}.Id)).Select(x => x.Id).ToListAsync();
""");
                }
                else
                {
                    result.Add($$"""
                List<{{nestedEntityClassIdType}}> {{nameOfTheEntityClassFirstLower}}{{nestedEntityClassName}}ListToDelete = await _context.DbSet<{{nestedEntityClassName}}>().Where(x => {{parentNameOfTheEntityClass.FirstCharToLower()}}{{nameOfTheEntityClass}}ListToDelete.Contains(x.{{prop.Name}}.Id)).Select(x => x.Id).ToListAsync();
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

        #region One To Many

        static List<string> GetOneToManyMethods(SoftClass entity, List<SoftClass> entities)
        {
            string entityIdType = Helper.GetIdType(entity, entities);

            List<SoftProperty> oneToManyProperties = entity.Properties
                .Where(prop => prop.Type.IsEnumerable())
                .ToList();

            List<string> result = new List<string>();

            foreach (SoftProperty property in oneToManyProperties) // List<Role> Roles
            {
                string extractedPropertyType = Helper.ExtractTypeFromGenericType(property.Type); // Role
                SoftClass extractedPropertyEntity = entities.Where(x => x.Name == extractedPropertyType).Single(); // Role

                string extractedPropertyEntityIdType = Helper.GetIdType(extractedPropertyEntity, entities); // int

                if (extractedPropertyEntityIdType == null) // FT: M2M List, maybe do something else in the future.
                    continue;

                string extractedPropertyEntityDisplayName = Helper.GetDisplayNamePropForClass(extractedPropertyEntity); // Name

                SoftProperty manyToManyPropFromTheListProperties = extractedPropertyEntity.Properties.Where(x => x.Type.IsEnumerable() && Helper.ExtractTypeFromGenericType(x.Type) == entity.Name).SingleOrDefault(); // List<User> Users
                SoftProperty manyToOneProperty = extractedPropertyEntity.GetManyToOnePropertyWithManyAttribute(entity.Name, property.Name);

                if (manyToOneProperty != null)
                {
                    result.Add($$"""
        public async virtual Task<List<NamebookDTO<{{extractedPropertyEntityIdType}}>>> Get{{extractedPropertyType}}NamebookListFor{{entity.Name}}({{entityIdType}} id, bool authorize = true)
        {
            return await _context.WithTransactionAsync(async () =>
            {
                if (authorize)
                {
                    await _authorizationService.{{entity.Name}}SingleReadAuthorize(id);
                }

                return await _context.DbSet<{{extractedPropertyType}}>()
                    .AsNoTracking()
                    .Where(x => x.{{manyToOneProperty.Name}}.Id == id)
                    .Select(x => new NamebookDTO<{{extractedPropertyEntityIdType}}>
                    {
                        Id = x.Id,
                        DisplayName = x.{{extractedPropertyEntityDisplayName}},
                    })
                    .ToListAsync();
            });
        }

        public async Task<List<{{extractedPropertyType}}>> Get{{extractedPropertyType}}ListFor{{entity.Name}}({{entityIdType}} id, bool authorize = true)
        {
            return await _context.WithTransactionAsync(async () =>
            {
                if (authorize)
                {
                    await _authorizationService.{{entity.Name}}SingleReadAuthorize(id);
                }

                return await _context.DbSet<{{extractedPropertyType}}>()
                    .Where(x => x.{{manyToOneProperty.Name}}.Id == id)
                    .ToListAsync();
            });
        }
""");
                    if (property.Attributes.Any(x => x.Name == "UIOrderedOneToMany"))
                        result.Add(GetOrderedOneToManyMethod(property, entity, entities));
                }
                else if (manyToManyPropFromTheListProperties != null)
                {
                    result.Add($$"""
        public async virtual Task<List<NamebookDTO<{{extractedPropertyEntityIdType}}>>> Get{{extractedPropertyType}}NamebookListFor{{entity.Name}}({{entityIdType}} {{entity.Name.FirstCharToLower()}}Id, bool authorize = true)
        {
            return await _context.WithTransactionAsync(async () =>
            {
                if (authorize)
                {
                    await _authorizationService.{{entity.Name}}SingleReadAuthorize({{entity.Name.FirstCharToLower()}}Id);
                }

                return await _context.DbSet<{{extractedPropertyType}}>()
                    .AsNoTracking()
                    .Where(x => x.{{manyToManyPropFromTheListProperties.Name}}.Any(x => x.Id == {{entity.Name.FirstCharToLower()}}Id))
                    .Select(x => new NamebookDTO<{{extractedPropertyEntityIdType}}>
                    {
                        Id = x.Id,
                        DisplayName = x.{{extractedPropertyEntityDisplayName}},
                    })
                    .ToListAsync();
            });
        }

        public async Task<List<{{extractedPropertyType}}>> Get{{extractedPropertyType}}ListFor{{entity.Name}}({{entityIdType}} {{entity.Name.FirstCharToLower()}}Id, bool authorize = true)
        {
            return await _context.WithTransactionAsync(async () =>
            {
                if (authorize)
                {
                    await _authorizationService.{{entity.Name}}SingleReadAuthorize({{entity.Name.FirstCharToLower()}}Id);
                }

                return await _context.DbSet<{{extractedPropertyType}}>()
                    .Where(x => x.{{manyToManyPropFromTheListProperties.Name}}.Any(x => x.Id == {{entity.Name.FirstCharToLower()}}Id))
                    .ToListAsync();
            });
        }

        public async Task Update{{extractedPropertyType}}ListFor{{entity.Name}}({{entityIdType}} {{entity.Name.FirstCharToLower()}}Id, List<{{extractedPropertyEntityIdType}}> selected{{extractedPropertyType}}Ids)
        {
            if (selected{{extractedPropertyType}}Ids == null)
                return;

            List<{{extractedPropertyEntityIdType}}> selectedIdsHelper = selected{{extractedPropertyType}}Ids.ToList();

            await _context.WithTransactionAsync(async () =>
            {
                // FT: Not doing authorization here, because we can not figure out here if we are updating while inserting object (eg. User), or updating object, we will always get the id which is not 0 here.

                {{((entity.IsBusinessObject() || entity.IsReadonlyObject() == false)
                ? $"{entity.Name} {entity.Name.FirstCharToLower()} = await GetInstanceAsync<{entity.Name}, {entityIdType}>({entity.Name.FirstCharToLower()}Id, null); // FT: Version will always be checked before or after this method"
                : $"{entity.Name} {entity.Name.FirstCharToLower()} = await GetInstanceAsync<{entity.Name}, {entityIdType}>({entity.Name.FirstCharToLower()}Id);"
                )}}
                
                foreach ({{extractedPropertyType}} {{extractedPropertyType.FirstCharToLower()}} in {{entity.Name.FirstCharToLower()}}.{{property.Name}}.ToList())
                {
                    if (selectedIdsHelper.Contains({{extractedPropertyType.FirstCharToLower()}}.Id))
                        selectedIdsHelper.Remove({{extractedPropertyType.FirstCharToLower()}}.Id);
                    else
                        {{entity.Name.FirstCharToLower()}}.{{property.Name}}.Remove({{extractedPropertyType.FirstCharToLower()}});
                }

                List<{{extractedPropertyType}}> {{extractedPropertyType.FirstCharToLower()}}ListToInsert = await _context.DbSet<{{extractedPropertyType}}>().Where(x => selectedIdsHelper.Contains(x.Id)).ToListAsync();

                {{entity.Name.FirstCharToLower()}}.{{property.Name}}.AddRange({{extractedPropertyType.FirstCharToLower()}}ListToInsert);
                await _context.SaveChangesAsync();
            });
        }

        /// <summary>
        /// It's mandatory to pass queryable ordered by the same field as the table data
        /// </summary>
        public async Task<LazyLoadSelectedIdsResultDTO<{{extractedPropertyEntityIdType}}>> LazyLoadSelected{{extractedPropertyType}}IdsFor{{entity.Name}}(TableFilterDTO tableFilterDTO, IQueryable<{{extractedPropertyType}}> {{extractedPropertyType.FirstCharToLower()}}Query)
        {
            LazyLoadSelectedIdsResultDTO<{{extractedPropertyEntityIdType}}> lazyLoadSelectedIdsResultDTO = new();

            {{extractedPropertyType.FirstCharToLower()}}Query = {{extractedPropertyType.FirstCharToLower()}}Query
                .Skip(tableFilterDTO.First)
                .Take(tableFilterDTO.Rows)
                .Where(x => x.{{manyToManyPropFromTheListProperties.Name}}
                    .Any(x => x.Id == tableFilterDTO.{{extractedPropertyEntityIdType.GetTableFilterAdditionalFilterPropertyName()}}));

            await _context.WithTransactionAsync(async () =>
            {
                var paginationResult = await Get{{extractedPropertyType}}ListForPagination(tableFilterDTO, {{extractedPropertyType.FirstCharToLower()}}Query);

                lazyLoadSelectedIdsResultDTO.SelectedIds = await paginationResult.Query
                    .Select(x => x.Id)
                    .ToListAsync();

                int count = await _context.DbSet<{{extractedPropertyType}}>()
                    .Where(x => x.{{manyToManyPropFromTheListProperties.Name}}
                        .Any(x => x.Id == tableFilterDTO.{{extractedPropertyEntityIdType.GetTableFilterAdditionalFilterPropertyName()}}))
                    .CountAsync();

                lazyLoadSelectedIdsResultDTO.TotalRecordsSelected = count;
            });

            return lazyLoadSelectedIdsResultDTO;
        }

        public async Task Update{{extractedPropertyType}}ListFor{{entity.Name}}WithLazyTableSelection(IQueryable<{{extractedPropertyType}}> {{extractedPropertyType.FirstCharToLower()}}Query, {{entityIdType}} {{entity.Name.FirstCharToLower()}}Id, ILazyTableSelectionDTO<{{extractedPropertyEntityIdType}}> lazyTableSelectionDTO)
        {
            await _context.WithTransactionAsync(async () =>
            {
                List<{{extractedPropertyEntityIdType}}> {{extractedPropertyType.FirstCharToLower()}}ListToInsert = null;

                if (lazyTableSelectionDTO.IsAllSelected == true)
                {
                    {{extractedPropertyType.FirstCharToLower()}}ListToInsert = await {{extractedPropertyType.FirstCharToLower()}}Query.Where(x => lazyTableSelectionDTO.UnselectedIds.Contains(x.Id) == false).Select(x => x.Id).ToListAsync();
                }
                else if (lazyTableSelectionDTO.IsAllSelected == false)
                {
                    {{extractedPropertyType.FirstCharToLower()}}ListToInsert = await {{extractedPropertyType.FirstCharToLower()}}Query.Where(x => lazyTableSelectionDTO.SelectedIds.Contains(x.Id) == true).Select(x => x.Id).ToListAsync();
                }
                else if (lazyTableSelectionDTO.IsAllSelected == null)
                {
                    {{((entity.IsBusinessObject() || entity.IsReadonlyObject() == false)
                    ? $"var {entity.Name.FirstCharToLower()} = await GetInstanceAsync<{entity.Name}, {entityIdType}>({entity.Name.FirstCharToLower()}Id, null); // FT: Version will always be checked before or after this method"
                    : $"var {entity.Name.FirstCharToLower()} = await GetInstanceAsync<{entity.Name}, {entityIdType}>({entity.Name.FirstCharToLower()}Id);"
                    )}}

                    var alreadySelected = {{entity.Name.FirstCharToLower()}}.{{property.Name}} == null ? new List<{{extractedPropertyEntityIdType}}>() : {{entity.Name.FirstCharToLower()}}.{{property.Name}}.Select(x => x.Id).ToList();

                    {{extractedPropertyType.FirstCharToLower()}}ListToInsert = alreadySelected
                        .Union(lazyTableSelectionDTO.SelectedIds)
                        .Except(lazyTableSelectionDTO.UnselectedIds)
                        .ToList();
                }

                await Update{{extractedPropertyType}}ListFor{{entity.Name}}({{entity.Name.FirstCharToLower()}}Id, {{extractedPropertyType.FirstCharToLower()}}ListToInsert);
            });
        }
""");
                }
                else if (extractedPropertyEntity == null)
                {
                    result.Add("Invalid entity class, you can't have List<Entity> without List<AssociationEntity> or AssociationEntity on the other side."); // He can (User/Role example, many to many on the one side)
                }

            }

            return result;
        }

        private static string GetOrderedOneToManyMethod(SoftProperty property, SoftClass entity, List<SoftClass> entities)
        {
            if (property.Type.IsEnumerable() == false)
                return null;

            string entityIdType = Helper.GetIdType(entity, entities);

            string extractedPropertyType = Helper.ExtractTypeFromGenericType(property.Type);
            SoftClass extractedPropertyEntity = entities.Where(x => x.Name == extractedPropertyType).Single();
            SoftProperty manyToOneProperty = extractedPropertyEntity.GetManyToOnePropertyWithManyAttribute(entity.Name, property.Name);

            return $$"""
        public async Task<List<{{extractedPropertyType}}DTO>> GetOrdered{{property.Name}}For{{entity.Name}}({{entityIdType}} id, bool authorize = true)
        {
            return await _context.WithTransactionAsync(async () =>
            {
                IQueryable<{{extractedPropertyType}}> query = _context.DbSet<{{extractedPropertyType}}>()
                    .Where(x => x.{{manyToOneProperty.Name}}.Id == id)
                    .OrderBy(x => x.OrderNumber);

                return await Get{{extractedPropertyType}}DTOList(query, authorize);
            });
        }
""";
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

            string mainEntityPropertyName = mainEntityProperty.Name; // eg. "DiscountProductGroup"
            string mainEntityClassName = mainEntityProperty.Type; // eg. Category
            SoftClass mainEntityClass = allEntityClasses.Where(x => x.Name == mainEntityClassName).Single();
            string mainEntityIdType = Helper.GetIdType(mainEntityClass, allEntityClasses);

            string extendEntityPropertyName = extendEntityProperty.Name;
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
                        {{entity.Name.FirstCharToLower()}}.{{mainEntityPropertyName}} = await GetInstanceAsync<{{mainEntityClassName}}, {{mainEntityIdType}}>({{mainEntityPropertyName.FirstCharToLower()}}Id, null);
                        {{entity.Name.FirstCharToLower()}}.{{extendEntityPropertyName}} = await GetInstanceAsync<{{extendEntityClassName}}, {{extendEntityIdType}}>(selected{{entity.Name}}DTO.{{extendEntityPropertyName}}Id.Value, null);
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