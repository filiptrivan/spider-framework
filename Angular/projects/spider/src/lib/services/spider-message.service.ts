import { TranslocoService } from '@jsverse/transloco';
import { Injectable } from '@angular/core';
import { MessageService } from 'primeng/api';

@Injectable({
  providedIn: 'root',
})
export class SpiderMessageService { // TODO FT: nece da prikaze poruku ako je neki angular error koji se dogodi tek nakon api poziva
  constructor(
    private messageService: MessageService, 
    private translocoService: TranslocoService, 
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

  warningMessage(detail: string, title: string = this.translocoService.translate('Warning')){
    this.messageService.add({
      severity: 'warn',
      summary: title,
      detail: detail,
      life: 10000,
    });
  }

  // FT HACK: Only for the http status 419
  warning419Message(detail: string, title: string = this.translocoService.translate('Warning')){
    setTimeout(() => {
      this.messageService.add({
        severity: 'warn',
        summary: title,
        detail: detail,
        life: 10000,
      });
    }, 500);
  }

  errorMessage(detail: string, title: string = this.translocoService.translate('Error')){
    this.messageService.add({
      severity: 'error',
      summary: title,
      detail: detail,
      life: 10000,
    });
  }
  
}
