import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { Namebook } from '../entities/namebook';
import { TableFilter } from '../entities/table-filter';
import { Login, Registration, RegistrationVerificationResult, RefreshTokenRequest, AuthResult, Role, User, ExternalProvider, VerificationTokenRequest, RoleSaveBody, RoleMainUIForm } from '../entities/security-entities';
import { ConfigBaseService } from './config-base.service';
import { TableResponse } from '../entities/table-response';

@Injectable({
    providedIn: 'root'
})
export class ApiSecurityService {

    constructor(
        protected http: HttpClient,
        protected config: ConfigBaseService
    ) {
        
    }

    //#region Authentication

    register = (request: VerificationTokenRequest): Observable<AuthResult> => { 
        return this.http.post<AuthResult>(`${this.config.apiUrl}/Security/Register`, request, this.config.httpOptions);
    }

    sendRegistrationVerificationEmail = (registrationDTO: Registration): Observable<RegistrationVerificationResult> => { 
        return this.http.post<RegistrationVerificationResult>(`${this.config.apiUrl}/Security/SendRegistrationVerificationEmail`, registrationDTO, this.config.httpOptions);
    }
    
    login = (request: VerificationTokenRequest): Observable<AuthResult> => { 
        return this.http.post<AuthResult>(`${this.config.apiUrl}/Security/Login`, request, this.config.httpOptions);
    }

    loginExternal = (externalProviderDTO: ExternalProvider): Observable<AuthResult> => { 
        return this.http.post<AuthResult>(`${this.config.apiUrl}/Security/LoginExternal`, externalProviderDTO, this.config.httpOptions);
    }

    sendLoginVerificationEmail = (loginDTO: Login): Observable<any> => { 
        return this.http.post<any>(`${this.config.apiUrl}/Security/SendLoginVerificationEmail`, loginDTO, this.config.httpOptions);
    }


    logout = (browserId: string): Observable<any> => { 
        return this.http.get<any>(`${this.config.apiUrl}/Security/Logout?browserId=${browserId}`);
    }

    refreshToken = (request: RefreshTokenRequest): Observable<AuthResult> => { 
        return this.http.post<AuthResult>(`${this.config.apiUrl}/Security/RefreshToken`, request, this.config.httpOptions);
    }

    //#endregion

    //#region User

    getCurrentUser = (): Observable<User> => { 
        return this.http.get<User>(`${this.config.apiUrl}/Security/GetCurrentUser`, this.config.httpSkipSpinnerOptions);
    }

    getCurrentUserPermissionCodes = (): Observable<string[]> => { 
        return this.http.get<string[]>(`${this.config.apiUrl}/Security/GetCurrentUserPermissionCodes`, this.config.httpSkipSpinnerOptions);
    }

    //#endregion

    //#region Role

    getRoleTableData = (dto: TableFilter): Observable<TableResponse> => { 
        return this.http.post<TableResponse>(`${this.config.apiUrl}/Security/GetRoleTableData`, dto, this.config.httpSkipSpinnerOptions);
    }

    exportRoleTableDataToExcel = (dto: TableFilter): Observable<any> => { 
        return this.http.post<any>(`${this.config.apiUrl}/Security/ExportRoleTableDataToExcel`, dto, this.config.httpOptions);
    }

    deleteRole = (id: number): Observable<any> => { 
        return this.http.delete<any>(`${this.config.apiUrl}/Security/DeleteRole?id=${id}`);
    }

    getRoleMainUIFormDTO = (id: number): Observable<RoleMainUIForm> => {
        return this.http.get<RoleMainUIForm>(`${this.config.apiUrl}/Security/GetRoleMainUIFormDTO?id=${id}`);
    }

    getRole = (id: number): Observable<Role> => {
        return this.http.get<Role>(`${this.config.apiUrl}/Security/GetRole?id=${id}`);
    }

    saveRole = (dto: RoleSaveBody): Observable<Role> => { 
        return this.http.put<Role>(`${this.config.apiUrl}/Security/SaveRole`, dto, this.config.httpOptions);
    }

    getUsersNamebookListForRole = (roleId: number): Observable<Namebook[]> => {
        return this.http.get<Namebook[]>(`${this.config.apiUrl}/Security/GetUsersNamebookListForRole?roleId=${roleId}`, this.config.httpSkipSpinnerOptions);
    }

    getPermissionsDropdownListForRole = (): Observable<Namebook[]> => {
        return this.http.get<Namebook[]>(`${this.config.apiUrl}/Security/GetPermissionsDropdownListForRole`, this.config.httpSkipSpinnerOptions);
    }

    getPermissionsNamebookListForRole = (roleId: number): Observable<Namebook[]> => {
        return this.http.get<Namebook[]>(`${this.config.apiUrl}/Security/GetPermissionsNamebookListForRole?roleId=${roleId}`, this.config.httpSkipSpinnerOptions);
    }

    getUsersAutocompleteListForRole = (limit: number, query: string): Observable<Namebook[]> => {
        return this.http.get<Namebook[]>(`${this.config.apiUrl}/Security/GetUsersAutocompleteListForRole?limit=${limit}&query=${query}`, this.config.httpSkipSpinnerOptions);
    }

    //#endregion

    //#region Notification

    getUnreadNotificationsCountForCurrentUser = (): Observable<number> => { 
        return this.http.get<number>(`${this.config.apiUrl}/Notification/GetUnreadNotificationsCountForCurrentUser`, this.config.httpOptions);
    }

    //#endregion

}

