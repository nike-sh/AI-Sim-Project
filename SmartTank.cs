using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SmartTank : AITank
{
    private enum AIStates
    {
        idle,
        patrol,
        attack
    }


    [SerializeField] private AIStates state; //current state of the AI
    [SerializeField] private bool changeState;
    [SerializeField] float ammo;


    public Dictionary<GameObject, float> targets = new Dictionary<GameObject, float>();
    public Dictionary<GameObject, float> consumables = new Dictionary<GameObject, float>();
    public Dictionary<GameObject, float> bases = new Dictionary<GameObject, float>();

    public GameObject target;
    public GameObject consumable;
    public GameObject baseFound;

    float t;
    bool fuckoff = false;


    public override void AITankStart()
    {

    }


    public override void AITankUpdate()
    {
        targets = GetTargetsFound;
        consumables = GetConsumablesFound;
        bases = GetBasesFound;
        ammo = GetAmmo;
        FSMController();
    }


    public override void AIOnCollisionEnter(Collision collision)
    {
    }


    public void FSMController()
    {

        ChangeState();

        if (state == AIStates.idle)
        {
            Idle();
        }
        else if (state == AIStates.patrol)
        {
            Patrol();
        }
        else if (state == AIStates.attack)
        {
            Attack();
        }
    }

    public void ChangeState()
    {
        if (changeState)
        {
            switch (state)
            {
                case AIStates.idle:
                    state = AIStates.patrol;
                    changeState = false;
                    break;


                case AIStates.patrol:
                    if (targets.Count > 0 && targets.First().Key != null)
                    {
                        target = targets.First().Key;
                        if (target != null)
                        {
                            state = AIStates.attack;
                            changeState = false;
                        }
                    }
                    break;

                case AIStates.attack:
                    if (target == null)
                    {
                        state = AIStates.patrol;
                        changeState = false;
                    }
                    break;



            }
        }
    }

    private void Idle()
    {
        changeState = true;
        Debug.Log("AI is idle rn");
    }

    private void Patrol()
    {
        Debug.Log("AI is patrolling rn");
        FollowPathToRandomPoint(1f);
        t += Time.deltaTime;
        if (t > 10)
        {
            FindAnotherRandomPoint();
            t = 0;
        }
        changeState = true;
    }

    private void Attack()
    {
        //get closer to target, and fire
        if (Vector3.Distance(transform.position, target.transform.position) < 25f)
        {
            FireAtPointInWorld(target);
        }
        else if (Vector3.Distance(transform.position, target.transform.position) > 35f)
        {
            target = null;
            changeState = true;
        }
        else
        {
            FollowPathToPointInWorld(target, 1f);
        }

    }











    public void AITankUpdateSalimsEdition()
    {
        if (fuckoff)
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
                    if (target != null)
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
                else if (bases.Count > 0)
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
    }
}
