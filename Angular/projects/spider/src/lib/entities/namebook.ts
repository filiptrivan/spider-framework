import { BaseEntity } from "../entities/base-entity";

export class Namebook extends BaseEntity
{
    id?: number;
    displayName?: string;
  
    constructor(
    {
        id,
        displayName,
    }:{
        id?: number;
        displayName?: string;
    } = {}
    ) {
        super('Namebook');

        this.id = id;
        this.displayName = displayName;
    }
}