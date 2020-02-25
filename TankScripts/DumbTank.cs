using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DumbTank : AITank
{


    public Dictionary<GameObject, float> targets = new Dictionary<GameObject, float>();
    public Dictionary<GameObject, float> consumables = new Dictionary<GameObject, float>();
    public Dictionary<GameObject, float> bases = new Dictionary<GameObject, float>();

    public GameObject target;
    public GameObject consumable;
    public GameObject baseFound;

    float t;

    public enum tankState
    {
        fire,
        search,

    }
    /*******************************************************************************************************      
    WARNING, do not include void Start(), use AITankStart() instead if you want to use Start method from Monobehaviour.
    *******************************************************************************************************/
    public override void AITankStart()
    {
    }

    /*******************************************************************************************************       
    WARNING, do not include void Update(), use AITankUpdate() instead if you want to use Update method from Monobehaviour.
    *******************************************************************************************************/
    public override void AITankUpdate()
    {
        //Get the targets found from the sensor view
        targets = GetTargetsFound;
        consumables = GetConsumablesFound;
        bases = GetBasesFound;


        //if low health or ammo, go searching
        if (GetHealth < 50 || GetAmmo < 5)
        {
            if (consumables.Count > 0)
            {
                consumable = consumables.First().Key;
                FollowPathToPointInWorld(consumable, 1f);
                t += Time.deltaTime;
                if (t > 10)
                {
                    FindAnotherRandomPoint();
                    t = 0;
                }
            }
            else
            {
                target = null;
                consumable = null;
                baseFound = null;
                FollowPathToRandomPoint(1f);
            }
        }
        else
        {
            //if there is a target found
            if (targets.Count > 0 && targets.First().Key != null)
            {
                target = targets.First().Key;
                if(target != null)
                {
                    //get closer to target, and fire
                    if (Vector3.Distance(transform.position, target.transform.position) < 25f)
                    {
                        FireAtPointInWorld(target);
                    }
                    else
                    {
                        FollowPathToPointInWorld(target, 1f);
                    }
                }
            }
            else if (consumables.Count > 0)
            {
                //if consumables are found, go to it.
                consumable = consumables.First().Key;
                FollowPathToPointInWorld(consumable, 1f);

            }
            else if (bases.Count > 0 )
            {
                //if base if found
                baseFound = bases.First().Key;
                if (baseFound != null)
                {
                    //go close to it and fire
                    if (Vector3.Distance(transform.position, baseFound.transform.position) < 25f)
                    {
                        FireAtPointInWorld(baseFound);
                    }
                    else
                    {
                        FollowPathToPointInWorld(baseFound, 1f);
                    }
                }
            }
            else
            {
                //searching
                target = null;
                consumable = null;
                baseFound = null;
                FollowPathToRandomPoint(1f);
                t += Time.deltaTime;
                if (t > 10)
                {
                    FindAnotherRandomPoint();
                    t = 0;
                }
            }
        }
    }

    /*******************************************************************************************************       
    WARNING, do not include void OnCollisionEnter(), use AIOnCollisionEnter() instead if you want to use Update method from Monobehaviour.
    *******************************************************************************************************/
    public override void AIOnCollisionEnter(Collision collision)
    {
    }
}
