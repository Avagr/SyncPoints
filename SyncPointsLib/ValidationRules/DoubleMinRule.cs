using System;
using System.Globalization;
using System.Windows.Controls;

namespace SyncPointsLib.ValidationRules
{
    /// <summary>
    /// Checks whether the given double is in range
    /// </summary>
    public class DoubleMinRule : ValidationRule
    {
        public double Min { get; set; }

        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            double number = Min;

            try
            {                if (((string)value).Length > 0) number = double.Parse((string)value);
            }
            catch (Exception)
            {
                return new ValidationResult(false, "Please enter a valid double value " + $"greater or equal to {Min}");
            }
            if (number < Min) return new ValidationResult(false, "Please enter a valid double value " + $"greater or equal to {Min}");
            return ValidationResult.ValidResult;
        }
    }
}
