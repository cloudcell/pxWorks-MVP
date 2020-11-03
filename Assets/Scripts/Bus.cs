// Copyright (c) 2020 Cloudcell Limited

using Signals;
using System.Collections;
using System.Collections.Generic;
using uGraph;
using UnityEngine;

class Bus
{
    public static Signal SceneFilePathChanged;
    public static Signal SceneChanged;
    [HideInLog]
    public static Signal<string> SetStatusLabel;
    [HideInLog]
    public static Signal<string> SetStatusLabelSelectedNode;
    [HideInLog]
    public static Signal<object> SelectionChanged;
    [HideInLog]
    public static State<Node> SelectedNode;
    [HideInLog]
    public static Signal UpdateToolbars;
    public static Signal SaveUserSettings;
    public static Signal LibraryChanged;
    [HideInLog]
    public static Signal ClosePopupMenu;
    public static State<bool> EULA;

    public static State<RunnerState> RunnerState;
    public static Signal RunCompleted;


    static Bus() => BusHelper.InitFields<Bus>();
}
