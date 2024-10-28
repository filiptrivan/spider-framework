using OfficeOpenXml;
using System.Globalization;
using Nucleus.Core.Caching;
using System.Reflection;
using Soft.Generator.Shared.Excel.DTO;
using System.Drawing;
using OfficeOpenXml.Style;
using Soft.Generator.Shared.DTO;
using Riok.Mapperly.Abstractions;
using System;
using OfficeOpenXml.Table;
using System.ComponentModel;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Math;

namespace Soft.Generator.Shared.Excel
{
    public class ExcelService
    {
        private string _excelTemplatesFullPath;
        public string ExcelTemplatesFullPath
        {
            get
            {
                if (_excelTemplatesFullPath == null)
                {
                    _excelTemplatesFullPath = "Excel\\ExcelTemplates";
                    if (!Path.IsPathRooted(_excelTemplatesFullPath))
                    {
                        _excelTemplatesFullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _excelTemplatesFullPath);
                    }
                    if (!Directory.Exists(_excelTemplatesFullPath))
                    {
                        throw new DirectoryNotFoundException(string.Format(CultureInfo.CurrentCulture, "Excel templates path \"{0}\" does not exist.", _excelTemplatesFullPath));
                    }
                }
                return _excelTemplatesFullPath;
            }
        }

        public MemoryStream LoadExcelToMemoryStream(string excelName)
        {
            string fileName = $"{excelName}.xlsx";
            string filePath = Path.Combine(ExcelTemplatesFullPath, fileName);
            FileStream fileStream = File.Open(filePath, FileMode.Open);
            MemoryStream mem = new MemoryStream();
            fileStream.CopyTo(mem);
            return mem;
        }

        public MemoryStream FillReportTemplate<T>(IList<T> data, int count, string[] excelPropertiesToExclude, ExcelReportOptionsDTO options = null)
            where T : class
        {
            if (options == null)
                options = new ExcelReportOptionsDTO();

            MemoryStream outputStream = new MemoryStream();

            using (ExcelPackage excel = new ExcelPackage())
            {
                if (data != null && count > 0)
                {
                    ExcelWorksheet sheet = excel.Workbook.Worksheets.Add(options.DataSheetName);
                    Type type = typeof(T);
                    PropertyInfo[] propertiesToInclude = GetMembersToInclude(excelPropertiesToExclude, type);

                    LoadFromCollectionOverride(data, count, type, sheet, propertiesToInclude);
                }
                excel.SaveAs(outputStream);
            }

            outputStream.Position = 0;
            return outputStream;
        }

        private static PropertyInfo[] GetMembersToInclude(string[] excelPropertiesToExclude, Type type)
        {
            PropertyInfo[] memberInfos = type
                .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                // uzmi svaki property koji nema isto ime kao parametar iz customAttributeDataList
                .Where(prop => excelPropertiesToExclude.Contains(prop.Name) == false)
                .ToArray();

            return memberInfos;
        }

        private static void LoadFromCollectionOverride<T>(IList<T> data, int count, Type typeofT, ExcelWorksheet sheet, PropertyInfo[] propertiesToInclude)
        {
            int cellRow = 0;
            int cellCol = 0;
            for (int headerIndex = 0; headerIndex < propertiesToInclude.Length; headerIndex++)
            {
                cellCol = headerIndex+1;
                sheet.Cells[1, cellCol].Value = propertiesToInclude[headerIndex].Name;
                sheet.Cells[1, cellCol].Style.Fill.PatternType = ExcelFillStyle.Solid;
                sheet.Cells[1, cellCol].Style.Fill.BackgroundColor.SetColor(ColorTranslator.FromHtml("#F0F0F0"));
                sheet.Cells[1, cellCol].Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                sheet.Column(cellCol).Width = 22;

                for (int dataIndex = 0; dataIndex < count; dataIndex++)
                {
                    cellRow = dataIndex+2;
                    if (typeofT==typeof(string) || typeofT==typeof(decimal) || typeofT==typeof(DateTime) || typeofT.IsPrimitive)
                    {
                        sheet.Cells[cellRow, cellCol].Value = data[dataIndex];
                    }
                    else
                    {
                        sheet.Cells[cellRow, cellCol].Value = propertiesToInclude[headerIndex].GetValue(data[dataIndex], null);
                    }
                }

                if (propertiesToInclude[headerIndex].PropertyType==typeof(DateTime) || propertiesToInclude[headerIndex].PropertyType==typeof(DateTime?))
                {
                    sheet.Column(cellCol).Style.Numberformat.Format = "dd.MM.yyyy."; // TODO FT: make this with locale
                }
            }
        }
    }
}
