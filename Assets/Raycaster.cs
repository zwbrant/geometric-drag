using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using Vec3 = UnityEngine.Vector3;

public class Raycaster : MonoBehaviour
{
    public int RayCount;
    public bool DrawRays = false;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    float _timer = 0f;
    JobHandle _jobHandle;

    // Update is called once per frame
    void Update()
    {
        if (_timer >= 1f)
        {
            for (int i = 0; i < RayCount; i++)
            {

                RaycastHit hit;
                // Does the ray intersect any objects excluding the player layer
                if (Physics.Raycast(new Vec3(i / 10, 0, 0), Vec3.up, out hit, Mathf.Infinity))
                {
                    Debug.DrawRay(new Vec3(i / 10, 0, 0), Vec3.up * hit.distance, Color.yellow);
                }
                else
                {
                    Debug.DrawRay(new Vec3(i / 10, 0, 0), Vec3.up * 1000, Color.white);
                }
            }
            _timer = 0f;

        } else
        {
            _timer += Time.deltaTime;

        }



        //var job = new RayJob();

        //_jobHandle = job.Schedule(100, 1);

    }

    private void LateUpdate()
    {
        
    }

    public struct RayJob : IJobParallelFor
    {
        public void Execute(int index)
        {
            
        }
    }
}
