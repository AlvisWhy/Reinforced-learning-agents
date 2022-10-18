using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using static Unity.Mathematics.Random;

public class Food : Agent
{
    float MaxSpeed = 0.1f;
    public float RequiredStrengthToHunt;
    public float Energy =2f;
    public float detectRadius = 10f;
    Transform body;
    Rigidbody rig;
    public bool isCaught = false;
    List<human> HumanInrange;
    List<Food> FoodInRange;
    GameObject[] G;
    GameObject[] K;
    int MaxDtectNum = 5;

    private void Start()
    {
        body = GetComponent<Transform>();
        rig = GetComponent<Rigidbody>();
        body.localScale = new Vector3(RequiredStrengthToHunt * 0.5f, 1, RequiredStrengthToHunt * 0.5f);
        Energy *= RequiredStrengthToHunt* RequiredStrengthToHunt;
        isCaught = false;
        HumanInrange = new List<human>();
        FoodInRange = new List<Food>();
        G = GameObject.FindGameObjectsWithTag("Human");
        K = GameObject.FindGameObjectsWithTag("Food");
        foreach (GameObject humanA in G)
        {
            if (Vector3.Distance(humanA.transform.position,body.position) < detectRadius)
            {
                HumanInrange.Add(GetComponent<human>());
            }
        }
        foreach (GameObject FoodA in K)
        {
            if (Vector3.Distance(FoodA.transform.position, body.position) < detectRadius)
            {
                FoodInRange.Add(GetComponent<Food>());
            }
        }
        FoodInRange.Remove(this);
    }

    public override void OnEpisodeBegin()
    {
        isCaught = false;

    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        Vector3 dirToGo = Vector3.zero;
        Vector3 dirToRotate = Vector3.zero;

        var forwardAxis = (int)actions.DiscreteActions[0];
        var rightAxis = (int)actions.DiscreteActions[1];
        var rotateAxis = (int)actions.DiscreteActions[2];

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
        AddReward(0.01f);
        EnergyConsuming();
    }

    public override void CollectObservations(VectorSensor sensor)
    {
       
        sensor.AddObservation(body.position);
        sensor.AddObservation(RequiredStrengthToHunt);

        //感受周围人的位置
        int numH = HumanInrange.Count;
        if (numH == 0)
        {
            for (int i = 0; i < MaxDtectNum; i++)
            {
                sensor.AddObservation(Vector3.zero);
              
            }
        }
        else if(numH < MaxDtectNum && numH > 0)
        {
            for (int i = 0; i < numH; i++)
            {
                if (HumanInrange[i] != null)
                {
                    sensor.AddObservation(HumanInrange[i].transform.position);
                    
                }
                else
                {
                    sensor.AddObservation(Vector3.zero);
                }
            }

            for (int i = 0; i < MaxDtectNum - numH; i++)
            {
                sensor.AddObservation(Vector3.zero);
             
            }
        }
        else
        {
            for (int i = 0; i < MaxDtectNum; i++)
            {
                if (HumanInrange[i] != null)
                {
                    sensor.AddObservation(HumanInrange[i].transform.position);
                 
                }
                else
                {
                    sensor.AddObservation(Vector3.zero);
                }
            }
        }

        //感受周围同类的位置
        int numG = FoodInRange.Count;
        if (numG == 0)
        {
            for (int i = 0; i < MaxDtectNum; i++)
            {
                sensor.AddObservation(Vector3.zero);
                
            }
        }
        else if (numG < MaxDtectNum && numG >0)
        {
            for (int i = 0; i < numG; i++)
            {
                if (FoodInRange[i] != null)
                {
                    sensor.AddObservation(FoodInRange[i].transform.position);

                }
                else
                {
                    sensor.AddObservation(Vector3.zero);
                }
            }

            for (int i = 0; i < MaxDtectNum - numG; i++)
            {
                sensor.AddObservation(Vector3.zero);

            }
        }
        else
        {
            for (int i = 0; i < MaxDtectNum; i++)
            {
                if (FoodInRange[i] != null)
                {
                    sensor.AddObservation(FoodInRange[i].transform.position);

                }
                else
                {
                    sensor.AddObservation(Vector3.zero);
                }
            }
        }
    }

    public void EnergyConsuming()
    {
        AddReward(-1.0f * rig.velocity.magnitude / 1000);

    }

    
}
