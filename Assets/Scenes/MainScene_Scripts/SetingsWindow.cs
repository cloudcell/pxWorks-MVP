// Copyright (c) 2020 Cloudcell Limited

using System;
using CometUI;

namespace MainScene_UI
{
    partial class SetingsWindow : BaseView
    {
        private void Start()
        {
            //subscribe buttons or events here
            Subscribe(btOk, Save);
        }

        private void Save()
        {
            var settings = UserSettings.Instance;
            var prevLibPath = settings.LibraryPath;

            settings.ExecutableFolderPath = GetString(ifPath);
            settings.LibraryPath= GetString(ifLibraryPath);
            settings.ProjectFile= GetString(ifProjectFile);
            settings.OutputGraphicsFolder= GetString(ifOutputGraphicsFolder);
            settings.InputMetaFileName= GetString(ifInputMetaFileName);
            settings.OutputMetaFileName= GetString(ifOutputMetaFileName);
            settings.PathsMetaFileName= GetString(ifPathsMetaFileName);
            settings.RunMetaFileName= GetString(ifRunMetaFileName);

            Bus.SaveUserSettings += true;

            if(prevLibPath != settings.LibraryPath)
                Bus.LibraryChanged += true;

            Close();
        }

        protected override void OnBuild(bool isFirstBuild)
        {
            //copy data to UI controls here
            if (isFirstBuild)
            {
                var settings = UserSettings.Instance;
                Set(ifPath, settings.ExecutableFolderPath);
                Set(ifLibraryPath, settings.LibraryPath);
                Set(ifProjectFile, settings.ProjectFile);
                Set(ifOutputGraphicsFolder, settings.OutputGraphicsFolder);
                Set(ifInputMetaFileName, settings.InputMetaFileName);
                Set(ifOutputMetaFileName, settings.OutputMetaFileName);
                Set(ifPathsMetaFileName, settings.PathsMetaFileName);
                Set(ifRunMetaFileName, settings.RunMetaFileName);
            }
        }
        
        protected override void OnChanged()
        {
            //copy data from UI controls to data object
            //...
            base.OnChanged();
        }
    }
}