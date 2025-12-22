using System.Reflection;
using System.IO;
using ScePSP.Compat;

namespace ScePSP.Core
{
    public static class ApplicationPaths
    {
        public static string ExecutablePath => Assembly.GetEntryAssembly().Location;

        private static string _MemoryStickRootFolder;

        private static string _Flash0FilePath;

        public static string MemoryStickRootFolder
        {
            get
            {
                if (_MemoryStickRootFolder == null)
                {
                    _MemoryStickRootFolder = Path.GetDirectoryName(Application.ExecutablePath) + "/memstick";

                    try
                    {
                        Directory.CreateDirectory(_MemoryStickRootFolder);
                    }
                    catch
                    {
                    }
                }
                return _MemoryStickRootFolder;
            }
        }

        public static string Flash0FilePath
        {
            get
            {
                if (_Flash0FilePath == null)
                {
                    _Flash0FilePath = Path.GetDirectoryName(Application.ExecutablePath) + "/assert/flash0.zip";

                    try
                    {
                        Directory.CreateDirectory(_Flash0FilePath);
                    }
                    catch
                    {
                    }
                }
                return _Flash0FilePath;
            }
        }
    }
}