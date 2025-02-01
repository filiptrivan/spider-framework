import { Component, OnInit } from '@angular/core';
import { BaseControl } from '../base-control';
import { ReactiveFormsModule, FormsModule } from '@angular/forms';
import { RequiredComponent } from '../../components/required/required.component';
import { CommonModule } from '@angular/common';
import { TranslocoService } from '@jsverse/transloco';
import { PrimengModule } from '../../modules/primeng.module';

@Component({
    selector: 'spider-textarea',
    templateUrl: './spider-textarea.component.html',
    styles: [],
    standalone: true,
    imports: [
        CommonModule,
        ReactiveFormsModule,
        FormsModule,
        PrimengModule,
        RequiredComponent
    ]
})
export class SpiderTextareaComponent extends BaseControl implements OnInit {

    constructor(
        protected override translocoService: TranslocoService,
    ) { 
        super(translocoService);
    }

    override ngOnInit(){
        super.ngOnInit();
    }
}
