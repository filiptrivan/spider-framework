using Riok.Mapperly.Abstractions;
using Soft.Generator.Security.DTO;
using Soft.Generator.Security.Entities;

namespace Soft.Generator.Security.DataMappers
{
    [Mapper]
    public static partial class Mapper
    {

        #region Permission

        public static partial Permission Map(PermissionDTO dto);

                
        public static partial PermissionDTO Map(Permission poco);

                
        public static partial PermissionDTO ExcelMap(Permission poco);

                
        public static partial IQueryable<PermissionDTO> ProjectTo(this IQueryable<Permission> poco);

                
        public static partial IQueryable<PermissionDTO> ExcelProjectTo(this IQueryable<Permission> poco);

        public static partial void MergeMap(PermissionDTO dto, Permission poco);

        #endregion


        #region Role

        public static partial Role Map(RoleDTO dto);

                
        public static partial RoleDTO Map(Role poco);

                
        public static partial RoleDTO ExcelMap(Role poco);

                
        public static partial IQueryable<RoleDTO> ProjectTo(this IQueryable<Role> poco);

                
        public static partial IQueryable<RoleDTO> ExcelProjectTo(this IQueryable<Role> poco);

        public static partial void MergeMap(RoleDTO dto, Role poco);

        #endregion


        #region User

        public static partial User Map(UserDTO dto);

        

        

                
        public static partial IQueryable<UserDTO> ProjectTo(this IQueryable<User> poco);

                
        public static partial IQueryable<UserDTO> ExcelProjectTo(this IQueryable<User> poco);

        public static partial void MergeMap(UserDTO dto, User poco);

        #endregion

    }
}

