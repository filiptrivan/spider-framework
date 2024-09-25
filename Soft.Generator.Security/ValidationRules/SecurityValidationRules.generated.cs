//using FluentValidation;
//using Soft.Generator.Security.DTO;
//using Soft.Generator.Shared.SoftFluentValidation;

//namespace Soft.Generator.Security.ValidationRules
//{
//    public class VerificationTokenRequestDTOValidationRules : AbstractValidator<VerificationTokenRequestDTO>
//    {
//        public VerificationTokenRequestDTOValidationRules()
//        {
//            RuleFor(x => x.VerificationCode).NotEmpty().Length(6);
//			RuleFor(x => x.Email).NotEmpty().Length(5, 100).EmailAddress();
//        }
//    }
//    public class RefreshTokenRequestDTOValidationRules : AbstractValidator<RefreshTokenRequestDTO>
//    {
//        public RefreshTokenRequestDTOValidationRules()
//        {
            
//        }
//    }
//    public class RoleSaveBodyDTOValidationRules : AbstractValidator<RoleSaveBodyDTO>
//    {
//        public RoleSaveBodyDTOValidationRules()
//        {
            
//        }
//    }
//    public class RegistrationDTOValidationRules : AbstractValidator<RegistrationDTO>
//    {
//        public RegistrationDTOValidationRules()
//        {
//            RuleFor(x => x.Email).NotEmpty().Length(5, 100).EmailAddress();
//			RuleFor(x => x.Password).NotEmpty().Length(6, 20);
//        }
//    }
//    public class RoleDTOValidationRules : AbstractValidator<RoleDTO>
//    {
//        public RoleDTOValidationRules()
//        {
//            RuleFor(x => x.Name).NotEmpty().Length(0, 100);
//			RuleFor(x => x.Description).Length(0, 400);
//        }
//    }
//    public class NotificationDTOValidationRules : AbstractValidator<NotificationDTO>
//    {
//        public NotificationDTOValidationRules()
//        {
//            RuleFor(x => x.Title).Length(1, 60).NotEmpty();
//			RuleFor(x => x.TitleLatin).Length(1, 60).NotEmpty();
//			RuleFor(x => x.Description).Length(1, 255).NotEmpty();
//			RuleFor(x => x.DescriptionLatin).Length(1, 255).NotEmpty();
//        }
//    }
//    public class PermissionDTOValidationRules : AbstractValidator<PermissionDTO>
//    {
//        public PermissionDTOValidationRules()
//        {
//            RuleFor(x => x.Name).NotEmpty().Length(0, 100);
//			RuleFor(x => x.NameLatin).NotEmpty().Length(0, 100);
//			RuleFor(x => x.Description).Length(0, 400);
//			RuleFor(x => x.DescriptionLatin).Length(0, 400);
//			RuleFor(x => x.Code).NotEmpty().Length(0, 100);
//        }
//    }
//    public class RegistrationVerificationTokenDTOValidationRules : AbstractValidator<RegistrationVerificationTokenDTO>
//    {
//        public RegistrationVerificationTokenDTOValidationRules()
//        {
            
//        }
//    }
//    public class JwtAuthResultDTOValidationRules : AbstractValidator<JwtAuthResultDTO>
//    {
//        public JwtAuthResultDTOValidationRules()
//        {
            
//        }
//    }
//    public class LoginVerificationTokenDTOValidationRules : AbstractValidator<LoginVerificationTokenDTO>
//    {
//        public LoginVerificationTokenDTOValidationRules()
//        {
            
//        }
//    }
//    public class ForgotPasswordDTOValidationRules : AbstractValidator<ForgotPasswordDTO>
//    {
//        public ForgotPasswordDTOValidationRules()
//        {
//            RuleFor(x => x.Email).NotEmpty().Length(5, 100).EmailAddress();
//			RuleFor(x => x.NewPassword).NotEmpty().Length(6, 20);
//        }
//    }
//    public class RegistrationVerificationResultDTOValidationRules : AbstractValidator<RegistrationVerificationResultDTO>
//    {
//        public RegistrationVerificationResultDTOValidationRules()
//        {
            
//        }
//    }
//    public class ForgotPasswordVerificationTokenDTOValidationRules : AbstractValidator<ForgotPasswordVerificationTokenDTO>
//    {
//        public ForgotPasswordVerificationTokenDTOValidationRules()
//        {
            
//        }
//    }
//    public class ExternalProviderDTOValidationRules : AbstractValidator<ExternalProviderDTO>
//    {
//        public ExternalProviderDTOValidationRules()
//        {
            
//        }
//    }
//    public class NotificationSaveBodyDTOValidationRules : AbstractValidator<NotificationSaveBodyDTO>
//    {
//        public NotificationSaveBodyDTOValidationRules()
//        {
            
//        }
//    }
//    public class LoginDTOValidationRules : AbstractValidator<LoginDTO>
//    {
//        public LoginDTOValidationRules()
//        {
//            RuleFor(x => x.Email).NotEmpty().Length(5, 100).EmailAddress();
//			RuleFor(x => x.Password).NotEmpty().Length(6, 20);
//        }
//    }
//    public class RefreshTokenDTOValidationRules : AbstractValidator<RefreshTokenDTO>
//    {
//        public RefreshTokenDTOValidationRules()
//        {
            
//        }
//    }
//    public class LoginResultDTOValidationRules : AbstractValidator<LoginResultDTO>
//    {
//        public LoginResultDTOValidationRules()
//        {
            
//        }
//    }
//}

