using System.Collections.Generic;
using System.Linq;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;

namespace DeltaDocumentCreator
{
    public abstract class BaseTypeConverter<T> : TypeConverter where T : class
    {
        public abstract T ConvertFromStringToT(string text);

        public abstract string ConvertToStringFromT(T value);

        public override object ConvertFromString(string text, IReaderRow row, MemberMapData memberMapData) => ConvertFromStringToT(text);

        public override string ConvertToString(object value, IWriterRow row, MemberMapData memberMapData) => ConvertToStringFromT(value as T);
    }
    
    public class ListTypeConverter : BaseTypeConverter<List<string>>
    {
        public override List<string> ConvertFromStringToT(string text)
        {
            // remove the leading and trailing brackets
            text = text.Remove(0, 1);
            text = text.Remove(text.Length - 1, 1);
            return text.Split(',').ToList();
        }

        public override string ConvertToStringFromT(List<string> value)
        {
            return $"({string.Join(",", value)})";
        }
    }
}