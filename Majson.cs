using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MajdataEdit
{
    class Majson
    {
        public List<SimaiTimingPoint> timingList = new List<SimaiTimingPoint>();
    }

    class EditRequestjson
    {
        public EditorControlMethod control;
        public float startTime;
        public long startAt;
        public string jsonPath;
    }

    enum EditorControlMethod
    {
        Start,Stop
    }
}
