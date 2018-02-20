using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChupooTemplateEngine
{
    class Asset
    {
        public static void ClearAssets()
        {
            if (!Directory.Exists(Directories.Public))
            {
                return;
            }
            string[] public_dirs = Directory.GetDirectories(Directories.Public);
            foreach (string dir in public_dirs)
            {
                DirectoryInfo dinfo = new DirectoryInfo(dir);
                if (!Directory.Exists(Directories.View + dinfo.Name))
                {
                    Directory.Delete(dir, true);
                }
            }
            string[] public_files = Directory.GetFiles(Directories.Public);
            foreach (string file in public_files)
            {
                FileInfo finfo = new FileInfo(file);
                if (!File.Exists(Directories.View + finfo.Name))
                {
                    File.Delete(file);
                }
            }
        }
    }
}
