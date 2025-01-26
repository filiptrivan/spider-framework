import { Injectable } from "@angular/core";

@Injectable({
    providedIn: 'root',
})
export abstract class TranslateLabelsAbstractService {

    constructor(
    ) {
    }

    abstract translate (name: string): string;
}
