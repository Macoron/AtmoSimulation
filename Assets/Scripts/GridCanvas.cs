using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GridCanvas : MonoBehaviour
{
    public Text fpsText;

    public TilemapToGrid visualisation;

    // Update is called once per frame
    void Update()
    {
        if (visualisation)
        {
            var simulation = visualisation.simulation;
            if (simulation == null)
                return;

            if (simulation.lastSimulationFrame > 0)
            {
                var fps = 1000f / simulation.lastSimulationFrame;
                fpsText.text = "FPS: " + fps.ToString("n0");
            }
        }


    }
}
