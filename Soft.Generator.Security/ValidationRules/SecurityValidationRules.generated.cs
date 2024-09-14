using FluentValidation;
using Soft.Generator.Security.DTO;
using Soft.Generator.Shared.SoftFluentValidation;

namespace Soft.Generator.Security.ValidationRules
{
    public class JwtAuthResultDTOValidationRules : AbstractValidator<JwtAuthResultDTO>
    {
        public JwtAuthResultDTOValidationRules()
        {
            
        }
    }
    public class RegistrationResultDTOValidationRules : AbstractValidator<RegistrationResultDTO>
    {
        public RegistrationResultDTOValidationRules()
        {
            
        }
    }
    public class VerificationTokenRequestDTOValidationRules : AbstractValidator<VerificationTokenRequestDTO>
    {
        public VerificationTokenRequestDTOValidationRules()
        {
            
        }
    }
    public class PermissionDTOValidationRules : AbstractValidator<PermissionDTO>
    {
        public PermissionDTOValidationRules()
        {
            RuleFor(x => x.Name).NotEmpty().Length(0, 255);
			RuleFor(x => x.Description).Length(0, 1000);
        }
    }
    public class RoleDTOValidationRules : AbstractValidator<RoleDTO>
    {
        public RoleDTOValidationRules()
        {
            RuleFor(x => x.Name).NotEmpty().Length(0, 255);
			RuleFor(x => x.Description).Length(0, 1000);
        }
    }
    public class UserDTOValidationRules : AbstractValidator<UserDTO>
    {
        public UserDTOValidationRules()
        {
            RuleFor(x => x.Password).NotEmpty().Length(6, 20);
			RuleFor(x => x.Email).EmailAddress().Length(0, 70).NotEmpty();
			RuleFor(x => x.HasLoggedInWithExternalProvider).NotEmpty();
			RuleFor(x => x.NumberOfFailedAttemptsInARow).NotEmpty();
			RuleFor(x => x.IsVerified).NotEmpty();
        }
    }
    public class ExternalProviderDTOValidationRules : AbstractValidator<ExternalProviderDTO>
    {
        public ExternalProviderDTOValidationRules()
        {
            
        }
    }
    public class LoginDTOValidationRules : AbstractValidator<LoginDTO>
    {
        public LoginDTOValidationRules()
        {
            RuleFor(x => x.Email).NotEmpty().Length(5, 100);
			RuleFor(x => x.Password).NotEmpty().Length(6, 20);
        }
    }
    public class RefreshTokenRequestDTOValidationRules : AbstractValidator<RefreshTokenRequestDTO>
    {
        public RefreshTokenRequestDTOValidationRules()
        {
            
        }
    }
    public class RegistrationDTOValidationRules : AbstractValidator<RegistrationDTO>
    {
        public RegistrationDTOValidationRules()
        {
            RuleFor(x => x.Email).NotEmpty().Length(5, 100).EmailAddress();
			RuleFor(x => x.Password).NotEmpty().Length(6, 20);
        }
    }
    public class LoginResultDTOValidationRules : AbstractValidator<LoginResultDTO>
    {
        public LoginResultDTOValidationRules()
        {
            
        }
    }
    public class RefreshTokenDTOValidationRules : AbstractValidator<RefreshTokenDTO>
    {
        public RefreshTokenDTOValidationRules()
        {
            
        }
    }
}

