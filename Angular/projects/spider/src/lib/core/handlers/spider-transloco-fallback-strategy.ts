import { Injectable } from '@angular/core';
import { TranslocoFallbackStrategy, TranslocoService } from '@jsverse/transloco';

import { HashMap, TranslocoMissingHandler, TranslocoMissingHandlerData } from "@jsverse/transloco";

@Injectable({
    providedIn: 'root'
})
export class SpiderTranslocoFallbackStrategy implements TranslocoFallbackStrategy {
  getNextLangs(failedLang: string): string[] {
    return ['sr-Latn-RS.generated'];
  }

}

export class SpiderTranslocoMissingHandler implements TranslocoMissingHandler {
  constructor(private translocoService: TranslocoService) {
    
  }

  handle(key: string, data: TranslocoMissingHandlerData, params?: HashMap) {
      return this.translocoService.translate(key, params, `${this.translocoService.getActiveLang()}.generated`);
  }

}
