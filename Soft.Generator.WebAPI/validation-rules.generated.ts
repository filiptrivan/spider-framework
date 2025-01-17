import { ValidationErrors } from '@angular/forms';
import { SoftFormControl, SoftValidatorFn } from 'src/app/core/components/soft-form-control/soft-form-control';
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

    setValidator(formControl: SoftFormControl, className: string): SoftValidatorFn {
        switch(formControl.label + className){







        case 'emailLogin':
            return this.emailLoginValidator(formControl);




        case 'namePermission':
            return this.namePermissionValidator(formControl);
        case 'nameLatinPermission':
            return this.nameLatinPermissionValidator(formControl);
        case 'descriptionPermission':
            return this.descriptionPermissionValidator(formControl);
        case 'descriptionLatinPermission':
            return this.descriptionLatinPermissionValidator(formControl);
        case 'codePermission':
            return this.codePermissionValidator(formControl);





        case 'emailRegistration':
            return this.emailRegistrationValidator(formControl);



        case 'nameRole':
            return this.nameRoleValidator(formControl);
        case 'descriptionRole':
            return this.descriptionRoleValidator(formControl);
        case 'versionRole':
            return this.versionRoleValidator(formControl);
        case 'createdAtRole':
            return this.createdAtRoleValidator(formControl);
        case 'modifiedAtRole':
            return this.modifiedAtRoleValidator(formControl);











        case 'verificationCodeVerificationTokenRequest':
            return this.verificationCodeVerificationTokenRequestValidator(formControl);
        case 'emailVerificationTokenRequest':
            return this.emailVerificationTokenRequestValidator(formControl);

            default:
                return null;
        }
    }








    emailLoginValidator(control: SoftFormControl): SoftValidatorFn {
        const validator: SoftValidatorFn = (): ValidationErrors | null => {
            const value = control.value;

        const notEmptyRule = typeof value !== 'undefined' && value !== null && value !== '';
        const min = 5;
        const max = 100;
        const stringLengthRule = (value?.length >= min && value?.length <= max) || (typeof value === 'undefined' || value === null || value === '');
        const emailAddressRule = /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(value);

            const emailValid = notEmptyRule && stringLengthRule && emailAddressRule;

            return emailValid ? null : { _ : this.translocoService.translate('NotEmptyLengthEmailAddress', {min, max}) };
        };
        validator.hasNotEmptyRule = true;
        control.required = true;
        control.validator = validator;
        // TODO FT: When you improve generated code, and could realize on the backend is this property of the Date type, generate this line only for Date form controls.
        control.updateValueAndValidity(); // FT: It's necessary for Date angular type
        return validator;
    }




    namePermissionValidator(control: SoftFormControl): SoftValidatorFn {
        const validator: SoftValidatorFn = (): ValidationErrors | null => {
            const value = control.value;

        const notEmptyRule = typeof value !== 'undefined' && value !== null && value !== '';
        const min = 1;
        const max = 100;
        const stringLengthRule = (value?.length >= min && value?.length <= max) || (typeof value === 'undefined' || value === null || value === '');

            const nameValid = notEmptyRule && stringLengthRule;

            return nameValid ? null : { _ : this.translocoService.translate('NotEmptyLength', {min, max}) };
        };
        validator.hasNotEmptyRule = true;
        control.required = true;
        control.validator = validator;
        // TODO FT: When you improve generated code, and could realize on the backend is this property of the Date type, generate this line only for Date form controls.
        control.updateValueAndValidity(); // FT: It's necessary for Date angular type
        return validator;
    }
    nameLatinPermissionValidator(control: SoftFormControl): SoftValidatorFn {
        const validator: SoftValidatorFn = (): ValidationErrors | null => {
            const value = control.value;

        const notEmptyRule = typeof value !== 'undefined' && value !== null && value !== '';
        const min = 1;
        const max = 100;
        const stringLengthRule = (value?.length >= min && value?.length <= max) || (typeof value === 'undefined' || value === null || value === '');

            const nameLatinValid = notEmptyRule && stringLengthRule;

            return nameLatinValid ? null : { _ : this.translocoService.translate('NotEmptyLength', {min, max}) };
        };
        validator.hasNotEmptyRule = true;
        control.required = true;
        control.validator = validator;
        // TODO FT: When you improve generated code, and could realize on the backend is this property of the Date type, generate this line only for Date form controls.
        control.updateValueAndValidity(); // FT: It's necessary for Date angular type
        return validator;
    }
    descriptionPermissionValidator(control: SoftFormControl): SoftValidatorFn {
        const validator: SoftValidatorFn = (): ValidationErrors | null => {
            const value = control.value;

        const min = 1;
        const max = 400;
        const stringLengthRule = (value?.length >= min && value?.length <= max) || (typeof value === 'undefined' || value === null || value === '');

            const descriptionValid = stringLengthRule;

            return descriptionValid ? null : { _ : this.translocoService.translate('Length', {min, max}) };
        };

        control.validator = validator;
        // TODO FT: When you improve generated code, and could realize on the backend is this property of the Date type, generate this line only for Date form controls.
        control.updateValueAndValidity(); // FT: It's necessary for Date angular type
        return validator;
    }
    descriptionLatinPermissionValidator(control: SoftFormControl): SoftValidatorFn {
        const validator: SoftValidatorFn = (): ValidationErrors | null => {
            const value = control.value;

        const min = 1;
        const max = 400;
        const stringLengthRule = (value?.length >= min && value?.length <= max) || (typeof value === 'undefined' || value === null || value === '');

            const descriptionLatinValid = stringLengthRule;

            return descriptionLatinValid ? null : { _ : this.translocoService.translate('Length', {min, max}) };
        };

        control.validator = validator;
        // TODO FT: When you improve generated code, and could realize on the backend is this property of the Date type, generate this line only for Date form controls.
        control.updateValueAndValidity(); // FT: It's necessary for Date angular type
        return validator;
    }
    codePermissionValidator(control: SoftFormControl): SoftValidatorFn {
        const validator: SoftValidatorFn = (): ValidationErrors | null => {
            const value = control.value;

        const notEmptyRule = typeof value !== 'undefined' && value !== null && value !== '';
        const min = 1;
        const max = 100;
        const stringLengthRule = (value?.length >= min && value?.length <= max) || (typeof value === 'undefined' || value === null || value === '');

            const codeValid = notEmptyRule && stringLengthRule;

            return codeValid ? null : { _ : this.translocoService.translate('NotEmptyLength', {min, max}) };
        };
        validator.hasNotEmptyRule = true;
        control.required = true;
        control.validator = validator;
        // TODO FT: When you improve generated code, and could realize on the backend is this property of the Date type, generate this line only for Date form controls.
        control.updateValueAndValidity(); // FT: It's necessary for Date angular type
        return validator;
    }





    emailRegistrationValidator(control: SoftFormControl): SoftValidatorFn {
        const validator: SoftValidatorFn = (): ValidationErrors | null => {
            const value = control.value;

        const notEmptyRule = typeof value !== 'undefined' && value !== null && value !== '';
        const min = 5;
        const max = 100;
        const stringLengthRule = (value?.length >= min && value?.length <= max) || (typeof value === 'undefined' || value === null || value === '');
        const emailAddressRule = /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(value);

            const emailValid = notEmptyRule && stringLengthRule && emailAddressRule;

            return emailValid ? null : { _ : this.translocoService.translate('NotEmptyLengthEmailAddress', {min, max}) };
        };
        validator.hasNotEmptyRule = true;
        control.required = true;
        control.validator = validator;
        // TODO FT: When you improve generated code, and could realize on the backend is this property of the Date type, generate this line only for Date form controls.
        control.updateValueAndValidity(); // FT: It's necessary for Date angular type
        return validator;
    }



    nameRoleValidator(control: SoftFormControl): SoftValidatorFn {
        const validator: SoftValidatorFn = (): ValidationErrors | null => {
            const value = control.value;

        const notEmptyRule = typeof value !== 'undefined' && value !== null && value !== '';
        const min = 1;
        const max = 255;
        const stringLengthRule = (value?.length >= min && value?.length <= max) || (typeof value === 'undefined' || value === null || value === '');

            const nameValid = notEmptyRule && stringLengthRule;

            return nameValid ? null : { _ : this.translocoService.translate('NotEmptyLength', {min, max}) };
        };
        validator.hasNotEmptyRule = true;
        control.required = true;
        control.validator = validator;
        // TODO FT: When you improve generated code, and could realize on the backend is this property of the Date type, generate this line only for Date form controls.
        control.updateValueAndValidity(); // FT: It's necessary for Date angular type
        return validator;
    }
    descriptionRoleValidator(control: SoftFormControl): SoftValidatorFn {
        const validator: SoftValidatorFn = (): ValidationErrors | null => {
            const value = control.value;

        const min = 1;
        const max = 400;
        const stringLengthRule = (value?.length >= min && value?.length <= max) || (typeof value === 'undefined' || value === null || value === '');

            const descriptionValid = stringLengthRule;

            return descriptionValid ? null : { _ : this.translocoService.translate('Length', {min, max}) };
        };

        control.validator = validator;
        // TODO FT: When you improve generated code, and could realize on the backend is this property of the Date type, generate this line only for Date form controls.
        control.updateValueAndValidity(); // FT: It's necessary for Date angular type
        return validator;
    }
    versionRoleValidator(control: SoftFormControl): SoftValidatorFn {
        const validator: SoftValidatorFn = (): ValidationErrors | null => {
            const value = control.value;

        const notEmptyRule = typeof value !== 'undefined' && value !== null && value !== '';

            const versionValid = notEmptyRule;

            return versionValid ? null : { _ : this.translocoService.translate('NotEmpty', {}) };
        };
        validator.hasNotEmptyRule = true;
        control.required = true;
        control.validator = validator;
        // TODO FT: When you improve generated code, and could realize on the backend is this property of the Date type, generate this line only for Date form controls.
        control.updateValueAndValidity(); // FT: It's necessary for Date angular type
        return validator;
    }
    createdAtRoleValidator(control: SoftFormControl): SoftValidatorFn {
        const validator: SoftValidatorFn = (): ValidationErrors | null => {
            const value = control.value;

        const notEmptyRule = typeof value !== 'undefined' && value !== null && value !== '';

            const createdAtValid = notEmptyRule;

            return createdAtValid ? null : { _ : this.translocoService.translate('NotEmpty', {}) };
        };
        validator.hasNotEmptyRule = true;
        control.required = true;
        control.validator = validator;
        // TODO FT: When you improve generated code, and could realize on the backend is this property of the Date type, generate this line only for Date form controls.
        control.updateValueAndValidity(); // FT: It's necessary for Date angular type
        return validator;
    }
    modifiedAtRoleValidator(control: SoftFormControl): SoftValidatorFn {
        const validator: SoftValidatorFn = (): ValidationErrors | null => {
            const value = control.value;

        const notEmptyRule = typeof value !== 'undefined' && value !== null && value !== '';

            const modifiedAtValid = notEmptyRule;

            return modifiedAtValid ? null : { _ : this.translocoService.translate('NotEmpty', {}) };
        };
        validator.hasNotEmptyRule = true;
        control.required = true;
        control.validator = validator;
        // TODO FT: When you improve generated code, and could realize on the backend is this property of the Date type, generate this line only for Date form controls.
        control.updateValueAndValidity(); // FT: It's necessary for Date angular type
        return validator;
    }











    verificationCodeVerificationTokenRequestValidator(control: SoftFormControl): SoftValidatorFn {
        const validator: SoftValidatorFn = (): ValidationErrors | null => {
            const value = control.value;

        const notEmptyRule = typeof value !== 'undefined' && value !== null && value !== '';
        const length = 6;
        const stringSingleLengthRule = (value?.length == length) || (typeof value === 'undefined' || value === null || value === '');

            const verificationCodeValid = notEmptyRule && stringSingleLengthRule;

            return verificationCodeValid ? null : { _ : this.translocoService.translate('NotEmptySingleLength', {length}) };
        };
        validator.hasNotEmptyRule = true;
        control.required = true;
        control.validator = validator;
        // TODO FT: When you improve generated code, and could realize on the backend is this property of the Date type, generate this line only for Date form controls.
        control.updateValueAndValidity(); // FT: It's necessary for Date angular type
        return validator;
    }
    emailVerificationTokenRequestValidator(control: SoftFormControl): SoftValidatorFn {
        const validator: SoftValidatorFn = (): ValidationErrors | null => {
            const value = control.value;

        const notEmptyRule = typeof value !== 'undefined' && value !== null && value !== '';
        const min = 5;
        const max = 100;
        const stringLengthRule = (value?.length >= min && value?.length <= max) || (typeof value === 'undefined' || value === null || value === '');
        const emailAddressRule = /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(value);

            const emailValid = notEmptyRule && stringLengthRule && emailAddressRule;

            return emailValid ? null : { _ : this.translocoService.translate('NotEmptyLengthEmailAddress', {min, max}) };
        };
        validator.hasNotEmptyRule = true;
        control.required = true;
        control.validator = validator;
        // TODO FT: When you improve generated code, and could realize on the backend is this property of the Date type, generate this line only for Date form controls.
        control.updateValueAndValidity(); // FT: It's necessary for Date angular type
        return validator;
    }


}

