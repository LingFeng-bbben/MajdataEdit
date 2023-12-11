using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using MajdataEdit.AutoSaveModule;

namespace MajdataEdit;

/// <summary>
///     AutoSaveRecover.xaml 的交互逻辑
/// </summary>
public partial class AutoSaveRecover : Window
{
    private readonly IAutoSaveRecoverer Recoverer = new AutoSaveRecoverer();
    private readonly List<Tuple<AutoSaveIndex.FileInfo, FumenInfos>> RecoverList = new();
    private int currentSelectedIndex = -1;
    private int currentSelectedLevel = -1;

    public AutoSaveRecover()
    {
        InitializeComponent();
        InitializeAutosaveList();
    }

    private void InitializeAutosaveList()
    {
        var fileInfos = Recoverer.GetLocalAutoSaves();

        var i = 0;

        foreach (var fileInfo in fileInfos)
        {
            AddNewItem(fileInfo, i, false);
            i++;
        }

        fileInfos = Recoverer.GetGlobalAutoSaves();

        foreach (var fileInfo in fileInfos)
        {
            AddNewItem(fileInfo, i, true);
            i++;
        }
    }

    /// <summary>
    ///     添加一个条目
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

        var fumenInfo = Recoverer.GetFumenInfos(fileInfo.FileName);
        RecoverList.Add(new Tuple<AutoSaveIndex.FileInfo, FumenInfos>(fileInfo, fumenInfo));


        var item = new ListBoxItem
        {
            Name = "item" + i,
            Content = GetItemDisplayText(fileInfo, fumenInfo, isGlobal),
            ToolTip = GetItemDisplayText(fileInfo, fumenInfo, isGlobal)
        };
        item.AddHandler(ListBoxItem.SelectedEvent, new RoutedEventHandler(ListBoxItem_Selected));
        Autosave_Listbox.Items.Add(item);
    }

    public void ListBoxItem_Selected(object sender, RoutedEventArgs e)
    {
        var item = (ListBoxItem)sender;
        currentSelectedIndex = int.Parse(item.Name.Substring(4));

        var currentRecoverItem = RecoverList[currentSelectedIndex];

        Lb_Path.Content = currentRecoverItem.Item1.RawPath.Replace('/', '\\');
        Lb_Title.Content = currentRecoverItem.Item2.Title;
        Lb_Artist.Content = currentRecoverItem.Item2.Artist;
        Lb_Designer.Content = currentRecoverItem.Item2.Designer;

        Button[] fumenButton = { Btn_Easy, Btn_Basic, Btn_Advance, Btn_Expert, Btn_Master, Btn_ReMaster, Btn_Original };


        for (var i = 0; i < 7; i++)
        {
            var fumenText = currentRecoverItem.Item2.Fumens[i];
            if (fumenText == null)
            {
                fumenButton[i].IsEnabled = false;
            }
            else
            {
                fumenText = fumenText.Trim();

                if (fumenText.Length == 0)
                    fumenButton[i].IsEnabled = false;
                else
                    fumenButton[i].IsEnabled = true;
            }
        }

        // 先检查之前选中的难度有没有谱面
        if (currentSelectedLevel != -1 && fumenButton[currentSelectedLevel].IsEnabled)
        {
            var content = RecoverList[currentSelectedIndex].Item2.Fumens[currentSelectedLevel];
            SetRtbFumenContent(content);
        }
        else
        {
            // 否则 按照Mas, ReMas, Exp, Adv, Bas, Eas, Ori的顺序尝试选中一个谱面
            int[] authSeq = { 4, 5, 3, 2, 1, 0, 6 };
            foreach (var i in authSeq)
                if (fumenButton[i].IsEnabled)
                {
                    currentSelectedLevel = i;
                    var content = RecoverList[currentSelectedIndex].Item2.Fumens[currentSelectedLevel];
                    SetRtbFumenContent(content);
                    break;
                }
        }
    }

    private string GetItemDisplayText(AutoSaveIndex.FileInfo fileInfo, FumenInfos fumenInfo, bool isGlobal)
    {
        var result = fumenInfo.Title + " ";

        if (isGlobal)
            result += "(global)";
        else
            result += "(local)";

        result += " - ";

        result += FormatUnixTime(fileInfo.SavedTime);

        return result;
    }

    private string FormatUnixTime(long unixTime)
    {
        var dateTime = DateTimeOffset.FromUnixTimeSeconds(unixTime);

        return dateTime.Hour + ":" +
               dateTime.Minute + ":" +
               dateTime.Second + " " +
               dateTime.Year + "/" +
               dateTime.Month + "/" +
               dateTime.Day;
    }

    private void SetRtbFumenContent(string content)
    {
        Rtb_Fumen.Document.Blocks.Clear();

        if (content == null) return;

        var lines = content.Split('\n');
        foreach (var line in lines)
        {
            var paragraph = new Paragraph();
            paragraph.Inlines.Add(line);
            Rtb_Fumen.Document.Blocks.Add(paragraph);
        }
    }

    private void Btn_Easy_Click(object sender, RoutedEventArgs e)
    {
        currentSelectedLevel = 0;

        var content = RecoverList[currentSelectedIndex].Item2.Fumens[currentSelectedLevel];
        SetRtbFumenContent(content);
    }

    private void Btn_Basic_Click(object sender, RoutedEventArgs e)
    {
        currentSelectedLevel = 1;

        var content = RecoverList[currentSelectedIndex].Item2.Fumens[currentSelectedLevel];
        SetRtbFumenContent(content);
    }

    private void Btn_Advance_Click(object sender, RoutedEventArgs e)
    {
        currentSelectedLevel = 2;

        var content = RecoverList[currentSelectedIndex].Item2.Fumens[currentSelectedLevel];
        SetRtbFumenContent(content);
    }

    private void Btn_Expert_Click(object sender, RoutedEventArgs e)
    {
        currentSelectedLevel = 3;

        var content = RecoverList[currentSelectedIndex].Item2.Fumens[currentSelectedLevel];
        SetRtbFumenContent(content);
    }

    private void Btn_Master_Click(object sender, RoutedEventArgs e)
    {
        currentSelectedLevel = 4;

        var content = RecoverList[currentSelectedIndex].Item2.Fumens[currentSelectedLevel];
        SetRtbFumenContent(content);
    }

    private void Btn_ReMaster_Click(object sender, RoutedEventArgs e)
    {
        currentSelectedLevel = 5;

        var content = RecoverList[currentSelectedIndex].Item2.Fumens[currentSelectedLevel];
        SetRtbFumenContent(content);
    }

    private void Btn_Original_Click(object sender, RoutedEventArgs e)
    {
        currentSelectedLevel = 6;

        var content = RecoverList[currentSelectedIndex].Item2.Fumens[currentSelectedLevel];
        SetRtbFumenContent(content);
    }

    private void Btn_Recover_Click(object sender, RoutedEventArgs e)
    {
        var currentItem = RecoverList[currentSelectedIndex];

        var result = MessageBox.Show(
            string.Format(MainWindow.GetLocalizedString("RecoveryDoubleCheck"),
                FormatUnixTime(currentItem.Item1.SavedTime), currentItem.Item1.RawPath),
            MainWindow.GetLocalizedString("Attention"),
            MessageBoxButton.YesNo
        );

        if (result == MessageBoxResult.No) return;

        Recoverer.RecoverFile(currentItem.Item1);
        ((MainWindow)Owner).OpenFile(currentItem.Item1.RawPath);
        Close();
    }

    private void Btn_Cancel_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}