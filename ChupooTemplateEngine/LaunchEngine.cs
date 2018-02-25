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

        public void Run(LaunchTypeEnum launchType)
        {
            Parser.ClearAll();
            LaunchType = launchType;
            Asset.ClearAssets();
            Directory.CreateDirectory(Directories.PublicAsset);
            Directory.CreateDirectory(Directories.PublicAsset + "\\local");
            Directories.Current = Directories.View;
            RenderDirectoryRecursively(Directories.View, "");
            LaunchAssets(Directories.Asset);
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

        private void RenderDirectoryRecursively(string path, string asset_level)
        {
            string[] dirs = Directory.GetDirectories(path);
            foreach (string dir in dirs)
            {
                DirectoryInfo dinfo = new DirectoryInfo(dir);
                if (dinfo.Name[0] != '@') continue;
                string file = dir + "\\main.html";
                if (File.Exists(file))
                {
                    string path_stage = file.Replace(Directories.Current, "").Substring(1).Replace("\\main.html", "");
                    ViewParser viewParser = null;
                    switch (LaunchType)
                    {
                        case LaunchTypeEnum.HTML_TEMPLATE:
                            viewParser = new HtmlTemplate();
                            viewParser.Parse(path_stage, path_stage);
                            break;
                        case LaunchTypeEnum.WORDPRESS:
                            viewParser = new Wordpress();
                            viewParser.Parse(path_stage, path_stage);
                            LayoutParsers.Wordpress.CreateFunctionsFile();
                            break;
                    }
                }
                Directories.Current = Directories.View;
            }
        }
    }
}
