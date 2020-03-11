using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class AddWallCommand : ICommand
{
    public int2 pos;

    public void Execute(AtmosSimulation simulation)
    {
        var grid = simulation.currentState;
        if (!grid.HasCell(pos.x, pos.y))
        {
            var newCell = new AtmosCell() { isWall = true };
            grid.AddCell(pos.x, pos.y, newCell);
            simulation.currentState = grid;
        }
        else
        {
            var cell = simulation.currentState[pos.x, pos.y];
            cell.isWall = true;
            simulation.currentState[pos.x, pos.y] = cell;
        }
    }
}

