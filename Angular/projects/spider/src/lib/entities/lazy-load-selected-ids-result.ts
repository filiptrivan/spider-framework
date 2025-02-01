import { BaseEntity } from "../entities/base-entity";

export class LazyLoadSelectedIdsResult extends BaseEntity
{
    selectedIds?: number[] = []; // FT: Only for showing checkboxes, we will not send this to the backend
    totalRecordsSelected?: number = 0;

    constructor(
    {
        selectedIds,
        totalRecordsSelected,
    }:{
        selectedIds?: number[];
        totalRecordsSelected?: number;
    } = {}
    ) {
        super('LazyLoadSelectedIdsResult');

        this.selectedIds = selectedIds;
        this.totalRecordsSelected = totalRecordsSelected;
    }
}