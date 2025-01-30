import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { BaseControl } from '../base-control';
import { ReactiveFormsModule, FormsModule } from '@angular/forms';
import { RequiredComponent } from '../../components/required/required.component';
import { CommonModule } from '@angular/common';
import { TranslocoService } from '@jsverse/transloco';
import { PrimengModule } from '../../modules/primeng.module';

@Component({
    selector: 'spider-textbox',
    templateUrl: './spider-textbox.component.html',
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
export class SpiderTextboxComponent extends BaseControl implements OnInit {
    @Input() showButton: boolean = false;
    @Input() buttonIcon: string;
    @Output() onButtonClick = new EventEmitter();
    
    constructor(
        protected override translocoService: TranslocoService,
    ) { 
        super(translocoService);
    }

    override ngOnInit(){
        super.ngOnInit();
    }

    
    buttonClick() {
        this.onButtonClick.next(null);
    }
    
}
