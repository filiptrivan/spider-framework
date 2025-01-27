
# Spider.SourceGenerators
## Quickstart
1. Generate the app structure with this [project](https://github.com/filiptrivan/soft-web-app-generator).
2. Add entities to the project (inside {appName}.Business -> Entities folder): 
  - If crud operations can be performed on the entity from the application, it should inherit `BusinessObject<ID>`, if the entity is only for reading from the database (eg Gender entity), it should inherit `ReadonlyObject<ID>`. For BusinessObject entities, the necessary methods for basic crud operations will be generated, while e.g. for ReadonlyObject entities Create, Update, Delete methods will not be generated.
  - Example of the EF Core entity:
```
namespace PlayertyLoyals.Business.Entities
{
    [TranslateSingularSrLatnRS("Korisnik")] // Where necessary (eg. UserExtended Excel file name), the entity UserExtended will be translated into Serbian as "Korisnik" (eg. "UserExtended.xlsx"/"Korisnik.xlsx")
    public class UserExtended : BusinessObject<long>, IUser
    {
        [UIColWidth("col-12")] // On the UI this control will be displayed over the entire width of the screen for any device size (by default it is half, then from a certain number of pixels the whole screen)
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
### M2MExtendEntity