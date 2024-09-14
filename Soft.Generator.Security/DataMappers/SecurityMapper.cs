using Riok.Mapperly.Abstractions;
using Soft.Generator.Security.DTO;
using Soft.Generator.Security.Entities;

namespace Soft.Generator.Security.DataMappers
{
    public static partial class Mapper
    {
        // I don't need to ignore Id and Version here because when it's protected set it ignores without me specifying it
        // also you can specify Lists and reference types inside DTO but need to be very careful because of infinite loops
        // public static partial User Map(UserDTO dto);
        [MapProperty("Id", "TestColumnForGrid")]
        //[MapProperty("Role.Id", "RoleDisplayName")]
        [MapperIgnoreTarget(nameof(UserDTO.Password))]
        public static partial UserDTO Map(User poco);
        // User - source
        // UserDTO - target
        // da li cu ikada prebacivati iz Name = DTO.nesto

        [MapperIgnoreTarget(nameof(UserDTO.Password))]
        public static partial UserDTO ExcelMap(User poco);

    }
}
