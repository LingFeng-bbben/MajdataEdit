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

namespace MajdataEdit
{
    public partial class MainWindow : Window
    {
        Timer currentTimeRefreshTimer = new Timer(100);
        Timer clickSoundTimer = new Timer(10);
        Timer VisualEffectRefreshTimer = new Timer(1);

        SoundSetting soundSetting = new SoundSetting();

        public int bgmStream = -1024;
        public int clickStream = -8848;
        public int breakStream = -114514;
        public int exStream = -1919;
        public int hanabiStream = -810;
        public int holdRiserStream = -52013;
        public int trackStartStream = 66065;

        public string maidataDir;
        const string majSettingFilename = "majSetting.json";
        const string editorSettingFilename = "EditorSetting.json";

        float[] waveLevels;
        float[] waveEnergies;

        double playStartTime = 0d;

        bool isDrawing = false;
        bool isSaved = true;

        int selectedDifficulty = -1;
        float sampleTime = 0.02f;
        int zoominPower = 4;
        EditorControlMethod lastEditorState;

        double lastMousePointX; //Used for drag scroll

        //*TEXTBOX CONTROL
        string GetRawFumenText()
        {
            string text = "";
            text = new TextRange(FumenContent.Document.ContentStart, FumenContent.Document.ContentEnd).Text;
            text = text.Replace("\r", "");
            return text;
        }
        void SetRawFumenText(string content)
        {
            FumenContent.Document.Blocks.Clear();
            if (content == null) return;
            string[] lines = content.Split('\n');
            foreach (var line in lines)
            {
                Paragraph paragraph = new Paragraph();
                paragraph.Inlines.Add(line);
                FumenContent.Document.Blocks.Add(paragraph);
            }
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

        //*FILE CONTROL
        void initFromFile(string path)//file name should not be included in path
        {
            var audioPath = path + "/track.mp3";
            var dataPath = path + "/maidata.txt";
            if (!File.Exists(audioPath)) MessageBox.Show("请存入track.mp3", "错误");
            if (!File.Exists(audioPath)) MessageBox.Show("未找到maidata.txt", "错误");
            maidataDir = path;
            SetRawFumenText("");
            if (bgmStream != -1024)
            {
                Bass.BASS_ChannelStop(bgmStream);
                Bass.BASS_StreamFree(bgmStream);
            }
            soundSetting.Close();
            var decodeStream = Bass.BASS_StreamCreateFile(audioPath, 0L, 0L, BASSFlag.BASS_STREAM_DECODE);
            bgmStream = BassFx.BASS_FX_TempoCreate(decodeStream, BASSFlag.BASS_FX_FREESOURCE);
            //Bass.BASS_StreamCreateFile(audioPath, 0L, 0L, BASSFlag.BASS_SAMPLE_FLOAT);

            Bass.BASS_ChannelSetAttribute(bgmStream, BASSAttribute.BASS_ATTRIB_VOL, 0.7f);
            var info = Bass.BASS_ChannelGetInfo(bgmStream);
            if (info.freq != 44100) MessageBox.Show("Simai可能不支持非44100Hz的mp3文件", "注意");

            SimaiProcess.ClearData();

            if (!SimaiProcess.ReadData(dataPath))
            {
                return;
            }



            ReadWaveFromFile();
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
            MenuSetting.IsEnabled = true;
            SetSavedState(true);
        }
        private void ReadWaveFromFile()
        {
            var bgmDecode = Bass.BASS_StreamCreateFile(maidataDir + "/track.mp3", 0L, 0L, BASSFlag.BASS_STREAM_DECODE);
            var length = Bass.BASS_ChannelBytes2Seconds(bgmDecode, Bass.BASS_ChannelGetLength(bgmDecode,BASSMode.BASS_POS_BYTE));
            int sampleNumber = (int)((length * 1000) / (sampleTime * 1000)) + 1;
            waveLevels = new float[sampleNumber];
            waveEnergies = new float[sampleNumber];
            for (int i = 0; i < sampleNumber; i++)
            {
                waveLevels[i] = Bass.BASS_ChannelGetLevels(bgmDecode, sampleTime, BASSLevel.BASS_LEVEL_MONO)[0];
                waveEnergies[i] = 0.01f;
            }
            Bass.BASS_StreamFree(bgmDecode);
        }
        void SetSavedState(bool state)
        {
            if (state)
            {
                isSaved = true;
                LevelSelector.IsEnabled = true;
                TheWindow.Title = "MajdataEdit - " + SimaiProcess.title;
            }
            else
            {
                isSaved = false;
                LevelSelector.IsEnabled = false;
                TheWindow.Title = "MajdataEdit - (未保存)" + SimaiProcess.title;
            }
        }
        /// <summary>
        /// Ask the user and save fumen.
        /// </summary>
        /// <returns>Return false if user cancel the action</returns>
        bool AskSave()
        {
            var result = MessageBox.Show("未保存，要保存吗？", "警告", MessageBoxButton.YesNoCancel);
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
            setting.backgroundCover = float.Parse(ViewerCover.Text);
            setting.playSpeed = float.Parse(ViewerSpeed.Text);
            setting.lastEditDiff = selectedDifficulty;
            setting.lastEditTime = Bass.BASS_ChannelBytes2Seconds(bgmStream, Bass.BASS_ChannelGetPosition(bgmStream));
            Bass.BASS_ChannelGetAttribute(bgmStream, BASSAttribute.BASS_ATTRIB_VOL, ref setting.BGM_Level);
            Bass.BASS_ChannelGetAttribute(clickStream, BASSAttribute.BASS_ATTRIB_VOL, ref setting.Tap_Level);
            Bass.BASS_ChannelGetAttribute(breakStream, BASSAttribute.BASS_ATTRIB_VOL, ref setting.Break_Level);
            Bass.BASS_ChannelGetAttribute(exStream, BASSAttribute.BASS_ATTRIB_VOL, ref setting.Ex_Level);
            Bass.BASS_ChannelGetAttribute(hanabiStream, BASSAttribute.BASS_ATTRIB_VOL, ref setting.Hanabi_Level);
            string json = JsonConvert.SerializeObject(setting);
            File.WriteAllText(maidataDir + "/" + majSettingFilename,json);
        }
        void ReadSetting()
        {
            var path = maidataDir + "/" + majSettingFilename;
            if (!File.Exists(path)) return;
            var setting = JsonConvert.DeserializeObject<MajSetting>(File.ReadAllText(path));
            ViewerCover.Text = setting.backgroundCover.ToString();
            ViewerSpeed.Text = setting.playSpeed.ToString();
            LevelSelector.SelectedIndex = setting.lastEditDiff;
            selectedDifficulty = setting.lastEditDiff;
            SetBgmPosition(setting.lastEditTime);
            Bass.BASS_ChannelSetAttribute(bgmStream, BASSAttribute.BASS_ATTRIB_VOL,setting.BGM_Level);
            Bass.BASS_ChannelSetAttribute(clickStream, BASSAttribute.BASS_ATTRIB_VOL, setting.Tap_Level);
            Bass.BASS_ChannelSetAttribute(breakStream, BASSAttribute.BASS_ATTRIB_VOL, setting.Break_Level);
            Bass.BASS_ChannelSetAttribute(exStream, BASSAttribute.BASS_ATTRIB_VOL, setting.Ex_Level);
            Bass.BASS_ChannelSetAttribute(hanabiStream, BASSAttribute.BASS_ATTRIB_VOL, setting.Hanabi_Level);
        }
        private void CreateNewFumen(string path)
        {
            if (File.Exists(path + "/maidata.txt"))
            {
                MessageBox.Show("maidata.txt已存在");
            }
            else
            {
                File.WriteAllText(path + "/maidata.txt",
                    "&title=请设置标题\n" +
                    "&artist=请设置艺术家\n" +
                    "&des=请设置做谱人\n" +
                    "&first=0\n");
            }
        }
        void CreateEditorSetting()
        {
            EditorSetting setting = new EditorSetting();
            File.WriteAllText(editorSettingFilename, JsonConvert.SerializeObject(setting, Formatting.Indented));
        }
        void ReadEditorSetting()
        {
            if (!File.Exists(editorSettingFilename))
            {
                CreateEditorSetting();
                return;
            }
            var json = File.ReadAllText(editorSettingFilename);
            var setting = JsonConvert.DeserializeObject<EditorSetting>(json);
            AddGesture(setting.PlayPauseKey, "PlayAndPause");
            AddGesture(setting.PlayStopKey, "StopPlaying");
            AddGesture(setting.SaveKey, "SaveFile");
            AddGesture(setting.SendViewerKey, "SendToView");
            AddGesture(setting.IncreasePlaybackSpeedKey, "IncreasePlaybackSpeed");
            AddGesture(setting.DecreasePlaybackSpeedKey, "DecreasePlaybackSpeed");
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
            clickStream = Bass.BASS_StreamCreateFile(path + "tap.mp3", 0L, 0L, BASSFlag.BASS_SAMPLE_FLOAT);
            breakStream = Bass.BASS_StreamCreateFile(path + "break.mp3", 0L, 0L, BASSFlag.BASS_SAMPLE_FLOAT);
            exStream = Bass.BASS_StreamCreateFile(path + "ex.mp3", 0L, 0L, BASSFlag.BASS_SAMPLE_FLOAT);
            hanabiStream = Bass.BASS_StreamCreateFile(path + "hanabi.mp3", 0L, 0L, BASSFlag.BASS_SAMPLE_FLOAT);
            holdRiserStream = Bass.BASS_StreamCreateFile(path + "touchHold_riser.mp3", 0L, 0L, BASSFlag.BASS_SAMPLE_FLOAT);
            trackStartStream = Bass.BASS_StreamCreateFile(path + "track_start.mp3", 0L, 0L, BASSFlag.BASS_SAMPLE_FLOAT);
        }
        private void SoundEffectUpdate()
        {
            try
            {
                var currentTime = Bass.BASS_ChannelBytes2Seconds(bgmStream, Bass.BASS_ChannelGetPosition(bgmStream));
                var waitToBePlayed = SimaiProcess.notelist.FindAll(o => o.havePlayed == false && o.time > currentTime);
                //foreach(var a in waitToBePlayed) Console.WriteLine("Sort:"+a.time);
                if (waitToBePlayed.Count < 1) return;
                var nearestTime = waitToBePlayed[0].time;
                //Console.WriteLine(nearestTime);
                if (currentTime - nearestTime < 0.05 && currentTime - nearestTime > -0.05)
                {
                    var notes = waitToBePlayed[0].getNotes();
                    Bass.BASS_ChannelPlay(clickStream, true);

                    if (notes.FindAll(o => o.isBreak).Count > 0) //may cause delay
                    {
                        Bass.BASS_ChannelPlay(breakStream, true);
                    }
                    if (notes.FindAll(o => o.isHanabi && o.noteType == SimaiNoteType.Touch).Count > 0) //may cause delay
                    {
                        Bass.BASS_ChannelPlay(hanabiStream, true);
                    }
                    if (notes.FindAll(o => o.isEx).Count > 0)
                    {
                        Bass.BASS_ChannelPlay(exStream, true);
                    }
                    if (notes.FindAll(o => o.noteType == SimaiNoteType.TouchHold).Count > 0)
                    {
                        Bass.BASS_ChannelPlay(holdRiserStream, true);
                    }
                    //
                    Dispatcher.Invoke(() => {

                        if ((bool)FollowPlayCheck.IsChecked)
                            SeekTextFromTime();
                    });
                    //Console.WriteLine(waitToBePlayed[0].content);
                    SimaiProcess.notelist.FindAll(o => o.havePlayed == false && o.time > currentTime)[0].havePlayed = true; //Since the data was added as time followed, we modify the first one
                    foreach (var note in notes)
                    {
                        if (note.noteType == SimaiNoteType.Hold)
                        {
                            Timer holdClickTimer = new Timer(note.holdTime * 1000d * (1 / GetPlaybackSpeed()));
                            holdClickTimer.Elapsed += HoldClickTimer_Elapsed;
                            holdClickTimer.AutoReset = false;
                            holdClickTimer.Start();
                        }
                        if (note.noteType == SimaiNoteType.TouchHold)
                        {
                            Timer holdClickTimer = new Timer(note.holdTime * 1000d * (1 / GetPlaybackSpeed()));
                            holdClickTimer.Elapsed += HoldRiserTimer_Elapsed;
                            holdClickTimer.AutoReset = false;
                            holdClickTimer.Start();
                        }
                        if (note.noteType == SimaiNoteType.TouchHold && note.isHanabi)
                        {
                            Timer holdClickTimer = new Timer(note.holdTime * 1000d * (1 / GetPlaybackSpeed()));
                            holdClickTimer.Elapsed += HoldHanibiTimer_Elapsed;
                            holdClickTimer.Elapsed += HoldRiserTimer_Elapsed;
                            holdClickTimer.AutoReset = false;
                            holdClickTimer.Start();
                        }
                    }
                }

            }
            catch { }
        }
        private void HoldClickTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            Bass.BASS_ChannelPlay(clickStream, true);
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
                    graphics.Clear(System.Drawing.Color.Black);
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
                    pen = new System.Drawing.Pen(System.Drawing.Color.Yellow, 1);
                    for (int i = 1; i < bpmChangeTimes.Count; i++)
                    {
                        while (time < bpmChangeTimes[i])//在那个时间之前都是之前的bpm
                        {
                            if (currentBeat > beat) currentBeat = 1;
                            var xbase = (float)(time / sampleTime) * zoominPower;
                            var timePerBeat = 1d / (bpmChangeValues[i - 1] / 60d);
                            float xoneBeat = (float)(timePerBeat / sampleTime) * zoominPower;
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
                        bool isEach = notes.Count > 1;

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
                                pen.Width = 2;
                                pen.Color = isEach ? System.Drawing.Color.Gold : System.Drawing.Color.LightPink;
                                graphics.DrawEllipse(pen, x - 2.5f, y - 2.5f, 5, 5);
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
                                pen.Color = isEach ? System.Drawing.Color.Gold : System.Drawing.Color.LightPink;

                                float xRight = x + (float)(noteD.holdTime / sampleTime) * zoominPower;
                                if (xRight - x < 1f) xRight = x + 5;
                                graphics.DrawLine(pen, x, y, xRight, y);

                            }

                            if (noteD.noteType == SimaiNoteType.TouchHold)
                            {
                                pen.Width = 3;
                                float xDelta = ((float)(noteD.holdTime / sampleTime) * zoominPower) / 4;
                                //Console.WriteLine("HoldPixel"+ xDelta);
                                if (xDelta < 1f) xDelta = x + 1;

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
                                pen.Color = System.Drawing.Color.DeepSkyBlue;
                                System.Drawing.Brush brush = new SolidBrush(pen.Color);
                                graphics.DrawString("*", new Font("Consolas", 12, System.Drawing.FontStyle.Bold), brush, new PointF(x - 7f, y - 7f));

                                pen.Color = System.Drawing.Color.SkyBlue;
                                pen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dot;
                                float xSlide = (float)(noteD.slideStartTime / sampleTime) * zoominPower;
                                float xSlideRight = (float)(noteD.slideTime / sampleTime) * zoominPower + xSlide;
                                graphics.DrawLine(pen, xSlide, y, xSlideRight, y);
                                pen.DashStyle = System.Drawing.Drawing2D.DashStyle.Solid;
                            }



                        }

                    }
                }
                catch { }
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
            double second = currentPlayTime % 60d;
            Dispatcher.Invoke(new Action(() => { TimeLabel.Content = String.Format("{0}:{1:00}", minute, second); }));
        }
        void ScrollWave(double delta)
        {
            var time = Bass.BASS_ChannelBytes2Seconds(bgmStream, Bass.BASS_ChannelGetPosition(bgmStream));
            var destnationTime = time + (0.002d * -delta * (1.0d / zoominPower));
            SetBgmPosition(destnationTime);
            SimaiProcess.ClearNoteListPlayedState();

            SeekTextFromTime();
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
            PlayAndPauseButton.Content = "  ▌▌ ";
            var CusorTime = SimaiProcess.Serialize(GetRawFumenText(), GetRawFumenPosition());//scan first

            var startAt = DateTime.Now;
            if (isOpIncluded)
            {
                InternalSwitchWindow(false);
                Bass.BASS_ChannelSetPosition(bgmStream, 0);
                startAt = DateTime.Now.AddSeconds(5d);
                Bass.BASS_ChannelPlay(trackStartStream, true);
                if (!sendRequestRun(startAt, isOpIncluded)) return;
                Task.Run(() =>
                {
                    while (DateTime.Now.Ticks < startAt.Ticks) ;
                    Dispatcher.Invoke(() =>
                    {
                        playStartTime = Bass.BASS_ChannelBytes2Seconds(bgmStream, Bass.BASS_ChannelGetPosition(bgmStream));
                        SimaiProcess.ClearNoteListPlayedState();
                        clickSoundTimer.Start();
                        Bass.BASS_ChannelPlay(bgmStream, false);
                    });
                });
            }
            else
            {
                playStartTime = Bass.BASS_ChannelBytes2Seconds(bgmStream, Bass.BASS_ChannelGetPosition(bgmStream));
                SimaiProcess.ClearNoteListPlayedState();
                clickSoundTimer.Start();
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

            FumenContent.Focus();
            PlayAndPauseButton.Content = "▶";
            Bass.BASS_ChannelStop(bgmStream);
            clickSoundTimer.Stop();
            sendRequestPause();
            DrawWave();
        }
        void ToggleStop()
        {
            Export_Button.IsEnabled = true;


            FumenContent.Focus();
            PlayAndPauseButton.Content = "▶";
            Bass.BASS_ChannelStop(bgmStream);
            clickSoundTimer.Stop();
            sendRequestStop();
            Bass.BASS_ChannelSetPosition(bgmStream, playStartTime);
            DrawWave();
        }
        void TogglePlayAndPause(bool isOpIncluded = false)
        {
            if (Bass.BASS_ChannelIsActive(bgmStream) == BASSActive.BASS_ACTIVE_PLAYING)
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
            if (Bass.BASS_ChannelIsActive(bgmStream) == BASSActive.BASS_ACTIVE_PLAYING)
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
            if (response == "ERROR") { MessageBox.Show("请确保你打开了MajdataView且端口（8013）畅通"); return false; }
            lastEditorState = EditorControlMethod.Stop;
            return true;
        }
        bool sendRequestPause()
        {
            EditRequestjson requestStop = new EditRequestjson();
            requestStop.control = EditorControlMethod.Pause;
            string json = Newtonsoft.Json.JsonConvert.SerializeObject(requestStop);
            var response = WebControl.RequestPOST("http://localhost:8013/", json);
            if (response == "ERROR") { MessageBox.Show("请确保你打开了MajdataView且端口（8013）畅通"); return false; }
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
            if (response == "ERROR") { MessageBox.Show("请确保你打开了MajdataView且端口（8013）畅通"); return false; }
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
                request.playSpeed = float.Parse(ViewerSpeed.Text);
                request.backgroundCover = float.Parse(ViewerCover.Text);
                request.audioSpeed = GetPlaybackSpeed();
            });

            json = Newtonsoft.Json.JsonConvert.SerializeObject(request);
            var response = WebControl.RequestPOST("http://localhost:8013/", json);
            if (response == "ERROR") { MessageBox.Show("请确保你打开了MajdataView且端口（8013）畅通"); return false; }
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
            ShowWindow(windowPtr, 1);//还原窗口
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
    }
}
