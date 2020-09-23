using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CometUI
{
    public static class Localization
    {
        const string defaultLang = "default";
        const string localizationFileFolder = "Resources";
        const string localizationFileName = "Localization.csv";

        static string currentLanguage = defaultLang;
        static int currentLanguageIndex = 0;
        static Dictionary<string, string[]> dict = new Dictionary<string, string[]>();
        static string[] languagesInFile = new string[] { defaultLang };

        public static void CreateLocalizationFile()
        {
            dict.Clear();

            //load exists file
            LoadLocalizationFile();

            //grab
            foreach(var str in GrabAllTextsOnScene().Where(s => !string.IsNullOrWhiteSpace(s) && s.Any(c => char.IsLetter(c))))
            {
                if (!dict.ContainsKey(str))
                    dict[str] = new string[] { str };
            }

            //create file
            var dir = Path.Combine(Application.dataPath, localizationFileFolder);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            //write header
            var sb = new StringBuilder();
            sb.AppendLine(string.Join("\t", languagesInFile.Select(s => EncodeString(s))));

            //write lines
            foreach (var item in dict.Values)
            {
                sb.AppendLine(string.Join("\t", item.Select(s=>EncodeString(s))));
            }

            File.WriteAllText(Path.Combine(dir, localizationFileName), sb.ToString());
        }

        public static void LoadLocalizationFile()
        {
            languagesInFile = new string[] { defaultLang };
            dict.Clear();

            var dir = Path.Combine(Application.dataPath, localizationFileFolder);
            var file = Path.Combine(dir, localizationFileName);
            if (!File.Exists(file))
                return;

            var text = File.ReadAllText(file);
            var lines = text.Split(new char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length == 0)
                return;

            languagesInFile = lines[0].Split('\t').Select(s => DecodeString(s.ToLower())).ToArray();

            foreach (var line in lines.Skip(1))
            {
                var item = line.Split('\t').Select(s => DecodeString(s)).ToArray();
                if (item.Length == 0 || string.IsNullOrWhiteSpace(item[0]))
                    continue;
                dict[item[0]] = item;
            }
        }

        public static bool SetCurrentLanguage(CultureInfo cultureInfo)
        {
            return SetCurrentLanguage(cultureInfo.TwoLetterISOLanguageName);
        }

        public static bool SetCurrentLanguage(string twoLettersLanguage)
        {
            twoLettersLanguage = twoLettersLanguage.ToLower();

            var i = Array.IndexOf(languagesInFile, twoLettersLanguage);
            if (i < 0)
            {
                Debug.LogWarning($"Language '{twoLettersLanguage}' is not presented in localization file");
                return false;
            }

            currentLanguage = twoLettersLanguage;
            currentLanguageIndex = i;
            return true;
        }

        public static void TranslateScene()
        {
            foreach (var text in SceneInfoGrabber<Text>.GetUIComponentsOnScene())
                text.text = text.text.Translate();
            foreach (var text in SceneInfoGrabber<TMPro.TextMeshProUGUI>.GetUIComponentsOnScene())
                text.text = text.text.Translate();
            foreach (var dd in SceneInfoGrabber<Dropdown>.GetUIComponentsOnScene())
            foreach (var opt in dd.options)
                opt.text = opt.text.Translate();
        }

        public static string Translate(this string phrase, params string[] parameters)
        {
            if (string.IsNullOrWhiteSpace(phrase))
                return phrase;
            if(dict.TryGetValue(phrase, out var item))
            {
                if (currentLanguageIndex < item.Length)
                {
                    var res = item[currentLanguageIndex];
                    if (!string.IsNullOrEmpty(res))
                        return string.Format(res, parameters);
                }
            }

            return phrase;
        }

        static string EncodeString(string str)
        {
            return str.Replace("\r\n", "\\n").Replace("\r", "\\n").Replace("\n", "\\n").Replace("\t", "\\t");
        }

        static string DecodeString(string str)
        {
            return str.Replace("\\n", Environment.NewLine).Replace("\\t", "\t");
        }

        static IEnumerable<string> GrabAllTextsOnScene()
        {
            foreach (var text in SceneInfoGrabber<Text>.GetUIComponentsOnScene())
                yield return text.text;
            foreach (var text in SceneInfoGrabber<TMPro.TextMeshProUGUI>.GetUIComponentsOnScene())
                yield return text.text;

            foreach (var dd in SceneInfoGrabber<Dropdown>.GetUIComponentsOnScene())
            foreach (var opt in dd.options)
                yield return opt.text;
        }
    }
}
