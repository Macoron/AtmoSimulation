using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class AddWallCommand : ICommand
{
    public int2 pos;

    public void Execute(AtmosSimulation simulation)
    {
        var cell = simulation.currentState[pos.x, pos.y];
        cell.isWall = true;
        simulation.currentState[pos.x, pos.y] = cell;
    }
}

