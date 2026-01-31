using ScePSP.Cpu;
using ScePSPUtils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;

namespace ScePSP.Core
{
    public class ControllerConfig
    {
        public string DigitalUp = "Up";
        public string DigitalDown = "Down";
        public string DigitalLeft = "Left";
        public string DigitalRight = "Right";

        public string AnalogUp = "I";
        public string AnalogDown = "K";
        public string AnalogLeft = "J";
        public string AnalogRight = "L";

        public string SelectButton = "Space";
        public string StartButton = "Return";

        public string SquareButton = "A";
        public string CircleButton = "D";
        public string TriangleButton = "W";
        public string CrossButton = "S";

        public string LeftTriggerButton = "Q";
        public string RightTriggerButton = "E";
    }

    public class PspStoredConfig
    {
        public static Logger Logger = Logger.GetLogger("Config");

        public DateTime LastCheckedTime;

        public bool LimitVerticalSync = true;

        public int MaxTexCache = 500;

        public int TexMinMinutes = 1;

        public int TexMinHits = 50;

        public int RenderScale = 3;

        public int AuPlayBackMs = 16;

        public int AuDecodeBuf = 64;

        public int AvDecodeBuf = 1024;

        public bool H264Enable = false;

        public bool TrackHLECalls = false;

        public bool UseFastMemory = true;

        public bool AstOptimizations = true;

        public ControllerConfig ControllerConfig = new ControllerConfig();

        public List<string> RecentFiles = new List<string>();

        public string IsosPath = null;

        public bool ScaleTextures = false;

        public string FromPos;

        public int TexScaleType = -1;

        public int TexScaleX = 2;

        public bool FastMemoryReader = false;

        public bool DepthOpt = false;

        public bool TailCall = false;

        public bool TickCall = false;

        public bool ScanCreatingFunctions = true;

        public int ScanFunctionsDepth = 1;

        public CpuConfig CpuConfig = new CpuConfig();

        #region Serializing
        private static XmlSerializer Serializer;

        private PspStoredConfig()
        {
        }

        private static string ConfigFilePath
        {
            get
            {
                return ApplicationPaths.AssertFolder + "/Config.xml";
            }
        }

        private static object Lock = new object();

        public static PspStoredConfig Load()
        {
            lock (Lock)
            {
                try
                {
                    if (Serializer == null)
                    {
                        Serializer = XmlSerializer.FromTypes(new[] { typeof(PspStoredConfig) })[0];
                    }

                    using (var Stream = File.OpenRead(ConfigFilePath))
                    {
                        return (PspStoredConfig)Serializer.Deserialize(Stream);
                    }
                }
                catch (Exception Exception)
                {
                    Logger.Error(Exception);
                    return new PspStoredConfig();
                }
            }
        }

        public void Save()
        {
            try
            {
                lock (Lock)
                {
                    using (var Stream = File.Open(ConfigFilePath, FileMode.Create, FileAccess.Write))
                    {
                        Serializer.Serialize(Stream, this);
                    }
                }
            }
            catch (Exception Exception)
            {
                Logger.Error(Exception);
            }
        }
        #endregion
    }
}
