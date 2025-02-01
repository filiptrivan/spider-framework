import { FormArray, FormControl, FormControlOptions, FormGroup, ValidatorFn } from '@angular/forms';
import { Observable } from 'rxjs';
import { BaseEntity } from '../../entities/base-entity';

export interface SpiderValidatorFn extends ValidatorFn {
    hasNotEmptyRule?: boolean;
}

// FT: It's made like generic type because of <number>, <string> etc. not to put class like User.
export class SpiderFormControl<T = any> extends FormControl<T> {
    public label: string;
    public labelForDisplay: string;
    public required: boolean;
    private _spiderValidator: SpiderValidatorFn | null;

    constructor(value: any, opts: FormControlOptions=null, required:boolean=false) {
        opts = opts ?? {updateOn: 'blur'};
        super(value, opts);
        this.required = required;
     }

    override getRawValue(): T { // FT: Doing this because .value gets only not disabled values
        return super.getRawValue() as T;
    }

    public override get validator(): SpiderValidatorFn | null {
        return this._spiderValidator;
    }

    public override set validator(validator: SpiderValidatorFn | null) {
        this._spiderValidator = validator;
        this.setValidators(validator); 
    }
}

export class SpiderFormGroup<TValue = any> extends FormGroup {
    declare controls: { [P in keyof TValue]: SpiderFormControl<TValue[P]> };

    constructor(controls: { [P in keyof TValue]: SpiderFormControl<TValue[P]> }) {
        super(controls);
    }

    override getRawValue(): TValue { // FT: Doing this because .value gets only not disabled values
        return super.getRawValue() as TValue;
    }

    public name?: string; // FT: Using for nested form groups
    public mainDTOName?: string;
    public saveObservableMethod?: (saveBody: any) => Observable<any>;
    public initSaveBody?: () => BaseEntity = () => null;
    public controlNamesFromHtml?: string[] = [];
}

export class SpiderFormArray<TValue = any> extends FormArray {
    override value: TValue[]; // FT: There is no getRawValue in FormArray
    public required: boolean;
    public modelConstructor: TValue;
    public translationKey: string;
    public controlNamesFromHtml?: string[] = [];
}