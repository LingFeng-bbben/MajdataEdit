using MajdataEdit.AutoSaveModule;
using System;
using System.Collections.Generic;
using System.IO;
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
    /// AutoSaveRecover.xaml 的交互逻辑
    /// </summary>
    public partial class AutoSaveRecover : Window
    {
        private IAutoSaveRecoverer Recoverer = new AutoSaveRecoverer();
        private List<Tuple<AutoSaveIndex.FileInfo, FumenInfos>> RecoverList = new List<Tuple<AutoSaveIndex.FileInfo, FumenInfos>>();
        private int currentSelectedIndex = -1;
        private int currentSelectedLevel = -1;

        public AutoSaveRecover()
        {
            InitializeComponent();
            InitializeAutosaveList();
        }

        private void InitializeAutosaveList()
        {
            List<AutoSaveIndex.FileInfo> fileInfos = Recoverer.GetLocalAutoSaves();

            int i = 0;

            foreach (AutoSaveIndex.FileInfo fileInfo in fileInfos)
            {
                AddNewItem(fileInfo, i, false);
                i++;
            }

            fileInfos = Recoverer.GetGlobalAutoSaves();

            foreach (AutoSaveIndex.FileInfo fileInfo in fileInfos)
            {
                AddNewItem(fileInfo, i, true);
                i++;
            }
        }

        /// <summary>
        /// 添加一个条目
        /// </summary>
        /// <param name="fileInfo"></param>
        /// <param name="i"></param>
        /// <param name="isGlobal"></param>
        private void AddNewItem(AutoSaveIndex.FileInfo fileInfo, int i, bool isGlobal)
        {
            if (!File.Exists(fileInfo.FileName))
            {
                Console.WriteLine(fileInfo.FileName + "  is not exists! skip");
                return;
            }

            FumenInfos fumenInfo = Recoverer.GetFumenInfos(fileInfo.FileName);
            RecoverList.Add(new Tuple<AutoSaveIndex.FileInfo, FumenInfos>(fileInfo, fumenInfo));


            ListBoxItem item = new ListBoxItem
            {
                Name = "item" + i.ToString(),
                Content = GetItemDisplayText(fileInfo, fumenInfo, isGlobal),
                ToolTip = GetItemDisplayText(fileInfo, fumenInfo, isGlobal)
            };
            item.AddHandler(ListBoxItem.SelectedEvent, new RoutedEventHandler(ListBoxItem_Selected));
            this.Autosave_Listbox.Items.Add(item);
        }

        public void ListBoxItem_Selected(object sender, RoutedEventArgs e)
        {
            ListBoxItem item = (ListBoxItem)sender;
            this.currentSelectedIndex = int.Parse(item.Name.Substring(4));

            Tuple<AutoSaveIndex.FileInfo, FumenInfos> currentRecoverItem = this.RecoverList[this.currentSelectedIndex];

            Lb_Path.Content = currentRecoverItem.Item1.RawPath.Replace('/', '\\');
            Lb_Title.Content = currentRecoverItem.Item2.Title;
            Lb_Artist.Content = currentRecoverItem.Item2.Artist;
            Lb_Designer.Content = currentRecoverItem.Item2.Designer;

            Button[] fumenButton = new Button[]{ Btn_Easy, Btn_Basic, Btn_Advance, Btn_Expert, Btn_Master, Btn_ReMaster, Btn_Original };


            for (int i = 0; i < 7; i++)
            {
                string fumenText = currentRecoverItem.Item2.Fumens[i];
                if (fumenText == null)
                {
                    fumenButton[i].IsEnabled = false;
                }
                else
                {
                    fumenText = fumenText.Trim();

                    if (fumenText.Length == 0)
                    {
                        fumenButton[i].IsEnabled = false;
                    }
                    else
                    {
                        fumenButton[i].IsEnabled = true;
                    }
                }
            }

            // 先检查之前选中的难度有没有谱面
            if (this.currentSelectedLevel != -1 && fumenButton[this.currentSelectedLevel].IsEnabled)
            {
                string content = this.RecoverList[this.currentSelectedIndex].Item2.Fumens[this.currentSelectedLevel];
                SetRtbFumenContent(content);
            }
            else
            {
                // 否则 按照Mas, ReMas, Exp, Adv, Bas, Eas, Ori的顺序尝试选中一个谱面
                int[] authSeq = { 4, 5, 3, 2, 1, 0, 6 };
                foreach (int i in authSeq)
                {
                    if (fumenButton[i].IsEnabled)
                    {
                        this.currentSelectedLevel = i;
                        string content = this.RecoverList[this.currentSelectedIndex].Item2.Fumens[this.currentSelectedLevel];
                        SetRtbFumenContent(content);
                        break;
                    }
                }
            }
        }

        private string GetItemDisplayText(AutoSaveIndex.FileInfo fileInfo, FumenInfos fumenInfo, bool isGlobal)
        {
            string result = fumenInfo.Title + " ";

            if (isGlobal)
            {
                result += "(global)";
            }
            else
            {
                result += "(local)";
            }

            result += " - ";

            result += FormatUnixTime(fileInfo.SavedTime);

            return result;
        }

        private string FormatUnixTime(long unixTime)
        {
            DateTimeOffset dateTime = DateTimeOffset.FromUnixTimeSeconds(unixTime);

            return dateTime.Hour.ToString() + ":" +
                   dateTime.Minute.ToString() + ":" +
                   dateTime.Second.ToString() + " " +
                   dateTime.Year.ToString() + "/" +
                   dateTime.Month.ToString() + "/" +
                   dateTime.Day.ToString();
        }

        private void SetRtbFumenContent(string content)
        {
            Rtb_Fumen.Document.Blocks.Clear();

            if (content == null)
            {
                return;
            }

            string[] lines = content.Split('\n');
            foreach (var line in lines)
            {
                Paragraph paragraph = new Paragraph();
                paragraph.Inlines.Add(line);
                Rtb_Fumen.Document.Blocks.Add(paragraph);
            }
        }

        private void Btn_Easy_Click(object sender, RoutedEventArgs e)
        {
            this.currentSelectedLevel = 0;

            string content = this.RecoverList[this.currentSelectedIndex].Item2.Fumens[this.currentSelectedLevel];
            SetRtbFumenContent(content);
        }

        private void Btn_Basic_Click(object sender, RoutedEventArgs e)
        {
            this.currentSelectedLevel = 1;

            string content = this.RecoverList[this.currentSelectedIndex].Item2.Fumens[this.currentSelectedLevel];
            SetRtbFumenContent(content);
        }

        private void Btn_Advance_Click(object sender, RoutedEventArgs e)
        {
            this.currentSelectedLevel = 2;

            string content = this.RecoverList[this.currentSelectedIndex].Item2.Fumens[this.currentSelectedLevel];
            SetRtbFumenContent(content);
        }

        private void Btn_Expert_Click(object sender, RoutedEventArgs e)
        {
            this.currentSelectedLevel = 3;

            string content = this.RecoverList[this.currentSelectedIndex].Item2.Fumens[this.currentSelectedLevel];
            SetRtbFumenContent(content);
        }

        private void Btn_Master_Click(object sender, RoutedEventArgs e)
        {
            this.currentSelectedLevel = 4;

            string content = this.RecoverList[this.currentSelectedIndex].Item2.Fumens[this.currentSelectedLevel];
            SetRtbFumenContent(content);
        }

        private void Btn_ReMaster_Click(object sender, RoutedEventArgs e)
        {
            this.currentSelectedLevel = 5;

            string content = this.RecoverList[this.currentSelectedIndex].Item2.Fumens[this.currentSelectedLevel];
            SetRtbFumenContent(content);
        }

        private void Btn_Original_Click(object sender, RoutedEventArgs e)
        {
            this.currentSelectedLevel = 6;

            string content = this.RecoverList[this.currentSelectedIndex].Item2.Fumens[this.currentSelectedLevel];
            SetRtbFumenContent(content);
        }

        private void Btn_Recover_Click(object sender, RoutedEventArgs e)
        {
            Tuple<AutoSaveIndex.FileInfo, FumenInfos> currentItem = this.RecoverList[this.currentSelectedIndex];

            MessageBoxResult result = MessageBox.Show(
                String.Format(MainWindow.GetLocalizedString("RecoveryDoubleCheck"), FormatUnixTime(currentItem.Item1.SavedTime), currentItem.Item1.RawPath),
                MainWindow.GetLocalizedString("Attention"),
                MessageBoxButton.YesNo
            );

            if (result == MessageBoxResult.No)
            {
                return;
            }

            this.Recoverer.RecoverFile(currentItem.Item1);
            ((MainWindow)this.Owner).OpenFile(currentItem.Item1.RawPath);
            this.Close();
        }

        private void Btn_Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
