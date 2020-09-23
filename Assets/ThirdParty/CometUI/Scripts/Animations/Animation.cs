using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace CometUI
{
    public interface IAnimation
    {
        IEnumerator Play(RectTransform rt, float timeScale = 1, int repeatCount = 1);
        void StopAndResetTransform();
    }

    [Serializable]
    public class Animation : IAnimation
    {
        public float TimeScale = 3;
        public int RepeatCount = 1;

        [HideInInspector]
        public List<SingleAnimation> Sequence = new List<SingleAnimation>();

        IEnumerator IAnimation.Play(RectTransform rt, float timeScale, int repeatCount)
        {
            repeatCount *= RepeatCount;

            for (int i=0; i < repeatCount; i++)
            foreach (var a in Sequence.Cast<IAnimation>())
                yield return a.Play(rt, TimeScale * timeScale);
        }

        void IAnimation.StopAndResetTransform()
        {
            foreach (var a in Sequence.Cast<IAnimation>())
                a.StopAndResetTransform();
        }

        public IAnimation CreateInstance()
        {
            var res = (Animation)MemberwiseClone();
            res.Sequence = Sequence.Select(a=>(SingleAnimation)a.CreateInstance()).ToList();
            return res;
        }
    }

    [Serializable]
    public class SingleAnimation : IAnimation
    {
        public float TimeScale = 1;
        public int RepeatCount = 1;

        [HideInInspector]
        public List<BaseAnimation> Animations = new List<BaseAnimation>();

        [NonSerialized]
        TransformInfo source;
        [NonSerialized]
        RectTransform rt;
        [NonSerialized]
        bool stop;

        IEnumerator IAnimation.Play(RectTransform rt, float timeScale, int repeatCount)
        {
            stop = false;
            source = new TransformInfo(rt);
            this.rt = rt;
            timeScale *= TimeScale;
            repeatCount *= RepeatCount;

            foreach (var a in Animations)
                a.Init(rt);

            for (int i = 0; i < repeatCount; i++)
            { 
                var time = 0f;
                while (time < 1)
                {
                    if (stop || !rt)
                        yield break;

                    foreach (var a in Animations)
                        a.Apply(rt, time);

                    yield return null;

                    if (stop || !rt)
                        yield break;

                    time += Time.unscaledDeltaTime * timeScale;
                    source.Apply(rt);
                }
            }

            foreach (var a in Animations)
                a.OnFinish();
        }

        void IAnimation.StopAndResetTransform()
        {
            foreach (var a in Animations)
                a.OnFinish();

            source?.Apply(rt);
            stop = true;
        }

        public IAnimation CreateInstance()
        {
            var res = (SingleAnimation)MemberwiseClone();
            res.Animations = Animations.Select(a => a.Clone()).ToList();
            return res;
        }

        class TransformInfo
        {
            Vector3 localPosition;
            Vector3 localScale;
            Quaternion localRotation;

            public TransformInfo(RectTransform tr)
            {
                localPosition = tr.localPosition;
                localScale = tr.localScale;
                localRotation = tr.localRotation;
            }

            public void Apply(RectTransform tr)
            {
                if (!tr)
                    return;

                tr.localPosition = localPosition;
                tr.localScale = localScale;
                tr.localRotation = localRotation;
            }
        }
    }

    [Serializable]
    public abstract class BaseAnimation
    {
        //public virtual int Priority { get; } = 10;

        [NonSerialized]
        public AnimationCurve Curve = new AnimationCurve();

        [NonSerialized]
        protected Rect rect;

        byte[] raw;

        public void OnAfterDeserialize()
        {
            Curve = new AnimationCurve();

            if (raw != null)
            using (var ms = new MemoryStream(raw))
            using (var bw = new BinaryReader(ms))
            {
                var count = bw.ReadInt32();
                for (int i = 0; i < count; i++)
                {
                    var kf = new Keyframe();
                    kf.time = bw.ReadSingle();
                    kf.value= bw.ReadSingle();
                    kf.inTangent = bw.ReadSingle();
                    kf.outTangent = bw.ReadSingle();
                    kf.inWeight = bw.ReadSingle();
                    kf.outWeight = bw.ReadSingle();
                    kf.weightedMode = (WeightedMode)bw.ReadByte();
                    Curve.AddKey(kf);
                }
            }
                
            raw = null;
        }

        public void OnBeforeSerialize()
        {
            using (var ms = new MemoryStream())
            using (var bw = new BinaryWriter(ms))
            {
                bw.Write(Curve.keys.Length);
                foreach (var kf in Curve.keys)
                {
                    bw.Write(kf.time);
                    bw.Write(kf.value);
                    bw.Write(kf.inTangent);
                    bw.Write(kf.outTangent);
                    bw.Write(kf.inWeight);
                    bw.Write(kf.outWeight);
                    bw.Write((byte)kf.weightedMode);
                }

                raw = ms.ToArray();
            }
        }

        public virtual void Init(RectTransform tr)
        {
            rect = new Rect(0, 0, Screen.width, Screen.height);

            var parent = tr.parent as RectTransform;
            if (parent)
                rect = parent.rect;
        }

        public virtual void OnFinish()
        {
        }

        public BaseAnimation Clone()
        {
            return (BaseAnimation)MemberwiseClone();
        }

        public abstract void Apply(RectTransform tr, float time);
    }

    [Serializable]
    public class ScaleAnimation : BaseAnimation
    {
        [SerializeField]
        public Axis ScaleAxis = ScaleAnimation.Axis.All;

        [SerializeField]
        public float Scale = 1;

        [SerializeField]
        public bool ScaleToParentSize = true;

        public ScaleAnimation()
        {
            Curve.AddKey(0, 0);
            Curve.AddKey(1, 1);
        }

        public enum Axis
        {
            All, X, Y
        }

        public override void Apply(RectTransform tr, float time)
        {
            var scale = tr.localScale;
            var val = Curve.Evaluate(time);
            val *= Scale;

            switch (ScaleAxis)
            {
                case Axis.All: tr.localScale = new Vector3(tr.localScale.x * val, tr.localScale.y * val, 1); break;
                case Axis.X: tr.localScale = new Vector3(tr.localScale.x * val, 1, 1); break;
                case Axis.Y: tr.localScale = new Vector3(1, tr.localScale.y * val, 1); break;
            }
        }
    }

    [Serializable]
    public class SlideAnimation : BaseAnimation, ISerializationCallbackReceiver
    {
        public SlideDirection Direction = SlideAnimation.SlideDirection.RightIn;

        public SlideAnimation()
        {
            Curve.AddKey(new Keyframe(0, 0, 0, 0));
            Curve.AddKey(new Keyframe(1, 1, 0, 0));
        }

        [Serializable]
        public enum SlideDirection
        {
            RightIn, LeftIn, UpIn, DownIn,
            RightOut, LeftOut, UpOut, DownOut
        }

        float padLeft;
        float padRight;
        float padTop;
        float padBottom;

        public override void Init(RectTransform tr)
        {
            base.Init(tr);

            padLeft = (tr.localPosition.x + tr.rect.xMax * tr.localScale.x) - rect.xMin;
            padRight = rect.xMax - (tr.localPosition.x + tr.rect.xMin * tr.localScale.x);

            padTop = rect.yMax - (tr.localPosition.y + tr.rect.yMin * tr.localScale.y);
            padBottom = (tr.localPosition.y + tr.rect.yMax * tr.localScale.y) - rect.yMin;
        }

        public override void Apply(RectTransform tr, float time)
        {
            var pos = tr.localPosition;
            var val = Curve.Evaluate(time);
            
            switch (Direction)
            {
                case SlideDirection.LeftOut: pos = new Vector3(pos.x - val * padLeft, pos.y, pos.z); break;
                case SlideDirection.RightOut: pos = new Vector3(pos.x + val * padRight, pos.y, pos.z); break;
                case SlideDirection.DownOut: pos = new Vector3(pos.x, pos.y - val * padBottom, pos.z); break;
                case SlideDirection.UpOut: pos = new Vector3(pos.x, pos.y + val * padTop, pos.z); break;

                case SlideDirection.RightIn: val = 1 - val; goto case SlideDirection.LeftOut;
                case SlideDirection.LeftIn: val = 1 - val; goto case SlideDirection.RightOut;
                case SlideDirection.UpIn: val = 1 - val; goto case SlideDirection.DownOut;
                case SlideDirection.DownIn: val = 1 - val; goto case SlideDirection.UpOut;
            }

            tr.localPosition = pos;
        }
    }

    [Serializable]
    public class OffsetAnimation : BaseAnimation
    {
        [SerializeField]
        public Axis OffsetAxis = OffsetAnimation.Axis.X;

        [SerializeField]
        public float Amplitude = 1;

        [SerializeField]
        public bool ScaleToParentSize = true;

        [Serializable]
        public enum Axis
        {
            X, Y
        }

        public OffsetAnimation()
        {
            Curve.AddKey(0, 0);
            Curve.AddKey(0.25f, 0.1f);
            Curve.AddKey(0.75f, -0.1f);
            Curve.AddKey(1, 0f);
        }

        public override void Apply(RectTransform tr, float time)
        {
            var pos = tr.localPosition;
            var val = Curve.Evaluate(time);

            val *= Amplitude;

            switch (OffsetAxis)
            {
                case Axis.X:
                    if (ScaleToParentSize)
                        val *= rect.width;
                    pos = new Vector3(pos.x + val, pos.y, pos.z);
                    break;
                case Axis.Y:
                    if (ScaleToParentSize)
                        val *= rect.height;
                    pos = new Vector3(pos.x, pos.y + val, pos.z);
                    break;
            }

            tr.localPosition = pos;
        }
    }

    [Serializable]
    public class RotateAnimation : BaseAnimation
    {
        public RotateAnimation()
        {
            Curve.AddKey(0, 0);
            Curve.AddKey(1, 1);
        }

        public override void Apply(RectTransform tr, float time)
        {
            var val = Curve.Evaluate(time);
            tr.Rotate(new Vector3(0, 0, val * 360), Space.Self);
        }
    }

    [Serializable]
    public class FadeAnimation : BaseAnimation
    {
        public FadeAnimation()
        {
            Curve.AddKey(0, 0);
            Curve.AddKey(1, 1);
        }

        [NonSerialized]
        CanvasGroup cg;
        [NonSerialized]
        bool createdCg;

        public override void Init(RectTransform tr)
        {
            base.Init(tr);

            createdCg = false;
            cg = tr.GetComponent<CanvasGroup>();
            if (!cg)
            {
                cg = tr.gameObject.AddComponent<CanvasGroup>();
                createdCg = true;
            }
        }

        public override void OnFinish()
        {
            base.OnFinish();

            if (createdCg)
                GameObject.Destroy(cg);
        }

        public override void Apply(RectTransform tr, float time)
        {
            if (cg)
            {
                var val = Curve.Evaluate(time);
                cg.alpha = val;
            }
        }
    }

    [Serializable]
    public class MoveAnimation : BaseAnimation
    {
        Vector2 from;
        Vector2 to;

        public MoveAnimation(Vector2 from, Vector2 to)
        {
            this.from = from;
            this.to = to;

            Curve.AddKey(0, 0);
            Curve.AddKey(0.25f, 0.1f);
            Curve.AddKey(0.75f, -0.1f);
            Curve.AddKey(1, 0f);
        }

        public override void Apply(RectTransform tr, float time)
        {
            var val = Curve.Evaluate(time);
            tr.localPosition = Vector2.Lerp(from, to, val);
        }
    }
}