using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace CometUI
{
    public class Templates
    {
        Dictionary<string, Template> templates = new Dictionary<string, Template>();

        public Template GetTemplate(string name) => templates[name].Reset();

        public static Templates Parse(string text)
        {
            var result = new Templates();

            var parts = text.Split(new string[] { "#TEMPLATE#" }, StringSplitOptions.None);
            foreach (var part in parts)
            {
                var parts2 = part.Split(new char[] { '\r', '\n' }, 2, StringSplitOptions.RemoveEmptyEntries);
                if (parts2.Length > 1)
                {
                    result.templates[parts2[0].Trim()] = new Template(parts2[1]);
                }
            }

            return result;
        }
    }

    public class Template
    {
        Dictionary<string, List<string>> replaces = new Dictionary<string, List<string>>();
        string template;

        public Template(string template)
        {
            this.template = template;

            //read all replaces
            foreach (Match match in Regex.Matches(template, @"#\w+#"))
                replaces[match.Value] = new List<string>();
        }

        public void Add(string key, string value)
        {
            if (!replaces.TryGetValue(key, out var repl))
                throw new Exception("Replace key is not found: " + key);

            repl.Add(value);
        }

        public Template Reset()
        {
            foreach (var replace in replaces.Values)
                replace.Clear();
            return this;
        }

        public override string ToString()
        {
            var sb = new StringBuilder(template);

            foreach (var replace in replaces)
            {
                var s = string.Join(Environment.NewLine, replace.Value);
                sb = sb.Replace(replace.Key, s);
            }

            return sb.ToString();
        }
    }

    public static class CSharpPreparer
    {
        static int TabSize = 4;

        public static string Prepare(string textCSharp)
        {
            var lines = textCSharp.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())//trim 
                .Where(s => s != "")//remove empty lines
                .ToList();

            //insert empty lines
            for (int i = 0; i < lines.Count; i++)
            {
                var line = lines[i];

                if (line == "{" && i > 1 && lines[i - 2] != "{")
                {
                    for (int j = i - 2; j >= 0; j--)
                    {
                        if (!lines[j].StartsWith("[") &&
                            !lines[j].StartsWith("///"))
                        {
                            lines.Insert(j + 1, "");
                            i++;
                            break;
                        }
                    }
                    continue;
                }
            }

            //insert indents
            var indent = 0;
            for (int i = 0; i < lines.Count; i++)
            {
                var line = lines[i];

                if (line == "}")
                    indent--;

                var newLine = new string(' ', TabSize * indent) + line;

                if (line == "{")
                    indent++;

                lines[i] = newLine;
            }

            //to string
            return string.Join(Environment.NewLine, lines);
        }
    }
}