import { BaseFormService } from './../../services/base-form.service';
import {
  ChangeDetectorRef,
  Component,
  KeyValueDiffer,
  KeyValueDiffers,
  OnInit,
} from '@angular/core';
import { FormGroup } from '@angular/forms';
import { SpiderFormArray, SpiderFormControl, SpiderFormGroup } from '../spider-form-control/spider-form-control';
import { HttpClient } from '@angular/common/http';
import { SpiderMessageService } from '../../services/spider-message.service';
import { ActivatedRoute, Router } from '@angular/router';
import { MenuItem } from 'primeng/api';
import { getControl, getParentUrl, singleOrDefault } from '../../services/helper-functions';
import { TranslocoService } from '@jsverse/transloco';
import { BaseEntity } from '../../entities/base-entity';
import { SpiderTab } from '../spider-panels/panel-header/panel-header.component';
import { LastMenuIconIndexClicked } from '../../entities/last-menu-icon-index-clicked';

@Component({
  selector: 'base-form',
  template: '',
  styles: [],
})
export class BaseFormCopy implements OnInit { 
  formGroup: SpiderFormGroup = new SpiderFormGroup({});
  saveBody: any;
  invalidForm: boolean = false; // FT: We are using this only if we manualy add some form field on the UI, like multiautocomplete, autocomplete etc...
  loading: boolean = true;

  private modelDiffer: KeyValueDiffer<string, any>;

  constructor(
    protected differs: KeyValueDiffers, 
    protected http: HttpClient, 
    protected messageService: SpiderMessageService, 
    protected changeDetectorRef: ChangeDetectorRef,
    protected router: Router, 
    protected route: ActivatedRoute,
    protected translocoService: TranslocoService,
    protected baseFormService: BaseFormService,
  ) {
  }

  ngOnInit(){
  }

  //#region Model

  initFormGroup<T>(
    formGroup: SpiderFormGroup<T>, 
    parentFormGroup: SpiderFormGroup, 
    modelConstructor: any, 
    propertyNameInSaveBody: string,
    updateOnChangeControls?: (keyof T)[])
  {
    return this.baseFormService.initFormGroup(
      formGroup, parentFormGroup, modelConstructor, propertyNameInSaveBody, updateOnChangeControls
    );
  }

  createFormGroup<T>(
    formGroup: SpiderFormGroup<T>, 
    modelConstructor: T & BaseEntity, 
    updateOnChangeControls?: (keyof T)[]
  ) {
    return this.baseFormService.createFormGroup(
      formGroup, modelConstructor, updateOnChangeControls
    );
  }

  control<T extends BaseEntity>(formControlName: string & keyof T, formGroup: SpiderFormGroup<T>) {
    return getControl(formControlName, formGroup);
  }

  onSave = (reroute: boolean = true) => {
    this.saveBody = this.formGroup.initSaveBody();
    this.onBeforeSave(this.saveBody);

    this.saveBody = this.saveBody ?? this.formGroup.getRawValue();

    let isValid: boolean = this.areFormGroupsValid();
    let isFormArrayValid: boolean = this.areFormArraysValid();

    if(isValid && isFormArrayValid){
      this.formGroup.saveObservableMethod(this.saveBody).subscribe(res => {
        this.messageService.successMessage(this.translocoService.translate('SuccessfulSaveToastDescription'));

        Object.keys(res).forEach((key) => {
          const formControl = this.formGroup.get(key);
          
          if (formControl) {
            if (formControl instanceof SpiderFormArray) {
              const formArray = formControl as SpiderFormArray;
              if (res[key].length !== 0) {
                formArray.clear();
              }
              else{
                // FT: This is okay because when we have M2M association with additional fields, we will not give back the list because we are not checking version on the server.
                // console.error(`You returned empty array for control: ${formArray.translationKey}.`);
              }

              res[key].forEach((model: any) => {
                if (typeof model === 'object' && model !== null) {
                  Object.assign(formArray.modelConstructor, model);
                  let helperFormGroup = new SpiderFormGroup({});
                  this.baseFormService.createFormGroup(helperFormGroup, formArray.modelConstructor)
                  formArray.push(helperFormGroup);
                } else {
                  console.error('Can not add primitive form control inside form array.');
                }
              });

            } else {
              formControl.patchValue(res[key]);
            }
          }else{
            // FT: It's okay to do this.
            // console.error('You returned something that is not in the save DTO.');
          }
        });

        if (reroute) {
          const savedObjectId = (res as any)[this.formGroup.mainDTOName]?.id;
          this.rerouteToSavedObject(savedObjectId); // You always need to have id, because of id == 0 and version change
        }
        
        this.onAfterSave();
      });
      
      this.onAfterSaveRequest();
    }else{
      this.baseFormService.showInvalidFieldsMessage();
    }
  }

  rerouteToSavedObject(rerouteId: number | string): void {
    if(rerouteId == null){
      // console.error('You do not have rerouteId in your DTO.')
      const currentUrl = this.router.url;
      const parentUrl: string = getParentUrl(currentUrl);
      this.router.navigateByUrl(parentUrl);
      return;
    }
      
    const segments = this.router.url.split('/');
    segments[segments.length - 1] = rerouteId.toString();

    const newUrl = segments.join('/');
    this.router.navigateByUrl(newUrl);
  }

  onBeforeSave = (saveBody?: any) => {}
  onAfterSave = () => {}
  onAfterSaveRequest = () => {}

  areFormGroupsValid(): boolean {
    if(this.formGroup.controls == null)
      return true;

    let invalid: boolean = false;

    Object.keys(this.formGroup.controls).forEach(key => {
      const formGroupOrControl = this.formGroup.controls[key];

      if (formGroupOrControl instanceof SpiderFormGroup){
        Object.keys(formGroupOrControl.controls).forEach(key => {
          const formControl = formGroupOrControl.controls[key] as SpiderFormControl; // this.formArray.markAsDirty(); // FT: For some reason this doesnt work

          if (formGroupOrControl.controlNamesFromHtml.includes(formControl.label) && formControl.invalid) {
            formControl.markAsDirty();
            invalid = true;
          }
        });
      }
      else if (formGroupOrControl instanceof SpiderFormControl){
        if (formGroupOrControl.invalid) {
          formGroupOrControl.markAsDirty();
          invalid = true;
        }
      }

    });

    if (invalid) {
      return false;
    }

    return true;
  }

  // FT: If you want to call single method
  checkFormGroupValidity(){
    if (this.formGroup.invalid || this.invalidForm) {
      Object.keys(this.formGroup.controls).forEach(key => {
        this.formGroup.controls[key].markAsDirty(); // this.formGroup.markAsDirty(); // FT: For some reason this doesnt work
      });

      this.baseFormService.showInvalidFieldsMessage();

      return false;
    }
    
    return true;
  }

  //#endregion

  //#region Model List
  
  getFormArrayControlByIndex<T>(formControlName: keyof T & string, formArray: SpiderFormArray<T>, index: number, filter?: (formGroups: SpiderFormGroup<T>[]) => SpiderFormGroup<T>[]): SpiderFormControl {
    if(formArray.controlNamesFromHtml.findIndex(x => x === formControlName) === -1)
      formArray.controlNamesFromHtml.push(formControlName);

    let filteredFormGroups: SpiderFormGroup<T>[];

    if (filter) {
      filteredFormGroups = filter(formArray.controls as SpiderFormGroup<T>[]);
    }
    else{
      return (formArray.controls[index] as SpiderFormGroup<T>).controls[formControlName] as SpiderFormControl;
    }

    return filteredFormGroups[index]?.controls[formControlName] as SpiderFormControl; // FT: Don't change this. It's always possible that change detection occurs before something.
  }

  getFormArrayControls<T>(formControlName: keyof T & string, formArray: SpiderFormArray<T>, filter?: (formGroups: SpiderFormGroup<T>[]) => SpiderFormGroup<T>[]): SpiderFormControl[] {
    if(formArray.controlNamesFromHtml.findIndex(x => x === formControlName) === -1)
      formArray.controlNamesFromHtml.push(formControlName);

    let filteredFormGroups: SpiderFormGroup<T>[];

    if (filter) {
      filteredFormGroups = filter(formArray.controls as SpiderFormGroup<T>[]);
    }
    else{
      return (formArray.controls as SpiderFormGroup<T>[]).map(x => x.controls[formControlName] as SpiderFormControl);
    }

    return filteredFormGroups.map(x => x.controls[formControlName] as SpiderFormControl);
  }

  getFormArrayGroups<T>(formArray: SpiderFormArray<T>): SpiderFormGroup<T>[]{
    return this.baseFormService.getFormArrayGroups(formArray);
  }

  addNewFormGroupToFormArray<T>(
    formArray: SpiderFormArray<T>, 
    modelConstructor: T & BaseEntity,
    index: number,
  ) : SpiderFormGroup {
    return this.baseFormService.addNewFormGroupToFormArray(formArray, modelConstructor, index)
  }

  initFormArray<T>(
    parentFormGroup: SpiderFormGroup, 
    modelList: (T & BaseEntity)[], 
    modelConstructor: T & BaseEntity, 
    formArraySaveBodyName: string, 
    formArrayTranslationKey: string, 
    required: boolean = false)
  {
    return this.baseFormService.initFormArray<T>(
      parentFormGroup, modelList, modelConstructor, formArraySaveBodyName, formArrayTranslationKey, required
    );
  }

  removeFormControlFromTheFormArray(formArray: SpiderFormArray, index: number) {
    if(index == null)
      throw new Error('Can not pass null index.');

    formArray.removeAt(index);
  }

  removeFormControlsFromTheFormArray(formArray: SpiderFormArray, indexes: number[]) {
    // Sort indexes in descending order to avoid index shifts when removing controls
    const sortedIndexes = indexes.sort((a, b) => b - a);

    sortedIndexes.forEach(index => {
      if (index >= 0 && index < formArray.length) {
        formArray.removeAt(index);
      }
    });
  }

  areFormArraysValid(): boolean {
    if(this.formGroup.controls == null)
      return true;

    let invalid: boolean = false;

    Object.keys(this.formGroup.controls).forEach(key => {
      const formArray = this.formGroup.controls[key] as unknown as SpiderFormArray;
      
      if (formArray instanceof SpiderFormArray){
        (formArray.controls as SpiderFormGroup[]).forEach(formGroup => {
          Object.keys(formGroup.controls).forEach(key => {
            const formControl = formGroup.controls[key] as SpiderFormControl; // this.formArray.markAsDirty(); // FT: For some reason this doesn't work

            if (
              (formGroup.controlNamesFromHtml.includes(formControl.label) || formArray.controlNamesFromHtml.includes(formControl.label)) && 
              formControl.invalid
            ) {
              formControl.markAsDirty();
              invalid = true;
            }
          });
        });

        if (formArray.required == true && formArray.length == 0) {
          invalid = true;
          this.messageService.warningMessage(this.translocoService.translate('ListCanNotBeEmpty'));
        }
      }
    });

    if (invalid || this.invalidForm) {
      return false;
    }

    return true;
  }

  onBeforeSaveList(){}
  onAfterSaveList(){}
  onAfterSaveListRequest(){}

  // FT: Sending LastMenuIconIndexClicked class because of reference type
  getCrudMenuForOrderedData = (
    formArray: SpiderFormArray, 
    modelConstructor: BaseEntity, 
    lastMenuIconIndexClicked: LastMenuIconIndexClicked, 
    adjustFormArrayManually: boolean = false
  ): MenuItem[] => {
    let crudMenuForOrderedData: MenuItem[] = [
        {label: this.translocoService.translate('Remove'), icon: 'pi pi-minus', command: () => {
          this.onBeforeRemove(formArray, modelConstructor, lastMenuIconIndexClicked.index);
          if (adjustFormArrayManually === false) {
            this.removeFormControlFromTheFormArray(formArray, lastMenuIconIndexClicked.index);
          }
        }},
        {label: this.translocoService.translate('AddAbove'), icon: 'pi pi-arrow-up', command: () => {
          this.onBeforeAddAbove(formArray, modelConstructor, lastMenuIconIndexClicked.index);
          if (adjustFormArrayManually === false) {
            this.baseFormService.addNewFormGroupToFormArray(
              formArray, modelConstructor, lastMenuIconIndexClicked.index
            );
          }
        }},
        {label: this.translocoService.translate('AddBelow'), icon: 'pi pi-arrow-down', command: () => {
          this.onBeforeAddBelow(formArray, modelConstructor, lastMenuIconIndexClicked.index);
          if (adjustFormArrayManually === false) {
            this.baseFormService.addNewFormGroupToFormArray(
              formArray, modelConstructor, lastMenuIconIndexClicked.index + 1
            );
          }
        }},
    ];

    return crudMenuForOrderedData;
  }

  onBeforeRemove = (formArray: SpiderFormArray, modelConstructor: any, lastMenuIconIndexClicked: number) => {}

  onBeforeAddAbove = (formArray: SpiderFormArray, modelConstructor: any, lastMenuIconIndexClicked: number) => {}

  onBeforeAddBelow = (formArray: SpiderFormArray, modelConstructor: any, lastMenuIconIndexClicked: number) => {}

  //#endregion

  //#region Helpers

  selectedTab(tabs: SpiderTab[]): number {
    const tab = singleOrDefault(tabs, x => x.isSelected);

    if (tab) {
      return tab.id;
    }
    else{
      return null;
    }
  }

  //#endregion

}
