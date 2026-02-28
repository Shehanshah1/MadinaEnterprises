using System;
using System.Globalization;

namespace MadinaEnterprises.Modules.Util
{
    public static class RateCalculation
    {
        public const double KgPerMaund = 37.3242;

        public static double MaundFromKg(double weightKg)
        {
            return weightKg <= 0 ? 0 : weightKg / KgPerMaund;
        }

        public static double AmountFromKg(double weightKg, double ratePerMaund)
        {
            if (weightKg <= 0 || ratePerMaund <= 0)
            {
                return 0;
            }

            return MaundFromKg(weightKg) * ratePerMaund;
        }

        public static bool TryParseDouble(string? input, out double value)
        {
            return double.TryParse(input, NumberStyles.Any, CultureInfo.InvariantCulture, out value)
                || double.TryParse(input, NumberStyles.Any, CultureInfo.CurrentCulture, out value);
        }
    }
}
