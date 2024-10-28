using MySqlConnector;
using Soft.Generator.DesktopApp.Entities;
using Soft.Generator.DesktopApp.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Soft.Generator.DesktopApp.Services
{
    public class DesktopAppService
    {
        private readonly MySqlConnection _connection;

        public DesktopAppService(MySqlConnection connection)
        {
            _connection = connection;
        }

        public Permission InsertPermission(Permission permission)
        {
            if (permission == null)
                throw new Exception("Ne možete da ubacite prazan objekat.");

            // FT: Not validating here property by property, because my sql will throw exception, we should already validate object on the form.

            string query = $"INSERT INTO {nameof(Permission)} (Name, Code) VALUES (@Name, @Code)";

            _connection.WithTransaction(() =>
            {
                using (MySqlCommand cmd = new MySqlCommand(query, _connection))
                {
                    cmd.Parameters.AddWithValue("@Name", permission.Name);
                    cmd.Parameters.AddWithValue("@Code", permission.Code);

                    cmd.ExecuteNonQuery();
                }
            });

            return permission;
        }

        public List<Permission> GetPermissions()
        {
            List<Permission> permissionList = new List<Permission>();

            string query = "SELECT * FROM Permission";

            _connection.WithTransaction(() =>
            {
                using (MySqlCommand cmd = new MySqlCommand(query, _connection))
                {
                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            permissionList.Add(new Permission
                            {
                                Id = reader.GetInt32("Id"),
                                Name = reader.GetString("Name"),
                                Code = reader.GetString("Code"),
                            });
                        }
                    }
                }
            });

            return permissionList;
        }

        public Permission GetPermission(long id)
        {
            List<Permission> permissionList = new List<Permission>();

            string query = $"SELECT * FROM {nameof(Permission)} WHERE Id = @id";

            _connection.WithTransaction(() =>
            {
                using (MySqlCommand cmd = new MySqlCommand(query, _connection))
                {
                    cmd.Parameters.AddWithValue("@id", id);

                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {

                        while (reader.Read())
                        {
                            permissionList.Add(new Permission
                            {
                                Id = reader.GetInt32("Id"),
                                Name = reader.GetString("Name"),
                                Code = reader.GetString("Code"),
                            });
                        }
                    }
                }
            });

            Permission permission = permissionList.SingleOrDefault();

            if (permission == null)
                throw new Exception("Objekat ne postoji u bazi podataka.");

            return permission;
        }

    }
}
