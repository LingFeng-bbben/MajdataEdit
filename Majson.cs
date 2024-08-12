namespace MajdataEdit;

internal class Majson
{
    public string artist = "default";
    public string designer = "default";
    public string difficulty = "EZ";
    public int diffNum = 0;
    public string level = "1";
    public List<SimaiTimingPoint> timingList = new();
    public string title = "default";
}

internal class EditRequestjson
{
    public float audioSpeed;
    public float backgroundCover;
    public EditorComboIndicator comboStatusType;
    public EditorPlayMethod editorPlayMethod;
    public EditorControlMethod control;
    public string? jsonPath;
    public float noteSpeed;
    public long startAt;
    public float startTime;
    public float touchSpeed;
    public bool smoothSlideAnime;
}

public enum EditorPlayMethod
{
    Classic,DJAuto,Random,Disabled
}

public enum EditorComboIndicator
{
    None,

    // List of viable indicators that won't be a static content.
    // ScoreBorder, AchievementMaxDown, ScoreDownDeluxe are static.
    Combo,
    ScoreClassic,
    AchievementClassic,
    AchievementDownClassic,
    AchievementDeluxe = 11,
    AchievementDownDeluxe,
    ScoreDeluxe,

    // Please prefix custom indicator with C
    CScoreDedeluxe = 101,
    CScoreDownDedeluxe,
    MAX
}

internal enum EditorControlMethod
{
    Start,
    Stop,
    OpStart,
    Pause,
    Continue,
    Record
}

//this setting is per maidata
internal class MajSetting
{
    public float Answer_Level = 0.7f;

    public float BGM_Level = 0.7f;
    public float Break_Level = 0.7f;
    public float Break_Slide_Level = 0.7f;
    public float Ex_Level = 0.7f;
    public float Hanabi_Level = 0.7f;
    public float Judge_Level = 0.7f;
    public int lastEditDiff;
    public double lastEditTime;
    public float Slide_Level = 0.7f;
    public float Touch_Level = 0.7f;
}

//this setting is global
public class EditorSetting
{
    public bool AutoCheckUpdate = true;
    public float backgroundCover = 0.6f;
    public int ChartRefreshDelay = 1000;
    public EditorComboIndicator comboStatusType = 0;
    public EditorPlayMethod editorPlayMethod;
    public string DecreasePlaybackSpeedKey = "Ctrl+o";
    public float Default_Answer_Level = 0.7f;
    public float Default_BGM_Level = 0.7f;
    public float Default_Break_Level = 0.7f;
    public float Default_Break_Slide_Level = 0.7f;
    public float Default_Ex_Level = 0.7f;
    public float Default_Hanabi_Level = 0.7f;
    public float Default_Judge_Level = 0.7f;
    public float Default_Slide_Level = 0.7f;
    public float Default_Touch_Level = 0.7f;
    public float DefaultSlideAccuracy = 0.2f;
    public float FontSize = 12;
    public string IncreasePlaybackSpeedKey = "Ctrl+p";
    public string Language = "en-US";
    public string Mirror180Key = "Ctrl+l";
    public string Mirror45Key = "Ctrl+OemSemicolon";
    public string MirrorCcw45Key = "Ctrl+OemQuotes";
    public string MirrorLeftRightKey = "Ctrl+j";
    public string MirrorUpDownKey = "Ctrl+k";
    public string PlayPauseKey = "Ctrl+Shift+c";
    public float playSpeed = 7.5f;
    public string PlayStopKey = "Ctrl+Shift+x";
    public int RenderMode = 0; //0=硬件渲染(默认)，1=软件渲染
    public string SaveKey = "Ctrl+s";
    public string SendViewerKey = "Ctrl+Shift+z";
    public float touchSpeed = 7.5f;
    public bool SmoothSlideAnime = false;
}