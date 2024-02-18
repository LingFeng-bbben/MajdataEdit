using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Automation;

namespace MajdataEdit.SyntaxModule
{
    internal class SimaiErrorInfo : ErrorInfo
    {
        public string eMessage;
        public SimaiErrorInfo(int _posX, int _posY, string eMessage) : base(_posX, _posY)
        {
            this.eMessage = eMessage;
        }
    }
    internal static class SyntaxChecker
    {
        static List<SimaiTimingPoint> NoteList;
        static readonly string[] SlideTypeList = { "qq", "pp", "q", "p", "w", "z", "s", "V", "v", "<", ">", "^", "-" };
        static readonly char[] SensorList = { 'A','B','C','D','E'};
        internal static List<SimaiErrorInfo> ErrorList = new();
        enum DirectionType
        {
            /// <summary>
            /// 顺时针
            /// </summary>
            Clockwise,
            /// <summary>
            /// 同一直线
            /// </summary>
            Opposite,
            /// <summary>
            /// 逆时针
            /// </summary>
            Anticlockwise
        }
        /// <summary>
        /// 检查原始Simai文本
        /// </summary>
        /// <param name="noteStr"></param>
        internal static async Task ScanAsync(string str)
        {
            await Task.Run(() =>
            {
                ErrorList.Clear();
                int line = 1;
                int column = 1;
                var simaiChart = str.Split(",");

                foreach (var s in simaiChart)
                {
                    string simaiStr = s.Replace("\n", "");

                    if (string.IsNullOrEmpty(s))
                        continue;
                    if (s.Contains("\n"))
                    {
                        line++;
                        column = 1;
                    }

                    if (string.IsNullOrEmpty(simaiStr))
                        continue;

                    var notes = simaiStr.Split("/");
                    for (int i = 0;i < notes.Length;i++)
                    {
                        var noteStr = notes[i];
                        if (i == 0 && !SpecialSyntaxCheck(ref noteStr, column, line))
                            continue;
                        else if (string.IsNullOrEmpty(noteStr))
                            continue;
                        NoteSyntaxCheck(noteStr, column, line);
                    }
                    column++;
                }
            });
        }
        /// <summary>
        /// 检查已解释的Note列表
        /// </summary>
        internal static void Scan()
        {
            NoteList = SimaiProcess.notelist;

            foreach (var note in NoteList)
            {
                var raw = note.notesContent;
                var notes = raw.Split("/");

                foreach (var _note in notes)
                    NoteSyntaxCheck(_note,note.rawTextPositionX,note.rawTextPositionY);
            }

        }
        /// <summary>
        /// 检查BPM与拍号的合法性
        /// </summary>
        static bool SpecialSyntaxCheck(ref string simaiStr,int posX,int posY)
        {
            int bpmHeadCount = 0;
            int bpmTailCount = 0;
            int beatHeadCount = 0;
            int beatTailCount = 0;
            int HSpeedHeadCount = 0;
            int HSpeedTailCount = 0;

            int bpmFirstIndex = simaiStr.IndexOf('(');
            int beatFirstIndex = simaiStr.IndexOf('{');

            Action<string> addError = s =>
            {
                ErrorList.Add(new SimaiErrorInfo(posX, posY,
                    string.Format(
                        MainWindow.GetLocalizedString("SyntaxError"),
                        s,
                        posY,
                        posX)));
            };

            for (int i = 0; i < simaiStr.Length; i++)
            {
                char c = simaiStr[i];
                switch(c)
                {
                    case '(':
                        bpmHeadCount++;
                        break;
                    case ')':
                        bpmTailCount++;
                        break;
                    case '{':
                        beatHeadCount++;
                        break;
                    case '}':
                        beatTailCount++;
                        break;
                    case '<':
                        if (i + 1 < simaiStr.Length && int.TryParse(simaiStr[(i + 1)..(i + 2)], out int j) && PointCheck(j))
                            break;
                        HSpeedHeadCount++;
                        break;
                    case '>':
                        if (i + 1 < simaiStr.Length && int.TryParse(simaiStr[(i + 1)..(i + 2)], out j) && PointCheck(j))
                            break;
                        HSpeedTailCount++;
                        break;
                }
            }

            //纯Note语句跳过检查
            if ((bpmTailCount + bpmHeadCount + beatHeadCount + beatTailCount) == 0)
                return true;

            if(bpmHeadCount > 1 || bpmTailCount > 1)
            {
                addError(simaiStr);
                return false;
            }
            else if (bpmHeadCount != bpmTailCount)
            {
                addError(simaiStr);
                return false;
            }

            if (beatHeadCount > 1 || beatTailCount > 1)
            {
                addError(simaiStr);
                return false;
            }
            else if (beatHeadCount != beatTailCount)
            {
                addError(simaiStr);
                return false;
            }

            if (HSpeedHeadCount != HSpeedTailCount)
            {
                addError(simaiStr);
                return false;
            }

            //{}与()必须在Note前面
            if (bpmFirstIndex != 0 && beatFirstIndex != 0)
                addError(simaiStr);
            else
            {
                int bpmEndIndex = simaiStr.IndexOf(')');
                int beatEndIndex = simaiStr.IndexOf('}');
                int HSpeedStartIndex = simaiStr.IndexOf('<');
                int HSpeedEndIndex = simaiStr.IndexOf('>');

                bool hadBpm = bpmFirstIndex != bpmEndIndex;
                bool hadBeat = beatFirstIndex != beatEndIndex;

                //HSpeed变速语法检查
                if ((HSpeedStartIndex != -1 && HSpeedEndIndex != -1))
                {
                    if(HSpeedEndIndex < HSpeedStartIndex)// 不知道是什么奇奇怪怪的东西，形如"> xxx <"
                    {
                        addError(simaiStr);
                        return false;
                    }

                    var HSpeedStr = simaiStr[(HSpeedStartIndex + 1)..(HSpeedEndIndex)].Replace(" ","");
                    var s = HSpeedStr.Split("HS*");

                    if(HSpeedStr.Contains("HS*"))
                    {
                        if (s.Length != 2)
                        {
                            addError(simaiStr);
                            return false;
                        }
                        else if (!string.IsNullOrEmpty(s[0]))
                        {
                            addError(simaiStr);
                            return false;
                        }
                        else if (!IsNum(s[1]))
                        {
                            addError(simaiStr);
                            return false;
                        }

                        simaiStr = simaiStr.Remove(HSpeedStartIndex, (HSpeedEndIndex - HSpeedStartIndex) + 1);
                    }
                }

                //有头无尾
                if((bpmFirstIndex != -1 && bpmEndIndex == -1) || (bpmFirstIndex != -1 && beatEndIndex == -1))
                {
                    addError(simaiStr);
                    return false;
                }

                if (hadBeat && !IsInteger(simaiStr[(beatFirstIndex+1)..(beatEndIndex)]))
                {
                    addError(simaiStr);
                    return false;
                }
                if (hadBpm && !IsNum(simaiStr[(bpmFirstIndex + 1)..(bpmEndIndex)]))
                {
                    addError(simaiStr);
                    return false;
                }

                simaiStr = simaiStr[(Math.Max(bpmEndIndex, beatEndIndex) + 1)..];
                
            }
            return true;
        }
        /// <summary>
        /// 检查Note语句的Body部分是否正确(譬如是否存在重复的"["或"]")
        /// </summary>
        /// <param name="bodyStr"></param>
        /// <returns>
        /// "["与"]"的索引位置
        /// </returns>
        static int[]? BodySyntaxCheck(string bodyStr,bool isSlide = false)
        {
            List<int> bodyIndex = new();
            int bodyHeadCount = 0;
            int bodyTailCount = 0;

            for(int index = 0;index < bodyStr.Length;index++)
            {
                char c = bodyStr[index];
                if(c == '[')
                {
                    bodyHeadCount++;
                    bodyIndex.Add(index);
                }
                else if(c == ']')
                {
                    bodyTailCount++;
                    bodyIndex.Add(index);
                }
            }

            //正常情况下"["与"]"的数量应该相等
            //当非Slide的Note语句结尾不是"]"时，判断为语法错误
            //Slide是特例，结尾为b表示Break Slide
            if (bodyHeadCount != bodyTailCount)
                return null;
            else if (!isSlide && (bodyHeadCount != 1 || bodyTailCount != 1))
                return null;
            else if (!isSlide && (bodyIndex.Last() != bodyStr.Length - 1))
                return null;

            return bodyIndex.ToArray();

        }
        /// <summary>
        /// 检查Note语句合法性，不检查BPM，拍号与变速语句
        /// </summary>
        /// <param name="noteStr"></param>
        static bool NoteSyntaxCheck(string noteStr,int posX,int posY)
        {
            if (IsTap(noteStr))
                return true;
            else if (IsHold(noteStr))
            {
                if (HoldSyntaxCheck(noteStr))
                    return true;
            }
            else if (IsSlide(noteStr))
            {
                if (SlideSyntaxCheck(noteStr))
                    return true;
            }
            else if (IsTouch(noteStr))
                return true;
            else if(noteStr == "E")
                return true;
            ErrorList.Add(new SimaiErrorInfo(posX, posY,
                    string.Format(
                        MainWindow.GetLocalizedString("SyntaxError"),
                        noteStr,
                        posY,
                        posX)));
            return false;

        }
        /// <summary>
        /// 检查Hold参数的合法性
        /// </summary>
        /// <param name="holdStr"></param>
        /// <returns></returns>
        static bool HoldSyntaxCheck(string holdStr)
        {
            //特殊：2h之类的短Hold，前面已经检查过一次，无需再次检查
            if (holdStr.Length <= 4)
                return true;

            int[]? bodyIndex = BodySyntaxCheck(holdStr);
            if (bodyIndex is null)//body部分错误
                return false;

            int startIndex = bodyIndex[0];
            int endIndex = bodyIndex[1];
            string body = holdStr[(startIndex + 1)..(endIndex)];
            if (body.Length < 2)//最短Hold参数: #2 (表示时值为2秒)
                return false;

            if (body.Contains("#"))
            {
                if (body[0] == '#')
                    return double.TryParse(body[1..], out double i);
                else
                {
                    var splitBody = body.Split("#");
                    if (splitBody.Length != 2)//正确格式: 150#4:1
                        return false;
                    else
                        return RatioSyntaxCheck(splitBody[1]) && double.TryParse(splitBody[0], out double i);
                }
            }
            else
                return RatioSyntaxCheck(body);
        }
        /// <summary>
        /// 检查Slide路径与参数的合法性
        /// </summary>
        /// <param name="slideStr"></param>
        /// <returns></returns>
        static bool SlideSyntaxCheck(string slideStr)
        {
            if (slideStr.Length < 3)
                return false;
            if (slideStr[1] is ('b' or 'x') && slideStr[2] is ('b' or 'x'))
                slideStr = slideStr.Remove(1,2);
            else if(slideStr[1] is ('b' or 'x'))
                slideStr = slideStr.Remove(1, 1);
            
            int starPoint = int.Parse(slideStr[0..1]);//星星头键位

            char[] typeList = string.Concat(SlideTypeList.Skip(2).ToArray()).ToCharArray();
            int slideCount = 0;

            foreach(var _slideStr in slideStr.Split("*"))//同头Slide处理
            {
                //传过去的参数应当为 1-7-5[8:1] 或 -7-5[8:1]
                int[]? bodyIndex = BodySyntaxCheck(_slideStr, true);
                if (bodyIndex is null)//body部分错误
                    return false;                

                int? startPoint = null;
                int? endPoint = null;
                int? flexionPoint = null;
                string slideType = "";

                //组合Slide多参数
                //e.g. 1-7[8:1]-5[8:1]
                //不得不说，这种写法多少有点xx
                int subSlideCount = 0;

                //这个循环用于检查Slide路径合法性
                for (int i = 0; i < _slideStr.Length;)
                {
                    //获取Slide路径的起始点
                    if (slideCount != 0 && i == 0)//同头Slide处理
                        startPoint = starPoint;
                    else if (subSlideCount > 0)//组合Slide识别
                    {
                        startPoint = endPoint;
                        endPoint = null;
                        i++;
                    }
                    else
                    {
                        if (IsInteger(_slideStr[i..(i + 1)]))
                            startPoint = int.Parse(_slideStr[i..(i + 1)]);
                        else
                            return false;
                        i++;
                    }
                    

                    //获取Slide类型
                    if (typeList.Contains(_slideStr[i]))
                    {
                        slideType = _slideStr[i..(i + 1)];
                        if (_slideStr[i] == _slideStr[i + 1])//用于检查"pp"与"qq"
                        {
                            slideType += _slideStr[i + 1];
                            i += 2;
                        }
                        else
                            i++;
                    }
                    else
                        return false;

                    //获取"V"类型Slide的拐点
                    if (slideType == "V")
                    {
                        if (IsInteger(_slideStr[i..(i + 1)]))
                            flexionPoint = int.Parse(_slideStr[i..(i + 1)]);
                        else
                            return false;
                        i++;
                    }
                    //获取Slide路径终点
                    if (IsInteger(_slideStr[i..(i + 1)]))
                        endPoint = int.Parse(_slideStr[i..(i + 1)]);
                    else
                        return false;

                    //Slide路径检查，检查Slide路径是否合法
                    //1-7这类将不会通过检查
                    if (!SlidePathCheck(slideType, (int)startPoint, (int)endPoint, flexionPoint))
                        return false;

                    //检查下一字符是否为"["或"b"
                    //同时避免越界

                    if ((i + 1 < _slideStr.Length) && _slideStr[i + 1] == '[')
                    {
                        var headIndex = Array.IndexOf<int>(bodyIndex, ++i);
                        //未找到头部索引
                        if (headIndex == -1)
                            return false;

                        //将当前位置设置为"]"的后一位
                        if (_slideStr.Last() == 'b')
                            i = bodyIndex[headIndex + 1] + 1;
                        else if (_slideStr.Last() == ']')
                            i = bodyIndex[headIndex + 1];
                        else
                            return false;
                    }
                    
                    subSlideCount++;
                    if (i + 1 >= _slideStr.Length)
                        break;
                }

                //1-4-6[4:1]-1[4:1]这种写法是不允许的
                //要么1-4-6-1[4:1]
                //或者1-4[4:1]-6[4:1]-1[4:1]
                if (subSlideCount != bodyIndex.Length / 2 && bodyIndex.Length != 2)
                    return false;

                //参数检查
                Func<int,bool> bodyChecker = i =>
                {
                    int bodyStartIndex = bodyIndex[i * 2];
                    int bodyEndIndex = bodyIndex[i * 2 + 1];
                    string body = _slideStr[(bodyStartIndex + 1)..bodyEndIndex];
                    int paramType = 0;

                    //匹配参数模式
                    for (int j = 0; j < body.Length; j++)
                        if (body[j] == '#')
                            paramType++;
                    if (paramType > 3)
                        return false;

                    try
                    {
                        switch (paramType)
                        {
                            case 0:
                                if (!RatioSyntaxCheck(body))
                                    return false;
                                break;
                            case 1:
                            case 2:
                                var param = body.Split("#");
                                var bpmStr = param[0];
                                var length = paramType == 2 ? param[2] : param[1];

                                if (!IsNum(bpmStr))
                                    return false;
                                if (!IsNum(length) && !RatioSyntaxCheck(length))
                                    return false;
                                break;
                            case 3:
                                param = body.Split("#");
                                var startLength = param[0];
                                bpmStr = param[2];
                                length = param[3];

                                if (!IsNum(startLength))
                                    return false;
                                if (!IsNum(bpmStr))
                                    return false;
                                if (!RatioSyntaxCheck(length))
                                    return false;
                                break;
                        }
                        return true;
                    }
                    catch
                    {
                        return false;
                    }
                };
                if(bodyIndex.Length == 2)
                {
                    if (!bodyChecker(0))
                        return false;
                }
                else
                {
                    for (int i = 0; i < subSlideCount; i++)
                    {
                        if (!bodyChecker(i))
                            return false;
                    }
                }
                slideCount++;
            }
            return true;

        }
        /// <summary>
        /// Slide路径检查
        /// </summary>
        /// <param name="slideType"></param>
        /// <param name="startPoint"></param>
        /// <param name="endPoint"></param>
        /// <param name="flexionPoint"></param>
        /// <returns></returns>
        static bool SlidePathCheck(string slideType,int startPoint,int endPoint,
                                   int? flexionPoint = null)
        {
            if (!PointCheck(startPoint) || !PointCheck(endPoint))
                return false;

            switch(slideType)
            {
                case "^":
                case "v":
                    if (GetPointInterval(startPoint, endPoint) is (0 or 4))
                        return false;
                    return true;
                case "-":
                    if (startPoint == endPoint)
                        return false;
                    else if (GetPointInterval(startPoint, endPoint) < 2)
                        return false;
                    return true;
                case "V":
                    if (startPoint == endPoint)
                        return false;
                    else if (GetPointInterval(startPoint, (int)flexionPoint!) != 2)
                        return false;
                    else if (GetPointInterval((int)flexionPoint!, endPoint) < 2)
                        return false;
                    return true;
                case "s":
                case "z":
                case "w":
                    if (startPoint == endPoint)
                        return false;
                    else if ((DirectionType)PointCompare(startPoint, endPoint)! != DirectionType.Opposite)
                        return false;
                    return true;
            }
            return true;
        }
        /// <summary>
        /// 获取键位的序号，用于判断键位的相对位置
        /// </summary>
        /// <param name="point"></param>
        /// <returns>
        /// 键位与目标键位的夹角；若目标键位不合法，返回null
        /// </returns>
        static int? GetPointIndex(int point)
        {
            //这里使用过#8，#4的直线作为中轴线，以#8为起始点
            //采用目标键位与起始点的夹角作为键位序号
            //e.g. #1的键位序号为45，#8键位序号为0
            //一般来说，除了#8，A键位序号 - B键位序号 > 0则说明B键位位于A键位的左边(逆时针方向)，反之亦然

            if (!PointCheck(point))
                return null;
            switch(point)
            {
                case 8:
                    return 0;
                default:
                    return point * 45;
            }
        }
        /// <summary>
        /// 获取键与键之间最短距离
        /// </summary>
        /// <param name="point"></param>
        /// <param name="targetPoint"></param>
        /// <returns></returns>
        static int GetPointInterval(int point,int targetPoint)
        {
            int a = (int)GetPointIndex(point)!;
            int b = (int)GetPointIndex(targetPoint)!;
            int result = Math.Abs(a - b);

            if (result == 0)
                return 0;
            else
                return Math.Min(8 - (result / 45), result / 45);

        }
        /// <summary>
        /// 比较键位的相对位置
        /// </summary>
        /// <param name="point"></param>
        /// <param name="targetPoint"></param>
        /// <returns>
        /// 目标键位的方向(顺时针,同一直线或逆时针)
        /// </returns>
        static DirectionType? PointCompare(int point,int targetPoint)
        {
            if (!PointCheck(point) || !PointCheck(targetPoint))
                return null;
            if(point == targetPoint) return null;

            int a = (int)GetPointIndex(point)!;
            int b = (int)GetPointIndex(targetPoint)!;
            int result = a - b;

            if (Math.Abs(result) == 180)
                return DirectionType.Opposite;
            else if (result < -180 || (result > 0 && result < 180))
                return DirectionType.Anticlockwise;
            else
                return DirectionType.Clockwise;
        }
        /// <summary>
        /// 检查比例时值的合法性
        /// </summary>
        /// <param name="ratioStr"></param>
        /// <returns></returns>
        static bool RatioSyntaxCheck(string ratioStr)
        {
            var s = ratioStr.Split(":");

            if (s.Length != 2)
                return false;

            return int.TryParse(s[0], out int i) && int.TryParse(s[1], out i);
        }
        /// <summary>
        /// 判断是否为Note
        /// </summary>
        /// <param name="s"></param>
        /// <returns>
        /// Note的类型，若不是合法Note语句，返回null
        /// </returns>
        static SimaiNoteType? IsNote(string s)
        {
            if (IsTap(s))
                return SimaiNoteType.Tap;
            else if (IsHold(s))
                return SimaiNoteType.Hold;
            else if (IsSlide(s))
                return SimaiNoteType.Slide;
            else if (IsTouch(s))
                return SimaiNoteType.Touch;
            else
                return null;
        }
        /// <summary>
        /// 判断是否为Tap
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        static bool IsTap(string s)
        {
            int index;

            if (!int.TryParse(s[0..1], out index))//总是检查第1位
                return false;
            if (!PointCheck(index))//错误键位直接返回
                return false;

            if (s.Length == 1)
                return true;
            else if(s.Length == 2)// e.g. 28 , 2b , 2x
            {
                if (s[1] is 'b' or 'x')
                    return true;
                else
                    return int.TryParse(s, out int i) && (PointCheck(i % 10) && PointCheck(i / 10));
            }
            else if (s.Length == 3)// e.g. 2bx
            {
                var isBreak = s[1] is 'b' || s[2] is 'b';
                var isHanabi = s[1] is 'x' || s[2] is 'x';

                return isBreak && isHanabi;
            }

            return false;//其他情况即非法
        }
        /// <summary>
        /// 判断是否为Hold，不检查Hold参数
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        static bool IsHold(string s)
        {
            int index = 0;
            string header = s.Split("[")[0];
            bool isTouch = header[0] == 'C';

            if (!isTouch && !int.TryParse(s[0..1], out index))//总是检查第1位
                return false;
            if (!isTouch && !PointCheck(index))//错误键位直接返回
                return false;
            if (s.Length < 2)
                return false;
            else if (header is ("Ch" or "C1h" or "Chf" or "C1hf"))//TouchHold特例
                return true;
            else if (header[1] != 'h')//第2位不是"h"直接返回
                return false;

            if (header.Length == 2)// e.g. 2h
                return true;
            else if (header.Length == 3)// e.g. 2hb,2hx
                return s[2] is 'b' or 'x';
            else if (header.Length == 4)// e.g. 2hbx,2hxb
            {
                var isBreak = s[2] is 'b' || s[3] is 'b';
                var isHanabi = s[2] is 'x' || s[3] is 'x';

                return isBreak && isHanabi;
            }

            return false;
        }
        /// <summary>
        /// 判断是否为Slide，不检查Slide参数，只检查头部
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        static bool IsSlide(string s)
        {
            int index;
            var types = SlideTypeList.Skip(2).ToArray();
            string header = s.Split(string.Concat(types).ToCharArray())[0];            

            if (!int.TryParse(s[0..1], out index))//总是检查第1位
                return false;
            if (!PointCheck(index))//错误键位直接返回
                return false;

            if (header.Length == 1)// e.g. 1-8处理后header为1
                return true;
            else if (header.Length == 2 && header[1] is 'b' or 'x')// e.g. 1x,1b
                return true;
            else if (header.Length == 3)// e.g. 1bx,1xb
            {
                var isBreak = s[1] is 'b' || s[2] is 'b';
                var isHanabi = s[1] is 'x' || s[2] is 'x';

                return isBreak && isHanabi;
            }

            //出现其他长度一般是Slide种类错误
            return false;
        }
        /// <summary>
        /// 判断是否为Touch
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        static bool IsTouch(string s)
        {
            char sensor = s[0];

            if (s.Length is not (1 or 2 or 3))//Touch长度只能是1,2或3 ; e.g. C,B1,B1f
                return false;

            if (s.Length == 1)
                return s[0] == 'C';
            else if (!SensorList.Contains(sensor))
                return false;
            else if (s.Length == 2 && s[0] == 'C')// C1
                return s[1] == '1';
            else if (s.Length == 3 && s[0] == 'C')// C1f
                return s[1] == '1' && s[2] == 'f';
            else if (s.Length == 3)
                return s[2] == 'f' && int.TryParse(s[1..2], out int i) && PointCheck(i);
            else
                return int.TryParse(s[1..2], out int i) && PointCheck(i);
        }
        /// <summary>
        /// 判断string是否为数字
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        static bool IsNum(string s) => IsInteger(s) || IsFloat(s);
        static bool IsInteger(string s) => int.TryParse(s, out int i);
        static bool IsFloat(string s) => double.TryParse(s, out double i);
        /// <summary>
        /// 用于判断键位是否合法
        /// </summary>
        /// <param name="k"></param>
        /// <returns></returns>
        static bool PointCheck(int k) => k >= 1 && k <= 8;

    }
}
