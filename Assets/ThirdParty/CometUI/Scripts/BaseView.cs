using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UI;
using UnityEngine;
using UnityEngine.UI;

namespace CometUI
{
    public abstract class BaseView : MonoBehaviour, IBaseViewInternal
    {
        public ViewAnimations Animations = new ViewAnimations();
        public ViewSounds Sounds = new ViewSounds();

        [Header("Layout")]
        /// <summary>Show the view at start of scene</summary>
        [Tooltip("Show the view at start of scene")]
        public bool ShowAtStart;
        /// <summary>Auto close opened neighbor views inside my parent</summary>
        [Tooltip("Close opened neighbor views inside my parent when the view is showing")]
        public bool Concurrent;
        /// <summary>When it is closing - close all children views</summary>
        [Tooltip("When it is closing - close all children views")]
        public bool AutoCloseChildren = true;
        /// <summary>Hide owner when opened</summary>
        [Tooltip("Hide owner when opened")]
        public bool HideOwner;
        /// <summary>Auto create fullscreen fade, modal window imitation</summary>
        [Tooltip("Auto create fullscreen fade, modal window imitation")]
        public bool FullscreenFade;
        /// <summary>Prefab for fullscreen fade. If null - will be used default Fade</summary>
        [Tooltip("Prefab for fullscreen fade. If null - will be used default Fade.")]
        public FullscreenFade FullscreenFadePrefab = null;
        /// <summary>Priority in Back stack. Higher priorities will be closed firstly. Value IgnoreBack will disable Back processing.</summary>
        [Tooltip("Priority in Back stack. Higher priorities will be closed firstly. Value IgnoreBack will disable Back processing.")]
        public BackPrority BackPrority = BackPrority.IgnoreBack;
        [Tooltip("Drag&Drop mode for the view")]
        public DragMode DragMode = DragMode.None;

#if UNITY_EDITOR
        [Header("Script Options")]
        [Tooltip("Parameters passed to Build method")]
        public string SignatureOfBuildMethod;
#endif

        [Header("Other")]
        [Tooltip("Button pressing causes OnChanged method")]
        public bool ButtonsCallOnChanged = true;
        [Tooltip("OnChanged method causes Rebuild method")]
        public bool OnChangedCallRebuild = true;
        [Tooltip("Do not pass gestures to owners")]
        public bool SuppressAnyGesturesForOwners = false;
        [Tooltip("Do not close the view by Back command. It allows to take place in Back stack, but blocks closing the view and all views under it.")]
        public bool SuppressBack = false;
        [Tooltip("Create Canvas component. Increases performance.")]
        public bool CreateCanvas = true;

        /// <summary>Logical visibility of the view</summary>
        public VisibleState VisibleState => visibleState;
        /// <summary>Current owner of the view</summary>
        public BaseView Owner { get; set; }
        /// <summary>Currently opened children of the view</summary>
        public List<BaseView> OpenedChildren { get; private set; } = new List<BaseView>();
        /// <summary>Owner for created children</summary>
        public BaseView OwnerForChild => AutoCloseChildren ? this : Owner;
        /// <summary>Data is changed</summary>
        public event Action Changed = delegate { };
        /// <summary>My RectTransform</summary>
        public RectTransform RectTransform => transform as RectTransform;
        /// <summary>True if the view was dynamically created from prefab</summary>
        public bool IsDynamicallyCreated { get; private set; } = false;
        /// <summary>True if the view is active and can receive user Input</summary>
        public bool IsActive => UIManager.IsActive(this);

        #region Private fields

        private Coroutine rebuildCoroutine;
        private int updatingCounter = 0;
        /// <summary>Components cache</summary>
        private Dictionary<(Component, Type), Component> componentCache;
        private VisibleState visibleState = VisibleState.Closed;

        #endregion

        /// <summary>This method is called for static views at scene was loaded.</summary>
        public virtual void Init()
        {   
            gameObject.SetActive(false);

            var mi = GetType().GetProperty("Instance", BindingFlags.Static | BindingFlags.Public);
            if (mi != null)
                mi.SetValue(null, this);

            //CreateCanvas to increase performance
            if (CreateCanvas)
            {
                var canvas = GetComponent<Canvas>();
                if (!canvas)
                    canvas = gameObject.AddComponent<Canvas>();
                var rc = GetComponent<GraphicRaycaster>();
                if (!rc)
                    rc = gameObject.AddComponent<GraphicRaycaster>();
            }
        }

        [VisibleInGraph(false)]
        public virtual void AutoSubscribe()
        {
            //this method is overridden in autogenerated script
        }

        public IEnumerable<BaseView> GetMeAndMyOwners()
        {
            yield return this;

            if (Owner != null)
                foreach (var owner in Owner.GetMeAndMyOwners())
                    yield return owner;
        }

        #region Show/Close/Hide

        /// <summary>Show the view</summary>
        [VisibleInGraph()]
        public void Show(BaseView owner, Action onAnimationDone = null, bool noAnimation = false, bool closeChildrenIfOpened = false)
        {
            UIManager.Show(this, owner, onAnimationDone, noAnimation, closeChildrenIfOpened);
        }

        /// <summary>Close the view and all children views</summary>
        [VisibleInGraph()]
        public void Close(Action onAnimationDone = null, bool noAnimation = false)
        {
            UIManager.Close(this, onAnimationDone, noAnimation);
        }

        /// <summary>Show the view if closed or hidden, close otherwise</summary>
        [VisibleInGraph()]
        public void ShowOrClose(BaseView owner, Action onAnimationDone = null, bool noAnimation = false)
        {
            UIManager.ShowOrClose(this, owner, onAnimationDone, noAnimation);
        }

        /// <summary>Make the view invisible, but do not close children</summary>
        [VisibleInGraph(false)]
        public void Hide(Action onAnimationDone = null, bool noAnimation = false)
        {
            UIManager.Hide(this, onAnimationDone, noAnimation);
        }

        /// <summary>Show the view and close all children (if opened)</summary>
        [VisibleInGraph(true)]
        public void Reopen(BaseView owner, Action onCloseAnimationDone = null, Action onShowAnimationDone = null, bool forcedAnimatedShow = false, bool forcedAnimatedClose = false)
        {
            var noAnimation = VisibleState != VisibleState.Closed;
            if (forcedAnimatedShow)
                noAnimation = false;

            Action onDone = () =>
            {
                onCloseAnimationDone?.Invoke();
                UIManager.Show(this, owner, onShowAnimationDone, noAnimation);
            };

            UIManager.Close(this, onDone, !forcedAnimatedClose);
        }

        /// <summary>Show the view and close all children (if opened)</summary>
        [VisibleInGraph(true)]
        public void ReopenAnimated(BaseView owner, Action onCloseAnimationDone = null, Action onShowAnimationDone = null)
        {
            Reopen(owner, onCloseAnimationDone, onShowAnimationDone, true, true);
        }

        /// <summary>Close last view in Back stack</summary>
        [VisibleInGraph(false)]
        public virtual void Back(Action onAnimationDone = null)
        {
            //close
            Close(onAnimationDone);
        }

        [VisibleInGraph(true)]
        public void CloseAllChildren()
        {
            UIManager.CloseAllChildren(this);
        }

        /// <summary>
        /// Call OnBuild(true) and Show.
        /// This method is suitable only for views with parameterless Build method.
        /// </summary>
        [VisibleInGraph(true)]
        public void BuildAndShow(BaseView owner, Action onAnimationDone = null, bool noAnimation = false, bool closeChildrenIfOpened = false)
        {
            OnBuild(true);
            UIManager.Show(this, owner, onAnimationDone, noAnimation, closeChildrenIfOpened);
        }

        #endregion

        #region Show/Close/Hide Coroutine and Async

        /// <summary>Shows the view and wait while animation will be done</summary>
        public IEnumerator ShowCoroutine(BaseView owner, Action onAnimationDone = null, bool noAnimation = false, bool closeChildrenIfOpened = false)
        {
            var isDone = false;
            Action onDone = () => { isDone = true; onAnimationDone?.Invoke(); };
            Show(owner, onDone, noAnimation, closeChildrenIfOpened);
            while (!isDone)
                yield return null;
        }

        /// <summary>Shows the view and wait while animation will be done</summary>
        public async Task ShowAsync(BaseView owner, Action onAnimationDone = null, bool noAnimation = false, bool closeChildrenIfOpened = false)
        {
            var isDone = false;
            Action onDone = () => { isDone = true; onAnimationDone?.Invoke(); };
            Show(owner, onDone, noAnimation, closeChildrenIfOpened);
            await TaskEx.WaitWhile(() => !isDone);
        }

        /// <summary>Closes the view and wait while animation will be done</summary>
        public IEnumerator CloseCoroutine(Action onAnimationDone = null, bool noAnimation = false)
        {
            var isDone = false;
            Action onDone = () => { isDone = true; onAnimationDone?.Invoke(); };
            Close(onDone, noAnimation);
            while (!isDone)
                yield return null;
        }

        /// <summary>Closes the view and wait while animation will be done</summary>
        public async Task CloseAsync(Action onAnimationDone = null, bool noAnimation = false)
        {
            var isDone = false;
            Action onDone = () => { isDone = true; onAnimationDone?.Invoke(); };
            Close(onDone, noAnimation);
            await TaskEx.WaitWhile(() => !isDone);
        }

        /// <summary>Hides the view and wait while animation will be done</summary>
        public IEnumerator HideCoroutine(Action onAnimationDone = null, bool noAnimation = false)
        {
            var isDone = false;
            Action onDone = () => { isDone = true; onAnimationDone?.Invoke(); };
            Hide(onDone, noAnimation);
            while (!isDone)
                yield return null;
        }

        /// <summary>Hides the view and wait while animation will be done</summary>
        public async Task HideAsync(Action onAnimationDone = null, bool noAnimation = false)
        {
            var isDone = false;
            Action onDone = () => { isDone = true; onAnimationDone?.Invoke(); };
            Hide(onDone, noAnimation);
            await TaskEx.WaitWhile(() => !isDone);
        }

        #endregion

        #region Build (MVVM like pattern)

        /// <summary>Assign data to UI controls, interface updating. 
        /// This method can be overrided and assumes copying data to UI controls
        /// </summary>
        /// <remarks>Usually this method is calling by owner class to build the view. 
        /// Building assumes passing data to View and copying data to UI controls.
        /// This method also can be multiply called during View life. Ususally it is called when user changes value of controls or press buttons.
        /// Also you can call this method manually to update view (also you can use Rebuild calling).
        /// </remarks>
        /// <param name="isFirstBuild">True if outside class passed new data to View and full rebuild required. 
        /// False - means that Data is changed slightly, and you need just refresh View.</param>
        protected virtual void OnBuild(bool isFirstBuild)
        {
            //override this method and copy data to UI controls here
        }

        /// <summary>OnChanged is automatically called when value of controls was changed by user or buttons were pressed.
        /// This method can be overridden, and assumes copying data from UI controls to target data object.</summary>
        protected virtual void OnChanged()
        {
            //
            //override this method and copy data from UI controls to data object
            //

            if (updatingCounter > 0)
                return;//prevent infinite updating loop

            if (rebuildCoroutine == null)
                rebuildCoroutine = StartCoroutine(OnChangedRaw());
        }

        [VisibleInGraph]
        public virtual void Rebuild()
        {
            OnBuildSafe(false);
        }

        private IEnumerator OnChangedRaw()
        {
            //wait while all callbacks are executed
            yield return new WaitForEndOfFrame();

            try
            {
                updatingCounter++;

                //call Rebuild to update interface
                if (OnChangedCallRebuild)
                    Rebuild();

                //call event
                Changed?.Invoke();
            }
            finally
            {
                updatingCounter--;
                rebuildCoroutine = null;
            }
        }

        protected void OnBuildSafe(bool isFirstCall)
        {
            try
            {
                updatingCounter++;
                OnBuild(isFirstCall);
            }
            finally
            {
                updatingCounter--;
            }
        }

        #endregion

        #region Grab components

        /// <summary>Find components inside me and assign to my fileds</summary>
        public virtual void GrabComponents()
        {
            var components = SceneInfoGrabber<BaseView>.GrabInfo(this.transform, false);
            var type = this.GetType();

            foreach (var pair in components)
            {
                var name = pair.Key;
                var fi = type.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (fi != null)
                {
                    //is not null?
                    var obj = fi.GetValue(this);
                    if ((obj as UnityEngine.Object) != null)
                        continue;//do not reassign
                    //
                    var component = pair.Value;
                    var compType = pair.Value.GetType();
                    if (fi.FieldType.IsAssignableFrom(compType))
                    {
                        //assign
                        fi.SetValue(this, component);
                    }
                    else
                        Debug.LogWarning("Type of field is not compatible: " + name);
                }
                else
                {
                    if (SceneInfoGrabber<BaseView>.IsSpecialName(name))
                        Debug.LogWarning("Field is not found: " + name + " in " + GetType().Name);
                }
            }
        }

        internal void GrabViews(Dictionary<string, BaseView> views)
        {
            var type = this.GetType();

            foreach (var pair in views)
            {
                var name = pair.Key;
                var fi = type.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (fi != null)
                {
                    //is not null?
                    if (fi.GetValue(this) != null)
                        continue;//do not reassign
                    //
                    var component = pair.Value;
                    var compType = pair.Value.GetType();
                    if (fi.FieldType.IsAssignableFrom(compType))
                    {
                        //assign
                        fi.SetValue(this, component);
                    }
                    else
                        Debug.LogWarning("Type of field is not compatible: " + name);
                }
            }
        }

        #endregion

        #region Component helpers

        //subscribe Click/OnChanged
        protected void Subscribe(Button bt, Action act)
        {
            bt?.onClick.AddListener(() => { if (updatingCounter == 0) act?.Invoke(); });
        }

        protected void Subscribe(InputField comp, Action<string> act, bool onEndEdit = false)
        {
            if (onEndEdit)
                comp?.onEndEdit.AddListener((v) => { if (updatingCounter == 0) act?.Invoke(v); });
            else
                comp?.onValueChanged.AddListener((v) => { if (updatingCounter == 0) act?.Invoke(v); });
        }

        protected void Subscribe(TMPro.TMP_InputField comp, Action<string> act)
        {
            comp?.onValueChanged.AddListener((v) => { if (updatingCounter == 0) act?.Invoke(v); });
        }

        protected void Subscribe(Slider comp, Action<float> act)
        {
            comp?.onValueChanged.AddListener((v) => { if (updatingCounter == 0) act?.Invoke(v); });
        }

        protected void Subscribe(Dropdown comp, Action<int> act)
        {
            comp?.onValueChanged.AddListener((v) => { if (updatingCounter == 0) act?.Invoke(v); });
        }

        protected void Subscribe(Toggle comp, Action<bool> act)
        {
            comp?.onValueChanged.AddListener((v) => { if (updatingCounter == 0) act?.Invoke(v); });
        }

        protected void Subscribe(BaseView view, Action act)
        {
            view.Changed += act;
        }

        protected void Subscribe(Gesture gesture, Action act)
        {
            Gestured += (info) =>
            {
                if (info.Gesture == gesture)
                {
                    info.IsHandled = true;
                    act();
                }
            };
        }

        //subscribe OnChanged method
        protected void SubscribeOnChanged(Button bt)
        {
            bt?.onClick.AddListener(() => { OnAnyButtonPressed(bt); if (ButtonsCallOnChanged && updatingCounter == 0) OnChanged(); });
        }

        protected void SubscribeOnChanged(InputField comp)
        {
            //comp?.onValueChanged.AddListener((v) => { if (updatingCounter == 0) OnChanged(); });
            comp?.onEndEdit.AddListener((v) => { if (updatingCounter == 0) OnChanged(); });
        }

        protected void SubscribeOnChanged(TMPro.TMP_InputField comp)
        {
            //comp?.onValueChanged.AddListener((v) => { if (updatingCounter == 0) OnChanged(); });
            comp?.onEndEdit.AddListener((v) => { if (updatingCounter == 0) OnChanged(); });
        }

        protected void SubscribeOnChanged(Slider comp)
        {
            comp?.onValueChanged.AddListener((v) => { if (updatingCounter == 0) OnChanged(); });
        }

        protected void SubscribeOnChanged(Dropdown comp)
        {
            comp?.onValueChanged.AddListener((v) => { if (updatingCounter == 0) OnChanged(); });
        }

        protected void SubscribeOnChanged(Toggle comp)
        {
            comp?.onValueChanged.AddListener((v) => { if (updatingCounter == 0) OnChanged(); });
        }

        protected void SubscribeOnChanged(Component comp)
        {
            //nothing (this method for autogenerated script)
        }

        //Set color
        protected void Set(Graphic comp, Color color)
        {
            comp.color = color;
        }

        protected void Set(Component comp, Color color)
        {
            var c0 = Get<Image>(comp);
            if (c0) c0.color = color;
            var c1 = Get<RawImage>(comp);
            if (c1) c1.color = color;
        }

        //Set text
        protected void Set(InputField comp, string text)
        {
            comp.SetTextWithoutNotify(text);
        }

        protected void Set(TMPro.TMP_InputField comp, string text)
        {
            comp.SetTextWithoutNotify(text);
        }

        protected void Set(Text comp, string text)
        {
            comp.text = text;
        }

        protected void Set(TMPro.TextMeshProUGUI comp, string text)
        {
            comp.text = text;
        }

        protected void Set(Component comp, string text)
        {
            var c0 = Get<Text>(comp);
            if (c0) c0.text = text;

            var c1 = Get<TMPro.TextMeshProUGUI>(comp);
            if (c1) c1.text = text;
        }

        //Set bool
        protected void Set(Toggle comp, bool val)
        {
            comp.SetIsOnWithoutNotify(val);
        }

        //Set int
        protected void Set(Slider comp, int val)
        {
            comp.SetValueWithoutNotify(val);
        }

        protected void Set(Dropdown comp, int val)
        {
            comp.SetValueWithoutNotify(val);
        }

        //Set float
        protected void Set(Slider comp, float val)
        {
            comp.SetValueWithoutNotify(val);
        }

        //Set sprite/texture
        protected void Set(Image comp, Sprite sprite)
        {
            comp.sprite = sprite;
        }

        protected void Set(RawImage comp, Texture texture)
        {
            comp.texture = texture;
        }

        protected void Set(Component comp, Sprite sprite)
        {
            var c0 = Get<Image>(comp);
            if (c0) c0.sprite = sprite;
        }

        protected void Set(Component comp, Texture texture)
        {
            var c0 = Get<RawImage>(comp);
            if (c0) c0.texture = texture;
        }

        //set active
        protected void SetActive(Component comp, bool active)
        {
            comp.gameObject.SetActive(active);
        }

        //set Interactable
        protected void SetInteractable(Selectable comp, bool interactable, bool createFade = true)
        {
            if (comp.interactable != interactable)
            {
                comp.interactable = interactable;
                if (createFade && !interactable)
                    CreateFade(comp);
                else
                    RemoveFade(comp);
            }
        }

        protected void SetInteractable(Component comp, bool interactable)
        {
            if (!interactable)
                CreateFade(comp);
            else
                RemoveFade(comp);
        }

        private void RemoveFade(Component comp)
        {
            var gr = comp.GetComponent<CanvasGroup>();
            if (gr)
            {
                gr.interactable = true;
                GameObject.Destroy(gr);
            }
        }

        private void CreateFade(Component comp)
        {
            var gr = comp.GetComponent<CanvasGroup>();
            if (!gr)
                gr = comp.gameObject.AddComponent<CanvasGroup>();
            gr.alpha = 0.45f;
            gr.interactable = false;
        }

        //get string
        protected string GetString(InputField comp)
        {
            return comp?.text;
        }

        protected string GetString(TMPro.TMP_InputField comp)
        {
            return comp?.text;
        }

        protected string GetString(Dropdown comp)
        {
            if (comp == null) return null;
            var index = comp.value;
            if (index < 0 || index >= comp.options.Count)
                return null;
            return comp.options[index].text;
        }

        protected string GetString(TMPro.TMP_Dropdown comp)
        {
            if (comp == null) return null;
            var index = comp.value;
            if (index < 0 || index >= comp.options.Count)
                return null;
            return comp.options[index].text;
        }

        //get float
        protected float GetFloat(Slider comp)
        {
            return comp.value;
        }

        protected float GetFloat(InputField comp)
        {
            var text = comp.text.Trim().Replace(",", ".");
            if (float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var val))
                return val;

            return 0f;
        }

        //get int
        protected int GetInt(Dropdown comp)
        {
            return comp.value;
        }

        protected int GetInt(InputField comp)
        {
            return Mathf.RoundToInt(GetFloat(comp));
        }

        //get bool
        protected bool GetBool(Toggle comp)
        {
            return comp.isOn;
        }

        /// <summary>Get component of type T (used cache)</summary>
        protected T Get<T>(Component component, bool onlyChildren = false) where T : Component
        {
            if (componentCache == null)
                componentCache = new Dictionary<(Component, Type), Component>();

            var key = (component, typeof(T));

            if (!componentCache.TryGetValue(key, out var comp))
            {
                if (onlyChildren)
                    comp = componentCache[key] = component.GetComponentsInChildren<T>(true).FirstOrDefault(c => c.gameObject != component.gameObject);
                else
                    comp = componentCache[key] = component.GetComponentInChildren<T>(true);
            }

            return (T)comp;
        }

        #endregion

        #region Instantiate prefabs

        protected new T Instantiate<T>(T prefab) where T : UnityEngine.Object
        {
            if (prefab is BaseView bv)
            {
                PrepareViewAsPrefab(bv);

                //spawn BaseView in same parent
                var res = GameObject.Instantiate(prefab, bv.transform.parent);
                //subscribe BaseView
                (res as BaseView).IsDynamicallyCreated = true;
                (res as BaseView).AutoSubscribe();
                return res;
            }

            //default spawn for other types
            return GameObject.Instantiate(prefab);
        }

        private static void PrepareViewAsPrefab(BaseView view)
        {
            if (view.CreateCanvas)
            {
                view.CreateCanvas = false;

                //remove canvas for prefabs
                var rc = view.gameObject.GetComponent<GraphicRaycaster>();
                if (rc)
                    GameObject.DestroyImmediate(rc);

                var canvas = view.gameObject.GetComponent<Canvas>();
                if (canvas)
                    GameObject.DestroyImmediate(canvas);
            }
        }

        /// <summary>Clone the view with data</summary>
        [VisibleInGraph(false)]
        public virtual BaseView Clone()
        {
            var clone = Instantiate(this);
            return clone;
        }

        #endregion

        #region Gestures

        public event Action<GestureInfo> Gestured;

        public virtual void OnGesture(GestureInfo info)
        {
            Gestured?.Invoke(info);
        }

        #endregion

        #region Tooltips

        public event Action<TooltipInfo> TooltipRequired;

        public virtual void OnTooltipRequired(TooltipInfo info)
        {
            TooltipRequired?.Invoke(info);
        }

        #endregion

        #region Drag&Drop

        public event Action<BaseView, BaseView> Dropped;
        public event Action<BaseView> StartDrag;

        /// <summary>This method is calling when other view is dragged over the view</summary>
        public virtual bool CanDropIn(BaseView draggedView)
        {
            //override this method and return True if you want to allow drag&drop other view into this view
            return false;
        }

        /// <summary>This method is calling when other view try drop into the view</summary>
        public virtual void DropIn(BaseView draggedView)
        {
            //override this method and return True if you want accept dragged view into this view
        }

        /// <summary>This method is calling when my children is moved to other view</summary>
        public virtual void DropOut(BaseView draggedView, BaseView acceptor)
        {
            //override this method and return True if you want accept dragged view into this view
        }

        /// <summary>This method is calling when the view is successfully dropped into other view</summary>
        public virtual void OnDropped(BaseView acceptor)
        {
            Dropped?.Invoke(this, acceptor);
        }

        /// <summary>This method is calling when the view start dragging</summary>
        public virtual void OnStartDrag()
        {
            StartDrag?.Invoke(this);
        }

        #endregion

        #region Events

        /// <summary>VisibleState of the view was changed</summary>
        public event Action<VisibleState> VisibleStateChanged;
        /// <summary>View became visible</summary>
        public event Action<BaseView> Shown;
        /// <summary>View became closed</summary>
        public event Action<BaseView> Closed;
        /// <summary>View became hidden</summary>
        public event Action<BaseView> Hidden;
        /// <summary>View became closed and disabled</summary>
        public event Action<BaseView> ClosedAndDisabled;
        /// <summary>View became hidden and disabled</summary>
        public event Action<BaseView> HiddenAndDisabled;
        /// <summary>Fired when any button of the View is pressed</summary>
        public event Action<Button> AnyButtonPressed;


        protected virtual void OnVisibleStateChanged(VisibleState newState)
        {
            switch(newState)
            {
                case VisibleState.Visible: OnShown(); break;
                case VisibleState.Closed: OnClosed(); break;
                case VisibleState.Hidden: OnHidden(); break;
            }

            VisibleStateChanged?.Invoke(newState);
        }

        protected virtual void OnShown()
        {
            Shown?.Invoke(this);
        }

        protected virtual void OnClosed()
        {
            Closed?.Invoke(this);
        }

        protected virtual void OnHidden()
        {
            Hidden?.Invoke(this);
        }

        protected virtual void OnDisable()
        {
            switch (VisibleState)
            {
                case VisibleState.Closed: ClosedAndDisabled?.Invoke(this); break;
                case VisibleState.Hidden: HiddenAndDisabled?.Invoke(this); break;
            }
        }

        protected virtual void OnAnyButtonPressed(Button bt)
        {
            if (Sounds.PlayButtonClickSound)
                UIManager.PlayButtonSound();

            AnyButtonPressed?.Invoke(bt);
        }

        #endregion

        #region Utils

        public static Vector2 GetSizeOfRectTransform(RectTransform tr)
        {
            //var scale = tr.GetComponentsInParent<Canvas>().LastOrDefault().scaleFactor;
            //var size = new Vector2(tr.rect.width, tr.rect.height) * scale;
            //return size;

            return Vector2.Scale(tr.rect.size, tr.lossyScale);
        }

        public static Rect GetRectTransformRect(RectTransform tr)
        {
            Vector2 size = Vector2.Scale(tr.rect.size, tr.lossyScale);
            Rect rect = new Rect(tr.position.x, tr.position.y, size.x, size.y);
            rect.x -= tr.pivot.x * size.x;
            rect.y -= tr.pivot.y * size.y;
            return rect;
        }

        public static bool Place(RectTransform transform, Vector3 center, bool keepInsideScreen = true, float maxOutOfScreen = float.MaxValue)
        {
            var size = GetSizeOfRectTransform(transform);
            var pos = new Vector2(center.x - size.x / 2, center.y + size.y / 2);

            if (keepInsideScreen)
            {
                var pivot = new Vector2(size.x * (transform.pivot.x - 0.5f), size.y * (transform.pivot.y - 0.5f));
                pos -= pivot;

                if (pos.x + size.x > Screen.width)
                {
                    pos.x = pos.x - (pos.x + size.x - Screen.width);
                }

                if (pos.x < 0)
                {
                    pos.x = 0;
                }

                if (pos.y > Screen.height)
                {
                    pos.y = pos.y - (pos.y - Screen.height);
                }

                if (pos.y - size.y < 0)
                {
                    pos.y = pos.y - (pos.y - size.y);
                }

                pos += pivot;
            }

            transform.position = pos + new Vector2(size.x * 0.5f, -size.y * 0.5f);

            var visible = center.z >= 0 && center.x > -maxOutOfScreen && center.x - Screen.width < maxOutOfScreen && center.y > -maxOutOfScreen && center.y - Screen.height < maxOutOfScreen;

            if (!visible)
                transform.position = new Vector3(-50000, 0, 0);

            return visible;
        }

        [Obsolete]
        public static void PlaceAround(RectTransform transform, Rect place)
        {
            var size = GetSizeOfRectTransform(transform);
            var pos = new Vector2(place.xMax, place.yMax);
            var pivot = new Vector2(size.x * (transform.pivot.x - 0.5f), size.y * (transform.pivot.y - 0.5f));
            pos -= pivot;

            if (pos.x + size.x > Screen.width)
            {
                pos.x = pos.x - size.x - place.width;
            }

            if (pos.y > Screen.height)
            {
                pos.y = pos.y - (pos.y - Screen.height);
            }

            if (pos.y - size.y < 0)
            {
                pos.y = pos.y - (pos.y - size.y);
            }

            pos += pivot;

            transform.position = pos + new Vector2(size.x * 0.5f, -size.y * 0.5f);
        }

        public static void PlaceAround(RectTransform transform, Rect place, PlaceAppear appearing, bool keepInScreen, bool forcedAppearing, ref bool flipped)
        {
            var size = GetSizeOfRectTransform(transform);

            switch (appearing)
            {
                case PlaceAppear.Right:
                {
                    var pos = new Vector3(place.xMax, place.yMax, 0);
                    pos += new Vector3(size.x, -size.y, 0) / 2;
                    if (keepInScreen && !forcedAppearing)
                    {
                        var outOfScreen = place.xMax + size.x > Screen.width;
                        if (outOfScreen)
                        {
                            flipped = true;
                            PlaceAround(transform, place, PlaceAppear.Left, keepInScreen, true, ref flipped);
                            return;
                        }
                    }

                    Place(transform, pos, keepInScreen);
                    break;
                }

                case PlaceAppear.Left:
                {
                    var pos = new Vector3(place.xMin, place.yMax, 0);
                    pos += new Vector3(-size.x, -size.y, 0) / 2;
                    if (keepInScreen && !forcedAppearing)
                    {
                        var outOfScreen = pos.x < 0;
                        if (outOfScreen)
                        {
                            flipped = true;
                            PlaceAround(transform, place, PlaceAppear.Right, keepInScreen, true, ref flipped);
                            return;
                        }
                    }

                    Place(transform, pos, keepInScreen);
                    break;
                }

                case PlaceAppear.Top:
                {
                    var pos = new Vector3(place.xMin, place.yMax, 0);
                    pos += new Vector3(size.x, size.y, 0) / 2;
                    if (keepInScreen && !forcedAppearing)
                    {
                        var outOfScreen = place.yMax + size.y > Screen.height;
                        if (outOfScreen)
                        {
                            flipped = true;
                            PlaceAround(transform, place, PlaceAppear.Bottom, keepInScreen, true, ref flipped);
                            return;
                        }
                    }

                    Place(transform, pos, keepInScreen);
                    break;
                }

                case PlaceAppear.Bottom:
                {
                    var pos = new Vector3(place.xMin, place.yMin, 0);
                    pos += new Vector3(size.x, -size.y, 0) / 2;
                    if (keepInScreen && !forcedAppearing)
                    {
                        var outOfScreen = place.yMin - size.y < 0;
                        if (outOfScreen)
                        {
                            flipped = true;
                            PlaceAround(transform, place, PlaceAppear.Top, keepInScreen, true, ref flipped);
                            return;
                        }
                    }

                    Place(transform, pos, keepInScreen);
                    break;
                }
            }
        }

        #endregion

        #region IBaseViewInternal

        void IBaseViewInternal.SetVisibleState(VisibleState state)
        {
            if (visibleState != state)
            {
                visibleState = state;
                OnVisibleStateChanged(state);
            }
        }

        void IBaseViewInternal.SetDynamicallyCreated(bool val)
        {
            IsDynamicallyCreated = val;
        }

        #endregion
    }

    public enum PlaceAppear
    {
        NearMouse, Right, Left, Top, Bottom
    }

    [Serializable]
    public class ViewAnimations
    {
        public AnimationLink ShowAnimation;
        public AnimationLink CloseAnimation;
        public AnimationLink HideAnimation;
    }

    /// <summary>Internal interface of BaseView</summary>
    interface IBaseViewInternal
    {
        void SetVisibleState(VisibleState state);
        void SetDynamicallyCreated(bool val);
    }

    [Serializable]
    public enum BackPrority
    {
        IgnoreBack  = 0,
        Lowest  = 1,
        Low     = 2,
        Normal  = 3,
        High    = 4,
        Highest = 5
    }

    [Serializable]
    public enum DragMode
    {
        None, Move, Copy
    }

    [Serializable]
    public enum VisibleState
    {
        Closed, Visible, Hidden
    }

    [AttributeUsage(AttributeTargets.Field)]
    public class AutoGeneratedAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class VisibleInGraphAttribute : Attribute
    {
        public bool Visible { get; set; } = true;

        public VisibleInGraphAttribute(bool visible = true)
        {
            Visible = visible;
        }
    }

    [Serializable]
    public class ViewSounds
    {
        public AudioClip BackgroundSound;
        public OwnerSoundMode OwnerSoundMode = OwnerSoundMode.Normal;
        public AudioClip ShowSound;
        public AudioClip CloseSound;
        [Tooltip("Play default sound on button click")]
        public bool PlayButtonClickSound = true;
        [Tooltip("Always Play background sound from beginning")]
        public bool PlayBackgroundFromStart = true;
    }

    [Serializable]
    public enum OwnerSoundMode
    {
        Normal, Mute, Fade
    }
}