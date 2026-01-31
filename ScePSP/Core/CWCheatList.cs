using ScePSP.Components.Display;
using ScePSP.Display;
using ScePSP.Memory;
using ScePSPUtils;
using System;
using System.Collections.Generic;

namespace ScePSP.cheats
{
    public class CWCheatList : IContextInitialize
    {
        [Context]
        PspDisplay PspDisplay;

        [Context]
        DisplayConfig DIsplyConfig;

        [Context]
        PspMemory PspMemory;

        public List<CWCheatEntry> CWCheats = new List<CWCheatEntry>();

        void PspEmulator_VBlankEventCall()
        {
            foreach (var CWCheat in CWCheats)
            {
                if (PspMemory != null)
                {
                    CWCheat.Patch(PspMemory);
                }
            }
            //Console.Error.WriteLine("VBlank!");
        }

        void IContextInitialize.Initialize()
        {
            PspDisplay.VBlankEventCall += new Action(PspEmulator_VBlankEventCall);
        }

        private CWCheatList()
        {
        }

        public void dispose()
        {
            PspDisplay.VBlankEventCall -= new Action(PspEmulator_VBlankEventCall);
        }

        public void Add(Queue<uint> Values)
        {
            while (Values.Count > 0)
            {
                var Entry = new CWCheatEntry();
                Entry.Read(Values);
                CWCheats.Add(Entry);
            }
        }

        public void Clear()
        {
            CWCheats.Clear();
        }

        public void ParseCwCheat(string[] Lines)
        {
            CWCheats.Clear();
            var Values = new Queue<uint>();
            foreach (var LineRaw in Lines)
            {
                var Line = LineRaw.Trim();
                if (Line.Substr(0, 1) == ";") continue;
                if (Line.Substr(0, 1) == "#") continue;
                var Parts = Line.Split(' ', '\t');
                foreach (var Part in Parts)
                {
                    if (Part.Substr(0, 2) == "0x")
                    {
                        Values.Enqueue((uint)NumberUtils.ParseIntegerConstant(Part));
                    }
                }
            }
            while (Values.Count > 0)
            {
                var Entry = new CWCheatEntry();
                Entry.Read(Values);
                CWCheats.Add(Entry);
            }
        }
    }
}