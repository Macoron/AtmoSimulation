using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;

public struct ChunkedGrid<T> : IDisposable, IEnumerable<T>
    where T : unmanaged
{
    public readonly int chunkSize;
    public readonly int totalchunkSize;

    [NativeDisableContainerSafetyRestriction]
    private NativeList<T> buffer;
    [NativeDisableContainerSafetyRestriction]
    private NativeHashMap<int2, int> hashMap;

    private int lastChunkIndex;

    public int CellsCount => lastChunkIndex + 1;

    public struct Chunk
    {
        public ChunkedGrid<T> grid;
        public readonly int2 chunkPos;

        public int2 MinPoint
        {
            get
            {
                return new int2(chunkPos.x * grid.chunkSize,
                    chunkPos.y * grid.chunkSize);
            }
        }

        public int2 MaxPoint
        {
            get
            {
                return new int2((chunkPos.x + 1) * grid.chunkSize - 1,
                    (chunkPos.y + 1) * grid.chunkSize - 1);
            }
        }

        public Chunk(ChunkedGrid<T> grid, int2 chunkPos)
        {
            this.grid = grid;
            this.chunkPos = chunkPos;
        }

        /*public T this[int x, int y]
        {
            get
            {
                var cellIndex = grid.GetCellIndex(x, y, chunkPos);
                return grid.buffer[cellIndex];
            }
            set
            {
                var cellIndex = grid.GetCellIndex(x, y, chunkPos);
                grid.buffer[cellIndex] = value;
            }
        }*/
    }

    public ChunkedGrid(int chunkSize, int cellsCapacity)
    {
        this.chunkSize = chunkSize;
        this.totalchunkSize = chunkSize * chunkSize;

        buffer = new NativeList<T>(cellsCapacity, Allocator.Persistent);
        hashMap = new NativeHashMap<int2, int>(cellsCapacity / chunkSize, Allocator.Persistent);

        lastChunkIndex = 0;
    }

    public ChunkedGrid(ChunkedGrid<T> cloneFrom)
    {
        this.chunkSize = cloneFrom.chunkSize;
        this.totalchunkSize = chunkSize * chunkSize;

        var cloneBuffer = cloneFrom.buffer;
        this.buffer = new NativeList<T>(cloneBuffer.Length, Allocator.Persistent);
        for (int i = 0; i < cloneBuffer.Length; i++)
            this.buffer.Add(cloneBuffer[i]);

        var cloneHashMap = cloneFrom.hashMap;
        var cloneHashMapKeys = cloneFrom.hashMap.GetKeyArray(Allocator.Temp);
        this.hashMap = new NativeHashMap<int2, int>(cloneHashMap.Length, Allocator.Persistent);

        foreach (var key in cloneHashMapKeys)
            this.hashMap.TryAdd(key, cloneHashMap[key]);

        cloneHashMapKeys.Dispose();

        lastChunkIndex = cloneFrom.lastChunkIndex;


    }

    public T this[int x, int y]
    {
        get
        {
            var cellIndex = GetCellIndex(x, y);
            return buffer[cellIndex];
        }
        set
        {
            var cellIndex = GetCellIndex(x, y);
            buffer[cellIndex] = value;
        }
    }

    public void AddCell(int x, int y, T cell)
    {
        var chunk = GetCellChunk(x, y);
        if (!HasChunk(chunk))
            AddChunk(chunk);

        int index = GetCellIndex(x, y, chunk);
        buffer[index] = cell;
    }

    public void AddChunk(int2 chunkPos)
    {
        hashMap.TryAdd(chunkPos, lastChunkIndex);
        for (int i = 0; i < totalchunkSize; i++)
            buffer.Add(default);
        lastChunkIndex += totalchunkSize;
    }

    public bool HasChunk(int2 chunkPos)
    {
        return hashMap.ContainsKey(chunkPos);
    }

    public bool HasCell(int x, int y)
    {
        var chunk = GetCellChunk(x, y);
        return hashMap.ContainsKey(chunk);
    }

    public int2 GetCellChunk(int x, int y)
    {
        var chunkX = x >= 0 ? x / chunkSize : x / (chunkSize + 1) - 1;
        var chunkY = y >= 0 ? y / chunkSize : y / (chunkSize + 1) - 1;
        return new int2(chunkX, chunkY);
    }

    private int GetCellIndex(int x, int y)
    {
        var cellChunk = GetCellChunk(x, y);
        return GetCellIndex(x, y, cellChunk);
    }

    private int GetCellIndex(int x, int y, int2 cellChunk)
    {
        var chunkIndex = hashMap[cellChunk];

        int localX = x % chunkSize;
        int localY = y % chunkSize;

        int cellIndex = Math.Abs(localX) + Math.Abs(localY * chunkSize);
        return chunkIndex + cellIndex;
    }

    public void Dispose()
    {
        buffer.Dispose();
        hashMap.Dispose();
    }

    public IEnumerable<Chunk> Chunks
    {
        get
        {
            var chunkPoses = hashMap.GetKeyArray(Allocator.Temp);

            foreach (var chunkPos in chunkPoses)
            {
                var chunk = new Chunk(this, chunkPos);
                yield return chunk;
            }

            chunkPoses.Dispose();
        }
    }

    public ChunkedGrid<T> Clone()
    {
        return new ChunkedGrid<T>(this);
    }

    public IEnumerator GetEnumerator()
    {
        return buffer.GetEnumerator();
    }

    IEnumerator<T> IEnumerable<T>.GetEnumerator()
    {
        return buffer.GetEnumerator();
    }
}
