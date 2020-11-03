// Copyright (c) 2020 Cloudcell Limited

using System;
using System.Collections.Generic;
using System.Xml.Serialization;

[Serializable]
public class UserSettings
{
    public static UserSettings Instance;

    //public string ScriptExecuteCommandLine = @"""%0""";
    public string ExecutableFolderPath = "";
    public string LibraryPath = "../Library";
    public string ProjectFile = "project.pxw";
    //public string DefaultOutputFileName = "output.csv";
    public string InputMetaFileName = "sockets_i.meta";
    public string OutputMetaFileName = "sockets_o.meta";
    public string PathsMetaFileName = "paths.meta";
    //internal string ScriptSearchPattern = "*.R";
    public string RunMetaFileName = "run.meta";
    //public string OutputDataFileExtension = ".csv";
    public string MetaKeyword = ".meta";
    public string OutputGraphicsFolder = "output.graphics";
    public string TempFolder = "tmp.undo";
    public int MaxOutputGraphicsFilesCount = 100;
    public int MaxConsoleLines = 40;
    public int ConsoleFontSize = 14;
}
