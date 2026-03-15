using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;
using TigerTrade.Chart.Base;
using TigerTrade.Chart.Indicators.Common;
using TigerTrade.Chart.Indicators.Drawings;
using TigerTrade.Chart.Indicators.Enums;
using TigerTrade.Core.UI.Converters;
using TigerTrade.Dx;
using TigerTrade.Dx.Enums;

namespace Akode.TigerTrade.Indicators
{
    [DataContract(
        Name = "AkodeTrendsIndicator",
        Namespace = "http://schemas.datacontract.org/2004/07/TigerTrade.Chart.Indicators.Custom"
    )]
    [Indicator("X_AkodeTrendsIndicator", "_Akode: Trends", true, Type = typeof(AkodeTrendsIndicator))]
    public sealed class AkodeTrendsIndicator : IndicatorBase
    {
        private AkodeTrendsProfileSettings _profile1;
        private AkodeTrendsProfileSettings _profile2;
        private AkodeTrendsProfileSettings _profile3;
        private AkodeTrendsProfileSettings _profile4;
        private AkodeTrendsProfileSettings _profile5;
        private bool _showTrendLines = true;
        private int _maxHighTrendLines = 3;
        private int _maxLowTrendLines = 3;
        private int _trendlineToleranceTicks = 2;
        private int _minHighTrendlineTouches = 2;
        private int _minLowTrendlineTouches = 2;
        private int _trendlineLeftPaddingBars = 3;
        private int _trendlineRightPaddingBars = 20;
        private bool _highSlopeFilterEnabled = true;
        private bool _lowSlopeFilterEnabled = true;
        private AkodeTrendlineAlgorithm _trendlineAlgorithm = AkodeTrendlineAlgorithm.ClassicTouches;
        private int _allowedPastCrossingBars = 5;
        private bool _hideBrokenTrendLines = true;
        private int _trendlineBreakBars = 2;
        private int _trendlineBreakToleranceTicks = 2;
        private ChartLine _highTrendSeries;
        private ChartLine _lowTrendSeries;
        private int _maxTotalHighLevels;
        private int _maxTotalLowLevels;
        private int _levelTimeFilterMinutes;
        private int _levelMergeDistanceTicks;
        private bool _applyLevelFiltersToTrendlines = true;

        [Browsable(false)]
        public override IndicatorCalculation Calculation
        {
            get { return IndicatorCalculation.OnBarClose; }
        }

        [DataMember(Name = "Profile1")]
        [Category("Profiles"), DisplayName("Profile 1")]
        public AkodeTrendsProfileSettings Profile1
        {
            get
            {
                InitializeProfile(ref _profile1, 1, true);
                return _profile1;
            }
            set
            {
                SetProfile(ref _profile1, value, 1, true);
            }
        }

        [DataMember(Name = "Profile2")]
        [Category("Profiles"), DisplayName("Profile 2")]
        public AkodeTrendsProfileSettings Profile2
        {
            get
            {
                InitializeProfile(ref _profile2, 2, false);
                return _profile2;
            }
            set
            {
                SetProfile(ref _profile2, value, 2, false);
            }
        }

        [DataMember(Name = "Profile3")]
        [Category("Profiles"), DisplayName("Profile 3")]
        public AkodeTrendsProfileSettings Profile3
        {
            get
            {
                InitializeProfile(ref _profile3, 3, false);
                return _profile3;
            }
            set
            {
                SetProfile(ref _profile3, value, 3, false);
            }
        }

        [DataMember(Name = "Profile4")]
        [Category("Profiles"), DisplayName("Profile 4")]
        public AkodeTrendsProfileSettings Profile4
        {
            get
            {
                InitializeProfile(ref _profile4, 4, false);
                return _profile4;
            }
            set
            {
                SetProfile(ref _profile4, value, 4, false);
            }
        }

        [DataMember(Name = "Profile5")]
        [Category("Profiles"), DisplayName("Profile 5")]
        public AkodeTrendsProfileSettings Profile5
        {
            get
            {
                InitializeProfile(ref _profile5, 5, false);
                return _profile5;
            }
            set
            {
                SetProfile(ref _profile5, value, 5, false);
            }
        }

        [DataMember(Name = "MaxTotalHighLevels")]
        [Category("Level lines"), DisplayName("Max total High levels")]
        public int MaxTotalHighLevels
        {
            get { return _maxTotalHighLevels; }
            set
            {
                value = Math.Max(0, value);

                if (value == _maxTotalHighLevels)
                {
                    return;
                }

                _maxTotalHighLevels = value;
                OnPropertyChanged();
            }
        }

        [DataMember(Name = "MaxTotalLowLevels")]
        [Category("Level lines"), DisplayName("Max total Low levels")]
        public int MaxTotalLowLevels
        {
            get { return _maxTotalLowLevels; }
            set
            {
                value = Math.Max(0, value);

                if (value == _maxTotalLowLevels)
                {
                    return;
                }

                _maxTotalLowLevels = value;
                OnPropertyChanged();
            }
        }

        [DataMember(Name = "LevelTimeFilterMinutes")]
        [Category("Level lines"), DisplayName("Time filter (minutes)")]
        public int LevelTimeFilterMinutes
        {
            get { return _levelTimeFilterMinutes; }
            set
            {
                value = Math.Max(0, value);

                if (value == _levelTimeFilterMinutes)
                {
                    return;
                }

                _levelTimeFilterMinutes = value;
                OnPropertyChanged();
            }
        }

        [DataMember(Name = "LevelMergeDistanceTicks")]
        [Category("Level lines"), DisplayName("Merge distance (ticks)")]
        public int LevelMergeDistanceTicks
        {
            get { return _levelMergeDistanceTicks; }
            set
            {
                value = Math.Max(0, value);

                if (value == _levelMergeDistanceTicks)
                {
                    return;
                }

                _levelMergeDistanceTicks = value;
                OnPropertyChanged();
            }
        }

        [DataMember(Name = "ApplyLevelFiltersToTrendlines")]
        [Category("Level lines"), DisplayName("Apply filters to trend lines")]
        public bool ApplyLevelFiltersToTrendlines
        {
            get { return _applyLevelFiltersToTrendlines; }
            set
            {
                if (value == _applyLevelFiltersToTrendlines)
                {
                    return;
                }

                _applyLevelFiltersToTrendlines = value;
                OnPropertyChanged();
            }
        }

        [DataMember(Name = "ShowTrendLines")]
        [Category("Trend lines"), DisplayName("Show trend lines")]
        public bool ShowTrendLines
        {
            get { return _showTrendLines; }
            set
            {
                if (value == _showTrendLines)
                {
                    return;
                }

                _showTrendLines = value;
                OnPropertyChanged();
            }
        }

        [DataMember(Name = "MaxHighTrendLines")]
        [Category("Trend lines"), DisplayName("Max High trend lines")]
        public int MaxHighTrendLines
        {
            get { return _maxHighTrendLines; }
            set
            {
                value = Math.Max(0, value);

                if (value == _maxHighTrendLines)
                {
                    return;
                }

                _maxHighTrendLines = value;
                OnPropertyChanged();
            }
        }

        [DataMember(Name = "MaxLowTrendLines")]
        [Category("Trend lines"), DisplayName("Max Low trend lines")]
        public int MaxLowTrendLines
        {
            get { return _maxLowTrendLines; }
            set
            {
                value = Math.Max(0, value);

                if (value == _maxLowTrendLines)
                {
                    return;
                }

                _maxLowTrendLines = value;
                OnPropertyChanged();
            }
        }

        [DataMember(Name = "TrendlineToleranceTicks")]
        [Category("Trend lines"), DisplayName("Tolerance in ticks")]
        public int TrendlineToleranceTicks
        {
            get { return _trendlineToleranceTicks; }
            set
            {
                value = Math.Max(0, value);

                if (value == _trendlineToleranceTicks)
                {
                    return;
                }

                _trendlineToleranceTicks = value;
                OnPropertyChanged();
            }
        }

        [DataMember(Name = "MinHighTrendlineTouches")]
        [Category("Trend lines"), DisplayName("Min High touches")]
        public int MinHighTrendlineTouches
        {
            get { return _minHighTrendlineTouches; }
            set
            {
                value = Math.Max(2, value);

                if (value == _minHighTrendlineTouches)
                {
                    return;
                }

                _minHighTrendlineTouches = value;
                OnPropertyChanged();
            }
        }

        [DataMember(Name = "MinLowTrendlineTouches")]
        [Category("Trend lines"), DisplayName("Min Low touches")]
        public int MinLowTrendlineTouches
        {
            get { return _minLowTrendlineTouches; }
            set
            {
                value = Math.Max(2, value);

                if (value == _minLowTrendlineTouches)
                {
                    return;
                }

                _minLowTrendlineTouches = value;
                OnPropertyChanged();
            }
        }

        [DataMember(Name = "TrendlineLeftPaddingBars")]
        [Category("Trend lines"), DisplayName("Left padding bars")]
        public int TrendlineLeftPaddingBars
        {
            get { return _trendlineLeftPaddingBars; }
            set
            {
                value = Math.Max(0, value);

                if (value == _trendlineLeftPaddingBars)
                {
                    return;
                }

                _trendlineLeftPaddingBars = value;
                OnPropertyChanged();
            }
        }

        [DataMember(Name = "TrendlineRightPaddingBars")]
        [Category("Trend lines"), DisplayName("Right padding bars")]
        public int TrendlineRightPaddingBars
        {
            get { return _trendlineRightPaddingBars; }
            set
            {
                value = Math.Max(0, value);

                if (value == _trendlineRightPaddingBars)
                {
                    return;
                }

                _trendlineRightPaddingBars = value;
                OnPropertyChanged();
            }
        }

        [DataMember(Name = "HighSlopeFilterEnabled")]
        [Category("Trend lines"), DisplayName("High: only down slope")]
        public bool HighSlopeFilterEnabled
        {
            get { return _highSlopeFilterEnabled; }
            set
            {
                if (value == _highSlopeFilterEnabled)
                {
                    return;
                }

                _highSlopeFilterEnabled = value;
                OnPropertyChanged();
            }
        }

        [DataMember(Name = "LowSlopeFilterEnabled")]
        [Category("Trend lines"), DisplayName("Low: only up slope")]
        public bool LowSlopeFilterEnabled
        {
            get { return _lowSlopeFilterEnabled; }
            set
            {
                if (value == _lowSlopeFilterEnabled)
                {
                    return;
                }

                _lowSlopeFilterEnabled = value;
                OnPropertyChanged();
            }
        }

        [DataMember(Name = "TrendlineAlgorithm")]
        [Category("Trend lines"), DisplayName("Trendline algorithm")]
        public AkodeTrendlineAlgorithm TrendlineAlgorithm
        {
            get { return _trendlineAlgorithm; }
            set
            {
                if (value == _trendlineAlgorithm)
                {
                    return;
                }

                _trendlineAlgorithm = value;
                OnPropertyChanged();
            }
        }

        [DataMember(Name = "AllowedPastCrossingBars")]
        [Category("Trend lines"), DisplayName("Allowed past crossing bars")]
        public int AllowedPastCrossingBars
        {
            get { return _allowedPastCrossingBars; }
            set
            {
                value = Math.Max(0, value);

                if (value == _allowedPastCrossingBars)
                {
                    return;
                }

                _allowedPastCrossingBars = value;
                OnPropertyChanged();
            }
        }

        [DataMember(Name = "HideBrokenTrendLines")]
        [Category("Trend lines"), DisplayName("Hide broken trend lines")]
        public bool HideBrokenTrendLines
        {
            get { return _hideBrokenTrendLines; }
            set
            {
                if (value == _hideBrokenTrendLines)
                {
                    return;
                }

                _hideBrokenTrendLines = value;
                OnPropertyChanged();
            }
        }

        [DataMember(Name = "TrendlineBreakBars")]
        [Category("Trend lines"), DisplayName("Break bars")]
        public int TrendlineBreakBars
        {
            get { return _trendlineBreakBars; }
            set
            {
                value = Math.Max(1, value);

                if (value == _trendlineBreakBars)
                {
                    return;
                }

                _trendlineBreakBars = value;
                OnPropertyChanged();
            }
        }

        [DataMember(Name = "TrendlineBreakToleranceTicks")]
        [Category("Trend lines"), DisplayName("Break tolerance in ticks")]
        public int TrendlineBreakToleranceTicks
        {
            get { return _trendlineBreakToleranceTicks; }
            set
            {
                value = Math.Max(0, value);

                if (value == _trendlineBreakToleranceTicks)
                {
                    return;
                }

                _trendlineBreakToleranceTicks = value;
                OnPropertyChanged();
            }
        }

        [DataMember(Name = "HighTrendSeries")]
        [Category("Trend display"), DisplayName("High trends")]
        public ChartLine HighTrendSeries
        {
            get
            {
                InitializeTrendStyles();
                return _highTrendSeries;
            }
            set
            {
                SetTrendSeries(ref _highTrendSeries, value, CreateDefaultTrendSeries(true));
            }
        }

        [DataMember(Name = "LowTrendSeries")]
        [Category("Trend display"), DisplayName("Low trends")]
        public ChartLine LowTrendSeries
        {
            get
            {
                InitializeTrendStyles();
                return _lowTrendSeries;
            }
            set
            {
                SetTrendSeries(ref _lowTrendSeries, value, CreateDefaultTrendSeries(false));
            }
        }

        public AkodeTrendsIndicator()
        {
            InitializeProfiles();
            InitializeTrendStyles();
        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            InitializeProfiles();
            InitializeTrendStyles();
        }

        public override void ApplyColors(IChartTheme theme)
        {
            InitializeProfiles();
            InitializeTrendStyles();

            Profile1.HighSeries.Color = theme.GetNextColor();
            Profile1.LowSeries.Color = theme.GetNextColor();
            Profile2.HighSeries.Color = theme.GetNextColor();
            Profile2.LowSeries.Color = theme.GetNextColor();
            Profile3.HighSeries.Color = theme.GetNextColor();
            Profile3.LowSeries.Color = theme.GetNextColor();
            Profile4.HighSeries.Color = theme.GetNextColor();
            Profile4.LowSeries.Color = theme.GetNextColor();
            Profile5.HighSeries.Color = theme.GetNextColor();
            Profile5.LowSeries.Color = theme.GetNextColor();
            HighTrendSeries.Color = theme.GetNextColor();
            LowTrendSeries.Color = theme.GetNextColor();

            base.ApplyColors(theme);
        }

        public override void CopyTemplate(IndicatorBase indicator, bool style)
        {
            var source = (AkodeTrendsIndicator)indicator;

            Profile1.CopyFrom(source.Profile1);
            Profile2.CopyFrom(source.Profile2);
            Profile3.CopyFrom(source.Profile3);
            Profile4.CopyFrom(source.Profile4);
            Profile5.CopyFrom(source.Profile5);

            MaxTotalHighLevels = source.MaxTotalHighLevels;
            MaxTotalLowLevels = source.MaxTotalLowLevels;
            LevelTimeFilterMinutes = source.LevelTimeFilterMinutes;
            LevelMergeDistanceTicks = source.LevelMergeDistanceTicks;
            ApplyLevelFiltersToTrendlines = source.ApplyLevelFiltersToTrendlines;

            ShowTrendLines = source.ShowTrendLines;
            MaxHighTrendLines = source.MaxHighTrendLines;
            MaxLowTrendLines = source.MaxLowTrendLines;
            TrendlineToleranceTicks = source.TrendlineToleranceTicks;
            MinHighTrendlineTouches = source.MinHighTrendlineTouches;
            MinLowTrendlineTouches = source.MinLowTrendlineTouches;
            TrendlineLeftPaddingBars = source.TrendlineLeftPaddingBars;
            TrendlineRightPaddingBars = source.TrendlineRightPaddingBars;
            HighSlopeFilterEnabled = source.HighSlopeFilterEnabled;
            LowSlopeFilterEnabled = source.LowSlopeFilterEnabled;
            TrendlineAlgorithm = source.TrendlineAlgorithm;
            AllowedPastCrossingBars = source.AllowedPastCrossingBars;
            HideBrokenTrendLines = source.HideBrokenTrendLines;
            TrendlineBreakBars = source.TrendlineBreakBars;
            TrendlineBreakToleranceTicks = source.TrendlineBreakToleranceTicks;

            HighTrendSeries.CopyTheme(source.HighTrendSeries);
            LowTrendSeries.CopyTheme(source.LowTrendSeries);

            base.CopyTemplate(indicator, style);
        }

        protected override void Execute()
        {
            var dataLength = Helper.Count;
            if (dataLength == 0)
            {
                return;
            }

            var highLevels = new List<TrendsCalculationEngine.LevelLine>();
            var lowLevels = new List<TrendsCalculationEngine.LevelLine>();

            CollectProfileLevels(Profile1, 1, highLevels, lowLevels);
            CollectProfileLevels(Profile2, 2, highLevels, lowLevels);
            CollectProfileLevels(Profile3, 3, highLevels, lowLevels);
            CollectProfileLevels(Profile4, 4, highLevels, lowLevels);
            CollectProfileLevels(Profile5, 5, highLevels, lowLevels);

            var priceStep = DataProvider != null ? DataProvider.Step : 0.0;

            var filteredHigh = ApplyLevelFilters(highLevels, true, dataLength, priceStep);
            var filteredLow = ApplyLevelFilters(lowLevels, false, dataLength, priceStep);

            DrawFilteredHorizontalLines(filteredHigh, true, dataLength);
            DrawFilteredHorizontalLines(filteredLow, false, dataLength);

            if (!ShowTrendLines)
            {
                return;
            }

            var tolerance = DataProvider != null
                ? Math.Max(0, TrendlineToleranceTicks) * DataProvider.Step
                : 0.0;
            var currentPrice = Helper.Close[dataLength - 1];
            var breakTolerance = DataProvider != null
                ? Math.Max(0, TrendlineBreakToleranceTicks) * DataProvider.Step
                : 0.0;
            var bodyHigh = BuildBodyHigh(Helper.Open, Helper.Close);
            var bodyLow = BuildBodyLow(Helper.Open, Helper.Close);
            var trendHighInput = FilterByTrendlineEligibility(
                _applyLevelFiltersToTrendlines ? filteredHigh : highLevels);
            var trendLowInput = FilterByTrendlineEligibility(
                _applyLevelFiltersToTrendlines ? filteredLow : lowLevels);
            var selection = TrendsCalculationEngine.SelectTrendLines(
                trendHighInput,
                trendLowInput,
                TrendlineAlgorithm,
                HighSlopeFilterEnabled,
                LowSlopeFilterEnabled,
                MaxHighTrendLines,
                MaxLowTrendLines,
                MinHighTrendlineTouches,
                MinLowTrendlineTouches,
                tolerance,
                dataLength,
                currentPrice,
                priceStep,
                AllowedPastCrossingBars,
                HideBrokenTrendLines,
                TrendlineBreakBars,
                breakTolerance,
                bodyHigh,
                bodyLow);

            DrawTrendLines(
                selection.HighTrendLines,
                HighTrendSeries,
                dataLength);

            DrawTrendLines(
                selection.LowTrendLines,
                LowTrendSeries,
                dataLength);
        }

        private void InitializeProfiles()
        {
            InitializeProfile(ref _profile1, 1, true);
            InitializeProfile(ref _profile2, 2, false);
            InitializeProfile(ref _profile3, 3, false);
            InitializeProfile(ref _profile4, 4, false);
            InitializeProfile(ref _profile5, 5, false);
        }

        private void InitializeProfile(
            ref AkodeTrendsProfileSettings profile,
            int profileIndex,
            bool enabledByDefault)
        {
            if (profile == null)
            {
                profile = new AkodeTrendsProfileSettings();
                profile.Enabled = enabledByDefault;
            }

            profile.EnsureInitialized(profileIndex);
            profile.PropertyChanged -= HandleNestedSettingsChanged;
            profile.PropertyChanged += HandleNestedSettingsChanged;
        }

        private void SetProfile(
            ref AkodeTrendsProfileSettings field,
            AkodeTrendsProfileSettings value,
            int profileIndex,
            bool enabledByDefault)
        {
            if (field != null)
            {
                field.PropertyChanged -= HandleNestedSettingsChanged;
            }

            field = value ?? new AkodeTrendsProfileSettings();

            if (value == null)
            {
                field.Enabled = enabledByDefault;
            }

            field.EnsureInitialized(profileIndex);
            field.PropertyChanged -= HandleNestedSettingsChanged;
            field.PropertyChanged += HandleNestedSettingsChanged;

            OnPropertyChanged();
        }

        private void InitializeTrendStyles()
        {
            EnsureTrendSeries(ref _highTrendSeries, CreateDefaultTrendSeries(true));
            EnsureTrendSeries(ref _lowTrendSeries, CreateDefaultTrendSeries(false));
        }

        private void EnsureTrendSeries(ref ChartLine line, ChartLine defaultLine)
        {
            if (line == null)
            {
                line = defaultLine;
            }

            line.PropertyChanged -= HandleNestedSettingsChanged;
            line.PropertyChanged += HandleNestedSettingsChanged;
        }

        private void SetTrendSeries(ref ChartLine field, ChartLine value, ChartLine defaultLine)
        {
            if (field != null)
            {
                field.PropertyChanged -= HandleNestedSettingsChanged;
            }

            field = value ?? defaultLine;
            field.PropertyChanged -= HandleNestedSettingsChanged;
            field.PropertyChanged += HandleNestedSettingsChanged;

            OnPropertyChanged();
        }

        private static ChartLine CreateDefaultTrendSeries(bool isHigh)
        {
            return new ChartLine
            {
                Style = XDashStyle.Solid,
                Width = 2,
                Color = isHigh
                    ? XColor.FromArgb(180, 66, 66, 66)
                    : XColor.FromArgb(180, 99, 99, 99)
            };
        }

        private void HandleNestedSettingsChanged(object sender, PropertyChangedEventArgs e)
        {
            OnPropertyChanged(string.Empty);
        }

        private void CollectProfileLevels(
            AkodeTrendsProfileSettings profile,
            int profileIndex,
            List<TrendsCalculationEngine.LevelLine> highLevels,
            List<TrendsCalculationEngine.LevelLine> lowLevels)
        {
            if (profile == null || !profile.Enabled)
            {
                return;
            }

            var result = TrendsCalculationEngine.CalculateLevels(Helper, DataProvider, profile, profileIndex);

            highLevels.AddRange(result.HighLevels);
            lowLevels.AddRange(result.LowLevels);
        }

        private List<TrendsCalculationEngine.LevelLine> ApplyLevelFilters(
            List<TrendsCalculationEngine.LevelLine> levels,
            bool isHigh,
            int dataLength,
            double priceStep)
        {
            var filtered = new List<TrendsCalculationEngine.LevelLine>(levels);

            if (_levelTimeFilterMinutes > 0 && dataLength > 0)
            {
                var date = Helper.Date;
                var currentTime = DateTime.FromOADate(date[dataLength - 1]);
                var cutoff = currentTime.AddMinutes(-_levelTimeFilterMinutes);

                filtered.RemoveAll(level =>
                    level.StartIndex >= 0 && level.StartIndex < dataLength &&
                    DateTime.FromOADate(date[level.StartIndex]) < cutoff);
            }

            if (_levelMergeDistanceTicks > 0 && priceStep > 0.0 && filtered.Count > 1)
            {
                var mergeDistance = _levelMergeDistanceTicks * priceStep;

                filtered.Sort((a, b) =>
                {
                    var cmp = b.TimeframeWeight.CompareTo(a.TimeframeWeight);
                    return cmp != 0 ? cmp : b.StartIndex.CompareTo(a.StartIndex);
                });

                for (int i = 0; i < filtered.Count; i++)
                {
                    for (int j = filtered.Count - 1; j > i; j--)
                    {
                        if (Math.Abs(filtered[i].Price - filtered[j].Price) <= mergeDistance)
                        {
                            filtered.RemoveAt(j);
                        }
                    }
                }
            }

            var maxTotal = isHigh ? _maxTotalHighLevels : _maxTotalLowLevels;

            if (maxTotal > 0 && filtered.Count > maxTotal)
            {
                filtered.Sort((a, b) => b.StartIndex.CompareTo(a.StartIndex));
                filtered.RemoveRange(maxTotal, filtered.Count - maxTotal);
            }

            return filtered;
        }

        private void DrawFilteredHorizontalLines(
            List<TrendsCalculationEngine.LevelLine> levels,
            bool isHigh,
            int dataLength)
        {
            foreach (var level in levels)
            {
                var style = GetProfileStyle(level.ProfileIndex, isHigh);
                if (style == null)
                {
                    continue;
                }

                var data = CreateSeriesData(dataLength, level.StartIndex, delegate { return level.Price; });
                var lineStyle = CloneLine(style, level.IsBroken ? XDashStyle.Dot : style.Style);

                Series.Add(new IndicatorSeriesData(data, lineStyle)
                {
                    Style =
                    {
                        DisableMinMax = true
                    }
                });
            }
        }

        private ChartLine GetProfileStyle(int profileIndex, bool isHigh)
        {
            switch (profileIndex)
            {
                case 1: return isHigh ? Profile1.HighSeries : Profile1.LowSeries;
                case 2: return isHigh ? Profile2.HighSeries : Profile2.LowSeries;
                case 3: return isHigh ? Profile3.HighSeries : Profile3.LowSeries;
                case 4: return isHigh ? Profile4.HighSeries : Profile4.LowSeries;
                case 5: return isHigh ? Profile5.HighSeries : Profile5.LowSeries;
                default: return null;
            }
        }

        private AkodeTrendsProfileSettings GetProfile(int profileIndex)
        {
            switch (profileIndex)
            {
                case 1: return Profile1;
                case 2: return Profile2;
                case 3: return Profile3;
                case 4: return Profile4;
                case 5: return Profile5;
                default: return null;
            }
        }

        private List<TrendsCalculationEngine.LevelLine> FilterByTrendlineEligibility(
            List<TrendsCalculationEngine.LevelLine> levels)
        {
            return levels.FindAll(level =>
            {
                var profile = GetProfile(level.ProfileIndex);
                return profile == null || profile.IncludeInTrendlines;
            });
        }

        private void DrawTrendLines(
            IEnumerable<TrendsCalculationEngine.TrendLineCandidate> lines,
            ChartLine baseStyle,
            int dataLength)
        {
            foreach (var line in lines)
            {
                var startIndex = Math.Max(0, line.StartIndex - TrendlineLeftPaddingBars);
                var seriesLength = dataLength + TrendlineRightPaddingBars;
                var data = CreateSeriesData(seriesLength, startIndex, index => line.GetValue(index));

                Series.Add(new IndicatorSeriesData(data, CloneLine(baseStyle, baseStyle.Style))
                {
                    Style =
                    {
                        DisableMinMax = true,
                        StraightLine = true
                    }
                });
            }
        }

        private static double[] CreateSeriesData(int dataLength, int startIndex, Func<int, double> valueFactory)
        {
            var data = new double[dataLength];

            for (int i = 0; i < data.Length; i++)
            {
                data[i] = double.NaN;
            }

            for (int i = Math.Max(0, startIndex); i < dataLength; i++)
            {
                data[i] = valueFactory(i);
            }

            return data;
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

        private static ChartLine CloneLine(ChartLine source, XDashStyle style)
        {
            return new ChartLine
            {
                Visible = source.Visible,
                ShowMarker = source.ShowMarker,
                Color = source.Color,
                Width = source.Width,
                Style = style
            };
        }
    }

    [TypeConverter(typeof(EnumDescriptionTypeConverter))]
    [DataContract(
        Name = "AkodeTrendlineAlgorithm",
        Namespace = "http://schemas.datacontract.org/2004/07/TigerTrade.Chart.Indicators.Custom"
    )]
    public enum AkodeTrendlineAlgorithm
    {
        [EnumMember(Value = "ClassicTouches"), Description("Classic Touches")]
        ClassicTouches,
        [EnumMember(Value = "WeightedRegression"), Description("Weighted Regression")]
        WeightedRegression,
        [EnumMember(Value = "Ransac"), Description("RANSAC")]
        Ransac,
        [EnumMember(Value = "HoughTransform"), Description("Hough Transform")]
        HoughTransform
    }
}
