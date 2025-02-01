import { BaseEntity } from "./base-entity";

// FT HACK: Fake class, because of api imports
export class Codebook extends BaseEntity
{
    code?: string;
    displayName?: string;
  
    constructor(
    {
        code,
        displayName,
    }:{
        code?: string;
        displayName?: string;
    } = {}
    ) {
        super('Namebook');

        this.code = code;
        this.displayName = displayName;
    }
}