import { Injectable } from '@angular/core';
import { CanActivate, Router } from '@angular/router';
import { Observable, map } from 'rxjs';
import { AuthBaseService } from '../services/auth-base.service';
import { ConfigBaseService } from '../services/config-base.service';

@Injectable({
  providedIn: 'root',
})
export class AuthGuard implements CanActivate {
  constructor(
    private authService: AuthBaseService, 
    private router: Router,
    private config: ConfigBaseService
  ) {

  }

  canActivate(): Observable<boolean> {
    return this.checkAuth();
  }

  private checkAuth(): Observable<boolean> {
    return this.authService.user$.pipe(
      map((user) => {
        if (user) {
          return true;
        } else {
          // const returnUrl = this.router.getCurrentNavigation()?.extractedUrl.toString() || '/';
          // this.router.navigate(['auth/login'], {
          //    queryParams: { returnUrl },
          // });
          this.router.navigate([this.config.loginSlug]);
          return false;
        }
      })
    );
  }
}
