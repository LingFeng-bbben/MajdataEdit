using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MajdataEdit
{
    //小额负责的部分哟
    static class Mirror
    {
        static public string NoteMirrorLeftRight(string str)
        {

            string s = "";
            Dictionary<string, string> MirrorLeftToRight = new Dictionary<string, string>();//左右
            MirrorLeftToRight.Add("8", "1");
            MirrorLeftToRight.Add("1", "8");
            MirrorLeftToRight.Add("2", "7");
            MirrorLeftToRight.Add("7", "2");
            MirrorLeftToRight.Add("3", "6");
            MirrorLeftToRight.Add("6", "3");
            MirrorLeftToRight.Add("4", "5");
            MirrorLeftToRight.Add("5", "4");
            MirrorLeftToRight.Add("q", "p");
            MirrorLeftToRight.Add("p", "q");
            MirrorLeftToRight.Add("<", ">");
            MirrorLeftToRight.Add(">", "<");
            MirrorLeftToRight.Add("z", "s");
            MirrorLeftToRight.Add("s", "z");
            Dictionary<string, string> MirrorTouchLeftToRight = new Dictionary<string, string>();//Touch左右
            MirrorTouchLeftToRight.Add("8", "2");
            MirrorTouchLeftToRight.Add("2", "8");
            MirrorTouchLeftToRight.Add("3", "7");
            MirrorTouchLeftToRight.Add("7", "3");
            MirrorTouchLeftToRight.Add("4", "6");
            MirrorTouchLeftToRight.Add("6", "4");
            MirrorTouchLeftToRight.Add("1", "1");
            MirrorTouchLeftToRight.Add("5", "5");
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
                    if (MirrorLeftToRight.ContainsKey(s1))
                    {
                        s += MirrorLeftToRight[s1];
                    }
                    else if (a[i] == 'e' || a[i] == 'd' || a[i] == 'E' || a[i] == 'D')
                    {
                        s += a[i];
                        i += 1;
                        string st = a[i].ToString();
                        if (MirrorTouchLeftToRight.ContainsKey(st))
                        {
                            s += MirrorTouchLeftToRight[st];
                        }
                        else
                        {
                            s += a[i];
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
        static public string NoteMirrorUpDown(string str)
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
    }
}
