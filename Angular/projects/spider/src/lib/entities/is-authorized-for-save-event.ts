import { BaseEntity } from "./base-entity";

export class IsAuthorizedForSaveEvent extends BaseEntity
{
    isAuthorizedForSave?: boolean;
    currentUserPermissionCodes?: string[];

    constructor(
    {
        isAuthorizedForSave,
        currentUserPermissionCodes,
    }:{
        isAuthorizedForSave?: boolean;
        currentUserPermissionCodes?: string[];
    } = {}
    ) {
        super('IsAuthorizedForSaveEvent'); 

        this.isAuthorizedForSave = isAuthorizedForSave;
        this.currentUserPermissionCodes = currentUserPermissionCodes;
    }
}