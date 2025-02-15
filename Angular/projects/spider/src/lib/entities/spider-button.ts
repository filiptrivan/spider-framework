import { BaseEntity } from "../entities/base-entity";

export class SpiderButton extends BaseEntity
{
    label?: string;
    icon?: string;
    disabled?: boolean;
    onClick?: () => void;
  
    constructor(
    {
        label,
        icon,
        disabled,
        onClick,
    }:{
        label?: string;
        icon?: string;
        disabled?: boolean;
        onClick?: () => void;
    } = {}
    ) {
        super('SpiderButton');

        this.label = label;
        this.icon = icon;
        this.disabled = disabled;
        this.onClick = onClick;
    }
}