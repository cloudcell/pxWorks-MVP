using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace CometUI
{
    /// <summary>Creates Icon for 3D object (or loads from cache)</summary>
    public class IconCreator : MonoBehaviour
    {
        [SerializeField] string IconCacheSubDirectory = "IconCache";
        [SerializeField] string IconCreatorLayer = "Icon";
        [SerializeField] float IconPadding = 0.2f;

        [SerializeField] Camera Camera;
        [SerializeField] Transform ObjectHolder;
        [SerializeField] GameObject Holder;
        [Header("Cache")]
        [SerializeField] bool TryLoadFromCache = true;
        [SerializeField] bool SaveToCache = true;
        [SerializeField] CacheFormatEnum CacheFileFormat = CacheFormatEnum.Png;

        private string CacheDirectory => Path.Combine(Application.persistentDataPath, IconCacheSubDirectory);
        private string GetIconFileName(string id) => Path.Combine(CacheDirectory, $"{id}.png");
        private Queue<ModelData> queue = new Queue<ModelData>();
        private bool weAreInWork = false;

        class ModelData
        {
            public GameObject Object;
            public string Id;
            public RawImage TargetImage;
        }

        enum CacheFormatEnum
        {
            Png, Jpg
        }

        void Awake()
        {
            var layer = LayerMask.NameToLayer(IconCreatorLayer);
            if (layer == -1)
                throw new Exception($"Please, create Layer '{IconCreatorLayer}'");
            SetLayerRecursively(gameObject, layer);

            var mask = LayerMask.GetMask(IconCreatorLayer);
            foreach (var light in GetComponentsInChildren<Light>())
                light.cullingMask = mask;

            Camera.cullingMask = mask;

            //switch off camera
            Holder.SetActive(false);
        }

        /// <summary>Creates Icon for given 3d Object (or loads from cache)</summary>
        public void CreateIcon(GameObject objPrefab, string id, RawImage targetImage, bool doNotLoadFromCache = false)
        {
            //try get cache
            if (TryLoadFromCache && !doNotLoadFromCache)
            {
                var file = GetIconFileName(id);
                if (File.Exists(file))
                {
                    var texture = LoadTexture(file);
                    targetImage.texture = texture;
                    return;
                }
            }

            //send to render queue
            var data = new ModelData { Object = objPrefab, Id = id, TargetImage = targetImage };
            queue.Enqueue(data);
        }

        private void Update()
        {
            if (queue.Count > 0 && !weAreInWork)
            {
                //run coroutine to render icons
                StartCoroutine(CreateIconAsync());
            }
        }

        private IEnumerator CreateIconAsync()
        {
            weAreInWork = true;

            //wait camera initialization
            yield return null;

            try
            {
                //enumerate items to render
                while (queue.Count > 0)
                {
                    var data = queue.Dequeue();
                    if (data.Object != null)
                        yield return CreateIconAsync(data);
                }
            }
            finally
            {
                weAreInWork = false;
            }
        }

        private IEnumerator CreateIconAsync(ModelData data)
        {
            //remove all childs of holder
            RemoveAllChildren(ObjectHolder.gameObject);

            //create clone of object
            var clone = Instantiate(data.Object, ObjectHolder.transform.position, ObjectHolder.transform.rotation, ObjectHolder);
            clone.SetActive(true);
            var layer = LayerMask.NameToLayer(IconCreatorLayer);
            if (layer == -1)
                throw new Exception($"Please, create Layer '{IconCreatorLayer}'");
            SetLayerRecursively(clone, layer);

            //calc bounds
            var bounds = GetTotalBounds(clone);
            //clone.transform.localPosition -= new Vector3(bounds.center.x, bounds.center.y, 0);
            var maxSize = Mathf.Max(bounds.extents.x, bounds.extents.y);

            //render
            Holder.SetActive(true);
            Camera.orthographicSize = maxSize + IconPadding;
            yield return null;
            Holder.SetActive(false);

            //destroy clone
            Destroy(clone);

            //get texture
            var texture = ToTexture2D(Camera.targetTexture);
            data.TargetImage.texture = texture;

            //save to cache
            if (SaveToCache)
            {
                if (!Directory.Exists(CacheDirectory))
                    Directory.CreateDirectory(CacheDirectory);

                var file = GetIconFileName(data.Id);

                switch (CacheFileFormat)
                {
                    case CacheFormatEnum.Png: SaveTexturePng(texture, file); break;
                    case CacheFormatEnum.Jpg: SaveTextureJpg(texture, file); break;
                }
                
            }
        }

        #region Utils

        static void SaveTexturePng(Texture2D texture, string filePath)
        {
            var bytes = texture.EncodeToPNG();
            File.WriteAllBytes(filePath, bytes);
        }

        static void SaveTextureJpg(Texture2D texture, string filePath)
        {
            var bytes = texture.EncodeToJPG();
            File.WriteAllBytes(filePath, bytes);
        }

        static void RemoveAllChildren(GameObject obj)
        {
            var c = obj.transform.childCount;
            for (int i = c - 1; i >= 0; i--)
            {
                GameObject.Destroy(obj.transform.GetChild(i).gameObject);
            }
        }

        static void SetLayerRecursively(GameObject go, int layerNumber)
        {
            foreach (Transform trans in go.GetComponentsInChildren<Transform>(true))
            {
                trans.gameObject.layer = layerNumber;
            }
        }

        static Bounds GetTotalBounds(GameObject obj, Func<Renderer, bool> allow = null)
        {
            var rends = obj.GetComponentsInChildren<Renderer>().Where(b => allow == null || allow(b));
            if (!rends.Any())
                return new Bounds(obj.transform.position, new Vector3(1, 1, 1));
            else
            {
                var b = rends.First().bounds;

                foreach (var rend in rends.Skip(1))
                {
                    b.Encapsulate(rend.bounds);
                }
                return b;
            }
        }

        static Texture2D LoadTexture(string FilePath)
        {
            Texture2D Tex2D;
            byte[] FileData;

            if (File.Exists(FilePath))
            {
                FileData = File.ReadAllBytes(FilePath);
                return LoadTexture(FileData);
            }
            return null;
        }

        static Texture2D LoadTexture(byte[] bytes)
        {
            var tex = new Texture2D(2, 2);
            if (tex.LoadImage(bytes))
                return tex;

            return null;
        }

        static Texture2D ToTexture2D(RenderTexture rTex)
        {
            var tex = new Texture2D(rTex.width, rTex.height, TextureFormat.RGBA32, false);
            RenderTexture.active = rTex;
            tex.ReadPixels(new Rect(0, 0, rTex.width, rTex.height), 0, 0);
            tex.Apply();
            return tex;
        }

        #endregion
    }
}
