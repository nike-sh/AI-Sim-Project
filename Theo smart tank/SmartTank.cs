using System;
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
        attack,
        findCover,
        collectConsumables,
        defendBase
    }


    [SerializeField] private AIStates state; //current state of the AI
    [SerializeField] private bool changeState;
    [SerializeField] float currentAmmo;
    [SerializeField] float currentHealth;
    [SerializeField] float currentFuel;


    public Dictionary<GameObject, float> targets = new Dictionary<GameObject, float>();
    public Dictionary<GameObject, float> consumables = new Dictionary<GameObject, float>();
    public Dictionary<GameObject, float> bases = new Dictionary<GameObject, float>();
    public List<GameObject> currentBases;

    public GameObject homeTreeeee;
    public GameObject target;
    public GameObject consumable;
    public GameObject baseFound;

    float t;
    bool work = false;


    public override void AITankStart()
    {

    }


    public override void AITankUpdate()
    {
        targets = GetTargetsFound;
        consumables = GetConsumablesFound;
        currentBases = GetMyBases;
        currentAmmo = GetAmmo;
        currentFuel = GetFuel;
        currentHealth = GetHealth;
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
        else if (state == AIStates.findCover)
        {
            FindCover();
        }
        else if (state == AIStates.collectConsumables)
        {
            CollectConsumable();
        }
        else if (state == AIStates.defendBase)
        {
            DefendBase();
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

                    //Tank goes into attack state if there is enemy in sight
                    if (targets.Count > 0 && targets.First().Key != null)
                    {
                        target = targets.First().Key;
                        if (target != null)
                        {
                            state = AIStates.attack;
                            changeState = false;
                        }
                    }

                    //Tank goes into collect consumables state if there is no enemy in sight
                    if (targets.Count < 0 && consumables.Count > 0)
                    {
                        state = AIStates.collectConsumables;
                        changeState = false;
                    }

                    //Tank goes into defend base state when at least 1 base has been destroyed
                    else if (bases.Count < 2)
                    {
                        state = AIStates.defendBase;
                        changeState = false;
                    }
                    break;



                case AIStates.attack:

                    //Tank goes into patrol state if there are no enemies
                    if (target == null)
                    {
                        state = AIStates.patrol;
                        changeState = false;
                    }

                    //Tank goes into find cover state if his health is lower that 50 or his ammo is lower than 5
                    if (GetHealth < 50 || GetAmmo < 5)
                    {
                        //state = AIStates.findCover;
                        //changeState = false;
                    }

                    //Tank goes into collect consumables state if his health or ammo is low
                    if (GetHealth < 80 || GetAmmo < 8 || GetFuel < 50)
                    {
                        state = AIStates.collectConsumables;
                        changeState = false;
                    }

                    //Tank goes into defend base state if his fuel is really low
                    if(GetFuel < 20)
                    {
                        state = AIStates.defendBase;
                        changeState = false;
                    }
                    break;



                case AIStates.defendBase:

                    //Tank goes into attack state if there is an enemy in sight
                    if (target != null)
                    {
                        state = AIStates.attack;
                        changeState = false;
                    }

                    //Tank goes into patrol state when all of our bases are destroyed
                    else if (bases.Count == 0)
                    {
                        state = AIStates.patrol;
                        changeState = false;
                    }
                    break;


                case AIStates.collectConsumables:
                    break;



                case AIStates.findCover:
                   
                    break;

            }
        }
    }


    private void Idle()
    {
        changeState = true;
        Debug.Log("IDLE");
    }

    private void Patrol()
    {
        Debug.Log("PATROLING");
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
        Debug.Log("ATTACKING");
        if (Vector3.Distance(transform.position, target.transform.position) < 25f)
        {
            FireAtPointInWorld(target);
        }
        else if (Vector3.Distance(transform.position, target.transform.position) > 55f)
        {
            target = null;
            changeState = true;
        }
        else
        {
            FollowPathToPointInWorld(target, 1f);
        }

    }


   private void CollectConsumable()
    {
        Debug.Log("COLLECTING CONSUMABLES");
        target = null;
        consumable = null;
        baseFound = null;
        FollowPathToRandomPoint(1f);

        if (consumables.Count > 0)
        {
            consumable = consumables.First().Key;
            FollowPathToPointInWorld(consumable, 1f);
        }
      
    }


    private void FindCover()
    {
        Debug.Log("FINDING COVER");
    }


    private void DefendBase()
    {
        if (currentBases.Count < 2)
        {
            Debug.Log("DEFENDING BASE");
            FollowPathToPointInWorld(homeTreeeee, 1f);
            if (target == null)
            {
                StopTank();
            }
            else
            {
                StartTank();
                changeState = true;
            }
        }
        else if (currentBases.Count == 0)
        {
            changeState = true;
        }
    }










    public void AITankUpdateSalimsEdition()
    {
        if (work == true)
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
