using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Media;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Dashboard.Library
{
    public class ContentLibrary
    {
        public Texture2D DummyTexture;

        private Dictionary<string, object> _assets;

        public void Load(ContentManager content, GraphicsDevice device)
        {
            Load(content, device, "ControlsContent");
        }

        public void Load(ContentManager content, GraphicsDevice device, string dir)
        {
            DummyTexture = new Texture2D(device, 1, 1);
            DummyTexture.SetData<Color>(new Color[] { Color.White });

            _assets = new Dictionary<string, object>();

            string controlsPath = Path.Combine(content.RootDirectory, dir);
            string texPath = Path.Combine(controlsPath, "Textures");
            string modelPath = Path.Combine(controlsPath, "Models");
            string sfxPath = Path.Combine(controlsPath, "SoundEffects");
            string songPath = Path.Combine(controlsPath, "Songs");

            LoadDirectory<Texture2D>(content, texPath);
            LoadDirectory<Model>(content, modelPath);
            LoadDirectory<SoundEffect>(content, sfxPath);
            LoadDirectory<Song>(content, songPath);
        }

        private void LoadDirectory<T>(ContentManager content, string dir)
        {
            if (!Directory.Exists(dir)) return;

            string[] files = Directory.GetFiles(dir);

            foreach (string s in files)
            {
                string key = Path.GetFileNameWithoutExtension(s);

                _assets[key] = content.Load<T>(Path.Combine(dir, key));
            }

            string[] directories = Directory.GetDirectories(dir);

            foreach (string s in directories)
                LoadDirectory<T>(content, s);
        }

        public object Get(string assetName)
        {
            if (!_assets.ContainsKey(assetName))
                return null;

            return _assets[assetName];
        }
    }
}
