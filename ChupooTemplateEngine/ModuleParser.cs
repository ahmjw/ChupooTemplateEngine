﻿using System;
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
    class ModuleParser
    {
        private Hashtable attributes = new Hashtable();

        public bool ClonedPage { get; internal set; }

        public ModuleParser()
        {
            ClonedPage = false;
        }

        private void ReadAttributes(string text)
        {
            string pattern = @"([a-zA-Z0-9_-]+)\=""([^""]+)""";
            MatchCollection matches = Regex.Matches(text, pattern);
            if (matches.Count > 0)
            {
                foreach (Match match in matches)
                {
                    attributes[match.Groups[1].Value] = match.Groups[2].Value;
                }
            }
        }

        private string RenderNestedContent(string lib_name, bool is_file, string content)
        {
            string pattern = @"<c.module\[(.*?)\]([\w\W]+?)(?:\s*\/)?>([\w\W]+?)</c.module>";
            MatchCollection matches = Regex.Matches(content, pattern);
            if (matches.Count > 0)
            {
                int newLength = 0;
                foreach (Match match in matches)
                {
                    string part_content = match.Groups[3].Value;

                    CloningParser cp = new CloningParser();
                    cp.SetParentAttributes(attributes);
                    part_content = cp.Parse(lib_name, part_content);

                    part_content = ReplaceAttributes(part_content);
                    part_content = Parse(part_content);

                    LibParser lp = new LibParser();
                    part_content = lp.Parse(lib_name, part_content);

                    AssetParser ap = new AssetParser(AssetParser.DirectoryLocation.MODULE, Directories.Module);
                    ap.IsFile = is_file;
                    part_content = ap.Parse(lib_name, part_content);

                    content = Parser.SubsituteString(content, match.Groups[3].Index + newLength, match.Groups[3].Length, part_content);
                    newLength += part_content.Length - match.Groups[3].Length;
                }
            }
            return content;
        }

        public string ParseModule(string lib_name, string attributes, string inner_text = "")
        {
            string part_content = "";
            ReadAttributes(attributes);
            string lib_dir = Directories.GlobalModule + lib_name.Replace("/", "\\");
            string lib_file;
            bool is_exists = false;
            bool is_file = true;

            lib_file = lib_dir + ".html";
            is_exists = File.Exists(lib_file);

            if (!is_exists)
            {
                lib_file = lib_dir + "\\main.html";
                is_exists = File.Exists(lib_file);
                is_file = false;
            }

            if (!is_exists)
            {
                lib_dir = Directories.Module + lib_name.Replace("/", "\\");
                lib_file = lib_dir + ".html";
                is_exists = File.Exists(lib_file);
                is_file = true;
            }

            if (!is_exists)
            {
                lib_dir = Directories.Module + lib_name.Replace("/", "\\");
                lib_file = lib_dir + "\\main.html";
                is_exists = File.Exists(lib_file);
                is_file = false;
            }

            if (is_exists)
            {
                part_content = FileIo.GetHtmlContent(lib_file);
                part_content = RenderNestedContent(lib_name, is_file, part_content);

                ShowHidingParser shp = new ShowHidingParser();
                part_content = shp.Parse(part_content, this.attributes);

                NestedModuleParser np = new NestedModuleParser();
                part_content = np.ParseText("", lib_name, part_content);

                if (inner_text != "")
                {
                    part_content = Parser.ReplaceText(@"<c\.content(?:\s*\/)?>(?:<\/c\.content>)?", part_content, inner_text);
                }

                CloningParser cp = new CloningParser();
                cp.SetParentAttributes(this.attributes);
                part_content = cp.Parse(lib_name, part_content);

                part_content = ReplaceAttributes(part_content);
                part_content = Parse(part_content);

                LibParser lp = new LibParser();
                part_content = lp.Parse(lib_name, part_content);

                AssetParser ap = new AssetParser(AssetParser.DirectoryLocation.MODULE, Directories.Module);
                ap.IsFile = is_file;
                part_content = ap.Parse(lib_name, part_content);
            }
            else
                MessageController.Show("Warning: Module is not found > " + lib_name);
            return part_content;
        }

        public string Parse(string content)
        {
            string pattern = @"<c.module\[(.*?)\]([\w\W]+?)(?:\s*\/)?>([\w\W]+?)</c.module>";
            MatchCollection matches = Regex.Matches(content, pattern);
            if (matches.Count == 0)
            {
                pattern = @"<c\.module\[(.+)?\]([\w\W]+?)(?:\s*\/)?>(?:<\/c\.module>)?";
                matches = Regex.Matches(content, pattern);
            }
            if (matches.Count > 0)
            {
                int newLength = 0;
                foreach (Match match in matches)
                {
                    string lib_name = match.Groups[1].Value;
                    ReadAttributes(match.Groups[2].Value);
                    string lib_dir = Directories.GlobalModule + lib_name.Replace("/", "\\");
                    string lib_file;
                    bool is_exists = false;
                    bool is_file = true;

                    lib_file = lib_dir + ".html";
                    is_exists = File.Exists(lib_file);

                    if (!is_exists)
                    {
                        lib_file = lib_dir + "\\main.html";
                        is_exists = File.Exists(lib_file);
                        is_file = false;
                    }

                    if (!is_exists)
                    {
                        lib_dir = Directories.Module + lib_name.Replace("/", "\\");
                        lib_file = lib_dir + ".html";
                        is_exists = File.Exists(lib_file);
                        is_file = true;
                    }

                    if (!is_exists)
                    {
                        lib_dir = Directories.Module + lib_name.Replace("/", "\\");
                        lib_file = lib_dir + "\\main.html";
                        is_exists = File.Exists(lib_file);
                        is_file = false;
                    }

                    if (is_exists)
                    {
                        string part_content = FileIo.GetHtmlContent(lib_file);
                        part_content = RenderNestedContent(lib_name, is_file, part_content);

                        ShowHidingParser shp = new ShowHidingParser();
                        part_content = shp.Parse(part_content, attributes);

                        CloningParser cp = new CloningParser();
                        cp.SetParentAttributes(attributes);
                        part_content = cp.Parse(lib_name, part_content);

                        NestedModuleParser np = new NestedModuleParser();
                        part_content = np.ParseText("", lib_name, part_content);

                        if (match.Groups[3].Value != "")
                        {
                            part_content = Parser.ReplaceText(@"<c\.content(?:\s*\/)?>(?:<\/c\.content>)?", part_content, match.Groups[3].Value);
                        }

                        part_content = ReplaceAttributes(part_content);
                        part_content = Parse(part_content);

                        LibParser lp = new LibParser();
                        part_content = lp.Parse(lib_name, part_content);

                        AssetParser ap = new AssetParser(AssetParser.DirectoryLocation.MODULE, Directories.Module);
                        ap.IsFile = is_file;
                        //Console.WriteLine(lib_name + " " + is_file);
                        part_content = ap.Parse(lib_name, part_content);

                        content = Parser.SubsituteString(content, match.Index + newLength, match.Length, part_content);
                        newLength += part_content.Length - match.Length;
                    }
                    else
                    {
                        MessageController.Show("Warning: Module is not found > " + lib_name);
                    }

                    if (ClonedPage)
                    {
                        attributes.Clear();
                    }
                }
            }
            return content;
        }

        private string ReplaceAttributes(string content)
        {
            string pattern = @"\{\{([a-zA-Z0-9_-]+)\}\}";
            MatchCollection matches = Regex.Matches(content, pattern);
            if (matches.Count > 0)
            {
                int newLength = 0;
                foreach (Match match in matches)
                {
                    string part_content = attributes[match.Groups[1].Value] + "";

                    content = Parser.SubsituteString(content, match.Index + newLength, match.Length, part_content);
                    newLength += part_content.Length - match.Length;
                }
            }
            return content;
        }
    }
}
