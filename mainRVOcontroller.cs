using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RVO;

public class mainRVOcontroller : MonoBehaviour
{
    List<Vector3> pos;
    int totalAvatars;
    int[] typeAvatar;
    bool hasBench = false;
    public bool conversation = false;
    public bool square = false;

    RVO.Vector2 toRVOVector(Vector3 param)
    {
        return new RVO.Vector2(param.x, param.z);
    }
    // Start is called before the first frame update
    void Start()
    {
        if (GameObject.FindWithTag("Respawn")==null) return;
        avatarPositions();
        avatarType();
        Simulator.Instance.setTimeStep(0.15f);
        Simulator.Instance.setAgentDefaults(15.0f, 10, 5.0f, 5.0f, 2.0f, 2.0f, new RVO.Vector2(0.0f, 0.0f));

        for (int i = 0;i< pos.Count; i++)
        {
            int agentId = Simulator.Instance.addAgent(toRVOVector(pos[i]));
            float speed = 0.3f;
            Simulator.Instance.setAgentMaxSpeed(agentId, speed);
            float radius = Random.Range(0.4f, 0.6f);
            Simulator.Instance.setAgentRadius(agentId, radius);
            Transform[] targets = getTargets();
            int type = square == true ? typeAvatar[agentId] : -1;
            GameObject.FindWithTag("Respawn").GetComponent<RVOspawner>().decideAvatar(agentId, pos[i], targets, radius, speed , -1, type);
        }
        if (conversation) conversators();
    }

    // Update is called once per frame
    void Update()
    {
        Simulator.Instance.doStep();
    }

    Transform[] getTargets()
    {
        GameObject[] totalPatrolTargets = GameObject.FindGameObjectsWithTag("target");
        ///// Set target list to the avatar
        int min = totalPatrolTargets.Length == 1 ? 1 : 2;
        int max = min + totalPatrolTargets.Length / 2 + 1;
        int numTargetsForAvatar = UnityEngine.Random.Range(min, max);

        // turn to a list to randomly pick numTargetsForAvatar from the options
        List <GameObject> totalPatrolTargetsList = new List<GameObject>(totalPatrolTargets);
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
        return newPatrolTargets;
    }
    void avatarPositions()
    {
        // get spawners
        GameObject[] spawners = GameObject.FindGameObjectsWithTag("spawner");

        // variations in spawner position
        Vector3 var1 = new Vector3(2f, 0f, 2f);
        Vector3 var2 = new Vector3(-2f, 0f, -2f);
        Vector3 var3 = new Vector3(-2f, 0f, 0f);
        Vector3[] variants = new Vector3[3] { var1, var2, var3 };
        // fill list of positions
        pos = new List<Vector3>();
        foreach (GameObject sp in spawners)
        {
            pos.Add(sp.transform.position);
            foreach (Vector3 var in variants)
            {
                pos.Add(sp.transform.position + var);
            }
        }
    }
    void avatarType()
    {
        // 0-18 female
        // 19-39 male
        // 40-43 bussiness
        // Animated
        // rep 16 x2 en 15 -> new int[15] { 0, 16, 4, 40, 35, 28, 42, 30, 16, 25, 1, 7, 13, 12, 31 };
        // rep 4 x2, 31 x2, 29 x2  en 15 -> new int[15] { 0, 5, 4, 40, 35, 29, 42, 31, 4, 29, 1, 7, 13, 12, 31 };
        // Non Animated
        // rep 4 x2 en 15 -> new int[15] { 0, 11, 4, 40, 35, 28, 42, 30, 16, 25, 1, 4, 13, 12, 32 };
        // rep 0 x2, 37 x2, 7 x2  en 15 -> new int[15] { 0, 16, 4, 40, 37, 7, 42, 31, 0, 29, 1, 7, 13, 12, 37 };
        typeAvatar = new int[15] { 0, 16, 4, 40, 35, 28, 42, 30, 14, 25, 1, 7, 13, 12, 31 };

    }
    void conversators()
    {
        typeAvatar = new int[0];
        List<Vector3> posConver = new List<Vector3>();

        // conversation 1
        posConver.Add(new Vector3(-443.9f, 0f, -79.23f));
        posConver.Add(new Vector3(-443.9f, 0f, -77.0f));

        // conversation 2
        posConver.Add(new Vector3(-447.0f, 0f, -101.1f));
        posConver.Add(new Vector3(-447.0f, 0f, -103.7f));

        for (int i = 0; i < posConver.Count; i +=2)
        {
            int agentId1 = Simulator.Instance.addAgent(toRVOVector(posConver[i]));
            int agentId2 = Simulator.Instance.addAgent(toRVOVector(posConver[i+1]));
            float speed = 0.5f;
            Simulator.Instance.setAgentMaxSpeed(agentId1, speed);
            Simulator.Instance.setAgentMaxSpeed(agentId2, speed);
            float radius = Random.Range(0.4f, 0.6f);
            Simulator.Instance.setAgentRadius(agentId1, radius);
            Simulator.Instance.setAgentRadius(agentId2, radius);
            Transform[] targets1 = getTargets();
            GameObject.FindWithTag("Respawn").GetComponent<RVOspawner>().decideAvatar(agentId1, posConver[i], targets1, radius, speed, -1, -1);
            Transform[] targets2 = getTargets();
            GameObject.FindWithTag("Respawn").GetComponent<RVOspawner>().decideAvatar(agentId2, posConver[i+1], targets2, radius, speed, agentId1, -1);

        }
    }
}
