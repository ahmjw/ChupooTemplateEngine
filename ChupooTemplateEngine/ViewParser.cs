using ChupooTemplateEngine.ViewParsers;
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
    abstract class ViewParser : Parser
    {
        public abstract void Parse(string route, string dest);
        public abstract void LoopViews(string path);

        protected string GetAssetLeveling(string route)
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
                MessageController.Show("Error: " + route + " directory is not found");
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
                if (dinfo.Name[0] != '@') continue;
                string file = dir + "\\main.html";
                if (File.Exists(file))
                {
                    string path_stage = file.Replace(Directories.Current, "").Substring(1).Replace("\\main.html", "");
                    HtmlTemplate viewParser = new HtmlTemplate();
                    viewParser.Parse(path_stage, path_stage);
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

        protected string SeparateViewStyle(string content)
        {
            string pattern;
            MatchCollection matches;
            pattern = @"<link.*?rel=""stylesheet"".*?>\n?";
            matches = Regex.Matches(content, pattern);
            if (matches.Count > 0)
            {
                int newLength = 0;
                foreach (Match match in matches)
                {
                    content = SubsituteString(content, match.Index + newLength, match.Length, "");
                    newLength += -match.Length;
                }
            }
            pattern = @"<style.*?>[\w\W]*?</style>\n?";
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

        protected string SeparateViewScript(string content)
        {
            string pattern;
            MatchCollection matches;
            pattern = @"<script.*?></script>\n?";
            matches = Regex.Matches(content, pattern);
            if (matches.Count > 0)
            {
                int newLength = 0;
                foreach (Match match in matches)
                {
                    content = SubsituteString(content, match.Index + newLength, match.Length, "");
                    newLength += -match.Length;
                }
            }
            pattern = @"<script.*?>[\w\W]*?</script>\n?";
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

        protected string RenderPartialCss(string dir, string view_content)
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
                    MessageController.Show("Warning: CSS file " + matched.Groups[1].Value + " is not found");
                }
            }
            return view_content;
        }

        public static string ReplaceFormattedDataText(string content, JObject data, bool remove_footage = true)
        {
            string pattern = @"\{\{([^\.][a-zA-Z0-9_-]+)\}\}";
            MatchCollection matches = Regex.Matches(content, pattern);
            if (matches.Count > 0)
            {
                int newLength = 0;
                foreach (Match match in matches)
                {
                    bool do_remove = false;
                    if (remove_footage)
                    {
                        do_remove = true;
                    }
                    else if (data != null && data[match.Groups[1].Value] != null)
                    {
                        do_remove = true;
                    }
                    if (do_remove)
                    {
                        if (data != null)
                        {
                            string new_value = data[match.Groups[1].Value] + "";
                            content = SubsituteString(content, match.Index + newLength, match.Length, new_value);
                            newLength += new_value.Length - match.Length;
                        }
                        else
                        {
                            content = SubsituteString(content, match.Index + newLength, match.Length, "");
                            newLength += match.Length;
                        }
                    }
                }
            }
            return content;
        }

        protected string ReplaceAssetUrlText(string content, string asset_level, string component_name = null)
        {
            string pattern = @"<(?:link|script|img|source).*?(?:href|src|poster)=""(\.[^\.].*?)"".*?>";
            MatchCollection matches = Regex.Matches(content, pattern);
            if (matches.Count > 0)
            {
                int newLength = 0;
                if (CurrentCommand == CommandType.LAUNCH)
                {
                    asset_level = asset_level.Substring(2);
                }
                else if (CurrentCommand == CommandType.RENDER_BACKUP)
                {
                    asset_level = asset_level.Substring(2) + "..";
                }
                else
                {
                    asset_level = asset_level.Substring(2) + "..";
                }

                foreach (Match match in matches)
                {
                    string new_value = "";
                    if (match.Groups[1].Value.Substring(0, 2) == "./")
                    {
                        if (CurrentCommand != CommandType.LAUNCH)
                            new_value += asset_level + match.Groups[1].Value.Substring(1);
                        else
                        {
                            if (match.Groups[1].Length >= 6 && match.Groups[1].Value.Substring(2, 6) == "assets")
                            {
                                new_value += asset_level + match.Groups[1].Value.Substring(2);
                            }
                            else
                            {
                                string view_asset = asset_level + match.Groups[1].Value.Substring(2);
                                new_value += LaunchViewAssets(view_asset);
                            }
                        }
                    }
                    else
                    {
                        if (CurrentCommand != CommandType.LAUNCH)
                        {
                            new_value += asset_level + "/" + component_name + match.Groups[1].Value.Substring(1);
                        }
                        else
                        {
                            string view_asset = asset_level + component_name + match.Groups[1].Value.Substring(1);
                            new_value += LaunchViewAssets(view_asset);
                        }
                    }

                    // INSIDE VIEW'S PART
                    FileInfo finfo = new FileInfo(new_value);
                    if (finfo.Extension == ".js")
                        RegisterUniversalJsFile(new_value);
                    else if (finfo.Extension == ".css")
                        RegisterUniversalCssFile(new_value);
                    else if (!LaunchEngine.IsCodeOnly && CurrentCommand == CommandType.LAUNCH && LaunchEngine.LaunchType == LaunchEngine.LaunchTypeEnum.WORDPRESS)
                        new_value = "<?= get_template_directory_uri() ?>/" + new_value;

                    content = SubsituteString(content, match.Groups[1].Index + newLength, match.Groups[1].Length, new_value);
                    newLength += new_value.Length - match.Groups[1].Length;
                }
            }
            return content;
        }

        protected string RenderViewComponent(string layout_name, string layout_file, string parent_route, JObject page_data)
        {
            FileInfo finfo = new FileInfo(layout_file);
            string content = File.ReadAllText(layout_file);

            content = ReplaceFormattedDataText(content, page_data, false);

            NestedModuleParser np = new NestedModuleParser();
            content = np.ParseText(parent_route, layout_name, content);

            LibParser lp = new LibParser();
            content = lp.Parse(layout_name, content);

            ModuleParser mp = new ModuleParser();
            content = mp.Parse(content);

            string c_name = finfo.DirectoryName.Replace(Directories.Project, "").Replace('\\', '/') + "/";
            content = ReplaceAssetUrlText(content, "./", c_name);
            content = LoadPartialView(content, page_data, parent_route);
            RenderPartialAssets(layout_name, Directories.View, content, true, parent_route);
            content = RenderPartialCss(finfo.DirectoryName, content);
            content = SeparateViewStyle(content);
            content = SeparateViewScript(content);
            return content;
        }

        protected string LoadPartialView(string content, JObject page_data, string parent_route = null)
        {
            string pattern = @"<c\.part\[(.+)?\](.*?)(?:\s*\/)?>(?:<\/c\.part>)?";
            MatchCollection matches = Regex.Matches(content, pattern);
            if (matches.Count > 0)
            {
                int newLength = 0;
                foreach (Match match in matches)
                {
                    string _layout_name = match.Groups[1].Value.Replace("/", "\\");
                    string layout_name;
                    string layout_file;
                    if (parent_route != null)
                    {
                        layout_name = parent_route + "\\" + _layout_name;
                    }
                    else
                    {
                        layout_name = match.Groups[1].Value.Replace("/", "\\");
                    }
                    layout_file = Directories.View + layout_name + ".html";

                    if (File.Exists(layout_file))
                    {
                        string part_content = RenderViewComponent(_layout_name, layout_file, parent_route, page_data);
                        content = SubsituteString(content, match.Index + newLength, match.Length, part_content);
                        newLength += part_content.Length - match.Length;
                    }
                    else
                    {
                        string v_dir = Directories.View + layout_name;
                        if (parent_route != null)
                        {
                            v_dir = Directories.View + @"\" + layout_name;
                        }

                        if (Directory.Exists(v_dir))
                        {
                            layout_file = v_dir + @"\main.html";
                            if (File.Exists(layout_file))
                            {
                                string part_content = RenderViewComponent(_layout_name, layout_file, parent_route, page_data);
                                content = SubsituteString(content, match.Index + newLength, match.Length, part_content);
                                newLength += part_content.Length - match.Length;
                            }
                            else
                            {
                                MessageController.Show("Warning: Partial view " + layout_name + "/main.html is not found");
                            }
                        }
                        else
                        {
                            MessageController.Show("Warning: Partial view " + layout_name + ".html is not found");
                        }
                    }
                }
            }
            return content;
        }

        protected string PasteStyles(string content)
        {
            string appended = "";
            foreach (string item in style_file_list)
            {
                string _item = item;
                if (CurrentCommand == CommandType.LAUNCH && LaunchEngine.LaunchType == LaunchEngine.LaunchTypeEnum.WORDPRESS)
                    _item = "<?= get_template_directory_uri() ?>/" + item;
                appended += "<link rel=\"stylesheet\" type=\"text/css\" href=\"" + _item + "\">\n";
            }
            foreach (string style in v_style_code_list)
                appended += style;
            foreach (string style in l_style_code_list)
                appended += style;
            return appended + content;
        }

        protected string PasteScripts(string content)
        {
            string appended = "";
            foreach (string item in script_file_list)
            {
                string _item = item;
                if (CurrentCommand == CommandType.LAUNCH && LaunchEngine.LaunchType == LaunchEngine.LaunchTypeEnum.WORDPRESS)
                    _item = "<?= get_template_directory_uri() ?>/" + item;
                appended += "<script type=\"text/javascript\" src=\"" + _item + "\"></script>\n";
            }
            foreach (string script in l_script_code_list)
                appended += script;
            foreach (string script in v_script_code_list)
                appended += script;
            return appended + content;
        }
    }
}
