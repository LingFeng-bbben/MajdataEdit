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
            SetSlider(BGM_Slider, MainWindow.bgmStream, MainWindow.trackStartStream, MainWindow.allperfectStream);
            SetSlider(Tap_Slider, MainWindow.clickStream);
            SetSlider(Break_Slider, MainWindow.breakStream);
            SetSlider(Slide_Slider, MainWindow.slideStream);

            SetSlider(EX_Slider, MainWindow.exStream, MainWindow.touchStream);

            SetSlider(Hanabi_Slider, MainWindow.hanabiStream, MainWindow.holdRiserStream);

            UpdateLevelTimer.AutoReset = true;
            UpdateLevelTimer.Elapsed += UpdateLevelTimer_Elapsed;
            UpdateLevelTimer.Start();
        }

        private void UpdateLevelTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                UpdateProgressBar(BGM_Level, MainWindow.bgmStream, MainWindow.trackStartStream, MainWindow.allperfectStream);
                UpdateProgressBar(Tap_Level, MainWindow.clickStream);
                UpdateProgressBar(Break_Level, MainWindow.breakStream);
                UpdateProgressBar(Slide_Level, MainWindow.slideStream);
                UpdateProgressBar(EX_Level, MainWindow.exStream, MainWindow.touchStream);
                UpdateProgressBar(Hanabi_Level, MainWindow.hanabiStream, MainWindow.holdRiserStream);
            });
        }
        void UpdateProgressBar(ProgressBar bar, params int[] channels)
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
        void SetSlider(Slider slider, params int[] channels)
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
