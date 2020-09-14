using UnityEngine;

public abstract class TankBrain : ScriptableObject
{
	public virtual void Initialize(TankThinker tank) { }
	public abstract void Think(TankThinker tank);

	/// <summary>
	/// This is used to later identify which tank has been hit, or is shooting.
	/// <see cref="TankShooting"/> <see cref="ShellExplosion"/>
	/// </summary>
	public Rigidbody TankRigidBody;

}
