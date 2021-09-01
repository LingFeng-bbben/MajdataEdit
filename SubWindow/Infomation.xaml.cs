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
        }
    }
}
