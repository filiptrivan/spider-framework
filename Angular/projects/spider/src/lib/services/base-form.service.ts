import { Injectable } from '@angular/core';
import { SpiderFormArray, SpiderFormControl, SpiderFormGroup } from '../components/spider-form-control/spider-form-control';
import { BaseEntity } from '../entities/base-entity';
import { TranslateLabelsAbstractService } from './translate-labels-abstract.service';
import { ValidatorAbstractService } from './validator-abstract.service';

@Injectable({
  providedIn: 'root',
})
export class BaseFormService {
  constructor(
    private translateLabelsService: TranslateLabelsAbstractService,
    private validatorService: ValidatorAbstractService,
  ) {}

  initFormGroup<T>(
    formGroup: SpiderFormGroup<T>, 
    parentFormGroup: SpiderFormGroup, 
    modelConstructor: any, 
    propertyNameInSaveBody: string,
    updateOnChangeControls?: (keyof T)[])
  {
    if (modelConstructor == null)
      return null;

    if (formGroup == null)
      console.error('FT: You need to instantiate the form group.')

    this.createFormGroup(formGroup, modelConstructor, updateOnChangeControls);
    parentFormGroup.addControl(propertyNameInSaveBody, formGroup);

    return formGroup;
  }

  createFormGroup<T>(
    formGroup: SpiderFormGroup<T>, 
    modelConstructor: T & BaseEntity, 
    updateOnChangeControls?: (keyof T)[])
  {
    if (formGroup == null)
      console.error('FT: You need to instantiate the form group.')

    Object.keys(modelConstructor).forEach((formControlName) => {
      let formControl: SpiderFormControl;
      
      const formControlValue = modelConstructor[formControlName];
      
      if (updateOnChangeControls?.includes(formControlName as keyof T) ||
        (formControlName.endsWith('Id') && formControlName.length > 2)
      ){
        formControl = new SpiderFormControl(formControlValue, { updateOn: 'change' });
      }
      else{
        formControl = new SpiderFormControl(formControlValue, { updateOn: 'blur' });
      }

      formControl.label = formControlName;
      formControl.labelForDisplay = this.getTranslatedLabel(formControlName);

      formGroup.addControl(formControlName, formControl);

      this.validatorService.setValidator(formControl, modelConstructor.typeName);
    });

    return formGroup;
  }

  getTranslatedLabel(formControlName: string): string {
    if (formControlName.endsWith('Id') && formControlName.length > 2) {
      formControlName = formControlName.substring(0, formControlName.length - 2);
    } 
    else if (formControlName.endsWith('DisplayName')) {
      formControlName = formControlName.replace('DisplayName', '');
    } 

    return this.translateLabelsService.translate(formControlName);
  }

  getFormArrayGroups<T>(formArray: SpiderFormArray<T>): SpiderFormGroup<T>[]{
    return formArray.controls as SpiderFormGroup<T>[]
  }

  addNewFormGroupToFormArray<T>(
    formArray: SpiderFormArray<T>, 
    modelConstructor: T & BaseEntity,
    index: number,
  ) : SpiderFormGroup {
    let helperFormGroup = new SpiderFormGroup({});
    this.createFormGroup(helperFormGroup, modelConstructor);
    
    if (index == null) {
      formArray.push(helperFormGroup);
    }else{
      formArray.insert(index, helperFormGroup);
    }

    return helperFormGroup;
  }

  initFormArray<T>(
    parentFormGroup: SpiderFormGroup, 
    modelList: (T & BaseEntity)[], 
    modelConstructor: T & BaseEntity, 
    formArraySaveBodyName: string, 
    formArrayTranslationKey: string, 
    required: boolean = false)
  {
    if (modelList == null)
      return null;

    let formArray = new SpiderFormArray<T>([]);
    formArray.required = required;
    formArray.modelConstructor = modelConstructor;
    formArray.translationKey = formArrayTranslationKey;

    modelList.forEach(model => {
      Object.assign(modelConstructor, model);
      let helperFormGroup: SpiderFormGroup = new SpiderFormGroup({});
      this.createFormGroup(helperFormGroup, formArray.modelConstructor);
      formArray.push(helperFormGroup);
    });

    parentFormGroup.addControl(formArraySaveBodyName, formArray);

    return formArray;
  }

  disableAllFormControls<T>(formArray: SpiderFormArray<T>){
    formArray.controls.forEach((segmentationItemFormGroup: SpiderFormGroup) => {
        Object.keys(segmentationItemFormGroup.controls).forEach(key => {
            segmentationItemFormGroup.controls[key].disable();
        });
    });
  }

  enableAllFormControls<T>(formArray: SpiderFormArray<T>){
    formArray.controls.forEach((segmentationItemFormGroup: SpiderFormGroup) => {
        Object.keys(segmentationItemFormGroup.controls).forEach(key => {
            segmentationItemFormGroup.controls[key].enable();
        });
    });
  }

  generateNewNegativeId<T extends BaseEntity>(formArray: SpiderFormArray<T>){
    return -formArray.getRawValue().filter(x => x.id < 0).length - 1;
  }

}
