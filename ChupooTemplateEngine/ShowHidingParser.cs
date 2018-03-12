using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ChupooTemplateEngine
{
    class ShowHidingParser
    {
        public string Parse(string content, JObject attributes)
        {
            string pattern = @"\{([a-zA-Z0-9-_]+)@([a-zA-Z0-9-_]+)\(([^\)]*?)\)\}";
            MatchCollection matches = Regex.Matches(content, pattern);

            if (matches.Count > 0)
            {
                Hashtable data = new Hashtable();
                int newLength = 0;
                foreach (Match match in matches)
                {
                    bool do_write = false;
                    string func_name = match.Groups[1].Value;
                    string var_name = match.Groups[2].Value;
                    string target = match.Groups[3].Value;

                    if (func_name == "show")
                    {
                        do_write = attributes[var_name] != null && attributes[var_name].ToString() == "true";
                    }
                    else if (func_name == "hide")
                    {
                        do_write = attributes[var_name] == null || attributes[var_name].ToString() != "true";
                    }

                    if (do_write)
                    {
                        content = Parser.SubsituteString(content, match.Index + newLength, match.Length, target);
                        newLength += target.Length - match.Length;
                    }
                    else
                    {
                        content = Parser.SubsituteString(content, match.Index + newLength, match.Length, "");
                        newLength += target.Length;
                    }
                }
            }
            else
            {
                pattern = @"\{(!?[a-zA-Z0-9-_]+)@([a-zA-Z0-9-_]+)\,([a-zA-Z0-9-_]+)\(([^\)]*?)\)\}";
                matches = Regex.Matches(content, pattern);
                if (matches.Count > 0)
                {
                    Hashtable data = new Hashtable();
                    int newLength = 0;
                    foreach (Match match in matches)
                    {
                        bool do_write = false;
                        string func_name = match.Groups[1].Value;
                        string ord_a = match.Groups[2].Value;
                        string ord_b = match.Groups[3].Value;
                        string target = match.Groups[4].Value;

                        if (func_name == "equal")
                        {
                            do_write = !(attributes[ord_a] != null && attributes[ord_b] != null 
                                && attributes[ord_a].ToString() == attributes[ord_b].ToString());
                        }
                        else if (func_name == "!equal")
                        {
                            do_write = attributes[ord_a] != null && attributes[ord_b] != null
                                && attributes[ord_a].ToString() == attributes[ord_b].ToString();
                        }

                        if (do_write)
                        {
                            content = Parser.SubsituteString(content, match.Index + newLength, match.Length, target);
                            newLength += target.Length - match.Length;
                        }
                        else
                        {
                            content = Parser.SubsituteString(content, match.Index + newLength, match.Length, "");
                            newLength += target.Length;
                        }
                    }
                }
            }
            return content;
        }

        public string Parse(string content, Hashtable attributes)
        {
            string pattern = @"\{([a-zA-Z0-9-_]+)@([a-zA-Z0-9-_]+)\(([^\)]*?)\)\}";
            MatchCollection matches = Regex.Matches(content, pattern);

            if (matches.Count > 0)
            {
                Hashtable data = new Hashtable();
                int newLength = 0;
                foreach (Match match in matches)
                {
                    bool do_write = false;
                    string func_name = match.Groups[1].Value;
                    string var_name = match.Groups[2].Value;
                    string target = match.Groups[3].Value;

                    if (func_name == "show")
                    {
                        do_write = attributes.Contains(var_name) && attributes[var_name].ToString() == "true";
                    }
                    else if (func_name == "hide")
                    {
                        do_write = !attributes.Contains(var_name) || attributes[var_name].ToString() != "true";
                    }

                    if (do_write)
                    {
                        content = Parser.SubsituteString(content, match.Index + newLength, match.Length, target);
                        newLength += target.Length - match.Length;
                    }
                    else
                    {
                        content = Parser.SubsituteString(content, match.Index + newLength, match.Length, "");
                        newLength += target.Length;
                    }
                }
            }
            else
            {
                pattern = @"\{(!?[a-zA-Z0-9-_]+)@([^\,]+)\,([^\(]+)\(([^\:]*)\:([^\)]*?)\)\}";
                matches = Regex.Matches(content, pattern);
                if (matches.Count > 0)
                {
                    int newLength = 0;
                    foreach (Match match in matches)
                    {
                        string func_name = match.Groups[1].Value;
                        string ord_a = match.Groups[2].Value;
                        string ord_b = match.Groups[3].Value;
                        string true_target = match.Groups[4].Value;
                        string false_target = match.Groups[5].Value;

                        if (func_name == "equal")
                        {
                            if (!(attributes.Contains(ord_a) && attributes.Contains(ord_b)
                                && attributes[ord_a].ToString() == attributes[ord_b].ToString()))
                            {
                                content = Parser.SubsituteString(content, match.Index + newLength, match.Length, true_target);
                                newLength += true_target.Length - match.Length;
                            }
                            else
                            {
                                content = Parser.SubsituteString(content, match.Index + newLength, match.Length, false_target);
                                newLength += false_target.Length;
                            }
                        }
                    }
                }
            }

            return content;
        }
    }
}
