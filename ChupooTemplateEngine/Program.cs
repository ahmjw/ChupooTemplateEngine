using ChupooTemplateEngine.ViewParsers;
using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using static ChupooTemplateEngine.Command;

namespace ChupooTemplateEngine
{
    class Program : ApplicationContext
    {
        public static string current_route = ".temp";
        private static string w_view_dir;
        private static bool has_changed_file = false;
        private static string[] watcher_exts = { ".html" };
        private static string current_project_name;
        private static FileSystemWatcher watcher;
        private static Process browser;

        [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
        static void Main(string[] args)
        {
            Console.WriteLine("Welcome to Chupoo Template Engine's console.");
            Console.WriteLine("You can render your web design data to HTML linked-page here.");
            Directories.Resources = AppDomain.CurrentDomain.BaseDirectory + @"resources";
            Directories.Lib = AppDomain.CurrentDomain.BaseDirectory + @"libs\";

            if (Properties.Settings.Default.current_project_name != null && Properties.Settings.Default.current_project_name != "")
            {
                LoadProject(Properties.Settings.Default.current_project_name);
            }
            Run();
        }

        [DllImport("user32.dll")]
        static extern int SetForegroundWindow(IntPtr hWnd);

        private static void LoadBackup(string project_name)
        {
            if (!Directory.Exists(AppDomain.CurrentDomain.BaseDirectory + @"projects\" + project_name))
            {
                Console.WriteLine("Project " + project_name + " is invalid name");
                return;
            }

            current_project_name = project_name;

            Directories.Project = AppDomain.CurrentDomain.BaseDirectory + @"projects\" + project_name + @"\";
            Directories.View = AppDomain.CurrentDomain.BaseDirectory + @"projects\" + project_name + @"\views\";
            Directories.Module = AppDomain.CurrentDomain.BaseDirectory + @"projects\" + project_name + @"\modules\";
            w_view_dir = AppDomain.CurrentDomain.BaseDirectory + @"projects\" + project_name + @"\views";
            Directories.Layout = AppDomain.CurrentDomain.BaseDirectory + @"projects\" + project_name + @"\layouts\";
            Directories.Asset = AppDomain.CurrentDomain.BaseDirectory + @"projects\" + project_name + @"\assets\";
            Directories.Config = AppDomain.CurrentDomain.BaseDirectory + @"projects\" + project_name + @"\config\";
            Directories.ViewDataJson = AppDomain.CurrentDomain.BaseDirectory + @"projects\" + project_name + @"\views_data\";
            Directories.Public = AppDomain.CurrentDomain.BaseDirectory + @"projects\" + project_name + @"\public\";
            Directories.PublicAsset = AppDomain.CurrentDomain.BaseDirectory + @"projects\" + project_name + @"\public\assets\";

            Console.WriteLine("Project " + current_project_name + " has loaded.");

            string public_route_file = Directories.Config + "public_routes.json";
            if (File.Exists(public_route_file))
            {
                string public_route = File.ReadAllText(public_route_file);
                Route.public_routes = JObject.Parse(public_route);
                Console.WriteLine("Loaded using public routes.");
            }
            else
                Console.WriteLine("Loaded without public routes.");
        }

        private static void LoadProject(string project_name)
        {
            project_name = Regex.Replace(project_name, @"^([a-zA-Z0-9-_]+)\\.*?$", "$1");
            if (!Directory.Exists(AppDomain.CurrentDomain.BaseDirectory + @"projects\" + project_name))
            {
                Console.WriteLine("Project " + project_name + " is invalid name");
                return;
            }

            current_project_name = project_name;
            Properties.Settings.Default.current_project_name = project_name;
            Properties.Settings.Default.Save();

            Directories.Project = AppDomain.CurrentDomain.BaseDirectory + @"projects\" + project_name + @"\";
            Directories.Dev = AppDomain.CurrentDomain.BaseDirectory + @"projects\" + project_name + @"\dev\";
            Directories.View = AppDomain.CurrentDomain.BaseDirectory + @"projects\" + project_name + @"\dev\views\";
            Directories.Module = AppDomain.CurrentDomain.BaseDirectory + @"projects\" + project_name + @"\dev\modules\";
            w_view_dir = AppDomain.CurrentDomain.BaseDirectory + @"projects\" + project_name + @"\dev\views";
            Directories.Layout = AppDomain.CurrentDomain.BaseDirectory + @"projects\" + project_name + @"\dev\layouts\";
            Directories.Asset = AppDomain.CurrentDomain.BaseDirectory + @"projects\" + project_name + @"\dev\assets\";
            Directories.Config = AppDomain.CurrentDomain.BaseDirectory + @"projects\" + project_name + @"\dev\config\";
            Directories.Backup = AppDomain.CurrentDomain.BaseDirectory + @"projects\" + project_name + @"\dev\backups\";
            Directories.ViewDataJson = AppDomain.CurrentDomain.BaseDirectory + @"projects\" + project_name + @"\dev\views_data\";
            Directories.Public = AppDomain.CurrentDomain.BaseDirectory + @"projects\" + project_name + @"\public\";
            Directories.PublicAsset = AppDomain.CurrentDomain.BaseDirectory + @"projects\" + project_name + @"\public\assets\";

            if (watcher != null)
            {
                watcher.EnableRaisingEvents = false;

                watcher.Changed -= new FileSystemEventHandler(OnChanged);
                watcher.Dispose();
            }
            watcher = new FileSystemWatcher();
            watcher.Path = w_view_dir;
            watcher.NotifyFilter = NotifyFilters.LastWrite;
            watcher.Changed += new FileSystemEventHandler(OnChanged);
            watcher.Filter = "*.*";
            watcher.IncludeSubdirectories = true;
            watcher.EnableRaisingEvents = true;
            Console.WriteLine("Project " + current_project_name + " has loaded.");

            string public_route_file = Directories.Config + "public_routes.json";
            if (File.Exists(public_route_file))
            {
                string public_route = File.ReadAllText(public_route_file);
                Route.public_routes = JObject.Parse(public_route);
                Console.WriteLine("Loaded using public routes.");
            }
            else
                Console.WriteLine("Loaded without public routes.");
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
                    string path_stage = view_name;
                    HtmlTemplate viewParser = new HtmlTemplate();
                    Match matched = Regex.Match(view_name, @"^@([^\\]+)\\.*$");
                    if (matched.Success)
                    {
                        path_stage = matched.Groups[1].Value;
                    }
                    viewParser.Parse(path_stage, path_stage);
                    RefreshBrowser();
                    CurrentCommand = CommandType.FILE_SYSTEM_WATCHER;
                }
                finally
                {
                    has_changed_file = true;
                    if (current_project_name != null)
                        Console.Write("Chupoo[" + current_project_name + "]$ ");
                    else
                        Console.Write("Chupoo$ ");
                }
            }
        }

        private static void RefreshBrowser()
        {
            if (browser != null)
            {
                try
                {
                    IntPtr ptr = browser.MainWindowHandle;
                    SetForegroundWindow(ptr);
                    SendKeys.SendWait("{F5}");
                }
                catch
                {
                    RunCommand("browse");
                }
            }
        }

        private static void RunCommand(string command)
        {
            bool ran = false;
            Match matched;
            matched = Regex.Match(command, @"^clear$");
            if (!ran && matched.Success)
            {
                CurrentCommand = CommandType.CLEAR;
                Console.Clear();
                Parser.ClearAll();
                ran = true;
            }
            matched = Regex.Match(command, @"^project\screate\s(.+?)$");
            if (!ran && matched.Success)
            {
                CurrentCommand = CommandType.CREATE_PROJECT;
                CreateProject(matched.Groups[1].Value);
                ran = true;
            }
            matched = Regex.Match(command, @"^project\sload\s(.+?)$");
            if (!ran && matched.Success)
            {
                CurrentCommand = CommandType.LOAD_PROJECT;
                LoadProject(matched.Groups[1].Value);
                ran = true;
            }
            if (!ran && current_project_name == null)
            {
                Console.WriteLine("No project was loaded. Please run command: project load <name>");
                Run();
            }

            matched = Regex.Match(command, @"^browse$");
            if (!ran && matched.Success)
            {
                CurrentCommand = CommandType.BROWSE;
                string path = Directories.Public + current_route + ".html";
                if (current_route != null)
                {
                    if (File.Exists(path))
                        Process.Start(path);
                    else
                    {
                        path = Directories.Public + "index.html";
                        if (File.Exists(path))
                        {
                            Process process = new Process();
                            process.StartInfo.FileName = "chrome.exe";
                            process.StartInfo.Arguments = path;
                            process.Start();
                            browser = process;
                        }
                        else
                            Console.WriteLine("Error: No route for browsing");
                    }
                }
                ran = true;
            }
            matched = Regex.Match(command, @"^explore\s(.+?)$");
            if (!ran && matched.Success)
            {
                CurrentCommand = CommandType.EXPLORE_PROJECT;
                string dir = AppDomain.CurrentDomain.BaseDirectory + @"projects\" + matched.Groups[1].Value + @"\dev\";
                Process.Start(dir);
                ran = true;
            }
            matched = Regex.Match(command, @"^explore");
            if (!ran && matched.Success)
            {
                CurrentCommand = CommandType.EXPLORE;
                Process.Start(Directories.Dev);
                ran = true;
            }
            matched = Regex.Match(command, @"^render$");
            if (!ran && matched.Success)
            {
                CurrentCommand = CommandType.RENDER_ALL;
                Directories.Current = Directories.View;
                Parser.ClearAll();
                Asset.ClearAssets();
                ViewParser.RenderDirectoryRecursively(Directories.View, "");
                current_route = "index";
                Directories.Current = null;
                ran = true;
            }
            matched = Regex.Match(command, @"^launch$");
            if (!ran && matched.Success)
            {
                CurrentCommand = CommandType.LAUNCH;
                LaunchEngine.IsCodeOnly = false;
                LaunchEngine le = new LaunchEngine();
                le.Run(LaunchEngine.LaunchTypeEnum.HTML_TEMPLATE);
                current_route = "index";
                ran = true;
            }
            matched = Regex.Match(command, @"^launch\s-co$");
            if (!ran && matched.Success)
            {
                CurrentCommand = CommandType.LAUNCH;
                LaunchEngine.IsCodeOnly = true;
                LaunchEngine le = new LaunchEngine();
                le.Run(LaunchEngine.LaunchTypeEnum.HTML_TEMPLATE);
                current_route = "index";
                ran = true;
            }
            matched = Regex.Match(command, @"^launch\s-f\swp$");
            if (!ran && matched.Success)
            {
                CurrentCommand = CommandType.LAUNCH;
                LaunchEngine.IsCodeOnly = false;
                LaunchEngine le = new LaunchEngine();
                le.Run(LaunchEngine.LaunchTypeEnum.WORDPRESS);
                current_route = "index";
                ran = true;
            }
            matched = Regex.Match(command, @"^launch\s-f\scwm$");
            if (!ran && matched.Success)
            {
                CurrentCommand = CommandType.LAUNCH;
                LaunchEngine.IsCodeOnly = false;
                LaunchEngine le = new LaunchEngine();
                le.Run(LaunchEngine.LaunchTypeEnum.CHUPOO_WP_MVC);
                current_route = "index";
                ran = true;
            }
            matched = Regex.Match(command, @"^backup$");
            if (!ran && matched.Success)
            {
                CurrentCommand = CommandType.BACKUP;
                Directories.Current = Directories.View;
                Backup();
                current_route = "index";
                Directories.Current = null;
                ran = true;
            }
            matched = Regex.Match(command, @"^render\s-f\s(.+?)$");
            if (!ran && matched.Success)
            {
                CurrentCommand = CommandType.RENDER_FILE;
                string view_name = matched.Groups[1].Value;
                HtmlTemplate viewParser = new HtmlTemplate();
                viewParser.Parse(view_name, view_name);
                current_route = view_name;
                ran = true;
            }
            matched = Regex.Match(command, @"^render\s-d\s(.+?)$");
            if (!ran && matched.Success)
            {
                CurrentCommand = CommandType.RENDER_DIRECTORY;
                string view_name = matched.Groups[1].Value;
                HtmlTemplate viewParser = new HtmlTemplate();
                viewParser.RenderDirectory(view_name);
                current_route = view_name;
                ran = true;
            }
            matched = Regex.Match(command, @"^render\s-t\s(.+?)$");
            if (!ran && matched.Success)
            {
                CurrentCommand = CommandType.RENDER_TEMPORARILY;
                string view_name = matched.Groups[1].Value;
                HtmlTemplate viewParser = new HtmlTemplate();
                viewParser.Parse(view_name, ".temp");
                current_route = ".temp";
                ran = true;
            }
            matched = Regex.Match(command, @"^render\s-b\s(.+?)$");
            if (!ran && matched.Success)
            {
                CurrentCommand = CommandType.RENDER_BACKUP;
                string view_name = matched.Groups[1].Value;
                RenderBackup(view_name);
                ran = true;
            }
            if (!ran)
                Console.WriteLine("Error: Invalid command");
            Run();
        }

        private static void Run()
        {
            if (current_project_name != null)
                Console.Write("Chupoo[" + current_project_name + "]$ ");
            else
                Console.Write("Chupoo$ ");
            string command = Console.ReadLine();
            RunCommand(command);
        }

        private static void RenderBackup(string name)
        {
            string t_name = current_project_name;
            string t_dir = current_project_name + @"\dev\backups\" + name;
            LoadBackup(t_dir);

            Directories.Current = Directories.View;
            Asset.ClearAssets();
            ViewParser.RenderDirectoryRecursively(Directories.View, "");
            current_route = "index";
            Directories.Current = null;

            string path = Directories.Public + current_route + ".html";
            if (current_route != null)
            {
                if (File.Exists(path))
                    Process.Start(path);
                else
                {
                    path = Directories.Public + "index.html";
                    if (File.Exists(path))
                        Process.Start(path);
                    else
                        Console.WriteLine("Error: No route for browsing");
                }
            }

            current_project_name = t_name;
            LoadProject(t_name);

        }

        private static void CreateProject(string name)
        {
            Console.WriteLine("Creating project " + name + " ... please wait");
            string new_p_dir = AppDomain.CurrentDomain.BaseDirectory + @"projects\" + name;
            Directory.CreateDirectory(new_p_dir);
            CopyDirectory(Directories.Resources + "\\project_dir", new_p_dir);
            Console.WriteLine("Project " + name + " has successfully created");
            LoadProject(name);
        }

        public static void CopyDirectory(string SourcePath, string DestinationPath)
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
            string[] dirs = Directory.GetDirectories(Directories.Backup);
            string version = "0.0.1";
            if (dirs.Length > 0)
            {
                string last_dir = dirs[dirs.Length - 1];
                DirectoryInfo dinfo = new DirectoryInfo(last_dir);
                int last_version = Convert.ToInt32(dinfo.Name.Replace(".", ""));
                version = "0.0." + (last_version + 1);
            }
            string dir = Directories.Backup + version;
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            CopyDirectory(Directories.View, dir + @"\views\");
            CopyDirectory(Directories.Lib, dir + @"\libs\");
            CopyDirectory(Directories.Layout, dir + @"\layouts\");
            CopyDirectory(Directories.Config, dir + @"\config\");
            CopyDirectory(Directories.ViewDataJson, dir + @"\views_data\");
            CopyDirectory(Directories.Asset, dir + @"\assets\");
            Directory.CreateDirectory(dir + @"\public\");
            Console.WriteLine("Backed up to version " + version);
        }
    }
}