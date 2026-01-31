using ScePSP.Hle.Formats;
using ScePSP.Hle.Vfs.Iso;
using ScePSPUtils.Extensions;
using System;
using System.IO;
using System.Windows.Forms;

namespace ScePSP.UI
{
    public partial class FileExtract : Form
    {
        string filepath;
        IsoFile iso;
        HleIoDriverIso hleIoDriver;

        public FileExtract(string isoFile)
        {
            InitializeComponent();

            filepath = isoFile;
            iso = IsoLoader.GetIso(isoFile);
            hleIoDriver = new HleIoDriverIso(iso);
            //foreach (HleIoDirent Dirent in hleIoDriver.ListDir("/"))
            //{
            //    //Console.WriteLine("{0} : {1}", text2, hleIoDriver.GetStat(text2).Size);
            //}
        }

        private void btnExtract_Click(object sender, EventArgs e)
        {
            string childName = EdFile.Text;

            if (childName == "" || childName == "." || childName == "..") return;

            if (childName.IndexOf(":") >= -1)
            {
                childName = childName.Split(':')[1];
            }

            using (Stream stream = hleIoDriver.OpenRead(childName))
            {
                stream.CopyToFile("./" + Path.GetFileNameWithoutExtension(childName));
            }

            Close();
        }

    }
}
