using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MajdataEdit
{
    class Majson
    {
        public string level = "1";
        public string difficulty = "EZ";
        public int diffNum = 0;
        public string title = "default";
        public string artist = "default";
        public string designer = "default";
        public List<SimaiTimingPoint> timingList = new List<SimaiTimingPoint>();
    }

    class EditRequestjson
    {
        public EditorControlMethod control;
        public float startTime;
        public long startAt;
        public string jsonPath;
        public float noteSpeed;
        public float touchSpeed;
        public float backgroundCover;
        public float audioSpeed;
    }

    enum EditorControlMethod
    {
        Start, Stop, OpStart, Pause, Continue
    }

    class MajSetting
    {
        public int lastEditDiff;
        public double lastEditTime;

        public float BGM_Level;
        public float Answer_Level;
        public float Judge_Level;
        public float Slide_Level;
        public float Break_Level;
        public float Ex_Level;
        public float Touch_Level;
        public float Hanabi_Level;
    }

    public class EditorSetting
    {
        public bool AutoCheckUpdate = true;
        public int ChartRefreshDelay = 1000;
        public float DefaultSlideAccuracy = 0.2f;
        public string Language = "zh-CN";
        public string PlayPauseKey = "Ctrl+Shift+c";
        public string PlayStopKey = "Ctrl+Shift+x";
        public string SendViewerKey = "Ctrl+Shift+z";
        public string SaveKey = "Ctrl+s";
        public string IncreasePlaybackSpeedKey = "Ctrl+p";
        public string DecreasePlaybackSpeedKey = "Ctrl+o";
        public float FontSize = 12;
        public int RenderMode = 0;  //0=硬件渲染(默认)，1=软件渲染
        public float playSpeed = 7.0f;
        public float touchSpeed = 7.5f;
        public float backgroundCover = 0.6f;
        public float Default_BGM_Level;
        public float Default_Answer_Level;
        public float Default_Judge_Level;
        public float Default_Slide_Level;
        public float Default_Break_Level;
        public float Default_Ex_Level;
        public float Default_Touch_Level;
        public float Default_Hanabi_Level;
    }
}
