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
using System.Windows.Interop;
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
        private readonly string[] langList = new string[3] {"zh-CN", "en-US", "ja"}; // 语言列表
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
            var window = (MainWindow) Owner;

            string curLang = window.editorSetting.Language;
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

            RenderModeComboBox.SelectedIndex = window.editorSetting.RenderMode;

            ViewerCover.Text = window.editorSetting.backgroundCover.ToString();
            ViewerSpeed.Text = window.editorSetting.playSpeed.ToString("F1"); // 转化为形如"7.0", "9.5"这样的速度
            ViewerTouchSpeed.Text = window.editorSetting.touchSpeed.ToString("F1");
        }

        private void LanguageComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //LanguageComboBox.SelectedIndex
            LocalizeDictionary.Instance.Culture = new CultureInfo(langList[LanguageComboBox.SelectedIndex]);
        }

        private void RenderModeComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            RenderOptions.ProcessRenderMode =
                RenderModeComboBox.SelectedIndex == 0 ? RenderMode.Default : RenderMode.SoftwareOnly;
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
            var window = (MainWindow) Owner;
            window.editorSetting.Language = langList[LanguageComboBox.SelectedIndex];
            window.editorSetting.RenderMode = RenderModeComboBox.SelectedIndex;
            window.editorSetting.backgroundCover = float.Parse(ViewerCover.Text);
            window.editorSetting.playSpeed = float.Parse(ViewerSpeed.Text);
            window.editorSetting.touchSpeed = float.Parse(ViewerTouchSpeed.Text);
            window.SaveEditorSetting();

            window.ViewerCover.Content = window.editorSetting.backgroundCover.ToString();
            window.ViewerSpeed.Content = window.editorSetting.playSpeed.ToString("F1");    // 转化为形如"7.0", "9.5"这样的速度
            window.ViewerTouchSpeed.Content = window.editorSetting.touchSpeed.ToString("F1");

            saveFlag = true;
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
                if (dialogMode)
                {
                    // 模态窗口状态下 则阻止关闭
                    e.Cancel = true;
                    MessageBox.Show(MainWindow.GetLocalizedString("NoEditorSetting"),
                        MainWindow.GetLocalizedString("Error"));
                }
                else
                {
                    LocalizeDictionary.Instance.Culture = new CultureInfo(((MainWindow) Owner).editorSetting.Language);
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