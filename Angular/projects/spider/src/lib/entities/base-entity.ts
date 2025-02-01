export class BaseEntity {

    public pipedProperties?: PipedProperty[];
    public typeName?: string;

    constructor(typeName: string) {
        this.pipedProperties = [];
        this.typeName = typeName;
    }
}

export class PipedProperty {
    pipe: PropertyPipes;
    control: string;

    constructor(p: PropertyPipes, c: string) {
        this.pipe = p;
        this.control = c;
    }
}

export enum PropertyPipes {
    IntegerPipe,
    DecimalPipe
}