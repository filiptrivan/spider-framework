import { Component, EventEmitter, Inject, Input, LOCALE_ID, OnInit, Output, ViewChild } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { SelectItem } from 'primeng/api';
import { Table, TableFilterEvent, TableLazyLoadEvent } from 'primeng/table';
import { DialogService, DynamicDialogRef } from 'primeng/dynamicdialog';
import { SpiderDeleteConfirmationComponent } from '../spider-delete-dialog/spider-delete-confirmation.component';
import { CommonModule, formatDate } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { SpiderMessageService } from '../../services/spider-message.service';
import { firstValueFrom, Observable } from 'rxjs';
import { PrimengOption } from '../../entities/primeng-option';
import { TranslocoDirective, TranslocoService } from '@jsverse/transloco';
import { SpiderControlsModule } from '../../controls/spider-controls.module';
import { SpiderFormControl } from '../spider-form-control/spider-form-control';
import { TableResponse } from '../../entities/table-response';
import { LazyLoadSelectedIdsResult } from '../../entities/lazy-load-selected-ids-result';
import { exportListToExcel } from '../../services/helper-functions';
import { TableFilter } from '../../entities/table-filter';
import { PrimengModule } from '../../modules/primeng.module';

@Component({
  selector: 'spider-data-table',
  templateUrl: './spider-data-table.component.html',
  styleUrl: 'spider-data-table.component.scss',
  styles: [`
  	:host {
		  ::ng-deep {
		    .remove-button-border-focus:focus, 
        .remove-button-border-focus:enabled:focus {
        box-shadow: none;
        -webkit-box-shadow: none;
        -moz-box-shadow: none;
        background-color: var(--gray-200);
		    }
		  }
	  }
  `],
  imports: [
    FormsModule,
    CommonModule,
    PrimengModule,
    TranslocoDirective,
    SpiderControlsModule,
  ],
  standalone: true,
})
export class SpiderDataTableComponent implements OnInit {
  @ViewChild('dt') table: Table;
  @Input() tableTitle: string;
  @Input() tableIcon: string = 'pi pi-list';
  @Input() items: any[]; // FT: Pass only when hasLazyLoad === false
  @Input() rows: number = 10;
  @Input() cols: Column[];
  @Input() showPaginator: boolean = true; // FT: Pass only when hasLazyLoad === false
  @Input() showCardWrapper: boolean = true;
  totalRecords: number;
  @Output() onTotalRecordsChange: EventEmitter<number> = new EventEmitter();;
  
  @Input() getTableDataObservableMethod: (tableFilter: TableFilter) => Observable<TableResponse>;
  @Input() exportTableDataToExcelObservableMethod: (tableFilter: TableFilter) => Observable<any>;
  @Input() deleteItemFromTableObservableMethod: (rowId: number) => Observable<any>;

  lastLazyLoadEvent: TableLazyLoadEvent;
  loading: boolean = true;
  
  @Input() newlySelectedItems: number[] = [];
  fakeSelectedItems: number[] = []; // FT: Only for showing checkboxes, we will not send this to the backend
  currentPageSelectedItemsFromDb: number[] = []; // FT: Made so we can add only newly selected items to the newlySelectedItems
  @Input() unselectedItems: number[] = [];
  @Input() selectionMode: 'single' | 'multiple' | undefined | null;
  @Output() onLazyLoad: EventEmitter<TableFilter> = new EventEmitter();
  rowsSelectedNumber: number = 0;
  isAllSelected: boolean = null;
  fakeIsAllSelected: boolean = false; // FT: Only for showing checkboxes, we will not send this to the backend
  isFirstTimeLazyLoad: boolean = true;
  @Output() onIsAllSelectedChange: EventEmitter<AllClickEvent> = new EventEmitter();
  @Input() selectedLazyLoadObservableMethod: (tableFilter: TableFilter) => Observable<LazyLoadSelectedIdsResult>;
  @Input() additionalFilterIdLong: number;
  
  matchModeDateOptions: SelectItem[] = [];
  matchModeNumberOptions: SelectItem[] = [];
  @Input() showAddButton: boolean = true; 
  @Input() showExportToExcelButton: boolean = true;
  @Input() showReloadTableButton: boolean = false;

  deleteRef: DynamicDialogRef;

  // Client side table
  // @Input() formArrayItems: any[]; // FT: Pass this only if you have some additional logic for showing data
  @Input() getFormArrayItems: (additionalIndexes?: any) => any[];
  @Input() hasLazyLoad: boolean = true; 
  selectedItemIds: number[] = []; // FT: Pass only when hasLazyLoad === false, it's enough if the M2M association hasn't additional fields
  @Input() getAlreadySelectedItemIds: (additionalIndexes?: any) => number[]; // FT: Pass only when hasLazyLoad === false, it's enough if the M2M association hasn't additional fields
  selectedItems: any[] = []; // FT: Pass only when hasLazyLoad === false
  @Input() getAlreadySelectedItems: (additionalIndexes?: any) => any[]; // FT: Pass only when hasLazyLoad === false, it's enough if the M2M association hasn't additional fields
  @Input() getFormControl: (formControlName: string, index: number, additionalIndexes?: any) => SpiderFormControl;
  @Input() additionalIndexes: any;
  @Output() onRowSelect: EventEmitter<RowClickEvent> = new EventEmitter();
  @Output() onRowUnselect: EventEmitter<RowClickEvent> = new EventEmitter();

  constructor(
    private router: Router,
    private dialogService: DialogService,
    private route: ActivatedRoute,
    private messageService: SpiderMessageService,
    private translocoService: TranslocoService,
    @Inject(LOCALE_ID) private locale: string
  ) {}

  ngOnInit(): void {
    this.matchModeDateOptions = [
      { label: this.translocoService.translate('DatesBefore'), value: 'dateBefore' },
      { label: this.translocoService.translate('DatesAfter'), value: 'dateAfter' },
    ];

    this.matchModeNumberOptions = [
      { label: this.translocoService.translate('Equals'), value: 'equals' },
      { label: this.translocoService.translate('MoreThan'), value: 'gte' },
      { label: this.translocoService.translate('LessThan'), value: 'lte' },
    ];

    if (this.hasLazyLoad === false) {
      this.clientLoad();
    }
  }
  
  lazyLoad(event: TableLazyLoadEvent) {
    this.lastLazyLoadEvent = event;

    let tableFilter: TableFilter = event as unknown as TableFilter;
    tableFilter.additionalFilterIdLong = this.additionalFilterIdLong;

    this.onLazyLoad.next(tableFilter);
    
    this.getTableDataObservableMethod(tableFilter).subscribe({
      next: async (res) => { 
        this.items = res.data;
        this.totalRecords = res.totalRecords;
        this.onTotalRecordsChange.next(res.totalRecords);
        
        if (this.selectedLazyLoadObservableMethod != null) {
          let selectedRowsMethodResult: LazyLoadSelectedIdsResult = await firstValueFrom(this.selectedLazyLoadObservableMethod(tableFilter));
  
          this.currentPageSelectedItemsFromDb = [...selectedRowsMethodResult.selectedIds];

          if (this.isFirstTimeLazyLoad == true) {
            this.rowsSelectedNumber = selectedRowsMethodResult.totalRecordsSelected;
            this.setFakeIsAllSelected();
            this.isFirstTimeLazyLoad = false;
          }
  
          if (this.isAllSelected == true) {
            let idsToInsert = [...this.items.map(x => x.id)];
            idsToInsert = idsToInsert.filter(x => this.unselectedItems.includes(x) == false);
            this.fakeSelectedItems = [...idsToInsert]; // FT: Only for showing checkboxes, we will not send this to the backend
          }
          else if (this.isAllSelected == false) {
            this.fakeSelectedItems = [...this.newlySelectedItems]; // FT: Only for showing checkboxes, we will not send this to the backend
          }
          else if (this.isAllSelected == null) {
            let idsToInsert = [...selectedRowsMethodResult.selectedIds, ...this.newlySelectedItems];
            idsToInsert = idsToInsert.filter(x => this.unselectedItems.includes(x) == false);
            this.fakeSelectedItems = [...idsToInsert];
          }
        }

        this.loading = false;
      },
      error: () => {
        this.loading = false;
      },
    });
  }

  clientLoad(){
    this.loading = false;

    this.loadFormArrayItems();
    this.totalRecords = this.items.length;
    this.onTotalRecordsChange.next(this.items.length);

    if (this.getAlreadySelectedItemIds) {
      this.selectedItemIds = this.getAlreadySelectedItemIds(this.additionalIndexes);
    }
    if (this.getAlreadySelectedItems) {
      this.selectedItems = this.getAlreadySelectedItems(this.additionalIndexes);
    }
    this.rowsSelectedNumber = this.selectedItemIds.length;
    this.setFakeIsAllSelected();
  }

  private clientFilterCount = 0;

  filter(event: TableFilterEvent){
    if (this.hasLazyLoad && this.selectionMode === 'multiple')
      this.selectAll(false); // FT: We need to do it like this because: totalRecords: 1 -> selectedRecords from earlyer selection 2 -> unselect current -> all checkbox is set to true

    if (this.hasLazyLoad === false && this.selectionMode === 'multiple') {
      if (this.clientFilterCount === 0) {
        this.loadFormArrayItems();
        this.clientFilterCount++;
      }else{
        this.clientFilterCount--;
      }
    }
  }

  private loadFormArrayItems(){
    this.items = this.getFormArrayItems(this.additionalIndexes);
    this.items.forEach((item, index) => {
      item.index = index;
    });
  }
  
  getColHeaderWidth(filterType: string) {
    switch (filterType) {
      case 'text':
        return 'min-width: 12rem;';
      case 'date':
        return 'min-width: 10rem;';
      case 'multiselect':
        return 'min-width: 12rem;';
      case 'boolean':
        return 'min-width: 8rem;';
      case 'numeric':
        return 'min-width: 12rem;';
      default:
        return 'width: 0rem;'; // fitting content of the row like this
    }
  }

  getColMatchModeOptions(filterType: string){
    switch (filterType) {
        case 'text':
          return null;
        case 'date':
          return this.matchModeDateOptions;
        case 'multiselect':
          return null;
        case 'boolean':
          return null;
        case 'numeric':
          return this.matchModeNumberOptions;
        default:
          return null;
      }
  }
  
  getColMatchMode(filterType: string){
    switch (filterType) {
        case 'text':
          return 'contains';
        case 'date':
          return null;
        case 'multiselect':
          return 'in';
        case 'boolean':
          return 'equals';
        case 'numeric':
          return null;
        default:
          return null;
      }
  }

  isDropOrMulti(filterType: string){
    if (filterType == 'dropdown' || filterType == 'multiselect') {
        return true;
    } 
    else {
        return false;
    }
  }

  navigateToDetails(rowId: number){
    this.router.navigate([rowId], {relativeTo: this.route});
  }

  deleteObject(rowId: number){
    this.deleteRef = this.dialogService.open(SpiderDeleteConfirmationComponent, 
      { 
        header: this.translocoService.translate('AreYouSureToDelete'),
        width: '400px',
        data:{ deleteItemFromTableObservableMethod: this.deleteItemFromTableObservableMethod, id: rowId, } 
      });

      this.deleteRef.onClose.subscribe((deletedSuccessfully: boolean)=>{
        if(deletedSuccessfully === true){
          this.messageService.successMessage(this.translocoService.translate('SuccessfullyDeletedMessage'));
          this.reloadTable();
        }
      });
  }

  reloadTable(){
    this.loading = true;
    this.items = null;
    this.lazyLoad(this.lastLazyLoadEvent);
  }

  showActions(): boolean {
    return this.cols.some(x => x.actions?.length > 0);
  }

  getStyleForBodyColumn(col: Column<any>) {
    switch(col.filterType){
      case 'numeric':
        return 'text-align: right;';
      default:
        return null;
    }
  }

  getClassForAction(action: Action): string{
    switch(action.field){
      case 'Details':
        return 'pi pi-pencil text-lg cursor-pointer primary-color';
      case 'Delete':
        return 'pi pi-trash text-lg text-red-500 cursor-pointer';
      default:
        return `pi ${action.icon} text-lg cursor-pointer`;
    }
  }

  getMethodForAction(action: Action, rowData: any){
    switch(action.field){
      case 'Details':
        return this.navigateToDetails(rowData.id);
      case 'Delete':
        return this.deleteObject(rowData.id);
      default:
        return action.onClick();
    }
  }

  getRowData(rowData: any, col: Column): string{
      switch (col.filterType) {
        case 'text':
          return rowData[col.field];
        case 'date':
          if (rowData[col.field] == null)
            return null;
          
          if (col.showTime)
            return formatDate(rowData[col.field], 'dd.MM.yyyy. HH:mm', this.locale);
          else
            return formatDate(rowData[col.field], 'dd.MM.yyyy.', this.locale);
        case 'multiselect':
          return rowData[col.field];
        case 'boolean':
          return rowData[col.field] == true ? this.translocoService.translate('Yes') : this.translocoService.translate('No');
        case 'numeric':
          // TODO FT: make decimal pipe
          return rowData[col.field];
        default:
          return null;
      }
  }

  colTrackByFn(index, item){
    return item.field;
  }

  actionTrackByFn(index, item: Action){
    return `${index}${item.field}`
  }

  exportListToExcel() {
    let tableFilter: TableFilter = this.lastLazyLoadEvent as unknown as TableFilter;
    tableFilter.additionalFilterIdLong = this.additionalFilterIdLong;

    exportListToExcel(this.exportTableDataToExcelObservableMethod, tableFilter);
  }

  clear(table: Table) {
    table.clear();
  }

  //#region Selection

  setFakeIsAllSelected(){
    if(this.rowsSelectedNumber == this.totalRecords)
      this.fakeIsAllSelected = true;
    else
      this.fakeIsAllSelected = false;
  }

  selectAll(checked: boolean){
    this.unselectedItems.length = 0;
    this.newlySelectedItems.length = 0;

    if (checked == true) {
      this.isAllSelected = true;
      this.fakeIsAllSelected = true;
      this.onIsAllSelectedChange.next(new AllClickEvent({checked: true, additionalIndexes: this.additionalIndexes}));
      this.rowsSelectedNumber = this.totalRecords;
      this.fakeSelectedItems = [...this.items.map(x => x.id)];
      this.selectedItemIds = [...this.items.map(x => x.id)]
    }
    else{
      this.isAllSelected = false;
      this.fakeIsAllSelected = false;
      this.onIsAllSelectedChange.next(new AllClickEvent({checked: false, additionalIndexes: this.additionalIndexes}));
      this.rowsSelectedNumber = 0;
      this.fakeSelectedItems = [];
      this.selectedItemIds = [];
    }
  }

  selectRow(id: number, index: number) {
    if (this.isRowSelected(id)) {
      this.rowUnselect(id);
      this.onRowUnselect.next(new RowClickEvent({ index: index, id: id, additionalIndexes: this.additionalIndexes }));
    } else {
      this.rowSelect(id);
      this.onRowSelect.next(new RowClickEvent({ index: index, id: id, additionalIndexes: this.additionalIndexes }));
    }
  }

  isRowSelected(id: number){
    if (this.hasLazyLoad){
      return this.fakeSelectedItems.find(x => x === id) != undefined;
    }
    else {
      return this.selectedItemIds.find(x => x === id) != undefined;
    }
  }

  rowSelect(id: number){
    if (this.isAllSelected == false || this.currentPageSelectedItemsFromDb.includes(id) == false) {
      this.newlySelectedItems.push(id);
    }

    if (this.hasLazyLoad){
      this.fakeSelectedItems.push(id);
    }
    else {
      this.selectedItemIds.push(id);
    }
    
    this.rowsSelectedNumber++;

    const index = this.unselectedItems.indexOf(id);
    if (index !== -1) {
      this.unselectedItems.splice(index, 1); // FT: Splice is mutating the array
    }

    this.setFakeIsAllSelected();
  }
  
  rowUnselect(id: number) {
    if (this.isAllSelected == true || this.currentPageSelectedItemsFromDb.includes(id) == true) {
      this.unselectedItems.push(id);
    }

    this.rowsSelectedNumber--;

    const index = this.newlySelectedItems.indexOf(id);
    const fakeIndex = this.fakeSelectedItems.indexOf(id);
    const nonLazyLoadIndex = this.selectedItemIds.indexOf(id);

    if (index !== -1) {
      this.newlySelectedItems.splice(index, 1); // FT: Splice is mutating the array
    }
    if (fakeIndex !== -1) {
      this.fakeSelectedItems.splice(fakeIndex, 1); // FT: Splice is mutating the array
    }
    if (nonLazyLoadIndex !== -1) {
      this.selectedItemIds.splice(nonLazyLoadIndex, 1); // FT: Splice is mutating the array
    }

    this.setFakeIsAllSelected();
  }
  //#endregion

  //#region Client side table

  // FT: Can do it with Id also, because we are never adding the new record in the table at the same page.
  getFormArrayControlByIndex(formControlName: string, index: number): SpiderFormControl{
    if (this.getFormControl) {
      return this.getFormControl(formControlName, index, this.additionalIndexes);
    }
    else{
      return null;
    }
  }

  //#endregion
}

export class Action {
  name?: string;
  field?: string;
  icon?: string;
  onClick?: () => void;

  constructor(
    {
      name,
      field,
      icon,
      onClick,
    }:{
      name?: string;
      field?: string;
      icon?: string;
      onClick?: () => void;
    } = {}
    ) {
      this.name = name;
      this.field = field;
      this.icon = icon;
      this.onClick = onClick;
  }
}

export class Column<T = any> {
  name: string;
  field?: string & keyof T;
  filterField?: string & keyof T; // FT: Made specificaly for multiautocomplete, maybe for something more in the future
  filterType?: 'text' | 'date' | 'multiselect' | 'boolean' | 'numeric';
  filterPlaceholder?: string;
  showMatchModes?: boolean;
  showAddButton?: boolean;
  dropdownOrMultiselectValues?: PrimengOption[];
  actions?: Action[];
  editable?: boolean;
  showTime?: boolean;

  constructor(
    {
      name,
      field,
      filterField,
      filterType,
      filterPlaceholder,
      showMatchModes,
      showAddButton,
      dropdownOrMultiselectValues,
      actions,
      editable,
      showTime,
    }:{
      name?: string;
      field?: string & keyof T;
      filterField?: string & keyof T; // FT: Made specificaly for multiautocomplete, maybe for something more in the future;
      filterType?: 'text' | 'date' | 'multiselect' | 'boolean' | 'numeric';
      filterPlaceholder?: string;
      showMatchModes?: boolean;
      showAddButton?: boolean;
      dropdownOrMultiselectValues?: PrimengOption[];
      actions?: Action[];
      editable?: boolean;
      showTime?: boolean;
    } = {}
    ) {
      this.name = name;
      this.field = field;
      this.filterField = filterField;
      this.filterType = filterType;
      this.filterPlaceholder = filterPlaceholder;
      this.showMatchModes = showMatchModes;
      this.showAddButton = showAddButton;
      this.dropdownOrMultiselectValues = dropdownOrMultiselectValues;
      this.actions = actions;
      this.editable = editable;
      this.showTime = showTime;
  }
}

export class RowClickEvent {
  index?: number;
  id?: number;
  additionalIndexes?: any;

  constructor(
    {
      index, 
      id, 
      additionalIndexes
    }:{
      index?: number; 
      id?: number; 
      additionalIndexes?: any;
    } = {}
    ) {
    this.index = index;
    this.id = id;
    this.additionalIndexes = additionalIndexes;
  }
}

export class AllClickEvent {
  checked?: boolean;
  additionalIndexes?: any;

  constructor(
    {
      checked, 
      additionalIndexes
    }:{
      checked?: boolean; 
      additionalIndexes?: any;
    } = {}
    ) {
    this.checked = checked;
    this.additionalIndexes = additionalIndexes;
  }
}