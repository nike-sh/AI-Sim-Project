using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PatrolingEnemy : MonoBehaviour
{

    private enum AIStates //the states the AI has
    {
        idle,
        patrol,
        attack
    }

    public enum AITypes
    {
        weak,
        strong,
    }


    [SerializeField]private AIStates state; //current state of the AI
    public AITypes type;

    private bool changeState; //is true when changing state is needed

    //waypoint variables
    [SerializeField] private int waypointNum;
    [SerializeField] private Transform[] waypoints; //drag and drop waypoints here (code is written for 2 waypoints)
    [SerializeField] Vector3 nextWaypoint;
    private float distanceToWaypoint;

    [SerializeField] Vector3 currentTarget;//target of the position the AI needs to go
    //enemy stats
    [SerializeField] private int health; 
    [SerializeField] private int speed; 
    [SerializeField] private int attackDamage; 
    [SerializeField] private int defenceSuccessRate; 

    //idle timer variables
    [SerializeField] private float IdleTimer;
    [SerializeField] private int IdleTimerValue;

    private Rigidbody2D enemyRb;

    [SerializeField] private bool facingRight;//used to flip the AI

    //Combat variables
    [SerializeField] private Transform playerDetection; //drag and drop the playerDetection gameObject which is a of the enemy gameObject
    [SerializeField] private Transform playerPos;
    [SerializeField] private LayerMask playerColliderLayer;
    private RaycastHit2D playerAtLeft;
    private RaycastHit2D playerAtRight;
    [SerializeField] private bool playerSeen;
    private const float fieldOfView = 6f;

    private float distanceToPlayer;
    private bool nearPlayer;//state of enemy when is near the player
    [SerializeField] private bool isInitiatingInAttack; //used to control the nearPlayer bool accordingly

    private bool waitBeforeAttack; //enemy stays at one place before attacking the player
    [SerializeField] private float waitTimer;
    private const float waitTimerInitValue = 0.6f;
    private float waitBeforeMovingBack;
    private const float waitBeforeMovingBackInitValue = 0.15f;

    [SerializeField] private bool attack; //state of enemy when is about to attack
    private bool punch;
    private bool attackLeft; //since we want the enemy to attack at only one direction and not follow the player
    private bool attackRight;
    
    
    [SerializeField] private float attackSeconds;
    private const float attackSecondsInitValue = 0.4f;

    [SerializeField]private bool moveBack;
    private float moveBackTimer;
    private const float moveBackTimerInitValue = 1f;




    void Start()
    {
        enemyRb = GetComponent<Rigidbody2D>();
        enemyRb.constraints = RigidbodyConstraints2D.FreezeRotation;

        IdleTimerValue = Random.Range(3 , 6);
        IdleTimer = IdleTimerValue;
        waitTimer = waitTimerInitValue;
        waitBeforeMovingBack = waitBeforeMovingBackInitValue;
        attackSeconds = attackSecondsInitValue;
        moveBackTimer = moveBackTimerInitValue;

        changeState = true;
        facingRight = true;
        isInitiatingInAttack = false;
        moveBack = false;

        waypointNum = 0;

        initializeStats();
       
    }



    // Update is called once per frame
    void Update()
    {
        FSMController();
    }



    private void initializeStats()
    {
        if(type == AITypes.weak)
        {
            health = 100;
            speed = 5;
            attackDamage = 10;
            defenceSuccessRate = 10;
        }
        else if(type == AITypes.strong)
        {
            health = 80;
            speed = 4;
            attackDamage = 20;
            defenceSuccessRate = 15;
        }
    }



    private void FSMController()
    {
        ChangeState();
        Flip(); //flip if needed
        seekPlayer();

        if (state == AIStates.idle)
        {
            Idle();
        }

        else if(state == AIStates.patrol)
        {
            Patrol();
        }

        else if(state == AIStates.attack)
        {
            Attack();
        }

    }
    


    private void ChangeState() //Changes from one state to another. It only changes from one state to another if the changeStates bool is set to true (check the functions to see when it's true)
    {
        if(changeState)
        {
            switch (state)
            {
                case AIStates.idle:
                    if (playerSeen)
                    {
                        state = AIStates.attack;
                        changeState = false; //resetting the changeState bool since it will loop forever
                    } else
                    {
                        state = AIStates.patrol;
                        changeState = false;
                    }
                    
                    break;


                case AIStates.patrol:
                    if (playerSeen)
                    {
                        state = AIStates.attack;
                        changeState = false;
                    }
                    else
                    {
                        state = AIStates.idle;
                        changeState = false;
                    }
                   
                    break;


                case AIStates.attack:
                    state = AIStates.idle;
                    break;


                default:
                    state = AIStates.idle;
                    changeState = false;
                    break;
            }
        }
    }

    

    private void Idle()
    {
        if (playerSeen)
        {
            IdleTimer = IdleTimerValue;
            changeState = true;
        }
        else
        {
            state = AIStates.idle;

            IdleTimer -= Time.deltaTime;
            if (IdleTimer <= 0)
            {
                IdleTimer = IdleTimerValue;
                changeState = true; //making the state transition possible in the ChangeState function
            }
        }
    }



    private void Patrol()
    {
        if (playerSeen)
        {
            changeState = true;
        } else
        {
            Vector3 tempPos = transform.position; // enemy position

            Vector3 fixedWaypointPosRight= new Vector3(waypoints[waypointNum].position.x + 2f, waypoints[waypointNum].position.y, waypoints[waypointNum].position.z); //this is used for a bug fix, where the enemy bugs itself and stays at one place and uses the flip function forever
            Vector3 fixedWaypointPosLeft = new Vector3(waypoints[waypointNum].position.x + 2f, waypoints[waypointNum].position.y, waypoints[waypointNum].position.z);


            currentTarget = waypoints[waypointNum].position; //setting the next waypoint as currentTarget

            if (currentTarget.x > transform.position.x) //going right
            {
                enemyRb.velocity = new Vector2(speed, enemyRb.velocity.y);
            }
            else if (currentTarget.x < transform.position.x) //going left   
            {
                enemyRb.velocity = new Vector2(-speed, enemyRb.velocity.y);
            }

            distanceToWaypoint = (waypoints[waypointNum].position - transform.position).magnitude; //calculating the distance between waypoints
            
            if(distanceToWaypoint <= 0.5f)
            {
                enemyRb.velocity = new Vector2(0 , 0); //stopping the player movement abruptly because otherwise the angular drag would make the player slide

                if (waypointNum == 0) //adjusting the waypoints
                {
                    waypointNum += 1;
                } else if(waypointNum == 1)
                {
                    waypointNum -= 1;
                }
                
                changeState = true; //making the state transition possible in the ChangeState function
            }
        } 
    }



    private void seekPlayer()
    {

        playerAtRight = Physics2D.Raycast(playerDetection.position, Vector2.right, fieldOfView, playerColliderLayer);
        Debug.DrawRay(transform.position, Vector2.right * fieldOfView, Color.green);

        playerAtLeft = Physics2D.Raycast(playerDetection.position, -Vector2.right, fieldOfView, playerColliderLayer);
        Debug.DrawRay(transform.position, -Vector2.right * fieldOfView, Color.green);



        if (playerAtRight.collider != null)
        {
            playerSeen = true;
        }
        if(playerAtLeft.collider != null)
        {
            playerSeen = true;
        }

        else if (playerAtRight.collider == null && playerAtLeft.collider == null)
        {
            playerSeen = false;
        }
    }



    private void Attack()
    {
        currentTarget = playerPos.position;

        if (currentTarget.x > transform.position.x && isInitiatingInAttack == false) //going right
        {
           enemyRb.velocity = new Vector2(speed, enemyRb.velocity.y);
        } else if (currentTarget.x < transform.position.x && isInitiatingInAttack == false) //going left   
        {
            enemyRb.velocity = new Vector2(-speed, enemyRb.velocity.y);
        }


        distanceToPlayer = (playerPos.position - transform.position).magnitude; //calculating distance between enemy and player


        if(distanceToPlayer < 3f && isInitiatingInAttack == false) //when the enemy is near the player it triggers the enemy to stop at one place
        {
            nearPlayer = true;
            isInitiatingInAttack = true;
        }


        if (distanceToPlayer > 7f)
        {
            nearPlayer = false;
        }


        if(nearPlayer) //when the enemy is near the player, it stops at one place
        {
            enemyRb.velocity = new Vector2(0, 0); //stop enemy movement
            waitBeforeAttack = true;
            waitTimer = waitTimerInitValue;
            nearPlayer = false;
        }


        if (waitBeforeAttack) //enemy waits 2.5 seconds before attacking
        {
            if (currentTarget.x > transform.position.x) //moving back
            {
                if(GameObject.Find("Player").GetComponent<Rigidbody2D>().velocity.x < 0 && GameObject.Find("Player").GetComponent<Rigidbody2D>().velocity.y == 0) //it only moves back if the player is going towards the enemy.Although if the player is jumping the enemy doesn't go back since it'd be hard to not get hit
                {
                    waitBeforeMovingBack -= Time.deltaTime;
                    if(waitBeforeMovingBack <= 0)
                    {
                        enemyRb.velocity = new Vector2(-speed, enemyRb.velocity.y);
                    }
                }
            }
            else if (currentTarget.x < transform.position.x) //moving back
            {
                if (GameObject.Find("Player").GetComponent<Rigidbody2D>().velocity.x > 0 && GameObject.Find("Player").GetComponent<Rigidbody2D>().velocity.y == 0)
                {
                    waitBeforeMovingBack -= Time.deltaTime;
                    if (waitBeforeMovingBack <= 0)
                    {
                        enemyRb.velocity = new Vector2(speed, enemyRb.velocity.y);
                    }
                }
            }

            moveBackTimer = waitTimer;
            moveBackTimer -= Time.deltaTime;
            if (moveBackTimer <= 0)
            {
                enemyRb.velocity = new Vector2(0, enemyRb.velocity.y);
                moveBackTimer = moveBackTimerInitValue;
                waitBeforeMovingBack = waitBeforeMovingBackInitValue;
            }


            waitTimer -= Time.deltaTime;
            if (waitTimer <= 0)
            {
                waitTimer = waitTimerInitValue;
                waitBeforeAttack = false;
                attack = true;
                punch = true;
            }
        }


        if(attack) //triggering the attack
        {
            if (distanceToPlayer > 3.5f) //If the player is way to far to attackresetting all of the above variables if the player is too far
            {
                enemyRb.velocity = new Vector2(0, enemyRb.velocity.y);
                waitTimer -= Time.deltaTime;
                if (waitTimer <= 0)
                {
                    nearPlayer = false;
                    waitTimer = waitTimerInitValue;
                    waitBeforeAttack = false;
                    attackSeconds = attackSecondsInitValue;
                    attack = false;
                    moveBack = false;
                    isInitiatingInAttack = false;
                }
            }
            else if (distanceToPlayer < 3.5f)//if player is not too far, the enemy attacks
            {
                if (punch)
                {
                    if (currentTarget.x > transform.position.x) //going to attack the player if he is on the left
                    {
                        attackLeft = true;
                        attackRight = false;
                        punch = false;
                    }
                    else if (currentTarget.x < transform.position.x) //going to attack the player if he is on the right  
                    {
                        attackLeft = false;
                        attackRight = true;
                        punch = false;
                    }
                }

                if (attackLeft) //initiating attack at left
                {
                    enemyRb.velocity = new Vector2(speed * 1.8f, enemyRb.velocity.y);

                }
                if (attackRight) //initiating attack at right
                {
                    enemyRb.velocity = new Vector2(-speed * 1.8f, enemyRb.velocity.y);

                }

                attackSeconds -= Time.deltaTime;
                if (attackSeconds <= 0)
                {
                    attackSeconds = attackSecondsInitValue;
                    attack = false;
                    moveBack = true;

                }
            }
        }


        if (moveBack) //enemy moves back after an attack
        {
            if (currentTarget.x > transform.position.x) //moving back
            {
                enemyRb.velocity = new Vector2(-speed, enemyRb.velocity.y);
            }
            else if (currentTarget.x < transform.position.x) //moving back
            {
                enemyRb.velocity = new Vector2(speed, enemyRb.velocity.y);
            }

            moveBackTimer -= Time.deltaTime;
            if(moveBackTimer <= 0)
            {
                moveBack = false;
                moveBackTimer = moveBackTimerInitValue;
                isInitiatingInAttack = false;
            }
        }


        if (distanceToPlayer > 12.5f) //if the enemy can't see the player anymore
        {
            playerSeen = false;
            changeState = true;

        }
    }



    private void TakeDamage(int damageTaken)
    {
        health -= damageTaken;
        if(health <= 0)
        {
            Death();
        }
    }



    private void BlockAttack()
    {
        Debug.Log("Enemy has blocked attack");
    }



    private void Death()
    {
        Debug.Log("Enemy is dead");
    }



    private void Flip()
    {
        if (currentTarget.x > transform.position.x && !facingRight) //flipping conditions    
        {
            Vector3 tempScale = transform.localScale;
            tempScale.x *= -1;
            transform.localScale = tempScale;
            facingRight = !facingRight;
        }

        else if (currentTarget.x < transform.position.x && facingRight)
        {
            Vector3 tempScale = transform.localScale;
            tempScale.x *= -1;
            transform.localScale = tempScale;
            facingRight = !facingRight;
        }

    }







}
