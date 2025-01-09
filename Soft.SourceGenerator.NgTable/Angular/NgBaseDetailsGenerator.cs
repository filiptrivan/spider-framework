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

            // E:\Projects\PlayertyLoyals\API\PlayertyLoyals.Business -> E:\Projects\PlayertyLoyals\Angular\src\app\business\components\base-details\{projectName}.ts
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

            foreach (SoftClass entity in entities)
            {
                if (entity.IsManyToMany())
                    continue;

                List<SoftProperty> properties = entity.Properties;

                SoftClass customDTOClass = customDTOClasses.Where(x => x.Name.Replace("DTO", "") == entity.Name).SingleOrDefault();

                if (customDTOClass != null)
                {
                    List<SoftProperty> customDTOClassProperties = customDTOClass.Properties;

                    properties.AddRange(customDTOClassProperties);
                }

                result.Add($$"""
@Component({
    selector: '{{entity.Name.FromPascalToKebabCase()}}-base-details',
    template:`
<ng-container *transloco="let t">
    <soft-panel>
        <panel-header></panel-header>

        <panel-body>
            <form class="grid">
{{string.Join("\n", GetPropertyBlocks(properties, entity))}}
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
    ]
})
export class {{entity.Name}}BaseComponent {
    @Input() {{entity.Name.FirstCharToLower()}}FormGroup: SoftFormGroup<{{entity.Name}}>;
    @Input() onSave: (reroute?: boolean) => void; 

    constructor(
        private apiService: ApiService
    ) {

    }

    ngOnInit(){

    }

    control(formControlName: keyof {{entity.Name}}, formGroup: SoftFormGroup){
        return getControl<{{entity.Name}}>(formControlName, formGroup);
    }

}
""");
            }

            return result;
        }

        private static List<string> GetPropertyBlocks(List<SoftProperty> properties, SoftClass entity)
        {
            List<string> result = new List<string>();

            foreach (SoftProperty property in properties
                .Where(x =>
                    x.Name != "Version" &&
                    x.Name != "Id" &&
                    x.Name != "CreatedAt" &&
                    x.Name != "ModifiedAt" &&
                    x.Type.IsEnumerable() == false))
            {
                string controlType = GetUIStringControlType(GetUIControlType(property));

                result.Add($$"""
                <div class="{{GetUIColWidth(property)}}">
                    <{{controlType}} [control]="control('{{GetFormControlName(property)}}', {{entity.Name.Replace("DTO", "").FirstCharToLower()}}FormGroup)" {{GetControlAttributes(property, entity)}}></{{controlType}}>
                </div>
""");
            }

            return result;
        }

        private static string GetFormControlName(SoftProperty property)
        {
            if (property.Type.IsManyToOneType())
                return $"{property.Name.FirstCharToLower()}Id";

            return property.Name.FirstCharToLower();
        }

        private static string GetControlAttributes(SoftProperty property, SoftClass entity)
        {
            UIControlType controlType = GetUIControlType(property);

            if (controlType == UIControlType.Decimal)
            {
                return $"[decimal]=\"true\" [maxFractionDigits]=\"{property.GetDecimalScale()}\"";
            }
            else if (controlType == UIControlType.File)
            {
                return $"[fileData]=\"{entity.Name.FirstCharToLower()}FormGroup.controls.{property.Name.FirstCharToLower()}Data.getRawValue()\" [objectId]=\"{entity.Name.FirstCharToLower()}FormGroup.controls.id.getRawValue()\"";
            }
            else if (controlType == UIControlType.Dropdown)
            {
                return $"[options]=\"{property.Name.FirstCharToLower()}Options\"";
            }

            return null;
        }

        private static string GetUIColWidth(SoftProperty property)
        {
            SoftAttribute uiColWidthAttribute = property.Attributes.Where(x => x.Name == "UIColWidth").SingleOrDefault();

            if (uiColWidthAttribute != null)
                return uiColWidthAttribute.Value;

            UIControlType controlType = GetUIControlType(property);

            if (controlType == UIControlType.File ||
                controlType == UIControlType.TextArea)
            {
                return "col-12";
            }

            return "col-12 md:col-6";
        }

        private static UIControlType GetUIControlType(SoftProperty property)
        {
            SoftAttribute uiControlTypeAttribute = property.Attributes.Where(x => x.Name == "UIControlType").SingleOrDefault();

            if (uiControlTypeAttribute != null)
            {
                Enum.TryParse(uiControlTypeAttribute.Value, out UIControlType parseResult);
                return parseResult;
            }

            if (property.IsBlob())
                return UIControlType.File;

            if (property.Type.IsManyToOneType())
                return UIControlType.Autocomplete;

            switch (property.Type)
            {
                case "string":
                    return UIControlType.TextBox;
                case "bool":
                case "bool?":
                    return UIControlType.CheckBox;
                case "DateTime":
                case "DateTime?":
                    return UIControlType.Calendar;
                case "decimal":
                case "decimal?":
                case "float":
                case "float?":
                case "double":
                case "double?":
                    return UIControlType.Decimal;
                case "long":
                case "long?":
                case "int":
                case "int?":
                case "byte":
                case "byte?":
                    return UIControlType.Integer;
                default:
                    break;
            }

            return UIControlType.TODO;
        }

        private static string GetUIStringControlType(UIControlType controlType)
        {
            switch (controlType)
            {
                case UIControlType.Autocomplete:
                    return "soft-autocomplete";
                case UIControlType.Calendar:
                    return "soft-calendar";
                case UIControlType.CheckBox:
                    return "soft-checkbox";
                case UIControlType.ColorPick:
                    return "soft-colorpick";
                case UIControlType.Dropdown:
                    return "soft-dropdown";
                case UIControlType.Editor:
                    return "soft-editor";
                case UIControlType.File:
                    return "soft-file";
                case UIControlType.MultiAutocomplete:
                    return "soft-multiautocomplete";
                case UIControlType.MultiSelect:
                    return "soft-multiselect";
                case UIControlType.Integer:
                case UIControlType.Decimal:
                    return "soft-number";
                case UIControlType.Password:
                    return "soft-password";
                case UIControlType.TextArea:
                    return "soft-textarea";
                case UIControlType.TextBlock:
                    return "soft-textblock";
                case UIControlType.TextBox:
                    return "soft-textbox";
                case UIControlType.TODO:
                    return "TODO";
                default:
                    return "TODO";

            }
        }

        private static string GetImports(List<SoftClass> customDTOClasses, List<SoftClass> entities, string projectName)
        {
            List<string> classNamesForImport = customDTOClasses.Concat(entities).Select(x => x.Name.Replace("DTO", "")).Distinct().ToList();

            return $$"""
import { CommonModule } from '@angular/common';
import { Component, Input } from '@angular/core';
import { PrimengModule } from 'src/app/core/modules/primeng.module';
import { ApiService } from '../../services/api/api.service';
import { TranslocoDirective } from '@jsverse/transloco';
import { SoftControlsModule } from 'src/app/core/controls/soft-controls.module';
import { getControl } from 'src/app/core/services/helper-functions';
import { SoftFormGroup } from 'src/app/core/components/soft-form-control/soft-form-control';
import { {{string.Join(", ", classNamesForImport)}} } from '../../entities/{{projectName.FromPascalToKebabCase()}}-entities.generated';
""";
        }

    }
}
