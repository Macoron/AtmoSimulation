using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IAtmosEngine : IDisposable
{
    IEnumerator MakeStep(AtmosSimulation simulation);
}
