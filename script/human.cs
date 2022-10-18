using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using static Unity.Mathematics.Random;
using System.Linq;


public class human : Agent

{  //个体状态
    public int index;
    GameObject bodyC;
    Vector3 startPosition;
    Quaternion startRotation;
    int counter = 0;
    public Transform body;

    public Rigidbody rig;

    //能力
    public float detectRadius = 15.0f;
    int MaxDtectNum = 5;
    int MaxDtectHumanNum = 3;
    public float strength = 2.0f;
    public float MaxSpeed = 0.1f;

    //加入群体分数变化
    float DecideToUnion = 0.1f;
    float DecideToLeave = -1.0f;
    Vector3 SubV;


    //分组状态

    public Material mat;
    public Color OriginalColor;
    public Environment world;
    public Group selfGroup;
    float TimeInAteam;
    public int k2;



    //感知到的环境
    public List<Food> foodInRange;
    public List<human> humamInRange;

    void Start()
    {

        body = GetComponent<Transform>();
        rig = GetComponent<Rigidbody>();
        startPosition = body.position;
        startRotation = body.rotation;
        mat = GetComponent<MeshRenderer>().material;
        OriginalColor = mat.color;
        foodInRange = new List<Food>();
        TimeInAteam = 200;
    }

    public override void OnEpisodeBegin()
    {
        world.GroupList.Clear();
        counter = 0;
        selfGroup = null;
        mat.color = OriginalColor;
        rig.transform.localPosition = startPosition;
        rig.transform.rotation = startRotation;
        rig.velocity = Vector3.zero;
        rig.angularVelocity = Vector3.zero;
        SubV = new Vector3(0, 0, 0);
    }

    public override void OnActionReceived(ActionBuffers actions)

    {
        Vector3 dirToGo = Vector3.zero;
        Vector3 dirToRotate = Vector3.zero;

        var forwardAxis = (int)actions.DiscreteActions[0];
        var rightAxis = (int)actions.DiscreteActions[1];
        var rotateAxis = (int)actions.DiscreteActions[2];
        if (this.selfGroup != null)
        {
            counter++;
        }

        //检测其他人类

        humamInRange.Clear();

        foreach (human h in world.AllHuman)
        {

            if (Vector3.Distance(body.position, h.body.position) < detectRadius)
            {
                this.humamInRange.Add(h);

            }
        }

        this.humamInRange.Remove(this);

        if (humamInRange.Count > MaxDtectHumanNum)
        {
           for (int i = 0; i < humamInRange.Count - 1; i++)
           {
               for (int j = 0; j < humamInRange.Count - 1; j++)
               {
                   if (Vector3.Distance(humamInRange[j].body.position, body.position) > Vector3.Distance(humamInRange[j + 1].body.position, body.position))
                  {
                       var temp = humamInRange[j+1];
                       humamInRange[j + 1] = humamInRange[j];
                       humamInRange[j] = temp;
                   }
               }
            }
        }
        //决定是否离开群体

        if (selfGroup != null)
        {
            int k1 = (int)actions.DiscreteActions[3];
            if (k1 == 0 && counter > TimeInAteam)
            {
                LeaveGroup();
            }
        }


        //决定是否组建或加入群体
        k2 = (int)actions.DiscreteActions[4];


        if (humamInRange.Count != 0)
        {
            foreach (human neighbor in humamInRange)
            {


                //决定是否组建群体
                if (this.selfGroup == null && neighbor.selfGroup == null)
                {
                    if (k2 == 0)
                    {
                        StartATeam(neighbor);

                    }
                }

                //决定是否加入群体
                else if (this.selfGroup == null && neighbor.selfGroup != null)
                {
                    if (k2 == 0)
                    {
                        ChangeGroup(neighbor.selfGroup);
                        break;
                    }
                }

                else { break; }
            }

        }



        //检测猎物

        foodInRange.Clear();

        foreach (Food f in world.foodList)
        {
            if (Vector3.Distance(body.position, f.transform.position) < detectRadius && Vector3.Distance(body.position, f.transform.position) > 0)
            {
                foodInRange.Add(f);
            }
        }

        if (foodInRange.Count > MaxDtectNum)
        {
            for (int i = 0; i < foodInRange.Count - 1; i++)
            {
                for (int j = 0; j < foodInRange.Count - 1; j++)
                {
                  if (Vector3.Distance(foodInRange[j].transform.position, body.position) > Vector3.Distance(foodInRange[j + 1].transform.position, body.position))
                    {
                        var temp = foodInRange[j+1];
                        foodInRange[j + 1] = foodInRange[j];
                        foodInRange[j] = temp;
                    }
                }
            }
        }

        //独立的运动方式
        switch (forwardAxis)
        {
            case 1:
                dirToGo = transform.forward * 1.0f;
                break;
            case 2:
                dirToGo = transform.forward * -1.0f;
                break;
        }
        switch (rightAxis)
        {
            case 1:
                dirToGo = transform.right * 0.5f;
                break;
            case 2:
                dirToGo = transform.right * -0.5f;
                break;
        }
        switch (rotateAxis)
        {
            case 1:
                dirToRotate = transform.up * -0.5f;
                break;
            case 2:
                dirToRotate = transform.up * 0.5f;
                break;
        }

        float speed = Mathf.Clamp(0.5f * MaxSpeed, 1.0f * MaxSpeed, actions.ContinuousActions[0]);

        body.Rotate(dirToRotate, Time.fixedDeltaTime * 100f);
        body.Translate(dirToGo * speed);

        EnergyConsuming();

        var statsRecorder = Academy.Instance.StatsRecorder;
        if (selfGroup != null)
        {
            statsRecorder.Add("GroupMass", selfGroup.members.Count);
        }
        else
        {
            statsRecorder.Add("GroupMass", 1);
        }

        statsRecorder.Add("GroupCount", world.GroupList.Count);

    }

    public override void CollectObservations(VectorSensor sensor)
    {
        //感受自身位置,队伍状态,自身或群组战斗力(6)

        sensor.AddObservation(body.position);
        sensor.AddObservation(selfGroup == null);
        if (this.selfGroup == null)
        {
            sensor.AddObservation(1);
            sensor.AddObservation(this.strength);
        }
        else
        {
            sensor.AddObservation(this.selfGroup.members.Count);
            sensor.AddObservation(this.selfGroup.GroupStrength);
        }


        //感受猎物位置和战斗力(35)
        int numH = foodInRange.Count;
        if (numH == 0)
        {
            for (int i = 0; i < MaxDtectNum; i++)
            {
                sensor.AddObservation(Vector3.zero);
                sensor.AddObservation(0);
                sensor.AddObservation(Vector3.zero);

            }
        }
        else if (numH < MaxDtectNum && numH > 0)
        {
            for (int i = 0; i < numH; i++)
            {
                if (foodInRange[i] != null)
                {
                    sensor.AddObservation(foodInRange[i].transform.position);
                    sensor.AddObservation(foodInRange[i].RequiredStrengthToHunt);
                    sensor.AddObservation(foodInRange[i].GetComponent<Rigidbody>().velocity);
                }
            }

            for (int i = 0; i < MaxDtectNum - numH; i++)
            {
                sensor.AddObservation(Vector3.zero);
                sensor.AddObservation(0);
                sensor.AddObservation(Vector3.zero);
            }
        }
        else
        {
            for (int i = 0; i < MaxDtectNum; i++)
            {
                if (foodInRange[i] != null)
                {
                    sensor.AddObservation(foodInRange[i].transform.position);
                    sensor.AddObservation(foodInRange[i].RequiredStrengthToHunt);
                    sensor.AddObservation(foodInRange[i].GetComponent<Rigidbody>().velocity);

                }
            }
        }
        //感受同类位置和战斗力/同类所在组的战斗力(12)

        int numM = humamInRange.Count;
        if (numM == 0)
        {
            for (int i = 0; i < MaxDtectHumanNum; i++)
            {
                sensor.AddObservation(Vector3.zero);
                sensor.AddObservation(0);
            }
        }
        else if (numM < MaxDtectHumanNum && numM > 0)
        {
            for (int i = 0; i < numM; i++)
            {
                sensor.AddObservation(humamInRange[i].transform.position);
                if (humamInRange[i].selfGroup != null)
                {
                    sensor.AddObservation(humamInRange[i].selfGroup.GroupStrength);
                }
                else
                {
                    sensor.AddObservation(humamInRange[i].strength);
                }
            }

            for (int i = 0; i < MaxDtectHumanNum - numM; i++)
            {
                sensor.AddObservation(Vector3.zero);
                sensor.AddObservation(0);
            }
        }
        else
        {
            for (int i = 0; i < MaxDtectHumanNum; i++)
            {
                sensor.AddObservation(humamInRange[i].transform.position);
                if (humamInRange[i].selfGroup != null)
                {
                    sensor.AddObservation(humamInRange[i].selfGroup.GroupStrength);
                }
                else
                {
                    sensor.AddObservation(humamInRange[i].strength);
                }
            }
        }

    }

    //------------------------行动函数--------------------------------------

    //能量消耗
    public void EnergyConsuming()
    {
        AddReward(-1.0f * rig.velocity.magnitude / 3000);

    }

    //创建新队伍
    public void StartATeam(human Meet)
    {
        if (Meet.k2 == 0)
        {
            world.ToBuildGroupList.Add(new int[] { this.index, Meet.index });
            AddReward(DecideToUnion);
        }
    }
    //换队伍

    //加入已有队伍
    public void ChangeGroup(Group g)
    {

        AddReward(DecideToUnion);
        this.selfGroup = g;

        if (g.members.Contains(this) == false)
        {

            g.members.Add(this);
            g.GroupStrength += this.strength;
            mat.color = g.flag;
        }


    }
    //退出原队伍
    public void LeaveGroup()
    {

        mat.color = OriginalColor;
        AddReward(DecideToLeave);
        counter = 0;
        selfGroup.members.Remove(this);
        selfGroup.GroupStrength -= this.strength;
        if (selfGroup.members.Count == 1)
        {

            selfGroup.members[0].mat.color = OriginalColor;
            selfGroup.members[0].selfGroup = null;
            selfGroup.members.Clear();
            world.GroupList.Remove(selfGroup);
        }
        selfGroup = null;

    }

    //个体捕猎过程
    private void OnCollisionEnter(Collision collision)
    {
        GameObject target = collision.gameObject;

        if (target.CompareTag("Food") && selfGroup == null)
        {

            Food targetFood = target.GetComponent<Food>();
            if (targetFood.RequiredStrengthToHunt < strength)
            {
                AddReward(targetFood.Energy);
                world.foodList.Remove(targetFood);
                foodInRange.Remove(targetFood);
                targetFood.isCaught = true;
                targetFood.AddReward(-5);
                targetFood.EndEpisode();
                GameObject.Destroy(target);
            }

        }

        if (target.CompareTag("Food") && selfGroup != null)
        {
            Food targetFood = target.GetComponent<Food>();
            print(selfGroup.GroupStrength);
            if (targetFood.RequiredStrengthToHunt < selfGroup.GroupStrength)
            {
                print(selfGroup.members.Count);
                AddReward(targetFood.Energy / selfGroup.members.Count);
                world.foodList.Remove(targetFood);
                foodInRange.Remove(targetFood);
                targetFood.isCaught = true;
                //print(targetFood.RequiredStrengthToHunt);
                targetFood.AddReward(-5);
                targetFood.EndEpisode();
                GameObject.Destroy(target);
            }

        }


    }





}
