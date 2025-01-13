import { Injectable } from '@angular/core';
import { TranslocoService } from '@jsverse/transloco';

@Injectable({
  providedIn: 'root',
})
export class TranslateClassNamesGeneratedService {

    constructor(
    private translocoService: TranslocoService
    ) {
    }

    translate(name: string): string
    {
        switch(name) 
        {
            case 'TableFilter':
                return this.translocoService.translate('TableFilter');
            case 'LazyLoadSelectedIdsResult':
                return this.translocoService.translate('LazyLoadSelectedIdsResult');
            case 'BusinessObject':
                return this.translocoService.translate('BusinessObject');
            case 'SimpleSaveResult':
                return this.translocoService.translate('SimpleSaveResult');
            case 'TableResponse':
                return this.translocoService.translate('TableResponse');
            case 'ReadonlyObject':
                return this.translocoService.translate('ReadonlyObject');
            case 'Namebook':
                return this.translocoService.translate('Namebook');
            case 'TableFilterSortMeta':
                return this.translocoService.translate('TableFilterSortMeta');
            case 'PaginationResult':
                return this.translocoService.translate('PaginationResult');
            case 'TableFilterContext':
                return this.translocoService.translate('TableFilterContext');
            case 'Codebook':
                return this.translocoService.translate('Codebook');
            case 'ExcelReportOptions':
                return this.translocoService.translate('ExcelReportOptions');
            default:
                return null;
        }
    }
}

