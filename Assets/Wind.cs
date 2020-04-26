using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Wind : MonoBehaviour
{
    public Vector3 Direction = new Vector3(1f, 0, 0);
    public float Power = 1f;

    Rigidbody[] _rBodies;
    Vector3 _debugOrigin = new Vector3(0, 5, 0);

    // Start is called before the first frame update
    void Start()
    {
        _rBodies = (Rigidbody[])FindObjectsOfType(typeof(Rigidbody));
        Direction = Vector3.Normalize(Direction);
    }

    // Update is called once per frame
    void Update()
    {
        Debug.DrawLine(_debugOrigin, _debugOrigin + Direction * Power, Color.cyan);
    }

    private void FixedUpdate()
    {
        for (int i = 0; i < _rBodies.Length; i++)
        {
            _rBodies[i].AddForce(Direction * Power);
        }
    }
}
