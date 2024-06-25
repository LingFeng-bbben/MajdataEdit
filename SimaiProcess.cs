using System.IO;
using System.Text;
using System.Windows;

namespace MajdataEdit;

internal static class SimaiProcess
{
    public static string? title;
    public static string? artist;
    public static string? designer;
    public static string? other_commands;
    public static float first;
    public static string[] fumens = new string[7];
    public static string[] levels = new string[7];

    /// <summary>
    ///     the timing points that contains notedata
    /// </summary>
    public static List<SimaiTimingPoint> notelist = new();

    /// <summary>
    ///     the timing points made by "," in maidata
    /// </summary>
    public static List<SimaiTimingPoint> timinglist = new();

    /// <summary>
    ///     Reset all the data in the static class.
    /// </summary>
    public static void ClearData()
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
    ///     Read the maidata.txt into the static class, including the variables. Show up a messageBox when enconter any
    ///     exception.
    /// </summary>
    /// <param name="filename">file path of maidata.txt</param>
    /// <returns>if the read process faced any error</returns>
    public static bool ReadData(string filename)
    {
        var i = 0;
        other_commands = "";
        try
        {
            var maidataTxt = File.ReadAllLines(filename, Encoding.UTF8);
            for (i = 0; i < maidataTxt.Length; i++)
                if (maidataTxt[i].StartsWith("&title="))
                    title = GetValue(maidataTxt[i]);
                else if (maidataTxt[i].StartsWith("&artist="))
                    artist = GetValue(maidataTxt[i]);
                else if (maidataTxt[i].StartsWith("&des="))
                    designer = GetValue(maidataTxt[i]);
                else if (maidataTxt[i].StartsWith("&first="))
                    first = float.Parse(GetValue(maidataTxt[i]));
                else if (maidataTxt[i].StartsWith("&lv_") || maidataTxt[i].StartsWith("&inote_"))
                    for (var j = 1; j < 8 && i < maidataTxt.Length; j++)
                    {
                        if (maidataTxt[i].StartsWith("&lv_" + j + "="))
                            levels[j - 1] = GetValue(maidataTxt[i]);
                        if (maidataTxt[i].StartsWith("&inote_" + j + "="))
                        {
                            var TheNote = "";
                            TheNote += GetValue(maidataTxt[i]) + "\n";
                            i++;
                            for (; i < maidataTxt.Length; i++)
                            {
                                if (i < maidataTxt.Length)
                                    if (maidataTxt[i].StartsWith("&"))
                                        break;
                                TheNote += maidataTxt[i] + "\n";
                            }

                            fumens[j - 1] = TheNote;
                        }
                    }
                else
                    other_commands += maidataTxt[i].Trim() + "\n";

            other_commands = other_commands.Trim();
            return true;
        }
        catch (Exception e)
        {
            MessageBox.Show("在maidata.txt第" + (i + 1) + "行:\n" + e.Message, "读取谱面时出现错误");
            return false;
        }
    }

    /// <summary>
    ///     Save the static data to maidata.txt
    /// </summary>
    /// <param name="filename">file path of maidata.txt</param>
    public static void SaveData(string filename)
    {
        var maidata = new List<string>
        {
            "&title=" + title,
            "&artist=" + artist,
            "&first=" + first,
            "&des=" + designer,
            other_commands!
        };
        for (var i = 0; i < levels.Length; i++)
            if (levels[i] != null && levels[i] != "")
                maidata.Add("&lv_" + (i + 1) + "=" + levels[i].Trim());
        for (var i = 0; i < fumens.Length; i++)
            if (fumens[i] != null && fumens[i] != "")
                maidata.Add("&inote_" + (i + 1) + "=" + fumens[i].Trim());
        File.WriteAllLines(filename, maidata.ToArray());
    }

    private static string GetValue(string varline)
    {
        return varline.Substring(varline.IndexOf("=") + 1);
    }

    /// <summary>
    ///     This method serialize the fumen data and load it into the static class.
    /// </summary>
    /// <param name="text">fumen text</param>
    /// <param name="position">the position of the cusor, to get the return time</param>
    /// <returns>the song time at the position</returns>
    public static double Serialize(string text, long position = 0)
    {
        var _notelist = new List<SimaiTimingPoint>();
        var _timinglist = new List<SimaiTimingPoint>();
        try
        {
            float bpm = 0;
            var curHSpeed = 1f;
            double time = first; //in seconds
            double requestedTime = 0;
            var beats = 4;
            var haveNote = false;
            var noteTemp = "";
            int Ycount = 0, Xcount = 0;

            for (var i = 0; i < text.Length; i++)
            {
                if (text[i] == '|' && i + 1 < text.Length && text[i + 1] == '|')
                {
                    // 跳过注释
                    Xcount++;
                    while (i < text.Length && text[i] != '\n')
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

                if (i - 1 < position) requestedTime = time;
                if (text[i] == '(')
                    //Get bpm
                {
                    haveNote = false;
                    noteTemp = "";
                    var bpm_s = "";
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
                    var beats_s = "";
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

                if (text[i] == 'H')
                    //Get HS
                {
                    haveNote = false;
                    noteTemp = "";
                    var hs_s = "";
                    if (text[i + 1] == 'S' && text[i + 2] == '*')
                    {
                        i += 3;
                        Xcount += 3;
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

                if (isNote(text[i])) haveNote = true;
                if (haveNote && text[i] != ',') noteTemp += text[i];
                if (text[i] == ',')
                {
                    if (haveNote)
                    {
                        if (noteTemp.Contains('`'))
                        {
                            // 伪双
                            var fakeEachList = noteTemp.Split('`');
                            var fakeTime = time;
                            var timeInterval = 1.875 / bpm; // 128分音
                            foreach (var fakeEachGroup in fakeEachList)
                            {
                                Console.WriteLine(fakeEachGroup);
                                _notelist.Add(new SimaiTimingPoint(fakeTime, Xcount, Ycount, fakeEachGroup, bpm,
                                    curHSpeed));
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

                    _timinglist.Add(new SimaiTimingPoint(time, Xcount, Ycount, "", bpm));


                    time += 1d / (bpm / 60d) * 4d / beats;
                    //Console.WriteLine(time);
                    haveNote = false;
                }
            }

            notelist = _notelist;
            timinglist = _timinglist;
            //Console.WriteLine(notelist.ToArray());
            return requestedTime;
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            return 0;
        }
    }

    public static void ClearNoteListPlayedState()
    {
        notelist.Sort((x, y) => x.time.CompareTo(y.time));
        for (var i = 0; i < notelist.Count; i++) notelist[i].havePlayed = false;
    }

    private static bool isNote(char noteText)
    {
        var SlideMarks = "1234567890ABCDE"; ///ABCDE for touch
        foreach (var mark in SlideMarks)
            if (noteText == mark)
                return true;
        return false;
    }

    public static string GetDifficultyText(int index)
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

internal class SimaiTimingPoint
{
    public float currentBpm = -1;
    public bool havePlayed;
    public float HSpeed = 1f;
    public List<SimaiNote> noteList = new(); //only used for json serialize
    public string notesContent;
    public int rawTextPositionX;
    public int rawTextPositionY;
    public double time;

    public SimaiTimingPoint(double _time, int textposX = 0, int textposY = 0, string _content = "", float bpm = 0f,
        float _hspeed = 1f)
    {
        time = _time;
        rawTextPositionX = textposX;
        rawTextPositionY = textposY;
        notesContent = _content.Replace("\n", "").Replace(" ", "");
        currentBpm = bpm;
        HSpeed = _hspeed;
    }

    public List<SimaiNote> getNotes()
    {
        if (noteList.Count != 0) return noteList;

        var simaiNotes = new List<SimaiNote>();
        if (notesContent == "") return simaiNotes;
        try
        {
            var dummy = 0;
            if (notesContent.Length == 2 && int.TryParse(notesContent, out dummy)) //连写数字
            {
                simaiNotes.Add(getSingleNote(notesContent[0].ToString()));
                simaiNotes.Add(getSingleNote(notesContent[1].ToString()));
                return simaiNotes;
            }

            if (notesContent.Contains('/'))
            {
                var notes = notesContent.Split('/');
                foreach (var note in notes)
                    if (note.Contains('*'))
                        simaiNotes.AddRange(getSameHeadSlide(note));
                    else
                        simaiNotes.Add(getSingleNote(note));
                return simaiNotes;
            }

            if (notesContent.Contains('*'))
            {
                simaiNotes.AddRange(getSameHeadSlide(notesContent));
                return simaiNotes;
            }

            simaiNotes.Add(getSingleNote(notesContent));
            noteList = simaiNotes;
            return simaiNotes;
        }
        catch
        {
            noteList = new List<SimaiNote>();
            return noteList;
        }
    }

    private List<SimaiNote> getSameHeadSlide(string content)
    {
        var simaiNotes = new List<SimaiNote>();
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
        var simaiNote = new SimaiNote();

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

        if (noteText.Contains('f')) simaiNote.isHanabi = true;

        //hold
        if (noteText.Contains('h'))
        {
            if (isTouchNote(noteText))
            {
                simaiNote.noteType = SimaiNoteType.TouchHold;
                simaiNote.holdTime = getTimeFromBeats(noteText);
                //Console.WriteLine("Hold:" +simaiNote.touchArea+ simaiNote.startPosition + " TimeLastFor:" + simaiNote.holdTime);
            }
            else
            {
                simaiNote.noteType = SimaiNoteType.Hold;
                if (noteText.Last() == 'h')
                    simaiNote.holdTime = 0;
                else
                    simaiNote.holdTime = getTimeFromBeats(noteText);
                //Console.WriteLine("Hold:" + simaiNote.startPosition + " TimeLastFor:" + simaiNote.holdTime);
            }
        }

        //slide
        if (isSlideNote(noteText))
        {
            simaiNote.noteType = SimaiNoteType.Slide;
            simaiNote.slideTime = getTimeFromBeats(noteText);
            var timeStarWait = getStarWaitTime(noteText);
            simaiNote.slideStartTime = time + timeStarWait;
            if (noteText.Contains('!'))
            {
                simaiNote.isSlideNoHead = true;
                noteText = noteText.Replace("!", "");
            }
            else if (noteText.Contains('?'))
            {
                simaiNote.isSlideNoHead = true;
                noteText = noteText.Replace("?", "");
            }
            //Console.WriteLine("Slide:" + simaiNote.startPosition + " TimeLastFor:" + simaiNote.slideTime);
        }

        //break
        if (noteText.Contains('b'))
        {
            if (simaiNote.noteType == SimaiNoteType.Slide)
            {
                // 如果是Slide 则要检查这个b到底是星星头的还是Slide本体的

                // !!! **SHIT CODE HERE** !!!
                var startIndex = 0;
                while ((startIndex = noteText.IndexOf('b', startIndex)) != -1)
                {
                    if (startIndex < noteText.Length - 1)
                    {
                        // 如果b不是最后一个字符 我们就检查b之后一个字符是不是`[`符号：如果是 那么就是break slide
                        if (noteText[startIndex + 1] == '[')
                            simaiNote.isSlideBreak = true;
                        else
                            // 否则 那么不管这个break出现在slide的哪一个地方 我们都认为他是星星头的break
                            // SHIT CODE!
                            simaiNote.isBreak = true;
                    }
                    else
                    {
                        // 如果b符号是整个文本的最后一个字符 那么也是break slide（Simai语法）
                        simaiNote.isSlideBreak = true;
                    }

                    startIndex++;
                }
            }
            else
            {
                // 除此之外的Break就无所谓了
                simaiNote.isBreak = true;
            }

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
            if (noteText.Count(o => o == '$') == 2)
                simaiNote.isFakeRotate = true;
            noteText = noteText.Replace("$", "");
        }

        simaiNote.noteContent = noteText;
        return simaiNote;
    }

    private bool isSlideNote(string noteText)
    {
        var SlideMarks = "-^v<>Vpqszw";
        foreach (var mark in SlideMarks)
            if (noteText.Contains(mark))
                return true;
        return false;
    }

    private bool isTouchNote(string noteText)
    {
        var SlideMarks = "ABCDE";
        foreach (var mark in SlideMarks)
            if (noteText.StartsWith(mark.ToString()))
                return true;
        return false;
    }

    private double getTimeFromBeats(string noteText)
    {
        if (noteText.Count(c => { return c == '['; }) > 1)
        {
            // 组合slide 有多个时长
            double wholeTime = 0;

            var partStartIndex = 0;
            while (noteText.IndexOf('[', partStartIndex) >= 0)
            {
                var startIndex = noteText.IndexOf('[', partStartIndex);
                var overIndex = noteText.IndexOf(']', partStartIndex);
                partStartIndex = overIndex + 1;
                var innerString = noteText.Substring(startIndex + 1, overIndex - startIndex - 1);
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
                        wholeTime += double.Parse(times[1]);
                        continue;
                    }
                }

                if (innerString.Count(o => o == '#') == 2)
                {
                    var times = innerString.Split('#');
                    wholeTime += double.Parse(times[2]);
                    continue;
                }

                var numbers = innerString.Split(':');
                var divide = int.Parse(numbers[0]);
                var count = int.Parse(numbers[1]);


                wholeTime += timeOneBeat * 4d / divide * count;
            }

            return wholeTime;
        }

        {
            var startIndex = noteText.IndexOf('[');
            var overIndex = noteText.IndexOf(']');
            var innerString = noteText.Substring(startIndex + 1, overIndex - startIndex - 1);
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

            var numbers = innerString.Split(':'); //TODO:customBPM
            var divide = int.Parse(numbers[0]);
            var count = int.Parse(numbers[1]);


            return timeOneBeat * 4d / divide * count;
        }
    }

    private double getStarWaitTime(string noteText)
    {
        var startIndex = noteText.IndexOf('[');
        var overIndex = noteText.IndexOf(']');
        var innerString = noteText.Substring(startIndex + 1, overIndex - startIndex - 1);
        double bpm = currentBpm;
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

internal enum SimaiNoteType
{
    Tap,
    Slide,
    Hold,
    Touch,
    TouchHold
}

internal class SimaiNote
{
    public double holdTime;
    public bool isBreak;
    public bool isEx;
    public bool isFakeRotate;
    public bool isForceStar;
    public bool isHanabi;
    public bool isSlideBreak;
    public bool isSlideNoHead;

    public string? noteContent; //used for star explain
    public SimaiNoteType noteType;

    public double slideStartTime;
    public double slideTime;

    public int startPosition = 1; //键位（1-8）
    public char touchArea = ' ';
}