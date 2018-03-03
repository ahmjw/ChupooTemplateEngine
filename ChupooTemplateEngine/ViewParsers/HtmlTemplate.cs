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
    class HtmlTemplate : ViewParser
    {
        public override void LoopViews(string path)
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
                HtmlTemplate viewParser = new HtmlTemplate();
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
                    Console.WriteLine("Skip file " + route + ".html");
            }
            else if (File.Exists(path))
            {
                view_content = File.ReadAllText(path);

                // Cloning page
                CloningPageParser cpp = new CloningPageParser();
                CloningPage cp = cpp.Parse(route, view_content);
                if (cp.Data.Count > 0)
                {
                    foreach (JToken datum in cp.Data)
                    {
                        JObject page_data = (JObject)datum;
                        view_content = ReplaceFormattedDataText(cp.Content, page_data);
                        ParseFile(route, cpp.GetName(page_data), asset_level, matched, cp.Content, page_data);
                    }
                }
                else
                {
                    ParseFile(route, dest, asset_level, matched, view_content);
                }
            }
            else
            {
                Console.WriteLine("View file is not found: " + route + ".html");
            }
        }

        private void ParseFile(string route, string dest, string asset_level, Match matched, string content, JObject page_data = null)
        {
            NestedModuleParser np = new NestedModuleParser();
            content = np.ParseText("", route, content);

            content = ReplaceAssetUrlText(content, "./", "dev/views/@" + route + "/");

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

            // Replace data
            if (page_data == null)
            {
                string data_path = Directories.ViewDataJson + route + ".json";
                if (File.Exists(data_path))
                {
                    Console.WriteLine("Rendering " + route + ".html JSON data ...");
                    string json_str = File.ReadAllText(data_path);
                    page_data = JObject.Parse(json_str);
                    content = ReplaceFormattedDataText(content, page_data);
                }
            }
            else
            {
                content = ReplaceFormattedDataText(content, page_data);
            }

            view_content = ReplaceLinkUrlText(content, asset_level);
            LayoutParsers.HtmlTemplate layoutParser = new LayoutParsers.HtmlTemplate();
            layoutParser.Parse(route, asset_level, dest);
        }
    }
}
