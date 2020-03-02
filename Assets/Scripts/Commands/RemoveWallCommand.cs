using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class RemoveWallCommand : ICommand
{
    public int2 pos;

    public void Execute(AtmosSimulation simulation)
    {
        var cell = simulation.currentState[pos.x, pos.y];
        cell.isWall = false;
        simulation.currentState[pos.x, pos.y] = cell;
    }
}
