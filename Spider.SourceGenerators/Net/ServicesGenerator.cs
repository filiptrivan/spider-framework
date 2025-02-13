using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Spider.SourceGenerators.Shared;
using Spider.SourceGenerators.Enums;
using Spider.SourceGenerators.Models;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System;
using System.Diagnostics;
using System.Reflection;

namespace Spider.SourceGenerators.Net
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
            IncrementalValuesProvider<ClassDeclarationSyntax> classDeclarations = Helpers.GetClassIncrementalValuesProvider(context.SyntaxProvider, new List<NamespaceExtensionCodes>
                {
                    NamespaceExtensionCodes.Entities,
                });

            IncrementalValueProvider<List<SpiderClass>> referencedProjectClasses = Helpers.GetIncrementalValueProviderClassesFromReferencedAssemblies(context,
                new List<NamespaceExtensionCodes>
                {
                    NamespaceExtensionCodes.Entities,
                });

            var allClasses = classDeclarations.Collect()
                .Combine(referencedProjectClasses);

            context.RegisterImplementationSourceOutput(allClasses, static (spc, source) => Execute(source.Left, source.Right, spc));
        }

        private static void Execute(IList<ClassDeclarationSyntax> classes, List<SpiderClass> referencedProjectEntities, SourceProductionContext context)
        {
            if (classes.Count <= 1)
                return;

            List<SpiderClass> currentProjectClasses = Helpers.GetSpiderClasses(classes, referencedProjectEntities);
            List<SpiderClass> currentProjectEntities = currentProjectClasses.Where(x => x.Namespace.EndsWith(".Entities")).ToList();
            List<SpiderClass> allEntities = currentProjectEntities.Concat(referencedProjectEntities).ToList();

            string namespaceValue = currentProjectEntities[0].Namespace;
            string basePartOfNamespace = Helpers.GetBasePartOfNamespace(namespaceValue);
            string projectName = Helpers.GetProjectName(namespaceValue);

            bool isSecurityProject = projectName == "Security";

            string result = $$"""
{{GetUsings(basePartOfNamespace, projectName)}}

namespace {{basePartOfNamespace}}.Services
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

{{string.Join("\n\n", GetBusinessServiceMethods(currentProjectEntities, allEntities, projectName))}}

    }
}
""";

            context.AddSource($"BusinessService.generated", SourceText.From(result, Encoding.UTF8));
        }

        private static List<string> GetBusinessServiceMethods(List<SpiderClass> entityClasses, List<SpiderClass> allEntityClasses, string projectName)
        {
            List<string> result = new();

            foreach (SpiderClass entity in entityClasses)
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

{{GetReadBusinessServiceMethods(entity, allEntityClasses, projectName)}}

        #endregion

        #region Save

{{GetSavingData(entity, allEntityClasses)}}

{{string.Join("\n\n", GetUploadBlobMethods(entity, allEntityClasses))}}
        
        #endregion

        #region Delete

{{string.Join("\n\n", GetDeletingData(entity, allEntityClasses))}}

        #endregion

        #region One To Many

{{string.Join("\n\n", GetOneToManyMethods(entity, allEntityClasses))}}

        #endregion

        #endregion
""");
                }
            }

            return result;
        }

        #region Read

        private static string GetReadBusinessServiceMethods(SpiderClass entity, List<SpiderClass> allEntities, string projectName)
        {
            string entityIdType = entity.GetIdType(allEntities);

            return $$"""
        public async Task<{{entity.Name}}DTO> Get{{entity.Name}}DTO({{entityIdType}} id, bool authorize)
        {
            return await _context.WithTransactionAsync(async () =>
            {
                if (authorize)
                {
                    {{GetAuthorizeEntityMethodCall(entity.Name, CrudCodes.Read, "id")}}
                }

                var dto = await _context.DbSet<{{entity.Name}}>()
                    .AsNoTracking()
                    .Where(x => x.Id == id).ProjectToType<{{entity.Name}}DTO>(Mapper.{{entity.Name}}ProjectToConfig())
                    .SingleOrDefaultAsync();

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

        public async virtual Task<TableResponseDTO<{{entity.Name}}DTO>> Get{{entity.Name}}TableData(TableFilterDTO tableFilterPayload, IQueryable<{{entity.Name}}> query, bool authorize)
        {
            PaginationResult<{{entity.Name}}> paginationResult = new();
            List<{{entity.Name}}DTO> data = null;

            await _context.WithTransactionAsync(async () =>
            {
                paginationResult = await Get{{entity.Name}}ListForPagination(tableFilterPayload, query);

                data = await paginationResult.Query
                    .Skip(tableFilterPayload.First)
                    .Take(tableFilterPayload.Rows)
                    .ProjectToType<{{entity.Name}}DTO>(Mapper.{{entity.Name}}ProjectToConfig())
                    .ToListAsync();

                if (authorize)
                {
                    {{GetAuthorizeEntityMethodCall(entity.Name, CrudCodes.Read, "data.Select(x => x.Id).ToList()")}}
                }
            });

            return new TableResponseDTO<{{entity.Name}}DTO> { Data = data, TotalRecords = paginationResult.TotalRecords };
        }

        public async Task<byte[]> Export{{entity.Name}}TableDataToExcel(TableFilterDTO tableFilterPayload, IQueryable<{{entity.Name}}> query, bool authorize)
        {
            PaginationResult<{{entity.Name}}> paginationResult = new();
            List<{{entity.Name}}DTO> data = null;

            await _context.WithTransactionAsync(async () =>
            {
                paginationResult = await Get{{entity.Name}}ListForPagination(tableFilterPayload, query);

                data = await paginationResult.Query.ProjectToType<{{entity.Name}}DTO>(Mapper.{{entity.Name}}ExcelProjectToConfig()).ToListAsync();

                if (authorize)
                {
                    {{GetAuthorizeEntityMethodCall(entity.Name, CrudCodes.Read, "data.Select(x => x.Id).ToList()")}}
                }
            });

            string[] excelPropertiesToExclude = ExcelPropertiesToExclude.GetHeadersToExclude(new {{entity.Name}}DTO());
            return _excelService.FillReportTemplate<{{entity.Name}}DTO>(data, paginationResult.TotalRecords, excelPropertiesToExclude, {{GetTermsClassName(projectName)}}.ResourceManager).ToArray();
        }

        public async Task<List<{{entity.Name}}>> Get{{entity.Name}}List(IQueryable<{{entity.Name}}> query, bool authorize)
        {
            return await _context.WithTransactionAsync(async () =>
            {
                var result = await query
                    .ToListAsync();

                if (authorize)
                {
                    {{GetAuthorizeEntityMethodCall(entity.Name, CrudCodes.Read, "result.Select(x => x.Id).ToList()")}}
                }

                return result;
            });
        }

        public async Task<List<{{entity.Name}}DTO>> Get{{entity.Name}}DTOList(IQueryable<{{entity.Name}}> query, bool authorize)
        {
            return await _context.WithTransactionAsync(async () =>
            {
                var dtoList = await query
                    .AsNoTracking()
                    .ProjectToType<{{entity.Name}}DTO>(Mapper.{{entity.Name}}ToDTOConfig())
                    .ToListAsync();

                if (authorize)
                {
                    {{GetAuthorizeEntityMethodCall(entity.Name, CrudCodes.Read, "dtoList.Select(x => x.Id).ToList()")}}
                }

{{GetPopulateDTOWithBlobPartsForDTOList(entity, entity.Properties)}}

                return dtoList;
            });
        }

{{GetManyToOneReadMethods(entity, allEntities)}}
""";
        }

        private static string GetTermsClassName(string projectName)
        {
            return projectName == "Security" ? "SharedTerms" : "TermsGenerated";
        }

        private static string GetPopulateDTOWithBlobPartsForDTO(SpiderClass entityClass, List<SpiderProperty> propertiesEntityClass)
        {
            List<string> blobParts = GetPopulateDTOWithBlobParts(propertiesEntityClass);

            if (blobParts.Count == 0)
                return null;

            return $$"""
                {{string.Join("\n", blobParts)}}
""";
        }

        private static string GetPopulateDTOWithBlobPartsForDTOList(SpiderClass entityClass, List<SpiderProperty> propertiesEntityClass)
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

        private static List<string> GetPopulateDTOWithBlobParts(List<SpiderProperty> propertiesEntityClass)
        {
            List<string> blobParts = new();

            List<SpiderProperty> blobProperies = Helpers.GetBlobProperties(propertiesEntityClass);

            foreach (SpiderProperty property in blobProperies)
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

        #region Many To One

        public static string GetManyToOneReadMethods(SpiderClass entity, List<SpiderClass> allEntities)
        {
            StringBuilder sb = new();

            foreach (SpiderProperty property in entity.Properties)
            {
                if (property.ShouldGenerateAutocompleteControllerMethod())
                {
                    sb.Append($$"""
{{GetAutocompleteMethod(property, entity, allEntities)}}

""");
                }

                if (property.ShouldGenerateDropdownControllerMethod())
                {
                    sb.Append($$"""
{{GetDropdownMethod(property, entity, allEntities)}}

""");
                }
            }

            return sb.ToString();
        }

        private static string GetAutocompleteMethod(SpiderProperty property, SpiderClass entity, List<SpiderClass> allEntities)
        {
            SpiderClass autocompleteEntity = allEntities.Where(x => x.Name == Helpers.ExtractTypeFromGenericType(property.Type)).Single();
            string autocompleteEntityIdType = autocompleteEntity.GetIdType(allEntities);
            string autocompleteEntityDisplayName = Helpers.GetDisplayNameProperty(autocompleteEntity);

            return $$"""
        public async virtual Task<List<NamebookDTO<{{autocompleteEntityIdType}}>>> Get{{property.Name}}AutocompleteListFor{{entity.Name}}(
            int limit, 
            string filter, 
            IQueryable<{{autocompleteEntity.Name}}> query, 
            bool authorize,
            {{entity.GetIdType(allEntities)}}? {{entity.Name.FirstCharToLower()}}Id = null
        )
        {
            return await _context.WithTransactionAsync(async () =>
            {
                if (authorize)
                {
                    {{GetAuthorizeEntityMethodCall(entity.Name, CrudCodes.Read, $"{entity.Name.FirstCharToLower()}Id")}}
                }

                if (!string.IsNullOrEmpty(filter))
                    query = query.Where(x => x.{{autocompleteEntityDisplayName}}.Contains(filter));

                var result = await query
                    .AsNoTracking()
                    .Take(limit)
                    .Select(x => new NamebookDTO<{{autocompleteEntityIdType}}>
                    {
                        Id = x.Id,
                        DisplayName = x.{{autocompleteEntityDisplayName}},
                    })
                    .ToListAsync();

                return result;
            });
        }
""";
        }

        private static string GetDropdownMethod(SpiderProperty property, SpiderClass entity, List<SpiderClass> allEntities)
        {
            SpiderClass dropdownEntity = allEntities.Where(x => x.Name == Helpers.ExtractTypeFromGenericType(property.Type)).Single();
            string dropdownEntityIdType = dropdownEntity.GetIdType(allEntities);
            string dropdownDisplayName = Helpers.GetDisplayNameProperty(dropdownEntity);

            return $$"""
        public async virtual Task<List<NamebookDTO<{{dropdownEntityIdType}}>>> Get{{property.Name}}DropdownListFor{{entity.Name}}(
            IQueryable<{{dropdownEntity.Name}}> query, 
            bool authorize,
            {{entity.GetIdType(allEntities)}}? {{entity.Name.FirstCharToLower()}}Id = null
        )
        {
            return await _context.WithTransactionAsync(async () =>
            {
                if (authorize)
                {
                    {{GetAuthorizeEntityMethodCall(entity.Name, CrudCodes.Read, $"{entity.Name.FirstCharToLower()}Id")}}
                }

                var result = await query
                    .AsNoTracking()
                    .Select(x => new NamebookDTO<{{dropdownEntityIdType}}>
                    {
                        Id = x.Id,
                        DisplayName = x.{{dropdownDisplayName}},
                    })
                    .ToListAsync();

                return result;
            });
        }
""";
        }

        #endregion

        #endregion

        #region One To Many

        static List<string> GetOneToManyMethods(SpiderClass entity, List<SpiderClass> entities)
        {
            string entityIdType = entity.GetIdType(entities);

            List<string> result = new();

            foreach (SpiderProperty oneToManyProperty in entity.Properties.Where(prop => prop.Type.IsOneToManyType())) // List<Role> Roles
            {
                SpiderClass extractedPropertyEntity = entities.Where(x => x.Name == Helpers.ExtractTypeFromGenericType(oneToManyProperty.Type)).Single(); // Role

                string extractedPropertyEntityIdType = extractedPropertyEntity.GetIdType(entities); // int

                if (extractedPropertyEntityIdType == null) // FT: M2M List, maybe do something else in the future.
                    continue;

                string extractedPropertyEntityDisplayName = Helpers.GetDisplayNameProperty(extractedPropertyEntity); // Name

                SpiderProperty extractedEntityManyToManyProperty = Helpers.GetOppositeManyToManyProperty(oneToManyProperty, extractedPropertyEntity, entity, entities);
                SpiderProperty manyToOneProperty = extractedPropertyEntity.GetManyToOnePropertyWithManyAttribute(entity.Name, oneToManyProperty.Name);

                if (manyToOneProperty != null)
                {
                    result.Add($$"""
{{GetOneToManyNamebookListForEntity(oneToManyProperty, extractedPropertyEntity, manyToOneProperty, entity, entities)}}

{{GetOneToManyListForEntity(oneToManyProperty, extractedPropertyEntity, manyToOneProperty, entity, entities)}}

{{GetOrderedOneToManyMethod(oneToManyProperty, entity, entities)}}
""");
                }
                else if (extractedEntityManyToManyProperty != null)
                {
                    result.Add($$"""
        public async virtual Task<List<NamebookDTO<{{extractedPropertyEntityIdType}}>>> Get{{oneToManyProperty.Name}}NamebookListFor{{entity.Name}}({{entityIdType}} id, bool authorize)
        {
            return await _context.WithTransactionAsync(async () =>
            {
                if (authorize)
                {
                    {{GetAuthorizeEntityMethodCall(entity.Name, CrudCodes.Read, "id")}}
                }

                return await _context.DbSet<{{extractedPropertyEntity.Name}}>()
                    .AsNoTracking()
                    .Where(x => x.{{extractedEntityManyToManyProperty.Name}}.Any(x => x.Id == id))
                    .Select(x => new NamebookDTO<{{extractedPropertyEntityIdType}}>
                    {
                        Id = x.Id,
                        DisplayName = x.{{extractedPropertyEntityDisplayName}},
                    })
                    .ToListAsync();
            });
        }

        public async Task<List<{{extractedPropertyEntity.Name}}>> Get{{oneToManyProperty.Name}}For{{entity.Name}}({{entityIdType}} id)
        {
            return await _context.WithTransactionAsync(async () =>
            {
                return await _context.DbSet<{{extractedPropertyEntity.Name}}>()
                    .Where(x => x.{{extractedEntityManyToManyProperty.Name}}.Any(x => x.Id == id))
                    .ToListAsync();
            });
        }

        public async Task Update{{oneToManyProperty.Name}}For{{entity.Name}}({{entityIdType}} id, List<{{extractedPropertyEntityIdType}}> selectedIds)
        {
            if (selectedIds == null)
                return;

            List<{{extractedPropertyEntityIdType}}> selectedIdsHelper = selectedIds.ToList();

            await _context.WithTransactionAsync(async () =>
            {
                // FT: Not doing authorization here, because we can not figure out here if we are updating while inserting object (eg. User), or updating object, we will always get the id which is not 0 here.

                {{((entity.IsBusinessObject() || entity.IsReadonlyObject() == false)
                ? $"var entity = await GetInstanceAsync<{entity.Name}, {entityIdType}>(id, null); // FT: Version will always be checked before or after this method"
                : $"var entity = await GetInstanceAsync<{entity.Name}, {entityIdType}>(id);"
                )}}
                
                foreach ({{extractedPropertyEntity.Name}} item in entity.{{oneToManyProperty.Name}}.ToList())
                {
                    if (selectedIdsHelper.Contains(item.Id))
                        selectedIdsHelper.Remove(item.Id);
                    else
                        entity.{{oneToManyProperty.Name}}.Remove(item);
                }

                var listToInsert = await _context.DbSet<{{extractedPropertyEntity.Name}}>().Where(x => selectedIdsHelper.Contains(x.Id)).ToListAsync();

                entity.{{oneToManyProperty.Name}}.AddRange(listToInsert);
                await _context.SaveChangesAsync();
            });
        }

        /// <summary>
        /// It's mandatory to pass queryable ordered by the same field as the table data
        /// </summary>
        public async Task<LazyLoadSelectedIdsResultDTO<{{extractedPropertyEntityIdType}}>> LazyLoadSelected{{oneToManyProperty.Name}}IdsFor{{entity.Name}}(TableFilterDTO tableFilterDTO, IQueryable<{{extractedPropertyEntity.Name}}> query, bool authorize)
        {
            LazyLoadSelectedIdsResultDTO<{{extractedPropertyEntityIdType}}> lazyLoadSelectedIdsResultDTO = new();

            query = query
                .Skip(tableFilterDTO.First)
                .Take(tableFilterDTO.Rows)
                .Where(x => x.{{extractedEntityManyToManyProperty.Name}}
                    .Any(x => x.Id == tableFilterDTO.{{entityIdType.GetTableFilterAdditionalFilterPropertyName()}}));

            await _context.WithTransactionAsync(async () =>
            {
                if (authorize)
                {
                    {{GetAuthorizeEntityMethodCall(entity.Name, CrudCodes.Read, $"({entityIdType})tableFilterDTO.{entityIdType.GetTableFilterAdditionalFilterPropertyName()}")}}
                }

                var paginationResult = await Get{{extractedPropertyEntity.Name}}ListForPagination(tableFilterDTO, query);

                lazyLoadSelectedIdsResultDTO.SelectedIds = await paginationResult.Query
                    .Select(x => x.Id)
                    .ToListAsync();

                int count = await _context.DbSet<{{extractedPropertyEntity.Name}}>()
                    .Where(x => x.{{extractedEntityManyToManyProperty.Name}}
                        .Any(x => x.Id == tableFilterDTO.{{entityIdType.GetTableFilterAdditionalFilterPropertyName()}}))
                    .CountAsync();

                lazyLoadSelectedIdsResultDTO.TotalRecordsSelected = count;
            });

            return lazyLoadSelectedIdsResultDTO;
        }

{{GetSimpleManyToManyUpdateWithLazyTableSelectionMethod(oneToManyProperty, entity, entities)}}
""");
                }
                else if (extractedPropertyEntity == null)
                {
                    result.Add("Invalid entity class, you can't have List<Entity> without List<AssociationEntity> or AssociationEntity on the other side."); // He can (User/Role example, many to many on the one side)
                }

            }

            return result;
        }

        private static string GetOneToManyListForEntity(SpiderProperty oneToManyProperty, SpiderClass extractedPropertyEntity, SpiderProperty manyToOneProperty, SpiderClass entity, List<SpiderClass> entities)
        {
            return $$"""
        public async Task<List<{{extractedPropertyEntity.Name}}>> Get{{oneToManyProperty.Name}}For{{entity.Name}}({{entity.GetIdType(entities)}} id)
        {
            return await _context.WithTransactionAsync(async () =>
            {
                return await _context.DbSet<{{extractedPropertyEntity.Name}}>()
                    .Where(x => x.{{manyToOneProperty.Name}}.Id == id)
                    .ToListAsync();
            });
        }
""";
        }

        private static string GetOneToManyNamebookListForEntity(SpiderProperty oneToManyProperty, SpiderClass extractedPropertyEntity, SpiderProperty manyToOneProperty, SpiderClass entity, List<SpiderClass> entities)
        {
            string extractedPropertyEntityIdType = extractedPropertyEntity.GetIdType(entities); // int

            return $$"""
        public async virtual Task<List<NamebookDTO<{{extractedPropertyEntityIdType}}>>> Get{{oneToManyProperty.Name}}NamebookListFor{{entity.Name}}({{entity.GetIdType(entities)}} id, bool authorize)
        {
            return await _context.WithTransactionAsync(async () =>
            {
                if (authorize)
                {
                    {{GetAuthorizeEntityMethodCall(entity.Name, CrudCodes.Read, "id")}}
                }

                return await _context.DbSet<{{extractedPropertyEntity.Name}}>()
                    .AsNoTracking()
                    .Where(x => x.{{manyToOneProperty.Name}}.Id == id)
                    .Select(x => new NamebookDTO<{{extractedPropertyEntityIdType}}>
                    {
                        Id = x.Id,
                        DisplayName = x.{{Helpers.GetDisplayNameProperty(extractedPropertyEntity)}},
                    })
                    .ToListAsync();
            });
        }
""";
        }

        private static string GetSimpleManyToManyUpdateWithLazyTableSelectionMethod(SpiderProperty property, SpiderClass entity, List<SpiderClass> entities)
        {
            if (property.HasSimpleManyToManyTableLazyLoadAttribute() == false)
                return null;

            string entityIdType = entity.GetIdType(entities);
            SpiderClass extractedPropertyEntity = entities.Where(x => x.Name == Helpers.ExtractTypeFromGenericType(property.Type)).Single(); // Role

            string extractedPropertyEntityIdType = extractedPropertyEntity.GetIdType(entities); // int

            return $$"""
        public async Task Update{{property.Name}}WithLazyTableSelectionFor{{entity.Name}}(IQueryable<{{extractedPropertyEntity.Name}}> query, {{entityIdType}} id, {{entity.Name}}SaveBodyDTO saveBodyDTO)
        {
            await _context.WithTransactionAsync(async () =>
            {
                List<{{extractedPropertyEntityIdType}}> listToInsert = null;

                if (saveBodyDTO.AreAll{{property.Name}}Selected == true)
                {
                    listToInsert = await query.Where(x => saveBodyDTO.Unselected{{property.Name}}Ids.Contains(x.Id) == false).Select(x => x.Id).ToListAsync();
                }
                else if (saveBodyDTO.AreAll{{property.Name}}Selected == false)
                {
                    listToInsert = await query.Where(x => saveBodyDTO.Selected{{property.Name}}Ids.Contains(x.Id) == true).Select(x => x.Id).ToListAsync();
                }
                else if (saveBodyDTO.AreAll{{property.Name}}Selected == null)
                {
                    {{((entity.IsBusinessObject() || entity.IsReadonlyObject() == false)
                    ? $"var entity = await GetInstanceAsync<{entity.Name}, {entityIdType}>(id, null); // FT: Version will always be checked before or after this method"
                    : $"var entity = await GetInstanceAsync<{entity.Name}, {entityIdType}>(id);"
                    )}}

                    var alreadySelected = entity.{{property.Name}} == null ? new List<{{extractedPropertyEntityIdType}}>() : entity.{{property.Name}}.Select(x => x.Id).ToList();

                    listToInsert = alreadySelected
                        .Union(saveBodyDTO.Selected{{property.Name}}Ids)
                        .Except(saveBodyDTO.Unselected{{property.Name}}Ids)
                        .ToList();
                }

                await Update{{property.Name}}For{{entity.Name}}(id, listToInsert);
            });
        }
""";
        }

        private static string GetOrderedOneToManyMethod(SpiderProperty property, SpiderClass entity, List<SpiderClass> entities)
        {
            if (property.HasOrderedOneToManyAttribute() == false)
                return null;

            SpiderClass extractedPropertyEntity = entities.Where(x => x.Name == Helpers.ExtractTypeFromGenericType(property.Type)).Single();
            SpiderProperty manyToOneProperty = extractedPropertyEntity.GetManyToOnePropertyWithManyAttribute(entity.Name, property.Name);

            return $$"""
        public async Task<List<{{extractedPropertyEntity.Name}}DTO>> GetOrdered{{property.Name}}For{{entity.Name}}({{entity.GetIdType(entities)}} id, bool authorize)
        {
            return await _context.WithTransactionAsync(async () =>
            {
                var query = _context.DbSet<{{extractedPropertyEntity.Name}}>()
                    .Where(x => x.{{manyToOneProperty.Name}}.Id == id)
                    .OrderBy(x => x.OrderNumber);

                return await Get{{extractedPropertyEntity.Name}}DTOList(query, authorize);
            });
        }
""";
        }

        #endregion

        #region Save

        static string GetSavingData(SpiderClass entity, List<SpiderClass> entities)
        {
            if (entity.IsAbstract || entity.IsReadonlyObject())
                return null;

            string entityIdType = entity.GetIdType(entities);

            return $$"""
{{GetSaveAndReturnSaveBodyDTOData(entity, entities)}}

        public async Task<{{entity.Name}}DTO> Save{{entity.Name}}AndReturnDTO({{entity.Name}}DTO dto, bool authorizeUpdate, bool authorizeInsert)
        {
            return await _context.WithTransactionAsync(async () =>
            {
                var poco = await Save{{entity.Name}}(dto, authorizeUpdate, authorizeInsert);

                return poco.Adapt<{{entity.Name}}DTO>(Mapper.{{entity.Name}}ToDTOConfig());
            });
        }

        public async Task<{{entity.Name}}> Save{{entity.Name}}({{entity.Name}}DTO dto, bool authorizeUpdate, bool authorizeInsert)
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
                        {{GetAuthorizeEntityMethodCall(entity.Name, CrudCodes.Update, "dto")}}
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
                        {{GetAuthorizeEntityMethodCall(entity.Name, CrudCodes.Insert, "dto")}}
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

        private static string GetSaveAndReturnSaveBodyDTOData(SpiderClass entity, List<SpiderClass> entities)
        {
            return $$"""
        public virtual async Task<{{entity.Name}}SaveBodyDTO> Save{{entity.Name}}AndReturnSaveBodyDTO({{entity.Name}}SaveBodyDTO saveBodyDTO, bool authorizeUpdate, bool authorizeInsert)
        {
            return await _context.WithTransactionAsync(async () =>
            {
                await OnBeforeSave{{entity.Name}}AndReturnSaveBodyDTO(saveBodyDTO);

                var savedDTO = await Save{{entity.Name}}AndReturnDTO(saveBodyDTO.{{entity.Name}}DTO, authorizeUpdate, authorizeInsert);

                await OnAfterSave{{entity.Name}}AndReturnSaveBodyDTO(savedDTO, saveBodyDTO);

{{string.Join("\n", GetOrderedOneToManyUpdateVariables(entity, entities))}}
{{string.Join("\n", GetManyToManyMultiControlTypesUpdateMethods(entity, entities))}}
{{string.Join("\n", GetSimpleManyToManyTableLazyLoad(entity, entities))}}

                var result = new {{entity.Name}}SaveBodyDTO
                {
                    {{entity.Name}}DTO = savedDTO,
{{string.Join(",\n", GetOrderedOneToManySaveBodyDTOVariables(entity, entities))}}
                };

                return result;
            });
        }

{{string.Join("\n", GetOrderedOneToManyUpdateMethods(entity, entities))}}
{{string.Join("\n", GetSimpleManyToManyTableLazyLoadGetAllQueryHook(entity, entities))}}

        protected virtual async Task OnBeforeSave{{entity.Name}}AndReturnSaveBodyDTO({{entity.Name}}SaveBodyDTO saveBodyDTO) { }

        protected virtual async Task OnAfterSave{{entity.Name}}AndReturnSaveBodyDTO({{entity.Name}}DTO savedDTO, {{entity.Name}}SaveBodyDTO saveBodyDTO) { }
""";
        }

        #region Ordered One To Many

        private static List<string> GetOrderedOneToManyUpdateVariables(SpiderClass entity, List<SpiderClass> entities)
        {
            List<string> result = new();

            foreach (SpiderProperty property in entity.GetOrderedOneToManyProperties())
            {
                SpiderClass extractedEntity = entities.Where(x => x.Name == Helpers.ExtractTypeFromGenericType(property.Type)).SingleOrDefault();

                result.Add($$"""
                var saved{{property.Name}}DTO = await UpdateOrdered{{property.Name}}For{{entity.Name}}(savedDTO.Id, saveBodyDTO.{{property.Name}}DTO);
""");
            }

            return result;
        }

        private static List<string> GetOrderedOneToManySaveBodyDTOVariables(SpiderClass entity, List<SpiderClass> entities)
        {
            List<string> result = new();

            foreach (SpiderProperty property in entity.GetOrderedOneToManyProperties())
            {
                SpiderClass extractedEntity = entities.Where(x => x.Name == Helpers.ExtractTypeFromGenericType(property.Type)).SingleOrDefault();

                result.Add($$"""
                    {{property.Name}}DTO = saved{{property.Name}}DTO
""");
            }

            return result;
        }

        private static List<string> GetOrderedOneToManyUpdateMethods(SpiderClass entity, List<SpiderClass> entities)
        {
            List<string> result = new();

            foreach (SpiderProperty property in entity.GetOrderedOneToManyProperties())
            {
                SpiderClass extractedEntity = entities.Where(x => x.Name == Helpers.ExtractTypeFromGenericType(property.Type)).SingleOrDefault();

                result.Add($$"""
        public async Task<List<{{extractedEntity.Name}}DTO>> UpdateOrdered{{property.Name}}For{{entity.Name}}({{entity.GetIdType(entities)}} id, List<{{extractedEntity.Name}}DTO> orderedItemsDTO)
        {
            var orderedItemIds = orderedItemsDTO.Select(x => x.Id).ToList();

{{GetOrderedOneToManyRequiredValidation(property, entity)}}

            return await _context.WithTransactionAsync(async () =>
            {
                await _context.DbSet<{{extractedEntity.Name}}>().Where(x => x.{{extractedEntity.GetManyToOnePropertyWithManyAttribute(entity.Name, property.Name)?.Name}}.Id == id && orderedItemIds.Contains(x.Id) == false).ExecuteDeleteAsync();

                var savedOrderedItemsDTO = new List<{{extractedEntity.Name}}DTO>();

                for (int i = 0; i < orderedItemsDTO.Count; i++)
                {
                    orderedItemsDTO[i].{{extractedEntity.GetManyToOnePropertyWithManyAttribute(entity.Name, property.Name)?.Name}}Id = id;
                    orderedItemsDTO[i].OrderNumber = i + 1;
                    savedOrderedItemsDTO.Add(await Save{{extractedEntity.Name}}AndReturnDTO(orderedItemsDTO[i], false, false));
                }

                return savedOrderedItemsDTO;
            });
        } 
""");
            }

            return result;
        }

        private static string GetOrderedOneToManyRequiredValidation(SpiderProperty property, SpiderClass entity)
        {
            if (property.HasRequiredAttribute())
            {
                return $$"""
            if (orderedItemIds.Count == 0)
                throw new HackerException("The ordered {{property.Name}} for {{entity.Name}} can't be empty.");
""";

            }

            return null;
        }

        #endregion

        #region Many To Many

        private static List<string> GetManyToManyMultiControlTypesUpdateMethods(SpiderClass entity, List<SpiderClass> entities)
        {
            List<string> result = new();

            foreach (SpiderProperty property in entity.Properties.Where(x => x.HasExcludeServiceMethodsFromGenerationAttribute() == false))
            {
                if (property.IsMultiSelectControlType() ||
                    property.IsMultiAutocompleteControlType())
                {
                    result.Add($$"""
                await Update{{property.Name}}For{{entity.Name}}(savedDTO.Id, saveBodyDTO.Selected{{property.Name}}Ids);
""");
                }
            }

            return result;
        }

        private static List<string> GetSimpleManyToManyTableLazyLoad(SpiderClass entity, List<SpiderClass> entities)
        {
            List<string> result = new();

            foreach (SpiderProperty property in entity.Properties)
            {
                if (property.HasSimpleManyToManyTableLazyLoadAttribute())
                {
                    SpiderClass extractedEntity = entities.Where(x => x.Name == Helpers.ExtractTypeFromGenericType(property.Type)).SingleOrDefault();

                    result.Add($$"""
                var all{{property.Name}}Query = await GetAll{{property.Name}}QueryFor{{entity.Name}}(_context.DbSet<{{extractedEntity.Name}}>());
                var {{property.Name.FirstCharToLower()}}PaginationResult = await Get{{extractedEntity.Name}}ListForPagination(saveBodyDTO.{{property.Name}}TableFilter, all{{property.Name}}Query);
                await Update{{property.Name}}WithLazyTableSelectionFor{{entity.Name}}({{property.Name.FirstCharToLower()}}PaginationResult.Query, savedDTO.Id, saveBodyDTO);
""");
                }
            }

            return result;
        }

        private static List<string> GetSimpleManyToManyTableLazyLoadGetAllQueryHook(SpiderClass entity, List<SpiderClass> entities)
        {
            List<string> result = new();

            foreach (SpiderProperty property in entity.Properties)
            {
                if (property.HasSimpleManyToManyTableLazyLoadAttribute())
                {
                    SpiderClass extractedEntity = entities.Where(x => x.Name == Helpers.ExtractTypeFromGenericType(property.Type)).SingleOrDefault();

                    result.Add($$"""
        protected virtual async Task<IQueryable<{{extractedEntity.Name}}>> GetAll{{property.Name}}QueryFor{{entity.Name}}(IQueryable<{{extractedEntity.Name}}> query)
        {
            return query;
        }
""");
                }
            }

            return result;
        }

        #endregion

        static List<string> GetManyToOneInstancesForSave(SpiderClass entityClass, List<SpiderClass> allEntityClasses)
        {
            List<string> result = new();

            List<SpiderProperty> properties = entityClass.Properties
                .Where(prop => prop.Type.IsManyToOneType())
                .ToList();

            foreach (SpiderProperty prop in properties)
            {
                SpiderClass classOfManyToOneProperty = GetClassOfManyToOneProperty(prop.Type, allEntityClasses);

                if (classOfManyToOneProperty == null)
                    continue;

                if (classOfManyToOneProperty.IsBusinessObject() || classOfManyToOneProperty.IsReadonlyObject() == false)
                {
                    result.Add($$"""
            if (dto.{{prop.Name}}Id > 0)
                poco.{{prop.Name}} = await GetInstanceAsync<{{prop.Type}}, {{classOfManyToOneProperty.GetIdType(allEntityClasses)}}>(dto.{{prop.Name}}Id.Value, null);
            else
                poco.{{prop.Name}} = null;
""");
                }
                else
                {
                    result.Add($$"""
            if (dto.{{prop.Name}}Id > 0)
                poco.{{prop.Name}} = await GetInstanceAsync<{{prop.Type}}, {{classOfManyToOneProperty.GetIdType(allEntityClasses)}}>(dto.{{prop.Name}}Id.Value);
            else
                poco.{{prop.Name}} = null;
""");
                }
            }

            return result;
        }

        static SpiderClass GetClassOfManyToOneProperty(string propType, List<SpiderClass> allEntityClasses)
        {
            SpiderClass manyToOneclass = allEntityClasses.Where(x => x.Name == propType).SingleOrDefault();

            if (manyToOneclass == null)
                return null;

            return manyToOneclass;
        }

        private static List<string> GetNonActiveDeleteBlobMethods(SpiderClass entity)
        {
            List<string> result = new();

            List<SpiderProperty> blobProperies = Helpers.GetBlobProperties(entity.Properties);

            foreach (SpiderProperty property in blobProperies)
            {
                result.Add($$"""
                await DeleteNonActiveBlobs(dto.{{property.Name}}, nameof({{entity.Name}}), nameof({{entity.Name}}.{{property.Name}}), poco.Id.ToString());
""");
            }

            return result;
        }

        private static List<string> GetUploadBlobMethods(SpiderClass entity, List<SpiderClass> allEntities)
        {
            List<string> result = new();

            string entityIdType = entity.GetIdType(allEntities);

            List<SpiderProperty> blobProperies = Helpers.GetBlobProperties(entity.Properties);

            foreach (SpiderProperty property in blobProperies)
            {
                result.Add($$"""
        public async Task<string> Upload{{property.Name}}For{{entity.Name}}(IFormFile file, bool authorizeUpdate, bool authorizeInsert) // FT: It doesn't work without interface
        {
            {{entityIdType}} {{entity.Name.FirstCharToLower()}}Id = GetObjectIdFromFileName<{{entityIdType}}>(file.FileName);

            OnBefore{{property.Name}}BlobFor{{entity.Name}}IsUploaded(file, {{entity.Name.FirstCharToLower()}}Id); // FT: Validate

            if ({{entity.Name.FirstCharToLower()}}Id > 0 && authorizeUpdate)
            {
                {{GetAuthorizeEntityMethodCall($"{property.Name}For{entity.Name}", CrudCodes.Update, $"{entity.Name.FirstCharToLower()}Id")}}
            }
            else if (authorizeInsert)
            {
                {{GetAuthorizeEntityMethodCall($"{property.Name}For{entity.Name}", CrudCodes.Insert, "")}}
            }

            using Stream stream = file.OpenReadStream();

            string fileName = await UploadFileAsync(file.FileName, nameof({{entity.Name}}), nameof({{entity.Name}}.{{property.Name}}), {{entity.Name.FirstCharToLower()}}Id.ToString(), stream);

            return fileName;
        }

        public virtual async Task OnBefore{{property.Name}}BlobFor{{entity.Name}}IsUploaded (IFormFile file, {{entityIdType}} {{entity.Name.FirstCharToLower()}}Id) { }
"""
);
            }

            return result;
        }

        #endregion

        #region Delete

        private static List<string> GetDeletingData(SpiderClass entity, List<SpiderClass> allEntities)
        {
            if (entity.IsAbstract || entity.IsReadonlyObject())
                return new List<string>();

            List<string> result = new();

            result.Add(GetDeleteEntityData(entity, allEntities));

            result.Add(GetDeleteEntityListData(entity, allEntities));

            return result;
        }

        private static string GetDeleteEntityData(SpiderClass entity, List<SpiderClass> allEntities)
        {
            string entityIdType = entity.GetIdType(allEntities);
            int deleteIterator = 1;

            return $$"""
        public virtual async Task OnBefore{{entity.Name}}Delete({{entityIdType}} id) { }

        public async Task Delete{{entity.Name}}({{entityIdType}} id, bool authorize)
        {
            await _context.WithTransactionAsync(async () =>
            {
                await OnBefore{{entity.Name}}Delete(id);

                if (authorize)
                {
                    {{GetAuthorizeEntityMethodCall(entity.Name, CrudCodes.Delete, "id")}}
                }

                List<{{entityIdType}}> listForDelete_{{deleteIterator}} = id.StructToList();

{{string.Join("\n\n", GetManyToOneDeleteQueries(entity, allEntities, "listForDelete", deleteIterator))}}

                await DeleteEntityAsync<{{entity.Name}}, {{entityIdType}}>(id);
            });
        }
""";
        }

        private static string GetDeleteEntityListData(SpiderClass entity, List<SpiderClass> allEntities)
        {
            string entityIdType = entity.GetIdType(allEntities);
            int deleteIterator = 1;

            return $$"""
        public virtual async Task OnBefore{{entity.Name}}ListDelete(List<{{entityIdType}}> listForDelete) { }

        public async Task Delete{{entity.Name}}List(List<{{entityIdType}}> listForDelete_{{deleteIterator}}, bool authorize)
        {
            await _context.WithTransactionAsync(async () =>
            {
                await OnBefore{{entity.Name}}ListDelete(listForDelete_{{deleteIterator}});

                if (authorize)
                {
                    {{GetAuthorizeEntityMethodCall(entity.Name, CrudCodes.Delete, $"listForDelete_{deleteIterator}")}}
                }

{{string.Join("\n\n", GetManyToOneDeleteQueries(entity, allEntities, "listForDelete", deleteIterator))}}

                await DeleteEntitiesAsync<{{entity.Name}}, {{entityIdType}}>(listForDelete_{{deleteIterator}});
            });
        }
"""; 
        }

        private static List<string> GetManyToOneDeleteQueries(SpiderClass entity, List<SpiderClass> allEntities, string listForDeleteVariableName, int deleteIterator)
        {
            if (deleteIterator > 5000)
                return new List<string> { "You made cascade delete infinite loop." };

            List<string> result = new();

            List<SpiderProperty> manyToOneRequiredProperties = Helpers.GetManyToOneRequiredProperties(entity.Name, allEntities);

            foreach (SpiderProperty property in manyToOneRequiredProperties)
            {
                SpiderClass parentEntity = allEntities.Where(x => x.Name == property.EntityName).SingleOrDefault();

                if (parentEntity.IsManyToMany())
                {
                    result.Add($$"""
                await _context.DbSet<{{parentEntity.Name}}>()
                    .Where(x => {{listForDeleteVariableName}}_{{deleteIterator}}.Contains(x.{{property.Name}}.Id))
                    .ExecuteDeleteAsync();
""");

                    continue; // FT: Continue because M2M could never be required
                }
                else
                {
                    result.Add($$"""
                var {{parentEntity.Name.FirstCharToLower()}}ListForDeleteBecause{{property.Name}}_{{deleteIterator + 1}} = await _context.DbSet<{{parentEntity.Name}}>()
                    .Where(x => {{listForDeleteVariableName}}_{{deleteIterator}}.Contains(x.{{property.Name}}.Id))
                    .Select(x => x.Id).ToListAsync();
""");

                }

                result.AddRange(GetManyToOneDeleteQueries(parentEntity, allEntities, $"{parentEntity.Name.FirstCharToLower()}ListForDeleteBecause{property.Name}", deleteIterator + 1));

                result.Add($$"""
                await _context.DbSet<{{parentEntity.Name}}>()
                    .Where(x => {{parentEntity.Name.FirstCharToLower()}}ListForDeleteBecause{{property.Name}}_{{deleteIterator + 1}}.Contains(x.Id))
                    .ExecuteDeleteAsync();
""");
            }

            return result;
        }

        #endregion

        #region M2M

        private static string GetManyToManyData(SpiderClass entity, List<SpiderClass> allEntityClasses)
        {
            if (entity.Properties.Count == Settings.NumberOfPropertiesWithoutAdditionalManyToManyProperties)
                return null;

            List<SpiderProperty> manyToManyProperties = entity.Properties;

            SpiderProperty mainEntityProperty = manyToManyProperties
                .Where(x => x.Attributes.Any(x => x.Name == "M2MMaintanceEntity"))
                .SingleOrDefault(); // eg. Category
            SpiderAttribute mainEntityAttribute = mainEntityProperty.Attributes.Where(x => x.Name == "M2MMaintanceEntity").SingleOrDefault();

            SpiderProperty extendEntityProperty = manyToManyProperties
                .Where(x => x.Attributes.Any(x => x.Name == "M2MEntity"))
                .SingleOrDefault();
            SpiderAttribute extendEntityAttribute = extendEntityProperty.Attributes.Where(x => x.Name == "M2MEntity").SingleOrDefault();

            if (mainEntityProperty == null)
                return null;

            if (extendEntityProperty == null)
                return "YouNeedToDefineExtendEntityAlso";

            if (mainEntityAttribute?.Value != extendEntityAttribute?.Value) // FT HACK, FT TODO: For now, when we migrate UserNotification and PartnerUserPartnerNotification, we should change this.
                return null;

            SpiderClass mainEntityClass = allEntityClasses.Where(x => x.Name == mainEntityProperty.Type).Single();
            string mainEntityIdType = mainEntityClass.GetIdType(allEntityClasses);

            SpiderClass extendEntityClass = allEntityClasses.Where(x => x.Name == extendEntityProperty.Type).Single();
            string extendEntityIdType = extendEntityClass.GetIdType(allEntityClasses);

            return $$"""
        /// <summary>
        /// Call this method when you have additional fields in M2M association
        /// </summary>
        public async Task Update{{extendEntityProperty.Type}}ListFor{{mainEntityProperty.Type}}({{mainEntityIdType}} {{mainEntityProperty.Name.FirstCharToLower()}}Id, List<{{entity.Name}}DTO> selected{{entity.Name}}DTOList)
        {
            if (selected{{entity.Name}}DTOList == null)
                return;

            List<{{entity.Name}}DTO> selectedDTOListHelper = selected{{entity.Name}}DTOList.ToList();

            await _context.WithTransactionAsync(async () =>
            {
                // FT: Not doing authorization here, because we can not figure out here if we are updating while inserting object (eg. User), or updating object, we will always get the id which is not 0 here.

                var dbSet = _context.DbSet<{{entity.Name}}>();
                var {{entity.Name.FirstCharToLower()}}List = await dbSet.Where(x => x.{{mainEntityProperty.Name}}.Id == {{mainEntityProperty.Name.FirstCharToLower()}}Id).ToListAsync();

                foreach ({{entity.Name}}DTO selected{{entity.Name}}DTO in selectedDTOListHelper)
                {
                    var validationRules = new {{entity.Name}}DTOValidationRules();
                    DefaultValidatorExtensions.ValidateAndThrow(validationRules, selected{{entity.Name}}DTO);

                    var {{entity.Name.FirstCharToLower()}} = {{entity.Name.FirstCharToLower()}}List.Where(x => x.{{extendEntityProperty.Name}}.Id == selected{{entity.Name}}DTO.{{extendEntityProperty.Name}}Id).SingleOrDefault();

                    if ({{entity.Name.FirstCharToLower()}} == null)
                    {
                        {{entity.Name.FirstCharToLower()}} = TypeAdapter.Adapt<{{entity.Name}}>(selected{{entity.Name}}DTO, Mapper.{{entity.Name}}DTOToEntityConfig());
                        {{entity.Name.FirstCharToLower()}}.{{mainEntityProperty.Name}} = await GetInstanceAsync<{{mainEntityProperty.Type}}, {{mainEntityIdType}}>({{mainEntityProperty.Name.FirstCharToLower()}}Id, null);
                        {{entity.Name.FirstCharToLower()}}.{{extendEntityProperty.Name}} = await GetInstanceAsync<{{extendEntityProperty.Type}}, {{extendEntityIdType}}>(selected{{entity.Name}}DTO.{{extendEntityProperty.Name}}Id.Value, null);
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

        private static string GetUsings(string basePartOfTheNamespace, string projectName)
        {
            return $$"""
using {{basePartOfTheNamespace}}.ValidationRules;
using {{basePartOfTheNamespace}}.DataMappers;
using {{basePartOfTheNamespace}}.DTO;
using {{basePartOfTheNamespace}}.Entities;
using {{basePartOfTheNamespace}}.Enums;
using {{basePartOfTheNamespace}}.ExcelProperties;
using {{basePartOfTheNamespace}}.TableFiltering;
{{(projectName == "Security" ? "" : $"using {basePartOfTheNamespace.ReplaceEverythingAfterLast(".", ".Shared")}.Resources;")}}
using Microsoft.EntityFrameworkCore;
using System.Data;
using FluentValidation;
using Spider.Security.Services;
using Spider.Security.Interfaces;
using Spider.Security.Entities;
using Spider.Shared.Excel;
using Spider.Shared.Interfaces;
using Spider.Shared.Services;
using Spider.Shared.DTO;
using Spider.Shared.Entities;
using Spider.Shared.Extensions;
using Spider.Shared.Exceptions;
using Spider.Shared.Resources;
using Mapster;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.Http;
""";
        }

        private static string GetAuthorizeEntityMethodCall(string entityName, CrudCodes crudCode, string parametersBody)
        {
            string methodName = Helpers.GetAuthorizeEntityMethodName(entityName, crudCode);
            return $"await _authorizationService.{methodName}({parametersBody});";
        }

        #endregion

    }
}