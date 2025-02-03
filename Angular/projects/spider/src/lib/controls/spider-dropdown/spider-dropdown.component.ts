import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormsModule } from '@angular/forms';
import { RequiredComponent } from '../../components/required/required.component';
import { BaseDropdownControl } from '../base-dropdown-control';
import { TranslocoService } from '@jsverse/transloco';
import { DropdownChangeEvent } from 'primeng/dropdown';
import { PrimengModule } from '../../modules/primeng.module';

@Component({
    selector: 'spider-dropdown',
    templateUrl: './spider-dropdown.component.html',
    styles: [],
    standalone: true,
    imports: [
        ReactiveFormsModule,
        FormsModule,
        PrimengModule,
        CommonModule,
        RequiredComponent
    ]
})
export class SpiderDropdownComponent extends BaseDropdownControl implements OnInit {
    @Output() onChange: EventEmitter<DropdownChangeEvent> = new EventEmitter();

    constructor(
        protected override translocoService: TranslocoService,
    ) { 
        super(translocoService);
    }

    override ngOnInit(){
        super.ngOnInit();
    }

    change(event: DropdownChangeEvent){
        this.onChange.next(event);
    }

}
