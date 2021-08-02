using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Un4seen.Bass;

namespace MajdataEdit
{
    /// <summary>
    /// SoundSetting.xaml 的交互逻辑
    /// </summary>
    public partial class SoundSetting : Window
    {
        MainWindow MainWindow;
        public SoundSetting()
        {
            MainWindow = Application.Current.Windows
            .Cast<Window>()
            .FirstOrDefault(window => window is MainWindow) as MainWindow;

            InitializeComponent();
        }

        Timer UpdateLevelTimer = new Timer(1);

        private void SoundSettingWindow_Loaded(object sender, RoutedEventArgs e)
        {
            int[] streams = { MainWindow.bgmStream, MainWindow.trackStartStream };
            SetSlider(BGM_Slider, streams);
            SetSlider(Tap_Slider, MainWindow.clickStream);
            SetSlider(Break_Slider, MainWindow.breakStream);
            SetSlider(EX_Slider, MainWindow.exStream);
            int[] streams1 = { MainWindow.hanabiStream, MainWindow.holdRiserStream };
            SetSlider(Hanabi_Slider,streams1);

            UpdateLevelTimer.AutoReset = true;
            UpdateLevelTimer.Elapsed += UpdateLevelTimer_Elapsed;
            UpdateLevelTimer.Start();
        }

        private void UpdateLevelTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                int[] streams = { MainWindow.bgmStream, MainWindow.trackStartStream};
                UpdateProgressBar(BGM_Level, streams);
                UpdateProgressBar(Tap_Level, MainWindow.clickStream);
                UpdateProgressBar(Break_Level, MainWindow.breakStream);
                UpdateProgressBar(EX_Level, MainWindow.exStream);
                int[] streams1 = { MainWindow.hanabiStream, MainWindow.holdRiserStream };
                UpdateProgressBar(Hanabi_Level, streams1);
            });
        }
        void UpdateProgressBar(ProgressBar bar,int channel)
        {
            float ampLevel = 0f;
            Bass.BASS_ChannelGetAttribute(channel, BASSAttribute.BASS_ATTRIB_VOL, ref ampLevel);
            var value = (Utils.LevelToDB(Utils.LowWord(Bass.BASS_ChannelGetLevel(channel)) * ampLevel, 32768) +40);
            if (!double.IsNaN(value) && !double.IsInfinity(value))
            {
                bar.Value = value*ampLevel;
            }
            if (double.IsNegativeInfinity(value)) bar.Value = bar.Minimum;
            if (double.IsPositiveInfinity(value)) bar.Value = bar.Maximum;
            if (double.IsNaN(value)) bar.Value -= 1;
        }
        void UpdateProgressBar(ProgressBar bar, int[] channels)
        {
            double[] values = new double[channels.Length];
            float ampLevel = 0f;
            for (int i = 0; i < channels.Length; i++)
            {
                Bass.BASS_ChannelGetAttribute(channels[i], BASSAttribute.BASS_ATTRIB_VOL, ref ampLevel);
                values[i] = (Utils.LevelToDB(Utils.LowWord(Bass.BASS_ChannelGetLevel(channels[i])) * ampLevel, 32768) + 40);
            }
            var value = values.Max();
            if (!double.IsNaN(value) && !double.IsInfinity(value))
            {
                bar.Value = value * ampLevel;
            }
            if (double.IsNegativeInfinity(value)) bar.Value = bar.Minimum;
            if (double.IsPositiveInfinity(value)) bar.Value = bar.Maximum;
            if (double.IsNaN(value)) bar.Value -= 1;
        }
        void SetSlider(Slider slider,int channel)
        {
            float ampLevel = 0f;
            Bass.BASS_ChannelGetAttribute(channel, BASSAttribute.BASS_ATTRIB_VOL, ref ampLevel);
            slider.Value = ampLevel;
            void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
            {
                Bass.BASS_ChannelSetAttribute(channel, BASSAttribute.BASS_ATTRIB_VOL, (float)((Slider)sender).Value);
            }
            slider.ValueChanged += Slider_ValueChanged;
        }
        void SetSlider(Slider slider, int[] channels)
        {
            float ampLevel = 0f;
            foreach(var channel in channels)
                Bass.BASS_ChannelGetAttribute(channel, BASSAttribute.BASS_ATTRIB_VOL, ref ampLevel);
            slider.Value = ampLevel;
            void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
            {
                foreach (var channel in channels)
                    Bass.BASS_ChannelSetAttribute(channel, BASSAttribute.BASS_ATTRIB_VOL, (float)((Slider)sender).Value);
            }
            slider.ValueChanged += Slider_ValueChanged;
        }

        private void SoundSettingWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            UpdateLevelTimer.Stop();
            UpdateLevelTimer.Dispose();
        }
    }
}
