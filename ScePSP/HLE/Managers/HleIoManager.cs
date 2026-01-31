using ScePSP.Hle.Vfs;
using ScePSPUtils.Extensions;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace ScePSP.Hle.Managers
{
    public struct ParsePathInfo
    {
        public HleIoDrvFileArg HleIoDrvFileArg;

        public string LocalPath;

        public IHleIoDriver HleIoDriver
        {
            get
            {
                return HleIoDrvFileArg.HleIoDriver;
            }
        }
    }

    public class HleIoWrapper
    {
        HleIoManager HleIoManager;

        public HleIoWrapper(HleIoManager HleIoManager)
        {
            this.HleIoManager = HleIoManager;
        }

        public void Mkdir(string Path, SceMode SceMode)
        {
            var PathInfo = HleIoManager.ParsePath(Path);
            PathInfo.HleIoDriver.IoMkdir(PathInfo.HleIoDrvFileArg, PathInfo.LocalPath, SceMode);
        }

        public FileHandle Open(string FileName, HleIoFlags Flags, SceMode Mode)
        {
            var PathInfo = HleIoManager.ParsePath(FileName);
            PathInfo.HleIoDrvFileArg.HleIoDriver.IoOpen(PathInfo.HleIoDrvFileArg, PathInfo.LocalPath, Flags, Mode);
            //return new FileHandle(this.HleIoManager, PathInfo.HleIoDrvFileArg);
            return new FileHandle(PathInfo.HleIoDrvFileArg);
        }

        public byte[] ReadBytes(string FileName)
        {
            using (var File = Open(FileName, HleIoFlags.Read, SceMode.File))
            {
                return File.ReadAll();
            }
        }

        public void WriteBytes(string FileName, byte[] Data)
        {
            using (var File = Open(FileName, HleIoFlags.Create | HleIoFlags.Write | HleIoFlags.Truncate, SceMode.All))
            {
                File.WriteBytes(Data);
            }
        }
    }

    public class HleIoManager
    {
        protected readonly Dictionary<string, IHleIoDriver> Drivers = new Dictionary<string, IHleIoDriver>();

        public readonly HleUidPoolSpecial<HleIoDrvFileArg, SceUID> HleIoDrvFileArgPool = new HleUidPoolSpecial<HleIoDrvFileArg, SceUID>();

        public HleIoWrapper HleIoWrapper;

        public HleIoManager()
        {
            HleIoWrapper = new HleIoWrapper(this);
        }

        public ParsePathInfo ParsePath(string FullPath)
        {
            if (FullPath.IndexOf(':') == -1)
            {
                FullPath = CurrentDirectoryPath + "/" + FullPath;
            }
            //Console.Error.WriteLine("FullPath: {0}", FullPath);
            var Match = new Regex(@"^([a-zA-Z]+)(\d*):(.*)$").Match(FullPath);
            var DriverName = Match.Groups[1].Value.ToLower() + ":";
            int FileSystemNumber = 0;
            IHleIoDriver HleIoDriver = null;
            Int32.TryParse(Match.Groups[2].Value, out FileSystemNumber);
            if (!Drivers.TryGetValue(DriverName, out HleIoDriver))
            {
                foreach (var Driver in Drivers)
                {
                    Console.WriteLine("Available Driver: '{0}'", Driver.Key);
                }
                throw (new KeyNotFoundException("Can't find HleIoDriver '" + DriverName + "'"));
            }

            return new ParsePathInfo()
            {
                HleIoDrvFileArg = new HleIoDrvFileArg(DriverName, HleIoDriver, FileSystemNumber, null),
                LocalPath = Match.Groups[3].Value.Replace('\\', '/'),
            };
        }

        public IHleIoDriver GetDriver(string Name)
        {
            return Drivers[Name];
        }

        public void SetDriver(string Name, IHleIoDriver Driver)
        {
            //Console.Error.WriteLine("SetDriver: {0}", Name);
            //Drivers.Add(Name, Driver);
            Drivers[Name] = Driver;
            try
            {
                Driver.IoInit();
            }
            catch (Exception Exception)
            {
                Console.Error.WriteLine(Exception);
            }
        }

        public void RemoveDriver(string Name)
        {
            try
            {
                Drivers[Name].IoExit();
                Drivers.Remove(Name);
            }
            catch (Exception Exception)
            {
                Console.Error.WriteLine(Exception);
            }
        }

        public ParsePathInfo ParseDeviceName(string DeviceName)
        {
            var Match = new Regex(@"^([a-zA-Z]+)(\d*):$").Match(DeviceName);
            int FileSystemNumber = 0;
            Int32.TryParse(Match.Groups[2].Value, out FileSystemNumber);

            var BaseDeviceName = Match.Groups[1].Value + ":";

            if (!Drivers.ContainsKey(BaseDeviceName))
            {
                throw (new NotImplementedException(String.Format("Unknown device '{0}'", BaseDeviceName)));
            }

            return new ParsePathInfo()
            {
                HleIoDrvFileArg = new HleIoDrvFileArg(BaseDeviceName, Drivers[BaseDeviceName], FileSystemNumber),
                LocalPath = "",
            };
        }

        public string CurrentDirectoryPath = "";

        public void Chdir(string DirectoryPath)
        {
            CurrentDirectoryPath = DirectoryPath;
        }
    }
}
