using Microsoft.EntityFrameworkCore;
using Soft.Generator.Shared.Attributes;
using Soft.Generator.Shared.BaseEntities;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Soft.Generator.Security.Entities
{
    // FT: There will not be the problem even if the app needs two tables for the same entity, because it will be sent from the client what should be in the table data
    [CustomValidator("RuleFor(x => x.Password).NotEmpty().Length(6, 20);")] // FT: making it again because the validation for entity and DTO are not the same, generator will always take first custom validators over the EF parsed ones.
    public class User : BusinessObject<long>
    {
        // FT: I think we don't need username, we are just complicating with it, email is sufficient identifier
        //[SoftDisplayName]
        //[CustomValidator("Must(CustomValidators.NotHaveWhiteSpace)")]
        //[DatabaseGenerated(DatabaseGeneratedOption.Identity)] // TODO FT: Check if this is working
        //[StringLength(75, MinimumLength = 2)]
        //[Required] // If user doensn't provide it, we will generate it for him (Username = Email)
        //public string Username { get; set; }

        [SoftDisplayName]
        [CustomValidator("EmailAddress()")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [StringLength(70, MinimumLength = 5)]
        [Required]
        public string Email { get; set; }

        // FT HACK: Password is not required in database because of external provider login, but the DTO property Password is
        [StringLength(80, MinimumLength = 40)]
        public string Password { get; set; }

        [Required]
        public bool HasLoggedInWithExternalProvider { get; set; }

        [Required]
        public int NumberOfFailedAttemptsInARow { get; set; }

        [Required]
        public bool IsVerified { get; set; }

        public virtual List<Role> Roles { get; set; }
    }
}
