using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using Un4seen.Bass;

namespace MajdataEdit
{
    /// <summary>
    /// Window1.xaml 的交互逻辑
    /// </summary>
    
    public class ErrorInfo
    {
        public int positionX;
        public int positionY;
        public ErrorInfo(int _posX, int _posY)
        {
            positionX = _posX;
            positionY = _posY;
        }
    }

    public partial class MuriCheckResult : Window
    {
        public List<ErrorInfo> errorPosition = new List<ErrorInfo>();
        public MuriCheckResult()
        {
            InitializeComponent();
        }

        public void ListBoxItem_PreviewMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            ListBoxItem item = (ListBoxItem)sender;
            int index = int.Parse(item.Name.Substring(2));
            ErrorInfo errorInfo = errorPosition[index];

            ((MainWindow)Owner).ScrollToFumenContentSelection(errorInfo.positionX, errorInfo.positionY);
        }
    }
}
