using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class ButtonController : MonoBehaviour
{
    public Button Button { get; private set; }
    [SerializeField] Image Icon;
    Color initIconColor;
    bool selected;

    void Start()
    {
        Button = GetComponent<Button>();
        initIconColor = Icon.color;
    }

    public bool Selected
    {
        get { return selected; }
        set
        {
            if(value != selected)
            {
                value = selected;
                OnSelectedChanged();
            }
        }
    }

    private void OnSelectedChanged()
    {
        Icon.color = selected ? Color.white : initIconColor;
    }
}
