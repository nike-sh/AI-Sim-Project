using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SmartTank : AITank
{
    private enum AIStates //The states of the AI 
    {
        idle,
        patrol,
        attack,
        collectConsumables,
        defendBase
    }


    [SerializeField] private AIStates state; //current state of the AI
    [SerializeField] private bool changeState; //controls the switch of the FSM. Allows to switch from one state to another
    [SerializeField] float currentAmmo; 
    [SerializeField] float currentHealth;
    [SerializeField] float currentFuel;

    public Dictionary<GameObject, float> targets = new Dictionary<GameObject, float>();
    public Dictionary<GameObject, float> consumables = new Dictionary<GameObject, float>();
    public Dictionary<GameObject, float> bases = new Dictionary<GameObject, float>();
    public List<GameObject> currentBases; //our bases


    public GameObject target;
    public GameObject consumable;
    public GameObject baseFound;

    float t;

    Vector3 waypointPos; //Position of the waypoint 
    public GameObject waypoint; //Waypoint game object
    bool hasChangedPos; //indicating if the position of the waypoint has changed

    [SerializeField] int basesDestroyed = 0; //counter for destroyed enemy bases
    float destroyT; //2 seconds are needed to destroy an enemy base. This is a timer which controls the destroyed enemy bases count.



    public override void AITankStart()
    {
        hasChangedPos = false;
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


    public void FSMController() //This function associates the states with their functionality
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


    public void ChangeState() // This is the finite state machine controller 
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

                    //Tank goes into attack state if there is enemy in sight only if it has enough ammo and health, otherwise goes into patrol state
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

                    //Tank goes into patrol state when all enemy bases are destroyed and all of our bases are destroyed
                    if (basesDestroyed == 2 && currentBases.Count == 0)
                    {
                        state = AIStates.patrol;
                        changeState = false;
                    }

                    //Tank goes into defend base state if all enemy bases are destroyed, we have at least one base intact and we have enough health and at least 1 bullet
                    if (basesDestroyed == 2 && currentBases.Count > 0 && (GetAmmo > 0 && GetHealth >= 40))
                    {
                        state = AIStates.defendBase;
                        changeState = false;
                    }

                    //Tank attacks base if it has ammo, otherwise it goes into patrol state
                    if (bases.Count > 0 && bases.First().Key != null && GetAmmo == 0)
                    {
                        state = AIStates.patrol;
                        changeState = false;
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

                    //Tank goes into defend base state if all enemy bases are destroyed, we have at least one base intact and we have enough health and at least 1 bullet
                    if (target == null)
                    {
                        if (basesDestroyed == 2 && currentBases.Count > 0 && (GetAmmo >= 4 && GetHealth >= 40))
                        {
                            state = AIStates.defendBase;
                            changeState = false;
                        }
                        else
                        {
                            state = AIStates.patrol;
                            changeState = false;
                        }

                    }
                    //Tank goes into patrol state if there is no enemy base found
                    else if (baseFound == null)
                    {
                        state = AIStates.patrol;
                        changeState = false;
                    }
                    //Tank goes into patrol state if he hasn't got enough ammo
                    else if (GetAmmo < 4)
                    {
                        state = AIStates.patrol;
                        changeState = false;
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

                    //Tank goes into patrol state if all our bases are destroyed
                    if (currentBases.Count == 0)
                    {
                        state = AIStates.patrol;
                        changeState = false;
                    }

                    break;


                case AIStates.collectConsumables:
                    //Tank goes into attack state if there is enemy in sight, otherwise it will return to patrol
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


    private void Patrol()  //This adds the patrol functionality of the tank
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


    private void Attack() //This function adds the attack functionality of the tank

    {
        waypointResetPos();

        if (target != null && GetAmmo == 0)
        {
            changeState = true;
        }
        else if (baseFound != null && GetAmmo <= 4)
        {
            changeState = true;
        }

        if (target != null && bases != null) // Tank will prioritise attacking enemy tank over bases, if both are found

        {
            Debug.Log("TANK IS ATTACKING");
            if (Vector3.Distance(transform.position, target.transform.position) < 35f) //get closer to target and fire

            {
                FireAtPointInWorld(target);
            }
            else if (Vector3.Distance(transform.position, target.transform.position) > 55f) //if enemy tank is far enough from smartTank, smartTank will change states
            {
                target = null;
                changeState = true;
            }
            else
            {
                FollowPathToPointInWorld(target, 0.8f); //if enemy tank is not near enough, the smartTank will get closer to it
            }
        }
        else if (baseFound != null && target == null && basesDestroyed == 0)  // Tank will attack bases if they are found and no target is found

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


    private void CollectConsumable() //This adds the collect consumable functionality 

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


    private void DefendBase() //This is the defend base function. It goes back to the base and makes sure it faces the right direction in case the enemy tank tries to attack our base
    {

        Debug.Log("DEFENDING BASE");
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


    void waypointChangePos() //when the tank reaches the original waypoint position, the waypoint changes in order to make the tank face the right direction
    {
        waypointPos = waypoint.transform.position;
        waypointPos.x += 7;
        waypointPos.z += 15;
        waypoint.transform.position = waypointPos;
        hasChangedPos = true;
    }


    void waypointResetPos() //everytime the tank changes state the waypoint for defend base resets to it's original position;

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
}