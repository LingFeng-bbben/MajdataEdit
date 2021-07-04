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
        static public bool simaiFirst = false;
        static public List<SimaiTimingPoint> notelist = new List<SimaiTimingPoint>();
        static public List<SimaiTimingPoint> timinglist = new List<SimaiTimingPoint>();
        static public void ReadData(string filename)
        {
            string[] maidataTxt = File.ReadAllLines(filename, Encoding.UTF8);
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
            Console.WriteLine(first);
            if (first == -0.04f)
            {
                simaiFirst = true;
            }
            if (first != 0 && first!=-0.04f)
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
            if (simaiFirst)
            {
                maidata.Add("&first=-0.04");
            }
            else
            {
                maidata.Add("&first=0");
            }
            for (int i = 0; i < levels.Length; i++)
            {
                if (levels[i] != null && levels[i] != "")
                {
                    maidata.Add("&lv_" + (i + 1) + "=" + levels[i]);
                }
            }
            for (int i = 0; i < fumens.Length; i++)
            {
                if (fumens[i] != null && fumens[i] != "")
                {
                    maidata.Add("&inote_" + (i+1) + "=" + fumens[i]);
                }
            }
            File.WriteAllLines(filename, maidata.ToArray(),Encoding.UTF8);
        }
        static private string GetValue(string varline)
        {
            return varline.Split('=')[1];
        }
        static public double getSongTimeAndScan(string text, long position)
        {
            notelist.Clear();
            timinglist.Clear();
            try
            {
                float bpm = 0;
                double time = 0; //in seconds
                double requestedTime = 0;
                int beats = 4;
                bool haveNote = false;
                string noteTemp = "";
                int Ycount=0, Xcount = 0;

                for (int i = 0; i < text.Length; i++)
                {
                    if (text[i] == '\n')
                    {
                        Ycount++;
                        Xcount = 0;
                    }
                    else
                    {
                        Xcount++;
                    }
                    if (i-1 < position)
                    {
                        requestedTime = time;
                    }
                    if (text[i] == '(')
                    //Get bpm
                    {
                        string bpm_s = "";
                        i++;
                        Xcount++;
                        while (text[i] != ')')
                        {
                            bpm_s += text[i];
                            i++;
                            Xcount++;
                        }
                        bpm = float.Parse(bpm_s);
                        //Console.WriteLine("BPM" + bpm);
                        continue;
                    }
                    if (text[i] == '{')
                    //Get beats
                    {
                        string beats_s = "";
                        i++;
                        Xcount++;
                        while (text[i] != '}')
                        {
                            beats_s += text[i];
                            i++;
                            Xcount++;
                        }
                        beats = int.Parse(beats_s);
                        //Console.WriteLine("BEAT" + beats);
                        continue;
                    }
                    int dummy;
                    if (int.TryParse(text[i].ToString(), out dummy))//if has number (not for touch note now)
                    {
                        haveNote = true;
                    }
                    if (haveNote&& text[i] != ',')
                    {
                        noteTemp += text[i];
                    }
                    if (text[i] == ',')
                    {
                        if (haveNote)
                        {
                            notelist.Add(new SimaiTimingPoint(time, Xcount, Ycount,noteTemp,bpm));
                            //Console.WriteLine("Note:" + noteTemp);
                            
                            noteTemp = "";
                        }
                        timinglist.Add(new SimaiTimingPoint(time,Xcount,Ycount));


                        time += (1d / (bpm / 60d)) * 4d / (double)beats;
                        //Console.WriteLine(time);
                        haveNote = false;
                        continue;
                    }
                }
                //Console.WriteLine(notelist.ToArray());
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

    class SimaiTimingPoint
    {
        public double time;
        public bool havePlayed;
        public int rawTextPositionX;
        public int rawTextPositionY;
        public string noteContent;
        public float currentBpm;
        public SimaiTimingPoint(double _time, int textposX = 0, int textposY = 0,string _content = "",float bpm=0f)
        {
            time = _time;
            rawTextPositionX = textposX;
            rawTextPositionY = textposY;
            noteContent = _content;
            currentBpm = bpm;
        }

        public List<SimaiNote> getNotes()
        {
            if (noteContent == "") return null;
            List<SimaiNote> simaiNotes = new List<SimaiNote>();
            int dummy = 0;
            if(noteContent.Length==2&&int.TryParse(noteContent,out dummy))
            {
                simaiNotes.Add(getSingleNote(noteContent[0].ToString()));
                simaiNotes.Add(getSingleNote(noteContent[1].ToString()));
            }
            if (noteContent.Contains('/'))
            {
                var notes = noteContent.Split('/');
                foreach(var note in notes)
                {
                    simaiNotes.Add(getSingleNote(note));
                }
            }
            else
            {
                simaiNotes.Add(getSingleNote(noteContent));
            }
            return simaiNotes;
        }
        
        private SimaiNote getSingleNote(string noteText)
        {
            SimaiNote simaiNote = new SimaiNote();

            simaiNote.startPosition = int.Parse(noteText[0].ToString());
            simaiNote.noteType = SimaiNoteType.Tap; //if nothing happen in following if


            //hold
            if (noteText.Contains('h')) {
                simaiNote.noteType = SimaiNoteType.Hold;
                simaiNote.holdTime = getTimeFromBeats(noteText);
                Console.WriteLine("Hold:" + simaiNote.startPosition + " TimeLastFor:" + simaiNote.holdTime);
            }
            if (noteText.Contains('b'))
            {
                simaiNote.isBreak = true;
            }
            if (isSlideNote(noteText)) {
                simaiNote.noteType = SimaiNoteType.Slide;
                simaiNote.slideTime = getTimeFromBeats(noteText);
                var timeOneBeat = 1d / (currentBpm / 60d);
                simaiNote.slideStartTime = time + timeOneBeat;
                Console.WriteLine("Slide:" + simaiNote.startPosition + " TimeLastFor:" + simaiNote.slideTime);
            } 


            return simaiNote;
        }

        private bool isSlideNote(string noteText)
        {
            string SlideMarks = "-^v<>Vpqszw";
            foreach(var mark in SlideMarks)
            {
                if (noteText.Contains(mark)) return true;
            }
            return false;
        }

        private double getTimeFromBeats(string noteText)
        {
            var startIndex = noteContent.IndexOf('[');
            var overIndex = noteContent.IndexOf(']');
            var innerString = noteContent.Substring(startIndex + 1, overIndex - startIndex-1);
            var numbers = innerString.Split(':');   //TODO:customBPM
            var divide = int.Parse(numbers[0]);
            var count = int.Parse(numbers[1]);
            var timeOneBeat = 1d / (currentBpm / 60d);

            return (timeOneBeat*4d / (double)divide) * (double)count; 
        }
    }
    enum SimaiNoteType
    {
        Tap,Slide,Hold
    }
    class SimaiNote
    {
        public SimaiNoteType noteType;
        public bool isBreak = false;
        //bool isExnote = false;
        public int startPosition = 1; //键位（1-8）
        public double holdTime = 0d;
        public double slideStartTime = 0d;
        public double slideTime = 0d;
        //TODO: 增加描述星星形状的类
    }
}
