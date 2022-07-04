using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
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
using WPFLocalizeExtension.Engine;

namespace MajdataEdit
{
    /// <summary>
    /// BPMtap.xaml 的交互逻辑
    /// </summary>
    public partial class EditorSettingPanel : Window
    {
        private readonly string[] langList = new string[3] { "zh-CN", "en-US", "ja" }; // 语言列表
        private bool saveFlag = false;
        private bool dialogMode;

        public EditorSettingPanel(bool _dialogMode = false)
        {
            this.dialogMode = _dialogMode;
            InitializeComponent();

            if (dialogMode)
            {
                Cancel_Button.IsEnabled = false;
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            string curLang = ((MainWindow)Owner).editorSetting.Language;
            int boxIndex = -1;
            for (int i = 0; i < langList.Length; i++)
            {
                if (curLang == langList[i])
                {
                    boxIndex = i;
                    break;
                }
            }

            if (boxIndex == -1)
            {
                // 如果没有语言设置 或者语言未知 就自动切换到English
                boxIndex = 1;
            }
            LanguageComboBox.SelectedIndex = boxIndex;

            ViewerCover.Text = ((MainWindow)Owner).editorSetting.backgroundCover.ToString();
            ViewerSpeed.Text = ((MainWindow)Owner).editorSetting.playSpeed.ToString("F1");    // 转化为形如"7.0", "9.5"这样的速度
            ViewerTouchSpeed.Text = ((MainWindow)Owner).editorSetting.touchSpeed.ToString("F1");
        }

        private void LanguageComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //LanguageComboBox.SelectedIndex
            LocalizeDictionary.Instance.Culture = new CultureInfo(langList[LanguageComboBox.SelectedIndex]);
        }

        private void ViewerCover_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            var offset = float.Parse(ViewerCover.Text);
            offset += e.Delta > 0 ? 0.1f : -0.1f;
            ViewerCover.Text = offset.ToString();
        }

        private void ViewerSpeed_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            var offset = float.Parse(ViewerSpeed.Text);
            offset += e.Delta > 0 ? 0.5f : -0.5f;
            ViewerSpeed.Text = offset.ToString();
        }

        private void ViewerTouchSpeed_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            var offset = float.Parse(ViewerTouchSpeed.Text);
            offset += e.Delta > 0 ? 0.5f : -0.5f;
            ViewerTouchSpeed.Text = offset.ToString();
        }

        private void Save_Button_Click(object sender, RoutedEventArgs e)
        {
            saveFlag = true;
            ((MainWindow)Owner).editorSetting.Language = langList[LanguageComboBox.SelectedIndex];
            ((MainWindow)Owner).editorSetting.backgroundCover = float.Parse(ViewerCover.Text);
            ((MainWindow)Owner).editorSetting.playSpeed = float.Parse(ViewerSpeed.Text);
            ((MainWindow)Owner).editorSetting.touchSpeed = float.Parse(ViewerTouchSpeed.Text);
            ((MainWindow)Owner).SaveEditorSetting();
            this.Close();
        }

        private void Cancel_Button_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!saveFlag)
            {
                // 取消或直接关闭窗口
                if (dialogMode) {
                    // 模态窗口状态下 则阻止关闭
                    e.Cancel = true;
                    MessageBox.Show(MainWindow.GetLocalizedString("NoEditorSetting"), MainWindow.GetLocalizedString("Error"));
                }
                else
                {
                    LocalizeDictionary.Instance.Culture = new CultureInfo(((MainWindow)Owner).editorSetting.Language);
                }
            }
            else
            {
                if (dialogMode)
                {
                    this.DialogResult = true;
                }
            }
        }
    }
}
