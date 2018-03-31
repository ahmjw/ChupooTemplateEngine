using ChupooTemplateEngine.ViewParsers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChupooTemplateEngine
{
    class LaunchEngine
    {
        private string[] asset_exts = { ".js", ".css", ".ico", ".png", ".jpeg", ".jpg", ".jpeg", ".bmp", ".svg" };

        public static LaunchTypeEnum LaunchType { set; get; }

        public enum LaunchTypeEnum
        {
            HTML_TEMPLATE,
            WORDPRESS
        }

        public static bool IsCodeOnly { set; get; }

        public void Run(LaunchTypeEnum launchType)
        {
            Parser.ClearAll();
            LaunchType = launchType;
            // Preparation
            Asset.ClearAssets();
            MessageController.Show("Creating necessary directories ...");
            Directory.CreateDirectory(Directories.PublicAsset);
            Directory.CreateDirectory(Directories.PublicAsset + "\\local");

            Directories.Current = Directories.View;
            ViewParser viewParser = GetViewParser();
            if (launchType == LaunchTypeEnum.WORDPRESS)
            {
                MessageController.Show("Reading configuration file ...");
                LaunchingConfig lc = new LaunchingConfig();
                lc.Load("wordpress");
                if (lc.Data.Contains("lib"))
                {
                    string lib_dir = Directories.LaunchLib + lc.Data["lib"];
                    if (Directory.Exists(lib_dir))
                    {
                        MessageController.Show("Copying library ...");
                        Program.CopyDirectory(lib_dir, Directories.Public);
                    }
                }
            }
            viewParser.LoopViews(Directories.View);

            LaunchAssets(Directories.Asset);
            Directories.Current = null;
        }

        public void Run(string route, LaunchTypeEnum launchType)
        {
            Parser.ClearAll();
            LaunchType = launchType;
            Asset.ClearAssets();
            Directory.CreateDirectory(Directories.PublicAsset);
            Directory.CreateDirectory(Directories.PublicAsset + "\\local");
            Directories.Current = Directories.View;
            ViewParser viewParser = GetViewParser();
            viewParser.Parse(route, route);
            Directories.Current = null;
        }

        private void LaunchAssets(string path)
        {
            string[] dirs = Directory.GetDirectories(path);
            string path_stage;
            foreach (string dir in dirs)
            {
                path_stage = dir.Replace(Directories.Asset, "");


                if (!Directory.Exists(Directories.PublicAsset + path_stage))
                    Directory.CreateDirectory(Directories.PublicAsset + path_stage);

                string[] subdirs = Directory.GetDirectories(path);
                if (subdirs.Length > 0)
                {
                    LaunchAssets(dir);
                }
            }
            string[] files = Directory.GetFiles(path);
            path_stage = path.Replace(Directories.Asset, "");
            foreach (string file in files)
            {
                FileInfo finfo = new FileInfo(file);
                if (!asset_exts.Any(finfo.Extension.Equals)) continue;
                File.Copy(file, Directories.PublicAsset + path_stage + @"\" + finfo.Name, true);
            }
        }

        private ViewParser GetViewParser()
        {
            ViewParser viewParser = null;
            switch (LaunchType)
            {
                case LaunchTypeEnum.HTML_TEMPLATE:
                    viewParser = new HtmlTemplate();
                    break;
                case LaunchTypeEnum.WORDPRESS:
                    viewParser = new Wordpress();
                    break;
            }
            return viewParser;
        }
    }
}
