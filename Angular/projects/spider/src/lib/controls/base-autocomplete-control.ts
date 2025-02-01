import {
    Component,
    EventEmitter,
    Output,
  } from '@angular/core';
import { AutoCompleteCompleteEvent } from 'primeng/autocomplete';
import { BaseDropdownControl } from './base-dropdown-control';

  @Component({
    selector: 'base-autocomplete-control',
    template: '',
    styles: [],
  })
  export class BaseAutocompleteControl extends BaseDropdownControl {
    @Output() onTextInput: EventEmitter<AutoCompleteCompleteEvent> = new EventEmitter();
  }