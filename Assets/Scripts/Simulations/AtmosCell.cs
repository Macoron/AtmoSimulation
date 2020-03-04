using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum WindDirection
{
    LEFT = 0,
    RIGHT = 1,
    UP = 2,
    DOWN = 3
}

public unsafe struct AtmosCell
{
    public bool isWall;
    public float pressure;
    public fixed float wind[4];

    public void AddWind(WindDirection dir, float ammount)
    {
        unsafe
        {
            wind[(int)dir] = ammount;
        }
    }

    public void ClearAllWind()
    {
        unsafe
        {
            for (int i = 0; i < 4; i++)
                wind[i] = 0f;
        }
    }
}