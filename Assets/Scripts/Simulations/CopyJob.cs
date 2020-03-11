using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Jobs;

public struct CopyJob : IJobParallelFor
{
    public ChunkedGrid<AtmosCell> currentState;
    public ChunkedGrid<AtmosCell> nextState;

    public void Execute(int index)
    {
        nextState[index] = currentState[index];
    }
}
