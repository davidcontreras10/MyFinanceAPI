using System.Globalization;

namespace MyFinanceModel.Utilities
{
    public static class NumUtils
    {
        public static string GetCurrencyFormatted(float value)
        {
            return string.Format(CultureInfo.InvariantCulture, "{0:#,0.##}", value).Replace(',', ' ');
        }

        public static decimal ToDecimal(object value)
        {
            if(value == null) return 0;
            if(value is decimal dec) return dec;
            return decimal.TryParse(value.ToString(), out var dec2) ? dec2 : 0;
        }
    }
}
