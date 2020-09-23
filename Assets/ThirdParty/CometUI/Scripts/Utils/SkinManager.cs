using System;
using UnityEngine;
using UnityEngine.UI;

namespace CometUI
{
    public class SkinManager : MonoBehaviour
    {
        [SerializeField]
        public string[] SkinNames = new string[] { "Default" };

        [SerializeField] SpriteSkin[] SpriteSkins;
        [SerializeField] TextureSkin[] TextureSkins;
        [SerializeField] ColorSkin[] ColorSkins;
        [SerializeField] PaddingSkin[] PaddingSkins;

        int currentSkinIndex = 0;
        bool initialized = false;

        public static SkinManager Instance { get; private set; }

        protected SkinManager()
        {
            if (Instance == null)
                Instance = this;
        }

        private void Start()
        {
            //Bus.UserSettingsChanged.Subscribe(this, () => SetSkin(Bus.UserSettings.SkinIndex));
        }

        public void SetSkin(int skinIndex)
        {
            InitIfNeeded();

            foreach (var skin in SpriteSkins)
                skin.SetSkin(skinIndex);

            foreach (var skin in TextureSkins)
                skin.SetSkin(skinIndex);

            foreach (var skin in ColorSkins)
                skin.SetSkin(skinIndex);

            foreach (var skin in PaddingSkins)
                skin.SetSkin(skinIndex);

            currentSkinIndex = skinIndex;
        }

        void InitIfNeeded()
        {
            if (initialized)
                return;

            foreach (var skin in SpriteSkins)
                skin.Init();

            foreach (var skin in TextureSkins)
                skin.Init();

            foreach (var skin in ColorSkins)
                skin.Init();

            foreach (var skin in PaddingSkins)
                skin.Init();

            initialized = true;
        }
    }

    [Serializable]
    public abstract class SkinItem
    {
        public abstract void Init();
        public abstract void SetSkin(int skinIndex);
    }

    [Serializable]
    public class SpriteSkin : SkinItem
    {
        Sprite Default;

        [SerializeField] Image Image;
        [SerializeField] Sprite Skin1;
        [SerializeField] Sprite Skin2;
        [SerializeField] Sprite Skin3;

        public Sprite this[int skinIndex]
        {
            get
            {
                switch (skinIndex)
                {
                    case 0: return Default;
                    case 1: return Skin1;
                    case 2: return Skin2;
                    case 3: return Skin3;
                    default:
                        return null;
                }
            }
        }

        public override void Init()
        {
            if (Image)
                Default = Image.sprite;
        }

        public override void SetSkin(int skinIndex)
        {
            if (!Image)
                return;
            Image.overrideSprite = this[skinIndex];
        }
    }

    [Serializable]
    public class TextureSkin : SkinItem
    {
        Texture Default;

        [SerializeField] RawImage Image;
        [SerializeField] Texture Skin1;
        [SerializeField] Texture Skin2;
        [SerializeField] Texture Skin3;

        public Texture this[int skinIndex]
        {
            get
            {
                switch (skinIndex)
                {
                    case 0: return Default;
                    case 1: return Skin1;
                    case 2: return Skin2;
                    case 3: return Skin3;
                    default:
                        return null;
                }
            }
        }

        public override void Init()
        {
            if (Image)
                Default = Image.texture;
        }

        public override void SetSkin(int skinIndex)
        {
            if (!Image)
                return;
            Image.texture = this[skinIndex];
        }
    }


    [Serializable]
    public class ColorSkin : SkinItem
    {
        Color[] Default;

        [SerializeField] RectTransform[] RectTransforms;
        [SerializeField] Color Skin1;
        [SerializeField] Color Skin2;
        [SerializeField] Color Skin3;

        public Color this[int trIndex, int skinIndex]
        {
            get
            {
                switch (skinIndex)
                {
                    case 0: return Default[trIndex];
                    case 1: return Skin1;
                    case 2: return Skin2;
                    case 3: return Skin3;
                    default:
                        return Color.white;
                }
            }
        }

        public override void Init()
        {
            Default = new Color[RectTransforms.Length];

            for (int i = 0; i < RectTransforms.Length; i++)
            {
                var im = RectTransforms[i].GetComponent<Image>();
                if (im)
                    Default[i] = im.color;

                var rim = RectTransforms[i].GetComponent<RawImage>();
                if (rim)
                    Default[i] = rim.color;

                var text = RectTransforms[i].GetComponent<Text>();
                if (text)
                    Default[i] = text.color;

                var tmp = RectTransforms[i].GetComponent<TMPro.TextMeshPro>();
                if (tmp)
                    Default[i] = tmp.color;
            }
        }

        public override void SetSkin(int skinIndex)
        {
            for (int i = 0; i < RectTransforms.Length; i++)
            {
                var im = RectTransforms[i].GetComponent<Image>();
                if (im)
                    im.color = this[i, skinIndex];

                var rim = RectTransforms[i].GetComponent<RawImage>();
                if (rim)
                    rim.color = this[i, skinIndex];

                var text = RectTransforms[i].GetComponent<Text>();
                if (text)
                    text.color = this[i, skinIndex];

                var tmp = RectTransforms[i].GetComponent<TMPro.TextMeshPro>();
                if (tmp)
                    tmp.color = this[i, skinIndex];
            }
        }
    }

    [Serializable]
    public class PaddingSkin : SkinItem
    {
        Padding[] Default;

        [SerializeField] RectTransform[] RectTransforms;
        [SerializeField] Padding Skin1;
        [SerializeField] Padding Skin2;
        [SerializeField] Padding Skin3;

        [Serializable]
        class Padding
        {
            public Vector2 OffsetMin;
            public Vector2 OffsetMax;

            public Padding()
            {
            }

            public Padding(RectTransform tr)
            {
                OffsetMin = tr.offsetMin;
                OffsetMax = tr.offsetMax;
            }

            public void Apply(RectTransform tr)
            {
                tr.offsetMin = OffsetMin;
                tr.offsetMax = OffsetMax;
            }
        }

        Padding this[int trIndex, int skinIndex]
        {
            get
            {
                switch (skinIndex)
                {
                    case 0: return Default[trIndex];
                    case 1: return Skin1;
                    case 2: return Skin2;
                    case 3: return Skin3;
                    default:
                        return new Padding();
                }
            }
        }

        public override void Init()
        {
            Default = new Padding[RectTransforms.Length];

            for (int i = 0; i < RectTransforms.Length; i++)
            {
                var tr = RectTransforms[i];
                Default[i] = new Padding(tr);
            }
        }

        public override void SetSkin(int skinIndex)
        {
            for (int i = 0; i < RectTransforms.Length; i++)
            {
                var paddings = this[i, skinIndex];
                var tr = RectTransforms[i];
                paddings.Apply(tr);
            }
        }
    }
}