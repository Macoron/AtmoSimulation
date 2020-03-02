using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

public struct TestDataStruct : IDisposable
{

    public NativeList<int> list;
    public NativeHashMap<int2, int> hashMap;

    public void Dispose()
    {
        list.Dispose();
        hashMap.Dispose();
    }
}

public struct TestJob : IJobParallelFor
{
    [NativeDisableContainerSafetyRestriction]
    public TestDataStruct dataStruct;

    public void Execute(int index)
    {
        dataStruct.list[index] = -132;
    }
}

public class Test : MonoBehaviour
{
    private TestDataStruct dataStruct;

    private IEnumerator Start()
    {
        dataStruct = new TestDataStruct()
        {
            list = new NativeList<int>(100, Allocator.Persistent),
            hashMap = new NativeHashMap<int2, int>(100, Allocator.Persistent)
        };

        for (int i = 0; i < 100; i++)
        {
            dataStruct.list.Add(i);
            dataStruct.hashMap.TryAdd(new int2(i, i), i);
        }

        var job = new TestJob()
        {
            dataStruct = dataStruct
        };

        var jobHandler = job.Schedule(100, 10);
        yield return new WaitUntil(() => jobHandler.IsCompleted);

        for (int i = 0; i < 100; i++)
        {
            print(dataStruct.list[i]);
        }
    }

    private void OnDestroy()
    {
        dataStruct.Dispose();
    }
}
