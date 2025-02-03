import { NgModule } from '@angular/core';
import { SpiderPanelsModule } from '../components/spider-panels/spider-panels.module';
import { SpiderReturnButtonComponent } from '../components/spider-buttons/spider-return-button.component';
import { SpiderMultiAutocompleteComponent } from './spider-multiautocomplete/spider-multiautocomplete.component';
import { SpiderPasswordComponent } from './spider-password/spider-password.component';
import { SpiderTextboxComponent } from './spider-textbox/spider-textbox.component';
import { SpiderCheckboxComponent } from './spider-checkbox/spider-checkbox.component';
import { SpiderMultiselectComponent } from './spider-multiselect/spider-multiselect.component';
import { SpiderTextareaComponent } from './spider-textarea/spider-textarea.component';
import { SpiderNumberComponent } from './spider-number/spider-number.component';
import { SpiderDropdownComponent } from './spider-dropdown/spider-dropdown.component';
import { SpiderEditorComponent } from './spider-editor/spider-editor.component';
import { SpiderColorpickComponent } from './spider-colorpick/spider-colorpick.component';
import { SpiderFileComponent } from './spider-file/spider-file.component';
import { SpiderCalendarComponent } from './spider-calendar/spider-calendar.component';
import { SpiderAutocompleteComponent } from './spider-autocomplete/spider-autocomplete.component';
import { SpiderButtonComponent } from '../components/spider-buttons/spider-button.component';

@NgModule({
  imports: [
    SpiderTextboxComponent,
    SpiderTextareaComponent,
    SpiderCheckboxComponent,
    SpiderCalendarComponent,
    SpiderReturnButtonComponent,
    SpiderButtonComponent,
    SpiderPanelsModule,
    SpiderPasswordComponent,
    SpiderAutocompleteComponent,
    SpiderMultiAutocompleteComponent,
    SpiderMultiselectComponent,
    SpiderNumberComponent,
    SpiderDropdownComponent,
    SpiderEditorComponent,
    SpiderColorpickComponent,
    SpiderFileComponent,
  ],
  exports: [
    SpiderTextboxComponent,
    SpiderTextareaComponent,
    SpiderCheckboxComponent,
    SpiderCalendarComponent,
    SpiderReturnButtonComponent,
    SpiderButtonComponent,
    SpiderPanelsModule,
    SpiderPasswordComponent,
    SpiderAutocompleteComponent,
    SpiderMultiAutocompleteComponent,
    SpiderMultiselectComponent,
    SpiderNumberComponent,
    SpiderDropdownComponent,
    SpiderEditorComponent,
    SpiderColorpickComponent,
    SpiderFileComponent
  ],
  declarations: [
  ],
  providers: [
  ]
})
export class SpiderControlsModule {}