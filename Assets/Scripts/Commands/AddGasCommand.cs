using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class AddGasCommand : ICommand
{
    public float ammount = 100f;
    public int2 pos;

    public void Execute(AtmosSimulation simulation)
    {
        var grid = simulation.currentState;
        if (!grid.HasCell(pos.x, pos.y))
        {
            var newCell = new AtmosCell() { isWall = false, pressure = ammount };
            grid.AddCell(pos.x, pos.y, newCell);
            simulation.currentState = grid;
        }
        else
        {
            var cell = simulation.currentState[pos.x, pos.y];
            cell.pressure += ammount;
            simulation.currentState[pos.x, pos.y] = cell;
        }


    }
}
