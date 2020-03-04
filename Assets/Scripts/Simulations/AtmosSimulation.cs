using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Tilemaps;

public struct CalculateTotalPressureJob : IJob
{
    public ChunkedGrid<AtmosCell> grid;

    public void Execute()
    {
        float totalPressure = 0f;

        foreach (AtmosCell cell in grid)
        {
            totalPressure += cell.pressure;
        }

        UnityEngine.Debug.Log(totalPressure);
    }
}

public class AtmosSimulation : IDisposable
{
    public ChunkedGrid<AtmosCell> currentState;
    public ChunkedGrid<AtmosCell> nextState;

    public IAtmosEngine atmosEngine = new WindEngine();

    public long lastSimulationFrame;

    private Queue<ICommand> commandsBuffer = new Queue<ICommand>();


    public void AddCommand(ICommand command)
    {
        commandsBuffer.Enqueue(command);
    }

    public AtmosSimulation(ChunkedGrid<AtmosCell> grid)
    {
        currentState = grid;
        nextState = grid.Clone();
    }

    public void Dispose()
    {
        atmosEngine.Dispose();

        currentState.Dispose();
        nextState.Dispose();
    }

    public IEnumerator SimulationRoutine()
    {
        while (true)
        {
            //yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space));
            yield return null;

            ExecuteCommandsRoutine();

            yield return RunSim();
        }
    }

    private void ExecuteCommandsRoutine()
    {
        foreach (var cmd in commandsBuffer)
            cmd.Execute(this);

        commandsBuffer.Clear();
    }

    public IEnumerator RunSim()
    {
        var sw = new Stopwatch();

        sw.Start();
        yield return atmosEngine.MakeStep(this);
        sw.Stop();

        lastSimulationFrame = sw.ElapsedMilliseconds;
        UnityEngine.Debug.Log($"One step time: {sw.ElapsedMilliseconds} ms");
    }

    public static AtmosSimulation FromTilemap(Tilemap tilemapVisual, int chunkSize = 3)
    {
        var tilemapSize = tilemapVisual.size;
        var tilemapOrigin = tilemapVisual.origin;

        var worstCaseSize = tilemapSize.x * tilemapSize.y;
        var grid = new ChunkedGrid<AtmosCell>(chunkSize, worstCaseSize);

        for (int x = tilemapOrigin.x; x < tilemapSize.x + tilemapOrigin.x; x++)
            for (int y = tilemapOrigin.y; y < tilemapSize.y + tilemapOrigin.y; y++)
            {
                var pos = new Vector3Int(x, y, 0);

                // Is there any tile?
                var tile = tilemapVisual.GetTile<GridTile>(pos);
                if (tile != null)
                {
                    var atmosCell = new AtmosCell()
                    {
                        pressure = tile.pressure,
                        isWall = tile.isWall
                    };

                    grid.AddCell(x, y, atmosCell);
                }
            }

        return new AtmosSimulation(grid);
    }
}
