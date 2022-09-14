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
        Dictionary<Slider, Label> SliderValueBindingMap = new Dictionary<Slider, Label>(); // Slider和ValueLabel的绑定关系

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
            SliderValueBindingMap.Add(BGM_Slider, BGM_Value);
            SliderValueBindingMap.Add(Tap_Slider, Tap_Value);
            SliderValueBindingMap.Add(Break_Slider, Break_Value);
            SliderValueBindingMap.Add(Slide_Slider, Slide_Value);
            SliderValueBindingMap.Add(EX_Slider, EX_Value);
            SliderValueBindingMap.Add(Touch_Slider, Touch_Value);
            SliderValueBindingMap.Add(Hanabi_Slider, Hanabi_Value);

            SetSlider(BGM_Slider, MainWindow.bgmStream, MainWindow.trackStartStream, MainWindow.allperfectStream, MainWindow.clockStream);
            SetSlider(Tap_Slider, MainWindow.answerStream, MainWindow.judgeStream);
            SetSlider(Break_Slider, MainWindow.breakStream, MainWindow.judgeBreakStream);
            SetSlider(Slide_Slider, MainWindow.slideStream);

            SetSlider(EX_Slider, MainWindow.judgeExStream);
            SetSlider(Touch_Slider, MainWindow.touchStream);

            SetSlider(Hanabi_Slider, MainWindow.hanabiStream, MainWindow.holdRiserStream);

            UpdateLevelTimer.AutoReset = true;
            UpdateLevelTimer.Elapsed += UpdateLevelTimer_Elapsed;
            UpdateLevelTimer.Start();
        }

        private void UpdateLevelTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                UpdateProgressBar(BGM_Level, MainWindow.bgmStream, MainWindow.trackStartStream, MainWindow.allperfectStream, MainWindow.clockStream);
                UpdateProgressBar(Tap_Level, MainWindow.answerStream);
                UpdateProgressBar(Break_Level, MainWindow.breakStream);
                UpdateProgressBar(Slide_Level, MainWindow.slideStream);
                UpdateProgressBar(EX_Level, MainWindow.judgeExStream);
                UpdateProgressBar(Touch_Level, MainWindow.touchStream);
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
            SliderValueBindingMap[slider].Content = slider.Value.ToString("P0");
            void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
            {
                Slider sld = (Slider)sender;
                foreach (var channel in channels)
                    Bass.BASS_ChannelSetAttribute(channel, BASSAttribute.BASS_ATTRIB_VOL, (float)sld.Value);

                SliderValueBindingMap[sld].Content = sld.Value.ToString("P0");

            }
            slider.ValueChanged += Slider_ValueChanged;
        }

        private void SoundSettingWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            UpdateLevelTimer.Stop();
            UpdateLevelTimer.Dispose();
        }

        private void BtnSetDefault_Click(object sender, RoutedEventArgs e)
        {
            Bass.BASS_ChannelGetAttribute(MainWindow.bgmStream, BASSAttribute.BASS_ATTRIB_VOL, ref MainWindow.editorSetting.Default_BGM_Level);
            Bass.BASS_ChannelGetAttribute(MainWindow.answerStream, BASSAttribute.BASS_ATTRIB_VOL, ref MainWindow.editorSetting.Default_Tap_Level);
            Bass.BASS_ChannelGetAttribute(MainWindow.breakStream, BASSAttribute.BASS_ATTRIB_VOL, ref MainWindow.editorSetting.Default_Break_Level);
            Bass.BASS_ChannelGetAttribute(MainWindow.slideStream, BASSAttribute.BASS_ATTRIB_VOL, ref MainWindow.editorSetting.Default_Slide_Level);
            Bass.BASS_ChannelGetAttribute(MainWindow.judgeExStream, BASSAttribute.BASS_ATTRIB_VOL, ref MainWindow.editorSetting.Default_Ex_Level);
            Bass.BASS_ChannelGetAttribute(MainWindow.touchStream, BASSAttribute.BASS_ATTRIB_VOL, ref MainWindow.editorSetting.Default_Touch_Level);
            Bass.BASS_ChannelGetAttribute(MainWindow.hanabiStream, BASSAttribute.BASS_ATTRIB_VOL, ref MainWindow.editorSetting.Default_Hanabi_Level);
            MainWindow.SaveEditorSetting();
            MessageBox.Show(MainWindow.GetLocalizedString("SetVolumeDefaultSuccess"));
        }
    }
}
