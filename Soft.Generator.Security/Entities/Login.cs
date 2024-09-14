//using System;
//using System.Collections.Generic;
//using System.ComponentModel.DataAnnotations.Schema;
//using System.ComponentModel.DataAnnotations;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using Microsoft.EntityFrameworkCore.Metadata.Internal;
//using Soft.Generator.Shared.BaseEntities;
//using Soft.Generator.Shared.Attributes;

//namespace Soft.Generator.Security.Entities
//{
//    public class Login : ReadonlyObject<long>
//    {
//        // FT: we don't need any validation for Email and Password, because we want to store bad data also, to show every login try to administrators
//        [SoftDisplayName]
//        [Required]
//        public string Email { get; set; }

//        [Required]
//        [StringLength(45)]
//        public string IpAddress { get; set; }

//        [Required]
//        public bool IsSuccessful { get; set; }

//        [Required]
//        public bool IsExternal { get; set; }
//    }
//}


 