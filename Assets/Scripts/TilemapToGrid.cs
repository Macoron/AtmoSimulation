using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

public class TilemapToGrid : MonoBehaviour
{
    public Tile airTile;
    public Tile wallTile;

    public const float maxPressure = 200;
    public Gradient gradient;

    public Tilemap tilemapVisual;
    public int chunkSize = 3;
    public int viewRadius = 5;

    public AtmosSimulation simulation;

    [Header("Visualisation Helper")]
    public PressureVisualisation visualisation;
    public WindVisualisation windVisualisation;

    // Start is called before the first frame update
    void Start()
    {
        simulation = AtmosSimulation.FromTilemap(tilemapVisual, chunkSize);
        StartCoroutine(simulation.SimulationRoutine());
    }


    private void Update()
    {
        Visualisation();
        HandleInput();
    }

    private void HandleInput()
    {
        var mouseClickPos = Input.mousePosition;
        var worldPos = Camera.main.ScreenToWorldPoint(mouseClickPos);

        var gridPos = tilemapVisual.WorldToCell(worldPos);
        var grid = simulation.currentState;

        // Don't allow add new chunks 
        if (!grid.HasCell(gridPos.x, gridPos.y))
            return;

        if (Input.GetMouseButtonDown(0))
        {
            simulation.AddCommand(new AddGasCommand()
            {
                ammount = 100,
                pos = new int2(gridPos.x, gridPos.y)
            });
        }
        else if (Input.GetMouseButtonDown(1))
        {
            if (grid[gridPos.x, gridPos.y].isWall)
            {
                simulation.AddCommand(new RemoveWallCommand()
                {
                    pos = new int2(gridPos.x, gridPos.y)
                });
            }
            else
            {
                simulation.AddCommand(new AddWallCommand()
                {
                    pos = new int2(gridPos.x, gridPos.y)
                });
            }


        }
    }

    private void Visualisation()
    {
        var camera = Camera.main.transform;
        var cameraCenter = tilemapVisual.WorldToCell(camera.position);

        var grid = simulation.currentState;

        visualisation?.HideAll();
        windVisualisation?.HideAll();

        for (int x = -viewRadius; x <= viewRadius; x++)
            for (int y = -viewRadius; y <= viewRadius; y++)
            {
                int posX = cameraCenter.x + x;
                int posY = cameraCenter.y + y;

                if (grid.HasCell(posX, posY))
                {
                    var cell = grid[posX, posY];
                    var cellPos = new Vector3Int(posX, posY, 0);

                    if (!cell.isWall)
                    {
                        var pressure = cell.pressure;

                        var normPressure = Mathf.Clamp01(pressure / maxPressure);
                        var color = gradient.Evaluate(normPressure);

                        tilemapVisual.SetTile(cellPos, airTile);
                        tilemapVisual.SetColor(cellPos, color);

                        var worldPos = tilemapVisual.GetCellCenterWorld(cellPos);
                        visualisation?.ShowPressure(worldPos, pressure);
                        if (windVisualisation)
                        {
                            for (int i = 0; i < 4; i++)
                            {
                                var windDir = (WindDirection)i;

                                var windPower = cell.GetWind(windDir);
                                windVisualisation.ShowWind(worldPos, windPower, windDir);
                            }
                        }
                    }
                    else
                    {

                        tilemapVisual.SetTile(cellPos, wallTile);
                        tilemapVisual.SetColor(cellPos, wallTile.color);
                    }



                }

            }
    }

    /*#if UNITY_EDITOR
      private void OnDrawGizmos()
       {
           if (!Application.isPlaying)
               return;

           var camera = Camera.main.transform;
           var cameraCenter = tilemapVisual.WorldToCell(camera.position);

           var grid = simulation.currentState;

           for (int x = -viewRadius; x <= viewRadius; x++)
               for (int y = -viewRadius; y <= viewRadius; y++)
               {
                   int posX = cameraCenter.x + x;
                   int posY = cameraCenter.y + y;

                   if (grid.HasCell(posX, posY))
                   {
                       var pressure = grid[posX, posY].pressure;
                       var cellPos = new Vector3Int(posX, posY, 0);
                       var worldPos = tilemapVisual.GetCellCenterWorld(cellPos);

                       UnityEditor.Handles.Label(worldPos, pressure.ToString("n2"));
                   }

               }
       }
    #endif*/

    private void OnDestroy()
    {
        simulation.Dispose();
    }

}
