using ChupooTemplateEngine.ViewParsers;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
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
        public static bool is_scanning_content = false;

        public static string Extension { get; set; }

        abstract protected void OnViewParsed(string route, string dest, string asset_level);

        public static List<string> RouteHasVariableAssets = new List<string>();
        public static List<Hashtable> VariableAssets = new List<Hashtable>();

        public ViewParser()
        {
        }

        public void LoopViews(string path)
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
                Parse(path_stage, path_stage);

                Directories.Current = Directories.View;
                ClearAll();
            }

            // Reparses route has variable inside asset
            foreach (string v_route in RouteHasVariableAssets)
            {
                string v_path = Directories.View + v_route + "\\main.html";
                string v_content = File.ReadAllText(v_path);

                string p_route = v_route.Substring(0, v_route.IndexOf('/'));
                string p_path = Directories.View + p_route + "\\main.html";
                string p_content = File.ReadAllText(p_path);

                CloningPageParser cpp = new CloningPageParser();
                CloningPage cp = cpp.Parse(p_route, p_content);

                foreach (JToken datum in cp.Data)
                {
                    JObject page_data = (JObject)datum;

                    CollectVariableAsset(v_route, v_content, page_data);
                }
            }

            // Copies all variable assets
            AssetParser ap = new AssetParser();
            if (VariableAssets.Count > 0)
            {
                foreach (Hashtable data in VariableAssets)
                {
                    foreach (DictionaryEntry de in data)
                    {
                        List<string> items = (List<string>)de.Value;
                        foreach (string item in items)
                        {
                            string loc = de.Key + "\\" + item.Trim('.');
                            loc = loc.Replace("/", "\\");

                            string src_url = Directories.View + loc;
                            if (File.Exists(src_url))
                            {
                                FileInfo fi = new FileInfo(loc);
                                string dst_url = Directories.PublicAsset + "local\\" +
                                    ap.LookupDirectoryName(fi.Extension) + "\\" + fi.Name;
                                if (!File.Exists(dst_url))
                                {
                                    File.Copy(src_url, dst_url);
                                }
                            }
                        }
                    }
                }
            }
            RouteHasVariableAssets.Clear();
        }

        private void CollectVariableAsset(string route, string content, JObject page_data)
        {
            string pattern = @"<(?:link|script|img|source).*?(?:href|src|poster)=""(\.[^\.].*?)"".*?>";
            MatchCollection mc = Regex.Matches(content, pattern);
            List<string> items = new List<string>();

            foreach (Match m in mc)
            {
                pattern = @"\{\{\$([^\}]+)\}\}";
                string url = m.Groups[1].Value;
                MatchCollection mc0 = Regex.Matches(url, pattern);
                if (mc0.Count > 0)
                {
                    int newLength = 0;
                    foreach (Match m0 in mc0)
                    {
                        string var_name = m0.Groups[1].Value;
                        if (page_data[var_name] != null)
                        {
                            string new_value = page_data[var_name] + "";
                            url = SubsituteString(url, m0.Index + newLength, m0.Length, new_value);
                            newLength += new_value.Length - m0.Length;
                        }
                    }
                    items.Add(url);
                }
            }
            if (items.Count > 0)
            {
                Hashtable data = new Hashtable();
                data.Add(route, items);
                VariableAssets.Add(data);
            }
        }

        public void Parse(string route, string dest)
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

                // Cloning page
                CloningPageParser cpp = new CloningPageParser();
                CloningPage cp = cpp.Parse(route, view_content);
                if (cp.IsCloningPage)
                {
                    if (!cpp.SingleLaunch)
                    {
                        foreach (JToken datum in cp.Data)
                        {
                            JObject page_data = (JObject)datum;
                            view_content = ReplaceFormattedDataText(route, cp.Content, page_data);
                            CloningPage newContent = cpp.ApplyData(route, cp, page_data);
                            ParseGenerally(route, newContent.NewName, asset_level, matched, newContent.Content, page_data);
                            Directories.Current = Directories.View;
                        }
                    }
                    else
                    {
                        //LookupVariableAsset(route, dest, asset_level, cp, matched);
                        ParseGenerally(route, dest, asset_level, matched, view_content, null, true);
                    }
                }
                else
                {
                    ParseGenerally(route, dest, asset_level, matched, view_content);
                }
            }
            else
            {
                MessageController.Show("View file is not found: " + route + ".html");
            }
        }

        private void ParseGenerally(string route, string dest, string asset_level, Match matched, string content, JObject page_data = null, bool single_launch = false)
        {
            if (is_scanning_content)
            {
                //CollectVariableAsset(route, content, page_data);
            }

            content = ReplaceFormattedDataText(route, content, page_data, false, single_launch);

            NestedModuleParser np = new NestedModuleParser();
            content = np.ParseText("", route, content);

            content = ReplaceAssetUrlText(content, "./", "dev/views/@" + route + "/");

            CloningParser cp = new CloningParser();
            content = cp.Parse(route, content);

            LibParser lp = new LibParser();
            content = lp.Parse(route, content);

            ModuleParser mp = new ModuleParser();
            mp.ClonedPage = true;
            content = mp.Parse(content);

            ShowHidingParser shp = new ShowHidingParser();
            content = shp.Parse(content, page_data);

            AssetParser ap = new AssetParser(AssetParser.DirectoryLocation.MODULE, Directories.View);
            content = ap.Parse(route, content);

            if (!is_scanning_content)
            {
                matched = Regex.Match(content, @"<c\.config\slayout=""(.+)?""(?:\s*\/)?>(?:<\/c\.config>)?");
                if (matched.Success)
                {
                    cfg_layout_name = matched.Groups[1].Value;
                    content = SubsituteString(content, matched.Index, matched.Length, "");
                }
                else
                    cfg_layout_name = "page";

            }

            string c_dir = Directories.View + "@" + route;
            if (Directory.Exists(c_dir))
                content = LoadPartialView(content, page_data, single_launch, "@" + route);
            else
                content = LoadPartialView(content, page_data, single_launch);

            content = RenderPartialCss(c_dir, content);
            RenderPartialAssets(route, Directories.View, content);

            // Replace data
            if (page_data == null)
            {
                string data_path = Directories.ViewDataJson + route + ".json";
                if (File.Exists(data_path))
                {
                    MessageController.Show("Rendering " + route + ".html JSON data ...");
                    string json_str = File.ReadAllText(data_path);
                    page_data = JObject.Parse(json_str);
                }
                else
                {
                    matched = Regex.Match(content, @"<c\.config\sdata-source=""(.+)?""(?:\s*\/)?>(?:<\/c\.config>)?");
                    if (matched.Success)
                    {
                        string data_source_url = matched.Groups[1].Value;
                        MessageController.Show("Loading data from URL > " + data_source_url);
                        Http http = new Http();
                        try
                        {
                            string json_text = http.GetResponse(data_source_url);
                            page_data = JObject.Parse(json_text);
                        }
                        catch
                        {
                            MessageController.Show("Error: Failed to connect to server");
                            page_data = new JObject();
                        }
                    }
                }
            }

            content = ReplaceFormattedDataText(route, content, page_data, true, single_launch);
            view_content = ReplaceLinkUrlText(content, asset_level);

            OnViewParsed(route, dest, asset_level);
        }

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
            if (is_scanning_content) return view_content;

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

        public static string ReplaceFormattedDataText(string route, string content, JObject data, bool remove_footage = true, bool single_launch = false)
        {
            if (is_scanning_content) return content;

            string pattern = @"\{\{([^\.][^\}]+)\}\}";
            MatchCollection matches = Regex.Matches(content, pattern);
            if (matches.Count > 0)
            {
                int newLength = 0;
                foreach (Match match in matches)
                {
                    string var_name = match.Groups[1].Value;
                    string new_value = "";

                    bool do_remove = false;
                    if (remove_footage)
                    {
                        do_remove = true;
                    }
                    else if (data != null && data[var_name] != null)
                    {
                        do_remove = true;
                    }
                    if (do_remove)
                    {
                        if (Regex.Match(var_name, @"^\$page\.").Success)
                            continue;

                        if (data != null)
                        {
                            bool use_server_var = false;

                            if (CurrentCommand == CommandType.LAUNCH)
                            {
                                if (LaunchEngine.LaunchType == LaunchEngine.LaunchTypeEnum.WORDPRESS &&
                                    var_name[0] == '$')
                                {
                                    new_value = "<?= " + var_name + " ?>";
                                    use_server_var = true;
                                }
                            }

                            if (!use_server_var)
                            {
                                if (var_name[0] == '$')
                                {
                                    var_name = var_name.Substring(1);
                                }

                                string[] arrays = var_name.Split('.');
                                if (arrays.Length > 0)
                                {
                                    JToken current_data = data;
                                    foreach (string item in arrays)
                                    {
                                        if (current_data[item] == null)
                                            break;
                                        new_value = current_data[item] + "";
                                        current_data = current_data[item];
                                    }
                                }
                                else
                                {
                                    new_value = data[var_name] + "";
                                }
                            }
                            content = SubsituteString(content, match.Index + newLength, match.Length, new_value);
                            newLength += new_value.Length - match.Length;
                        }
                        else
                        {
                            if (!single_launch)
                            {
                                content = SubsituteString(content, match.Index + newLength, match.Length, "");
                                newLength += match.Length;
                            }
                            else
                            {
                                //Hashtable v_data = new Hashtable();
                                //v_data["route"] = route;
                                //v_data["string"] = match.Value;
                                //v_data["replacement"] = "<?= " + var_name + " ?>";
                                if (route[0] == '@' && !RouteHasVariableAssets.Contains(route))
                                {
                                    RouteHasVariableAssets.Add(route);
                                }

                                new_value = "<?= " + var_name + " ?>";
                                content = SubsituteString(content, match.Index + newLength, match.Length, new_value);
                                newLength += new_value.Length - match.Length;
                            }
                        }
                    }
                }
            }
            return content;
        }

        protected string ReplaceAssetUrlText(string content, string asset_level, string component_name = null)
        {
            if (is_scanning_content) return content;

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
                    string new_value;
                    string ext;

                    if (match.Groups[1].Value.Substring(0, 2) == "./")
                    {
                        if (CurrentCommand != CommandType.LAUNCH)
                            new_value = asset_level + match.Groups[1].Value.Substring(1);
                        else
                        {
                            if (match.Groups[1].Length >= 6 && match.Groups[1].Value.Substring(2, 6) == "assets")
                            {
                                new_value = asset_level + match.Groups[1].Value.Substring(2);
                            }
                            else
                            {
                                string view_asset = asset_level + match.Groups[1].Value.Substring(2);
                                new_value = LaunchViewAssets(view_asset);
                            }
                        }
                        FileInfo finfo = new FileInfo(new_value);
                        ext = finfo.Extension;
                    }
                    else
                    {
                        if (CurrentCommand != CommandType.LAUNCH)
                        {
                            new_value = asset_level + "/" + component_name + match.Groups[1].Value.Substring(1);
                            FileInfo finfo = new FileInfo(new_value);
                            ext = finfo.Extension;
                        }
                        else
                        {
                            // Is a variable asset?
                            string url = match.Groups[1].Value;
                            Match mc0 = Regex.Match(url, @"<.*?>");
                            if (mc0.Success)
                            {
                                string f_name = Regex.Replace(url, @"^.+\/([^\/]+)(\.[a-zA-Z0-9_]+)$", "$1$2");
                                AssetParser ap = new AssetParser();
                                ext = f_name.Substring(f_name.LastIndexOf('.'));
                                new_value = "assets/local/" + ap.GetAssetDirectoryName(ext) + "/" + f_name;
                            }
                            else
                            {
                                string view_asset = asset_level + component_name + url.Substring(1);
                                new_value = LaunchViewAssets(view_asset);
                                FileInfo finfo = new FileInfo(new_value);
                                ext = finfo.Extension;
                            }
                        }
                    }

                    // INSIDE VIEW'S PART
                    try
                    {
                        if (ext == ".js")
                            RegisterUniversalJsFile(new_value);
                        else if (ext == ".css")
                            RegisterUniversalCssFile(new_value);
                        else if (!LaunchEngine.IsCodeOnly && CurrentCommand == CommandType.LAUNCH && LaunchEngine.LaunchType == LaunchEngine.LaunchTypeEnum.WORDPRESS)
                            new_value = "<?= get_template_directory_uri() ?>/" + new_value;
                    }
                    catch(Exception ex)
                    {
                        MessageController.Show("Error:" + ex.Message);
                    }

                    content = SubsituteString(content, match.Groups[1].Index + newLength, match.Groups[1].Length, new_value);
                    newLength += new_value.Length - match.Groups[1].Length;
                }
            }
            return content;
        }

        protected string RenderViewComponent(string layout_name, string layout_file, string parent_route, JObject page_data, bool single_launch)
        {
            FileInfo finfo = new FileInfo(layout_file);
            string content = File.ReadAllText(layout_file);
            if (is_scanning_content)
            {
                //CollectVariableAsset(parent_route + "/" + layout_name, content, page_data);
            }

            bool remove_footage = CurrentCommand == CommandType.LAUNCH;
            content = ReplaceFormattedDataText(parent_route + "/" + layout_name, content, page_data, remove_footage, single_launch);

            NestedModuleParser np = new NestedModuleParser();
            content = np.ParseText(parent_route, layout_name, content);

            LibParser lp = new LibParser();
            content = lp.Parse(layout_name, content);

            ModuleParser mp = new ModuleParser();
            content = mp.Parse(content);

            string c_name = finfo.DirectoryName.Replace(Directories.Project, "").Replace('\\', '/') + "/";
            content = ReplaceAssetUrlText(content, "./", c_name);
            content = LoadPartialView(content, page_data, single_launch, parent_route);
            RenderPartialAssets(layout_name, Directories.View, content, true, parent_route);
            content = RenderPartialCss(finfo.DirectoryName, content);
            content = SeparateViewStyle(content);
            content = SeparateViewScript(content);
            return content;
        }

        protected string LoadPartialView(string content, JObject page_data, bool single_launch, string parent_route = null)
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
                        string part_content = RenderViewComponent(_layout_name, layout_file, parent_route, page_data, single_launch);
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
                                string part_content = RenderViewComponent(_layout_name, layout_file, parent_route, page_data, single_launch);
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
