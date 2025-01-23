import { ValidationErrors } from '@angular/forms';
import { SpiderFormControl, SpiderValidatorFn } from 'src/app/core/components/spider-form-control/spider-form-control';
import { validatePrecisionScale } from 'src/app/core/services/helper-functions';
import { TranslocoService } from '@jsverse/transloco';
import { Injectable } from '@angular/core';

@Injectable({
    providedIn: 'root',
})
export class ValidatorServiceGenerated {

    constructor(
        protected translocoService: TranslocoService
    ) {
    }

    setValidator = (formControl: SpiderFormControl, className: string): SpiderValidatorFn => {
        switch(formControl.label + className){

















            default:
                return null;
        }
    }



















}

