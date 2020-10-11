// Copyright (c) 2020 Cloudcell Limited

using System;
using System.IO;

namespace uGraph
{
    public class StateCommand : UndoableCommand, IDisposable
    {
        Action redo;
        Action undo;

        public StateCommand(string actionDesc)
        {
            ActionDescription = actionDesc;

            var buffer = new byte[0];
            using (var ms = new MemoryStream())
            {
                SaverLoader.SavePxw(Graph.Instance, ms);
                buffer = ms.ToArray();
            }

            undo = () =>
            {
                using (var ms = new MemoryStream(buffer))
                {
                    SaverLoader.Load(Graph.Instance, ms, true);
                }
                SaverLoader.Save(Graph.Instance, Graph.Instance.SceneFilePath);
                Bus.SceneChanged += true;
            };

            UndoRedoManager.Instance.Add(this);
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

        public void Dispose()
        {
            var buffer = new byte[0];
            using (var ms = new MemoryStream())
            {
                SaverLoader.SavePxw(Graph.Instance, ms);
                buffer = ms.ToArray();
            }

            redo = () =>
            {
                using (var ms = new MemoryStream(buffer))
                    SaverLoader.Load(Graph.Instance, ms, true);
                SaverLoader.Save(Graph.Instance, Graph.Instance.SceneFilePath);
                Bus.SceneChanged += true;
            };

            SaverLoader.Save(Graph.Instance, Graph.Instance.SceneFilePath);
            Bus.SceneChanged += true;
        }
    }
}
