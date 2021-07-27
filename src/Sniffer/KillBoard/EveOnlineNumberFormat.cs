using System.Globalization;

namespace Sniffer.KillBoard
{
    public static class EveOnlineNumberFormat
    {

        public static readonly NumberFormatInfo IskNumberFormat = new NumberFormatInfo()
        {
            CurrencySymbol = "ISk",
            CurrencyPositivePattern = 3,
            CurrencyNegativePattern = 8,
            CurrencyDecimalDigits = 0,
            CurrencyGroupSeparator = ",",
            CurrencyGroupSizes = new int[1] { 3 }
        };
    }
}
