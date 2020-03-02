using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(fileName = "Tiles/Grid Tile")]
public class GridTile : Tile
{
    public float pressure;
    public bool isWall;

}
