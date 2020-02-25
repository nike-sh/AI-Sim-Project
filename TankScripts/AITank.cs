using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public abstract class AITank : MonoBehaviour
{


    private float fuel = 100f;
    private float fuelMax = 125f;
    private int ammo;
    private int ammoMax = 15;
    private float health = 100f;
    private float healthMax = 125f;
    private float moveSpeed = 1500;
    private float bodyRotationSpeed = 7f;
    private float turrentRotationSpeed = 2f;
    private GameObject projectileObject;
    private GameObject randomPoint;

    private ParticleSystem smokeParticles;
    private ParticleSystem TankExplosionParticle;
    private ParticleSystem fireParticle;
    private ParticleSystem.EmissionModule smokePartEmission;
    private GameObject turretObject;
    private AStar aStarScript;
    private List<GameObject> myBases = new List<GameObject>();
    private Rigidbody rb;
    private SpriteRenderer ammoSprite;
    private SpriteRenderer fuelSprite;
    private SpriteRenderer healthSprite;
    private Quaternion turretStartRot;
    private bool firing;
    private bool destroyed;
    private Vector3 projectileForce = new Vector3(0, 0, 60);
    private bool collisionWithObstacle;
    private float tankMaxSpeed;
    private float tankMaxSpeedHolder;
    //sensor
    private float viewRadius;
    [Range(0, 360)]
    private float viewAngle;


    private AudioSource engineSound;
    private AudioSource fireSound;


    private LayerMask tankMainMask;
    private LayerMask obstacleMask;
    private LayerMask consumableMask;
    private LayerMask baseMask;

    private GameObject sensorPoint;

    private Dictionary<GameObject, float> targetsFound = new Dictionary<GameObject, float>();
    private Dictionary<GameObject, float> basesFound = new Dictionary<GameObject, float>();
    private Dictionary<GameObject, float> consumablesFound = new Dictionary<GameObject, float>();

    private List<Vector3> pathFound = new List<Vector3>();

    private bool randomNodeFound = true;

    private GameController gameControllerScript;

    // Start is called before the first frame update
    void Start()
    {
        gameControllerScript = GameObject.Find("GameController").GetComponent<GameController>();
        TankExplosionParticle = GameObject.Find("GameController").transform.Find("TankExplosionParticle").GetComponent<ParticleSystem>();
        projectileObject = GameObject.Find("Projectile").gameObject;
        randomPoint = GameObject.Instantiate(GameObject.Find("RandomPoint").gameObject, Vector3.zero, Quaternion.identity);
        rb = GetComponent<Rigidbody>();
        smokeParticles = transform.Find("Model").transform.Find("Body").transform.Find("SmokeParticles").GetComponent<ParticleSystem>();
        fireParticle = transform.Find("Model").transform.Find("Turret").transform.Find("FireParticle").GetComponent<ParticleSystem>();
        ammoSprite = transform.Find("Model").transform.Find("Turret").transform.Find("Ammo").transform.Find("Bar").GetComponent<SpriteRenderer>();
        fuelSprite = transform.Find("Stats").transform.Find("Fuel").transform.Find("Bar").GetComponent<SpriteRenderer>();
        healthSprite = transform.Find("Stats").transform.Find("Health").transform.Find("Bar").GetComponent<SpriteRenderer>();
        smokePartEmission = smokeParticles.emission;
        aStarScript = GameObject.Find("AStarPlane").GetComponent<AStar>();
        turretObject = transform.Find("Model").transform.Find("Turret").gameObject;
        turretStartRot = turretObject.transform.localRotation;
        engineSound = GetComponent<AudioSource>();
        fireSound = transform.Find("FireSound").GetComponent<AudioSource>();

        BaseScript[] basesScript = transform.parent.GetComponentsInChildren<BaseScript>();

        foreach (var item in basesScript)
        {
            myBases.Add(item.gameObject);
        }

        viewRadius = 55;
        viewAngle = 160;

        tankMaxSpeed = 20f;
        tankMaxSpeedHolder = tankMaxSpeed;

        ammo = 12;
        fuel = 100f;
        health = 100f;


        sensorPoint = turretObject;

        //Abstact Function 
        AITankStart();

        tankMainMask = LayerMask.GetMask("TankMain");
        obstacleMask = LayerMask.GetMask("Obstacle");
        consumableMask = LayerMask.GetMask("Consumable");
        baseMask = LayerMask.GetMask("Base");

        StartCoroutine(TargetsFind(0.2f));
    }

    // Update is called once per frame
    void Update()
    {
        //Particles
        smokePartEmission.rateOverTime = Mathf.Abs(((rb.velocity.x + rb.velocity.y + rb.velocity.z) / 3f) * 10f);

        //Fuel Depletion
        fuel -= Mathf.Abs(((rb.velocity.x + rb.velocity.y + rb.velocity.z) / 3f) * 0.003f);
        fuel -= 0.001f;
        fuelSprite.size = new Vector2(fuelSprite.size.x, Mathf.Lerp(0, 1.7f, Mathf.InverseLerp(0, fuelMax, fuel)));
        if (fuel <= 0)
        {
            print(this.transform.parent.gameObject.name + " has no Fuel!");
        }

        //ammo depletion
        ammoSprite.size = new Vector2(ammoSprite.size.x, Mathf.Lerp(0, 1.7f, Mathf.InverseLerp(0, ammoMax, ammo)));

        //Health 
        healthSprite.size = new Vector2(healthSprite.size.x, Mathf.Lerp(0, 1.7f, Mathf.InverseLerp(0, healthMax, health)));

        if (health <= 0)
        {
            destroyed = true;
            print(this.transform.parent.gameObject.name + " has been destroyed!");
            StartCoroutine(DestroyWait());
        }


        //sound
        engineSound.pitch = Mathf.Abs(((Mathf.Abs(rb.velocity.x) + Mathf.Abs(rb.velocity.y) + Mathf.Abs(rb.velocity.z)) / 3f) * 0.12f + 0.3f); 

        //Abstract Function
        AITankUpdate();
    }




    IEnumerator DestroyWait()
    {

        yield return new WaitForSeconds(3f);
        GameObject.Instantiate((GameObject)TankExplosionParticle.gameObject, transform.position, Quaternion.identity).GetComponent<ParticleSystem>().Play();
        Destroy(this.gameObject);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.tag == "Projectile")
        {
            if (projectileHit == false)
            {
                StartCoroutine(ProjectileHit());
            }
        }

        if (collision.gameObject.tag == "Health")
        {
            health = Mathf.Clamp(health + 25f, 0f, healthMax);
            print(this.transform.parent.gameObject.name + " has collected Health!");
            collision.gameObject.SetActive(false);
        }

        if (collision.gameObject.tag == "Ammo")
        {
            ammo = Mathf.Clamp(ammo + 3, 0, ammoMax);
            print(this.transform.parent.gameObject.name + " has collected Ammo!");
            collision.gameObject.SetActive(false);
        }


        if (collision.gameObject.tag == "Fuel")
        {
            fuel = Mathf.Clamp(fuel + 30f, 0f, fuelMax);
            print(this.transform.parent.gameObject.name + " has collected Fuel!");
            collision.gameObject.SetActive(false);
        }



        if (collision.gameObject.tag == "Obstacle")
        {
            if(collisionWithObstacle == false)
            {
                StartCoroutine(CollisionWithObstacle());
            }

        }

        if (collision.gameObject.tag == "ObstacleRock")
        {
            if (collisionWithObstacleRock == false)
            {
                StartCoroutine(CollisionWithObstacleRock());
            }

        }

        AIOnCollisionEnter(collision);
    }


    IEnumerator CollisionWithObstacle()
    {
        collisionWithObstacle = true;
        yield return new WaitForSeconds(1.4f);
        collisionWithObstacle = false;
        yield return new WaitForSeconds(5f);


    }

    IEnumerator CollisionWithObstacleRock()
    {
        collisionWithObstacleRock = true;
        yield return new WaitForSeconds(2.5f);
        collisionWithObstacleRock = false;
        yield return new WaitForSeconds(5f);


    }

    IEnumerator ProjectileHit()
    {
        projectileHit = true;
        health -= 15f;
        print(this.transform.parent.gameObject.name + " has been hit!");
        yield return new WaitForSeconds(1f);
        projectileHit = false;
        yield return new WaitForSeconds(0.5f);


    }

    //Request a path from this to pointInWorld;
    public void FindPathTo(GameObject pointInWorld)
    {
        List<Node> path = new List<Node>();

        if (pointInWorld != null && fuel > 0)
        {
            AStar tempAStar = aStarScript;

            path = tempAStar.RequestPath(this.gameObject, pointInWorld);
        }

        if (path != null && path.Count > 3)
        {
            pathFound.Clear();
            foreach (Node item in path)
            {
                pathFound.Add(item.nodePos);
            }
        }
    }



    //Follow path to a target
    public void FollowPathToPointInWorld(GameObject pointInWorld, float normalizedSpeed)
    {
        randomNodeFound = true;

        float speed = Mathf.Lerp(0f, moveSpeed, normalizedSpeed);

        if (!firing && fuel > 0)
        {
            if(pointInWorld != null)
            {
                //Request Path
                FindPathTo(pointInWorld);
            }
            if (pathFound != null)
            {
                MoveTank(pathFound, speed);
            }

            if (pointInWorld != null)
            {
                FaceTurretToPointInWorld(pointInWorld.transform.position);
            }
        }

    }


    public void FollowPathToRandomPoint(float normalizedSpeed)
    {
        float speed = Mathf.Lerp(0f, moveSpeed, normalizedSpeed);

        if (randomNodeFound)
        {
            StartCoroutine(GenerateRandomPointInWorld());
            FindPathTo(randomPoint);

        }

        if (!firing && fuel > 0)
        {
            FindPathTo(randomPoint);
            TurretReset();
            MoveTank(pathFound, speed);
        }

        if (Vector3.Distance(transform.position, randomPoint.transform.position) < 12)
        {
            randomNodeFound = true;
        }    
    }

    public void FindAnotherRandomPoint()
    {
        randomNodeFound = true;
    }

    IEnumerator GenerateRandomPointInWorld()
    {
        AStar tempAStar = aStarScript;


        Node randomNode = tempAStar.NodePositionInGrid(new Vector3(Random.Range(-90, 90), 0, Random.Range(-90, 90)));
        Vector3 consPos = Vector3.zero;
        while (!randomNode.traversable)
        {
            randomNode = tempAStar.NodePositionInGrid(new Vector3(Random.Range(-90, 90), 0, Random.Range(-90, 90)));

            yield return new WaitForEndOfFrame();
        }
        randomNodeFound = false;
        randomPoint.transform.position = randomNode.nodePos;
    }

    private Vector3 velocity;
    private Vector3 velocityRot;
    private Vector3 velocityCentre;

    private void MoveTank(List<Vector3> path, float speed)
    {
        if (gameControllerScript.gameStarted)
        {
            StartTank();
            if (path != null)
            {
                Vector3 centrePos = FindCentre(path);

                TankLookAt(centrePos);

                if (collisionWithObstacle || collisionWithObstacleRock)
                {
                    rb.AddRelativeForce(Vector3.back * speed, ForceMode.Impulse);
                    rb.velocity = Vector3.ClampMagnitude(rb.velocity, tankMaxSpeed * 0.8f);           
                }
                else
                {
                    rb.AddRelativeForce(new Vector3(0, 0, 1) * speed, ForceMode.Impulse);
                    rb.velocity = Vector3.ClampMagnitude(rb.velocity, tankMaxSpeed);
                }

            }
            else if (collisionWithObstacle || collisionWithObstacleRock)
            {
                Vector3 centrePos = FindCentre(path);

                rb.AddRelativeForce(Vector3.back * speed, ForceMode.Impulse);
                rb.velocity = Vector3.ClampMagnitude(rb.velocity, tankMaxSpeed * 1.5f);
                TankLookAt(centrePos);
            }
            else
            {
                rb.AddRelativeForce(Vector3.forward * speed, ForceMode.Impulse);
                rb.velocity = Vector3.ClampMagnitude(rb.velocity, tankMaxSpeed * 1.5f);
            }
        }
    }

    void TankLookAt(Vector3 pos)
    {
        transform.LookAt(Vector3.SmoothDamp(transform.position, pos, ref velocityRot, 7f));
    }

    public void StopTank()
    {
        tankMaxSpeed = tankMaxSpeedHolder * 0.3f;

    }


    public void StartTank()
    {
        tankMaxSpeed = tankMaxSpeedHolder;

    }


    private Vector3 velocityTurretRot;
    private bool projectileHit;
    private bool collisionWithObstacleRock;

    //Face turret to target
    public void FaceTurretToPointInWorld(Vector3 pointInWorld)
    {
        Vector3 faceTarget = new Vector3(pointInWorld.x, pointInWorld.y, pointInWorld.z);

        faceTarget = Vector3.SmoothDamp(turretObject.transform.position, faceTarget, ref velocityTurretRot, turrentRotationSpeed);

        turretObject.transform.LookAt(faceTarget);

    }




    public void TurretReset()
    {
        turretObject.transform.localRotation = turretStartRot;
    }




    public void FireAtPointInWorld(GameObject pointInWorld)
    {
        StopTank();
        randomNodeFound = true;
        if (!firing && ammo > 0)
        {
            firing = true;
            StartCoroutine(Fire(pointInWorld));
        }
        else if (ammo <= 0)
        {
            print(this.transform.parent.gameObject.name + " has no Ammo!");
        }
    }

    IEnumerator Fire(GameObject target)
    {
        
        float tWait = 1.2f;

        while (tWait > 0 && target != null && ammo > 0)
        {
            FaceTurretToPointInWorld(target.transform.position);
            tWait -= Time.deltaTime;
            if(ammo == 0)
            {
                break;
            }
            yield return null;
        }

        fireParticle.Play();
        if (fireSound.isPlaying)
        {
            fireSound.Stop();
        }
        fireSound.Play();
        ammo -= 1;
        print(this.transform.parent.gameObject.name + " has Fired!");

        Vector3 turPart = turretObject.transform.Find("TurretPart").position;
        Rigidbody bulletClone = (Rigidbody)Instantiate(projectileObject.GetComponent<Rigidbody>(), new Vector3(turPart.x + 0.55f, turPart.y + 1.7f, turPart.z)
                                                                                                            , turretObject.transform.rotation);
        bulletClone.isKinematic = false;

        bulletClone.AddRelativeForce(projectileForce, ForceMode.Impulse);

        tWait = 1;

        while (tWait > 0)
        {
            tWait -= Time.deltaTime;
            yield return null;
        }

        firing = false;

    }

    private Vector3 FindCentre(List<Vector3> _path)
    {
        float x = 0;
        float y = 0;
        float z = 0;

        int pathCount = Mathf.Clamp(_path.Count, 0, 5);

        for (int i = 0; i < pathCount; i++)
        {
            x += _path[i].x;
            y += this.transform.position.y;
            z += _path[i].z;

        }

        x = x / pathCount;
        y = y / pathCount;
        z = z / pathCount;

        Vector3 centrePosNew = new Vector3(x, y, z);

        return centrePosNew;
    }

    //Sensor Stuff
    IEnumerator TargetsFind(float delay)
    {
        while (true)
        {

            yield return new WaitForSeconds(delay);
            targetsFound.Clear();
            consumablesFound.Clear();
            basesFound.Clear();
            FindVisibleTargets();
        }
    }

    void FindVisibleTargets()
    {


        Collider[] targetsInViewRadius = Physics.OverlapSphere(sensorPoint.transform.position, viewRadius, tankMainMask);
        Collider[] consumableInViewRadius = Physics.OverlapSphere(sensorPoint.transform.position, viewRadius, consumableMask);
        Collider[] baseInViewRadius = Physics.OverlapSphere(sensorPoint.transform.position, viewRadius, baseMask);

        for (int i = 0; i < targetsInViewRadius.Length; i++)
        {
            GameObject target = targetsInViewRadius[i].gameObject;

            Vector3 directionToTarget = (target.transform.position - sensorPoint.transform.position).normalized;


            if (Vector3.Angle(sensorPoint.transform.forward, directionToTarget) < viewAngle / 2)
            {
                float distanceToTarget = Vector3.Distance(sensorPoint.transform.position, target.transform.position);

                if (!Physics.Raycast(sensorPoint.transform.position, directionToTarget, distanceToTarget, obstacleMask))
                {
                    if (target != this.gameObject && !targetsFound.ContainsKey(target))
                    {
                        targetsFound.Add(target, distanceToTarget);
                    }
                }
            }

        }

        for (int i = 0; i < consumableInViewRadius.Length; i++)
        {
            GameObject target = consumableInViewRadius[i].gameObject;

            Vector3 directionToTarget = (target.transform.position - sensorPoint.transform.position).normalized;


            if (Vector3.Angle(sensorPoint.transform.forward, directionToTarget) < viewAngle / 2)
            {
                float distanceToTarget = Vector3.Distance(sensorPoint.transform.position, target.transform.position);

                if (!Physics.Raycast(sensorPoint.transform.position, directionToTarget, distanceToTarget, obstacleMask))
                {
                    if (target != this.gameObject && !targetsFound.ContainsKey(target))
                    {
                        consumablesFound.Add(target, distanceToTarget);
                    }
                }
            }

        }

        for (int i = 0; i < baseInViewRadius.Length; i++)
        {
            GameObject target = baseInViewRadius[i].gameObject;

            Vector3 directionToTarget = (target.transform.position - sensorPoint.transform.position).normalized;


            if (Vector3.Angle(sensorPoint.transform.forward, directionToTarget) < viewAngle / 2)
            {
                float distanceToTarget = Vector3.Distance(sensorPoint.transform.position, target.transform.position);
                if (!Physics.Raycast(sensorPoint.transform.position, directionToTarget, distanceToTarget, obstacleMask))
                {
                    if (target != this.gameObject && !targetsFound.ContainsKey(target) && !myBases.Contains(target))
                    {
                        basesFound.Add(target, distanceToTarget);
                    }
                }
            }

        }
    }

    public Vector3 DirectionFromAngle(float angleInDegrees, bool angleIsGlobal)
    {
        if (!angleIsGlobal)
        {
            angleInDegrees += sensorPoint.transform.eulerAngles.y;
        }

        return new Vector3(Mathf.Sin(angleInDegrees * Mathf.Deg2Rad), 0, Mathf.Cos(angleInDegrees * Mathf.Deg2Rad));
    }

    private void OnDrawGizmos()
    {
        if (sensorPoint != null)
        {

            Handles.color = Color.white;

            Vector3 sensorAngleA = DirectionFromAngle(-viewAngle / 2, false);
            Vector3 sensorAngleB = DirectionFromAngle(viewAngle / 2, false);

            Handles.DrawLine(sensorPoint.transform.position, sensorPoint.transform.position + sensorAngleA * viewRadius);
            Handles.DrawLine(sensorPoint.transform.position, sensorPoint.transform.position + sensorAngleB * viewRadius);

            Handles.color = Color.red;
            foreach (KeyValuePair<GameObject, float> item in targetsFound)
            {
                if (item.Key != null)
                {
                    Handles.DrawLine(sensorPoint.transform.position, item.Key.transform.position);
                }
            }



            Handles.color = Color.green;


            foreach (KeyValuePair<GameObject, float> item in consumablesFound)
            {
                if (item.Key != null)
                {
                    Handles.DrawLine(sensorPoint.transform.position, item.Key.transform.position);
                }
            }




            Handles.color = Color.blue;


            foreach (KeyValuePair<GameObject, float> item in basesFound)
            {
                if (item.Key != null)
                {
                    Handles.DrawLine(sensorPoint.transform.position, item.Key.transform.position);
                }
            }
        }



        foreach (Vector3 node in pathFound)
        {
           
            Gizmos.color = Color.black;
            Gizmos.DrawCube(node, new Vector3(3 * 0.9f, 0.1f, 3 * 0.9f));
        }



    }



    public bool IsFiring
    {
        get
        {
            return firing;
        }
    }

    public bool IsDestroyed
    {
        get
        {
            return destroyed;
        }
    }

    public float GetHealth
    {
        get
        {
            return health;
        }
    }

    public float GetAmmo
    {
        get
        {
            return ammo;
        }
    }


    public float GetFuel
    {
        get
        {
            return fuel;
        }
    }

    public List<GameObject> GetMyBases
    {
        get
        {
            return myBases;
        }
    }

    public Dictionary<GameObject, float> GetTargetsFound
    {
        get
        {
            return targetsFound;
        }
    }

    public Dictionary<GameObject, float> GetConsumablesFound
    {
        get
        {
            return consumablesFound;
        }
    }

    public Dictionary<GameObject, float> GetBasesFound
    {
        get
        {
            return basesFound;
        }
    }

    public abstract void AITankStart();


    public abstract void AITankUpdate();
    public abstract void AIOnCollisionEnter(Collision collision);


}




