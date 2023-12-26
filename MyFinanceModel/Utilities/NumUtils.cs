using System.Globalization;

namespace MyFinanceModel.Utilities
{
    public static class NumUtils
    {
        public static string GetCurrencyFormatted(float value)
        {
            return string.Format(CultureInfo.InvariantCulture, "{0:#,0.##}", value).Replace(',', ' ');
        }
    }
}
