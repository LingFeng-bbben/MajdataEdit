using System;
using System.Collections.Generic;

namespace MajdataEdit
{
    //小额负责的部分哟
    static class Mirror
    {
        static public string NoteMirrorLeftRight(string str) //左右镜像
        {

            string handledStr = "";
            Dictionary<char, char> MirrorLeftToRight = new Dictionary<char, char>()
            {
                { '8','1' },
                { '1','8' },
                { '2','7' },
                { '7','2' },
                { '3','6' },
                { '6','3' },
                { '4','5' },
                { '5','4' },
                { 'q','p' },
                { 'p','q' },
                { '<','>' },
                { '>','<' },
                { 'z','s' },
                { 's','z' }
            };//Note左右
            Dictionary<char, char> MirrorTouchLeftToRight = new Dictionary<char, char>()
            {
                { '8','2' },
                { '2','8' },
                { '3','7' },
                { '7','3' },
                { '4','6' },
                { '6','4' },
                { '1','1' },
                { '5','5' }
            };//Touch左右
            string[] noteStr = str.Split(new string[]{","},StringSplitOptions.RemoveEmptyEntries);
            
            foreach(var note in noteStr)
            {
                bool isArg = false;
                bool isSpTouch = false;
                foreach (var a in note)
                {
                    var index = note.IndexOf(a);
                    if (a.Equals(new char[] { '{','[','(' }))
                    {
                        isArg = true;
                        handledStr += a;
                        continue;
                    }
                    else if(a.Equals('<') && index < note.Length - 1 && note[index + 1].Equals('H'))
                    {
                        isArg = true;
                        handledStr += a;
                        continue;
                    }
                    else if (a.Equals(new char[] { '}', ']', ')','>' }))
                    {
                        isArg = false;
                        handledStr += a;
                        continue;
                    }               
                    else if(isArg)
                    {
                        handledStr += a;
                        continue;
                    }
                    else
                    {
                        if( a.Equals(new char[]{'E','D'})) //D区及E区Touch特殊处理
                        {
                            isSpTouch = true;
                            handledStr += a;
                            continue;
                        }
                        else if(isSpTouch)
                        {
                            handledStr += MirrorTouchLeftToRight[a];
                            isSpTouch = false;
                            continue;
                        }
                        handledStr += MirrorLeftToRight[a];
                    }
                }
            }
            return handledStr;
            


                //char[] a = str.ToCharArray();
                //for (int i = 0; i < a.Length; i++)
                //{
                //    string s1 = a[i].ToString();
                //    if (a[i] == '{' || a[i] == '[' || a[i] == '(')
                //    {
                //        s += s1;

                //        while (i + 1 < a.Length && a[i] != '}' && a[i] != ']' && a[i] != ')')
                //        {
                //            i += 1;
                //            s += a[i];
                //        }
                //    }
                //    else
                //    {
                //        if (MirrorLeftToRight.ContainsKey(s1))
                //        {
                //            s += MirrorLeftToRight[s1];
                //        }
                //        else if (a[i] == 'e' || a[i] == 'd' || a[i] == 'E' || a[i] == 'D')
                //        {
                //            s += a[i];
                //            i += 1;
                //            string st = a[i].ToString();
                //            if (MirrorTouchLeftToRight.ContainsKey(st))
                //            {
                //                s += MirrorTouchLeftToRight[st];
                //            }
                //            else
                //            {
                //                s += a[i];
                //            }
                //        }
                //        else
                //        {
                //            s += s1;
                //        }
                //    }


                //}
        }
        static public string NoteMirrorUpDown(string str)//上下翻转
        {

            string s = "";
            Dictionary<string, string> MirrorUpsideDown = new Dictionary<string, string>();//上下（全反=上下+左右）
            MirrorUpsideDown.Add("4", "1");
            MirrorUpsideDown.Add("5", "8");
            MirrorUpsideDown.Add("6", "7");
            MirrorUpsideDown.Add("3", "2");
            MirrorUpsideDown.Add("7", "6");
            MirrorUpsideDown.Add("2", "3");
            MirrorUpsideDown.Add("8", "5");
            MirrorUpsideDown.Add("1", "4");
            MirrorUpsideDown.Add("q", "p");
            MirrorUpsideDown.Add("p", "q");
            MirrorUpsideDown.Add("z", "s");
            MirrorUpsideDown.Add("s", "z");
            Dictionary<string, string> MirrorTouchUpsideDown = new Dictionary<string, string>();//Touch左右
            MirrorTouchUpsideDown.Add("4", "2");
            MirrorTouchUpsideDown.Add("2", "4");
            MirrorTouchUpsideDown.Add("1", "5");
            MirrorTouchUpsideDown.Add("5", "1");
            MirrorTouchUpsideDown.Add("8", "6");
            MirrorTouchUpsideDown.Add("6", "8");
            MirrorTouchUpsideDown.Add("3", "3");
            MirrorTouchUpsideDown.Add("7", "7");
            char[] a = str.ToCharArray();
            for (int i = 0; i < a.Length; i++)
            {
                string s1 = a[i].ToString();
                if (a[i] == '{' || a[i] == '[' || a[i] == '(')//跳过括号内内容
                {
                    s += s1;

                    while (i + 1 < a.Length && a[i] != '}' && a[i] != ']' && a[i] != ')')
                    {
                        i += 1;
                        s += a[i];


                    }
                }
                else
                {
                    if (MirrorUpsideDown.ContainsKey(s1))
                    {
                        s += MirrorUpsideDown[s1];
                    }
                    else if (a[i] == 'e' || a[i] == 'd' || a[i] == 'E' || a[i] == 'D')
                    {
                        s += a[i];
                        i += 1;
                        string st = a[i].ToString();
                        if (MirrorTouchUpsideDown.ContainsKey(st))
                        {
                            s += MirrorTouchUpsideDown[st];
                        }
                    }
                    else
                    {
                        s += s1;
                    }
                }


            }
            //     Console.WriteLine(s);
            //     Console.ReadKey();
            return s;
        }
        static public string NoteMirror180(string str)//翻转180°
        {

            string s = "";
            Dictionary<string, string> Mirror180 = new Dictionary<string, string>();
            Mirror180.Add("5", "1");
            Mirror180.Add("4", "8");
            Mirror180.Add("3", "7");
            Mirror180.Add("6", "2");
            Mirror180.Add("2", "6");
            Mirror180.Add("7", "3");
            Mirror180.Add("1", "5");
            Mirror180.Add("8", "4");
            Mirror180.Add("<", ">");
            Mirror180.Add(">", "<");
            char[] a = str.ToCharArray();
            for (int i = 0; i < a.Length; i++)
            {
                string s1 = a[i].ToString();
                if (a[i] == '{' || a[i] == '[' || a[i] == '(')
                {
                    s += s1;

                    while (i + 1 < a.Length && a[i] != '}' && a[i] != ']' && a[i] != ')')
                    {
                        i += 1;
                        s += a[i];


                    }
                }
                else
                {
                    if (Mirror180.ContainsKey(s1))
                    {
                        s += Mirror180[s1];
                    }

                    else
                    {
                        s += s1;
                    }
                }


            }
            return s;
        }
        static public string NoteMirrorSpin45(string str)
        {
            string s = "";
            Dictionary<string, string> Mirror45 = new Dictionary<string, string>();
            Mirror45.Add("8", "1");
            Mirror45.Add("7", "8");
            Mirror45.Add("6", "7");
            Mirror45.Add("5", "6");
            Mirror45.Add("4", "5");
            Mirror45.Add("3", "4");
            Mirror45.Add("2", "3");
            Mirror45.Add("1", "2");
            Dictionary<string, string> Mirror45special = new Dictionary<string, string>();//2或6键位旋转改变<>符号
            Mirror45special.Add("<", ">");
            Mirror45special.Add(">", "<");
            char[] a = str.ToCharArray();
            for (int i = 0; i < a.Length; i++)
            {
                string s1 = a[i].ToString();
                if (a[i] == '{' || a[i] == '[' || a[i] == '(')
                {
                    s += s1;

                    while (i + 1 < a.Length && a[i] != '}' && a[i] != ']' && a[i] != ')')
                    {
                        i += 1;
                        s += a[i];
                    }
                }
                else
                {
                    if (Mirror45.ContainsKey(s1))
                    {
                        if (i + 2 < a.Length && (a[i] == '2' || a[i] == '6')&& a[i+1] != '[')
                        {
                            s += Mirror45[s1];
                            while (i + 1 < a.Length && a[i] != ',')
                            {
                                i += 1;
                                string st = a[i].ToString();
                                if (st == "/")
                                {
                                    s += "/";
                                    break;
                                }
                                else if (st == "[")
                                {
                                    s += st;

                                    while (i + 1 < a.Length && a[i] != '}' && a[i] != ']' && a[i] != ')')
                                    {
                                        i += 1;
                                        s += a[i];


                                    }
                                }
                                else if (Mirror45special.ContainsKey(st))
                                {
                                    s += Mirror45special[st];

                                }
                                else if (Mirror45.ContainsKey(st))
                                {
                                    s += Mirror45[st];
                                }
                                else
                                {
                                    s += st;

                                }
                            }
                        }
                        else
                        {
                            s += Mirror45[s1];
                        }
                    }

                    else
                    {
                        s += s1;
                    }
                }
            }
            return s;
        }
        static public string NoteMirrorSpinCcw45(string str)
        {
            // anti-clockwise
            string s = "";
            Dictionary<string, string> Mirror45 = new Dictionary<string, string>();
            Mirror45.Add("1", "8");
            Mirror45.Add("2", "1");
            Mirror45.Add("3", "2");
            Mirror45.Add("4", "3");
            Mirror45.Add("5", "4");
            Mirror45.Add("6", "5");
            Mirror45.Add("7", "6");
            Mirror45.Add("8", "7");
            Dictionary<string, string> Mirror45special = new Dictionary<string, string>();//3或7键位旋转改变<>符号
            Mirror45special.Add("<", ">");
            Mirror45special.Add(">", "<");
            char[] a = str.ToCharArray();
            for (int i = 0; i < a.Length; i++)
            {
                string s1 = a[i].ToString();
                if (a[i] == '{' || a[i] == '[' || a[i] == '(')
                {
                    s += s1;

                    while (i + 1 < a.Length && a[i] != '}' && a[i] != ']' && a[i] != ')')
                    {
                        i += 1;
                        s += a[i];
                    }
                }
                else
                {
                    if (Mirror45.ContainsKey(s1))
                    {
                        if (i + 2 < a.Length && (a[i] == '3' || a[i] == '7') && a[i + 1] != '[')
                        {
                            s += Mirror45[s1];
                            while (i + 1 < a.Length && a[i] != ',')
                            {
                                i += 1;
                                string st = a[i].ToString();
                                if (st == "/")
                                {
                                    s += "/";
                                    break;
                                }
                                else if (st == "[")
                                {
                                    s += st;

                                    while (i + 1 < a.Length && a[i] != '}' && a[i] != ']' && a[i] != ')')
                                    {
                                        i += 1;
                                        s += a[i];


                                    }
                                }
                                else if (Mirror45special.ContainsKey(st))
                                {
                                    s += Mirror45special[st];

                                }
                                else if (Mirror45.ContainsKey(st))
                                {
                                    s += Mirror45[st];
                                }
                                else
                                {
                                    s += st;

                                }
                            }
                        }
                        else
                        {
                            s += Mirror45[s1];
                        }
                    }

                    else
                    {
                        s += s1;
                    }
                }
            }
            return s;
        }
    }
}
