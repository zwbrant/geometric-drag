using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GlobalWindSource : MonoBehaviour
{
    public Vector3 AirForce = new Vector3();

    public int ForceIndex { get; private set; }

    // Start is called before the first frame update
    void Start()
    {
        ForceIndex = DragSolver.AddGlobalAirForce(AirForce);
    }

    // Update is called once per frame
    void Update()
    {
        DragSolver.SetGlobalAirForce(ForceIndex, AirForce);
    }
}
