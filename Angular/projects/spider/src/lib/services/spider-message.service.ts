import { TranslocoService } from '@jsverse/transloco';
import { Injectable, NgZone } from '@angular/core';
import { MessageService } from 'primeng/api';

@Injectable({
  providedIn: 'root',
})
export class SpiderMessageService { // TODO FT: nece da prikaze poruku ako je neki angular error koji se dogodi tek nakon api poziva
  constructor(
    private messageService: MessageService, 
    private translocoService: TranslocoService,
    private ngZone: NgZone
  ) {
    
  }

  successMessage(detail: string, title: string = this.translocoService.translate('SuccessfulAction')) {
    this.messageService.add({
      severity: 'success',
      summary: title,
      detail: detail,
      life: 10000,
    });
  }

  warningMessage(detail: string, title?: string, sticky?: boolean){
    this.messageService.add({
      severity: 'warn',
      summary: title ?? this.translocoService.translate('Warning'),
      detail: detail,
      life: 10000,
      sticky: sticky
    });
  }

  warningMessageWithTimeout(detail: string, title: string = this.translocoService.translate('Warning')){
    setTimeout(() => {
      this.messageService.add({
        severity: 'warn',
        summary: title,
        detail: detail,
        life: 10000,
      });
    }, 100);
  }

  errorMessage(detail: string, title: string = this.translocoService.translate('Error')){
    this.messageService.add({
      severity: 'error',
      summary: title,
      detail: detail,
      life: 10000,
    });
  }

  infoMessage(detail: string, title?: string, sticky?: boolean){
    this.messageService.add({
      severity: 'info',
      summary: title ?? this.translocoService.translate('Info'),
      detail: detail,
      life: 10000,
      sticky: sticky,
    });
  }
  
}
