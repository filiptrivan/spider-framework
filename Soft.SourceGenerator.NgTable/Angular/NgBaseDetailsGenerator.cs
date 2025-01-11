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
                    //NamespaceExtensionCodes.Entities,
                    //NamespaceExtensionCodes.DTO,
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

            List<SoftClass> softClasses = Helper.GetSoftClasses(classes);
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
    <soft-panel>
        <panel-header></panel-header>

        <panel-body>
            <form class="grid">
{{string.Join("\n", GetPropertyBlocks(entity.Properties, entity, entities, customDTOClasses))}}
            </form>
        </panel-body>

        <panel-footer>
            <p-button (onClick)="onSave()" [label]="t('Save')" icon="pi pi-save"></p-button>
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
        IndexCardComponent
    ]
})
export class {{entity.Name}}BaseComponent {
    @Input() {{entity.Name.FirstCharToLower()}}FormGroup: SoftFormGroup<{{entity.Name}}>;
    @Input() onSave: (reroute?: boolean) => void; 

{{string.Join("\n", GetPrimengOptionVariables(entity.Properties, entity, entities))}}

    constructor(
        private apiService: ApiService
    ) {

    }

    ngOnInit(){

    }

{{string.Join("\n\n", GetAutocompleteSearchMethods(entity.Properties, entity, entities))}}

    control(formControlName: keyof {{entity.Name}}, formGroup: SoftFormGroup){
        return getControl<{{entity.Name}}>(formControlName, formGroup);
    }

}
""");
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
                    controlType == UIControlTypeCodes.Dropdown)
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
                    controlType == UIControlTypeCodes.Dropdown)
                {
                    result.Add($$"""
    search{{property.Name}}For{{entity.Name}}(event: AutoCompleteCompleteEvent) {
        this.apiService.getPrimengNamebookListForAutocomplete(this.apiService.get{{property.Type}}ListForAutocomplete, 50, event.query).subscribe(po => {
            this.{{property.Name.FirstCharToLower()}}For{{entity.Name}}Options = po;
        });
    }
""");

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
                    <{{controlType}} [control]="control('{{GetFormControlName(property)}}', {{entity.Name.FirstCharToLower()}}FormGroup)" {{GetControlAttributes(property, entity)}}></{{controlType}}>
                </div>
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

        private static List<SoftProperty> GetPropertiesForUIBlocks(List<SoftProperty> properties)
        {
            List<SoftProperty> orderedProperties = properties
                .Where(x =>
                    x.Name != "Version" &&
                    x.Name != "Id" &&
                    x.Name != "CreatedAt" &&
                    x.Name != "ModifiedAt" &&
                    (
                        x.Type.IsEnumerable() == false
                        || x.Attributes.Any(x => x.Name == "UIOrderedOneToMany")
                    ) &&
                    x.Attributes.Any(x => x.Name == "UIDoNotGenerate") == false
                )
                .OrderBy(x =>
                    x.Attributes.Any(attr => attr.Name == "BlobName") ? 0 :
                    x.Attributes.Any(attr => attr.Value == "TextArea") ? 2 : 1)
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
                return $"[decimal]=\"true\" [maxFractionDigits]=\"{property.GetDecimalScale()}\"";
            }
            else if (controlType == UIControlTypeCodes.File)
            {
                return $"[fileData]=\"{entity.Name.FirstCharToLower()}FormGroup.controls.{property.Name.FirstCharToLower()}Data.getRawValue()\" [objectId]=\"{entity.Name.FirstCharToLower()}FormGroup.controls.id.getRawValue()\"";
            }
            else if (controlType == UIControlTypeCodes.Dropdown)
            {
                return $"[options]=\"{property.Name.FirstCharToLower()}For{entity.Name}Options\"";
            }
            else if (controlType == UIControlTypeCodes.Autocomplete)
            {
                return $"[options]=\"{property.Name.FirstCharToLower()}For{entity.Name}Options\" (onTextInput)=\"search{property.Name}For{entity.Name}($event)\"";
            }

            return null;
        }

        private static string GetUIColWidth(SoftProperty property)
        {
            SoftAttribute uiColWidthAttribute = property.Attributes.Where(x => x.Name == "UIColWidth").SingleOrDefault();

            if (uiColWidthAttribute != null)
                return uiColWidthAttribute.Value;

            UIControlTypeCodes controlType = GetUIControlType(property);

            if (controlType == UIControlTypeCodes.File ||
                controlType == UIControlTypeCodes.TextArea)
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
                case UIControlTypeCodes.TODO:
                    return "TODO";
                default:
                    return "TODO";

            }
        }

        private static string GetImports(List<SoftClass> customDTOClasses, List<SoftClass> entities, string projectName)
        {
            List<string> classNamesForImport = customDTOClasses.Concat(entities).Select(x => x.Name.Replace("DTO", "")).Distinct().ToList();

            return $$"""
import { ValidatorService } from 'src/app/business/services/validators/validation-rules';
import { BaseFormService } from './../../../core/services/base-form.service';
import { CommonModule } from '@angular/common';
import { Component, Input } from '@angular/core';
import { PrimengModule } from 'src/app/core/modules/primeng.module';
import { ApiService } from '../../services/api/api.service';
import { TranslocoDirective } from '@jsverse/transloco';
import { SoftControlsModule } from 'src/app/core/controls/soft-controls.module';
import { SoftFormArray, SoftFormGroup } from 'src/app/core/components/soft-form-control/soft-form-control';
import { PrimengOption } from 'src/app/core/entities/primeng-option';
import { AutoCompleteCompleteEvent } from 'primeng/autocomplete';
import { getControl, nameof } from 'src/app/core/services/helper-functions';
import { ActivatedRoute } from '@angular/router';
import { forkJoin, Observable } from 'rxjs';
import { BaseEntity } from 'src/app/core/entities/base-entity';
import { CardSkeletonComponent } from "../../../core/components/card-skeleton/card-skeleton.component";
import { SoftButton } from 'src/app/core/entities/soft-button';
import { IndexCardComponent } from 'src/app/core/components/index-card/index-card.component';
import { LastMenuIconIndexClicked } from 'src/app/core/entities/last-menu-icon-index-clicked';
import { MenuItem } from 'primeng/api';
import { {{string.Join(", ", classNamesForImport)}} } from '../../entities/{{projectName.FromPascalToKebabCase()}}-entities.generated';
""";
        }

    }
}
