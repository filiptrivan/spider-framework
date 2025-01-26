import { BaseEntity } from "../entities/base-entity";

// FT HACK: Fake class, because of api imports
export class SimpleSaveResult extends BaseEntity
{
    id?: string;
  
    constructor(
    {
        id,
    }:{
        id?: string;
    } = {}
    ) {
        super('SimpleSaveResult');

        this.id = id;
    }
}