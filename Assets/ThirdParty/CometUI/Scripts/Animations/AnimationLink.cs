using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

namespace CometUI
{
    [CreateAssetMenu(fileName = "Animation", menuName = "Comet UI/Animation", order = 2)]
    [Serializable]
    public class AnimationLink : ScriptableObject, ISerializationCallbackReceiver
    {
        [NonSerialized]
        Animation _Animation;

        public Animation Animation
        {
            get
            {
                if (_Animation != null)
                {
                    return _Animation;
                }

                if (raw == null)
                {
                    _Animation = new Animation();
                }
                else
                {
                    using (var ms = new MemoryStream(raw))
                    try
                    {
                        _Animation = (Animation)new BinaryFormatter().Deserialize(ms);
                    }catch(Exception ex)
                    {
                        Debug.LogException(ex);
                        _Animation = new Animation();
                    }
                }

                foreach (var sa in Animation.Sequence)
                    foreach (var a in sa.Animations)
                        a.OnAfterDeserialize();

                raw = null;
                return _Animation;
            }
        }

        [SerializeField]
        [HideInInspector]
        byte[] raw;

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
        }

        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
            if (_Animation == null)
            {
                return;
            }

            foreach (var sa in Animation.Sequence)
            foreach (var a in sa.Animations)
                a.OnBeforeSerialize();

            using (var ms = new MemoryStream())
            {
                new BinaryFormatter().Serialize(ms, Animation);
                raw = ms.ToArray();
            }
        }

        public static implicit operator Animation(AnimationLink link)
        {
            return link.Animation;
        }
    }
}