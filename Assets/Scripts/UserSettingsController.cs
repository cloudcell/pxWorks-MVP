// Copyright (c) 2020 Cloudcell Limited

using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml.Serialization;
using UnityEngine;

public class UserSettingsController : MonoBehaviour
{
    private void Awake()
    {
        //Test();
        LoadSettings();

#if UNITY_EDITOR
        SaveSettings();
#endif
    }

    void Start()
    {
        Bus.SaveUserSettings.Subscribe(this, SaveSettings);
    }

    private void LoadSettings()
    {
        var file = Path.Combine(Application.streamingAssetsPath, "settings.xml");

        var xml = (string)"";
        if (File.Exists(file))
            xml = File.ReadAllText(file);

        if (!string.IsNullOrWhiteSpace(xml))
        {
            using (var sr = new StringReader(xml))
                try
                {
                    UserSettings.Instance = (UserSettings)new XmlSerializer(typeof(UserSettings)).Deserialize(sr);
                }
                catch
                {
                    UserSettings.Instance = new UserSettings();
                }
        }
        else
        {
            UserSettings.Instance = new UserSettings();
        }
    }

    private void SaveSettings()
    {
        var sb = new StringBuilder();
        using (var sr = new StringWriter(sb))
            new XmlSerializer(typeof(UserSettings)).Serialize(sr, UserSettings.Instance);

        var file = Path.Combine(Application.streamingAssetsPath, "settings.xml");
        File.WriteAllText(file, sb.ToString());
    }
}
