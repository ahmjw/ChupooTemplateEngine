using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Permissions;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ChupooTemplateEngine
{
    class Program
    {
        private static string layout_dir;
        private static string view_dir;
        private static string view_content = "";
        private static string cfg_layout_name = "page";
        private static string current_route = ".temp";
        private static string current_dir;
        private static string public_dir;
        private static string view_data_json_dir;
        private static JObject public_routes;
        private static string w_view_dir;
        private static bool has_changed_file = false;
        private static CommandType commandType;
        private static List<string> v_script_file_list = new List<string>();
        private static List<string> v_style_file_list = new List<string>();
        private static List<string> v_script_code_list = new List<string>();
        private static List<string> v_style_code_list = new List<string>();
        private static List<string> l_script_file_list = new List<string>();
        private static List<string> l_style_file_list = new List<string>();
        private static List<string> l_script_code_list = new List<string>();
        private static List<string> l_style_code_list = new List<string>();
        private static string[] watcher_exts = { ".html", ".js", ".css", ".scss" };
        private static string[] asset_exts = { ".js", ".css", ".ico", ".png", ".jpeg", ".jpg", ".jpeg", ".bmp", ".svg" };
        private static string asset_dir;
        private static string backup_dir;
        private static string config_dir;

        private enum CommandType
        {
            FILE_SYSTEM_WATCHER,
            RENDER_ALL,
            RENDER_FILE,
            RENDER_DIRECTORY,
            RENDER_TEMPORARILY,
            LAUNCH,
            CLEAR,
            BROWSE,
            EDIT,
            BACKUP
        }

        [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
        static void Main(string[] args)
        {
            Console.WriteLine("Welcome to Chupoo View Engine's console.");
            Console.WriteLine("You can render your web design data to HTML linked-page here.");

            view_dir = AppDomain.CurrentDomain.BaseDirectory + "modules\\views\\";
            w_view_dir = AppDomain.CurrentDomain.BaseDirectory + "modules\\views";
            layout_dir = AppDomain.CurrentDomain.BaseDirectory + "modules\\layouts\\";
            asset_dir = AppDomain.CurrentDomain.BaseDirectory + "modules\\assets\\";
            config_dir = AppDomain.CurrentDomain.BaseDirectory + "modules\\config\\";
            backup_dir = AppDomain.CurrentDomain.BaseDirectory + "modules\\backups\\";
            view_data_json_dir = AppDomain.CurrentDomain.BaseDirectory + "modules\\views_data\\";
            public_dir = AppDomain.CurrentDomain.BaseDirectory + "public\\";

            string public_route_file = config_dir + "public_routes.json";
            if (File.Exists(public_route_file))
            {
                string public_route = File.ReadAllText(public_route_file);
                public_routes = JObject.Parse(public_route);
                Console.WriteLine("Public routes were loaded.");
            }
            else
                Console.WriteLine("Running without public route.");

            FileSystemWatcher watcher = new FileSystemWatcher();
            watcher.Path = w_view_dir;
            watcher.NotifyFilter = NotifyFilters.LastWrite;
            watcher.Changed += new FileSystemEventHandler(OnChanged);
            watcher.Filter = "*.*";
            watcher.IncludeSubdirectories = true;
            watcher.EnableRaisingEvents = true;

            Run();
        }
        
        private static bool WaitForFile(string fullPath, string short_name)
        {
            int numTries = 0;
            while (true)
            {
                ++numTries;
                try
                {
                    using (FileStream fs = new FileStream(fullPath,
                        FileMode.Open, FileAccess.ReadWrite,
                        FileShare.None, 100))
                    {
                        fs.ReadByte();
                        break;
                    }
                }
                catch
                {
                    if (numTries > 10)
                    {
                        Console.WriteLine("Wait for file {0} giving up after 10 tries", short_name);
                        return false;
                    }
                    System.Threading.Thread.Sleep(500);
                }
            }
            return true;
        }

        private static void OnChanged(object sender, FileSystemEventArgs e)
        {
            FileInfo finfo = new FileInfo(e.Name);
            if (!watcher_exts.Any(finfo.Extension.Equals)) return;

            if (has_changed_file)
            {
                has_changed_file = false;
                return;
            }

            string view_name = e.FullPath.Replace(w_view_dir + @"\", "").Replace(finfo.Extension, "");
            if (WaitForFile(e.FullPath, view_name + ".html"))
            {
                try
                {
                    ParseView(view_name, view_name);
                    commandType = CommandType.FILE_SYSTEM_WATCHER;
                }
                finally
                {
                    has_changed_file = true;
                    Console.Write("Chupoo$ ");
                }
            }
        }

        private static void Run()
        {
            Console.Write("Chupoo$ ");
            string command = Console.ReadLine();

            bool ran = false;
            Match matched;
            matched = Regex.Match(command, @"^clear$");
            if (!ran && matched.Success)
            {
                commandType = CommandType.CLEAR;
                Console.Clear();
                ran = true;
            }
            matched = Regex.Match(command, @"^browse$");
            if (!ran && matched.Success)
            {
                commandType = CommandType.BROWSE;
                string path = public_dir + current_route + ".html";
                if (current_route != null)
                {
                    if (File.Exists(path))
                        Process.Start(path);
                    else
                    {
                        path = public_dir + "index.html";
                        if (File.Exists(path))
                            Process.Start(path);
                        else
                            Console.WriteLine("Error: No route for browsing");
                    }
                }
                ran = true;
            }
            matched = Regex.Match(command, @"^edit$");
            if (!ran && matched.Success)
            {
                commandType = CommandType.EDIT;
                string path = view_dir + current_route + ".html";
                if (current_route != null)
                {
                    if (File.Exists(path))
                        Process.Start("notepad " + path);
                    else
                    {
                        path = view_dir + "index.html";
                        if (File.Exists(path))
                            Process.Start("notepad " + path);
                        else
                            Console.WriteLine("Error: No route for editing");
                    }
                }
                ran = true;
            }
            matched = Regex.Match(command, @"^render$");
            if (!ran && matched.Success)
            {
                commandType = CommandType.RENDER_ALL;
                current_dir = view_dir;
                ClearAssets();
                RenderDirectoryRecursively(view_dir, "");
                current_route = "index";
                current_dir = null;
                ran = true;
            }
            matched = Regex.Match(command, @"^launch$");
            if (!ran && matched.Success)
            {
                commandType = CommandType.LAUNCH;
                current_dir = view_dir;
                ClearAssets();
                RenderDirectoryRecursively(view_dir, "");
                LaunchAssets(asset_dir);
                current_route = "index";
                current_dir = null;
                ran = true;
            }
            matched = Regex.Match(command, @"^backup$");
            if (!ran && matched.Success)
            {
                commandType = CommandType.BACKUP;
                current_dir = view_dir;
                Backup();
                current_route = "index";
                current_dir = null;
                ran = true;
            }
            matched = Regex.Match(command, @"^render\s-f\s(.+?)$");
            if (!ran && matched.Success)
            {
                commandType = CommandType.RENDER_FILE;
                string view_name = matched.Groups[1].Value;
                ParseView(view_name, view_name);
                current_route = view_name;
                ran = true;
            }
            matched = Regex.Match(command, @"^render\s-d\s(.+?)$");
            if (!ran && matched.Success)
            {
                commandType = CommandType.RENDER_DIRECTORY;
                string view_name = matched.Groups[1].Value;
                RenderDirectory(view_name);
                current_route = view_name;
                ran = true;
            }
            matched = Regex.Match(command, @"^render\s-t\s(.+?)$");
            if (!ran && matched.Success)
            {
                commandType = CommandType.RENDER_TEMPORARILY;
                string view_name = matched.Groups[1].Value;
                ParseView(view_name, ".temp");
                current_route = ".temp";
                ran = true;
            }
            if (!ran)
                Console.WriteLine("Error: Invalid command");
            Run();
        }

        private static void CopyDirectory(string SourcePath, string DestinationPath)
        {
            if (!Directory.Exists(DestinationPath))
                Directory.CreateDirectory(DestinationPath);
            foreach (string dirPath in Directory.GetDirectories(SourcePath, "*",
                SearchOption.AllDirectories))
                Directory.CreateDirectory(dirPath.Replace(SourcePath, DestinationPath));

            foreach (string newPath in Directory.GetFiles(SourcePath, "*.*",
                SearchOption.AllDirectories))
                File.Copy(newPath, newPath.Replace(SourcePath, DestinationPath), true);
        }

        private static void Backup()
        {
            string[] dirs = Directory.GetDirectories(backup_dir);
            string version = "0.0.1";
            if (dirs.Length > 0)
            {
                string last_dir = dirs[dirs.Length - 1];
                DirectoryInfo dinfo = new DirectoryInfo(last_dir);
                int last_version = Convert.ToInt32(dinfo.Name.Replace(".", ""));
                version = "0.0." + (last_version + 1);
            }
            string dir = backup_dir + version;
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            CopyDirectory(view_dir, dir + "\\views\\");
            CopyDirectory(layout_dir, dir + "\\layouts\\");
            CopyDirectory(config_dir, dir + "\\config\\");
            CopyDirectory(view_data_json_dir, dir + "\\views_data\\");
            CopyDirectory(asset_dir, dir + "\\assets\\");
            Console.WriteLine("Backed up to version " + version);
        }

        private static void ClearAssets()
        {
            string[] public_dirs = Directory.GetDirectories(public_dir);
            foreach (string dir in public_dirs)
            {
                DirectoryInfo dinfo = new DirectoryInfo(dir);
                if (!Directory.Exists(view_dir + dinfo.Name))
                {
                    Directory.Delete(dir, true);
                }
            }
            string[] public_files = Directory.GetFiles(public_dir);
            foreach (string file in public_files)
            {
                FileInfo finfo = new FileInfo(file);
                if (!File.Exists(view_dir + finfo.Name))
                {
                    File.Delete(file);
                }
            }
        }

        private static void LaunchAssets(string path)
        {
            string[] dirs = Directory.GetDirectories(path);
            string path_stage;
            foreach (string dir in dirs)
            {
                path_stage = dir.Replace(asset_dir, "");


                if (!Directory.Exists(public_dir + path_stage))
                    Directory.CreateDirectory(public_dir + path_stage);

                string[] subdirs = Directory.GetDirectories(path);
                if (subdirs.Length > 0)
                {
                    LaunchAssets(dir);
                }
            }
            string[] files = Directory.GetFiles(path);
            path_stage = path.Replace(asset_dir, "");
            foreach (string file in files)
            {
                FileInfo finfo = new FileInfo(file);
                if (!asset_exts.Any(finfo.Extension.Equals)) continue;
                File.Copy(file, public_dir + path_stage + "\\" + finfo.Name, true);
            }
        }

        private static void RenderDirectory(string route)
        {
            string path = view_dir + route;
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
                string path_stage = file.Replace(view_dir, "").Replace(".html", "");
                ParseView(path_stage, path_stage);
            }
        }

        private static void RenderDirectoryRecursively(string path, string asset_level)
        {
            string[] dirs = Directory.GetDirectories(path);
            foreach (string dir in dirs)
            {
                DirectoryInfo dinfo = new DirectoryInfo(dir);
                if (dinfo.Name[0] == '_') continue;
                string path_stage = dir.Replace(current_dir, "");
                if (!Directory.Exists(public_dir + path_stage))
                    Directory.CreateDirectory(public_dir + path_stage);

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
                string path_stage = file.Replace(current_dir, "").Replace(".html", "");
                ParseView(path_stage, path_stage);
            }
        }

        private static string GetAssetLeveling(string route)
        {
            string level = "./";
            int length = route.Split('\\').Length - 1;
            for (int i = 0; i < length; i++)
            {
                level += "../";
            }
            return level;
        }

        private static void ParseView(string route, string dest)
        {
            string asset_level = GetAssetLeveling(route);
            string path = view_dir + route + ".html";
            Match matched = Regex.Match(route, @"^(.*?)\/?_[a-zA-Z0-9_-]+$");
            if (matched.Success)
            {
                if (commandType == CommandType.FILE_SYSTEM_WATCHER)
                {
                    string dir_route = Regex.Replace(path.Replace(view_dir, ""), @"^(.*?)[a-zA-Z0-9_-]+\.html$", "$1");
                    RenderDirectory(dir_route);
                }
                else
                    Console.WriteLine("Skip file " + route + ".html");
            }
            else if (File.Exists(path))
            {
                view_content = File.ReadAllText(path);

                matched = Regex.Match(view_content, @"<c\.config\slayout=""(.+)?""(?:\s*\/)?>(?:<\/c\.config>)?");
                if (matched.Success)
                {
                    cfg_layout_name = matched.Groups[1].Value;
                    view_content = SubsituteString(view_content, matched.Index, matched.Length, "");
                }
                else
                    cfg_layout_name = "page";

                string c_dir = view_dir + "_" + route;
                if (Directory.Exists(c_dir))
                    view_content = LoadPartialView(view_content, "_" + route);
                else
                    view_content = LoadPartialView(view_content);

                view_content = RenderPartialCss(c_dir, view_content);
                RenderPartialAssets(route, view_dir, view_content);
                view_content = SeparateViewStyle(view_content);
                view_content = SeparateViewScript(view_content);

                string data_path = view_data_json_dir + route + ".json";
                if (File.Exists(data_path))
                {
                    Console.WriteLine("Rendering " + route + ".html JSON data ...");
                    string json_str = File.ReadAllText(data_path);
                    JObject data = JObject.Parse(json_str);
                    view_content = ReplaceFormattedDataText(view_content, data);
                }
                view_content = ReplaceLinkUrlText(view_content, asset_level);
                view_content = ReplaceAssetUrlText(view_content, asset_level);
                ParseLayout(dest, asset_level);
            }
            else
            {
                Console.WriteLine("View file is not found: " + route + ".html");
            }
        }

        private static string LoadPartialView(string content, string parent_route = null)
        {
            string pattern = @"<c\.partial\sname=""(.+)?""(?:\s*\/)?>(?:<\/c\.partial>)?";
            MatchCollection matches = Regex.Matches(content, pattern);
            if (matches.Count > 0)
            {
                int newLength = 0;
                foreach (Match match in matches)
                {
                    string layout_name = "_" + match.Groups[1].Value;
                    string layout_file = view_dir + layout_name + ".html";

                    if (File.Exists(layout_file))
                    {
                        string part_content = RenderViewComponent(layout_name, layout_file, parent_route);
                        content = SubsituteString(content, match.Index + newLength, match.Length, part_content);
                        newLength += part_content.Length - match.Length;
                    }
                    else
                    {
                        string v_dir = view_dir + layout_name;
                        if (parent_route != null)
                        {
                            v_dir = view_dir + parent_route + "\\" + layout_name;
                        }
                        if (Directory.Exists(v_dir))
                        {
                            layout_file = v_dir + "\\main.html";
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

        private static string RenderViewComponent(string layout_name, string layout_file, string parent_route)
        {
            string content = File.ReadAllText(layout_file);
            content = LoadPartialView(content);
            RenderPartialAssets(layout_name, view_dir, content, true, parent_route);
            FileInfo finfo = new FileInfo(layout_file);
            content = RenderPartialCss(finfo.DirectoryName, content);
            content = SeparateViewStyle(content);
            content = SeparateViewScript(content);
            return content;
        }

        private static string SeparateViewScript(string content)
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

        private static string SeparateLayoutScript(string content)
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
                    l_script_file_list.Add(match.Value);
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
                    l_script_code_list.Add(match.Value);
                    content = SubsituteString(content, match.Index + newLength, match.Length, "");
                    newLength += -match.Length;
                }
            }
            return content;
        }

        private static string SeparateViewStyle(string content)
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

        private static string SeparateLayoutStyle(string content)
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
                    l_style_file_list.Add(match.Value);
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
                    l_style_code_list.Add(match.Value);
                    content = SubsituteString(content, match.Index + newLength, match.Length, "");
                    newLength += -match.Length;
                }
            }
            return content;
        }

        private static string RenderPartialCss(string dir, string view_content)
        {
            Match matched = Regex.Match(view_content, @"<c\.css\shref=""(.*)?""(?:\s*\/)?>(?:<\/c\.css>)?");
            if (matched.Success)
            {
                string css_path = dir + "\\" + matched.Groups[1].Value;

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

        private static void RenderPartialAssets(string route, string dir, string view_content, bool is_component = false, string parent_route = null)
        {
            string v_dir = dir;
            if (parent_route != null)
            {
                v_dir = dir + parent_route + "\\";
            }
            else if (route[0] != '_' && Directory.Exists(dir + parent_route))
            {
                route = "_" + route + "\\";
                is_component = true;
            }

            string path;
            if (is_component)
                path = v_dir + route + "\\main.css";
            else
                path = v_dir + route + ".css";
            if (File.Exists(path))
            {
                string content = "<style type=\"text/css\">" + File.ReadAllText(path) + "</style>";
                v_style_code_list.Add(content);
            }

            if (is_component)
                path = v_dir + route + "\\main.js";
            else
                path = v_dir + route + ".js";
            if (File.Exists(path))
            {
                string content = "<script language=\"javascript\">" + File.ReadAllText(path) + "</script>";
                v_script_code_list.Add(content);
            }
        }

        private static string ReplaceFormattedDataText(string content, JObject data)
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

        private static string ReplaceAssetUrlText(string content, string asset_level)
        {
            string pattern = @"<(?:link|script|img|source).*?(?:href|src|poster)=""\./(.*?)"".*?>";
            MatchCollection matches = Regex.Matches(content, pattern);
            if (matches.Count > 0)
            {
                int newLength = 0;
                if (commandType == CommandType.LAUNCH)
                    asset_level = asset_level.Substring(2);
                else
                    asset_level = asset_level.Substring(2) + "../modules/";

                foreach (Match match in matches)
                {
                    string new_value = asset_level + match.Groups[1].Value;
                    content = SubsituteString(content, match.Groups[1].Index + newLength, match.Groups[1].Length, new_value);
                    newLength += new_value.Length - match.Groups[1].Length;
                }
            }
            return content;
        }

        private static string ReplaceLinkUrlText(string content, string asset_level)
        {
            string pattern = @"<a.*?href=""\./(.*?)"".*?>";
            MatchCollection matches = Regex.Matches(content, pattern);
            if (matches.Count > 0)
            {
                int newLength = 0;
                asset_level = asset_level.Substring(2);
                foreach (Match match in matches)
                {
                    string url_target = public_routes != null && public_routes.Count > 0 && public_routes[match.Groups[1].Value] != null ? public_routes[match.Groups[1].Value] + "" : "index";
                    string new_value = asset_level + url_target + ".html";
                    content = SubsituteString(content, match.Groups[1].Index + newLength, match.Groups[1].Length, new_value);
                    newLength += new_value.Length - match.Groups[1].Length;
                }
            }
            return content;
        }

        private static string ReplaceText(string pattern, string content, string replacement)
        {
            Match matched = Regex.Match(content, pattern);
            if (matched.Success)
            {
                content = SubsituteString(content, matched.Index, matched.Length, replacement);
            }
            return content;
        }

        private static void ParseLayout(string dest, string asset_level)
        {
            string layout_content = "";
            string path = layout_dir + cfg_layout_name + ".html";
            if (!File.Exists(path)) {
                Console.WriteLine("Warning: " + dest + " >> Layout file is not found: " + cfg_layout_name + ".html");
                cfg_layout_name = "page";
                path = layout_dir + cfg_layout_name + ".html";
                if (File.Exists(path))
                {
                    Console.WriteLine("\tChange layout to " + cfg_layout_name + ".html");
                }
                else
                {
                    Console.WriteLine("Warning: " + dest + " >> Layout file is not found: " + cfg_layout_name + ".html");
                    return;
                }
            }
            layout_content = File.ReadAllText(path);

            layout_content = RenderPartialLayout(layout_content);
            layout_content = RenderLayoutComponent(cfg_layout_name, layout_content);

            layout_content = PasteScripts(layout_content);
            layout_content = PasteStyles(layout_content);
            layout_content = ReplaceLinkUrlText(layout_content, asset_level);
            layout_content = ReplaceAssetUrlText(layout_content, asset_level);
            string pattern = @"<c\.content(?:\s*\/)?>(?:<\/c\.content>)?";
            layout_content = ReplaceText(pattern, layout_content, view_content);

            string p_dir = "";
            Match matched = Regex.Match(dest, "^(.*?)\\?[a-zA-Z0-9-_]+$");
            if (matched.Success)
            {
                p_dir = matched.Groups[1].Value;
            }
            if (Directory.Exists(public_dir + p_dir))
            {
                string p_file = public_dir + dest + ".html";
                File.WriteAllText(p_file, layout_content);
                Console.WriteLine("OK: " + dest + ".html");
            }
            else
            {
                Console.WriteLine("Error: Layout directory " + p_dir + " is not found");
            }
        }

        private static string RenderPartialLayout(string content)
        {
            string pattern = @"<c\.partial\sname=""(.+)?""(?:\s*\/)?>(?:<\/c\.partial>)?";
            MatchCollection matches = Regex.Matches(content, pattern);
            if (matches.Count > 0)
            {
                int newLength = 0;
                foreach (Match match in matches)
                {
                    string layout_name = "_" + match.Groups[1].Value;
                    string layout_file = layout_dir + layout_name + ".html";

                    if (File.Exists(layout_file))
                    {
                        string part_content = File.ReadAllText(layout_file);
                        part_content = RenderLayoutComponent(layout_name, part_content);
                        content = SubsituteString(content, match.Index + newLength, match.Length, part_content);
                        newLength += part_content.Length - match.Length;
                    }
                    else
                    {
                        layout_file = layout_dir + layout_name + "\\main.html";
                        if (File.Exists(layout_file))
                        {
                            string part_content = File.ReadAllText(layout_file);
                            part_content = RenderLayoutComponent(layout_name, part_content);
                            content = SubsituteString(content, match.Index + newLength, match.Length, part_content);
                            newLength += part_content.Length - match.Length;
                        }
                        else
                        {
                            Console.WriteLine("Warning: Partial layout " + layout_name + ".html is not found");
                        }
                    }
                }
            }
            return content;
        }

        private static string RenderLayoutComponent(string name, string content, string parent_route = null)
        {
            content = RenderPartialLayout(content);
            RenderPartialAssets(name, layout_dir, content, true, parent_route);
            content = SeparateLayoutStyle(content);
            content = SeparateLayoutScript(content);
            return content;
        }

        private static string PasteStyles(string content)
        {
            Match matched = Regex.Match(content, @"</head>[\w\W]*?<body.*?>");
            if (matched.Success)
            {
                string appended = "";
                foreach (string style in l_style_file_list)
                    appended += style;
                foreach (string style in v_style_file_list)
                    appended += style;
                string new_content = appended + matched.Value;
                content = SubsituteString(content, matched.Index, matched.Length, new_content);
            }
            matched = Regex.Match(content, @"</head>[\w\W]*?<body.*?>");
            if (matched.Success)
            {
                string appended = "";
                foreach (string style in v_style_code_list)
                    appended += style;
                foreach (string style in l_style_code_list)
                    appended += style;
                string new_content = appended + matched.Value;
                content = SubsituteString(content, matched.Index, matched.Length, new_content);
            }
            l_style_file_list.Clear();
            l_style_code_list.Clear();
            v_style_file_list.Clear();
            v_style_code_list.Clear();
            return content;
        }

        private static string PasteScripts(string content)
        {
            Match matched = Regex.Match(content, @"</body>[\w\W]*?</html>");
            if (matched.Success)
            {
                string appended = "";
                foreach (string script in l_script_file_list)
                    appended += script;
                foreach (string script in v_script_file_list)
                    appended += script;
                string new_content = appended + matched.Value;
                content = SubsituteString(content, matched.Index, matched.Length, new_content);
            }
            matched = Regex.Match(content, @"</body>[\w\W]*?</html>");
            if (matched.Success)
            {
                string appended = "";
                foreach (string script in l_script_code_list)
                    appended += script;
                foreach (string script in v_script_code_list)
                    appended += script;
                string new_content = appended + matched.Value;
                content = SubsituteString(content, matched.Index, matched.Length, new_content);
            }
            l_script_file_list.Clear();
            l_script_code_list.Clear();
            v_script_file_list.Clear();
            v_script_code_list.Clear();
            return content;
        }

        public static string SubsituteString(string OriginalStr, int index, int length, string subsituteStr)
        {
            return new StringBuilder(OriginalStr).Remove(index, length).Insert(index, subsituteStr).ToString();
        }
    }
}