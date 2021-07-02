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
using System.Drawing;


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
            if(writeToDisk)
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
        Timer VisualEffectRefreshTimer = new Timer(32);
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

            DrawWave();

            SimaiProcess.ReadData("maidata.txt");
        }


        float sampleTime = 0.02f;
        int zoominPower = 4;
        private void DrawWave()
        {
            var bgmDecode = Bass.BASS_StreamCreateFile("track.mp3", 0L, 0L, BASSFlag.BASS_STREAM_DECODE);
            var length = Bass.BASS_ChannelBytes2Seconds(bgmDecode, Bass.BASS_ChannelGetLength(bgmStream));
            int sampleNumber = (int)((length * 1000) / (sampleTime*1000))/2 + 1;
            float[] levels = new float[sampleNumber];
            for (int i = 0; i < sampleNumber; i++)
            {
                levels[i] = Bass.BASS_ChannelGetLevels(bgmDecode,sampleTime,BASSLevel.BASS_LEVEL_MONO)[0];
            }
            Bass.BASS_StreamFree(bgmDecode);

            var writableBitmap = new WriteableBitmap(levels.Length*zoominPower, 74, 72, 72, PixelFormats.Pbgra32, null);
            MusicWave.Source = writableBitmap;
            MusicWave.Width = levels.Length * zoominPower;
            writableBitmap.Lock();
            //the process starts
            Bitmap backBitmap = new Bitmap(levels.Length * zoominPower, 74, writableBitmap.BackBufferStride,
                    System.Drawing.Imaging.PixelFormat.Format32bppArgb, writableBitmap.BackBuffer);
            Graphics graphics = Graphics.FromImage(backBitmap);

            graphics.Clear(System.Drawing.Color.Black);
            System.Drawing.Pen pen = new System.Drawing.Pen(System.Drawing.Color.Green, zoominPower);
            var drawoffset = 0;
            if (sampleTime == 0.02f) drawoffset = -0;
            for (int i=0;i<levels.Length;i++)
            {
                var lv = levels[i] * 37;
                
                graphics.DrawLine(pen, (i + drawoffset )* zoominPower , 37 + lv, (i + drawoffset) * zoominPower , 37 - lv);
            }

            pen = new System.Drawing.Pen(System.Drawing.Color.LightPink, 2);
            foreach (var note in SimaiProcess.notelist)
            {
                float x = (float)(note.time / sampleTime) * zoominPower;
                graphics.DrawLine(pen, x, 0, x, 75);
            }

            pen = new System.Drawing.Pen(System.Drawing.Color.White, 1);
            foreach (var note in SimaiProcess.timinglist)
            {
                float x = (float)(note.time / sampleTime) * zoominPower;
                graphics.DrawLine(pen, x, 0, x, 10);
                graphics.DrawLine(pen, x, 65, x, 75);
            }

            graphics.Flush();
            graphics.Dispose();
            backBitmap.Dispose();

            writableBitmap.AddDirtyRect(new Int32Rect(0, 0, writableBitmap.PixelWidth, writableBitmap.PixelHeight));
            writableBitmap.Unlock();
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

                float[] fft = new float[256];
                Bass.BASS_ChannelGetData(bgmStream, fft, (int)BASSData.BASS_DATA_FFT256);
                PointF[] points = new PointF[256];
                for (int i = 0; i < fft.Length; i++)
                {
                    points[i] = new PointF((float)Math.Log10(i+1)*100f, (240 - fft[i] * 256)); //semilog
                }

                graphics.DrawCurve(new System.Drawing.Pen(System.Drawing.Color.Red, 1), points);

                graphics.Flush();
                graphics.Dispose();
                backBitmap.Dispose();

                writableBitmap.AddDirtyRect(new Int32Rect(0, 0, 255, 255));
                writableBitmap.Unlock();
            }));
        }

        private void ClickSoundTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            var currentTime = Bass.BASS_ChannelBytes2Seconds(bgmStream, Bass.BASS_ChannelGetPosition(bgmStream));
            var waitToBePlayed = SimaiProcess.notelist.FindAll(o => o.havePlayed == false && o.time>currentTime);
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

        private void FumenContent_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        private void PlayAndPause_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Console.WriteLine("Executed");
            var time = SimaiProcess.getSongTimeAndScan(GetRawFumenText(), GetRawFumenPosition());//scan first
            Console.WriteLine(time);
            Bass.BASS_ChannelSetPosition(bgmStream, time);
            Bass.BASS_ChannelPlay(bgmStream, false);
            SimaiProcess.ClearNoteListPlayedState();
            DrawWave(); //then the wave could be draw
            clickSoundTimer.Start();
        }

        private void PlayAndPause_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            Console.WriteLine("CanExecute");
            if (Bass.BASS_ChannelIsActive(bgmStream)==BASSActive.BASS_ACTIVE_PLAYING)
            {
                Bass.BASS_ChannelStop(bgmStream);
                clickSoundTimer.Stop();
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
            Console.WriteLine(time);
            Bass.BASS_ChannelSetPosition(bgmStream, time);
            SimaiProcess.ClearNoteListPlayedState();
        }

        int selectedDifficulty = -1;

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
        }

        private void Menu_SaveAs_Click(object sender, RoutedEventArgs e)
        {

        }

    }
}
