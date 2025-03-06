import { HttpHeaders, HttpParams } from "@angular/common/http";
import { Injectable } from "@angular/core";

@Injectable({
    providedIn: 'root'
})
export class ConfigBaseService
{
    production = false;
    apiUrl: string;
    frontendUrl = 'http://localhost:4200';
    googleClientId: string;
    companyName = 'Company Name';
    primaryColor = '#111b2c';
    
    googleAuth = true;
    usersCanRegister = true;

    /* URLs */
    loginSlug = 'login';

    /* Local storage */
    accessTokenKey = 'access_token';
    refreshTokenKey = 'refresh_token';
    browserIdKey = 'browser_id';

    httpOptions = {};
    httpSkipSpinnerOptions = {
        headers: new HttpHeaders({ 'Content-Type': 'application/json' }),
        params: new HttpParams().set('X-Skip-Spinner', 'true')
    };

    constructor(
    ) {
    }
}