using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace ScePSP.Core
{
    public static class ApplicationPaths
    {
        public static string ExecutablePath
        {
            get
            {
                return Assembly.GetEntryAssembly().Location;
            }
        }

        private static string _MemoryStickRootFolder;

        private static string _AssertFolder;

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

        public static string AssertFolder
        {
            get
            {
                if (_AssertFolder == null)
                {
                    _AssertFolder = Path.GetDirectoryName(Application.ExecutablePath) + "/assert";

                    try
                    {
                        Directory.CreateDirectory(_AssertFolder);
                    }
                    catch
                    {
                    }
                }
                return _AssertFolder;
            }
        }
    }
}
