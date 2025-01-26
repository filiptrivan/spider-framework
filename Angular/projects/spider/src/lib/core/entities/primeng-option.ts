import { BaseEntity } from "../entities/base-entity";

export class PrimengOption extends BaseEntity
{
    label?: string;
    value?: any;
  
    constructor(
    {
        label,
        value,
    }:{
        label?: string;
        value?: any;
    } = {}
    ) {
        super('PrimengOption');

        this.label = label;
        this.value = value;
    }

}