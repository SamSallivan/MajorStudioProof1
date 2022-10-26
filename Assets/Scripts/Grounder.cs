using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Grounder : MonoBehaviour
{	
	public delegate void Action();
	public Action OnGround;

	public Action OnUnground;

	public LayerMask groundMask;

	public bool grounded;
	public int groundContactCount;

	public int stepSinceUngrounded;
    public float timeSinceUngrounded;

    public float delay = 3f;

	public float highestPoint;

	public float jumpHeight;

	public float maxGroundAngle = 75f;

	public float minGroundNormal;

	public Vector3 tempGroundNormal;

	public Rigidbody rb;

	public PlayerController pc;

	public RaycastHit hit;

	public ContactPoint contactPoint;

	public Collider groundCollider;

	public Vector3 groundNormal;

    public Vector3 lastGroundNormal;


    void Awake()
    {
        pc = GetComponent<PlayerController>();
		rb = GetComponent<Rigidbody>();
		groundNormal = Vector3.up;
		highestPoint = transform.position.y;
		jumpHeight = 0f;
		minGroundNormal = Mathf.Cos(maxGroundAngle * ((float)Mathf.PI / 180f)); //translates a 0-90 angle to a 1-0 normal value.
    }	
    
	//executes when lands from air.
	public void Ground()
	{

        grounded = true;
		stepSinceUngrounded = 0;
		timeSinceUngrounded = 0;

        pc.gTimer = 0f;


		//if not climbing
		if (!pc.rb.isKinematic)
		{
			//recalculates velocity based on ground normal. 
			pc.rb.velocity = Vector3.ProjectOnPlane(pc.vel, groundNormal);
			pc.headPosition.Bounce((0f - jumpHeight) / 12f);
		}

		if(OnGround != null)
			OnGround();

	}

    public void Unground()
	{
		//sets a delay that pauses ground checking in a short while
		//prevent player from jump again before leaving the grounded raycast distance.
		delay = 5f;

		if (grounded)
		{
			grounded = false;
			groundContactCount = 0;
			highestPoint = transform.position.y;
			tempGroundNormal = Vector3.zero;
			groundNormal = Vector3.up;
			
			//sets a timeframe that allows player to jump after ungrounding
            pc.gTimer = 0.2f;
		}

		if(OnUnground != null)
			OnUnground();
		
	}

	//checks whether player is close enough to ground, if not grounded.
	public bool CheckWithRaycast(float dot = 0f)
	{
		Physics.Raycast(rb.position, -rb.transform.up, out hit, 1.75f, groundMask);
		//return false;
		return hit.normal.y > dot;
	}

    void Update()
    {
		if (!grounded)
		{
		
			//updates the highestpoint during an unground.
			if (transform.position.y > highestPoint)
			{
				highestPoint = transform.position.y;
			}

		//calculates how high the player has fell.
		jumpHeight = highestPoint - transform.position.y;

		}
    }

	private void FixedUpdate()
	{
		if (delay > 0f)
		{
			delay -= 1f;
			return;
		}

        UpdateState();
	}



	private void UpdateState()
	{
		if (groundContactCount > 0)
		{
			if (groundContactCount > 1)
			{
				//normalizes the sum of ground normal, if there are multiple
				tempGroundNormal.Normalize();
			}

			groundNormal = tempGroundNormal;

			if (!grounded)
			{
				Ground();
            }
        }
		else
		{	
			//if player was ungrounded not long ago, and the normal of the ground below it still meets the minimal ground normal, ground the player.
			//for smoother control on bumpy surfaces.
			if (stepSinceUngrounded < 5 && CheckWithRaycast(minGroundNormal))
			{
				if (!grounded)
				{
					Ground();
				}

				tempGroundNormal = hit.normal;

				//recalculates velocity based on ground normal.
				//applies the velocity back if player is not climbing.
				Vector3 normalized = Vector3.ProjectOnPlane(rb.velocity, hit.normal).normalized;
				if (!rb.isKinematic && rb.velocity.y > normalized.y)
				{
					//rb.velocity = normalized * rb.velocity.magnitude;
				}

			}
			else
			{	//if not on any ground, unground player.
				if (grounded)
				{
					grounded = false;
					highestPoint = transform.position.y;

                    pc.gTimer = 0.2f;

                }
                lastGroundNormal = groundNormal;
                tempGroundNormal = Vector3.up;
                groundNormal = tempGroundNormal;
            }

            //groundNormal = tempGroundNormal;

        }

		//calculates the time player has been in air.
		if (!grounded)
		{
			stepSinceUngrounded++;
            timeSinceUngrounded += Time.deltaTime;
        }

		//clears temporary values
		groundContactCount = 0;
		tempGroundNormal = Vector3.zero;
	}
		


	private void OnCollisionEnter(Collision c)
	{
		HandleCollision(c);

		if (groundContactCount > 0 && timeSinceUngrounded > 1)
		{
			var angle = Vector3.Angle(pc.vel.normalized, tempGroundNormal) - 90;
            //Debug.Log(angle);
			if (Mathf.Abs(angle) <= 15)
			{
				pc.rating.text = "Perfect";
				//pc.energy += 60 - Mathf.Abs(angle);

			}
			else if (Mathf.Abs(angle) <= 30)
			{
				pc.rating.text = "Great";
				pc.energy += 60 - Mathf.Abs(angle);
			}
			else if (Mathf.Abs(angle) <= 40)
			{
				pc.rating.text = "Good";
				pc.energy += 60 - Mathf.Abs(angle);
			}
			else
            {
                pc.rating.text = "Nope";
            }
			//Debug.Log(tempGroundNormal);
			pc.flipRotaion = 0;
        }

    }

    private void OnCollisionStay(Collision c)
	{
		HandleCollision(c);
	}
    
	private void HandleCollision(Collision c)
	{
		//if player just left ground, or is climbing
		//do not check ground collision
		if (delay > 0f || rb.isKinematic)
		{
			return;
		}
        
		//for each contact point, add to the temporary ground value.
		//adds to ground contact count.
		for (int i = 0; i < c.contactCount; i++)
		{
			contactPoint = c.GetContact(i);
			if (contactPoint.normal.y > minGroundNormal)
			{
				groundCollider = c.collider;
				tempGroundNormal += contactPoint.normal;
				groundContactCount++;
			}
		}
	}

}
