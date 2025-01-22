namespace Spider.NgTable.Core
{
    public interface ILinqOperator<out TEntity>
    {
        void AddFilterProperty(string propertyName, object propertyValue, string extensionMethod,
            OperatorCodes operatorAction, bool isNegation = false);
        void AddFilterListProperty(string propertyName, object propertyValue, OperatorCodes operatorAction);
        void WhereExecute();
        void OrderBy(string orderProperty);
        void OrderByDescending(string orderProperty);
        void ThenBy(string orderProperty);
        void ThenByDescending(string orderProperty);
        IQueryable<TEntity> GetResult();
    }
}