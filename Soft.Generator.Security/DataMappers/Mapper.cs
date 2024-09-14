//using Riok.Mapperly.Abstractions;
//using Soft.Generator.Security.DTO;
//using Soft.Generator.Security.Entities;

//namespace Soft.Generator.Security.DataMappers
//{
//    [Mapper] // https://mapperly.riok.app/docs/configuration/user-implemented-methods/
//    public static partial class Mapper
//    {

//        // I don't need to ignore Id and Version here because when it's protected set it ignores without me specifying it
//        // also you can specify Lists and reference types inside DTO but need to be very careful because infinite loops
//        //public static partial User Map(UserDTO dto);

//        [MapperIgnoreTarget(nameof(UserDTO.Password))]
//        public static partial UserDTO Map(User poco);

//        [MapperIgnoreTarget(nameof(UserDTO.Password))]
//        public static partial UserDTO ExcelMap(User poco);

//    }
//}

//https://www.reddit.com/r/programming/comments/109wi5h/mapperly_a_net_source_generator_for_object_to/