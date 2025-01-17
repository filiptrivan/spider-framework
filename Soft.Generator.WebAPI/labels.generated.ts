import { Injectable } from '@angular/core';
import { TranslocoService } from '@jsverse/transloco';

@Injectable({
  providedIn: 'root',
})
export class TranslateLabelsGeneratedService {

    constructor(
        private translocoService: TranslocoService
    ) {
    }

    translate(name: string): string
    {
        switch(name) 
        {
            case 'user':
                return this.translocoService.translate('User');
            case 'email':
                return this.translocoService.translate('Email');
            case 'accessToken':
                return this.translocoService.translate('AccessToken');
            case 'refreshToken':
                return this.translocoService.translate('RefreshToken');
            case 'id':
                return this.translocoService.translate('Id');
            case 'version':
                return this.translocoService.translate('Version');
            case 'createdAt':
                return this.translocoService.translate('CreatedAt');
            case 'modifiedAt':
                return this.translocoService.translate('ModifiedAt');
            case 'code':
                return this.translocoService.translate('Code');
            case 'displayName':
                return this.translocoService.translate('DisplayName');
            case 'additionalColumnHeaders':
                return this.translocoService.translate('AdditionalColumnHeaders');
            case 'additionalDataStartColumn':
                return this.translocoService.translate('AdditionalDataStartColumn');
            case 'dataSheetName':
                return this.translocoService.translate('DataSheetName');
            case 'dataSheetName2':
                return this.translocoService.translate('DataSheetName2');
            case 'dataStartRow':
                return this.translocoService.translate('DataStartRow');
            case 'dataStartColumn':
                return this.translocoService.translate('DataStartColumn');
            case 'createNewDataRows':
                return this.translocoService.translate('CreateNewDataRows');
            case 'idToken':
                return this.translocoService.translate('IdToken');
            case 'browser':
                return this.translocoService.translate('Browser');
            case 'userEmail':
                return this.translocoService.translate('UserEmail');
            case 'token':
                return this.translocoService.translate('Token');
            case 'selectedIds':
                return this.translocoService.translate('SelectedIds');
            case 'totalRecordsSelected':
                return this.translocoService.translate('TotalRecordsSelected');
            case 'expireAt':
                return this.translocoService.translate('ExpireAt');
            case 'totalRecords':
                return this.translocoService.translate('TotalRecords');
            case 'query':
                return this.translocoService.translate('Query');
            case 'name':
                return this.translocoService.translate('Name');
            case 'nameLatin':
                return this.translocoService.translate('NameLatin');
            case 'description':
                return this.translocoService.translate('Description');
            case 'descriptionLatin':
                return this.translocoService.translate('DescriptionLatin');
            case 'permissionDTO':
                return this.translocoService.translate('PermissionDTO');
            case 'ipAddress':
                return this.translocoService.translate('IpAddress');
            case 'tokenString':
                return this.translocoService.translate('TokenString');
            case 'status':
                return this.translocoService.translate('Status');
            case 'message':
                return this.translocoService.translate('Message');
            case 'role':
                return this.translocoService.translate('Role');
            case 'permission':
                return this.translocoService.translate('Permission');
            case 'rolePermissionDTO':
                return this.translocoService.translate('RolePermissionDTO');
            case 'selectedPermissionIds':
                return this.translocoService.translate('SelectedPermissionIds');
            case 'selectedUserIds':
                return this.translocoService.translate('SelectedUserIds');
            case 'roleDTO':
                return this.translocoService.translate('RoleDTO');
            case 'value':
                return this.translocoService.translate('Value');
            case 'matchMode':
                return this.translocoService.translate('MatchMode');
            case 'operator':
                return this.translocoService.translate('Operator');
            case 'filters':
                return this.translocoService.translate('Filters');
            case 'first':
                return this.translocoService.translate('First');
            case 'rows':
                return this.translocoService.translate('Rows');
            case 'sortField':
                return this.translocoService.translate('SortField');
            case 'sortOrder':
                return this.translocoService.translate('SortOrder');
            case 'multiSortMeta':
                return this.translocoService.translate('MultiSortMeta');
            case 'additionalFilterIdInt':
                return this.translocoService.translate('AdditionalFilterIdInt');
            case 'additionalFilterIdLong':
                return this.translocoService.translate('AdditionalFilterIdLong');
            case 'field':
                return this.translocoService.translate('Field');
            case 'order':
                return this.translocoService.translate('Order');
            case 'data':
                return this.translocoService.translate('Data');
            case 'userRoleDTO':
                return this.translocoService.translate('UserRoleDTO');
            case 'verificationCode':
                return this.translocoService.translate('VerificationCode');
            default:
                return null;
        }
    }
}

