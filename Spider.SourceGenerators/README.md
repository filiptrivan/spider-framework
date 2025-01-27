
# Spider.SourceGenerators
## Quickstart
1. Generate the app structure with this [project](https://github.com/filiptrivan/soft-web-app-generator).
2. Add entities to the project (inside {appName}.Business -> Entities folder): 
  - If crud operations can be performed on the entity from the application, it should inherit `BusinessObject<ID>`, if the entity is only for reading from the database (eg Gender entity), it should inherit `ReadonlyObject<ID>`. For BusinessObject entities, the necessary methods for basic crud operations will be generated, while e.g. for ReadonlyObject entities Create, Update, Delete methods will not be generated.
  - Example of the EF Core entity:
```
namespace PlayertyLoyals.Business.Entities
{
    [TranslateSingularSrLatnRS("Korisnik")] // Where necessary, the entity UserExtended will be translated into Serbian as "Korisnik"
    public class UserExtended : BusinessObject<long>, IUser
    {
        [UIControlWidth("col-12")] // On the UI this control will be displayed over the entire width of the screen for any device size (by default it is half, then from a certain number of pixels the whole screen)
        [DisplayName] // A Property with this attribute will be used as a display name for the class it is in (eg when we display the UserExtended list in the dropdown, their emails will be used for display). If you don't put this property anywhere, the Id will be taken by default.
        [StringLength(70, MinimumLength = 5)] // This attribute is already built in EF Core, but apart from that, we also use it to generate validations (Backend and Frontend)
        [Required] // This attribute is already built in EF Core, but apart from that, we also use it to generate validations (Backend and Frontend)
        public string Email { get; set; }

        [UIDoNotGenerate] // We don't show this control to the end user on the UI
        public bool? HasLoggedInWithExternalProvider { get; set; }

        public DateTime? BirthDate { get; set; }

        [UIControlType(nameof(UIControlTypeCodes.Dropdown))] // This many to one property will be handled with dropdown on the UI (the necessary structure will be generated on the Backend to support it too)
        [SetNull] // When referenced Gender is deleted this property will be set to null
        [WithMany(nameof(Gender.Users))] // Connected one to many property 
        public virtual Gender Gender { get; set; }

        [BusinessServiceDoNotGenerate] // Frontend structure and controller method will be generated, but method inside BusinessServiceGenerated will not
        [UIControlType(nameof(UIControlTypeCodes.MultiSelect))] // This many to many property will be handled with multiselect on the UI (the necessary structure will be generated on the Backend to support it too)
        public virtual List<Role> Roles { get; } = new(); // M2M
    }
}
```
3. Write custom logic, 
- This is an example for the Notification entity, on the administration page of the entity we want to add a button, click on which we will send an email notification to users:
```
import { HttpClient } from '@angular/common/http';
import { ChangeDetectorRef, Component, KeyValueDiffers, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { TranslocoService } from '@jsverse/transloco';
import { Notification } from 'src/app/business/entities/business-entities.generated';
import { ApiService } from 'src/app/business/services/api/api.service';
import { BaseFormCopy, SpiderFormGroup, SpiderFormControl, SpiderButton, SpiderMessageService, BaseFormService } from '@playerty/spider';

@Component({
    selector: 'notification-details',
    templateUrl: './notification-details.component.html',
    styles: [],
})
export class NotificationDetailsComponent extends BaseFormCopy implements OnInit {
    notificationFormGroup = new SpiderFormGroup<Notification>({});

    isMarkedAsRead = new SpiderFormControl<boolean>(true, {updateOn: 'change'})

    additionalButtons: SpiderButton[];

    constructor(
        protected override differs: KeyValueDiffers,
        protected override http: HttpClient,
        protected override messageService: SpiderMessageService, 
        protected override changeDetectorRef: ChangeDetectorRef,
        protected override router: Router, 
        protected override route: ActivatedRoute,
        protected override translocoService: TranslocoService,
        protected override baseFormService: BaseFormService,
        private apiService: ApiService,
    ) {
        super(differs, http, messageService, changeDetectorRef, router, route, translocoService, baseFormService);
    }
         
    override ngOnInit() {
        this.additionalButtons = [
            {label: this.translocoService.translate('SendEmailNotification'), onClick: this.sendEmailNotification, icon: 'pi pi-send'}
        ];
    }

    // FT: We must to do it like arrow function
    sendEmailNotification = () => {
        this.apiService.sendNotificationEmail(this.notificationFormGroup.controls.id.value, this.notificationFormGroup.controls.version.value).subscribe(() => {
            this.messageService.successMessage(this.translocoService.translate('SuccessfulAttempt'));
        });
    }

    override onBeforeSave = (): void => {
        this.saveBody.isMarkedAsRead = this.isMarkedAsRead.value;
    }
}
```
- Example of adding custom code to html:
```
<ng-container *transloco="let t">
    <spider-card [title]="t('PartnerNotification')" icon="pi pi-bell">
        <spider-panel [isFirstMultiplePanel]="true" [showPanelHeader]="false">
            <panel-body>
                Custom HTML logic!
            </panel-body>
        </spider-panel>
        
        <notification-base-details
        [formGroup]="formGroup" 
        [notificationFormGroup]="notificationFormGroup" 
        (onSave)="onSave()"
        [isLastMultiplePanel]="true"
        [additionalButtons]="additionalButtons"
        />

    </spider-card>
</ng-container>
```

## Entity Attributes
### Required
- This attribute is already built in EF Core, but apart from that, we also use it to generate validations (Backend and Frontend). 
- When it's used on the enumerable property (for now, only in combination with `UIOrderedOneToMany` attribute) will not allow saving an empty list.
### StringLength
- This attribute is already built in EF Core, but apart from that, we also use it to generate validations (Backend and Frontend).
### GreaterThanOrEqualTo
- Set this attribute to the numeric properties only.
### CustomValidator
- If you cannot achieve something with built in fluent validations, you can write custom on the class.
- eg `[CustomValidator("RuleFor(x => x.Name).NotEmpty();")]`

### DisplayName
- A Property with this attribute will be used as a display name for the class it is in (eg when we display the `UserExtended` list in the dropdown, their emails will be used for display). If you don't put this property anywhere, the Id will be taken by default.
- Don't use nameof, because source generator will take only "Email" if you pass nameof(User.Email)
- Pass the parameter only if the display name is like this: User.Email
### BlobName
- Set this attribute to a property that serves as a pointer to the file identifier in azure storage.
### Controller
- Set this attribute to the entities for which you do not want the controller to be called {entityName}Controller, but to give it a custom name and possibly connect more entities to that controller.
### ExcludeFromDTO
- Set this attribute to the property you don't want generated in the DTO.
### IncludeInDTO
- Set this attribute to the property you want generated in the DTO.
- It only makes sense for enumerable properties (because they are not generated in a DTO by default).
- The generated property in DTO will not be included in the mapping library.
### ExcludeServiceMethodsFromGeneration
- All the logic that should be generated in the `BusinessServiceGenerated` class for this property will not be generated.
### GenerateCommaSeparatedDisplayName
- Set this attribute to the enumerable property for which you want the List<string> property to be generated in the DTO.
- It will be filled with display names using mapper. 
- It is used to display comma separated display names ​​in a table on the UI.
### WithMany
- Set to the many to one property.
- Pass enumerable parameter in order to know which enumerable property on the other side many to one property connects to.
### ManyToOneRequired
- Set to the many to one property to perform a `cascade` delete.
- We also use it to generate validations (Backend and Frontend). 
- The parent entity cannot exist without the property which has this attribute.
### SetNull
- Set to the many to one property to perform a `set null` delete.
### M2MEntity
- Set to a property in the M2M class that represents a reference to another entity. 
- As a parameter, you need to pass an enumerable property from the referenced entity.
### M2MMaintanceEntity
- Set to a property in the M2M class that represents a reference to another entity. 
- M2M relationship will be maintained through this referenced entity, both on the UI and on the backend.
- As a parameter, you need to pass an enumerable property from the referenced entity.
### SimpleManyToManyTableLazyLoad
- Set to the enumerable property which represents a navigation to other side of M2M relationship.
- Will generate such a structure on the backend and frontend that the many-to-many relationship is maintained using a lazy loading table.

### UI Attributes
These attributes are used exclusively for the UI.

#### UIControlType
- We try to conclude what type of controller should be on the front based on the property data type, but in some cases it is impossible
- Such as e.g. with the `color-picker` UI control type, the data type in C# is string, but that doesn't tell us enough.
- eg 
```
[UIControlType(nameof(UIControlTypeCodes.ColorPick))]
public string PrimaryColor { get; set; }
```

#### UIControlWidth
- Set to the property whose default width you want to change.
- eg `[UIControlWidth("col-12")]`, in the example, the width control will always be full screen.
- Default values for different control types are:
- - file, text-area, color-picker, multiselect, multiautocomplete, table, editor: `col-12`
- - everything else: `col-12 md:col-6`

#### UIDoNotGenerate
- This attribute can be set on an entity as well as on a property.
- If it is set on an entity, that entity will not be generated at all for administration on the UI.
- If it is set on a property, only that property will not be displayed.

#### UIOrderedOneToMany
- Set to the one to many property.
- Will generate such a structure on the backend and frontend that the one-to-many relationship is maintained using ordered list.
- For this way of maintenance on the entity, on the other hand, you must have the property `OrderNumber`, eg
```
[UIDoNotGenerate]
[Required]
public int OrderNumber { get; set; }
```

#### UIPanel
- With this attribute you determine in which panel the UI control will be located.
- By default all controls are inside the "Details" panel.

#### UIPropertyBlockOrder
- With this control, you determine the order in which controls will be displayed on the UI.
- The controls are displayed in the order you specified the properties on the entity (except `file`, `text-area`, `editor`, `table` control types, they are always displayed last in the written order).

#### UITableColumn
- Set to the enumerable property in combination with `SimpleManyToManyTableLazyLoad` attribute.
- eg
```
#region UITableColumn
[UITableColumn(nameof(PartnerUserDTO.UserDisplayName))]
[UITableColumn(nameof(PartnerUserDTO.Points))]
[UITableColumn(nameof(PartnerUserDTO.TierDisplayName))]
[UITableColumn(nameof(PartnerUserDTO.CheckedSegmentationItemsCommaSeparated), "Segmentation")]
[UITableColumn(nameof(PartnerUserDTO.CreatedAt))]
#endregion
[SimpleManyToManyTableLazyLoad]
public virtual List<PartnerUser> Recipients { get; } = new(); // M2M
```

### Translation Attributes

#### TranslatePluralEn
- eg `Users`
#### TranslatePluralSrLatnRS
- eg `Korisnici`
#### TranslateExcelEn
- If you don't pass a property for this attribute, but you do pass for plural, we'll use that translation.
- eg `Users.xlsx`
#### TranslateExcelSrLatnRS
- If you don't pass a property for this attribute, but you do pass for plural, we'll use that translation.
- eg `Korisnici.xlsx` 
#### TranslateSingularEn
- eg `User`
#### TranslateSingularSrLatnRS
- eg `Korisnik`
