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

export enum SecurityPermissionCodes
{
	ReadUser = "ReadUser",
	UpdateUser = "UpdateUser",
	InsertUser = "InsertUser",
	DeleteUser = "DeleteUser",
	ReadRole = "ReadRole",
	UpdateRole = "UpdateRole",
	InsertRole = "InsertRole",
	DeleteRole = "DeleteRole",
	ReadPermission = "ReadPermission",
	UpdatePermission = "UpdatePermission",
	InsertPermission = "InsertPermission",
	DeletePermission = "DeletePermission",
}
