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
            text = text.Replace("\r","");
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
            Bass.BASS_Init(-1, 44100, BASSInit.BASS_DEVICE_CPSPEAKERS,handle);
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

            LevelSelector.SelectedItem = LevelSelector.Items[0];
            ReadWaveFromFile();
            SimaiProcess.getSongTimeAndScan(GetRawFumenText(), GetRawFumenPosition());
            DrawWave();
            FumenContent.Focus();
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

                    pen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(150,200,200,220), 2);
                    PointF[] curvepoints = new PointF[waveLevels.Length];
                    for (int i = 0; i < waveLevels.Length; i++)
                    {
                        curvepoints[i] = new PointF((i + drawoffset) * zoominPower, (1f - waveEnergies[i]) * 35 +2);
                    }
                    graphics.DrawCurve(pen, curvepoints);
                    //Draw notes
                    pen = new System.Drawing.Pen(System.Drawing.Color.LightPink, 2);
                    foreach (var note in SimaiProcess.notelist)
                    {
                        if (note == null) { break; }
                        float x = (float)(note.time / sampleTime) * zoominPower;
                        graphics.DrawLine(pen, x, 0, x, 75);
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
                MusicWave.Margin = new Thickness(-currentTime/ sampleTime * zoominPower, Margin.Left, MusicWave.Margin.Right, Margin.Bottom);//Todo:the scale
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
                    points[i] = new PointF((float)Math.Log10(i+1)*100f, (240 - fft[i] * 256)); //semilog
                }

                graphics.DrawCurve(new System.Drawing.Pen(System.Drawing.Color.LightSkyBlue, 1), points);

                float outputHz = 0;
                new Visuals().DetectPeakFrequency(bgmStream, out outputHz);

                if (Bass.BASS_ChannelIsActive(bgmStream)==BASSActive.BASS_ACTIVE_PLAYING)
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
                    //Console.WriteLine("Tick");
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
            Dispatcher.Invoke(new Action(()=>{ TimeLabel.Content = String.Format("{0}:{1:00}", minute, second); }));
            
        }


        double playStartTime = 0d;
        private void PlayAndPause_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            SaveRawFumenText();
            Console.WriteLine("Executed");
            var CusorTime = SimaiProcess.getSongTimeAndScan(GetRawFumenText(), GetRawFumenPosition());//scan first
            var channeltime = Bass.BASS_ChannelBytes2Seconds(bgmStream, Bass.BASS_ChannelGetPosition(bgmStream));
            playStartTime = channeltime;

            Bass.BASS_ChannelPlay(bgmStream, false);
            SimaiProcess.ClearNoteListPlayedState();
            DrawWave(CusorTime); //then the wave could be draw
            clickSoundTimer.Start();
        }

        private void PlayAndPause_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            Console.WriteLine("CanExecute");
            if (Bass.BASS_ChannelIsActive(bgmStream)==BASSActive.BASS_ACTIVE_PLAYING)
            {
                Bass.BASS_ChannelStop(bgmStream);
                clickSoundTimer.Stop();
                Bass.BASS_ChannelSetPosition(bgmStream, playStartTime);
                DrawWave();
                e.CanExecute = false;
            }
            else
            {
                e.CanExecute = true;
            }
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
            var newList = SimaiProcess.timinglist;
            newList.Sort((x, y) => Math.Abs(destnationTime - x.time).CompareTo(Math.Abs(destnationTime - y.time)));
            var theNote = newList[0];
            var pointer = FumenContent.Document.Blocks.ToList()[theNote.rawTextPositionY].ContentStart.GetPositionAtOffset(theNote.rawTextPositionX);
            var pointer1 = pointer.GetPositionAtOffset(1);
            FumenContent.Selection.Select(pointer, pointer1);
            
        }
    }
}
