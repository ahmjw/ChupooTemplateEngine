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
    class ChupooWp : ViewParser
    {
        public void LoopLayouts(string path)
        {
            string[] files = Directory.GetFiles(path);
            foreach (string item in files)
            {
                FileInfo finfo = new FileInfo(item);
                string dest = finfo.Name.Replace(finfo.Extension, "");
                LayoutParsers.ChupooWp layoutParser = new LayoutParsers.ChupooWp();
                layoutParser.Parse(dest, "./");
            }
        }

        public override void LoopViews(string path)
        {
            Program.CopyDirectory(Directories.Resources + "\\launch_templates\\chupoowp\\public", Directories.Public);

            string[] dirs = Directory.GetDirectories(path);
            foreach (string dir in dirs)
            {
                DirectoryInfo dinfo = new DirectoryInfo(dir);
                if (dinfo.Name[0] != '@') continue;
                string file = dir + "\\main.html";
                if (File.Exists(file))
                {
                    string path_stage = file.Replace(Directories.Current, "").Substring(1).Replace("\\main.html", "");
                    Console.WriteLine(path_stage);
                    Parse(path_stage, path_stage);
                }

                Directories.Current = Directories.View;
                ClearAll();
            }

            LoopLayouts(Directories.Layout);
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
                    Console.WriteLine("Skip file " + route + ".html");
            }
            else if (File.Exists(path))
            {
                string content = File.ReadAllText(path);

                LibParser lp = new LibParser();
                content = lp.Parse(route, content);

                ModuleParser mp = new ModuleParser();
                content = mp.Parse(content);

                matched = Regex.Match(content, @"<c\.config\slayout=""(.+)?""(?:\s*\/)?>(?:<\/c\.config>)?");
                if (matched.Success)
                {
                    cfg_layout_name = matched.Groups[1].Value;
                    content = SubsituteString(content, matched.Index, matched.Length, "");
                }
                else
                    cfg_layout_name = "page";

                string c_dir = Directories.View + "@" + route;
                if (Directory.Exists(c_dir))
                    content = LoadPartialView(content, "@" + route);
                else
                    content = LoadPartialView(content);

                content = RenderPartialCss(c_dir, content);
                RenderPartialAssets(route, Directories.View, content);

                string data_path = Directories.ViewDataJson + route + ".json";
                if (File.Exists(data_path))
                {
                    Console.WriteLine("Rendering " + route + ".html JSON data ...");
                    string json_str = File.ReadAllText(data_path);
                    JObject data = JObject.Parse(json_str);
                    content = ReplaceFormattedDataText(content, data);
                }
                content = ReplaceLinkUrlText(content, asset_level);
                string p_file = Directories.Public + "modules\\views\\" + dest + ".html";
                File.WriteAllText(p_file, content);
                Console.WriteLine("OK: " + dest + ".html");
            }
            else
            {
                Console.WriteLine("View file is not found: " + route + ".html");
            }
        }
    }
}
