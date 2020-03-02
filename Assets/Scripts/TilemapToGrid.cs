using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Tilemaps;

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
        if (Input.GetMouseButtonDown(0))
        {
            var mouseClickPos = Input.mousePosition;
            var worldPos = Camera.main.ScreenToWorldPoint(mouseClickPos);

            var gridPos = tilemapVisual.WorldToCell(worldPos);
            var grid = simulation.currentState;
            if (grid.HasCell(gridPos.x, gridPos.y))
            {
                simulation.AddCommand(new AddGasCommand()
                {
                    ammount = 100,
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
                    }
                    else
                    {

                        tilemapVisual.SetTile(cellPos, wallTile);
                    }



                }

            }
    }

#if UNITY_EDITOR
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

        /*foreach (var chunk in simulation.currentState.Chunks)
        {
            var minPos = chunk.MinPoint;
            var maxPos = chunk.MaxPoint;

            for (int x = minPos.x; x <= maxPos.x; x++)
                for (int y = minPos.y; y <= maxPos.y; y++)
                {
                    var cell = chunk[x, y];

                    var pos = new Vector3Int(x, y, 0);
                    var worldPos = tilemapVisual.GetCellCenterWorld(pos);
                    UnityEditor.Handles.Label(worldPos, cell.pressure.ToString("n2"));
                }
        }*/

        /*foreach (AtmosCell cell in simulation.currentState)
        {
            var pos = new Vector3Int(cell.cellPos.x, cell.cellPos.y, 0);
            var worldPos = tilemapVisual.GetCellCenterWorld(pos);

            UnityEditor.Handles.Label(worldPos, cell.pressure.ToString());

        }*/
    }
#endif

    private void OnDestroy()
    {
        simulation.Dispose();
    }

}
