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
using Un4seen.Bass;
using Un4seen.Bass.Misc;
using System.Drawing;
using System.Media;

namespace MajdataEdit
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        int bgmStream = -1024;
        int clickStream = -8848;

        string GetRawFumenText()
        {
            string text = "";
            text = new TextRange(FumenContent.Document.ContentStart, FumenContent.Document.ContentEnd).Text;
            text = text.Replace("\r", "");
            return text;
        }

        void SaveRawFumenText(bool writeToDisk = false)
        {
            if (selectedDifficulty == -1) return;
            SimaiProcess.fumens[selectedDifficulty] = GetRawFumenText();
            SimaiProcess.simaiFirst = (bool)SimaiFirst.IsChecked;
            SimaiProcess.SaveData("maidata.bak.txt");
            if (writeToDisk)
                SimaiProcess.SaveData("maidata.txt");
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

        Timer currentTimeRefreshTimer = new Timer(100);
        Timer clickSoundTimer = new Timer(10);
        Timer VisualEffectRefreshTimer = new Timer(1);
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var handle = (new WindowInteropHelper(this)).Handle;
            Bass.BASS_Init(-1, 44100, BASSInit.BASS_DEVICE_CPSPEAKERS, handle);
            bgmStream = Bass.BASS_StreamCreateFile("track.mp3", 0L, 0L, BASSFlag.BASS_SAMPLE_FLOAT);
            clickStream = Bass.BASS_StreamCreateFile("tap.mp3", 0L, 0L, BASSFlag.BASS_SAMPLE_FLOAT);

            Bass.BASS_ChannelSetAttribute(bgmStream, BASSAttribute.BASS_ATTRIB_VOL, 0.7f);

            currentTimeRefreshTimer.Elapsed += CurrentTimeRefreshTimer_Elapsed;
            currentTimeRefreshTimer.Start();
            clickSoundTimer.Elapsed += ClickSoundTimer_Elapsed;
            VisualEffectRefreshTimer.Elapsed += VisualEffectRefreshTimer_Elapsed;
            VisualEffectRefreshTimer.Start();

            var info = Bass.BASS_ChannelGetInfo(bgmStream);
            if (info.freq != 44100) MessageBox.Show("Simai可能不支持非44100Hz的mp3文件", "注意");

            SimaiProcess.ReadData("maidata.txt");
            SimaiFirst.IsChecked = SimaiProcess.simaiFirst;

            
            ReadWaveFromFile();
            SimaiProcess.getSongTimeAndScan(GetRawFumenText(), GetRawFumenPosition());
            DrawWave();
            FumenContent.Focus();
            LevelSelector.SelectedItem = LevelSelector.Items[0];
        }

        float[] waveLevels;
        float[] waveEnergies;
        private void ReadWaveFromFile()
        {
            var bgmDecode = Bass.BASS_StreamCreateFile("track.mp3", 0L, 0L, BASSFlag.BASS_STREAM_DECODE);
            var length = Bass.BASS_ChannelBytes2Seconds(bgmDecode, Bass.BASS_ChannelGetLength(bgmStream));
            int sampleNumber = (int)((length * 1000) / (sampleTime * 1000)) / 2 + 1;
            waveLevels = new float[sampleNumber];
            waveEnergies = new float[sampleNumber];
            for (int i = 0; i < sampleNumber; i++)
            {
                waveLevels[i] = Bass.BASS_ChannelGetLevels(bgmDecode, sampleTime, BASSLevel.BASS_LEVEL_MONO)[0];
                waveEnergies[i] = 0.01f;
            }
            Bass.BASS_StreamFree(bgmDecode);
        }

        float sampleTime = 0.02f;
        int zoominPower = 4;
        bool isDrawing = false;
        private async void DrawWave(double ghostCusorPositionTime = 0)
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
                                graphics.DrawEllipse(pen, x-2.5f, y-2.5f, 5, 5);
                            }

                            if (noteD.noteType == SimaiNoteType.Touch)
                            {
                                pen.Width = 2;
                                pen.Color = isEach ? System.Drawing.Color.Gold : System.Drawing.Color.DeepSkyBlue;
                                graphics.DrawRectangle(pen, x - 2.5f, y - 2.5f, 5, 5);

                            }

                            if (noteD.noteType== SimaiNoteType.Hold)
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
                                graphics.DrawString("*", new Font("Consolas", 12,System.Drawing.FontStyle.Bold), brush, new PointF(x-7f, y-7f));

                                pen.Color = System.Drawing.Color.SkyBlue;
                                pen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dot;
                                float xSlide = (float)(noteD.slideStartTime / sampleTime) * zoominPower;
                                float xSlideRight = (float)(noteD.slideTime / sampleTime) * zoominPower + xSlide;
                                graphics.DrawLine(pen, xSlide, y, xSlideRight, y);
                                pen.DashStyle = System.Drawing.Drawing2D.DashStyle.Solid;
                            }



                        }

                    }

                    //Draw timing lines
                    pen = new System.Drawing.Pen(System.Drawing.Color.White, 1);
                    foreach (var note in SimaiProcess.timinglist)
                    {
                        if (note == null) { break; }
                        float x = (float)(note.time / sampleTime) * zoominPower;
                        graphics.DrawLine(pen, x, 0, x, 10);
                        graphics.DrawLine(pen, x, 65, x, 75);
                    }

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

        private void VisualEffectRefreshTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            Dispatcher.Invoke(new Action(() =>
            {
                //Scroll WaveView
                var currentTime = Bass.BASS_ChannelBytes2Seconds(bgmStream, Bass.BASS_ChannelGetPosition(bgmStream));
                MusicWave.Margin = new Thickness(-currentTime / sampleTime * zoominPower, Margin.Left, MusicWave.Margin.Right, Margin.Bottom);//Todo:the scale
                                                                                                                                              //Draw FFT

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

        private void ClickSoundTimer_Elapsed(object sender, ElapsedEventArgs e)
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
                    Bass.BASS_ChannelPlay(clickStream, true);
                    //
                    Dispatcher.Invoke(() => { 
                        NoteNowText.Content = waitToBePlayed[0].noteContent;
                    if ((bool)FollowPlayCheck.IsChecked)
                        SeekTextFromTime(); 
                    });
                    //Console.WriteLine(waitToBePlayed[0].content);
                    SimaiProcess.notelist.FindAll(o => o.havePlayed == false && o.time > currentTime)[0].havePlayed = true; //Since the data was added as time followed, we modify the first one
                    
                }
                
            }
            catch { }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            currentTimeRefreshTimer.Stop();
            VisualEffectRefreshTimer.Stop();
            Bass.BASS_ChannelStop(bgmStream);
            Bass.BASS_StreamFree(bgmStream);
            Bass.BASS_ChannelStop(clickStream);
            Bass.BASS_StreamFree(clickStream);
            Bass.BASS_Stop();
            Bass.BASS_Free();
        }

        private void CurrentTimeRefreshTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            var currentPlayTime = Bass.BASS_ChannelBytes2Seconds(bgmStream, Bass.BASS_ChannelGetPosition(bgmStream));
            int minute = (int)currentPlayTime / 60;
            double second = currentPlayTime % 60d;
            Dispatcher.Invoke(new Action(() => { TimeLabel.Content = String.Format("{0}:{1:00}", minute, second); }));

        }


        double playStartTime = 0d;

        void TogglePlayAndPause()
        {
            FumenContent.Focus();
            if (Bass.BASS_ChannelIsActive(bgmStream) == BASSActive.BASS_ACTIVE_PLAYING)
            {
                PlayAndPauseButton.Content = "▶";
                Bass.BASS_ChannelStop(bgmStream);
                clickSoundTimer.Stop();
                DrawWave();
            }
            else
            {
                PlayAndPauseButton.Content = "  ▌▌ ";
                SaveRawFumenText();
                var CusorTime = SimaiProcess.getSongTimeAndScan(GetRawFumenText(), GetRawFumenPosition());//scan first
                var channeltime = Bass.BASS_ChannelBytes2Seconds(bgmStream, Bass.BASS_ChannelGetPosition(bgmStream));
                playStartTime = channeltime;

                Bass.BASS_ChannelPlay(bgmStream, false);
                SimaiProcess.ClearNoteListPlayedState();
                DrawWave(CusorTime); //then the wave could be draw
                clickSoundTimer.Start();
            }
        }

        void ToggleStop()
        {
            FumenContent.Focus();
            PlayAndPauseButton.Content = "▶";
            Bass.BASS_ChannelStop(bgmStream);
            clickSoundTimer.Stop();
            Bass.BASS_ChannelSetPosition(bgmStream, playStartTime);
            DrawWave();
        }

        private void PlayAndPause_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            TogglePlayAndPause();
        }

        private void PlayAndPauseButton_Click(object sender, RoutedEventArgs e)
        {
            TogglePlayAndPause();
        }

        private void StopPlaying_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            ToggleStop();
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            ToggleStop();
        }

        private void FumenContent_SelectionChanged(object sender, RoutedEventArgs e)
        {
            var time = SimaiProcess.getSongTimeAndScan(GetRawFumenText(), GetRawFumenPosition());
            if (Keyboard.IsKeyDown(Key.LeftCtrl))
            {
                Bass.BASS_ChannelSetPosition(bgmStream, time);
            }
            Console.WriteLine("SelectionChanged");
            SimaiProcess.ClearNoteListPlayedState();
            DrawWave(time);
        }

        int selectedDifficulty = -1;
        private void FumenContent_TextChanged(object sender, TextChangedEventArgs e)
        {

        }
        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SaveRawFumenText();
            ComboBoxItem selected = (ComboBoxItem)LevelSelector.SelectedItem;
            for (int i = 0; i < 7; i++)
            {
                if (selected.Content.ToString() == ("&lv_" + (i + 1)))
                {
                    SetRawFumenText(SimaiProcess.fumens[i]);
                    selectedDifficulty = i;
                }
            }
            LevelTextBox.Text = SimaiProcess.levels[selectedDifficulty];
            SimaiProcess.getSongTimeAndScan(GetRawFumenText(), GetRawFumenPosition());
            DrawWave();
        }

        private void Menu_Open_Click(object sender, RoutedEventArgs e)
        {

        }

        private void Menu_Save_Click(object sender, RoutedEventArgs e)
        {
            SaveRawFumenText(true);
            SystemSounds.Beep.Play();
        }

        private void Menu_SaveAs_Click(object sender, RoutedEventArgs e)
        {

        }

        private void SaveFile_Command_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            SaveRawFumenText(true);
            SystemSounds.Beep.Play();
        }

        private void WaveViewZoomIn_Click(object sender, RoutedEventArgs e)
        {
            if (zoominPower <6)
                zoominPower += 1;
            DrawWave();
            FumenContent.Focus();
        }

        private void WaveViewZoomOut_Click(object sender, RoutedEventArgs e)
        {
            if(zoominPower>1)
            zoominPower -= 1;
            DrawWave();
            FumenContent.Focus();
        }

        private void MusicWave_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            var time = Bass.BASS_ChannelBytes2Seconds(bgmStream, Bass.BASS_ChannelGetPosition(bgmStream));
            var destnationTime = time + (0.002d * e.Delta * (1.0d / zoominPower));
            Bass.BASS_ChannelSetPosition(bgmStream, destnationTime);

            SeekTextFromTime();
        }

        void SeekTextFromTime()
        {
            var time = Bass.BASS_ChannelBytes2Seconds(bgmStream, Bass.BASS_ChannelGetPosition(bgmStream));
            var newList = SimaiProcess.timinglist;
            newList.Sort((x, y) => Math.Abs(time - x.time).CompareTo(Math.Abs(time - y.time)));
            var theNote = newList[0];
            newList.Sort((x, y) => x.time.CompareTo(y.time));
            var indexOfTheNote = newList.IndexOf(theNote);
            SimaiTimingPoint previoisNote;
            if (indexOfTheNote > 0)
                previoisNote = newList[indexOfTheNote - 1];
            else
                previoisNote = theNote;
            var pointer = FumenContent.Document.Blocks.ToList()[theNote.rawTextPositionY].ContentStart.GetPositionAtOffset(theNote.rawTextPositionX);
            var pointer1 = FumenContent.Document.Blocks.ToList()[previoisNote.rawTextPositionY].ContentStart.GetPositionAtOffset(previoisNote.rawTextPositionX + 1);
            FumenContent.Selection.Select(pointer1, pointer);
        }

        private void MenuItem_InfomationEdit_Click(object sender, RoutedEventArgs e)
        {
            var infoWindow = new Infomation();
            infoWindow.ShowDialog(); 
        }

        private void LevelTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (selectedDifficulty == -1) return;
            SimaiProcess.levels[selectedDifficulty] = LevelTextBox.Text;
        }

        private void MenuItem_SimaiWiki_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://w.atwiki.jp/simai/pages/25.html");
            //maidata.txtの譜面書式
        }

        private void FollowPlayCheck_Click(object sender, RoutedEventArgs e)
        {
            FumenContent.Focus();
        }

        private void GitHub_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://github.com/LingFeng-bbben/MajdataEdit");
        }
    }
}
