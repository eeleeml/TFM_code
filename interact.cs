using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using RVO;

[RequireComponent(typeof(NavMeshObstacle))]
public class interact : MonoBehaviour
{
    Animator anim;
    NavMeshAgent agent;
    NavMeshObstacle obstacle;

    float timeToStart = 0.0f;
    float repeatRate = 1.0f;
    public bool animated = true;
    public bool isInteracting = false;

    float[] phoneClips;
    float byeTime;

    // talking
    public bool onConversation = false;
    public GameObject conversator;
    public GameObject lastConversator;

    // sit
    public GameObject anchor;
    public bool isSit = false;
    float lerpSpeed = 2.0f;
    public bool IKhandR = true;
    public bool IKhandL = true;

    // RVO
    bool rvo = false;
    RVO.Vector2 toRVOVector(Vector3 param)
    {
        return new RVO.Vector2(param.x, param.z);
    }

    public void Start()
    {
        // Set values
        anim = GetComponent<Animator>();
        agent = GetComponent<NavMeshAgent>();
        obstacle = GetComponent<NavMeshObstacle>();
        // RVO
        if (GetComponent<moveRVO>().enabled) rvo = true;

        anchor = null;
        getClipsLength();
    }
    /// SIT
    /// 
    public void adjustAnchor()
    {
        Vector3 pos = anchor.transform.position;
        anchor.transform.position = new Vector3(pos.x + 0.2f, pos.y, pos.z);
    }
    public void goSit(GameObject sitTarget)
    {
        if (isSit) return;
        isSit = true;
        anchor = sitTarget;
        anchor.GetComponentInParent<triggerGoSit>().empty = false;
        isObstacle(1);
        if (!animated)
        {
            anim.SetBool("move",false);
            StartCoroutine("getUp");
        }
        else
        {
            anim.SetTrigger("goSit");
        }
        changeLayerWeight("head", 0.0f);
        if (rvo)
        {
            Simulator.Instance.setAgentPrefVelocity(GetComponentInParent<moveRVO>().agentId, new RVO.Vector2(0f, 0f));
        }
    }
    void AnimLerp()
    {
        if (!anchor || !animated) return;
        if (Vector3.Distance(this.transform.position, anchor.transform.position) > 0.1f)
        {
            this.transform.rotation = Quaternion.Lerp(transform.rotation, anchor.transform.rotation, Time.deltaTime * lerpSpeed);
            if (rvo)
            {
                Simulator.Instance.setAgentPosition(GetComponentInParent<moveRVO>().agentId, toRVOVector(Vector3.Lerp(transform.position, anchor.transform.position, Time.deltaTime * lerpSpeed)));
            }
            else
            {
                this.transform.position = Vector3.Lerp(transform.position, anchor.transform.position, Time.deltaTime * lerpSpeed);
            }
            
            
        }
        else
        {
            this.transform.rotation = anchor.transform.rotation;
            if (rvo)
            {
                Simulator.Instance.setAgentPosition(GetComponentInParent<moveRVO>().agentId, toRVOVector(anchor.transform.position));
            }
            else
            {
                this.transform.position = anchor.transform.position;
            }
        }
    }
    void FixedUpdate()
    {
        AnimLerp();
    }
    IEnumerator getUp()
    {
        if (!animated)
        {
            yield return new WaitForSeconds(UnityEngine.Random.Range(3.0f, 6.0f));
        }
        else
        {
            anim.SetTrigger("standUp");
            Vector3 correctTo = anchor.transform.position + anchor.transform.forward * 0.4f;
            if (rvo)
            {
                Simulator.Instance.setAgentPosition(GetComponentInParent<moveRVO>().agentId, toRVOVector(correctTo));
            }
            else
            {
                this.transform.position = correctTo;
            }
        }
        isSit = false;
        anchor.GetComponentInParent<triggerGoSit>().StartCoroutine("leaveBench");
        anchor = null;
        yield return new WaitForSeconds(2.0f);
        changeLayerWeight("head", 1.0f);
        isObstacle(0);
    }
    private void OnAnimatorIK(int layerIndex)
    {
        float weight = isSit == false ? 0.0f : anim.GetFloat("IKweight");
        // handR 
        Transform kneeR;
        kneeR = transform.Find("Bip01/Bip01 Pelvis/Bip01 Spine/Bip01 R Thigh/Bip01 R Calf");
        if (kneeR == null)
        {
            kneeR = transform.Find("mixamorig:Hips/mixamorig:RightUpLeg/mixamorig:RightLeg");
        }
        anim.SetIKPosition(AvatarIKGoal.RightHand, kneeR.position);
        anim.SetIKPositionWeight(AvatarIKGoal.RightHand, IKhandR == true? weight:0.0f);
        // handL
        Transform kneeL;
        kneeL = transform.Find("Bip01/Bip01 Pelvis/Bip01 Spine/Bip01 L Thigh/Bip01 L Calf");
        if (kneeL == null)
        {
            kneeL = transform.Find("mixamorig:Hips/mixamorig:LeftUpLeg/mixamorig:LeftLeg");
        }
        anim.SetIKPosition(AvatarIKGoal.LeftHand, kneeL.position);
        anim.SetIKPositionWeight(AvatarIKGoal.LeftHand, IKhandL == true ? weight : 0.0f);
    }
    public void enableHands(string str)
    {
        var opt = str.Split(" "[0]);
        if (opt[0] == "R") // RIGHT
        {
            IKhandR = opt[1] == "T" ? true : false;
        }
        else if(opt[0] == "L")// LEFT
        {
            IKhandL = opt[1] == "T" ? true : false;
        }
        else //BOTH
        {
            IKhandR = opt[1] == "T" ? true : false;
            IKhandL = opt[1] == "T" ? true : false;
        }
    }
    /// INTERACT
    /// 
    
    public void callTriggerInteraction()
    {
        // Animate interaction in timeToStart seconds, then repeatedly every repeatRate seconds if its animated
        timeToStart = UnityEngine.Random.Range(0.5f, 1.5f);
        repeatRate = UnityEngine.Random.Range(0.2f, 1.0f);
        InvokeRepeating("triggerInteraction", timeToStart, repeatRate);
    }
    public void triggerInteraction()
    {
        // check if interacting
        if (isInteracting) return;
        // only some interactions 
        int probInteract = UnityEngine.Random.Range(0, 2);
        if (probInteract == 0) return;
        // Random choice
        int interaction;
        interaction = UnityEngine.Random.Range(0, 12);
        //Debug.Log(interaction);
        switch (interaction)
        {
            case 0:
                if (isSit)
                {
                    StartCoroutine("getUp");
                }
                break;
            case 1:
                anim.SetTrigger("pocket");
                break;
            case 2:
                //phoneClipsEvents(float time, string hands, int shouldInteract, int shouldShowPhone)
                StartCoroutine(phoneClipsEvents(phoneClips[0]*0.05f, "B F", 1, 1));
                anim.SetTrigger("texting");
                StartCoroutine(phoneClipsEvents(phoneClips[0]*0.94f, "B T", 0, 0));
                break;
            case 3:
                StartCoroutine(phoneClipsEvents(0f, "R F", 1, 1));
                anim.SetTrigger("textingOneHand");
                StartCoroutine(phoneClipsEvents(phoneClips[1]*0.97f, "R T", 0, 0));
                break;
            case 4:
                StartCoroutine(phoneClipsEvents(0f, "B F", 1,1));
                anim.SetTrigger("phone");
                StartCoroutine(phoneClipsEvents(phoneClips[2], "B T", 0, 0));
                break;
            case 5:
                anim.SetTrigger("bugs");
                break;
            case 6:
                anim.SetTrigger("look_far");
                break;
            case 7:
                anim.SetTrigger("sweat");
                break;
            case 8:
                anim.SetTrigger("shiver");
                break;
            case 9:
                anim.SetTrigger("touch_arm");
                break;
            case 10:
                anim.SetTrigger("wrist");
                break;
            case 11:
                anim.SetTrigger("watch");
                break;
            case 12:
                anim.SetTrigger("hips");
                break;
            default:
                break;
        }
    }
    public void triggerInteractionArrived()
    {
        // check if interacting
        if (isInteracting) return;
        // only some interactions 
        int probInteract = UnityEngine.Random.Range(0, 2);
        if (probInteract != 0) return;
        // Random choice
        int interaction = UnityEngine.Random.Range(0, 3);
        switch (interaction)
        {
            case 0:
                anim.SetTrigger("look_around");
                break;
            case 1:
                anim.SetTrigger("look_nervous");
                break;
            case 2:
                anim.SetTrigger("look_cross");
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
        if (phone == null)
        {
            phone = transform.Find("mixamorig:Hips/mixamorig:Spine/mixamorig:Spine1/mixamorig:Spine2/mixamorig:RightShoulder/mixamorig:RightArm/mixamorig:RightForeArm/mixamorig:RightHand/handR/Iphone");
        }
        phone.gameObject.SetActive(doShow);

        // if show phone, change weight to 1 for the layer about animation with briefcase "carrySuitcase" or 0.8 if hide phone
        Transform briefcase;
        briefcase = transform.Find("Bip01/Bip01 Pelvis/Bip01 Spine/Bip01 Spine1/Bip01 Spine2/Bip01 Neck/Bip01 L Clavicle/Bip01 L UpperArm/Bip01 L Forearm/Bip01 L Hand/handL/briefcase");
        float weight = show == 1 ? 1.0f : 0.8f;
        if (briefcase != null)
        {
            changeLayerWeight("carrySuitcase", weight);
        }
        // if show phone, change weight to 0 for the layer about "head" or 1 if hide phone
        float weight2 = show == 1 ? 1.0f : 0.0f;
        if (briefcase != null)
        {
            changeLayerWeight("head", weight);
        }
    }

    /// TALK
    /// 
    public void shouldTalk(GameObject otherPerson)
    {
        int talkOpt = UnityEngine.Random.Range(0, 1000);
        if (Vector3.Dot(agent.velocity, otherPerson.GetComponent<NavMeshAgent>().velocity) < 0 && talkOpt == 0)
        {
            // facing each other
            if (lastConversator != otherPerson)
            {
                // not lastConversator
                this.conversator = otherPerson;
                otherPerson.GetComponent<interact>().conversator = this.gameObject;
                startToConversate();
                otherPerson.GetComponent<interact>().startToConversate();
            }
            
        }
    }
    public void startToConversate()
    {
        lastConversator = conversator;
        onConversation = true;
        agent.nextPosition = transform.position;
        if (conversator)
        {
            transform.LookAt(conversator.transform.position);
        }
        if (rvo)
        {
            Simulator.Instance.setAgentPrefVelocity(GetComponentInParent<moveRVO>().agentId, new RVO.Vector2(0f, 0f));
        }
        anim.SetBool("move",false);
        changeLayerWeight("armsHead", 0.0f);
        changeLayerWeight("head", 0.0f);
        changeLayerWeight("conversation", 1.0f);

        int greet = UnityEngine.Random.Range(0, 1);
        if (greet == 0 && animated)
        {
            anim.SetTrigger("hi");
        }
        float repeatRateConv = UnityEngine.Random.Range(0.6f, 1.0f);
        InvokeRepeating("conversate", 0.0f, repeatRateConv);
        isObstacle(1);
    }
    void conversate()
    {
        // check if interacting
        if (isInteracting) return;
        anim.SetBool("buss_conver", true);
        int converOption = UnityEngine.Random.Range(0, 17);
        if (!animated)
        {
            if (converOption == 0)
            {
                StartCoroutine("stopConversationAfterTime");
            }
        }
        else
        {
            switch (converOption)
            {
                case 0:
                    conversator.GetComponent<interact>().StartCoroutine("stopConversation");
                    StartCoroutine("stopConversation");
                    break;
                case 1:
                    anim.SetTrigger("talk1");
                    break;
                case 2:
                    anim.SetTrigger("talk2");
                    break;
                case 3:
                    anim.SetTrigger("talk3");
                    break;
                case 4:
                    anim.SetTrigger("talk4");
                    break;
                case 5:
                    anim.SetTrigger("talk5");
                    break;
                case 6:
                    anim.SetTrigger("talk6");
                    break;
                case 7:
                    anim.SetTrigger("talk7");
                    break;
                case 8:
                    anim.SetTrigger("talk8");
                    break;
                case 9:
                    anim.SetTrigger("talk9");
                    break;
                case 10:
                    anim.SetTrigger("talk10");
                    break;
                case 11:
                    anim.SetTrigger("talk11");
                    break;
                case 12:
                    anim.SetTrigger("talk12");
                    break;
                case 13:
                    anim.SetTrigger("talk13");
                    break;
                case 14:
                    anim.SetTrigger("talk14");
                    break;
                case 15:
                    anim.SetTrigger("talk15");
                    break;
                case 16:
                    anim.SetTrigger("talk");
                    break;
                default:
                    break;
            }
        }
    }
    IEnumerator stopConversationAfterTime()
    {
        yield return new WaitForSeconds(UnityEngine.Random.Range(1.5f, 5.0f));
        conversator.GetComponent<interact>().StartCoroutine("stopConversation");
        StartCoroutine("stopConversation");
        yield return 0;
    }

    IEnumerator stopConversation()
    {
        CancelInvoke();
        if (animated) anim.SetTrigger("bye");
        yield return new WaitForSeconds(byeTime);
        changeLayerWeight("armsHead", 1.0f);
        changeLayerWeight("head", 1.0f);
        changeLayerWeight("conversation", 0.0f);
        anim.SetBool("buss_conver", false);
        isObstacle(0);
        conversator = null;
        isInteracting = false;
        onConversation = false;
        if (rvo)
        {
            GetComponent<moveRVO>().setTargets();
        }
        else
        {
            GetComponent<locomotionManual>().setTargets();
        }
        yield return 0;
    }

    /// GENERAL
    /// 
    void changeLayerWeight(string name, float weight)
    {
        anim.SetLayerWeight(anim.GetLayerIndex(name), weight);
    }
    void changeIsInteracting(int isIt)
    {
        isInteracting = isIt == 0 ? false : true;
    }
    IEnumerator alternIsInteracting(float secs)
    {
        isInteracting = true;
        yield return new WaitForSeconds(secs);
        isInteracting = false;
        yield return 0;
    }
    void isObstacle(int turnObstacle)
    {
        if (turnObstacle == 1)
        {
            agent.enabled = false;
            obstacle.enabled = true;
        }
        else
        {
            obstacle.enabled = false;
            agent.enabled = true;
            if (rvo)
            {
                GetComponent<moveRVO>().resetDestination();
            }
            else
            {
                GetComponent<locomotionManual>().resetDestination();
            }
        }
    }
    IEnumerator phoneClipsEvents(float time, string hands, int shouldInteract, int shouldShowPhone)
    {
        yield return new WaitForSeconds(time);
        enableHands(hands);
        changeIsInteracting(shouldInteract);
        showPhone(shouldShowPhone);
    }
    void getClipsLength()
    {
        phoneClips = new float[3];
        AnimationClip[] clips = anim.runtimeAnimatorController.animationClips;
        foreach (AnimationClip clip in clips)
        {
            //Debug.Log(clip.name);
            switch (clip.name)
            {
                case "texting":
                    phoneClips[0] = clip.length;
                    break;
                case "textingOneHand":
                    phoneClips[1] = clip.length;
                    break;
                case "phone":
                    phoneClips[2] = clip.length;
                    break;
                case "bye":
                    byeTime = clip.length;
                    break;
                default:
                    break;

            }
        }
    }
}
