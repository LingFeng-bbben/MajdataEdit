using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using DiscordRPC;
using MajdataEdit.AutoSaveModule;
using Microsoft.Win32;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Semver;
using Un4seen.Bass;
using Un4seen.Bass.AddOn.Fx;
using WPFLocalizeExtension.Engine;
using WPFLocalizeExtension.Extensions;
using Brush = System.Drawing.Brush;
using Color = System.Drawing.Color;
using DashStyle = System.Drawing.Drawing2D.DashStyle;
using LinearGradientBrush = System.Drawing.Drawing2D.LinearGradientBrush;
using Pen = System.Drawing.Pen;
using PixelFormat = System.Drawing.Imaging.PixelFormat;
using Timer = System.Timers.Timer;

namespace MajdataEdit;

public partial class MainWindow : Window
{
    private const string majSettingFilename = "majSetting.json";
    private const string editorSettingFilename = "EditorSetting.json";
    public static readonly string MAJDATA_VERSION_STRING = $"v{Assembly.GetExecutingAssembly().GetName().Version!.ToString(3)}";
    public static readonly SemVersion MAJDATA_VERSION = SemVersion.Parse(MAJDATA_VERSION_STRING, SemVersionStyles.Any);

    public static string maidataDir = "";

    //float[] wavedBs;
    private readonly short[][] waveRaws = new short[3][];
    public Timer chartChangeTimer = new(1000); // 谱面变更延迟解析]\
    private readonly Timer currentTimeRefreshTimer = new(100);

    public DiscordRpcClient DCRPCclient = new("1068882546932326481");

    private float deltatime = 4f;
    public EditorSetting? editorSetting;

    private bool fumenOverwriteMode; //谱面文本覆盖模式
    private float ghostCusorPositionTime;
    private bool isDrawing;
    private bool isLoading;
    private bool isReplaceConformed;

    private bool isSaved = true;
    private EditorControlMethod lastEditorState;
    private TextSelection? lastFindPosition;

    private double lastMousePointX; //Used for drag scroll

    private int selectedDifficulty = -1;
    private double songLength;

    private SoundSetting soundSetting = new();
    private bool UpdateCheckLock;


    //*UI DRAWING
    private readonly Timer visualEffectRefreshTimer = new(1);

    private WriteableBitmap? WaveBitmap;

    //*TEXTBOX CONTROL
    private string GetRawFumenText()
    {
        var text = new TextRange(FumenContent.Document.ContentStart, FumenContent.Document.ContentEnd).Text!;

        text = text.Replace("\r", "");
        // 亲爱的bbben在这里对text进行了Trim 引发了行位置不正确的BUG 谨此纪念（
        return text;
    }

    private void SetRawFumenText(string content)
    {
        isLoading = true;
        FumenContent.Document.Blocks.Clear();
        if (content == null)
        {
            isLoading = false;
            return;
        }

        var lines = content.Split('\n');
        foreach (var line in lines)
        {
            var paragraph = new Paragraph();
            paragraph.Inlines.Add(line);
            FumenContent.Document.Blocks.Add(paragraph);
        }

        isLoading = false;
    }

    private long GetRawFumenPosition()
    {
        long pos = new TextRange(FumenContent.Document.ContentStart, FumenContent.CaretPosition).Text.Replace("\r", "")
            .Length;
        return pos;
    }

    private void SeekTextFromTime()
    {
        //Console.WriteLine("SeekText");
        var time = Bass.BASS_ChannelBytes2Seconds(bgmStream, Bass.BASS_ChannelGetPosition(bgmStream));
        var timingList = new List<SimaiTimingPoint>();
        timingList.AddRange(SimaiProcess.timinglist);
        var noteList = SimaiProcess.notelist;
        if (SimaiProcess.timinglist.Count <= 0) return;
        timingList.Sort((x, y) => Math.Abs(time - x.time).CompareTo(Math.Abs(time - y.time)));
        var theNote = timingList[0];
        timingList.Clear();
        timingList.AddRange(SimaiProcess.timinglist);
        var indexOfTheNote = timingList.IndexOf(theNote);
        var pointer = FumenContent.Document.Blocks.ToList()[theNote.rawTextPositionY].ContentStart
            .GetPositionAtOffset(theNote.rawTextPositionX);
        FumenContent.Selection.Select(pointer, pointer);
    }

    private void SeekTextFromIndex(int noteGroupIndex)
    {
        if (SimaiProcess.notelist.Count > noteGroupIndex + 1 && noteGroupIndex >= 0)
        {
            var theNote = SimaiProcess.notelist[noteGroupIndex];
            var pointer = FumenContent.Document.Blocks.ToList()[theNote.rawTextPositionY].ContentStart
                .GetPositionAtOffset(theNote.rawTextPositionX);
            FumenContent.Selection.Select(pointer, pointer);
        }
    }

    public void ScrollToFumenContentSelection(int positionX, int positionY)
    {
        // 这玩意用于其他窗口来滚动Scroll 因为涉及到好多变量都是private的
        var pointer = FumenContent.Document.Blocks.ToList()[positionY].ContentStart.GetPositionAtOffset(positionX);
        FumenContent.Focus();
        FumenContent.Selection.Select(pointer, pointer);
        Focus();

        if (Bass.BASS_ChannelIsActive(bgmStream) == BASSActive.BASS_ACTIVE_PLAYING && (bool)FollowPlayCheck.IsChecked!)
            return;
        var time = SimaiProcess.Serialize(GetRawFumenText(), GetRawFumenPosition());
        SetBgmPosition(time);
        //Console.WriteLine("SelectionChanged");
        SimaiProcess.ClearNoteListPlayedState();
        ghostCusorPositionTime = (float)time;
    }

    //*FIND AND REPLACE
    private void Find_icon_MouseDown(object? sender, MouseButtonEventArgs e)
    {
        FindAndScroll();
    }

    private void Replace_icon_MouseDown(object? sender, MouseButtonEventArgs e)
    {
        if (!isReplaceConformed)
        {
            FindAndScroll();
            return;
        }

        if (FumenContent.Selection == lastFindPosition)
        {
            FumenContent.Selection.Text = ReplaceText.Text;
            FindAndScroll();
        }
        else
        {
            isReplaceConformed = false;
        }
    }

    public TextRange? GetTextRangeFromPosition(TextPointer position, string input)
    {
        TextRange? textRange = null;

        while (position != null)
        {
            if (position.CompareTo(FumenContent.Document.ContentEnd) == 0) break;

            if (position.GetPointerContext(LogicalDirection.Forward) == TextPointerContext.Text)
            {
                var textRun = position.GetTextInRun(LogicalDirection.Forward);
                var stringComparison = StringComparison.CurrentCultureIgnoreCase;
                var indexInRun = textRun.IndexOf(input, stringComparison);

                if (indexInRun >= 0)
                {
                    position = position.GetPositionAtOffset(indexInRun);
                    var nextPointer = position.GetPositionAtOffset(input.Length);
                    textRange = new TextRange(position, nextPointer);

                    // If a none-WholeWord match is found, directly terminate the loop.
                    position = position.GetPositionAtOffset(input.Length);
                    break;
                }

                // If a match is not found, go over to the next context position after the "textRun".
                position = position.GetPositionAtOffset(textRun.Length);
            }
            else
            {
                //If the current position doesn't represent a text context position, go to the next context position.
                // This can effectively ignore the formatting or embed element symbols.
                position = position.GetNextContextPosition(LogicalDirection.Forward);
            }
        }

        return textRange;
    }

    public void FindAndScroll()
    {
        var position = GetTextRangeFromPosition(FumenContent.CaretPosition, InputText.Text);
        if (position == null)
        {
            isReplaceConformed = false;
            return;
        }

        FumenContent.Selection.Select(position.Start, position.End);
        lastFindPosition = FumenContent.Selection;
        FumenContent.Focus();
        isReplaceConformed = true;
    }

    //*FILE CONTROL
    private void initFromFile(string path) //file name should not be included in path
    {
        if (soundSetting != null) soundSetting.Close();
        if (editorSetting == null) ReadEditorSetting();

        var useOgg = File.Exists(path + "/track.ogg");

        var audioPath = path + "/track" + (useOgg ? ".ogg" : ".mp3");
        var dataPath = path + "/maidata.txt";
        if (!File.Exists(audioPath))
        {
            MessageBox.Show(GetLocalizedString("NoTrack"), GetLocalizedString("Error"));
            return;
        }

        if (!File.Exists(dataPath))
        {
            MessageBox.Show(GetLocalizedString("NoMaidata_txt"), GetLocalizedString("Error"));
            return;
        }

        maidataDir = path;
        SafeTerminationDetector.Of().ChangePath(maidataDir);
        SetRawFumenText("");
        if (bgmStream != -1024)
        {
            Bass.BASS_ChannelStop(bgmStream);
            Bass.BASS_StreamFree(bgmStream);
        }

        //soundSetting.Close();
        var decodeStream = Bass.BASS_StreamCreateFile(audioPath, 0L, 0L, BASSFlag.BASS_STREAM_DECODE | BASSFlag.BASS_STREAM_PRESCAN);
        bgmStream = BassFx.BASS_FX_TempoCreate(decodeStream, BASSFlag.BASS_FX_FREESOURCE);
        //Bass.BASS_StreamCreateFile(audioPath, 0L, 0L, BASSFlag.BASS_SAMPLE_FLOAT);

        Bass.BASS_ChannelSetAttribute(bgmStream, BASSAttribute.BASS_ATTRIB_VOL, editorSetting!.Default_BGM_Level);
        Bass.BASS_ChannelSetAttribute(trackStartStream, BASSAttribute.BASS_ATTRIB_VOL, editorSetting!.Default_BGM_Level);
        Bass.BASS_ChannelSetAttribute(allperfectStream, BASSAttribute.BASS_ATTRIB_VOL, editorSetting!.Default_BGM_Level);
        Bass.BASS_ChannelSetAttribute(fanfareStream, BASSAttribute.BASS_ATTRIB_VOL, editorSetting!.Default_BGM_Level);
        Bass.BASS_ChannelSetAttribute(clockStream, BASSAttribute.BASS_ATTRIB_VOL, editorSetting!.Default_BGM_Level);
        Bass.BASS_ChannelSetAttribute(answerStream, BASSAttribute.BASS_ATTRIB_VOL, editorSetting!.Default_Answer_Level);
        Bass.BASS_ChannelSetAttribute(judgeStream, BASSAttribute.BASS_ATTRIB_VOL, editorSetting!.Default_Judge_Level);
        Bass.BASS_ChannelSetAttribute(judgeBreakStream, BASSAttribute.BASS_ATTRIB_VOL,
            editorSetting!.Default_Break_Level);
        Bass.BASS_ChannelSetAttribute(judgeBreakSlideStream, BASSAttribute.BASS_ATTRIB_VOL,
            editorSetting!.Default_Break_Slide_Level);
        Bass.BASS_ChannelSetAttribute(slideStream, BASSAttribute.BASS_ATTRIB_VOL, editorSetting!.Default_Slide_Level);
        Bass.BASS_ChannelSetAttribute(breakSlideStartStream, BASSAttribute.BASS_ATTRIB_VOL,
            editorSetting!.Default_Slide_Level);
        Bass.BASS_ChannelSetAttribute(breakStream, BASSAttribute.BASS_ATTRIB_VOL, editorSetting!.Default_Break_Level);
        Bass.BASS_ChannelSetAttribute(breakSlideStream, BASSAttribute.BASS_ATTRIB_VOL,
            editorSetting!.Default_Break_Slide_Level);
        Bass.BASS_ChannelSetAttribute(judgeExStream, BASSAttribute.BASS_ATTRIB_VOL, editorSetting!.Default_Ex_Level);
        Bass.BASS_ChannelSetAttribute(touchStream, BASSAttribute.BASS_ATTRIB_VOL, editorSetting!.Default_Touch_Level);
        Bass.BASS_ChannelSetAttribute(hanabiStream, BASSAttribute.BASS_ATTRIB_VOL, editorSetting!.Default_Hanabi_Level);
        Bass.BASS_ChannelSetAttribute(holdRiserStream, BASSAttribute.BASS_ATTRIB_VOL,
            editorSetting!.Default_Hanabi_Level);
        var info = Bass.BASS_ChannelGetInfo(bgmStream);
        if (info.freq != 44100) MessageBox.Show(GetLocalizedString("Warn44100Hz"), GetLocalizedString("Attention"));
        ReadWaveFromFile();
        SimaiProcess.ClearData();

        if (!SimaiProcess.ReadData(dataPath)) return;


        LevelSelector.SelectedItem = LevelSelector.Items[0];
        ReadSetting();
        SetRawFumenText(SimaiProcess.fumens[selectedDifficulty]);
        SeekTextFromTime();
        SimaiProcess.Serialize(GetRawFumenText());
        FumenContent.Focus();
        DrawWave();

        OffsetTextBox.Text = SimaiProcess.first.ToString();

        Cover.Visibility = Visibility.Collapsed;
        MenuEdit.IsEnabled = true;
        VolumnSetting.IsEnabled = true;
        MenuMuriCheck.IsEnabled = true;
        Menu_ExportRender.IsEnabled = true;
        AutoSaveManager.Of().SetAutoSaveEnable(true);
        SetSavedState(true);
    }

    private void ReadWaveFromFile()
    {
        var useOgg = File.Exists(maidataDir + "/track.ogg");
        var bgmDecode = Bass.BASS_StreamCreateFile(maidataDir + "/track" + (useOgg ? ".ogg" : ".mp3"), 0L, 0L, BASSFlag.BASS_STREAM_DECODE);
        try
        {
            songLength = Bass.BASS_ChannelBytes2Seconds(bgmDecode,
                Bass.BASS_ChannelGetLength(bgmDecode, BASSMode.BASS_POS_BYTE));
/*                int sampleNumber = (int)((songLength * 1000) / (0.02f * 1000));
                wavedBs = new float[sampleNumber];
                for (int i = 0; i < sampleNumber; i++)
                {
                    wavedBs[i] = Bass.BASS_ChannelGetLevels(bgmDecode, 0.02f, BASSLevel.BASS_LEVEL_MONO)[0];
                }*/
            Bass.BASS_StreamFree(bgmDecode);
            var bgmSample = Bass.BASS_SampleLoad(maidataDir + "/track" + (useOgg ? ".ogg" : ".mp3"), 0, 0, 1, BASSFlag.BASS_DEFAULT);
            var bgmInfo = Bass.BASS_SampleGetInfo(bgmSample);
            var freq = bgmInfo.freq;
            var sampleCount = (long)(songLength * freq * 2);
            var bgmRAW = new short[sampleCount];
            Bass.BASS_SampleGetData(bgmSample, bgmRAW);

            waveRaws[0] = new short[sampleCount / 20 + 1];
            for (var i = 0; i < sampleCount; i = i + 20) waveRaws[0][i / 20] = bgmRAW[i];
            waveRaws[1] = new short[sampleCount / 50 + 1];
            for (var i = 0; i < sampleCount; i = i + 50) waveRaws[1][i / 50] = bgmRAW[i];
            waveRaws[2] = new short[sampleCount / 100 + 1];
            for (var i = 0; i < sampleCount; i = i + 100) waveRaws[2][i / 100] = bgmRAW[i];
        }
        catch (Exception e)
        {
            MessageBox.Show("mp3/ogg解码失败。\nMP3/OGG Decode fail.\n" + e.Message + Bass.BASS_ErrorGetCode());
            Bass.BASS_StreamFree(bgmDecode);
            Process.Start("https://github.com/LingFeng-bbben/MajdataEdit/issues/26");
        }
    }

    private void SetSavedState(bool state)
    {
        if (state)
        {
            isSaved = true;
            LevelSelector.IsEnabled = true;
            TheWindow.Title = GetWindowsTitleString(SimaiProcess.title!);
        }
        else
        {
            isSaved = false;
            LevelSelector.IsEnabled = false;
            TheWindow.Title = GetWindowsTitleString(GetLocalizedString("Unsaved") + SimaiProcess.title!);
            AutoSaveManager.Of().SetFileChanged();
        }
    }

    /// <summary>
    ///     Ask the user and save fumen.
    /// </summary>
    /// <returns>Return false if user cancel the action</returns>
    private bool AskSave()
    {
        var result = MessageBox.Show(GetLocalizedString("AskSave"), GetLocalizedString("Warning"),
            MessageBoxButton.YesNoCancel);
        if (result == MessageBoxResult.Yes)
        {
            SaveFumen(true);
            return true;
        }

        if (result == MessageBoxResult.Cancel) return false;
        return true;
    }

    private void SaveFumen(bool writeToDisk = false)
    {
        if (selectedDifficulty == -1) return;
        SimaiProcess.fumens[selectedDifficulty] = GetRawFumenText();
        SimaiProcess.first = float.Parse(OffsetTextBox.Text);
        if (maidataDir == "")
        {
            var saveDialog = new SaveFileDialog
            {
                Filter = "maidata.txt|maidata.txt",
                OverwritePrompt = true
            };
            if ((bool)saveDialog.ShowDialog()!) maidataDir = new FileInfo(saveDialog.FileName).DirectoryName!;
        }

        SimaiProcess.SaveData(maidataDir + "/maidata.bak.txt");
        SaveSetting();
        if (writeToDisk)
        {
            SimaiProcess.SaveData(maidataDir + "/maidata.txt");
            SetSavedState(true);
        }
    }

    private void SaveSetting()
    {
        if (maidataDir == "") return;
        var setting = new MajSetting
        {
            lastEditDiff = selectedDifficulty,
            lastEditTime = Bass.BASS_ChannelBytes2Seconds(bgmStream, Bass.BASS_ChannelGetPosition(bgmStream))
        };
        Bass.BASS_ChannelGetAttribute(bgmStream, BASSAttribute.BASS_ATTRIB_VOL, ref setting.BGM_Level);
        Bass.BASS_ChannelGetAttribute(answerStream, BASSAttribute.BASS_ATTRIB_VOL, ref setting.Answer_Level);
        Bass.BASS_ChannelGetAttribute(judgeStream, BASSAttribute.BASS_ATTRIB_VOL, ref setting.Judge_Level);
        Bass.BASS_ChannelGetAttribute(judgeBreakStream, BASSAttribute.BASS_ATTRIB_VOL, ref setting.Break_Level);
        Bass.BASS_ChannelGetAttribute(breakSlideStream, BASSAttribute.BASS_ATTRIB_VOL, ref setting.Break_Slide_Level);
        Bass.BASS_ChannelGetAttribute(judgeExStream, BASSAttribute.BASS_ATTRIB_VOL, ref setting.Ex_Level);
        Bass.BASS_ChannelGetAttribute(touchStream, BASSAttribute.BASS_ATTRIB_VOL, ref setting.Touch_Level);
        Bass.BASS_ChannelGetAttribute(slideStream, BASSAttribute.BASS_ATTRIB_VOL, ref setting.Slide_Level);
        Bass.BASS_ChannelGetAttribute(hanabiStream, BASSAttribute.BASS_ATTRIB_VOL, ref setting.Hanabi_Level);
        var json = JsonConvert.SerializeObject(setting);
        File.WriteAllText(maidataDir + "/" + majSettingFilename, json);
    }

    private void ReadSetting()
    {
        var path = maidataDir + "/" + majSettingFilename;
        if (!File.Exists(path)) return;
        var setting = JsonConvert.DeserializeObject<MajSetting>(File.ReadAllText(path));
        LevelSelector.SelectedIndex = setting!.lastEditDiff;
        selectedDifficulty = setting.lastEditDiff;
        SetBgmPosition(setting.lastEditTime);
        Bass.BASS_ChannelSetAttribute(bgmStream, BASSAttribute.BASS_ATTRIB_VOL, setting.BGM_Level);
        Bass.BASS_ChannelSetAttribute(trackStartStream, BASSAttribute.BASS_ATTRIB_VOL, setting.BGM_Level);
        Bass.BASS_ChannelSetAttribute(allperfectStream, BASSAttribute.BASS_ATTRIB_VOL, setting.BGM_Level);
        Bass.BASS_ChannelSetAttribute(fanfareStream, BASSAttribute.BASS_ATTRIB_VOL, setting.BGM_Level);
        Bass.BASS_ChannelSetAttribute(clockStream, BASSAttribute.BASS_ATTRIB_VOL, setting.BGM_Level);
        Bass.BASS_ChannelSetAttribute(answerStream, BASSAttribute.BASS_ATTRIB_VOL, setting.Answer_Level);
        Bass.BASS_ChannelSetAttribute(judgeStream, BASSAttribute.BASS_ATTRIB_VOL, setting.Judge_Level);
        Bass.BASS_ChannelSetAttribute(judgeBreakStream, BASSAttribute.BASS_ATTRIB_VOL, setting.Break_Level);
        Bass.BASS_ChannelSetAttribute(judgeBreakSlideStream, BASSAttribute.BASS_ATTRIB_VOL, setting.Break_Slide_Level);
        Bass.BASS_ChannelSetAttribute(slideStream, BASSAttribute.BASS_ATTRIB_VOL, setting.Slide_Level);
        Bass.BASS_ChannelSetAttribute(breakSlideStartStream, BASSAttribute.BASS_ATTRIB_VOL, setting.Slide_Level);
        Bass.BASS_ChannelSetAttribute(breakStream, BASSAttribute.BASS_ATTRIB_VOL, setting.Break_Level);
        Bass.BASS_ChannelSetAttribute(breakSlideStream, BASSAttribute.BASS_ATTRIB_VOL, setting.Break_Slide_Level);
        Bass.BASS_ChannelSetAttribute(judgeExStream, BASSAttribute.BASS_ATTRIB_VOL, setting.Ex_Level);
        Bass.BASS_ChannelSetAttribute(touchStream, BASSAttribute.BASS_ATTRIB_VOL, setting.Touch_Level);
        Bass.BASS_ChannelSetAttribute(hanabiStream, BASSAttribute.BASS_ATTRIB_VOL, setting.Hanabi_Level);
        Bass.BASS_ChannelSetAttribute(holdRiserStream, BASSAttribute.BASS_ATTRIB_VOL, setting.Hanabi_Level);

        SaveSetting(); // 覆盖旧版本setting
    }

    private void CreateNewFumen(string path)
    {
        if (File.Exists(path + "/maidata.txt"))
            MessageBox.Show(GetLocalizedString("MaidataExist"));
        else
            File.WriteAllText(path + "/maidata.txt",
                "&title=" + GetLocalizedString("SetTitle") + "\n" +
                "&artist=" + GetLocalizedString("SetArtist") + "\n" +
                "&des=" + GetLocalizedString("SetDes") + "\n" +
                "&first=0\n");
    }

    private void CreateEditorSetting()
    {
        editorSetting = new EditorSetting
        {
            RenderMode =
            RenderOptions.ProcessRenderMode == RenderMode.SoftwareOnly ? 1 : 0 // 使用命令行指定强制软件渲染时，同步修改配置值
        };

        File.WriteAllText(editorSettingFilename, JsonConvert.SerializeObject(editorSetting, Formatting.Indented));

        var esp = new EditorSettingPanel(true)
        {
            Owner = this
        };
        esp.ShowDialog();
    }

    private void ReadEditorSetting()
    {
        if (!File.Exists(editorSettingFilename)) CreateEditorSetting();
        var json = File.ReadAllText(editorSettingFilename);
        editorSetting = JsonConvert.DeserializeObject<EditorSetting>(json)!;

        if (RenderOptions.ProcessRenderMode != RenderMode.SoftwareOnly)
            //如果没有通过命令行预先指定渲染模式，则使用设置项的渲染模式
            RenderOptions.ProcessRenderMode =
                editorSetting.RenderMode == 0 ? RenderMode.Default : RenderMode.SoftwareOnly;
        else
            //如果通过命令行指定了使用软件渲染模式，则覆盖设置项
            editorSetting.RenderMode = 1;

        LocalizeDictionary.Instance.Culture = new CultureInfo(editorSetting.Language);
        AddGesture(editorSetting.PlayPauseKey, "PlayAndPause");
        AddGesture(editorSetting.PlayStopKey, "StopPlaying");
        AddGesture(editorSetting.SaveKey, "SaveFile");
        AddGesture(editorSetting.SendViewerKey, "SendToView");
        AddGesture(editorSetting.IncreasePlaybackSpeedKey, "IncreasePlaybackSpeed");
        AddGesture(editorSetting.DecreasePlaybackSpeedKey, "DecreasePlaybackSpeed");
        AddGesture("Ctrl+f", "Find");
        AddGesture(editorSetting.MirrorLeftRightKey, "MirrorLR");
        AddGesture(editorSetting.MirrorUpDownKey, "MirrorUD");
        AddGesture(editorSetting.Mirror180Key, "Mirror180");
        AddGesture(editorSetting.Mirror45Key, "Mirror45");
        AddGesture(editorSetting.MirrorCcw45Key, "MirrorCcw45");
        FumenContent.FontSize = editorSetting.FontSize;

        ViewerCover.Content = editorSetting.backgroundCover.ToString();
        ViewerSpeed.Content = editorSetting.playSpeed.ToString("F1"); // 转化为形如"7.0", "9.5"这样的速度
        ViewerTouchSpeed.Content = editorSetting.touchSpeed.ToString("F1");

        chartChangeTimer.Interval = editorSetting.ChartRefreshDelay; // 设置更新延迟

        SaveEditorSetting(); // 覆盖旧版本setting
    }

    public void SaveEditorSetting()
    {
        File.WriteAllText(editorSettingFilename, JsonConvert.SerializeObject(editorSetting, Formatting.Indented));
    }

    private void AddGesture(string keyGusture, string command)
    {
        var gesture = (InputGesture) new KeyGestureConverter().ConvertFromString(keyGusture)!;
        var inputBinding = new InputBinding((ICommand)FumenContent.Resources[command], gesture);
        FumenContent.InputBindings.Add(inputBinding);
    }

    // This update very freqently to Draw FFT wave.
    private void VisualEffectRefreshTimer_Elapsed(object? sender, ElapsedEventArgs e)
    {
        try
        {
            DrawFFT();
            DrawWave();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }
    }

    // 谱面变更延迟解析
    private void ChartChangeTimer_Elapsed(object? sender, ElapsedEventArgs e)
    {
        Console.WriteLine("TextChanged");
        Dispatcher.Invoke(
            delegate
            {
                SimaiProcess.Serialize(GetRawFumenText(), GetRawFumenPosition());
                DrawWave();
            }
        );
    }

    private void DrawFFT()
    {
        Dispatcher.InvokeAsync(() =>
        {
            //Scroll WaveView
            var currentTime = Bass.BASS_ChannelBytes2Seconds(bgmStream, Bass.BASS_ChannelGetPosition(bgmStream));
            //MusicWave.Margin = new Thickness(-currentTime / sampleTime * zoominPower, Margin.Left, MusicWave.Margin.Right, Margin.Bottom);
            //MusicWaveCusor.Margin = new Thickness(-currentTime / sampleTime * zoominPower, Margin.Left, MusicWave.Margin.Right, Margin.Bottom);

            var writableBitmap = new WriteableBitmap(255, 255, 72, 72, PixelFormats.Pbgra32, null);
            FFTImage.Source = writableBitmap;
            writableBitmap.Lock();
            var backBitmap = new Bitmap(255, 255, writableBitmap.BackBufferStride,
                PixelFormat.Format32bppArgb, writableBitmap.BackBuffer);

            var graphics = Graphics.FromImage(backBitmap);
            graphics.Clear(Color.Transparent);

            var fft = new float[1024];
            Bass.BASS_ChannelGetData(bgmStream, fft, (int)BASSData.BASS_DATA_FFT1024);
            var points = new PointF[1024];
            for (var i = 0; i < fft.Length; i++)
                points[i] = new PointF((float)Math.Log10(i + 1) * 100f, 240 - fft[i] * 256); //semilog

            graphics.DrawCurve(new Pen(Color.LightSkyBlue, 1), points);


            //no please
            /*
            var isSuccess = new Visuals().CreateSpectrumWave(bgmStream, graphics, new System.Drawing.Rectangle(0, 0, 255, 255),
                System.Drawing.Color.White, System.Drawing.Color.Red,
                System.Drawing.Color.Black, 1,
                false, false, false);
            Console.WriteLine(isSuccess);
            */
            graphics.Flush();
            graphics.Dispose();
            backBitmap.Dispose();

            writableBitmap.AddDirtyRect(new Int32Rect(0, 0, 255, 255));
            writableBitmap.Unlock();
        });
    }

    private void InitWave()
    {
        var width = (int)Width - 2;
        var height = (int)MusicWave.Height;
        WaveBitmap = new WriteableBitmap(width, height, 72, 72, PixelFormats.Pbgra32, null);
        MusicWave.Source = WaveBitmap;
    }

    private void DrawWave()
    {
        if (isDrawing) return;
        if (WaveBitmap == null) return;

        Dispatcher.Invoke(() =>
        {
            isDrawing = true;
            var width = WaveBitmap.PixelWidth;
            var height = WaveBitmap.PixelHeight;

            if (waveRaws[0] == null)
            {
                isDrawing = false;
                return;
            }

            WaveBitmap.Lock();

            //the process starts
            var backBitmap = new Bitmap(width, height, WaveBitmap.BackBufferStride,
                PixelFormat.Format32bppArgb, WaveBitmap.BackBuffer);
            var graphics = Graphics.FromImage(backBitmap);
            var currentTime = Bass.BASS_ChannelBytes2Seconds(bgmStream, Bass.BASS_ChannelGetPosition(bgmStream));

            graphics.Clear(Color.FromArgb(100, 0, 0, 0));

            var resample = (int)deltatime - 1;
            if (resample > 1 && resample <= 3) resample = 1;
            if (resample > 3) resample = 2;
            var waveLevels = waveRaws[resample];

            var step = songLength / waveLevels.Length;
            var startindex = (int)((currentTime - deltatime) / step);
            var stopindex = (int)((currentTime + deltatime) / step);
            var linewidth = backBitmap.Width / (float)(stopindex - startindex);
            var pen = new Pen(Color.Green, linewidth);
            var points = new List<PointF>();
            for (var i = startindex; i < stopindex; i = i + 1)
            {
                if (i < 0) i = 0;
                if (i >= waveLevels.Length - 1) break;

                var x = (i - startindex) * linewidth;
                var y = waveLevels[i] / 65535f * height + height / 2;

                points.Add(new PointF(x, y));
            }

            graphics.DrawLines(pen, points.ToArray());

            //Draw Bpm lines
            var lastbpm = -1f;
            var bpmChangeTimes = new List<double>(); //在什么时间变成什么值
            var bpmChangeValues = new List<float>();
            bpmChangeTimes.Clear();
            bpmChangeValues.Clear();
            foreach (var timing in SimaiProcess.timinglist)
                if (timing.currentBpm != lastbpm)
                {
                    bpmChangeTimes.Add(timing.time);
                    bpmChangeValues.Add(timing.currentBpm);
                    lastbpm = timing.currentBpm;
                }

            bpmChangeTimes.Add(Bass.BASS_ChannelBytes2Seconds(bgmStream, Bass.BASS_ChannelGetLength(bgmStream)));

            double time = SimaiProcess.first;
            var signature = 4; //预留拍号
            var currentBeat = 1;
            var timePerBeat = 0d;
            pen = new Pen(Color.Yellow, 1);
            var strongBeat = new List<double>();
            var weakBeat = new List<double>();
            for (var i = 1; i < bpmChangeTimes.Count; i++)
            {
                while (time - bpmChangeTimes[i] < -0.05) //在那个时间之前都是之前的bpm
                {
                    if (currentBeat > signature) currentBeat = 1;
                    timePerBeat = 1d / (bpmChangeValues[i - 1] / 60d);
                    if (currentBeat == 1)
                        strongBeat.Add(time);
                    else
                        weakBeat.Add(time);
                    currentBeat++;
                    time += timePerBeat;
                }

                time = bpmChangeTimes[i];
                currentBeat = 1;
            }

            foreach (var btime in strongBeat)
            {
                if (btime - currentTime > deltatime) continue;
                var x = ((float)(btime / step) - startindex) * linewidth;
                graphics.DrawLine(pen, x, 0, x, 75);
            }

            foreach (var btime in weakBeat)
            {
                if (btime - currentTime > deltatime) continue;
                var x = ((float)(btime / step) - startindex) * linewidth;
                graphics.DrawLine(pen, x, 0, x, 15);
            }

            //Draw timing lines
            pen = new Pen(Color.White, 1);
            foreach (var note in SimaiProcess.timinglist)
            {
                if (note == null) break;
                if (note.time - currentTime > deltatime) continue;
                var x = ((float)(note.time / step) - startindex) * linewidth;
                graphics.DrawLine(pen, x, 60, x, 75);
            }

            //Draw notes                    
            foreach (var note in SimaiProcess.notelist)
            {
                if (note == null) break;
                if (note.time - currentTime > deltatime) continue;
                var notes = note.getNotes();
                var isEach = notes.Count(o => !o.isSlideNoHead) > 1;

                var x = ((float)(note.time / step) - startindex) * linewidth;

                foreach (var noteD in notes)
                {
                    var y = noteD.startPosition * 6.875f + 8f; //与键位有关

                    if (noteD.isHanabi)
                    {
                        var xDeltaHanabi = (float)(1f / step) * linewidth; //Hanabi is 1s due to frame analyze
                        var rectangleF = new RectangleF(x, 0, xDeltaHanabi, 75);
                        if (noteD.noteType == SimaiNoteType.TouchHold)
                            rectangleF.X += (float)(noteD.holdTime / step) * linewidth;
                        var gradientBrush = new LinearGradientBrush(
                            rectangleF,
                            Color.FromArgb(100, 255, 0, 0),
                            Color.FromArgb(0, 255, 0, 0),
                            LinearGradientMode.Horizontal
                        );
                        graphics.FillRectangle(gradientBrush, rectangleF);
                    }

                    if (noteD.noteType == SimaiNoteType.Tap)
                    {
                        if (noteD.isForceStar)
                        {
                            pen.Width = 3;
                            if (noteD.isBreak)
                                pen.Color = Color.OrangeRed;
                            else if (isEach)
                                pen.Color = Color.Gold;
                            else
                                pen.Color = Color.DeepSkyBlue;
                            Brush brush = new SolidBrush(pen.Color);
                            graphics.DrawString("*", new Font("Consolas", 12, System.Drawing.FontStyle.Bold), brush,
                                new PointF(x - 7f, y - 7f));
                        }
                        else
                        {
                            pen.Width = 2;
                            if (noteD.isBreak)
                                pen.Color = Color.OrangeRed;
                            else if (isEach)
                                pen.Color = Color.Gold;
                            else
                                pen.Color = Color.LightPink;
                            graphics.DrawEllipse(pen, x - 2.5f, y - 2.5f, 5, 5);
                        }
                    }

                    if (noteD.noteType == SimaiNoteType.Touch)
                    {
                        pen.Width = 2;
                        pen.Color = isEach ? Color.Gold : Color.DeepSkyBlue;
                        graphics.DrawRectangle(pen, x - 2.5f, y - 2.5f, 5, 5);
                    }

                    if (noteD.noteType == SimaiNoteType.Hold)
                    {
                        pen.Width = 3;
                        if (noteD.isBreak)
                            pen.Color = Color.OrangeRed;
                        else if (isEach)
                            pen.Color = Color.Gold;
                        else
                            pen.Color = Color.LightPink;

                        var xRight = x + (float)(noteD.holdTime / step) * linewidth;
                        if (xRight - x < 1f) xRight = x + 5;
                        graphics.DrawLine(pen, x, y, xRight, y);
                    }

                    if (noteD.noteType == SimaiNoteType.TouchHold)
                    {
                        pen.Width = 3;
                        var xDelta = (float)(noteD.holdTime / step) * linewidth / 4f;
                        //Console.WriteLine("HoldPixel"+ xDelta);
                        if (xDelta < 1f) xDelta = 1;

                        pen.Color = Color.FromArgb(200, 255, 75, 0);
                        graphics.DrawLine(pen, x, y, x + xDelta * 4f, y);
                        pen.Color = Color.FromArgb(200, 255, 241, 0);
                        graphics.DrawLine(pen, x, y, x + xDelta * 3f, y);
                        pen.Color = Color.FromArgb(200, 2, 165, 89);
                        graphics.DrawLine(pen, x, y, x + xDelta * 2f, y);
                        pen.Color = Color.FromArgb(200, 0, 140, 254);
                        graphics.DrawLine(pen, x, y, x + xDelta, y);
                    }

                    if (noteD.noteType == SimaiNoteType.Slide)
                    {
                        pen.Width = 3;
                        if (!noteD.isSlideNoHead)
                        {
                            if (noteD.isBreak)
                                pen.Color = Color.OrangeRed;
                            else if (isEach)
                                pen.Color = Color.Gold;
                            else
                                pen.Color = Color.DeepSkyBlue;
                            Brush brush = new SolidBrush(pen.Color);
                            graphics.DrawString("*", new Font("Consolas", 12, System.Drawing.FontStyle.Bold), brush,
                                new PointF(x - 7f, y - 7f));
                        }

                        if (noteD.isSlideBreak)
                            pen.Color = Color.OrangeRed;
                        else if (notes.Count(o => o.noteType == SimaiNoteType.Slide) >= 2)
                            pen.Color = Color.Gold;
                        else
                            pen.Color = Color.SkyBlue;
                        pen.DashStyle = DashStyle.Dot;
                        var xSlide = (float)(noteD.slideStartTime / step - startindex) * linewidth;
                        var xSlideRight = (float)(noteD.slideTime / step) * linewidth + xSlide;
                        graphics.DrawLine(pen, xSlide, y, xSlideRight, y);
                        pen.DashStyle = DashStyle.Solid;
                    }
                }
            }

            if (playStartTime - currentTime <= deltatime)
            {
                //Draw play Start time
                pen = new Pen(Color.Red, 5);
                var x1 = (float)(playStartTime / step - startindex) * linewidth;
                PointF[] tranglePoints = { new(x1 - 2, 0), new(x1 + 2, 0), new(x1, 3.46f) };
                graphics.DrawPolygon(pen, tranglePoints);
            }

            if (ghostCusorPositionTime - currentTime <= deltatime)
            {
                //Draw ghost cusor
                pen = new Pen(Color.Orange, 5);
                var x2 = (float)(ghostCusorPositionTime / step - startindex) * linewidth;
                PointF[] tranglePoints2 = { new(x2 - 2, 0), new(x2 + 2, 0), new(x2, 3.46f) };
                graphics.DrawPolygon(pen, tranglePoints2);
            }

            graphics.Flush();
            graphics.Dispose();
            backBitmap.Dispose();

            //MusicWave.Width = waveLevels.Length * zoominPower;
            WaveBitmap.AddDirtyRect(new Int32Rect(0, 0, WaveBitmap.PixelWidth, WaveBitmap.PixelHeight));
            WaveBitmap.Unlock();
            isDrawing = false;
        });
    }

    // This update less frequently. set the time text.
    private void CurrentTimeRefreshTimer_Elapsed(object? sender, ElapsedEventArgs e)
    {
        UpdateTimeDisplay();
    }

    private void UpdateTimeDisplay()
    {
        var currentPlayTime = Bass.BASS_ChannelBytes2Seconds(bgmStream, Bass.BASS_ChannelGetPosition(bgmStream));
        var minute = (int)currentPlayTime / 60;
        double second = (int)(currentPlayTime - 60 * minute);
        Dispatcher.Invoke(() => { TimeLabel.Content = string.Format("{0}:{1:00}", minute, second); });
    }

    private void ScrollWave(double delta)
    {
        if (Bass.BASS_ChannelIsActive(bgmStream) == BASSActive.BASS_ACTIVE_PLAYING)
            TogglePause();
        delta = delta * deltatime / (Width / 2);
        var time = Bass.BASS_ChannelBytes2Seconds(bgmStream, Bass.BASS_ChannelGetPosition(bgmStream));
        SetBgmPosition(time + delta);
        SimaiProcess.ClearNoteListPlayedState();
        SeekTextFromTime();
        Task.Run(() => DrawWave());
    }

    public static string GetLocalizedString(string key, string resourceFileName = "Langs", bool addSpaceAfter = false)
    {

        // Build up the fully-qualified name of the key

        var assemblyName = Assembly.GetExecutingAssembly().GetName().Name;
        var fullKey = assemblyName + ":" + resourceFileName + ":" + key;
        var locExtension = new LocExtension(fullKey);
        locExtension.ResolveLocalizedValue(out string? localizedString);

        // Add a space to the end, if requested
        if (addSpaceAfter) localizedString += " ";

        return localizedString ?? key;
    }

    private void TogglePlay(PlayMethod playMethod = PlayMethod.Normal)
    {
        if (Op_Button.IsEnabled == false) return;

        if (lastEditorState == EditorControlMethod.Start || playMethod != PlayMethod.Normal)
            if (!sendRequestStop())
                return;

        FumenContent.Focus();
        SaveFumen();
        if (CheckAndStartView()) return;
        Op_Button.IsEnabled = false;
        isPlaying = true;
        isPlan2Stop = false;

        PlayAndPauseButton.Content = "  ▌▌ ";
        var CusorTime = SimaiProcess.Serialize(GetRawFumenText(), GetRawFumenPosition()); //scan first

        //TODO: Moeying改一下你的generateSoundEffect然后把下面这行删了
        var isOpIncluded = playMethod == PlayMethod.Normal ? false : true;

        var startAt = DateTime.Now;
        switch (playMethod)
        {
            case PlayMethod.Record:
                Bass.BASS_ChannelSetPosition(bgmStream, 0);
                startAt = DateTime.Now.AddSeconds(5d);
                //TODO: i18n
                MessageBox.Show(GetLocalizedString("AskRender"), GetLocalizedString("Attention"));
                InternalSwitchWindow(false);
                generateSoundEffectList(0.0, isOpIncluded);
                var task = new Task(() => renderSoundEffect(5d));
                try
                {
                    task.Start();
                    task.Wait();
                }
                catch (AggregateException)
                {
                    MessageBox.Show(task.Exception!.InnerException!.Message + "\n" +
                                    task.Exception.InnerException.StackTrace);
                    return;
                }

                if (!sendRequestRun(startAt, playMethod)) return;
                break;
            case PlayMethod.Op:
                generateSoundEffectList(0.0, isOpIncluded);
                InternalSwitchWindow(false);
                Bass.BASS_ChannelSetPosition(bgmStream, 0);
                startAt = DateTime.Now.AddSeconds(5d);
                Bass.BASS_ChannelPlay(trackStartStream, true);
                Task.Run(() =>
                {
                    if (!sendRequestRun(startAt, playMethod)) return;
                    while (DateTime.Now.Ticks < startAt.Ticks)
                        if (lastEditorState != EditorControlMethod.Start)
                            return;
                    Dispatcher.Invoke(() =>
                    {
                        playStartTime =
                            Bass.BASS_ChannelBytes2Seconds(bgmStream, Bass.BASS_ChannelGetPosition(bgmStream));
                        SimaiProcess.ClearNoteListPlayedState();
                        StartSELoop();
                        //soundEffectTimer.Start();
                        waveStopMonitorTimer.Start();
                        visualEffectRefreshTimer.Start();
                        Bass.BASS_ChannelPlay(bgmStream, false);
                    });
                });
                break;
            case PlayMethod.Normal:
                playStartTime = Bass.BASS_ChannelBytes2Seconds(bgmStream, Bass.BASS_ChannelGetPosition(bgmStream));
                generateSoundEffectList(playStartTime, isOpIncluded);
                SimaiProcess.ClearNoteListPlayedState();
                StartSELoop();
                //soundEffectTimer.Start();
                waveStopMonitorTimer.Start();
                visualEffectRefreshTimer.Start();
                startAt = DateTime.Now;
                Bass.BASS_ChannelPlay(bgmStream, false);
                Task.Run(() =>
                {
                    if (lastEditorState == EditorControlMethod.Pause)
                    {
                        if (!sendRequestContinue(startAt)) return;
                    }
                    else
                    {
                        if (!sendRequestRun(startAt, playMethod)) return;
                    }
                });
                break;
        }

        ghostCusorPositionTime = (float)CusorTime;
        DrawWave();
    }

    private void TogglePause()
    {
        Op_Button.IsEnabled = true;
        isPlaying = false;
        isPlan2Stop = false;

        FumenContent.Focus();
        PlayAndPauseButton.Content = "▶";
        Bass.BASS_ChannelStop(bgmStream);
        Bass.BASS_ChannelStop(holdRiserStream);
        //soundEffectTimer.Stop();
        waveStopMonitorTimer.Stop();
        visualEffectRefreshTimer.Stop();
        sendRequestPause();
        DrawWave();
    }

    private void ToggleStop()
    {
        Op_Button.IsEnabled = true;
        isPlaying = false;
        isPlan2Stop = false;

        FumenContent.Focus();
        PlayAndPauseButton.Content = "▶";
        Bass.BASS_ChannelStop(bgmStream);
        Bass.BASS_ChannelStop(holdRiserStream);
        //soundEffectTimer.Stop();
        waveStopMonitorTimer.Stop();
        visualEffectRefreshTimer.Stop();
        sendRequestStop();
        Bass.BASS_ChannelSetPosition(bgmStream, playStartTime);
        DrawWave();
    }

    private void TogglePlayAndPause(PlayMethod playMethod = PlayMethod.Normal)
    {
        if (isPlaying)
            TogglePause();
        else
            TogglePlay(playMethod);
    }

    private void TogglePlayAndStop(PlayMethod playMethod = PlayMethod.Normal)
    {
        if (isPlaying)
            ToggleStop();
        else
            TogglePlay(playMethod);
    }

    private void SetPlaybackSpeed(float speed)
    {
        var scale = (speed - 1) * 100f;
        Bass.BASS_ChannelSetAttribute(bgmStream, BASSAttribute.BASS_ATTRIB_TEMPO, scale);
    }

    private float GetPlaybackSpeed()
    {
        var speed = 0f;
        Bass.BASS_ChannelGetAttribute(bgmStream, BASSAttribute.BASS_ATTRIB_TEMPO, ref speed);
        return speed / 100f + 1f;
    }

    private void SetBgmPosition(double time)
    {
        if (lastEditorState == EditorControlMethod.Pause) sendRequestStop();
        Bass.BASS_ChannelSetPosition(bgmStream, time);
    }


    //*VIEW COMMUNICATION
    private bool sendRequestStop()
    {
        var requestStop = new EditRequestjson
        {
            control = EditorControlMethod.Stop
        };
        var json = JsonConvert.SerializeObject(requestStop);
        var response = WebControl.RequestPOST("http://localhost:8013/", json);
        if (response == "ERROR")
        {
            MessageBox.Show(GetLocalizedString("PortClear"));
            return false;
        }

        lastEditorState = EditorControlMethod.Stop;
        return true;
    }

    private bool sendRequestPause()
    {
        var requestStop = new EditRequestjson
        {
            control = EditorControlMethod.Pause
        };
        var json = JsonConvert.SerializeObject(requestStop);
        var response = WebControl.RequestPOST("http://localhost:8013/", json);
        if (response == "ERROR")
        {
            MessageBox.Show(GetLocalizedString("PortClear"));
            return false;
        }

        lastEditorState = EditorControlMethod.Pause;
        return true;
    }

    private bool sendRequestContinue(DateTime StartAt)
    {
        var request = new EditRequestjson
        {
            control = EditorControlMethod.Continue,
            startAt = StartAt.Ticks,
            startTime = (float)Bass.BASS_ChannelBytes2Seconds(bgmStream, Bass.BASS_ChannelGetPosition(bgmStream)),
            audioSpeed = GetPlaybackSpeed()
        };
        var json = JsonConvert.SerializeObject(request);
        var response = WebControl.RequestPOST("http://localhost:8013/", json);
        if (response == "ERROR")
        {
            MessageBox.Show(GetLocalizedString("PortClear"));
            return false;
        }

        lastEditorState = EditorControlMethod.Start;
        return true;
    }

    private bool sendRequestRun(DateTime StartAt, PlayMethod playMethod)
    {
        var jsonStruct = new Majson();
        foreach (var note in SimaiProcess.notelist)
        {
            note.noteList = note.getNotes();
            jsonStruct.timingList.Add(note);
        }

        jsonStruct.title = SimaiProcess.title!;
        jsonStruct.artist = SimaiProcess.artist!;
        jsonStruct.level = SimaiProcess.levels[selectedDifficulty];
        jsonStruct.designer = SimaiProcess.designer!;
        jsonStruct.difficulty = SimaiProcess.GetDifficultyText(selectedDifficulty);
        jsonStruct.diffNum = selectedDifficulty;

        var json = JsonConvert.SerializeObject(jsonStruct);
        var path = maidataDir + "/majdata.json";
        File.WriteAllText(path, json);

        var request = new EditRequestjson();
        if (playMethod == PlayMethod.Op)
            request.control = EditorControlMethod.OpStart;
        else if (playMethod == PlayMethod.Normal)
            request.control = EditorControlMethod.Start;
        else
            request.control = EditorControlMethod.Record;

        Dispatcher.Invoke(() =>
        {
            request.jsonPath = path;
            request.startAt = StartAt.Ticks;
            request.startTime =
                (float)Bass.BASS_ChannelBytes2Seconds(bgmStream, Bass.BASS_ChannelGetPosition(bgmStream));
            // request.playSpeed = float.Parse(ViewerSpeed.Text);
            // 将maimaiDX速度换算为View中的单位速度 MajSpeed = 107.25 / (71.4184491 * (MaiSpeed + 0.9975) ^ -0.985558604)
            request.noteSpeed = editorSetting!.playSpeed;
            request.touchSpeed = editorSetting!.touchSpeed;
            request.backgroundCover = editorSetting!.backgroundCover;
            request.comboStatusType = editorSetting!.comboStatusType;
            request.audioSpeed = GetPlaybackSpeed();
            request.smoothSlideAnime = editorSetting!.SmoothSlideAnime;
        });

        json = JsonConvert.SerializeObject(request);
        var response = WebControl.RequestPOST("http://localhost:8013/", json);
        if (response == "ERROR")
        {
            MessageBox.Show(GetLocalizedString("PortClear"));
            return false;
        }

        lastEditorState = EditorControlMethod.Start;
        return true;
    }

    [DllImport("user32.dll")]
    public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

    [DllImport("user32.dll", EntryPoint = "MoveWindow")]
    public static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);

    [DllImport("user32.dll")]
    public static extern bool ShowWindow(IntPtr hwnd, int nCmdShow);

    [DllImport("user32.dll")]
    public static extern bool SwitchToThisWindow(IntPtr hWnd, bool fAltTab);

    private bool CheckAndStartView()
    {
        if (Process.GetProcessesByName("MajdataView").Length == 0 && Process.GetProcessesByName("Unity").Length == 0)
        {
            var viewProcess = Process.Start("MajdataView.exe");
            var setWindowPosTimer = new Timer(2000)
            {
                AutoReset = false
            };
            setWindowPosTimer.Elapsed += SetWindowPosTimer_Elapsed;
            setWindowPosTimer.Start();
            return true;
        }

        return false;
    }

    private string GetViewerWorkingDirectory()
    {
        return Environment.CurrentDirectory + "/MajdataView_Data/StreamingAssets";
        /*string tempPath = "";
        Process baseProc;
        Process[] viewProcs;
        viewProcs = Process.GetProcessesByName("MajdataView");
        // Prioritize Majdata First
        if (viewProcs.Length > 0)
        {
            baseProc = viewProcs.First();
            string pwd;
            pwd = baseProc.StartInfo.WorkingDirectory.TrimEnd('/');
            if (pwd.Length == 0) pwd = ".";
            tempPath = pwd + "/MajdataView_Data/StreamingAssets";
        }
        else
        {
            viewProcs = Process.GetProcessesByName("Unity");
        }
        if (viewProcs.Length <= 0)
            throw new Exception("Unable to find MajdataView instance!");

        return (tempPath.Length == 0) ?
            Environment.CurrentDirectory + "/SFX" :
            tempPath;*/
    }

    private void InternalSwitchWindow(bool moveToPlace = true)
    {
        var windowPtr = FindWindow(null, "MajdataView");
        //var thisWindow = FindWindow(null, this.Title);
        ShowWindow(windowPtr, 5); //还原窗口
        SwitchToThisWindow(windowPtr, true);
        //SwitchToThisWindow(thisWindow, true);
        if (moveToPlace) InternalMoveWindow();
    }

    private void InternalMoveWindow()
    {
        var windowPtr = FindWindow(null, "MajdataView");
        var source = PresentationSource.FromVisual(this);

        double dpiX = 1, dpiY = 1;
        if (source != null)
        {
            dpiX = 96.0 * source.CompositionTarget.TransformToDevice.M11;
            dpiY = 96.0 * source.CompositionTarget.TransformToDevice.M22;
        }

        //Console.WriteLine(dpiX+" "+dpiY);
        dpiX /= 96d;
        dpiY /= 96d;

        var Height = this.Height * dpiY;
        var Left = this.Left * dpiX;
        var Top = this.Top * dpiY;
        MoveWindow(windowPtr,
            (int)(Left - Height + 20),
            (int)Top,
            (int)Height - 20,
            (int)Height, true);
    }

    private void SetWindowGoldenPosition()
    {
        // 属于你的独享黄金位置
        var ScreenWidth = SystemParameters.PrimaryScreenWidth;
        var ScreenHeight = SystemParameters.PrimaryScreenHeight;

        Left = (ScreenWidth - Width + Height) / 2 - 10;
        Top = (ScreenHeight - Height) / 2;
    }

    private void SwitchFumenOverwriteMode()
    {
        fumenOverwriteMode = !fumenOverwriteMode;

        //修改覆盖模式启用状态
        // fetch TextEditor from FumenContent
        var textEditorProperty =
            typeof(TextBox).GetProperty("TextEditor", BindingFlags.NonPublic | BindingFlags.Instance);
        var textEditor = textEditorProperty!.GetValue(FumenContent, null);

        // set _OvertypeMode on the TextEditor
        var overtypeModeProperty = textEditor!.GetType()
            .GetProperty("_OvertypeMode", BindingFlags.NonPublic | BindingFlags.Instance)!;
        overtypeModeProperty!.SetValue(textEditor, fumenOverwriteMode, null);

        //修改提示弹窗可见性
        OverrideModeTipsPopup.Visibility = fumenOverwriteMode ? Visibility.Visible : Visibility.Collapsed;
    }

    private void CheckUpdate(bool onStart = false)
    {
        if (UpdateCheckLock) return;
        UpdateCheckLock = true;

        #region 子函数

        SemVersion oldVersionCompatible(string versionString)
        {
            var result = SemVersion.Parse("v0.0.0", SemVersionStyles.Any);
            try
            {
                // 尝试解析版本号，解析失败说明是旧版本格式
                result = SemVersion.Parse(versionString, SemVersionStyles.Any);
            }
            catch (FormatException)
            {
                if (versionString.Contains("Back2Root"))
                {
                    // back to root特别版本
                    result = SemVersion.Parse("v0.0.0", SemVersionStyles.Any);
                }
                else if (versionString.Contains("Early Access"))
                {
                    // EA版本
                    result = SemVersion.Parse("v0.0.1", SemVersionStyles.Any);
                }
                else if (versionString.Contains("Alpha"))
                {
                    // 旧版本格式 Alpha<MainVersion>.<SubVersion>[.<ModifiedVersion>]
                    // 从4.0开始，结束于6.4
                    // 在原版本号基础上增加 0. 主版本前缀，并增加 -alpha 后缀
                    var startPos = versionString.IndexOfAny("0123456789".ToArray());
                    versionString = "0." + versionString[startPos..];
                    if (versionString.Count(c => { return c == '.'; }) > 2)
                        versionString = versionString[..versionString.LastIndexOf('.')];
                    versionString += "-alpha";
                    result = SemVersion.Parse(versionString, SemVersionStyles.Any);
                }
                else if (versionString.Contains("Beta"))
                {
                    // 旧版本格式 Beta<MainVersion>.<SubVersion>[.<ModifiedVersion>]
                    // 从1.0开始，结束于3.1。后续的语义化版本号继承该版本号进度，从4.0开始
                    // 增加 -beta 后缀
                    var startPos = versionString.IndexOfAny("0123456789".ToArray());
                    versionString = versionString[startPos..];
                    if (versionString.Contains(' '))
                        versionString = versionString[..versionString.IndexOf(' ')];
                    versionString += "-beta";
                    result = SemVersion.Parse(versionString, SemVersionStyles.Any);
                }
                else
                {
                    // 其他无法识别的版本，均设置为v0.0.1-unknown
                    result = SemVersion.Parse("v0.0.1-unknown", SemVersionStyles.Any);
                }
            }

            return result;
        }

        void requestHandler(string response)
        {
            UpdateCheckLock = false;

            var resJson = JsonConvert.DeserializeObject<JObject>(response)!;
            var latestVersionString = resJson["tag_name"]!.ToString();
            var releaseUrl = resJson["html_url"]!.ToString();

            var latestVersion = oldVersionCompatible(latestVersionString);

            if (latestVersion.ComparePrecedenceTo(MAJDATA_VERSION) > 0)
            {
                // 版本不同，需要更新
                var msgboxText = string.Format(GetLocalizedString("NewVersionDetected"), latestVersionString,
                    MAJDATA_VERSION_STRING);
                if (onStart) msgboxText += "\n\n" + GetLocalizedString("AutoUpdateCheckTip");

                var result = MessageBox.Show(
                    msgboxText,
                    GetLocalizedString("CheckUpdate"),
                    MessageBoxButton.YesNo);
                switch (result)
                {
                    case MessageBoxResult.Yes:
                        var startInfo = new ProcessStartInfo(releaseUrl)
                        {
                            UseShellExecute = true
                        };
                        Process.Start(startInfo);
                        break;
                    case MessageBoxResult.No:
                        break;
                }
            }
            else
            {
                // 没有新版本，可以不用更新
                if (!onStart) MessageBox.Show(GetLocalizedString("NoNewVersion"), GetLocalizedString("CheckUpdate"));
            }
        }

        #endregion

        // 检查是否需要更新软件

        try
        {
            requestHandler(
                WebControl.RequestGETAsync("http://api.github.com/repos/LingFeng-bbben/MajdataView/releases/latest"));
        } catch {
            // 网络请求失败
            if (!onStart) MessageBox.Show(GetLocalizedString("RequestFail"), GetLocalizedString("CheckUpdate"));
        }
    }

    public string GetWindowsTitleString()
    {
        return $"MajdataEdit ({MAJDATA_VERSION_STRING})";
    }

    public string GetWindowsTitleString(string info)
    {
        try
        {
            var details = "Editing: " + SimaiProcess.title;
            if (details.Length > 50)
                details = details[..50];
            DCRPCclient.SetPresence(new RichPresence
            {
                Details = details,
                State = "With note count of " + SimaiProcess.notelist.Count,
                Assets = new Assets
                {
                    LargeImageKey = "salt",
                    LargeImageText = "Majdata",
                    SmallImageKey = "None"
                }
            });
        }
        catch
        {
        }

        return GetWindowsTitleString() + " - " + info;
    }

    public void OpenFile(string path)
    {
        initFromFile(path);
    }


    //*PLAY CONTROL

    private enum PlayMethod
    {
        Normal,
        Op,
        Record
    }
}
