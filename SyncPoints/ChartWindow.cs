using LiveCharts;
using LiveCharts.Configurations;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;

namespace SyncPoints
{
    /// <summary>
    /// Interaction logic for GraphWindow.xaml
    /// </summary>
    public partial class ChartWindow : Window, INotifyPropertyChanged
    {
        private double chartWidth;

        public SeriesCollection SeriesCollection { get; set; }
        public Func<double, string> TimeSpanFormatter { get; set; }
        public double AxisUnit { get; set; }
        public double AxisStep { get; set; }
        List<PointValues> BlueValues { get; set; }
        List<PointValues> GreenValues { get; set; }
        public double ChartWidth { get => chartWidth; set { chartWidth = value; OnPropertyChanged("ChartWidth"); } }

        public ChartWindow(string chartName, List<PointValues> bluevalues, List<PointValues> greenvalues)
        {
            BlueValues = bluevalues;
            GreenValues = greenvalues;
            InitializeComponent();
            chartTitle.Text = chartName;
            var mapper = Mappers.Xy<PointValues>()
            .X(model => model.TimeElapsed.Ticks)   //use TimeElapsed.Ticks as X
            .Y(model => model.PointNumber);
            Charting.For<PointValues>(mapper);
            TimeSpanFormatter = value => new TimeSpan((long)value).Seconds.ToString();
            line.Values = new ChartValues<PointValues>(BlueValues);
            line2.Values = new ChartValues<PointValues>(GreenValues);
            AxisUnit = TimeSpan.TicksPerSecond;
            AxisStep = TimeSpan.FromSeconds(1).Ticks;
            UpdateChartWidth();
            DataContext = this;

        }

        /// <summary>
        /// Updates the width based on the 
        /// </summary>
        void UpdateChartWidth()
        {
            ChartWidth = Math.Max(750, 25 * BlueValues.Count);
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            line.Values = new ChartValues<PointValues>(BlueValues);
            line2.Values = new ChartValues<PointValues>(GreenValues);
            UpdateChartWidth();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
