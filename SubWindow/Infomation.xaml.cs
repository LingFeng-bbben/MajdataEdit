using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.IO;

namespace MajdataEdit
{
    /// <summary>
    /// Infomation.xaml 的交互逻辑
    /// </summary>
    public partial class Infomation : Window
    {
        public Infomation()
        {
            InitializeComponent();
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            SimaiProcess.title = TitleTextbox.Text;
            SimaiProcess.artist = ArtistTextbox.Text;
            SimaiProcess.designer = DesignTextbox.Text;
            this.Close();
        }

        private void InfomationWindow_Loaded(object sender, RoutedEventArgs e)
        {
            TitleTextbox.Text = SimaiProcess.title;
            ArtistTextbox.Text = SimaiProcess.artist;
            DesignTextbox.Text = SimaiProcess.designer;
            LoadImageFromDefault();
            RenderOptions.SetBitmapScalingMode(SaltImage, BitmapScalingMode.HighQuality);
        }

        void LoadImageFromDefault()
        {
            if (File.Exists(MainWindow.maidataDir + "/bg.png"))
            {
                LoadImageFromByte(File.ReadAllBytes(MainWindow.maidataDir + "/bg.png"));
            }
            else if (File.Exists(MainWindow.maidataDir + "/bg.jpg"))
            {
                LoadImageFromByte(File.ReadAllBytes(MainWindow.maidataDir + "/bg.jpg"));
            }
            else
            {
                SaltImage.Source = new BitmapImage(new Uri("pack://application:,,,/MajdataEdit;component/Image/bg_dummy.jpg"));
            }
        }

        void LoadImageFromByte(byte[] data)
        {
            var image = new BitmapImage();
            image.BeginInit();
            image.StreamSource = new MemoryStream(data);
            image.EndInit();
            SaltImage.Source = image;
        }

        void ReadMetadata(string path)
        {
            var file = TagLib.File.Create(path);
            TitleTextbox.Text = file.Tag.Title;
            ArtistTextbox.Text = file.Tag.JoinedPerformers;
            if (file.Tag.Pictures.Length > 0)
            {
                var pic = file.Tag.Pictures[0];
                LoadImageFromByte(pic.Data.Data);
                var result = MessageBox.Show(MainWindow.GetLocalizedString("IsOverridePicture"), MainWindow.GetLocalizedString("Info"), MessageBoxButton.YesNo);
                if (result != MessageBoxResult.Yes) {
                    LoadImageFromDefault();
                    return; 
                }
                if (pic.MimeType.Contains("jpeg"))
                {
                    DelBGs();
                    File.WriteAllBytes(MainWindow.maidataDir + "/bg.jpg", pic.Data.Data);
                }
                if (pic.MimeType.Contains("png"))
                {
                    DelBGs();
                    File.WriteAllBytes(MainWindow.maidataDir + "/bg.png", pic.Data.Data);
                }
                Console.WriteLine(pic.MimeType);
            }
        }

        private void ReadTrackButton_Click(object sender, RoutedEventArgs e)
        {
            ReadMetadata(MainWindow.maidataDir + "/track.mp3");
        }

        private void ReadFileButton_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog();
            openFileDialog.Filter = "*.mp3|*.mp3";
            if ((bool)openFileDialog.ShowDialog())
            {
                ReadMetadata(openFileDialog.FileName);
            }
        }

        private void SaltImage_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog();
            openFileDialog.InitialDirectory = "";
            openFileDialog.Filter = "图片|*.png;*.jpg";
            if ((bool)openFileDialog.ShowDialog())
            {
                var data = File.ReadAllBytes(openFileDialog.FileName);
                var info = new FileInfo(openFileDialog.FileName);
                LoadImageFromByte(data);
                DelBGs();
                File.Copy(openFileDialog.FileName, MainWindow.maidataDir + "/bg" + info.Extension);
            }
        }

        void DelBGs()
        {
            if (File.Exists(MainWindow.maidataDir + "/bg.png"))
            {
                File.Delete(MainWindow.maidataDir + "/bg.png");
            }
            if (File.Exists(MainWindow.maidataDir + "/bg.jpg"))
            {
                File.Delete(MainWindow.maidataDir + "/bg.jpg");
            }
        }
    }
}
