import { Component, OnInit } from '@angular/core';
import { BaseControl } from '../base-control';
import { ReactiveFormsModule, FormsModule } from '@angular/forms';
import { RequiredComponent } from '../../components/required/required.component';
import { CommonModule } from '@angular/common';
import { TranslocoService } from '@jsverse/transloco';
import { PrimengModule } from '../../modules/primeng.module';

@Component({
    selector: 'spider-colorpick',
    templateUrl: './spider-colorpick.component.html',
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
export class SpiderColorpickComponent extends BaseControl implements OnInit {

    constructor(
        protected override translocoService: TranslocoService,
    ) { 
        super(translocoService);
    }

    override ngOnInit(){
        this.control.valueChanges.subscribe((value) => {
            this.control.setValue(value, { emitEvent: false }); // FT: Preventing infinite loop
        });

        if (this.control.value == null)
            this.placeholder = this.translocoService.translate('SelectAColor');

        super.ngOnInit();
    }

}
