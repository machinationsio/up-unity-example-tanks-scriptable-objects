using UnityEngine;
using System.Collections;
using MachinationsUP.Logger;

[CreateAssetMenu(menuName = "Brains/Player Controlled")]
public class PlayerControlledTank : TankBrain
{

    public int PlayerNumber;
    private string m_MovementAxisName;
    private string m_TurnAxisName;
    private string m_FireButton;

    //TODO: ideally, shouldn't be static, because this will fail when 2 humans play.

    /// <summary>
    /// Used to access this Tank's Rigid Body.
    /// </summary>
    public static PlayerControlledTank Instance;

    /// <summary>
    /// This is used in TankMovement.cs to verify that this is the Player controlled tank.
    /// </summary>
    public static TankMovement PlayerControlledTankMovement;

    /// <summary>
    /// This is used in TankHealth.cs to verify that we're using the Player's Tank Health.
    /// </summary>
    public static TankHealth PlayerControlledTankHealth;

    public override void Initialize (TankThinker tank)
    {
    }

    public void OnEnable ()
    {
        Instance = this;
        m_MovementAxisName = "Vertical" + PlayerNumber;
        m_TurnAxisName = "Horizontal" + PlayerNumber;
        m_FireButton = "Fire" + PlayerNumber;
    }

    public override void Think (TankThinker tank)
    {
        var movement = tank.GetComponent<TankMovement>();
        movement.Steer(Input.GetAxis(m_MovementAxisName), Input.GetAxis(m_TurnAxisName));

        var shooting = tank.GetComponent<TankShooting>();

        if (Input.GetButton(m_FireButton))
            shooting.BeginChargingShot();
        else
            shooting.FireChargedShot();

        //Setup player vs enemy awareness.
        
        if (PlayerControlledTankMovement == null)
            PlayerControlledTankMovement = tank.GetComponent<TankMovement>();

        if (PlayerControlledTankHealth == null)
        {
            PlayerControlledTankHealth = tank.GetComponent<TankHealth>();
        }

        if (TankRigidBody == null || TankRigidBody.GetHashCode() == 0)
        {
            Instance = this;
            //Saving a reference to the Rigid Body of this Tank, so that we can later check against it.
            //Used for damage-related tasks.
            TankRigidBody = tank.GetComponent<Rigidbody>();
            //L.D("TankRigidBody created for PlayerControlledTank with Hash: " + GetHashCode());
        }
    }

}