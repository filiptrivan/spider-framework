using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Spider.SourceGenerators.Angular;
using Spider.SourceGenerators.Enums;
using Spider.SourceGenerators.Shared;
using Spider.SourceGenerators.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Spider.SourceGenerators.Angular
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
            IncrementalValuesProvider<ClassDeclarationSyntax> classDeclarations = Helpers.GetClassInrementalValuesProvider(context.SyntaxProvider, new List<NamespaceExtensionCodes>
                {
                    NamespaceExtensionCodes.Entities,
                    NamespaceExtensionCodes.DTO
                });

            IncrementalValueProvider<List<SpiderClass>> referencedProjectClasses = Helpers.GetIncrementalValueProviderClassesFromReferencedAssemblies(context,
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

        private static void Execute(IList<ClassDeclarationSyntax> classes, List<SpiderClass> referencedProjectClasses, string callingProjectDirectory, SourceProductionContext context)
        {
            if (classes.Count <= 1)
                return; // FT: one because of config settings

            string[] namespacePartsWithoutLastElement = Helpers.GetNamespacePartsWithoutLastElement(classes[0]);
            string projectName = namespacePartsWithoutLastElement.LastOrDefault() ?? "ERROR"; // eg. Security

            // ...\API\PlayertyLoyals.Business -> ...\Angular\src\app\business\components\base-details\{projectName}.ts
            string outputPath = callingProjectDirectory.ReplaceEverythingAfter(@"\API\", $@"\Angular\src\app\business\components\base-details\{projectName.FromPascalToKebabCase()}-base-details.generated.ts");

            List<SpiderClass> spiderClasses = Helpers.GetSpiderClasses(classes, referencedProjectClasses);
            List<SpiderClass> customDTOClasses = spiderClasses.Where(x => x.Namespace.EndsWith(".DTO")).ToList();
            List<SpiderClass> currentProjectEntities = spiderClasses.Where(x => x.Namespace.EndsWith(".Entities")).ToList();
            List<SpiderClass> referencedProjectEntityClasses = referencedProjectClasses.Where(x => x.Namespace.EndsWith(".Entities")).ToList();
            List<SpiderClass> allEntities = currentProjectEntities.Concat(referencedProjectEntityClasses).ToList();

            string result = $$"""
{{GetImports(customDTOClasses, allEntities)}}

{{string.Join("\n\n", GetAngularBaseDetailsComponents(customDTOClasses, currentProjectEntities, allEntities))}}
""";

            Helpers.WriteToTheFile(result, outputPath);
        }

        private static List<string> GetAngularBaseDetailsComponents(List<SpiderClass> customDTOClasses, List<SpiderClass> currentProjectEntities, List<SpiderClass> allEntities)
        {
            List<string> result = new List<string>();

            foreach (SpiderClass entity in currentProjectEntities
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
    <spider-panel [isFirstMultiplePanel]="isFirstMultiplePanel" [isMiddleMultiplePanel]="isMiddleMultiplePanel" [isLastMultiplePanel]="isLastMultiplePanel">
        <panel-header></panel-header>

        <panel-body>
            @defer (when loading === false) {
                <form class="grid">
{{string.Join("\n", GetPropertyBlocks(entity.Properties, entity, allEntities, customDTOClasses))}}
                </form>
            } @placeholder {
                <card-skeleton [height]="502"></card-skeleton>
            }
        </panel-body>

        <panel-footer>
            <spider-button (onClick)="save()" [label]="t('Save')" icon="pi pi-save"></spider-button>
            @for (button of additionalButtons; track button.label) {
                <spider-button (onClick)="button.onClick()" [label]="button.label" [icon]="button.icon"></spider-button>
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
        IndexCardComponent,
        SpiderDataTableComponent,
    ]
})
export class {{entity.Name}}BaseDetailsComponent {
    @Output() onSave = new EventEmitter<void>();
    @Output() on{{entity.Name}}FormGroupInitFinish = new EventEmitter<void>();
    @Input() getCrudMenuForOrderedData: (formArray: SpiderFormArray, modelConstructor: BaseEntity, lastMenuIconIndexClicked: LastMenuIconIndexClicked, adjustFormArrayManually: boolean) => MenuItem[];
    @Input() formGroup: SpiderFormGroup;
    @Input() {{entity.Name.FirstCharToLower()}}FormGroup: SpiderFormGroup<{{entity.Name}}>;
    @Input() additionalButtons: SpiderButton[] = [];
    @Input() isFirstMultiplePanel: boolean = false;
    @Input() isMiddleMultiplePanel: boolean = false;
    @Input() isLastMultiplePanel: boolean = false;
    modelId: number;
    loading: boolean = true;

    {{entity.Name.FirstCharToLower()}}SaveBodyName: string = nameof<{{entity.Name}}SaveBody>('{{entity.Name.FirstCharToLower()}}DTO');

{{string.Join("\n\n", GetOrderedOneToManyVariables(entity, allEntities))}}

{{string.Join("\n", GetPrimengOptionVariables(entity.Properties, entity, allEntities))}}

{{string.Join("\n", GetSpiderFormControls(entity))}}

{{string.Join("\n", GetSimpleManyToManyTableLazyLoadVariables(entity, allEntities))}}

    constructor(
        private apiService: ApiService,
        private route: ActivatedRoute,
        private baseFormService: BaseFormService,
        private validatorService: ValidatorService,
        private translateLabelsService: TranslateLabelsService,
        private translocoService: TranslocoService,
    ) {}

    ngOnInit(){
        this.formGroup.initSaveBody = () => { 
            let saveBody = new {{entity.Name}}SaveBody();
            saveBody.{{entity.Name.FirstCharToLower()}}DTO = this.{{entity.Name.FirstCharToLower()}}FormGroup.getRawValue();
{{string.Join("\n", GetOrderedOneToManySaveBodyAssignements(entity, allEntities))}}
{{string.Join("\n", GetManyToManyMultiSelectSaveBodyAssignements(entity))}}
{{string.Join("\n", GetManyToManyMultiAutocompleteSaveBodyAssignements(entity))}}
{{string.Join("\n", GetSimpleManyToManyTableLazyLoadSaveBodyAssignements(entity))}}
            return saveBody;
        }

        this.formGroup.saveObservableMethod = this.apiService.save{{entity.Name}};
        this.formGroup.mainDTOName = this.{{entity.Name.FirstCharToLower()}}SaveBodyName;

        this.route.params.subscribe(async (params) => {
            this.modelId = params['id'];

{{string.Join("\n", GetManyToManyMultiSelectListForDropdownMethods(entity, allEntities))}}
{{string.Join("\n", GetSimpleManyToManyTableLazyLoadColsInitializations(entity, allEntities, customDTOClasses))}}

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
            this.{{entity.Name.FirstCharToLower()}}FormGroup, 
            this.formGroup, 
            {{entity.Name.FirstCharToLower()}}, 
            this.{{entity.Name.FirstCharToLower()}}SaveBodyName,
            [{{string.Join(", ", GetCustomOnChangeProperties(entity))}}]
        );
        this.{{entity.Name.FirstCharToLower()}}FormGroup.mainDTOName = this.{{entity.Name.FirstCharToLower()}}SaveBodyName;
        this.loading = false;
        this.on{{entity.Name}}FormGroupInitFinish.next();
    }

{{string.Join("\n", GetOrderedOneToManyInitFormArrayMethods(entity, allEntities))}}

{{string.Join("\n", GetOrderedOneToManyAddNewItemMethods(entity, allEntities))}}

{{string.Join("\n", GetSimpleManyToManyMethods(entity, allEntities))}}

{{string.Join("\n", GetAutocompleteSearchMethods(entity.Properties, entity, allEntities))}}

{{string.Join("\n", GetUploadImageMethods(entity.Properties, entity, allEntities))}}

    control(formControlName: string, formGroup: SpiderFormGroup){
        return getControl(formControlName, formGroup);
    }

    getFormArrayGroups<T>(formArray: SpiderFormArray): SpiderFormGroup<T>[]{
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

        private static List<string> GetSimpleManyToManyMethods(SpiderClass entity, List<SpiderClass> entities)
        {
            List<string> result = new List<string>();

            foreach (SpiderProperty property in entity.Properties.Where(x => x.HasSimpleManyToManyTableLazyLoadAttribute()))
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

        private static List<string> GetSimpleManyToManyTableLazyLoadColsInitializations(SpiderClass entity, List<SpiderClass> entities, List<SpiderClass> customDTOClasses)
        {
            List<string> result = new List<string>();

            foreach (SpiderProperty property in entity.Properties.Where(x => x.HasSimpleManyToManyTableLazyLoadAttribute()))
            {
                result.Add($$"""
            this.{{property.Name.FirstCharToLower()}}TableColsFor{{entity.Name}} = [
{{string.Join(",\n", GetSimpleManyToManyTableLazyLoadCols(property, entity, entities, customDTOClasses))}}
            ];
""");
            }

            return result;
        }

        private static List<string> GetSimpleManyToManyTableLazyLoadCols(SpiderProperty property, SpiderClass entity, List<SpiderClass> entities, List<SpiderClass> customDTOClasses)
        {
            List<string> result = new List<string>();

            foreach (UITableColumn col in property.GetUITableColumns())
            {
                SpiderClass extractedEntity = entities.Where(x => x.Name == Helpers.ExtractTypeFromGenericType(property.Type)).SingleOrDefault();
                SpiderProperty extractedEntityProperty = extractedEntity?.Properties?.Where(x => x.Name == col.Field.Replace("DisplayName", "").Replace("CommaSeparated", ""))?.SingleOrDefault();

                SpiderClass extractedDTO = customDTOClasses.Where(x => x.Name == $"{Helpers.ExtractTypeFromGenericType(property.Type)}DTO").SingleOrDefault();
                SpiderProperty extractedDTOProperty = extractedDTO?.Properties?.Where(x => x.Name == col.Field)?.SingleOrDefault();

                result.Add($$"""
                {name: this.translocoService.translate('{{col.TranslationKey}}'), filterType: '{{GetTableColFilterType(extractedEntityProperty ?? extractedDTOProperty)}}', field: '{{col.Field.FirstCharToLower()}}' {{GetTableColAdditionalProperties(extractedEntityProperty ?? extractedDTOProperty, entities)}} }
""");
            }

            return result;
        }

        private static string GetTableColAdditionalProperties(SpiderProperty property, List<SpiderClass> entities)
        {
            if (property.IsDropdownControlType())
                return $", filterField: '{property.Name.FirstCharToLower()}Id', dropdownOrMultiselectValues: await firstValueFrom(getPrimengNamebookListForDropdown(this.apiService.get{property.Type}ListForDropdown))";

            if (property.HasGenerateCommaSeparatedDisplayNameAttribute())
            {
                SpiderClass extractedEntity = entities.Where(x => x.Name == Helpers.ExtractTypeFromGenericType(property.Type)).SingleOrDefault();
                return $", dropdownOrMultiselectValues: await firstValueFrom(getPrimengNamebookListForDropdown(this.apiService.get{extractedEntity.Name}ListForDropdown))";
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

        private static string GetTableColFilterType(SpiderProperty property)
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

        private static List<string> GetSimpleManyToManyTableLazyLoadSaveBodyAssignements(SpiderClass entity)
        {
            List<string> result = new List<string>();

            foreach (SpiderProperty property in entity.Properties.Where(x => x.HasSimpleManyToManyTableLazyLoadAttribute()))
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

        private static List<string> GetSimpleManyToManyTableLazyLoadVariables(SpiderClass entity, List<SpiderClass> entities)
        {
            List<string> result = new List<string>();

            foreach (SpiderProperty property in entity.Properties.Where(x => x.HasSimpleManyToManyTableLazyLoadAttribute()))
            {
                SpiderClass extractedEntity = entities.Where(x => x.Name == Helpers.ExtractTypeFromGenericType(property.Type)).SingleOrDefault();

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

        private static List<string> GetManyToManyMultiSelectSaveBodyAssignements(SpiderClass entity)
        {
            List<string> result = new List<string>();

            foreach (SpiderProperty property in entity.Properties.Where(x => x.IsMultiSelectControlType()))
            {
                result.Add($$"""
            saveBody.selected{{property.Name}}Ids = this.selected{{property.Name}}For{{entity.Name}}.getRawValue();
""");
            }

            return result;
        }

        private static List<string> GetManyToManyMultiAutocompleteSaveBodyAssignements(SpiderClass entity)
        {
            List<string> result = new List<string>();

            foreach (SpiderProperty property in entity.Properties.Where(x => x.IsMultiAutocompleteControlType()))
            {
                result.Add($$"""
            saveBody.selected{{property.Name}}Ids = this.selected{{property.Name}}For{{entity.Name}}.getRawValue()?.map(n => n.value);
""");
            }

            return result;
        }

        private static List<string> GetManyToManyMultiSelectInitFormControls(SpiderClass entity)
        {
            List<string> result = new List<string>();

            foreach (SpiderProperty property in entity.Properties.Where(x => x.IsMultiSelectControlType()))
            {
                result.Add($$"""
                    this.selected{{property.Name}}For{{entity.Name}}.setValue(
                        {{property.Name.FirstCharToLower()}}For{{entity.Name}}.map(n => { return n.id })
                    );
""");
            }

            return result;
        }

        private static List<string> GetManyToManyMultiAutocompleteInitFormControls(SpiderClass entity)
        {
            List<string> result = new List<string>();

            foreach (SpiderProperty property in entity.Properties.Where(x => x.IsMultiAutocompleteControlType()))
            {
                result.Add($$"""
                    this.selected{{property.Name}}For{{entity.Name}}.setValue(
                        {{property.Name.FirstCharToLower()}}For{{entity.Name}}.map(n => ({ label: n.displayName, value: n.id }))
                    );
""");
            }

            return result;
        }

        private static List<string> GetManyToManyMultiSelectListForDropdownMethods(SpiderClass entity, List<SpiderClass> entities)
        {
            List<string> result = new List<string>();

            foreach (SpiderProperty property in entity.Properties
                .Where(x =>
                    x.IsMultiSelectControlType() ||
                    x.IsDropdownControlType()
                )
            )
            {
                SpiderClass extractedEntity = entities.Where(x => x.Name == Helpers.ExtractTypeFromGenericType(property.Type)).SingleOrDefault();

                result.Add($$"""
            getPrimengNamebookListForDropdown(this.apiService.get{{extractedEntity.Name}}ListForDropdown).subscribe(po => {
                this.{{property.Name.FirstCharToLower()}}For{{entity.Name}}Options = po;
            });
""");
            }

            return result;
        }

        private static List<string> GetManyToManyMultiControlTypesForkJoinParameters(SpiderClass entity)
        {
            List<string> result = new List<string>();

            foreach (SpiderProperty property in entity.Properties
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

        private static List<string> GetSpiderFormControls(SpiderClass entity)
        {
            List<string> result = new List<string>();

            foreach (SpiderProperty property in entity.Properties)
            {
                if (property.IsMultiSelectControlType())
                {
                    result.Add($$"""
    selected{{property.Name}}For{{entity.Name}} = new SpiderFormControl<number[]>(null, {updateOn: 'change'});
""");
                }
                else if (property.IsMultiAutocompleteControlType())
                {
                    result.Add($$"""
    selected{{property.Name}}For{{entity.Name}} = new SpiderFormControl<PrimengOption[]>(null, {updateOn: 'change'});
""");
                }
            }

            return result;
        }

        #region Ordered One To Many

        private static List<string> GetOrderedOneToManyAddNewItemMethods(SpiderClass entity, List<SpiderClass> entities)
        {
            List<string> result = new List<string>();

            foreach (SpiderProperty property in entity.GetOrderedOneToManyProperties())
            {
                SpiderClass extractedEntity = entities.Where(x => x.Name == Helpers.ExtractTypeFromGenericType(property.Type)).SingleOrDefault();

                result.Add($$"""
    addNewItemTo{{property.Name}}(index: number){ 
        this.baseFormService.addNewFormGroupToFormArray(
            this.{{property.Name.FirstCharToLower()}}FormArray, 
            new {{extractedEntity.Name}}({id: 0}), 
            index
        );
    }
""");
            }

            return result;
        }

        private static List<string> GetOrderedOneToManyInitFormArrayMethods(SpiderClass entity, List<SpiderClass> entities)
        {
            List<string> result = new List<string>();

            foreach (SpiderProperty property in entity.GetOrderedOneToManyProperties())
            {
                SpiderClass extractedEntity = entities.Where(x => x.Name == Helpers.ExtractTypeFromGenericType(property.Type)).SingleOrDefault();

                result.Add($$"""
    init{{property.Name}}FormArray({{property.Name.FirstCharToLower()}}: {{extractedEntity.Name}}[]){
        this.{{property.Name.FirstCharToLower()}}FormArray = this.baseFormService.initFormArray(
            this.formGroup, 
            {{property.Name.FirstCharToLower()}}, 
            this.{{property.Name.FirstCharToLower()}}Model, 
            this.{{property.Name.FirstCharToLower()}}SaveBodyName, 
            this.{{property.Name.FirstCharToLower()}}TranslationKey, 
            true
        );
        this.{{property.Name.FirstCharToLower()}}CrudMenu = this.getCrudMenuForOrderedData(this.{{property.Name.FirstCharToLower()}}FormArray, new {{extractedEntity.Name}}({id: 0}), this.{{property.Name.FirstCharToLower()}}LastIndexClicked, false);
{{GetFormArrayEmptyValidator(property)}}
    }
""");
            }

            return result;
        }

        private static string GetFormArrayEmptyValidator(SpiderProperty property)
        {
            if (property.HasRequiredAttribute())
            {
                return $$"""
        this.{{property.Name.FirstCharToLower()}}FormArray.validator = this.validatorService.isFormArrayEmpty(this.{{property.Name.FirstCharToLower()}}FormArray);
""";
            }

            return null;
        }

        private static List<string> GetOrderedOneToManyForkJoinParameters(SpiderClass entity)
        {
            List<string> result = new List<string>();

            foreach (SpiderProperty property in entity.GetOrderedOneToManyProperties())
            {
                result.Add($$"""
                    {{property.Name.FirstCharToLower()}}For{{entity.Name}}: this.apiService.getOrdered{{property.Name}}For{{entity.Name}}(this.modelId),
""");
            }

            return result;
        }

        private static List<string> GetOrderedOneToManyInitFormGroupForExistingObject(SpiderClass entity)
        {
            List<string> result = new List<string>();

            foreach (SpiderProperty property in entity.GetOrderedOneToManyProperties())
            {
                result.Add($$"""
                    this.init{{property.Name}}FormArray({{property.Name.FirstCharToLower()}}For{{entity.Name}});
""");
            }

            return result;
        }

        private static List<string> GetOrderedOneToManyInitFormGroupForNonExistingObject(SpiderClass entity)
        {
            List<string> result = new List<string>();

            foreach (SpiderProperty property in entity.GetOrderedOneToManyProperties())
            {
                result.Add($$"""
                this.init{{property.Name}}FormArray([]);
""");
            }

            return result;
        }

        private static List<string> GetOrderedOneToManySaveBodyAssignements(SpiderClass entity, List<SpiderClass> entities)
        {
            List<string> result = new List<string>();

            foreach (SpiderProperty property in entity.GetOrderedOneToManyProperties())
            {
                SpiderClass extractedEntity = entities.Where(x => x.Name == Helpers.ExtractTypeFromGenericType(property.Type)).SingleOrDefault();

                result.Add($$"""
            saveBody.{{property.Name.FirstCharToLower()}}DTO = this.{{property.Name.FirstCharToLower()}}FormArray.getRawValue();
""");
            }

            return result;
        }

        private static List<string> GetOrderedOneToManyVariables(SpiderClass entity, List<SpiderClass> entities)
        {
            List<string> result = new List<string>();

            foreach (SpiderProperty property in entity.GetOrderedOneToManyProperties())
            {
                SpiderClass extractedEntity = entities.Where(x => x.Name == Helpers.ExtractTypeFromGenericType(property.Type)).SingleOrDefault();

                result.Add($$"""
    {{property.Name.FirstCharToLower()}}Model: {{extractedEntity.Name}} = new {{extractedEntity.Name}}();
    {{property.Name.FirstCharToLower()}}SaveBodyName: string = nameof<{{extractedEntity.Name}}SaveBody>('{{extractedEntity.Name.FirstCharToLower()}}DTO');
    {{property.Name.FirstCharToLower()}}TranslationKey: string = new {{extractedEntity.Name}}().typeName;
    {{property.Name.FirstCharToLower()}}FormArray: SpiderFormArray<{{extractedEntity.Name}}>;
    {{property.Name.FirstCharToLower()}}LastIndexClicked: LastMenuIconIndexClicked = new LastMenuIconIndexClicked();
    {{property.Name.FirstCharToLower()}}CrudMenu: MenuItem[] = [];
""");
            }

            return result;
        }


        /// <summary>
        /// </summary>
        /// <param name="property">eg. List<SegmentationItem> SegmentationItems</param>
        /// <param name="allEntities"></param>
        /// <param name="customDTOClasses"></param>
        /// <returns></returns>
        private static string GetOrderedOneToManyBlock(SpiderProperty property, List<SpiderClass> allEntities, List<SpiderClass> customDTOClasses)
        {
            SpiderClass entity = allEntities.Where(x => x.Name == Helpers.ExtractTypeFromGenericType(property.Type)).SingleOrDefault(); // eg. SegmentationItem

            // Every property of SegmentationItem without the many to one reference (Segmentation) and enumerable properties
            List<SpiderProperty> propertyBlocks = entity.Properties
                .Where(x =>
                    x.WithMany() != property.Name &&
                    x.Type.IsEnumerable() == false
                )
                .ToList();

            return $$"""
                 <div class="col-12">
                    <spider-panel>
                        <panel-header [title]="t('{{property.Name}}')" icon="pi pi-list"></panel-header>
                        <panel-body [normalBottomPadding]="true">
                            @for ({{entity.Name.FirstCharToLower()}}FormGroup of getFormArrayGroups({{property.Name.FirstCharToLower()}}FormArray); track {{entity.Name.FirstCharToLower()}}FormGroup; let index = $index; let last = $last) {
                                <index-card [index]="index" [last]="false" [crudMenu]="{{property.Name.FirstCharToLower()}}CrudMenu" (onMenuIconClick)="{{property.Name.FirstCharToLower()}}LastIndexClicked.index = $event">
                                    <form [formGroup]="{{entity.Name.FirstCharToLower()}}FormGroup" class="grid">
{{string.Join("\n", GetPropertyBlocks(propertyBlocks, entity, allEntities, customDTOClasses))}}
                                    </form>
                                </index-card>
                            }

                            <div class="panel-add-button">
                                <spider-button (onClick)="addNewItemTo{{property.Name}}(null)" [label]="t('AddNew{{Helpers.ExtractTypeFromGenericType(property.Type)}}')" icon="pi pi-plus"></spider-button>
                            </div>

                        </panel-body>
                    </spider-panel>
                </div>       
""";
        }

        #endregion

        private static List<string> GetCustomOnChangeProperties(SpiderClass entity)
        {
            List<string> result = new List<string>();

            foreach (SpiderProperty property in entity.Properties)
            {
                if (property.IsColorControlType())
                {
                    result.Add($"'{property.Name.FirstCharToLower()}'");
                }
            }

            return result;
        }

        private static List<string> GetPrimengOptionVariables(List<SpiderProperty> properties, SpiderClass entity, List<SpiderClass> entities)
        {
            List<string> result = new List<string>();

            foreach (SpiderProperty property in properties.Where(x => x.Attributes.Any(x => x.Name == "UIDoNotGenerate") == false))
            {
                if (property.Attributes.Any(x => x.Name == "UIOrderedOneToMany"))
                {
                    SpiderClass extractedEntity = entities.Where(x => x.Name == Helpers.ExtractTypeFromGenericType(property.Type)).SingleOrDefault();
                    List<SpiderProperty> extractedProperties = extractedEntity.Properties
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

        private static List<string> GetAutocompleteSearchMethods(List<SpiderProperty> properties, SpiderClass entity, List<SpiderClass> entities)
        {
            List<string> result = new List<string>();

            foreach (SpiderProperty property in properties.Where(x => x.Attributes.Any(x => x.Name == "UIDoNotGenerate") == false))
            {
                if (property.Attributes.Any(x => x.Name == "UIOrderedOneToMany"))
                {
                    SpiderClass extractedEntity = entities.Where(x => x.Name == Helpers.ExtractTypeFromGenericType(property.Type)).SingleOrDefault();
                    List<SpiderProperty> extractedProperties = extractedEntity.Properties
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
                    controlType == UIControlTypeCodes.MultiAutocomplete)
                {
                    result.Add($$"""
    search{{property.Name}}For{{entity.Name}}(event: AutoCompleteCompleteEvent) {
        getPrimengNamebookListForAutocomplete(this.apiService.get{{Helpers.ExtractTypeFromGenericType(property.Type)}}ListForAutocomplete, 50, event?.query ?? '').subscribe(po => {
            this.{{property.Name.FirstCharToLower()}}For{{entity.Name}}Options = po;
        });
    }
""");

                }

            }

            return result;
        }

        private static List<string> GetUploadImageMethods(List<SpiderProperty> properties, SpiderClass entity, List<SpiderClass> entities)
        {
            List<string> result = new List<string>();

            foreach (SpiderProperty property in properties.Where(x => x.Attributes.Any(x => x.Name == "UIDoNotGenerate") == false))
            {
                if (property.Attributes.Any(x => x.Name == "UIOrderedOneToMany"))
                {
                    SpiderClass extractedEntity = entities.Where(x => x.Name == Helpers.ExtractTypeFromGenericType(property.Type)).SingleOrDefault();
                    List<SpiderProperty> extractedProperties = extractedEntity.Properties
                        .Where(x =>
                            x.WithMany() != property.Name &&
                            x.Type.IsEnumerable() == false
                        )
                        .ToList();

                    GetUploadImageMethods(extractedProperties, extractedEntity, entities);

                    continue;
                }

                UIControlTypeCodes controlType = GetUIControlType(property);

                if (controlType == UIControlTypeCodes.File)
                {
                    result.Add($$"""
    upload{{property.Name}}For{{entity.Name}}(event: SpiderFileSelectEvent){
        this.apiService.upload{{property.Name}}For{{entity.Name}}(event.formData).subscribe((completeFileName: string) => {
            this.{{entity.Name.FirstCharToLower()}}FormGroup.controls.{{property.Name.FirstCharToLower()}}.setValue(completeFileName);
        });
    }
""");

                }

            }

            return result;
        }

        private static List<string> GetForkJoinParameterNames(SpiderClass entity)
        {
            List<string> result = new List<string>();

            result.Add(entity.Name.FirstCharToLower());

            foreach (SpiderProperty property in entity.Properties)
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
            List<SpiderProperty> properties,
            SpiderClass entity,
            List<SpiderClass> allEntities,
            List<SpiderClass> customDTOClasses
        )
        {
            List<string> result = new List<string>();

            SpiderClass customDTOClass = customDTOClasses.Where(x => x.Name.Replace("DTO", "") == entity.Name).SingleOrDefault();

            if (customDTOClass != null)
            {
                List<SpiderProperty> customDTOClassProperties = customDTOClass.Properties;

                properties.AddRange(customDTOClassProperties);
            }

            foreach (SpiderProperty property in GetPropertiesForUIBlocks(properties))
            {
                if (property.Attributes.Any(x => x.Name == "UIOrderedOneToMany"))
                {
                    result.Add(GetOrderedOneToManyBlock(property, allEntities, customDTOClasses));

                    continue;
                }

                string controlType = GetUIStringControlType(GetUIControlType(property));

                result.Add($$"""
                    <div class="{{GetUIControlWidth(property)}}">
                        <{{controlType}} {{GetControlAttributes(property, entity)}}></{{controlType}}>
                    </div>
""");
            }

            return result;
        }

        private static string GetControlHtmlAttributeValue(SpiderProperty property, SpiderClass entity)
        {
            if (property.IsMultiSelectControlType() ||
                property.IsMultiAutocompleteControlType())
            {
                return $"selected{property.Name}For{entity.Name}";
            }

            return $"control('{GetFormControlName(property)}', {entity.Name.FirstCharToLower()}FormGroup)";
        }

        private static List<SpiderProperty> GetPropertiesForUIBlocks(List<SpiderProperty> properties)
        {
            List<SpiderProperty> orderedProperties = properties
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

        private static string GetFormControlName(SpiderProperty property)
        {
            if (property.Type.IsManyToOneType())
                return $"{property.Name.FirstCharToLower()}Id";

            return property.Name.FirstCharToLower();
        }

        private static string GetControlAttributes(SpiderProperty property, SpiderClass entity)
        {
            UIControlTypeCodes controlType = GetUIControlType(property);

            if (controlType == UIControlTypeCodes.Decimal)
            {
                return $"[control]=\"{GetControlHtmlAttributeValue(property, entity)}\" [decimal]=\"true\" [maxFractionDigits]=\"{property.GetDecimalScale()}\"";
            }
            else if (controlType == UIControlTypeCodes.File)
            {
                return $"[control]=\"{GetControlHtmlAttributeValue(property, entity)}\" [fileData]=\"{entity.Name.FirstCharToLower()}FormGroup.controls.{property.Name.FirstCharToLower()}Data.getRawValue()\" [objectId]=\"{entity.Name.FirstCharToLower()}FormGroup.controls.id.getRawValue()\" (onSelectedFile)=\"upload{property.Name}For{entity.Name}($event)\"";
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

        private static string GetUIControlWidth(SpiderProperty property)
        {
            SpiderAttribute uiControlWidthAttribute = property.Attributes.Where(x => x.Name == "UIControlWidth").SingleOrDefault();

            if (uiControlWidthAttribute != null)
                return uiControlWidthAttribute.Value;

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

        private static UIControlTypeCodes GetUIControlType(SpiderProperty property)
        {
            SpiderAttribute uiControlTypeAttribute = property.Attributes.Where(x => x.Name == "UIControlType").SingleOrDefault();

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
                    return "spider-autocomplete";
                case UIControlTypeCodes.Calendar:
                    return "spider-calendar";
                case UIControlTypeCodes.CheckBox:
                    return "spider-checkbox";
                case UIControlTypeCodes.ColorPick:
                    return "spider-colorpick";
                case UIControlTypeCodes.Dropdown:
                    return "spider-dropdown";
                case UIControlTypeCodes.Editor:
                    return "spider-editor";
                case UIControlTypeCodes.File:
                    return "spider-file";
                case UIControlTypeCodes.MultiAutocomplete:
                    return "spider-multiautocomplete";
                case UIControlTypeCodes.MultiSelect:
                    return "spider-multiselect";
                case UIControlTypeCodes.Integer:
                case UIControlTypeCodes.Decimal:
                    return "spider-number";
                case UIControlTypeCodes.Password:
                    return "spider-password";
                case UIControlTypeCodes.TextArea:
                    return "spider-textarea";
                case UIControlTypeCodes.TextBlock:
                    return "spider-textblock";
                case UIControlTypeCodes.TextBox:
                    return "spider-textbox";
                case UIControlTypeCodes.Table:
                    return "spider-data-table";
                case UIControlTypeCodes.TODO:
                    return "TODO";
                default:
                    return "TODO";

            }
        }

        private static string GetImports(List<SpiderClass> customDTOClasses, List<SpiderClass> entities)
        {
            List<AngularImport> customDTOImports = customDTOClasses
                .Select(x => new AngularImport
                {
                    Namespace = x.Namespace.Replace(".DTO", ""),
                    Name = x.Name.Replace("DTO", "")
                })
                .ToList();

            List<AngularImport> entityImports = entities
                .Select(x => new AngularImport
                {
                    Namespace = x.Namespace.Replace(".Entities", ""),
                    Name = x.Name
                })
                .ToList();

            List<AngularImport> saveBodyImports = entities
                .Select(x => new AngularImport
                {
                    Namespace = x.Namespace.Replace(".Entities", ""),
                    Name = $"{x.Name}SaveBody"
                })
                .ToList();

            List<AngularImport> imports = customDTOImports.Concat(entityImports).Concat(saveBodyImports).ToList();

            return $$"""
import { ValidatorService } from 'src/app/business/services/validators/validators';
import { TranslateLabelsService } from '../../services/translates/merge-labels';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { Component, EventEmitter, Input, Output } from '@angular/core';
import { ApiService } from '../../services/api/api.service';
import { TranslocoDirective, TranslocoService } from '@jsverse/transloco';
import { AutoCompleteCompleteEvent } from 'primeng/autocomplete';
import { ActivatedRoute } from '@angular/router';
import { firstValueFrom, forkJoin, Observable } from 'rxjs';
import { MenuItem } from 'primeng/api';
import { PrimengModule, SpiderControlsModule, CardSkeletonComponent, IndexCardComponent, SpiderDataTableComponent, SpiderFormArray, BaseEntity, LastMenuIconIndexClicked, SpiderFormGroup, SpiderButton, nameof, BaseFormService, getControl, Column, TableFilter, LazyLoadSelectedIdsResult, AllClickEvent, SpiderFileSelectEvent, getPrimengNamebookListForDropdown, PrimengOption, SpiderFormControl, getPrimengNamebookListForAutocomplete } from '@playerty/spider';
{{string.Join("\n", GetDynamicNgImports(imports))}}
""";
        }

        /// <summary>
        /// Key - Namespace
        /// Value - Name of the class to import in Angular
        /// </summary>
        private static List<string> GetDynamicNgImports(List<AngularImport> imports)
        {
            List<string> result = new List<string>();

            foreach (var projectImports in imports.GroupBy(x => x.Namespace))
            {
                string projectName = projectImports.Key.Split('.').Last(); // eg. Security

                if (projectName == "Shared" ||
                    projectName == "Security")
                    continue;

                result.Add($$"""
import { {{string.Join(", ", projectImports.DistinctBy(x => x.Name).Select(x => x.Name))}} } from '../../entities/{{projectName.FromPascalToKebabCase()}}-entities.generated';
""");
            }

            return result;
        }
    }
}
