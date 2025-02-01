import { ConfirmationService } from 'primeng/api';
import { Component } from "@angular/core";
import { DynamicDialogConfig, DynamicDialogRef } from "primeng/dynamicdialog";
import { TranslocoDirective } from '@jsverse/transloco';
import { PrimengModule } from '../../modules/primeng.module';

@Component({
  selector: 'spider-delete-confirmation',
  templateUrl: './spider-delete-confirmation.component.html',
  styles: [],
  standalone: true,
  imports: [
    PrimengModule,
    TranslocoDirective,
  ],
  providers: [
    ConfirmationService
  ]
})
export class SpiderDeleteConfirmationComponent {

  constructor(public ref: DynamicDialogRef, public config: DynamicDialogConfig) {}

  accept(){
    this.config.data.deleteItemFromTableObservableMethod(this.config.data.id).subscribe({
      next: () => {
        this.ref.close(true); // deleted succesfully
      },
      error: () => {
        this.ref.close(false); // not deleted succesfully
      },
    });
  }

  reject(){
    this.ref.close(false);
  }
}