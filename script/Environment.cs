using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Unity.Mathematics.Random;

public class Environment : MonoBehaviour
{
    //地图尺寸
    public float MapSizeX = 50;
    public float MapSizeZ = 50;
    public int MaxFoodNum =30;
    

    //食物
    public GameObject food;
    public List<Group> GroupList = new List<Group>();
    public List<Food> foodList = new List<Food>();
    public human[] AllHuman;
    public List<int[]> ToBuildGroupList = new List<int[]>();
    List<int> indexPaired = new List<int>();

    private void Update()


    {
        
        //删除不存在的组,计算每组强度数值
        if (GroupList.Count != 0) { 
        for(int i = GroupList.Count-1; i>0;i--)
       
        {   
            if (GroupList[i].members.Count < 2)
            {
                if(GroupList[i].members.Count != 0)
                {
                     
                        GroupList[i].members[0].mat.color = GroupList[i].members[0].OriginalColor;
                        GroupList[i].members[0].selfGroup = null;
                }
                GroupList.Remove(GroupList[i]);
                
            }

                
                else
                {
                    GroupList[i].GroupStrength = 0;
                    GroupList[i].members.Clear();
                    foreach (human k in AllHuman)
                    {
                        if (k.selfGroup == GroupList[i])
                        {
                            GroupList[i].members.Add(k);
                        }
                    }
                       
                    foreach (human M in GroupList[i].members)
                        GroupList[i].GroupStrength += M.strength;
                       
                     
                    GroupList[i].coherence();
                }
        }

            

        }
        //创建新组
        //print(ToBuildGroupList.Count);
        if(ToBuildGroupList.Count != 0) { 
        foreach (int[] list in ToBuildGroupList)
        {
            bool kt = true;
            for (int f = 0; f < indexPaired.Count; f++)
            {
                if (list[0] == indexPaired[f] || list[1] == indexPaired[f])
                {
                    kt = false;
                        break;
                }

            }


            if (kt == true && list[0] != list[1])
            {

                    Color s = Random.ColorHSV();
                    
                    GroupList.Add(new Group(Random.ColorHSV()));

              
                AllHuman[list[0]].mat.color = GroupList[GroupList.Count - 1].flag;
                AllHuman[list[0]].selfGroup = GroupList[GroupList.Count - 1];
                    GroupList[GroupList.Count - 1].members.Add(AllHuman[list[0]]);
                indexPaired.Add(list[0]);
                
               
                AllHuman[list[1]].mat.color = GroupList[GroupList.Count - 1].flag;
                AllHuman[list[1]].selfGroup = GroupList[GroupList.Count - 1];
                indexPaired.Add(list[1]);
                    GroupList[GroupList.Count - 1].members.Add(AllHuman[list[1]]);

                    GroupList[GroupList.Count - 1].GroupStrength = AllHuman[list[1]].strength + AllHuman[list[0]].strength;
                    

                }

            

        }

      
        }

        int count = 0;
        foreach(human mm in AllHuman)
        {
            if (mm.selfGroup != null)
            {
                count++;
            }
        }
       
        indexPaired.Clear();
        ToBuildGroupList.Clear();

        //补充食物
        while (foodList.Count<MaxFoodNum){
            
            
            Vector3 Position = new Vector3(Random.Range(-0.5f * MapSizeX, 0.5f * MapSizeX),0, Random.Range(-0.5f * MapSizeZ, 0.5f * MapSizeZ));
            GameObject newFood1 = GameObject.Instantiate(food, Position ,new Quaternion(0,0,0,0));
            Food newFood = newFood1.GetComponent<Food>();
            newFood.RequiredStrengthToHunt = Random.Range(1, 5);
            foodList.Add(newFood);
        }
    }
    
  
        
  
}

//聚落类
public class Group
{
    
    public float GroupStrength;
    public Color flag ;
    public List<human> members;
    public float publishDis = 5.0f;
    public Vector3 AverageLoc = Vector3.zero;
    public Vector3 AverageSpeed = Vector3.zero;
    
    public Group(Color co)
    {
        members = new List<human>();
        flag = co;
       
    }

    //集体行动控制成员远离惩罚，靠近奖励//集体行动控制成员速度方向相似
    public void coherence()
    {
        AverageLoc = Vector3.zero;
        AverageSpeed = Vector3.zero;
        foreach (human men in members) {
            AverageLoc += men.transform.position;
            AverageSpeed += men.GetComponent<Rigidbody>().velocity;
        }

        AverageLoc /= members.Count;
        AverageSpeed.Normalize();

        foreach (human men in members)
        {
            
            float velDifference = Vector3.Dot(men.GetComponent<Rigidbody>().velocity, AverageSpeed) * men.GetComponent<Rigidbody>().velocity.magnitude;
        
            if(Vector3.Distance(men.transform.position,AverageLoc) > publishDis)
            {
                men.AddReward(-0.001f);
            }
            else
            {
                men.AddReward(0.001f);
            }

            men.AddReward(velDifference * 0.001f);
        }
    }

    //集体捕猎(民主决议)
    public void hunting()
    {

    }

    //集体捕猎(独裁)


    //分配食物(均分)
    public void distributeFoodAve(Food d)
    {   
        foreach(human m in members)
        {
            m.AddReward(d.Energy /members.Count);
        }
    }
    //分配食物(按劳动量分)
}