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
        static public string other_commands;
        static public float first = 0;
        static public string[] fumens = new string[7];
        static public string[] levels = new string[7];
        /// <summary>
        /// the timing points that contains notedata
        /// </summary>
        static public List<SimaiTimingPoint> notelist = new List<SimaiTimingPoint>(); 
        /// <summary>
        /// the timing points made by "," in maidata
        /// </summary>
        static public List<SimaiTimingPoint> timinglist = new List<SimaiTimingPoint>();
        /// <summary>
        /// Reset all the data in the static class.
        /// </summary>
        static public void ClearData()
        {
            title = "";
            artist = "";
            designer = "";
            first = 0;
            fumens = new string[7];
            levels = new string[7];
            notelist = new List<SimaiTimingPoint>(); 
            timinglist = new List<SimaiTimingPoint>();
        }
        /// <summary>
        /// Read the maidata.txt into the static class, including the variables. Show up a messageBox when enconter any exception.
        /// </summary>
        /// <param name="filename">file path of maidata.txt</param>
        /// <returns>if the read process faced any error</returns>
        static public bool ReadData(string filename)
        {
            int i = 0;
            other_commands = "";
            try
            {
                string[] maidataTxt = File.ReadAllLines(filename, Encoding.UTF8);
                for (i = 0; i < maidataTxt.Length; i++)
                {
                    if (maidataTxt[i].StartsWith("&title="))
                        title = GetValue(maidataTxt[i]);
                    else if (maidataTxt[i].StartsWith("&artist="))
                        artist = GetValue(maidataTxt[i]);
                    else if (maidataTxt[i].StartsWith("&des="))
                        designer = GetValue(maidataTxt[i]);
                    else if (maidataTxt[i].StartsWith("&first="))
                        first = float.Parse(GetValue(maidataTxt[i]));
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
                return true;
            }
            catch (Exception e){
                MessageBox.Show("在maidata.txt第"+(i+1)+"行:\n"+e.Message, "读取谱面时出现错误");
                return false;
            }
        }
        /// <summary>
        /// Save the static data to maidata.txt
        /// </summary>
        /// <param name="filename">file path of maidata.txt</param>
        static public void SaveData(string filename)
        {
            List<string> maidata = new List<string>();
            maidata.Add("&title=" + title);
            maidata.Add("&artist=" + artist);
            maidata.Add("&first=" + first);
            maidata.Add("&des=" + designer);
            maidata.Add(other_commands);
            for (int i = 0; i < levels.Length; i++)
            {
                if (levels[i] != null && levels[i] != "")
                {
                    maidata.Add("&lv_" + (i + 1) + "=" + levels[i].Trim());
                }
            }
            for (int i = 0; i < fumens.Length; i++)
            {
                if (fumens[i] != null && fumens[i] != "")
                {
                    maidata.Add("&inote_" + (i+1) + "=" + fumens[i].Trim());
                }
            }
            File.WriteAllLines(filename, maidata.ToArray(),Encoding.UTF8);
        }
        static private string GetValue(string varline)
        {
            return varline.Split('=')[1];
        }
        /// <summary>
        /// This method serialize the fumen data and load it into the static class.
        /// </summary>
        /// <param name="text">fumen text</param>
        /// <param name="position">the position of the cusor, to get the return time</param>
        /// <returns>the song time at the position</returns>
        static public double Serialize(string text, long position=0)
        {
            List<SimaiTimingPoint> _notelist = new List<SimaiTimingPoint>();
            List<SimaiTimingPoint> _timinglist = new List<SimaiTimingPoint>();
            try
            {
                float bpm = 0;
                float curHSpeed = 1f;
                double time = first; //in seconds
                double requestedTime = 0;
                int beats = 4;
                bool haveNote = false;
                string noteTemp = "";
                int Ycount=0, Xcount = 0;

                for (int i = 0; i < text.Length; i++)
                {
                    if (text[i] == '|' && i+1 < text.Length && text[i+1] == '|')
                    {
                        // 跳过注释
                        Xcount++;
                        while(i < text.Length && text[i] != '\n')
                        {
                            i++;
                            Xcount++;
                        }
                        Ycount++;
                        Xcount = 0;
                        continue;
                    }
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
                        haveNote = false;
                        noteTemp = "";
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
                        haveNote = false;
                        noteTemp = "";
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
                    if (text[i] == '<')
                    //Get HS
                    {
                        haveNote = false;
                        noteTemp = "";
                        string hs_s = "";
                        if (text[i+1] == 'H' && text[i+2] == 'S' && text[i+3] == '*')
                        {
                            i += 4;
                            Xcount += 4;
                        }
                        while (text[i] != '>')
                        {
                            hs_s += text[i];
                            i++;
                            Xcount++;
                        }
                        curHSpeed = float.Parse(hs_s);
                        //Console.WriteLine("HS" + curHSpeed);
                        continue;
                    }
                    if (isNote(text[i]))
                    {
                        haveNote = true;
                    }
                    if (haveNote && text[i] != ',' )
                    {
                        noteTemp += text[i];
                    }
                    if (text[i] == ',')
                    {
                        if (haveNote)
                        {
                            if (noteTemp.Contains('`'))
                            {
                                // 伪双
                                string[] fakeEachList = noteTemp.Split('`');
                                double fakeTime = time;
                                double timeInterval = 1.875 / bpm; // 128分音
                                foreach(string fakeEachGroup in fakeEachList)
                                {
                                    Console.WriteLine(fakeEachGroup);
                                    _notelist.Add(new SimaiTimingPoint(fakeTime, Xcount, Ycount, fakeEachGroup, bpm, curHSpeed));
                                    fakeTime += timeInterval;
                                }
                            }
                            else
                            {
                                _notelist.Add(new SimaiTimingPoint(time, Xcount, Ycount, noteTemp, bpm, curHSpeed));
                            }
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
        public List<SimaiNote> noteList = new List<SimaiNote>(); //only used for json serialize
        public float HSpeed = 1f;
        public SimaiTimingPoint(double _time, int textposX = 0, int textposY = 0,string _content = "",float bpm=0f,float _hspeed = 1f)
        {
            time = _time;
            rawTextPositionX = textposX;
            rawTextPositionY = textposY;
            notesContent = _content.Replace("\n","").Replace(" ","");
            currentBpm = bpm;
            HSpeed = _hspeed;
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
            var noteContents = content.Split('*');
            var note1 = getSingleNote(noteContents[0]);
            simaiNotes.Add(note1);
            var newNoteContent = noteContents.ToList();
            newNoteContent.RemoveAt(0);
            //删除第一个NOTE
            foreach (var item in newNoteContent)
            {
                var note2text = note1.startPosition + item;
                var note2 = getSingleNote(note2text);
                note2.isSlideNoHead = true;
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
                    //Console.WriteLine("Hold:" +simaiNote.touchArea+ simaiNote.startPosition + " TimeLastFor:" + simaiNote.holdTime);
                }
                else
                {
                    simaiNote.noteType = SimaiNoteType.Hold;
                    if (noteText.Last() == 'h')
                    {
                        simaiNote.holdTime = 0;
                    }
                    else
                    {
                        simaiNote.holdTime = getTimeFromBeats(noteText);
                    }
                    //Console.WriteLine("Hold:" + simaiNote.startPosition + " TimeLastFor:" + simaiNote.holdTime);
                }
            }
            //slide
            if (isSlideNote(noteText)) {
                simaiNote.noteType = SimaiNoteType.Slide;
                simaiNote.slideTime = getTimeFromBeats(noteText);
                var timeStarWait = getStarWaitTime(noteText);
                simaiNote.slideStartTime = time + timeStarWait;
                if(noteText.Contains('!'))
                {
                    simaiNote.isSlideNoHead = true;
                    noteText = noteText.Replace("!", "");
                }else if(noteText.Contains('?'))
                {
                    simaiNote.isSlideNoHead = true;
                    noteText = noteText.Replace("?", "");
                }
                //Console.WriteLine("Slide:" + simaiNote.startPosition + " TimeLastFor:" + simaiNote.slideTime);
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
            //starHead
            if (noteText.Contains('$'))
            {
                simaiNote.isForceStar = true;
                if (noteText.Count(o=>o=='$') == 2)
                    simaiNote.isFakeRotate = true;
                noteText = noteText.Replace("$", "");
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
        public bool isSlideNoHead = false;
        public bool isForceStar = false;
        public bool isFakeRotate = false;

        public int startPosition = 1; //键位（1-8）
        public char touchArea = ' ';

        public double holdTime = 0d;

        public double slideStartTime = 0d;
        public double slideTime = 0d;

        public string noteContent; //used for star explain
    }

   
}
