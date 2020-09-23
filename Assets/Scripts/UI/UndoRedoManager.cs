using System;
using System.Collections.Generic;

/// <summary>
/// Undo/Redo manager
/// </summary>
public class UndoRedoManager
{
    LinkedList<UndoableCommand> history;
    Stack<UndoableCommand> redoStack = new Stack<UndoableCommand>();
    int maxHistoryLength;
    int updating = 0;

    public static int DefaultMaxHistoryLength = 20;

    public static UndoRedoManager Instance
    {
        get
        {
            if (sessions.Count == 0)
                CreateNewSession();
            return sessions.Peek();
        }
    }

    private static Stack<UndoRedoManager> sessions = new Stack<UndoRedoManager>();

    public static int SessionsCount
    {
        get { return sessions.Count; }
    }

    public static void CreateNewSession(int maxHistoryLength = -1)
    {
        sessions.Push(new UndoRedoManager(maxHistoryLength));
    }

    public static void ReturnToPrevSession()
    {
        sessions.Pop();
    }

    public UndoRedoManager(int maxHistoryLength = -1)
    {
        this.maxHistoryLength = maxHistoryLength;
        if (maxHistoryLength == -1)
            this.maxHistoryLength = DefaultMaxHistoryLength;
        history = new LinkedList<UndoableCommand>();
    }

    public virtual void Add(UndoableCommand cmd)
    {
        if (updating > 0)
            return;

        history.AddLast(cmd);
        if (history.Count > maxHistoryLength)
            history.RemoveFirst();

        redoStack.Clear();
    }

    public virtual void Add(Action undo, Action redo, string description = "")
    {
        Add(new ActionCommand(undo, redo) { ActionDescription = description});
    }

    public void Undo()
    {
        if (history.Count > 0)
        {
            var cmd = history.Last.Value;
            history.RemoveLast();
            //
            updating++;//prevent text changing into handlers
            try
            {
                cmd.Undo();
            }
            finally
            {
                updating--;
            }
            //
            redoStack.Push(cmd);
        }
    }


    public void ClearHistory()
    {
        history.Clear();
        redoStack.Clear();
    }

    public void Redo()
    {
        if (redoStack.Count == 0)
            return;

        updating++;//prevent text changing into handlers
        try
        {
            var cmd = redoStack.Pop();
            cmd.Redo();

            history.AddLast(cmd);
            if (history.Count > maxHistoryLength)
                history.RemoveFirst();
        }
        finally
        {
            updating--;
        }

    }

    public bool CanUndo(out string actionDescription)
    {
        actionDescription = "";
        if (history.Count == 0)
            return false;
        actionDescription = history.Last.Value.ActionDescription;
        return true;
    }

    public bool CanRedo(out string actionDescription)
    {
        actionDescription = "";
        if (redoStack.Count == 0)
            return false;
        actionDescription = redoStack.Peek().ActionDescription;
        return true;
    }
}

public abstract class UndoableCommand
{
    public string ActionDescription { get; set; }
    public abstract void Undo();
    public abstract void Redo();
}

public class ActionCommand : UndoableCommand
{
    Action redo;
    Action undo;

    public ActionCommand(Action undo, Action redo)
    {
        this.redo = redo;
        this.undo = undo;
    }

    public override void Undo()
    {
        if (undo != null)
            undo();
    }

    public override void Redo()
    {
        if (redo != null)
            redo();
    }
}

public class MultipleCommand : UndoableCommand
{
    UndoableCommand[] commands;

    public MultipleCommand(params UndoableCommand[] commands)
    {
        this.commands = commands;
    }

    public override void Undo()
    {
        for(int i = commands.Length - 1; i>=0; i--)
            commands[i].Undo();
    }

    public override void Redo()
    {
        foreach (var cmd in commands)
            cmd.Redo();
    }
}