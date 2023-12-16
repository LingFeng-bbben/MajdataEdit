using System.Text.RegularExpressions;

namespace MajdataEdit;

//小额负责的部分哟
internal static class Mirror
{
    public enum HandleType
    {
        LRMirror,
        UDMirror,
        HalfRotation,
        Rotation45,
        CcwRotation45
    }

    private static readonly Dictionary<char, char> MirrorLeftToRight = new()
    {
        { '8', '1' },
        { '1', '8' },
        { '2', '7' },
        { '7', '2' },
        { '3', '6' },
        { '6', '3' },
        { '4', '5' },
        { '5', '4' },
        { 'q', 'p' },
        { 'p', 'q' },
        { '<', '>' },
        { '>', '<' },
        { 'z', 's' },
        { 's', 'z' }
    }; //Note左右

    private static readonly Dictionary<char, char> MirrorTouchLeftToRight = new()
    {
        { '8', '2' },
        { '2', '8' },
        { '3', '7' },
        { '7', '3' },
        { '4', '6' },
        { '6', '4' },
        { '1', '1' },
        { '5', '5' }
    }; //Touch左右

    private static readonly Dictionary<char, char> MirrorUpsideDown = new()
    {
        { '4', '1' },
        { '5', '8' },
        { '6', '7' },
        { '3', '2' },
        { '7', '6' },
        { '2', '3' },
        { '8', '5' },
        { '1', '4' },
        { 'q', 'p' },
        { 'p', 'q' },
        { 'z', 's' },
        { 's', 'z' }
    }; //上下（全反=上下+左右）

    private static readonly Dictionary<char, char> MirrorTouchUpsideDown = new()
    {
        { '4', '2' },
        { '2', '4' },
        { '1', '5' },
        { '5', '1' },
        { '8', '6' },
        { '6', '8' },
        { '3', '3' },
        { '7', '7' }
    }; //Touch上下

    private static readonly Dictionary<char, char> Mirror180 = new()
    {
        { '5', '1' },
        { '4', '8' },
        { '3', '7' },
        { '6', '2' },
        { '2', '6' },
        { '7', '3' },
        { '1', '5' },
        { '8', '4' },
        { '<', '>' },
        { '>', '<' }
    }; //180旋转

    private static readonly Dictionary<char, char> Mirror45 = new()
    {
        { '8', '1' },
        { '7', '8' },
        { '6', '7' },
        { '5', '6' },
        { '4', '5' },
        { '3', '4' },
        { '2', '3' },
        { '1', '2' }
    };

    private static readonly Dictionary<char, char> MirrorCcw45 = new()
    {
        { '1', '8' },
        { '2', '1' },
        { '3', '2' },
        { '4', '3' },
        { '5', '4' },
        { '6', '5' },
        { '7', '6' },
        { '8', '7' }
    };

    private static readonly Dictionary<char, char> Mirror45special = new()
    {
        { '<', '>' },
        { '>', '<' }
    }; //2、6或3、7键位旋转改变<>符号

    public static string NoteMirrorHandle(string str, HandleType Type)
    {
        var handledStr = "";
        var noteStr = Regex.Replace(str, @"\s", "").Split(new[] { "," }, StringSplitOptions.None);
        var arrayIndex = 0;
        foreach (var note in noteStr)
        {
            var isArg = false;
            var isSpTouch = false;
            foreach (var a in note)
            {
                var index = note.IndexOf(a);
                if (new[] { '{', '[', '(' }.Contains(a))
                {
                    isArg = true;
                    handledStr += a;
                    continue;
                }

                if (a.Equals('<') && index < note.Length - 1 && note[index + 1].Equals('H'))
                {
                    isArg = true;
                    handledStr += a;
                    continue;
                }

                if (new[] { '}', ']', ')', '>' }.Contains(a))
                {
                    isArg = false;
                    handledStr += a;
                    continue;
                }

                if (isArg)
                {
                    handledStr += a;
                    continue;
                }

                switch (Type)
                {
                    case HandleType.LRMirror:
                        if (new[] { 'E', 'D' }.Contains(a)) //D区及E区Touch特殊处理
                        {
                            isSpTouch = true;
                            handledStr += a;
                            continue;
                        }

                        if (isSpTouch)
                        {
                            handledStr += MirrorTouchLeftToRight[a];
                            isSpTouch = false;
                            continue;
                        }

                        if (MirrorLeftToRight.ContainsKey(a))
                        {
                            if (note[Math.Max(0, index - 1)].Equals('C'))
                                continue;
                            handledStr += MirrorLeftToRight[a];
                        }
                        else
                        {
                            handledStr += a;
                        }

                        break;
                    case HandleType.UDMirror:
                        if (new[] { 'E', 'D' }.Contains(a)) //D区及E区Touch特殊处理
                        {
                            isSpTouch = true;
                            handledStr += a;
                            continue;
                        }

                        if (isSpTouch)
                        {
                            handledStr += MirrorTouchUpsideDown[a];
                            isSpTouch = false;
                            continue;
                        }

                        if (MirrorUpsideDown.ContainsKey(a))
                        {
                            if (note[Math.Max(0, index - 1)].Equals('C'))
                                continue;
                            handledStr += MirrorUpsideDown[a];
                        }
                        else
                        {
                            handledStr += a;
                        }

                        break;
                    case HandleType.HalfRotation:
                        if (Mirror180.ContainsKey(a))
                        {
                            if (note[Math.Max(0, index - 1)].Equals('C'))
                                continue;
                            handledStr += Mirror180[a];
                        }
                        else
                        {
                            handledStr += a;
                        }

                        break;
                    case HandleType.Rotation45:
                        if (Mirror45.ContainsKey(a))
                        {
                            if (note[Math.Max(0, index - 1)].Equals('C'))
                                continue;
                            handledStr += Mirror45[a];
                        }
                        else if (Mirror45special.ContainsKey(a) &&
                                 new[] { '2', '6' }.Contains(note[index - 1])) //2、6号键位"<"或">"Slide需要特殊处理
                        {
                            handledStr += Mirror45special[a];
                        }
                        else
                        {
                            handledStr += a;
                        }

                        break;
                    case HandleType.CcwRotation45:
                        if (MirrorCcw45.ContainsKey(a))
                        {
                            if (note[Math.Max(0, index - 1)].Equals('C'))
                                continue;
                            handledStr += MirrorCcw45[a];
                        }
                        else if (Mirror45special.ContainsKey(a) &&
                                 new[] { '3', '7' }.Contains(note[index - 1])) //3、7号键位"<"或">"Slide需要特殊处理
                        {
                            handledStr += Mirror45special[a];
                        }
                        else
                        {
                            handledStr += a;
                        }

                        break;
                }
            }

            if (arrayIndex++ < noteStr.Length - 1)
                handledStr += ",";
        }

        return handledStr;
    }
    //static public string NoteMirrorLeftRight(string str) //左右镜像
    //{

    //    string handledStr = "";
    //    Dictionary<char, char> MirrorLeftToRight = new Dictionary<char, char>()
    //    {
    //        { '8','1' },
    //        { '1','8' },
    //        { '2','7' },
    //        { '7','2' },
    //        { '3','6' },
    //        { '6','3' },
    //        { '4','5' },
    //        { '5','4' },
    //        { 'q','p' },
    //        { 'p','q' },
    //        { '<','>' },
    //        { '>','<' },
    //        { 'z','s' },
    //        { 's','z' }
    //    };//Note左右
    //    Dictionary<char, char> MirrorTouchLeftToRight = new Dictionary<char, char>()
    //    {
    //        { '8','2' },
    //        { '2','8' },
    //        { '3','7' },
    //        { '7','3' },
    //        { '4','6' },
    //        { '6','4' },
    //        { '1','1' },
    //        { '5','5' }
    //    };//Touch左右
    //    string[] noteStr = Regex.Replace(str, @"\s", "").Split(new string[] { "," }, StringSplitOptions.None);
    //    int arrayIndex = 0;
    //    foreach (var note in noteStr)
    //    {
    //        bool isArg = false;
    //        bool isSpTouch = false;
    //        foreach (var a in note)
    //        {
    //            var index = note.IndexOf(a);
    //            if ((new char[] { '{', '[', '(' }).Contains(a))
    //            {
    //                isArg = true;
    //                handledStr += a;
    //                continue;
    //            }
    //            else if (a.Equals('<') && index < note.Length - 1 && note[index + 1].Equals('H'))
    //            {
    //                isArg = true;
    //                handledStr += a;
    //                continue;
    //            }
    //            else if ((new char[] { '}', ']', ')', '>' }).Contains(a))
    //            {
    //                isArg = false;
    //                handledStr += a;
    //                continue;
    //            }
    //            else if (isArg)
    //            {
    //                handledStr += a;
    //                continue;
    //            }
    //            else
    //            {
    //                if ((new char[] { 'E', 'D' }).Contains(a)) //D区及E区Touch特殊处理
    //                {
    //                    isSpTouch = true;
    //                    handledStr += a;
    //                    continue;
    //                }
    //                else if (isSpTouch)
    //                {
    //                    handledStr += MirrorTouchLeftToRight[a];
    //                    isSpTouch = false;
    //                    continue;
    //                }
    //                if (MirrorLeftToRight.ContainsKey(a))
    //                    handledStr += MirrorLeftToRight[a];
    //                else
    //                    handledStr += a;
    //            }
    //        }
    //        if (arrayIndex++ < noteStr.Length - 1)
    //            handledStr += ",";
    //    }
    //    return handledStr;


    //    char[] a = str.ToCharArray();
    //    for (int i = 0; i < a.Length; i++)
    //    {
    //        string s1 = a[i].ToString();
    //        if (a[i] == '{' || a[i] == '[' || a[i] == '(')
    //        {
    //            s += s1;

    //            while (i + 1 < a.Length && a[i] != '}' && a[i] != ']' && a[i] != ')')
    //            {
    //                i += 1;
    //                s += a[i];
    //            }
    //        }
    //        else
    //        {
    //            if (MirrorLeftToRight.ContainsKey(s1))
    //            {
    //                s += MirrorLeftToRight[s1];
    //            }
    //            else if (a[i] == 'e' || a[i] == 'd' || a[i] == 'E' || a[i] == 'D')
    //            {
    //                s += a[i];
    //                i += 1;
    //                string st = a[i].ToString();
    //                if (MirrorTouchLeftToRight.ContainsKey(st))
    //                {
    //                    s += MirrorTouchLeftToRight[st];
    //                }
    //                else
    //                {
    //                    s += a[i];
    //                }
    //            }
    //            else
    //            {
    //                s += s1;
    //            }
    //        }


    //    }
    //}
    //static public string NoteMirrorUpDown(string str)//上下翻转
    //{

    //    string handledStr = "";
    //    Dictionary<char, char> MirrorUpsideDown = new Dictionary<char, char>()
    //    {
    //        { '4','1' },
    //        { '5','8' },
    //        { '6','7' },
    //        { '3','2' },
    //        { '7','6' },
    //        { '2','3' },
    //        { '8','5' },
    //        { '1','4' },
    //        { 'q','p' },
    //        { 'p','q' },
    //        { 'z','s' },
    //        { 's','z' }
    //    };//上下（全反=上下+左右）
    //    Dictionary<char, char> MirrorTouchUpsideDown = new Dictionary<char, char>()
    //    {
    //        { '4','2' },
    //        { '2','4' },
    //        { '1','5' },
    //        { '5','1' },
    //        { '8','6' },
    //        { '6','8' },
    //        { '3','3' },
    //        { '7','7' }
    //    };//Touch左右
    //    string[] noteStr = Regex.Replace(str, @"\s", "").Split(new string[] { "," }, StringSplitOptions.None);
    //    int arrayIndex = 0;
    //    foreach (var note in noteStr)
    //    {
    //        bool isArg = false;
    //        bool isSpTouch = false;
    //        foreach (var a in note)
    //        {
    //            var index = note.IndexOf(a);
    //            if ((new char[] { '{', '[', '(' }).Contains(a))
    //            {
    //                isArg = true;
    //                handledStr += a;
    //                continue;
    //            }
    //            else if (a.Equals('<') && index < note.Length - 1 && note[index + 1].Equals('H'))
    //            {
    //                isArg = true;
    //                handledStr += a;
    //                continue;
    //            }
    //            else if ((new char[] { '}', ']', ')', '>' }).Contains(a))
    //            {
    //                isArg = false;
    //                handledStr += a;
    //                continue;
    //            }
    //            else if (isArg)
    //            {
    //                handledStr += a;
    //                continue;
    //            }
    //            else
    //            {
    //                if ((new char[] { 'E', 'D' }).Contains(a)) //D区及E区Touch特殊处理
    //                {
    //                    isSpTouch = true;
    //                    handledStr += a;
    //                    continue;
    //                }
    //                else if (isSpTouch)
    //                {
    //                    handledStr += MirrorTouchUpsideDown[a];
    //                    isSpTouch = false;
    //                    continue;
    //                }
    //                if (MirrorUpsideDown.ContainsKey(a))
    //                    handledStr += MirrorUpsideDown[a];
    //                else
    //                    handledStr += a;
    //            }
    //        }
    //        if (arrayIndex++ < noteStr.Length - 1)
    //            handledStr += ",";
    //    }
    //    return handledStr;
    //    char[] a = str.ToCharArray();
    //    for (int i = 0; i < a.Length; i++)
    //    {
    //        string s1 = a[i].ToString();
    //        if (a[i] == '{' || a[i] == '[' || a[i] == '(')//跳过括号内内容
    //        {
    //            s += s1;

    //            while (i + 1 < a.Length && a[i] != '}' && a[i] != ']' && a[i] != ')')
    //            {
    //                i += 1;
    //                s += a[i];


    //            }
    //        }
    //        else
    //        {
    //            if (MirrorUpsideDown.ContainsKey(s1))
    //            {
    //                s += MirrorUpsideDown[s1];
    //            }
    //            else if (a[i] == 'e' || a[i] == 'd' || a[i] == 'E' || a[i] == 'D')
    //            {
    //                s += a[i];
    //                i += 1;
    //                string st = a[i].ToString();
    //                if (MirrorTouchUpsideDown.ContainsKey(st))
    //                {
    //                    s += MirrorTouchUpsideDown[st];
    //                }
    //            }
    //            else
    //            {
    //                s += s1;
    //            }
    //        }


    //    }
    //    Console.WriteLine(s);
    //    Console.ReadKey();
    //    return s;
    //}
    //static public string NoteMirror180(string str)//翻转180°
    //{

    //    string handledStr = "";
    //    Dictionary<char, char> Mirror180 = new Dictionary<char, char>()
    //    {
    //        {'5','1'},
    //        {'4','8'},
    //        {'3','7'},
    //        {'6','2'},
    //        {'2','6'},
    //        {'7','3'},
    //        {'1','5'},
    //        {'8','4'},
    //        {'<','>'},
    //        {'>','<'}
    //    };
    //    string[] noteStr = Regex.Replace(str, @"\s", "").Split(new string[] { "," }, StringSplitOptions.None);
    //    int arrayIndex = 0;
    //    foreach (var note in noteStr)
    //    {
    //        bool isArg = false;
    //        foreach (var a in note)
    //        {
    //            var index = note.IndexOf(a);
    //            if ((new char[] { '{', '[', '(' }).Contains(a))
    //            {
    //                isArg = true;
    //                handledStr += a;
    //                continue;
    //            }
    //            else if (a.Equals('<') && index < note.Length - 1 && note[index + 1].Equals('H'))
    //            {
    //                isArg = true;
    //                handledStr += a;
    //                continue;
    //            }
    //            else if ((new char[] { '}', ']', ')', '>' }).Contains(a))
    //            {
    //                isArg = false;
    //                handledStr += a;
    //                continue;
    //            }
    //            else if (isArg)
    //            {
    //                handledStr += a;
    //                continue;
    //            }
    //            else
    //            {
    //                if (Mirror180.ContainsKey(a))
    //                    handledStr += Mirror180[a];
    //                else
    //                    handledStr += a;
    //            }
    //        }
    //        if (arrayIndex++ < noteStr.Length - 1)
    //            handledStr += ",";
    //    }

    //    return handledStr;
    //    char[] a = str.ToCharArray();
    //    for (int i = 0; i < a.Length; i++)
    //    {
    //        string s1 = a[i].ToString();
    //        if (a[i] == '{' || a[i] == '[' || a[i] == '(')
    //        {
    //            s += s1;
    //            while (i + 1 < a.Length && a[i] != '}' && a[i] != ']' && a[i] != ')')
    //            {
    //                i += 1;
    //                s += a[i];


    //            }
    //        }
    //        else
    //        {
    //            if (Mirror180.ContainsKey(s1))
    //            {
    //                s += Mirror180[s1];
    //            }

    //            else
    //            {
    //                s += s1;
    //            }
    //        }
    //    }
    //}
    //static public string NoteMirrorSpin45(string str)//顺时针旋转45
    //{
    //    string handledStr = "";
    //    Dictionary<char, char> Mirror45 = new Dictionary<char, char>()
    //    {
    //        { '8','1' },
    //        { '7','8' },
    //        { '6','7' },
    //        { '5','6' },
    //        { '4','5' },
    //        { '3','4' },
    //        { '2','3' },
    //        { '1','2' }
    //    };
    //    Dictionary<char, char> Mirror45special = new Dictionary<char, char>()
    //    {
    //        { '<','>' },
    //        { '>','<' }
    //    };//2或6键位旋转改变<>符号
    //    string[] noteStr = Regex.Replace(str, @"\s", "").Split(new string[] { "," }, StringSplitOptions.None);
    //    int arrayIndex = 0;
    //    foreach (var note in noteStr)
    //    {
    //        bool isArg = false;
    //        foreach (var a in note)
    //        {
    //            var index = note.IndexOf(a);
    //            if ((new char[] { '{', '[', '(' }).Contains(a))
    //            {
    //                isArg = true;
    //                handledStr += a;
    //                continue;
    //            }
    //            else if (a.Equals('<') && index < note.Length - 1 && note[index + 1].Equals('H'))
    //            {
    //                isArg = true;
    //                handledStr += a;
    //                continue;
    //            }
    //            else if ((new char[] { '}', ']', ')', '>' }).Contains(a))
    //            {
    //                isArg = false;
    //                handledStr += a;
    //                continue;
    //            }
    //            else if (isArg)
    //            {
    //                handledStr += a;
    //                continue;
    //            }
    //            else
    //            {

    //                if (Mirror45.ContainsKey(a))
    //                    handledStr += Mirror45[a];
    //                else if (Mirror45special.ContainsKey(a) && (new char[] { '2', '6' }).Contains(note[index - 1]))//2、6号键位"<"或">"Slide需要特殊处理
    //                    handledStr += Mirror45special[a];
    //                else
    //                    handledStr += a;
    //            }
    //        }
    //        if (arrayIndex++ < noteStr.Length - 1)
    //            handledStr += ",";
    //    }
    //    return handledStr;

    //    char[] a = str.ToCharArray();
    //    for (int i = 0; i < a.Length; i++)
    //    {
    //        string s1 = a[i].ToString();
    //        if (a[i] == '{' || a[i] == '[' || a[i] == '(')
    //        {
    //            s += s1;

    //            while (i + 1 < a.Length && a[i] != '}' && a[i] != ']' && a[i] != ')')
    //            {
    //                i += 1;
    //                s += a[i];
    //            }
    //        }
    //        else
    //        {
    //            if (Mirror45.ContainsKey(s1))
    //            {
    //                if (i + 2 < a.Length && (a[i] == '2' || a[i] == '6') && a[i + 1] != '[')
    //                {
    //                    s += Mirror45[s1];
    //                    while (i + 1 < a.Length && a[i] != ',')
    //                    {
    //                        i += 1;
    //                        string st = a[i].ToString();
    //                        if (st == "/")
    //                        {
    //                            s += "/";
    //                            break;
    //                        }
    //                        else if (st == "[")
    //                        {
    //                            s += st;

    //                            while (i + 1 < a.Length && a[i] != '}' && a[i] != ']' && a[i] != ')')
    //                            {
    //                                i += 1;
    //                                s += a[i];


    //                            }
    //                        }
    //                        else if (Mirror45special.ContainsKey(st))
    //                        {
    //                            s += Mirror45special[st];

    //                        }
    //                        else if (Mirror45.ContainsKey(st))
    //                        {
    //                            s += Mirror45[st];
    //                        }
    //                        else
    //                        {
    //                            s += st;

    //                        }
    //                    }
    //                }
    //                else
    //                {
    //                    s += Mirror45[s1];
    //                }
    //            }

    //            else
    //            {
    //                s += s1;
    //            }
    //        }
    //    }
    //    return s;
    //}
    //static public string NoteMirrorSpinCcw45(string str)//逆时针旋转45
    //{
    //    anti - clockwise
    //    string handledStr = "";
    //    Dictionary<char, char> Mirror45 = new Dictionary<char, char>()
    //    {
    //        { '1','8' },
    //        { '2','1' },
    //        { '3','2' },
    //        { '4','3' },
    //        { '5','4' },
    //        { '6','5' },
    //        { '7','6' },
    //        { '8','7' }
    //    };
    //    Dictionary<char, char> Mirror45special = new Dictionary<char, char>()
    //    {
    //        { '<','>'},
    //        { '>','<'}
    //    };//3或7键位旋转改变<>符号
    //    string[] noteStr = Regex.Replace(str, @"\s", "").Split(new string[] { "," }, StringSplitOptions.None);
    //    int arrayIndex = 0;
    //    foreach (var note in noteStr)
    //    {
    //        bool isArg = false;
    //        foreach (var a in note)
    //        {
    //            var index = note.IndexOf(a);
    //            if ((new char[] { '{', '[', '(' }).Contains(a))
    //            {
    //                isArg = true;
    //                handledStr += a;
    //                continue;
    //            }
    //            else if (a.Equals('<') && index < note.Length - 1 && note[index + 1].Equals('H'))
    //            {
    //                isArg = true;
    //                handledStr += a;
    //                continue;
    //            }
    //            else if ((new char[] { '}', ']', ')', '>' }).Contains(a))
    //            {
    //                isArg = false;
    //                handledStr += a;
    //                continue;
    //            }
    //            else if (isArg)
    //            {
    //                handledStr += a;
    //                continue;
    //            }
    //            else
    //            {

    //                if (Mirror45.ContainsKey(a))
    //                    handledStr += Mirror45[a];
    //                else if (Mirror45special.ContainsKey(a) && (new char[] { '3', '7' }).Contains(note[index - 1]))//3、7号键位"<"或">"Slide需要特殊处理
    //                    handledStr += Mirror45special[a];
    //                else
    //                    handledStr += a;
    //            }
    //        }
    //        if (arrayIndex++ < noteStr.Length - 1)
    //            handledStr += ",";
    //    }
    //    return handledStr;

    //    char[] a = str.ToCharArray();
    //    for (int i = 0; i < a.Length; i++)
    //    {
    //        string s1 = a[i].ToString();
    //        if (a[i] == '{' || a[i] == '[' || a[i] == '(')
    //        {
    //            s += s1;

    //            while (i + 1 < a.Length && a[i] != '}' && a[i] != ']' && a[i] != ')')
    //            {
    //                i += 1;
    //                s += a[i];
    //            }
    //        }
    //        else
    //        {
    //            if (Mirror45.ContainsKey(s1))
    //            {
    //                if (i + 2 < a.Length && (a[i] == '3' || a[i] == '7') && a[i + 1] != '[')
    //                {
    //                    s += Mirror45[s1];
    //                    while (i + 1 < a.Length && a[i] != ',')
    //                    {
    //                        i += 1;
    //                        string st = a[i].ToString();
    //                        if (st == "/")
    //                        {
    //                            s += "/";
    //                            break;
    //                        }
    //                        else if (st == "[")
    //                        {
    //                            s += st;

    //                            while (i + 1 < a.Length && a[i] != '}' && a[i] != ']' && a[i] != ')')
    //                            {
    //                                i += 1;
    //                                s += a[i];


    //                            }
    //                        }
    //                        else if (Mirror45special.ContainsKey(st))
    //                        {
    //                            s += Mirror45special[st];

    //                        }
    //                        else if (Mirror45.ContainsKey(st))
    //                        {
    //                            s += Mirror45[st];
    //                        }
    //                        else
    //                        {
    //                            s += st;

    //                        }
    //                    }
    //                }
    //                else
    //                {
    //                    s += Mirror45[s1];
    //                }
    //            }

    //            else
    //            {
    //                s += s1;
    //            }
    //        }
    //    }
    //    return s;
    //}
}