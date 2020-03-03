using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PressureVisualisation : MonoBehaviour
{
    public TextMesh textPrefab;

    private Stack<TextMesh> pool = new Stack<TextMesh>();
    private Stack<TextMesh> shown = new Stack<TextMesh>();

    private void Awake()
    {
        textPrefab.gameObject.SetActive(false);
    }

    public void HideAll()
    {
        foreach (var t in shown)
        {
            t.gameObject.SetActive(false);
            pool.Push(t);
        }

        shown.Clear();
    }

    public void ShowPressure(Vector3 worldPos, float pressure)
    {
        TextMesh pressureUI;

        if (pool.Count == 0)
            pressureUI = Instantiate(textPrefab, transform);
        else
            pressureUI = pool.Pop();

        pressureUI.gameObject.SetActive(true);
        pressureUI.transform.position = worldPos;
        pressureUI.text = pressure.ToString("n2");
        shown.Push(pressureUI);
    }
}
