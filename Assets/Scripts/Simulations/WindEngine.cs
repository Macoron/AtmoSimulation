using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Jobs;
using UnityEngine;

public class WindEngine : IAtmosEngine
{
    private List<JobHandle> windJobs = new List<JobHandle>();

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

        // Swap the buffer
        var temp = sim.currentState;
        sim.currentState = sim.nextState;
        sim.nextState = temp;
    }

}
