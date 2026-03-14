using System;
using System.Collections.Generic;
using System.Linq;
using TigerTrade.Chart.Base.Enums;
using TigerTrade.Chart.Data;
using TigerTrade.Chart.Indicators.Common;
using TigerTrade.Core.Utils.Time;

namespace Akode.TigerTrade.Indicators
{
    internal static class TrendsCalculationEngine
    {
        internal struct LevelLine
        {
            public double Price;
            public int StartIndex;
            public bool IsBroken;
        }

        internal sealed class ProfileLevelsResult
        {
            public static readonly ProfileLevelsResult Empty = new ProfileLevelsResult(
                new List<LevelLine>(),
                new List<LevelLine>());

            public ProfileLevelsResult(List<LevelLine> highLevels, List<LevelLine> lowLevels)
            {
                HighLevels = highLevels;
                LowLevels = lowLevels;
            }

            public List<LevelLine> HighLevels { get; private set; }

            public List<LevelLine> LowLevels { get; private set; }
        }

        internal sealed class TrendLineCandidate
        {
            public int StartIndex { get; set; }

            public int EndIndex { get; set; }

            public double StartPrice { get; set; }

            public double Slope { get; set; }

            public int TouchCount { get; set; }

            public int LastTouchIndex { get; set; }

            public double GetValue(int index)
            {
                return StartPrice + (Slope * (index - StartIndex));
            }
        }

        private sealed class TimeFrameBar
        {
            public double High;
            public double Low;
            public int HighIndex;
            public int LowIndex;
            public int FirstIndex;
        }

        public static ProfileLevelsResult CalculateLevels(
            IndicatorsHelper helper,
            IChartDataProvider dataProvider,
            AkodeTrendsProfileSettings settings)
        {
            var candlesBefore = Math.Max(0, settings.CandlesBefore);
            var candlesAfter = Math.Max(0, settings.CandlesAfter);
            var minBars = candlesBefore + candlesAfter + 1;

            if (helper.Count < minBars)
            {
                return ProfileLevelsResult.Empty;
            }

            var highSource = settings.UseCandleBodyInsteadOfWicks
                ? BuildBodyHigh(helper.Open, helper.Close)
                : helper.High;
            var lowSource = settings.UseCandleBodyInsteadOfWicks
                ? BuildBodyLow(helper.Open, helper.Close)
                : helper.Low;

            List<LevelLine> highPivots;
            List<LevelLine> lowPivots;

            if (settings.PeriodType == AkodeLevelsPeriodType.AnyTimeFrame)
            {
                highPivots = FindPivotsInCurrentData(highSource, true, candlesBefore, candlesAfter);
                lowPivots = FindPivotsInCurrentData(lowSource, false, candlesBefore, candlesAfter);
            }
            else
            {
                var bars = BuildOnTimeframe(helper.Date, highSource, lowSource, dataProvider, settings);
                if (bars.Count < minBars)
                {
                    return ProfileLevelsResult.Empty;
                }

                highPivots = FindPivotsInTimeFrameData(bars, highSource, true, candlesBefore, candlesAfter);
                lowPivots = FindPivotsInTimeFrameData(bars, lowSource, false, candlesBefore, candlesAfter);
            }

            return new ProfileLevelsResult(
                FinalizeLevels(highPivots, true, settings),
                FinalizeLevels(lowPivots, false, settings));
        }

        public static List<TrendLineCandidate> SelectTrendLines(
            IEnumerable<LevelLine> levels,
            bool isHigh,
            bool enforceSlopeFilter,
            int maxTrendLines,
            int minTouches,
            double tolerance,
            int dataLength)
        {
            var orderedLevels = levels
                .OrderBy(level => level.StartIndex)
                .ThenBy(level => level.Price)
                .ToList();

            if (orderedLevels.Count < 2 || maxTrendLines <= 0 || dataLength <= 0)
            {
                return new List<TrendLineCandidate>();
            }

            minTouches = Math.Max(2, minTouches);
            tolerance = Math.Max(0.0, tolerance);

            var candidates = new List<TrendLineCandidate>();

            for (int i = 0; i < orderedLevels.Count - 1; i++)
            {
                for (int j = i + 1; j < orderedLevels.Count; j++)
                {
                    var first = orderedLevels[i];
                    var second = orderedLevels[j];

                    if (first.StartIndex == second.StartIndex)
                    {
                        continue;
                    }

                    var slope = (second.Price - first.Price) / (second.StartIndex - first.StartIndex);

                    if (enforceSlopeFilter)
                    {
                        if (isHigh && slope >= 0.0)
                        {
                            continue;
                        }

                        if (!isHigh && slope <= 0.0)
                        {
                            continue;
                        }
                    }

                    var candidate = EvaluateCandidate(
                        orderedLevels,
                        isHigh,
                        first,
                        second,
                        slope,
                        minTouches,
                        tolerance);

                    if (candidate != null)
                    {
                        candidates.Add(candidate);
                    }
                }
            }

            var rankedCandidates = candidates
                .OrderByDescending(candidate => candidate.TouchCount)
                .ThenByDescending(candidate => candidate.EndIndex - candidate.StartIndex)
                .ThenByDescending(candidate => candidate.LastTouchIndex)
                .ToList();

            var selected = new List<TrendLineCandidate>();

            foreach (var candidate in rankedCandidates)
            {
                if (selected.Count >= maxTrendLines)
                {
                    break;
                }

                if (selected.Any(existing => AreSimilar(existing, candidate, tolerance, dataLength)))
                {
                    continue;
                }

                selected.Add(candidate);
            }

            return selected;
        }

        private static TrendLineCandidate EvaluateCandidate(
            List<LevelLine> levels,
            bool isHigh,
            LevelLine first,
            LevelLine second,
            double slope,
            int minTouches,
            double tolerance)
        {
            var candidate = new TrendLineCandidate
            {
                StartIndex = first.StartIndex,
                EndIndex = second.StartIndex,
                StartPrice = first.Price,
                Slope = slope,
                TouchCount = 0,
                LastTouchIndex = second.StartIndex
            };

            foreach (var level in levels)
            {
                if (level.StartIndex < candidate.StartIndex)
                {
                    continue;
                }

                var linePrice = candidate.GetValue(level.StartIndex);
                var delta = level.Price - linePrice;

                if (isHigh)
                {
                    if (delta > tolerance)
                    {
                        return null;
                    }
                }
                else if (delta < -tolerance)
                {
                    return null;
                }

                if (Math.Abs(delta) <= tolerance)
                {
                    candidate.TouchCount++;
                    candidate.LastTouchIndex = Math.Max(candidate.LastTouchIndex, level.StartIndex);
                }
            }

            if (candidate.TouchCount < minTouches)
            {
                return null;
            }

            return candidate;
        }

        private static bool AreSimilar(
            TrendLineCandidate left,
            TrendLineCandidate right,
            double tolerance,
            int dataLength)
        {
            var lastIndex = Math.Max(0, dataLength - 1);
            var valueDelta = Math.Abs(left.GetValue(lastIndex) - right.GetValue(lastIndex));
            if (valueDelta > tolerance)
            {
                return false;
            }

            var slopeTolerance = tolerance / Math.Max(10, dataLength);
            return Math.Abs(left.Slope - right.Slope) <= slopeTolerance;
        }

        private static List<LevelLine> FinalizeLevels(
            List<LevelLine> pivots,
            bool isHigh,
            AkodeTrendsProfileSettings settings)
        {
            var orderedPivots = pivots
                .OrderByDescending(pivot => pivot.StartIndex)
                .ToList();

            var activeLimit = isHigh
                ? Math.Max(0, settings.MaxLinesHigh)
                : Math.Max(0, settings.MaxLinesLow);
            var brokenLimit = isHigh
                ? Math.Max(0, settings.MaxBrokenLinesHigh)
                : Math.Max(0, settings.MaxBrokenLinesLow);

            var levels = orderedPivots
                .Where(pivot => !pivot.IsBroken)
                .Take(activeLimit)
                .ToList();

            if (settings.ShowBrokenLines && brokenLimit > 0)
            {
                levels.AddRange(orderedPivots
                    .Where(pivot => pivot.IsBroken)
                    .Take(brokenLimit));
            }

            return levels;
        }

        private static List<TimeFrameBar> BuildOnTimeframe(
            double[] date,
            double[] high,
            double[] low,
            IChartDataProvider dataProvider,
            AkodeTrendsProfileSettings settings)
        {
            var timeOffset = TimeHelper.GetSessionOffsetTs(dataProvider.Symbol.Exchange);
            var chartPeriodType = settings.PeriodType == AkodeLevelsPeriodType.Minute ? ChartPeriodType.Minute :
                                  settings.PeriodType == AkodeLevelsPeriodType.Hour ? ChartPeriodType.Hour :
                                  settings.PeriodType == AkodeLevelsPeriodType.Week ? ChartPeriodType.Week :
                                  settings.PeriodType == AkodeLevelsPeriodType.Month ? ChartPeriodType.Month :
                                  ChartPeriodType.Minute;
            var periodValue = settings.PeriodValue > 0 ? settings.PeriodValue : 1;
            var selectedBars = new Dictionary<int, TimeFrameBar>();

            for (int i = 0; i < date.Length; i++)
            {
                var sequence = dataProvider.Period.GetSequence(
                    chartPeriodType,
                    periodValue,
                    date[i],
                    timeOffset.TotalHours);

                TimeFrameBar bar;
                if (selectedBars.TryGetValue(sequence, out bar))
                {
                    if (high[i] > bar.High)
                    {
                        bar.High = high[i];
                        bar.HighIndex = i;
                    }

                    if (low[i] < bar.Low)
                    {
                        bar.Low = low[i];
                        bar.LowIndex = i;
                    }
                }
                else
                {
                    selectedBars[sequence] = new TimeFrameBar
                    {
                        High = high[i],
                        Low = low[i],
                        HighIndex = i,
                        LowIndex = i,
                        FirstIndex = i
                    };
                }
            }

            return selectedBars.Values
                .OrderBy(bar => bar.FirstIndex)
                .ToList();
        }

        private static List<LevelLine> FindPivotsInTimeFrameData(
            List<TimeFrameBar> bars,
            double[] originalPrices,
            bool isHigh,
            int candlesBefore,
            int candlesAfter)
        {
            var pivots = new List<LevelLine>();

            for (int i = candlesBefore; i < bars.Count - candlesAfter; i++)
            {
                var isPivot = true;
                var centralBar = bars[i];
                var pivotPrice = isHigh ? centralBar.High : centralBar.Low;

                for (int j = i - candlesBefore; j <= i + candlesAfter; j++)
                {
                    if (i == j)
                    {
                        continue;
                    }

                    var comparePrice = isHigh ? bars[j].High : bars[j].Low;
                    if (isHigh ? comparePrice > pivotPrice : comparePrice < pivotPrice)
                    {
                        isPivot = false;
                        break;
                    }
                }

                if (!isPivot)
                {
                    continue;
                }

                var startIndex = isHigh ? centralBar.HighIndex : centralBar.LowIndex;
                var isBroken = IsBroken(startIndex, pivotPrice, originalPrices, isHigh);

                pivots.Add(new LevelLine
                {
                    Price = pivotPrice,
                    StartIndex = startIndex,
                    IsBroken = isBroken
                });
            }

            return pivots;
        }

        private static List<LevelLine> FindPivotsInCurrentData(
            double[] prices,
            bool isHigh,
            int candlesBefore,
            int candlesAfter)
        {
            var pivots = new List<LevelLine>();

            for (int i = candlesBefore; i < prices.Length - candlesAfter; i++)
            {
                var isPivot = true;
                var price = prices[i];

                for (int j = i - candlesBefore; j <= i + candlesAfter; j++)
                {
                    if (i == j)
                    {
                        continue;
                    }

                    if (isHigh ? prices[j] > price : prices[j] < price)
                    {
                        isPivot = false;
                        break;
                    }
                }

                if (!isPivot)
                {
                    continue;
                }

                pivots.Add(new LevelLine
                {
                    Price = price,
                    StartIndex = i,
                    IsBroken = IsBroken(i, price, prices, isHigh)
                });
            }

            return pivots;
        }

        private static bool IsBroken(int startIndex, double price, double[] prices, bool isHighLevel)
        {
            for (int i = startIndex + 1; i < prices.Length; i++)
            {
                if (isHighLevel ? prices[i] > price : prices[i] < price)
                {
                    return true;
                }
            }

            return false;
        }

        private static double[] BuildBodyHigh(double[] open, double[] close)
        {
            var values = new double[open.Length];

            for (int i = 0; i < open.Length; i++)
            {
                values[i] = Math.Max(open[i], close[i]);
            }

            return values;
        }

        private static double[] BuildBodyLow(double[] open, double[] close)
        {
            var values = new double[open.Length];

            for (int i = 0; i < open.Length; i++)
            {
                values[i] = Math.Min(open[i], close[i]);
            }

            return values;
        }
    }
}
