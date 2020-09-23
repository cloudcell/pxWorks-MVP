using Signals;
using System.Collections;
using System.Collections.Generic;
using uGraph;
using UnityEngine;

class Bus
{
    public static Signal SceneFilePathChanged;
    public static Signal SceneChanged;
    public static Signal<string> SetStatusLabel;
    public static Signal<object> SelectionChanged;
    public static Signal UpdateToolbars;
    public static Signal SaveUserSettings;
    public static Signal LibraryChanged;
    public static Signal ClosePopupMenu;

    public static State<RunnerState> RunnerState;
    public static Signal RunCompleted;


    static Bus() => BusHelper.InitFields<Bus>();
}
