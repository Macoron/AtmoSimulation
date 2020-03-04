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

                var meanPressure = myPressure;
                var smallerPressure = new List<(int2, AtmosCell)>(4);
                // Get neighbors that has smaller pressure
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
                            smallerPressure.Add((pos, otherCell));
                            meanPressure += otherCell.pressure;
                        }
                    }
                }

                // Sort cells by their pressure
                smallerPressure = smallerPressure.OrderBy((cell) => { return cell.Item2.pressure; }).ToList();
                meanPressure /= (smallerPressure.Count + 1);

                // Share myCell pressure with them
                foreach (var cell in smallerPressure)
                {
                    var otherCell = cell.Item2;

                    var dif = meanPressure - otherCell.pressure;
                    if (myPressure - dif < 0)
                        break;

                    myPressure -= dif;
                    otherCell.pressure += dif;

                    var otherPos = cell.Item1;
                    currentState.grid[otherPos.x, otherPos.y] = otherCell;

                    // Apply changed pressure
                    myCell.pressure = myPressure;
                    currentState.grid[x, y] = myCell;
                }
            }

        /*                         {
                            var meanPressure = (myPressure + otherPressure) / 2f;
                            var step = myPressure - meanPressure;

                            myPressure -= step;
                            otherCell.pressure += step;

                            currentState.grid[pos.x, pos.y] = otherCell;

                            myCell.pressure = myPressure;
                            currentState.grid[x, y] = myCell;
                        }*/
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
                currentState = currentChunks[i]
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
