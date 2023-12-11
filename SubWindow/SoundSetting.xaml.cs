using System.ComponentModel;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using Un4seen.Bass;
using Timer = System.Timers.Timer;

namespace MajdataEdit;

/// <summary>
///     SoundSetting.xaml 的交互逻辑
/// </summary>
public partial class SoundSetting : Window
{
    private readonly MainWindow MainWindow;
    private readonly Dictionary<Slider, Label> SliderValueBindingMap = new(); // Slider和ValueLabel的绑定关系

    private readonly Timer UpdateLevelTimer = new(1);

    public SoundSetting()
    {
        MainWindow = Application.Current.Windows
            .Cast<Window>()
            .FirstOrDefault(window => window is MainWindow) as MainWindow;

        InitializeComponent();
    }

    private void SoundSettingWindow_Loaded(object sender, RoutedEventArgs e)
    {
        SliderValueBindingMap.Add(BGM_Slider, BGM_Value);
        SliderValueBindingMap.Add(Answer_Slider, Answer_Value);
        SliderValueBindingMap.Add(Judge_Slider, Judge_Value);
        SliderValueBindingMap.Add(Break_Slider, Break_Value);
        SliderValueBindingMap.Add(BreakSlide_Slider, BreakSlide_Value);
        SliderValueBindingMap.Add(Slide_Slider, Slide_Value);
        SliderValueBindingMap.Add(EX_Slider, EX_Value);
        SliderValueBindingMap.Add(Touch_Slider, Touch_Value);
        SliderValueBindingMap.Add(Hanabi_Slider, Hanabi_Value);

        SetSlider(BGM_Slider, MainWindow.bgmStream, MainWindow.trackStartStream, MainWindow.allperfectStream,
            MainWindow.clockStream);
        SetSlider(Answer_Slider, MainWindow.answerStream);
        SetSlider(Judge_Slider, MainWindow.judgeStream);
        SetSlider(Break_Slider, MainWindow.breakStream, MainWindow.judgeBreakStream);
        SetSlider(BreakSlide_Slider, MainWindow.breakSlideStream, MainWindow.judgeBreakSlideStream);
        SetSlider(Slide_Slider, MainWindow.slideStream, MainWindow.breakSlideStartStream);
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
            UpdateProgressBar(BGM_Level, MainWindow.bgmStream, MainWindow.trackStartStream, MainWindow.allperfectStream,
                MainWindow.clockStream);
            UpdateProgressBar(Answer_Level, MainWindow.answerStream);
            UpdateProgressBar(Judge_Level, MainWindow.judgeStream);
            UpdateProgressBar(Break_Level, MainWindow.breakStream, MainWindow.judgeBreakStream);
            UpdateProgressBar(BreakSlide_Level, MainWindow.breakSlideStream, MainWindow.judgeBreakSlideStream);
            UpdateProgressBar(Slide_Level, MainWindow.slideStream, MainWindow.breakSlideStartStream);
            UpdateProgressBar(EX_Level, MainWindow.judgeExStream);
            UpdateProgressBar(Touch_Level, MainWindow.touchStream);
            UpdateProgressBar(Hanabi_Level, MainWindow.hanabiStream, MainWindow.holdRiserStream);
        });
    }

    private void UpdateProgressBar(ProgressBar bar, params int[] channels)
    {
        var values = new double[channels.Length];
        var ampLevel = 0f;
        for (var i = 0; i < channels.Length; i++)
        {
            Bass.BASS_ChannelGetAttribute(channels[i], BASSAttribute.BASS_ATTRIB_VOL, ref ampLevel);
            values[i] = Utils.LevelToDB(Utils.LowWord(Bass.BASS_ChannelGetLevel(channels[i])) * ampLevel, 32768) + 40;
        }

        var value = values.Max();
        if (!double.IsNaN(value) && !double.IsInfinity(value)) bar.Value = value * ampLevel;
        if (double.IsNegativeInfinity(value)) bar.Value = bar.Minimum;
        if (double.IsPositiveInfinity(value)) bar.Value = bar.Maximum;
        if (double.IsNaN(value)) bar.Value -= 1;
    }

    private void SetSlider(Slider slider, params int[] channels)
    {
        var ampLevel = 0f;
        foreach (var channel in channels)
            Bass.BASS_ChannelGetAttribute(channel, BASSAttribute.BASS_ATTRIB_VOL, ref ampLevel);
        slider.Value = ampLevel;
        SliderValueBindingMap[slider].Content = slider.Value.ToString("P0");

        void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            var sld = (Slider)sender;
            foreach (var channel in channels)
                Bass.BASS_ChannelSetAttribute(channel, BASSAttribute.BASS_ATTRIB_VOL, (float)sld.Value);

            SliderValueBindingMap[sld].Content = sld.Value.ToString("P0");
        }

        slider.ValueChanged += Slider_ValueChanged;
    }

    private void SoundSettingWindow_Closing(object sender, CancelEventArgs e)
    {
        UpdateLevelTimer.Stop();
        UpdateLevelTimer.Dispose();
    }

    private void BtnSetDefault_Click(object sender, RoutedEventArgs e)
    {
        Bass.BASS_ChannelGetAttribute(MainWindow.bgmStream, BASSAttribute.BASS_ATTRIB_VOL,
            ref MainWindow.editorSetting.Default_BGM_Level);
        Bass.BASS_ChannelGetAttribute(MainWindow.answerStream, BASSAttribute.BASS_ATTRIB_VOL,
            ref MainWindow.editorSetting.Default_Answer_Level);
        Bass.BASS_ChannelGetAttribute(MainWindow.judgeStream, BASSAttribute.BASS_ATTRIB_VOL,
            ref MainWindow.editorSetting.Default_Judge_Level);
        Bass.BASS_ChannelGetAttribute(MainWindow.breakStream, BASSAttribute.BASS_ATTRIB_VOL,
            ref MainWindow.editorSetting.Default_Break_Level);
        Bass.BASS_ChannelGetAttribute(MainWindow.breakSlideStream, BASSAttribute.BASS_ATTRIB_VOL,
            ref MainWindow.editorSetting.Default_Break_Slide_Level);
        Bass.BASS_ChannelGetAttribute(MainWindow.slideStream, BASSAttribute.BASS_ATTRIB_VOL,
            ref MainWindow.editorSetting.Default_Slide_Level);
        Bass.BASS_ChannelGetAttribute(MainWindow.judgeExStream, BASSAttribute.BASS_ATTRIB_VOL,
            ref MainWindow.editorSetting.Default_Ex_Level);
        Bass.BASS_ChannelGetAttribute(MainWindow.touchStream, BASSAttribute.BASS_ATTRIB_VOL,
            ref MainWindow.editorSetting.Default_Touch_Level);
        Bass.BASS_ChannelGetAttribute(MainWindow.hanabiStream, BASSAttribute.BASS_ATTRIB_VOL,
            ref MainWindow.editorSetting.Default_Hanabi_Level);
        MainWindow.SaveEditorSetting();
        MessageBox.Show(MainWindow.GetLocalizedString("SetVolumeDefaultSuccess"));
    }

    private void BtnSetToDefault_Click(object sender, RoutedEventArgs e)
    {
        BGM_Slider.Value = MainWindow.editorSetting.Default_BGM_Level;
        Answer_Slider.Value = MainWindow.editorSetting.Default_Answer_Level;
        Judge_Slider.Value = MainWindow.editorSetting.Default_Judge_Level;
        Break_Slider.Value = MainWindow.editorSetting.Default_Break_Level;
        BreakSlide_Slider.Value = MainWindow.editorSetting.Default_Break_Slide_Level;
        Slide_Slider.Value = MainWindow.editorSetting.Default_Slide_Level;
        EX_Slider.Value = MainWindow.editorSetting.Default_Ex_Level;
        Touch_Slider.Value = MainWindow.editorSetting.Default_Touch_Level;
        Hanabi_Slider.Value = MainWindow.editorSetting.Default_Hanabi_Level;
    }
}