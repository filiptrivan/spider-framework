import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { Component, EventEmitter, Input, Output } from '@angular/core';
import { TranslocoDirective } from '@jsverse/transloco';
import { ActivatedRoute } from '@angular/router';
import { combineLatest, forkJoin, map, Observable, of, Subscription } from 'rxjs';
import { MenuItem } from 'primeng/api';
import { PrimengModule } from '../../modules/primeng.module';
import { SpiderControlsModule } from '../../controls/spider-controls.module';
import { CardSkeletonComponent } from '../card-skeleton/card-skeleton.component';
import { SpiderFormArray, SpiderFormControl, SpiderFormGroup } from '../spider-form-control/spider-form-control';
import { BaseEntity } from '../../entities/base-entity';
import { LastMenuIconIndexClicked } from '../../entities/last-menu-icon-index-clicked';
import { Role, RoleSaveBody } from '../../entities/security-entities';
import { SpiderButton } from '../../entities/spider-button';
import { getControl, getPrimengAutocompleteNamebookOptions, getPrimengDropdownNamebookOptions, nameof } from '../../services/helper-functions';
import { BaseFormService } from '../../services/base-form.service';
import { ApiSecurityService } from '../../services/api.service.security';
import { PrimengOption } from '../../entities/primeng-option';
import { AutoCompleteCompleteEvent } from 'primeng/autocomplete';
import { IsAuthorizedForSaveEvent } from '../../entities/is-authorized-for-save-event';
import { AuthBaseService } from '../../services/auth-base.service';

@Component({
    selector: 'role-base-details',
    template:`
<ng-container *transloco="let t">
    <spider-panel [isFirstMultiplePanel]="isFirstMultiplePanel" [isMiddleMultiplePanel]="isMiddleMultiplePanel" [isLastMultiplePanel]="isLastMultiplePanel">
        <panel-header></panel-header>

        <panel-body>
            @defer (when loading === false) {
                <form class="grid">
                    <div class="col-12">
                        <spider-textbox [control]="control('name', roleFormGroup)"></spider-textbox>
                    </div>
                    <div class="col-12">
                        <spider-textarea [control]="control('description', roleFormGroup)"></spider-textarea>
                    </div>
                    <div class="col-12">
                        <spider-multiautocomplete [control]="selectedUsersForRole" [options]="usersForRoleOptions" (onTextInput)="searchUsersForRole($event)" [label]="t('Users')"></spider-multiautocomplete>
                    </div>
                    <div class="col-12">
                        <spider-multiselect [control]="selectedPermissionsForRole" [options]="permissionsForRoleOptions" [label]="t('Permissions')"></spider-multiselect>
                    </div>
                </form>
            } @placeholder {
                <card-skeleton [height]="502"></card-skeleton>
            }
        </panel-body>

        <panel-footer>
        <spider-button [disabled]="!isAuthorizedForSave" (onClick)="save()" [label]="t('Save')" icon="pi pi-save"></spider-button>
            @for (button of additionalButtons; track button.label) {
                <spider-button (onClick)="button.onClick()" [disabled]="button.disabled" [label]="button.label" [icon]="button.icon"></spider-button>
            }
            <spider-return-button></spider-return-button>
        </panel-footer>
    </spider-panel>
</ng-container>
    `,
    standalone: true,
    imports: [
        CommonModule, 
        FormsModule,
        ReactiveFormsModule,
        PrimengModule,
        SpiderControlsModule,
        TranslocoDirective,
        CardSkeletonComponent,
    ]
})
export class RoleBaseDetailsComponent {
    @Output() onSave = new EventEmitter<void>();
    @Output() onRoleFormGroupInitFinish = new EventEmitter<void>();
    @Input() getCrudMenuForOrderedData: (formArray: SpiderFormArray, modelConstructor: BaseEntity, lastMenuIconIndexClicked: LastMenuIconIndexClicked, adjustFormArrayManually: boolean) => MenuItem[];
    @Input() formGroup: SpiderFormGroup;
    @Input() roleFormGroup: SpiderFormGroup<Role>;
    @Input() additionalButtons: SpiderButton[] = [];
    @Input() isFirstMultiplePanel: boolean = false;
    @Input() isMiddleMultiplePanel: boolean = false;
    @Input() isLastMultiplePanel: boolean = false;

    authorizationForSaveSubscription: Subscription;
    @Input() authorizedForSaveObservable: () => Observable<boolean> = () => of(false);
    isAuthorizedForSave: boolean = false;
    @Output() onIsAuthorizedForSaveChange = new EventEmitter<IsAuthorizedForSaveEvent>(); 
    
    modelId: number;
    loading: boolean = true;
    roleSaveBodyName: string = nameof<RoleSaveBody>('roleDTO');

    usersForRoleOptions: PrimengOption[];
    permissionsForRoleOptions: PrimengOption[];

    selectedUsersForRole = new SpiderFormControl<PrimengOption[]>(null, {updateOn: 'change'});
    selectedPermissionsForRole = new SpiderFormControl<number[]>(null, {updateOn: 'change'});

    constructor(
        private apiService: ApiSecurityService,
        private route: ActivatedRoute,
        private baseFormService: BaseFormService,
        private authService: AuthBaseService
    ) {}

    ngOnInit(){
        this.formGroup.initSaveBody = () => { 
            let saveBody = new RoleSaveBody();
            saveBody.roleDTO = this.roleFormGroup.getRawValue();

            saveBody.selectedPermissionsIds = this.selectedPermissionsForRole.getRawValue();
            saveBody.selectedUsersIds = this.selectedUsersForRole.getRawValue()?.map(n => n.value);

            return saveBody;
        }

        this.formGroup.saveObservableMethod = this.apiService.saveRole;
        this.formGroup.mainDTOName = this.roleSaveBodyName;

        this.route.params.subscribe(async (params) => {
            this.modelId = params['id'];

            getPrimengDropdownNamebookOptions(this.apiService.getPermissionsDropdownListForRole).subscribe(po => {
                this.permissionsForRoleOptions = po;
            });

            if(this.modelId > 0){
                forkJoin({
                    role: this.apiService.getRole(this.modelId),
                    usersForRole: this.apiService.getUsersNamebookListForRole(this.modelId),
                    permissionsForRole: this.apiService.getPermissionsNamebookListForRole(this.modelId),
                })
                .subscribe(({ role, usersForRole, permissionsForRole }) => {
                    this.initRoleFormGroup(new Role(role));

                    this.selectedPermissionsForRole.setValue(
                        permissionsForRole.map(n => { return n.id })
                    );
                    this.selectedUsersForRole.setValue(
                        usersForRole.map(n => ({ label: n.displayName, value: n.id }))
                    );

                    this.authorizationForSaveSubscription = this.handleAuthorizationForSave().subscribe();
                    this.loading = false;
                });
            }
            else{
                this.initRoleFormGroup(new Role({id: 0}));

                this.authorizationForSaveSubscription = this.handleAuthorizationForSave().subscribe();
                this.loading = false;
            }
        });
    }

    initRoleFormGroup(role: Role) {
        this.baseFormService.initFormGroup<Role>(
            this.roleFormGroup, 
            this.formGroup, 
            role, 
            this.roleSaveBodyName,
            []
        );
        this.roleFormGroup.mainDTOName = this.roleSaveBodyName;
        this.onRoleFormGroupInitFinish.next();
    }
    
    handleAuthorizationForSave = () => {
        return combineLatest([this.authService.currentUserPermissionCodes$, this.authorizedForSaveObservable()]).pipe(
            map(([currentUserPermissionCodes, isAuthorizedForSave]) => {
                if (currentUserPermissionCodes != null && isAuthorizedForSave != null) {
                    this.isAuthorizedForSave =
                        (currentUserPermissionCodes.includes('InsertRole') && this.modelId <= 0) || 
                        (currentUserPermissionCodes.includes('UpdateRole') && this.modelId > 0) ||
                        isAuthorizedForSave;

                    if (this.isAuthorizedForSave) { 
                        this.roleFormGroup.controls.name.enable();
                        this.roleFormGroup.controls.description.enable();
                        this.selectedUsersForRole.enable();
                        this.selectedPermissionsForRole.enable();
                    }
                    else{
                        this.roleFormGroup.controls.name.disable();
                        this.roleFormGroup.controls.description.disable();
                        this.selectedUsersForRole.disable();
                        this.selectedPermissionsForRole.disable();
                    }

                    this.onIsAuthorizedForSaveChange.next(new IsAuthorizedForSaveEvent({
                        isAuthorizedForSave: this.isAuthorizedForSave, 
                        currentUserPermissionCodes: currentUserPermissionCodes
                    })); 
                }
            })
        );
    }

    searchUsersForRole(event: AutoCompleteCompleteEvent) {
        getPrimengAutocompleteNamebookOptions(this.apiService.getUsersAutocompleteListForRole, 50, event?.query ?? '').subscribe(po => {
            this.usersForRoleOptions = po;
        });
    }

    control(formControlName: string, formGroup: SpiderFormGroup){
        return getControl(formControlName, formGroup);
    }

    getFormArrayGroups<T>(formArray: SpiderFormArray): SpiderFormGroup<T>[]{
        return this.baseFormService.getFormArrayGroups<T>(formArray);
    }

    save(){
        this.onSave.next();
    }

    ngOnDestroy(){
        if (this.authorizationForSaveSubscription) {
            this.authorizationForSaveSubscription.unsubscribe();
        }
    }

}
