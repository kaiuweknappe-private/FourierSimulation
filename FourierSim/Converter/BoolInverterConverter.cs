using System.Globalization;
using Avalonia.Data.Converters;

namespace FourierSim.Converter;

/// <summary>
/// Too simple to need a summary..
/// </summary>
public class BoolInverterConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if(value is bool flag)
            return !flag;
        
        return false;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if(value is bool flag)
            return !flag;
        
        return false;
    }
}