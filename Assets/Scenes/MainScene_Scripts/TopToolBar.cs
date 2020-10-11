// Copyright (c) 2020 Cloudcell Limited

using System;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using CometUI;
using System.Threading.Tasks;
using uGraph;
using System.IO;
using System.Linq;
using System.Diagnostics;
using Signals;

namespace MainScene_UI
{
    partial class TopToolBar : BaseView
    {
        [SerializeField] uGraph.Graph graph;
        public static bool saveLogFile = false;

        private void Start()
        {
            if (!Bus.EULA.Value)
                Close();

            Bus.SetStatusLabel.Subscribe(this, s => Set(lbStatus, s));
            Bus.SceneChanged.Subscribe(this, Rebuild);
            Bus.SelectionChanged.Subscribe(this, (m) => Rebuild());
            Bus.UpdateToolbars.Subscribe(this, Rebuild);
            Bus.SceneFilePathChanged.Subscribe(this, OnSceneFilePathChanged);

            //subscribe buttons or events here
            Subscribe(btNew, OnNewScene);
            Subscribe(btOpen, OnOpenScene);
            //Subscribe(btSave, () => SaverLoaderController.Instance.Save(graph, false));
            //Subscribe(btSaveAs, () => SaverLoaderController.Instance.Save(graph, true));
            Subscribe(btUndo, () => UndoRedoManager.Instance.Undo());
            Subscribe(btRedo, () => UndoRedoManager.Instance.Redo());
            Subscribe(btPlay, () => RunOrStop());
            Subscribe(btPause, () => Pause());
            Subscribe(btWindowState, () => WindowStateController.Instance.ChangeWinState());
            Subscribe(btFullScreen, () => Screen.fullScreen = true);
            Subscribe(btTest, () => Test());
            Subscribe(btCleanUp, () => CleanUp());
            Subscribe(btDebug, () => { saveLogFile = !saveLogFile; btNew.Select(); });

            Subscribe(btOpenGraphics, () => GraphicsWindowController.Instance.OpenGraphicsWindow());

            //
            Build();

            Application.quitting += Application_quitting;
            Bus.EULA.Subscribe(this, (a) => { if (a) Show(null); }).CallWhenInactive();

            SignalBase.LogSignals = saveLogFile;
        }

        private void OnSceneFilePathChanged()
        {
            if (!string.IsNullOrEmpty(Graph.Instance.SceneFilePath))
            {
                txProjectLink.text = Path.GetDirectoryName(Graph.Instance.SceneFilePath);
                SetActive(txProjectLink, true);
            }
            else
            {
                SetActive(txProjectLink, false);
            }
        }

        private void CleanUp()
        {
            UIManager.ShowDialog(this, "Confirm Cleanup?", "Cleanup", "Cancel", closeByTap: false, onClosed: (res) =>
            {
                if (res == DialogResult.Ok)
                {
                    GraphHelper.CleanUp();
                }
            });
        }

        private void Pause()
        {
            if (Bus.RunnerState == RunnerState.Run)
                Bus.RunnerState += RunnerState.Pause;
            else
            if (Bus.RunnerState == RunnerState.Pause)
                Bus.RunnerState += RunnerState.Run;
        }

        private void Application_quitting()
        {
            if (Graph.Instance.DirectoryIsDefined)
                SaverLoader.Save(Graph.Instance, Graph.Instance.SceneFilePath);
        }

        [SerializeField] Sprite PlayIcon;
        [SerializeField] Sprite StopIcon;

        private void RunOrStop()
        {
            try
            {
                if (Bus.RunnerState != RunnerState.Stop)
                {
                    Runner.Instance.Stop();
                    return;
                }
                
                Runner.Instance.Run(graph);
            }catch(Exception ex)
            {
                UIManager.ShowDialog(this, ex.Message, "Ok");
            }
        }

        private void AddNode()
        {
            if (!Graph.Instance.DirectoryIsDefined)
            {
                UIManager.ShowDialog(null, "Project directory does not exist or is not defined." + Environment.NewLine + "Create new project.", "Ok");
                return;
            }

            AddNodeWindow.Build();
            AddNodeWindow.Show(this);
        }

        private void Test()
        {
            Bus.SaveUserSettings += true;
        }

        private void Update()
        {
            if (Time.frameCount % 2 == 0)
            {
                SetActive(btWindowState, Screen.fullScreenMode != FullScreenMode.Windowed);
                SetActive(btQuit, Screen.fullScreenMode != FullScreenMode.Windowed);
                SetActive(btFullScreen, false /* Screen.fullScreenMode != FullScreenMode.FullScreenWindow*/);

                if (Screen.fullScreen)
                {
                    var res = Screen.resolutions[Screen.resolutions.Length - 1];
                    if (Screen.currentResolution.width != res.width || Screen.currentResolution.height != res.height)
                        Screen.SetResolution(res.width, res.height, true);
                }

                //Bus.SetStatusLabel += "" + Screen.height + " " + Screen.safeArea + " " + Screen.currentResolution.height;
                //if (Screen.fullScreenMode == FullScreenMode.MaximizedWindow)
                //    Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
                //Bus.SetStatusLabel += "" + Screen.fullScreenMode;

                SignalBase.LogSignals = saveLogFile;
            }

            if (Bus.RunnerState == RunnerState.Stop)
            {
                Set(imPlayIcon, PlayIcon);
                SetInteractable(btPause, false);
            }
            else
            {
                Set(imPlayIcon, StopIcon);
                SetInteractable(btPause, true);
                var colors = btPause.colors;
                if (Bus.RunnerState == RunnerState.Pause)
                    colors.normalColor = Color.white;
                else
                    colors.normalColor = new Color(1, 1, 1, 0);
                btPause.colors = colors;
            }

            {
                var colors = btDebug.colors;
                if (saveLogFile)
                {
                    colors.normalColor = Color.white;
                    colors.selectedColor = new Color(1, 1, 1, 0.55f);
                    colors.highlightedColor = new Color(1, 1, 1, 0.55f);
                }
                else
                {
                    colors.normalColor = new Color(1, 1, 1, 0);
                    colors.selectedColor = new Color(1, 1, 1, 0.3f);
                    colors.highlightedColor = new Color(1, 1, 1, 0.3f);
                }
                btDebug.colors = colors;
            }

            //UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
        }


        //public static async Task<bool> AskAboutDiscardChanges()
        //{
        //    if (Graph.Instance.IsDirty)
        //    {
        //        var res = await UIManager.ShowDialogAsync(TopToolBar.Instance, "The scene has been modified.\r\nDo you want to save it?", "Yes", "No");
        //        if (res != DialogResult.Cancel)
        //            return false;
        //    }

        //    return true;
        //}

        private void OnOpenScene()
        {
            if (Graph.Instance.DirectoryIsDefined)
                SaverLoader.Save(Graph.Instance, Graph.Instance.SceneFilePath);

            SaverLoaderController.Instance.Load(graph);
        }

        private void OnNewScene()
        {
            if (Graph.Instance.DirectoryIsDefined)
                SaverLoader.Save(Graph.Instance, Graph.Instance.SceneFilePath);

            SaverLoaderController.Instance.New(graph);
        }

        protected override void OnBuild(bool isFirstBuild)
        {
            //copy data to UI controls here
            if (UndoRedoManager.Instance.CanUndo(out var desc))
            {
                SetInteractable(btUndo, true, true);
                btUndo.GetComponent<Tooltip>().TextLeft = "Undo " + desc;
            }
            else
            {
                SetInteractable(btUndo, false, true);
                btUndo.GetComponent<Tooltip>().TextLeft = "";
            }

            if (UndoRedoManager.Instance.CanRedo(out var descRedo))
            {
                SetInteractable(btRedo, true, true);
                btRedo.GetComponent<Tooltip>().TextLeft = "Redo " + descRedo;
            }
            else
            {
                SetInteractable(btRedo, false, true);
                btRedo.GetComponent<Tooltip>().TextLeft = "";
            }

            //
            SetActive(btTest, Application.isEditor);
            SetInteractable(btCleanUp, graph.DirectoryIsDefined);
            SetInteractable(btOpenGraphics, graph.DirectoryIsDefined);
        }
    }
}