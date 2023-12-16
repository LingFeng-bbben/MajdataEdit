﻿using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MajdataEdit;

/// <summary>
///     BPMtap.xaml 的交互逻辑
/// </summary>
/*
 * 请原谅我把检测逻辑和界面逻辑写到一个文件里去了
 * 因为我懒
 * 如果有空我会把他分出去的
 * 感谢！
 */
internal class MaimaiOperationMultNote
{
    public int endArea;
    public double endTime;
    public string noteContent;
    public int ntype;
    public int positionX;
    public int positionY;

    public int startArea;

    // 用于多押检测的类 表示一次操作
    public double startTime;

    public MaimaiOperationMultNote(double _startTime, double _endTime, int _startArea,
        int _endArea, int _ntype, string _noteContent, int _positionX,
        int _positionY)
    {
        startTime = _startTime;
        endTime = _endTime;
        startArea = _startArea;
        endArea = _endArea;
        ntype = _ntype;
        noteContent = _noteContent;
        positionX = _positionX;
        positionY = _positionY;
    }
}

internal class MaimaiOperationSlide
{
    public int area;
    public string noteContent;
    public int ntype;
    public int positionX;

    public int positionY;

    // 用于撞尾检测的类 表示一次操作
    public double time;

    public MaimaiOperationSlide(double _time, int _area, int _ntype,
        string _noteContent, int _positionX, int _positionY)
    {
        time = _time;
        area = _area;
        ntype = _ntype;
        noteContent = _noteContent;
        positionX = _positionX;
        positionY = _positionY;
    }
}

public partial class MuriCheck : Window
{
    private MuriCheckResult? mcr;
    public JObject? SLIDE_TIME; // 无理检测用的SLIDE_TIME数据

    public MuriCheck()
    {
        InitializeComponent();
    }

    private void ReadMuriCheckSlideTime()
    {
        using var r = new StreamReader("./slide_time.json");
        var json = r.ReadToEnd();
        SLIDE_TIME = JsonConvert.DeserializeObject<JObject>(json)!;
    }

    private int notePos(int pos, bool relative)
    {
        if (pos <= 0) pos += 8;

        if (relative)
            pos %= 8;
        else
            pos = (pos - 1) % 8 + 1;
        return pos;
    }

    private void StartCheck_Button_Click(object sender, RoutedEventArgs e)
    {
        double slideAccuracy;
        try
        {
            slideAccuracy = double.Parse(SlideAccuracy_TextBox.Text);
        }
        catch (FormatException)
        {
            MessageBox.Show(MainWindow.GetLocalizedString("SlideAccInputError"),
                MainWindow.GetLocalizedString("Error"));
            SlideAccuracy_TextBox.Text = "";
            return;
        }

        BeatmapMuriCheck(MultNote_Checkbox.IsChecked == true, slideAccuracy);
    }

    private void addWarning(string content, int posX, int posY)
    {
        if (mcr != null)
        {
            mcr.errorPosition.Add(new ErrorInfo(posX, posY));
            var resultRow = new ListBoxItem
            {
                Content = content,
                Name = "rr" + mcr.CheckResult_Listbox.Items.Count
            };
            resultRow.AddHandler(PreviewMouseDoubleClickEvent,
                new MouseButtonEventHandler(mcr.ListBoxItem_PreviewMouseDoubleClick));
            mcr.CheckResult_Listbox.Items.Add(resultRow);
        }
    }

    private int multNoteDetect()
    {
        var TIME_EPS = 5;

        // 注释可见 https://github.com/Moying-moe/maimaiMuriDetector MaiMuriDetector.multNoteDetect(self, eps=5)

        var prog = @"(\d)(.+?)(\d{1,2})\[.+?\]";
        var errorCnt = 0;

        var opSequence = new List<MaimaiOperationMultNote>();

        foreach (var noteGroup in SimaiProcess.notelist)
        {
            var baseTime = noteGroup.time;
            var positionX = noteGroup.rawTextPositionX;
            var positionY = noteGroup.rawTextPositionY;

            foreach (var note in noteGroup.getNotes())
                if (note.noteType == SimaiNoteType.Tap)
                {
                    opSequence.Add(new MaimaiOperationMultNote(
                        Math.Round(baseTime, TIME_EPS),
                        Math.Round(baseTime, TIME_EPS),
                        note.startPosition,
                        note.startPosition,
                        0,
                        note.noteContent!,
                        positionX,
                        positionY
                    ));
                }
                else if (note.noteType == SimaiNoteType.Slide)
                {
                    opSequence.Add(new MaimaiOperationMultNote(
                        Math.Round(baseTime, TIME_EPS),
                        Math.Round(baseTime, TIME_EPS),
                        note.startPosition,
                        note.startPosition,
                        1,
                        note.noteContent!,
                        positionX,
                        positionY
                    ));
                    int endPosition;
                    try
                    {
                        var temp = Regex.Match(note.noteContent!, prog);
                        endPosition = int.Parse(
                            temp.Groups[3].Value.Substring(temp.Groups[3].Value.Length - 1, 1)
                        );
                    }
                    catch
                    {
                        addWarning(string.Format(
                            MainWindow.GetLocalizedString("SyntaxError"),
                            note.noteContent,
                            positionY + 1,
                            positionX + 1
                        ), positionX, positionY);
                        continue;
                    }

                    opSequence.Add(new MaimaiOperationMultNote(
                        Math.Round(note.slideStartTime, TIME_EPS),
                        Math.Round(note.slideStartTime + note.slideTime, TIME_EPS),
                        note.startPosition,
                        endPosition,
                        3,
                        note.noteContent!,
                        positionX,
                        positionY
                    ));
                }
                else if (note.noteType == SimaiNoteType.Hold)
                {
                    opSequence.Add(new MaimaiOperationMultNote(
                        Math.Round(baseTime, TIME_EPS),
                        Math.Round(baseTime + note.holdTime, TIME_EPS),
                        note.startPosition,
                        note.startPosition,
                        2,
                        note.noteContent!,
                        positionX,
                        positionY
                    ));
                }
                else
                {
                    // TODO: dx谱面兼容
                    MessageBox.Show("无理检测暂时不支持dx谱面！ / dx map not support now", "警告");
                    return -1;
                }
        }

        opSequence.Sort(delegate(MaimaiOperationMultNote x, MaimaiOperationMultNote y)
        {
            if (x.startTime == y.startTime)
            {
                if (x.ntype == y.ntype)
                    return 0;
                return x.ntype < y.ntype ? -1 : 1;
            }

            return x.startTime < y.startTime ? -1 : 1;
        });

        var inHandling = new List<MaimaiOperationMultNote>();

        foreach (var op in opSequence)
        {
            for (var i = inHandling.Count - 1; i >= 0; i--)
                if (inHandling[i].endTime < op.startTime)
                    inHandling.RemoveAt(i);

            if (op.ntype == 3)
            {
                for (var i = inHandling.Count - 1; i >= 0; i--)
                    if (inHandling[i].endTime == op.startTime &&
                        inHandling[i].endArea == op.startArea)
                        inHandling.RemoveAt(i);
            }
            else if (op.ntype == 1)
            {
                for (var i = inHandling.Count - 1; i >= 0; i--)
                    if (inHandling[i].ntype == 1 &&
                        inHandling[i].startTime == op.startTime &&
                        inHandling[i].startArea == op.startArea)
                        inHandling.RemoveAt(i);
            }

            inHandling.Add(op);

            if (inHandling.Count > 2)
            {
                var warningText = MainWindow.GetLocalizedString("MultNoteError1");
                foreach (var e in inHandling)
                {
                    if (e.ntype == 1) warningText += "*";
                    warningText += string.Format(
                        "\"{0}\"({1}L,{2}C) ",
                        e.noteContent, e.positionY + 1, e.positionX + 1
                    );
                }

                warningText += string.Format(MainWindow.GetLocalizedString("MultNoteError2"), inHandling.Count);
                addWarning(warningText, inHandling[0].positionX, inHandling[0].positionY);
                errorCnt++;
            }
        }

        return errorCnt;
    }

    private int slideDetect(double judgementLength)
    {
        // 注释可见 https://github.com/Moying-moe/maimaiMuriDetector MaiMuriDetector.slideDetect(self, judgementLength = 0.15)

        var prog = @"(\d)(.+?)(\d{1,2})\[.+?\]";

        var opSequence = new List<MaimaiOperationSlide>();

        foreach (var noteGroup in SimaiProcess.notelist)
        {
            var baseTime = noteGroup.time;
            var positionX = noteGroup.rawTextPositionX;
            var positionY = noteGroup.rawTextPositionY;

            foreach (var note in noteGroup.getNotes())
                if (note.noteType == SimaiNoteType.Tap ||
                    note.noteType == SimaiNoteType.Hold)
                {
                    opSequence.Add(new MaimaiOperationSlide(
                        baseTime,
                        note.startPosition,
                        0,
                        note.noteContent!,
                        positionX,
                        positionY
                    ));
                }
                else if (note.noteType == SimaiNoteType.Slide)
                {
                    // 星星头加入队列
                    opSequence.Add(new MaimaiOperationSlide(
                        baseTime,
                        note.startPosition,
                        0,
                        note.noteContent!,
                        positionX,
                        positionY
                    ));
                    string sStart;
                    string sType;
                    string sEnd;

                    try
                    {
                        var temp = Regex.Match(note.noteContent!, prog);
                        sStart = temp.Groups[1].Value;
                        sType = temp.Groups[2].Value;
                        sEnd = temp.Groups[3].Value;
                    }
                    catch
                    {
                        addWarning(string.Format(
                            MainWindow.GetLocalizedString("SyntaxError"),
                            note.noteContent,
                            positionY + 1,
                            positionX + 1
                        ), positionX, positionY);
                        continue;
                    }

                    if (sType == "V")
                    {
                        // 转折型
                        var sEnd0 = notePos(
                            int.Parse(sEnd.Substring(0, 1)) - int.Parse(sStart),
                            true
                        );
                        var sEnd1 = notePos(
                            int.Parse(sEnd.Substring(1, 1)) - int.Parse(sStart),
                            true
                        );
                        sEnd = sEnd0 + "," + sEnd1;
                    }
                    else
                    {
                        sEnd = notePos(int.Parse(sEnd) - int.Parse(sStart), true).ToString();
                    }

                    if (sType == ">" &&
                        int.Parse(sStart) >= 3 && int.Parse(sStart) <= 6)
                        /*
                         * WARNING:
                         * 这其实是一个测定数据时的遗留问题
                         * 在测定数据的时候，对于每一种slide，都以1开头来测定，并存储相对的位置
                         * 在实际判定的时候，会根据实际的起点和相对位置计算绝对位置，也就是说，是在测定数据的基础上进行了旋转
                         * 但是>和<型的slide，其方向会受到起点位置的影响
                         * 以>为例，当起点是7812时，是顺时针，起点是3456时，则为逆时针
                         * 但是在测定时，因为起点总是1，所以>总是顺时针的，<总是逆时针的
                         * --- 换言之，在SLIDE_TIME里，>不表示向右开始回旋的slide，而表示“总是顺时针的回旋slide” ---
                         * 所以此处选择对>和<slide进行特判，如果和测定时的方向相反，则人为反转操作符
                         *
                         * 请注意：这是目前的权宜之计，也许后续会更正这个问题
                         * **/
                        // 当起点为3456 slide类型为>时 和测定方向相反
                        sType = "<";
                    else if (sType == "<" &&
                             int.Parse(sStart) >= 3 && int.Parse(sStart) <= 6)
                        sType = ">";

                    JToken sTimeInfo;
                    try
                    {
                        sTimeInfo = SLIDE_TIME![sType]![sEnd]!;
                        foreach (var each in sTimeInfo!)
                        {
                            var timeRatio = each["time"]!.ToObject<double>();
                            var passArea = each["area"]!.ToObject<int>();
                            opSequence.Add(new MaimaiOperationSlide(
                                timeRatio * note.slideTime + note.slideStartTime,
                                notePos(passArea + int.Parse(sStart), false),
                                1,
                                note.noteContent!,
                                positionX,
                                positionY
                            ));
                        }
                    }
                    catch
                    {
                        addWarning(string.Format(
                            MainWindow.GetLocalizedString("SyntaxError"),
                            note.noteContent,
                            positionY + 1,
                            positionX + 1
                        ), positionX, positionY);
                    }
                }
                else
                {
                    // TODO: dx谱面兼容
                    MessageBox.Show("无理检测暂时不支持dx谱面！ / dx map not support now", "警告");
                    return -1;
                }
        }

        opSequence.Sort(delegate(MaimaiOperationSlide x, MaimaiOperationSlide y)
        {
            if (x.time == y.time)
            {
                if (x.ntype == y.ntype)
                    return 0;
                return x.ntype > y.ntype ? -1 : 1;
            }

            return x.time < y.time ? -1 : 1;
        });
        var errorCnt = 0;

        var inJudgement = new List<MaimaiOperationSlide>();

        foreach (var op in opSequence)
        {
            var curTime = op.time;

            for (var i = inJudgement.Count - 1; i >= 0; i--)
                if (inJudgement[i].time + judgementLength < curTime)
                    inJudgement.RemoveAt(i);

            if (op.ntype == 1)
                inJudgement.Add(op);
            else if (op.ntype == 0)
                foreach (var e in inJudgement)
                    if (e.area == op.area &&
                        op.time - judgementLength < e.time &&
                        e.time < op.time)
                    {
                        addWarning(string.Format(
                            MainWindow.GetLocalizedString("SlideError"),
                            e.noteContent, e.positionY + 1, e.positionX + 1,
                            op.noteContent, op.positionY + 1, op.positionX + 1,
                            Math.Floor((op.time - e.time) * 1000)
                        ), e.positionX, e.positionY);
                        errorCnt++;
                    }
        }

        return errorCnt;
    }

    private void BeatmapMuriCheck(bool multNoteEnable, double slideCheckAccuracy)
    {
        if (mcr != null) mcr.Close();
        mcr = new MuriCheckResult();
        mcr.Owner = Owner;
        mcr.CheckResult_Listbox.Items.Clear();

        int multNoteError;
        if (multNoteEnable)
        {
            multNoteError = multNoteDetect();
            if (multNoteError == -1)
                // 不支持dx谱面 退出
                return;
        }
        else
        {
            multNoteError = -114514; // This is a MAGIC NUMBER, Do not touch ;)
        }

        var slideError = slideDetect(slideCheckAccuracy);
        if (slideError == -1)
            // 不支持dx谱面 退出
            return;
        mcr.Show();
        if (multNoteEnable)
            MessageBox.Show(
                string.Format(MainWindow.GetLocalizedString("CheckDone1"),
                    multNoteError, slideError),
                MainWindow.GetLocalizedString("Info")
            );
        else
            MessageBox.Show(
                string.Format(MainWindow.GetLocalizedString("CheckDone2"),
                    slideError),
                MainWindow.GetLocalizedString("Info")
            );
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        SlideAccuracy_TextBox.Text = ((MainWindow)Owner).editorSetting!.DefaultSlideAccuracy.ToString();
    }

    private void Window_Initialized(object sender, EventArgs e)
    {
        ReadMuriCheckSlideTime();
    }
}