using System;
using System.Globalization;
using System.Windows.Controls;

namespace SyncPointsLib.ValidationRules
{
    /// <summary>
    /// Checks whether the given double is a valid probability
    /// </summary>
    public class ProbabilityRule : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            double probability = 0;
            try
            {
                if (((string)value).Length > 0) probability = double.Parse((string)value, CultureInfo.InvariantCulture);
            }
            catch (Exception)
            {
                return new ValidationResult(false, "Please enter a valid decimal value");
            }
            if (probability < 0 || probability > 1) return new ValidationResult(false, "Invalid probability");
            return ValidationResult.ValidResult;
        }
    }
}
