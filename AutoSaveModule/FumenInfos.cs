/*
  Copyright (c) Moying-moe All rights reserved. Licensed under the MIT license.
  See LICENSE in the project root for license information.
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MajdataEdit.AutoSaveModule
{
    /// <summary>
    /// 谱面信息
    /// </summary>
    public class FumenInfos
    {
        public string Title = "";
        public string Artist = "";
        public string Designer = "";
        public string OtherCommands = "";
        public float First = 0f;
        public string[] Levels = new string[7];
        public string[] Fumens = new string[7];

        public FumenInfos() { }

        public FumenInfos(string title, string artist, string designer, string otherCommands, float first, string[] levels, string[] fumens)
        {
            Title = title;
            Artist = artist;
            Designer = designer;
            OtherCommands = otherCommands;
            First = first;
            Levels = levels;
            Fumens = fumens;
        }

        /// <summary>
        /// 从path中读取谱面信息
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static FumenInfos FromFile(string path)
        {
            string title = "";
            string artist = "";
            string designer = "";
            string other_commands = "";
            float first = 0f;
            string[] levels = new string[7];
            string[] fumens = new string[7];

            string[] maidataTxt = File.ReadAllLines(path, Encoding.UTF8);

            for (int i = 0; i < maidataTxt.Length; i++)
            {
                if (maidataTxt[i].StartsWith("&title="))
                {
                    title = GetValue(maidataTxt[i]);
                }
                else if (maidataTxt[i].StartsWith("&artist="))
                {
                    artist = GetValue(maidataTxt[i]);
                }
                else if (maidataTxt[i].StartsWith("&des="))
                {
                    designer = GetValue(maidataTxt[i]);
                }
                else if (maidataTxt[i].StartsWith("&first="))
                {
                    first = float.Parse(GetValue(maidataTxt[i]));
                }
                else if (maidataTxt[i].StartsWith("&lv_") || maidataTxt[i].StartsWith("&inote_"))
                {
                    for (int j = 1; j < 8 && i < maidataTxt.Length; j++)
                    {
                        if (maidataTxt[i].StartsWith("&lv_" + j + "="))
                            levels[j - 1] = GetValue(maidataTxt[i]);
                        if (maidataTxt[i].StartsWith("&inote_" + j + "="))
                        {
                            string TheNote = "";
                            TheNote += GetValue(maidataTxt[i]) + "\n";
                            i++;
                            for (; i < maidataTxt.Length; i++)
                            {
                                if ((i) < maidataTxt.Length)
                                {
                                    if (maidataTxt[i].StartsWith("&"))
                                        break;
                                }
                                TheNote += maidataTxt[i] + "\n";
                            }
                            fumens[j - 1] = TheNote;
                        }
                    }
                }
                else
                {
                    other_commands += maidataTxt[i].Trim() + "\n";
                }

            }
            other_commands = other_commands.Trim();

            return new FumenInfos(title, artist, designer, other_commands, first, levels, fumens);
        }

        private static string GetValue(string varline)
        {
            return varline.Split('=')[1];
        }
    }
}
