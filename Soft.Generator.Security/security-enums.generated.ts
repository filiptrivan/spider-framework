export enum LoginVerificationResultStatusCodes
{
    
    
}

export enum PermissionCodes
{
    
    ReadPermission,
	EditPermission,
	InsertPermission,
	DeletePermission,
	ReadRole,
	EditRole,
	InsertRole,
	DeleteRole,
	ReadRolePermission,
	EditRolePermission,
	InsertRolePermission,
	DeleteRolePermission,
	ReadUserRole,
	EditUserRole,
	InsertUserRole,
	DeleteUserRole,
	ReadUserExtended,
	EditUserExtended,
	InsertUserExtended,
	DeleteUserExtended
}

export enum RegistrationVerificationResultStatusCodes
{
    UserDoesNotExistAndDoesNotHaveValidToken = 0,
	UserWithoutPasswordExists = 1,
	UserWithPasswordExists = 2,
	UnexpectedError = 3,
    
}


