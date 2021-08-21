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
using System.Text.RegularExpressions;

namespace MajdataEdit
{
    /// <summary>
    /// BPMtap.xaml 的交互逻辑
    /// </summary>
    
    class MaimaiOperation
    {
        public double startTime;
        public double endTime;
        public int startArea;
        public int endArea;
        public int ntype;
        public String noteContent;
        public int positionX;
        public int positionY;

        public MaimaiOperation(double _startTime, double _endTime, int _startArea,
            int _endArea, int _ntype, String _noteContent, int _positionX, 
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

    public partial class MuriCheck : Window
    {
        MuriCheckResult mcr = null;
        public MuriCheck()
        {
            InitializeComponent();
        }

        private void StartCheck_Button_Click(object sender, RoutedEventArgs e)
        {
            double slideAccuracy;
            try
            {
                slideAccuracy = Double.Parse(SlideAccuracy_TextBox.Text);
            } catch (System.FormatException)
            {
                MessageBox.Show("Slide撞尾检测精度必须是个数字! 请检查您输入是否有误, 如误输入了空格.", "警告");
                return;
            }
            BeatmapMuriCheck(MultNote_Checkbox.IsChecked == true, slideAccuracy);
        }

        private void addWarning(String content)
        {
            if(mcr != null)
            {
                ListBoxItem resultRow = new ListBoxItem();
                resultRow.Content = content;
                mcr.CheckResult_Listbox.Items.Add(resultRow);
            }
        }

        private int multNoteDetect()
        {
            int TIME_EPS = 5;

            // 注释可见 https://github.com/Moying-moe/maimaiMuriDetector MaiMuriDetector.multNoteDetect(self, eps=5)

            var prog = @"(\d)(.+?)(\d{1,2})\[\d+?\:\d+?\]";
            int errorCnt = 0;

            List<MaimaiOperation> opSequence = new List<MaimaiOperation>();

            foreach(var noteGroup in SimaiProcess.notelist)
            {
                double baseTime = noteGroup.time;
                int positionX = noteGroup.rawTextPositionX;
                int positionY = noteGroup.rawTextPositionY;

                foreach(var note in noteGroup.getNotes())
                {
                    if(note.noteType == SimaiNoteType.Tap)
                    {
                        opSequence.Add(new MaimaiOperation(
                            _startTime: Math.Round(baseTime, TIME_EPS),
                            _endTime: Math.Round(baseTime, TIME_EPS),
                            _startArea: note.startPosition,
                            _endArea: note.startPosition,
                            _ntype: 0,
                            _noteContent: note.noteContent,
                            _positionX: positionX,
                            _positionY: positionY
                            ));
                    }else if(note.noteType == SimaiNoteType.Slide)
                    {
                        opSequence.Add(new MaimaiOperation(
                            _startTime: Math.Round(baseTime, TIME_EPS),
                            _endTime: Math.Round(baseTime, TIME_EPS),
                            _startArea: note.startPosition,
                            _endArea: note.startPosition,
                            _ntype: 1,
                            _noteContent: note.noteContent,
                            _positionX: positionX,
                            _positionY: positionY
                            ));
                        int endPosition;
                        try
                        {
                            Match temp = Regex.Match(note.noteContent, prog);
                            endPosition = int.Parse(
                                temp.Groups[3].Value.Substring(temp.Groups[3].Value.Length-1, 1)
                            );
                        }catch( Exception )
                        {
                            addWarning(String.Format(
                                "[语法错误] \"{0}\"({1}L,{2}C)解析失败，可能存在语法错误",
                                note.noteContent,
                                positionY+1,
                                positionX+1
                                ));
                            continue;
                        }
                        opSequence.Add(new MaimaiOperation(
                            _startTime: Math.Round(note.slideStartTime, TIME_EPS),
                            _endTime: Math.Round(note.slideStartTime + note.slideTime, TIME_EPS),
                            _startArea: note.startPosition,
                            _endArea: endPosition,
                            _ntype: 3,
                            _noteContent: note.noteContent,
                            _positionX: positionX,
                            _positionY: positionY
                            ));
                    }else if(note.noteType == SimaiNoteType.Hold)
                    {
                        opSequence.Add(new MaimaiOperation(
                            _startTime: Math.Round(baseTime, TIME_EPS),
                            _endTime: Math.Round(baseTime + note.holdTime, TIME_EPS),
                            _startArea: note.startPosition,
                            _endArea: note.startPosition,
                            _ntype: 2,
                            _noteContent: note.noteContent,
                            _positionX: positionX,
                            _positionY: positionY
                            ));
                    }
                    else
                    {
                        MessageBox.Show("无理检测暂时不支持dx谱面！", "警告");
                        return -1;
                    }
                }
            }

            opSequence.Sort(delegate (MaimaiOperation x, MaimaiOperation y)
            {
                if (x.startTime == y.startTime)
                {
                    if (x.ntype == y.ntype)
                    {
                        return 0;
                    }
                    else
                    {
                        return x.ntype < y.ntype ? -1 : 1;
                    }
                }
                else
                {
                    return x.startTime < y.startTime ? -1 : 1;
                }
            });

            List<MaimaiOperation> inHandling = new List<MaimaiOperation>();

            foreach( var op in opSequence)
            {
                for(int i=inHandling.Count-1; i>=0; i--)
                {
                    if(inHandling[i].endTime < op.startTime)
                    {
                        inHandling.RemoveAt(i);
                    }
                }

                if(op.ntype == 3)
                {
                    for (int i = inHandling.Count - 1; i >= 0; i--)
                    {
                        if (inHandling[i].endTime == op.startTime &&
                            inHandling[i].endArea == op.startArea)
                        {
                            inHandling.RemoveAt(i);
                        }
                    }
                }else if(op.ntype == 1)
                {
                    for (int i = inHandling.Count - 1; i >= 0; i--)
                    {
                        if (inHandling[i].ntype == 1 &&
                            inHandling[i].startTime == op.startTime &&
                            inHandling[i].startArea == op.startArea)
                        {
                            inHandling.RemoveAt(i);
                        }
                    }
                }

                inHandling.Add(op);

                if(inHandling.Count > 2)
                {
                    String warningText = "[多押无理] ";
                    foreach(var e in inHandling)
                    {
                        if(e.ntype == 1)
                        {
                            warningText += "*";
                        }
                        warningText += String.Format(
                            "\"{0}\"({1}L,{2}C) ",
                            e.noteContent, e.positionY+1, e.positionX+1
                            );
                    }
                    warningText += String.Format("可能形成了{0}押", inHandling.Count);
                    addWarning(warningText);
                    errorCnt++;
                }
            }
            return errorCnt;
        }

        void BeatmapMuriCheck(bool multNoteEnable = true, double slideCheckAccuracy = 0.2)
        {
            mcr = new MuriCheckResult();
            mcr.Owner = this;

            int multNoteError = this.multNoteDetect();
            int slideError = 0;
            /*
                ListBoxItem resultRow = new ListBoxItem();
                resultRow.Content = note.notesContent;
                mcr.CheckResult_Listbox.Items.Add(resultRow);
             */
            mcr.Show();
            MessageBox.Show(
                String.Format("检测完毕, 共发现{0}个多押无理，{1}个撞尾无理。您可在 检测结果 窗口中查看\n请注意：谱面无理检测提供的意见并不一定准确，结果仅供参考。",
                    multNoteError,slideError),
                "提示"
                );
        }
    }
}
