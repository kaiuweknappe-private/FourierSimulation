using System.Globalization;
using Avalonia.Data.Converters;

namespace FourierSim.Converter;

/// <summary>
/// used for validation between a Textbox and an int? Property 
/// </summary>
public class StringToNullableIntConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is int intValue ? intValue.ToString() : string.Empty;   
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string stringValue && int.TryParse(stringValue, out var result))
        {
            return result;
        }

        return null;
    }
}