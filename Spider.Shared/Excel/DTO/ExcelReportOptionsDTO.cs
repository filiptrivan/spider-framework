using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spider.Shared.Excel.DTO
{
    public class ExcelReportOptionsDTO
    {
        /// <summary>
        /// List of column headers that will be added to the right, starting from <see cref="AdditionalDataStartColumn"/> column. 
        /// If <see cref="AdditionalDataStartColumn"/> is null, headers will not be added.
        /// </summary>
        public string[] AdditionalColumnHeaders { get; set; }
        /// <summary>
        /// Column number from where are additional columns be inserted to the right. Required if <see cref="AdditionalColumnHeaders"/> should be applied.
        /// </summary>
        public int? AdditionalDataStartColumn { get; set; }

        /// <summary>
        /// Name of the excel sheet to which excel data is rendered. Default value is 'Data'
        /// </summary>
        public string DataSheetName { get; set; } = "Data";

        /// <summary>
        /// Name of the excel sheet to which excel data is rendered. Default value is 'Data'
        /// </summary>
        public string DataSheetName2 { get; set; } = "Data2";

        /// <summary>
        /// Start row number from where is data inserted row by row, one based. Default is 2.
        /// </summary>
        public int DataStartRow { get; set; } = 2;

        /// <summary>
        /// Start column number from where is data inserted column by column, one based. Default is 1.
        /// </summary>
        public int DataStartColumn { get; set; } = 1;

        /// <summary>
        /// Creates new data row with default styles for each data item. 
        /// Default is true, set to false to keep original template design.
        /// </summary>
        public bool CreateNewDataRows { get; set; } = true;
    }
}
