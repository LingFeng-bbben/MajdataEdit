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
using System.ComponentModel;

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
            if (Environment.GetCommandLineArgs().Contains("--ForceSoftwareRender"))
            {
                MessageBox.Show("正在以软件渲染模式运行");
                RenderOptions.ProcessRenderMode = RenderMode.SoftwareOnly;
            }
        }
       
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            CheckAndStartView();

            var handle = (new WindowInteropHelper(this)).Handle;
            Bass.BASS_Init(-1, 44100, BASSInit.BASS_DEVICE_CPSPEAKERS, handle);

            ReadSoundEffect();
            ReadEditorSetting();

            currentTimeRefreshTimer.Elapsed += CurrentTimeRefreshTimer_Elapsed;
            currentTimeRefreshTimer.Start();
            clickSoundTimer.Elapsed += ClickSoundTimer_Elapsed;
            VisualEffectRefreshTimer.Elapsed += VisualEffectRefreshTimer_Elapsed;
            VisualEffectRefreshTimer.Start();

        }

        //start the view and wait for boot, then set window pos
        private void SetWindowPosTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            Timer setWindowPosTimer = (Timer)sender;
            Dispatcher.Invoke(() =>
            {
                InternalSwitchWindow();
            });
            setWindowPosTimer.Stop();
            setWindowPosTimer.Dispose();
        }

        // This update very freqently to Draw FFT wave.
        private void VisualEffectRefreshTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            DrawFFT();
        }
        // This update very freqently to play sound effect.
        private void ClickSoundTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            SoundEffectUpdate();
        }
        // This update less frequently. set the time text.
        private void CurrentTimeRefreshTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            UpdateTimeDisplay();
        }

        //Window events
        private void TheWindow_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (CheckAndStartView()) return;
            InternalSwitchWindow();
        }
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!isSaved)
            {
                if (!AskSave())
                {
                    e.Cancel = true;
                    return;
                }
            }
            var process = Process.GetProcessesByName("MajdataView");
            if (process.Length > 0)
            {
                var result = MessageBox.Show("要关闭View吗？", "警告", MessageBoxButton.YesNo);
                if (result == MessageBoxResult.Yes)
                    process[0].Kill();
            }

            currentTimeRefreshTimer.Stop();
            VisualEffectRefreshTimer.Stop();

            soundSetting.Close();
            SaveSetting();

            Bass.BASS_ChannelStop(bgmStream);
            Bass.BASS_StreamFree(bgmStream);
            Bass.BASS_ChannelStop(clickStream);
            Bass.BASS_StreamFree(clickStream);
            Bass.BASS_ChannelStop(breakStream);
            Bass.BASS_StreamFree(breakStream);
            Bass.BASS_ChannelStop(exStream);
            Bass.BASS_StreamFree(exStream);
            Bass.BASS_ChannelStop(hanabiStream);
            Bass.BASS_StreamFree(hanabiStream);
            Bass.BASS_Stop();
            Bass.BASS_Free();
        }

        //Window grid events
        private void Grid_DragEnter(object sender, DragEventArgs e)
        {
            e.Effects = DragDropEffects.Move;
        }
        private void Grid_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                Console.WriteLine(e.Data.GetData(DataFormats.FileDrop).ToString());
                if (e.Data.GetData(DataFormats.FileDrop).ToString() == "System.String[]")
                {
                    var path = ((string[])e.Data.GetData(DataFormats.FileDrop))[0];
                    if (path.ToLower().Contains("maidata.txt"))
                    {
                        if (!isSaved)
                        {
                            if (!AskSave()) return;
                        }
                        FileInfo fileInfo = new FileInfo(path);
                        initFromFile(fileInfo.DirectoryName);

                    }

                    return;
                }
            }
        }

        //MENU BARS
        private void Menu_New_Click(object sender, RoutedEventArgs e)
        {
            if (!isSaved)
            {
                if (!AskSave()) return;
            }
            var openFileDialog = new Microsoft.Win32.OpenFileDialog();
            openFileDialog.Filter = "track.mp3|track.mp3";
            if ((bool)openFileDialog.ShowDialog())
            {
                FileInfo fileInfo = new FileInfo(openFileDialog.FileName);
                CreateNewFumen(fileInfo.DirectoryName);
                initFromFile(fileInfo.DirectoryName);
            }
        }
        private void Menu_Open_Click(object sender, RoutedEventArgs e)
        {
            if (!isSaved)
            {
                if (!AskSave()) return;
            }
            var openFileDialog = new Microsoft.Win32.OpenFileDialog();
            openFileDialog.Filter = "maidata.txt|maidata.txt";
            if ((bool)openFileDialog.ShowDialog())
            {
                FileInfo fileInfo = new FileInfo(openFileDialog.FileName);
                initFromFile(fileInfo.DirectoryName);
            }
        }
        private void Menu_Save_Click(object sender, RoutedEventArgs e)
        {
            SaveFumen(true);
            SystemSounds.Beep.Play();
        }
        private void Menu_SaveAs_Click(object sender, RoutedEventArgs e)
        {

        }
        private void MirrorLeftRight_MenuItem_Click(object sender, RoutedEventArgs e)
        {
            var result = Mirror.NoteMirrorLeftRight(FumenContent.Selection.Text);
            FumenContent.Selection.Text = result;
        }
        private void MirrorUpDown_MenuItem_Click(object sender, RoutedEventArgs e)
        {
            var result = Mirror.NoteMirrorUpDown(FumenContent.Selection.Text);
            FumenContent.Selection.Text = result;
        }
        private void Mirror180_MenuItem_Click(object sender, RoutedEventArgs e)
        {
            var result = Mirror.NoteMirror180(FumenContent.Selection.Text);
            FumenContent.Selection.Text = result;
        }
        private void BPMtap_MenuItem_Click(object sender, RoutedEventArgs e)
        {
            BPMtap tap = new BPMtap();
            tap.Show();
        }
        private void MenuItem_InfomationEdit_Click(object sender, RoutedEventArgs e)
        {
            var infoWindow = new Infomation();
            infoWindow.ShowDialog();
            TheWindow.Title = "MajdataEdit - " + SimaiProcess.title;
        }
        private void MenuItem_SimaiWiki_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://w.atwiki.jp/simai/pages/25.html");
            //maidata.txtの譜面書式
        }
        private void MenuItem_GitHub_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://github.com/LingFeng-bbben/MajdataView");
        }
        private void MenuItem_SoundSetting_Click(object sender, RoutedEventArgs e)
        {
            soundSetting = new SoundSetting();
            soundSetting.Show();
        }

        //快捷键
        private void PlayAndPause_CanExecute(object sender, CanExecuteRoutedEventArgs e) //快捷键
        {
            TogglePlayAndStop();
        }
        private void StopPlaying_CanExecute(object sender, CanExecuteRoutedEventArgs e) //快捷键
        {
            TogglePlayAndPause();
        }
        private void SaveFile_Command_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            SaveFumen(true);
            SystemSounds.Beep.Play();
        }
        private void SendToView_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            ToggleExport();
        }

        //Left componients
        private void PlayAndPauseButton_Click(object sender, RoutedEventArgs e)
        {
            TogglePlayAndPause();
        }
        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            ToggleStop();
        }
        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int i = LevelSelector.SelectedIndex;
            SetRawFumenText(SimaiProcess.fumens[i]);
            selectedDifficulty = i;
            LevelTextBox.Text = SimaiProcess.levels[selectedDifficulty];
            SetSavedState(true);
            SimaiProcess.Serialize(GetRawFumenText());
            DrawWave();

        }
        private void LevelTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (selectedDifficulty == -1) return;
            SimaiProcess.levels[selectedDifficulty] = LevelTextBox.Text;
        }
        private void OffsetTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                SimaiProcess.first = float.Parse(OffsetTextBox.Text);
                SimaiProcess.Serialize(GetRawFumenText());
                DrawWave();
            }
            catch { SimaiProcess.first = 0f; }
        }
        private void FollowPlayCheck_Click(object sender, RoutedEventArgs e)
        {
            FumenContent.Focus();
        }
        private void Export_Button_Click(object sender, RoutedEventArgs e)
        {
            ToggleExport();
        }

        //RichTextbox events
        private void FumenContent_SelectionChanged(object sender, RoutedEventArgs e)
        {
            NoteNowText.Content = "" + (
                 new TextRange(FumenContent.Document.ContentStart, FumenContent.CaretPosition).Text.
                 Replace("\r", "").Count(o => o == '\n') + 1) + " 行";
            if (Bass.BASS_ChannelIsActive(bgmStream) == BASSActive.BASS_ACTIVE_PLAYING && (bool)FollowPlayCheck.IsChecked)
                return;
            var time = SimaiProcess.Serialize(GetRawFumenText(), GetRawFumenPosition());
            if (Keyboard.IsKeyDown(Key.LeftCtrl))
            {
                Bass.BASS_ChannelSetPosition(bgmStream, time);
            }
            Console.WriteLine("SelectionChanged");
            SimaiProcess.ClearNoteListPlayedState();
            DrawWave(time);
        }
        private void FumenContent_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (GetRawFumenText() == "") return;
            SetSavedState(false);
        }

        //Wave displayer
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
            ScrollWave(e.Delta);
        }
        private void MusicWave_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            //lastMousePointX = e.GetPosition(this).X;
        }
        private void MusicWave_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                double delta = e.GetPosition(this).X - lastMousePointX;
                lastMousePointX = e.GetPosition(this).X;
                ScrollWave(delta*zoominPower*4d);
            }
            lastMousePointX = e.GetPosition(this).X;
        }


    }
}
