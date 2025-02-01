import {
    Component, Input,
  } from '@angular/core';
import { SpiderFormControl } from '../components/spider-form-control/spider-form-control';
import { TranslocoService } from '@jsverse/transloco';
  
  @Component({
    selector: 'base-control',
    template: '',
    styles: [],
  })
  export class BaseControl {
    @Input() control: SpiderFormControl; // FT: if you name it formControl: https://stackoverflow.com/a/54755671/21209982
    @Input() disabled: boolean = false;
    @Input() showLabel: boolean = true;
    @Input() label: string = null; // NgModel/Want custom translation
    @Input() controlValid: boolean = true; // NgModel
    @Input() placeholder: string = '';
    @Input() showTooltip: boolean = false;
    @Input() tooltipText: string = null;
    @Input() tooltipIcon: string = 'pi pi-info-circle';
    errorMessageTooltipEvent: string;
    validationErrorMessage: string;
    
    constructor(
      protected translocoService: TranslocoService,
    ) {

    }

    ngOnInit(){
      if(this.control != null && this.disabled == true)
        this.control.disable();

      if(this.control?.validator?.hasNotEmptyRule == true) // FT HACK: Be carefull with this name, if you change it in generator you need to change it here also
        this.control.required = true;

       this.errorMessageTooltipEvent = window.innerWidth > 1000 ? 'hover' : 'focus'
    }

    ngAfterViewInit(){

    }

    getTranslatedLabel(): string{
      return this.label ?? this.control.labelForDisplay;
    }

    getValidationErrrorMessages(){
      if(this.control?.errors && this.control?.dirty){
          // FT: it should always be one error message for single form control, 
          // also i don't need to reassign it to null because it will be shown only when control.valid == false
          this.validationErrorMessage = this.control.errors['_'];
      }
      
      return this.validationErrorMessage;
    }

  }