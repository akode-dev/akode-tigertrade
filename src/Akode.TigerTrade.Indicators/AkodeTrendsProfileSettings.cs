using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using TigerTrade.Chart.Indicators.Drawings;
using TigerTrade.Core.UI.Common;
using TigerTrade.Dx;
using TigerTrade.Dx.Enums;

namespace Akode.TigerTrade.Indicators
{
    [ReadOnly(true)]
    [TypeConverter(typeof(ExpandableObjectConverter))]
    [DataContract(
        Name = "AkodeTrendsProfileSettings",
        Namespace = "http://schemas.datacontract.org/2004/07/TigerTrade.Chart.Indicators.Custom"
    )]
    public sealed class AkodeTrendsProfileSettings : INotifyPropertyChanged, IDynamicProperty
    {
        private static readonly XColor[] DefaultHighColors =
        {
            XColor.FromArgb(120, 8, 153, 129),
            XColor.FromArgb(120, 46, 134, 193),
            XColor.FromArgb(120, 85, 145, 80),
            XColor.FromArgb(120, 0, 121, 107),
            XColor.FromArgb(120, 121, 85, 72)
        };

        private static readonly XColor[] DefaultLowColors =
        {
            XColor.FromArgb(120, 247, 82, 95),
            XColor.FromArgb(120, 255, 112, 67),
            XColor.FromArgb(120, 233, 30, 99),
            XColor.FromArgb(120, 171, 71, 188),
            XColor.FromArgb(120, 229, 57, 53)
        };

        private bool _enabled;
        private AkodeLevelsPeriodType _periodType;
        private int _periodValue;
        private int _candlesBefore;
        private int _candlesAfter;
        private int _maxLinesHigh;
        private int _maxLinesLow;
        private bool _useCandleBodyInsteadOfWicks;
        private int _maxBrokenLinesHigh;
        private int _maxBrokenLinesLow;
        private bool _showBrokenLines;
        private bool _includeInTrendlines = true;
        private ChartLine _highSeries;
        private ChartLine _lowSeries;
        private int _profileIndex;

        public AkodeTrendsProfileSettings()
        {
            _profileIndex = 1;
            _periodType = AkodeLevelsPeriodType.AnyTimeFrame;
            _periodValue = 1;
            _candlesBefore = 2;
            _candlesAfter = 2;
            _maxLinesHigh = 5;
            _maxLinesLow = 5;
            _maxBrokenLinesHigh = 2;
            _maxBrokenLinesLow = 2;
            EnsureInitialized(_profileIndex);
        }

        [DataMember(Name = "Enabled")]
        [Category("Profile"), DisplayName("Enabled")]
        public bool Enabled
        {
            get { return _enabled; }
            set
            {
                if (value == _enabled)
                {
                    return;
                }

                _enabled = value;
                OnPropertyChanged();
            }
        }

        [DataMember(Name = "IncludeInTrendlines")]
        [Category("Profile"), DisplayName("Include in trend lines")]
        public bool IncludeInTrendlines
        {
            get { return _includeInTrendlines; }
            set
            {
                if (value == _includeInTrendlines)
                {
                    return;
                }

                _includeInTrendlines = value;
                OnPropertyChanged();
            }
        }

        [DataMember(Name = "PeriodType")]
        [Category("Period"), DisplayName("Interval")]
        public AkodeLevelsPeriodType PeriodType
        {
            get { return _periodType; }
            set
            {
                if (value == _periodType)
                {
                    return;
                }

                _periodType = value;
                _periodValue = _periodType == AkodeLevelsPeriodType.Minute ? 5 : 1;

                OnPropertyChanged();
                OnPropertyChanged(nameof(PeriodValue));
            }
        }

        [DataMember(Name = "PeriodValue")]
        [Category("Period"), DisplayName("Value")]
        public int PeriodValue
        {
            get { return _periodValue; }
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

        [DataMember(Name = "CandlesBefore")]
        [Category("Settings"), DisplayName("Candles before")]
        public int CandlesBefore
        {
            get { return _candlesBefore; }
            set
            {
                value = Math.Max(0, value);

                if (value == _candlesBefore)
                {
                    return;
                }

                _candlesBefore = value;
                OnPropertyChanged();
            }
        }

        [DataMember(Name = "CandlesAfter")]
        [Category("Settings"), DisplayName("Candles after")]
        public int CandlesAfter
        {
            get { return _candlesAfter; }
            set
            {
                value = Math.Max(0, value);

                if (value == _candlesAfter)
                {
                    return;
                }

                _candlesAfter = value;
                OnPropertyChanged();
            }
        }

        [DataMember(Name = "MaxLinesHigh")]
        [Category("Settings"), DisplayName("Max High lines to show")]
        public int MaxLinesHigh
        {
            get { return _maxLinesHigh; }
            set
            {
                value = Math.Max(0, value);

                if (value == _maxLinesHigh)
                {
                    return;
                }

                _maxLinesHigh = value;
                OnPropertyChanged();
            }
        }

        [DataMember(Name = "MaxLinesLow")]
        [Category("Settings"), DisplayName("Max Low lines to show")]
        public int MaxLinesLow
        {
            get { return _maxLinesLow; }
            set
            {
                value = Math.Max(0, value);

                if (value == _maxLinesLow)
                {
                    return;
                }

                _maxLinesLow = value;
                OnPropertyChanged();
            }
        }

        [DataMember(Name = "UseCandleBodyInsteadOfWicks")]
        [Category("Settings"), DisplayName("Use candle body instead of wicks")]
        public bool UseCandleBodyInsteadOfWicks
        {
            get { return _useCandleBodyInsteadOfWicks; }
            set
            {
                if (value == _useCandleBodyInsteadOfWicks)
                {
                    return;
                }

                _useCandleBodyInsteadOfWicks = value;
                OnPropertyChanged();
            }
        }

        [DataMember(Name = "MaxBrokenLinesHigh")]
        [Category("Broken lines"), DisplayName("Max High lines")]
        public int MaxBrokenLinesHigh
        {
            get { return _maxBrokenLinesHigh; }
            set
            {
                value = Math.Max(0, value);

                if (value == _maxBrokenLinesHigh)
                {
                    return;
                }

                _maxBrokenLinesHigh = value;
                OnPropertyChanged();
            }
        }

        [DataMember(Name = "MaxBrokenLinesLow")]
        [Category("Broken lines"), DisplayName("Max Low lines")]
        public int MaxBrokenLinesLow
        {
            get { return _maxBrokenLinesLow; }
            set
            {
                value = Math.Max(0, value);

                if (value == _maxBrokenLinesLow)
                {
                    return;
                }

                _maxBrokenLinesLow = value;
                OnPropertyChanged();
            }
        }

        [DataMember(Name = "ShowBrokenLines")]
        [Category("Broken lines"), DisplayName("Show broken lines")]
        public bool ShowBrokenLines
        {
            get { return _showBrokenLines; }
            set
            {
                if (value == _showBrokenLines)
                {
                    return;
                }

                _showBrokenLines = value;
                OnPropertyChanged();
            }
        }

        [DataMember(Name = "HighLineColor")]
        [Category("Display"), DisplayName("High levels")]
        public ChartLine HighSeries
        {
            get
            {
                EnsureHighSeries();
                return _highSeries;
            }
            set
            {
                ReplaceLine(ref _highSeries, value, HandleHighSeriesChanged, CreateDefaultHighSeries(_profileIndex));
                OnPropertyChanged();
            }
        }

        [DataMember(Name = "LowLineColor")]
        [Category("Display"), DisplayName("Low levels")]
        public ChartLine LowSeries
        {
            get
            {
                EnsureLowSeries();
                return _lowSeries;
            }
            set
            {
                ReplaceLine(ref _lowSeries, value, HandleLowSeriesChanged, CreateDefaultLowSeries(_profileIndex));
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void EnsureInitialized(int profileIndex)
        {
            _profileIndex = Math.Max(1, profileIndex);
            EnsureHighSeries();
            EnsureLowSeries();
        }

        public void CopyFrom(AkodeTrendsProfileSettings other)
        {
            if (other == null)
            {
                return;
            }

            Enabled = other.Enabled;
            IncludeInTrendlines = other.IncludeInTrendlines;
            PeriodType = other.PeriodType;
            PeriodValue = other.PeriodValue;
            CandlesBefore = other.CandlesBefore;
            CandlesAfter = other.CandlesAfter;
            MaxLinesHigh = other.MaxLinesHigh;
            MaxLinesLow = other.MaxLinesLow;
            UseCandleBodyInsteadOfWicks = other.UseCandleBodyInsteadOfWicks;
            MaxBrokenLinesHigh = other.MaxBrokenLinesHigh;
            MaxBrokenLinesLow = other.MaxBrokenLinesLow;
            ShowBrokenLines = other.ShowBrokenLines;

            EnsureInitialized(_profileIndex);

            if (other.HighSeries != null)
            {
                HighSeries.CopyTheme(other.HighSeries);
            }

            if (other.LowSeries != null)
            {
                LowSeries.CopyTheme(other.LowSeries);
            }
        }

        public override string ToString()
        {
            return Enabled ? "Enabled" : "Disabled";
        }

        public bool GetPropertyHasStandardValues(string propertyName)
        {
            return false;
        }

        public bool GetPropertyReadOnly(string propertyName)
        {
            return false;
        }

        public IEnumerable<object> GetPropertyStandardValues(string propertyName)
        {
            return null;
        }

        public bool GetPropertyVisibility(string propertyName)
        {
            return true;
        }

        private void EnsureHighSeries()
        {
            if (_highSeries == null)
            {
                _highSeries = CreateDefaultHighSeries(_profileIndex);
            }

            _highSeries.PropertyChanged -= HandleHighSeriesChanged;
            _highSeries.PropertyChanged += HandleHighSeriesChanged;
        }

        private void EnsureLowSeries()
        {
            if (_lowSeries == null)
            {
                _lowSeries = CreateDefaultLowSeries(_profileIndex);
            }

            _lowSeries.PropertyChanged -= HandleLowSeriesChanged;
            _lowSeries.PropertyChanged += HandleLowSeriesChanged;
        }

        private void ReplaceLine(
            ref ChartLine field,
            ChartLine value,
            PropertyChangedEventHandler handler,
            ChartLine defaultValue)
        {
            if (ReferenceEquals(field, value))
            {
                return;
            }

            if (field != null)
            {
                field.PropertyChanged -= handler;
            }

            field = value ?? defaultValue;
            field.PropertyChanged -= handler;
            field.PropertyChanged += handler;
        }

        private void HandleHighSeriesChanged(object sender, PropertyChangedEventArgs e)
        {
            OnPropertyChanged(nameof(HighSeries));
        }

        private void HandleLowSeriesChanged(object sender, PropertyChangedEventArgs e)
        {
            OnPropertyChanged(nameof(LowSeries));
        }

        private ChartLine CreateDefaultHighSeries(int profileIndex)
        {
            return CreateDefaultSeries(DefaultHighColors[GetColorIndex(profileIndex)]);
        }

        private ChartLine CreateDefaultLowSeries(int profileIndex)
        {
            return CreateDefaultSeries(DefaultLowColors[GetColorIndex(profileIndex)]);
        }

        private static ChartLine CreateDefaultSeries(XColor color)
        {
            return new ChartLine
            {
                Style = XDashStyle.Solid,
                Width = 1,
                Color = color
            };
        }

        private static int GetColorIndex(int profileIndex)
        {
            return Math.Max(0, Math.Min(DefaultHighColors.Length - 1, profileIndex - 1));
        }

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            var handler = PropertyChanged;
            if (handler == null)
            {
                return;
            }

            handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
