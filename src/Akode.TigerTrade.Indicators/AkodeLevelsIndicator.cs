using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Windows;
using TigerTrade.Chart.Base;
using TigerTrade.Chart.Base.Enums;
using TigerTrade.Chart.Indicators.Common;
using TigerTrade.Chart.Indicators.Drawings;
using TigerTrade.Chart.Indicators.Enums;
using TigerTrade.Core.UI.Converters;
using TigerTrade.Core.Utils.Time;
using TigerTrade.Dx;
using TigerTrade.Dx.Enums;

namespace Akode.TigerTrade.Indicators
{
    [DataContract(
        Name = "AkodeLevelsIndicator", 
        Namespace = "http://schemas.datacontract.org/2004/07/TigerTrade.Chart.Indicators.Custom"
    )]
    [Indicator("X_AkodeLevelsIndicator", "_Akode: Levels", true, Type = typeof(AkodeLevelsIndicator))]
    public sealed class AkodeLevelsIndicator : IndicatorBase
    {
        private AkodeLevelsPeriodType _periodType;

        [DataMember(Name = "PeriodType")]
        [Category("Period"), DisplayName("Interval")]
        public AkodeLevelsPeriodType PeriodType
        {
            get => _periodType;
            set
            {
                if (value == _periodType)
                {
                    return;
                }

                _periodType = value;

                if (_periodType == AkodeLevelsPeriodType.Minute)
                {
                    _periodValue = 5;
                }
                else
                {
                    _periodValue = 1;
                }

                OnPropertyChanged();
            }
        }

        private int _periodValue;

        [DataMember(Name = "PeriodValue")]
        [Category("Period"), DisplayName("Value")]
        public int PeriodValue
        {
            get => _periodValue;
            set
            {
                value = Math.Max(1, value);

                if (value == _periodValue)
                {
                    return;
                }

                _periodValue = value;
                OnPropertyChanged();
            }
        }

        [DataMember(Name = "CandlesBefore"), DefaultValue(2)]
        [Category("Settings"), DisplayName("Candles before")]
        public int CandlesBefore { get; set; } = 2;

        [DataMember(Name = "CandlesAfter"), DefaultValue(2)]
        [Category("Settings"), DisplayName("Candles after")]
        public int CandlesAfter { get; set; } = 2;

        [DataMember(Name = "MaxLinesHigh"), DefaultValue(15)]
        [Category("Settings"), DisplayName("Max High lines to show")]
        public int MaxLinesHigh { get; set; } = 15;

        [DataMember(Name = "MaxLinesLow"), DefaultValue(15)]
        [Category("Settings"), DisplayName("Max Low lines to show")]
        public int MaxLinesLow { get; set; } = 15;

        [DataMember(Name = "MaxBrokenLinesHigh"), DefaultValue(2)]
        [Category("Broken lines"), DisplayName("Max High lines")]
        public int MaxBrokenLinesHigh { get; set; } = 2;

        [DataMember(Name = "MaxBrokenLinesLow"), DefaultValue(2)]
        [Category("Broken lines"), DisplayName("Max Low lines")]
        public int MaxBrokenLinesLow { get; set; } = 2;

        [DataMember(Name = "ShowBrokenLines"), DefaultValue(true)]
        [Category("Broken lines"), DisplayName("Show broken lines")]
        public bool ShowBrokenLines { get; set; } = true;

        [DataMember(Name = "HighLineColor")]
        [Category("Display"), DisplayName("High levels")]
        public ChartLine HighSeries { get; set; }

        [DataMember(Name = "LowLineColor")]
        [Category("Display"), DisplayName("Low levels")]
        public ChartLine LowSeries { get; set; }

        [DataMember(Name = "HighlightRoundLevels"), DefaultValue(false)]
        [Category("Round levels"), DisplayName("Highlight round levels")]
        public bool HighlightRoundLevels { get; set; }

        [DataMember(Name = "RoundLevelStep"), DefaultValue(0.0)]
        [Category("Round levels"), DisplayName("Round step")]
        public double RoundLevelStep { get; set; }

        [DataMember(Name = "RoundLevelToleranceTicks"), DefaultValue(0)]
        [Category("Round levels"), DisplayName("Round tolerance (ticks)")]
        public int RoundLevelToleranceTicks { get; set; }

        [DataMember(Name = "RoundHighSeries")]
        [Category("Round levels"), DisplayName("Round High levels")]
        public ChartLine RoundHighSeries { get; set; }

        [DataMember(Name = "RoundLowSeries")]
        [Category("Round levels"), DisplayName("Round Low levels")]
        public ChartLine RoundLowSeries { get; set; }

        [DataMember(Name = "ShowDistancePercentLabels"), DefaultValue(false)]
        [Category("Display"), DisplayName("Show distance % labels")]
        public bool ShowDistancePercentLabels { get; set; }

        private struct LevelLine
        {
            public double Price;
            public int StartIndex;
            public bool IsBroken;
        }

        private struct VisibleLevel
        {
            public double Price;
            public bool IsHigh;
            public XColor Color;
        }

        private const int MaxDistanceLabelsPerSide = 10;
        private const double LabelPadding = 6.0;
        private const double LabelLineGap = 4.0;
        private const double LabelSpacing = 2.0;
        private List<VisibleLevel> _visibleLevels;

        private class TimeFrameBar
        {
            public double High;
            public double Low;
            public int HighIndex;
            public int LowIndex;
            public int FirstIndex;
        }

        [Browsable(false)]
        public override IndicatorCalculation Calculation => IndicatorCalculation.OnBarClose;

        public AkodeLevelsIndicator() { InitializeStyles(); }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            if (HighSeries == null || LowSeries == null) InitializeStyles();
            if (RoundHighSeries == null || RoundLowSeries == null) InitializeRoundStyles();
        }

        private void InitializeStyles()
        {
            HighSeries = new ChartLine
            {
                Style = XDashStyle.Solid,
                Width = 1,
                Color = XColor.FromArgb(100, 8, 153, 129)
            };

            LowSeries = new ChartLine
            {
                Style = XDashStyle.Solid,
                Width = 1,
                Color = XColor.FromArgb(100, 247, 82, 95)
            };

            InitializeRoundStyles();
        }

        private void InitializeRoundStyles()
        {
            RoundHighSeries = new ChartLine
            {
                Style = XDashStyle.Solid,
                Width = 2,
                Color = XColor.FromArgb(200, 218, 165, 32)
            };

            RoundLowSeries = new ChartLine
            {
                Style = XDashStyle.Solid,
                Width = 2,
                Color = XColor.FromArgb(200, 218, 165, 32)
            };
        }
        public override void ApplyColors(IChartTheme theme)
        {
            HighSeries.Color = theme.GetNextColor();
            LowSeries.Color = theme.GetNextColor();

            if (RoundHighSeries == null || RoundLowSeries == null) InitializeRoundStyles();
            RoundHighSeries.Color = XColor.FromArgb(200, 218, 165, 32);
            RoundLowSeries.Color = XColor.FromArgb(200, 218, 165, 32);

            base.ApplyColors(theme);
        }

        public override void CopyTemplate(IndicatorBase indicator, bool style)
        {
            var i = (AkodeLevelsIndicator)indicator;

            PeriodType = i.PeriodType;
            PeriodValue = i.PeriodValue;

            CandlesBefore = i.CandlesBefore;
            CandlesAfter = i.CandlesAfter;
            MaxLinesHigh = i.MaxLinesHigh;
            MaxLinesLow = i.MaxLinesLow;
            ShowBrokenLines = i.ShowBrokenLines;
            MaxBrokenLinesHigh = i.MaxBrokenLinesHigh;
            MaxBrokenLinesLow = i.MaxBrokenLinesLow;

            if (HighSeries == null || LowSeries == null)
            {
                InitializeStyles();
            }

            if (i.HighSeries != null)
            {
                HighSeries.CopyTheme(i.HighSeries);
            }

            if (i.LowSeries != null)
            {
                LowSeries.CopyTheme(i.LowSeries);
            }

            ShowDistancePercentLabels = i.ShowDistancePercentLabels;

            HighlightRoundLevels = i.HighlightRoundLevels;
            RoundLevelStep = i.RoundLevelStep;
            RoundLevelToleranceTicks = i.RoundLevelToleranceTicks;

            if (RoundHighSeries == null || RoundLowSeries == null) InitializeRoundStyles();
            if (i.RoundHighSeries != null) RoundHighSeries.CopyTheme(i.RoundHighSeries);
            if (i.RoundLowSeries != null) RoundLowSeries.CopyTheme(i.RoundLowSeries);

            base.CopyTemplate(indicator, style);
        }

        protected override void Execute()
        {
            if (_visibleLevels == null) _visibleLevels = new List<VisibleLevel>();
            _visibleLevels.Clear();

            var dataLength = Helper.Count;
            if (dataLength < CandlesBefore + CandlesAfter + 1) return;

            var date = Helper.Date;
            var high = Helper.High;
            var low = Helper.Low;

            var highPivots = new List<LevelLine>();
            var lowPivots = new List<LevelLine>();

            var currentPeriod = PeriodType;
            var currentPeriodValue = PeriodValue;

            if (currentPeriod == AkodeLevelsPeriodType.AnyTimeFrame)
            {
                highPivots = FindPivotsInCurrentData(high, true);
                lowPivots = FindPivotsInCurrentData(low, false);
            }
            else
            {
                var bars = BuildOnTimeframe(date, high, low);
                if (bars.Count >= CandlesBefore + CandlesAfter + 1)
                {
                    highPivots = FindPivotsInTimeFrameData(bars, high, true);
                    lowPivots = FindPivotsInTimeFrameData(bars, low, false);
                }
            }

            var orderedHighPivots = highPivots.OrderByDescending(p => p.StartIndex);
            var orderedLowPivots = lowPivots.OrderByDescending(p => p.StartIndex);

            var finalHighs = orderedHighPivots.Where(p => !p.IsBroken).Take(MaxLinesHigh).ToList();
            var finalLows = orderedLowPivots.Where(p => !p.IsBroken).Take(MaxLinesLow).ToList();

            if (ShowBrokenLines)
            {
                if (MaxBrokenLinesHigh > 0)
                {
                    finalHighs.AddRange(orderedHighPivots.Where(p => p.IsBroken).Take(MaxBrokenLinesHigh));
                }
                if (MaxBrokenLinesLow > 0)
                {
                    finalLows.AddRange(orderedLowPivots.Where(p => p.IsBroken).Take(MaxBrokenLinesLow));
                }
            }

            var finalHighsBuffer = ToCircularBuffer(finalHighs);
            var finalLowsBuffer = ToCircularBuffer(finalLows);

            DrawLines(finalHighsBuffer, HighSeries, true, dataLength);
            DrawLines(finalLowsBuffer, LowSeries, false, dataLength);
        }

        private List<TimeFrameBar> BuildOnTimeframe(double[] date, double[] high, double[] low)
        {
            var timeOffset = TimeHelper.GetSessionOffsetTs(DataProvider.Symbol.Exchange);
            var chartPeriodType = PeriodType == AkodeLevelsPeriodType.Minute ? ChartPeriodType.Minute :
                                  PeriodType == AkodeLevelsPeriodType.Hour ? ChartPeriodType.Hour :
                                  PeriodType == AkodeLevelsPeriodType.Week ? ChartPeriodType.Week :
                                  PeriodType == AkodeLevelsPeriodType.Month ? ChartPeriodType.Month :
                                  ChartPeriodType.Minute;

            var periodValue = PeriodValue > 0 ? PeriodValue : 1;

            var selectedBars = new Dictionary<int, TimeFrameBar>();
            for (int i = 0; i < date.Length; i++)
            {
                var sequence = DataProvider.Period.GetSequence(
                    chartPeriodType, periodValue, date[i], timeOffset.TotalHours);

                if (selectedBars.TryGetValue(sequence, out var bar))
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
            return selectedBars.Values.OrderBy(b => b.FirstIndex).ToList();
        }

        private List<LevelLine> FindPivotsInTimeFrameData(List<TimeFrameBar> bars, double[] originalPrices, bool isHigh)
        {
            var pivots = new List<LevelLine>();
            for (int i = CandlesBefore; i < bars.Count - CandlesAfter; i++)
            {
                bool isPivot = true;
                var centralBar = bars[i];
                double pivotPrice = isHigh ? centralBar.High : centralBar.Low;

                for (int j = i - CandlesBefore; j <= i + CandlesAfter; j++)
                {
                    if (i == j) continue;
                    double comparePrice = isHigh ? bars[j].High : bars[j].Low;
                    if (isHigh ? comparePrice > pivotPrice : comparePrice < pivotPrice)
                    {
                        isPivot = false;
                        break;
                    }
                }
                if (isPivot)
                {
                    int startIndex = isHigh ? centralBar.HighIndex : centralBar.LowIndex;
                    bool isBroken = IsBroken(startIndex, pivotPrice, originalPrices, isHigh);
                    pivots.Add(new LevelLine { Price = pivotPrice, StartIndex = startIndex, IsBroken = isBroken });
                }
            }
            return pivots;
        }

        private List<LevelLine> FindPivotsInCurrentData(double[] prices, bool isHigh)
        {
            var pivots = new List<LevelLine>();
            for (int i = CandlesBefore; i < prices.Length - CandlesAfter; i++)
            {
                bool isPivot = true;
                double price = prices[i];
                for (int j = i - CandlesBefore; j <= i + CandlesAfter; j++)
                {
                    if (i == j) continue;
                    if (isHigh ? prices[j] > price : prices[j] < price)
                    {
                        isPivot = false;
                        break;
                    }
                }
                if (isPivot)
                {
                    bool isBroken = IsBroken(i, price, prices, isHigh);
                    pivots.Add(new LevelLine { Price = price, StartIndex = i, IsBroken = isBroken });
                }
            }
            return pivots;
        }

        private bool IsBroken(int startIndex, double price, double[] prices, bool isHighLevel)
        {
            for (int k = startIndex + 1; k < prices.Length; k++)
            {
                if (isHighLevel ? prices[k] > price : prices[k] < price) return true;
            }
            return false;
        }

        private CircularBuffer<LevelLine> ToCircularBuffer(List<LevelLine> levels)
        {
            var buffer = new CircularBuffer<LevelLine>(levels.Count);

            foreach (var level in levels)
            {
                buffer.Push(level);
            }

            return buffer;
        }

        private void DrawLines(CircularBuffer<LevelLine> lines, ChartLine baseStyle, bool isHigh, int dataLength)
        {
            var priceStep = DataProvider != null ? DataProvider.Step : 0.0;

            foreach (var line in lines)
            {
                if (line.IsBroken && !ShowBrokenLines)
                    continue;

                var style = baseStyle;

                if (Helpers.RoundPriceHelper.IsRoundPrice(line.Price, priceStep, HighlightRoundLevels, RoundLevelStep, RoundLevelToleranceTicks))
                {
                    style = isHigh ? RoundHighSeries : RoundLowSeries;
                }

                var data = new double[dataLength];
                for (int i = 0; i < data.Length; i++) data[i] = double.NaN;
                for (int i = line.StartIndex; i < dataLength; i++) data[i] = line.Price;

                var lineStyle = new ChartLine
                {
                    Color = style.Color,
                    Width = style.Width,
                    Style = line.IsBroken ? XDashStyle.Dot : style.Style
                };

                if (lineStyle.Visible)
                {
                    _visibleLevels.Add(new VisibleLevel
                    {
                        Price = line.Price,
                        IsHigh = isHigh,
                        Color = lineStyle.Color
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

        public override void Render(DxVisualQueue visual)
        {
            if (_visibleLevels == null) _visibleLevels = new List<VisibleLevel>();

            if (!ShowDistancePercentLabels || _visibleLevels.Count == 0 || Canvas == null)
            {
                return;
            }

            var currentPrice = GetCurrentReferencePrice();
            if (double.IsNaN(currentPrice) || double.IsInfinity(currentPrice) || Math.Abs(currentPrice) < double.Epsilon)
            {
                return;
            }

            DrawDistanceLabels(visual, currentPrice, true);
            DrawDistanceLabels(visual, currentPrice, false);
        }

        private void DrawDistanceLabels(DxVisualQueue visual, double currentPrice, bool highSide)
        {
            var candidates = new List<KeyValuePair<VisibleLevel, double>>();

            for (int i = 0; i < _visibleLevels.Count; i++)
            {
                var level = _visibleLevels[i];
                if (level.IsHigh != highSide || !IsLevelInViewport(level.Price))
                {
                    continue;
                }

                var distance = level.Price - currentPrice;
                if (highSide ? distance <= 0.0 : distance >= 0.0)
                {
                    continue;
                }

                candidates.Add(new KeyValuePair<VisibleLevel, double>(level, Math.Abs(distance)));
            }

            if (candidates.Count == 0)
            {
                return;
            }

            candidates.Sort(delegate (KeyValuePair<VisibleLevel, double> a, KeyValuePair<VisibleLevel, double> b)
            {
                return a.Value.CompareTo(b.Value);
            });

            var font = Canvas.ChartFont;
            var acceptedRects = new List<Rect>(MaxDistanceLabelsPerSide);
            var chartRect = Canvas.Rect;
            var acceptedCount = 0;

            for (int i = 0; i < candidates.Count && acceptedCount < MaxDistanceLabelsPerSide; i++)
            {
                var text = FormatDistancePercent(candidates[i].Key.Price, currentPrice);
                if (string.IsNullOrEmpty(text))
                {
                    continue;
                }

                var size = font.GetSize(text);
                var lineY = GetY(candidates[i].Key.Price);
                var x = chartRect.Right - size.Width - LabelPadding;
                var y = lineY - size.Height - LabelLineGap;

                if (x < chartRect.Left + LabelPadding)
                {
                    x = chartRect.Left + LabelPadding;
                }

                if (y < chartRect.Top)
                {
                    y = lineY + LabelLineGap;
                }

                if (y > chartRect.Bottom - size.Height)
                {
                    y = chartRect.Bottom - size.Height;
                }

                if (y < chartRect.Top)
                {
                    y = chartRect.Top;
                }

                var labelRect = new Rect(x, y, size.Width, size.Height);
                var overlaps = false;

                for (int j = 0; j < acceptedRects.Count; j++)
                {
                    if (labelRect.Bottom > acceptedRects[j].Top - LabelSpacing &&
                        labelRect.Top < acceptedRects[j].Bottom + LabelSpacing)
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
                visual.DrawString(text, font, new XBrush(candidates[i].Key.Color), labelRect);
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
    }


    [TypeConverter(typeof(EnumDescriptionTypeConverter))]
    [DataContract(
        Name = "AkodeLevelsPeriodType",
        Namespace = "http://schemas.datacontract.org/2004/07/TigerTrade.Chart.Indicators.Custom"
    )]
    public enum AkodeLevelsPeriodType
    {
        [EnumMember(Value = "AnyTimeFrame"), Description("Any Time Frame")]
        AnyTimeFrame,
        [EnumMember(Value = "M"), Description("Minute")]
        Minute,
        [EnumMember(Value = "H"), Description("Hour")]
        Hour,
        [EnumMember(Value = "Week"), Description("Week")]
        Week,
        [EnumMember(Value = "Month"), Description("Month")]
        Month
    }
}