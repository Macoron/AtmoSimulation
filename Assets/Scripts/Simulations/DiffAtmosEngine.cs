using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

public struct EqualisationJob : IJob
{
    // read and write
    public ChunkedGrid<AtmosCell>.Chunk nextState;

    private (float, List<WindDirection>) CalculateMeanPressure(int2[] neighbourCells, float myPressure)
    {
        var sumPressure = myPressure;
        var windDirection = new List<WindDirection>(4);

        // Only sum neighbors that has smaller pressure
        for (int i = 0; i < neighbourCells.Length; i++)
        {
            var pos = neighbourCells[i];

            if (nextState.grid.HasCell(pos.x, pos.y))
            {
                var otherCell = nextState.grid[pos.x, pos.y];
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

    public void Execute()
    {
        var minPos = nextState.MinPoint;
        var maxPos = nextState.MaxPoint;

        var allCells = new List<int2>();

        for (int x = minPos.x; x <= maxPos.x; x++)
            for (int y = minPos.y; y <= maxPos.y; y++)
                allCells.Add(new int2(x, y));

        var random = new Unity.Mathematics.Random((uint)DateTime.Now.Ticks);
        var randomOrder = allCells.OrderBy((c) => random.NextInt());

        foreach (var cell in randomOrder)
        {
            var x = cell.x;
            var y = cell.y;

            var neighbourCells = new int2[]{
                new int2(x - 1, y),
                new int2(x + 1, y),
                new int2(x, y + 1),
                new int2(x, y - 1)
            };

            var myCell = nextState.grid[x, y];
            var myPressure = nextState.grid[x, y].pressure;

            myCell.ClearAllWind();

            // First calculate where wind can move pressure
            var windCalculation = CalculateMeanPressure(neighbourCells, myPressure);
            var meanPressure = windCalculation.Item1;
            var windDirections = windCalculation.Item2;


            // Share myCell pressure with them
            foreach (var dir in windDirections)
            {
                var otherPos = neighbourCells[(int)dir];
                var otherCell = nextState.grid[otherPos.x, otherPos.y];

                var dif = meanPressure - otherCell.pressure;
                if (myPressure - dif < 0)
                    break;

                myPressure -= dif;
                otherCell.pressure += dif;
                myCell.AddWind(dir, dif);

                nextState.grid[otherPos.x, otherPos.y] = otherCell;

                // Apply changed pressure
                myCell.pressure = myPressure;
                nextState.grid[x, y] = myCell;
            }
        }
    }
}

public class DiffAtmosEngine : IAtmosEngine
{
    private JobHandle lastJob;

    public void Dispose()
    {
        lastJob.Complete();
    }

    public IEnumerator MakeStep(AtmosSimulation sim)
    {
        var currentChunks = sim.currentState.Chunks.ToArray();
        var nextChunks = sim.nextState.Chunks.ToArray();

        lastJob = default(JobHandle);

        for (int i = 0; i < currentChunks.Length; i++)
        {
            var job = new EqualisationJob()
            {
                nextState = currentChunks[i]
            };

            lastJob = job.Schedule(lastJob);
        }

        lastJob = new CalculateTotalPressureJob()
        {
            grid = sim.currentState
        }.Schedule(lastJob);

        while (!lastJob.IsCompleted)
            yield return null;

        /*var temp = sim.currentState;
        sim.currentState = sim.nextState;
        sim.nextState = temp;*/

    }
}
