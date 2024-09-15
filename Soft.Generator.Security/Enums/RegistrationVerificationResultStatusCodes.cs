using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Soft.Generator.Security.Enums
{
    public enum RegistrationVerificationResultStatusCodes
    {
        UserDoesNotExistAndDoesNotHaveValidToken = 0,
        UserWithoutPasswordExists = 1,
        UserWithPasswordExists = 2,
        UnexpectedError = 3,
        // UserDoesNotExistAndHasValidToken, // Maybe the user wants to change the password, even if he already has valid verification token
    }
}
