import { Injectable } from "@angular/core";
import { SpiderFormControl, SpiderValidatorFn } from "../components/spider-form-control/spider-form-control";

@Injectable({
    providedIn: 'root',
})
export abstract class ValidatorAbstractService {

    constructor(
    ) {
    }

    abstract setValidator (formControl: SpiderFormControl, className: string): SpiderValidatorFn;
}
