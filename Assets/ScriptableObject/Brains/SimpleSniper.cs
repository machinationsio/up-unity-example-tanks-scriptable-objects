using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MachinationsUP.Engines.Unity;
using MachinationsUP.Integration.Binder;
using MachinationsUP.Integration.Elements;
using MachinationsUP.Integration.Inventory;

[CreateAssetMenu(menuName="Brains/Simple sniper")]
public class SimpleSniper : TankBrain, IMachinationsScriptableObject
{

	public float aimAngleThreshold = 2f;
	[MinMaxRange(0, 0.05f)]
	public RangedFloat chargeTimePerDistance;
	[MinMaxRange(0, 10)]
	public RangedFloat timeBetweenShots;

	//Machinations.

	//Tracked Machinations Elements.
	private const string M_EXTRA_ENEMIES = "ExtraEnemies";
	//Binders used to transfer information to this SO.
	private Dictionary<string, ElementBinder> _binders;
	//Manifest that defines what the SO uses from Machinations.
	static readonly private MachinationsGameObjectManifest _manifest = new MachinationsGameObjectManifest
	{
		PropertiesToSync = new List<DiagramMapping>
		{
			new DiagramMapping
			{
				GameObjectPropertyName = M_EXTRA_ENEMIES,
				DiagramElementID = 23,
				DefaultElementBase = new ElementBase(5)
			}
		},
	};

	public void OnEnable ()
	{
		//Register this SO with the MGL.
		MachinationsGameLayer.RegisterScriptableObject(this, _manifest);
	}
	
	#region IMachinationsScriptableObject

	/// <summary>
	/// Called when Machinations initialization has been completed.
	/// </summary>
	public void MGLInitCompleteSO ()
	{
		_binders = MachinationsGameLayer.CreateBindersForManifest(_manifest); //Get our Binders.
		MGLUpdateSO();
	}

	/// <summary>
	/// Called by the <see cref="MachinationsGameLayer"/> when an element has been updated in the Machinations back-end.
	/// </summary>
	/// <param name="diagramMapping">The <see cref="DiagramMapping"/> of the modified element.</param>
	/// <param name="elementBase">The <see cref="ElementBase"/> that was sent from the backend.</param>
	public void MGLUpdateSO (DiagramMapping diagramMapping = null, ElementBase elementBase = null)
	{
		timeBetweenShots.minValue = _binders[M_EXTRA_ENEMIES].Value - 2;
		timeBetweenShots.maxValue = timeBetweenShots.minValue + 1;
	}

	#endregion
	
	public override void Think(TankThinker tank)
	{
		GameObject target = tank.Remember<GameObject>("target");
		var movement = tank.GetComponent<TankMovement>();

		if (!target)
		{
			// Find the nearest tank that isn't me
			target =
				GameObject
					.FindGameObjectsWithTag("Player")
					.OrderBy(go => Vector3.Distance(go.transform.position, tank.transform.position))
					.FirstOrDefault(go => go != tank.gameObject);

			tank.Remember<GameObject>("target");
		}

		if (!target)
		{
			// No targets left - drive in a victory circles
			movement.Steer(0.5f, 1f);
			return;
		}

		// aim at the target
		Vector3 desiredForward = (target.transform.position - tank.transform.position).normalized;
		if (Vector3.Angle(desiredForward, tank.transform.forward) > aimAngleThreshold)
		{
			bool clockwise = Vector3.Cross(desiredForward, tank.transform.forward).y > 0;
			movement.Steer(0f, clockwise ? -1 : 1);
		}
		else
		{
			// Stop
			movement.Steer(0f, 0f);
		}

		// Fire at the target
		var shooting = tank.GetComponent<TankShooting>();
		if (!shooting.IsCharging)
		{
			if (Time.time > tank.Remember<float>("nextShotAllowedAfter"))
			{
				float distanceToTarget = Vector3.Distance(target.transform.position, tank.transform.position);
				float timeToCharge = distanceToTarget*Random.Range(chargeTimePerDistance.minValue, chargeTimePerDistance.maxValue);
				tank.Remember("fireAt", Time.time + timeToCharge);
				shooting.BeginChargingShot();
			}
		}
		else
		{
			float fireAt = tank.Remember<float>("fireAt");
			if (Time.time > fireAt)
			{
				shooting.FireChargedShot();
				tank.Remember("nextShotAllowedAfter", Time.time + Random.Range(timeBetweenShots.minValue, timeBetweenShots.maxValue));
			}
		}
	}
}
