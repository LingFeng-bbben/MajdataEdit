﻿using System;
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
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Diagnostics;
using System.IO;
using Un4seen.Bass;
using Un4seen.Bass.Misc;
using System.Drawing;
using System.Media;
using Newtonsoft.Json;
using Un4seen.Bass.AddOn.Fx;
using Newtonsoft.Json.Linq;
using WPFLocalizeExtension.Extensions;
using System.Globalization;
using System.Reflection;
using WPFLocalizeExtension.Engine;
using System.Windows.Threading;
using Semver;

namespace MajdataEdit
{
    public partial class MainWindow : Window
    {
        public static readonly string MAJDATA_VERSION_STRING = "v4.0-SNAPSHOT.3";
        public static readonly SemVersion MAJDATA_VERSION = SemVersion.Parse(MAJDATA_VERSION_STRING, SemVersionStyles.Any);
        bool UpdateCheckLock = false;

        Timer currentTimeRefreshTimer = new Timer(100);
        Timer clickSoundTimer = new Timer(1);
        Timer VisualEffectRefreshTimer = new Timer(1);
        Timer waveStopMonitorTimer = new Timer(33);

        SoundSetting soundSetting = new SoundSetting();
        public EditorSetting editorSetting = null;

        public int bgmStream = -114514;
        public int answerStream = -114514;
        public int judgeStream = -114514;
        public int judgeBreakStream = -114514;   // 这个是break的判定音效 不是欢呼声
        public int breakStream = -114514;        // 这个才是欢呼声
        public int judgeExStream = -114514;
        public int hanabiStream = -114514;
        public int holdRiserStream = -114514;
        public int trackStartStream = -114514;
        public int slideStream = -114514;
        public int touchStream = -114514;
        public int allperfectStream = -114514;
        public int clockStream = -114514;

        public static string maidataDir;
        const string majSettingFilename = "majSetting.json";
        const string editorSettingFilename = "EditorSetting.json";

        float[] waveLevels;
        float[] waveEnergies;

        double playStartTime = 0d;
        double extraTime4AllPerfect;     // 需要在播放完后等待All Perfect特效的秒数

        bool isDrawing = false;
        bool isSaved = true;
        bool isLoading = false;
        bool isPlaying = false;          // 为了解决播放到结束时自动停止
        bool isPlan2Stop = false;        // 已准备停止 当all perfect无法在播放完BGM前结束时需要此功能

        int selectedDifficulty = -1;
        float sampleTime = 0.02f;
        int zoominPower = 4;
        EditorControlMethod lastEditorState;

        double lastMousePointX; //Used for drag scroll

        public JObject SLIDE_TIME; // 无理检测用的SLIDE_TIME数据

        List<SoundEffectTiming> waitToBePlayed;

        private bool fumenOverwriteMode = false;    //谱面文本覆盖模式

        public Timer chartChangeTimer = new Timer(1000);    // 谱面变更延迟解析

        //*TEXTBOX CONTROL
        string GetRawFumenText()
        {
            string text = "";
            text = new TextRange(FumenContent.Document.ContentStart, FumenContent.Document.ContentEnd).Text;
            text = text.Replace("\r", "");
            // 亲爱的bbben在这里对text进行了Trim 引发了行位置不正确的BUG 谨此纪念（
            return text;
        }
        void SetRawFumenText(string content)
        {
            isLoading = true;
            FumenContent.Document.Blocks.Clear();
            if (content == null) {
                isLoading = false;
                return;
            }
            string[] lines = content.Split('\n');
            foreach (var line in lines)
            {
                Paragraph paragraph = new Paragraph();
                paragraph.Inlines.Add(line);
                FumenContent.Document.Blocks.Add(paragraph);
            }
            isLoading = false;
        }
        long GetRawFumenPosition()
        {
            long pos = new TextRange(FumenContent.Document.ContentStart, FumenContent.CaretPosition).Text.Replace("\r", "").Length;
            return pos;
        }
        void SeekTextFromTime()
        {
            var time = Bass.BASS_ChannelBytes2Seconds(bgmStream, Bass.BASS_ChannelGetPosition(bgmStream));
            var timingList = SimaiProcess.timinglist;
            var noteList = SimaiProcess.notelist;
            if (SimaiProcess.timinglist.Count <= 0) return;
            timingList.Sort((x, y) => Math.Abs(time - x.time).CompareTo(Math.Abs(time - y.time)));
            var theNote = timingList[0];
            timingList = SimaiProcess.timinglist;
            var indexOfTheNote = timingList.IndexOf(theNote);
            SimaiTimingPoint prevNote;
            if (indexOfTheNote > 0)
                prevNote = timingList[indexOfTheNote - 1];
            else
                prevNote = theNote;
            //this may fuck up when the text changed and reload the document may solve it. it could be a bug of .net or something.
            var pointer = FumenContent.Document.Blocks.ToList()[prevNote.rawTextPositionY].ContentStart.GetPositionAtOffset(prevNote.rawTextPositionX);
            var pointer1 = FumenContent.Document.Blocks.ToList()[theNote.rawTextPositionY].ContentStart.GetPositionAtOffset(theNote.rawTextPositionX);
            FumenContent.Selection.Select(pointer, pointer1);
        }
        public void ScrollToFumenContentSelection(int positionX, int positionY)
        {
            // 这玩意用于其他窗口来滚动Scroll 因为涉及到好多变量都是private的
            var pointer = FumenContent.Document.Blocks.ToList()[positionY].ContentStart.GetPositionAtOffset(positionX);
            FumenContent.Focus();
            FumenContent.Selection.Select(pointer, pointer);
            this.Focus();

            if (Bass.BASS_ChannelIsActive(bgmStream) == BASSActive.BASS_ACTIVE_PLAYING && (bool)FollowPlayCheck.IsChecked)
                return;
            var time = SimaiProcess.Serialize(GetRawFumenText(), GetRawFumenPosition());
            SetBgmPosition(time);
            //Console.WriteLine("SelectionChanged");
            SimaiProcess.ClearNoteListPlayedState();
            DrawCusor(time);
        }
        
        //*FIND AND REPLACE
        private void Find_icon_MouseDown(object sender, MouseButtonEventArgs e)
        {
            FindAndScroll();
        }
        bool isReplaceConformed = false;
        TextSelection lastFindPosition;
        private void Replace_icon_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (!isReplaceConformed)
            {
                FindAndScroll();
                return;
            }
            else
            {
                if(FumenContent.Selection == lastFindPosition)
                {
                    FumenContent.Selection.Text = ReplaceText.Text;
                    FindAndScroll();
                }
                else
                {
                    isReplaceConformed = false;
                }
            }
        }

        public TextRange GetTextRangeFromPosition(TextPointer position, String input)
        {

            TextRange textRange = null;

            while (position != null)
            {
                if (position.CompareTo(FumenContent.Document.ContentEnd) == 0)
                {
                    break;
                }

                if (position.GetPointerContext(LogicalDirection.Forward) == TextPointerContext.Text)
                {
                    String textRun = position.GetTextInRun(LogicalDirection.Forward);
                    StringComparison stringComparison = StringComparison.CurrentCultureIgnoreCase;
                    Int32 indexInRun = textRun.IndexOf(input, stringComparison);

                    if (indexInRun >= 0)
                    {
                        position = position.GetPositionAtOffset(indexInRun);
                        TextPointer nextPointer = position.GetPositionAtOffset(input.Length);
                        textRange = new TextRange(position, nextPointer);

                        // If a none-WholeWord match is found, directly terminate the loop.
                        position = position.GetPositionAtOffset(input.Length);
                        break;
                    }
                    else
                    {
                        // If a match is not found, go over to the next context position after the "textRun".
                        position = position.GetPositionAtOffset(textRun.Length);
                    }
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
            if (position == null) {
                isReplaceConformed = false;
                return;
            }
            FumenContent.Selection.Select(position.Start, position.End);
            lastFindPosition = FumenContent.Selection;
            FumenContent.Focus();
            isReplaceConformed = true;
        }
        //*FILE CONTROL
        void initFromFile(string path)//file name should not be included in path
        {
            if (soundSetting != null)
            {
                soundSetting.Close();
            }
            if (editorSetting == null)
            {
                ReadEditorSetting();
            }

            var audioPath = path + "/track.mp3";
            var dataPath = path + "/maidata.txt";
            if (!File.Exists(audioPath))
            {
                MessageBox.Show(GetLocalizedString("NoTrack_mp3"), GetLocalizedString("Error"));
                return;
            }
            if (!File.Exists(dataPath))
            {
                MessageBox.Show(GetLocalizedString("NoMaidata_txt"), GetLocalizedString("Error"));
                return;
            }
            maidataDir = path;
            SetRawFumenText("");
            if (bgmStream != -1024)
            {
                Bass.BASS_ChannelStop(bgmStream);
                Bass.BASS_StreamFree(bgmStream);
            }
            //soundSetting.Close();
            var decodeStream = Bass.BASS_StreamCreateFile(audioPath, 0L, 0L, BASSFlag.BASS_STREAM_DECODE);
            bgmStream = BassFx.BASS_FX_TempoCreate(decodeStream, BASSFlag.BASS_FX_FREESOURCE);
            //Bass.BASS_StreamCreateFile(audioPath, 0L, 0L, BASSFlag.BASS_SAMPLE_FLOAT);

            Bass.BASS_ChannelSetAttribute(bgmStream, BASSAttribute.BASS_ATTRIB_VOL, editorSetting.Default_BGM_Level);
            Bass.BASS_ChannelSetAttribute(trackStartStream, BASSAttribute.BASS_ATTRIB_VOL, editorSetting.Default_BGM_Level);
            Bass.BASS_ChannelSetAttribute(allperfectStream, BASSAttribute.BASS_ATTRIB_VOL, editorSetting.Default_BGM_Level);
            Bass.BASS_ChannelSetAttribute(clockStream, BASSAttribute.BASS_ATTRIB_VOL, editorSetting.Default_BGM_Level);
            Bass.BASS_ChannelSetAttribute(answerStream, BASSAttribute.BASS_ATTRIB_VOL, editorSetting.Default_Answer_Level);
            Bass.BASS_ChannelSetAttribute(judgeStream, BASSAttribute.BASS_ATTRIB_VOL, editorSetting.Default_Judge_Level);
            Bass.BASS_ChannelSetAttribute(judgeBreakStream, BASSAttribute.BASS_ATTRIB_VOL, editorSetting.Default_Break_Level);
            Bass.BASS_ChannelSetAttribute(slideStream, BASSAttribute.BASS_ATTRIB_VOL, editorSetting.Default_Slide_Level);
            Bass.BASS_ChannelSetAttribute(breakStream, BASSAttribute.BASS_ATTRIB_VOL, editorSetting.Default_Break_Level);
            Bass.BASS_ChannelSetAttribute(judgeExStream, BASSAttribute.BASS_ATTRIB_VOL, editorSetting.Default_Ex_Level);
            Bass.BASS_ChannelSetAttribute(touchStream, BASSAttribute.BASS_ATTRIB_VOL, editorSetting.Default_Touch_Level);
            Bass.BASS_ChannelSetAttribute(hanabiStream, BASSAttribute.BASS_ATTRIB_VOL, editorSetting.Default_Hanabi_Level);
            Bass.BASS_ChannelSetAttribute(holdRiserStream, BASSAttribute.BASS_ATTRIB_VOL, editorSetting.Default_Hanabi_Level);
            var info = Bass.BASS_ChannelGetInfo(bgmStream);
            if (info.freq != 44100) MessageBox.Show(GetLocalizedString("Warn44100Hz"), GetLocalizedString("Attention"));
            ReadWaveFromFile();
            SimaiProcess.ClearData();

            if (!SimaiProcess.ReadData(dataPath))
            {
                return;
            }




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
            SetSavedState(true);
        }
        private void ReadWaveFromFile()
        {
            var bgmDecode = Bass.BASS_StreamCreateFile(maidataDir + "/track.mp3", 0L, 0L, BASSFlag.BASS_STREAM_DECODE);
            try
            {
                var length = Bass.BASS_ChannelBytes2Seconds(bgmDecode, Bass.BASS_ChannelGetLength(bgmDecode, BASSMode.BASS_POS_BYTE));
                int sampleNumber = (int)((length * 1000) / (sampleTime * 1000));
                waveLevels = new float[sampleNumber];
                waveEnergies = new float[sampleNumber];
                for (int i = 0; i < sampleNumber; i++)
                {
                    waveLevels[i] = Bass.BASS_ChannelGetLevels(bgmDecode, sampleTime, BASSLevel.BASS_LEVEL_MONO)[0];
                    waveEnergies[i] = 0.01f;
                }
                Bass.BASS_StreamFree(bgmDecode);
            }
            catch {
                MessageBox.Show("mp3解码失败。\nMP3 Decode fail.\n"+ Bass.BASS_ErrorGetCode());
                Bass.BASS_StreamFree(bgmDecode);
                Process.Start("https://github.com/LingFeng-bbben/MajdataEdit/issues/26");
            }
        }
        void SetSavedState(bool state)
        {
            if (state)
            {
                isSaved = true;
                LevelSelector.IsEnabled = true;
                TheWindow.Title = GetWindowsTitleString(SimaiProcess.title);
            }
            else
            {
                isSaved = false;
                LevelSelector.IsEnabled = false;
                TheWindow.Title = GetWindowsTitleString(GetLocalizedString("Unsaved") + SimaiProcess.title);
            }
        }
        /// <summary>
        /// Ask the user and save fumen.
        /// </summary>
        /// <returns>Return false if user cancel the action</returns>
        bool AskSave()
        {
            var result = MessageBox.Show(GetLocalizedString("AskSave"), GetLocalizedString("Warning"), MessageBoxButton.YesNoCancel);
            if (result == MessageBoxResult.Yes)
            {
                SaveFumen(true);
                return true;
            }
            if (result == MessageBoxResult.Cancel)
            {
                return false;
            }
            return true;
        }
        void SaveFumen(bool writeToDisk = false)
        {
            if (selectedDifficulty == -1) return;
            SimaiProcess.fumens[selectedDifficulty] = GetRawFumenText();
            SimaiProcess.first = float.Parse(OffsetTextBox.Text);
            if (maidataDir == "")
            {
                var saveDialog = new Microsoft.Win32.SaveFileDialog();
                saveDialog.Filter = "maidata.txt|maidata.txt";
                saveDialog.OverwritePrompt = true;
                if ((bool)saveDialog.ShowDialog())
                {
                    maidataDir = new FileInfo(saveDialog.FileName).DirectoryName;
                }
            }
            SimaiProcess.SaveData(maidataDir + "/maidata.bak.txt");
            SaveSetting();
            if (writeToDisk)
            {
                SimaiProcess.SaveData(maidataDir + "/maidata.txt");
                SetSavedState(true);
            }
        }
        void SaveSetting()
        {
            MajSetting setting = new MajSetting();
            setting.lastEditDiff = selectedDifficulty;
            setting.lastEditTime = Bass.BASS_ChannelBytes2Seconds(bgmStream, Bass.BASS_ChannelGetPosition(bgmStream));
            Bass.BASS_ChannelGetAttribute(bgmStream, BASSAttribute.BASS_ATTRIB_VOL, ref setting.BGM_Level);
            Bass.BASS_ChannelGetAttribute(answerStream, BASSAttribute.BASS_ATTRIB_VOL, ref setting.Answer_Level);
            Bass.BASS_ChannelGetAttribute(judgeStream, BASSAttribute.BASS_ATTRIB_VOL, ref setting.Judge_Level);
            Bass.BASS_ChannelGetAttribute(breakStream, BASSAttribute.BASS_ATTRIB_VOL, ref setting.Break_Level);
            Bass.BASS_ChannelGetAttribute(judgeExStream, BASSAttribute.BASS_ATTRIB_VOL, ref setting.Ex_Level);
            Bass.BASS_ChannelGetAttribute(touchStream, BASSAttribute.BASS_ATTRIB_VOL, ref setting.Touch_Level);
            Bass.BASS_ChannelGetAttribute(slideStream, BASSAttribute.BASS_ATTRIB_VOL, ref setting.Slide_Level);
            Bass.BASS_ChannelGetAttribute(hanabiStream, BASSAttribute.BASS_ATTRIB_VOL, ref setting.Hanabi_Level);
            string json = JsonConvert.SerializeObject(setting);
            File.WriteAllText(maidataDir + "/" + majSettingFilename,json);
        }
        void ReadSetting()
        {
            var path = maidataDir + "/" + majSettingFilename;
            if (!File.Exists(path)) return;
            var setting = JsonConvert.DeserializeObject<MajSetting>(File.ReadAllText(path));
            LevelSelector.SelectedIndex = setting.lastEditDiff;
            selectedDifficulty = setting.lastEditDiff;
            SetBgmPosition(setting.lastEditTime);
            Bass.BASS_ChannelSetAttribute(bgmStream, BASSAttribute.BASS_ATTRIB_VOL,setting.BGM_Level);
            Bass.BASS_ChannelSetAttribute(trackStartStream, BASSAttribute.BASS_ATTRIB_VOL, setting.BGM_Level);
            Bass.BASS_ChannelSetAttribute(allperfectStream, BASSAttribute.BASS_ATTRIB_VOL, setting.BGM_Level);
            Bass.BASS_ChannelSetAttribute(clockStream, BASSAttribute.BASS_ATTRIB_VOL, setting.BGM_Level);
            Bass.BASS_ChannelSetAttribute(answerStream, BASSAttribute.BASS_ATTRIB_VOL, setting.Answer_Level);
            Bass.BASS_ChannelSetAttribute(judgeStream, BASSAttribute.BASS_ATTRIB_VOL, setting.Judge_Level);
            Bass.BASS_ChannelSetAttribute(judgeBreakStream, BASSAttribute.BASS_ATTRIB_VOL, setting.Break_Level);
            Bass.BASS_ChannelSetAttribute(slideStream, BASSAttribute.BASS_ATTRIB_VOL, setting.Slide_Level);
            Bass.BASS_ChannelSetAttribute(breakStream, BASSAttribute.BASS_ATTRIB_VOL, setting.Break_Level);
            Bass.BASS_ChannelSetAttribute(judgeExStream, BASSAttribute.BASS_ATTRIB_VOL, setting.Ex_Level);
            Bass.BASS_ChannelSetAttribute(touchStream, BASSAttribute.BASS_ATTRIB_VOL, setting.Touch_Level);
            Bass.BASS_ChannelSetAttribute(hanabiStream, BASSAttribute.BASS_ATTRIB_VOL, setting.Hanabi_Level);
            Bass.BASS_ChannelSetAttribute(holdRiserStream, BASSAttribute.BASS_ATTRIB_VOL, setting.Hanabi_Level);

            SaveSetting(); // 覆盖旧版本setting
        }
        private void CreateNewFumen(string path)
        {
            if (File.Exists(path + "/maidata.txt"))
            {
                MessageBox.Show(GetLocalizedString("MaidataExist"));
            }
            else
            {
                File.WriteAllText(path + "/maidata.txt",
                    "&title="+GetLocalizedString("SetTitle")+"\n" +
                    "&artist=" + GetLocalizedString("SetArtist") + "\n" +
                    "&des=" + GetLocalizedString("SetDes") + "\n" +
                    "&first=0\n");
            }
        }
        void CreateEditorSetting()
        {
            editorSetting = new EditorSetting();
            editorSetting.Language = "en-US";   // 在未初始化EditorSetting的时候 以较为通用的英文运行
            editorSetting.RenderMode = RenderOptions.ProcessRenderMode == RenderMode.SoftwareOnly ? 1 : 0;  // 使用命令行指定强制软件渲染时，同步修改配置值

            editorSetting.DefaultSlideAccuracy = 0.2f; // 大家都要做能pass 200ms撞尾检测的好孩子喔
            // 设置默认音量
            editorSetting.Default_BGM_Level = 0.85f;
            editorSetting.Default_Answer_Level = 0.95f;
            editorSetting.Default_Judge_Level = 0.5f;
            editorSetting.Default_Slide_Level = 0.35f;
            editorSetting.Default_Break_Level = 0.55f;
            editorSetting.Default_Ex_Level = 0.4f;
            editorSetting.Default_Touch_Level = 0.5f;
            editorSetting.Default_Hanabi_Level = 0.4f;

            File.WriteAllText(editorSettingFilename, JsonConvert.SerializeObject(editorSetting, Formatting.Indented));

            EditorSettingPanel esp = new EditorSettingPanel(true);
            esp.Owner = this;
            esp.ShowDialog();
        }
        void ReadEditorSetting()
        {
            if (!File.Exists(editorSettingFilename))
            {
                CreateEditorSetting();
                return;
            }
            var json = File.ReadAllText(editorSettingFilename);
            editorSetting = JsonConvert.DeserializeObject<EditorSetting>(json);

            if (RenderOptions.ProcessRenderMode != RenderMode.SoftwareOnly)
            {
                //如果没有通过命令行预先指定渲染模式，则使用设置项的渲染模式
                RenderOptions.ProcessRenderMode =
                    editorSetting.RenderMode == 0 ? RenderMode.Default : RenderMode.SoftwareOnly;
            }
            else
            {
                //如果通过命令行指定了使用软件渲染模式，则覆盖设置项
                editorSetting.RenderMode = 1;
            }

            LocalizeDictionary.Instance.Culture = new CultureInfo(editorSetting.Language);
            AddGesture(editorSetting.PlayPauseKey, "PlayAndPause");
            AddGesture(editorSetting.PlayStopKey, "StopPlaying");
            AddGesture(editorSetting.SaveKey, "SaveFile");
            AddGesture(editorSetting.SendViewerKey, "SendToView");
            AddGesture(editorSetting.IncreasePlaybackSpeedKey, "IncreasePlaybackSpeed");
            AddGesture(editorSetting.DecreasePlaybackSpeedKey, "DecreasePlaybackSpeed");
            AddGesture("Ctrl+f", "Find");
            FumenContent.FontSize = editorSetting.FontSize;
            
            ViewerCover.Content = editorSetting.backgroundCover.ToString();
            ViewerSpeed.Content = editorSetting.playSpeed.ToString("F1");    // 转化为形如"7.0", "9.5"这样的速度
            ViewerTouchSpeed.Content = editorSetting.touchSpeed.ToString("F1");

            chartChangeTimer.Interval = editorSetting.ChartRefreshDelay; // 设置更新延迟

            SaveEditorSetting(); // 覆盖旧版本setting
        }
        public void SaveEditorSetting()
        {
            File.WriteAllText(editorSettingFilename, JsonConvert.SerializeObject(editorSetting, Formatting.Indented));
        }
        void ReadMuriCheckSlideTime()
        {
            using (StreamReader r = new StreamReader("./slide_time.json"))
            {
                string json = r.ReadToEnd();
                SLIDE_TIME = JsonConvert.DeserializeObject<JObject>(json);
            }
        }
        void AddGesture( string keyGusture,string command)
        {
            var gesture = (KeyGesture)new KeyGestureConverter().ConvertFromString(keyGusture);
            InputBinding inputBinding = new InputBinding((ICommand)FumenContent.Resources[command], gesture);
            FumenContent.InputBindings.Add(inputBinding);
        }

        //*SOUND EFFECT
        private void ReadSoundEffect()
        {
            var path = Environment.CurrentDirectory + "/SFX/";
            answerStream = Bass.BASS_StreamCreateFile(path + "answer.wav", 0L, 0L, BASSFlag.BASS_SAMPLE_FLOAT);
            judgeStream = Bass.BASS_StreamCreateFile(path + "judge.wav", 0L, 0L, BASSFlag.BASS_SAMPLE_FLOAT);
            judgeBreakStream = Bass.BASS_StreamCreateFile(path + "judge_break.wav", 0L, 0L, BASSFlag.BASS_SAMPLE_FLOAT);
            judgeExStream = Bass.BASS_StreamCreateFile(path + "judge_ex.wav", 0L, 0L, BASSFlag.BASS_SAMPLE_FLOAT);
            breakStream = Bass.BASS_StreamCreateFile(path + "break.wav", 0L, 0L, BASSFlag.BASS_SAMPLE_FLOAT);
            hanabiStream = Bass.BASS_StreamCreateFile(path + "hanabi.wav", 0L, 0L, BASSFlag.BASS_SAMPLE_FLOAT);
            holdRiserStream = Bass.BASS_StreamCreateFile(path + "touchHold_riser.wav", 0L, 0L, BASSFlag.BASS_SAMPLE_FLOAT);
            trackStartStream = Bass.BASS_StreamCreateFile(path + "track_start.wav", 0L, 0L, BASSFlag.BASS_SAMPLE_FLOAT);
            slideStream = Bass.BASS_StreamCreateFile(path + "slide.wav", 0L, 0L, BASSFlag.BASS_SAMPLE_FLOAT);
            touchStream = Bass.BASS_StreamCreateFile(path + "touch.wav", 0L, 0L, BASSFlag.BASS_SAMPLE_FLOAT);
            allperfectStream = Bass.BASS_StreamCreateFile(path + "all_perfect.wav", 0L, 0L, BASSFlag.BASS_SAMPLE_FLOAT);
            clockStream = Bass.BASS_StreamCreateFile(path + "clock.wav", 0L, 0L, BASSFlag.BASS_SAMPLE_FLOAT);
        }
        private void SoundEffectUpdate()
        {
            try
            {
                var currentTime = Bass.BASS_ChannelBytes2Seconds(bgmStream, Bass.BASS_ChannelGetPosition(bgmStream));
                //var waitToBePlayed = SimaiProcess.notelist.FindAll(o => o.havePlayed == false && o.time > currentTime);
                if (waitToBePlayed.Count < 1) return;
                var nearestTime = waitToBePlayed[0].time;
                //Console.WriteLine(nearestTime);
                if (Math.Abs(currentTime - nearestTime) < 0.055)
                {
                    SoundEffectTiming se = waitToBePlayed[0];
                    waitToBePlayed.RemoveAt(0);

                    if (se.hasAnswer)
                    {
                        Bass.BASS_ChannelPlay(answerStream, true);
                    }
                    if (se.hasJudge)
                    {
                        Bass.BASS_ChannelPlay(judgeStream, true);
                    }
                    if (se.hasJudgeBreak)
                    {
                        Bass.BASS_ChannelPlay(judgeBreakStream, true);
                    }
                    if (se.hasJudgeEx)
                    {
                        Bass.BASS_ChannelPlay(judgeExStream, true);
                    }
                    if (se.hasBreak)
                    {
                        Bass.BASS_ChannelPlay(breakStream, true);
                    }
                    if (se.hasTouch)
                    {
                        Bass.BASS_ChannelPlay(touchStream, true);
                    }
                    if (se.hasHanabi) //may cause delay
                    {
                        Bass.BASS_ChannelPlay(hanabiStream, true);
                    }
                    if (se.hasTouchHold)
                    {
                        Bass.BASS_ChannelPlay(holdRiserStream, true);
                    }
                    if (se.hasTouchHoldEnd)
                    {
                        Bass.BASS_ChannelStop(holdRiserStream);
                    }
                    if (se.hasSlide)
                    {
                        Bass.BASS_ChannelPlay(slideStream, true);
                    }
                    if (se.hasAllPerfect)
                    {
                        Bass.BASS_ChannelPlay(allperfectStream, true);
                    }
                    if (se.hasClock)
                    {
                        Bass.BASS_ChannelPlay(clockStream, true);
                    }
                    //
                    Dispatcher.Invoke(() => {

                        if ((bool)FollowPlayCheck.IsChecked)
                            SeekTextFromTime();
                    });
                }

            }
            catch { }
        }
        private void SlideTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            Bass.BASS_ChannelPlay(slideStream, true);
            var father = (Timer)sender;
            father.Stop();
            father.Dispose();
        }
        private void HoldHanibiTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            Bass.BASS_ChannelPlay(hanabiStream, true);
            var father = (Timer)sender;
            father.Stop();
            father.Dispose();
        }
        private void HoldRiserTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            Bass.BASS_ChannelStop(holdRiserStream);
            //Console.WriteLine("stop");
            var father = (Timer)sender;
            father.Stop();
            father.Dispose();
        }


        //*UI DRAWING
        private async void DrawWave()
        {

            if (isDrawing) return;
            Console.WriteLine("DrawWave");
            isDrawing = true;
            var writableBitmap = new WriteableBitmap(waveLevels.Length * zoominPower, 74, 72, 72, PixelFormats.Pbgra32, null);
            writableBitmap.Lock();
            //the process starts
            Bitmap backBitmap = new Bitmap(waveLevels.Length * zoominPower, 74, writableBitmap.BackBufferStride,
                        System.Drawing.Imaging.PixelFormat.Format32bppArgb, writableBitmap.BackBuffer);
            Graphics graphics = Graphics.FromImage(backBitmap);
            System.Drawing.Pen pen = new System.Drawing.Pen(System.Drawing.Color.Green, zoominPower);

            var drawoffset = 0;
            await Task.Run(() =>
            {
                try
                {
                    graphics.Clear(System.Drawing.Color.FromArgb(100,0,0,0));
                    for (int i = 0; i < waveLevels.Length; i++)
                    {
                        var lv = waveLevels[i] * 35;
                        graphics.DrawLine(pen, (i + drawoffset) * zoominPower, 37 + lv, (i + drawoffset) * zoominPower, 37 - lv);
                    }

                    pen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(150, 200, 200, 220), 2);
                    PointF[] curvepoints = new PointF[waveLevels.Length];
                    for (int i = 0; i < waveLevels.Length; i++)
                    {
                        curvepoints[i] = new PointF((i + drawoffset) * zoominPower, (1f - waveEnergies[i]) * 35 + 2);
                    }
                    graphics.DrawCurve(pen, curvepoints);

                    if (SimaiProcess.timinglist.Count == 0) throw new Exception("NO notes");

                    //Draw Bpm lines
                    var lastbpm = -1f;
                    List<double> bpmChangeTimes = new List<double>();     //在什么时间变成什么值
                    List<float> bpmChangeValues = new List<float>();
                    bpmChangeTimes.Clear();
                    bpmChangeValues.Clear();
                    foreach (var timing in SimaiProcess.timinglist)
                    {
                        if (timing.currentBpm != lastbpm)
                        {
                            bpmChangeTimes.Add(timing.time);
                            bpmChangeValues.Add(timing.currentBpm);
                            lastbpm = timing.currentBpm;
                        }
                    }
                    bpmChangeTimes.Add(Bass.BASS_ChannelBytes2Seconds(bgmStream, Bass.BASS_ChannelGetLength(bgmStream)));
                    
                    double time = SimaiProcess.first;
                    int beat = 4; //预留拍号
                    int currentBeat = 1;
                    var timePerBeat = 0d;
                    pen = new System.Drawing.Pen(System.Drawing.Color.Yellow, 1);
                    for (int i = 1; i < bpmChangeTimes.Count; i++)
                    {
                        while ((time - bpmChangeTimes[i])<-0.05)//在那个时间之前都是之前的bpm
                        {
                            if (currentBeat > beat) currentBeat = 1;
                            var xbase = (float)(time / sampleTime) * zoominPower;
                            timePerBeat = 1d / (bpmChangeValues[i - 1] / 60d);
                            if (currentBeat == 1)
                            {
                                graphics.DrawLine(pen, xbase, 0, xbase, 75);
                            }
                            else
                            {
                                graphics.DrawLine(pen, xbase, 0, xbase, 15);
                            }
                            currentBeat++;
                            time += timePerBeat;
                        }
                        time = bpmChangeTimes[i];
                        currentBeat = 1;
                    }

                    //Draw timing lines
                    pen = new System.Drawing.Pen(System.Drawing.Color.White, 1);
                    foreach (var note in SimaiProcess.timinglist)
                    {
                        if (note == null) { break; }
                        float x = (float)(note.time / sampleTime) * zoominPower;
                        graphics.DrawLine(pen, x, 60, x, 75);
                    }

                    //Draw notes                    
                    foreach (var note in SimaiProcess.notelist)
                    {
                        if (note == null) { break; }
                        var notes = note.getNotes();
                        bool isEach = notes.Count(o => !o.isSlideNoHead) > 1;

                        float x = (float)(note.time / sampleTime) * zoominPower;

                        foreach (var noteD in notes)
                        {
                            float y = noteD.startPosition * 6.875f + 8f; //与键位有关

                            if (noteD.isHanabi)
                            {
                                float xDeltaHanabi = (float)(1f / sampleTime) * zoominPower; //Hanabi is 1s due to frame analyze
                                RectangleF rectangleF = new RectangleF(x, 0, xDeltaHanabi, 75);
                                if (noteD.noteType == SimaiNoteType.TouchHold)
                                {
                                    rectangleF.X += ((float)(noteD.holdTime / sampleTime) * zoominPower);
                                }
                                System.Drawing.Drawing2D.LinearGradientBrush gradientBrush = new System.Drawing.Drawing2D.LinearGradientBrush(
                                    rectangleF,
                                    System.Drawing.Color.FromArgb(100, 255, 0, 0),
                                    System.Drawing.Color.FromArgb(0, 255, 0, 0),
                                    System.Drawing.Drawing2D.LinearGradientMode.Horizontal
                                    );
                                graphics.FillRectangle(gradientBrush, rectangleF);
                            }

                            if (noteD.noteType == SimaiNoteType.Tap)
                            {
                                if (noteD.isForceStar)
                                {
                                    pen.Width = 3;
                                    if (noteD.isBreak)
                                    {
                                        pen.Color = System.Drawing.Color.OrangeRed;
                                    }
                                    else if (isEach)
                                    {
                                        pen.Color = System.Drawing.Color.Gold;
                                    }
                                    else
                                    {
                                        pen.Color = System.Drawing.Color.DeepSkyBlue;
                                    }
                                    System.Drawing.Brush brush = new SolidBrush(pen.Color);
                                    graphics.DrawString("*", new Font("Consolas", 12, System.Drawing.FontStyle.Bold), brush, new PointF(x - 7f, y - 7f));
                                } else
                                {
                                    pen.Width = 2;
                                    if (noteD.isBreak)
                                    {
                                        pen.Color = System.Drawing.Color.OrangeRed;
                                    }
                                    else if (isEach)
                                    {
                                        pen.Color = System.Drawing.Color.Gold;
                                    }
                                    else
                                    {
                                        pen.Color = System.Drawing.Color.LightPink;
                                    }
                                    graphics.DrawEllipse(pen, x - 2.5f, y - 2.5f, 5, 5);
                                }
                            }

                            if (noteD.noteType == SimaiNoteType.Touch)
                            {
                                pen.Width = 2;
                                pen.Color = isEach ? System.Drawing.Color.Gold : System.Drawing.Color.DeepSkyBlue;
                                graphics.DrawRectangle(pen, x - 2.5f, y - 2.5f, 5, 5);

                            }

                            if (noteD.noteType == SimaiNoteType.Hold)
                            {
                                pen.Width = 3;
                                if (noteD.isBreak)
                                {
                                    pen.Color = System.Drawing.Color.OrangeRed;
                                }
                                else if (isEach)
                                {
                                    pen.Color = System.Drawing.Color.Gold;
                                }
                                else
                                {
                                    pen.Color = System.Drawing.Color.LightPink;
                                }

                                float xRight = x + (float)(noteD.holdTime / sampleTime) * zoominPower;
                                if (xRight - x < 1f) xRight = x + 5;
                                graphics.DrawLine(pen, x, y, xRight, y);

                            }

                            if (noteD.noteType == SimaiNoteType.TouchHold)
                            {
                                pen.Width = 3;
                                float xDelta = ((float)(noteD.holdTime / sampleTime) * zoominPower) / 4;
                                //Console.WriteLine("HoldPixel"+ xDelta);
                                if (xDelta < 1f) xDelta = 1;

                                pen.Color = System.Drawing.Color.FromArgb(200, 255, 75, 0);
                                graphics.DrawLine(pen, x, y, x + xDelta * 4f, y);
                                pen.Color = System.Drawing.Color.FromArgb(200, 255, 241, 0);
                                graphics.DrawLine(pen, x, y, x + xDelta * 3f, y);
                                pen.Color = System.Drawing.Color.FromArgb(200, 2, 165, 89);
                                graphics.DrawLine(pen, x, y, x + xDelta * 2f, y);
                                pen.Color = System.Drawing.Color.FromArgb(200, 0, 140, 254);
                                graphics.DrawLine(pen, x, y, x + xDelta, y);
                            }

                            if (noteD.noteType == SimaiNoteType.Slide)
                            {
                                pen.Width = 3;
                                if (!noteD.isSlideNoHead)
                                {
                                    if (noteD.isBreak)
                                    {
                                        pen.Color = System.Drawing.Color.OrangeRed;
                                    }
                                    else if (isEach)
                                    {
                                        pen.Color = System.Drawing.Color.Gold;
                                    }
                                    else
                                    {
                                        pen.Color = System.Drawing.Color.DeepSkyBlue;
                                    }
                                    System.Drawing.Brush brush = new SolidBrush(pen.Color);
                                    graphics.DrawString("*", new Font("Consolas", 12, System.Drawing.FontStyle.Bold), brush, new PointF(x - 7f, y - 7f));
                                }

                                if (noteD.isSlideBreak)
                                {
                                    pen.Color = System.Drawing.Color.OrangeRed;
                                }
                                else if (notes.Count(o => o.noteType == SimaiNoteType.Slide) >= 2)
                                {
                                    pen.Color = System.Drawing.Color.Gold;
                                }
                                else
                                {
                                    pen.Color = System.Drawing.Color.SkyBlue;
                                }
                                pen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dot;
                                float xSlide = (float)(noteD.slideStartTime / sampleTime) * zoominPower;
                                float xSlideRight = (float)(noteD.slideTime / sampleTime) * zoominPower + xSlide;
                                graphics.DrawLine(pen, xSlide, y, xSlideRight, y);
                                pen.DashStyle = System.Drawing.Drawing2D.DashStyle.Solid;
                            }



                        }

                    }
                }
                catch{}
            }
            );
            graphics.Flush();
            graphics.Dispose();
            backBitmap.Dispose();
            MusicWave.Source = writableBitmap;
            MusicWave.Width = waveLevels.Length * zoominPower;
            writableBitmap.AddDirtyRect(new Int32Rect(0, 0, writableBitmap.PixelWidth, writableBitmap.PixelHeight));
            writableBitmap.Unlock();
            isDrawing = false;
            GC.Collect();

        }
        private async void DrawCusor(double ghostCusorPositionTime = 0)
        {
            var writableBitmap = new WriteableBitmap(waveLevels.Length * zoominPower, 74, 72, 72, PixelFormats.Pbgra32, null);
            writableBitmap.Lock();
            //the process starts
            Bitmap backBitmap = new Bitmap(waveLevels.Length * zoominPower, 74, writableBitmap.BackBufferStride,
                        System.Drawing.Imaging.PixelFormat.Format32bppArgb, writableBitmap.BackBuffer);
            Graphics graphics = Graphics.FromImage(backBitmap);
            System.Drawing.Pen pen = new System.Drawing.Pen(System.Drawing.Color.Green, zoominPower);

            await Task.Run(() =>
            {
                try
                {
                    //Draw play Start time
                    pen = new System.Drawing.Pen(System.Drawing.Color.Red, 5);
                    float x1 = (float)(playStartTime / sampleTime) * zoominPower;
                    PointF[] tranglePoints = { new PointF(x1 - 2, 0), new PointF(x1 + 2, 0), new PointF(x1, 3.46f) };
                    graphics.DrawPolygon(pen, tranglePoints);

                    //Draw ghost cusor
                    pen = new System.Drawing.Pen(System.Drawing.Color.Orange, 5);
                    float x2 = (float)(ghostCusorPositionTime / sampleTime) * zoominPower;
                    PointF[] tranglePoints2 = { new PointF(x2 - 2, 0), new PointF(x2 + 2, 0), new PointF(x2, 3.46f) };
                    graphics.DrawPolygon(pen, tranglePoints2);
                }
                catch { }
            });
            graphics.Flush();
            graphics.Dispose();
            backBitmap.Dispose();
            MusicWaveCusor.Source = writableBitmap;
            MusicWaveCusor.Width = waveLevels.Length * zoominPower;
            writableBitmap.AddDirtyRect(new Int32Rect(0, 0, writableBitmap.PixelWidth, writableBitmap.PixelHeight));
            writableBitmap.Unlock();
        }
        private void DrawFFT()
        {
            Dispatcher.Invoke(new Action(() =>
            {
                //Scroll WaveView
                var currentTime = Bass.BASS_ChannelBytes2Seconds(bgmStream, Bass.BASS_ChannelGetPosition(bgmStream));
                MusicWave.Margin = new Thickness(-currentTime / sampleTime * zoominPower, Margin.Left, MusicWave.Margin.Right, Margin.Bottom);
                MusicWaveCusor.Margin = new Thickness(-currentTime / sampleTime * zoominPower, Margin.Left, MusicWave.Margin.Right, Margin.Bottom);

                var writableBitmap = new WriteableBitmap(255, 255, 72, 72, PixelFormats.Pbgra32, null);
                FFTImage.Source = writableBitmap;
                writableBitmap.Lock();
                Bitmap backBitmap = new Bitmap(255, 255, writableBitmap.BackBufferStride,
                    System.Drawing.Imaging.PixelFormat.Format32bppArgb, writableBitmap.BackBuffer);

                Graphics graphics = Graphics.FromImage(backBitmap);
                graphics.Clear(System.Drawing.Color.Transparent);

                float[] fft = new float[1024];
                Bass.BASS_ChannelGetData(bgmStream, fft, (int)BASSData.BASS_DATA_FFT1024);
                PointF[] points = new PointF[1024];
                for (int i = 0; i < fft.Length; i++)
                {
                    points[i] = new PointF((float)Math.Log10(i + 1) * 100f, (240 - fft[i] * 256)); //semilog
                }

                graphics.DrawCurve(new System.Drawing.Pen(System.Drawing.Color.LightSkyBlue, 1), points);

                float outputHz = 0;
                new Visuals().DetectPeakFrequency(bgmStream, out outputHz);

                if (Bass.BASS_ChannelIsActive(bgmStream) == BASSActive.BASS_ACTIVE_PLAYING)
                {
                    try
                    {
                        var currentSample = (int)(currentTime / sampleTime);
                        if (currentSample < waveEnergies.Length - 1)
                        {
                            waveEnergies[currentSample] = outputHz;
                        }
                    }
                    catch { }
                }
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
            }));
        }
        private void UpdateTimeDisplay()
        {
            var currentPlayTime = Bass.BASS_ChannelBytes2Seconds(bgmStream, Bass.BASS_ChannelGetPosition(bgmStream));
            int minute = (int)currentPlayTime / 60;
            double second = (int)( currentPlayTime - (60 * minute));
            Dispatcher.Invoke(new Action(() => { TimeLabel.Content = String.Format("{0}:{1:00}", minute, second); }));
        }
        private void WaveStopMonitorUpdate()
        {
            // 监控是否应当停止
            if (!isPlan2Stop &&
                isPlaying &&
                Bass.BASS_ChannelIsActive(bgmStream) == BASSActive.BASS_ACTIVE_STOPPED)
            {
                isPlan2Stop = true;
                if (extraTime4AllPerfect < 0)
                {
                    // 足够播完 直接停止
                    Dispatcher.Invoke(() => { ToggleStop(); });
                }
                else
                {
                    // 不够播完 等待后停止
                    Timer stopPlayingTimer = new Timer((int)(extraTime4AllPerfect * 1000))
                    {
                        AutoReset = false
                    };
                    stopPlayingTimer.Elapsed += (object sender, ElapsedEventArgs e) =>
                    {
                        Dispatcher.Invoke(() =>
                        {
                            ToggleStop();
                        });
                    };
                    stopPlayingTimer.Start();
                }
            }
        }
        void ScrollWave(double delta)
        {
            if (Bass.BASS_ChannelIsActive(bgmStream) == BASSActive.BASS_ACTIVE_PLAYING)
                TogglePause();

            var time = Bass.BASS_ChannelBytes2Seconds(bgmStream, Bass.BASS_ChannelGetPosition(bgmStream));
            var destnationTime = time + (0.002d * -delta * (1.0d / zoominPower));
            SetBgmPosition(destnationTime);
            SimaiProcess.ClearNoteListPlayedState();

            SeekTextFromTime();
        }
        public static string GetLocalizedString(string key, string resourceFileName = "Langs", bool addSpaceAfter = false)
        {
            var localizedString = String.Empty;

            // Build up the fully-qualified name of the key
            var assemblyName = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name;
            var fullKey = assemblyName + ":" + resourceFileName + ":" + key;
            var locExtension = new LocExtension(fullKey);
            locExtension.ResolveLocalizedValue(out localizedString);

            // Add a space to the end, if requested
            if (addSpaceAfter)
            {
                localizedString += " ";
            }

            return localizedString;
        }

        double GetAllPerfectStartTime()
        {
            // 获取All Perfect理论上播放的时间点 也就是最后一个被完成的note
            double latestNoteFinishTime = -1;
            double baseTime, noteTime;
            foreach (var noteGroup in SimaiProcess.notelist)
            {
                baseTime = noteGroup.time;
                foreach (var note in noteGroup.getNotes())
                {
                    if(note.noteType == SimaiNoteType.Tap || note.noteType == SimaiNoteType.Touch)
                    {
                        noteTime = baseTime;
                    }else if(note.noteType == SimaiNoteType.Hold || note.noteType == SimaiNoteType.TouchHold)
                    {
                        noteTime = baseTime + note.holdTime;
                    }else if(note.noteType == SimaiNoteType.Slide)
                    {
                        noteTime = note.slideStartTime + note.slideTime;
                    }
                    else
                    {
                        noteTime = -1;
                    }
                    if(noteTime > latestNoteFinishTime)
                    {
                        latestNoteFinishTime = noteTime;
                    }
                }
            }
            return latestNoteFinishTime;
        }

        //*PLAY CONTROL
        void TogglePlay(bool isOpIncluded = false)
        {
            if (Export_Button.IsEnabled == false) return;

            if (lastEditorState == EditorControlMethod.Start || isOpIncluded)
            {
                if (!sendRequestStop()) return;
            }

            FumenContent.Focus();
            SaveFumen();
            if (CheckAndStartView()) return;
            Export_Button.IsEnabled = false;
            isPlaying = true;
            isPlan2Stop = false;

            double apTime = GetAllPerfectStartTime();
            double waveLength = Bass.BASS_ChannelBytes2Seconds(bgmStream, Bass.BASS_ChannelGetLength(bgmStream));
            if (waveLength < apTime + 4.0)
            {
                // 如果BGM的时长不足以播放完AP特效 这里假设AP特效持续4秒
                extraTime4AllPerfect = apTime + 4.0 - waveLength; // 预留给AP的额外时间（播放结束后）
                Debug.Print(extraTime4AllPerfect.ToString());
            }
            else
            {
                // 如果足够播完 那么就等到BGM结束再停止
                extraTime4AllPerfect = -1;
            }

            PlayAndPauseButton.Content = "  ▌▌ ";
            var CusorTime = SimaiProcess.Serialize(GetRawFumenText(), GetRawFumenPosition());//scan first

            var startAt = DateTime.Now;
            if (isOpIncluded)
            {
                generateSoundEffectList(0.0, isOpIncluded);
                InternalSwitchWindow(false);
                Bass.BASS_ChannelSetPosition(bgmStream, 0);
                startAt = DateTime.Now.AddSeconds(5d);
                Bass.BASS_ChannelPlay(trackStartStream, true);
                if (!sendRequestRun(startAt, isOpIncluded)) return;
                Task.Run(() =>
                {
                    while (DateTime.Now.Ticks < startAt.Ticks )
                    {
                        if (lastEditorState != EditorControlMethod.Start)
                            return;
                    }
                    Dispatcher.Invoke(() =>
                    {
                        playStartTime = Bass.BASS_ChannelBytes2Seconds(bgmStream, Bass.BASS_ChannelGetPosition(bgmStream));
                        SimaiProcess.ClearNoteListPlayedState();
                        clickSoundTimer.Start();
                        waveStopMonitorTimer.Start();
                        Bass.BASS_ChannelPlay(bgmStream, false);
                    });
                });
            }
            else
            {
                playStartTime = Bass.BASS_ChannelBytes2Seconds(bgmStream, Bass.BASS_ChannelGetPosition(bgmStream));
                generateSoundEffectList(playStartTime, isOpIncluded);
                SimaiProcess.ClearNoteListPlayedState();
                clickSoundTimer.Start();
                waveStopMonitorTimer.Start();
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
                        if (!sendRequestRun(startAt, isOpIncluded)) return;
                    }
                });
            }

            DrawWave();
            DrawCusor(CusorTime); //then the wave could be draw
        }
        void TogglePause()
        {
            Export_Button.IsEnabled = true;
            isPlaying = false;
            isPlan2Stop = false;

            FumenContent.Focus();
            PlayAndPauseButton.Content = "▶";
            Bass.BASS_ChannelStop(bgmStream);
            Bass.BASS_ChannelStop(holdRiserStream);
            clickSoundTimer.Stop();
            waveStopMonitorTimer.Stop();
            sendRequestPause();
            DrawWave();
        }
        void ToggleStop()
        {
            Export_Button.IsEnabled = true;
            isPlaying = false;
            isPlan2Stop = false;

            FumenContent.Focus();
            PlayAndPauseButton.Content = "▶";
            Bass.BASS_ChannelStop(bgmStream);
            Bass.BASS_ChannelStop(holdRiserStream);
            clickSoundTimer.Stop();
            waveStopMonitorTimer.Stop();
            sendRequestStop();
            Bass.BASS_ChannelSetPosition(bgmStream, playStartTime);
            DrawWave();
        }
        void TogglePlayAndPause(bool isOpIncluded = false)
        {
            if (isPlaying)
            {
                TogglePause();
            }
            else
            {
                TogglePlay(isOpIncluded);
            }
        }
        void TogglePlayAndStop(bool isOpIncluded = false)
        {
            if (isPlaying)
            {
                ToggleStop();
            }
            else
            {
                TogglePlay(isOpIncluded);
            }
        }
        void SetPlaybackSpeed(float speed)
        {
            var scale = (speed - 1) * 100f;
            Bass.BASS_ChannelSetAttribute(bgmStream, BASSAttribute.BASS_ATTRIB_TEMPO, scale);
        }
        float GetPlaybackSpeed()
        {
            float speed = 0f;
            Bass.BASS_ChannelGetAttribute(bgmStream, BASSAttribute.BASS_ATTRIB_TEMPO, ref speed);
            return (speed / 100f) + 1f;
        }
        void SetBgmPosition(double time)
        {
            if (lastEditorState == EditorControlMethod.Pause)
            {
                sendRequestStop();
            }
            Bass.BASS_ChannelSetPosition(bgmStream, time);
        }


        //*VIEW COMMUNICATION
        bool sendRequestStop()
        {
            EditRequestjson requestStop = new EditRequestjson();
            requestStop.control = EditorControlMethod.Stop;
            string json = Newtonsoft.Json.JsonConvert.SerializeObject(requestStop);
            var response = WebControl.RequestPOST("http://localhost:8013/", json);
            if (response == "ERROR") { MessageBox.Show(GetLocalizedString("PortClear")); return false; }
            lastEditorState = EditorControlMethod.Stop;
            return true;
        }
        bool sendRequestPause()
        {
            EditRequestjson requestStop = new EditRequestjson();
            requestStop.control = EditorControlMethod.Pause;
            string json = Newtonsoft.Json.JsonConvert.SerializeObject(requestStop);
            var response = WebControl.RequestPOST("http://localhost:8013/", json);
            if (response == "ERROR") { MessageBox.Show(GetLocalizedString("PortClear")); return false; }
            lastEditorState = EditorControlMethod.Pause;
            return true;
        }
        bool sendRequestContinue(DateTime StartAt)
        {
            EditRequestjson request = new EditRequestjson();
            request.control = EditorControlMethod.Continue;
            request.startAt = StartAt.Ticks;
            request.startTime = (float)Bass.BASS_ChannelBytes2Seconds(bgmStream, Bass.BASS_ChannelGetPosition(bgmStream));
            request.audioSpeed = GetPlaybackSpeed();
            string json = Newtonsoft.Json.JsonConvert.SerializeObject(request);
            var response = WebControl.RequestPOST("http://localhost:8013/", json);
            if (response == "ERROR") { MessageBox.Show(GetLocalizedString("PortClear")); return false; }
            lastEditorState = EditorControlMethod.Start;
            return true;
        }
        bool sendRequestRun(DateTime StartAt,bool isOpIncluded)
        {

            Majson jsonStruct = new Majson();
            foreach (var note in SimaiProcess.notelist)
            {
                note.noteList = note.getNotes();
                jsonStruct.timingList.Add(note);
            }

            jsonStruct.title = SimaiProcess.title;
            jsonStruct.artist = SimaiProcess.artist;
            jsonStruct.level = SimaiProcess.levels[selectedDifficulty];
            jsonStruct.designer = SimaiProcess.designer;
            jsonStruct.difficulty = SimaiProcess.GetDifficultyText(selectedDifficulty);
            jsonStruct.diffNum = selectedDifficulty;

            string json = Newtonsoft.Json.JsonConvert.SerializeObject(jsonStruct);
            var path = maidataDir + "/majdata.json";
            System.IO.File.WriteAllText(path, json);

            EditRequestjson request = new EditRequestjson();
            if(isOpIncluded)
                request.control = EditorControlMethod.OpStart;
            else
                request.control = EditorControlMethod.Start;
            Dispatcher.Invoke(() =>
            {
                request.jsonPath = path;
                request.startAt = StartAt.Ticks;
                request.startTime = (float)Bass.BASS_ChannelBytes2Seconds(bgmStream, Bass.BASS_ChannelGetPosition(bgmStream));
                // request.playSpeed = float.Parse(ViewerSpeed.Text);
                // 将maimaiDX速度换算为View中的单位速度 MajSpeed = 107.25 / (71.4184491 * (MaiSpeed + 0.9975) ^ -0.985558604)
                request.noteSpeed = editorSetting.playSpeed;
                request.touchSpeed = editorSetting.touchSpeed;
                request.backgroundCover = editorSetting.backgroundCover;
                request.audioSpeed = GetPlaybackSpeed();
            });

            json = JsonConvert.SerializeObject(request);
            var response = WebControl.RequestPOST("http://localhost:8013/", json);
            if (response == "ERROR") { MessageBox.Show(GetLocalizedString("PortClear")); return false; }
            lastEditorState = EditorControlMethod.Start;
            return true;
        }

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
        [System.Runtime.InteropServices.DllImport("user32.dll", EntryPoint = "MoveWindow")]
        public static extern bool MoveWindow(System.IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern bool ShowWindow(IntPtr hwnd, int nCmdShow);
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern bool SwitchToThisWindow(IntPtr hWnd, bool fAltTab);

        bool CheckAndStartView()
        {
            if (Process.GetProcessesByName("MajdataView").Length == 0 && Process.GetProcessesByName("Unity").Length == 0)
            {
                var viewProcess = Process.Start("MajdataView.exe");
                Timer setWindowPosTimer = new Timer(2000);
                setWindowPosTimer.AutoReset = false;
                setWindowPosTimer.Elapsed += SetWindowPosTimer_Elapsed;
                setWindowPosTimer.Start();
                return true;
            }
            return false;
        }

        void InternalSwitchWindow(bool moveToPlace = true)
        {
            var windowPtr = FindWindow(null, "MajdataView");
            //var thisWindow = FindWindow(null, this.Title);
            ShowWindow(windowPtr, 5);//还原窗口
            SwitchToThisWindow(windowPtr, true);
            //SwitchToThisWindow(thisWindow, true);
            if (moveToPlace) InternalMoveWindow();
        }
        void InternalMoveWindow()
        {
            var windowPtr = FindWindow(null, "MajdataView");
            PresentationSource source = PresentationSource.FromVisual(this);

            double dpiX = 1, dpiY = 1;
            if (source != null)
            {
                dpiX = 96.0 * source.CompositionTarget.TransformToDevice.M11;
                dpiY = 96.0 * source.CompositionTarget.TransformToDevice.M22;
            }

            //Console.WriteLine(dpiX+" "+dpiY);
            dpiX = dpiX / 96d;
            dpiY = dpiY / 96d;

            var Height = this.Height * dpiY;
            var Left = this.Left * dpiX;
            var Top = this.Top * dpiY;
            MoveWindow(windowPtr,
                (int)(Left - Height + 20),
                (int)Top,
                (int)Height - 20,
                (int)Height, true);
        }
        void SetWindowGoldenPosition()
        {
            // 属于你的独享黄金位置
            var ScreenWidth = SystemParameters.PrimaryScreenWidth;
            var ScreenHeight = SystemParameters.PrimaryScreenHeight;

            this.Left = (ScreenWidth - this.Width + Height) / 2 - 10;
            this.Top = (ScreenHeight - this.Height) / 2;
        }
        void generateSoundEffectList(double startTime, bool isOpIncluded)
        {
            waitToBePlayed = new List<SoundEffectTiming>();
            if (isOpIncluded)
            {
                var cmds = SimaiProcess.other_commands.Split('\n');
                foreach (var cmdl in cmds)
                {
                    if (cmdl.Length > 12 && cmdl.Substring(1,11)=="clock_count")
                    {
                        try
                        {
                            int clock_cnt = int.Parse(cmdl.Substring(13));
                            double clock_int = 60.0d / SimaiProcess.notelist[0].currentBpm;
                            for (int i = 0; i < clock_cnt; i++)
                            {
                                waitToBePlayed.Add(new SoundEffectTiming(i*clock_int, _hasClock: true));
                            }
                        } catch
                        {

                        }
                    }
                }
            }
            foreach(var noteGroup in SimaiProcess.notelist)
            {
                if (noteGroup.time < startTime) { continue; }

                SoundEffectTiming stobj;

                // 如果目前为止已经有一个SE了 那么就直接使用这个SE
                var combIndex = waitToBePlayed.FindIndex(o => Math.Abs(o.time - noteGroup.time) < 0.001f);
                if (combIndex != -1)
                {
                    stobj = waitToBePlayed[combIndex];
                }
                else
                {
                    stobj = new SoundEffectTiming(noteGroup.time);
                }

                var notes = noteGroup.getNotes();
                foreach (SimaiNote note in notes)
                {
                    switch (note.noteType)
                    {
                        case SimaiNoteType.Tap:
                            {
                                stobj.hasAnswer = true;
                                if (note.isBreak)
                                {
                                    // 如果是Break 则有Break判定音和Break欢呼音（2600）
                                    stobj.hasBreak = true;
                                    stobj.hasJudgeBreak = true;
                                }
                                if (note.isEx)
                                {
                                    // 如果是Ex 则有Ex判定音
                                    stobj.hasJudgeEx = true;
                                }
                                if (!note.isBreak && !note.isEx)
                                {
                                    // 如果二者皆没有 则是普通note 播放普通判定音
                                    stobj.hasJudge = true;
                                }
                                break;
                            }
                        case SimaiNoteType.Hold:
                            {
                                stobj.hasAnswer = true;
                                // 类似于Tap 判断Break和Ex的音效 二者皆无则为普通
                                if (note.isBreak)
                                {
                                    stobj.hasBreak = true;
                                    stobj.hasJudgeBreak = true;
                                }
                                if (note.isEx)
                                {
                                    stobj.hasJudgeEx = true;
                                }
                                if (!note.isBreak && !note.isEx)
                                {
                                    stobj.hasJudge = true;
                                }

                                // 计算Hold尾部的音效
                                if (!(note.holdTime <= 0.00f))
                                {
                                    // 如果是短hold（六角tap），则忽略尾部音效。否则，才会计算尾部音效
                                    var targetTime = noteGroup.time + note.holdTime;
                                    var nearIndex = waitToBePlayed.FindIndex(o => Math.Abs(o.time - targetTime) < 0.001f);
                                    if (nearIndex != -1)
                                    {
                                        waitToBePlayed[nearIndex].hasAnswer = true;
                                        if (!note.isBreak && !note.isEx)
                                        {
                                            waitToBePlayed[nearIndex].hasJudge = true;
                                        }
                                    }
                                    else
                                    {
                                        // 只有最普通的Hold才有结尾的判定音 Break和Ex型则没有（Break没有为推定）
                                        SoundEffectTiming holdRelease = new SoundEffectTiming(targetTime, _hasAnswer: true, _hasJudge: !note.isBreak && !note.isEx);
                                        waitToBePlayed.Add(holdRelease);
                                    }
                                }
                                break;
                            }
                        case SimaiNoteType.Slide:
                            {
                                if (!note.isSlideNoHead)
                                {
                                    // 当Slide不是无头星星的时候 才有answer音和判定音
                                    stobj.hasAnswer = true;
                                    if (note.isBreak)
                                    {
                                        stobj.hasBreak = true;
                                        stobj.hasJudgeBreak = true;
                                    }
                                    if (note.isEx)
                                    {
                                        stobj.hasJudgeEx = true;
                                    }
                                    if (!note.isBreak && !note.isEx)
                                    {
                                        stobj.hasJudge = true;
                                    }
                                }

                                // Slide启动音效
                                var targetTime = note.slideStartTime;
                                var nearIndex = waitToBePlayed.FindIndex(o => Math.Abs(o.time - targetTime) < 0.001f);
                                if (nearIndex != -1)
                                {
                                    waitToBePlayed[nearIndex].hasSlide = true;
                                }
                                else
                                {
                                    SoundEffectTiming slide = new SoundEffectTiming(targetTime, _hasSlide: true);
                                    waitToBePlayed.Add(slide);
                                }
                                // Slide尾巴 如果是Break Slide的话 就要添加一个Break音效
                                if (note.isSlideBreak)
                                {
                                    targetTime = note.slideStartTime + note.slideTime;
                                    nearIndex = waitToBePlayed.FindIndex(o => Math.Abs(o.time - targetTime) < 0.001f);
                                    if (nearIndex != -1)
                                    {
                                        waitToBePlayed[nearIndex].hasBreak = true;
                                    }
                                    else
                                    {
                                        SoundEffectTiming slide = new SoundEffectTiming(targetTime, _hasBreak: true);
                                        waitToBePlayed.Add(slide);
                                    }
                                }
                                break;
                            }
                        case SimaiNoteType.Touch:
                            {
                                stobj.hasAnswer = true;
                                stobj.hasTouch = true;
                                if (note.isHanabi)
                                {
                                    stobj.hasHanabi = true;
                                }
                                break;
                            }
                        case SimaiNoteType.TouchHold:
                            {
                                stobj.hasAnswer = true;
                                stobj.hasTouch = true;
                                stobj.hasTouchHold = true;
                                // 计算TouchHold结尾
                                var targetTime = noteGroup.time + note.holdTime;
                                var nearIndex = waitToBePlayed.FindIndex(o => Math.Abs(o.time - targetTime) < 0.001f);
                                if (nearIndex != -1)
                                {
                                    if (note.isHanabi)
                                    {
                                        waitToBePlayed[nearIndex].hasHanabi = true;
                                    }
                                    waitToBePlayed[nearIndex].hasAnswer = true;
                                    waitToBePlayed[nearIndex].hasTouchHoldEnd = true;
                                }
                                else
                                {
                                    SoundEffectTiming tHoldRelease = new SoundEffectTiming(targetTime, _hasAnswer: true, _hasHanabi: note.isHanabi, _hasTouchHoldEnd: true);
                                    waitToBePlayed.Add(tHoldRelease);
                                }
                                break;
                            }
                    }
                }

                if (combIndex != -1)
                {
                    waitToBePlayed[combIndex] = stobj;
                }
                else
                {
                    waitToBePlayed.Add(stobj);
                }
            }
            if (isOpIncluded)
            {
                waitToBePlayed.Add(new SoundEffectTiming(GetAllPerfectStartTime(), _hasAllPerfect: true));
            }
            waitToBePlayed.Sort((o1,o2) => o1.time<o2.time?-1:1);
            Console.WriteLine(JsonConvert.SerializeObject(waitToBePlayed));
        }

        private void SwitchFumenOverwriteMode()
        {
            fumenOverwriteMode = !fumenOverwriteMode;

            //修改覆盖模式启用状态
            // fetch TextEditor from FumenContent
            var textEditorProperty = typeof(TextBox).GetProperty("TextEditor", BindingFlags.NonPublic | BindingFlags.Instance);
            var textEditor = textEditorProperty.GetValue(FumenContent, null);

            // set _OvertypeMode on the TextEditor
            var overtypeModeProperty = textEditor.GetType().GetProperty("_OvertypeMode", BindingFlags.NonPublic | BindingFlags.Instance);
            overtypeModeProperty.SetValue(textEditor, fumenOverwriteMode, null);

            //修改提示弹窗可见性
            OverrideModeTipsPopup.Visibility = fumenOverwriteMode ? Visibility.Visible : Visibility.Collapsed;
        }

        private void CheckUpdate(bool onStart = false)
        {
            if (UpdateCheckLock)
            {
                return;
            }
            UpdateCheckLock = true;

            #region 子函数
            SemVersion oldVersionCompatible(string versionString)
            {
                SemVersion result = SemVersion.Parse("v0.0.0", SemVersionStyles.Any);
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
                        int startPos = versionString.IndexOfAny("0123456789".ToArray());
                        versionString = "0." + versionString.Substring(startPos);
                        if (versionString.Count((c) => { return c == '.'; }) > 2)
                        {
                            versionString = versionString.Substring(0, versionString.LastIndexOf('.'));
                        }
                        versionString += "-alpha";
                        result = SemVersion.Parse(versionString, SemVersionStyles.Any);
                    }
                    else if (versionString.Contains("Beta"))
                    {
                        // 旧版本格式 Beta<MainVersion>.<SubVersion>[.<ModifiedVersion>]
                        // 从1.0开始，结束于3.1。后续的语义化版本号继承该版本号进度，从4.0开始
                        // 增加 -beta 后缀
                        int startPos = versionString.IndexOfAny("0123456789".ToArray());
                        versionString = versionString.Substring(startPos);
                        if (versionString.Contains(' '))
                        {
                            versionString = versionString.Substring(0, versionString.IndexOf(' '));
                        }
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
            void requestHandler(object sender, System.Net.DownloadDataCompletedEventArgs e)
            {
                UpdateCheckLock = false;

                if (e.Error != null)
                {
                    // 网络请求失败
                    if (!onStart)
                    {
                        MessageBox.Show(GetLocalizedString("RequestFail"), GetLocalizedString("CheckUpdate"));
                    }
                    return;
                }

                string response = Encoding.UTF8.GetString(e.Result);
                var resJson = JsonConvert.DeserializeObject<JObject>(response);
                string latestVersionString = resJson["tag_name"].ToString();
                string releaseUrl = resJson["html_url"].ToString();

                SemVersion latestVersion = oldVersionCompatible(latestVersionString);

                if (latestVersion.ComparePrecedenceTo(MAJDATA_VERSION) > 0)
                {
                    // 版本不同，需要更新
                    string msgboxText = String.Format(GetLocalizedString("NewVersionDetected"), latestVersionString, MAJDATA_VERSION_STRING);
                    if (onStart)
                    {
                        msgboxText += "\n\n" + GetLocalizedString("AutoUpdateCheckTip");
                    }

                    MessageBoxResult result = MessageBox.Show(
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
                    if (!onStart)
                    {
                        MessageBox.Show(GetLocalizedString("NoNewVersion"), GetLocalizedString("CheckUpdate"));
                    }
                }
            }
            #endregion

            // 检查是否需要更新软件
            WebControl.RequestGETAsync("http://api.github.com/repos/LingFeng-bbben/MajdataView/releases/latest", requestHandler);
            
        }

        public string GetWindowsTitleString()
        {
            return "MajdataEdit(" + MAJDATA_VERSION_STRING + ")";
        }

        public string GetWindowsTitleString(string info)
        {
            return GetWindowsTitleString() + " - " + info;
        }

        class SoundEffectTiming
        {
            public bool hasAnswer = false;
            public bool hasJudge = false;
            public bool hasJudgeBreak = false;
            public bool hasBreak = false;
            public bool hasTouch = false;
            public bool hasHanabi = false;
            public bool hasJudgeEx = false;
            public bool hasTouchHold = false;
            public bool hasTouchHoldEnd = false;
            public bool hasSlide = false;
            public bool hasAllPerfect = false;
            public bool hasClock = false;
            public double time;

            public SoundEffectTiming(double _time, bool _hasAnswer = false, bool _hasJudge = false, bool _hasJudgeBreak = false,
                                     bool _hasBreak = false, bool _hasTouch = false, bool _hasHanabi = false,
                                     bool _hasJudgeEx = false, bool _hasTouchHold = false, bool _hasSlide = false,
                                     bool _hasTouchHoldEnd = false, bool _hasAllPerfect = false, bool _hasClock = false)
            {
                time = _time;
                hasAnswer = _hasAnswer;
                hasJudge = _hasJudge;
                hasJudgeBreak = _hasJudgeBreak; // 我是笨蛋
                hasBreak = _hasBreak;
                hasTouch = _hasTouch;
                hasHanabi = _hasHanabi;
                hasJudgeEx = _hasJudgeEx;
                hasTouchHold = _hasTouchHold;
                hasSlide = _hasSlide;
                hasTouchHoldEnd = _hasTouchHoldEnd;
                hasAllPerfect = _hasAllPerfect;
                hasClock = _hasClock;
            }
        }
    }
}
