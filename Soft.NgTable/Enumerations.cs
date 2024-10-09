namespace Soft.NgTable.TableFilter
{
    public enum SortingEnumeration
    {
        OrderByAsc = 1,
        OrderByDesc = -1
    }

    public enum OperatorEnumeration
    {
        And = 1,
        Or = 2,
        None = 3
    }

    public static class OperatorConstant
    {
        private const string ConstantAnd = "and";
        private const string ConstantOr = "or";

        public static OperatorEnumeration ConvertOperatorEnumeration(string value)
        {
            switch (value.ToLower())
            {
                case ConstantAnd:
                    return OperatorEnumeration.And;
                case ConstantOr:
                    return OperatorEnumeration.Or;
                default:
                    return OperatorEnumeration.None;
            }
        }
    }
}