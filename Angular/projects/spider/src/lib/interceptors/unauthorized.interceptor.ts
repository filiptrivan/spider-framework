import { Injectable } from '@angular/core';
import {
  HttpRequest,
  HttpHandler,
  HttpEvent,
  HttpInterceptor,
  HttpErrorResponse,
} from '@angular/common/http';
import { Observable, of, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { SpiderMessageService } from '../services/spider-message.service';
import { TranslocoService } from '@jsverse/transloco';
import { ConfigBaseService } from '../services/config-base.service';

@Injectable({
  providedIn: 'root',
})
export class UnauthorizedInterceptor implements HttpInterceptor {
  constructor(
    private messageService: SpiderMessageService,
    private translocoService: TranslocoService,
    private config: ConfigBaseService,
  ) {}

  intercept(
    request: HttpRequest<any>,
    next: HttpHandler
  ): Observable<HttpEvent<any>> {
    return next.handle(request).pipe(
      catchError((err) => {
        return this.handleAuthError(err, request);
      })
    );
  }

  private handleAuthError(err: HttpErrorResponse, request: HttpRequest<any>): Observable<any> {

    if (!this.config.production) {
      console.error(err);
    }

    let errorResponse = err.error;

    if (request.responseType != 'json')
      errorResponse= JSON.parse(err.error);

    if (err.status == 0) {
      this.messageService.warningMessageWithTimeout( // FT: Had problem when the server is shut down, and try to refresh token, warning message didn't appear
        this.translocoService.translate('ServerLostConnectionDetails'),
        this.translocoService.translate('ServerLostConnectionTitle'),
      );
      return of(err.message);
    } 
    else if (err.status == 403) {
      this.messageService.warningMessage(
        this.translocoService.translate('PermissionErrorDetails'),
        this.translocoService.translate('PermissionErrorTitle'),
      );
      return of(err.message);
    } 
    else if (err.status == 404) {
      this.messageService.warningMessage(
        this.translocoService.translate('NotFoundDetails'),
        this.translocoService.translate('NotFoundTitle'),
      );
      return of(err.message);
    } 
    else if (err.status == 400 || err.status == 401) {
      this.messageService.warningMessage(
        errorResponse.message ?? this.translocoService.translate('BadRequestDetails'),
        this.translocoService.translate('Warning'),
      );

      if(err.status == 401) {
        // this.authService.logout();
      }

      return of(err.message);
    } 
    else if (err.status == 419) { // FT: We don't want to show error message to the user, we just log him out.
      return of(err.message);
    } 
    else {
      this.messageService.errorMessage(
        errorResponse.message,
        this.translocoService.translate('UnexpectedErrorTitle'),
      );

      return of(err.message);
    }

    return throwError(err);
  }

}
