using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RVO;


public class triggerGoSit : MonoBehaviour
{
    public bool empty = true;
    GameObject anchor;
    Vector3 toUnityVector(RVO.Vector2 param)
    {
        return new Vector3(param.x(), 0, param.y());
    }
    private void Start()
    {
        anchor = this.transform.GetChild(0).gameObject;
    }
    private void OnTriggerEnter(Collider obj)
    {
        if (!empty) return;
        Vector3 vel;
        float dist;
        if (obj.gameObject.GetComponent<moveRVO>().enabled)
        {
            vel = Vector3.Normalize(toUnityVector(Simulator.Instance.getAgentPrefVelocity(obj.gameObject.GetComponent<moveRVO>().agentId)));
            dist = Vector3.Distance(obj.GetComponent<moveRVO>().agent.destination, this.transform.GetChild(1).transform.position);
        }
        else
        {
            vel = Vector3.Normalize(obj.gameObject.GetComponent<locomotionManual>().agent.velocity);
            dist = Vector3.Distance(obj.GetComponent<locomotionManual>().agent.destination, this.transform.GetChild(1).transform.position);
        }
        Vector3 Lmargin = Vector3.Normalize(Quaternion.AngleAxis(-45, Vector3.up) * vel);
        Vector3 Rmargin = Vector3.Normalize(Quaternion.AngleAxis(45, Vector3.up) * vel);
        float dotLmargin = Vector3.Dot(anchor.transform.forward, Lmargin);
        float dotRmargin = Vector3.Dot(anchor.transform.forward, Rmargin);
        float dotVel = Vector3.Dot(anchor.transform.forward, vel);
        if (((dotVel < dotLmargin && dotVel > dotRmargin) || (dotVel > dotLmargin && dotVel < dotRmargin) || (dotVel-1< -1.9f)) && dist <= 1.6f)
        {
            obj.GetComponentInParent<interact>().goSit(anchor);
        }
    }
    IEnumerator leaveBench()
    {
        GetComponent<BoxCollider>().enabled = false;
        yield return new WaitForSeconds(6.0f);
        empty = true;
        GetComponent<BoxCollider>().enabled = true;
    }
}