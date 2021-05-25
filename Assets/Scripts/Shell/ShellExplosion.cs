using System;
using UnityEngine;

public class ShellExplosion : MonoBehaviour
{

    public LayerMask m_TankMask; // Used to filter what the explosion affects, this should be set to "Players".
    public ParticleSystem m_ExplosionParticles; // Reference to the particles that will play on explosion.
    public AudioSource m_ExplosionAudio; // Reference to the audio that will play on explosion.
    public AudioEvent m_ExplosionAudioEvent;
    public ShellStats m_ShellStats;
    public ShellStatsEnemy m_ShellStatsEnemy;

    //public float m_MaxDamage = 100f;                    // The amount of damage done if the explosion is centred on a tank.
    //public float m_ExplosionForce = 1000f;              // The amount of force added to a tank at the centre of the explosion.
    public float m_MaxLifeTime = 20f; // The time in seconds before the shell is removed.
    //public float m_ExplosionRadius = 5f;                // The maximum distance away from the explosion tanks can be and are still affected.


    private void Start ()
    {
        // If it isn't destroyed by then, destroy the shell after it's lifetime.
        Destroy(gameObject, 20f);
    }

    private void OnCollisionEnter (Collision collision)
    {
        Debug.Log("Player Projectile Entering Colission");
        float radius = m_ShellStats.Radius.CurrentValue;
        // Collect all the colliders in a sphere from the shell's current position to a radius of the explosion radius.
        Collider[] colliders = Physics.OverlapSphere(transform.position, radius);

        // Go through all the colliders...
        for (int i = 0; i < colliders.Length; i++)
        {
            // ... and find their rigidbody.
            Rigidbody targetRigidbody = colliders[i].GetComponent<Rigidbody>();

            float projectileRadius = m_ShellStats.Radius.CurrentValue + m_ShellStats.CurrentExplosionRadiusBuff;
            float projectileForce = m_ShellStats.Force.CurrentValue + m_ShellStats.CurrentExplosionForceBuff;
            float projectileDamage = m_ShellStats.Damage.CurrentValue;

            if (targetRigidbody)
            {
                // Add an explosion force.
                //targetRigidbody.AddExplosionForce(m_ExplosionForce, transform.position, m_ExplosionRadius);
                targetRigidbody.AddExplosionForce(projectileForce, transform.position, projectileRadius);
            }

            // Calculate the amount of damage the target should take based on it's distance from the shell.
            float damage = CalculateDamage(projectileRadius, projectileDamage, colliders[i].transform.position);

            colliders[i].SendMessage("TakeDamage", damage, SendMessageOptions.DontRequireReceiver);
        }

        // Unparent the particles from the shell.
        m_ExplosionParticles.transform.parent = null;

        // Play the particle system.
        m_ExplosionParticles.Play();

        // Play the explosion sound effect.
        m_ExplosionAudioEvent.Play(m_ExplosionAudio);

        // Once the particles have finished, destroy the gameobject they are on.
        Destroy(m_ExplosionParticles.gameObject, m_ExplosionParticles.duration+1);

        GetComponent<Renderer>().enabled = false;
        GetComponent<Collider>().enabled = false;
        GetComponent<Light>().enabled = false;
        GetComponent<Rigidbody>().isKinematic = true;

        Debug.Log("Destroying player shell");
        //Destroy the shell.
        Destroy(gameObject, m_ExplosionAudio.clip.length / m_ExplosionAudio.pitch);
    }


    private float CalculateDamage (float projectileRadius, float projectileDamage, Vector3 targetPosition)
    {
        // Create a vector from the shell to the target.
        Vector3 explosionToTarget = targetPosition - transform.position;

        // Calculate the distance from the shell to the target.
        float explosionDistance = explosionToTarget.magnitude;

        // Calculate the proportion of the maximum distance (the explosionRadius) the target is away.
        //float relativeDistance = (m_ExplosionRadius - explosionDistance) / m_ExplosionRadius;
        float relativeDistance = (projectileRadius - explosionDistance) / projectileRadius;

        // Calculate damage as this proportion of the maximum possible damage.
        //float damage = relativeDistance * m_MaxDamage;
        float damage = relativeDistance * projectileDamage;

        // Make sure that the minimum damage is always 0.
        damage = Mathf.Max(0f, damage);

        return damage;
    }

}