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

public struct EqualisationJob : IJob
{
    // read and write
    public ChunkedGrid<AtmosCell>.Chunk currentState;

    public void Execute()
    {
        var minPos = currentState.MinPoint;
        var maxPos = currentState.MaxPoint;

        for (int x = minPos.x; x <= maxPos.x; x++)
            for (int y = minPos.y; y <= maxPos.y; y++)
            {
                // Fixed order cause different behaviour on corners cells
                // Should I use some sorting?
                var neighbourCells = new int2[]{
                    new int2(x - 1, y),
                    new int2(x + 1, y),
                    new int2(x, y - 1),
                    new int2(x, y + 1)
                };

                var myCell = currentState.grid[x, y];
                var myPressure = currentState.grid[x, y].pressure;

                foreach (var pos in neighbourCells)
                {
                    if (currentState.grid.HasCell(pos.x, pos.y))
                    {
                        var otherCell = currentState.grid[pos.x, pos.y];
                        if (otherCell.isWall)
                            continue;

                        var otherPressure = otherCell.pressure;

                        if (myPressure > otherPressure)
                        {
                            var meanPressure = (myPressure + otherPressure) / 2f;
                            var step = myPressure - meanPressure;

                            myPressure -= step;
                            otherCell.pressure += step;

                            currentState.grid[pos.x, pos.y] = otherCell;

                            myCell.pressure = myPressure;
                            currentState.grid[x, y] = myCell;
                        }
                    }
                }
            }
    }
}

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

    public long lastSimulationFrame;

    private ChunkedGrid<AtmosCell> nextState;

    private Queue<ICommand> commandsBuffer = new Queue<ICommand>();
    private JobHandle lastJob;

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
        lastJob.Complete();

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

            yield return MakeStep();
        }
    }

    private void ExecuteCommandsRoutine()
    {
        foreach (var cmd in commandsBuffer)
            cmd.Execute(this);

        commandsBuffer.Clear();
    }

    public IEnumerator MakeStep()
    {
        var sw = new Stopwatch();
        sw.Start();

        var currentChunks = currentState.Chunks.ToArray();
        var nextChunks = nextState.Chunks.ToArray();

        lastJob = default(JobHandle);

        for (int i = 0; i < currentChunks.Length; i++)
        {
            var job = new EqualisationJob()
            {
                currentState = currentChunks[i]
            };

            lastJob = job.Schedule(lastJob);
        }

        // calculate total pressure
        // for validation only
        /*var validationJob = new CalculateTotalPressureJob()
        {
            grid = nextState
        };
        lastJob = validationJob.Schedule(lastJob);*/

        // wait while all works are finished
        while (!lastJob.IsCompleted)
            yield return null;

        sw.Stop();

        lastSimulationFrame = sw.ElapsedMilliseconds;
        UnityEngine.Debug.Log($"One step time: {sw.ElapsedMilliseconds} ms");

        // swap the buffers
        /*var temp = currentState;
        currentState = nextState;
        nextState = temp;*/
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
