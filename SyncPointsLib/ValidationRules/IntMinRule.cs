using System;
using System.Globalization;
using System.Windows.Controls;

namespace SyncPointsLib.ValidationRules
{
    /// <summary>
    /// Checks whether the given int is in range
    /// </summary>
    public class IntMinRule : ValidationRule
    {
        public int Min { get; set; }

        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            double number = Min;
            try
            {
                if (((string)value).Length > 0) number = int.Parse((string)value);
            }
            catch (Exception)
            {
                return new ValidationResult(false, "Please enter a valid integer value " + $"greater or equal to {Min}");
            }
            if (number < Min) return new ValidationResult(false, "Please enter a valid integer value " + $"greater or equal to {Min}");
            return ValidationResult.ValidResult;
        }
    }
}
