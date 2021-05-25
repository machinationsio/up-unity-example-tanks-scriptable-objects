using System;
using MachinationsUP.Logger;
using Tank;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class TankHealth : MonoBehaviour
{

    //public float m_StartingHealth = 100f;               // The amount of health each tank starts with.
    public TankStats m_TankStats;
    public TankStatsEnemy m_TankStatsEnemy;

    public Slider m_Slider; // The slider to represent how much health the tank currently has.
    public Image m_FillImage; // The image component of the slider.
    public Color m_FullHealthColor = Color.green; // The color the health bar will be when on full health.
    public Color m_ZeroHealthColor = Color.red; // The color the health bar will be when on no health.
    public GameObject m_ExplosionPrefab; // A prefab that will be instantiated in Awake, then used whenever the tank dies.

    public GameObject m_DeadTankPrefab;

    //Required for applying buff/debuffs.
    public ShellStats m_ShellStats;

    public ShellStatsEnemy m_ShellStatsEnemy;

    //Buff/debuff icons.
    public GameObject m_EnemyLifeBuffIcon;
    public GameObject m_ExplosionForceBuffIcon;
    public GameObject m_ExplosionRadiusBuffIcon;
    public GameObject m_EnemyCooldownDecreasedBuffIcon;
    public GameObject m_EnemyDamageIncreasedBuffIcon;
    public GameObject m_PlayerCooldownIncreasedDeBuffIcon;
    public GameObject m_PlayerProjectileSpeedDecreasedDeBuffIcon;
    public GameObject m_PlayerSpeedDecreasedDeBuffIcon;

    public GameObject m_PlayerLifeDecreasedDeBuffIcon;

    //Buff/debuffs drop ratios.
    public BuffDropRatios m_DropRatios;

    private Loot m_TheLoot; //Whatever Loot Icon this Tank will spawn, it will be stored here.
    private AudioSource m_ExplosionAudio; // The audio source to play when the tank explodes.
    private ParticleSystem m_ExplosionParticles; // The particle system the will play when the tank is destroyed.
    public float m_CurrentHealth; // How much health the tank currently has.
    private bool m_Dead; // Has the tank been reduced beyond zero health yet?

    private bool gotExtraLife;
    private float timeSinceGotExtraLife;

    private void Awake ()
    {
        // Instantiate the explosion prefab and get a reference to the particle system on it.
        m_ExplosionParticles = Instantiate(m_ExplosionPrefab).GetComponent<ParticleSystem>();

        // Get a reference to the audio source on the instantiated prefab.
        m_ExplosionAudio = m_ExplosionParticles.GetComponent<AudioSource>();

        // Disable the prefab so it can be activated when it's required.
        m_ExplosionParticles.gameObject.SetActive(false);
    }

    private bool _wasDisabledUntilHealthMemberAvailable;

    private void OnEnable ()
    {
        if (PlayerControlledTank.PlayerControlledTankHealth == null)
        {
            L.D("Tank Health: Player Tank Health not yet created. Waiting until setting Healths.");
            _wasDisabledUntilHealthMemberAvailable = true;
        }

        // When the tank is enabled, reset the tank's health and whether or not it's dead.
        if (this == PlayerControlledTank.PlayerControlledTankHealth)
        {
            L.D("Tank Health: " + m_TankStats.Health.CurrentValue);
            m_CurrentHealth = m_TankStats.Health.CurrentValue;
        }
        else
        {
            L.D("Tank Health enemy: " + m_TankStatsEnemy.Health.CurrentValue);
            m_CurrentHealth = m_TankStatsEnemy.Health.CurrentValue;
        }

        m_Dead = false;

        // Update the health slider's value and color.
        SetHealthUI();

        //Reset buffs.
        //Player Tank.
        m_TankStats.CurrentHealthBuff = 0;
        m_TankStats.CurrentSpeedBuff = 0;
        //PLayer Shells.
        m_ShellStats.CurrentShotCooldownBuff = 0;
        m_ShellStats.CurrentExplosionForceBuff = 0;
        m_ShellStats.CurrentExplosionRadiusBuff = 0;
        m_ShellStats.CurrentShellSpeedBuff = 0;
        //Enemy Tanks.
        m_TankStatsEnemy.CurrentHealthBuff = 0;
        //Enemy Shells.
        m_ShellStatsEnemy.CurrentShotCooldownBuff = 0;
        m_ShellStatsEnemy.CurrentExplosionForceBuff = 0;
        m_ShellStatsEnemy.CurrentExplosionRadiusBuff = 0;
        m_ShellStatsEnemy.CurrentDamageBuff = 0;
    }

    public void Update ()
    {
        if (_wasDisabledUntilHealthMemberAvailable && PlayerControlledTank.PlayerControlledTankHealth != null)
        {
            L.D("Tank Health: Player Tank Health was created!");
            _wasDisabledUntilHealthMemberAvailable = false;
            OnEnable();
        }

        if (GameState.Instance.PlayerMustTakeDamage != 0 && this == PlayerControlledTank.PlayerControlledTankHealth)
        {
            Debug.Log("Taking damage");
            TakeDamage(GameState.Instance.PlayerMustTakeDamage);
            GameState.Instance.PlayerMustTakeDamage = 0;
        }
        //Enemy buff handling.
        if (gotExtraLife && Time.time - timeSinceGotExtraLife > 10)
        {
            Debug.Log("Everybody must've gotten the life buff by now. Resetting.");
            GameState.Instance.EnemiesMustGetExtraLife = 0;
            gotExtraLife = false;
        }
        if (gotExtraLife == false && GameState.Instance.EnemiesMustGetExtraLife != 0 && this != PlayerControlledTank.PlayerControlledTankHealth)
        {
            Debug.Log("Getting Extra Life");
            TakeDamage(-GameState.Instance.EnemiesMustGetExtraLife);
            gotExtraLife = true;
            timeSinceGotExtraLife = Time.time;
        }
    }

    public void TakeDamage (float amount)
    {
        // Reduce current health by the amount of damage done.
        m_CurrentHealth -= amount;

        // Change the UI elements appropriately.
        SetHealthUI();

        // If the current health is at or below zero and it has not yet been registered, call OnDeath.
        if (m_CurrentHealth <= 0f && !m_Dead)
        {
            OnDeath();
        }
    }


    private void SetHealthUI ()
    {
        // Set the slider's value appropriately.
        m_Slider.value = m_CurrentHealth;

        // Interpolate the color of the bar between the choosen colours based on the current percentage of the starting health.
        m_FillImage.color = Color.Lerp(m_ZeroHealthColor, m_FullHealthColor,
            m_CurrentHealth / (this == PlayerControlledTank.PlayerControlledTankHealth
                ? m_TankStats.Health.CurrentValue
                : m_TankStatsEnemy.Health.CurrentValue));
    }


    private void OnDeath ()
    {
        // Set the flag so that this function is only called once.
        m_Dead = true;

        // Move the instantiated explosion prefab to the tank's position and turn it on.
        m_ExplosionParticles.transform.position = transform.position;
        m_ExplosionParticles.gameObject.SetActive(true);

        // Play the particle system of the tank exploding.
        m_ExplosionParticles.Play();

        //The Player died! This will lead to the round being ended.
        if (this == PlayerControlledTank.PlayerControlledTankHealth)
        {
            GameState.Instance.PlayerControlledTankIsDead = true;
        }

        //When a tank dies, we're going to offer a... reward... hehehehe.
        var dropRates = m_DropRatios.GetDropRates();
        var rnd = Random.Range(1, dropRates.Count);
        GameObject loot;
        Debug.Log("Buff randomized weight: " + rnd);
        switch (dropRates[rnd])
        {
            case (int) Drops.D1EnemyLifeIncreasedWeight: //D1EnemyLifeIncreased
                Debug.Log("Buffing Enemy Life with " + m_TankStatsEnemy.HealthBuff.CurrentValue);
                Instantiate(m_EnemyLifeBuffIcon, transform.position, Quaternion.identity);
                m_TankStatsEnemy.CurrentHealthBuff +=
                    m_TankStatsEnemy.HealthBuff.CurrentValue; //This is rather useless, since Health buffs are applied instantly.
                //Only modify health of enemy tanks.
                GameState.Instance.EnemiesMustGetExtraLife = m_TankStatsEnemy.HealthBuff.CurrentValue;
                break;
            case (int) Drops.D2ExplosionForceWeight:
                Debug.Log("Buffing Explosion Force (for everybody) with " + m_ShellStatsEnemy.ExplosionForceBuff.CurrentValue);
                Instantiate(m_ExplosionForceBuffIcon, transform.position, Quaternion.identity);
                m_ShellStatsEnemy.CurrentExplosionForceBuff += m_ShellStatsEnemy.ExplosionForceBuff.CurrentValue;
                m_ShellStats.CurrentExplosionForceBuff += m_ShellStats.ExplosionForceBuff.CurrentValue;
                break;
            case (int) Drops.D3ExplosionRadiusWeight:
                Debug.Log("Buffing Explosion Radius (for everybody) with " + m_ShellStatsEnemy.ExplosionRadiusBuff.CurrentValue);
                Instantiate(m_ExplosionRadiusBuffIcon, transform.position, Quaternion.identity);
                m_ShellStatsEnemy.CurrentExplosionRadiusBuff += m_ShellStatsEnemy.ExplosionRadiusBuff.CurrentValue;
                m_ShellStats.CurrentExplosionRadiusBuff += m_ShellStats.ExplosionRadiusBuff.CurrentValue;
                break;
            case (int) Drops.D4EnemyCooldownDecreasedWeight:
                Debug.Log("Buffing Enemy Cooldown with -" + m_ShellStatsEnemy.ShotCooldownBuff.CurrentValue);
                Instantiate(m_EnemyCooldownDecreasedBuffIcon, transform.position, Quaternion.identity);
                m_ShellStatsEnemy.CurrentShotCooldownBuff -= m_ShellStatsEnemy.ShotCooldownBuff.CurrentValue;
                break;
            case (int) Drops.D5EnemyDamageIncreasedWeight:
                Debug.Log("Buffing Enemy Damage with " + m_ShellStatsEnemy.DamageBuff.CurrentValue);
                Instantiate(m_EnemyDamageIncreasedBuffIcon, transform.position, Quaternion.identity);
                m_ShellStatsEnemy.CurrentDamageBuff += m_ShellStatsEnemy.DamageBuff.CurrentValue;
                break;
            case (int) Drops.D6PlayerCooldownIncreasedWeight:
                Debug.Log("DeBuffing Player Cooldown with " + m_ShellStats.ShotCooldownBuff.CurrentValue);
                Instantiate(m_PlayerCooldownIncreasedDeBuffIcon, transform.position, Quaternion.identity);
                m_ShellStats.CurrentShotCooldownBuff += m_ShellStats.ShotCooldownBuff.CurrentValue;
                break;
            case (int) Drops.D7PlayerProjectileSpeedDecreasedWeight:
                Debug.Log("DeBuffing Player Projectile Speed with " + m_ShellStats.ShellSpeedBuff.CurrentValue);
                Instantiate(m_PlayerProjectileSpeedDecreasedDeBuffIcon, transform.position, Quaternion.identity);
                m_ShellStats.CurrentShellSpeedBuff -= m_ShellStats.ShellSpeedBuff.CurrentValue;
                break;
            case (int) Drops.D8PlayerSpeedDecreasedWeight:
                Debug.Log("DeBuffing Player Speed with " + m_TankStats.SpeedBuff.CurrentValue);
                Instantiate(m_PlayerSpeedDecreasedDeBuffIcon, transform.position, Quaternion.identity);
                m_TankStats.CurrentSpeedBuff -= m_TankStats.SpeedBuff.CurrentValue;
                break;
            case (int) Drops.D9PlayerLifeDecreasedWeight:
                Debug.Log("DeBuffing Player Life with " + m_TankStats.HealthBuff.CurrentValue);
                Instantiate(m_PlayerLifeDecreasedDeBuffIcon, transform.position, Quaternion.identity);
                m_TankStats.CurrentHealthBuff -=
                    m_TankStats.HealthBuff.CurrentValue; //This is rather useless, since Health buffs are applied instantly.
                //Only modify health of PLAYER tank.
                GameState.Instance.PlayerMustTakeDamage = m_TankStats.HealthBuff.CurrentValue;
                break;
        }

        // Play the tank explosion sound effect.
        m_ExplosionAudio.Play();

        Instantiate(m_DeadTankPrefab, transform.position, transform.rotation);

        // Turn the tank off.
        gameObject.SetActive(false);
    }

}