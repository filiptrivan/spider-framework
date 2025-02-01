import { BaseEntity } from "./base-entity";

export class InitCompanyAuthDialogDetails extends BaseEntity
{
    image?: string;
    companyName?: string;
  
    constructor(
    {
        image,
        companyName,
    }:{
        image?: string;
        companyName?: string;
    } = {}
    ) {
        super('InitCompanyAuthDialogDetails');

        this.image = image;
        this.companyName = companyName;
    }
}