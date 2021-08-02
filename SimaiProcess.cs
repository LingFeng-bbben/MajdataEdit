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

        static public void ClearData()
        {
            title = "";
            artist = "";
            designer = "";
            first = 0;
            fumens = new string[7];
            levels = new string[7];
            simaiFirst = false;
            notelist = new List<SimaiTimingPoint>(); 
            timinglist = new List<SimaiTimingPoint>();
        }

        static public bool ReadData(string filename)
        {
            int i = 0;
            try
            {
                string[] maidataTxt = File.ReadAllLines(filename, Encoding.UTF8);
                for (i = 0; i < maidataTxt.Length; i++)
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
                if (first != 0 && first != -0.04f)
                {
                    MessageBox.Show("本编辑器不想支持offset,请剪好了再来");
                }
                Console.WriteLine(fumens[5]);
                return true;
            }
            catch (Exception e){
                MessageBox.Show("在maidata.txt第"+(i+1)+"行:\n"+e.Message, "读取谱面时出现错误");
                return false;
            }
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
            List<SimaiTimingPoint> _notelist = new List<SimaiTimingPoint>();
            List<SimaiTimingPoint> _timinglist = new List<SimaiTimingPoint>();
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
                    if (isNote(text[i]))//if has number (not for touch note now)
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
                            _notelist.Add(new SimaiTimingPoint(time, Xcount, Ycount,noteTemp,bpm));
                            //Console.WriteLine("Note:" + noteTemp);
                            
                            noteTemp = "";
                        }
                        _timinglist.Add(new SimaiTimingPoint(time,Xcount,Ycount,"",bpm));


                        time += (1d / (bpm / 60d)) * 4d / (double)beats;
                        //Console.WriteLine(time);
                        haveNote = false;
                        continue;
                    }
                }
                notelist = _notelist;
                timinglist = _timinglist;
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
        static private bool isNote(char noteText)
        {
            string SlideMarks = "1234567890ABCDE"; ///ABCDE for touch
            foreach (var mark in SlideMarks)
            {
                if (noteText==mark) return true;
            }
            return false;
        }
        static public string GetDifficultyText(int index)
        {
            if (index == 0) return "EASY";
            if (index == 1) return "BASIC";
            if (index == 2) return "ADVANCED";
            if (index == 3) return "EXPERT";
            if (index == 4) return "MASTER";
            if (index == 5) return "Re:MASTER";
            if (index == 6) return "ORIGINAL";
            return "DEFAULT";
        }
    }

    class SimaiTimingPoint
    {
        public double time;
        public bool havePlayed;
        public int rawTextPositionX;
        public int rawTextPositionY;
        public string notesContent;
        public float currentBpm = -1;
        public List<SimaiNote> noteList = new List<SimaiNote>(); //used for json
        public SimaiTimingPoint(double _time, int textposX = 0, int textposY = 0,string _content = "",float bpm=0f)
        {
            time = _time;
            rawTextPositionX = textposX;
            rawTextPositionY = textposY;
            notesContent = _content.Replace("\n","").Replace(" ","");
            currentBpm = bpm;
        }

        public List<SimaiNote> getNotes()
        {
            
            List<SimaiNote> simaiNotes = new List<SimaiNote>();
            if (notesContent == "") return simaiNotes;
            try
            {
                int dummy = 0;
                if (notesContent.Length == 2 && int.TryParse(notesContent, out dummy))//连写数字
                {
                    simaiNotes.Add(getSingleNote(notesContent[0].ToString()));
                    simaiNotes.Add(getSingleNote(notesContent[1].ToString()));
                    return simaiNotes;
                }
                if (notesContent.Contains('/'))
                {
                    var notes = notesContent.Split('/');
                    foreach (var note in notes)
                    {
                        if (note.Contains('*'))
                        {
                            simaiNotes.AddRange(getSameHeadSlide(note));
                        }
                        else
                        {
                            simaiNotes.Add(getSingleNote(note));
                        }
                    }
                    return simaiNotes;
                }   
                if (notesContent.Contains('*'))
                {
                    simaiNotes.AddRange(getSameHeadSlide(notesContent));
                    return simaiNotes;
                }
                simaiNotes.Add(getSingleNote(notesContent));
                return simaiNotes;
            }
            catch
            {
                return new List<SimaiNote>();
            }
        }
        
        private List<SimaiNote> getSameHeadSlide(string content)
        {
            List<SimaiNote> simaiNotes = new List<SimaiNote>();
            var notes1 = content.Split('*');
            var note1 = getSingleNote(notes1[0]);
            simaiNotes.Add(note1);
            var newnotlist = notes1.ToList();
            newnotlist.RemoveAt(0);
            //删除第一个NOTE
            foreach (var item in newnotlist)
            {
                var note2text = note1.startPosition + item;
                var note2 = getSingleNote(note2text);
                simaiNotes.Add(note2);
            }
            return simaiNotes;
        }

        private SimaiNote getSingleNote(string noteText)
        {
            SimaiNote simaiNote = new SimaiNote();

            if (isTouchNote(noteText))
            {
                simaiNote.touchArea = noteText[0];
                if (simaiNote.touchArea != 'C') simaiNote.startPosition = int.Parse(noteText[1].ToString());
                else simaiNote.startPosition = 8;
                simaiNote.noteType = SimaiNoteType.Touch;
            }
            else
            {
                simaiNote.startPosition = int.Parse(noteText[0].ToString());
                simaiNote.noteType = SimaiNoteType.Tap; //if nothing happen in following if
            }
            if (noteText.Contains('f'))
            {
                simaiNote.isHanabi= true;
            }

            //hold
            if (noteText.Contains('h')) {
                if (isTouchNote(noteText)) {
                    simaiNote.noteType = SimaiNoteType.TouchHold;
                    simaiNote.holdTime = getTimeFromBeats(noteText);
                    Console.WriteLine("Hold:" +simaiNote.touchArea+ simaiNote.startPosition + " TimeLastFor:" + simaiNote.holdTime);
                }
                else
                {
                    simaiNote.noteType = SimaiNoteType.Hold;
                    simaiNote.holdTime = getTimeFromBeats(noteText);
                    Console.WriteLine("Hold:" + simaiNote.startPosition + " TimeLastFor:" + simaiNote.holdTime);
                }
            }
            //slide
            if (isSlideNote(noteText)) {
                simaiNote.noteType = SimaiNoteType.Slide;
                simaiNote.slideTime = getTimeFromBeats(noteText);
                var timeStarWait = getStarWaitTime(noteText);
                simaiNote.slideStartTime = time + timeStarWait;
                Console.WriteLine("Slide:" + simaiNote.startPosition + " TimeLastFor:" + simaiNote.slideTime);
            }
            //break
            if (noteText.Contains('b'))
            {
                simaiNote.isBreak = true;
                noteText = noteText.Replace("b", "");
            }
            //EX
            if (noteText.Contains('x'))
            {
                simaiNote.isEx = true;
                noteText = noteText.Replace("x", "");
            }
            simaiNote.noteContent = noteText;
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
        private bool isTouchNote(string noteText)
        {
            string SlideMarks = "ABCDE";
            foreach (var mark in SlideMarks)
            {
                if (noteText.StartsWith(mark.ToString())) return true;
            }
            return false;
        }

        private double getTimeFromBeats(string noteText)
        {
            var startIndex = noteText.IndexOf('[');
            var overIndex = noteText.IndexOf(']');
            var innerString = noteText.Substring(startIndex + 1, overIndex - startIndex-1);
            var timeOneBeat = 1d / (currentBpm / 60d);
            if (innerString.Count(o => o == '#') == 1)
            {
                var times = innerString.Split('#');
                if (times[1].Contains(':'))
                {
                    innerString = times[1];
                    timeOneBeat = 1d / (double.Parse(times[0]) / 60d);
                }
                else
                {
                    return double.Parse(times[1]);
                }
            }
            if (innerString.Count(o => o == '#') == 2)
            {
                var times = innerString.Split('#');
                return double.Parse(times[2]);
            }
            var numbers = innerString.Split(':');   //TODO:customBPM
            var divide = int.Parse(numbers[0]);
            var count = int.Parse(numbers[1]);
            

            return (timeOneBeat*4d / (double)divide) * (double)count; 
        }

        private double getStarWaitTime(string noteText)
        {
            var startIndex = noteText.IndexOf('[');
            var overIndex = noteText.IndexOf(']');
            var innerString = noteText.Substring(startIndex + 1, overIndex - startIndex - 1);
            double bpm = currentBpm ;
            if (innerString.Count(o => o == '#') == 1)
            {
                var times = innerString.Split('#');
                bpm = double.Parse(times[0]);
            }
            if (innerString.Count(o => o == '#') == 2)
            {
                var times = innerString.Split('#');
                return double.Parse(times[0]);
            }
            return 1d / (bpm / 60d);
        }
    }
    enum SimaiNoteType
    {
        Tap,Slide,Hold,Touch,TouchHold
    }
    class SimaiNote
    {
        public SimaiNoteType noteType;
        public bool isBreak = false;
        public bool isHanabi = false;
        public bool isEx = false;
        public int startPosition = 1; //键位（1-8）
        public char touchArea = ' ';
        public double holdTime = 0d;
        public double slideStartTime = 0d;
        public double slideTime = 0d;
        public string noteContent; //used for star explain
    }

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

                    while (i+1<a.Length && a[i] != '}' && a[i] != ']' && a[i] != ')')
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
    }
}
