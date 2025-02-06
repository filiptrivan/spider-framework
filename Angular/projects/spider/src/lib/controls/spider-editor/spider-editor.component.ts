import { Component, Input, OnInit, ViewChild } from '@angular/core';
import { BaseControl } from '../base-control';
import { ReactiveFormsModule, FormsModule } from '@angular/forms';
import { RequiredComponent } from '../../components/required/required.component';
import { CommonModule } from '@angular/common';
import { TranslocoService } from '@jsverse/transloco';
import { PrimengModule } from '../../modules/primeng.module';
import { Editor } from 'primeng/editor';
import { Tooltip } from 'primeng/tooltip';

@Component({
    selector: 'spider-editor',
    templateUrl: './spider-editor.component.html',
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
export class SpiderEditorComponent extends BaseControl implements OnInit {
    @ViewChild(Editor) editor: Editor;
    @ViewChild(Tooltip) tooltip: Tooltip;

    constructor(
        protected override translocoService: TranslocoService,
    ) { 
        super(translocoService);
    }

    override ngOnInit(){
        super.ngOnInit();
    }

    onClick(){
        let editableArea: HTMLElement = this.editor.el.nativeElement.querySelector('.ql-editor');
        
        editableArea.onblur = () => {
            this.control.markAsDirty();
            this.tooltip.deactivate();
        };

        editableArea.onfocus = () => {
            if (this.errorMessageTooltipEvent == 'focus' ) {
                this.tooltip.activate();
            }
        };

        editableArea.onmouseover = () => {
            if (this.errorMessageTooltipEvent == 'hover' ) {
                this.tooltip.activate();
            }
        }
    }

}
