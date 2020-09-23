using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class Helper
{
    public static bool Contains(this LayerMask layers, GameObject gameObject)
    {
        return 0 != (layers.value & 1 << gameObject.layer);
    }

    public static float ClampAngle(float angle, float from, float to)
	{
		var mid = (from + to) / 2;
		if (Mathf.Abs(mid - angle) > 180)
		{
			if (mid > angle)
				angle += 360;
			else
				angle -= 360;
		}

		angle = Mathf.Clamp(angle, from, to);
		if (angle > 360) angle -= 360;
		if (angle < 0) angle += 360;

		return angle;
	}

    /// <summary>
    /// Угол по горизонтали и по вертикали между двумя векторами
    /// </summary>
    public static Vector2 SignedAngles(Vector3 forward, Vector3 vector)
    {
        var v1 = new Vector3(forward.x, 0, forward.z);
        var v2 = new Vector3(vector.x, 0, vector.z);
        var horizAngle = Vector3.SignedAngle(v1, v2, Vector3.up);
        var vertAngle = Mathf.Atan2(vector.y, v2.magnitude) * Mathf.Rad2Deg;

        return new Vector2(horizAngle, vertAngle);
    }

    public static float Lerp(float val, float minVal, float maxVal, float minRes, float maxRes)
    {
        if (val <= minVal) return minRes;
        if (val >= maxVal) return maxRes;
        var k = (val - minVal) / (maxVal - minVal);
        return minRes * (1 - k) + maxRes * k;
    }

    public static void DestroyAllChildren(this GameObject obj)
    {
        var c = obj.transform.childCount;
        for (int i = c - 1; i >= 0; i--)
        {
            GameObject.Destroy(obj.transform.GetChild(i).gameObject);
        }
    }

    public static void DestroyAllChildrenImmediate(this GameObject obj)
    {
        var c = obj.transform.childCount;
        for (int i = c - 1; i >= 0; i--)
        {
            GameObject.DestroyImmediate(obj.transform.GetChild(i).gameObject);
        }
    }

    /// <summary>
    /// Finds active and inactive objects by name
    /// </summary>
    public static GameObject FindObject(this GameObject parent, string name)
    {
        var trs = parent.GetComponentsInChildren<Transform>(true);
        foreach (Transform t in trs)
        {
            if (t.name == name)
            {
                return t.gameObject;
            }
        }
        return null;
    }

    /// <summary>
    /// Finds active and inactive objects by name
    /// </summary>
    public static T FindObject<T>(this GameObject parent, string name)
    {
        var trs = parent.GetComponentsInChildren<Transform>(true);
        foreach (Transform t in trs)
        {
            if (t.name == name)
            {
                return t.gameObject.GetComponent<T>();
            }
        }
        return default(T);
    }

    /// <summary>
    /// Finds active and inactive objects
    /// </summary>
    public static IEnumerable<T> FindObjects<T>(bool bOnlyRoot) where T : Component
    {
        T[] pAllObjects = (T[])Resources.FindObjectsOfTypeAll(typeof(T));

        foreach (T pObject in pAllObjects)
        {
            if (bOnlyRoot)
            {
                if (pObject.transform.parent != null)
                {
                    continue;
                }
            }

            if (pObject.hideFlags == HideFlags.NotEditable || pObject.hideFlags == HideFlags.HideAndDontSave)
            {
                continue;
            }

#if UNITY_EDITOR
            if (Application.isEditor)
            {
                string sAssetPath = UnityEditor.AssetDatabase.GetAssetPath(pObject.transform.root.gameObject);
                if (!string.IsNullOrEmpty(sAssetPath))
                {
                    continue;
                }
            }
#endif
            yield return pObject;
        }
    }

    static List<RaycastHit> hitsPool = new List<RaycastHit>();

    /// <summary>
    /// Returns hits ordered by distance. Shooter's colliders are ignored.
    /// </summary>
    public static List<RaycastHit> GetOrderedHits(Ray ray, float maxDist = 100, int layerMask = Physics.DefaultRaycastLayers, GameObject ignoredObject = null)
    {
        hitsPool.Clear();

        var arr = Physics.RaycastAll(ray, maxDist, layerMask, QueryTriggerInteraction.Ignore);
        if (arr.Length == 0)
            return hitsPool;

        for (int i = 0; i < arr.Length; i++)
        {
            //if it is not shooter collider...
            if (ignoredObject == null || !arr[i].collider.gameObject.transform.IsChildOf(ignoredObject.transform))
                hitsPool.Add(arr[i]);
        }

        //sort
        hitsPool.Sort((h1, h2) => h1.distance.CompareTo(h2.distance));

        return hitsPool;
    }

    public static IEnumerable<GameObject> GetAllParents(this GameObject obj, bool includeMe)
    {
        var parent = obj.transform;
        var first = true;

        while (parent != null)
        {
            if (!first || includeMe)
                yield return parent.gameObject;
            
            first = false;
            if (parent == parent.parent)
                break;
            parent = parent.parent;
        }
    }

    public static Bounds GetTotalBounds(IEnumerable<GameObject> objects, Func<Renderer, bool> allow)
    {
        var first = objects.FirstOrDefault();
        if (first == null)
            return new Bounds();
        var res = GetTotalBounds(first, allow);
        foreach (var obj in objects)
            res.Encapsulate(GetTotalBounds(obj, allow));

        return res;
    }

    public static Bounds GetTotalBounds(GameObject obj, Func<Renderer, bool> allow = null)
    {
        var rends = obj.GetComponentsInChildren<Renderer>().Where(b=>allow == null || allow(b));
        if (!rends.Any())
            return new Bounds(obj.transform.position, new Vector3(1, 1, 1));
        else
        {
            var b = rends.First().bounds;

            foreach(var rend in rends.Skip(1))
            {
                b.Encapsulate(rend.bounds);
            }
            return b;
        }
    }
	
	public static Bounds GetTotalBoundsAccurate(GameObject obj, Func<MeshFilter, bool> allow = null)
    {
        var meshes = obj.GetComponentsInChildren<MeshFilter>().Where(b => allow == null || allow(b)).ToArray();

        if (meshes.Length == 0)
            return new Bounds(obj.transform.position, new Vector3(1, 1, 1));

        Bounds bounds = default;
        for (int i = 0; i < meshes.Length; i ++)
        {
            var tr = meshes[i].transform;
            var vertices = meshes[i].mesh.vertices;

            if (i == 0)
                bounds = new Bounds(tr.TransformPoint(vertices[0]), Vector3.zero);

            for (int j = 0; j < vertices.Length; j++)
                bounds.Encapsulate(tr.TransformPoint(vertices[j]));
        }

        return bounds;
    }	

    public static void SetGlobalScale(Transform transform, Vector3 scale)
    {
        transform.localScale = Vector3.one;
        transform.localScale = new Vector3(scale.x / transform.lossyScale.x, scale.y / transform.lossyScale.y, scale.z / transform.lossyScale.z);
    }

    public static bool IsMouseOverGUI
    {
        get { return GUIUtility.hotControl != 0 || UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject(); }
    }

    public static T GetOrAddComponent<T>(this GameObject obj) where T : Component
    {
        var c = obj.GetComponent<T>();
        if (c == null)
            c = obj.AddComponent<T>();

        return c;
    }

    public static Coroutine StartCoroutine(this MonoBehaviour mn, IEnumerator func, Action onFinished)
    {
        return mn.StartCoroutine(StartCoroutine(func, onFinished));
    }

    private static IEnumerator StartCoroutine(IEnumerator func, Action onFinished)
    {
        yield return func;
        onFinished();
    }


    /// <summary>
    ///     Call delegate by time with normalized time in parameter (time scaled)
    /// </summary>
    /// <param name="mn"></param>
    /// <param name="func">Delegate with parameter float [0..1]</param>
    /// <param name="time">Time to work</param>
    /// <param name="endFunc">Delegate after end</param>
    // ReSharper disable once UnusedMember.Global
    public static Coroutine InvokeDelegate(this MonoBehaviour mn, Action<float> func, float time, Action endFunc = null) 
	{
        return mn.StartCoroutine(InvokeDelegateCor(func, time, endFunc));
    }
 
    private static IEnumerator InvokeDelegateCor(Action<float> func, float time, Action endFunc) 
	{
        var timer = 0f;
        while (timer < time) 
		{
            func(timer / time);
            yield return null;
            timer += Time.deltaTime;
        }
 
        func(1f);
        if (endFunc != null)
            endFunc();
    }

    public static Vector3 XZ(this Vector3 v)
    {
        return new Vector3(v.x, 0, v.z);
    }

    public static Vector3 XY(this Vector3 v)
    {
        return new Vector3(v.x, v.y, 0);
    }

    public static float ToSignedAngle(float angleFrom0to360)
    {
        if (angleFrom0to360 > 180)
            angleFrom0to360 = angleFrom0to360 - 360;
        return angleFrom0to360;
    }
}
