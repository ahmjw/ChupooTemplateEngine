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
    class AssetParser
    {
        private string[] pic_exts = { ".ico", ".png", ".jpeg", ".jpg", ".jpeg", ".bmp", ".svg" };
        private string[] vid_exts = { ".mp4", ".avi", ".mov", ".flv", ".wmv", ".3gp", ".mpg" };
        private string[] aud_exts = { ".mp3", ".amr", ".ogg", ".wav", ".wma" };
        private string v_dir;
        private DirectoryLocation dir_lv;

        public enum DirectoryLocation
        {
            LIBRARY,
            MODULE
        }

        public bool IsFile { get; internal set; }

        public AssetParser()
        {
        }

        public AssetParser(DirectoryLocation dir_lv, string v_dir)
        {
            this.dir_lv = dir_lv;
            this.v_dir = v_dir;
            IsFile = false;
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
                string i_dst_file_name = c_name;
                if (File.Exists(src_path))
                {
                    FileInfo finfo2 = new FileInfo(src_path);
                    string d_name = finfo2.DirectoryName.Replace(Directories.Project, "") + "\\" + finfo2.Name;
                    d_name = d_name.Replace("\\", "/");
                    i_dst_file_name = RenameAsset(d_name);
                    string i_dst_path = Directories.PublicAsset + "local\\" + i_dst_file_name;
                    if (!LaunchEngine.IsCodeOnly && !File.Exists(i_dst_path))
                    {
                        MessageController.Show("  CSS> " + d_name);
                        File.Copy(src_path, i_dst_path);
                    }
                }
                else
                {
                }
                string new_value = "../" + i_dst_file_name;
                content = Parser.SubsituteString(content, match.Groups[1].Index + newLength, match.Groups[1].Length, new_value);
                newLength += new_value.Length - match.Groups[1].Length;
            }
            File.WriteAllText(dst_path, content);
        }

        public string RenameAsset(string asset)
        {
            Match match = Regex.Match(asset, @"(_.*)?/([a-zA-Z0-9-_\.]+)(\.[a-zA-Z0-9-_]+)$");
            if (match.Success)
            {
                string d_name = "";
                string extension = match.Groups[3].Value;
                string f_name = match.Groups[2].Value;
                f_name = match.Groups[1].Value.Replace("/", "-") + f_name;
                f_name = f_name.Replace("-main", "");

                d_name = LookupDirectoryName(extension);
                return d_name + "/" + f_name + extension;
            }
            return asset;
        }

        public string GetAssetDirectoryName(string extension)
        {
            string d_name = "";
            if (pic_exts.Any(extension.ToLower().Equals))
            {
                d_name = "images";
            }
            else if (aud_exts.Any(extension.ToLower().Equals))
            {
                d_name = "audios";
            }
            else if (vid_exts.Any(extension.ToLower().Equals))
            {
                d_name = "videos";
            }
            else if (extension == ".js")
            {
                d_name = "scripts";
            }
            else if (extension == ".css")
            {
                d_name = "styles";
            }
            return d_name;
        }

        public string LookupDirectoryName(string extension)
        {
            string d_name = "";
            string d_root = Directories.PublicAsset + "local\\";
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
            return d_name;
        }

        private string GetParentName(string name)
        {
            int pos = name.LastIndexOf('/');
            if (pos > 0)
            {
                name = name.Substring(0, pos);
            }
            return name;
        }

        private string ReplaceAssetUrlText(string asset_level, string name, string content)
        {
            string pattern = @"<(?:link|script|img|source).*?(?:href|src|poster)=""(.+?)"".*?>";
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
                    if (dir_lv == DirectoryLocation.LIBRARY)
                    {
                        asset_level = asset_level.Substring(2) + "../../../dev-lib";
                    }
                    else
                    {
                        asset_level = asset_level.Substring(2) + "../dev/modules";
                    }
                }

                foreach (Match match in matches)
                {
                    if (Regex.Match(match.Groups[1].Value, @"^https?:\/\/").Success) continue;
                    if (CurrentCommand != CommandType.LAUNCH)
                    {
                        if (match.Groups[1].Value.Substring(0, 3) == "../") continue;
                    }
                    else
                    {
                        if (match.Groups[1].Value.Substring(0, 2) == "./"
                            || match.Groups[1].Value.Substring(0, 2) == "<?") continue;
                    }

                    string new_value = "";

                    if (CurrentCommand != CommandType.LAUNCH)
                    {
                        if (IsFile)
                        {
                            name = GetParentName(name);
                        }
                        new_value += asset_level + "/" + name + "/" + match.Groups[1].Value;
                    }
                    else
                    {
                        new_value = LookupAssetFile(dir_lv, name, match.Groups[1].Value);
                    }

                    try
                    {
                        FileInfo finfo = new FileInfo(new_value);
                        if (finfo.Extension == ".js")
                            Parser.RegisterUniversalJsFile(new_value);
                        else if (finfo.Extension == ".css")
                            Parser.RegisterUniversalCssFile(new_value);
                        else if (CurrentCommand == CommandType.LAUNCH && LaunchEngine.LaunchType == LaunchEngine.LaunchTypeEnum.WORDPRESS)
                            new_value = "<?= get_template_directory_uri() ?>/" + new_value.Substring(2);
                    }
                    catch (Exception ex)
                    {
                        MessageController.Show("Error: " + ex.Message);
                    }

                    if (new_value != "")
                    {
                        content = Parser.SubsituteString(content, match.Groups[1].Index + newLength, match.Groups[1].Length, new_value);
                        newLength += new_value.Length - match.Groups[1].Length;
                    }
                }
            }
            return content;
        }

        private string LookupAssetFile(DirectoryLocation dir_lv, string name, string url)
        {
            if (IsFile)
                name = GetParentName(name);
            string new_url = name + "/" + url;
            string src_dir = dir_lv == DirectoryLocation.LIBRARY ? Directories.DevLib : Directories.Module;
            string filePath = src_dir + name + "\\" + url.Replace("/", "\\");
            if (File.Exists(filePath))
            {
                Match matched = Regex.Match(url, @"/([^\/]+)(\.[a-zA-Z0-9_-]+)$");
                if (matched.Success)
                {
                    string fname = matched.Groups[1].Value + matched.Groups[2].Value;
                    string d_name = LookupDirectoryName(matched.Groups[2].Value);
                    string destPath = Directories.PublicAsset + "local\\" + d_name + "\\" + fname;
                    if (!LaunchEngine.IsCodeOnly && !File.Exists(destPath))
                    {
                        MessageController.Show("  HTML> " + new_url);
                        File.Copy(filePath, destPath);
                    }
                    new_url = "./assets/local/" + d_name + "/" + fname;
                }
            }
            return new_url;
        }

        public string Parse(string name, string content)
        {
            if (ViewParser.is_scanning_content) return content;

            string _route = name;
            content = ReplaceAssetUrlText("./", name, content);

            string path = v_dir + name + "\\main.css";
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
                        ParseCss(v_dir + name, path, a_dir + m_name);
                    }
                    asset_url = "./assets/local/styles/" + m_name;

                }
                Parser.RegisterUniversalCssFile(asset_url);
            }

            path = v_dir + name + "\\main.js";
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
                Parser.RegisterUniversalJsFile(asset_url);
            }
            return content;
        }
    }
}