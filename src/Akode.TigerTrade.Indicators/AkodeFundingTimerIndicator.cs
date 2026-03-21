using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;
using System.Windows;
using TigerTrade.Chart.Base;
using TigerTrade.Chart.Indicators.Common;
using TigerTrade.Chart.Indicators.Drawings;
using TigerTrade.Chart.Indicators.Enums;
using TigerTrade.Dx;
using TigerTrade.Dx.Enums;

namespace Akode.TigerTrade.Indicators
{
    [DataContract(
        Name = "AkodeFundingTimerIndicator",
        Namespace = "http://schemas.datacontract.org/2004/07/TigerTrade.Chart.Indicators.Custom"
    )]
    [Indicator("X_AkodeFundingTimerIndicator", "_Akode: Funding Timer", true, Type = typeof(AkodeFundingTimerIndicator))]
    public sealed class AkodeFundingTimerIndicator : IndicatorBase
    {
        private string _fundingHours = "0,8,16";

        [DataMember(Name = "FundingHours")]
        [Category("Settings"), DisplayName("Funding hours (UTC)")]
        public string FundingHours
        {
            get => _fundingHours;
            set
            {
                if (value == _fundingHours) return;
                _fundingHours = value;
                _parsedHours = null;
                OnPropertyChanged();
            }
        }

        private int _timeOffsetHours;

        [DataMember(Name = "TimeOffsetHours"), DefaultValue(0)]
        [Category("Settings"), DisplayName("Time offset (hours)")]
        public int TimeOffsetHours
        {
            get => _timeOffsetHours;
            set
            {
                value = Math.Max(-23, Math.Min(23, value));
                if (value == _timeOffsetHours) return;
                _timeOffsetHours = value;
                _parsedHours = null;
                OnPropertyChanged();
            }
        }

        [DataMember(Name = "ShowNextOnly"), DefaultValue(false)]
        [Category("Settings"), DisplayName("Show next funding only")]
        public bool ShowNextOnly { get; set; }

        [DataMember(Name = "LineSeries")]
        [Category("Display"), DisplayName("Line style")]
        public ChartLine LineSeries { get; set; }

        [Browsable(false)]
        public override IndicatorCalculation Calculation => IndicatorCalculation.OnBarClose;

        private HashSet<int> _parsedHours;
        private List<int> _fundingBarIndices;
        private int _nextFundingBarIndex = -1;

        public AkodeFundingTimerIndicator() { InitializeStyles(); }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            if (LineSeries == null) InitializeStyles();
        }

        private void InitializeStyles()
        {
            LineSeries = new ChartLine
            {
                Style = XDashStyle.Dash,
                Width = 1,
                Color = XColor.FromArgb(80, 200, 200, 200)
            };
        }

        private HashSet<int> GetParsedHours()
        {
            if (_parsedHours != null) return _parsedHours;

            _parsedHours = new HashSet<int>();
            if (string.IsNullOrWhiteSpace(_fundingHours)) return _parsedHours;

            foreach (var part in _fundingHours.Split(','))
            {
                if (int.TryParse(part.Trim(), out var hour) && hour >= 0 && hour <= 23)
                {
                    _parsedHours.Add(((hour + _timeOffsetHours) % 24 + 24) % 24);
                }
            }

            return _parsedHours;
        }

        public override void CopyTemplate(IndicatorBase indicator, bool style)
        {
            var i = (AkodeFundingTimerIndicator)indicator;
            FundingHours = i.FundingHours;
            TimeOffsetHours = i.TimeOffsetHours;
            ShowNextOnly = i.ShowNextOnly;

            if (LineSeries == null) InitializeStyles();
            if (i.LineSeries != null) LineSeries.CopyTheme(i.LineSeries);

            base.CopyTemplate(indicator, style);
        }

        protected override void Execute()
        {
            if (_fundingBarIndices == null) _fundingBarIndices = new List<int>();
            _fundingBarIndices.Clear();
            _nextFundingBarIndex = -1;

            var dataLength = Helper.Count;
            if (dataLength < 2) return;

            var hours = GetParsedHours();
            if (hours.Count == 0) return;

            var date = Helper.Date;

            for (int i = 0; i < dataLength; i++)
            {
                var candleTime = DateTime.FromOADate(date[i]);

                if (hours.Contains(candleTime.Hour) && candleTime.Minute == 0)
                {
                    _fundingBarIndices.Add(i);
                }
            }

            if (ShowNextOnly)
            {
                var lastTime = DateTime.FromOADate(date[dataLength - 1]);
                var barDuration = lastTime - DateTime.FromOADate(date[dataLength - 2]);

                if (barDuration.TotalSeconds > 0)
                {
                    var nextFundingTime = GetNextFundingTime(lastTime, hours);
                    var barsAhead = (int)Math.Ceiling((nextFundingTime - lastTime).TotalSeconds / barDuration.TotalSeconds);
                    _nextFundingBarIndex = dataLength - 1 + barsAhead;
                }
            }
        }

        private DateTime GetNextFundingTime(DateTime after, HashSet<int> hours)
        {
            var candidate = new DateTime(after.Year, after.Month, after.Day, after.Hour, 0, 0);
            if (candidate <= after) candidate = candidate.AddHours(1);

            for (int i = 0; i < 48; i++)
            {
                if (hours.Contains(candidate.Hour))
                    return candidate;
                candidate = candidate.AddHours(1);
            }

            return after.AddHours(8);
        }

        public override void Render(DxVisualQueue visual)
        {
            if (_fundingBarIndices == null || _fundingBarIndices.Count == 0 || Canvas == null)
                return;

            if (!LineSeries.Visible) return;

            var chartRect = Canvas.Rect;
            var pen = new XPen(new XBrush(LineSeries.Color), LineSeries.Width, LineSeries.Style);

            if (ShowNextOnly)
            {
                if (_nextFundingBarIndex >= 0)
                {
                    var x = Canvas.GetX(_nextFundingBarIndex);
                    if (x >= chartRect.Left && x <= chartRect.Right)
                        visual.DrawLine(pen, x, chartRect.Top, x, chartRect.Bottom);
                }
                return;
            }

            foreach (var barIndex in _fundingBarIndices)
            {
                var x = Canvas.GetX(barIndex);

                if (x < chartRect.Left || x > chartRect.Right) continue;

                visual.DrawLine(pen, x, chartRect.Top, x, chartRect.Bottom);
            }
        }
    }
}
