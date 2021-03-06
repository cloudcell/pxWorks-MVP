﻿// Copyright (c) 2020 Cloudcell Limited

using System;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace uGraph
{
    public class OutputKnob : MonoBehaviour
    {
        public Guid Id = Guid.NewGuid();
        [SerializeField] TMPro.TextMeshProUGUI headerText;
        public RectTransform OutputKnobTransform;


        [SerializeField] Image lightImage;
        [SerializeField] Sprite lightOnSprite;
        [SerializeField] Sprite lightOffSprite;

        //public KnobType Type;

        public string Name
        {
            get => headerText.text;
            set => headerText.text = value;
        }

        public string OutputFilePath => Path.Combine(GetComponentInParent<Node>().FullFolderPath, Name);

        public void OnConnectionChanged(bool hasInput)
        {
            lightImage.sprite = hasInput ? lightOnSprite : lightOffSprite;
            lightImage.color = Color.white;
        }
    }
}
