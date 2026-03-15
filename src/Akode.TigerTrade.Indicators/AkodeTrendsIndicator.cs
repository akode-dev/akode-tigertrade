using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;
using System.Windows;
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
        private const int MaxDistancePercentLabelsPerSide = 10;
        private const double DistancePercentLabelPadding = 6.0;
        private const double DistancePercentLabelLineGap = 4.0;
        private const double DistancePercentLabelSpacing = 2.0;

        private struct VisibleHorizontalLevel
        {
            public double Price;
            public int StartIndex;
            public bool IsHigh;
            public XColor Color;
            public bool ShowScaleLabel;
        }

        private struct DistanceLabelCandidate
        {
            public VisibleHorizontalLevel Level;
            public double Distance;
        }

        private AkodeTrendsProfileSettings _profile1;
        private AkodeTrendsProfileSettings _profile2;
        private AkodeTrendsProfileSettings _profile3;
        private AkodeTrendsProfileSettings _profile4;
        private AkodeTrendsProfileSettings _profile5;
        private bool _showTrendLines = true;
        private int _maxHighTrendLines = 4;
        private int _maxLowTrendLines = 4;
        private int _trendlineToleranceTicks = 100;
        private int _minHighTrendlineTouches = 2;
        private int _minLowTrendlineTouches = 2;
        private int _trendlineLeftPaddingBars = 50;
        private int _trendlineRightPaddingBars = 500;
        private bool _highSlopeFilterEnabled = true;
        private bool _lowSlopeFilterEnabled = true;
        private AkodeTrendlineAlgorithm _trendlineAlgorithm = AkodeTrendlineAlgorithm.Ransac;
        private int _allowedPastCrossingBars = 5;
        private bool _hideBrokenTrendLines = true;
        private int _trendlineBreakBars = 5;
        private int _trendlineBreakToleranceTicks = 5;
        private ChartLine _highTrendSeries;
        private ChartLine _lowTrendSeries;
        private int _maxTotalHighLevels = 7;
        private int _maxTotalLowLevels = 7;
        private int _levelTimeFilterMinutes;
        private int _levelMergeDistanceTicks = 50;
        private bool _applyLevelFiltersToTrendlines = true;
        private int _trendlineMemoryBars;
        private int _trendlineMemoryMinutes;
        private int _trendlineMergeTicks = 100;
        private TrendsCalculationEngine.TrendSelectionResult _cachedTrendSelection;
        private int _cachedAtDataLength;
        private DateTime _cachedAtTime;
        private double _cachedFirstBarPrice;
        private bool _showDistancePercentLabels = true;
        private bool _showBaseLine;
        private int _baseLineToleranceTicks = 10;
        private int _baseLineLookbackBars;
        private int _baseLineLookbackMinutes = 120;
        private ChartLine _baseLineSeries;
        private List<VisibleHorizontalLevel> _visibleHorizontalLevels;

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
        [Category("Level lines"), DisplayName("Level merge (ticks)")]
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

        [DataMember(Name = "ShowDistancePercentLabels")]
        [Category("Level lines"), DisplayName("Show distance % labels")]
        public bool ShowDistancePercentLabels
        {
            get { return _showDistancePercentLabels; }
            set
            {
                if (value == _showDistancePercentLabels)
                {
                    return;
                }

                _showDistancePercentLabels = value;
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
                value = NormalizeTrendlineAlgorithm(value);

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

        [DataMember(Name = "TrendlineMemoryBars")]
        [Category("Trend lines"), DisplayName("Memory (bars)")]
        public int TrendlineMemoryBars
        {
            get { return _trendlineMemoryBars; }
            set
            {
                value = Math.Max(0, value);

                if (value == _trendlineMemoryBars)
                {
                    return;
                }

                _trendlineMemoryBars = value;
                _cachedTrendSelection = null;
                OnPropertyChanged();
            }
        }

        [DataMember(Name = "TrendlineMemoryMinutes")]
        [Category("Trend lines"), DisplayName("Memory (minutes)")]
        public int TrendlineMemoryMinutes
        {
            get { return _trendlineMemoryMinutes; }
            set
            {
                value = Math.Max(0, value);

                if (value == _trendlineMemoryMinutes)
                {
                    return;
                }

                _trendlineMemoryMinutes = value;
                _cachedTrendSelection = null;
                OnPropertyChanged();
            }
        }

        [DataMember(Name = "TrendlineMergeTicks")]
        [Category("Trend lines"), DisplayName("Trend merge (ticks)")]
        public int TrendlineMergeTicks
        {
            get { return _trendlineMergeTicks; }
            set
            {
                value = Math.Max(0, value);

                if (value == _trendlineMergeTicks)
                {
                    return;
                }

                _trendlineMergeTicks = value;
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

        [DataMember(Name = "ShowBaseLine")]
        [Category("Base line"), DisplayName("Show base line")]
        public bool ShowBaseLine
        {
            get { return _showBaseLine; }
            set
            {
                if (value == _showBaseLine)
                {
                    return;
                }

                _showBaseLine = value;
                OnPropertyChanged();
            }
        }

        [DataMember(Name = "BaseLineToleranceTicks")]
        [Category("Base line"), DisplayName("Tolerance (ticks)")]
        public int BaseLineToleranceTicks
        {
            get { return _baseLineToleranceTicks; }
            set
            {
                value = Math.Max(1, value);

                if (value == _baseLineToleranceTicks)
                {
                    return;
                }

                _baseLineToleranceTicks = value;
                OnPropertyChanged();
            }
        }

        [DataMember(Name = "BaseLineLookbackBars")]
        [Category("Base line"), DisplayName("Lookback (bars)")]
        public int BaseLineLookbackBars
        {
            get { return _baseLineLookbackBars; }
            set
            {
                value = Math.Max(0, value);

                if (value == _baseLineLookbackBars)
                {
                    return;
                }

                _baseLineLookbackBars = value;
                OnPropertyChanged();
            }
        }

        [DataMember(Name = "BaseLineLookbackMinutes")]
        [Category("Base line"), DisplayName("Lookback (minutes)")]
        public int BaseLineLookbackMinutes
        {
            get { return _baseLineLookbackMinutes; }
            set
            {
                value = Math.Max(0, value);

                if (value == _baseLineLookbackMinutes)
                {
                    return;
                }

                _baseLineLookbackMinutes = value;
                OnPropertyChanged();
            }
        }

        [DataMember(Name = "BaseLineSeries")]
        [Category("Base line"), DisplayName("Base line style")]
        public ChartLine BaseLineSeries
        {
            get
            {
                EnsureBaseLineSeries();
                return _baseLineSeries;
            }
            set
            {
                if (_baseLineSeries != null)
                {
                    _baseLineSeries.PropertyChanged -= HandleNestedSettingsChanged;
                }

                _baseLineSeries = value ?? CreateDefaultBaseLineSeries();
                _baseLineSeries.PropertyChanged -= HandleNestedSettingsChanged;
                _baseLineSeries.PropertyChanged += HandleNestedSettingsChanged;

                OnPropertyChanged();
            }
        }

        public AkodeTrendsIndicator()
        {
            InitializeProfiles();
            InitializeTrendStyles();
            EnsureBaseLineSeries();
            EnsureVisibleHorizontalLevels();
        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            InitializeProfiles();
            InitializeTrendStyles();
            EnsureBaseLineSeries();
            EnsureVisibleHorizontalLevels();
        }

        public override void ApplyColors(IChartTheme theme)
        {
            InitializeProfiles();
            InitializeTrendStyles();
            EnsureBaseLineSeries();

            Profile1.ApplyDisplayDefaults(1);
            Profile2.ApplyDisplayDefaults(2);
            Profile3.ApplyDisplayDefaults(3);
            Profile4.ApplyDisplayDefaults(4);
            Profile5.ApplyDisplayDefaults(5);
            HighTrendSeries.CopyTheme(CreateDefaultTrendSeries(true));
            LowTrendSeries.CopyTheme(CreateDefaultTrendSeries(false));
            BaseLineSeries.CopyTheme(CreateDefaultBaseLineSeries());

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
            ShowDistancePercentLabels = source.ShowDistancePercentLabels;

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
            TrendlineMemoryBars = source.TrendlineMemoryBars;
            TrendlineMemoryMinutes = source.TrendlineMemoryMinutes;
            TrendlineMergeTicks = source.TrendlineMergeTicks;

            ShowBaseLine = source.ShowBaseLine;
            BaseLineToleranceTicks = source.BaseLineToleranceTicks;
            BaseLineLookbackBars = source.BaseLineLookbackBars;
            BaseLineLookbackMinutes = source.BaseLineLookbackMinutes;

            HighTrendSeries.CopyTheme(source.HighTrendSeries);
            LowTrendSeries.CopyTheme(source.LowTrendSeries);
            BaseLineSeries.CopyTheme(source.BaseLineSeries);

            base.CopyTemplate(indicator, style);
        }

        protected override void Execute()
        {
            var dataLength = Helper.Count;
            EnsureVisibleHorizontalLevels();
            _visibleHorizontalLevels.Clear();

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

            if (_showBaseLine && priceStep > 0.0)
            {
                var rawPivots = CollectAllRawPivots();
                var baseLineTolerance = Math.Max(1, _baseLineToleranceTicks) * priceStep;
                var lookbackStart = ComputeLookbackStart(dataLength);
                var baseLinePrice = FindBaseLinePrice(rawPivots, baseLineTolerance, dataLength, lookbackStart);

                if (!double.IsNaN(baseLinePrice))
                {
                    EnsureBaseLineSeries();
                    var data = CreateSeriesData(dataLength, 0, delegate { return baseLinePrice; });
                    Series.Add(new IndicatorSeriesData(data, CloneLine(_baseLineSeries, _baseLineSeries.Style))
                    {
                        Style = { DisableMinMax = true }
                    });
                }
            }

            if (!ShowTrendLines)
            {
                return;
            }

            var selection = GetOrComputeTrendSelection(
                highLevels, lowLevels, filteredHigh, filteredLow, dataLength, priceStep);

            if (_trendlineMergeTicks > 0 && priceStep > 0.0)
            {
                MergeSimilarTrendlines(selection.HighTrendLines, priceStep, dataLength);
                MergeSimilarTrendlines(selection.LowTrendLines, priceStep, dataLength);
            }

            DrawTrendLines(
                selection.HighTrendLines,
                HighTrendSeries,
                dataLength);

            DrawTrendLines(
                selection.LowTrendLines,
                LowTrendSeries,
                dataLength);
        }

        public override void GetLabels(ref List<IndicatorLabelInfo> labels)
        {
            EnsureVisibleHorizontalLevels();

            if (!ShowIndicatorLabels || _visibleHorizontalLevels.Count == 0)
            {
                return;
            }

            for (int i = 0; i < _visibleHorizontalLevels.Count; i++)
            {
                var level = _visibleHorizontalLevels[i];
                if (!level.ShowScaleLabel || !IsLevelInViewport(level.Price))
                {
                    continue;
                }

                labels.Add(new IndicatorLabelInfo(level.Price, level.Color));
            }
        }

        public override void Render(DxVisualQueue visual)
        {
            EnsureVisibleHorizontalLevels();

            if (!ShowDistancePercentLabels || _visibleHorizontalLevels.Count == 0 || Canvas == null)
            {
                return;
            }

            var currentPrice = GetCurrentReferencePrice();
            if (double.IsNaN(currentPrice) || double.IsInfinity(currentPrice) || Math.Abs(currentPrice) < double.Epsilon)
            {
                return;
            }

            DrawDistancePercentLabels(visual, currentPrice, true);
            DrawDistancePercentLabels(visual, currentPrice, false);
        }

        private TrendsCalculationEngine.TrendSelectionResult GetOrComputeTrendSelection(
            List<TrendsCalculationEngine.LevelLine> highLevels,
            List<TrendsCalculationEngine.LevelLine> lowLevels,
            List<TrendsCalculationEngine.LevelLine> filteredHigh,
            List<TrendsCalculationEngine.LevelLine> filteredLow,
            int dataLength,
            double priceStep)
        {
            var barMemoryActive = _trendlineMemoryBars > 0 &&
                (dataLength - _cachedAtDataLength) < _trendlineMemoryBars;
            var timeMemoryActive = _trendlineMemoryMinutes > 0 &&
                (DateTime.UtcNow - _cachedAtTime).TotalMinutes < _trendlineMemoryMinutes;
            var firstBarPrice = Helper.Open[0];
            var cacheValid = _cachedTrendSelection != null &&
                _cachedAtDataLength <= dataLength &&
                _cachedFirstBarPrice == firstBarPrice &&
                (barMemoryActive || timeMemoryActive);

            if (cacheValid)
            {
                return _cachedTrendSelection;
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

            if (_trendlineMemoryBars > 0 || _trendlineMemoryMinutes > 0)
            {
                _cachedTrendSelection = selection;
                _cachedAtDataLength = dataLength;
                _cachedAtTime = DateTime.UtcNow;
                _cachedFirstBarPrice = firstBarPrice;
            }

            return selection;
        }

        private void MergeSimilarTrendlines(
            List<TrendsCalculationEngine.TrendLineCandidate> lines,
            double priceStep,
            int dataLength)
        {
            var mergeDistance = _trendlineMergeTicks * priceStep;
            var lastIndex = dataLength - 1;

            for (int i = 0; i < lines.Count; i++)
            {
                var priceIEnd = lines[i].GetValue(lastIndex);

                for (int j = lines.Count - 1; j > i; j--)
                {
                    var overlapStart = Math.Max(lines[i].StartIndex, lines[j].StartIndex);
                    var diffAtStart = Math.Abs(lines[i].GetValue(overlapStart) - lines[j].GetValue(overlapStart));
                    var diffAtEnd = Math.Abs(priceIEnd - lines[j].GetValue(lastIndex));
                    var maxDiff = Math.Max(diffAtStart, diffAtEnd);

                    if (maxDiff <= mergeDistance)
                    {
                        lines.RemoveAt(j);
                    }
                }
            }
        }

        private void InitializeProfiles()
        {
            InitializeProfile(ref _profile1, 1, true);
            InitializeProfile(ref _profile2, 2, true);
            InitializeProfile(ref _profile3, 3, true);
            InitializeProfile(ref _profile4, 4, true);
            InitializeProfile(ref _profile5, 5, false);
        }

        private void InitializeProfile(
            ref AkodeTrendsProfileSettings profile,
            int profileIndex,
            bool enabledByDefault)
        {
            if (profile == null)
            {
                profile = CreateDefaultProfile(profileIndex, enabledByDefault);
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

            field = value ?? CreateDefaultProfile(profileIndex, enabledByDefault);

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
                    ? XColor.FromArgb(100, 0, 100, 0)
                    : XColor.FromArgb(100, 148, 0, 211)
            };
        }

        private static ChartLine CreateDefaultBaseLineSeries()
        {
            return new ChartLine
            {
                Style = XDashStyle.Solid,
                Width = 3,
                Color = XColor.FromArgb(94, 139, 69, 19)
            };
        }

        private void EnsureBaseLineSeries()
        {
            if (_baseLineSeries == null)
            {
                _baseLineSeries = CreateDefaultBaseLineSeries();
            }

            _baseLineSeries.PropertyChanged -= HandleNestedSettingsChanged;
            _baseLineSeries.PropertyChanged += HandleNestedSettingsChanged;
        }

        private void EnsureVisibleHorizontalLevels()
        {
            if (_visibleHorizontalLevels == null)
            {
                _visibleHorizontalLevels = new List<VisibleHorizontalLevel>();
            }
        }

        private void HandleNestedSettingsChanged(object sender, PropertyChangedEventArgs e)
        {
            OnPropertyChanged(string.Empty);
        }


        private List<TrendsCalculationEngine.LevelLine> CollectAllRawPivots()
        {
            var all = new List<TrendsCalculationEngine.LevelLine>();
            CollectRawPivotsFromProfile(Profile1, 1, all);
            CollectRawPivotsFromProfile(Profile2, 2, all);
            CollectRawPivotsFromProfile(Profile3, 3, all);
            CollectRawPivotsFromProfile(Profile4, 4, all);
            CollectRawPivotsFromProfile(Profile5, 5, all);
            return all;
        }

        private void CollectRawPivotsFromProfile(
            AkodeTrendsProfileSettings profile,
            int profileIndex,
            List<TrendsCalculationEngine.LevelLine> target)
        {
            if (profile == null || !profile.Enabled)
            {
                return;
            }

            target.AddRange(TrendsCalculationEngine.CalculateAllRawPivots(
                Helper, DataProvider, profile, profileIndex));
        }

        private int ComputeLookbackStart(int dataLength)
        {
            var startBar = 0;

            if (_baseLineLookbackBars > 0)
            {
                startBar = Math.Max(startBar, dataLength - _baseLineLookbackBars);
            }

            if (_baseLineLookbackMinutes > 0 && dataLength > 0)
            {
                var date = Helper.Date;
                var cutoff = DateTime.FromOADate(date[dataLength - 1]).AddMinutes(-_baseLineLookbackMinutes);

                for (int i = dataLength - 1; i >= 0; i--)
                {
                    if (DateTime.FromOADate(date[i]) < cutoff)
                    {
                        startBar = Math.Max(startBar, i + 1);
                        break;
                    }
                }
            }

            return Math.Min(startBar, dataLength - 1);
        }

        private double FindBaseLinePrice(
            List<TrendsCalculationEngine.LevelLine> pivots,
            double tolerance,
            int dataLength,
            int lookbackStart)
        {
            if (pivots.Count == 0 || dataLength == 0)
            {
                return double.NaN;
            }

            if (lookbackStart > 0)
            {
                pivots.RemoveAll(p => p.StartIndex < lookbackStart);
            }

            if (pivots.Count == 0)
            {
                return double.NaN;
            }

            pivots.Sort((a, b) => a.Price.CompareTo(b.Price));

            var bestScore = -1;
            var bestPrice = double.NaN;
            var high = Helper.High;
            var low = Helper.Low;
            var close = Helper.Close;
            var scanStart = Math.Max(0, lookbackStart);

            var groupStart = 0;

            while (groupStart < pivots.Count)
            {
                var anchor = pivots[groupStart].Price;
                var groupEnd = groupStart + 1;

                while (groupEnd < pivots.Count && pivots[groupEnd].Price - anchor <= tolerance)
                {
                    groupEnd++;
                }

                var pivotCount = groupEnd - groupStart;

                var priceSum = 0.0;
                for (int i = groupStart; i < groupEnd; i++)
                {
                    priceSum += pivots[i].Price;
                }

                var groupPrice = priceSum / pivotCount;

                var bounceCount = 0;
                for (int bar = scanStart; bar < dataLength - 1; bar++)
                {
                    var touchesHigh = Math.Abs(high[bar] - groupPrice) <= tolerance;
                    var touchesLow = Math.Abs(low[bar] - groupPrice) <= tolerance;

                    if (touchesHigh || touchesLow)
                    {
                        var nextClose = close[bar + 1];
                        var movedAway = Math.Abs(nextClose - groupPrice) > tolerance;

                        if (movedAway)
                        {
                            bounceCount++;
                        }
                    }
                }

                var score = pivotCount + bounceCount;

                if (score > bestScore)
                {
                    bestScore = score;
                    bestPrice = groupPrice;
                }

                groupStart = groupEnd;
            }

            return bestPrice;
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
            var seriesLength = GetHorizontalSeriesLength(dataLength);

            foreach (var level in levels)
            {
                var style = GetProfileStyle(level.ProfileIndex, isHigh);
                if (style == null)
                {
                    continue;
                }

                var data = CreateSeriesData(seriesLength, level.StartIndex, delegate { return level.Price; });
                var lineStyle = CloneLine(style, level.IsBroken ? XDashStyle.Dot : style.Style);

                if (lineStyle.Visible)
                {
                    _visibleHorizontalLevels.Add(new VisibleHorizontalLevel
                    {
                        Price = level.Price,
                        StartIndex = level.StartIndex,
                        IsHigh = isHigh,
                        Color = lineStyle.Color,
                        ShowScaleLabel = true
                    });
                }

                Series.Add(new IndicatorSeriesData(data, lineStyle)
                {
                    Style =
                    {
                        DisableMinMax = true
                    }
                });
            }
        }

        private int GetHorizontalSeriesLength(int dataLength)
        {
            var afterBars = Canvas != null ? Math.Max(0, Canvas.AfterBars) : 0;
            return dataLength + afterBars;
        }

        private void DrawDistancePercentLabels(
            DxVisualQueue visual,
            double currentPrice,
            bool highSide)
        {
            var candidates = new List<DistanceLabelCandidate>();

            for (int i = 0; i < _visibleHorizontalLevels.Count; i++)
            {
                var level = _visibleHorizontalLevels[i];
                if (level.IsHigh != highSide || !IsLevelInViewport(level.Price))
                {
                    continue;
                }

                var distance = level.Price - currentPrice;
                if (highSide)
                {
                    if (distance <= 0.0)
                    {
                        continue;
                    }
                }
                else if (distance >= 0.0)
                {
                    continue;
                }

                candidates.Add(new DistanceLabelCandidate
                {
                    Level = level,
                    Distance = Math.Abs(distance)
                });
            }

            if (candidates.Count == 0)
            {
                return;
            }

            candidates.Sort(delegate (DistanceLabelCandidate a, DistanceLabelCandidate b)
            {
                var cmp = a.Distance.CompareTo(b.Distance);
                return cmp != 0 ? cmp : b.Level.StartIndex.CompareTo(a.Level.StartIndex);
            });

            var font = Canvas.ChartFont;
            var acceptedRects = new List<Rect>(MaxDistancePercentLabelsPerSide);
            var chartRect = Canvas.Rect;
            var acceptedCount = 0;

            for (int i = 0; i < candidates.Count && acceptedCount < MaxDistancePercentLabelsPerSide; i++)
            {
                var text = FormatDistancePercent(candidates[i].Level.Price, currentPrice);
                if (string.IsNullOrEmpty(text))
                {
                    continue;
                }

                var size = font.GetSize(text);
                var lineY = GetY(candidates[i].Level.Price);
                var x = chartRect.Right - size.Width - DistancePercentLabelPadding;
                var y = lineY - size.Height - DistancePercentLabelLineGap;
                var minY = chartRect.Top;
                var maxY = chartRect.Bottom - size.Height;

                if (x < chartRect.Left + DistancePercentLabelPadding)
                {
                    x = chartRect.Left + DistancePercentLabelPadding;
                }

                if (y < minY)
                {
                    y = lineY + DistancePercentLabelLineGap;
                }

                if (y > maxY)
                {
                    y = maxY;
                }

                if (y < minY)
                {
                    y = minY;
                }

                var labelRect = new Rect(x, y, size.Width, size.Height);
                var overlaps = false;

                for (int j = 0; j < acceptedRects.Count; j++)
                {
                    var acceptedRect = acceptedRects[j];
                    if (labelRect.Bottom > acceptedRect.Top - DistancePercentLabelSpacing &&
                        labelRect.Top < acceptedRect.Bottom + DistancePercentLabelSpacing)
                    {
                        overlaps = true;
                        break;
                    }
                }

                if (overlaps)
                {
                    continue;
                }

                acceptedRects.Add(labelRect);
                visual.DrawString(
                    text,
                    font,
                    new XBrush(candidates[i].Level.Color),
                    labelRect);
                acceptedCount++;
            }
        }

        private double GetCurrentReferencePrice()
        {
            if (DataProvider != null)
            {
                var security = DataProvider.GetSecurity();
                if (security != null)
                {
                    var lastPrice = (double)security.LastPrice;
                    if (lastPrice > 0.0)
                    {
                        return lastPrice;
                    }
                }
            }

            if (Helper != null && Helper.Count > 0)
            {
                var closePrice = Helper.Close[Helper.Count - 1];
                if (closePrice > 0.0)
                {
                    return closePrice;
                }
            }

            return double.NaN;
        }

        private bool IsLevelInViewport(double price)
        {
            if (Canvas == null)
            {
                return false;
            }

            var y = GetY(price);
            var rect = Canvas.Rect;
            return y >= rect.Top && y <= rect.Bottom;
        }

        private static string FormatDistancePercent(double levelPrice, double currentPrice)
        {
            var percent = ((levelPrice - currentPrice) / currentPrice) * 100.0;
            if (double.IsNaN(percent) || double.IsInfinity(percent))
            {
                return null;
            }

            return percent.ToString("+0.00;-0.00;0.00") + "%";
        }

        private static AkodeTrendlineAlgorithm NormalizeTrendlineAlgorithm(AkodeTrendlineAlgorithm value)
        {
            return value == AkodeTrendlineAlgorithm.WeightedRegression
                ? AkodeTrendlineAlgorithm.WeightedRegression
                : value == AkodeTrendlineAlgorithm.Ransac
                    ? AkodeTrendlineAlgorithm.Ransac
                    : value == AkodeTrendlineAlgorithm.HoughTransform
                        ? AkodeTrendlineAlgorithm.HoughTransform
                        : AkodeTrendlineAlgorithm.ClassicTouches;
        }

        private static AkodeTrendsProfileSettings CreateDefaultProfile(
            int profileIndex,
            bool enabledByDefault)
        {
            var profile = new AkodeTrendsProfileSettings
            {
                Enabled = enabledByDefault,
                IncludeInTrendlines = profileIndex != 5,
                CandlesBefore = 2,
                CandlesAfter = 2,
                MaxLinesHigh = 5,
                MaxLinesLow = 5,
                UseCandleBodyInsteadOfWicks = false,
                MaxBrokenLinesHigh = 2,
                MaxBrokenLinesLow = 2,
                ShowBrokenLines = false
            };

            switch (profileIndex)
            {
                case 1:
                    profile.PeriodType = AkodeLevelsPeriodType.Hour;
                    profile.PeriodValue = 4;
                    break;
                case 2:
                    profile.PeriodType = AkodeLevelsPeriodType.Hour;
                    profile.PeriodValue = 1;
                    break;
                case 3:
                    profile.PeriodType = AkodeLevelsPeriodType.Minute;
                    profile.PeriodValue = 15;
                    break;
                case 4:
                    profile.PeriodType = AkodeLevelsPeriodType.Minute;
                    profile.PeriodValue = 1;
                    break;
                default:
                    profile.PeriodType = AkodeLevelsPeriodType.AnyTimeFrame;
                    profile.PeriodValue = 1;
                    break;
            }

            profile.EnsureInitialized(profileIndex);
            return profile;
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
