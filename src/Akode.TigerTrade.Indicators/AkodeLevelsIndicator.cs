using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;
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

        private struct LevelLine
        {
            public double Price;
            public int StartIndex;
            public bool IsBroken;
        }

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
        }
        public override void ApplyColors(IChartTheme theme)
        {
            HighSeries.Color = theme.GetNextColor();
            LowSeries.Color = theme.GetNextColor();

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

            base.CopyTemplate(indicator, style);
        }

        protected override void Execute()
        {
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

            DrawLines(finalHighsBuffer, HighSeries, dataLength);
            DrawLines(finalLowsBuffer, LowSeries, dataLength);
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

        private void DrawLines(CircularBuffer<LevelLine> lines, ChartLine baseStyle, int dataLength)
        {
            foreach (var line in lines)
            {
                if (line.IsBroken && !ShowBrokenLines) 
                    continue; 
                
                var data = new double[dataLength];
                for (int i = 0; i < data.Length; i++) data[i] = double.NaN;
                for (int i = line.StartIndex; i < dataLength; i++) data[i] = line.Price;
                
                var lineStyle = new ChartLine
                {
                    Color = baseStyle.Color,
                    Width = baseStyle.Width,
                    Style = line.IsBroken ? XDashStyle.Dot : baseStyle.Style
                };
                
                Series.Add(new IndicatorSeriesData(data, lineStyle) 
                { 
                    Style = 
                    { 
                        DisableMinMax = true 
                    } 
                });
            }
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