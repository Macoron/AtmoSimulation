using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

public struct ApplyWindJob : IJob
{
    [ReadOnly]
    public ChunkedGrid<AtmosCell>.Chunk currentState;

    // need read and write on this
    public ChunkedGrid<AtmosCell>.Chunk nextState;

    public void Execute()
    {
        var minPos = currentState.MinPoint;
        var maxPos = currentState.MaxPoint;

        for (int x = minPos.x; x <= maxPos.x; x++)
            for (int y = minPos.y; y <= maxPos.y; y++)
                ApplyNeighbourWind(x, y);
    }

    private void ApplyNeighbourWind(int x, int y)
    {
        var myCell = nextState.grid[x, y];
        if (myCell.isWall)
            return;

        var neighbourCells = new int2[]{
                    new int2(x - 1, y),
                    new int2(x + 1, y),
                    new int2(x, y + 1),
                    new int2(x, y - 1)
                };

        // inverted neighbour cells
        var incomeWinds = new WindDirection[] {
            WindDirection.RIGHT,
            WindDirection.LEFT,
            WindDirection.DOWN,
            WindDirection.UP
        };

        for (int i = 0; i < neighbourCells.Length; i++)
        {
            var pos = neighbourCells[i];

            if (!nextState.grid.HasCell(pos.x, pos.y))
                continue;

            var incomeWind = incomeWinds[i];

            var otherCell = nextState.grid[pos.x, pos.y];
            var incomePressure = otherCell.GetWind(incomeWind);

            myCell.pressure += incomePressure;
        }

        nextState.grid[x, y] = myCell;
    }
}
