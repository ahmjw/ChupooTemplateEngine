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
        private static string output_dir;
        private static string view_data_json_dir;
        private static JObject public_routes;
        private static string w_view_dir;
        private static bool has_changed_file = false;
        private static CommandType commandType;
        private static List<string> script_file_list = new List<string>();
        private static List<string> style_file_list = new List<string>();
        private static List<string> script_code_list = new List<string>();
        private static List<string> style_code_list = new List<string>();
        private static string[] watcher_exts = { ".html", ".js", ".css", ".scss" };
        private enum CommandType
        {
            FILE_SYSTEM_WATCHER,
            RENDER_ALL,
            RENDER_FILE,
            RENDER_DIRECTORY,
            RENDER_TEMPORARILY
        }

        [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
        static void Main(string[] args)
        {
            Console.WriteLine("Welcome to Chupoo View Engine's console.");
            Console.WriteLine("You can render your web design data to HTML linked-page here.");

            view_dir = AppDomain.CurrentDomain.BaseDirectory + "modules\\views\\";
            w_view_dir = AppDomain.CurrentDomain.BaseDirectory + "modules\\views";
            layout_dir = AppDomain.CurrentDomain.BaseDirectory + "modules\\layouts\\";
            view_data_json_dir = AppDomain.CurrentDomain.BaseDirectory + "modules\\views_data\\";
            output_dir = AppDomain.CurrentDomain.BaseDirectory + "output\\";

            string public_route_file = AppDomain.CurrentDomain.BaseDirectory + "modules\\config\\public_routes.json";
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
                Console.Clear();
                ran = true;
            }
            matched = Regex.Match(command, @"^browse$");
            if (!ran && matched.Success)
            {
                string path = output_dir + current_route + ".html";
                if (current_route != null)
                {
                    if (File.Exists(path))
                        Process.Start(path);
                    else
                    {
                        path = output_dir + "index.html";
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
            matched = Regex.Match(command, @"^render\s\-all$");
            if (!ran && matched.Success)
            {
                current_dir = view_dir;
                RenderDirectoryRecursively(view_dir, "");
                current_route = "index";
                current_dir = null;
                ran = true;
                commandType = CommandType.RENDER_ALL;
            }
            matched = Regex.Match(command, @"^render\s-f\s(.+?)$");
            if (!ran && matched.Success)
            {
                string view_name = matched.Groups[1].Value;
                ParseView(view_name, view_name);
                current_route = view_name;
                ran = true;
                commandType = CommandType.RENDER_FILE;
            }
            matched = Regex.Match(command, @"^render\s-d\s(.+?)$");
            if (!ran && matched.Success)
            {
                string view_name = matched.Groups[1].Value;
                RenderDirectory(view_name);
                current_route = view_name;
                ran = true;
                commandType = CommandType.RENDER_DIRECTORY;
            }
            matched = Regex.Match(command, @"^render\s-t\s(.+?)$");
            if (!ran && matched.Success)
            {
                string view_name = matched.Groups[1].Value;
                ParseView(view_name, ".temp");
                current_route = ".temp";
                ran = true;
                commandType = CommandType.RENDER_TEMPORARILY;
            }
            if (!ran)
                Console.WriteLine("Error: Invalid command");
            Run();
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
                string path_stage = dir.Replace(current_dir, "");
                if (!Directory.Exists(output_dir + path_stage))
                    Directory.CreateDirectory(output_dir + path_stage);

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

                string pattern;
                MatchCollection matches;

                pattern = @"<c\.partial\sname=""(.+)?""(?:\s*\/)?>(?:<\/c\.partial>)?";
                matches = Regex.Matches(view_content, pattern);
                if (matches.Count > 0)
                {
                    int newLength = 0;
                    foreach (Match match in matches)
                    {
                        string layout_name = "_" + match.Groups[1].Value;
                        string layout_file = view_dir + layout_name + ".html";

                        if (File.Exists(layout_file))
                        {
                            string part_content = File.ReadAllText(layout_file);
                            part_content = RenderPartialCss(layout_name, part_content);
                            part_content = RenderPartialAssets(layout_name, part_content);
                            part_content = SeparateStyle(part_content);
                            part_content = SeparateScript(part_content);
                            view_content = SubsituteString(view_content, match.Index + newLength, match.Length, part_content);
                            newLength += part_content.Length - match.Length;
                        }
                        else
                        {
                            Console.WriteLine("Warning: Partial view " + layout_name + ".html is not found");
                        }
                    }
                }

                view_content = RenderPartialCss(route, view_content);
                view_content = RenderPartialAssets(route, view_content);
                view_content = SeparateStyle(view_content);
                view_content = SeparateScript(view_content);

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

        private static string SeparateScript(string content)
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
                    script_file_list.Add(match.Value);
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
                    script_code_list.Add(match.Value);
                    content = SubsituteString(content, match.Index + newLength, match.Length, "");
                    newLength += -match.Length;
                }
            }
            return content;
        }

        private static string SeparateStyle(string content)
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
                    style_file_list.Add(match.Value);
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
                    style_code_list.Add(match.Value);
                    content = SubsituteString(content, match.Index + newLength, match.Length, "");
                    newLength += -match.Length;
                }
            }
            return content;
        }

        private static string RenderPartialCss(string route, string view_content)
        {
            Match matched = Regex.Match(view_content, @"<c\.css\shref=""(.*)?"">");
            if (matched.Success)
            {
                string css_path = view_dir + route + ".css";
                if (File.Exists(css_path))
                {
                    string target = matched.Groups[1].Value + "/_" + route + ".css";
                    string css_content = "<link rel=\"stylesheet\" type=\"text/css\" href=\"" + target + "\" />";
                    string dest_path = output_dir + target.Substring(2);
                    FileInfo finfo = new FileInfo(dest_path);
                    if (Directory.Exists(finfo.DirectoryName))
                    {
                        File.Copy(css_path, dest_path, true);
                        view_content = SubsituteString(view_content, matched.Index, matched.Length, css_content);
                    }
                }
                else
                {
                    Console.WriteLine("Warning: CSS file " + route + ".css is not found");
                }
            }
            return view_content;
        }

        private static string RenderPartialAssets(string route, string view_content)
        {
            string path = view_dir + route + ".css";
            if (File.Exists(path))
            {
                string content = "<style type=\"text/css\">" + File.ReadAllText(path) + "</style>";
                style_code_list.Add(content);
            }

            path = view_dir + route + ".js";
            if (File.Exists(path))
            {
                string content = "<script language=\"javascript\">" + File.ReadAllText(path) + "</script>";
                script_code_list.Add(content);
            }
            return view_content;
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
            string pattern = @"<(?:link|script|img).*?(?:href|src)=""\./(.*?)"".*?>";
            MatchCollection matches = Regex.Matches(content, pattern);
            if (matches.Count > 0)
            {
                int newLength = 0;
                //asset_level = asset_level.Substring(2);
                asset_level = asset_level.Substring(2) + "/../modules";
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
                    string url_target = public_routes[match.Groups[1].Value] != null ? public_routes[match.Groups[1].Value] + "" : "index";
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
                path = layout_dir + "page.html";
                Console.WriteLine("\tChange layout to page.html");
            }
            layout_content = File.ReadAllText(path);
            string pattern = @"<c\.partial\sname=""(.+)?""(?:\s*\/)?>(?:<\/c\.partial>)?";
            MatchCollection matches = Regex.Matches(layout_content, pattern);
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
                        layout_content = SubsituteString(layout_content, match.Index + newLength, match.Length, part_content);
                        newLength += part_content.Length - match.Length;
                    }
                    else
                    {
                        Console.WriteLine("Warning: Partial layout " + layout_name + ".html is not found");
                    }
                }
            }

            layout_content = SeparateStyle(layout_content);
            layout_content = SeparateScript(layout_content);
            layout_content = PasteScripts(layout_content);
            layout_content = PasteStyles(layout_content);
            layout_content = ReplaceLinkUrlText(layout_content, asset_level);
            layout_content = ReplaceAssetUrlText(layout_content, asset_level);
            pattern = @"<c\.content(?:\s*\/)?>(?:<\/c\.content>)?";
            layout_content = ReplaceText(pattern, layout_content, view_content);

            string output_path = AppDomain.CurrentDomain.BaseDirectory + "output\\" + dest + ".html";
            File.WriteAllText(output_path, layout_content);
            Console.WriteLine("OK: " + dest + ".html");
        }

        private static string PasteStyles(string content)
        {
            Match matched = Regex.Match(content, @"</head>[\w\W]*?<body.*?>");
            if (matched.Success)
            {
                string appended = "";
                foreach (string style in style_file_list)
                    appended += style;
                string new_content = appended + matched.Value;
                content = SubsituteString(content, matched.Index, matched.Length, new_content);
            }
            matched = Regex.Match(content, @"</head>[\w\W]*?<body.*?>");
            if (matched.Success)
            {
                string appended = "";
                foreach (string style in style_code_list)
                    appended += style;
                string new_content = appended + matched.Value;
                content = SubsituteString(content, matched.Index, matched.Length, new_content);
            }
            style_file_list.Clear();
            style_code_list.Clear();
            return content;
        }

        private static string PasteScripts(string content)
        {
            Match matched = Regex.Match(content, @"</body>[\w\W]*?</html>");
            if (matched.Success)
            {
                string appended = "";
                foreach (string script in script_file_list)
                    appended += script;
                string new_content = appended + matched.Value;
                content = SubsituteString(content, matched.Index, matched.Length, new_content);
            }
            matched = Regex.Match(content, @"</body>[\w\W]*?</html>");
            if (matched.Success)
            {
                string appended = "";
                foreach (string script in script_code_list)
                    appended += script;
                string new_content = appended + matched.Value;
                content = SubsituteString(content, matched.Index, matched.Length, new_content);
            }
            script_file_list.Clear();
            script_code_list.Clear();
            return content;
        }

        public static string SubsituteString(string OriginalStr, int index, int length, string subsituteStr)
        {
            return new StringBuilder(OriginalStr).Remove(index, length).Insert(index, subsituteStr).ToString();
        }
    }
}