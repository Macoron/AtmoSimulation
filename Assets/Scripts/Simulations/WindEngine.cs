using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Jobs;
using UnityEngine;

public class WindEngine : IAtmosEngine
{
    private List<JobHandle> windJobs = new List<JobHandle>();
    private List<JobHandle> applyWindJobs = new List<JobHandle>();

    public void Dispose()
    {
        foreach (var j in windJobs)
            j.Complete();
    }

    public IEnumerator MakeStep(AtmosSimulation sim)
    {
        var currentChunks = sim.currentState.Chunks.ToArray();
        var nextChunks = sim.nextState.Chunks.ToArray();

        // First generate wind map
        windJobs.Clear();
        for (int i = 0; i < currentChunks.Length; i++)
        {
            var job = new WindJob()
            {
                currentState = currentChunks[i],
                nextState = nextChunks[i]
            };

            var jobHandle = job.Schedule();
            windJobs.Add(jobHandle);
        }

        // Wait until wind jobs finished
        while (windJobs.Count((j) => !j.IsCompleted) > 0)
            yield return null;

        // Now apply wind
        applyWindJobs.Clear();
        for (int i = 0; i < currentChunks.Length; i++)
        {
            var job = new ApplyWindJob()
            {
                currentState = currentChunks[i],
                nextState = nextChunks[i]
            };

            var jobHandle = job.Schedule();
            applyWindJobs.Add(jobHandle);
        }

        // Wait until apply jobs finished
        while (applyWindJobs.Count((j) => !j.IsCompleted) > 0)
            yield return null;

        /*new CalculateTotalPressureJob()
        {
            grid = sim.currentState
        }.Execute();*/

        // Swap the buffer
        var temp = sim.currentState;
        sim.currentState = sim.nextState;
        sim.nextState = temp;
    }

}
