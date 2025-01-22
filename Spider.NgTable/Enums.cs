namespace Spider.NgTable
{
    public enum SortingCodes
    {
        OrderByAsc = 1,
        OrderByDesc = -1
    }

    public enum OperatorCodes
    {
        And = 1,
        Or = 2,
        None = 3
    }

    public static class OperatorConstant
    {
        private const string ConstantAnd = "and";
        private const string ConstantOr = "or";

        public static OperatorCodes ConvertOperatorEnumeration(string value)
        {
            switch (value.ToLower())
            {
                case ConstantAnd:
                    return OperatorCodes.And;
                case ConstantOr:
                    return OperatorCodes.Or;
                default:
                    return OperatorCodes.None;
            }
        }
    }
}