using ScePSP.Devices.Display;
using ScePSPUtils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Xml.Serialization;

namespace ScePSP
{
    public class ControllerConfig
    {
        public string DigitalUp = "W";
        public string DigitalDown = "S";
        public string DigitalLeft = "A";
        public string DigitalRight = "D";

        public string AnalogUp = "Up";
        public string AnalogDown = "Down";
        public string AnalogLeft = "Left";
        public string AnalogRight = "Right";

        public string SelectButton = "Space";
        public string StartButton = "Return";

        public string SquareButton = "U";
        public string CircleButton = "I";
        public string TriangleButton = "J";
        public string CrossButton = "K";

        public string LeftTriggerButton = "Q";
        public string RightTriggerButton = "E";
    }

    public class DisplayConfig
    {
        public bool NoticeUnimplementedGpuCommands = true;
        public bool VerticalSynchronization = true;
        public bool Enabled = true;
        public IntPtr WindowHandle = IntPtr.Zero;
        public int Width = PspDisplay.MaxVisibleWidth * 2;
        public int Height = PspDisplay.MaxVisibleHeight * 2;
        public bool H264Enabled = false;
    }

    public static class ApplicationPaths
    {
        public static string ExecutablePath => Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);

        private static string _MemoryStickRootFolder;

        private static string _AssertPath;

        public static string MemoryStickRootFolder
        {
            get
            {
                _MemoryStickRootFolder = ExecutablePath + "/memstick";

                if (!Path.Exists(_MemoryStickRootFolder))
                {
                    Directory.CreateDirectory(_MemoryStickRootFolder);
                }
                return _MemoryStickRootFolder;
            }
        }

        public static string AssertPath
        {
            get
            {
                _AssertPath = ExecutablePath + "/assert";

                if (!Path.Exists(_AssertPath))
                {
                    Directory.CreateDirectory(_AssertPath);
                }
                return _AssertPath;
            }
        }
    }

    public class PspHleRunningConfig
    {
        public string FileNameBase;
        public bool EnableDelayIo = true;
    }

    public class PspStoredConfig
    {
        public static Logger Logger = Logger.GetLogger("Config");

        public DateTime LastCheckedTime;
        public bool LimitVerticalSync = true;
        public int DisplayScale = 2;
        public int RenderScale = 2;
        public bool UseFastMemory = true;
        public bool EnableAstOptimizations = true;
        public ControllerConfig ControllerConfig = new ControllerConfig();
        public List<string> RecentFiles = new List<string>();
        public string IsosPath = null;
        public bool ScaleTextures = false;

        #region Serializing

        private static XmlSerializer Serializer;

        public PspStoredConfig()
        {
        }

        private static string ConfigFilePath => ApplicationPaths.MemoryStickRootFolder + "/Config.xml";

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
                catch// (Exception Exception)
                {
                    //Logger.Error(Exception);
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