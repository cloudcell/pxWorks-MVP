using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace CometUI
{
    /// <summary>UIManager controls opening and closing of views on scene</summary>
    public partial class UIManager : MonoBehaviour
    {
#pragma warning disable 0649
        [Tooltip("Play ShowAnimation when view already shown")]
        [SerializeField] bool ReplayShowAnimation = false;
        [Tooltip("Dynamically created Views will be destroyed after closing")]
        [SerializeField] bool DestroyDynamicViews = true;
        [Tooltip("Children will be closed after Owner is disappeared")]
        [SerializeField] bool CloseChildrenAfterOwner = true;
        [Tooltip("The Back closes App if back stack is empty")]
        [SerializeField] bool CloseAppOnBack = false;
        [Tooltip("Default gesture to Back")]
        [SerializeField] public Gesture BackGesture = Gesture.SwipeLeft;
        [SerializeField] bool CancelCausesBack = true;

        [Header("Default Prefabs")]
        [Tooltip("Prefab for fullscreen fade")]
        [SerializeField] FullscreenFade FullscreenFadePrefab = default;
        [Tooltip("Prefab for Dialog Window")]
        [SerializeField] DialogWindow DialogWindowPrefab = default;
        [Tooltip("Prefab for Dialog Input")]
        [SerializeField] DialogInput DialogInputPrefab = default;
        [Tooltip("Prefab for Background Sounds")]
        [SerializeField] AudioSource BackgroundSoundPrefab = default;
        [Tooltip("Prefab for Overlay Canvas")]
        [SerializeField] Canvas OverlayCanvas = default;
        [Tooltip("Prefab for Icon Creator")]
        [SerializeField] IconCreator IconCreatorPrefab = default;
        [Tooltip("Prefab for TooltipView")]
        [SerializeField] TooltipView TooltipViewPrefab = default;

        [Header("Default Animations")]
        [SerializeField] AnimationLink ShowAnimation = default;
        [SerializeField] AnimationLink CloseAnimation = default;
        [SerializeField] AnimationLink HideAnimation = default;

        [Header("Default Sounds")]
        [SerializeField] public AudioClip ButtonSound = default;
        [SerializeField] public AudioClip BackSwipeSound = default;
        [SerializeField] float FadeVolume = 0.25f;
        [SerializeField] float VolumeChangeSpeed = 1f;

        public static UIManager Instance { get; set; }

        private Dictionary<string, BaseView> staticViews = new Dictionary<string, BaseView>();
        private LinkedList<BaseView> backStack = new LinkedList<BaseView>();
        /// <summary>Stack of opened modal (fullscreen fade) views</summary>
        private LinkedList<BaseView> fullScreenFadeStack = new LinkedList<BaseView>();

        public static Dictionary<string, BaseView> StaticViews => Instance?.staticViews;
        public static LinkedList<BaseView> BackStack => Instance?.backStack;
        /// <summary>Stack of opened modal (fullscreen fade) views</summary>
        public static LinkedList<BaseView> FullScreenFadeStack => Instance?.fullScreenFadeStack;

        private Queue<Action> lateUpdateQueue = new Queue<Action>();


        public SimpleGestures Gestures { get; private set; }
        private AudioSource audioSource = default;
        private Dragger dragger = default;
        private SoundsManager soundsManager = default;
        private IconCreator iconCreator = default;

#if UNITY_EDITOR
        [Header("")]
        public UnityEngine.Object UIGraph;
#endif
#pragma warning restore 0649
        void Awake()
        {
            Instance = this;

            //get my components
            audioSource = GetComponent<AudioSource>();

            //clear static lists
            StaticViews.Clear();
            BackStack.Clear();
            FullScreenFadeStack.Clear();

            //init other objects
            dragger = new Dragger();
            soundsManager = new SoundsManager(BackgroundSoundPrefab, FadeVolume, VolumeChangeSpeed);

            //find all views
            var views = SceneInfoGrabber<BaseView>.GetUIComponentsOnScene(scene: gameObject.scene);

            //grab components for views
            foreach (var view in views)
            {
                view.GrabComponents();
                StaticViews[view.GetType().Name] = view;
            }

            //grab views for views
            foreach (var view in views)
            {
                view.GrabViews(StaticViews);
            }

            //init views
            foreach (var view in views)
                view.Init();

            //subscribe views
            foreach (var view in views)
                view.AutoSubscribe();

            //show views
            foreach (var view in views.Where(v => v.ShowAtStart))
                Show(view, null, noAnimation: true);

            //init gestures
            InitGestures();
        }

        bool cancelAxisFired;

        private void Update()
        {
            //Cancel => Back
            if (Input.GetAxis("Cancel") != 0)
            {
                if (!cancelAxisFired && CancelCausesBack)
                {
                    var res = Back();
                    if (res)
                        PlayOneShotSound(BackSwipeSound, 0.6f);
                }
                cancelAxisFired = true;
            }
            else
            {
                cancelAxisFired = false;
            }

            UpdateTooltips();
        }

        private void LateUpdate()
        {
            //execute late update queue
            while (lateUpdateQueue.Count > 0)
                lateUpdateQueue.Dequeue().Invoke();
        }

        /// <summary>Returns static view of type T</summary>
        public static T GetView<T>() where T : BaseView
        {
            if (StaticViews.TryGetValue(typeof(T).Name, out var res))
                return (T)res;

            return null;
        }

        #region Show/Close/Hide/Back

        /// <summary>Show the view</summary>
        public static void Show(BaseView view, BaseView owner, Action onAnimationDone = null, bool noAnimation = false, bool closeChildrenIfOpened = false)
        {
            //close opened children
            if (closeChildrenIfOpened)
                CloseAllChildren(view);

            //add view to list of children of owner
            AddOpenedChildIfNotPresented(owner, view);

            //already opened?
            if (view.VisibleState == VisibleState.Visible)
                if (!Instance.ReplayShowAnimation)
                {
                    onAnimationDone?.Invoke();
                    return;
                }

            //hidden?
            if (view.VisibleState == VisibleState.Hidden)
            {
                ReopenHidden(view, onAnimationDone);
                return;
            }

            //close concurrents
            if (view.Concurrent)
            {
                var concurrents = GetConcurrentList(view);
                foreach (var concurrent in concurrents)
                    if (concurrent != view)
                        if (concurrent.VisibleState != VisibleState.Closed)
                            Close(concurrent);
            }

            //AddToBackStack
            if (view.BackPrority != BackPrority.IgnoreBack)
                AddToBackStack(view);

            //[todo]
            //if (view.FullscreenFade)
            //    ;

            //hide owner
            if (view.HideOwner && view.Owner != null)
                Hide(view.Owner, noAnimation: noAnimation);

            //set visible state
            view.gameObject.SetActive(true);
            OnViewShown(view);

            //get show animation
            var anim = view.Animations.ShowAnimation ?? Instance.ShowAnimation;

            //play show animation
            if (anim != null && anim.Animation != null && !noAnimation)
                AnimationPlayer.Play(view.RectTransform, anim.Animation, onAnimationDone, true, 1, 1);
            else
                onAnimationDone?.Invoke();

            //show FullscreenFade
            if (view.FullscreenFade && (view.FullscreenFadePrefab != null || Instance.FullscreenFadePrefab != null))
            {
                var prefab = view.FullscreenFadePrefab ?? Instance.FullscreenFadePrefab;
                var fade = Instantiate(prefab, view.transform.parent);
                var i = view.transform.GetSiblingIndex();
                fade.transform.SetSiblingIndex(i);
                Show(fade, view);
            }
        }

        /// <summary>Close the view and all children views</summary>
        public static void Close(BaseView view, Action onAnimationDone = null, bool noAnimation = false)
        {
            //already closed?
            if (view.VisibleState == VisibleState.Closed)
            {
                //view.gameObject.SetActive(false);
                ClearOwnerAndShowIfHidden(view);
                onAnimationDone?.Invoke();
                return;
            }

            //close children w/o anitmation
            if (!Instance.CloseChildrenAfterOwner)
                CloseAllChildren(view);

            //remove from back stack
            while (BackStack.Remove(view)) ;

            //close
            var prevState = view.VisibleState;
            OnViewClosed(view);

            //if was hidden - return
            if (prevState == VisibleState.Hidden)
            {
                view.gameObject.SetActive(false);
                if (Instance.CloseChildrenAfterOwner)
                    CloseAllChildren(view);
                ClearOwnerAndShowIfHidden(view);
                return;
            }

            //get close animation
            var anim = view.Animations.CloseAnimation ?? Instance.CloseAnimation;

            //play animation and deactivate
            Action onDone = () =>
            {
                if (view)
                {
                    view.gameObject.SetActive(false);
                    if (Instance.CloseChildrenAfterOwner)
                        CloseAllChildren(view);
                    if (Instance.DestroyDynamicViews && view.IsDynamicallyCreated)
                        GameObject.Destroy(view.gameObject);
                }
                onAnimationDone?.Invoke();
            };

            var animation = anim?.Animation;
            if (noAnimation)
                animation = null;

            ClearOwnerAndShowIfHidden(view);

            AnimationPlayer.Play(view.RectTransform, animation, onDone, true, 1, 1);
        }

        /// <summary>Make the view invisible, but do not close children</summary>
        public static void Hide(BaseView view, Action onAnimationDone = null, bool noAnimation = false)
        {
            //already hidden?
            if (view.VisibleState == VisibleState.Hidden)
            {
                view.gameObject.SetActive(false);
                onAnimationDone?.Invoke();
                return;
            }

            //hide
            OnViewHidden(view);

            //get hide animation
            var anim = view.Animations.HideAnimation ?? Instance.HideAnimation;
            if (noAnimation)
                anim = null;

            //play animation and deactivate
            Action onDone = () => { view.gameObject.SetActive(false); onAnimationDone?.Invoke(); };

            if (anim != null && anim.Animation != null)
            {
                AnimationPlayer.Play(view.RectTransform, anim.Animation, onDone, true, 1, 1);
            }
            else
            {
                onDone();
            }
        }

        /// <summary>Show the view if closed or hidden, close otherwise</summary>
        public static void ShowOrClose(BaseView view, BaseView owner, Action onAnimationDone = null, bool noAnimation = false)
        {
            switch (view.VisibleState)
            {
                case VisibleState.Visible: Close(view, onAnimationDone); break;
                case VisibleState.Hidden: ReopenHidden(view); break;
                case VisibleState.Closed: Show(view, owner, onAnimationDone); break;
            }
        }

        /// <summary>Close last view in Back stack</summary>
        public static bool Back(Action onAnimationDone = null)
        {
            if (BackStack.Count == 0)
            {
                if (Instance.CloseAppOnBack)
                {
                    Application.Quit();
                    return true;
                }
                return false;
            }

            //get last and remove from back stack
            var view = BackStack.Last.Value;
            if (view.SuppressBack)
                return false;//ignore back for SuppressBack view

            view.Back(onAnimationDone);

            return true;
        }

        /// <summary>Close app</summary>
        public static void CloseApp()
        {
            Application.Quit();
        }

        /// <summary>Closes all children</summary>
        public static void CloseAllChildren(BaseView view)
        {
            //close children w/o anitmation
            foreach (var child in view.OpenedChildren.ToArray())
                if (child is FullscreenFade)
                    Close(child);
                else
                    CloseNoAnimation(child);

            if (view.VisibleState == VisibleState.Hidden)
                ReopenHidden(view);
        }

        /// <summary>Close all children and show the view with previous Owner</summary>
        public static void Reopen(BaseView view, Action onAnimationDone = null, bool noAnimation = false)
        {
            Show(view, view.Owner, onAnimationDone, noAnimation, true);
        }

        private static void ClearOwnerAndShowIfHidden(BaseView view)
        {
            var owner = view.Owner;

            //remove from opened children list
            owner?.OpenedChildren.Remove(view);
            view.Owner = null;

            //reopen hidden owner
            if (view.HideOwner)
                if (owner != null && owner.VisibleState == VisibleState.Hidden)
                {
                    view = owner;
                    ReopenHidden(view);
                }
        }

        private static void ReopenHidden(BaseView view, Action onAnimationDone = null)
        {
            //set visible state
            view.gameObject.SetActive(true);
            OnViewShown(view);

            //get show animation
            var anim = view.Animations.ShowAnimation ?? Instance.ShowAnimation;

            //play show animation
            if (anim != null && anim.Animation != null)
                AnimationPlayer.Play(view.RectTransform, anim.Animation, onAnimationDone, true, 1, 1);
            else
                onAnimationDone?.Invoke();
        }

        private static void CloseNoAnimation(BaseView view)
        {
            if (view.VisibleState != VisibleState.Closed)
            {
                //close children
                foreach (var child in view.OpenedChildren.ToArray())
                    CloseNoAnimation(child);

                //remove from back stack
                while (BackStack.Remove(view)) ;

                //close
                try
                {
                    view.gameObject.SetActive(false);
                    OnViewClosed(view);
                    if (Instance.DestroyDynamicViews && view.IsDynamicallyCreated)
                        GameObject.Destroy(view.gameObject);
                }
                catch
                { // object destroyed?
                }

                //remove from owner list
                view.Owner?.OpenedChildren.Remove(view);
            }
        }

        private static IEnumerable<BaseView> GetConcurrentList(BaseView view)
        {
            var parent = view.RectTransform.parent;
            return parent.OfType<RectTransform>().Select(rt => rt.GetComponent<BaseView>()).Where(v => v != null);
        }

        /// <summary>Returns true if the view is active and focused, it can accept user input.</summary>
        public static bool IsActive(BaseView view)
        {
            if (view != null && view.VisibleState == VisibleState.Visible)
            if (FullScreenFadeStack.Count == 0 || FullScreenFadeStack.Last.Value == view)
                return true;

            return false;
        }

        #endregion

        #region Events

        public static event Action<BaseView> ViewShown;
        public static event Action<BaseView> ViewClosed;
        public static event Action<BaseView> ViewHidden;
        public static event Action<BaseView, VisibleState, VisibleState> ViewVisibleStateChanged;

        private static void OnViewShown(BaseView view)
        {
            var prevState = view.VisibleState;

            (view as IBaseViewInternal).SetVisibleState(VisibleState.Visible);

            if (view.FullscreenFade)
            {
                FullScreenFadeStack.Remove(view);
                FullScreenFadeStack.AddLast(view);
            }

            ViewShown?.Invoke(view);
            ViewVisibleStateChanged?.Invoke(view, prevState, view.VisibleState);
        }

        private static void OnViewClosed(BaseView view)
        {
            var prevState = view.VisibleState;

            (view as IBaseViewInternal).SetVisibleState(VisibleState.Closed);

            if (view.FullscreenFade)
                FullScreenFadeStack.Remove(view);

            ViewClosed?.Invoke(view);
            ViewVisibleStateChanged?.Invoke(view, prevState, view.VisibleState);
        }

        private static void OnViewHidden(BaseView view)
        {
            var prevState = view.VisibleState;

            (view as IBaseViewInternal).SetVisibleState(VisibleState.Hidden);

            if (view.FullscreenFade)
                FullScreenFadeStack.Remove(view);

            ViewHidden?.Invoke(view);
            ViewVisibleStateChanged?.Invoke(view, prevState, view.VisibleState);
        }

        #endregion

        #region ShowCoroutine, ShowAsync

        /// <summary>Shows the view and wait while it will be closed</summary>
        public static IEnumerator ShowCoroutine(BaseView view, BaseView owner, Action onAnimationDone = null, bool noAnimation = false, bool closeChildrenIfOpened = false)
        {
            Show(view, owner, onAnimationDone, noAnimation, closeChildrenIfOpened);
            while (view.VisibleState != VisibleState.Closed)
                yield return null;
        }

        /// <summary>Shows the view and wait while it will be closed</summary>
        public static async Task ShowAsync(BaseView view, BaseView owner, Action onAnimationDone = null, bool noAnimation = false, bool closeChildrenIfOpened = false)
        {
            Show(view, owner, onAnimationDone, noAnimation, closeChildrenIfOpened);
            await TaskEx.WaitWhile(() => view.VisibleState != VisibleState.Closed);
        }

        #endregion

        #region Gestures

        private void InitGestures()
        {
            //init gestures
            Gestures = GetComponent<SimpleGestures>();
            Gestures.onSwipeHoriz += (i) => ProcessGesture(i < 0 ? Gesture.SwipeLeft : Gesture.SwipeRight);
            Gestures.onSwipeVert += (i) => ProcessGesture(i < 0 ? Gesture.SwipeDown : Gesture.SwipeUp);
            Gestures.onTap += (i) => ProcessGesture(Gesture.Tap);
            Gestures.onLongTap += (i) => ProcessGesture(Gesture.LongTap);
            Gestures.onDoubleTap += (i) => ProcessGesture(Gesture.DoubleTap);
            Gestures.onPan += (v) => { if (dragger.IsDragging) dragger.OnDragging(v); };
            Gestures.onDragStart += (v) => dragger.OnDragStart(v, Gestures.LastTouchedUI);
            Gestures.onDragEnd += dragger.OnDragEnd;
        }

        private void ProcessGesture(Gesture gest)
        {
            if (dragger.IsDragging)
                return;//we are in drag mode => ignore gestures

            //get last touched UI
            var ui = Gestures.LastTouchedUI;
            if (ui == null)
            {
                DefaultGestureProcessing(gest);
                return;
            }

            //find touched BaseView
            var view = ui.GetComponentsInParent<BaseView>().FirstOrDefault(v => v.VisibleState != VisibleState.Closed);

            //try process gesture in touched view and it's owners
            var info = new GestureInfo(gest, view);
            while (view != null)
            {
                //call method of view
                view.OnGesture(info);
                if (info.IsHandled || view.SuppressAnyGesturesForOwners)
                    return;//gesture is handled
                //go to owner
                view = view.Owner;
            }

            //gesture is not handled => try process Back
            DefaultGestureProcessing(gest);
        }

        private void DefaultGestureProcessing(Gesture gest)
        {
            //is Back gesture?
            if (gest == BackGesture && BackStack.Count > 0)
            {
                PlayOneShotSound(BackSwipeSound, 0.6f);
                Back();
            }
        }

        #endregion

        #region Dialog Windows

        public static DialogWindow ShowDialog(BaseView owner, string message, string okText = null, string cancelText = null, string ignoreText = null, bool closeByTap = true, Action<DialogResult> onClosed = null)
        {
            var canvas = Instantiate(Instance.OverlayCanvas);
            var view = Instantiate(Instance.DialogWindowPrefab, canvas.transform);
            if (onClosed != null)
                view.Closed += (_) => onClosed(view.DialogResult);

            view.Closed += (_) => Destroy(canvas.gameObject, 1);
            PrepareDialogWindow(view);

            closeByTap = closeByTap && string.IsNullOrEmpty(cancelText) && string.IsNullOrEmpty(ignoreText);

            view.Build(message, okText, cancelText, ignoreText, closeByTap);
            view.Show(owner);
            return view;
        }

        public static async Task<DialogResult> ShowDialogAsync(BaseView owner, string message, string okText = null, string cancelText = null, string ignoreText = null, bool closeByTap = false)
        {
            var canvas = Instantiate(Instance.OverlayCanvas);
            var view = Instantiate(Instance.DialogWindowPrefab, canvas.transform);
            PrepareDialogWindow(view);

            closeByTap = closeByTap && string.IsNullOrEmpty(cancelText) && string.IsNullOrEmpty(ignoreText);

            view.Build(message, okText, cancelText, ignoreText, closeByTap);
            await view.ShowAsync(owner);
            Destroy(canvas.gameObject, 1);
            return view.DialogResult;
        }

        public static IEnumerator ShowDialogCoroutine(BaseView owner, string message, string okText = null, string cancelText = null, string ignoreText = null, bool closeByTap = true, Action<DialogResult> onClosed = null)
        {
            var view = Instantiate(Instance.DialogWindowPrefab);
            PrepareDialogWindow(view);

            closeByTap = closeByTap && string.IsNullOrEmpty(cancelText) && string.IsNullOrEmpty(ignoreText);

            view.Build(message, okText, cancelText, ignoreText, closeByTap);
            yield return view.ShowCoroutine(owner);
            if (onClosed != null)
                onClosed(view.DialogResult);
        }

        public static DialogInput ShowDialogInput(BaseView owner, string message, string text, string placeHolderText = "", string okText = "OK", string cancelText = "Cancel", Action<string> onClosed = null)
        {
            var canvas = Instantiate(Instance.OverlayCanvas);
            var view = Instantiate(Instance.DialogInputPrefab, canvas.transform);
            if (onClosed != null)
                view.Closed += (_) => onClosed(view.Result);

            view.Closed += (_) => Destroy(canvas.gameObject, 1);

            PrepareDialogWindow(view);

            view.Build(message, text, placeHolderText, okText, cancelText);
            view.Show(owner);
            return view;
        }

        private static void PrepareDialogWindow(BaseView view)
        {
            view.GrabComponents();
            view.Init();
            view.AutoSubscribe();
            view.gameObject.SetActive(false);
        }

        public static async Task<string> ShowDialogInputAsync(BaseView owner, string message, string text, string placeHolderText = "", string okText = "OK", string cancelText = "Cancel")
        {
            var canvas = Instantiate(Instance.OverlayCanvas);
            var view = Instantiate(Instance.DialogInputPrefab, canvas.transform);
            PrepareDialogWindow(view);
            view.Build(message, text, placeHolderText, okText, cancelText);
            await view.ShowAsync(owner);
            Destroy(canvas.gameObject, 1);
            return view.Result;
        }

        public static IEnumerator ShowDialogInputCoroutine(BaseView owner, string message, string text, string placeHolderText = "", string okText = "OK", string cancelText = "Cancel", Action<string> onClosed = null)
        {
            var view = Instantiate(Instance.DialogInputPrefab);
            PrepareDialogWindow(view);
            view.Build(message, text, placeHolderText, okText, cancelText);
            yield return view.ShowCoroutine(owner);
            if (onClosed != null)
                onClosed(view.Result);
        }

        #endregion

        #region Sounds

        public static void PlayButtonSound()
        {
            if (Instance.ButtonSound)
                PlayOneShotSound(Instance.ButtonSound, 0.6f);
        }

        public static void PlayOneShotSound(AudioClip clip, float volume = 1f)
        {
            if (clip)
                Instance.audioSource.PlayOneShot(clip, volume);
        }

        #endregion

        #region Create Icons for 3D objects

        /// <summary>Creates Icon for 3D Object (or loads image from cache)</summary>
        public static void CreateIcon(GameObject objPrefab, string id, RawImage targetImage, bool doNotLoadFromCache = false)
        {
            if (Instance.iconCreator == null && Instance.IconCreatorPrefab != null)
            {
                Instance.iconCreator = Instantiate(Instance.IconCreatorPrefab, Instance.transform);
            }

            if (Instance.iconCreator != null)
                Instance.iconCreator.CreateIcon(objPrefab, id, targetImage, doNotLoadFromCache);
        }

        #endregion

        #region Tooltips

        Dictionary<GameObject, Tooltip> toolTipsByObjects = new Dictionary<GameObject, Tooltip>();

        public void RegisterTooltip(Tooltip tooltip)
        {
            var prefab = tooltip.TooltipViewPrefab ?? TooltipViewPrefab;
            if (prefab != null)
                toolTipsByObjects[tooltip.gameObject] = tooltip;
        }

        public void UnRegisterTooltip(Tooltip tooltip)
        {
            if (currentTooltip == tooltip)
                CloseToolTip();
            toolTipsByObjects.Remove(tooltip.gameObject);
        }

        Vector3 prevMousePos;

        private void UpdateTooltips()
        {
            if (toolTipsByObjects.Count == 0)
                return;
            if (Input.mousePosition == Vector3.zero)//default mouse pos for mobile devices
                return;

#if UNITY_STANDALONE || UNITY_WEBGL || UNITY_EDITOR
            //mouse moved ?
            var moved = prevMousePos != Input.mousePosition;
            prevMousePos = Input.mousePosition;
            if (moved)
            {
                CloseToolTip();
                return;
            }
#endif
            //get objects under mouse
            var objects = GetObjectsUnderMouse().ToList();
            foreach (var ui in objects)
            {
                var view = ui.GetComponentsInParent<BaseView>().FirstOrDefault(v => v.VisibleState != VisibleState.Closed);
                if (view != null)
                {
                    if (view is FullscreenFade)
                    {
                        CloseToolTip();//mouse over screen fade
                        return;
                    }
                    break;
                }
            }

            //find tooltip for obects
            foreach (var obj in objects)
            if(toolTipsByObjects.TryGetValue(obj, out var tooltip))
            {
#if UNITY_STANDALONE || UNITY_WEBGL || UNITY_EDITOR
                var click = false;// Input.GetMouseButtonDown(0);
#else
                var click = Input.touches.Length == 1 && Input.touches[0].phase == TouchPhase.Began;
#endif
                if (click && obj.GetComponent<Button>() != null)
                    click = false;//discard forced tooltip for buttons

                //start show tooltip
                StartShowTooltip(tooltip, click);
                return;
            }
            CloseToolTip();
        }

        Tooltip currentTooltip;
        float timeToShow;
        float timeToClose;

        private void StartShowTooltip(Tooltip tooltip, bool forced)
        {
            if (currentTooltip == tooltip)
            {
                if (forced)
                    timeToShow = 0;
                if (timeToShow > 0)
                {
                    timeToShow -= Time.unscaledDeltaTime;
                    return;
                }
                else
                {
                    if (!tooltip.TooltipInstance)
                    {
                        var owner = tooltip.GetComponentInParent<BaseView>();
                        if (owner.VisibleState == VisibleState.Visible)
                        {
                            PlaceAndShowTooltipView(tooltip, owner);
                            return;
                        }
                    }else
                    {
                        if (tooltip.TooltipInstance.VisibleState == VisibleState.Hidden && forced)
                        {
                            var owner = tooltip.GetComponentInParent<BaseView>();
                            if (owner.VisibleState == VisibleState.Visible)
                            {
                                timeToClose = (tooltip.TooltipViewPrefab ?? TooltipViewPrefab).MaxDuration;
                                tooltip.TooltipInstance.Show(owner);
                                return;
                            }
                        }
                    }
                }

                if (timeToClose <= 0f)
                {
                    if (tooltip.TooltipInstance && tooltip.TooltipInstance.VisibleState == VisibleState.Visible)
                        Hide(tooltip.TooltipInstance);
                }else
                {
                    timeToClose -= Time.unscaledDeltaTime;
                }

                return;
            }

            if (currentTooltip != null)
            {
                CloseToolTip();
                return;
            }

            currentTooltip = tooltip;
            timeToShow = forced ? 0 : (tooltip.TooltipViewPrefab ?? TooltipViewPrefab).TooltipDelay;
        }

        private void PlaceAndShowTooltipView(Tooltip tooltip, BaseView owner)
        {
            var canvas = tooltip.GetComponentsInParent<Canvas>().LastOrDefault();
            var view = Instantiate(tooltip.TooltipViewPrefab ?? TooltipViewPrefab, canvas.transform);
            tooltip.TooltipInstance = view;
            view.GrabComponents();
            view.Init();
            view.AutoSubscribe();
            view.gameObject.SetActive(false);
            view.Build(tooltip);
            view.Show(owner);

            timeToClose = (tooltip.TooltipViewPrefab ?? TooltipViewPrefab).MaxDuration;
        }

        public bool CloseToolTip(Action onDone = null)
        {
            if (currentTooltip == null)
            {
                onDone?.Invoke();
                return false;
            }

            var tt = currentTooltip;
            currentTooltip = null;
            if (tt.TooltipInstance)
            {
                if (!tt.TooltipInstance.gameObject.activeSelf)
                {
                    GameObject.Destroy(tt.TooltipInstance.gameObject);
                    tt.TooltipInstance = null;
                }
                else
                    tt.TooltipInstance.Close(onDone);
            }

            return true;
        }

#endregion

        #region Utils

        private static void AddToBackStack(BaseView view)
        {
            //remove if presented
            while (BackStack.Remove(view)) ;

            //add to end
            if (BackStack.Count == 0)
            {
                BackStack.AddLast(view);
                return;
            }

            //find by priority
            var node = BackStack.Last;
            while (node != null && node.Value.BackPrority > view.BackPrority)
                node = node.Previous;

            if (node == null)
            {
                BackStack.AddFirst(view);
                return;
            }
            else
            {
                BackStack.AddAfter(node, view);
            }
        }

        private static void AddOpenedChildIfNotPresented(BaseView owner, BaseView child)
        {
            //hide prev owner 
            if (child.Owner != owner)
                ClearOwnerAndShowIfHidden(child);

            child.Owner = owner;

            if (owner == null)
                return;

            if (!owner.OpenedChildren.Contains(child))
                owner.OpenedChildren.Add(child);
        }

        public static IEnumerable<BaseView> GetViewsUnderMouse()
        {
            var uis = SimpleGestures.GetUIObjectsUnderPosition(Input.mousePosition).Select(r => r.gameObject);
            foreach (var ui in uis)
            {
                var view = ui.GetComponentsInParent<BaseView>().FirstOrDefault(v => v.VisibleState != VisibleState.Closed);
                while (view != null)
                {
                    yield return view;
                    view = view.Owner;
                }
            }
        }

        public static IEnumerable<GameObject> GetObjectsUnderMouse()
        {
            return SimpleGestures.GetUIObjectsUnderPosition(Input.mousePosition).Select(r=>r.gameObject);
        }

        /// <summary>Invokes action in LateUpdate</summary>
        public static void InvokeInMainThread(Action act)
        {
            Instance.lateUpdateQueue.Enqueue(act);
        }

        #endregion
    }

    public enum Gesture
    {
        None, SwipeLeft, SwipeRight, SwipeUp, SwipeDown, Tap, LongTap, DoubleTap
    }

    public class GestureInfo
    {
        /// <summary>Touched BaseView</summary>
        public BaseView TouchedUI { get; }
        /// <summary>Gesture type</summary>
        public Gesture Gesture { get; }
        /// <summary>Set to True to prevent owner to process gesture</summary>
        public bool IsHandled { get; set; }

        public GestureInfo(Gesture gesture, BaseView touchedView)
        {
            Gesture = gesture;
            TouchedUI = touchedView;
        }
    }
}
