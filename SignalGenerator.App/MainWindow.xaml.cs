using Microsoft.Win32;
using ScottPlot.Plottables;
using SignalProcessing.Core;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using static SkiaSharp.HarfBuzz.SKShaper;

namespace SignalGenerator.App
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private SingleChannelSignal sig;
        private List<SingleChannelSignal> signalList = new List<SingleChannelSignal>();
        private List<OutOfRangeEventArgs> markup = new List<OutOfRangeEventArgs>();
        private List<VerticalLine> markerList = new List<VerticalLine>();
        private float markupFreq;
        private readonly ThresholdAnalyzer _analyzer = new ThresholdAnalyzer();
        private SignalMarkerRenderer _markerRenderer;
        private SignalAnalysisResult _analysisResult;
        private readonly CrossingCounter _crossingCounter = new CrossingCounter();
        private readonly PeriodEstimator _periodEstimator = new PeriodEstimator();
        public MainWindow()
        {
            InitializeComponent();
            _markerRenderer = new SignalMarkerRenderer(WpfPlot1.Plot);
            _analyzer.MarkerDetected += ShowMarker;
            _analyzer.MarkerDetected += AppendMarkerRow;
            _analyzer.MarkerDetected += _crossingCounter.OnMarkerDetected;
            _analyzer.MarkerDetected += _periodEstimator.OnMarkerDetected;
            SignalTypeComboBox.ItemsSource = Enum.GetValues(typeof(SignalType));
            SignalTypeComboBox.SelectedIndex = 0;
        }
        private void ShowMarker(object sender, MarkerDetectedEventArgs e)
        {
            _markerRenderer.AddMarker(e.Time, e.Value, e.Marker.Direction);
        }
        private void AppendMarkerRow(object sender, MarkerDetectedEventArgs e)
        {
            string direction;
            if (e.Marker.Direction == CrossingDirection.Up)
            {
                direction = "вверх";
            }
            else
            {
                direction = "вниз";
            }
            MarkerListBox.Items.Add(e.Time.ToString("F3") + " с; " + e.Value + "; " + direction);
        }
        private void ClearAnalysis()
        {
            if (_markerRenderer != null)
            {
                _markerRenderer.ClearMarkers();
            }
            MarkerListBox.Items.Clear();
            SummaryTextBlock.Text = "";
            _analysisResult = null;
            _crossingCounter.Reset();
            _periodEstimator.Reset();
            CounterTextBlock.Text = "";
            PeriodTextBlock.Text = "";
        }
        private void Button_Click_FindCrossings(object sender, RoutedEventArgs e)
        {
            if (signalList.Count == 0)
            {
                MessageBox.Show("Нет сигналов");
                return;
            }
            float threshold;
            if (!float.TryParse(ThresholdTextBox.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out threshold)
                || float.IsNaN(threshold) || float.IsInfinity(threshold))
            {
                MessageBox.Show("Некорректный порог");
                return;
            }
            SingleChannelSignal signal = signalList.Last();
            ClearAnalysis();
            try
            {
                _analysisResult = _analyzer.Analyze(signal, threshold);
                SummaryTextBlock.Text =
                    $"Найдено пересечений: {_analysisResult.Markers.Count}";
                CounterTextBlock.Text = "Вверх: " + _crossingCounter.UpCount + ", вниз: " + _crossingCounter.DownCount;
                if (float.IsNaN(_periodEstimator.Period))
                {
                    PeriodTextBlock.Text = "Период: не определен";
                }
                else
                {
                    PeriodTextBlock.Text = "Период: " + _periodEstimator.Period.ToString("F3") + " с";
                }
            }
            catch (ArgumentException ex)
            {
                ClearAnalysis();
                MessageBox.Show(ex.Message);
            }
            WpfPlot1.Refresh();
        }
        private void Button_Click_ClearMarkers(object sender, RoutedEventArgs e)
        {
            ClearAnalysis();
            WpfPlot1.Refresh();
        }
        private void addSignal(SingleChannelSignal obj)
        {
            ClearAnalysis();
            signalList.Add(obj);
            WpfPlot1.Plot.Add.ScatterLine(obj.TimeTick, obj.Data);
            WpfPlot1.Plot.Axes.AutoScale();
            WpfPlot1.Refresh();
        }
        private void addMarkers(OutOfRangeEventArgs obj, float freq)
        {
            VerticalLine start = WpfPlot1.Plot.Add.VerticalLine(obj.Start / freq);
            VerticalLine end = WpfPlot1.Plot.Add.VerticalLine(obj.End / freq);
            start.Color = ScottPlot.Colors.Red;
            end.Color = ScottPlot.Colors.Red;
            markerList.Add(start);
            markerList.Add(end);
        }
        private void clearMarkers()
        {
            foreach (VerticalLine marker in markerList)
            {
                WpfPlot1.Plot.Remove(marker);
            }
            markerList.Clear();
            WpfPlot1.Refresh();
        }


        private void SignalTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SignalType type = (SignalType)SignalTypeComboBox.SelectedItem;
            SinePanel.Visibility = Visibility.Collapsed;
            LinePanel.Visibility = Visibility.Collapsed;
            NoisePanel.Visibility = Visibility.Collapsed;
            SawPanel.Visibility = Visibility.Collapsed;
            TrianglePanel.Visibility = Visibility.Collapsed;
            MeanderPanel.Visibility = Visibility.Collapsed;
            switch (type)
            {
                case SignalType.Sine:
                    SinePanel.Visibility = Visibility.Visible;
                    break;
                case SignalType.Line:
                    LinePanel.Visibility = Visibility.Visible;
                    break;
                case SignalType.Noise:
                    NoisePanel.Visibility = Visibility.Visible;
                    break;
                case SignalType.Saw:
                    SawPanel.Visibility = Visibility.Visible;
                    break;
                case SignalType.Triangle:
                    TrianglePanel.Visibility = Visibility.Visible;
                    break;
                case SignalType.Meander:
                    MeanderPanel.Visibility = Visibility.Visible;
                    break;
            }
        }
        private GeneratorParameters ReadParameters(SignalType type)
        {
            switch (type)
            {
                case SignalType.Sine:
                    float period;
                    float phase;
                    float amplitude;
                    if (!float.TryParse(period_textBox.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out period)
                        || !float.TryParse(phase_textBox.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out phase)
                        || !float.TryParse(scale_textBox.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out amplitude))
                    {
                        MessageBox.Show("Некорректные параметры синусоиды");
                        return null;
                    }
                    return new SineParameters { Period = period, Phase = phase, Amplitude = amplitude };
                case SignalType.Line:
                    float k;
                    float b;
                    if (!float.TryParse(k_textBox.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out k)
                        || !float.TryParse(b_textBox.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out b))
                    {
                        MessageBox.Show("Некорректные параметры прямой");
                        return null;
                    }
                    return new LineParameters { K = k, B = b };
                case SignalType.Noise:
                    float range;
                    if (!float.TryParse(Amplitude_textBox.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out range))
                    {
                        MessageBox.Show("Некорректный размах шума");
                        return null;
                    }
                    return new NoiseParameters { Range = range };
                case SignalType.Saw:
                    float saw_period;
                    float saw_phase;
                    float saw_amplitude;
                    if (!float.TryParse(saw_period_textBox.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out saw_period)
                        || !float.TryParse(saw_phase_textBox.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out saw_phase)
                        || !float.TryParse(saw_amplitude_textBox.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out saw_amplitude))
                    {
                        MessageBox.Show("Некорректные параметры пилообразного сигнала");
                        return null;
                    }
                    return new SawParameters { Period = saw_period, Phase = saw_phase, Amplitude = saw_amplitude };
                case SignalType.Triangle:
                    float triangle_period;
                    float triangle_phase;
                    float triangle_amplitude;
                    if (!float.TryParse(triangle_period_textBox.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out triangle_period)
                        || !float.TryParse(triangle_phase_textBox.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out triangle_phase)
                        || !float.TryParse(triangle_amplitude_textBox.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out triangle_amplitude))
                    {
                        MessageBox.Show("Некорректные параметры треугольного сигнала");
                        return null;
                    }
                    return new TriangleParameters { Period = triangle_period, Phase = triangle_phase, Amplitude = triangle_amplitude };
                case SignalType.Meander:
                    float meander_period;
                    float meander_phase;
                    float meander_amplitude;
                    float meander_duty;
                    if (!float.TryParse(meander_period_textBox.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out meander_period)
                        || !float.TryParse(meander_phase_textBox.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out meander_phase)
                        || !float.TryParse(meander_amplitude_textBox.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out meander_amplitude)
                        || !float.TryParse(meander_duty_textBox.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out meander_duty))
                    {
                        MessageBox.Show("Некорректные параметры меандра");
                        return null;
                    }
                    return new MeanderParameters { Period = meander_period, Phase = meander_phase, Amplitude = meander_amplitude, DutyCycle = meander_duty };
                default:
                    return null;
            }
        }
        private void Button_Click_Generate(object sender, RoutedEventArgs e)
        {
            float duration;
            float samplingRate;
            if (!float.TryParse(len_textBox.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out duration)
                || !float.TryParse(freq_textBox.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out samplingRate))
            {
                MessageBox.Show("Некорректная длина или частота");
                return;
            }
            SignalType selectedType = (SignalType)SignalTypeComboBox.SelectedItem;
            GeneratorParameters parameters = ReadParameters(selectedType);
            if (parameters == null)
            {
                return;
            }
            try
            {
                ISignalGenerator generator = SignalGeneratorFactory.Create(parameters);
                SingleChannelSignal signal = generator.Generate(new SamplingParameters(samplingRate, duration));
                addSignal(signal);
            }
            catch (ArgumentException ex)
            {
                MessageBox.Show(ex.Message);
            }
            catch (OutOfMemoryException)
            {
                MessageBox.Show("Не хватает памяти: уменьшите длительность или частоту");
            }
        }
        private void Button_Clear(object sender, RoutedEventArgs e)
        {
            ClearAnalysis();
            signalList.Clear();
            markup.Clear();
            markerList.Clear();
            WpfPlot1.Plot.Clear();
            WpfPlot1.Refresh();
        }
        private void Button_Write(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "csv-файл (*.csv)|*.csv";
            saveFileDialog.FileName = "MySignal";
            saveFileDialog.DefaultExt = "csv";
            if (saveFileDialog.ShowDialog() == true && signalList.Count() != 0)
            {
                string filePath = saveFileDialog.FileName;
                SingleChannelSignalWriter.Write(signalList.Last(), filePath);
            }


        }
        private void Button_Load(object sender, RoutedEventArgs e)
        {
            OpenFileDialog myDialog = new OpenFileDialog();
            myDialog.Filter = "csv-файл(*.csv)|*.csv";
            myDialog.CheckFileExists = true;
            myDialog.Multiselect = false;
            if (myDialog.ShowDialog() == true)
            {
                string filePath = myDialog.FileName;
                try
                {
                    addSignal(SingleChannelSignalReader.Read(filePath));
                }
                catch (InvalidDataException ex)
                {
                    MessageBox.Show(ex.Message);
                }
                
            }
            

        }
        private void Button_Click_Remove(object sender, RoutedEventArgs e)
        {
            if (signalList.Count == 0)
            {
                MessageBox.Show("Нет сигналов для удаления");
            }
            else
            {
                clearMarkers();
                ClearAnalysis();
                WpfPlot1.Plot.Remove(WpfPlot1.Plot.PlottableList.Last());
                signalList.Remove(signalList.Last());
                WpfPlot1.Refresh();
            }
        }
        private void Button_Click_Summ(object sender, RoutedEventArgs e)
        {
            try
            {
                SingleChannelSignal sig_summ = SignalOperations.Sum(signalList);
                addSignal(sig_summ);
            }
            catch (ArgumentException ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        private void Button_Click_Concat(object sender, RoutedEventArgs e)
        {
            try
            {
                SingleChannelSignal sig_con = SignalOperations.Concat(signalList);
                addSignal(sig_con);
            }
            catch (ArgumentException ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        private void Button_Click_Resample(object sender, RoutedEventArgs e)
        {
            if (signalList.Count == 0)
            {
                MessageBox.Show("Нет сигналов");
                return;
            }
            float new_freq = (float.Parse(newFreq_textBox.Text, CultureInfo.InvariantCulture));
            try
            {
                SingleChannelSignal sig_res = Resampler.Resample(signalList.Last(), new_freq);
                clearMarkers();
                ClearAnalysis();
                WpfPlot1.Plot.Remove(WpfPlot1.Plot.PlottableList.Last());
                signalList.Remove(signalList.Last());
                addSignal(sig_res);
            }
            catch (ArgumentException ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void Button_Click_Offline(object sender, RoutedEventArgs e)
        {
            if (signalList.Count == 0)
            {
                MessageBox.Show("Нет сигналов");
                return;
            }
            float window_size = (float.Parse(Window_textBox.Text, CultureInfo.InvariantCulture));
            int intWindowSize = (int)(window_size * signalList.Last().Frequency+ 0.5);
            if (intWindowSize % 2 == 0)
            {
                intWindowSize++;
            }
            bool is_median = filterType_comboBox.SelectedIndex == 1;
            try
            {
                float[] result;
                if (is_median)
                {
                    result = OfflineMedianFilter.Filter(signalList.Last().Data, intWindowSize);
                }
                else
                {
                    result = OfflineFilter.Filter(signalList.Last().Data, intWindowSize);
                }
                SingleChannelSignal newData = new SingleChannelSignal(result, signalList.Last().Frequency);
                addSignal(newData);
            }
            catch (ArgumentException ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void Button_Click_Online(object sender, RoutedEventArgs e)
        {
            if (signalList.Count == 0)
            {
                MessageBox.Show("Нет сигналов");
                return;
            }
            float window_size = (float.Parse(Window_textBox.Text, CultureInfo.InvariantCulture));
            int intWindowSize = (int)(window_size * signalList.Last().Frequency+ 0.5);
            bool is_median = filterType_comboBox.SelectedIndex == 1;
            if (is_median && intWindowSize % 2 == 0)
            {
                intWindowSize++;
            }
            float[] data = signalList.Last().Data;
            int chunkSize = 7;
            try
            {
                OnlineFilter filter = null;
                OnlineMedianFilter median_filter = null;
                if (is_median)
                {
                    median_filter = new OnlineMedianFilter(intWindowSize);
                }
                else
                {
                    filter = new OnlineFilter(intWindowSize);
                }
                List<float> result = new List<float>();

                // Подаём сигнал на фильтр фрагментами фиксированного размера
                for (int i = 0; i < data.Length; i += chunkSize)
                {
                    float[] chunk = data.Skip(i).Take(chunkSize).ToArray();
                    if (is_median)
                    {
                        result.AddRange(median_filter.Filter(chunk));
                    }
                    else
                    {
                        result.AddRange(filter.Filter(chunk));
                    }
                }

                SingleChannelSignal newData = new SingleChannelSignal(result.ToArray(), signalList.Last().Frequency);
                addSignal(newData);
            }
            catch (ArgumentException ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        private void Button_Click_Analyze(object sender, RoutedEventArgs e)
        {
            if (signalList.Count == 0)
            {
                MessageBox.Show("Нет сигналов");
                return;
            }
            float min = (float.Parse(min_textBox.Text, CultureInfo.InvariantCulture));
            float max = (float.Parse(max_textBox.Text, CultureInfo.InvariantCulture));
            float duration = (float.Parse(duration_textBox.Text, CultureInfo.InvariantCulture));
            SingleChannelSignal sig = signalList.Last();
            int minLength = (int)(duration * sig.Frequency+ 0.5);

            clearMarkers();
            markup.Clear();
            markupFreq = sig.Frequency;

            SingleChannelRangeAnalyzer analyzer = new SingleChannelRangeAnalyzer(min, max, minLength);
            analyzer.OutOfRange += Analyzer_OutOfRange;
            analyzer.Analyze(sig.Data);
            WpfPlot1.Refresh();
        }
        private void Analyzer_OutOfRange(object sender, OutOfRangeEventArgs e)
        {
            markup.Add(e);
            addMarkers(e, markupFreq);
        }
        private void Button_Write_Markup(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "csv-файл (*.csv)|*.csv";
            saveFileDialog.FileName = "MyMarkup";
            saveFileDialog.DefaultExt = "csv";
            if (saveFileDialog.ShowDialog() == true && markup.Count() != 0)
            {
                string filePath = saveFileDialog.FileName;
                MarkupWriter.Write(markupFreq, markup, filePath);
            }
        }
        private void Button_Load_Markup(object sender, RoutedEventArgs e)
        {
            OpenFileDialog myDialog = new OpenFileDialog();
            myDialog.Filter = "csv-файл(*.csv)|*.csv";
            myDialog.CheckFileExists = true;
            myDialog.Multiselect = false;
            if (myDialog.ShowDialog() == true)
            {
                string filePath = myDialog.FileName;
                clearMarkers();
                markup = MarkupLoader.Load(filePath, out markupFreq);
                foreach (OutOfRangeEventArgs element in markup)
                {
                    addMarkers(element, markupFreq);
                }
                WpfPlot1.Refresh();
            }
        }
        private void Button_Click_CutUp(object sender, RoutedEventArgs e)
        {
            if (signalList.Count == 0)
            {
                MessageBox.Show("Нет сигналов");
                return;
            }
            float upCut = (float.Parse(upCut_textBox.Text, CultureInfo.InvariantCulture));
            SingleChannelSignal sig = signalList.Last();
            ClearAnalysis();
            WpfPlot1.Plot.Clear();
            WpfPlot1.Refresh();
            ThresholdCutter thre = new ThresholdCutter();
            thre.SetSignal(sig);
            thre.CutUp(upCut);
            addSignal(sig);




        }
        private void Button_Click_CutDown(object sender, RoutedEventArgs e)
        {
            if (signalList.Count == 0)
            {
                MessageBox.Show("Нет сигналов");
                return;
            }
            float downCut = (float.Parse(downCut_textBox.Text, CultureInfo.InvariantCulture));
            SingleChannelSignal sig = signalList.Last();
            ClearAnalysis();
            WpfPlot1.Plot.Clear();
            WpfPlot1.Refresh();
            ThresholdCutter thre = new ThresholdCutter();
            thre.SetSignal(sig);
            thre.CutDown(downCut);
            addSignal(sig);
        }
        private void Button_Click_CutUpAndDown(object sender, RoutedEventArgs e)
        {
            if (signalList.Count == 0)
            {
                MessageBox.Show("Нет сигналов");
                return;
            }
            float upCut = (float.Parse(upCut_textBox.Text, CultureInfo.InvariantCulture));
            float downCut = (float.Parse(downCut_textBox.Text, CultureInfo.InvariantCulture));
            SingleChannelSignal sig = signalList.Last();
            ClearAnalysis();
            WpfPlot1.Plot.Clear();
            WpfPlot1.Refresh();
            ThresholdCutter thre = new ThresholdCutter();
            thre.SetSignal(sig);
            thre.CutUp(upCut);
            thre.CutDown(downCut);
            addSignal(sig);
        }

    }
}
