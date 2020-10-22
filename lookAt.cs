using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Animator))]
public class lookAt : MonoBehaviour
{
    public Transform head = null;
    public Vector3 target;
    public float speed = 2.0f;    

    void Start()
    {
        if (!head)
        {
            head = transform.Find("Bip01/Bip01 Pelvis/Bip01 Spine/Bip01 Spine1/Bip01 Spine2/Bip01 Neck/Bip01 Head");
            if (head == null)
            {
                head = transform.Find("mixamorig:Hips/mixamorig:Spine/mixamorig:Spine1/mixamorig:Spine2/mixamorig:Neck/mixamorig:Head");
            }
            if (!head)
            {
                Debug.LogError("No head transform - LookAt disabled");
                enabled = false;
            }
            return;
        }
    }

    public void changeLook()
    {
        if (target.x == Mathf.Infinity) return;
        if (!GetComponent<lookAt>().enabled && Vector3.Distance(target, transform.position) ==0) return;
        var targetRotation = Quaternion.LookRotation(target - transform.position);
        // Smoothly rotate towards the target point.
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, speed * Time.deltaTime);
    }
}
