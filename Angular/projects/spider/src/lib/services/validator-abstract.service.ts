import { TranslocoService } from '@jsverse/transloco';
import { Injectable } from "@angular/core";
import { SpiderFormArray, SpiderFormControl, SpiderValidatorFn } from "../components/spider-form-control/spider-form-control";
import { ValidationErrors } from "@angular/forms";

@Injectable({
    providedIn: 'root',
})
export abstract class ValidatorAbstractService {

    constructor(
        protected translocoService: TranslocoService
    ) {
    }

    abstract setValidator (formControl: SpiderFormControl, className: string): SpiderValidatorFn;

    isArrayEmpty = (control: SpiderFormControl): SpiderValidatorFn => {
        const validator: SpiderValidatorFn = (): ValidationErrors | null => {
            const value = control.value;
    
            const notEmptyRule = typeof value !== 'undefined' && value !== null && value.length !== 0;
    
            const arrayValid = notEmptyRule;
    
            return arrayValid ? null : { _ : this.translocoService.translate('NotEmpty')};
        };
        validator.hasNotEmptyRule = true;
        control.required = true;
        return validator;
    }

    notEmpty = (control: SpiderFormControl): void => {
        const validator: SpiderValidatorFn = (): ValidationErrors | null => {
            const value = control.value;
    
            const notEmptyRule = typeof value !== 'undefined' && value !== null && value !== '';
    
            const arrayValid = notEmptyRule;
    
            return arrayValid ? null : { _ : this.translocoService.translate('NotEmpty')};
        };
        validator.hasNotEmptyRule = true;
        control.required = true;
        control.validator = validator;
        control.updateValueAndValidity();
    }
    
    isFormArrayEmpty = (control: SpiderFormArray): SpiderValidatorFn => {
        const validator: SpiderValidatorFn = (): ValidationErrors | null => {
            const value = control;
    
            const notEmptyRule = typeof value !== 'undefined' && value !== null && value.length !== 0;
    
            const arrayValid = notEmptyRule;
    
            return arrayValid ? null : { _ : this.translocoService.translate('NotEmpty')};
        };
        validator.hasNotEmptyRule = true;
        control.required = true;
        return validator;
    }
}
