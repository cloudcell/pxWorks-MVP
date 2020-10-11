// Copyright (c) 2020 Cloudcell Limited

using System;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using CometUI;

namespace MainScene_UI
{
    partial class AgreementWindow : BaseView
    {
        public Scrollbar VertScrollbar;
        bool isAgreButtonVisible = false;

        private void Awake()
        {
            //PlayerPrefs.DeleteKey("agree");
            Bus.EULA.Value = PlayerPrefs.HasKey("agree");
        }

        private void Start()
        {
            if (Bus.EULA.Value)
                Close();

            //subscribe buttons or events here
            Subscribe(btClose, Cancel);
            Subscribe(btCancel, Cancel);
            Subscribe(btOk, Agree);

            SetInteractable(btOk, isAgreButtonVisible);
        }

        private void Agree()
        {
            PlayerPrefs.SetString("agree", "agree");
            PlayerPrefs.Save();
            Bus.EULA.Value = true;
            Close();
        }

        private void Cancel()
        {
            PlayerPrefs.DeleteKey("agree");
            PlayerPrefs.Save();
            Application.Quit();
        }

        private void Update()
        {
            if (VertScrollbar.value <= 0.01f)
            {
                isAgreButtonVisible = true;
                SetInteractable(btOk, true);
            }
        }
    }
}