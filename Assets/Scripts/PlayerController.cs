using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

//Added Function Die(), Damage(),
//And postprocessing effect when taken damage.
public class PlayerController : MonoBehaviour, Damagable//, Slappable
{
	public static PlayerController instance;

	public WeaponManager weapons; 

	public Transform t;

	public Transform tHead;
    public Transform head;

    public Rigidbody rb;

	public CapsuleCollider playerCollider;

	public PlayerDecapitate playerDecapitate;

	public Grounder grounder;

	public PlayerSlide slide;
	public PlayerDash dash;

	public MouseLook mouseLook;

	public CameraBob bob;

	public HeadPosition headPosition;

	public float hTemp;

	public float vTemp;

	public float h;

	public float v;

	public float speed = 1f;

	public Vector3 inputDir;

	public Vector3 vel;

	public Vector3 gVel;

	public Vector3 gDir;

	public Vector3 gDirCross;

	public Vector3 gDirCrossProject;

	public RaycastHit hit;

	public float airControl = 1f;

	public float airControlBlockTimer;

	public Vector3 jumpForce = new Vector3(0f, 15f, 0f);

	public float gTimer;

	public float gravity = -40f;

	public int climbState;

	public float climbTimer;

	public Vector3 climbStartPos;

	public Vector3 climbStartDir;

	public Vector3 climbTargetPos;

	public AnimationCurve climbCurve;

    public AnimationCurve gravityCurve;

    public AnimationCurve ratingCurve;

    public GameObject poofVFX; 

	public GameObject slamVFX; 

	public bool extraUpForce;

	public float damageTimer;   
	public PostProcessVolume volume;
    public Bloom bloom;
    public ChromaticAberration ca;
    public ColorGrading cg;
    public Vignette vg;

	public TMP_Text rating;
    public TMP_Text speedText;
    public TMP_Text distanceText;

    public float ratingTimer;
    public float energy;
    public GameObject energyBar;
    public GameObject consumeBar;
    public Gradient energyBarColor;

    public float energyConsumed;
    public float flipRotaion;

    public float targetFrontalSpeed = 20;
	public float shift = 0;
    public float space = 0;
    public float targetFOV = 90;

	public PlayerAudio audioSettings;

	public Terrain terrain;

	public GameObject destination;

	public bool speedingUp;

	private void Awake()
	{
        Cursor.lockState = CursorLockMode.Locked;
		Cursor.visible = false;
		instance = this;
		t = base.transform;
		tHead = t.Find("Head Pivot").transform;
		rb = GetComponent<Rigidbody>();
		playerCollider = GetComponent<CapsuleCollider>();
		grounder = GetComponent<Grounder>();
		slide = GetComponent<PlayerSlide>();
		dash = GetComponent<PlayerDash>();
		playerDecapitate = GetComponentInChildren<PlayerDecapitate>(true);
		bob = tHead.GetComponentInChildren<CameraBob>();
		headPosition = tHead.GetComponentInChildren<HeadPosition>();
		mouseLook = tHead.GetComponentInChildren<MouseLook>();

		volume = FindObjectOfType<PostProcessVolume>();
		volume.profile.TryGetSettings(out bloom);
		volume.profile.TryGetSettings(out ca);
		volume.profile.TryGetSettings(out cg);
		volume.profile.TryGetSettings(out vg);

		poofVFX = Instantiate(poofVFX, Vector3.zero, Quaternion.identity);
		slamVFX = Instantiate(slamVFX, Vector3.zero, Quaternion.identity);

		energyConsumed = 100;
    }

	//Executes when taken damage from a source.
	public void Damage(Damage damage)
	{
		slamVFX.transform.position = transform.position;
		slamVFX.transform.rotation = Quaternion.LookRotation(transform.forward);
		slamVFX.GetComponent<ParticleSystem>().Play();

		//When blocking, knocks off the current weapon.
		if (weapons.IsBlocking())
		{
			weapons.weapons[weapons.currentWeapon].Block();
			rb.AddForce(damage.dir * 20f, ForceMode.Impulse);
			bob.Sway(new Vector4(-20f, 20f, 0f, 5f));
		}
		//if player hasnt taken damage in 3 seconds, knocks player back and upwwards.
		//and next attck in 3 seconds will kill player.
		else if (damageTimer <= 0f && damage.amount < 100f)
		{
			if (grounder.grounded)
			{
				grounder.Unground();
				airControlBlockTimer = 0.2f;
				rb.velocity = Vector3.zero;
				rb.AddForce((Vector3.up + (Vector3)damage.dir).normalized * 10f, ForceMode.Impulse);
			}
			bob.Sway(new Vector4(5f, 0f, 30f, 3f));
			damageTimer = 3f;
			//QuickEffectsPool.Get("Damage", tHead.position, Quaternion.LookRotation(tHead.forward)).Play();
		}
		//else kill player.
		else
		{
			Die(damage.dir);
			TimeManager.instance.SlowMotion(0.1f, 1f, 0.2f);
		}
	}

	//Turns on Postprocessing. 
	//Enables the camera that simulates the decapitated head.
	//Disables Player.
	public void Die(Vector3 dir)
	{
		bloom.intensity.value = 10;
		ca.intensity.value = 10;
		cg.mixerGreenOutRedIn.value = -75;
		vg.intensity.value = 0.3f;

		playerDecapitate.gameObject.SetActive(true);
		playerDecapitate.Decapitate(tHead, dir);
		base.gameObject.SetActive(false);
	}

	private void JumpOrClimb()
	{
		//if is climbing, return
		if (rb.isKinematic)
		{
			return;
		}

		//if grounded, or just ungrouned, or just finished climbing
		//jump
		if (grounder.grounded
			|| gTimer > 0f 
			|| (climbState == 2 && climbTimer > 0.8f))
		{
			if (climbState == 2)
			{
				rb.isKinematic = false;
				climbState = 0;
			}
			Jump();
			return;
		}
		
		//if not grounded, but there is prop or enemy below
		//super jump
		Collider[] array = new Collider[1];
		Physics.OverlapCapsuleNonAlloc(t.position, t.position + Vector3.down * 1.25f, 1f, array, 25600);
		if (array[0] != null)
		{
			switch (array[0].gameObject.layer)
			{
			case 10:
			if (array[0].attachedRigidbody.isKinematic)
				{
					Damage damage = new Damage();
					damage.amount = 10f;
					damage.dir = t.forward;
					array[0].GetComponent<Damagable>().Damage(damage);
				}
				else
				{
					array[0].GetComponent<Slappable>().Slap(Vector3.down);
				}
				Jump(1.6f);
				bob.Sway(new Vector4(Mathf.Clamp(vel.magnitude, 5f, 10f), 0f, 0f, 4f));
				
				slamVFX.transform.position = t.position;
				slamVFX.transform.rotation = Quaternion.LookRotation(Vector3.up);
				slamVFX.GetComponent<ParticleSystem>().Play();

				break;
			case 13:
				weapons.Drop(Vector3.up + tHead.forward);
				array[0].GetComponent<Weapon>().Interact(weapons);
				//QuickEffectsPool.Get("Enemy Jump", t.position, Quaternion.LookRotation(Vector3.up)).Play();
				//CameraController.shake.Shake(2);
				bob.Sway(new Vector4(Mathf.Clamp(vel.magnitude, 5f, 10f), 0f, 0f, 4f));
				Jump((rb.velocity.y > 1f) ? 1.75f : 1.5f);
				//midairActionPossible = true;
				break;
			case 14:
				if (!array[0].attachedRigidbody.isKinematic)
				{
					array[0].GetComponent<Slappable>().Slap(tHead.forward);
				}
				Jump((rb.velocity.y > 1f) ? 1.75f : 1.5f);
				bob.Sway(new Vector4(Mathf.Clamp(vel.magnitude, 5f, 10f), 0f, 0f, 4f));
				
				slamVFX.transform.position = t.position;
				slamVFX.transform.rotation = Quaternion.LookRotation(Vector3.up);
				slamVFX.GetComponent<ParticleSystem>().Play(); 	
				
				break;
			}
			array[0] = null;
		} 

		//if none, check if player can climb
		else
		{
			Climb();
		}
	}

	public void Jump(float multiplier = 1f)
	{
		//if jumping on top of props, push props away
		if ((bool)grounder.groundCollider && grounder.groundCollider.gameObject.layer == 14)
		{
			Rigidbody attachedRigidbody = grounder.groundCollider.attachedRigidbody;
			if ((bool)attachedRigidbody)
			{
				attachedRigidbody.AddForce(Vector3.up * (7f * attachedRigidbody.mass), ForceMode.Impulse);
				attachedRigidbody.AddTorque(tHead.forward * 90f, ForceMode.Impulse);
			}
		}

		//ungrounds and jumps
		grounder.Unground();
		gTimer = 0f;
        //rb.velocity = new Vector3(0, 0, 0);
        rb.velocity = new Vector3(rb.velocity.x, 0, rb.velocity.z);
        rb.AddForce(jumpForce * multiplier, ForceMode.Impulse);
	}

	private void Climb()
	{	//if climbing, or no surface to climb up to, or surface too low, or obsticle on top of landing spot, too close to ground
		//no climbing
		if (climbState > 0
			|| !Physics.Raycast(t.position + Vector3.up * 1.5f + tHead.forward * 2f, Vector3.down, out hit, 4f, 1)
			|| !(hit.point.y + 1f > t.position.y)
			|| Physics.Raycast(new Vector3(t.position.x, hit.point.y + 1f, t.position.z), tHead.forward.normalized, 2f, 1) 
			|| Physics.Raycast(t.position, Vector3.down, 1.5f, 1)
			|| Physics.Raycast(t.position, Vector3.up, 2.5f, 1))
		{
			return;
		}
		
		//else sets target position and start climbing
		climbTargetPos = hit.point + hit.normal;
		climbState = 3;
	}

	private void ClimbingUpdate()
	{
		switch (climbState)
		{
			//sets player rb to kinematic to directly modify position
			case 3:
				rb.isKinematic = true;
				rb.velocity = Vector3.zero;
				climbTimer = 0f;
				climbStartPos = rb.position;
				climbStartDir = climbStartPos;
				climbStartDir.y += 2f;
				bob.Sway(new Vector4(10f, 0f, -5f, 2f));
				
				poofVFX.transform.position = climbTargetPos;
				ParticleSystem particle = poofVFX.GetComponent<ParticleSystem>();
				particle.Play();

				climbState--;
				break;

			//lerps from start position to target position based on curve value at current time
			//finishes climbing when timer ends
			case 2:
				bob.Angle(Mathf.Sin(climbTimer * (float)Mathf.PI * 5f));
				climbTimer = Mathf.MoveTowards(climbTimer, 1f, Time.deltaTime * 3f);
				t.position = Vector3.LerpUnclamped(climbStartPos, climbTargetPos, climbCurve.Evaluate(climbTimer));
				if (climbTimer == 1f)
				{
					climbState--;
				}
				break;
			
			//sets player rb back to not kinematic
			case 1:
				rb.isKinematic = false;
				climbState--;
				break;
		}
	}


	private void InputUpdate()
	{

		vTemp = 0f;
		vTemp += (Input.GetKey(KeyCode.W) ? 1 : 0);
		vTemp += (Input.GetKey(KeyCode.S) ? (-1) : 0);
		hTemp = 0f;
		hTemp += (Input.GetKey(KeyCode.A) ? (-1) : 0);
		hTemp += (Input.GetKey(KeyCode.D) ? 1 : 0);
		v = vTemp;
		h = hTemp;

        shift = (Input.GetKey(KeyCode.LeftShift) ? 1 : 0);
        space = (Input.GetKey(KeyCode.Space) ? 1 : 0);

        inputDir.x = h;
		inputDir.y = 0f;
		inputDir.z = v;
		inputDir = inputDir.normalized;

		if (Input.GetKeyDown(KeyCode.Space))
		{
			JumpOrClimb();
		}

		if (Input.GetKeyDown(KeyCode.LeftShift))
		{
			slide.Slide();
		}
		if (Input.GetKeyDown(KeyCode.E))
		{
			dash.Dash();
        }

        if (Input.GetKey(KeyCode.Q))
        {
			Die(rb.velocity.normalized);
        }

    }

	private void BobUpdate()
	{

		//tilts camera based on horizontal input
		if (slide.slideState == 0 && !rb.isKinematic)
		{
			
			bob.Angle(h * -4f - damageTimer * 3f);
		}

		//applies camera bob when grounded, walking, and not sliding
		//or sets camera position back to 0
		if (grounder.grounded && inputDir.sqrMagnitude > 0.25f && slide.slideState == 0)
		{
			if (gVel.sqrMagnitude > 1f)
			{
				bob.Bob(speed);
			}
			else
			{
				bob.Reset();
			}
		}
		else
		{
			bob.Reset();
		}

	}
    
	private void Update()
	{
		InputUpdate();

		BobUpdate();

		headPosition.PositionUpdate();

		if (climbState > 0)
		{
			ClimbingUpdate();
		}
		
		if (slide.slideState > 0)
		{
			slide.SlidingUpdate();
		}

		if (dash.state > 0)
		{
			dash.DashingUpdate();
		}

		//counts down the timer that restricts air control 
		if (airControlBlockTimer > 0f)
		{
			airControlBlockTimer -= Time.deltaTime;
			airControl = 0f;
		}

		//sets air control back to 1 over time
		else if (airControl != 1f)
		{
			airControl = Mathf.MoveTowards(airControl, 1f, Time.deltaTime);
		}

		if (gTimer > 0f)
		{
			gTimer -= Time.deltaTime;
		}

		if (damageTimer != 0f)
		{
			damageTimer = Mathf.MoveTowards(damageTimer, 0f, Time.deltaTime);
			bloom.intensity.value = Mathf.Lerp(0, 10, damageTimer/3);
			ca.intensity.value = Mathf.Lerp(0, 1, damageTimer/3);
			cg.mixerGreenOutRedIn.value = Mathf.Lerp(0, -100, damageTimer/3);
			vg.intensity.value = Mathf.Lerp(0, 0.3f, damageTimer/3);
		}

		audioSettings.Rvalue = transform.rotation.z * 90f*1.5f;
		//Debug.Log(targetFrontalSpeed);
		if (grounder.grounded)
		{
            if (audioSettings.Hvalue < targetFrontalSpeed / 200)
            {
				audioSettings.Hvalue += Time.deltaTime;

			}
			//audioSettings.Hvalue = targetFrontalSpeed / 200;
		}
		else
		{
            //audioSettings.PlayerSlide.Target.Stop();
			audioSettings.Hvalue = 0;
		}

		if (grounder.timeSinceUngrounded < 2)
		{
			audioSettings.Heightvalue = Mathf.Clamp(grounder.timeSinceUngrounded - 1, 0, 1);
		}
		else
		{
			if(Physics.Raycast(t.position, Vector3.down, out hit))
			{
				if ((transform.position.y - hit.point.y) < 10)
                {
                    audioSettings.Heightvalue = Mathf.Lerp(0, 1, (transform.position.y - hit.point.y)/10);
                }
			}
			
        }

		speedText.text = Mathf.Round(targetFrontalSpeed * 50/20)	 + "km/h";
        distanceText.text = Mathf.Round(destination.transform.position.x - transform.position.x) + "m";
    }

	private void FixedUpdate()
	{
		//recalculates the previous velocity based on new ground normals
		vel = rb.velocity;
		gVel = Vector3.ProjectOnPlane(vel, grounder.groundNormal);

        //recalculates direction based on new ground normals
        gDir = tHead.TransformDirection(inputDir);
		gDirCross = Vector3.Cross(Vector3.up, gDir).normalized; 
        gDirCrossProject = Vector3.ProjectOnPlane(grounder.groundNormal, gDirCross);
		gDir = Vector3.Cross(gDirCross, gDirCrossProject);


        Vector3 horizontalDif = tHead.InverseTransformDirection(- rb.velocity);
		//Debug.Log(horizontalDif.y);

        if (slide.slideState == 0)
		{
			//if moving fast, apply the calculated movement.
			//based on new input subtracted by previous velocity
			//so that player accelerates faster when start moving.
			if (inputDir.sqrMagnitude > 0.25f)
			{
				if (grounder.grounded)
				{
					rb.AddForce(gDir * 100f - gVel * 10f * speed);
				}
				else if (airControl > 0f)
				{
					rb.AddForce((gDir * 100f - gVel * 10f * speed) * airControl);
				}
			}
			//if not fast, accelerates the slowing down process
			else if (grounder.grounded && gVel.sqrMagnitude != 0f)
			{
				rb.AddForce(-gVel * 10f);
			}
		}
		else if (slide.slideState == 2)
		{
			//if sliding, modifies the direction according to horizontal inputs
			if (Mathf.Abs(h) > 0.1f)
			{
                
				if (Mathf.Abs(horizontalDif.x) < targetFrontalSpeed * 1.5f && grounder.timeSinceUngrounded > 0.25f)
				{
					rb.AddForce(Vector3.Cross(transform.forward, grounder.groundNormal) * (targetFrontalSpeed * 1.5f * (- h) - horizontalDif.x) * 2);
                }
                else if (Mathf.Abs(horizontalDif.x) < targetFrontalSpeed * 1.5f)
                {
                    rb.AddForce(Vector3.Cross(transform.forward, grounder.groundNormal) * (targetFrontalSpeed * 1.5f * (- h) - horizontalDif.x) * 2);
                }
            }
			else
			{
				if (Mathf.Abs(horizontalDif.x) > 0)
                {
                    rb.AddForce(Vector3.Cross(transform.forward, grounder.groundNormal) * (-50f * horizontalDif.x));
                }
            }



			if(shift == 1 && grounder.timeSinceUngrounded < 0.5f)
            {
                Time.timeScale = Mathf.Lerp(Time.timeScale, 0.1f, Time.deltaTime*10);

				//targetFrontalSpeed = 30;

                targetFOV = 90;
				if(energy > 1)
                {
					energy -= 1;
					energyConsumed += 1;
                }
                if (energy < 0)
                {
                    energy = 0;
                }
            }
			else
            {
                TimeManager.instance.StopSlowmo();

                if (energyConsumed > 0)
                {
                    if (grounder.grounded)
                    {
                        energyConsumed -= 0.25f;
                    }
                    else if (!grounder.grounded)
                    {
                        energyConsumed -= 0.125f;
                    }
                }
				targetFrontalSpeed = 30 + Mathf.Clamp(energyConsumed * 0.3f, 0, 70);
				targetFOV = 75 + targetFrontalSpeed / 5f + Mathf.Lerp(0, 30, grounder.timeSinceUngrounded/1.0f); 
			}
			if(grounder.timeSinceUngrounded < 0.25f)
            {
                if (energy < 100)
                {
                    energy += 0.1f;
                }
				else
				{
					energy = 100;
				}
            }



            if (Mathf.Abs(horizontalDif.z) < targetFrontalSpeed)// && grounder.timeSinceUngrounded < 0.25f)
            {
                rb.AddForce(transform.forward * ((targetFrontalSpeed - horizontalDif.z)*5 + energyConsumed * 5));
            }
			else if (Mathf.Abs(horizontalDif.z) > targetFrontalSpeed )
            {
                rb.AddForce(-transform.forward * (targetFrontalSpeed - horizontalDif.z) * 5);
            }



            //slows down if player holds back
            if (v < -0.5f)
			{
				//rb.AddForce(-vel.normalized * 20f);
			}
		}

		//applies gravity in the direction of ground normal
		//so player does not slide off within the tolerable angle
		//rb.AddForce(grounder.groundNormal * gravity * (grounder.timeSinceUngrounded));
		if (grounder.timeSinceUngrounded < 0.25f || grounder.timeSinceUngrounded > 0.5f)
        {
            /////////////////////////////////
            //EDIT HERE FOR FALL FORCE!!!!///
            /////////////////////////////////
            rb.AddForce(grounder.groundNormal * -Mathf.Lerp(150, 75, (targetFrontalSpeed - 20) / 100) * gravityCurve.Evaluate(grounder.timeSinceUngrounded)); //* gravityCurve.Evaluate(grounder.timeSinceUngrounded)

            /*if (Mathf.Abs(horizontalDif.y) < targetFrontalSpeed)
            {
            }
            else if (Mathf.Abs(horizontalDif.y) > targetFrontalSpeed)
            {
            }*/
        }
        else if(!grounder.grounded)
        {
            /////////////////////////////////
            //EDIT HERE FOR JUMP FORCE!!!!///
            /////////////////////////////////
            rb.AddForce(Vector3.up * 10 * Mathf.Lerp(0, 1, (targetFrontalSpeed - 20) / 100) * Mathf.Pow(grounder.lastGroundNormal.z, 2) * (40 - Mathf.Abs(horizontalDif.x)) / 40);
            rb.AddForce(rb.velocity.normalized * gravityCurve.Evaluate(grounder.timeSinceUngrounded));
        }

        if (extraUpForce)
		{
			rb.AddForce(Vector3.up * 12f);
			extraUpForce = false;
		}


        /////////////////////////////////
        //EDIT HERE FOR CAMERA ROTATION///
        /////////////////////////////////
        Vector3 axis = Vector3.Cross(Vector3.up, rb.velocity.normalized);
        Vector3 velocityNormal = Vector3.Cross(rb.velocity.normalized, axis);
        if (grounder.timeSinceUngrounded < 0.5f)
		{
			velocityNormal = grounder.groundNormal;

            Quaternion _rotationDifference = Quaternion.FromToRotation(transform.up, velocityNormal);
            Vector3 _newForwardDirection = _rotationDifference * transform.forward;
            Quaternion _newRotation = Quaternion.LookRotation(_newForwardDirection, velocityNormal);
            Quaternion _smoothRotation = Quaternion.Lerp(rb.rotation, _newRotation, Time.deltaTime * 1.5f);

            rb.MoveRotation(_smoothRotation);
        }
		else
		{
            velocityNormal = Vector3.Cross(rb.velocity.normalized, axis);

            Quaternion _rotationDifference = Quaternion.FromToRotation(transform.up, velocityNormal);
            Vector3 _newForwardDirection = _rotationDifference * transform.forward;
            Quaternion _newRotation = Quaternion.LookRotation(_newForwardDirection);
            transform.rotation = Quaternion.Lerp(transform.rotation, _newRotation, Time.deltaTime * 2.5f);
        }

        if (!grounder.grounded)
        {
            transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(transform.rotation.x, 90, 0), Time.deltaTime * 2f);
            //transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(transform.rotation.x, 90, transform.rotation.z), Time.deltaTime * 2f);
        }

        if (space > 0.1f && !grounder.grounded && grounder.timeSinceUngrounded > 0.25f)
        {
            head.Rotate(-space * 5, 0, 0);
            flipRotaion += -space * 5;

            if (Mathf.Abs(flipRotaion) / 270 >= 1)
            {
                rating.text = "FLIP!! * " + Mathf.Floor(Mathf.Abs(flipRotaion) / 270);

            }
        }
        else
        {
            head.localRotation = Quaternion.Lerp(head.localRotation, Quaternion.identity, Time.deltaTime * 5);
        }



        var name = Shader.PropertyToID("_VelocityNormal");
		terrain.materialTemplate.SetVector(Shader.PropertyToID("_VelocityNormal"), Vector3.Cross(rb.velocity.normalized, axis));



        Camera.main.fieldOfView = Mathf.Lerp(Camera.main.fieldOfView, targetFOV, Time.deltaTime * 5);

        if (ratingTimer > 0)
        {
            ratingTimer -= 0.01f;
        }
		rating.color = Color.Lerp(new Vector4(1,1,1,0), new Vector4(1, 0, 0, 1), ratingTimer);
		rating.fontSize = ratingCurve.Evaluate(ratingTimer);
        consumeBar.GetComponent<RectTransform>().sizeDelta = new Vector2(energy, 100);
        consumeBar.GetComponent<Image>().color = energyBarColor.Evaluate(energy/100);

		if(shift != 1 || grounder.timeSinceUngrounded >= 0.5f)
		{
            energyBar.GetComponent<RectTransform>().sizeDelta = consumeBar.GetComponent<RectTransform>().sizeDelta;

        }

}


	private void OnCollisionEnter(Collision collision)
	{
		if (collision.gameObject.layer == 17)
		{
			energyConsumed += 10;
			speedingUp = true;
		}
		if (collision.gameObject.layer == 16 && !speedingUp)
		{
			Die(collision.gameObject.transform.position - transform.position);
            TimeManager.instance.SlowMotion(0.1f, 1f, 0.2f);
        }
	}
	private void OnCollisionStay(Collision collision)
	{
		if (collision.gameObject.layer == 17)
		{
			energyConsumed += 0.25f;
			speedingUp = true;
		}
	}
	private void OnCollisionExit(Collision collision)
	{
		if (collision.gameObject.layer == 17)
		{
			speedingUp = false;
		}
	}

}
