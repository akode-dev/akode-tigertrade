using System;

namespace Akode.TigerTrade.Indicators.Helpers
{
    internal static class RoundPriceHelper
    {
        internal static bool IsRoundPrice(
            double price,
            double priceStep,
            bool enabled,
            double roundStep,
            int toleranceTicks)
        {
            if (!enabled || priceStep <= 0.0)
            {
                return false;
            }

            var step = roundStep > 0.0 ? roundStep : priceStep * 100.0;
            var tolerance = Math.Max(0, toleranceTicks) * priceStep;
            var remainder = Math.Abs(price % step);

            return Math.Min(remainder, step - remainder) <= tolerance + priceStep * 0.5;
        }
    }
}
