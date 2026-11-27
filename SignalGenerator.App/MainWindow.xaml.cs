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
        public MainWindow()
        {
            InitializeComponent();
            
        }
        private void addSignal(SingleChannelSignal obj)
        {

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


        private void Button_Click_Sin(object sender, RoutedEventArgs e)
        {
            float len = (float.Parse(len_textBox.Text, CultureInfo.InvariantCulture));
            float freq = (float.Parse(freq_textBox.Text, CultureInfo.InvariantCulture));
            float period = (float.Parse(period_textBox.Text, CultureInfo.InvariantCulture));
            float phase = (float.Parse(phase_textBox.Text, CultureInfo.InvariantCulture));
            float scale = (float.Parse(scale_textBox.Text, CultureInfo.InvariantCulture));

            addSignal(Tools.Sin(len, freq, period, phase, scale));
            

        }
        private void Button_Click_Line(object sender, RoutedEventArgs e)
        {
            float k = (float.Parse(k_textBox.Text, CultureInfo.InvariantCulture));
            float b = (float.Parse(b_textBox.Text, CultureInfo.InvariantCulture));
            float len = (float.Parse(len_textBox.Text, CultureInfo.InvariantCulture));
            float freq = (float.Parse(freq_textBox.Text, CultureInfo.InvariantCulture));
            addSignal(Tools.Line(len,freq,k, b));


        }
        private void Button_Clear(object sender, RoutedEventArgs e)
        {
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
                addSignal(SingleChannelSignalReader.Read(filePath));
                
            }
            

        }
        private void Button_Click_Noise(object sender, RoutedEventArgs e)
        {
            float amplitude = (float.Parse(Amplitude_textBox.Text, CultureInfo.InvariantCulture));
            float len = (float.Parse(len_textBox.Text, CultureInfo.InvariantCulture));
            float freq = (float.Parse(freq_textBox.Text, CultureInfo.InvariantCulture));
            addSignal(Tools.Noise(len, freq, amplitude));
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
                WpfPlot1.Plot.Remove(WpfPlot1.Plot.PlottableList.Last());
                signalList.Remove(signalList.Last());
                WpfPlot1.Refresh();
            }
        }
        private void Button_Click_Summ(object sender, RoutedEventArgs e)
        {
            SingleChannelSignal sig_summ = Tools.Summ(signalList);
            string text = "Нет сигналов или частоты не совпадают";
            if(sig_summ == null)
            {
                MessageBox.Show(text);
            }
            else
            {
                addSignal(sig_summ);
            }
                
        }
        private void Button_Click_Concat(object sender, RoutedEventArgs e)
        {
            SingleChannelSignal sig_con = Tools.Concat(signalList);
            string text = "Нет сигналов или частоты не совпадают";
            if (sig_con == null)
            {
                MessageBox.Show(text);
            }
            else
            {
                addSignal(sig_con);
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
            float[] result = OfflineFilter.Filter(signalList.Last().Data, intWindowSize);
            SingleChannelSignal newData = new SingleChannelSignal(result, signalList.Last().Frequency);
            addSignal(newData);
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
            float[] data = signalList.Last().Data;
            int chunkSize = 7;
            OnlineFilter filter = new OnlineFilter(intWindowSize);
            List<float> result = new List<float>();

            // Подаём сигнал на фильтр фрагментами фиксированного размера
            for (int i = 0; i < data.Length; i += chunkSize)
            {
                float[] chunk = data.Skip(i).Take(chunkSize).ToArray();
                result.AddRange(filter.Filter(chunk));
            }

            SingleChannelSignal newData = new SingleChannelSignal(result.ToArray(), signalList.Last().Frequency);
            addSignal(newData);
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
