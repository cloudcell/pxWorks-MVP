using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

namespace CometUI
{
    /// <summary>Provides gesture recognition</summary>
    public class SimpleGestures : MonoBehaviour
    {
        [SerializeField] Camera Camera;
        [SerializeField] float ZPos = 10;
        [SerializeField] bool DontDestroy = true;

        [Tooltip("Ignores touches over UI elements?")]
        [SerializeField] TouchStrategy touchStrategy = TouchStrategy.AnyTouch;

        [Header("Mouse simulation")]
        [Tooltip("Simulates touch with mouse (left and right buttons).")]
        [SerializeField] bool SimulateTouchWithMouse = true;
        [SerializeField] bool SimulateScaleWithMouseWheel = true;
        [Range(0, 20)]
        [SerializeField] float MouseWheelSensitivity = 4f;

        [Header("Tolerance")]
        [SerializeField] int SwipeTolerance = 25;
        [SerializeField] int RotateTolerance = 10;
        [SerializeField] float ScaleTolerance = 1.10f;
        [SerializeField] float TapMinTime = 0.02f;
        [SerializeField] float LongTapTime = 0.5f;
        [SerializeField] float DoubleTapTime = 0.3f;

        [Header("Inertion")]
        [Range(0.8f, 1)]
        [SerializeField] float PanInertion = 0.95f;
        [Range(0.8f, 1)]
        [SerializeField] float ScaleInertion = 0.94f;
        [Range(0.8f, 1)]
        [SerializeField] float RotateInertion = 0.95f;

        public static SimpleGestures Instance { get => GetOrCreateInstance(); private set => instance = value; }

        public RectTransform LastTouchedUI { get; private set; }
        public Vector2 LastTouchedMousePos { get; private set; }

        #region Events

        public event Action<Vector2> onTap = delegate { };
        public event Action<Vector2> onLongTap = delegate { };
        public event Action<Vector2> onDoubleTap = delegate { };
        /// <summary>Pan in world coordinates relative to Camera</summary>
        public event Action<Vector3> onPan = delegate { };
        /// <summary>Pan in screen coordinates</summary>
        public event Action<Vector2> onDrag = delegate { };
        /// <summary>Start Pan, Drag or Swipe. Passed start screen position.</summary>
        public event Action<Vector2> onDragStart = delegate { };
        /// <summary>Start Pan, Drag or Swipe. Passed start screen position.</summary>
        public event Action<Vector2> onDragEnd = delegate { };
        public event Action<int> onSwipeHoriz = delegate { };
        public event Action<int> onSwipeVert = delegate { };
        public event Action<float> onScale = delegate { };
        public event Action<int> onScaleStart = delegate { };
        public event Action<float> onRotate = delegate { };
        public event Action<int> onRotateStart = delegate { };

        public static event Action<Vector2> OnTap { add { Instance.onTap += value; } remove { Instance.onTap -= value; } }
        public static event Action<Vector2> OnLongTap { add { Instance.onLongTap += value; } remove { Instance.onLongTap -= value; } }
        public static event Action<Vector2> OnDoubleTap { add { Instance.onDoubleTap += value; } remove { Instance.onDoubleTap -= value; } }
        public static event Action<Vector3> OnPan { add { Instance.onPan += value; } remove { Instance.onPan -= value; } }
        public static event Action<Vector2> OnDrag { add { Instance.onDrag += value; } remove { Instance.onDrag -= value; } }
        public static event Action<Vector2> OnDragStart { add { Instance.onDragStart += value; } remove { Instance.onDragStart -= value; } }
        public static event Action<Vector2> OnDragEnd { add { Instance.onDragEnd += value; } remove { Instance.onDragEnd -= value; } }
        public static event Action<int> OnSwipeHoriz { add { Instance.onSwipeHoriz += value; } remove { Instance.onSwipeHoriz -= value; } }
        public static event Action<int> OnSwipeVert { add { Instance.onSwipeVert += value; } remove { Instance.onSwipeVert -= value; } }
        public static event Action<float> OnScale { add { Instance.onScale += value; } remove { Instance.onScale -= value; } }
        public static event Action<int> OnScaleStart { add { Instance.onScaleStart += value; } remove { Instance.onScaleStart -= value; } }
        public static event Action<float> OnRotate { add { Instance.onRotate += value; } remove { Instance.onRotate -= value; } }
        public static event Action<int> OnRotateStart { add { Instance.onRotateStart += value; } remove { Instance.onRotateStart -= value; } }

        #endregion

        #region Private

        private static SimpleGestures instance;
        private Gesture gesture = Gesture.None;
        private int prevTouches;
        private Vector2[] startPos = new Vector2[2];
        private DateTime startModeTime;
        private Vector2 prevPos;
        private float prevDist;
        private Camera _camCached = null;
        private Vector3 panVelocity;
        private float scaleVelocity = 1f;
        private float rotateVelocity = 0f;
        private DateTime lastTapTime;

        private Camera cam
        {
            get
            {
                if (Camera != null)
                    return Camera;
                if (_camCached == null)
                    _camCached = UnityEngine.Camera.main;
                return _camCached;
            }
        }

        #endregion

        enum Gesture
        {
            None, Pending, LongTap, Pan, Scale, Rotate
        }

        enum TouchStrategy
        {
            IgnoreTouchUI, OnlyTouchUI, AnyTouch
        }

        private static SimpleGestures GetOrCreateInstance()
        {
            if (instance == null)
            {
                Debug.LogWarning($"{nameof(SimpleGestures)} is not found on Scene. It will be created automatically.");
                var go = new GameObject() { name = nameof(SimpleGestures) };
                instance = go.AddComponent<SimpleGestures>();
                if (Application.isPlaying)
                    DontDestroyOnLoad(go);
            }

            return instance;
        }

        void Awake()
        {
            if (instance == null)
                Instance = this;

            Input.simulateMouseWithTouches = false;

            if (Application.isPlaying && DontDestroy)
                DontDestroyOnLoad(this);
        }

        void Update()
        {
            var touches = Input.touches.Where(touch => touch.phase != TouchPhase.Ended && touch.phase != TouchPhase.Canceled).ToList();

#if UNITY_STANDALONE || UNITY_EDITOR
            MouseSimulation(touches);
#endif

            //inertion
            if (touches.Count == 0)
                ProcessInertion1();

            if (touches.Count < 2)
                ProcessInertion2();

            if (touches.Count != prevTouches)
            {
                OnTouchCountChanged(touches);
                return;
            }

            if (touches.Count > 0)
            {
                ProcessGesture(touches);
                return;
            }
        }

        private void MouseSimulation(List<Touch> touches)
        {
            if (SimulateTouchWithMouse && (Input.GetMouseButton(0) || Input.GetMouseButton(1)))
            {
                touches.Add(new Touch { position = Input.mousePosition });
            }

            if (SimulateScaleWithMouseWheel && Input.mouseScrollDelta.y != 0f)
            {
                var k = 1 + Input.mouseScrollDelta.y * MouseWheelSensitivity / 100f;
                onScaleStart(k > 1 ? 1 : -1);
                onScale(k);
                scaleVelocity = k;
            }
        }

        private void ProcessInertion1()
        {
            if (panVelocity.sqrMagnitude > 0.0001f && PanInertion > 0.8f)
            {
                panVelocity *= PanInertion;
                onPan?.Invoke(panVelocity);
                return;
            }
            else
            {
                panVelocity = Vector3.zero;
            }
        }

        private void ProcessInertion2()
        {
            if (Mathf.Abs(scaleVelocity - 1) > 0.0001f && ScaleInertion > 0.8f)
            {
                scaleVelocity = Mathf.Lerp(1, scaleVelocity, ScaleInertion);
                onScale?.Invoke(scaleVelocity);
            }
            else
            {
                scaleVelocity = 1;
            }

            if (Mathf.Abs(rotateVelocity) > 0.01f && RotateInertion > 0.8f)
            {
                rotateVelocity *= RotateInertion;
                onRotate?.Invoke(rotateVelocity);
            }
            else
            {
                rotateVelocity = 0;
            }
        }

        private void OnTouchCountChanged(List<Touch> touches)
        {
            if (touches.Count < prevTouches && (prevTouches > 1 || touches.Count > 0))
            {
                gesture = Gesture.Pending;
                prevTouches = touches.Count;
                return;
            }

            //ignore click on GUI
            if (touches.Count == 1 && prevTouches == 0)
            {
                LastTouchedUI = GetUIObjectUnderPosition(touches[0].position)?.transform as RectTransform;
                LastTouchedMousePos = touches[0].position;
                switch (touchStrategy)
                {
                    case TouchStrategy.AnyTouch:
                        break;
                    case TouchStrategy.IgnoreTouchUI:
                        if (LastTouchedUI != null)
                        {
                            gesture = Gesture.Pending;
                            prevTouches = touches.Count;
                            return;
                        }
                        break;
                    case TouchStrategy.OnlyTouchUI:
                        if (LastTouchedUI == null)
                        {
                            gesture = Gesture.Pending;
                            prevTouches = touches.Count;
                            return;
                        }
                        break;
                }
            }

            //change mode
            OnEndMode(prevTouches, touches.Count);
            gesture = Gesture.None;
            prevTouches = touches.Count;
            if (prevTouches >= 1) startPos[0] = touches[0].position;
            if (prevTouches >= 2) startPos[1] = touches[1].position;
            startModeTime = DateTime.Now;
        }

        private void ProcessGesture(List<Touch> touches)
        {
            var pos0 = touches[0].position;
            var pos1 = touches.Count > 1 ? touches[1].position : pos0;

            switch (gesture)
            {
                case Gesture.None:
                    if (touches.Count == 1)
                        ProcessNone1(pos0);
                    else
                        ProcessNone2(pos0, pos1);
                    break;
                case Gesture.Pan:
                    ProcessPan(pos0);
                    break;
                case Gesture.Scale:
                    ProcessScale(pos0, pos1);
                    break;
                case Gesture.Rotate:
                    ProcessRotate(pos0, pos1);
                    break;
            }
        }

        private void OnEndMode(int oldTouches, int newTouches)
        {
            //tap?
            if (gesture == Gesture.None && oldTouches == 1 && newTouches == 0)
            {
                var now = DateTime.Now;
                var time = (now - startModeTime).TotalSeconds;
                if (time > TapMinTime)
                {
                    time = (now - lastTapTime).TotalSeconds;
                    if (time < DoubleTapTime)
                    {
                        //double tap
                        onDoubleTap(startPos[0]);
                    }
                    else
                    {
                        //tap
                        lastTapTime = now;
                        onTap(startPos[0]);
                    }
                }
            }

            //end drag?
            if (gesture == Gesture.Pan)
                onDragEnd?.Invoke(prevPos);
        }

        public static GameObject GetUIObjectUnderPosition(Vector2 pos)
        {
            var eventDataCurrentPosition = new PointerEventData(EventSystem.current);
            eventDataCurrentPosition.position = pos;
            var results = new List<RaycastResult>();
            if (EventSystem.current == null)
                return null;
            EventSystem.current.RaycastAll(eventDataCurrentPosition, results);
            if (results.Count == 0)
                return null;
            results.Sort((r1, r2) =>
            {
                var o1 = r1.sortingOrder;
                var o2 = r2.sortingOrder;
                if (o1 != o2) return o2.CompareTo(o1);
                return r2.depth.CompareTo(r1.depth);
            }
            );
            return results.First().gameObject;
        }

        public static IEnumerable<RaycastResult> GetUIObjectsUnderPosition(Vector2 pos)
        {
            var eventDataCurrentPosition = new PointerEventData(EventSystem.current);
            eventDataCurrentPosition.position = pos;
            var results = new List<RaycastResult>();
            if (EventSystem.current == null)
                yield break;
            EventSystem.current.RaycastAll(eventDataCurrentPosition, results);
            if (results.Count == 0)
                yield break;

            results.Sort((r1, r2) =>
            {
                var o1 = r1.sortingOrder;
                var o2 = r2.sortingOrder;
                if (o1 != o2) return o2.CompareTo(o1);
                return r2.depth.CompareTo(r1.depth);
            });

            foreach (var res in results)
                yield return res;
        }

        /// <summary>Is mouse over UI object?</summary>
        public static bool IsPointerOverUIObject()
        {
            return GetUIObjectUnderPosition(Input.mousePosition) != null;
        }

        #region Process Gestures (1 touch)

        private void ProcessNone1(Vector2 pos)
        {
            var dPos = (pos - startPos[0]);
            if (dPos.sqrMagnitude > SwipeTolerance)
            {
                if (Mathf.Abs(dPos.x) > Mathf.Abs(dPos.y))
                {
                    onDragStart?.Invoke(startPos[0]);
                    onSwipeHoriz((int)Mathf.Sign(dPos.x));
                    prevPos = startPos[0];
                    gesture = Gesture.Pan;
                    ProcessPan(pos);
                }
                else
                {
                    onDragStart?.Invoke(startPos[0]);
                    onSwipeVert((int)Mathf.Sign(dPos.y));
                    prevPos = startPos[0];
                    gesture = Gesture.Pan;
                    ProcessPan(pos);
                }
            }
            else
            {
                var time = (DateTime.Now - startModeTime).TotalSeconds;
                if (time > LongTapTime)
                {
                    gesture = Gesture.LongTap;
                    onLongTap(startPos[0]);
                }
            }
        }

        private void ProcessPan(Vector2 pos)
        {
            if (onPan != null)
            {
                if (cam != null)
                {
                    var p1 = cam.ScreenToWorldPoint(new Vector3(pos.x, pos.y, ZPos));
                    var p2 = cam.ScreenToWorldPoint(new Vector3(prevPos.x, prevPos.y, ZPos));
                    var d = p1 - p2;
                    onPan(d);
                    panVelocity = Vector3.Lerp(panVelocity, d, 0.2f);
                }
            }

            onDrag?.Invoke(pos - prevPos);

            prevPos = pos;
        }

        #endregion

        #region Process Gestures (2 touches)

        private void ProcessNone2(Vector2 pos0, Vector2 pos1)
        {
            var dirStart = startPos[0] - startPos[1];
            var dir = pos0 - pos1;

            var ang = Vector2.SignedAngle(dirStart, dir);

            if (Mathf.Abs(ang) > RotateTolerance)
            {
                onRotateStart((int)Mathf.Sign(ang));
                prevPos = dir;
                gesture = Gesture.Rotate;
                ProcessRotate(pos0, pos1);
            }
            else
            {
                var distPrev = dirStart.magnitude;
                var dist = dir.magnitude;
                var k = dist / distPrev;
                if (k > ScaleTolerance)
                {
                    onScaleStart(1);
                    prevDist = dist;
                    gesture = Gesture.Scale;
                    ProcessScale(pos0, pos1);
                }
                else
                if (k < 1 / ScaleTolerance)
                {
                    onScaleStart(-1);
                    prevDist = dist;
                    gesture = Gesture.Scale;
                    ProcessScale(pos0, pos1);
                }
            }
        }

        private void ProcessRotate(Vector2 pos0, Vector2 pos1)
        {
            var dirStart = startPos[0] - startPos[1];
            var dir = pos0 - pos1;

            var dAng = Vector2.SignedAngle(prevPos, dir);
            onRotate?.Invoke(dAng);
            rotateVelocity = Mathf.Lerp(rotateVelocity, dAng, 0.2f);

            prevPos = dir;
        }

        private void ProcessScale(Vector2 pos0, Vector2 pos1)
        {
            var dirStart = startPos[0] - startPos[1];
            var dir = pos0 - pos1;

            var distStart = dirStart.magnitude;
            var dist = dir.magnitude;
            var k = dist / distStart;
            var prevK = prevDist / distStart;
            k = k / prevK;

            onScale(k);
            scaleVelocity = Mathf.Lerp(scaleVelocity, k, 0.2f);
            prevDist = dist;
        }

        #endregion
    }
}