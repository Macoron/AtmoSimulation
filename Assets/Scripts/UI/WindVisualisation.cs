using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WindVisualisation : MonoBehaviour
{
    public float windThr = 30f;
    public SpriteRenderer windArrowPrefab;

    private Stack<SpriteRenderer> pool = new Stack<SpriteRenderer>();
    private Stack<SpriteRenderer> shown = new Stack<SpriteRenderer>();

    private void Awake()
    {
        windArrowPrefab.gameObject.SetActive(false);
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

    public void ShowWind(Vector3 worldPos, float power, WindDirection dir)
    {
        if (!gameObject.activeInHierarchy)
            return;

        if (power <= windThr)
            return;

        SpriteRenderer windUI;

        if (pool.Count == 0)
            windUI = Instantiate(windArrowPrefab, transform);
        else
            windUI = pool.Pop();

        windUI.gameObject.SetActive(true);
        windUI.transform.position = worldPos;

        switch (dir)
        {
            case WindDirection.UP:
                windUI.transform.up = Vector3.up;
                break;
            case WindDirection.RIGHT:
                windUI.transform.up = Vector3.right;
                break;
            case WindDirection.DOWN:
                windUI.transform.up = Vector3.down;
                break;
            case WindDirection.LEFT:
                windUI.transform.up = Vector3.left;
                break;
        }

        shown.Push(windUI);
    }
}
