using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Soft.SourceGenerator.NgTable.Angular;
using Soft.SourceGenerator.NgTable.Helpers;
using Soft.SourceGenerators.Enums;
using Soft.SourceGenerators.Helpers;
using Soft.SourceGenerators.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Soft.SourceGenerators.Angular
{
    [Generator]
    public class NgBaseDetailsGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
//#if DEBUG
//            if (!Debugger.IsAttached)
//            {
//                Debugger.Launch();
//            }
//#endif
            IncrementalValuesProvider<ClassDeclarationSyntax> classDeclarations = Helper.GetClassInrementalValuesProvider(context.SyntaxProvider, new List<NamespaceExtensionCodes>
                {
                    NamespaceExtensionCodes.Entities,
                    NamespaceExtensionCodes.DTO
                });

            IncrementalValueProvider<List<SoftClass>> referencedProjectClasses = Helper.GetIncrementalValueProviderClassesFromReferencedAssemblies(context,
                new List<NamespaceExtensionCodes>
                {
                    NamespaceExtensionCodes.Entities,
                    NamespaceExtensionCodes.DTO,
                });

            IncrementalValueProvider<string> callingProjectDirectory = context.GetCallingPath();

            var combined = classDeclarations.Collect()
                .Combine(referencedProjectClasses)
                .Combine(callingProjectDirectory);

            context.RegisterImplementationSourceOutput(combined, static (spc, source) =>
            {
                var (classesAndEntities, callingPath) = source;
                var (classes, referencedClasses) = classesAndEntities;

                Execute(classes, referencedClasses, callingPath, spc);
            });
        }

        private static void Execute(IList<ClassDeclarationSyntax> classes, List<SoftClass> referencedProjectClasses, string callingProjectDirectory, SourceProductionContext context)
        {
            if (classes.Count <= 1)
                return; // FT: one because of config settings

            string[] namespacePartsWithoutLastElement = Helper.GetNamespacePartsWithoutLastElement(classes[0]);
            string projectName = namespacePartsWithoutLastElement.LastOrDefault() ?? "ERROR"; // eg. Security

            // ...\API\PlayertyLoyals.Business -> ...\Angular\src\app\business\components\base-details\{projectName}.ts
            string outputPath = callingProjectDirectory.ReplaceEverythingAfter(@"\API\", $@"\Angular\src\app\business\components\base-details\{projectName.FromPascalToKebabCase()}-base-details.generated.ts");

            List<SoftClass> softClasses = Helper.GetSoftClasses(classes, referencedProjectClasses);
            List<SoftClass> customDTOClasses = softClasses.Where(x => x.Namespace.EndsWith(".DTO")).ToList();
            List<SoftClass> entities = softClasses.Where(x => x.Namespace.EndsWith(".Entities")).ToList();

            string result = $$"""
{{GetImports(customDTOClasses, entities, projectName)}}

{{string.Join("\n\n", GetAngularBaseDetailsComponents(customDTOClasses, entities))}}
""";

            Helper.WriteToTheFile(result, outputPath);
        }

        private static List<string> GetAngularBaseDetailsComponents(List<SoftClass> customDTOClasses, List<SoftClass> entities)
        {
            List<string> result = new List<string>();

            foreach (SoftClass entity in entities
                .Where(x => x.Attributes.Any(x => x.Name == "UIDoNotGenerate") == false)
            )
            {
                if (entity.IsManyToMany())
                    continue;

                result.Add($$"""
@Component({
    selector: '{{entity.Name.FromPascalToKebabCase()}}-base-details',
    template:`
<ng-container *transloco="let t">
    <soft-panel [isFirstMultiplePanel]="isFirstMultiplePanel" [isMiddleMultiplePanel]="isMiddleMultiplePanel" [isLastMultiplePanel]="isLastMultiplePanel">
        <panel-header></panel-header>

        <panel-body>
            @defer (when loading === false) {
                <form class="grid">
{{string.Join("\n", GetPropertyBlocks(entity.Properties, entity, entities, customDTOClasses))}}
                </form>
            } @placeholder {
                <card-skeleton [height]="502"></card-skeleton>
            }
        </panel-body>

        <panel-footer>
            <p-button (onClick)="save()" [label]="t('Save')" icon="pi pi-save"></p-button>
            @for (button of additionalButtons; track button.label) {
                <p-button (onClick)="button.onClick()" [label]="button.label" [icon]="button.icon"></p-button>
            }
            <soft-return-button></soft-return-button>
        </panel-footer>
    </soft-panel>
</ng-container>
    `,
    standalone: true,
    imports: [
        CommonModule, 
        PrimengModule,
        SoftControlsModule,
        TranslocoDirective,
        CardSkeletonComponent,
        IndexCardComponent,
        SoftDataTableComponent,
    ]
})
export class {{entity.Name}}BaseComponent {
    @Output() onSave = new EventEmitter<void>();
    @Input() getCrudMenuForOrderedData: (formArray: SoftFormArray, modelConstructor: BaseEntity, lastMenuIconIndexClicked: LastMenuIconIndexClicked, adjustFormArrayManually: boolean) => MenuItem[];
    @Input() formGroup: SoftFormGroup;
    @Input() {{entity.Name.FirstCharToLower()}}FormGroup: SoftFormGroup<{{entity.Name}}>;
    @Input() additionalButtons: SoftButton[] = [];
    @Input() isFirstMultiplePanel: boolean = false;
    @Input() isMiddleMultiplePanel: boolean = false;
    @Input() isLastMultiplePanel: boolean = false;
    modelId: number;
    loading: boolean = true;

    {{entity.Name.FirstCharToLower()}}SaveBodyName: string = nameof<{{entity.Name}}SaveBody>('{{entity.Name.FirstCharToLower()}}DTO');

{{string.Join("\n\n", GetOrderedOneToManyVariables(entity, entities))}}

{{string.Join("\n", GetPrimengOptionVariables(entity.Properties, entity, entities))}}

{{string.Join("\n", GetSoftFormControls(entity))}}

{{string.Join("\n", GetSimpleManyToManyTableLazyLoadVariables(entity, entities))}}

    constructor(
        private apiService: ApiService,
        private route: ActivatedRoute,
        private baseFormService: BaseFormService,
        private validatorService: ValidatorService,
        private translocoService: TranslocoService,
    ) {}

    ngOnInit(){
        this.formGroup.initSaveBody = () => { 
            let saveBody = new {{entity.Name}}SaveBody();
            saveBody.{{entity.Name.FirstCharToLower()}}DTO = this.{{entity.Name.FirstCharToLower()}}FormGroup.getRawValue();
{{string.Join("\n", GetOrderedOneToManySaveBodyAssignements(entity, entities))}}
{{string.Join("\n", GetManyToManyMultiSelectSaveBodyAssignements(entity))}}
{{string.Join("\n", GetManyToManyMultiAutocompleteSaveBodyAssignements(entity))}}
{{string.Join("\n", GetSimpleManyToManyTableLazyLoadSaveBodyAssignements(entity))}}
            return saveBody;
        }

        this.formGroup.saveObservableMethod = this.apiService.save{{entity.Name}};
        this.formGroup.mainDTOName = this.{{entity.Name.FirstCharToLower()}}SaveBodyName;

        this.route.params.subscribe(async (params) => {
            this.modelId = params['id'];

{{string.Join("\n", GetManyToManyMultiSelectListForDropdownMethods(entity, entities))}}
{{string.Join("\n", GetSimpleManyToManyTableLazyLoadColsInitializations(entity, entities, customDTOClasses))}}

            if(this.modelId > 0){
                forkJoin({
                    {{entity.Name.FirstCharToLower()}}: this.apiService.get{{entity.Name}}(this.modelId),
{{string.Join("\n", GetOrderedOneToManyForkJoinParameters(entity))}}
{{string.Join("\n", GetManyToManyMultiControlTypesForkJoinParameters(entity))}}
                })
                .subscribe(({ {{string.Join(", ", GetForkJoinParameterNames(entity))}} }) => {
                    this.init{{entity.Name}}FormGroup(new {{entity.Name}}({{entity.Name.FirstCharToLower()}}));
{{string.Join("\n", GetOrderedOneToManyInitFormGroupForExistingObject(entity))}}
{{string.Join("\n", GetManyToManyMultiSelectInitFormControls(entity))}}
{{string.Join("\n", GetManyToManyMultiAutocompleteInitFormControls(entity))}}
                });
            }
            else{
                this.init{{entity.Name}}FormGroup(new {{entity.Name}}({id: 0}));
{{string.Join("\n", GetOrderedOneToManyInitFormGroupForNonExistingObject(entity))}}
            }
        });
    }

    init{{entity.Name}}FormGroup({{entity.Name.FirstCharToLower()}}: {{entity.Name}}) {
        this.baseFormService.initFormGroup<{{entity.Name}}>(
            this.{{entity.Name.FirstCharToLower()}}FormGroup, this.formGroup, {{entity.Name.FirstCharToLower()}}, this.{{entity.Name.FirstCharToLower()}}SaveBodyName, [{{string.Join(", ", GetCustomOnChangeProperties(entity))}}]
        );
        this.{{entity.Name.FirstCharToLower()}}FormGroup.mainDTOName = this.{{entity.Name.FirstCharToLower()}}SaveBodyName;
        this.loading = false;
    }

{{string.Join("\n", GetOrderedOneToManyInitFormArrayMethods(entity, entities))}}

{{string.Join("\n", GetOrderedOneToManyAddNewItemMethods(entity, entities))}}

{{string.Join("\n", GetSimpleManyToManyMethods(entity, entities))}}

{{string.Join("\n\n", GetAutocompleteSearchMethods(entity.Properties, entity, entities))}}

    control(formControlName: string, formGroup: SoftFormGroup){
        return getControl(formControlName, formGroup);
    }

    getFormArrayGroups<T>(formArray: SoftFormArray): SoftFormGroup<T>[]{
        return this.baseFormService.getFormArrayGroups<T>(formArray);
    }

    save(){
        this.onSave.next();
    }

}
""");
            }

            return result;
        }

        private static List<string> GetSimpleManyToManyMethods(SoftClass entity, List<SoftClass> entities)
        {
            List<string> result = new List<string>();

            foreach (SoftProperty property in entity.Properties.Where(x => x.HasSimpleManyToManyTableLazyLoadAttribute()))
            {
                result.Add($$"""
    selected{{property.Name}}LazyLoadMethodFor{{entity.Name}} = (event: TableFilter): Observable<LazyLoadSelectedIdsResult> => {
        let tableFilter: TableFilter = event;
        tableFilter.additionalFilterIdLong = this.modelId;

        return this.apiService.lazyLoadSelected{{property.Name}}IdsFor{{entity.Name}}(tableFilter);
    }
    areAll{{property.Name}}SelectedChangeFor{{entity.Name}}(event: AllClickEvent){
        this.areAll{{property.Name}}SelectedFor{{entity.Name}} = event.checked;
    }
    on{{property.Name}}LazyLoadFor{{entity.Name}}(event: TableFilter){
        this.last{{property.Name}}LazyLoadTableFilterFor{{entity.Name}} = event;
    }
""");
            }

            return result;
        }

        private static List<string> GetSimpleManyToManyTableLazyLoadColsInitializations(SoftClass entity, List<SoftClass> entities, List<SoftClass> customDTOClasses)
        {
            List<string> result = new List<string>();

            foreach (SoftProperty property in entity.Properties.Where(x => x.HasSimpleManyToManyTableLazyLoadAttribute()))
            {
                result.Add($$"""
            this.{{property.Name.FirstCharToLower()}}TableColsFor{{entity.Name}} = [
{{string.Join(",\n", GetSimpleManyToManyTableLazyLoadCols(property, entity, entities, customDTOClasses))}}
            ];
""");
            }

            return result;
        }

        private static List<string> GetSimpleManyToManyTableLazyLoadCols(SoftProperty property, SoftClass entity, List<SoftClass> entities, List<SoftClass> customDTOClasses)
        {
            List<string> result = new List<string>();

            foreach (UIColumn col in property.GetUIColumns())
            {
                SoftClass extractedEntity = entities.Where(x => x.Name == Helper.ExtractTypeFromGenericType(property.Type)).SingleOrDefault();
                SoftProperty extractedEntityProperty = extractedEntity?.Properties?.Where(x => x.Name == col.Field.Replace("DisplayName", "").Replace("CommaSeparated", ""))?.SingleOrDefault();

                SoftClass extractedDTO = customDTOClasses.Where(x => x.Name == $"{Helper.ExtractTypeFromGenericType(property.Type)}DTO").SingleOrDefault();
                SoftProperty extractedDTOProperty = extractedDTO?.Properties?.Where(x => x.Name == col.Field)?.SingleOrDefault();

                result.Add($$"""
                {name: this.translocoService.translate('{{col.TranslationKey}}'), filterType: '{{GetTableColFilterType(extractedEntityProperty ?? extractedDTOProperty)}}', field: '{{col.Field.FirstCharToLower()}}' {{GetTableColAdditionalProperties(extractedEntityProperty ?? extractedDTOProperty, entities)}} }
""");
            }

            return result;
        }

        private static string GetTableColAdditionalProperties(SoftProperty property, List<SoftClass> entities)
        {
            if (property.IsDropdownControlType())
                return $", filterField: '{property.Name.FirstCharToLower()}Id', dropdownOrMultiselectValues: await firstValueFrom(this.apiService.getPrimengNamebookListForDropdown(this.apiService.get{property.Type}ListForDropdown))";

            if (property.HasGenerateCommaSeparatedDisplayNameAttribute())
            {
                SoftClass extractedEntity = entities.Where(x => x.Name == Helper.ExtractTypeFromGenericType(property.Type)).SingleOrDefault();
                return $", dropdownOrMultiselectValues: await firstValueFrom(this.apiService.getPrimengNamebookListForDropdown(this.apiService.get{extractedEntity.Name}ListForDropdown))";
            }

            switch (property.Type)
            {
                case "DateTime":
                case "DateTime?":
                    return ", showMatchModes: true";
                case "decimal":
                case "decimal?":
                case "float":
                case "float?":
                case "double":
                case "double?":
                case "long":
                case "long?":
                case "int":
                case "int?":
                case "byte":
                case "byte?":
                    return ", showMatchModes: true";
                default:
                    break;
            }

            return null;
        }

        private static string GetTableColFilterType(SoftProperty property)
        {
            if (property.IsDropdownControlType())
                return "multiselect";

            if (property.HasGenerateCommaSeparatedDisplayNameAttribute())
                return "multiselect";

            if (property.Type.IsManyToOneType())
                return "text";

            switch (property.Type)
            {
                case "string":
                    return "text";
                case "bool":
                case "bool?":
                    return "boolean";
                case "DateTime":
                case "DateTime?":
                    return "date";
                case "decimal":
                case "decimal?":
                case "float":
                case "float?":
                case "double":
                case "double?":
                case "long":
                case "long?":
                case "int":
                case "int?":
                case "byte":
                case "byte?":
                    return "numeric";
                default:
                    break;
            }

            return null;
        }

        private static List<string> GetSimpleManyToManyTableLazyLoadSaveBodyAssignements(SoftClass entity)
        {
            List<string> result = new List<string>();

            foreach (SoftProperty property in entity.Properties.Where(x => x.HasSimpleManyToManyTableLazyLoadAttribute()))
            {
                result.Add($$"""
            saveBody.selected{{property.Name}}Ids = this.newlySelected{{property.Name}}IdsFor{{entity.Name}};
            saveBody.unselected{{property.Name}}Ids = this.unselected{{property.Name}}IdsFor{{entity.Name}};
            saveBody.areAll{{property.Name}}Selected = this.areAll{{property.Name}}SelectedFor{{entity.Name}};
            saveBody.{{property.Name.FirstCharToLower()}}TableFilter = this.last{{property.Name}}LazyLoadTableFilterFor{{entity.Name}};
""");
            }

            return result;
        }

        private static List<string> GetSimpleManyToManyTableLazyLoadVariables(SoftClass entity, List<SoftClass> entities)
        {
            List<string> result = new List<string>();

            foreach (SoftProperty property in entity.Properties.Where(x => x.HasSimpleManyToManyTableLazyLoadAttribute()))
            {
                SoftClass extractedEntity = entities.Where(x => x.Name == Helper.ExtractTypeFromGenericType(property.Type)).SingleOrDefault();

                result.Add($$"""
    {{property.Name.FirstCharToLower()}}TableColsFor{{entity.Name}}: Column<{{extractedEntity.Name}}>[];
    get{{property.Name}}TableDataObservableMethodFor{{entity.Name}} = this.apiService.get{{property.Name}}TableDataFor{{entity.Name}};
    export{{property.Name}}TableDataToExcelObservableMethodFor{{entity.Name}} = this.apiService.export{{property.Name}}TableDataToExcelFor{{entity.Name}};
    newlySelected{{property.Name}}IdsFor{{entity.Name}}: number[] = [];
    unselected{{property.Name}}IdsFor{{entity.Name}}: number[] = [];
    areAll{{property.Name}}SelectedFor{{entity.Name}}: boolean = null;
    last{{property.Name}}LazyLoadTableFilterFor{{entity.Name}}: TableFilter;
""");
            }

            return result;
        }

        private static List<string> GetManyToManyMultiSelectSaveBodyAssignements(SoftClass entity)
        {
            List<string> result = new List<string>();

            foreach (SoftProperty property in entity.Properties.Where(x => x.IsMultiSelectControlType()))
            {
                result.Add($$"""
            saveBody.selected{{property.Name}}Ids = this.selected{{property.Name}}For{{entity.Name}}.getRawValue();
""");
            }

            return result;
        }

        private static List<string> GetManyToManyMultiAutocompleteSaveBodyAssignements(SoftClass entity)
        {
            List<string> result = new List<string>();

            foreach (SoftProperty property in entity.Properties.Where(x => x.IsMultiAutocompleteControlType()))
            {
                result.Add($$"""
            saveBody.selected{{property.Name}}Ids = this.selected{{property.Name}}For{{entity.Name}}.getRawValue()?.map(n => n.value);
""");
            }

            return result;
        }

        private static List<string> GetManyToManyMultiSelectInitFormControls(SoftClass entity)
        {
            List<string> result = new List<string>();

            foreach (SoftProperty property in entity.Properties.Where(x => x.IsMultiSelectControlType()))
            {
                result.Add($$"""
                    this.selected{{property.Name}}For{{entity.Name}}.setValue(
                        {{property.Name.FirstCharToLower()}}For{{entity.Name}}.map(n => { return n.id })
                    );
""");
            }

            return result;
        }

        private static List<string> GetManyToManyMultiAutocompleteInitFormControls(SoftClass entity)
        {
            List<string> result = new List<string>();

            foreach (SoftProperty property in entity.Properties.Where(x => x.IsMultiAutocompleteControlType()))
            {
                result.Add($$"""
                    this.selected{{property.Name}}For{{entity.Name}}.setValue(
                        {{property.Name.FirstCharToLower()}}For{{entity.Name}}.map(n => ({ label: n.displayName, value: n.id }))
                    );
""");
            }

            return result;
        }

        private static List<string> GetManyToManyMultiSelectListForDropdownMethods(SoftClass entity, List<SoftClass> entities)
        {
            List<string> result = new List<string>();

            foreach (SoftProperty property in entity.Properties.Where(x => x.IsMultiSelectControlType()))
            {
                SoftClass extractedEntity = entities.Where(x => x.Name == Helper.ExtractTypeFromGenericType(property.Type)).SingleOrDefault();

                result.Add($$"""
            this.apiService.getPrimengNamebookListForDropdown(this.apiService.get{{extractedEntity.Name}}ListForDropdown).subscribe(po => {
                this.{{property.Name.FirstCharToLower()}}For{{entity.Name}}Options = po;
            });
""");
            }

            return result;
        }

        private static List<string> GetManyToManyMultiControlTypesForkJoinParameters(SoftClass entity)
        {
            List<string> result = new List<string>();

            foreach (SoftProperty property in entity.Properties
                .Where(x => 
                    x.IsMultiSelectControlType() ||
                    x.IsMultiAutocompleteControlType()))
            {
                result.Add($$"""
                    {{property.Name.FirstCharToLower()}}For{{entity.Name}}: this.apiService.get{{property.Name}}NamebookListFor{{entity.Name}}(this.modelId),
""");
            }

            return result;
        }

        private static List<string> GetSoftFormControls(SoftClass entity)
        {
            List<string> result = new List<string>();

            foreach (SoftProperty property in entity.Properties)
            {
                if (property.IsMultiSelectControlType())
                {
                    result.Add($$"""
    selected{{property.Name}}For{{entity.Name}} = new SoftFormControl<number[]>(null, {updateOn: 'change'});
""");
                }
                else if (property.IsMultiAutocompleteControlType())
                {
                    result.Add($$"""
    selected{{property.Name}}For{{entity.Name}} = new SoftFormControl<PrimengOption[]>(null, {updateOn: 'change'});
""");
                }
            }

            return result;
        }

        #region Ordered One To Many

        private static List<string> GetOrderedOneToManyAddNewItemMethods(SoftClass entity, List<SoftClass> entities)
        {
            List<string> result = new List<string>();

            foreach (SoftProperty property in entity.GetOrderedOneToManyProperties())
            {
                SoftClass extractedEntity = entities.Where(x => x.Name == Helper.ExtractTypeFromGenericType(property.Type)).SingleOrDefault();

                result.Add($$"""
    addNewItemTo{{property.Name}}(index: number){ 
        this.baseFormService.addNewFormGroupToFormArray(this.{{property.Name.FirstCharToLower()}}FormArray, new {{extractedEntity.Name}}({id: 0}), index);
    }
""");
            }

            return result;
        }

        private static List<string> GetOrderedOneToManyInitFormArrayMethods(SoftClass entity, List<SoftClass> entities)
        {
            List<string> result = new List<string>();

            foreach (SoftProperty property in entity.GetOrderedOneToManyProperties())
            {
                SoftClass extractedEntity = entities.Where(x => x.Name == Helper.ExtractTypeFromGenericType(property.Type)).SingleOrDefault();

                result.Add($$"""
    init{{property.Name}}FormArray({{property.Name.FirstCharToLower()}}: {{extractedEntity.Name}}[]){
        this.{{property.Name.FirstCharToLower()}}FormArray = this.baseFormService.initFormArray(
            this.formGroup, {{property.Name.FirstCharToLower()}}, this.{{property.Name.FirstCharToLower()}}Model, this.{{property.Name.FirstCharToLower()}}SaveBodyName, this.{{property.Name.FirstCharToLower()}}TranslationKey, true
        );
        this.{{property.Name.FirstCharToLower()}}CrudMenu = this.getCrudMenuForOrderedData(this.{{property.Name.FirstCharToLower()}}FormArray, new {{extractedEntity.Name}}({id: 0}), this.{{property.Name.FirstCharToLower()}}LastIndexClicked, false);
{{GetFormArrayEmptyValidator(property)}}
    }
""");
            }

            return result;
        }

        private static string GetFormArrayEmptyValidator(SoftProperty property)
        {
            if (property.HasNonEmptyAttribute())
            {
                return $$"""
        this.{{property.Name.FirstCharToLower()}}FormArray.validator = this.validatorService.isFormArrayEmpty(this.{{property.Name.FirstCharToLower()}}FormArray);
""";
            }

            return null;
        }

        private static List<string> GetOrderedOneToManyForkJoinParameters(SoftClass entity)
        {
            List<string> result = new List<string>();

            foreach (SoftProperty property in entity.GetOrderedOneToManyProperties())
            {
                result.Add($$"""
                    {{property.Name.FirstCharToLower()}}For{{entity.Name}}: this.apiService.getOrdered{{property.Name}}For{{entity.Name}}(this.modelId),
""");
            }

            return result;
        }

        private static List<string> GetOrderedOneToManyInitFormGroupForExistingObject(SoftClass entity)
        {
            List<string> result = new List<string>();

            foreach (SoftProperty property in entity.GetOrderedOneToManyProperties())
            {
                result.Add($$"""
                    this.init{{property.Name}}FormArray({{property.Name.FirstCharToLower()}}For{{entity.Name}});
""");
            }

            return result;
        }

        private static List<string> GetOrderedOneToManyInitFormGroupForNonExistingObject(SoftClass entity)
        {
            List<string> result = new List<string>();

            foreach (SoftProperty property in entity.GetOrderedOneToManyProperties())
            {
                result.Add($$"""
                this.init{{property.Name}}FormArray([]);
""");
            }

            return result;
        }

        private static List<string> GetOrderedOneToManySaveBodyAssignements(SoftClass entity, List<SoftClass> entities)
        {
            List<string> result = new List<string>();

            foreach (SoftProperty property in entity.GetOrderedOneToManyProperties())
            {
                SoftClass extractedEntity = entities.Where(x => x.Name == Helper.ExtractTypeFromGenericType(property.Type)).SingleOrDefault();

                result.Add($$"""
            saveBody.{{property.Name.FirstCharToLower()}}DTO = this.{{property.Name.FirstCharToLower()}}FormArray.getRawValue();
""");
            }

            return result;
        }

        private static List<string> GetOrderedOneToManyVariables(SoftClass entity, List<SoftClass> entities)
        {
            List<string> result = new List<string>();

            foreach (SoftProperty property in entity.GetOrderedOneToManyProperties())
            {
                SoftClass extractedEntity = entities.Where(x => x.Name == Helper.ExtractTypeFromGenericType(property.Type)).SingleOrDefault();

                result.Add($$"""
    {{property.Name.FirstCharToLower()}}Model: {{extractedEntity.Name}} = new {{extractedEntity.Name}}();
    {{property.Name.FirstCharToLower()}}SaveBodyName: string = nameof<{{extractedEntity.Name}}SaveBody>('{{extractedEntity.Name.FirstCharToLower()}}DTO');
    {{property.Name.FirstCharToLower()}}TranslationKey: string = new {{extractedEntity.Name}}().typeName;
    {{property.Name.FirstCharToLower()}}FormArray: SoftFormArray<{{extractedEntity.Name}}[]>;
    {{property.Name.FirstCharToLower()}}LastIndexClicked: LastMenuIconIndexClicked = new LastMenuIconIndexClicked();
    {{property.Name.FirstCharToLower()}}CrudMenu: MenuItem[] = [];
""");
            }

            return result;
        }


        /// <summary>
        /// </summary>
        /// <param name="property">eg. List<SegmentationItem> SegmentationItems</param>
        /// <param name="entities"></param>
        /// <param name="customDTOClasses"></param>
        /// <returns></returns>
        private static string GetOrderedOneToManyBlock(SoftProperty property, List<SoftClass> entities, List<SoftClass> customDTOClasses)
        {
            SoftClass entity = entities.Where(x => x.Name == Helper.ExtractTypeFromGenericType(property.Type)).SingleOrDefault(); // eg. SegmentationItem

            // Every property of SegmentationItem without the many to one reference (Segmentation) and enumerable properties
            List<SoftProperty> propertyBlocks = entity.Properties
                .Where(x =>
                    x.WithMany() != property.Name &&
                    x.Type.IsEnumerable() == false
                )
                .ToList();

            return $$"""
                 <div class="col-12">
                    <soft-panel>
                        <panel-header [title]="t('{{property.Name}}')" icon="pi pi-list"></panel-header>
                        <panel-body [normalBottomPadding]="true">
                            @for ({{entity.Name.FirstCharToLower()}}FormGroup of getFormArrayGroups({{property.Name.FirstCharToLower()}}FormArray); track {{entity.Name.FirstCharToLower()}}FormGroup; let index = $index; let last = $last) {
                                <index-card [index]="index" [last]="false" [crudMenu]="{{property.Name.FirstCharToLower()}}CrudMenu" (onMenuIconClick)="{{property.Name.FirstCharToLower()}}LastIndexClicked.index = $event">
                                    <form [formGroup]="{{entity.Name.FirstCharToLower()}}FormGroup" class="grid">
{{string.Join("\n", GetPropertyBlocks(propertyBlocks, entity, entities, customDTOClasses))}}
                                    </form>
                                </index-card>
                            }

                            <div class="panel-add-button">
                                <p-button (onClick)="addNewItemTo{{property.Name}}(null)" [label]="t('AddNew{{Helper.ExtractTypeFromGenericType(property.Type)}}')" icon="pi pi-plus"></p-button>
                            </div>

                        </panel-body>
                    </soft-panel>
                </div>       
""";
        }

        #endregion

        private static List<string> GetCustomOnChangeProperties(SoftClass entity)
        {
            List<string> result = new List<string>();

            foreach (SoftProperty property in entity.Properties)
            {
                if (property.IsColorControlType())
                {
                    result.Add($"'{property.Name.FirstCharToLower()}'");
                }
            }

            return result;
        }

        private static List<string> GetPrimengOptionVariables(List<SoftProperty> properties, SoftClass entity, List<SoftClass> entities)
        {
            List<string> result = new List<string>();

            foreach (SoftProperty property in properties.Where(x => x.Attributes.Any(x => x.Name == "UIDoNotGenerate") == false))
            {
                if (property.Attributes.Any(x => x.Name == "UIOrderedOneToMany"))
                {
                    SoftClass extractedEntity = entities.Where(x => x.Name == Helper.ExtractTypeFromGenericType(property.Type)).SingleOrDefault();
                    List<SoftProperty> extractedProperties = extractedEntity.Properties
                        .Where(x =>
                            x.WithMany() != property.Name &&
                            x.Type.IsEnumerable() == false
                        )
                        .ToList();

                    GetPrimengOptionVariables(extractedProperties, extractedEntity, entities);

                    continue;
                }

                UIControlTypeCodes controlType = GetUIControlType(property);

                if (controlType == UIControlTypeCodes.Autocomplete ||
                    controlType == UIControlTypeCodes.Dropdown ||
                    controlType == UIControlTypeCodes.MultiAutocomplete ||
                    controlType == UIControlTypeCodes.MultiSelect)
                {
                    result.Add($$"""
    {{property.Name.FirstCharToLower()}}For{{entity.Name}}Options: PrimengOption[];
""");

                }
            }

            return result;
        }

        private static List<string> GetAutocompleteSearchMethods(List<SoftProperty> properties, SoftClass entity, List<SoftClass> entities)
        {
            List<string> result = new List<string>();

            foreach (SoftProperty property in properties.Where(x => x.Attributes.Any(x => x.Name == "UIDoNotGenerate") == false))
            {
                if (property.Attributes.Any(x => x.Name == "UIOrderedOneToMany"))
                {
                    SoftClass extractedEntity = entities.Where(x => x.Name == Helper.ExtractTypeFromGenericType(property.Type)).SingleOrDefault();
                    List<SoftProperty> extractedProperties = extractedEntity.Properties
                        .Where(x =>
                            x.WithMany() != property.Name &&
                            x.Type.IsEnumerable() == false
                        )
                        .ToList();

                    GetAutocompleteSearchMethods(extractedProperties, extractedEntity, entities);

                    continue;
                }

                UIControlTypeCodes controlType = GetUIControlType(property);

                if (controlType == UIControlTypeCodes.Autocomplete ||
                    controlType == UIControlTypeCodes.Dropdown ||
                    controlType == UIControlTypeCodes.MultiAutocomplete)
                {
                    result.Add($$"""
    search{{property.Name}}For{{entity.Name}}(event: AutoCompleteCompleteEvent) {
        this.apiService.getPrimengNamebookListForAutocomplete(this.apiService.get{{Helper.ExtractTypeFromGenericType(property.Type)}}ListForAutocomplete, 50, event?.query ?? '').subscribe(po => {
            this.{{property.Name.FirstCharToLower()}}For{{entity.Name}}Options = po;
        });
    }
""");

                }

            }

            return result;
        }

        private static List<string> GetForkJoinParameterNames(SoftClass entity)
        {
            List<string> result = new List<string>();

            result.Add(entity.Name.FirstCharToLower());

            foreach (SoftProperty property in entity.Properties)
            {
                if (property.HasOrderedOneToManyAttribute() ||
                    property.IsMultiSelectControlType() ||
                    property.IsMultiAutocompleteControlType())
                {
                    result.Add($"{property.Name.FirstCharToLower()}For{entity.Name}");
                }
            }

            return result;
        }

        private static List<string> GetPropertyBlocks(
            List<SoftProperty> properties,
            SoftClass entity,
            List<SoftClass> entities,
            List<SoftClass> customDTOClasses
        )
        {
            List<string> result = new List<string>();

            SoftClass customDTOClass = customDTOClasses.Where(x => x.Name.Replace("DTO", "") == entity.Name).SingleOrDefault();

            if (customDTOClass != null)
            {
                List<SoftProperty> customDTOClassProperties = customDTOClass.Properties;

                properties.AddRange(customDTOClassProperties);
            }

            foreach (SoftProperty property in GetPropertiesForUIBlocks(properties))
            {
                if (property.Attributes.Any(x => x.Name == "UIOrderedOneToMany"))
                {
                    result.Add(GetOrderedOneToManyBlock(property, entities, customDTOClasses));

                    continue;
                }

                string controlType = GetUIStringControlType(GetUIControlType(property));

                result.Add($$"""
                    <div class="{{GetUIColWidth(property)}}">
                        <{{controlType}} {{GetControlAttributes(property, entity)}}></{{controlType}}>
                    </div>
""");
            }

            return result;
        }

        private static string GetControlHtmlAttributeValue(SoftProperty property, SoftClass entity)
        {
            if (property.IsMultiSelectControlType() ||
                property.IsMultiAutocompleteControlType())
            {
                return $"selected{property.Name}For{entity.Name}";
            }

            return $"control('{GetFormControlName(property)}', {entity.Name.FirstCharToLower()}FormGroup)";
        }

        private static List<SoftProperty> GetPropertiesForUIBlocks(List<SoftProperty> properties)
        {
            List<SoftProperty> orderedProperties = properties
                .Where(x =>
                    x.Name != "Version" &&
                    x.Name != "Id" &&
                    x.Name != "CreatedAt" &&
                    x.Name != "ModifiedAt" &&
                    (
                        x.Type.IsEnumerable() == false ||
                        x.Attributes.Any(x => x.Name == "UIOrderedOneToMany") ||
                        x.IsMultiSelectControlType() ||
                        x.IsMultiAutocompleteControlType() ||
                        x.HasSimpleManyToManyTableLazyLoadAttribute()
                    ) &&
                    x.Attributes.Any(x => x.Name == "UIDoNotGenerate") == false
                )
                .OrderBy(x =>
                    x.Attributes.Any(attr => attr.Name == "BlobName") ? 0 :
                    x.Attributes.Any(attr => attr.Value == UIControlTypeCodes.TextArea.ToString()) ? 2 :
                    x.Attributes.Any(attr => attr.Value == UIControlTypeCodes.Editor.ToString()) ? 3 :
                    x.Attributes.Any(attr => attr.Name == "UIOrderedOneToMany") ? 4 :
                    x.Attributes.Any(attr => attr.Name == "SimpleManyToManyTableLazyLoad") ? 5
                    : 1)
                .ToList();

            return orderedProperties;
        }

        private static string GetFormControlName(SoftProperty property)
        {
            if (property.Type.IsManyToOneType())
                return $"{property.Name.FirstCharToLower()}Id";

            return property.Name.FirstCharToLower();
        }

        private static string GetControlAttributes(SoftProperty property, SoftClass entity)
        {
            UIControlTypeCodes controlType = GetUIControlType(property);

            if (controlType == UIControlTypeCodes.Decimal)
            {
                return $"[control]=\"{GetControlHtmlAttributeValue(property, entity)}\" [decimal]=\"true\" [maxFractionDigits]=\"{property.GetDecimalScale()}\"";
            }
            else if (controlType == UIControlTypeCodes.File)
            {
                return $"[control]=\"{GetControlHtmlAttributeValue(property, entity)}\" [fileData]=\"{entity.Name.FirstCharToLower()}FormGroup.controls.{property.Name.FirstCharToLower()}Data.getRawValue()\" [objectId]=\"{entity.Name.FirstCharToLower()}FormGroup.controls.id.getRawValue()\"";
            }
            else if (controlType == UIControlTypeCodes.Dropdown)
            {
                return $"[control]=\"{GetControlHtmlAttributeValue(property, entity)}\" [options]=\"{property.Name.FirstCharToLower()}For{entity.Name}Options\"";
            }
            else if (controlType == UIControlTypeCodes.Autocomplete)
            {
                return $"[control]=\"{GetControlHtmlAttributeValue(property, entity)}\" [options]=\"{property.Name.FirstCharToLower()}For{entity.Name}Options\" [displayName]=\"{entity.Name.FirstCharToLower()}FormGroup.controls.{property.Name.FirstCharToLower()}DisplayName.getRawValue()\" (onTextInput)=\"search{property.Name}For{entity.Name}($event)\"";
            }
            else if (controlType == UIControlTypeCodes.MultiSelect)
            {
                return $"[control]=\"{GetControlHtmlAttributeValue(property, entity)}\" [options]=\"{property.Name.FirstCharToLower()}For{entity.Name}Options\" [label]=\"t('{property.Name}')\"";
            }
            else if (controlType == UIControlTypeCodes.MultiAutocomplete)
            {
                return $"[control]=\"{GetControlHtmlAttributeValue(property, entity)}\" [options]=\"{property.Name.FirstCharToLower()}For{entity.Name}Options\" (onTextInput)=\"search{property.Name}For{entity.Name}($event)\" [label]=\"t('{property.Name}')\"";
            }
            else if (controlType == UIControlTypeCodes.Table)
            {
                return $$"""

                            [tableTitle]="t('{{property.Name}}For{{entity.Name}}')" 
                            [cols]="{{property.Name.FirstCharToLower()}}TableColsFor{{entity.Name}}" 
                            [getTableDataObservableMethod]="get{{property.Name}}TableDataObservableMethodFor{{entity.Name}}" 
                            [exportTableDataToExcelObservableMethod]="export{{property.Name}}TableDataToExcelObservableMethodFor{{entity.Name}}"
                            [showAddButton]="false" 
                            selectionMode="multiple"
                            [newlySelectedItems]="newlySelected{{property.Name}}IdsFor{{entity.Name}}" 
                            [unselectedItems]="unselected{{property.Name}}IdsFor{{entity.Name}}" 
                            [rows]="5" 
                            (onLazyLoad)="on{{property.Name}}LazyLoadFor{{entity.Name}}($event)"
                            [selectedLazyLoadObservableMethod]="selected{{property.Name}}LazyLoadMethodFor{{entity.Name}}" 
                            (onIsAllSelectedChange)="areAll{{property.Name}}SelectedChangeFor{{entity.Name}}($event)"
""";
            }

            return $"[control]=\"{GetControlHtmlAttributeValue(property, entity)}\"";
        }

        private static string GetUIColWidth(SoftProperty property)
        {
            SoftAttribute uiColWidthAttribute = property.Attributes.Where(x => x.Name == "UIColWidth").SingleOrDefault();

            if (uiColWidthAttribute != null)
                return uiColWidthAttribute.Value;

            UIControlTypeCodes controlType = GetUIControlType(property);

            if (controlType == UIControlTypeCodes.File ||
                controlType == UIControlTypeCodes.TextArea ||
                controlType == UIControlTypeCodes.MultiSelect ||
                controlType == UIControlTypeCodes.MultiAutocomplete ||
                controlType == UIControlTypeCodes.Table ||
                controlType == UIControlTypeCodes.Editor)
            {
                return "col-12";
            }

            return "col-12 md:col-6";
        }

        private static UIControlTypeCodes GetUIControlType(SoftProperty property)
        {
            SoftAttribute uiControlTypeAttribute = property.Attributes.Where(x => x.Name == "UIControlType").SingleOrDefault();

            if (uiControlTypeAttribute != null)
            {
                Enum.TryParse(uiControlTypeAttribute.Value, out UIControlTypeCodes parseResult);
                return parseResult;
            }

            if (property.IsBlob())
                return UIControlTypeCodes.File;

            if (property.Type.IsManyToOneType())
                return UIControlTypeCodes.Autocomplete;

            if (property.HasSimpleManyToManyTableLazyLoadAttribute())
                return UIControlTypeCodes.Table;

            switch (property.Type)
            {
                case "string":
                    return UIControlTypeCodes.TextBox;
                case "bool":
                case "bool?":
                    return UIControlTypeCodes.CheckBox;
                case "DateTime":
                case "DateTime?":
                    return UIControlTypeCodes.Calendar;
                case "decimal":
                case "decimal?":
                case "float":
                case "float?":
                case "double":
                case "double?":
                    return UIControlTypeCodes.Decimal;
                case "long":
                case "long?":
                case "int":
                case "int?":
                case "byte":
                case "byte?":
                    return UIControlTypeCodes.Integer;
                default:
                    break;
            }

            return UIControlTypeCodes.TODO;
        }

        private static string GetUIStringControlType(UIControlTypeCodes controlType)
        {
            switch (controlType)
            {
                case UIControlTypeCodes.Autocomplete:
                    return "soft-autocomplete";
                case UIControlTypeCodes.Calendar:
                    return "soft-calendar";
                case UIControlTypeCodes.CheckBox:
                    return "soft-checkbox";
                case UIControlTypeCodes.ColorPick:
                    return "soft-colorpick";
                case UIControlTypeCodes.Dropdown:
                    return "soft-dropdown";
                case UIControlTypeCodes.Editor:
                    return "soft-editor";
                case UIControlTypeCodes.File:
                    return "soft-file";
                case UIControlTypeCodes.MultiAutocomplete:
                    return "soft-multiautocomplete";
                case UIControlTypeCodes.MultiSelect:
                    return "soft-multiselect";
                case UIControlTypeCodes.Integer:
                case UIControlTypeCodes.Decimal:
                    return "soft-number";
                case UIControlTypeCodes.Password:
                    return "soft-password";
                case UIControlTypeCodes.TextArea:
                    return "soft-textarea";
                case UIControlTypeCodes.TextBlock:
                    return "soft-textblock";
                case UIControlTypeCodes.TextBox:
                    return "soft-textbox";
                case UIControlTypeCodes.Table:
                    return "soft-data-table";
                case UIControlTypeCodes.TODO:
                    return "TODO";
                default:
                    return "TODO";

            }
        }

        private static string GetImports(List<SoftClass> customDTOClasses, List<SoftClass> entities, string projectName)
        {
            List<string> customDTOImports = customDTOClasses.Select(x => x.Name.Replace("DTO", "")).Distinct().ToList();
            List<string> entityImports = entities.Select(x => x.Name).ToList();
            List<string> saveBodyImports = entities.Select(x => $"{x.Name}SaveBody").ToList();

            List<string> imports = customDTOImports.Concat(entityImports).Concat(saveBodyImports).Distinct().ToList();

            return $$"""
import { ValidatorService } from 'src/app/business/services/validators/validation-rules';
import { BaseFormService } from './../../../core/services/base-form.service';
import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, Output } from '@angular/core';
import { PrimengModule } from 'src/app/core/modules/primeng.module';
import { ApiService } from '../../services/api/api.service';
import { TranslocoDirective, TranslocoService } from '@jsverse/transloco';
import { SoftControlsModule } from 'src/app/core/controls/soft-controls.module';
import { SoftFormArray, SoftFormControl, SoftFormGroup } from 'src/app/core/components/soft-form-control/soft-form-control';
import { PrimengOption } from 'src/app/core/entities/primeng-option';
import { AutoCompleteCompleteEvent } from 'primeng/autocomplete';
import { getControl, nameof } from 'src/app/core/services/helper-functions';
import { ActivatedRoute } from '@angular/router';
import { firstValueFrom, forkJoin, Observable } from 'rxjs';
import { BaseEntity } from 'src/app/core/entities/base-entity';
import { CardSkeletonComponent } from "../../../core/components/card-skeleton/card-skeleton.component";
import { SoftButton } from 'src/app/core/entities/soft-button';
import { IndexCardComponent } from 'src/app/core/components/index-card/index-card.component';
import { LastMenuIconIndexClicked } from 'src/app/core/entities/last-menu-icon-index-clicked';
import { MenuItem } from 'primeng/api';
import { AllClickEvent, Column, SoftDataTableComponent } from 'src/app/core/components/soft-data-table/soft-data-table.component';
import { TableFilter } from 'src/app/core/entities/table-filter';
import { LazyLoadSelectedIdsResult } from 'src/app/core/entities/lazy-load-selected-ids-result';
import { {{string.Join(", ", imports)}} } from '../../entities/{{projectName.FromPascalToKebabCase()}}-entities.generated';
""";
        }

    }
}
