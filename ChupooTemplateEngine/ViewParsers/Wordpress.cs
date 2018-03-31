using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static ChupooTemplateEngine.Command;

namespace ChupooTemplateEngine.ViewParsers
{
    class Wordpress : ViewParser
    {
        public void CopyResources()
        {
            string fname = "style.css";
            string dst = Directories.Public + fname;
            string wp_dir = Directories.Dev + "launch\\wordpress\\";
            if (!Directory.Exists(wp_dir))
            {
                MessageController.Show("Warning: Wordpress launching directory is not available");
                return;
            }

            string src = wp_dir + fname;
            if (!File.Exists(dst))
            {
                File.Copy(src, dst);
            }

            fname = "functions.php";
            dst = Directories.Public + fname;
            src = wp_dir + fname;
            if (!File.Exists(dst))
            {
                File.Copy(src, dst);
            }
        }

        protected override void OnViewParsed(string route, string dest, string asset_level)
        {
            LayoutParsers.Wordpress layoutParser = new LayoutParsers.Wordpress();
            layoutParser.Parse(route, Extension, asset_level, dest);
        }
    }
}
