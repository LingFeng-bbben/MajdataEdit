using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace MajdataEdit.SyntaxModule
{
    internal static class SyntaxCheck
    {
        static List<SimaiTimingPoint> noteList;
        //static 
        static void Scan(string noteStr)
        {
            int line = 0;
            int column = 0;
        }
        static void Scan()
        {
            noteList = SimaiProcess.notelist;


        }

        /// <summary>
        /// 判断是否为Tap
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        static bool IsTap(string s)
        {
            if (s.Length == 1)
                return int.TryParse(s, out int i) && KeyIndexCheck(i);
            else if(s.Length == 2)
            {
                if (s[1] is 'b' or 'x')
                    return int.TryParse(s[0..0], out int i) && KeyIndexCheck(i);
                else
                    return int.TryParse(s, out int i) && (KeyIndexCheck(i % 10) && KeyIndexCheck(i / 10));
            }

            return false;
        }
        /// <summary>
        /// 判断是否为Hold，不检查Hold参数
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        static bool IsHold(string s)
        {
            if (s.Length >= 2)
            {
                var isValidity = int.TryParse(s[0..0], out int i) ? KeyIndexCheck(i) : false ;

                if (s[1] == 'h')
                    return isValidity;
                else if(s[2] is 'x' or 'b' && s[1] is 'h')
                    return isValidity;
                else if (s[0..1] == "Ch")
                    return true;
            }           

            return false;
        }
        /// <summary>
        /// 判断是否为Slide，不检查Slide参数
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        static bool IsSlide(string s)
        {
            if(s.Length >= 3)
            {
                var isBreakStar = s[1] is 'b';
                var isHanabiStar = s[1] is 'x';
                string cStr;

                if (isBreakStar || isHanabiStar)
                    cStr = int.TryParse(s[3..3], out int i) ? s[2..2] : s[2..3];
                else
                    cStr = int.TryParse(s[2..2], out int i) ? s[1..1] : s[1..2];

                switch (cStr)
                {
                    case "qq":
                    case "pp":
                    case "q":
                    case "p":
                    case "w":
                    case "z":
                    case "s":
                    case "V":
                    case "v":
                    case "<":
                    case ">":
                    case "^":
                    case "-":
                        return true;
                }
            }
            return false;
        }
        /// <summary>
        /// 判断是否为Touch
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        static bool IsTouch(string s)
        {
            try
            {
                if (s.Length == 2)
                {
                    var isValidity = int.TryParse(s[1..1], out int i) ? KeyIndexCheck(i) : false;
                    switch (s[0])
                    {
                        case 'A':
                        case 'B':
                        case 'C':
                        case 'D':
                        case 'E':
                            return isValidity;
                    }
                }

                return false;
            }
            catch
            {
                return false;
            }
        }
        /// <summary>
        /// 用于判断键位是否合法
        /// </summary>
        /// <param name="k"></param>
        /// <returns></returns>
        static bool KeyIndexCheck(int k) => k >= 1 && k <= 8;

    }
}
