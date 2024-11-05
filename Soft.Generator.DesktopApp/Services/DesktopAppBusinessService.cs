using Microsoft.Data.SqlClient;
using Soft.Generator.DesktopApp.Entities;
using Soft.Generator.DesktopApp.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Soft.Generator.DesktopApp.Services
{
    /// <summary>
    /// Every get method is returning only flat data without any related data, because of performance
    /// When inserting data with a foreign key, only the Id field in related data is mandatory. Additionally, the Id must correspond to an existing record in the database.
    /// </summary>
    public class DesktopAppBusinessService : DesktopAppBusinessServiceGenerated
    {
        private readonly SqlConnection _connection;

        public DesktopAppBusinessService(SqlConnection connection)
            : base(connection)
        {
            _connection = connection;
        }

        public GeneratedFile InsertGeneratedFile(GeneratedFile entity)
        {
            if (entity == null)
                throw new Exception("Ne možete da ubacite prazan objekat.");

            // FT: Not validating here property by property, because sql server will throw exception, we should already validate object on the form.

            string query = $"UPDATE GeneratedFile SET Id = @Id, DisplayName, ClassName, Namespace, Regenerate, ApplicationId, DomainFolderPathId) VALUES (@Id, @DisplayName, @ClassName, @Namespace, @Regenerate, @ApplicationId, @DomainFolderPathId);";

            _connection.WithTransaction(() =>
            {
                using (SqlCommand cmd = new SqlCommand(query, _connection))
                {
                    cmd.Parameters.AddWithValue("@Id", entity.Id);
                    cmd.Parameters.AddWithValue("@DisplayName", entity.DisplayName);
                    cmd.Parameters.AddWithValue("@ClassName", entity.ClassName);
                    cmd.Parameters.AddWithValue("@Namespace", entity.Namespace);
                    cmd.Parameters.AddWithValue("@Regenerate", entity.Regenerate);
                    cmd.Parameters.AddWithValue("@ApplicationId", entity.Application.Id);
                    cmd.Parameters.AddWithValue("@DomainFolderPathId", entity.DomainFolderPath.Id);

                    cmd.ExecuteNonQuery();
                }
            });

            return entity;
        }

        //        public Permission InsertPermission(Permission permission)
        //        {
        //            if (permission == null)
        //                throw new Exception("Ne možete da ubacite prazan objekat.");

        //            // FT: Not validating here property by property, because my sql will throw exception, we should already validate object on the form.

        //            string query = $"INSERT INTO Permission (Name, Code) VALUES (@Name, @Code);";

        //            _connection.WithTransaction(() =>
        //            {
        //                using (SqlCommand cmd = new SqlCommand(query, _connection))
        //                {
        //                    cmd.Parameters.AddWithValue("@Name", permission.Name);
        //                    cmd.Parameters.AddWithValue("@Code", permission.Code);

        //                    cmd.ExecuteNonQuery();
        //                }
        //            });

        //            return permission;
        //        }

        //        public List<Permission> GetPermissionList()
        //        {
        //            List<Permission> permissionList = new List<Permission>();
        //            Dictionary<int, Permission> permissionDict = new Dictionary<int, Permission>();

        //            string query = @$"
        //SELECT permission.Id AS PermissionId, permission.Name AS PermissionName, permission.Code AS PermissionCode, company.Id AS CompanyId, company.Name AS CompanyName
        //FROM Permission AS permission
        //LEFT JOIN CompanyPermission AS companypermission ON permission.Id = companypermission.PermissionId
        //LEFT JOIN Company AS company ON company.Id = companypermission.CompanyId
        //";

        //            _connection.WithTransaction(() =>
        //            {
        //                using (SqlCommand cmd = new SqlCommand(query, _connection))
        //                {
        //                    using (SqlDataReader reader = cmd.ExecuteReader())
        //                    {
        //                        while (reader.Read())
        //                        {
        //                            int permissionId = reader.GetInt32(reader.GetOrdinal("PermissionId"));

        //                            if (!permissionDict.TryGetValue(permissionId, out Permission permission))
        //                            {
        //                                permission = new Permission
        //                                {
        //                                    Id = permissionId,
        //                                    Name = reader.GetString(reader.GetOrdinal("PermissionName")),
        //                                    Code = reader.GetString(reader.GetOrdinal("PermissionCode")),
        //                                    Companies = new List<Company>()
        //                                };

        //                                permissionDict[permissionId] = permission;
        //                                permissionList.Add(permission);
        //                            }

        //                            if (!reader.IsDBNull(reader.GetOrdinal("CompanyId")))
        //                            {
        //                                Company company = new Company
        //                                {
        //                                    Id = reader.GetInt32(reader.GetOrdinal("CompanyId")),
        //                                    Name = reader.GetString(reader.GetOrdinal("CompanyName"))
        //                                };

        //                                permission.Companies.Add(company);
        //                            }
        //                        }
        //                    }
        //                }
        //            });

        //            return permissionList;
        //        }

        //        public List<Permission> GetPermissionListe(List<int> ids)
        //        {
        //            List<Permission> permissionList = new List<Permission>();
        //            Dictionary<int, Permission> permissionDict = new Dictionary<int, Permission>();

        //            if (ids == null || ids.Count == 0)
        //                throw new ArgumentException("Lista koju želite da obrišete ne može da bude prazna.");

        //            List<string> parameters = new List<string>();
        //            for (int i = 0; i < ids.Count; i++)
        //            {
        //                parameters.Add($"@id{i}");
        //            }

        //            string query = @$"
        //SELECT DISTINCT permission.Id AS PermissionId, permission.Name AS PermissionName, permission.Code AS PermissionCode
        //FROM Permission AS permission
        //LEFT JOIN Company AS company on company.PermissionId = permission.Id
        //WHERE company.Id IN ({string.Join(", ", parameters)});
        //";

        //            _connection.WithTransaction(() =>
        //            {
        //                using (SqlCommand cmd = new SqlCommand(query, _connection))
        //                {
        //                    for (int i = 0; i < ids.Count; i++)
        //                    {
        //                        cmd.Parameters.AddWithValue($"@id{i}", ids[i]);
        //                    }

        //                    using (SqlDataReader reader = cmd.ExecuteReader())
        //                    {
        //                        while (reader.Read())
        //                        {
        //                            int permissionId = reader.GetInt32(reader.GetOrdinal("PermissionId"));

        //                            if (!permissionDict.TryGetValue(permissionId, out Permission permission))
        //                            {
        //                                permission = new Permission
        //                                {
        //                                    Id = permissionId,
        //                                    Name = reader.GetString(reader.GetOrdinal("PermissionName")),
        //                                    Code = reader.GetString(reader.GetOrdinal("PermissionCode")),
        //                                    Companies = new List<Company>()
        //                                };

        //                                permissionDict[permissionId] = permission;
        //                                permissionList.Add(permission);
        //                            }
        //                        }
        //                    }
        //                }
        //            });

        //            return permissionList;
        //        }

        //        public Permission GetPermission(int id)
        //        {
        //            List<Permission> permissionList = new List<Permission>();
        //            Dictionary<int, Permission> permissionDict = new Dictionary<int, Permission>();
        //            Dictionary<int, Company> companyDict = new Dictionary<int, Company>();
        //            Dictionary<long, WebApplication> applicationDict = new Dictionary<long, WebApplication>();

        //            string query = @$"
        //SELECT 
        //permission.Id AS PermissionId, permission.Name AS PermissionName, permission.Code AS PermissionCode, 
        //company.Id AS CompanyId, company.Name AS CompanyName,
        //application.Id AS WebApplicationId, application.Name AS WebApplicationName
        //FROM Permission AS permission
        //LEFT JOIN CompanyPermission AS companypermission ON permission.Id = companypermission.PermissionId
        //LEFT JOIN Company AS company ON company.Id = companypermission.CompanyId
        //LEFT JOIN WebApplication AS application ON application.CompanyId = companypermission.CompanyId
        //WHERE permission.Id = @id
        //";

        //            _connection.WithTransaction(() =>
        //            {
        //                using (SqlCommand cmd = new SqlCommand(query, _connection))
        //                {
        //                    cmd.Parameters.AddWithValue("@id", id);

        //                    using (SqlDataReader reader = cmd.ExecuteReader())
        //                    {
        //                        while (reader.Read())
        //                        {
        //                            if (reader.IsDBNull(reader.GetOrdinal("PermissionId")))
        //                            {
        //                                int permissionId = reader.GetInt32(reader.GetOrdinal("PermissionId"));
        //                                bool permissionAlreadyAdded = permissionDict.TryGetValue(permissionId, out Permission permission);
        //                                if (!permissionAlreadyAdded)
        //                                {
        //                                    permission = new Permission
        //                                    {
        //                                        Id = permissionId,
        //                                        Name = reader.GetString(reader.GetOrdinal("PermissionName")),
        //                                        Code = reader.GetString(reader.GetOrdinal("PermissionCode")),
        //                                        Companies = new List<Company>()
        //                                    };

        //                                    permissionDict[permissionId] = permission;
        //                                }

        //                                if (reader.IsDBNull(reader.GetOrdinal("CompanyId")))
        //                                {
        //                                    int companyId = reader.GetInt32(reader.GetOrdinal("CompanyId"));
        //                                    bool companyAlreadyAdded = companyDict.TryGetValue(companyId, out Company company);
        //                                    if (!companyAlreadyAdded)
        //                                    {
        //                                        company = new Company
        //                                        {
        //                                            Id = companyId,
        //                                            Name = reader.GetString(reader.GetOrdinal("CompanyName")),
        //                                        };

        //                                        companyDict[companyId] = company;
        //                                        permission.Companies.Add(company);
        //                                    }

        //                                    if (reader.IsDBNull(reader.GetOrdinal("WebApplicationId")))
        //                                    {
        //                                        long applicationId = reader.GetInt32(reader.GetOrdinal("WebApplicationId"));
        //                                        bool applicationAlreadyAdded = applicationDict.TryGetValue(applicationId, out WebApplication application);
        //                                        if (!applicationAlreadyAdded)
        //                                        {
        //                                            application = new WebApplication
        //                                            {
        //                                                Id = applicationId,
        //                                                Name = reader.GetString(reader.GetOrdinal("WebApplicationName")),
        //                                                Setting = new Setting(),
        //                                            };

        //                                            company.WebApplications.Add(application);
        //                                        }

        //                                        if (reader.IsDBNull(reader.GetOrdinal("SettingId")))
        //                                        {
        //                                            long settingId = reader.GetInt32(reader.GetOrdinal("WebApplicationId"));
        //                                            bool settingAlreadyAdded = settingDict.TryGetValue(settingId, out Setting setting);
        //                                            if (!applicationAlreadyAdded)
        //                                            {
        //                                                setting = new Setting
        //                                                {
        //                                                    Id = settingId,    
        //                                                };

        //                                                application.Setting = setting;
        //                                            }
        //                                        }
        //                                    }
        //                                }

        //                                permissionList.Add(permission);
        //                            }
        //                        }
        //                    }
        //                }
        //            });

        //            Permission permission = permissionList.SingleOrDefault();

        //            if (permission == null)
        //                throw new Exception("Objekat ne postoji u bazi podataka.");

        //            return permission;
        //        }

        //public void DeletePermission(int id)
        //{
        //    _connection.WithTransaction(() =>
        //    {
        //        //List<int> permissionListToDelete = GetPermissionList().Where(x => x.Id == id).Select(x => x.Id).ToList();
        //        //List<int> companyListToDelete = GetGeneratedFileListForApplication(id).Select(x => x.Id).ToList();
        //        //DeleteEntities<Company, int>(companyListToDelete);
        //        //DeleteEntities<Permission, int>(permissionListToDelete);
        //    });
        //}

    }
}
