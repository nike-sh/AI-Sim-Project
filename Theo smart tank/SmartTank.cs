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
        flee,
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

    public GameObject homeTreeeee;
    public GameObject target;
    public GameObject consumable;
    public GameObject baseFound;
    public List<GameObject> currentBases;
    float timer;
    private bool workpls = true;


    public override void AITankStart()
    {

    }


    public override void AITankUpdate()
    {
        targets = GetTargetsFound;
        consumables = GetConsumablesFound;
        bases = GetBasesFound;
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
       // else if (state == AIStates.findCover)
       // {
       //     FindCover();
       // }
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
                    {

                        state = AIStates.patrol;
                        
                        changeState = false;
                        break;

                    }
                case AIStates.patrol:
                    if (targets.Count > 0 && targets.First().Key != null)
                    {
                        target = targets.First().Key;
                        if (target != null && currentAmmo >= 4)
                        {
                            state = AIStates.attack;
                            changeState = false;
                        }
                        else if (currentBases.Count < 2)
                        {
                            state = AIStates.defendBase;
                            changeState = false;
                        }
                    }

                
                    if (bases.Count > 0 && bases.First().Key !=null)
                    {
                        baseFound = bases.First().Key;
                        if(baseFound != null && currentAmmo >= 4)
                        {
                            state = AIStates.attack;
                            changeState = false;
                        }

                    }
                    break;

                case AIStates.attack:
                    if (target == null || baseFound == null) // if no tatget or no base, change to patrol
                    {
                        state = AIStates.patrol;
                        changeState = false;
                    }
                    if (GetHealth < 50 || GetAmmo < 5)
                    {
                        //state = AIStates.findCover;
                        //changeState = false;
                    }
                    break;

              //  case AIStates.findCover:
              //    break;

                case AIStates.collectConsumables:
                    if (GetHealth < 50 || GetAmmo < 5 || GetFuel < 50)
                    {
                        changeState = false;
                    }
                    break;

                case AIStates.defendBase:
                    if (target != null)
                    {
                        state = AIStates.attack;
                        changeState = false;
                    }
                    else if (currentBases.Count < 1)
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
        Debug.Log("AI IS IDLE");
    }

    private void Patrol()
    {
        Debug.Log("AI  IS PATROLLING");
        FollowPathToRandomPoint(1f);
        timer += Time.deltaTime;
        if (timer > 10)
        {
            FindAnotherRandomPoint();
            timer = 0;
        }
        changeState = true;
    }

    private void Attack()
    {
        if(target != null)
        {
            Debug.Log("TANK IS ATTACKING");
            //get closer to target, and fire
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

        // If base is found, and distance is less than 25 fire at base else change state to patrol
        if (baseFound != null)
        {
            if (Vector3.Distance(transform.position, baseFound.transform.position) < 25f)
            {
                FireAtPointInWorld(baseFound);
                Debug.Log("FIRING AT ENEMY BASES");
               
            }
            else // get closer to base if base is found
            {
                FollowPathToPointInWorld(baseFound, 1f);
            }
            
        }
        else
        {
            changeState = true;
        }
    }

    /// <summary>
    /// NEED TO IMPLIMENT
    /// </summary>
    private void FindCover()
    {
        Debug.Log("FINDING COVER");
    }

    private void CollectConsumable()
    {
        Debug.Log("FINDING CONSUMABLES");
        if (consumable = GameObject.FindGameObjectWithTag("Health"))
        {
            Debug.Log("FOUND HEALTH");

        }
        if (consumables.Count > 0)
        {
            consumable = consumables.First().Key;
            FollowPathToPointInWorld(consumable, 1f);
            timer += Time.deltaTime;
            if (timer > 10)
            {
                FindAnotherRandomPoint();
                timer = 0;
            }
        }
        else
        {
            target = null;
            consumable = null;
          //  baseFound = null;
            FollowPathToRandomPoint(1f);
            changeState = true;
        }
    }

    private void DefendBase()
    {
        if (currentBases.Count < 2)
        {
            Debug.Log("DEFENDING BASE");
            FollowPathToPointInWorld( homeTreeeee ,1f);
            if (target == null)
            {
                StopTank();
            }
            else
            {
                changeState = true;
                state = AIStates.attack;

                StartTank();
            }
        }
        else if (currentBases.Count == 0)
        {
            state = AIStates.patrol;
            changeState = true;
        }
    }













    /*
    public void AITankUpdateSalimsEdition()
    {
        if (workpls == false)
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
                    timer += Time.deltaTime;
                    if (timer > 10)
                    {
                        FindAnotherRandomPoint();
                        timer = 0;
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
                    timer += Time.deltaTime;
                    if (timer > 10)
                    {
                        FindAnotherRandomPoint();
                        timer = 0;
                    }
                }
            }

        }
    }*/
}
