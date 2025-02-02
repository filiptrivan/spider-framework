using OfficeOpenXml;
using System.Globalization;
using System.Reflection;
using Spider.Shared.Excel.DTO;
using System.Drawing;
using OfficeOpenXml.Style;
using OfficeOpenXml.Table;

namespace Spider.Shared.Excel
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

        /// <summary>
        /// https://stackoverflow.com/questions/36637882/epplus-read-excel-table
        /// </summary>
        public static IEnumerable<T> ConvertTableToObjects<T>(ExcelTable table) where T : new()
        {
            //DateTime Conversion
            var convertDateTime = new Func<double, DateTime>(excelDate =>
            {
                if (excelDate < 1)
                    throw new ArgumentException("Excel dates cannot be smaller than 0.");

                var dateOfReference = new DateTime(1900, 1, 1);

                if (excelDate > 60d)
                    excelDate = excelDate - 2;
                else
                    excelDate = excelDate - 1;
                return dateOfReference.AddDays(excelDate);
            });

            //Get the properties of T
            var tprops = (new T())
                .GetType()
                .GetProperties()
                .ToList();

            //Get the cells based on the table address
            var start = table.Address.Start;
            var end = table.Address.End;
            var cells = new List<ExcelRangeBase>();

            //Have to use for loops insteadof worksheet.Cells to protect against empties
            for (var r = start.Row; r <= end.Row; r++)
                for (var c = start.Column; c <= end.Column; c++)
                    cells.Add(table.WorkSheet.Cells[r, c]);

            var groups = cells
                .GroupBy(cell => cell.Start.Row)
                .ToList();

            //Assume the second row represents column data types (big assumption!)
            var types = groups
                .Skip(1)
                .First()
                .Select(rcell => rcell.Value.GetType())
                .ToList();

            //Assume first row has the column names
            var colnames = groups
                .First()
                .Select((hcell, idx) => new { Name = hcell.Value.ToString(), index = idx })
                .Where(o => tprops.Select(p => p.Name).Contains(o.Name))
                .ToList();

            //Everything after the header is data
            var rowvalues = groups
                .Skip(1) //Exclude header
                .Select(cg => cg.Select(c => c.Value).ToList());

            //Create the collection container
            var collection = rowvalues
                .Select(row =>
                {
                    var tnew = new T();
                    colnames.ForEach(colname =>
                    {
                        //This is the real wrinkle to using reflection - Excel stores all numbers as double including int
                        var val = row[colname.index];
                        var type = types[colname.index];
                        var prop = tprops.First(p => p.Name == colname.Name);

                        //If it is numeric it is a double since that is how excel stores all numbers
                        if (type == typeof(double))
                        {
                            if (!string.IsNullOrWhiteSpace(val?.ToString()))
                            {
                                //Unbox it
                                var unboxedVal = (double)val;

                                //FAR FROM A COMPLETE LIST!!!
                                if (prop.PropertyType == typeof(Int32))
                                    prop.SetValue(tnew, (int)unboxedVal);
                                else if (prop.PropertyType == typeof(double))
                                    prop.SetValue(tnew, unboxedVal);
                                else if (prop.PropertyType == typeof(DateTime))
                                    prop.SetValue(tnew, convertDateTime(unboxedVal));
                                else
                                    throw new NotImplementedException(String.Format("Type '{0}' not implemented yet!", prop.PropertyType.Name));
                            }
                        }
                        else
                        {
                            //Its a string
                            prop.SetValue(tnew, val);
                        }
                    });

                    return tnew;
                });


            //Send it back
            return collection;
        }
    }
}
