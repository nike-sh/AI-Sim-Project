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

    public GameObject waypoint;
    public GameObject target;
    public GameObject consumable;
    public GameObject baseFound;

    bool searchingForHealth;
    bool searchingForAmmo;
    bool searchingForFuel;

    float t;
    Vector3 waypointPos;
    bool hasChangedPos;
    [SerializeField]
    bool work = false;

    [SerializeField] int basesDestroyed = 0;
    bool baseDestroyed;
    float destroyT;



    public override void AITankStart()
    {
        hasChangedPos = false;
        baseDestroyed = false;
    }


    public override void AITankUpdate()
    {
        targets = GetTargetsFound;
        bases = GetBasesFound;
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
                    state = AIStates.defendBase;
                    changeState = false;
                    break;



                case AIStates.patrol:

                    //Tank goes into attack state if there is enemy in sight
                    if (targets.Count > 0 && targets.First().Key != null && (GetAmmo < 4 || GetHealth < 40))
                    {
                        state = AIStates.patrol;
                    }
                    else if (targets.Count > 0 && targets.First().Key != null)
                    {
                        target = targets.First().Key;
                        if (target != null)
                        {
                            state = AIStates.attack;
                            changeState = false;
                        }
                    }

                    //Tank goes into collect consumable state if there is a consumable in sight and there are no enemies attacking
                    if (consumables.Count > 0 && consumables.First().Key != null)
                    {
                        consumable = consumables.First().Key;
                        if (consumable != null)
                        {
                            state = AIStates.collectConsumables;
                            changeState = false;
                        }
                    }

                    //Tank attacks base
                    if (bases.Count > 0 && bases.First().Key != null && GetAmmo == 0)
                    {
                        state = AIStates.patrol;
                    }
                    else if (bases.Count > 0 && bases.First().Key != null)
                    {
                        baseFound = bases.First().Key;
                        if (baseFound != null)
                        {
                            state = AIStates.attack;
                            changeState = false;
                        }

                    }
                    break;





                case AIStates.attack:

                    //Tank goes into defend base state unless both bases are destroyed
                    if (target == null)
                    {
                        if (currentBases.Count == 0)
                        {
                            state = AIStates.patrol;
                            changeState = false;
                        }
                        else
                        {
                            state = AIStates.defendBase;
                            changeState = false;
                        }
                    }
                    break;

                case AIStates.defendBase:

                    //Tank goes into attack state if there is an enemy in sight
                    if (targets.Count > 0 && targets.First().Key != null)
                    {
                        target = targets.First().Key;
                        if (target != null)
                        {
                            state = AIStates.attack;
                            changeState = false;
                        }
                    }

                    if (currentBases.Count == 0)
                    {
                        state = AIStates.patrol;
                        changeState = false;
                    }

                    break;


                case AIStates.collectConsumables:
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
                    else
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
        Debug.Log("IDLE");
    }

    private void Patrol()
    {
        Debug.Log("PATROLING");
        waypointResetPos();
        FollowPathToRandomPoint(1.0f);
        t += Time.deltaTime;
        if (t > 15)
        {
            FindAnotherRandomPoint();
            t = 0;
        }
        else
        {
            changeState = true;
        }

    }

    private void Attack()
    {

        if (target != null && GetAmmo == 0)
        {
            changeState = true;
        }
        else if (baseFound != null && GetAmmo <= 4)
        {
            changeState = true;
        }

        // Tank will prioritise attacking enemy tank over bases, if both are found
        if (target != null && bases != null)
        {
            Debug.Log("TANK IS ATTACKING");
            //get closer to target, and fire
            if (Vector3.Distance(transform.position, target.transform.position) < 35f)
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
                FollowPathToPointInWorld(target, 0.8f);
            }
        }
        // Tank will attack bases if they are found and no target is found
        else if (baseFound != null && target == null && basesDestroyed == 0)
        {
            if (Vector3.Distance(transform.position, baseFound.transform.position) < 25f)
            {

                destroyT += Time.deltaTime;
                FireAtPointInWorld(baseFound);
                Debug.Log("FIRING AT ENEMY BASES");

            }
            else // get closer to base if base is found
            {
                FollowPathToPointInWorld(baseFound, 0.5f);
            }
            if (destroyT > 2)
            {
                basesDestroyed = 1;
                destroyT = 0;
            }
        }
        else if (baseFound != null && target == null && basesDestroyed == 1)
        {
            if (Vector3.Distance(transform.position, baseFound.transform.position) < 25f)
            {

                destroyT += Time.deltaTime;
                FireAtPointInWorld(baseFound);
                Debug.Log("FIRING AT ENEMY BASES");

            }
            else // get closer to base if base is found
            {
                FollowPathToPointInWorld(baseFound, 0.5f);
            }
            if (destroyT > 3)
            {
                basesDestroyed = 2;
                destroyT = 0;
            }
        }
        else
        {
            changeState = true;
        }
    }


    private void CollectConsumable()
    {
        Debug.Log("COLLECTING CONSUMABLES");
        waypointResetPos();

        FollowPathToPointInWorld(consumable, 1f);
        if (Vector3.Distance(transform.position, consumable.transform.position) < 5f)
        {
            consumable = null;
            changeState = true;
        }
    }


    private void DefendBase()
    {

        // Debug.Log("DEFENDING BASE");
        if (Vector3.Distance(transform.localPosition, waypoint.transform.position) > 6f)
        {
            FollowPathToPointInWorld(waypoint, 1f);
        }
        if (Vector3.Distance(transform.localPosition, waypoint.transform.position) < 5f)
        {
            if (hasChangedPos == false)
            {
                waypointChangePos();
            }
            changeState = true;
        }
    }


    void waypointChangePos()
    {
        waypointPos = waypoint.transform.position;
        waypointPos.x += 7;
        waypointPos.z += 15;
        waypoint.transform.position = waypointPos;
        hasChangedPos = true;
    }


    void waypointResetPos()
    {
        if (hasChangedPos == true)
        {
            waypointPos = waypoint.transform.position;
            waypointPos.x -= 7;
            waypointPos.z -= 15;
            waypoint.transform.position = waypointPos;
            hasChangedPos = false;
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
