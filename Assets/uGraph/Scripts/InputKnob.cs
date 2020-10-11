// Copyright (c) 2020 Cloudcell Limited

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.Extensions;

namespace uGraph
{
    public class InputKnob : MonoBehaviour
    {
        public Guid Id = Guid.NewGuid();
        public KnobType Type;

        [SerializeField] InputKnobAcceptor inputKnobAcceptor;
        [SerializeField] TMPro.TextMeshProUGUI headerText;
        [SerializeField] UILineRenderer lineRendererPrefab;
        [SerializeField] Image lightImage;
        [SerializeField] Sprite lightOffSprite;
        [SerializeField] Sprite lightOnSprite;

        [HideInInspector] public UILineRenderer lineRenderer;
        Vector2[] points = new Vector2[4];
        bool needToUpdatePoints = false;
        OutputKnob connectedOutputKnob;
        Vector3 prevPos;
        Vector3 prevConnectedPos;

        public OutputKnob JoinedKnob => connectedOutputKnob;
        public Node JoinedNode => connectedOutputKnob?.GetComponentInParent<Node>();

        //temp data
        internal Guid joinedGuid;

        public int LastProcessedDataVersion = 0;

        public string Name
        {
            get => headerText.text;
            set => headerText.text = value;
        }

        public void Init()
        {
            lightImage.color = lightColor;
        }

        Color lightColor
        {
            get
            {
                if (Type == KnobType.signal)
                    return new Color32(255, 150, 255, 255);

                return Color.white;
            }
        }

        public void SetInputConnection(OutputKnob knob)
        {
            var prevConnectedOutputKnob = connectedOutputKnob;

            connectedOutputKnob = knob;
            needToUpdatePoints = true;

            if (knob == null)
            {
                lightImage.sprite = lightOffSprite;
                lightImage.color = lightColor;
                if (lineRenderer != null)
                {
                    DestroyImmediate(lineRenderer.gameObject);
                    lineRenderer = null;
                }
            }
            else
            {
                lightImage.sprite = lightOnSprite;
                lightImage.color = lightColor;
            }

            //update output knobs
            if (prevConnectedOutputKnob != null)
            {
                var count = Graph.Instance.NodesHolder.GetComponentsInChildren<InputKnob>().Count(k => k.JoinedKnob == prevConnectedOutputKnob);
                prevConnectedOutputKnob.OnConnectionChanged(count > 0);
            }

            if (knob != null)
                knob.OnConnectionChanged(true);
        }

        private void Update()
        {
            if (inputKnobAcceptor.transform.position != prevPos)
            {
                prevPos = inputKnobAcceptor.transform.position;
                needToUpdatePoints = true;
            }

            if (connectedOutputKnob != null && connectedOutputKnob.OutputKnobTransform.position != prevConnectedPos)
            {
                prevConnectedPos = connectedOutputKnob.OutputKnobTransform.position;
                needToUpdatePoints = true;
            }

            if (connectedOutputKnob == null && lineRenderer != null)
            {
                DestroyImmediate(lineRenderer.gameObject);
                lineRenderer = null;
                needToUpdatePoints = false;
            }

            if (connectedOutputKnob != null && lineRenderer == null)
            {
                lineRenderer = Instantiate(lineRendererPrefab);
                lineRenderer.transform.SetParent(Graph.Instance.LinesHolder.transform);
                lineRenderer.transform.localScale = new Vector3(1, 1, 1);
                points[3] = points[2] = points[1] = points[0] = new Vector2(0, 0);
                needToUpdatePoints = true;
            }

            if (needToUpdatePoints && connectedOutputKnob != null)
            {
                lineRenderer.transform.position = inputKnobAcceptor.transform.position;

                var p = GetPos(connectedOutputKnob.OutputKnobTransform.position);

                if (p.x < 0)
                    BuildPoints1(p);
                else
                    BuildPoints2(p, p.x);

                lineRenderer.Points = points;
                lineRenderer.RelativeSize = true;
                lineRenderer.drivenExternally = true;

                lineRenderer.color = GetLineColorForType(Type);

                needToUpdatePoints = false;
            }
        }

        public static Color GetLineColorForType(KnobType type)
        {
            switch (type)
            {
                case KnobType.data: return new Color(0.68f, 0.68f, 0.68f, 1);
                case KnobType.signal: return new Color(0.8f, 0f, 0.8f, 1);
            }

            return Color.white;
        }

        private void BuildPoints1(Vector2 p)
        {
            const int d = 60;
            if (points.Length != 4)
                points = new Vector2[4];
            points[0] = new Vector2(0, 0);
            points[1] = new Vector2(-d, 0);
            points[2] = p + new Vector2(d, 0);
            points[3] = p;
        }

        private void BuildPoints2(Vector2 p, float dist)
        {
            int d = 60 + (int)dist / 5;
            if (points.Length != 4)
                points = new Vector2[4];
            points[0] = new Vector2(0, 0);
            points[1] = new Vector2(-d, 0);
            points[2] = p + new Vector2(d, 0);
            points[3] = p;
        }

        Vector2 GetPos(Vector3 pos)
        {
            var p = (pos - lineRenderer.transform.position);
            p *= 1 / lineRenderer.transform.lossyScale.x;
            return p;
        }
    }
}
