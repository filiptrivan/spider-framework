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
    private zone: NgZone
  ) {
    
  }

  successMessage(detail: string, title: string = this.translocoService.translate('SuccessfulAction')) {
    this.zone.run(()=>{
      this.messageService.add({
        severity: 'success',
        summary: title,
        detail: detail,
        life: 10000,
      });
    });
  }

  warningMessage(detail: string, title: string = this.translocoService.translate('Warning')){
    this.zone.run(()=>{
      this.messageService.add({
        severity: 'warn',
        summary: title,
        detail: detail,
        life: 10000,
      });
    });
  }

  errorMessage(detail: string, title: string = this.translocoService.translate('Error')){
    this.zone.run(()=>{
      this.messageService.add({
        severity: 'error',
        summary: title,
        detail: detail,
        life: 10000,
      });
    });
  }
  
}
