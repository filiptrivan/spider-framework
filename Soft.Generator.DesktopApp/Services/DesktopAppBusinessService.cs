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
    public class DesktopAppBusinessService : BusinessServiceBase
    {
        private readonly SqlConnection _connection;

        public DesktopAppBusinessService(SqlConnection connection)
            : base(connection)
        {
            _connection = connection;
        }

        public Permission InsertPermission(Permission permission)
        {
            if (permission == null)
                throw new Exception("Ne možete da ubacite prazan objekat.");

            // FT: Not validating here property by property, because my sql will throw exception, we should already validate object on the form.

            string query = $"INSERT INTO Permission (Name, Code) VALUES (@Name, @Code);";

            _connection.WithTransaction(() =>
            {
                using (SqlCommand cmd = new SqlCommand(query, _connection))
                {
                    cmd.Parameters.AddWithValue("@Name", permission.Name);
                    cmd.Parameters.AddWithValue("@Code", permission.Code);

                    cmd.ExecuteNonQuery();
                }
            });

            return permission;
        }

        public List<Permission> GetPermissionList()
        {
            List<Permission> permissionList = new List<Permission>();
            Dictionary<int, Permission> permissionDict = new Dictionary<int, Permission>();

            string query = @$"
SELECT permission.Id AS PermissionId, permission.Name AS PermissionName, permission.Code AS PermissionCode, company.Id AS CompanyId, company.Name AS CompanyName
FROM Permission AS permission
LEFT JOIN CompanyPermission AS companypermission ON permission.Id = companypermission.PermissionId
LEFT JOIN Company AS company ON company.Id = companypermission.CompanyId
";

            _connection.WithTransaction(() =>
            {
                using (SqlCommand cmd = new SqlCommand(query, _connection))
                {
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            int permissionId = reader.GetInt32(reader.GetOrdinal("PermissionId"));

                            if (!permissionDict.TryGetValue(permissionId, out Permission permission))
                            {
                                permission = new Permission
                                {
                                    Id = permissionId,
                                    Name = reader.GetString(reader.GetOrdinal("PermissionName")),
                                    Code = reader.GetString(reader.GetOrdinal("PermissionCode")),
                                    Companies = new List<Company>()
                                };

                                permissionDict[permissionId] = permission;
                                permissionList.Add(permission);
                            }

                            if (!reader.IsDBNull(reader.GetOrdinal("CompanyId")))
                            {
                                Company company = new Company
                                {
                                    Id = reader.GetInt32(reader.GetOrdinal("CompanyId")),
                                    Name = reader.GetString(reader.GetOrdinal("CompanyName"))
                                };

                                permission.Companies.Add(company);
                            }
                        }
                    }
                }
            });

            return permissionList;
        }

        public Permission GetPermission(int id)
        {
            List<Permission> permissionList = new List<Permission>();
            Dictionary<int, Permission> permissionDict = new Dictionary<int, Permission>();

            string query = @$"
SELECT permission.Id AS PermissionId, permission.Name AS PermissionName, permission.Code AS PermissionCode, company.Id AS CompanyId, company.Name AS CompanyName
FROM Permission AS permission
LEFT JOIN CompanyPermission AS companypermission ON permission.Id = companypermission.PermissionId
LEFT JOIN Company AS company ON company.Id = companypermission.CompanyId
WHERE permission.Id = @id
";

            _connection.WithTransaction(() =>
            {
                using (SqlCommand cmd = new SqlCommand(query, _connection))
                {
                    cmd.Parameters.AddWithValue("@id", id);

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            int permissionId = reader.GetInt32(reader.GetOrdinal("PermissionId"));

                            if (!permissionDict.TryGetValue(permissionId, out Permission permission))
                            {
                                permission = new Permission
                                {
                                    Id = permissionId,
                                    Name = reader.GetString(reader.GetOrdinal("PermissionName")),
                                    Code = reader.GetString(reader.GetOrdinal("PermissionCode")),
                                    Companies = new List<Company>()
                                };

                                permissionDict[permissionId] = permission;
                                permissionList.Add(permission);
                            }

                            if (!reader.IsDBNull(reader.GetOrdinal("CompanyId")))
                            {
                                Company company = new Company
                                {
                                    Id = reader.GetInt32(reader.GetOrdinal("CompanyId")),
                                    Name = reader.GetString(reader.GetOrdinal("CompanyName"))
                                };

                                permission.Companies.Add(company);
                            }
                        }
                    }
                }
            });

            Permission permission = permissionList.SingleOrDefault();

            if (permission == null)
                throw new Exception("Objekat ne postoji u bazi podataka.");

            return permission;
        }

        public void DeletePermission(int id)
        {
            _connection.WithTransaction(() =>
            {
                List<int> permissionListToDelete = GetPermissionList().Where(x => x.Id == id).Select(x => x.Id).ToList();
                List<long> companyListToDelete = [1];
                DeleteEntities<Company, long>(companyListToDelete);
                DeleteEntities<Permission, int>(permissionListToDelete);
            });
        }

    }
}
