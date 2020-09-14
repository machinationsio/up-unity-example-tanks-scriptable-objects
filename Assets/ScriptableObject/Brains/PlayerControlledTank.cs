﻿using UnityEngine;
using System.Collections;

[CreateAssetMenu(menuName = "Brains/Player Controlled")]
public class PlayerControlledTank : TankBrain
{

    public int PlayerNumber;
    private string m_MovementAxisName;
    private string m_TurnAxisName;
    private string m_FireButton;

    public static PlayerControlledTank Instance;
    
    /// <summary>
    /// This is used in TankMovement.cs to verify that this is the Player controlled tank.
    /// </summary>
    public TankMovement PlayerControlledTankMovement;

    public void OnEnable ()
    {
        Instance = this;
        m_MovementAxisName = "Vertical" + PlayerNumber;
        m_TurnAxisName = "Horizontal" + PlayerNumber;
        m_FireButton = "Fire" + PlayerNumber;
    }

    public static TankHealth PlayerControlledTankHealth;

    public override void Think (TankThinker tank)
    {
        var movement = tank.GetComponent<TankMovement>();
        PlayerControlledTankMovement = movement;

        movement.Steer(Input.GetAxis(m_MovementAxisName), Input.GetAxis(m_TurnAxisName));

        var shooting = tank.GetComponent<TankShooting>();

        if (PlayerControlledTankHealth == null)
        {
            PlayerControlledTankHealth = tank.GetComponent<TankHealth>();
        }

        if (Input.GetButton(m_FireButton))
            shooting.BeginChargingShot();
        else
            shooting.FireChargedShot();
    }

}