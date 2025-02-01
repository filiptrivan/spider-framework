import { Injectable, OnDestroy } from '@angular/core';
import { Router } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, firstValueFrom, Observable, of, Subject, Subscription } from 'rxjs';
import { map, tap, delay, finalize } from 'rxjs/operators';
import { SocialUser, SocialAuthService } from '@abacritt/angularx-social-login';
import { ExternalProvider, Login, VerificationTokenRequest, AuthResult, Registration, RegistrationVerificationResult, RefreshTokenRequest, User } from '../entities/security-entities';
import { ConfigBaseService } from './config-base.service';
import { ApiSecurityService } from './api.service.security';
import { InitCompanyAuthDialogDetails } from '../entities/init-company-auth-dialog-details';

@Injectable({
  providedIn: 'root'
})
export class AuthBaseService implements OnDestroy {
  private readonly apiUrl = this.config.apiUrl;
  private timer?: Subscription;

  protected _currentUserPermissions = new BehaviorSubject<string[] | null>(null);
  currentUserPermissions$ = this._currentUserPermissions.asObservable();

  protected _user = new BehaviorSubject<User | null>(null);
  user$ = this._user.asObservable();

  // Google auth
  private authChangeSub = new Subject<boolean>();
  private extAuthChangeSub = new Subject<SocialUser>();
  public authChanged = this.authChangeSub.asObservable();
  public extAuthChanged = this.extAuthChangeSub.asObservable();
  
  constructor(
    protected router: Router,
    protected http: HttpClient,
    protected externalAuthService: SocialAuthService,
    protected apiService: ApiSecurityService,
    protected config: ConfigBaseService,
  ) {
    window.addEventListener('storage', this.storageEventListener.bind(this));

    // Google auth
    this.externalAuthService.authState.subscribe((user) => {
      const externalAuth: ExternalProvider = {
        // provider: user.provider,
        idToken: user.idToken
      }
      this.loginExternal(externalAuth).subscribe(() => {
        this.navigateToDashboard();
      });
      this.extAuthChangeSub.next(user);
    });
  }

  private storageEventListener(event: StorageEvent) {
    if (event.storageArea === localStorage) {
      if (event.key === 'logout-event') {
        this.stopTokenTimer();
        this._user.next(null);

        this.onAfterLogoutEvent();
      }
      if (event.key === 'login-event') {
        this.stopTokenTimer();

        this.apiService.getCurrentUser().subscribe(async (user: User) => {
            this._user.next({
              id: user.id,
              email: user.email
            });

            this.onAfterLoginEvent();
          });
      }
    }
  }

  onAfterLogoutEvent = () => {
    this._currentUserPermissions.next(null);
  }

  onAfterLoginEvent = async () => {
    await firstValueFrom(this.getCurrentUserPermissionCodes()); // FT: Needs to be after setting local storage
  }
    
  sendLoginVerificationEmail(body: Login): Observable<any> {
    const browserId = this.getBrowserId();
    body.browserId = browserId;
    return this.apiService.sendLoginVerificationEmail(body);
  }

  login(body: VerificationTokenRequest): Observable<Promise<AuthResult>> {
    const browserId = this.getBrowserId();
    body.browserId = browserId;
    const loginResultObservable = this.http.post<AuthResult>(`${this.apiUrl}/Security/Login`, body);
    return this.handleLoginResult(loginResultObservable);
  }

  loginExternal(body: ExternalProvider): Observable<Promise<AuthResult>> {
    const browserId = this.getBrowserId();
    body.browserId = browserId;
    const loginResultObservable = this.http.post<AuthResult>(`${this.apiUrl}/Security/LoginExternal`, body);
    return this.handleLoginResult(loginResultObservable);
  }

  sendRegistrationVerificationEmail(body: Registration): Observable<RegistrationVerificationResult> {
    const browserId = this.getBrowserId();
    body.browserId = browserId;
    return this.apiService.sendRegistrationVerificationEmail(body);
  }
  
  register(body: VerificationTokenRequest): Observable<Promise<AuthResult>> {
    const browserId = this.getBrowserId();
    body.browserId = browserId;
    const loginResultObservable = this.apiService.register(body);
    return this.handleLoginResult(loginResultObservable);
  }

  handleLoginResult(loginResultObservable: Observable<AuthResult>){
    return loginResultObservable.pipe(
      map(async (loginResult: AuthResult) => {
        this._user.next({
          id: loginResult.userId,
          email: loginResult.email,
        });
        this.setLocalStorage(loginResult);
        this.startTokenTimer();
        this.onAfterHandledLoginResult();
        return loginResult;
      })
    );
  }

  onAfterHandledLoginResult = async () => {
    await firstValueFrom(this.getCurrentUserPermissionCodes()); // FT: Needs to be after setting local storage
  }

  logout() {
    const browserId = this.getBrowserId();
    this.http
      .get(`${this.apiUrl}/Security/Logout?browserId=${browserId}`)
      .pipe(
        finalize(() => {
          this.clearLocalStorage();
          this._user.next(null);
          this.onAfterLogout();
          this.stopTokenTimer();
          this.router.navigate([this.config.loginSlug]);
        })
      )
      .subscribe();
  }

  onAfterLogout = () => {
    this._currentUserPermissions.next(null);
  }

  refreshToken(): Observable<Promise<AuthResult> | null> {
    let refreshToken = localStorage.getItem(this.config.refreshTokenKey);

    if (!refreshToken) {
      this.clearLocalStorage();
      return of(null);
    }

    const browserId = this.getBrowserId();
    let body: RefreshTokenRequest = new RefreshTokenRequest();
    body.browserId = browserId;
    body.refreshToken = refreshToken;
    return this.http
    .post<AuthResult>(`${this.apiUrl}/Security/RefreshToken`, body, this.config.httpSkipSpinnerOptions)
    .pipe(
      map(async (loginResult) => {
        this._user.next({
          id: loginResult.userId,
          email: loginResult.email
        });
        
        this.setLocalStorage(loginResult);
        this.startTokenTimer();
        this.onAfterRefreshToken();
        
        return loginResult;
      })
    );
  }

  onAfterRefreshToken = async () => {
    await firstValueFrom(this.getCurrentUserPermissionCodes()); // FT: Needs to be after setting local storage
  }

  setLocalStorage(loginResult: AuthResult) {
    localStorage.setItem(this.config.accessTokenKey, loginResult.accessToken);
    localStorage.setItem(this.config.refreshTokenKey, loginResult.refreshToken);
    localStorage.setItem('login-event', 'login' + Math.random());
  }

  clearLocalStorage() {
    localStorage.removeItem(this.config.accessTokenKey);
    localStorage.removeItem(this.config.refreshTokenKey);
    localStorage.setItem('logout-event', 'logout' + Math.random());
  }

  getBrowserId(): string {
    let browserId = localStorage.getItem(this.config.browserIdKey); // FT: We don't need to remove this from the local storage ever, only if the user manuely deletes it, we will handle it
    if (!browserId) {
      browserId = crypto.randomUUID();
      localStorage.setItem(this.config.browserIdKey, browserId);
    }
    return browserId;
  }

  isAccessTokenExpired(): Observable<boolean> {
    const expired = this.getTokenRemainingTime() < 5000;
    
    return of(expired);
  }

  getTokenRemainingTime(): number {
    const accessToken = this.getAccessToken();

    if (!accessToken) {
      return 0;
    }

    const jwtToken = JSON.parse(atob(accessToken.split('.')[1]));
    const expires = new Date(jwtToken.exp * 1000);
    
    return expires.getTime() - Date.now();
  }

  getAccessToken(): string {
    return localStorage.getItem(this.config.accessTokenKey);
  }

  private startTokenTimer() {
    const timeout = this.getTokenRemainingTime();
    this.timer = of(true)
      .pipe(
        delay(timeout),
        tap({
          next: () => this.refreshToken().subscribe(),
        })
      )
      .subscribe();
  }

  private stopTokenTimer() {
    this.timer?.unsubscribe();
  }

  navigateToDashboard(){
    this.router.navigate(['/']);
  }

  logoutGoogle = () => {
    this.externalAuthService.signOut();
  }

  initCompanyAuthDialogDetails = (): Observable<InitCompanyAuthDialogDetails> => {
    return of(
      new InitCompanyAuthDialogDetails ({
        image: `assets/demo/images/logo/logo-dark.svg`, 
        companyName: this.config.companyName,
      })
    );
  }

  getCurrentUserPermissionCodes(): Observable<string[]> {
    return this.apiService.getCurrentUserPermissionCodes().pipe(
      map(permissionCodes => {
        this._currentUserPermissions.next(permissionCodes);
        return permissionCodes;
      }
    ));
  }

  ngOnDestroy(): void {
    window.removeEventListener('storage', this.storageEventListener.bind(this));
    this.onAfterNgOnDestroy();
  }

  onAfterNgOnDestroy = () => {};
}
