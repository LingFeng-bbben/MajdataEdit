using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;

namespace MajdataEdit;

/// <summary>
///     Infomation.xaml 的交互逻辑
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
        SimaiProcess.other_commands = OtherTextbox.Text;
        Close();
    }

    private void InfomationWindow_Loaded(object sender, RoutedEventArgs e)
    {
        TitleTextbox.Text = SimaiProcess.title;
        ArtistTextbox.Text = SimaiProcess.artist;
        DesignTextbox.Text = SimaiProcess.designer;
        OtherTextbox.Text = SimaiProcess.other_commands;
        LoadImageFromDefault();
        RenderOptions.SetBitmapScalingMode(SaltImage, BitmapScalingMode.HighQuality);
    }

    private void LoadImageFromDefault()
    {
        if (File.Exists(MainWindow.maidataDir + "/bg.png"))
            LoadImageFromByte(File.ReadAllBytes(MainWindow.maidataDir + "/bg.png"));
        else if (File.Exists(MainWindow.maidataDir + "/bg.jpg"))
            LoadImageFromByte(File.ReadAllBytes(MainWindow.maidataDir + "/bg.jpg"));
        else
            SaltImage.Source =
                new BitmapImage(new Uri("pack://application:,,,/MajdataEdit;component/Image/bg_dummy.jpg"));
    }

    private void LoadImageFromByte(byte[] data)
    {
        var image = new BitmapImage();
        image.BeginInit();
        image.StreamSource = new MemoryStream(data);
        image.EndInit();
        SaltImage.Source = image;
    }

    private void ReadMetadata(string path)
    {
        var formattedPath = path;

        if (!path.EndsWith(".ogg") && !path.EndsWith(".mp3"))
        {
            var ext = ".mp3";

            if (File.Exists(path + ".ogg")) ext = ".ogg";

            formattedPath += ext;
        }

        var file = TagLib.File.Create(formattedPath);
        TitleTextbox.Text = file.Tag.Title;
        ArtistTextbox.Text = file.Tag.JoinedPerformers;
        if (file.Tag.Pictures.Length > 0)
        {
            var pic = file.Tag.Pictures[0];
            LoadImageFromByte(pic.Data.Data);
            var result = MessageBox.Show(MainWindow.GetLocalizedString("IsOverridePicture"),
                MainWindow.GetLocalizedString("Info"), MessageBoxButton.YesNo);
            if (result != MessageBoxResult.Yes)
            {
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
        ReadMetadata(MainWindow.maidataDir + "/track");
    }

    private void ReadFileButton_Click(object sender, RoutedEventArgs e)
    {
        var openFileDialog = new OpenFileDialog
        {
            Filter = "*.mp3/*.ogg|*.mp3|*.ogg"
        };
        if ((bool)openFileDialog.ShowDialog()!) ReadMetadata(openFileDialog.FileName);
    }

    private void SaltImage_MouseDown(object sender, MouseButtonEventArgs e)
    {
        var openFileDialog = new OpenFileDialog
        {
            InitialDirectory = "",
            Filter = "图片|*.png;*.jpg"
        };
        if ((bool)openFileDialog.ShowDialog()!)
        {
            var data = File.ReadAllBytes(openFileDialog.FileName);
            var info = new FileInfo(openFileDialog.FileName);
            LoadImageFromByte(data);
            DelBGs();
            File.Copy(openFileDialog.FileName, MainWindow.maidataDir + "/bg" + info.Extension);
        }
    }

    private void DelBGs()
    {
        if (File.Exists(MainWindow.maidataDir + "/bg.png")) File.Delete(MainWindow.maidataDir + "/bg.png");
        if (File.Exists(MainWindow.maidataDir + "/bg.jpg")) File.Delete(MainWindow.maidataDir + "/bg.jpg");
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}