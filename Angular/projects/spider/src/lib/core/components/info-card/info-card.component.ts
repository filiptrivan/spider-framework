import { CommonModule } from '@angular/common';
import { Component, Input } from '@angular/core';
import { FormBuilder } from '@angular/forms';
import { SpiderPanelsModule } from "../spider-panels/spider-panels.module";
import { PrimengModule } from '../../modules/primeng.module';

@Component({
    selector: 'info-card',
    templateUrl: './info-card.component.html',
    standalone: true,
    imports: [
    CommonModule,
    PrimengModule,
    SpiderPanelsModule
]
})
export class InfoCardComponent {
    @Input() public header: string = '';
    @Input() public description: string;
    
    constructor(
        protected formBuilder: FormBuilder,
        ) {

        }

    ngOnInit(){
    }

}