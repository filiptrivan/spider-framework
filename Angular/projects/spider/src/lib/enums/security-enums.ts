export enum LoginVerificationResultStatusCodes
{
    
}

export enum RegistrationVerificationResultStatusCodes
{
    UserDoesNotExistAndDoesNotHaveValidToken = 0,
	UserWithoutPasswordExists = 1,
	UserWithPasswordExists = 2,
	UnexpectedError = 3,
}


