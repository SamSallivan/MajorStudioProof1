using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//weapon that is floating in level, waiting to be picked up
public class FloatingWeapon : Weapon
{
	public Transform tMesh;

	protected override void Awake()
	{
		base.Awake();
		tMesh = base.t.GetChild(0).transform;
	}

	private void Update()
	{
		if ((bool)tMesh)
		{
			tMesh.localPosition = new Vector3(0f, Mathf.Sin(Time.timeSinceLevelLoad) * 0.025f, 0f);
			tMesh.Rotate(Vector3.up * (360f * Time.deltaTime));
		}
	}

	public override void Slap(Vector3 dir)
	{
		//Cannot be slapped
	}

	public override void Interact(WeaponManager manager)
	{
		base.Interact(manager);
	}
}
