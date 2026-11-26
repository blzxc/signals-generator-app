using HarfBuzzSharp;
using OpenTK;
using SignalProcessing.Core;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;

namespace SignalGenerator.App
{
    internal class Tools
    {

        public static SingleChannelSignal Line(float len, float freq, float k, float b)
        {
            int count = (int)(len * freq + 0.5);
            
            float[]x = new float[count];
            float[]y = new float[count];
            float diff = 1 / freq;
            for (int i = 0; i < count; i++) {

                    x[i] = i * diff;
                    y[i] = k * x[i] + b;

        }

            return new SingleChannelSignal(freq, y);
        }
        public static SingleChannelSignal Sin(float len, float freq, float period, float phase, float scale)
        {
            int count = (int)(len * freq + 0.5);
            float[]x = new float[count];
            float[]y = new float[count];
            float diff = 1 / freq;
            for (int i = 0; i < count; i++)
            {
              
                    x[i] = i * diff;
                    y[i] = scale * (float)Math.Sin((x[i] - phase) * 2 * Math.PI / period);
               
                
            }

            return new SingleChannelSignal(freq, y);
        }
        public static SingleChannelSignal Noise(float len, float freq,float amplitude)
        {
            Random random = new Random();
            int count = (int)(len * freq + 0.5);
            float[] x = new float[count];
            float[] y = new float[count];
            float diff = 1 / freq;
            for (int i = 0; i < count; i++)
            {

                x[i] = i;
                y[i] = (float)random.NextDouble()*2*amplitude - amplitude;


            }

            return new SingleChannelSignal(freq, y);
        }
        public static SingleChannelSignal Summ(List<SingleChannelSignal> signalList)
        {

            if (signalList.Count == 0)
            {
                return null;
                
            }

            float freq = signalList.First().Freq;

            int max_len = signalList.Select(el => el.Value.Length).Max();
            float[] summ_signals = new float[max_len];
            double eps = 0.0000001;
            for (int i = 0; i < signalList.Count; i++) {
                if (Math.Abs(signalList[i].Freq - freq) > eps)
                {
                    return null;
                }
            }
                for (int i = 0; i< signalList.Count; i++)
            {
                {
                    for(int j = 0; j < signalList[i].Value.Length; j++)
                    {
                        summ_signals[j] += signalList[i].Value[j];
                    }
                }
            }
            
            return new SingleChannelSignal(freq, summ_signals);
        }

        public static SingleChannelSignal Concat(List<SingleChannelSignal> signalList)
        {

            if (signalList.Count == 0)
            {
                return null;

            }

            float freq = signalList.First().Freq;

            IEnumerable<float> concat_signals = new float[0];
            double eps = 0.0000001;
            for (int i = 0; i < signalList.Count; i++)
            {
                if (Math.Abs(signalList[i].Freq - freq) > eps)
                {
                    return null;
                }
            }
            for (int i = 0; i < signalList.Count; i++)
            {
                concat_signals = concat_signals.Concat(signalList[i].Value);
            }

            return new SingleChannelSignal(freq, concat_signals.ToArray());
        }
    }
}
