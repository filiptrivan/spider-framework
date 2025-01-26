import { Injectable } from '@angular/core';
import { HttpInterceptor, HttpRequest, HttpHandler, HttpEvent, HttpResponse } from '@angular/common/http';
import { Observable, tap } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class JsonHttpInterceptor implements HttpInterceptor {
  private dateRegex = /^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}/;

  constructor() { }

  intercept(request: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
    return next.handle(request)
      .pipe(
        tap((event: HttpEvent<any>) => {
          if (event instanceof HttpResponse) {
            this.convertDates(event.body);
          }
        }
    ));
  }

  private convertDates(
    object: unknown,
    parent?: Record<string, unknown> | unknown[],
    key?: number | string,
  ) {
    if (object === null) return;

    if (typeof object === 'string') {
      if (this.dateRegex.test(object)) {
        /**
         * @see https://stackoverflow.com/a/54733846/1306679
         */

        parent[key] = new Date(object);
      }
    } else if (Array.isArray(object)) {
      for (let i = 0; i < object.length; i++)
        this.convertDates(object[i], object, i);
    } else {
      for (const key of Object.keys(object as Record<string, unknown>)) {
        this.convertDates(
          (object as Record<string, unknown>)[key],
          object as Record<string, unknown>,
          key,
        );
      }
    }
  }

}