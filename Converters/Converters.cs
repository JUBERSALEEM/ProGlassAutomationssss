using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace ProGlassAutomation.Converters
{
    // ==================== BOOLEAN CONVERTERS ====================

    /// <summary>
    /// Converts Boolean to Visibility (True = Visible, False = Collapsed)
    /// </summary>
    public class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visibility visibility)
            {
                return visibility == Visibility.Visible;
            }
            return false;
        }
    }

    /// <summary>
    /// Converts Boolean to Visibility (alias - same as BoolToVisibilityConverter)
    /// </summary>
    public class BooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visibility visibility)
            {
                return visibility == Visibility.Visible;
            }
            return false;
        }
    }

    /// <summary>
    /// Converts Boolean to Visibility (True = Collapsed, False = Visible)
    /// </summary>
    public class InverseBooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue ? Visibility.Collapsed : Visibility.Visible;
            }
            return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visibility visibility)
            {
                return visibility != Visibility.Visible;
            }
            return false;
        }
    }

    /// <summary>
    /// Inverts Boolean value
    /// </summary>
    public class InverseBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return !boolValue;
            }
            return true;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return !boolValue;
            }
            return false;
        }
    }

    // ==================== COLOR CONVERTERS ====================

    /// <summary>
    /// Converts Boolean to Color (True = Green, False = Red)
    /// </summary>
    public class BoolToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue
                    ? new SolidColorBrush(Color.FromRgb(16, 185, 129))
                    : new SolidColorBrush(Color.FromRgb(239, 68, 68)); // ✅ FIXED: removed extra )
            }
            return new SolidColorBrush(Color.FromRgb(100, 116, 139));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converts Status string to Color
    /// </summary>
    public class StatusToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var status = value as string;
            return status switch
            {
                "Release" => new SolidColorBrush(Color.FromRgb(16, 185, 129)),
                "Hold" => new SolidColorBrush(Color.FromRgb(245, 158, 11)),
                "Cancel" => new SolidColorBrush(Color.FromRgb(239, 68, 68)),
                "Completed" => new SolidColorBrush(Color.FromRgb(16, 185, 129)),
                "In Progress" => new SolidColorBrush(Color.FromRgb(59, 130, 246)),
                "Pending" => new SolidColorBrush(Color.FromRgb(245, 158, 11)),
                "Active" => new SolidColorBrush(Color.FromRgb(16, 185, 129)),
                "Inactive" => new SolidColorBrush(Color.FromRgb(100, 116, 139)),
                _ => new SolidColorBrush(Color.FromRgb(100, 116, 139))
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converts Boolean to Green Color (True = #059669, False = #64748B)
    /// </summary>
    public class BoolToColorGreenConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue
                    ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#059669"))
                    : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#64748B"));
            }
            return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#64748B"));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converts Boolean to Green Brush (True = Light Green, False = White)
    /// </summary>
    public class BoolToBrushGreenConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue
                    ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#ECFDF5"))
                    : new SolidColorBrush(Colors.White);
            }
            return new SolidColorBrush(Colors.White);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converts Boolean to Green Border Brush (True = #10B981, False = #E2E8F0)
    /// </summary>
    public class BoolToBrushGreenBorderConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue
                    ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#10B981"))
                    : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E2E8F0"));
            }
            return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E2E8F0"));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    // ==================== STRING CONVERTERS ====================

    /// <summary>
    /// Converts String to Visibility (Empty = Collapsed, Has Value = Visible)
    /// </summary>
    public class StringToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return string.IsNullOrEmpty(value as string) ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    // ==================== NULL CONVERTERS ====================

    /// <summary>
    /// Converts Null/Not Null to Visibility
    /// </summary>
    public class NullToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value != null ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converts Null/Not Null to Boolean
    /// </summary>
    public class NullToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value != null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    // ==================== NUMBER CONVERTERS ====================

    /// <summary>
    /// Converts Decimal/Double to Currency String (AED)
    /// </summary>
    public class CurrencyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is decimal decimalValue)
            {
                return $"AED {decimalValue:N2}";
            }
            if (value is double doubleValue)
            {
                return $"AED {doubleValue:N2}";
            }
            return "AED 0.00";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converts Decimal/Double to Area String (SQM)
    /// </summary>
    public class AreaConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is decimal decimalValue)
            {
                return $"{decimalValue:N4} SQM";
            }
            if (value is double doubleValue)
            {
                return $"{doubleValue:N4} SQM";
            }
            return "0.0000 SQM";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converts Decimal/Double to Length String (LM)
    /// </summary>
    public class LengthConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is decimal decimalValue)
            {
                return $"{decimalValue:N4} LM";
            }
            if (value is double doubleValue)
            {
                return $"{doubleValue:N4} LM";
            }
            return "0.0000 LM";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    // ==================== MULTI-VALUE CONVERTERS ====================

    /// <summary>
    /// Multi-value converter - all True = Visible, any False = Collapsed
    /// </summary>
    public class MultiBoolToVisibilityConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length == 0)
                return Visibility.Collapsed;

            foreach (var value in values)
            {
                if (value is bool boolValue && !boolValue)
                    return Visibility.Collapsed;
            }
            return Visibility.Visible;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    // ==================== DATE/TIME CONVERTERS ====================

    /// <summary>
    /// Converts DateTime to formatted string
    /// </summary>
    public class DateTimeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DateTime dateTime)
            {
                var format = parameter as string ?? "dd MMM yyyy";
                return dateTime.ToString(format);
            }
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (DateTime.TryParse(value as string, out var result))
            {
                return result;
            }
            return DateTime.Now;
        }
    }

    // ==================== OTHER CHARGES CONVERTERS ====================

    /// <summary>
    /// Converts ObservableCollection of OtherChargeModel to Total Amount
    /// </summary>
    public class ChargesTotalConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is System.Collections.ObjectModel.ObservableCollection<ProGlassAutomation.Models.OtherChargeModel> charges)
            {
                double total = 0;
                foreach (var charge in charges)
                {
                    total += charge.Amount;
                }
                return $"AED {total:N2}";
            }
            return "AED 0.00";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converts ObservableCollection of OtherChargeModel to Total Amount (numeric)
    /// </summary>
    public class ChargesTotalNumericConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is System.Collections.ObjectModel.ObservableCollection<ProGlassAutomation.Models.OtherChargeModel> charges)
            {
                double total = 0;
                foreach (var charge in charges)
                {
                    total += charge.Amount;
                }
                return total;
            }
            return 0.0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Checks if TargetsAllSpecs is true/false and returns appropriate text
    /// </summary>
    public class SpecLinkTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool targetsAll)
            {
                return targetsAll ? "All Specs" : "Selected";
            }
            return "All Specs";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converts charge type to display text
    /// </summary>
    public class ChargeTypeDisplayConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var type = value as string;
            return type?.ToLower() switch
            {
                "lm" => "LM",
                "sqm" => "SQM",
                "qty" => "QTY",
                "1x" => "1X",
                "2x" => "2X",
                "percent" => "%",
                "amount" => "Fixed",
                _ => type?.ToUpper() ?? ""
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converts charge type to unit display (AED/LM, AED/SQM, etc.)
    /// </summary>
    public class ChargeUnitDisplayConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var type = value as string;
            return type?.ToLower() switch
            {
                "lm" => "AED/LM",
                "sqm" => "AED/SQM",
                "qty" or "1x" or "2x" => "AED/pc",
                "percent" => "AED/%",
                "amount" => "AED",
                _ => "AED"
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    // ==================== STATUS COLOR CONVERTER ====================

    /// <summary>
    /// Converts Status string to Background Color for PI Dashboard
    /// </summary>
    public class StatusColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var status = value as string ?? "";

            return status switch
            {
                "Draft" => new SolidColorBrush(Color.FromRgb(107, 114, 128)),       // Gray - #6B7280
                "Sent" => new SolidColorBrush(Color.FromRgb(59, 130, 246)),         // Blue - #3B82F6
                "Pending" => new SolidColorBrush(Color.FromRgb(245, 158, 11)),     // Amber - #F59E0B
                "Hold" => new SolidColorBrush(Color.FromRgb(245, 158, 11)),           // Amber - #F59E0B
                "Confirmed" => new SolidColorBrush(Color.FromRgb(34, 197, 94)),      // Green - #22C55E
                "Revised" => new SolidColorBrush(Color.FromRgb(139, 92, 246)),       // Purple - #8B5CF6
                "In Progress" => new SolidColorBrush(Color.FromRgb(59, 130, 246)), // Blue - #3B82F6
                "Converted To JO" => new SolidColorBrush(Color.FromRgb(20, 184, 166)), // Teal - #14B8A6
                "Partial Delivered" => new SolidColorBrush(Color.FromRgb(251, 191, 36)), // Amber - #FBBF24
                "Delivered" => new SolidColorBrush(Color.FromRgb(34, 197, 94)),        // Green - #22C55E
                "Invoiced" => new SolidColorBrush(Color.FromRgb(59, 130, 246)),       // Blue - #3B82F6
                "Completed" => new SolidColorBrush(Color.FromRgb(22, 163, 74)),      // Dark Green - #16A34A
                "Cancelled" => new SolidColorBrush(Color.FromRgb(239, 68, 68)),   // Red - #EF4444
                "Voided" => new SolidColorBrush(Color.FromRgb(156, 163, 175)),        // Gray - #9CA3AF
                _ => new SolidColorBrush(Color.FromRgb(107, 114, 128))             // Default Gray
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
