using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using static ChupooTemplateEngine.Route;
using static ChupooTemplateEngine.Command;
using System.Collections;

namespace ChupooTemplateEngine
{
    class Parser
    {
        protected static string view_content = "";
        protected static string cfg_layout_name = "page";
        protected static List<string> script_file_list = new List<string>();
        protected static List<string> style_file_list = new List<string>();

        protected static List<string> lib_list = new List<string>();

        protected static List<string> v_script_code_list = new List<string>();
        protected static List<string> v_style_code_list = new List<string>();
        protected static List<string> l_script_file_list = new List<string>();
        protected static List<string> l_style_file_list = new List<string>();
        protected static List<string> l_script_code_list = new List<string>();
        protected static List<string> l_style_code_list = new List<string>();

        public static void RegisterUniversalCssFile(string url)
        {
            if (!style_file_list.Contains(url))
            style_file_list.Add(url);
        }

        public static void RegisterUniversalJsFile(string url)
        {
            if (!script_file_list.Contains(url))
                script_file_list.Add(url);
        }

        private static string[] pic_exts = { ".ico", ".png", ".jpeg", ".jpg", ".jpeg", ".bmp", ".svg" };
        private static string[] vid_exts = { ".mp4", ".avi", ".mov", ".flv", ".wmv", ".3gp", ".mpg" };
        private static string[] aud_exts = { ".mp3", ".amr", ".ogg", ".wav", ".wma" };

        public static string ReplaceText(string pattern, string content, string replacement)
        {
            Match matched = Regex.Match(content, pattern);
            if (matched.Success)
            {
                content = SubsituteString(content, matched.Index, matched.Length, replacement);
            }
            return content;
        }

        public static string SubsituteString(string OriginalStr, int index, int length, string subsituteStr)
        {
            return new StringBuilder(OriginalStr).Remove(index, length).Insert(index, subsituteStr).ToString();
        }

        public static void ClearStyles()
        {
            style_file_list.Clear();
            l_style_file_list.Clear();
            l_style_code_list.Clear();
            v_style_code_list.Clear();
        }

        public static void ClearScripts()
        {
            script_file_list.Clear();
            v_script_code_list.Clear();
            l_script_file_list.Clear();
            l_script_code_list.Clear();
        }

        public static void ClearAll()
        {
            ClearStyles();
            ClearScripts();
            lib_list.Clear();
        }

        protected void RenderPartialAssets(string route, string dir, string view_content, bool is_component = false, string parent_route = null)
        {
            string v_dir = dir;
            string _route = route;
            if (parent_route != null)
            {
                v_dir = dir + parent_route + @"\";
            }
            else if (route[0] != '@' && Directory.Exists(dir + parent_route))
            {
                route = route + @"\";
                is_component = true;
            }

            string path;
            if (is_component)
            {
                if (route[route.Length - 1] == '\\')
                {
                    if (Directories.View == dir)
                        path = v_dir + "@" + route + @"main.css";
                    else
                    {
                        path = v_dir + route + @"main.css";
                    }
                }
                else
                    path = v_dir + route + @"\main.css";
            }
            else
                path = v_dir + route + ".css";

            if (File.Exists(path))
            {
                string _m_name = path.Replace(Directories.Dev, "").Replace('\\', '/');
                string asset_url;
                if (CurrentCommand != CommandType.LAUNCH)
                {
                    asset_url = "../dev/" + _m_name;
                }
                else
                {
                    string m_name = _m_name.Replace("/main.css", ".css");
                    m_name = m_name.Replace("/", "-");
                    m_name = m_name.Replace("-@", "-");
                    
                    string a_dir = Directories.PublicAsset + "local\\styles\\";
                    if (!Directory.Exists(a_dir))
                    {
                        Directory.CreateDirectory(a_dir);
                    }
                    if (!File.Exists(a_dir + m_name))
                    {
                        MessageController.Show("  File> " + _m_name);
                        ParseCss(v_dir + route.Trim('\\'), path, a_dir + m_name);
                    }
                    asset_url = "./assets/local/styles/" + m_name;
                }

                RegisterUniversalCssFile(asset_url);
            }

            if (is_component)
            {
                if (route[route.Length - 1] == '\\')
                {
                    if (Directories.View == dir)
                        path = v_dir + "@" + route + @"main.js";
                    else
                        path = v_dir + route + @"main.js";
                }
                else
                    path = v_dir + route + @"\main.js";
            }
            else
                path = v_dir + route + ".js";

            if (File.Exists(path))
            {
                string asset_url;
                string _m_name = path.Replace(Directories.Dev, "").Replace('\\', '/');
                if (CurrentCommand != CommandType.LAUNCH)
                {
                    asset_url = "../dev/" + _m_name;
                }
                else
                {
                    string m_name = _m_name.Replace("/main.js", ".js");
                    m_name = m_name.Replace("/", "-");
                    m_name = m_name.Replace("-@", "-");
                    string a_dir = Directories.PublicAsset + "local\\scripts\\";
                    if (!Directory.Exists(a_dir))
                    {
                        Directory.CreateDirectory(a_dir);
                    }
                    if (!File.Exists(a_dir + m_name))
                    {
                        MessageController.Show("  File> " + _m_name);
                        File.Copy(path, a_dir + m_name);
                    }
                    asset_url = "./assets/local/scripts/" + m_name;
                }

                RegisterUniversalJsFile(asset_url);
            }
        }

        internal static void RegisterLib(string new_value)
        {
            lib_list.Add(new_value);
        }

        internal static bool IsLibExists(string new_value)
        {
            return lib_list.Contains(new_value);
        }

        private void ParseCss(string dir, string src_path, string dst_path)
        {
            string content = File.ReadAllText(src_path);
            MatchCollection matches = Regex.Matches(content, @"url\s*\(\s*(.*?)\s*\)");
            int newLength = 0;
            foreach (Match match in matches)
            {
                string c_name = match.Groups[1].Value.Replace("/", "\\");
                src_path = dir + "\\" + c_name;
                if (match.Groups[1].Value.Substring(0, 8) == "./assets")
                {
                    src_path = Directories.Asset + match.Groups[1].Value.Substring(9).Replace("/", "\\");
                }
                
                string i_dst_file_name = c_name;

                if (File.Exists(src_path))
                {
                    FileInfo finfo2 = new FileInfo(src_path);
                    string d_name = finfo2.DirectoryName.Replace(Directories.Dev, "") + "\\" + finfo2.Name;
                    d_name = d_name.Replace("\\", "/");
                    string i_dst_path;
                    if (match.Groups[1].Value.Substring(0, 8) != "./assets")
                    {
                        i_dst_file_name = RenameAsset(d_name);
                        i_dst_path = Directories.PublicAsset + "local\\" + i_dst_file_name;
                        if (!File.Exists(i_dst_path))
                        {
                            MessageController.Show("  CSS> " + d_name);
                            File.Copy(src_path, i_dst_path);
                        }
                        else
                        {
                        }
                        if (CurrentCommand != CommandType.LAUNCH)
                        {
                            i_dst_file_name = d_name;
                        }
                    }
                    else
                    {
                        i_dst_path = Directories.Public + "\\" + match.Groups[1].Value.Substring(9).Replace("/", "\\");
                        i_dst_file_name = "../../" + d_name;
                    }
                }
                else
                {
                }
                string new_value = "../" + i_dst_file_name.Replace("\\", "/");
                
                content = SubsituteString(content, match.Groups[1].Index + newLength, match.Groups[1].Length, new_value);
                newLength += new_value.Length - match.Groups[1].Length;
            }
            File.WriteAllText(dst_path, content);
        }

        protected string ReplaceLinkUrlText(string content, string asset_level)
        {
            string pattern = @"<a.*?href=""(.+?)"".*?>";
            MatchCollection matches = Regex.Matches(content, pattern);
            if (matches.Count > 0)
            {
                int newLength = 0;
                asset_level = asset_level.Substring(2);
                foreach (Match match in matches)
                {
                    string url_target;
                    if (CurrentCommand == CommandType.LAUNCH && LaunchEngine.LaunchType == LaunchEngine.LaunchTypeEnum.WORDPRESS)
                    {
                        if (match.Groups[1].Value.Substring(0, 3) == "<?=") continue;
                        url_target = "<?= get_site_url() ?>/" + match.Groups[1].Value;
                    }
                    else
                    {
                        url_target = match.Groups[1].Value.Replace("/", "-").Replace("\\", "-");
                    }
                    //string url_target = public_routes != null && public_routes.Count > 0 && public_routes[match.Groups[1].Value] != null ? public_routes[match.Groups[1].Value] + "" : "index";
                    string new_value = asset_level + url_target;
                    content = SubsituteString(content, match.Groups[1].Index + newLength, match.Groups[1].Length, new_value);
                    newLength += new_value.Length - match.Groups[1].Length;
                }
            }
            return content;
        }

        private string CalculateMD5Hash(string input)
        {
            System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create();
            byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
            byte[] hash = md5.ComputeHash(inputBytes);
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < hash.Length; i++)
            {
                sb.Append(hash[i].ToString("X2"));
            }
            return sb.ToString();
        }

        protected string LaunchViewAssets(string asset)
        {
            string ext = asset.Substring(asset.LastIndexOf('.'));
            if (Directories.Current == Directories.View)
                asset = asset.Replace("//", "/");
            else
            {
                asset = "dev/layouts/" + asset.Replace("//", "/");
            }

            string src_path = Directories.Project + asset.Replace("/", "\\");

            if (File.Exists(src_path))
            {
                string dst_file_name = RenameAsset(asset);
                string dst_path = Directories.PublicAsset + "local\\" + dst_file_name.Replace("/", "\\");

                if (ext == ".css")
                {
                    FileInfo info = new FileInfo(src_path);
                    ParseCss(info.DirectoryName, src_path, dst_path);
                }

                if (!LaunchEngine.IsCodeOnly && !File.Exists(dst_path))
                {
                    MessageController.Show("  HTML> " + asset);
                    File.Copy(src_path, dst_path);
                }

                dst_file_name = "./assets/local/" + dst_file_name;
                return dst_file_name;
            }
            else
            {
            }
            return asset;
        }

        private string RenameAsset(string asset)
        {
            string d_root = Directories.PublicAsset + "local\\";
            Match match = Regex.Match(asset, @"(_.*)?/[a-zA-Z0-9-_\.]+/([^\///]+(?:[^\///]+))(\.[a-zA-Z0-9-_]+)$");
            if (match.Success)
            {
                string d_name = "";
                string extension = match.Groups[3].Value;
                string f_name = match.Groups[2].Value;
                f_name = match.Groups[1].Value.Replace("/", "-") + f_name;
                f_name = f_name.Replace("-main", "");

                if (pic_exts.Any(extension.ToLower().Equals))
                {
                    d_name = "images";
                    if (!Directory.Exists(d_root + d_name))
                    {
                        Directory.CreateDirectory(d_root + d_name);
                    }
                }
                else if (aud_exts.Any(extension.ToLower().Equals))
                {
                    d_name = "audios";
                    if (!Directory.Exists(d_root + d_name))
                    {
                        Directory.CreateDirectory(d_root + d_name);
                    }
                }
                else if (vid_exts.Any(extension.ToLower().Equals))
                {
                    d_name = "videos";
                    if (!Directory.Exists(d_root + d_name))
                    {
                        Directory.CreateDirectory(d_root + d_name);
                    }
                }
                else if (extension == ".js")
                {
                    d_name = "scripts";
                    if (!Directory.Exists(d_root + d_name))
                    {
                        Directory.CreateDirectory(d_root + d_name);
                    }
                }
                else if (extension == ".css")
                {
                    d_name = "styles";
                    if (!Directory.Exists(d_root + d_name))
                    {
                        Directory.CreateDirectory(d_root + d_name);
                    }
                }
                return d_name + "/" + f_name + extension;
            }
            return asset;
        }
    }
}
