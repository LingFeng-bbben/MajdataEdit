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
            public double time;

            public SoundEffectTiming(double _time, bool _hasAnswer = false, bool _hasJudge = false, bool _hasJudgeBreak = false,
                                     bool _hasBreak = false, bool _hasTouch = false, bool _hasHanabi = false,
                                     bool _hasJudgeEx = false, bool _hasTouchHold = false, bool _hasSlide = false,
                                     bool _hasTouchHoldEnd = false, bool _hasAllPerfect = false, bool _hasClock = false)
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
            }
        }
    }
}
