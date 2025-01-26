import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, Output } from '@angular/core';
import { FormBuilder } from '@angular/forms';
import { MenuItem } from 'primeng/api';
import { SpiderPanelsModule } from "../spider-panels/spider-panels.module";
import { PrimengModule } from '../../modules/primeng.module';

@Component({
    selector: 'index-card',
    templateUrl: './index-card.component.html',
    standalone: true,
    imports: [
    CommonModule,
    PrimengModule,
    SpiderPanelsModule
]
})
export class IndexCardComponent {
    @Input() public last: boolean;
    @Input() public index: number;
    @Input() public header: string = '';
    @Input() public description: string;
    @Input() public showRemoveIcon: boolean;

    @Input() crudMenu: MenuItem[];

    @Output() onMenuIconClick: EventEmitter<number> = new EventEmitter();
    @Output() onRemoveIconClick: EventEmitter<null> = new EventEmitter();
    
    constructor(
        protected formBuilder: FormBuilder,
        ) {

        }

    ngOnInit(){
        // console.log(this.last);
    }

    menuIconClick(index: number){
        this.onMenuIconClick.next(index);
    }

    removeIconClick(){
        this.onRemoveIconClick.next(null);
    }

}