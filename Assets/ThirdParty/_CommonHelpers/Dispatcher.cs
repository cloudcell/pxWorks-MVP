using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Dispatcher : MonoBehaviour
{
    public static Dispatcher Instance { get; private set; }
    public static event Action LateAwake;
    public static event Action LateLateAwake;
    public static event Action LateUpdate;
    public static event Action LateLateUpdate;
    public static event Action LateFixedUpdate;
    public static event Action LateLateFixedUpdate;

    private readonly LinkedList<DispatcherItem> list = new LinkedList<DispatcherItem>();
    private readonly object _lock = new object();
    private const string autoLoadPrefabName = "AutoLoad";

    [RuntimeInitializeOnLoadMethod]
    static void AutoCreate()
    {
        var go = new GameObject("Dispatcher");
        DontDestroyOnLoad(go);
        Instance = go.AddComponent<Dispatcher>();

        try
        {
            LateAwake?.Invoke();
        }catch{}

        try
        {
            LateLateAwake?.Invoke();
        }
        catch { }
    }

    public void Awake()
    {
        Instance = this;
        StartCoroutine(LateFixedUpdateCoroutine());
        StartCoroutine(LateUpdateCoroutine());

        //add global prefab
        var autoLoads = Resources.LoadAll<GameObject>(autoLoadPrefabName);
        foreach (var a in autoLoads)
            Instantiate(a, this.transform).name = autoLoadPrefabName;
    }

    private IEnumerator LateFixedUpdateCoroutine()
    {
        var waiter = new WaitForFixedUpdate();
        while(true)
        {
            yield return waiter;

            try
            {
                LateFixedUpdate?.Invoke();
            }
            catch { }

            try
            {
                LateLateFixedUpdate?.Invoke();
            }
            catch { }
        }
    }

    private IEnumerator LateUpdateCoroutine()
    {
        var waiter = new WaitForEndOfFrame();
        while (true)
        {
            yield return waiter;

            try
            {
                LateUpdate?.Invoke();
            }
            catch { }

            try
            {
                LateLateUpdate?.Invoke();
            }
            catch { }
        }
    }

    public void Update()
    {
        lock (_lock)
        {
            var node = list.First;
            while(node != null)
            {
                var remove = false;

                try
                {
                    if (node.Value.ExecuteCondition == null || node.Value.ExecuteCondition())
                    {
                        node.Value.Action();
                        remove = true;
                    }
                }
                catch
                {
                    remove = true;
                }

                if (remove)
                {
                    var next = node.Next;
                    list.Remove(node);
                    node = next;
                }
                else
                    node = node.Next;
            }
        }
    }

    public static void Enqueue(Action action)
    {
        Enqueue(null, action);
    }

    public static void Enqueue(Func<bool> executeCondition, Action action)
    {
        lock (Instance._lock)
        {
            Instance.list.AddLast(new DispatcherItem() {Action = action, ExecuteCondition = executeCondition});
        }
    }
}

class DispatcherItem
{
    public Func<bool> ExecuteCondition;
    public Action Action;
}
