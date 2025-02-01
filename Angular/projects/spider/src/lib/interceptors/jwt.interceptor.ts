import { Injectable } from '@angular/core';
import {
  HttpRequest,
  HttpHandler,
  HttpEvent,
  HttpInterceptor,
} from '@angular/common/http';
import { Observable } from 'rxjs';
import { ConfigBaseService } from '../services/config-base.service';

@Injectable({
    providedIn: 'root'
})
export class JwtInterceptor implements HttpInterceptor {

  constructor(
    private config: ConfigBaseService
  ) {
    
  }

  intercept(
    request: HttpRequest<unknown>,
    next: HttpHandler
  ): Observable<HttpEvent<unknown>> {
    const accessToken = localStorage.getItem('access_token');
    const isApiUrl = request.url.startsWith(this.config.apiUrl);
    if (accessToken && isApiUrl) {
      request = request.clone({
        setHeaders: { Authorization: `Bearer ${accessToken}` },
      });
    }

    return next.handle(request);
  }
}