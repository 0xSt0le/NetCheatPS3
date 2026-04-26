namespace NetCheatPS3
{
    public partial class SearchControl
    {
        private int GetEffectiveSearchAlignment(int typeIndex)
        {
            if (typeIndex < 0 || typeIndex >= SearchTypes.Count)
                return 1;

            ncSearchType type = SearchTypes[typeIndex];

            if (type.ignoreAlignment)
                return 1;

            if (type.ByteSize <= 0)
                return 1;

            return type.ByteSize;
        }
    }
}