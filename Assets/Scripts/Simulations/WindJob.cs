using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

/// <summary>
/// Generete wind map and write it to the next state
/// Can be run in parallel (double-buffering)
/// Part of the WindEngine Atmo Simulation
/// </summary>
public struct WindJob : IJob
{
    [ReadOnly]
    public ChunkedGrid<AtmosCell>.Chunk currentState;
    [WriteOnly]
    public ChunkedGrid<AtmosCell>.Chunk nextState;

    public void Execute()
    {
        var minPos = currentState.MinPoint;
        var maxPos = currentState.MaxPoint;

        for (int x = minPos.x; x <= maxPos.x; x++)
            for (int y = minPos.y; y <= maxPos.y; y++)
                GenerateWind(x, y);
    }

    private (float, List<WindDirection>) CalculateMeanPressure(int2[] neighbourCells, float myPressure)
    {
        var sumPressure = myPressure;
        var windDirection = new List<WindDirection>(4);

        // Only sum neighbors that has smaller pressure
        for (int i = 0; i < neighbourCells.Length; i++)
        {
            var pos = neighbourCells[i];

            if (currentState.grid.HasCell(pos.x, pos.y))
            {
                var otherCell = currentState.grid[pos.x, pos.y];
                if (otherCell.isWall)
                    continue;

                var otherPressure = otherCell.pressure;

                if (myPressure > otherPressure)
                {
                    sumPressure += otherCell.pressure;
                    windDirection.Add((WindDirection)i);
                }
            }
        }

        var meanPressure = sumPressure / (windDirection.Count + 1);
        return (meanPressure, windDirection);
    }

    private void GenerateWind(int x, int y)
    {
        var myCell = currentState.grid[x, y];
        // Clear all wind from previous frame
        myCell.ClearAllWind();

        if (!myCell.isWall)
        {
            var neighbourCells = new int2[]{
                    new int2(x - 1, y),
                    new int2(x + 1, y),
                    new int2(x, y + 1),
                    new int2(x, y - 1)
                };

            var myPressure = currentState.grid[x, y].pressure;

            // First calculate where wind can move pressure
            var windCalculation = CalculateMeanPressure(neighbourCells, myPressure);
            var meanPressure = windCalculation.Item1;
            var windDirections = windCalculation.Item2;

            // Next save wind directions
            foreach (var windDir in windDirections)
            {
                var pos = neighbourCells[(int)windDir];
                var otherCell = currentState.grid[pos.x, pos.y];

                var windPower = meanPressure - otherCell.pressure;
                if (windPower < 0)
                    continue;

                if (myPressure - windPower < 0)
                    break;

                myCell.AddWind(windDir, windPower);
                myPressure -= windPower;
            }

            myCell.pressure = myPressure;
        }

        // Apply changes the future state
        nextState.grid[x, y] = myCell;
    }
}