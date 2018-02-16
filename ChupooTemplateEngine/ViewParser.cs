using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static ChupooTemplateEngine.Command;

namespace ChupooTemplateEngine
{
    class ViewParser : Parser
    {
        private string GetAssetLeveling(string route)
        {
            string level = "./";
            int length = route.Split('\\').Length - 1;
            for (int i = 0; i < length; i++)
            {
                level += "../";
            }
            return level;
        }

        public void RenderDirectory(string route)
        {
            string path = Directories.View + route;
            if (!Directory.Exists(path))
            {
                Console.WriteLine("Error: " + route + " directory is not found");
                return;
            }
            string[] files = Directory.GetFiles(path);
            foreach (string file in files)
            {
                FileInfo finfo = new FileInfo(file);
                if (finfo.Name[0] == '_' || finfo.Extension != ".html") continue;
                string path_stage = file.Replace(Directories.View, "").Replace(".html", "");
                Parse(path_stage, path_stage);
            }
        }

        public static void RenderDirectoryRecursively(string path, string asset_level)
        {
            string[] dirs = Directory.GetDirectories(path);
            foreach (string dir in dirs)
            {
                DirectoryInfo dinfo = new DirectoryInfo(dir);
                if (dinfo.Name[0] == '_') continue;
                string path_stage = dir.Replace(Directories.Current, "");
                if (!Directory.Exists(Directories.Public + path_stage))
                    Directory.CreateDirectory(Directories.Public + path_stage);

                string[] subdirs = Directory.GetDirectories(path);
                if (subdirs.Length > 0)
                {
                    string old_asset_level = asset_level;
                    RenderDirectoryRecursively(dir, asset_level + "../");
                    asset_level = old_asset_level;
                }
            }
            string[] files = Directory.GetFiles(path);
            foreach (string file in files)
            {
                FileInfo finfo = new FileInfo(file);
                if (finfo.Name[0] == '_' || finfo.Extension != ".html") continue;
                string path_stage = file.Replace(Directories.Current, "").Replace(".html", "");
                ViewParser viewParser = new ViewParser();
                viewParser.Parse(path_stage, path_stage);
            }
        }

        public void Parse(string route, string dest)
        {
            string asset_level = GetAssetLeveling(route);
            string path = Directories.View + route + ".html";
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
                view_content = ReplaceAssetUrlText(view_content, asset_level, Directories.View);

                matched = Regex.Match(view_content, @"<c\.config\slayout=""(.+)?""(?:\s*\/)?>(?:<\/c\.config>)?");
                if (matched.Success)
                {
                    cfg_layout_name = matched.Groups[1].Value;
                    view_content = SubsituteString(view_content, matched.Index, matched.Length, "");
                }
                else
                    cfg_layout_name = "page";

                string c_dir = Directories.View + "_" + route;
                if (Directory.Exists(c_dir))
                    view_content = LoadPartialView(view_content, "_" + route);
                else
                    view_content = LoadPartialView(view_content);

                view_content = RenderPartialCss(c_dir, view_content);
                RenderPartialAssets(route, Directories.View, view_content);
                view_content = SeparateViewStyle(view_content);
                view_content = SeparateViewScript(view_content);

                string data_path = Directories.ViewDataJson + route + ".json";
                if (File.Exists(data_path))
                {
                    Console.WriteLine("Rendering " + route + ".html JSON data ...");
                    string json_str = File.ReadAllText(data_path);
                    JObject data = JObject.Parse(json_str);
                    view_content = ReplaceFormattedDataText(view_content, data);
                }
                view_content = ReplaceLinkUrlText(view_content, asset_level);
                LayoutParser layoutParser = new LayoutParser();
                layoutParser.ParseLayout(dest, asset_level);
            }
            else
            {
                Console.WriteLine("View file is not found: " + route + ".html");
            }
        }

        private string SeparateViewStyle(string content)
        {
            string pattern;
            MatchCollection matches;
            pattern = @"<link.*?rel=""stylesheet"".*?>";
            matches = Regex.Matches(content, pattern);
            if (matches.Count > 0)
            {
                int newLength = 0;
                foreach (Match match in matches)
                {
                    v_style_file_list.Add(match.Value);
                    content = SubsituteString(content, match.Index + newLength, match.Length, "");
                    newLength += -match.Length;
                }
            }
            pattern = @"<style.*?>[\w\W]*?</style>";
            matches = Regex.Matches(content, pattern);
            if (matches.Count > 0)
            {
                int newLength = 0;
                foreach (Match match in matches)
                {
                    v_style_code_list.Add(match.Value);
                    content = SubsituteString(content, match.Index + newLength, match.Length, "");
                    newLength += -match.Length;
                }
            }
            return content;
        }

        private string SeparateViewScript(string content)
        {
            string pattern;
            MatchCollection matches;
            pattern = @"<script.*?></script>";
            matches = Regex.Matches(content, pattern);
            if (matches.Count > 0)
            {
                int newLength = 0;
                foreach (Match match in matches)
                {
                    v_script_file_list.Add(match.Value);
                    content = SubsituteString(content, match.Index + newLength, match.Length, "");
                    newLength += -match.Length;
                }
            }
            pattern = @"<script.*?>[\w\W]*?</script>";
            matches = Regex.Matches(content, pattern);
            if (matches.Count > 0)
            {
                int newLength = 0;
                foreach (Match match in matches)
                {
                    v_script_code_list.Add(match.Value);
                    content = SubsituteString(content, match.Index + newLength, match.Length, "");
                    newLength += -match.Length;
                }
            }
            return content;
        }

        private string RenderPartialCss(string dir, string view_content)
        {
            Match matched = Regex.Match(view_content, @"<c\.css\shref=""(.*)?""(?:\s*\/)?>(?:<\/c\.css>)?");
            if (matched.Success)
            {
                string css_path = dir + @"\" + matched.Groups[1].Value;

                if (File.Exists(css_path))
                {
                    string css_content = "<style type=\"text/css\">" + File.ReadAllText(css_path) + "</style>";
                    view_content = SubsituteString(view_content, matched.Index, matched.Length, css_content);
                }
                else
                {
                    Console.WriteLine("Warning: CSS file " + matched.Groups[1].Value + " is not found");
                }
            }
            return view_content;
        }

        private string ReplaceFormattedDataText(string content, JObject data)
        {
            string pattern = @"\{\{([^\.][a-zA-Z0-9_-]+)\}\}";
            MatchCollection matches = Regex.Matches(content, pattern);
            if (matches.Count > 0)
            {
                int newLength = 0;
                foreach (Match match in matches)
                {
                    string new_value = data[match.Groups[1].Value] + "";
                    content = SubsituteString(content, match.Index + newLength, match.Length, new_value);
                    newLength += new_value.Length - match.Length;
                }
            }
            return content;
        }

        private string RenderViewComponent(string layout_name, string layout_file, string parent_route)
        {
            FileInfo finfo = new FileInfo(layout_file);
            string content = File.ReadAllText(layout_file);
            content = ReplaceAssetUrlText(content, "./", finfo.DirectoryName.Replace(Directories.Module, "").Replace('\\', '/') + "/");
            content = LoadPartialView(content);
            RenderPartialAssets(layout_name, Directories.View, content, true, parent_route);
            content = RenderPartialCss(finfo.DirectoryName, content);
            content = SeparateViewStyle(content);
            content = SeparateViewScript(content);
            return content;
        }

        private string LoadPartialView(string content, string parent_route = null)
        {
            string pattern = @"<c\.import\sname=""(.+)?""(?:\s*\/)?>(?:<\/c\.import>)?";
            MatchCollection matches = Regex.Matches(content, pattern);
            if (matches.Count > 0)
            {
                int newLength = 0;
                foreach (Match match in matches)
                {
                    string layout_name = "_" + match.Groups[1].Value;
                    string layout_file = Directories.View + layout_name + ".html";

                    if (File.Exists(layout_file))
                    {
                        string part_content = RenderViewComponent(layout_name, layout_file, parent_route);
                        content = SubsituteString(content, match.Index + newLength, match.Length, part_content);
                        newLength += part_content.Length - match.Length;
                    }
                    else
                    {
                        string v_dir = Directories.View + layout_name;
                        if (parent_route != null)
                        {
                            v_dir = Directories.View + parent_route + @"\" + layout_name;
                        }
                        if (Directory.Exists(v_dir))
                        {
                            layout_file = v_dir + @"\main.html";
                            if (File.Exists(layout_file))
                            {
                                string part_content = RenderViewComponent(layout_name, layout_file, parent_route);
                                content = SubsituteString(content, match.Index + newLength, match.Length, part_content);
                                newLength += part_content.Length - match.Length;
                            }
                            else
                            {
                                Console.WriteLine("Warning: Partial view " + layout_name + "/main.html is not found");
                            }
                        }
                        else
                        {
                            Console.WriteLine("Warning: Partial view " + layout_name + ".html is not found");
                        }
                    }
                }
            }
            return content;
        }
    }
}
