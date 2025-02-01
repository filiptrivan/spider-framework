import { BrowserAnimationsModule } from '@angular/platform-browser/animations';
import { NgModule, APP_INITIALIZER, Optional, SkipSelf, ErrorHandler } from '@angular/core';
import { HTTP_INTERCEPTORS, HttpClientModule } from '@angular/common/http';
import { appInitializer } from '../services/app-initializer';
import { JwtInterceptor } from '../interceptors/jwt.interceptor';
import { UnauthorizedInterceptor } from '../interceptors/unauthorized.interceptor';
import { HttpLoadingInterceptor } from '../interceptors/http-loading.interceptor';
import { JsonHttpInterceptor } from '../interceptors/json-parser.interceptor';
import { AuthBaseService } from '../services/auth-base.service';
import { SpiderErrorHandler } from '../handlers/spider-error-handler';
import { MessageService } from 'primeng/api';
import { CommonModule } from '@angular/common';
import { BrowserModule } from '@angular/platform-browser';
import { ToastModule } from 'primeng/toast';

@NgModule({
  declarations: [],
  imports: [
    BrowserModule,
    CommonModule,
    HttpClientModule,
    BrowserAnimationsModule,
    ToastModule,
  ],
  providers: [
    MessageService,
    {
      provide: APP_INITIALIZER,
      useFactory: appInitializer,
      multi: true,
      deps: [AuthBaseService],
    },
    { 
      provide: HTTP_INTERCEPTORS,
      useClass: JwtInterceptor,
      multi: true 
    },
    {
      provide: HTTP_INTERCEPTORS,
      useClass: UnauthorizedInterceptor,
      multi: true,
    },
    {
      provide: HTTP_INTERCEPTORS,
      useClass: HttpLoadingInterceptor,
      multi: true,
    },
    { 
      provide: HTTP_INTERCEPTORS,
      useClass: JsonHttpInterceptor,
      multi: true
    },
    {
      provide: ErrorHandler,
      useClass: SpiderErrorHandler,
    },
  ],
  exports: [
    BrowserModule,
    CommonModule,
    HttpClientModule,
    BrowserAnimationsModule,
    ToastModule,
  ] 
})
export class CoreModule {
  constructor(@Optional() @SkipSelf() core: CoreModule) {
    if (core) {
      throw new Error('Core Module can only be imported to AppModule.');
    }
  }
}