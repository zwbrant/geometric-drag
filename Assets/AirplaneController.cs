using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AirplaneController : MonoBehaviour
{
    public float TiltRange = 5f;
    public Transform Wing;
    public Transform Rudder;

    public Thruster Thruster;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKey(KeyCode.W))
            Thruster.Power = 300;
        else
            Thruster.Power = 0;

        float yAxis = Input.GetAxis("Mouse Y");
        float xAxis = Input.GetAxis("Mouse X");


        Wing.GetComponent<FuckinAbout>().DragMulti += yAxis * .005f;
        //Wing.RotateAround(Wing.position, Vector3.right, yAxis);

    }
}
