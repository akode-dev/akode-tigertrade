using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;
using System.Windows;
using TigerTrade.Chart.Base;
using TigerTrade.Chart.Base.Enums;
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

        private struct ConfirmedLevelPoint
        {
            public double Price;
            public int Point1Index;
            public int Point2Index;
            public bool IsHigh;
            public XColor Color;
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
        private bool _highlightRoundLevels;
        private double _roundLevelStep;
        private int _roundLevelToleranceTicks;
        private ChartLine _roundHighSeries;
        private ChartLine _roundLowSeries;
        private ChartLine _testedHighSeries;
        private ChartLine _testedLowSeries;
        private List<VisibleHorizontalLevel> _visibleHorizontalLevels;
        private bool _showConfirmationDots;
        private double _confirmationTolerancePercent = 0.5;
        private int _confirmationMinBars = 10;
        private double _confirmationDotSize = 6.0;
        private XColor _confirmationDotColor = XColor.FromArgb(255, 0, 191, 255);
        private List<ConfirmedLevelPoint> _confirmedLevelPoints;
        private int _confirmationTimeframeMinutes;
        private int _confirmationMatureBars = 5;
        private XColor _confirmationMatureColor = XColor.FromArgb(255, 0, 200, 0);
        private int _distancePercentDecimals = 1;
        private int _distanceLabelFontSize = 9;
        private bool _distanceLabelBold;
        private int _gapLabelFontSize = 9;
        private bool _gapLabelBold;

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

        [DataMember(Name = "DistancePercentDecimals")]
        [Category("Level lines"), DisplayName("Distance % decimals")]
        public int DistancePercentDecimals
        {
            get { return _distancePercentDecimals; }
            set
            {
                value = Math.Max(1, Math.Min(4, value));

                if (value == _distancePercentDecimals)
                {
                    return;
                }

                _distancePercentDecimals = value;
                OnPropertyChanged();
            }
        }

        [DataMember(Name = "DistanceLabelFontSize")]
        [Category("Level lines"), DisplayName("Distance label font size")]
        public int DistanceLabelFontSize
        {
            get { return _distanceLabelFontSize; }
            set
            {
                value = Math.Max(6, Math.Min(30, value));

                if (value == _distanceLabelFontSize)
                {
                    return;
                }

                _distanceLabelFontSize = value;
                OnPropertyChanged();
            }
        }

        [DataMember(Name = "DistanceLabelBold")]
        [Category("Level lines"), DisplayName("Distance label bold")]
        public bool DistanceLabelBold
        {
            get { return _distanceLabelBold; }
            set
            {
                if (value == _distanceLabelBold)
                {
                    return;
                }

                _distanceLabelBold = value;
                OnPropertyChanged();
            }
        }

        [DataMember(Name = "GapLabelFontSize")]
        [Category("Level lines"), DisplayName("Gap label font size")]
        public int GapLabelFontSize
        {
            get { return _gapLabelFontSize; }
            set
            {
                value = Math.Max(6, Math.Min(30, value));

                if (value == _gapLabelFontSize)
                {
                    return;
                }

                _gapLabelFontSize = value;
                OnPropertyChanged();
            }
        }

        [DataMember(Name = "GapLabelBold")]
        [Category("Level lines"), DisplayName("Gap label bold")]
        public bool GapLabelBold
        {
            get { return _gapLabelBold; }
            set
            {
                if (value == _gapLabelBold)
                {
                    return;
                }

                _gapLabelBold = value;
                OnPropertyChanged();
            }
        }

        [DataMember(Name = "HighlightRoundLevels")]
        [Category("Round levels"), DisplayName("Highlight round levels")]
        public bool HighlightRoundLevels
        {
            get { return _highlightRoundLevels; }
            set
            {
                if (value == _highlightRoundLevels)
                {
                    return;
                }

                _highlightRoundLevels = value;
                OnPropertyChanged();
            }
        }

        [DataMember(Name = "RoundLevelStep")]
        [Category("Round levels"), DisplayName("Round step")]
        public double RoundLevelStep
        {
            get { return _roundLevelStep; }
            set
            {
                value = Math.Max(0.0, value);

                if (value == _roundLevelStep)
                {
                    return;
                }

                _roundLevelStep = value;
                OnPropertyChanged();
            }
        }

        [DataMember(Name = "RoundLevelToleranceTicks")]
        [Category("Round levels"), DisplayName("Round tolerance (ticks)")]
        public int RoundLevelToleranceTicks
        {
            get { return _roundLevelToleranceTicks; }
            set
            {
                value = Math.Max(0, value);

                if (value == _roundLevelToleranceTicks)
                {
                    return;
                }

                _roundLevelToleranceTicks = value;
                OnPropertyChanged();
            }
        }

        [DataMember(Name = "RoundHighSeries")]
        [Category("Round levels"), DisplayName("Round High levels")]
        public ChartLine RoundHighSeries
        {
            get
            {
                EnsureRoundLevelStyles();
                return _roundHighSeries;
            }
            set
            {
                if (_roundHighSeries != null)
                {
                    _roundHighSeries.PropertyChanged -= HandleNestedSettingsChanged;
                }

                _roundHighSeries = value ?? CreateDefaultRoundSeries(true);
                _roundHighSeries.PropertyChanged -= HandleNestedSettingsChanged;
                _roundHighSeries.PropertyChanged += HandleNestedSettingsChanged;

                OnPropertyChanged();
            }
        }

        [DataMember(Name = "RoundLowSeries")]
        [Category("Round levels"), DisplayName("Round Low levels")]
        public ChartLine RoundLowSeries
        {
            get
            {
                EnsureRoundLevelStyles();
                return _roundLowSeries;
            }
            set
            {
                if (_roundLowSeries != null)
                {
                    _roundLowSeries.PropertyChanged -= HandleNestedSettingsChanged;
                }

                _roundLowSeries = value ?? CreateDefaultRoundSeries(false);
                _roundLowSeries.PropertyChanged -= HandleNestedSettingsChanged;
                _roundLowSeries.PropertyChanged += HandleNestedSettingsChanged;

                OnPropertyChanged();
            }
        }

        [DataMember(Name = "TestedHighSeries")]
        [Category("Tested display"), DisplayName("Tested High levels")]
        public ChartLine TestedHighSeries
        {
            get
            {
                EnsureTestedStyles();
                return _testedHighSeries;
            }
            set
            {
                if (_testedHighSeries != null)
                {
                    _testedHighSeries.PropertyChanged -= HandleNestedSettingsChanged;
                }

                _testedHighSeries = value ?? CreateDefaultTestedSeries(true);
                _testedHighSeries.PropertyChanged -= HandleNestedSettingsChanged;
                _testedHighSeries.PropertyChanged += HandleNestedSettingsChanged;

                OnPropertyChanged();
            }
        }

        [DataMember(Name = "TestedLowSeries")]
        [Category("Tested display"), DisplayName("Tested Low levels")]
        public ChartLine TestedLowSeries
        {
            get
            {
                EnsureTestedStyles();
                return _testedLowSeries;
            }
            set
            {
                if (_testedLowSeries != null)
                {
                    _testedLowSeries.PropertyChanged -= HandleNestedSettingsChanged;
                }

                _testedLowSeries = value ?? CreateDefaultTestedSeries(false);
                _testedLowSeries.PropertyChanged -= HandleNestedSettingsChanged;
                _testedLowSeries.PropertyChanged += HandleNestedSettingsChanged;

                OnPropertyChanged();
            }
        }

        [DataMember(Name = "ShowConfirmationDots")]
        [Category("Confirmation dots"), DisplayName("Show confirmation dots")]
        public bool ShowConfirmationDots
        {
            get { return _showConfirmationDots; }
            set
            {
                if (value == _showConfirmationDots)
                {
                    return;
                }

                _showConfirmationDots = value;
                OnPropertyChanged();
            }
        }

        [DataMember(Name = "ConfirmationTolerancePercent")]
        [Category("Confirmation dots"), DisplayName("Tolerance (%)")]
        public double ConfirmationTolerancePercent
        {
            get { return _confirmationTolerancePercent; }
            set
            {
                value = Math.Max(0.01, value);

                if (Math.Abs(value - _confirmationTolerancePercent) < double.Epsilon)
                {
                    return;
                }

                _confirmationTolerancePercent = value;
                OnPropertyChanged();
            }
        }

        [DataMember(Name = "ConfirmationMinBars")]
        [Category("Confirmation dots"), DisplayName("Min bars between touches")]
        public int ConfirmationMinBars
        {
            get { return _confirmationMinBars; }
            set
            {
                value = Math.Max(1, value);

                if (value == _confirmationMinBars)
                {
                    return;
                }

                _confirmationMinBars = value;
                OnPropertyChanged();
            }
        }

        [DataMember(Name = "ConfirmationTimeframeMinutes")]
        [Category("Confirmation dots"), DisplayName("Timeframe (minutes, 0=chart)")]
        public int ConfirmationTimeframeMinutes
        {
            get { return _confirmationTimeframeMinutes; }
            set
            {
                value = Math.Max(0, value);

                if (value == _confirmationTimeframeMinutes)
                {
                    return;
                }

                _confirmationTimeframeMinutes = value;
                OnPropertyChanged();
            }
        }

        [DataMember(Name = "ConfirmationDotSize")]
        [Category("Confirmation dots"), DisplayName("Dot size")]
        public double ConfirmationDotSize
        {
            get { return _confirmationDotSize; }
            set
            {
                value = Math.Max(1.0, Math.Min(20.0, value));

                if (Math.Abs(value - _confirmationDotSize) < double.Epsilon)
                {
                    return;
                }

                _confirmationDotSize = value;
                OnPropertyChanged();
            }
        }

        [DataMember(Name = "ConfirmationDotColor")]
        [Category("Confirmation dots"), DisplayName("Dot color")]
        public XColor ConfirmationDotColor
        {
            get { return _confirmationDotColor; }
            set
            {
                if (value == _confirmationDotColor)
                {
                    return;
                }

                _confirmationDotColor = value;
                OnPropertyChanged();
            }
        }

        [DataMember(Name = "ConfirmationMatureBars")]
        [Category("Confirmation dots"), DisplayName("Mature after bars")]
        public int ConfirmationMatureBars
        {
            get { return _confirmationMatureBars; }
            set
            {
                value = Math.Max(1, Math.Min(1000, value));

                if (value == _confirmationMatureBars)
                {
                    return;
                }

                _confirmationMatureBars = value;
                OnPropertyChanged();
            }
        }

        [DataMember(Name = "ConfirmationMatureColor")]
        [Category("Confirmation dots"), DisplayName("Mature dot color")]
        public XColor ConfirmationMatureColor
        {
            get { return _confirmationMatureColor; }
            set
            {
                if (value == _confirmationMatureColor)
                {
                    return;
                }

                _confirmationMatureColor = value;
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

        public AkodeTrendsIndicator()
        {
            InitializeProfiles();
            InitializeTrendStyles();
            EnsureRoundLevelStyles();
            EnsureTestedStyles();
            EnsureVisibleHorizontalLevels();
            EnsureConfirmedLevelPoints();
        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            InitializeProfiles();
            InitializeTrendStyles();
            EnsureRoundLevelStyles();
            EnsureTestedStyles();
            EnsureVisibleHorizontalLevels();
            EnsureConfirmedLevelPoints();
        }

        public override void ApplyColors(IChartTheme theme)
        {
            InitializeProfiles();
            InitializeTrendStyles();

            Profile1.ApplyDisplayDefaults(1);
            Profile2.ApplyDisplayDefaults(2);
            Profile3.ApplyDisplayDefaults(3);
            Profile4.ApplyDisplayDefaults(4);
            Profile5.ApplyDisplayDefaults(5);
            HighTrendSeries.CopyTheme(CreateDefaultTrendSeries(true));
            LowTrendSeries.CopyTheme(CreateDefaultTrendSeries(false));
            RoundHighSeries.CopyTheme(CreateDefaultRoundSeries(true));
            RoundLowSeries.CopyTheme(CreateDefaultRoundSeries(false));
            TestedHighSeries.CopyTheme(CreateDefaultTestedSeries(true));
            TestedLowSeries.CopyTheme(CreateDefaultTestedSeries(false));
            _confirmationDotColor = XColor.FromArgb(255, 0, 191, 255);
            _confirmationMatureColor = XColor.FromArgb(255, 0, 200, 0);

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
            DistancePercentDecimals = source.DistancePercentDecimals;
            DistanceLabelFontSize = source.DistanceLabelFontSize;
            DistanceLabelBold = source.DistanceLabelBold;
            GapLabelFontSize = source.GapLabelFontSize;
            GapLabelBold = source.GapLabelBold;

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

            HighlightRoundLevels = source.HighlightRoundLevels;
            RoundLevelStep = source.RoundLevelStep;
            RoundLevelToleranceTicks = source.RoundLevelToleranceTicks;

            HighTrendSeries.CopyTheme(source.HighTrendSeries);
            LowTrendSeries.CopyTheme(source.LowTrendSeries);
            RoundHighSeries.CopyTheme(source.RoundHighSeries);
            RoundLowSeries.CopyTheme(source.RoundLowSeries);
            TestedHighSeries.CopyTheme(source.TestedHighSeries);
            TestedLowSeries.CopyTheme(source.TestedLowSeries);

            ShowConfirmationDots = source.ShowConfirmationDots;
            ConfirmationTolerancePercent = source.ConfirmationTolerancePercent;
            ConfirmationMinBars = source.ConfirmationMinBars;
            ConfirmationTimeframeMinutes = source.ConfirmationTimeframeMinutes;
            ConfirmationDotSize = source.ConfirmationDotSize;
            ConfirmationDotColor = source.ConfirmationDotColor;
            ConfirmationMatureBars = source.ConfirmationMatureBars;
            ConfirmationMatureColor = source.ConfirmationMatureColor;

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

            DrawFilteredHorizontalLines(filteredHigh, true, dataLength, priceStep);
            DrawFilteredHorizontalLines(filteredLow, false, dataLength, priceStep);

            if (_showConfirmationDots)
            {
                EnsureConfirmedLevelPoints();
                _confirmedLevelPoints.Clear();
                DetectConfirmedLevels(filteredHigh, true);
                DetectConfirmedLevels(filteredLow, false);
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
            if (Canvas == null)
            {
                return;
            }

            EnsureVisibleHorizontalLevels();

            if (ShowDistancePercentLabels && _visibleHorizontalLevels.Count > 0)
            {
                var currentPrice = GetCurrentReferencePrice();
                if (!double.IsNaN(currentPrice) && !double.IsInfinity(currentPrice)
                    && Math.Abs(currentPrice) > double.Epsilon)
                {
                    DrawDistancePercentLabels(visual, currentPrice, true);
                    DrawDistancePercentLabels(visual, currentPrice, false);
                }
            }

            if (_showConfirmationDots)
            {
                DrawConfirmedLevelDots(visual);
            }
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

        private void EnsureVisibleHorizontalLevels()
        {
            if (_visibleHorizontalLevels == null)
            {
                _visibleHorizontalLevels = new List<VisibleHorizontalLevel>();
            }
        }

        private void EnsureConfirmedLevelPoints()
        {
            if (_confirmedLevelPoints == null)
            {
                _confirmedLevelPoints = new List<ConfirmedLevelPoint>();
            }
        }

        private void DetectConfirmedLevels(
            List<TrendsCalculationEngine.LevelLine> levels, bool isHigh)
        {
            if (levels.Count == 0 || Helper.Count == 0)
            {
                return;
            }

            double[] high, low, close;
            int[] barToAgg = null;
            List<TrendsCalculationEngine.TimeFrameBar> aggBars = null;

            if (_confirmationTimeframeMinutes > 0 && DataProvider != null && DataProvider.Period != null)
            {
                var settings = new AkodeTrendsProfileSettings
                {
                    PeriodType = _confirmationTimeframeMinutes >= 60
                        ? AkodeLevelsPeriodType.Hour
                        : AkodeLevelsPeriodType.Minute,
                    PeriodValue = _confirmationTimeframeMinutes >= 60
                        ? _confirmationTimeframeMinutes / 60
                        : _confirmationTimeframeMinutes
                };

                aggBars = TrendsCalculationEngine.BuildOnTimeframe(
                    Helper.Date, Helper.High, Helper.Low, DataProvider, settings);

                if (aggBars.Count < 2)
                {
                    return;
                }

                // Build aggregated arrays
                high = new double[aggBars.Count];
                low = new double[aggBars.Count];
                close = new double[aggBars.Count];
                for (int i = 0; i < aggBars.Count; i++)
                {
                    high[i] = aggBars[i].High;
                    low[i] = aggBars[i].Low;
                    // Close = last candle's close in this timeframe bar
                    var lastOrigIdx = i + 1 < aggBars.Count
                        ? aggBars[i + 1].FirstIndex - 1
                        : Helper.Count - 1;
                    close[i] = Helper.Close[lastOrigIdx];
                }

                // Map original bar index → aggregated bar index
                barToAgg = new int[Helper.Count];
                for (int a = 0; a < aggBars.Count; a++)
                {
                    var end = a + 1 < aggBars.Count ? aggBars[a + 1].FirstIndex : Helper.Count;
                    for (int j = aggBars[a].FirstIndex; j < end; j++)
                    {
                        barToAgg[j] = a;
                    }
                }
            }
            else
            {
                high = Helper.High;
                low = Helper.Low;
                close = Helper.Close;
            }

            foreach (var level in levels)
            {
                if (level.IsBroken)
                {
                    continue;
                }

                var startIdx = barToAgg != null ? barToAgg[level.StartIndex] : level.StartIndex;

                var retestBar = TrendsCalculationEngine.FindClosestRetest(
                    startIdx, level.Price, isHigh,
                    _confirmationTolerancePercent, _confirmationMinBars,
                    high, low, close);

                if (retestBar < 0)
                {
                    continue;
                }

                // Map aggregated index back to original
                var originalRetest = aggBars != null
                    ? (isHigh ? aggBars[retestBar].HighIndex : aggBars[retestBar].LowIndex)
                    : retestBar;

                _confirmedLevelPoints.Add(new ConfirmedLevelPoint
                {
                    Price = level.Price,
                    Point1Index = level.StartIndex,
                    Point2Index = originalRetest,
                    IsHigh = isHigh,
                    Color = _confirmationDotColor
                });
            }
        }

        private void DrawConfirmedLevelDots(DxVisualQueue visual)
        {
            EnsureConfirmedLevelPoints();

            if (_confirmedLevelPoints.Count == 0)
            {
                return;
            }

            var chartRect = Canvas.Rect;
            var radius = _confirmationDotSize / 2.0;
            var numberFont = new XFont(Canvas.ChartFont.Name, _confirmationDotSize + 2.0);
            var numberOffset = radius + 2.0;

            for (int i = 0; i < _confirmedLevelPoints.Count; i++)
            {
                var cp = _confirmedLevelPoints[i];
                var y = GetY(cp.Price);

                if (y < chartRect.Top - radius - 20 || y > chartRect.Bottom + radius + 20)
                {
                    continue;
                }

                var brush = new XBrush(cp.Color);
                var isMature = (Helper.Count - 1 - cp.Point2Index) >= _confirmationMatureBars;
                var point2Brush = isMature ? new XBrush(_confirmationMatureColor) : brush;

                // Point 1
                var x1 = Canvas.GetX(cp.Point1Index);
                if (x1 >= chartRect.Left - radius && x1 <= chartRect.Right + radius)
                {
                    visual.FillEllipse(brush, new Point(x1, y), radius, radius);
                    var numSize1 = numberFont.GetSize("1");
                    var numY1 = cp.IsHigh ? y - numberOffset - numSize1.Height : y + numberOffset;
                    visual.DrawString("1", numberFont, brush,
                        new Rect(x1 - numSize1.Width / 2.0, numY1, numSize1.Width, numSize1.Height));
                }

                // Point 2
                var x2 = Canvas.GetX(cp.Point2Index);
                if (x2 >= chartRect.Left - radius && x2 <= chartRect.Right + radius)
                {
                    visual.FillEllipse(point2Brush, new Point(x2, y), radius, radius);
                    var numSize2 = numberFont.GetSize("2");
                    var numY2 = cp.IsHigh ? y - numberOffset - numSize2.Height : y + numberOffset;
                    visual.DrawString("2", numberFont, point2Brush,
                        new Rect(x2 - numSize2.Width / 2.0, numY2, numSize2.Width, numSize2.Height));
                }
            }
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
            int dataLength,
            double priceStep)
        {
            var seriesLength = GetHorizontalSeriesLength(dataLength);

            foreach (var level in levels)
            {
                var style = GetProfileStyle(level.ProfileIndex, isHigh);
                if (style == null)
                {
                    continue;
                }

                if (Helpers.RoundPriceHelper.IsRoundPrice(level.Price, priceStep, _highlightRoundLevels, _roundLevelStep, _roundLevelToleranceTicks))
                {
                    style = isHigh ? _roundHighSeries : _roundLowSeries;
                }
                else if (level.IsTested)
                {
                    style = isHigh ? _testedHighSeries : _testedLowSeries;
                }

                var data = CreateSeriesData(seriesLength, level.StartIndex, delegate { return level.Price; });
                var dashStyle = level.IsBroken ? XDashStyle.Dot : level.IsTested ? XDashStyle.Dash : style.Style;
                var lineStyle = CloneLine(style, dashStyle);

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
                        DisableMinMax = true,
                        DisableSelect = true
                    }
                });
            }
        }

        private int GetHorizontalSeriesLength(int dataLength)
        {
            var afterBars = Canvas != null ? Math.Max(0, Canvas.AfterBars) : 0;
            return dataLength + afterBars + 5000;
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

            var baseFontName = Canvas.ChartFont.Name;
            var font = new XFont(baseFontName, _distanceLabelFontSize, _distanceLabelBold);
            var gapFont = new XFont(baseFontName, _gapLabelFontSize, _gapLabelBold);
            var acceptedRects = new List<Rect>(MaxDistancePercentLabelsPerSide);
            var chartRect = Canvas.Rect;
            var acceptedCount = 0;

            for (int i = 0; i < candidates.Count && acceptedCount < MaxDistancePercentLabelsPerSide; i++)
            {
                var text = FormatDistancePercent(candidates[i].Level.Price, currentPrice, _distancePercentDecimals);
                if (string.IsNullOrEmpty(text))
                {
                    continue;
                }

                string gapText = null;
                if (i + 1 < candidates.Count)
                {
                    var gap = Math.Abs(candidates[i + 1].Level.Price - candidates[i].Level.Price);
                    var gapPercent = (gap / currentPrice) * 100.0;
                    if (!double.IsNaN(gapPercent) && !double.IsInfinity(gapPercent))
                    {
                        var triangle = highSide ? "\u25B2" : "\u25BC";
                        var fmt = "0." + new string('0', _distancePercentDecimals);
                        gapText = triangle + gapPercent.ToString(fmt) + "%";
                    }
                }

                var mainSize = font.GetSize(text);
                var gapSize = gapText != null ? gapFont.GetSize(gapText) : new Size(0, 0);
                var gapSpacing = gapText != null ? DistancePercentLabelPadding : 0.0;
                var totalWidth = mainSize.Width + gapSpacing + gapSize.Width;
                var totalHeight = Math.Max(mainSize.Height, gapSize.Height);
                var size = new Size(totalWidth, totalHeight);

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
                var brush = new XBrush(candidates[i].Level.Color);
                var mainRect = new Rect(x, y, mainSize.Width, mainSize.Height);
                visual.DrawString(text, font, brush, mainRect);

                if (gapText != null)
                {
                    var gapX = x + mainSize.Width + gapSpacing;
                    var gapY = y + (mainSize.Height - gapSize.Height);
                    var gapRect = new Rect(gapX, gapY, gapSize.Width, gapSize.Height);
                    visual.DrawString(gapText, gapFont, brush, gapRect);
                }

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

        private static string FormatDistancePercent(double levelPrice, double currentPrice, int decimals)
        {
            var percent = ((levelPrice - currentPrice) / currentPrice) * 100.0;
            if (double.IsNaN(percent) || double.IsInfinity(percent))
            {
                return null;
            }

            var fmt = "0." + new string('0', decimals);
            var posFmt = "+" + fmt;
            var negFmt = "-" + fmt;
            return percent.ToString(posFmt + ";" + negFmt + ";" + fmt) + "%";
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
                        StraightLine = true,
                        DisableSelect = true
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

        private static ChartLine CreateDefaultRoundSeries(bool isHigh)
        {
            return new ChartLine
            {
                Style = XDashStyle.Solid,
                Width = 2,
                Color = isHigh
                    ? XColor.FromArgb(200, 218, 165, 32)
                    : XColor.FromArgb(200, 218, 165, 32)
            };
        }

        private void EnsureRoundLevelStyles()
        {
            if (_roundHighSeries == null)
            {
                _roundHighSeries = CreateDefaultRoundSeries(true);
                _roundHighSeries.PropertyChanged += HandleNestedSettingsChanged;
            }

            if (_roundLowSeries == null)
            {
                _roundLowSeries = CreateDefaultRoundSeries(false);
                _roundLowSeries.PropertyChanged += HandleNestedSettingsChanged;
            }
        }

        private static ChartLine CreateDefaultTestedSeries(bool isHigh)
        {
            return new ChartLine
            {
                Style = XDashStyle.Dash,
                Width = 1,
                Color = isHigh
                    ? XColor.FromArgb(100, 8, 153, 129)
                    : XColor.FromArgb(100, 247, 82, 95)
            };
        }

        private void EnsureTestedStyles()
        {
            if (_testedHighSeries == null)
            {
                _testedHighSeries = CreateDefaultTestedSeries(true);
                _testedHighSeries.PropertyChanged += HandleNestedSettingsChanged;
            }

            if (_testedLowSeries == null)
            {
                _testedLowSeries = CreateDefaultTestedSeries(false);
                _testedLowSeries.PropertyChanged += HandleNestedSettingsChanged;
            }
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
