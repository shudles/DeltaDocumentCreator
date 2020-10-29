namespace DeltaDocumentCreator
{
    public static class Extensions
    {
        public static bool HasValue(this string str) => !string.IsNullOrWhiteSpace(str);
    }
}