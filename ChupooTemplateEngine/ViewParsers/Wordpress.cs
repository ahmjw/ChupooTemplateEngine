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

        private void CopyResources()
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

        public override void LoopViews(string path)
        {
            CopyResources();

            string[] dirs = Directory.GetDirectories(path);
            foreach (string dir in dirs)
            {
                DirectoryInfo dinfo = new DirectoryInfo(dir);
                if (dinfo.Name[0] != '@') continue;
                string file = dir + "\\main.html";
                if (File.Exists(file))
                {
                    string path_stage = file.Replace(Directories.Current, "").Substring(1).Replace("\\main.html", "");
                    Parse(path_stage, path_stage);
                }
                Directories.Current = Directories.View;
                ClearAll();
            }

            string[] files = Directory.GetFiles(path);
            foreach (string file in files)
            {
                FileInfo finfo = new FileInfo(file);
                string path_stage = finfo.Name.Replace(finfo.Extension, "");
                Wordpress viewParser = new Wordpress();
                viewParser.Parse(path_stage, path_stage);

                Directories.Current = Directories.View;
                ClearAll();
            }
        }

        public override void Parse(string route, string dest)
        {
            string asset_level = GetAssetLeveling(route);
            string path = Directories.View + route + ".html";
            if (!File.Exists(path))
            {
                path = Directories.View + "@" + route + "\\main.html";
            }
            Match matched = Regex.Match(route, @"^(.*?)\/?_[a-zA-Z0-9_-]+$");
            if (matched.Success)
            {
                if (CurrentCommand == CommandType.FILE_SYSTEM_WATCHER)
                {
                    string dir_route = Regex.Replace(path.Replace(Directories.View, ""), @"^(.*?)[a-zA-Z0-9_-]+\.html$", "$1");
                    RenderDirectory(dir_route);
                }
                else
                    MessageController.Show("Skip file " + route + ".html");
            }
            else if (File.Exists(path))
            {
                view_content = File.ReadAllText(path);

                NestedModuleParser np = new NestedModuleParser();
                view_content = np.ParseText("", route, view_content);

                view_content = ReplaceAssetUrlText(view_content, "./", "dev/views/@" + route + "/");

                LibParser lp = new LibParser();
                view_content = lp.Parse(route, view_content);

                ModuleParser mp = new ModuleParser();
                view_content = mp.Parse(view_content);

                matched = Regex.Match(view_content, @"<c\.config\slayout=""(.+)?""(?:\s*\/)?>(?:<\/c\.config>)?");
                if (matched.Success)
                {
                    cfg_layout_name = matched.Groups[1].Value;
                    view_content = SubsituteString(view_content, matched.Index, matched.Length, "");
                }
                else
                    cfg_layout_name = "page";

                string c_dir = Directories.View + "@" + route;
                if (Directory.Exists(c_dir))
                    view_content = LoadPartialView(view_content, null, "@" + route);
                else
                    view_content = LoadPartialView(view_content, null);

                view_content = RenderPartialCss(c_dir, view_content);
                RenderPartialAssets(route, Directories.View, view_content);

                string data_path = Directories.ViewDataJson + route + ".json";
                if (File.Exists(data_path))
                {
                    MessageController.Show("Rendering " + route + ".html JSON data ...");
                    string json_str = File.ReadAllText(data_path);
                    JObject data = JObject.Parse(json_str);
                    view_content = ReplaceFormattedDataText(view_content, data);
                }
                view_content = ReplaceLinkUrlText(view_content, asset_level);
                LayoutParsers.Wordpress layoutParser = new LayoutParsers.Wordpress();
                layoutParser.Parse(dest, asset_level);
            }
            else
            {
                MessageController.Show("View file is not found: " + route + ".html");
            }
        }
    }
}
