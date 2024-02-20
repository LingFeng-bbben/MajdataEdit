
using System.Windows.Navigation;

namespace MajdataEdit.SyntaxModule
{
    enum InfomationLevel
    {
        Warning,
        Error
    }
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
    internal class SimaiErrorInfo : ErrorInfo
    {
        public string eMessage;
        public InfomationLevel Level;
        public SimaiErrorInfo(int _posX, int _posY, string eMessage,InfomationLevel level = InfomationLevel.Error) : base(_posX, _posY)
        {
            this.eMessage = eMessage;
            this.Level = level;
        }
    }
    internal static class SyntaxChecker
    {
        static readonly string[] SlideTypeList = { "qq", "pp", "q", "p", "w", "z", "s", "V", "v", "<", ">", "^", "-" };
        static readonly char[] SensorList = { 'A','B','C','D','E'};
        internal static List<SimaiErrorInfo> ErrorList = new();

        public static int GetErrorCount() => ErrorList.Where(e => e.Level is InfomationLevel.Error).Count();
        /// <summary>
        /// 检查原始Simai文本
        /// </summary>
        /// <param name="noteStr"></param>
        internal static async Task ScanAsync(string str)
        {
            Action<string, int, int,string, InfomationLevel> addInfo = (s, x, y, localStr,level) =>
            {
                ErrorList.Add(new SimaiErrorInfo(x, y,
                    string.Format(
                        MainWindow.GetLocalizedString(localStr),
                        s,
                        y,
                        x), level));
            };
            Action<string, int, int> addError = (s, x, y) => addInfo(s,x,y, "SyntaxError",InfomationLevel.Error);

            await Task.Run(() =>
            {
                ErrorList.Clear();
                int line = 1;
                int column = 1;                
                var simaiChart = str.Split(",");

                if (simaiChart.Last().Replace("\n","") == "E")//移除结尾E
                    simaiChart = simaiChart.SkipLast(1).ToArray();
                else
                    addInfo("", -1, -1, "SyntaxWarning", InfomationLevel.Warning);

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

                    //分割多押与伪多押
                    var notes = simaiStr.Split(new char[] { '/','`'});
                    for (int i = 0;i < notes.Length;i++)
                    {
                        var noteStr = notes[i];

                        if (string.IsNullOrEmpty(noteStr))
                        {
                            addError(simaiStr, column, line);
                            continue;
                        }
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
            var noteList = SimaiProcess.notelist;

            foreach (var note in noteList)
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

            int bpmFirstIndex = simaiStr.IndexOf('(');
            int beatFirstIndex = simaiStr.IndexOf('{');

            int[]? tagIndex = FindHSpeedBody(simaiStr);

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

            if (tagIndex is null)
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
                
                bool hadBpm = bpmFirstIndex != bpmEndIndex;
                bool hadBeat = beatFirstIndex != beatEndIndex;

                if((hadBpm || hadBeat) && simaiStr[0] is not ('(' or '{'))
                {
                    addError(simaiStr);
                    return false;
                }               

                //HSpeed变速语法检查
                if(tagIndex.Length != 0)
                {
                    var tagHead = tagIndex[0];
                    var tagTail = tagIndex[1];
                    var body = simaiStr[(tagHead + 1)..tagTail];

                    var s = body.Split("HS*");

                    if (s.Length != 2)//正常情况分割后的得到的Array长度应当是2
                    {
                        addError(simaiStr);
                        return false;
                    }
                    else if (!string.IsNullOrEmpty(s[0]))//正常情况第一个元素应当是Empty
                    {
                        addError(simaiStr);
                        return false;
                    }
                    else if (!IsNum(s[1]))//第二个元素应当是Number
                    {
                        addError(simaiStr);
                        return false;
                    }

                    //删除"<HS*1.0>"字符串，传递给NoteSyntaxChecker进行Note语法检查
                    simaiStr = simaiStr.Remove(tagHead, (tagTail - tagHead) + 1);
                }

                //有头无尾
                if((bpmFirstIndex != -1 && bpmEndIndex == -1) || (bpmFirstIndex != -1 && beatEndIndex == -1))
                {
                    addError(simaiStr);
                    return false;
                }

                //(){}或{}()
                if (hadBpm && hadBeat)
                {
                    //(){}
                    if (bpmEndIndex < beatFirstIndex && (beatFirstIndex != bpmEndIndex + 1))
                    {
                        addError(simaiStr);
                        return false;
                    }
                    else if(bpmEndIndex < beatFirstIndex && (beatFirstIndex == bpmEndIndex + 1))
                    { 
                        // noting to do
                    }
                    //{}()
                    else if (beatEndIndex < bpmFirstIndex && (bpmFirstIndex != beatEndIndex + 1))
                    {
                        addError(simaiStr);
                        return false;
                    }

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
        /// 寻找HSpeed的主体部分
        /// </summary>
        /// <param name="simaiStr"></param>
        /// <returns>
        /// HSpeed主体头和尾的索引，未找到返回Empty，HS语法错误返回null
        /// </returns>
        static int[]? FindHSpeedBody(string simaiStr)
        {
            //<HS*>
            simaiStr = simaiStr.Replace(" ", "");
            List<int> bodyHead = new();
            List<int> bodyTail = new();
            int? tagHead = null;
            int? tagTail = null;            
            
            for(int i = 0;i < simaiStr.Length;i++)
            {
                if(i + 3 < simaiStr.Length)
                {
                    var s = simaiStr[i..(i + 3)];
                    if(s == "HS*")
                    {
                        if (tagHead != null)
                            return null;
                        tagHead = i;
                        tagTail = i + 2;
                    }
                }
                switch(simaiStr[i])
                {
                    case '<':
                        bodyHead.Add(i);
                        break;
                    case '>':
                        bodyTail.Add(i);
                        break;
                }
            }

            bool hadTag = tagHead is not null;
            if (hadTag)
            {
                int head = bodyHead.Where(h => h < tagHead).DefaultIfEmpty(-1).Max();
                int tail = bodyTail.Where(t => t > tagTail).DefaultIfEmpty(-1).Min();

                if (bodyHead.Count == 0 || bodyTail.Count == 0)
                    return null;
                if (head == -1 || tail == -1)
                    return null;

                return new int[] { head, tail };

            }

            return Array.Empty<int>();
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
            else if (IsSlide(ref noteStr))
            {
                if (SlideSyntaxCheck(noteStr))
                    return true;
            }
            else if (IsTouch(noteStr))
                return true;
            //else if(noteStr == "E")
            //    return true;
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
            {
                //防止出现2h[],2h[,2hxx这种傻蛋情况
                foreach (var s in holdStr[2..])
                    if (s is not ('b' or 'x'))
                        return false;
                return true;
            }

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
            else if (IsSlide(ref s))
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

            if(s.Contains("$"))
            {
                var f = s.IndexOf("$");
                var l = s.LastIndexOf("$");

                if (f == l && s[1] == '$')
                    s = s.Remove(1, 1);
                else if (Math.Abs(f - l) == 1 && s[1..3] == "$$")
                    s = s.Remove(1, 2);
                else 
                    return false;
            }

            if (s.Length == 1)
                return true;
            else if(s.Length == 2)// e.g. 28 , 2b , 2x
            {
                if (s[1] is ('b' or 'x'))
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
            var _s = s.Split("[");
            string header = _s[0];
            bool isTouch = header[0] == 'C';

            if (!isTouch && !int.TryParse(s[0..1], out index))//总是检查第1位
                return false;
            if (!isTouch && !PointCheck(index))//错误键位直接返回
                return false;
            if (s.Length < 2 || header.Length < 2)
                return false;
            else if (header is ("Ch" or "C1h" or "Chf" or "C1hf"))//TouchHold特例
                return true;
            //Hold严格判定：第二位必须是'h'，'b'，'x'不限制位置
            //妥协一下，改为松判定
            //else if (header[1] != 'h')//第2位不是"h"直接返回
            //    return false;

            //Hold松判定：'h','b','x'不限定位置
            return header.Length switch
            {
                2 => header[1] is 'h',
                3 => header.Contains('h') && (header.Contains('b') || header.Contains('x')),
                4 => header.Contains('h') && header.Contains('b') && header.Contains('x'),
                _ => false
            };

            //Hold严格判定：第二位必须是'h'，'b'，'x'不限制位置
            //if (header.Length == 2)// e.g. 2h
            //    return true;
            //else if (header.Length == 3)// e.g. 2hb,2hx
            //    return s[2] is 'b' or 'x';
            //else if (header.Length == 4)// e.g. 2hbx,2hxb
            //{
            //    var isBreak = s[2] is 'b' || s[3] is 'b';
            //    var isHanabi = s[2] is 'x' || s[3] is 'x';

            //    return isBreak && isHanabi;
            //}

            //return false;
        }
        /// <summary>
        /// 判断是否为Slide，不检查Slide参数，只检查头部
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        static bool IsSlide(ref string s)
        {
            int index;
            var types = SlideTypeList.Skip(2).ToArray();
            string header = s.Split(string.Concat(types).ToCharArray())[0];            

            if (!int.TryParse(s[0..1], out index))//总是检查第1位
                return false;
            if (!PointCheck(index))//错误键位直接返回
                return false;

            if (header.Contains("?") || header.Contains("!"))
                if (header[1] is '?' or '!')
                {
                    header = header.Remove(1, 1);
                    s = s.Remove(1, 1);
                }
                else
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

            if (s.Length == 1)// C
                return s[0] == 'C';
            else if (!SensorList.Contains(sensor))// 判断触控区号是否合法
                return false;
            else if (s.Length == 2 && s[0] == 'C')// C1 or Cf
                return s[1] is '1' or 'f';
            else if (s.Length == 3 && s[0] == 'C')// C1f
                return s[1] == '1' && s[2] == 'f';
            else if (s.Length == 3)// A1f B1f
                return s[2] == 'f' && int.TryParse(s[1..2], out int i) && PointCheck(i);
            else//A1 B1
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
