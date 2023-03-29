using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Timers;
using Un4seen.Bass;
using System.Windows.Threading;
using System.Text;
using System.Linq;
using System.IO;
using System.Threading;
using Timer = System.Timers.Timer;
using System.Runtime.InteropServices;

namespace MajdataEdit
{
    public partial class MainWindow
    {
        class SoundEffectTiming
        {
            public bool hasAnswer = false;
            public bool hasJudge = false;
            public bool hasJudgeBreak = false;
            public bool hasBreak = false;
            public bool hasTouch = false;
            public bool hasHanabi = false;
            public bool hasJudgeEx = false;
            public bool hasTouchHold = false;
            public bool hasTouchHoldEnd = false;
            public bool hasSlide = false;
            public bool hasAllPerfect = false;
            public bool hasClock = false;
            public bool hasBreakSlideStart = false;
            public bool hasBreakSlide = false;
            public bool hasJudgeBreakSlide = false;
            public double time;

            public SoundEffectTiming(double _time, bool _hasAnswer = false, bool _hasJudge = false, bool _hasJudgeBreak = false,
                                     bool _hasBreak = false, bool _hasTouch = false, bool _hasHanabi = false,
                                     bool _hasJudgeEx = false, bool _hasTouchHold = false, bool _hasSlide = false,
                                     bool _hasTouchHoldEnd = false, bool _hasAllPerfect = false, bool _hasClock = false,
                                     bool _hasBreakSlideStart = false, bool _hasBreakSlide = false, bool _hasJudgeBreakSlide = false)
            {
                time = _time;
                hasAnswer = _hasAnswer;
                hasJudge = _hasJudge;
                hasJudgeBreak = _hasJudgeBreak; // 我是笨蛋
                hasBreak = _hasBreak;
                hasTouch = _hasTouch;
                hasHanabi = _hasHanabi;
                hasJudgeEx = _hasJudgeEx;
                hasTouchHold = _hasTouchHold;
                hasSlide = _hasSlide;
                hasTouchHoldEnd = _hasTouchHoldEnd;
                hasAllPerfect = _hasAllPerfect;
                hasClock = _hasClock;
                hasBreakSlideStart = _hasBreakSlideStart;
                hasBreakSlide = _hasBreakSlide;
                hasJudgeBreakSlide = _hasJudgeBreakSlide;
            }
        }

        class SoundBank {
            internal SoundBank(string Path)
            {
                this.FilePath = Path;

                this.InitializeSampleData();
            }

            public bool Temp { get; private set; }
            public string FilePath { get; private set; }
            public int ID { get; private set; }
            public BASS_SAMPLE Info { get; private set; }

            public long RawSize { get; set; }
            public Int16[] Raw { get; private set; }

            public void Reassign(string FFMpegDirectory, string NewDirectory, string Filename, int NewFrequency)
            {
                if (FFMpegDirectory.Length == 0)
                    return;

                Func<string, string> NormalizePath = (path) => {
                    return string.Join(Path.DirectorySeparatorChar.ToString(), path.Split('/'));
                };
                
                Temp = true;
                var OriginalPath = FilePath;
                this.FilePath = NewDirectory + "/" + Filename;

                string args = string.Format(
                    "-loglevel 24 -y -i \"{0}\" -ac 2 -ar {2} \"{1}\"",
                    NormalizePath(OriginalPath),
                    NormalizePath(this.FilePath),
                    NewFrequency
                );
                var startInfo = new ProcessStartInfo(FFMpegDirectory + "/ffmpeg.exe", args);
                startInfo.UseShellExecute = false;
                startInfo.CreateNoWindow = true;
                startInfo.RedirectStandardError = true;
                var proc = Process.Start(startInfo);
                proc.WaitForExit();
                if (proc.ExitCode != 0)
                    throw new Exception(proc.StandardError.ReadToEnd());

                this.Free();
                this.InitializeSampleData();
            }

            void InitializeSampleData()
            {
                this.ID = Bass.BASS_SampleLoad(FilePath, 0, 0, 1, BASSFlag.BASS_DEFAULT);
                if(this.ID != 0)
                    this.Info = Bass.BASS_SampleGetInfo(this.ID);

                if (this.Info != null) {
                    this.RawSize = this.Info.length / 2;
                } else {
                    this.RawSize = 0;
                }
            }

            public void InitializeRawSample()
            {
                if (this.Info == null)
                    return;

                this.Raw = new Int16[this.RawSize];
                Bass.BASS_SampleGetData(this.ID, this.Raw);
            }

            public void Free()
            {
                if (this.ID <= 0)
                    return;

                this.Raw = null;
                Bass.BASS_SampleFree(this.ID);
            }

            public int Frequency
            {
                get
                {
                    if (this.Info != null)
                    {
                        return this.Info.freq;
                    }
                    return -1;
                }
            }

            public bool FrequencyCheck(SoundBank other)
            {
                return this.Frequency == other.Frequency && this.Frequency > 0;
            }
        }

        enum SoundDataType
        {
            None,
            Answer,
            Judge, JudgeBreak, JudgeEX,
            Break,
            Hanabi,
            TouchHold,
            Slide, Touch,
            AllPerfect, FullComboFanfare,
            Clock,
            BreakSlideStart, BreakSlide, JudgeBreakSlide
        }

        struct SoundDataRange
        {
            internal SoundDataRange(SoundDataType type, long from, long len)
            {
                this.Type = type;
                this.From = from;
                this.To = from + len;
            }

            public SoundDataType Type { get; private set; }
            public long From { get; private set; }
            public long To { get; private set; }
            public long Length
            {
                get
                {
                    return To - From;
                }
                set
                {
                    To = From + value;
                }
            }

            public bool In (long value)
            {
                return value >= From && value < To;
            }
        }


        Timer waveStopMonitorTimer = new Timer(33);

        double playStartTime = 0d;
        double extraTime4AllPerfect;     // 需要在播放完后等待All Perfect特效的秒数

        bool isPlaying = false;          // 为了解决播放到结束时自动停止
        bool isPlan2Stop = false;        // 已准备停止 当all perfect无法在播放完BGM前结束时需要此功能

        public int bgmStream = -114514;
        public int answerStream = -114514;
        public int judgeStream = -114514;
        public int judgeBreakStream = -114514;   // 这个是break的判定音效 不是欢呼声
        public int breakStream = -114514;        // 这个才是欢呼声
        public int judgeExStream = -114514;
        public int hanabiStream = -114514;
        public int holdRiserStream = -114514;
        public int trackStartStream = -114514;
        public int slideStream = -114514;
        public int touchStream = -114514;
        public int allperfectStream = -114514;
        public int fanfareStream = -114514;
        public int clockStream = -114514;
        public int breakSlideStartStream = -114514;     // break-slide启动音效
        public int breakSlideStream = -114514;          // break-slide欢呼声（critical perfect音效）
        public int judgeBreakSlideStream = -114514;     // break-slide判定音效

        List<SoundEffectTiming> waitToBePlayed;
        //private Stopwatch sw = new Stopwatch();

        // This update "middle" frequently to monitor if the wave has to be stopped
        private void WaveStopMonitorTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            WaveStopMonitorUpdate();
        }
        private void ReadSoundEffect()
        {
            var path = Environment.CurrentDirectory + "/SFX/";
            answerStream = Bass.BASS_StreamCreateFile(path + "answer.wav", 0L, 0L, BASSFlag.BASS_SAMPLE_FLOAT);
            judgeStream = Bass.BASS_StreamCreateFile(path + "judge.wav", 0L, 0L, BASSFlag.BASS_SAMPLE_FLOAT);
            judgeBreakStream = Bass.BASS_StreamCreateFile(path + "judge_break.wav", 0L, 0L, BASSFlag.BASS_SAMPLE_FLOAT);
            judgeExStream = Bass.BASS_StreamCreateFile(path + "judge_ex.wav", 0L, 0L, BASSFlag.BASS_SAMPLE_FLOAT);
            breakStream = Bass.BASS_StreamCreateFile(path + "break.wav", 0L, 0L, BASSFlag.BASS_SAMPLE_FLOAT);
            hanabiStream = Bass.BASS_StreamCreateFile(path + "hanabi.wav", 0L, 0L, BASSFlag.BASS_SAMPLE_FLOAT);
            holdRiserStream = Bass.BASS_StreamCreateFile(path + "touchHold_riser.wav", 0L, 0L, BASSFlag.BASS_SAMPLE_FLOAT);
            trackStartStream = Bass.BASS_StreamCreateFile(path + "track_start.wav", 0L, 0L, BASSFlag.BASS_SAMPLE_FLOAT);
            slideStream = Bass.BASS_StreamCreateFile(path + "slide.wav", 0L, 0L, BASSFlag.BASS_SAMPLE_FLOAT);
            touchStream = Bass.BASS_StreamCreateFile(path + "touch.wav", 0L, 0L, BASSFlag.BASS_SAMPLE_FLOAT);
            allperfectStream = Bass.BASS_StreamCreateFile(path + "all_perfect.wav", 0L, 0L, BASSFlag.BASS_SAMPLE_FLOAT);
            fanfareStream = Bass.BASS_StreamCreateFile(path + "fanfare.wav", 0L, 0L, BASSFlag.BASS_SAMPLE_FLOAT);
            clockStream = Bass.BASS_StreamCreateFile(path + "clock.wav", 0L, 0L, BASSFlag.BASS_SAMPLE_FLOAT);
            breakSlideStartStream = Bass.BASS_StreamCreateFile(path + "break_slide_start.wav", 0L, 0L, BASSFlag.BASS_SAMPLE_FLOAT);
            breakSlideStream = Bass.BASS_StreamCreateFile(path + "break_slide.wav", 0L, 0L, BASSFlag.BASS_SAMPLE_FLOAT);
            judgeBreakSlideStream = Bass.BASS_StreamCreateFile(path + "judge_break_slide.wav", 0L, 0L, BASSFlag.BASS_SAMPLE_FLOAT);
        }
        [DllImport("winmm")] 
        static extern void timeBeginPeriod(int t); 
        [DllImport("winmm")] 
        static extern void timeEndPeriod(int t);
        private void StartSELoop()
        {
            Thread thread = new Thread(() =>
            {
                timeBeginPeriod(1);
                var lasttime = Bass.BASS_ChannelBytes2Seconds(bgmStream, Bass.BASS_ChannelGetPosition(bgmStream));
                while (isPlaying)
                {
                    //sw.Reset();
                    //sw.Start();
                    SoundEffectUpdate();
                    Thread.Sleep(1);
                    //sw.Stop();
                    //if(sw.Elapsed.TotalMilliseconds>1.5)
                    //    Console.WriteLine(sw.Elapsed);
                }
                timeEndPeriod(1);
            });
            thread.Priority = ThreadPriority.Highest;
            thread.Start();

        }
        private void SoundEffectUpdate()
        {
            try
            {
                var currentTime = Bass.BASS_ChannelBytes2Seconds(bgmStream, Bass.BASS_ChannelGetPosition(bgmStream));
                //var waitToBePlayed = SimaiProcess.notelist.FindAll(o => o.havePlayed == false && o.time > currentTime);
                if (waitToBePlayed.Count < 1) return;
                var nearestTime = waitToBePlayed[0].time;
                //Console.WriteLine(nearestTime - currentTime);
                if ((nearestTime - currentTime) <= 0.0545)//dont touch this!!!!! this related to delay
                {
                    SoundEffectTiming se = waitToBePlayed[0];
                    waitToBePlayed.RemoveAt(0);

                    if (se.hasAnswer)
                    {
                        Bass.BASS_ChannelPlay(answerStream, true);
                    }
                    if (se.hasJudge)
                    {
                        Bass.BASS_ChannelPlay(judgeStream, true);
                    }
                    if (se.hasJudgeBreak)
                    {
                        Bass.BASS_ChannelPlay(judgeBreakStream, true);
                    }
                    if (se.hasJudgeEx)
                    {
                        Bass.BASS_ChannelPlay(judgeExStream, true);
                    }
                    if (se.hasBreak)
                    {
                        Bass.BASS_ChannelPlay(breakStream, true);
                    }
                    if (se.hasTouch)
                    {
                        Bass.BASS_ChannelPlay(touchStream, true);
                    }
                    if (se.hasHanabi) //may cause delay
                    {
                        Bass.BASS_ChannelPlay(hanabiStream, true);
                    }
                    if (se.hasTouchHold)
                    {
                        Bass.BASS_ChannelPlay(holdRiserStream, true);
                    }
                    if (se.hasTouchHoldEnd)
                    {
                        Bass.BASS_ChannelStop(holdRiserStream);
                    }
                    if (se.hasSlide)
                    {
                        Bass.BASS_ChannelPlay(slideStream, true);
                    }
                    if (se.hasBreakSlideStart)
                    {
                        Bass.BASS_ChannelPlay(breakSlideStartStream, true);
                    }
                    if (se.hasBreakSlide)
                    {
                        Bass.BASS_ChannelPlay(breakSlideStream, true);
                    }
                    if (se.hasJudgeBreakSlide)
                    {
                        Bass.BASS_ChannelPlay(judgeBreakSlideStream, true);
                    }
                    if (se.hasAllPerfect)
                    {
                        Bass.BASS_ChannelPlay(allperfectStream, true);
                        Bass.BASS_ChannelPlay(fanfareStream, true);
                    }
                    if (se.hasClock)
                    {
                        Bass.BASS_ChannelPlay(clockStream, true);
                    }
                    //
                    Dispatcher.Invoke(() =>
                    {
                        if ((bool)FollowPlayCheck.IsChecked)
                        {
                            ghostCusorPositionTime = (float)nearestTime;
                            SeekTextFromTime();
                        }
                    });
                }

            }
            catch { }
        }
        double GetAllPerfectStartTime()
        {
            // 获取All Perfect理论上播放的时间点 也就是最后一个被完成的note
            double latestNoteFinishTime = -1;
            double baseTime, noteTime;
            foreach (var noteGroup in SimaiProcess.notelist)
            {
                baseTime = noteGroup.time;
                foreach (var note in noteGroup.getNotes())
                {
                    if (note.noteType == SimaiNoteType.Tap || note.noteType == SimaiNoteType.Touch)
                    {
                        noteTime = baseTime;
                    }
                    else if (note.noteType == SimaiNoteType.Hold || note.noteType == SimaiNoteType.TouchHold)
                    {
                        noteTime = baseTime + note.holdTime;
                    }
                    else if (note.noteType == SimaiNoteType.Slide)
                    {
                        noteTime = note.slideStartTime + note.slideTime;
                    }
                    else
                    {
                        noteTime = -1;
                    }
                    if (noteTime > latestNoteFinishTime)
                    {
                        latestNoteFinishTime = noteTime;
                    }
                }
            }
            return latestNoteFinishTime;
        }
        void generateSoundEffectList(double startTime, bool isOpIncluded)
        {
            waitToBePlayed = new List<SoundEffectTiming>();
            if (isOpIncluded)
            {
                var cmds = SimaiProcess.other_commands.Split('\n');
                foreach (var cmdl in cmds)
                {
                    if (cmdl.Length > 12 && cmdl.Substring(1, 11) == "clock_count")
                    {
                        try
                        {
                            int clock_cnt = int.Parse(cmdl.Substring(13));
                            double clock_int = 60.0d / SimaiProcess.notelist[0].currentBpm;
                            for (int i = 0; i < clock_cnt; i++)
                            {
                                waitToBePlayed.Add(new SoundEffectTiming(i * clock_int, _hasClock: true));
                            }
                        }
                        catch
                        {

                        }
                    }
                }
            }
            foreach (var noteGroup in SimaiProcess.notelist)
            {
                if (noteGroup.time < startTime) { continue; }

                SoundEffectTiming stobj;

                // 如果目前为止已经有一个SE了 那么就直接使用这个SE
                var combIndex = waitToBePlayed.FindIndex(o => Math.Abs(o.time - noteGroup.time) < 0.001f);
                if (combIndex != -1)
                {
                    stobj = waitToBePlayed[combIndex];
                }
                else
                {
                    stobj = new SoundEffectTiming(noteGroup.time);
                }

                var notes = noteGroup.getNotes();
                foreach (SimaiNote note in notes)
                {
                    switch (note.noteType)
                    {
                        case SimaiNoteType.Tap:
                            {
                                stobj.hasAnswer = true;
                                if (note.isBreak)
                                {
                                    // 如果是Break 则有Break判定音和Break欢呼音（2600）
                                    stobj.hasBreak = true;
                                    stobj.hasJudgeBreak = true;
                                }
                                if (note.isEx)
                                {
                                    // 如果是Ex 则有Ex判定音
                                    stobj.hasJudgeEx = true;
                                }
                                if (!note.isBreak && !note.isEx)
                                {
                                    // 如果二者皆没有 则是普通note 播放普通判定音
                                    stobj.hasJudge = true;
                                }
                                break;
                            }
                        case SimaiNoteType.Hold:
                            {
                                stobj.hasAnswer = true;
                                // 类似于Tap 判断Break和Ex的音效 二者皆无则为普通
                                if (note.isBreak)
                                {
                                    stobj.hasBreak = true;
                                    stobj.hasJudgeBreak = true;
                                }
                                if (note.isEx)
                                {
                                    stobj.hasJudgeEx = true;
                                }
                                if (!note.isBreak && !note.isEx)
                                {
                                    stobj.hasJudge = true;
                                }

                                // 计算Hold尾部的音效
                                if (!(note.holdTime <= 0.00f))
                                {
                                    // 如果是短hold（六角tap），则忽略尾部音效。否则，才会计算尾部音效
                                    var targetTime = noteGroup.time + note.holdTime;
                                    var nearIndex = waitToBePlayed.FindIndex(o => Math.Abs(o.time - targetTime) < 0.001f);
                                    if (nearIndex != -1)
                                    {
                                        waitToBePlayed[nearIndex].hasAnswer = true;
                                        if (!note.isBreak && !note.isEx)
                                        {
                                            waitToBePlayed[nearIndex].hasJudge = true;
                                        }
                                    }
                                    else
                                    {
                                        // 只有最普通的Hold才有结尾的判定音 Break和Ex型则没有（Break没有为推定）
                                        SoundEffectTiming holdRelease = new SoundEffectTiming(targetTime, _hasAnswer: true, _hasJudge: !note.isBreak && !note.isEx);
                                        waitToBePlayed.Add(holdRelease);
                                    }
                                }
                                break;
                            }
                        case SimaiNoteType.Slide:
                            {
                                if (!note.isSlideNoHead)
                                {
                                    // 当Slide不是无头星星的时候 才有answer音和判定音
                                    stobj.hasAnswer = true;
                                    if (note.isBreak)
                                    {
                                        stobj.hasBreak = true;
                                        stobj.hasJudgeBreak = true;
                                    }
                                    if (note.isEx)
                                    {
                                        stobj.hasJudgeEx = true;
                                    }
                                    if (!note.isBreak && !note.isEx)
                                    {
                                        stobj.hasJudge = true;
                                    }
                                }

                                // Slide启动音效
                                var targetTime = note.slideStartTime;
                                var nearIndex = waitToBePlayed.FindIndex(o => Math.Abs(o.time - targetTime) < 0.001f);
                                if (nearIndex != -1)
                                {
                                    if (note.isSlideBreak)
                                    {
                                        // 如果是break slide的话 使用break slide的启动音效
                                        waitToBePlayed[nearIndex].hasBreakSlideStart = true;
                                    }
                                    else
                                    {
                                        // 否则使用普通slide的启动音效
                                        waitToBePlayed[nearIndex].hasSlide = true;
                                    }
                                }
                                else
                                {
                                    SoundEffectTiming slide;
                                    if (note.isSlideBreak)
                                    {
                                        slide = new SoundEffectTiming(targetTime, _hasBreakSlideStart: true);
                                    }
                                    else
                                    {
                                        slide = new SoundEffectTiming(targetTime, _hasSlide: true);
                                    }
                                    waitToBePlayed.Add(slide);
                                }
                                // Slide尾巴 如果是Break Slide的话 就要添加一个Break音效
                                if (note.isSlideBreak)
                                {
                                    targetTime = note.slideStartTime + note.slideTime;
                                    nearIndex = waitToBePlayed.FindIndex(o => Math.Abs(o.time - targetTime) < 0.001f);
                                    if (nearIndex != -1)
                                    {
                                        waitToBePlayed[nearIndex].hasBreakSlide = true;
                                        waitToBePlayed[nearIndex].hasJudgeBreakSlide = true;
                                    }
                                    else
                                    {
                                        SoundEffectTiming slide = new SoundEffectTiming(targetTime, _hasBreakSlide: true, _hasJudgeBreakSlide: true);
                                        waitToBePlayed.Add(slide);
                                    }
                                }
                                break;
                            }
                        case SimaiNoteType.Touch:
                            {
                                stobj.hasAnswer = true;
                                stobj.hasTouch = true;
                                if (note.isHanabi)
                                {
                                    stobj.hasHanabi = true;
                                }
                                break;
                            }
                        case SimaiNoteType.TouchHold:
                            {
                                stobj.hasAnswer = true;
                                stobj.hasTouch = true;
                                stobj.hasTouchHold = true;
                                // 计算TouchHold结尾
                                var targetTime = noteGroup.time + note.holdTime;
                                var nearIndex = waitToBePlayed.FindIndex(o => Math.Abs(o.time - targetTime) < 0.001f);
                                if (nearIndex != -1)
                                {
                                    if (note.isHanabi)
                                    {
                                        waitToBePlayed[nearIndex].hasHanabi = true;
                                    }
                                    waitToBePlayed[nearIndex].hasAnswer = true;
                                    waitToBePlayed[nearIndex].hasTouchHoldEnd = true;
                                }
                                else
                                {
                                    SoundEffectTiming tHoldRelease = new SoundEffectTiming(targetTime, _hasAnswer: true, _hasHanabi: note.isHanabi, _hasTouchHoldEnd: true);
                                    waitToBePlayed.Add(tHoldRelease);
                                }
                                break;
                            }
                    }
                }

                if (combIndex != -1)
                {
                    waitToBePlayed[combIndex] = stobj;
                }
                else
                {
                    waitToBePlayed.Add(stobj);
                }
            }
            if (isOpIncluded)
            {
                waitToBePlayed.Add(new SoundEffectTiming(GetAllPerfectStartTime(), _hasAllPerfect: true));
            }
            waitToBePlayed.Sort((o1, o2) => o1.time < o2.time ? -1 : 1);

            double apTime = GetAllPerfectStartTime();
            if (songLength < apTime + 4.0)
            {
                // 如果BGM的时长不足以播放完AP特效 这里假设AP特效持续4秒
                extraTime4AllPerfect = apTime + 4.0 - songLength; // 预留给AP的额外时间（播放结束后）
            }
            else
            {
                // 如果足够播完 那么就等到BGM结束再停止
                extraTime4AllPerfect = -1;
            }

            //Console.WriteLine(JsonConvert.SerializeObject(waitToBePlayed));
        }
        void renderSoundEffect(double delaySeconds)
        {
            //TODO: 改为异步并增加提示窗口
            string path = Environment.CurrentDirectory + "/SFX";
            string tempPath = GetViewerWorkingDirectory();
            string converterPath;

            List<string> pathEnv = new List<string>();
            pathEnv.Add(tempPath);
            pathEnv.AddRange(Environment.GetEnvironmentVariable("PATH").Split(Path.PathSeparator));
            converterPath = pathEnv.Where((scanPath) => {
                bool isExists = File.Exists(scanPath + "/ffmpeg.exe");
                return isExists;
            }).FirstOrDefault();

            bool throwErrorOnMismatch = converterPath.Length == 0;

            //默认参数：16bit
            Func<string, string> getBasePath = (rawPath) => {
                return rawPath.Split('/').Last();
            };
            SoundBank bgmBank = new SoundBank(maidataDir + "/track.mp3");

            Dictionary<string, SoundBank> comparableBanks = new Dictionary<string, SoundBank>();

            SoundBank answerBank = new SoundBank(path + "/answer.wav");
            SoundBank judgeBank = new SoundBank(path + "/judge.wav");
            SoundBank judgeBreakBank = new SoundBank(path + "/judge_break.wav");
            SoundBank judgeExBank = new SoundBank(path + "/judge_ex.wav");
            SoundBank breakBank = new SoundBank(path + "/break.wav");
            SoundBank hanabiBank = new SoundBank(path + "/hanabi.wav");
            SoundBank holdRiserBank = new SoundBank(path + "/touchHold_riser.wav");
            SoundBank trackStartBank = new SoundBank(path + "/track_start.wav");
            SoundBank slideBank = new SoundBank(path + "/slide.wav");
            SoundBank touchBank = new SoundBank(path + "/touch.wav");
            SoundBank apBank = new SoundBank(path + "/all_perfect.wav");
            SoundBank fanfareBank = new SoundBank(path + "/fanfare.wav");
            SoundBank clockBank = new SoundBank(path + "/clock.wav");
            SoundBank breakSlideStartBank = new SoundBank(path + "/break_slide_start.wav");
            SoundBank breakSlideBank = new SoundBank(path + "/break_slide.wav");
            SoundBank judgeBreakSlideBank = new SoundBank(path + "/judge_break_slide.wav");

            comparableBanks["Answer"] = answerBank;
            comparableBanks["Judge"] = judgeBank;
            comparableBanks["Judge Break"] = judgeBreakBank;
            comparableBanks["Judge EX"] = judgeExBank;
            comparableBanks["Break"] = breakBank;
            comparableBanks["Hanabi"] = hanabiBank;
            comparableBanks["Hold Riser"] = holdRiserBank;
            comparableBanks["Track Start"] = trackStartBank;
            comparableBanks["Slide"] = slideBank;
            comparableBanks["Touch"] = touchBank;
            comparableBanks["All Perfect"] = apBank;
            comparableBanks["Fanfare"] = fanfareBank;
            comparableBanks["Clock"] = clockBank;
            comparableBanks["Break Slide Start"] = breakSlideStartBank;
            comparableBanks["Break Slide"] = breakSlideBank;
            comparableBanks["Judge Break Slide"] = judgeBreakSlideBank;

            foreach (var compPair in comparableBanks)
            {
                // Skip non existent file.
                if (compPair.Value.Frequency < 0)
                    continue;

                if (bgmBank.FrequencyCheck(compPair.Value))
                    continue;

                if (throwErrorOnMismatch)
                    throw new Exception(
                        string.Format("BGM and {0} do not have same sample rate. Convert the {0} from {1}Hz into {2}Hz!", compPair.Key, compPair.Value.Frequency, bgmBank.Frequency)
                    );
                else
                {
                    Console.WriteLine(string.Format(
                        "Convert sample of {0} ({1}/{2})...",
                        compPair.Key,
                        compPair.Value.Info.length,
                        compPair.Value.Frequency
                    ));
                    compPair.Value.Reassign(converterPath, tempPath, "t_" + getBasePath(compPair.Value.FilePath), bgmBank.Frequency);
                }
            }

            var freq = bgmBank.Frequency;

            //读取原始采样数据
            long sampleCount = (long)((songLength + 5f) * freq * 2 );
            bgmBank.RawSize = sampleCount;
            Console.WriteLine(sampleCount);
            bgmBank.InitializeRawSample();

            foreach (var compPair in comparableBanks)
            {
                // Skip non existent file.
                if (compPair.Value.Frequency < 0)
                    continue;

                if (!bgmBank.FrequencyCheck(compPair.Value))
                    continue;

                Console.WriteLine(string.Format("Init sample for {0}...", compPair.Key));
                compPair.Value.InitializeRawSample();
            }

            List<SoundDataRange> trackOps = new List<SoundDataRange>();
            Dictionary<SoundDataType, Int16[]> typeSamples = new Dictionary<SoundDataType, Int16[]>();
            foreach (SoundDataType sType in Enum.GetValues(SoundDataType.None.GetType()))
            {
                if (sType == 0) continue;
                typeSamples[sType] = new Int16[sampleCount];
            }

            Func<SoundDataType, SoundBank> getSampleFromType = (type) =>
            {
                switch(type)
                {
                    case SoundDataType.Answer: return answerBank;
                    case SoundDataType.Judge: return judgeBank;
                    case SoundDataType.JudgeBreak: return judgeBreakBank;
                    case SoundDataType.JudgeEX: return judgeExBank;
                    case SoundDataType.Break: return breakBank;
                    case SoundDataType.Hanabi: return hanabiBank;
                    case SoundDataType.TouchHold: return holdRiserBank;
                    case SoundDataType.Slide: return slideBank;
                    case SoundDataType.Touch: return touchBank;
                    case SoundDataType.AllPerfect: return apBank;
                    case SoundDataType.FullComboFanfare: return fanfareBank;
                    case SoundDataType.Clock: return clockBank;
                    case SoundDataType.BreakSlideStart: return breakSlideStartBank;
                    case SoundDataType.BreakSlide: return breakSlideBank;
                    case SoundDataType.JudgeBreakSlide: return judgeBreakSlideBank;
                }
                return null;
            };

            Action<int, SoundDataType> sampleWrite = (time, type) =>
            {
                SoundBank sample = getSampleFromType(type);
                if (sample.Frequency <= 0) return;
                for (int t = 0; t < sample.RawSize; t++)
                    typeSamples[type][time + t] = sample.Raw[t];
            };

            Action<int, SoundDataType> sampleAdd = (time, type) =>
            {
                SoundBank sample = getSampleFromType(type);
                if (sample.Frequency <= 0) return;
                for (int t = 0; t < sample.RawSize; t++)
                {
                    Int32 value = typeSamples[type][time + t] + sample.Raw[t];
                    if (value > Int16.MaxValue)
                        value = Int16.MaxValue;
                    if (value < Int16.MinValue)
                        value = Int16.MinValue;
                    typeSamples[type][time + t] = (short)value;

                }
            };

            Action<int, int, SoundDataType> sampleWipe = (timeFrom, timeTo, type) =>
            {
                for (int t = timeFrom; t < timeTo; t++)
                    typeSamples[type][t] = 0;
            };

            //生成每个音效的track
            foreach (var soundTiming in waitToBePlayed)
            {
                var startIndex = (int)(soundTiming.time * freq) * 2; //乘2因为有两个channel
                if (soundTiming.hasAnswer)
                {
                    sampleWrite(startIndex, SoundDataType.Answer);
                }
                if (soundTiming.hasJudge)
                {
                    sampleWrite(startIndex, SoundDataType.Judge);
                }
                if (soundTiming.hasJudgeBreak)
                {
                    sampleWrite(startIndex, SoundDataType.JudgeBreak);
                }
                if (soundTiming.hasJudgeEx)
                {
                    sampleWrite(startIndex, SoundDataType.JudgeEX);
                }
                if (soundTiming.hasBreak)
                {
                    // Reach for the Stars.ogg
                    sampleAdd(startIndex, SoundDataType.Break);
                }
                if (soundTiming.hasHanabi)
                {
                    sampleWrite(startIndex, SoundDataType.Hanabi);
                }
                if (soundTiming.hasTouchHold)
                {
                    // no need to "CutNow" as HoldEnd did the work.
                    sampleWrite(startIndex, SoundDataType.TouchHold);
                    trackOps.Add(new SoundDataRange(SoundDataType.TouchHold, startIndex, holdRiserBank.RawSize));
                }
                if (soundTiming.hasTouchHoldEnd)
                {
                    //不覆盖整个track，只覆盖可能有的部分
                    SoundDataRange lastTouchHoldOp = trackOps.FindLast((trackOp) => trackOp.Type == SoundDataType.TouchHold);
                    sampleWipe(startIndex, (int)lastTouchHoldOp.To, SoundDataType.TouchHold);
                    continue;
                }
                if (soundTiming.hasSlide)
                {
                    sampleWrite(startIndex, SoundDataType.Slide);
                }
                if (soundTiming.hasTouch)
                {
                    sampleWrite(startIndex, SoundDataType.Touch);
                }
                if (soundTiming.hasBreakSlideStart)
                {
                    sampleWrite(startIndex, SoundDataType.BreakSlideStart);
                }
                if (soundTiming.hasBreakSlide)
                {
                    sampleWrite(startIndex, SoundDataType.BreakSlide);
                }
                if (soundTiming.hasJudgeBreakSlide)
                {
                    sampleWrite(startIndex, SoundDataType.JudgeBreakSlide);
                }
                if (soundTiming.hasAllPerfect)
                {
                    sampleWrite(startIndex, SoundDataType.AllPerfect);
                    sampleWrite(startIndex, SoundDataType.FullComboFanfare);
                }
                if (soundTiming.hasClock)
                {
                    sampleWrite(startIndex, SoundDataType.Clock);
                }
            }

            //获取原来实时播放时候的音量
            
            float bgmVol = 1f, answerVol = 1f, judgeVol = 1f, judgeExVol = 1f,
                hanabiVol = 1f, touchVol = 1f, slideVol = 1f, breakVol = 1f, breakSlideVol = 1f;
            Bass.BASS_ChannelGetAttribute(bgmStream, BASSAttribute.BASS_ATTRIB_VOL, ref bgmVol);
            Bass.BASS_ChannelGetAttribute(answerStream, BASSAttribute.BASS_ATTRIB_VOL, ref answerVol);
            Bass.BASS_ChannelGetAttribute(judgeStream, BASSAttribute.BASS_ATTRIB_VOL, ref judgeVol);
            Bass.BASS_ChannelGetAttribute(breakStream, BASSAttribute.BASS_ATTRIB_VOL, ref breakVol);
            Bass.BASS_ChannelGetAttribute(breakSlideStream, BASSAttribute.BASS_ATTRIB_VOL, ref breakSlideVol);
            Bass.BASS_ChannelGetAttribute(slideStream, BASSAttribute.BASS_ATTRIB_VOL, ref slideVol);
            Bass.BASS_ChannelGetAttribute(judgeExStream, BASSAttribute.BASS_ATTRIB_VOL, ref judgeExVol);
            Bass.BASS_ChannelGetAttribute(touchStream, BASSAttribute.BASS_ATTRIB_VOL, ref touchVol);
            Bass.BASS_ChannelGetAttribute(hanabiStream, BASSAttribute.BASS_ATTRIB_VOL, ref hanabiVol);

            List<byte> filedata = new List<byte>();
            Int16[] delayEmpty = new Int16[(int)(delaySeconds * freq * 2)];
            List<byte> filehead = CreateWaveFileHeader(bgmBank.Raw.Length * 2 + delayEmpty.Length * 2, 2, freq, 16).ToList();

            //if (trackStartRAW.Length > delayEmpty.Length)
            //    throw new Exception("track_start音效过长,请勿大于5秒");

            for(int i = 0; i < delayEmpty.Length; i++)
            {
                if(i < trackStartBank.Raw.Length)
                    delayEmpty[i] = trackStartBank.Raw[i];
                filehead.AddRange(BitConverter.GetBytes(delayEmpty[i]));
            }

            for (int i = 0; i < sampleCount; i++)
            {
                // Apply BGM Data
                float sampleValue = bgmBank.Raw[i] * bgmVol;
                
                foreach (var sampleTuple in typeSamples)
                {
                    SoundDataType type = sampleTuple.Key;
                    Int16[] track = sampleTuple.Value;

                    switch(type)
                    {
                        case SoundDataType.Answer:
                            sampleValue += track[i] * answerVol;
                            break;
                        case SoundDataType.Judge:
                            sampleValue += track[i] * judgeVol;
                            break;
                        case SoundDataType.JudgeBreak:
                            sampleValue += track[i] * breakVol;
                            break;
                        case SoundDataType.JudgeEX:
                            sampleValue += track[i] * judgeExVol;
                            break;
                        case SoundDataType.Break:
                            sampleValue += track[i] * breakVol * 0.75f;
                            break;
                        case SoundDataType.BreakSlide:
                        case SoundDataType.JudgeBreakSlide:
                            sampleValue += track[i] * breakSlideVol;
                            break;
                        case SoundDataType.Hanabi:
                        case SoundDataType.TouchHold:
                            sampleValue += track[i] * hanabiVol;
                            break;
                        case SoundDataType.Slide:
                        case SoundDataType.BreakSlideStart:
                            sampleValue += track[i] * slideVol;
                            break;
                        case SoundDataType.Touch:
                            sampleValue += track[i] * touchVol;
                            break;
                        case SoundDataType.AllPerfect:
                        case SoundDataType.FullComboFanfare:
                        case SoundDataType.Clock:
                            sampleValue += track[i] * bgmVol;
                            break;
                    }
                }
                long value = (long)sampleValue;
                if (value > Int16.MaxValue)
                    value = Int16.MaxValue;
                if (value < Int16.MinValue)
                    value = Int16.MinValue;
                filedata.AddRange(BitConverter.GetBytes((Int16)value));
            }
            filehead.AddRange(filedata);
            File.WriteAllBytes(maidataDir + "/out.wav", filehead.ToArray());

            typeSamples.Clear();
            bgmBank.Free();
            comparableBanks.Values.ToList().ForEach((otherBank) => {
                if (otherBank.Temp)
                {
                    File.Delete(otherBank.FilePath);
                }
                otherBank.Free();
            });
        }

        /// <summary>
        /// 创建WAV音频文件头信息,爱来自cnblogs:https://www.cnblogs.com/CUIT-DX037/p/14070754.html
        /// </summary>
        /// <param name="data_Len">音频数据长度</param>
        /// <param name="data_SoundCH">音频声道数</param>
        /// <param name="data_Sample">采样率，常见有：11025、22050、44100等</param>
        /// <param name="data_SamplingBits">采样位数，常见有：4、8、12、16、24、32</param>
        /// <returns></returns>
        private static byte[] CreateWaveFileHeader(int data_Len, int data_SoundCH, int data_Sample, int data_SamplingBits)
        {
            // WAV音频文件头信息
            List<byte> WAV_HeaderInfo = new List<byte>();  // 长度应该是44个字节
            WAV_HeaderInfo.AddRange(Encoding.ASCII.GetBytes("RIFF"));           // 4个字节：固定格式，“RIFF”对应的ASCII码，表明这个文件是有效的 "资源互换文件格式（Resources lnterchange File Format）"
            WAV_HeaderInfo.AddRange(BitConverter.GetBytes(data_Len + 44 - 8));  // 4个字节：总长度-8字节，表明从此后面所有的数据长度，小端模式存储数据
            WAV_HeaderInfo.AddRange(Encoding.ASCII.GetBytes("WAVE"));           // 4个字节：固定格式，“WAVE”对应的ASCII码，表明这个文件的格式是WAV
            WAV_HeaderInfo.AddRange(Encoding.ASCII.GetBytes("fmt "));           // 4个字节：固定格式，“fmt ”(有一个空格)对应的ASCII码，它是一个格式块标识
            WAV_HeaderInfo.AddRange(BitConverter.GetBytes(16));                 // 4个字节：fmt的数据块的长度（如果没有其他附加信息，通常为16），小端模式存储数据
            var fmt_Struct = new
            {
                PCM_Code = (short)1,                  // 4B，编码格式代码：常见WAV文件采用PCM脉冲编码调制格式，通常为1。
                SoundChannel = (short)data_SoundCH,   // 2B，声道数
                SampleRate = (int)data_Sample,        // 4B，没个通道的采样率：常见有：11025、22050、44100等
                BytesPerSec = (int)(data_SamplingBits * data_Sample * data_SoundCH / 8),  // 4B，数据传输速率 = 声道数×采样频率×每样本的数据位数/8。播放软件利用此值可以估计缓冲区的大小。
                BlockAlign = (short)(data_SamplingBits * data_SoundCH / 8),               // 2B，采样帧大小 = 声道数×每样本的数据位数/8。
                SamplingBits = (short)data_SamplingBits,     // 4B，每个采样值（采样本）的位数，常见有：4、8、12、16、24、32
            };
            // 依次写入fmt数据块的数据（默认长度为16）
            WAV_HeaderInfo.AddRange(BitConverter.GetBytes(fmt_Struct.PCM_Code));
            WAV_HeaderInfo.AddRange(BitConverter.GetBytes(fmt_Struct.SoundChannel));
            WAV_HeaderInfo.AddRange(BitConverter.GetBytes(fmt_Struct.SampleRate));
            WAV_HeaderInfo.AddRange(BitConverter.GetBytes(fmt_Struct.BytesPerSec));
            WAV_HeaderInfo.AddRange(BitConverter.GetBytes(fmt_Struct.BlockAlign));
            WAV_HeaderInfo.AddRange(BitConverter.GetBytes(fmt_Struct.SamplingBits));
            /* 还 可以继续写入其他的扩展信息，那么fmt的长度计算要增加。*/

            WAV_HeaderInfo.AddRange(Encoding.ASCII.GetBytes("data"));             // 4个字节：固定格式，“data”对应的ASCII码
            WAV_HeaderInfo.AddRange(BitConverter.GetBytes(data_Len));             // 4个字节：正式音频数据的长度。数据使用小端模式存放，如果是多声道，则声道数据交替存放。
            /* 到这里文件头信息填写完成，通常情况下共44个字节*/
            return WAV_HeaderInfo.ToArray();
        }

        private void WaveStopMonitorUpdate()
        {
            // 监控是否应当停止
            if (!isPlan2Stop &&
                isPlaying &&
                Bass.BASS_ChannelIsActive(bgmStream) == BASSActive.BASS_ACTIVE_STOPPED)
            {
                isPlan2Stop = true;
                if (extraTime4AllPerfect < 0)
                {
                    // 足够播完 直接停止
                    Dispatcher.Invoke(() => { ToggleStop(); });
                }
                else
                {
                    // 不够播完 等待后停止
                    Timer stopPlayingTimer = new Timer((int)(extraTime4AllPerfect * 1000))
                    {
                        AutoReset = false
                    };
                    stopPlayingTimer.Elapsed += (object sender, ElapsedEventArgs e) =>
                    {
                        Dispatcher.Invoke(() =>
                        {
                            ToggleStop();
                        });
                    };
                    stopPlayingTimer.Start();
                }
            }
        }
    }
}
