using System;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using CometUI;
using uGraph;

namespace MainScene_UI
{
    partial class ConsolePanel : BaseView
    {
        private void Start()
        {
            //subscribe buttons or events here
            Bus.SetStatusLabelSelectedNode.Subscribe(this, OnNewLine);
            Bus.SaveUserSettings.Subscribe(this, InitFont).CallWhenInactive();
            Bus.RunnerState.Subscribe(this, OnRunnerStateChnaged).CallWhenInactive();

            InitFont();
        }

        private void OnRunnerStateChnaged(RunnerState obj)
        {
            if (obj == RunnerState.Run)
            {
                lines.Clear();
                ifText.text = "";
            }
        }

        private void InitFont()
        {
            ifText.GetComponentInChildren<Text>().fontSize = UserSettings.Instance.ConsoleFontSize;
        }

        Queue<string> lines = new Queue<string>();

        private void OnNewLine(string line)
        {
            var max = UserSettings.Instance.MaxConsoleLines;
            while (lines.Count > max) lines.Dequeue();
            lines.Enqueue(line);

            ifText.text = string.Join(Environment.NewLine, lines);
        }
    }
}