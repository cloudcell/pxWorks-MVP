using System;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using CometUI;
using System.IO;
using System.Linq;
using System.Threading;
using System.Globalization;

namespace GraphicsScene_UI
{
    partial class GraphicsPanel : BaseView
    {
        string Folder;

        private void Start()
        {
#if UNITY_EDITOR
            Folder = @"Y:\Projects_Unity\Alex Bikeyev\pwxProjectF_SEEDLoop\output.graphics\";
#else
            var args = Environment.GetCommandLineArgs();
            if (args.Length < 3)
            {
                Application.Quit();
                return;
            }
            Folder = args[2].Trim('"') + Path.DirectorySeparatorChar;
            //UIManager.ShowDialog(null, Folder);

            if (!Directory.Exists(Folder))
            {
                Application.Quit();
                return;
            }
#endif

            InvokeRepeating("UpdateImage", 0, 0.5f);

            //subscribe buttons or events here
            SetActive(im, false);

            Subscribe(btPrev, () => GotoNext(-1));
            Subscribe(btNext, () => GotoNext(1));

            Rebuild();
        }

        DateTime lastImageFileDate;
        string displayedFile;
        FileInfo[] files;

        private void GotoNext(int dir)
        {
            var index = -1;
            for (int i = 0; i < files.Length; i++)
                if(files[i].FullName == displayedFile)
                {
                    index = i;
                    break;
                }
            if (index == -1)
                index = files.Length - 1;

            index += dir;

            if (index < 0 || index >= files.Length) return;

            LoadImage(files[index].FullName);
        }

        HashSet<string> allowedFileExt = new HashSet<string> { ".png" };

        void UpdateImage()
        {
            //get list of files
            files = new DirectoryInfo(Folder)
                .GetFiles()
                .Where(f => allowedFileExt.Contains(Path.GetExtension(f.Name).ToLower()))
                .OrderBy(f => f.LastWriteTime)
                .ToArray();

            //remove old files
            for (int i = 0; i < files.Length - UserSettings.Instance.MaxOutputGraphicsFilesCount; i++)
                try
                {
                    File.Delete(files[i].FullName);
                }
                catch { }

            files = files.Skip(files.Length - UserSettings.Instance.MaxOutputGraphicsFilesCount).ToArray();

            //show last file
            var file = files.LastOrDefault();

            if (file != null)
            if (lastImageFileDate != file.LastWriteTime)
            {
                try
                {
                    LoadImage(file.FullName);
                    lastImageFileDate = file.LastWriteTime;
                    Rebuild();
                }
                catch { }
            }
        }

        private void LoadImage(string fullName)
        {
            var old = im.sprite;

            if (old != null && old.texture != null)
                Destroy(old.texture);

            if (old != null)
                Destroy(old);

            Set(im, LoadNewSprite(fullName));
            im.preserveAspect = true;
            SetActive(im, true);

            displayedFile = fullName;
        }

        public static Sprite LoadNewSprite(string FilePath, float PixelsPerUnit = 100.0f, SpriteMeshType spriteType = SpriteMeshType.Tight)
        {
            // Load a PNG or JPG image from disk to a Texture2D, assign this texture to a new sprite and return its reference
            Texture2D SpriteTexture = LoadTexture(FilePath);
            Sprite NewSprite = Sprite.Create(SpriteTexture, new Rect(0, 0, SpriteTexture.width, SpriteTexture.height), new Vector2(0, 0), PixelsPerUnit, 0, spriteType);
            return NewSprite;
        }

        public static Sprite ConvertTextureToSprite(Texture2D texture, float PixelsPerUnit = 100.0f, SpriteMeshType spriteType = SpriteMeshType.Tight)
        {
            // Converts a Texture2D to a sprite, assign this texture to a new sprite and return its reference
            Sprite NewSprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0, 0), PixelsPerUnit, 0, spriteType);
            return NewSprite;
        }

        public static Texture2D LoadTexture(string FilePath)
        {
            if (!File.Exists(FilePath))
                return null;

            var ext = Path.GetExtension(FilePath).ToLower();

            //if (ext == ".svg")
            //{
            //    ISVGDevice device;
            //    //device = new SVGDeviceFast();
            //    device = new SVGDeviceSmall();
            //    var text = File.ReadAllText(FilePath);
            //    var implement = new Implement(text, device);
            //    var prevCulture = Thread.CurrentThread.CurrentCulture;
            //    Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

            //    try
            //    {
            //        implement.StartProcess();
            //        return implement.GetTexture();
            //    }
            //    catch(Exception ex)
            //    {
            //        Debug.LogException(ex);
            //        return null;
            //    }
            //    finally
            //    {
            //        Thread.CurrentThread.CurrentCulture = prevCulture;
            //    }
            //}

            // Load a PNG or JPG file from disk to a Texture2D
            // Returns null if load fails
            Texture2D Tex2D;
            byte[] FileData;

            FileData = File.ReadAllBytes(FilePath);
            Tex2D = new Texture2D(2, 2);           // Create new "empty" texture
            if (Tex2D.LoadImage(FileData))           // Load the imagedata into the texture (size is set automatically)
                return Tex2D;                 // If data = readable -> return texture
            
            return null;                     // Return null if load failed
        }

        protected override void OnBuild(bool isFirstBuild)
        {
            //copy data to UI controls here
            if (files == null || files.Length == 0)
            {
                SetInteractable(btPrev, false);
                SetInteractable(btNext, false);
            }else
            {
                SetInteractable(btPrev, files[0].FullName != displayedFile);
                SetInteractable(btNext, files[files.Length - 1].FullName != displayedFile);
            }
        }
    }
}