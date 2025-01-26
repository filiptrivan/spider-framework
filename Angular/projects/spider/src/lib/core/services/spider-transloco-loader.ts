import { Injectable } from '@angular/core';
import { TranslocoLoader } from '@jsverse/transloco';
import { HttpClient } from '@angular/common/http';

@Injectable({ 
  providedIn: 'root' 
})
export class SpiderTranslocoLoader implements TranslocoLoader {
  constructor(private http: HttpClient) {}

  getTranslation(lang: string) {
    return this.http.get(`./assets/i18n/${lang}.json`);
  }

}