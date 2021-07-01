using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace MajdataEdit
{
    static class SimaiProcess
    {
        static public string title;
        static public string artist;
        static public string designer;
        static public float first = 0;
        static public string[] fumens = new string[7];
        static public string[] levels = new string[7];
        static public List<SimaiNote> notelist = new List<SimaiNote>();
        static public void ReadData(string filename)
        {
            string[] maidataTxt = File.ReadAllLines(filename);
            for (int i = 0; i < maidataTxt.Length; i++)
            {
                if (maidataTxt[i].StartsWith("&title="))
                    title = GetValue(maidataTxt[i]);
                if (maidataTxt[i].StartsWith("&artist="))
                    artist = GetValue(maidataTxt[i]);
                if (maidataTxt[i].StartsWith("&des="))
                    designer = GetValue(maidataTxt[i]);
                if (maidataTxt[i].StartsWith("&first="))
                    first = float.Parse(GetValue(maidataTxt[i]));
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
                            TheNote += maidataTxt[i] + "\n";
                            if ((i + 1) < maidataTxt.Length)
                            {
                                if (maidataTxt[i + 1].StartsWith("&"))
                                    break;
                            }
                        }
                        fumens[j - 1] = TheNote;
                    }
                }

            }
            if (first != 0)
            {
                MessageBox.Show("本编辑器不想支持offset,请剪好了再来");
            }
            Console.WriteLine(fumens[5]);
        }
        static public void SaveData(string filename)
        {
            List<string> maidata = new List<string>();
            maidata.Add("&title=" + title);
            maidata.Add("&artist=" + artist);
            maidata.Add("&first=0");
            for (int i = 0; i < levels.Length; i++)
            {
                if (levels[i] != null && levels[i] != "")
                {
                    maidata.Add("&lv_" + i + "=" + levels[i]);
                }
            }
            for (int i = 0; i < fumens.Length; i++)
            {
                if (fumens[i] != null && fumens[i] != "")
                {
                    maidata.Add("&inote_" + i + "=\n" + fumens[i]);
                }
            }
            File.WriteAllLines(filename, maidata.ToArray());
        }
        static private string GetValue(string varline)
        {
            return varline.Split('=')[1];
        }
        static public double getSongTimeAndScan(string text, long position)
        {
            notelist.Clear();
            try
            {
                int bpm = 0;
                double time = 0; //in seconds
                double requestedTime = 0;
                int beats = 4;
                bool haveNote = false;
                for (int i = 0; i < text.Length; i++)
                {
                    if (i-1 < position)
                    {
                        requestedTime = time;
                    }
                    int dummy;
                    if (int.TryParse(text[i].ToString(),out dummy))//if has number
                    {
                        haveNote = true;
                    }
                    if (text[i] == '(')
                    //Get bpm
                    {
                        string bpm_s = "";
                        i++;
                        while (text[i] != ')')
                        {
                            bpm_s += text[i];
                            i++;
                        }
                        bpm = int.Parse(bpm_s);
                        Console.WriteLine("BPM" + bpm);
                        continue;
                    }
                    if (text[i] == '{')
                    //Get beats
                    {
                        string beats_s = "";
                        i++;
                        while (text[i] != '}')
                        {
                            beats_s += text[i];
                            i++;
                        }
                        beats = int.Parse(beats_s);
                        Console.WriteLine("BEAT" + beats);
                        continue;
                    }
                    if (text[i] == ',')
                    {
                        if (haveNote)
                        {
                            notelist.Add(new SimaiNote(time));
                        }
                        time += (1d / (bpm / 60d)) * 4d / (double)beats;
                        Console.WriteLine(time);
                        haveNote = false;
                        continue;
                    }
                }
                Console.WriteLine(notelist.ToArray());
                return requestedTime;
            }
            catch
            {
                return 0;
            }
        }
        static public void ClearNoteListPlayedState()
        {
            notelist.Sort((x, y) => x.time.CompareTo(y.time));
            for (int i = 0; i < notelist.Count; i++)
            {
                notelist[i].havePlayed = false;
            }
        }
    }

    class SimaiNote
    {
        public double time;
        public bool havePlayed;
        public SimaiNote(double _time)
        {
            time = _time;
        }
        //TODO: add some type here
    }
}
