using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using RVO;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(interact))]
[RequireComponent(typeof(lookAt))]
public class moveRVO : MonoBehaviour
{
    Animator anim;
    [HideInInspector]
    public NavMeshAgent agent;

    // RVO
    public int agentId;
    int corner;
    float minDistCorner = 0.5f;
    float velFactor = 10.0f;

    // Movement RVO
    bool shouldMove;
    UnityEngine.Vector2 smoothDeltaPosition = UnityEngine.Vector2.zero;
    UnityEngine.Vector2 velocity = UnityEngine.Vector2.zero;

    // Patrol
    public Transform[] patrolTargets;
    bool arrived = false;
    int destPoint = 0;
    bool hasBench = false;

    // lookAt
    lookAt lookAtScript;

    // Interactions
    interact interactScript;

    RVO.Vector2 toRVOVector(Vector3 param)
    {
        return new RVO.Vector2(param.x, param.z);
    }

    Vector3 toUnityVector(RVO.Vector2 param)
    {
        return new Vector3(param.x(), 0, param.y());
    }
    Vector3 toUnityVector2(RVO.Vector2 param)
    {
        return new UnityEngine.Vector2(param.x(), param.y());
    }

    // draw velocity vector
    //void OnDrawGizmos()
    //{
    //    // draw trajectory
    //    if (agent == null || agent.path == null)
    //        return;

    //    var line = this.GetComponent<LineRenderer>();
    //    if (line == null)
    //    {
    //        line = this.gameObject.AddComponent<LineRenderer>();
    //        line.material = new Material(Shader.Find("Sprites/Default")) { color = Color.yellow };

    //        line.startWidth = 0.1f;
    //        line.endWidth = 0.2f;

    //        line.startColor = Color.yellow;
    //        line.endColor = Color.red;
    //    }

    //    var path = agent.path;
    //    line.SetPositions(path.corners);
    //}

    void Start()
    {
        // Set values
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponentInChildren<Animator>();
        interactScript = this.GetComponent<interact>();
        lookAtScript = this.GetComponent<lookAt>();

        // enable lookAt
        enableLookAt(0);

        // Don’t update position automatically
        agent.updatePosition = false;

        // start path to first target
        resetDestination();

        // if on a conversation do not move or interact
        if (interactScript.onConversation)
        {
            interactScript.Start();
            interactScript.startToConversate();
            return;
        }

        // if its animated start triggering animations
        if (interactScript.animated)
        {
            interactScript.callTriggerInteraction();
        }
    }
    public void resetDestination()
    {
        agent.ResetPath();
        agent.SetDestination(patrolTargets[destPoint].position);
        actualizeLookAtTarget(agent.destination);
        corner = 0;
        updateRVO();
    }
    void updateRVO()
    {
        if (corner >= agent.path.corners.Length) return;
        RVO.Vector2 agentLoc = Simulator.Instance.getAgentPosition(agentId);
        RVO.Vector2 goalVector = toRVOVector(agent.path.corners[corner]) - agentLoc;

        if (RVOMath.absSq(goalVector) > 1.0f)
        {
            goalVector = RVOMath.normalize(goalVector);
        }

        Simulator.Instance.setAgentPrefVelocity(agentId, goalVector);
    }
    void Update()
    {
        if (agent.pathPending || !agent.GetComponent<NavMeshAgent>().enabled || interactScript.onConversation)
        {
            return;
        }
        updateRVO();
        move();
        checkArrivedCorner();
        checkArrived();
        checkBench();
    }
    void checkBench()
    {
        if (interactScript.onConversation || !agent.enabled) return;
        // if next target == not empty bench -> GoToNextPoint
        if (destPoint >= patrolTargets.Length)
        {
            StartCoroutine("GoToNextPoint");
            return;
        }
        if (patrolTargets[destPoint].transform.parent.tag == "bench")
        {
            if (agent.remainingDistance <= 8.0f)
            {
                if (!patrolTargets[destPoint].transform.parent.GetComponentInChildren<triggerGoSit>().empty)
                {
                    interactScript.triggerInteractionArrived();
                    resetDestination();
                }
            }
        }
    }
    void move()
    {
        if (interactScript.isSit) return;
        RVO.Vector2 agentVelRVO = Simulator.Instance.getAgentVelocity(agentId);
        RVO.Vector2 agentPosRVO = Simulator.Instance.getAgentPosition(agentId);
        UnityEngine.Vector2 agentVel = toUnityVector2(agentVelRVO);
        Vector3 agentPos = toUnityVector(agentPosRVO);
        float remDist = Vector3.Distance( agentPos, agent.destination);
        
        shouldMove = agentVel.magnitude * velFactor > 0.01f && remDist > agent.radius;

        float velx = Mathf.Lerp(-0.5f, 0.5f, agentVelRVO.x());
        velx *= transform.forward.x;


        float vely = Mathf.Lerp(-0.5f, 0.5f, agentVelRVO.y());
        vely *= transform.forward.z;

        // Update animation parameters
        anim.SetBool("move", shouldMove);
        anim.SetFloat("velx", velx);
        anim.SetFloat("vely", vely);

        checkPeople(); // look at near people
    }
    
    void checkArrivedCorner()
    {
        if (corner >= agent.path.corners.Length) return;
        RVO.Vector2 agentPosRVO = Simulator.Instance.getAgentPosition(agentId);
        Vector3 agentPos = toUnityVector(agentPosRVO);
        float distToCorner = Vector3.Distance(agentPos, agent.path.corners[corner]);

        // if arrived at next corner
        if (distToCorner <= minDistCorner)
        {
            corner++;
        }

    }
    void checkArrived()
    {
        if (interactScript.onConversation || !agent.enabled) return;

        RVO.Vector2 agentPosRVO = Simulator.Instance.getAgentPosition(agentId);
        Vector3 agentPos = toUnityVector(agentPosRVO);
        float remDist = Vector3.Distance(agentPos, agent.destination);

        // if arrived at destiny point
        if (remDist <= agent.stoppingDistance)
        {
            if (!arrived && !interactScript.isSit)
            {
                // trigger animation
                interactScript.triggerInteractionArrived();
                arrived = true;
                StartCoroutine("GoToNextPoint");
            }
        }
    }
    IEnumerator GoToNextPoint()
    {
        if (!agent.enabled) yield return null;
        // wait before go to next point
        yield return new WaitForSeconds(UnityEngine.Random.Range(0.5f, 1.0f));
        if (!agent.enabled) yield return null;
        destPoint = destPoint + 1;

        if (destPoint >= patrolTargets.Length)
        {
            // destroy when finished
            // Simulator.Instance.delAgent(agentId);
            // Destroy(gameObject);

            // loop targets
            destPoint = (destPoint) % patrolTargets.Length;
        }
        if (agent.enabled)
        {
            resetDestination();
        }
        arrived = false;
        yield return null;
    }

    
    void OnAnimatorMove()
    {
        if (this.GetComponent<interact>().onConversation) return;
        // Update postion to agent position
        RVO.Vector2 agentPosRVO = Simulator.Instance.getAgentPosition(agentId);
        Vector3 agentPos = toUnityVector(agentPosRVO);
        
        transform.position = agentPos;
        this.GetComponent<NavMeshAgent>().nextPosition = agentPos + transform.forward;
        //if(shouldMove) transform.position = agentPos;
    }

    IEnumerator interact(float time)
    // time for delay before interaction
    {
        yield return new WaitForSeconds(time);
        triggerInteraction();

        yield return 0;
    }

    void triggerInteraction()
    {
        Debug.Log(this.name + "interact");
        int interaction;
        // if bussiness -> no texting with two hands
        Transform briefcase;
        briefcase = transform.Find("Bip01/Bip01 Pelvis/Bip01 Spine/Bip01 Spine1/Bip01 Spine2/Bip01 Neck/Bip01 L Clavicle/Bip01 L UpperArm/Bip01 L Forearm/Bip01 L Hand/handL/briefcase");
        if (briefcase != null)
        {
            interaction = UnityEngine.Random.Range(0, 3);
        }
        else
        {
            interaction = UnityEngine.Random.Range(0, 4);
        }
        switch (interaction)
        {
            case 0:
                anim.SetTrigger("phone1");
                break;
            case 1:
                anim.SetTrigger("pocket");
                break;
            case 2:
                anim.SetTrigger("watch");
                break;
            case 3:
                anim.SetTrigger("textingOneHand");
                break;
            case 4:
                anim.SetTrigger("texting");
                break;
            default:
                break;
        }
    }
    void triggerInteractionArrived()
    {
        int interaction = UnityEngine.Random.Range(0, 1);
        switch (interaction)
        {
            case 0:
                anim.SetTrigger("look_around");
                break;
            case 1:
                anim.SetTrigger("look_nervous");
                break;
            default:
                break;
        }
    }
    void showPhone(int show)
    {
        // show or hide phone
        bool doShow = show == 1 ? true : false;
        Transform phone;
        phone = transform.Find("Bip01/Bip01 Pelvis/Bip01 Spine/Bip01 Spine1/Bip01 Spine2/Bip01 Neck/Bip01 R Clavicle/Bip01 R UpperArm/Bip01 R Forearm/Bip01 R Hand/handR/Iphone");
        phone.gameObject.SetActive(doShow);

        // if show phone, change weight to 1 for the layer about animation with briefcase or 0.8 if hide phone
        Transform briefcase;
        briefcase = transform.Find("Bip01/Bip01 Pelvis/Bip01 Spine/Bip01 Spine1/Bip01 Spine2/Bip01 Neck/Bip01 L Clavicle/Bip01 L UpperArm/Bip01 L Forearm/Bip01 L Hand/handL/briefcase");
        float weight = show == 1 ? 1.0f : 0.8f;
        if (briefcase != null)
        {
            anim.SetLayerWeight(2, weight);
        }
        // if show phone, change weight to 0 for the layer about arrived or 1 if hide phone
        float weight2 = show == 1 ? 1.0f : 0.0f;
        if (briefcase != null)
        {
            anim.SetLayerWeight(3, weight);
        }
    }
    void enableLookAt(int doLookAt)
    {
        // enable or not lookAt (if animation on going)
        bool doEnable = doLookAt == 1 ? true : false;
        lookAtScript.enabled = doEnable;
        if (doEnable)
        {
            actualizeLookAtTarget(agent.destination);
        }
    }
    void actualizeLookAtTarget(Vector3 lookAtPosition)
    {
        if (lookAtScript)
        {
            lookAtScript.target = lookAtPosition;
            lookAtScript.changeLook();
        }
    }
    void checkPeople()
    {
        if (interactScript.onConversation) return;
        // chech if near people
        // FWD
        rayCast(agent.nextPosition);
        // RIGHT
        Vector3 right = transform.TransformDirection(Vector3.right);
        rayCast(right);
        // LEFT
        Vector3 left = transform.TransformDirection(Vector3.left);
        rayCast(left);
    }
    void rayCast(Vector3 direction)
    {
        RaycastHit hit;
        if (Physics.Raycast(transform.position, direction, out hit, 3))
        {
            if (hit.collider.tag == "Player")
            {
                StartCoroutine(lookNearPerson(hit.transform.position));
                interactScript.shouldTalk(hit.collider.gameObject);
            }
        }
    }
    IEnumerator lookNearPerson(Vector3 nearPersonPosition)
    {
        actualizeLookAtTarget(nearPersonPosition);
        yield return new WaitForSeconds(1.5f);

        actualizeLookAtTarget(agent.destination);
        yield return 0;
    }
    public void setTargets()
    {
        GameObject[] totalPatrolTargets = GameObject.FindGameObjectsWithTag("target");
        ///// Set target list to the avatar
        int min = totalPatrolTargets.Length == 1 ? 1 : 2;
        int max = min + totalPatrolTargets.Length / 2 + 1;
        int numTargetsForAvatar = UnityEngine.Random.Range(min, max);

        // turn to a list to randomly pick numTargetsForAvatar from the options
        List<GameObject> totalPatrolTargetsList = new List<GameObject>(totalPatrolTargets);
        List<Transform> patrolTargetsList = new List<Transform>();
        while (patrolTargetsList.Count < numTargetsForAvatar && totalPatrolTargetsList.Count > 0)
        {
            int index2 = UnityEngine.Random.Range(0, totalPatrolTargetsList.Count);
            if ((totalPatrolTargetsList[index2].name == "targetBench" && !hasBench) || totalPatrolTargetsList[index2].name != "targetBench")
            {
                if (totalPatrolTargetsList[index2].name == "targetBench") hasBench = true;
                patrolTargetsList.Add(totalPatrolTargetsList[index2].transform);
                totalPatrolTargetsList.RemoveAt(index2);
            }
        }
        Transform[] newPatrolTargets = patrolTargetsList.ToArray();

        patrolTargets = newPatrolTargets;
    }
}