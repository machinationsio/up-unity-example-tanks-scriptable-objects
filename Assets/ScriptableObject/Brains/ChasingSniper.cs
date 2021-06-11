using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MachinationsUP.Engines.Unity;
using MachinationsUP.Integration.Binder;
using MachinationsUP.Integration.Elements;
using MachinationsUP.Integration.Inventory;

[CreateAssetMenu(menuName = "Brains/Chasing sniper")]
public class ChasingSniper : TankBrain
{

    public TankStatsEnemy enemyTankStats;
    public float aimAngleThreshold = 2f;
    [MinMaxRange(0, 0.05f)] public RangedFloat chargeTimePerDistance;
    [MinMaxRange(0, 10)] public RangedFloat timeBetweenShots;
    [MinMaxRange(0, 10)] public RangedFloat moveTime;

    public override void Think (TankThinker tank)
    {
        var shooting = tank.GetComponent<TankShooting>();

        var cooldown = shooting.m_ShellStatsEnemy.ShotCooldown.CurrentValue + shooting.m_ShellStatsEnemy.CurrentShotCooldownBuff;
        //Cooldown is set from Shell Stats.
        timeBetweenShots.minValue = cooldown;
        timeBetweenShots.maxValue = cooldown + 1;

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
            // No targets left - drive in a victory circle
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
        // Chasing tank advances slowly towards player.
        else
        {
            //But only close enough to kill.
            var distanceToTarget = Vector3.Distance(target.transform.position, tank.transform.position);
            if (distanceToTarget > 20)
                movement.Steer(0.01f * (enemyTankStats.Speed.CurrentValue + enemyTankStats.CurrentSpeedBuff), 0f);
            else if (distanceToTarget < 15)
                movement.Steer(-0.01f * (enemyTankStats.Speed.CurrentValue + enemyTankStats.CurrentSpeedBuff), 0f);
            //Reached target. Freeze!
            else
                movement.Steer(0, 0);
        }

        // Fire at the target
        if (!shooting.IsCharging)
        {
            if (Time.time > tank.Remember<float>("nextShotAllowedAfter"))
            {
                float distanceToTarget = Vector3.Distance(target.transform.position, tank.transform.position);
                float timeToCharge = distanceToTarget * Random.Range(chargeTimePerDistance.minValue, chargeTimePerDistance.maxValue);
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