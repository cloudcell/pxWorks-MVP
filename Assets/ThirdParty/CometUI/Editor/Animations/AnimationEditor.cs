using System;
using UnityEditor;
using UnityEngine;

namespace CometUI
{
    //[CustomEditor(typeof(ChainAnimation), true)]
    //public class ChainAnimationEditor : Editor
    //{
    //    Texture2D logo;

    //    void OnEnable()
    //    {
    //        logo = Resources.Load<Texture2D>("comet_icon_big");
    //    }

    //    public override void OnInspectorGUI()
    //    {
    //        GUILayout.Space(30);
    //        GUI.DrawTexture(new Rect(0, 2, 100, 26), logo, ScaleMode.StretchToFill, true);

    //        DrawDefaultInspector();
    //    }
    //}

    [CustomEditor(typeof(AnimationLink), true)]
    public class AnimationEditor : Editor
    {
        Texture2D logo;

        void OnEnable()
        {
            logo = Resources.Load<Texture2D>("comet_icon_big");
            foldout = null;
            var chain = (target as AnimationLink).Animation;
            if (chain != null && chain.Sequence.Count > 0)
                foldout = chain.Sequence[0];
        }

        SingleAnimation singleAnimation;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            GUILayout.Space(30);
            GUI.DrawTexture(new Rect(0, 2, 100, 26), logo, ScaleMode.StretchToFill, true);

            var chain = (target as AnimationLink).Animation;

            chain.TimeScale = EditorGUILayout.FloatField("Time Scale", chain.TimeScale);
            chain.RepeatCount = EditorGUILayout.IntField("Repeat Count", chain.RepeatCount);

            try
            {
                var i = 0;
                foreach (var sa in chain.Sequence)
                {
                    singleAnimation = sa;
                    GUILayout.Space(10);
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.HelpBox("Animation " + (++i), MessageType.None);
                    if (GUILayout.Button("X", GUILayout.Width(20)))
                    {
                        (this.target as AnimationLink).Animation.Sequence.Remove(sa);
                        EditorUtility.SetDirty(this.target);
                        break;
                    }
                    EditorGUILayout.EndHorizontal();
                    DrawSingleAnimation(singleAnimation);
                    EditorGUILayout.EndVertical();
                }
            }catch
            {
                //deleted animation
            }

            GUILayout.Space(20);

            if (GUILayout.Button("Add Chain Animation"))
            {
                var sa = new SingleAnimation();
                chain.Sequence.Add(sa);
                foldout = sa;
                EditorUtility.SetDirty(target);
            }

            serializedObject.ApplyModifiedProperties();
            //
            EditorUtility.SetDirty(target);
        }

        SingleAnimation foldout;

        public void DrawSingleAnimation(SingleAnimation target)
        {
            var title = target.Animations.Count > 0 ? "" : "Empty";
            foreach (BaseAnimation a in target.Animations)
            {
                title += a.GetType().Name + " + ";
            }

            title = title.TrimEnd(' ', '+');

            var style = new GUIStyle(EditorStyles.foldout);
            style.margin = new RectOffset(15, 0, 0, 0);

            if (EditorGUILayout.Foldout(foldout == target, title, true,  style))
            {
                foldout = target;
                target.TimeScale = EditorGUILayout.FloatField("Time Scale", target.TimeScale);
                target.RepeatCount = EditorGUILayout.IntField("Repeat Count", target.RepeatCount);

                try
                {
                    foreach (BaseAnimation a in target.Animations)
                    {
                        GUILayout.Space(10);
                        switch (a.GetType().Name)
                        {
                            case nameof(SlideAnimation):
                                DrawGUI(a as SlideAnimation);
                                break;
                            case nameof(OffsetAnimation):
                                DrawGUI(a as OffsetAnimation);
                                break;
                            case nameof(ScaleAnimation):
                                DrawGUI(a as ScaleAnimation);
                                break;
                            case nameof(RotateAnimation):
                                DrawGUI(a as RotateAnimation);
                                break;
                            case nameof(FadeAnimation):
                                DrawGUI(a as FadeAnimation);
                                break;
                        }
                    }
                }
                catch
                {
                    //deleted animation
                }

                if (target.Animations.Count > 0)
                    GUILayout.Space(30);
                GUILayout.Label("Add Animation:");

                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Slide"))
                {
                    var a = new SlideAnimation();
                    (target as SingleAnimation).Animations.Add(a);
                    EditorUtility.SetDirty(this.target);
                }

                if (GUILayout.Button("Scale"))
                {
                    var a = new ScaleAnimation();
                    (target as SingleAnimation).Animations.Add(a);
                    EditorUtility.SetDirty(this.target);
                }

                if (GUILayout.Button("Offset"))
                {
                    var a = new OffsetAnimation();
                    (target as SingleAnimation).Animations.Add(a);
                    EditorUtility.SetDirty(this.target);
                }

                if (GUILayout.Button("Rotate"))
                {
                    var a = new RotateAnimation();
                    (target as SingleAnimation).Animations.Add(a);
                    EditorUtility.SetDirty(this.target);
                }

                if (GUILayout.Button("Fade"))
                {
                    var a = new FadeAnimation();
                    (target as SingleAnimation).Animations.Add(a);
                    EditorUtility.SetDirty(this.target);
                }
                GUILayout.EndHorizontal();
            }
            else
            {
                if (foldout == target)
                    foldout = null;
            }
        }

        private void DrawGUI(FadeAnimation anim)
        {
            GUILayout.BeginHorizontal();

            GUILayout.BeginVertical();
            GUILayout.Label("Fade", GUILayout.Width(50));
            if (GUILayout.Button("X", GUILayout.Width(20))) Remove(anim);
            GUILayout.EndVertical();

            GUILayout.BeginVertical();
            DrawCurveGui(anim);
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
        }

        private void DrawGUI(RotateAnimation anim)
        {
            GUILayout.BeginHorizontal();

            GUILayout.BeginVertical();
            GUILayout.Label("Rotate", GUILayout.Width(50));
            if(GUILayout.Button("X", GUILayout.Width(20))) Remove(anim);
            GUILayout.EndVertical();

            GUILayout.BeginVertical();
            DrawCurveGui(anim);
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
        }

        private void Remove(BaseAnimation anim)
        {
            singleAnimation.Animations.Remove(anim);
            EditorUtility.SetDirty(this.target);
        }

        private void DrawGUI(ScaleAnimation anim)
        {
            GUILayout.BeginHorizontal();

            GUILayout.BeginVertical();
            GUILayout.Label("Scale", GUILayout.Width(50));
            if (GUILayout.Button("X", GUILayout.Width(20))) Remove(anim);
            GUILayout.EndVertical();

            GUILayout.BeginVertical();
            anim.ScaleAxis = (ScaleAnimation.Axis)EditorGUILayout.EnumPopup("Axis", anim.ScaleAxis);
            anim.Scale = EditorGUILayout.FloatField("Scale", anim.Scale);
            DrawCurveGui(anim);
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
        }

        private void DrawGUI(OffsetAnimation anim)
        {
            GUILayout.BeginHorizontal();

            GUILayout.BeginVertical();
            GUILayout.Label("Offset", GUILayout.Width(50));
            if (GUILayout.Button("X", GUILayout.Width(20))) Remove(anim);
            GUILayout.EndVertical();

            GUILayout.BeginVertical();
            anim.OffsetAxis = (OffsetAnimation.Axis)EditorGUILayout.EnumPopup("Axis", anim.OffsetAxis);
            anim.Amplitude = EditorGUILayout.FloatField("Amplitude", anim.Amplitude);
            anim.ScaleToParentSize = EditorGUILayout.Toggle("Scale To Parent Size", anim.ScaleToParentSize);
            DrawCurveGui(anim);
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
        }

        private void DrawCurveGui(BaseAnimation anim)
        {
            anim.Curve = EditorGUILayout.CurveField(anim.Curve, GUILayout.Height(100));
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Flip X"))
            {
                FlipX(anim.Curve);
            }
            if (GUILayout.Button("Flip Y"))
            {
                FlipY(anim.Curve);
            }
            if (GUILayout.Button("Mirror Y"))
            {
                MirrorY(anim.Curve);
            }
            GUILayout.EndHorizontal();
        }

        private void FlipX(AnimationCurve curve)
        {
            var frames = curve.keys;
            for (int i = 0; i < frames.Length; i++)
            {
                frames[i].time = 1f - frames[i].time;
                frames[i].inTangent *= -1;
                frames[i].outTangent *= -1;
            }

            curve.keys = frames;
        }

        private void FlipY(AnimationCurve curve)
        {
            var frames = curve.keys;
            var negative = (frames[frames.Length - 1].value + frames[0].value) / 2 < 0.2f;
            for (int i = 0; i < frames.Length; i++)
            {
                frames[i].value = negative ? -1f - frames[i].value : 1f - frames[i].value;
                frames[i].inTangent *= -1;
                frames[i].outTangent *= -1;
            }

            curve.keys = frames;
        }

        private void MirrorY(AnimationCurve curve)
        {
            var frames = curve.keys;
            for (int i = 0; i < frames.Length; i++)
            {
                frames[i].value = -frames[i].value;
                frames[i].inTangent *= -1;
                frames[i].outTangent *= -1;
            }

            curve.keys = frames;
        }

        private void DrawGUI(SlideAnimation anim)
        {
            GUILayout.BeginHorizontal();

            GUILayout.BeginVertical();
            GUILayout.Label("Slide", GUILayout.Width(50));
            if (GUILayout.Button("X", GUILayout.Width(20))) Remove(anim);
            GUILayout.EndVertical();

            GUILayout.BeginVertical();
            anim.Direction = (SlideAnimation.SlideDirection)EditorGUILayout.EnumPopup("Direction", anim.Direction);
            DrawCurveGui(anim);
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
        }
    }
}