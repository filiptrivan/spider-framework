using System.Collections.Generic;
using System.Linq;
using Soft.NgTable.Models;

namespace Soft.NgTable.Core
{
    public interface ITableFilterManager<out TEntity>
    {
        void MultipleOrderDataSet(TableFilterModel tableFilterPayload);
        void SingleOrderDataSet(TableFilterModel tableFilterPayload);
        void FilterDataSet(string key, TableFilterContext value);
        void FiltersDataSet(string key, IEnumerable<TableFilterContext> values);
        void ExecuteFilter();
        IQueryable<TEntity> GetResult();
    }
}