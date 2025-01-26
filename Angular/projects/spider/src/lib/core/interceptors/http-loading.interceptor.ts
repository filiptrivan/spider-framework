import { HttpInterceptor, HttpRequest, HttpHandler, HttpEvent } from "@angular/common/http";
import { Injectable } from "@angular/core";
import { NgxSpinnerService } from "ngx-spinner";
import { Observable, finalize } from "rxjs";

@Injectable({
  providedIn: 'root'
})
export class HttpLoadingInterceptor implements HttpInterceptor {
  constructor(private spinner: NgxSpinnerService) {}

  intercept(
    request: HttpRequest<any>,
    next: HttpHandler
  ): Observable<HttpEvent<any>> {
    const skipSpinner = request.params.has('X-Skip-Spinner'); // FT: Using this for multiautocomplete, autocomplete, dropdown, table etc...

    if (!skipSpinner)
      this.spinner.show();

    return next.handle(request).pipe(
      finalize(() => {
        if (!skipSpinner)
          this.spinner.hide();
      })
    ) as Observable<HttpEvent<any>>;
  }
}