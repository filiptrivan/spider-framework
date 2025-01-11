import { BaseEntity } from "src/app/core/entities/base-entity";
import { TableFilter } from "src/app/core/entities/table-filter";
import { TableFilterContext } from "src/app/core/entities/table-filter-context";
import { TableFilterSortMeta } from "src/app/core/entities/table-filter-sort-meta";
import { MimeTypes } from "src/app/core/entities/mime-type";
import { Status } from "../../enums/generated/security-enums.generated";


export class AuthResult extends BaseEntity
{
    userId?: number;
	email?: string;
	accessToken?: string;
	refreshToken?: string;

    constructor(
    {
        userId,
		email,
		accessToken,
		refreshToken
    }:{
        userId?: number;
		email?: string;
		accessToken?: string;
		refreshToken?: string;     
    } = {}
    ) {
        super('AuthResult'); 

        this.userId = userId;
		this.email = email;
		this.accessToken = accessToken;
		this.refreshToken = refreshToken;
    }
}


export class ExternalProvider extends BaseEntity
{
    idToken?: string;
	browserId?: string;

    constructor(
    {
        idToken,
		browserId
    }:{
        idToken?: string;
		browserId?: string;     
    } = {}
    ) {
        super('ExternalProvider'); 

        this.idToken = idToken;
		this.browserId = browserId;
    }
}


export class JwtAuthResult extends BaseEntity
{
    userId?: number;
	userEmail?: string;
	accessToken?: string;
	token?: RefreshToken;

    constructor(
    {
        userId,
		userEmail,
		accessToken,
		token
    }:{
        userId?: number;
		userEmail?: string;
		accessToken?: string;
		token?: RefreshToken;     
    } = {}
    ) {
        super('JwtAuthResult'); 

        this.userId = userId;
		this.userEmail = userEmail;
		this.accessToken = accessToken;
		this.token = token;
    }
}


export class Login extends BaseEntity
{
    email?: string;
	browserId?: string;

    constructor(
    {
        email,
		browserId
    }:{
        email?: string;
		browserId?: string;     
    } = {}
    ) {
        super('Login'); 

        this.email = email;
		this.browserId = browserId;
    }
}


export class LoginVerificationToken extends BaseEntity
{
    email?: string;
	userId?: number;
	browserId?: string;
	expireAt?: Date;

    constructor(
    {
        email,
		userId,
		browserId,
		expireAt
    }:{
        email?: string;
		userId?: number;
		browserId?: string;
		expireAt?: Date;     
    } = {}
    ) {
        super('LoginVerificationToken'); 

        this.email = email;
		this.userId = userId;
		this.browserId = browserId;
		this.expireAt = expireAt;
    }
}


export class RefreshToken extends BaseEntity
{
    email?: string;
	ipAddress?: string;
	browserId?: string;
	tokenString?: string;
	expireAt?: Date;

    constructor(
    {
        email,
		ipAddress,
		browserId,
		tokenString,
		expireAt
    }:{
        email?: string;
		ipAddress?: string;
		browserId?: string;
		tokenString?: string;
		expireAt?: Date;     
    } = {}
    ) {
        super('RefreshToken'); 

        this.email = email;
		this.ipAddress = ipAddress;
		this.browserId = browserId;
		this.tokenString = tokenString;
		this.expireAt = expireAt;
    }
}


export class RefreshTokenRequest extends BaseEntity
{
    refreshToken?: string;
	browserId?: string;

    constructor(
    {
        refreshToken,
		browserId
    }:{
        refreshToken?: string;
		browserId?: string;     
    } = {}
    ) {
        super('RefreshTokenRequest'); 

        this.refreshToken = refreshToken;
		this.browserId = browserId;
    }
}


export class Registration extends BaseEntity
{
    email?: string;
	browserId?: string;

    constructor(
    {
        email,
		browserId
    }:{
        email?: string;
		browserId?: string;     
    } = {}
    ) {
        super('Registration'); 

        this.email = email;
		this.browserId = browserId;
    }
}


export class RegistrationVerificationResult extends BaseEntity
{
    status?: RegistrationVerificationResultStatusCodes;
	message?: string;

    constructor(
    {
        status,
		message
    }:{
        status?: RegistrationVerificationResultStatusCodes;
		message?: string;     
    } = {}
    ) {
        super('RegistrationVerificationResult'); 

        this.status = status;
		this.message = message;
    }
}


export class RegistrationVerificationToken extends BaseEntity
{
    email?: string;
	browserId?: string;
	expireAt?: Date;

    constructor(
    {
        email,
		browserId,
		expireAt
    }:{
        email?: string;
		browserId?: string;
		expireAt?: Date;     
    } = {}
    ) {
        super('RegistrationVerificationToken'); 

        this.email = email;
		this.browserId = browserId;
		this.expireAt = expireAt;
    }
}


export class RoleSaveBody extends BaseEntity
{
    selectedPermissionIds?: number[];
	selectedUserIds?: number[];
	roleDTO?: Role;

    constructor(
    {
        selectedPermissionIds,
		selectedUserIds,
		roleDTO
    }:{
        selectedPermissionIds?: number[];
		selectedUserIds?: number[];
		roleDTO?: Role;     
    } = {}
    ) {
        super('RoleSaveBody'); 

        this.selectedPermissionIds = selectedPermissionIds;
		this.selectedUserIds = selectedUserIds;
		this.roleDTO = roleDTO;
    }
}


export class VerificationTokenRequest extends BaseEntity
{
    verificationCode?: string;
	browserId?: string;
	email?: string;

    constructor(
    {
        verificationCode,
		browserId,
		email
    }:{
        verificationCode?: string;
		browserId?: string;
		email?: string;     
    } = {}
    ) {
        super('VerificationTokenRequest'); 

        this.verificationCode = verificationCode;
		this.browserId = browserId;
		this.email = email;
    }
}


export class Permission extends BaseEntity
{
    name?: string;
	nameLatin?: string;
	description?: string;
	descriptionLatin?: string;
	code?: string;
	id?: number;

    constructor(
    {
        name,
		nameLatin,
		description,
		descriptionLatin,
		code,
		id
    }:{
        name?: string;
		nameLatin?: string;
		description?: string;
		descriptionLatin?: string;
		code?: string;
		id?: number;     
    } = {}
    ) {
        super('Permission'); 

        this.name = name;
		this.nameLatin = nameLatin;
		this.description = description;
		this.descriptionLatin = descriptionLatin;
		this.code = code;
		this.id = id;
    }
}


export class PermissionSaveBody extends BaseEntity
{
    permissionDTO?: Permission;

    constructor(
    {
        permissionDTO
    }:{
        permissionDTO?: Permission;     
    } = {}
    ) {
        super('PermissionSaveBody'); 

        this.permissionDTO = permissionDTO;
    }
}


export class Role extends BaseEntity
{
    name?: string;
	description?: string;
	version?: number;
	id?: number;
	createdAt?: Date;
	modifiedAt?: Date;

    constructor(
    {
        name,
		description,
		version,
		id,
		createdAt,
		modifiedAt
    }:{
        name?: string;
		description?: string;
		version?: number;
		id?: number;
		createdAt?: Date;
		modifiedAt?: Date;     
    } = {}
    ) {
        super('Role'); 

        this.name = name;
		this.description = description;
		this.version = version;
		this.id = id;
		this.createdAt = createdAt;
		this.modifiedAt = modifiedAt;
    }
}


export class RolePermission extends BaseEntity
{
    roleDisplayName?: string;
	roleId?: number;
	permissionDisplayName?: string;
	permissionId?: number;

    constructor(
    {
        roleDisplayName,
		roleId,
		permissionDisplayName,
		permissionId
    }:{
        roleDisplayName?: string;
		roleId?: number;
		permissionDisplayName?: string;
		permissionId?: number;     
    } = {}
    ) {
        super('RolePermission'); 

        this.roleDisplayName = roleDisplayName;
		this.roleId = roleId;
		this.permissionDisplayName = permissionDisplayName;
		this.permissionId = permissionId;
    }
}


export class RolePermissionSaveBody extends BaseEntity
{
    rolePermissionDTO?: RolePermission;

    constructor(
    {
        rolePermissionDTO
    }:{
        rolePermissionDTO?: RolePermission;     
    } = {}
    ) {
        super('RolePermissionSaveBody'); 

        this.rolePermissionDTO = rolePermissionDTO;
    }
}


export class UserRole extends BaseEntity
{
    roleId?: number;
	userId?: number;

    constructor(
    {
        roleId,
		userId
    }:{
        roleId?: number;
		userId?: number;     
    } = {}
    ) {
        super('UserRole'); 

        this.roleId = roleId;
		this.userId = userId;
    }
}


export class UserRoleSaveBody extends BaseEntity
{
    userRoleDTO?: UserRole;

    constructor(
    {
        userRoleDTO
    }:{
        userRoleDTO?: UserRole;     
    } = {}
    ) {
        super('UserRoleSaveBody'); 

        this.userRoleDTO = userRoleDTO;
    }
}

