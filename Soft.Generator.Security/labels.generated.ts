import { Injectable } from '@angular/core';
import { TranslocoService } from '@jsverse/transloco';

@Injectable({
  providedIn: 'root',
})
export class TranslateLabelsGeneratedService {

    constructor(
        private translocoService: TranslocoService
    ) {
    }

    translate(name: string): string
    {
        switch(name) 
        {
            case 'filters':
                return this.translocoService.translate('Filters');
            case 'first':
                return this.translocoService.translate('First');
            case 'rows':
                return this.translocoService.translate('Rows');
            case 'sortField':
                return this.translocoService.translate('SortField');
            case 'sortOrder':
                return this.translocoService.translate('SortOrder');
            case 'multiSortMeta':
                return this.translocoService.translate('MultiSortMeta');
            case 'additionalFilterIdInt':
                return this.translocoService.translate('AdditionalFilterIdInt');
            case 'additionalFilterIdLong':
                return this.translocoService.translate('AdditionalFilterIdLong');
            case 'selectedIds':
                return this.translocoService.translate('SelectedIds');
            case 'totalRecordsSelected':
                return this.translocoService.translate('TotalRecordsSelected');
            case 'id':
                return this.translocoService.translate('Id');
            case 'version':
                return this.translocoService.translate('Version');
            case 'createdAt':
                return this.translocoService.translate('CreatedAt');
            case 'modifiedAt':
                return this.translocoService.translate('ModifiedAt');
            case 'data':
                return this.translocoService.translate('Data');
            case 'totalRecords':
                return this.translocoService.translate('TotalRecords');
            case 'displayName':
                return this.translocoService.translate('DisplayName');
            case 'field':
                return this.translocoService.translate('Field');
            case 'order':
                return this.translocoService.translate('Order');
            case 'query':
                return this.translocoService.translate('Query');
            case 'value':
                return this.translocoService.translate('Value');
            case 'matchMode':
                return this.translocoService.translate('MatchMode');
            case 'operator':
                return this.translocoService.translate('Operator');
            case 'code':
                return this.translocoService.translate('Code');
            case 'additionalColumnHeaders':
                return this.translocoService.translate('AdditionalColumnHeaders');
            case 'additionalDataStartColumn':
                return this.translocoService.translate('AdditionalDataStartColumn');
            case 'dataSheetName':
                return this.translocoService.translate('DataSheetName');
            case 'dataSheetName2':
                return this.translocoService.translate('DataSheetName2');
            case 'dataStartRow':
                return this.translocoService.translate('DataStartRow');
            case 'dataStartColumn':
                return this.translocoService.translate('DataStartColumn');
            case 'createNewDataRows':
                return this.translocoService.translate('CreateNewDataRows');
            default:
                return null;
        }
    }
}

