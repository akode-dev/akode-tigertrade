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
            public bool IsTested;
            public int ProfileIndex;
            public ChartPeriodType TimeframeType;
            public int TimeframeInterval;
            public double TimeframeWeight;
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

        internal sealed class TrendSelectionResult
        {
            public TrendSelectionResult(
                List<TrendLineCandidate> highTrendLines,
                List<TrendLineCandidate> lowTrendLines)
            {
                HighTrendLines = highTrendLines;
                LowTrendLines = lowTrendLines;
            }

            public List<TrendLineCandidate> HighTrendLines { get; private set; }

            public List<TrendLineCandidate> LowTrendLines { get; private set; }
        }

        internal sealed class TrendLineCandidate
        {
            public TrendLineCandidate()
            {
                TouchedLevels = new List<LevelLine>();
            }

            public bool IsHighSide { get; set; }

            public int StartIndex { get; set; }

            public int EndIndex { get; set; }

            public double StartPrice { get; set; }

            public double Slope { get; set; }

            public int TouchCount { get; set; }

            public int LastTouchIndex { get; set; }

            public List<LevelLine> TouchedLevels { get; private set; }

            public double DistanceToCurrentPriceTicks { get; set; }

            public double TimeframeTouchScore { get; set; }

            public double SelectionScore { get; set; }

            public int SelectionRank { get; set; }

            public int BreakStartIndex { get; set; }

            public double AverageResidualTicks { get; set; }

            public double EnvelopeSlackTicks { get; set; }

            public double OuterDistanceTicks { get; set; }

            public int WeakViolationCount { get; set; }

            public int Span
            {
                get { return EndIndex - StartIndex; }
            }

            public double GetValue(int index)
            {
                return StartPrice + (Slope * (index - StartIndex));
            }
        }

        private sealed class TrendConflict
        {
            public TrendLineCandidate HighLine { get; set; }

            public TrendLineCandidate LowLine { get; set; }

            public double IntersectionIndex { get; set; }
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
            AkodeTrendsProfileSettings settings,
            int profileIndex)
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

            var closePrices = helper.Close;

            if (settings.PeriodType == AkodeLevelsPeriodType.AnyTimeFrame)
            {
                highPivots = FindPivotsInCurrentData(highSource, closePrices, true, candlesBefore, candlesAfter);
                lowPivots = FindPivotsInCurrentData(lowSource, closePrices, false, candlesBefore, candlesAfter);
            }
            else
            {
                var bars = BuildOnTimeframe(helper.Date, highSource, lowSource, dataProvider, settings);
                if (bars.Count < minBars)
                {
                    return ProfileLevelsResult.Empty;
                }

                highPivots = FindPivotsInTimeFrameData(bars, highSource, closePrices, true, candlesBefore, candlesAfter);
                lowPivots = FindPivotsInTimeFrameData(bars, lowSource, closePrices, false, candlesBefore, candlesAfter);
            }

            ChartPeriodType timeframeType;
            int timeframeInterval;
            var timeframeWeight = ResolveTimeframeWeight(settings, dataProvider, out timeframeType, out timeframeInterval);

            ApplyLevelMetadata(highPivots, profileIndex, timeframeType, timeframeInterval, timeframeWeight);
            ApplyLevelMetadata(lowPivots, profileIndex, timeframeType, timeframeInterval, timeframeWeight);

            return new ProfileLevelsResult(
                FinalizeLevels(highPivots, true, settings),
                FinalizeLevels(lowPivots, false, settings));
        }

        public static TrendSelectionResult SelectTrendLines(
            IEnumerable<LevelLine> highLevels,
            IEnumerable<LevelLine> lowLevels,
            AkodeTrendlineAlgorithm algorithm,
            bool enforceHighSlopeFilter,
            bool enforceLowSlopeFilter,
            int maxHighTrendLines,
            int maxLowTrendLines,
            int minHighTouches,
            int minLowTouches,
            double tolerance,
            int dataLength,
            double currentPrice,
            double priceStep,
            int allowedPastCrossingBars,
            bool hideBrokenTrendLines,
            int trendlineBreakBars,
            double trendlineBreakTolerance,
            double[] bodyHigh,
            double[] bodyLow)
        {
            tolerance = Math.Max(0.0, tolerance);
            priceStep = priceStep > 0.0 ? priceStep : 0.0;
            trendlineBreakBars = Math.Max(1, trendlineBreakBars);
            trendlineBreakTolerance = Math.Max(0.0, trendlineBreakTolerance);

            var highPool = BuildCandidatePool(
                highLevels,
                true,
                enforceHighSlopeFilter,
                minHighTouches,
                tolerance,
                dataLength,
                currentPrice,
                priceStep,
                algorithm,
                hideBrokenTrendLines,
                trendlineBreakBars,
                trendlineBreakTolerance,
                bodyHigh,
                bodyLow);
            var lowPool = BuildCandidatePool(
                lowLevels,
                false,
                enforceLowSlopeFilter,
                minLowTouches,
                tolerance,
                dataLength,
                currentPrice,
                priceStep,
                algorithm,
                hideBrokenTrendLines,
                trendlineBreakBars,
                trendlineBreakTolerance,
                bodyHigh,
                bodyLow);

            return ResolveSelections(
                highPool,
                lowPool,
                maxHighTrendLines,
                maxLowTrendLines,
                tolerance,
                dataLength,
                currentPrice,
                priceStep,
                algorithm,
                allowedPastCrossingBars);
        }

        private static List<TrendLineCandidate> BuildCandidatePool(
            IEnumerable<LevelLine> levels,
            bool isHigh,
            bool enforceSlopeFilter,
            int minTouches,
            double tolerance,
            int dataLength,
            double currentPrice,
            double priceStep,
            AkodeTrendlineAlgorithm algorithm,
            bool hideBrokenTrendLines,
            int trendlineBreakBars,
            double trendlineBreakTolerance,
            double[] bodyHigh,
            double[] bodyLow)
        {
            var orderedLevels = levels
                .OrderBy(level => level.StartIndex)
                .ThenBy(level => level.Price)
                .ToList();

            if (orderedLevels.Count < 2 || dataLength <= 0)
            {
                return new List<TrendLineCandidate>();
            }

            minTouches = Math.Max(2, minTouches);

            List<TrendLineCandidate> candidates;
            switch (algorithm)
            {
                case AkodeTrendlineAlgorithm.WeightedRegression:
                    candidates = BuildWeightedRegressionCandidates(orderedLevels, isHigh, enforceSlopeFilter, minTouches, tolerance, dataLength, currentPrice, priceStep);
                    break;
                case AkodeTrendlineAlgorithm.Ransac:
                    candidates = BuildRansacCandidates(orderedLevels, isHigh, enforceSlopeFilter, minTouches, tolerance, dataLength, currentPrice, priceStep);
                    break;
                case AkodeTrendlineAlgorithm.HoughTransform:
                    candidates = BuildHoughTransformCandidates(orderedLevels, isHigh, enforceSlopeFilter, minTouches, tolerance, dataLength, currentPrice, priceStep);
                    break;
                default:
                    candidates = BuildPairCandidates(orderedLevels, isHigh, enforceSlopeFilter, minTouches, tolerance, dataLength, currentPrice, priceStep, algorithm);
                    break;
            }

            if (hideBrokenTrendLines)
            {
                candidates = candidates
                    .Where(candidate => !IsBrokenByPrice(candidate, bodyHigh, bodyLow, trendlineBreakBars, trendlineBreakTolerance))
                    .ToList();
            }

            ApplySelectionScores(candidates, algorithm);
            SortCandidates(candidates, algorithm);

            return candidates;
        }

        private static TrendSelectionResult ResolveSelections(
            List<TrendLineCandidate> highPool,
            List<TrendLineCandidate> lowPool,
            int maxHighTrendLines,
            int maxLowTrendLines,
            double tolerance,
            int dataLength,
            double currentPrice,
            double priceStep,
            AkodeTrendlineAlgorithm algorithm,
            int allowedPastCrossingBars)
        {
            var selectedHigh = new List<TrendLineCandidate>();
            var selectedLow = new List<TrendLineCandidate>();
            var blockedHigh = new HashSet<TrendLineCandidate>();
            var blockedLow = new HashSet<TrendLineCandidate>();

            FillSelection(selectedHigh, highPool, blockedHigh, maxHighTrendLines, tolerance, dataLength, currentPrice, priceStep, algorithm);
            FillSelection(selectedLow, lowPool, blockedLow, maxLowTrendLines, tolerance, dataLength, currentPrice, priceStep, algorithm);

            var iterationLimit = ((highPool.Count + lowPool.Count + 1) * 4);

            for (int i = 0; i < iterationLimit; i++)
            {
                var changed = false;

                changed |= RemoveSameSideDuplicates(selectedHigh, blockedHigh, tolerance, dataLength, currentPrice, priceStep, algorithm);
                changed |= RemoveSameSideDuplicates(selectedLow, blockedLow, tolerance, dataLength, currentPrice, priceStep, algorithm);
                changed |= FillSelection(selectedHigh, highPool, blockedHigh, maxHighTrendLines, tolerance, dataLength, currentPrice, priceStep, algorithm);
                changed |= FillSelection(selectedLow, lowPool, blockedLow, maxLowTrendLines, tolerance, dataLength, currentPrice, priceStep, algorithm);

                TrendConflict conflict;
                if (TryFindConflict(selectedHigh, selectedLow, allowedPastCrossingBars, dataLength, out conflict))
                {
                    RemoveWeakerConflict(
                        selectedHigh,
                        selectedLow,
                        blockedHigh,
                        blockedLow,
                        conflict,
                        algorithm);
                    changed = true;
                }

                if (!changed)
                {
                    break;
                }
            }

            SortCandidates(selectedHigh, algorithm);
            SortCandidates(selectedLow, algorithm);

            return new TrendSelectionResult(selectedHigh, selectedLow);
        }

        private static List<TrendLineCandidate> BuildPairCandidates(
            List<LevelLine> levels,
            bool isHigh,
            bool enforceSlopeFilter,
            int minTouches,
            double tolerance,
            int dataLength,
            double currentPrice,
            double priceStep,
            AkodeTrendlineAlgorithm algorithm)
        {
            var candidates = new List<TrendLineCandidate>();

            for (int i = 0; i < levels.Count - 1; i++)
            {
                for (int j = i + 1; j < levels.Count; j++)
                {
                    var first = levels[i];
                    var second = levels[j];

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

                    var candidate = EvaluateStrictCandidate(levels, isHigh, first, second, slope, minTouches, tolerance, priceStep);

                    if (candidate == null)
                    {
                        continue;
                    }

                    PopulateCandidateMetrics(candidate, currentPrice, priceStep, dataLength);
                    candidates.Add(candidate);
                }
            }

            return candidates;
        }

        private static List<TrendLineCandidate> BuildWeightedRegressionCandidates(
            List<LevelLine> levels,
            bool isHigh,
            bool enforceSlopeFilter,
            int minTouches,
            double tolerance,
            int dataLength,
            double currentPrice,
            double priceStep)
        {
            var candidates = new List<TrendLineCandidate>();
            var regressionSeedTolerance = Math.Max(tolerance * 1.5, tolerance + (2.0 * priceStep));

            for (int i = 0; i < levels.Count - 1; i++)
            {
                for (int j = i + 1; j < levels.Count; j++)
                {
                    var first = levels[i];
                    var second = levels[j];

                    if (first.StartIndex == second.StartIndex)
                    {
                        continue;
                    }

                    var seedSlope = (second.Price - first.Price) / (second.StartIndex - first.StartIndex);

                    if (enforceSlopeFilter)
                    {
                        if (isHigh && seedSlope >= 0.0)
                        {
                            continue;
                        }

                        if (!isHigh && seedSlope <= 0.0)
                        {
                            continue;
                        }
                    }

                    var cluster = CollectSeedCluster(levels, isHigh, first, second, seedSlope, regressionSeedTolerance);
                    if (cluster.Count < minTouches)
                    {
                        continue;
                    }

                    double slope;
                    double intercept;
                    if (!TryFitWeightedRegression(cluster, currentPrice, priceStep, out slope, out intercept))
                    {
                        continue;
                    }

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

                    var startIndex = cluster.Min(level => level.StartIndex);
                    var breakStartIndex = GetBreakStartIndex(cluster, second.StartIndex);
                    var startPrice = intercept + (slope * startIndex);
                    var candidate = EvaluateLineCandidate(
                        levels,
                        isHigh,
                        startIndex,
                        startPrice,
                        slope,
                        minTouches,
                        tolerance,
                        priceStep,
                        breakStartIndex,
                        false,
                        regressionSeedTolerance);

                    if (candidate == null)
                    {
                        continue;
                    }

                    PopulateCandidateMetrics(candidate, currentPrice, priceStep, dataLength);
                    candidates.Add(candidate);
                }
            }

            return candidates;
        }

        private static List<TrendLineCandidate> BuildRansacCandidates(
            List<LevelLine> levels,
            bool isHigh,
            bool enforceSlopeFilter,
            int minTouches,
            double tolerance,
            int dataLength,
            double currentPrice,
            double priceStep)
        {
            var candidates = new List<TrendLineCandidate>();

            if (levels.Count < 2)
            {
                return candidates;
            }

            var seed = ComputeDeterministicSeed(levels);
            var rng = new Random(seed);

            var pairCount = levels.Count * (levels.Count - 1) / 2;
            var maxIterations = Math.Min(200, pairCount);
            maxIterations = Math.Max(maxIterations, 30);
            var ransacViolationTolerance = Math.Max(tolerance * 2.0, tolerance + (3.0 * priceStep));

            var usedPairs = new HashSet<long>();
            var bestTouchCount = 0;
            var consecutiveNoImprovement = 0;
            var earlyStopThreshold = (int)(maxIterations * 0.4);

            for (int iteration = 0; iteration < maxIterations; iteration++)
            {
                if (consecutiveNoImprovement > earlyStopThreshold)
                {
                    break;
                }

                var idx1 = rng.Next(levels.Count);
                var idx2 = rng.Next(levels.Count);

                if (idx1 == idx2)
                {
                    continue;
                }

                var lo = Math.Min(idx1, idx2);
                var hi = Math.Max(idx1, idx2);
                var pairKey = (long)lo * levels.Count + hi;

                if (usedPairs.Contains(pairKey))
                {
                    continue;
                }

                usedPairs.Add(pairKey);

                var first = levels[lo];
                var second = levels[hi];

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

                var inlierCount = 0;
                for (int k = 0; k < levels.Count; k++)
                {
                    if (levels[k].StartIndex < first.StartIndex)
                    {
                        continue;
                    }

                    var linePrice = first.Price + slope * (levels[k].StartIndex - first.StartIndex);
                    var delta = Math.Abs(levels[k].Price - linePrice);

                    if (delta <= tolerance)
                    {
                        inlierCount++;
                    }
                }

                if (inlierCount < minTouches)
                {
                    continue;
                }

                var candidate = EvaluateLineCandidate(
                    levels,
                    isHigh,
                    first.StartIndex,
                    first.Price,
                    slope,
                    minTouches,
                    tolerance,
                    priceStep,
                    first.StartIndex,
                    true,
                    ransacViolationTolerance);

                if (candidate == null)
                {
                    continue;
                }

                PopulateCandidateMetrics(candidate, currentPrice, priceStep, dataLength);
                candidates.Add(candidate);

                if (candidate.TouchCount > bestTouchCount)
                {
                    bestTouchCount = candidate.TouchCount;
                    consecutiveNoImprovement = 0;
                }
                else
                {
                    consecutiveNoImprovement++;
                }
            }

            return candidates;
        }

        private static int ComputeDeterministicSeed(List<LevelLine> levels)
        {
            var hash = 17;
            hash = hash * 31 + levels.Count;

            if (levels.Count > 0)
            {
                hash = hash * 31 + levels[0].StartIndex;
                hash = hash * 31 + (int)(levels[0].Price * 10000);
                hash = hash * 31 + levels[levels.Count - 1].StartIndex;
                hash = hash * 31 + (int)(levels[levels.Count - 1].Price * 10000);
            }

            return hash & 0x7FFFFFFF;
        }

        private static List<TrendLineCandidate> BuildHoughTransformCandidates(
            List<LevelLine> levels,
            bool isHigh,
            bool enforceSlopeFilter,
            int minTouches,
            double tolerance,
            int dataLength,
            double currentPrice,
            double priceStep)
        {
            var candidates = new List<TrendLineCandidate>();

            if (levels.Count < 2)
            {
                return candidates;
            }

            var minIdx = levels[0].StartIndex;
            var maxIdx = levels[0].StartIndex;
            var minPrice = levels[0].Price;
            var maxPrice = levels[0].Price;

            for (int i = 1; i < levels.Count; i++)
            {
                if (levels[i].StartIndex < minIdx) minIdx = levels[i].StartIndex;
                if (levels[i].StartIndex > maxIdx) maxIdx = levels[i].StartIndex;
                if (levels[i].Price < minPrice) minPrice = levels[i].Price;
                if (levels[i].Price > maxPrice) maxPrice = levels[i].Price;
            }

            var span = Math.Max(1, maxIdx - minIdx);
            var priceRange = Math.Max(priceStep > 0.0 ? priceStep : 1.0, maxPrice - minPrice);
            var referenceIndex = minIdx;

            var slopeBinSize = tolerance / Math.Max(1.0, (double)span);
            slopeBinSize = Math.Max(slopeBinSize, 0.5 * (priceStep > 0.0 ? priceStep : tolerance) / Math.Max(1.0, (double)span));

            var interceptBinSize = Math.Max(tolerance, priceStep > 0.0 ? priceStep : tolerance);

            var slopeMin = -priceRange / Math.Max(1.0, (double)span);
            var slopeMax = priceRange / Math.Max(1.0, (double)span);

            if (enforceSlopeFilter)
            {
                if (isHigh)
                {
                    slopeMax = -slopeBinSize;
                }
                else
                {
                    slopeMin = slopeBinSize;
                }
            }

            if (slopeMin >= slopeMax)
            {
                return candidates;
            }

            var numSlopeBins = (int)Math.Ceiling((slopeMax - slopeMin) / slopeBinSize) + 1;
            var numInterceptBins = (int)Math.Ceiling(priceRange / interceptBinSize) + 1;

            const int maxTotalBins = 100000;
            if ((long)numSlopeBins * numInterceptBins > maxTotalBins)
            {
                var scaleFactor = Math.Sqrt((double)((long)numSlopeBins * numInterceptBins) / maxTotalBins);
                slopeBinSize *= scaleFactor;
                interceptBinSize *= scaleFactor;
                numSlopeBins = (int)Math.Ceiling((slopeMax - slopeMin) / slopeBinSize) + 1;
                numInterceptBins = (int)Math.Ceiling(priceRange / interceptBinSize) + 1;
            }

            var interceptMin = minPrice;
            var voteCount = new int[numSlopeBins, numInterceptBins];
            var voteWeight = new double[numSlopeBins, numInterceptBins];

            for (int li = 0; li < levels.Count; li++)
            {
                var level = levels[li];
                for (int si = 0; si < numSlopeBins; si++)
                {
                    var slope = slopeMin + si * slopeBinSize;
                    var intercept = level.Price - slope * (level.StartIndex - referenceIndex);
                    var ii = (int)Math.Floor((intercept - interceptMin) / interceptBinSize);

                    if (ii < 0 || ii >= numInterceptBins)
                    {
                        continue;
                    }

                    voteCount[si, ii]++;
                    voteWeight[si, ii] += level.TimeframeWeight;
                }
            }

            var peaks = new List<HoughPeak>();
            for (int si = 0; si < numSlopeBins; si++)
            {
                for (int ii = 0; ii < numInterceptBins; ii++)
                {
                    if (voteCount[si, ii] >= minTouches)
                    {
                        peaks.Add(new HoughPeak(si, ii, voteCount[si, ii], voteWeight[si, ii]));
                    }
                }
            }

            peaks.Sort((a, b) =>
            {
                var cmp = b.Count.CompareTo(a.Count);
                return cmp != 0 ? cmp : b.Weight.CompareTo(a.Weight);
            });

            var suppressed = new HashSet<long>();
            var houghViolationTolerance = Math.Max(tolerance * 1.5, tolerance + (2.0 * priceStep));
            var peakCount = 0;
            const int maxPeaks = 50;

            for (int pi = 0; pi < peaks.Count && peakCount < maxPeaks; pi++)
            {
                var peak = peaks[pi];
                var peakKey = (long)peak.SlopeBin * numInterceptBins + peak.InterceptBin;

                if (suppressed.Contains(peakKey))
                {
                    continue;
                }

                peakCount++;

                for (int ds = -1; ds <= 1; ds++)
                {
                    for (int di = -1; di <= 1; di++)
                    {
                        if (ds == 0 && di == 0)
                        {
                            continue;
                        }

                        var ns = peak.SlopeBin + ds;
                        var ni = peak.InterceptBin + di;

                        if (ns >= 0 && ns < numSlopeBins && ni >= 0 && ni < numInterceptBins)
                        {
                            suppressed.Add((long)ns * numInterceptBins + ni);
                        }
                    }
                }

                var peakSlope = slopeMin + peak.SlopeBin * slopeBinSize;
                var peakIntercept = interceptMin + peak.InterceptBin * interceptBinSize;
                var startPrice = peakIntercept + peakSlope * (minIdx - referenceIndex);

                var candidate = EvaluateLineCandidate(
                    levels,
                    isHigh,
                    minIdx,
                    startPrice,
                    peakSlope,
                    minTouches,
                    tolerance,
                    priceStep,
                    minIdx,
                    true,
                    houghViolationTolerance);

                if (candidate == null)
                {
                    continue;
                }

                PopulateCandidateMetrics(candidate, currentPrice, priceStep, dataLength);
                candidates.Add(candidate);
            }

            return candidates;
        }

        private struct HoughPeak
        {
            public readonly int SlopeBin;
            public readonly int InterceptBin;
            public readonly int Count;
            public readonly double Weight;

            public HoughPeak(int slopeBin, int interceptBin, int count, double weight)
            {
                SlopeBin = slopeBin;
                InterceptBin = interceptBin;
                Count = count;
                Weight = weight;
            }
        }

        private static TrendLineCandidate EvaluateStrictCandidate(
            List<LevelLine> levels,
            bool isHigh,
            LevelLine first,
            LevelLine second,
            double slope,
            int minTouches,
            double tolerance,
            double priceStep)
        {
            return EvaluateLineCandidate(
                levels,
                isHigh,
                first.StartIndex,
                first.Price,
                slope,
                minTouches,
                tolerance,
                priceStep,
                first.StartIndex,
                false,
                tolerance);
        }

        private static TrendLineCandidate EvaluateLineCandidate(
            List<LevelLine> levels,
            bool isHigh,
            int startIndex,
            double startPrice,
            double slope,
            int minTouches,
            double tolerance,
            double priceStep,
            int breakStartIndex,
            bool allowWeakViolations,
            double maxViolationDistance)
        {
            var candidate = new TrendLineCandidate
            {
                IsHighSide = isHigh,
                StartIndex = startIndex,
                EndIndex = breakStartIndex,
                StartPrice = startPrice,
                Slope = slope,
                TouchCount = 0,
                LastTouchIndex = breakStartIndex,
                BreakStartIndex = startIndex
            };

            var residualSum = 0.0;
            var slackSum = 0.0;
            var slackCount = 0;
            var weakViolationCount = 0;
            var levelsCount = 0;

            foreach (var level in levels)
            {
                if (level.StartIndex < candidate.StartIndex)
                {
                    continue;
                }

                levelsCount++;

                var linePrice = candidate.GetValue(level.StartIndex);
                var delta = level.Price - linePrice;
                var violationDistance = isHigh ? delta : -delta;

                if (violationDistance > tolerance)
                {
                    if (!allowWeakViolations || violationDistance > maxViolationDistance)
                    {
                        return null;
                    }

                    weakViolationCount++;
                    residualSum += violationDistance;
                    continue;
                }

                var signedSlack = isHigh ? (linePrice - level.Price) : (level.Price - linePrice);
                slackSum += Math.Max(0.0, signedSlack);
                slackCount++;

                if (Math.Abs(delta) <= tolerance)
                {
                    candidate.TouchCount++;
                    candidate.LastTouchIndex = Math.Max(candidate.LastTouchIndex, level.StartIndex);
                    candidate.TouchedLevels.Add(level);
                    residualSum += Math.Abs(delta);
                }
            }

            if (candidate.TouchCount < minTouches)
            {
                return null;
            }

            if (allowWeakViolations && weakViolationCount > Math.Max(1, levelsCount / 5))
            {
                return null;
            }

            candidate.EndIndex = candidate.LastTouchIndex;
            candidate.BreakStartIndex = GetBreakStartIndex(candidate.TouchedLevels, candidate.BreakStartIndex);
            candidate.WeakViolationCount = weakViolationCount;
            candidate.AverageResidualTicks = ToTicks(
                residualSum / Math.Max(1, candidate.TouchCount + weakViolationCount),
                priceStep);
            candidate.EnvelopeSlackTicks = ToTicks(
                slackSum / Math.Max(1, slackCount),
                priceStep);

            return candidate;
        }

        private static void PopulateCandidateMetrics(
            TrendLineCandidate candidate,
            double currentPrice,
            double priceStep,
            int dataLength)
        {
            var lastIndex = Math.Max(0, dataLength - 1);
            var currentValue = candidate.GetValue(lastIndex);
            var priceDelta = Math.Abs(currentValue - currentPrice);

            candidate.DistanceToCurrentPriceTicks = priceStep > 0.0
                ? priceDelta / priceStep
                : priceDelta;
            candidate.OuterDistanceTicks = priceStep > 0.0
                ? Math.Max(0.0, candidate.IsHighSide ? currentValue - currentPrice : currentPrice - currentValue) / priceStep
                : Math.Max(0.0, candidate.IsHighSide ? currentValue - currentPrice : currentPrice - currentValue);
            candidate.TimeframeTouchScore = candidate.TouchedLevels.Sum(level => level.TimeframeWeight);
        }

        private static List<LevelLine> CollectSeedCluster(
            List<LevelLine> levels,
            bool isHigh,
            LevelLine first,
            LevelLine second,
            double slope,
            double clusterTolerance)
        {
            var startIndex = Math.Min(first.StartIndex, second.StartIndex);
            var startPrice = first.Price;
            var cluster = new List<LevelLine>();

            foreach (var level in levels)
            {
                if (level.StartIndex < startIndex)
                {
                    continue;
                }

                var linePrice = startPrice + (slope * (level.StartIndex - startIndex));
                var delta = level.Price - linePrice;
                var violationDistance = isHigh ? delta : -delta;

                if (violationDistance > clusterTolerance)
                {
                    continue;
                }

                if (Math.Abs(delta) <= clusterTolerance)
                {
                    cluster.Add(level);
                }
            }

            if (!cluster.Any(level => level.StartIndex == first.StartIndex) ||
                !cluster.Any(level => level.StartIndex == second.StartIndex))
            {
                cluster.Add(first);
                cluster.Add(second);
            }

            return cluster
                .GroupBy(level => new { level.StartIndex, level.Price })
                .Select(group => group.First())
                .OrderBy(level => level.StartIndex)
                .ThenBy(level => level.Price)
                .ToList();
        }

        private static bool TryFitWeightedRegression(
            List<LevelLine> levels,
            double currentPrice,
            double priceStep,
            out double slope,
            out double intercept)
        {
            slope = 0.0;
            intercept = 0.0;

            if (levels == null || levels.Count < 2)
            {
                return false;
            }

            var minIndex = levels.Min(level => level.StartIndex);
            var maxIndex = levels.Max(level => level.StartIndex);
            var totalWeight = 0.0;
            var weightedX = 0.0;
            var weightedY = 0.0;

            foreach (var level in levels)
            {
                var weight = CalculateRegressionWeight(level, currentPrice, priceStep, minIndex, maxIndex);
                totalWeight += weight;
                weightedX += weight * level.StartIndex;
                weightedY += weight * level.Price;
            }

            if (totalWeight <= 0.0)
            {
                return false;
            }

            var meanX = weightedX / totalWeight;
            var meanY = weightedY / totalWeight;
            var numerator = 0.0;
            var denominator = 0.0;

            foreach (var level in levels)
            {
                var weight = CalculateRegressionWeight(level, currentPrice, priceStep, minIndex, maxIndex);
                var deltaX = level.StartIndex - meanX;
                numerator += weight * deltaX * (level.Price - meanY);
                denominator += weight * deltaX * deltaX;
            }

            if (Math.Abs(denominator) < 0.0000001)
            {
                return false;
            }

            slope = numerator / denominator;
            intercept = meanY - (slope * meanX);
            return !double.IsNaN(slope) && !double.IsInfinity(slope) &&
                   !double.IsNaN(intercept) && !double.IsInfinity(intercept);
        }

        private static double CalculateRegressionWeight(
            LevelLine level,
            double currentPrice,
            double priceStep,
            int minIndex,
            int maxIndex)
        {
            var timeframeWeight = 1.0 + Math.Log(1.0 + Math.Max(0.0, level.TimeframeWeight));
            var span = Math.Max(1, maxIndex - minIndex);
            var recencyWeight = 1.0 + ((double)(level.StartIndex - minIndex) / span);
            var distance = priceStep > 0.0
                ? Math.Abs(level.Price - currentPrice) / priceStep
                : Math.Abs(level.Price - currentPrice);
            var priceWeight = 1.0 / (1.0 + distance);

            return timeframeWeight * recencyWeight * priceWeight;
        }

        private static void ApplySelectionScores(
            List<TrendLineCandidate> candidates,
            AkodeTrendlineAlgorithm algorithm)
        {
            if (candidates.Count == 0)
            {
                return;
            }

            var maxTouches = candidates.Max(candidate => candidate.TouchCount);
            var minTouches = candidates.Min(candidate => candidate.TouchCount);
            var maxDistance = candidates.Max(candidate => candidate.DistanceToCurrentPriceTicks);
            var minDistance = candidates.Min(candidate => candidate.DistanceToCurrentPriceTicks);
            var maxTimeframe = candidates.Max(candidate => candidate.TimeframeTouchScore);
            var minTimeframe = candidates.Min(candidate => candidate.TimeframeTouchScore);
            var maxSpan = candidates.Max(candidate => candidate.Span);
            var minSpan = candidates.Min(candidate => candidate.Span);
            var maxFreshness = candidates.Max(candidate => candidate.LastTouchIndex);
            var minFreshness = candidates.Min(candidate => candidate.LastTouchIndex);
            var maxResidual = candidates.Max(candidate => candidate.AverageResidualTicks);
            var minResidual = candidates.Min(candidate => candidate.AverageResidualTicks);
            foreach (var candidate in candidates)
            {
                var touchScore = Normalize(candidate.TouchCount, minTouches, maxTouches);
                var priceScore = 1.0 - Normalize(candidate.DistanceToCurrentPriceTicks, minDistance, maxDistance);
                var timeframeScore = Normalize(candidate.TimeframeTouchScore, minTimeframe, maxTimeframe);
                var spanScore = Normalize(candidate.Span, minSpan, maxSpan);
                var freshnessScore = Normalize(candidate.LastTouchIndex, minFreshness, maxFreshness);
                var residualScore = 1.0 - Normalize(candidate.AverageResidualTicks, minResidual, maxResidual);

                switch (algorithm)
                {
                    case AkodeTrendlineAlgorithm.WeightedRegression:
                        candidate.SelectionScore =
                            (0.35 * timeframeScore) +
                            (0.25 * residualScore) +
                            (0.20 * priceScore) +
                            (0.10 * touchScore) +
                            (0.10 * spanScore);
                        break;
                    case AkodeTrendlineAlgorithm.Ransac:
                        candidate.SelectionScore =
                            (0.35 * touchScore) +
                            (0.25 * residualScore) +
                            (0.15 * priceScore) +
                            (0.15 * timeframeScore) +
                            (0.10 * spanScore);
                        break;
                    case AkodeTrendlineAlgorithm.HoughTransform:
                        candidate.SelectionScore =
                            (0.30 * timeframeScore) +
                            (0.25 * residualScore) +
                            (0.20 * touchScore) +
                            (0.15 * priceScore) +
                            (0.10 * spanScore);
                        break;
                    default:
                        candidate.SelectionScore =
                            (0.55 * touchScore) +
                            (0.20 * spanScore) +
                            (0.15 * freshnessScore) +
                            (0.10 * priceScore);
                        break;
                }
            }
        }

        private static void SortCandidates(
            List<TrendLineCandidate> candidates,
            AkodeTrendlineAlgorithm algorithm)
        {
            candidates.Sort((left, right) => CompareCandidates(left, right, algorithm));

            for (int i = 0; i < candidates.Count; i++)
            {
                candidates[i].SelectionRank = i;
            }
        }

        private static int CompareCandidates(
            TrendLineCandidate left,
            TrendLineCandidate right,
            AkodeTrendlineAlgorithm algorithm)
        {
            int result;

            switch (algorithm)
            {
                case AkodeTrendlineAlgorithm.WeightedRegression:
                    result = CompareDescending(left.SelectionScore, right.SelectionScore);
                    if (result != 0) return result;
                    result = CompareAscending(left.AverageResidualTicks, right.AverageResidualTicks);
                    if (result != 0) return result;
                    result = CompareDescending(left.TimeframeTouchScore, right.TimeframeTouchScore);
                    if (result != 0) return result;
                    result = CompareAscending(left.DistanceToCurrentPriceTicks, right.DistanceToCurrentPriceTicks);
                    if (result != 0) return result;
                    break;

                case AkodeTrendlineAlgorithm.Ransac:
                    result = CompareDescending(left.TouchCount, right.TouchCount);
                    if (result != 0) return result;
                    result = CompareDescending(left.SelectionScore, right.SelectionScore);
                    if (result != 0) return result;
                    result = CompareAscending(left.AverageResidualTicks, right.AverageResidualTicks);
                    if (result != 0) return result;
                    result = CompareAscending(left.DistanceToCurrentPriceTicks, right.DistanceToCurrentPriceTicks);
                    if (result != 0) return result;
                    break;

                case AkodeTrendlineAlgorithm.HoughTransform:
                    result = CompareDescending(left.SelectionScore, right.SelectionScore);
                    if (result != 0) return result;
                    result = CompareDescending(left.TimeframeTouchScore, right.TimeframeTouchScore);
                    if (result != 0) return result;
                    result = CompareAscending(left.AverageResidualTicks, right.AverageResidualTicks);
                    if (result != 0) return result;
                    result = CompareAscending(left.DistanceToCurrentPriceTicks, right.DistanceToCurrentPriceTicks);
                    if (result != 0) return result;
                    break;

                default:
                    result = CompareDescending(left.TouchCount, right.TouchCount);
                    if (result != 0) return result;
                    result = CompareDescending(left.Span, right.Span);
                    if (result != 0) return result;
                    result = CompareDescending(left.LastTouchIndex, right.LastTouchIndex);
                    if (result != 0) return result;
                    result = CompareAscending(left.DistanceToCurrentPriceTicks, right.DistanceToCurrentPriceTicks);
                    if (result != 0) return result;
                    break;
            }

            result = CompareDescending(left.TimeframeTouchScore, right.TimeframeTouchScore);
            if (result != 0)
            {
                return result;
            }

            result = CompareDescending(left.SelectionScore, right.SelectionScore);
            if (result != 0)
            {
                return result;
            }

            result = CompareDescending(left.StartIndex, right.StartIndex);
            if (result != 0)
            {
                return result;
            }

            return CompareDescending(left.StartPrice, right.StartPrice);
        }

        private static bool FillSelection(
            List<TrendLineCandidate> selected,
            List<TrendLineCandidate> pool,
            HashSet<TrendLineCandidate> blocked,
            int maxCount,
            double tolerance,
            int dataLength,
            double currentPrice,
            double priceStep,
            AkodeTrendlineAlgorithm algorithm)
        {
            if (maxCount <= 0 || selected.Count >= maxCount)
            {
                return false;
            }

            var changed = false;

            foreach (var candidate in pool)
            {
                if (selected.Count >= maxCount)
                {
                    break;
                }

                if (blocked.Contains(candidate) || selected.Contains(candidate))
                {
                    continue;
                }

                if (selected.Any(existing => AreSameSideDuplicates(existing, candidate, tolerance, dataLength, currentPrice, priceStep)))
                {
                    continue;
                }

                selected.Add(candidate);
                changed = true;
            }

            if (changed)
            {
                SortCandidates(selected, algorithm);
            }

            return changed;
        }

        private static bool RemoveSameSideDuplicates(
            List<TrendLineCandidate> selected,
            HashSet<TrendLineCandidate> blocked,
            double tolerance,
            int dataLength,
            double currentPrice,
            double priceStep,
            AkodeTrendlineAlgorithm algorithm)
        {
            if (selected.Count < 2)
            {
                return false;
            }

            SortCandidates(selected, algorithm);

            var changed = false;

            for (int i = 0; i < selected.Count - 1; i++)
            {
                for (int j = selected.Count - 1; j > i; j--)
                {
                    if (!AreSameSideDuplicates(selected[i], selected[j], tolerance, dataLength, currentPrice, priceStep))
                    {
                        continue;
                    }

                    blocked.Add(selected[j]);
                    selected.RemoveAt(j);
                    changed = true;
                }
            }

            if (changed)
            {
                SortCandidates(selected, algorithm);
            }

            return changed;
        }

        private static bool AreSameSideDuplicates(
            TrendLineCandidate left,
            TrendLineCandidate right,
            double tolerance,
            int dataLength,
            double currentPrice,
            double priceStep)
        {
            var lastIndex = Math.Max(0, dataLength - 1);
            var leftValue = left.GetValue(lastIndex);
            var rightValue = right.GetValue(lastIndex);
            var onSamePriceSide = ((leftValue - currentPrice) * (rightValue - currentPrice)) >= 0.0;

            if (!onSamePriceSide)
            {
                return false;
            }

            var priceBand = Math.Max(2.0 * tolerance, 2.0 * priceStep);
            var slopeTolerance = priceBand / Math.Max(20, dataLength);

            return Math.Abs(leftValue - rightValue) <= priceBand &&
                   Math.Abs(left.Slope - right.Slope) <= slopeTolerance;
        }

        private static bool TryFindConflict(
            List<TrendLineCandidate> highLines,
            List<TrendLineCandidate> lowLines,
            int allowedPastCrossingBars,
            int dataLength,
            out TrendConflict conflict)
        {
            conflict = null;

            if (highLines.Count == 0 || lowLines.Count == 0)
            {
                return false;
            }

            var lastIndex = Math.Max(0, dataLength - 1);
            var threshold = lastIndex - Math.Max(0, allowedPastCrossingBars);

            foreach (var highLine in highLines)
            {
                foreach (var lowLine in lowLines)
                {
                    double intersectionIndex;
                    if (!TryGetIntersectionIndex(highLine, lowLine, out intersectionIndex))
                    {
                        continue;
                    }

                    if (intersectionIndex > lastIndex || intersectionIndex >= threshold)
                    {
                        continue;
                    }

                    if (conflict == null || intersectionIndex < conflict.IntersectionIndex)
                    {
                        conflict = new TrendConflict
                        {
                            HighLine = highLine,
                            LowLine = lowLine,
                            IntersectionIndex = intersectionIndex
                        };
                    }
                }
            }

            return conflict != null;
        }

        private static bool TryGetIntersectionIndex(
            TrendLineCandidate first,
            TrendLineCandidate second,
            out double intersectionIndex)
        {
            intersectionIndex = 0.0;

            var denominator = first.Slope - second.Slope;
            if (Math.Abs(denominator) < 0.0000000001)
            {
                return false;
            }

            var firstIntercept = first.StartPrice - (first.Slope * first.StartIndex);
            var secondIntercept = second.StartPrice - (second.Slope * second.StartIndex);
            intersectionIndex = (secondIntercept - firstIntercept) / denominator;

            return !double.IsNaN(intersectionIndex) && !double.IsInfinity(intersectionIndex);
        }

        private static void RemoveWeakerConflict(
            List<TrendLineCandidate> highLines,
            List<TrendLineCandidate> lowLines,
            HashSet<TrendLineCandidate> blockedHigh,
            HashSet<TrendLineCandidate> blockedLow,
            TrendConflict conflict,
            AkodeTrendlineAlgorithm algorithm)
        {
            if (ShouldRemoveHigh(conflict.HighLine, conflict.LowLine, algorithm))
            {
                blockedHigh.Add(conflict.HighLine);
                highLines.Remove(conflict.HighLine);
                return;
            }

            blockedLow.Add(conflict.LowLine);
            lowLines.Remove(conflict.LowLine);
        }

        private static bool ShouldRemoveHigh(
            TrendLineCandidate highLine,
            TrendLineCandidate lowLine,
            AkodeTrendlineAlgorithm algorithm)
        {
            var comparison = CompareCandidates(highLine, lowLine, algorithm);

            if (comparison < 0)
            {
                return false;
            }

            if (comparison > 0)
            {
                return true;
            }

            var distanceComparison = CompareAscending(highLine.DistanceToCurrentPriceTicks, lowLine.DistanceToCurrentPriceTicks);
            if (distanceComparison > 0)
            {
                return true;
            }

            if (distanceComparison < 0)
            {
                return false;
            }

            return CompareAscending(highLine.LastTouchIndex, lowLine.LastTouchIndex) < 0;
        }

        private static int CompareDescending(int left, int right)
        {
            return right.CompareTo(left);
        }

        private static int CompareDescending(double left, double right)
        {
            return right.CompareTo(left);
        }

        private static int CompareAscending(double left, double right)
        {
            return left.CompareTo(right);
        }

        private static double Normalize(double value, double min, double max)
        {
            if (Math.Abs(max - min) < 0.0000001)
            {
                return 1.0;
            }

            return (value - min) / (max - min);
        }

        private static bool IsBrokenByPrice(
            TrendLineCandidate candidate,
            double[] bodyHigh,
            double[] bodyLow,
            int trendlineBreakBars,
            double trendlineBreakTolerance)
        {
            if (candidate == null || bodyHigh == null || bodyLow == null)
            {
                return false;
            }

            var dataLength = Math.Min(bodyHigh.Length, bodyLow.Length);
            if (dataLength == 0)
            {
                return false;
            }

            var breakCount = 0;
            var startIndex = Math.Max(0, Math.Min(candidate.BreakStartIndex + 1, dataLength - 1));

            for (int i = startIndex; i < dataLength; i++)
            {
                var lineValue = candidate.GetValue(i);
                var isBroken = candidate.IsHighSide
                    ? bodyHigh[i] > lineValue + trendlineBreakTolerance
                    : bodyLow[i] < lineValue - trendlineBreakTolerance;

                if (isBroken)
                {
                    breakCount++;
                    if (breakCount >= trendlineBreakBars)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static int GetBreakStartIndex(IEnumerable<LevelLine> touchedLevels, int fallbackIndex)
        {
            if (touchedLevels == null)
            {
                return fallbackIndex;
            }

            var ordered = touchedLevels
                .OrderBy(level => level.StartIndex)
                .ToList();

            if (ordered.Count < 2)
            {
                return fallbackIndex;
            }

            return ordered[0].StartIndex;
        }

        private static double ToTicks(double value, double priceStep)
        {
            return priceStep > 0.0
                ? value / priceStep
                : value;
        }

        private static void ApplyLevelMetadata(
            List<LevelLine> levels,
            int profileIndex,
            ChartPeriodType timeframeType,
            int timeframeInterval,
            double timeframeWeight)
        {
            for (int i = 0; i < levels.Count; i++)
            {
                var level = levels[i];
                level.ProfileIndex = profileIndex;
                level.TimeframeType = timeframeType;
                level.TimeframeInterval = timeframeInterval;
                level.TimeframeWeight = timeframeWeight;
                levels[i] = level;
            }
        }

        private static double ResolveTimeframeWeight(
            AkodeTrendsProfileSettings settings,
            IChartDataProvider dataProvider,
            out ChartPeriodType timeframeType,
            out int timeframeInterval)
        {
            timeframeInterval = settings.PeriodValue > 0 ? settings.PeriodValue : 1;

            switch (settings.PeriodType)
            {
                case AkodeLevelsPeriodType.Minute:
                    timeframeType = ChartPeriodType.Minute;
                    return ConvertTimeframeToMinutes(timeframeType, timeframeInterval);
                case AkodeLevelsPeriodType.Hour:
                    timeframeType = ChartPeriodType.Hour;
                    return ConvertTimeframeToMinutes(timeframeType, timeframeInterval);
                case AkodeLevelsPeriodType.Week:
                    timeframeType = ChartPeriodType.Week;
                    return ConvertTimeframeToMinutes(timeframeType, timeframeInterval);
                case AkodeLevelsPeriodType.Month:
                    timeframeType = ChartPeriodType.Month;
                    return ConvertTimeframeToMinutes(timeframeType, timeframeInterval);
                case AkodeLevelsPeriodType.AnyTimeFrame:
                default:
                    if (dataProvider == null || dataProvider.Period == null)
                    {
                        timeframeType = ChartPeriodType.Tick;
                        timeframeInterval = 0;
                        return 0.0;
                    }

                    timeframeType = dataProvider.Period.Type;
                    timeframeInterval = Math.Max(1, dataProvider.Period.Interval);
                    return ConvertTimeframeToMinutes(timeframeType, timeframeInterval);
            }
        }

        private static double ConvertTimeframeToMinutes(
            ChartPeriodType timeframeType,
            int timeframeInterval)
        {
            timeframeInterval = Math.Max(1, timeframeInterval);

            switch (timeframeType)
            {
                case ChartPeriodType.Second:
                    return timeframeInterval / 60.0;
                case ChartPeriodType.Minute:
                    return timeframeInterval;
                case ChartPeriodType.Hour:
                    return timeframeInterval * 60.0;
                case ChartPeriodType.Day:
                    return timeframeInterval * 1440.0;
                case ChartPeriodType.Week:
                    return timeframeInterval * 10080.0;
                case ChartPeriodType.Month:
                    return timeframeInterval * 43200.0;
                case ChartPeriodType.Year:
                    return timeframeInterval * 525600.0;
                default:
                    return 0.0;
            }
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
            var testedLimit = isHigh
                ? Math.Max(0, settings.MaxTestedLinesHigh)
                : Math.Max(0, settings.MaxTestedLinesLow);
            var brokenLimit = isHigh
                ? Math.Max(0, settings.MaxBrokenLinesHigh)
                : Math.Max(0, settings.MaxBrokenLinesLow);

            var levels = orderedPivots
                .Where(pivot => !pivot.IsBroken && !pivot.IsTested)
                .Take(activeLimit)
                .ToList();

            if (settings.ShowTestedLines && testedLimit > 0)
            {
                levels.AddRange(orderedPivots
                    .Where(pivot => pivot.IsTested)
                    .Take(testedLimit));
            }

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
            double[] closePrices,
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
                bool isBroken, isTested;
                ClassifyLevel(startIndex, pivotPrice, originalPrices, closePrices, isHigh, out isBroken, out isTested);

                pivots.Add(new LevelLine
                {
                    Price = pivotPrice,
                    StartIndex = startIndex,
                    IsBroken = isBroken,
                    IsTested = isTested
                });
            }

            return pivots;
        }

        private static List<LevelLine> FindPivotsInCurrentData(
            double[] prices,
            double[] closePrices,
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

                bool isBroken, isTested;
                ClassifyLevel(i, price, prices, closePrices, isHigh, out isBroken, out isTested);

                pivots.Add(new LevelLine
                {
                    Price = price,
                    StartIndex = i,
                    IsBroken = isBroken,
                    IsTested = isTested
                });
            }

            return pivots;
        }

        private static void ClassifyLevel(int startIndex, double price,
            double[] wickPrices, double[] closePrices, bool isHigh,
            out bool isBroken, out bool isTested)
        {
            isBroken = false;
            isTested = false;

            for (int k = startIndex + 1; k < closePrices.Length; k++)
            {
                if (isHigh ? closePrices[k] > price : closePrices[k] < price)
                {
                    isBroken = true;
                    isTested = false;
                    return;
                }

                if (!isTested && (isHigh ? wickPrices[k] > price : wickPrices[k] < price))
                {
                    isTested = true;
                }
            }
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
