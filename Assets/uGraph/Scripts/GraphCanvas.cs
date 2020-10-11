// Copyright (c) 2020 Cloudcell Limited

using CometUI;
using MainScene_UI;
using System;
using System.Diagnostics;
using System.Linq;
using UnityEngine;

namespace uGraph
{
    class GraphCanvas : MonoBehaviour
    {
        Graph graph;

        private void Start()
        {
            SimpleGestures.OnScale += SimpleGestures_OnScale;
            SimpleGestures.OnDoubleTap += SimpleGestures_OnDoubleTap;
            SimpleGestures.OnTap += SimpleGestures_OnTap;
            graph = GetComponent<Graph>();
        }

        private void SimpleGestures_OnTap(Vector2 pos)
        {
            if (UIManager.FullScreenFadeStack.Count > 0)
                return;

            var forMe = SimpleGestures.GetUIObjectsUnderPosition(Input.mousePosition).Any(r => r.gameObject == gameObject);
            if (!forMe)
                return;

            var uis = SimpleGestures.GetUIObjectsUnderPosition(Input.mousePosition).Select(r => r.gameObject).ToArray();

            if (uis.All (ui => ui.GetComponentInParent<UnityEngine.UI.Button>() == null))
            if (uis.Contains(this.gameObject))
            {
                ClearSelected();

                var ika = uis.Select(s => s.GetComponent<InputKnobAcceptor>()).FirstOrDefault(s => s != null);
                if (ika != null)
                {
                    var knob = ika.GetComponentInParent<InputKnob>();
                    OnKnobClick(knob);
                }
                else
                {
                    var node = uis.Select(s => s.GetComponent<Node>()).FirstOrDefault(s => s != null);
                    if (node != null)
                    {
                        node.Selected = true;
                        //Bus.SetStatusLabel += "Id: " + node.Id.ToString();
                    }
                }
                Bus.SelectionChanged += true;
            }
        }

        private void SimpleGestures_OnDoubleTap(Vector2 pos)
        {
            if (UIManager.FullScreenFadeStack.Count > 0)
                return;

            var forMe = SimpleGestures.GetUIObjectsUnderPosition(Input.mousePosition).Any(r => r.gameObject == gameObject);
            if (!forMe)
                return;

            var nodes = graph.NodesHolder.GetComponentsInChildren<Node>();
            foreach (var node in nodes)
            if (node.Selected)
            {
                EditNode(node);
                break;
            }
        }

        private void EditNode(Node node)
        {
            //EditNodeWindow.Instance.Build(node, false);
            //EditNodeWindow.Instance.Show(null);
            Process.Start(node.ProjectDirectory);
        }

        private void SimpleGestures_OnScale(float d)
        {
            if (UIManager.FullScreenFadeStack.Count > 0)
                return;

            var forMe = SimpleGestures.GetUIObjectsUnderPosition(Input.mousePosition).Any(r => r.gameObject == gameObject);
            if (!forMe)
                return;

            var ui = SimpleGestures.Instance.LastTouchedUI;
            if (!ui) return;
            //
            const float MaxScale = 1.5f;
            const float MinScale = 0.1f;
            if (transform.localScale.x > MaxScale && d > 1f)
                return;
            if (transform.localScale.x < MinScale && d < 1f)
                return;
            //
            var obj = new GameObject("", typeof(RectTransform));
            obj.transform.SetParent(transform.parent);
            var rt = obj.transform as RectTransform;
            //rt.position = Input.mousePosition;
            rt.position = new Vector2(Screen.width / 2, Screen.height / 2);
            var parent = transform.parent;
            transform.SetParent(rt, true);
            rt.localScale *= ((d - 1) / 2) + 1;
            transform.SetParent(parent, true);
            //
            DestroyImmediate(obj);
        }

        void Update()
        {
            if (UIManager.FullScreenFadeStack.Count > 0)
                return;

            if (Input.GetKeyDown(KeyCode.Delete))
            {
                DeleteSelected();
            }
        }

        private void DeleteSelected()
        {
            var uis = graph.LinesHolder.GetComponentsInChildren<LineController>();
            foreach (var ui in uis)
            {
                if (ui.LineIsSelected)
                {
                    using (var command = new StateCommand("Remove join"))
                        DeleteLine(ui);
                    return;
                }
            }

            var nodes = graph.NodesHolder.GetComponentsInChildren<Node>();
            foreach (var node in nodes)
            if(node.Selected)
            {
                using (var command = new StateCommand("Remove node"))
                    node.Remove();
            }
        }

        private void DeleteLine(LineController ui)
        {
            var knobs = graph.NodesHolder.GetComponentsInChildren<InputKnob>();
            foreach (var knob in knobs)
                if(knob.lineRenderer != null && knob.lineRenderer.gameObject == ui.gameObject)
                {
                    knob.SetInputConnection(null);
                    break;
                }
        }

        void ClearSelected()
        {
            var uis = graph.LinesHolder.GetComponentsInChildren<LineController>();
            foreach (var ui in uis)
                ui.LineIsSelected = false;

            var nodes = graph.NodesHolder.GetComponentsInChildren<Node>();
            foreach (var node in nodes)
                node.Selected = false;

            Bus.ClosePopupMenu += true;
        }

        public void OnKnobClick(InputKnob knob)
        {
            if (knob.lineRenderer != null)
            {
                var lc = knob.lineRenderer.GetComponent<LineController>();
                lc.LineIsSelected = true;
            }
        }
    }
}
