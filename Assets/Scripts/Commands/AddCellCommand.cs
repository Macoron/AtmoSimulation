using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class AddCellCommand : ICommand
{
    public int2 pos;

    public void Execute(AtmosSimulation simulation)
    {
        simulation.currentState.AddCell(pos.x, pos.y, new AtmosCell());
    }
}
