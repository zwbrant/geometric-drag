using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Thruster : MonoBehaviour
{
    [Range(0, 1000)]
    public float Power = 10f;

    private Rigidbody _rBody;


    // Start is called before the first frame update
    void Start()
    {
        _rBody = GetComponent<Rigidbody>();
        if (_rBody == null)
            _rBody = gameObject.AddComponent<Rigidbody>();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        _rBody.AddRelativeForce(Vector3.forward * Power * 10, ForceMode.Acceleration);
    }
}
