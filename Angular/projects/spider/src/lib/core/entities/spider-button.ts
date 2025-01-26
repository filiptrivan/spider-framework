import { BaseEntity } from "../entities/base-entity";

export class SpiderButton extends BaseEntity
{
    label?: string;
    icon?: string;
    onClick?: () => void;
  
    constructor(
    {
        label,
        icon,
        onClick,
    }:{
        label?: string;
        icon?: string;
        onClick?: () => void;
    } = {}
    ) {
        super('SpiderButton');

        this.label = label;
        this.icon = icon;
        this.onClick = onClick;
    }
}