import {
    Component,
    EventEmitter,
    Input,
    Output,
  } from '@angular/core';
import { BaseControl } from './base-control';
import { PrimengOption } from '../entities/primeng-option';

  @Component({
    selector: 'base-dropdown-control',
    template: '',
    styles: [],
  })
  export class BaseDropdownControl extends BaseControl {
    @Input() options: PrimengOption[];
    @Input() showMoreOptions: boolean = false;
    @Input() moreOptionsIcon: string = 'pi-ellipsis-h';
    @Output() onMoreOptionsClick: EventEmitter<null> = new EventEmitter();
    
    dropdownMarkAsDirty(){
      this.control.markAsDirty();
    }

    moreOptionsClick(){
      this.onMoreOptionsClick.next(null);
    }
  }