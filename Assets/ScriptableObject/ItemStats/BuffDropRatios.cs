using System;
using System.Collections.Generic;
using MachinationsUP.Engines.Unity;
using MachinationsUP.Integration.Binder;
using MachinationsUP.Integration.Elements;
using MachinationsUP.Integration.Inventory;
using MachinationsUP.Logger;
using UnityEditor;
using UnityEngine;

public enum Drops
{

    EnemyLifeBuff = 1,
    ExplosionForceBuff = 2,
    ExplosionRadiusBuff = 3,
    EnemyCooldownBuff = 4,
    EnemyDamageBuff = 5,
    PlayerCooldownBuff = 6,
    PlayerProjectileSpeedBuff = 7,
    PlayerSpeedBuff = 8,
    PlayerLifeBuff = 9,
    EnemySpeedBuff = 10

}

[CreateAssetMenu(menuName = "ItemStats/DropRatios")]
public class BuffDropRatios : ScriptableObject, IMnScriptableObject
{

    //Machinations.

    public ElementBase EnemyLifeBuffWeight;
    public ElementBase ExplosionForceBuffWeight;
    public ElementBase ExplosionRadiusBuffWeight;
    public ElementBase EnemyCooldownBuffWeight;
    public ElementBase EnemyDamageBuffWeight;
    public ElementBase PlayerCooldownBuffWeight;
    public ElementBase PlayerProjectileSpeedBuffWeight;
    public ElementBase PlayerSpeedBuffWeight;
    public ElementBase PlayerLifeBuffWeight;
    public ElementBase EnemySpeedBuffWeight;

    private const string M_ENEMY_LIFE_BUFF_WEIGHT = "EnemyLifeBuffWeight";
    private const string M_EXPLOSION_FORCE_BUFF_WEIGHT = "ExplosionForceBuffWeight";
    private const string M_EXPLOSION_RADIUS_BUFF_WEIGHT = "ExplosionRadiusBuffWeight";
    private const string M_ENEMY_COOLDOWN_BUFF_WEIGHT = "EnemyCooldownBuffWeight";
    private const string M_ENEMY_DAMAGE_BUFF_WEIGHT = "EnemyDamageBuffWeight";
    private const string M_PLAYER_COOLDOWN_BUFF_WEIGHT = "PlayerCooldownBuffWeight";
    private const string M_PLAYER_PROJECTILE_SPEED_BUFF_WEIGHT = "PlayerProjectileSpeedBuffWeight";
    private const string M_PLAYER_SPEED_BUFF_WEIGHT = "PlayerSpeedBuffWeight";
    private const string M_PLAYER_LIFE_BUFF_WEIGHT = "PlayerLifeBuffWeight";
    private const string M_ENEMY_SPEED_BUFF_WEIGHT = "EnemySpeedBuffWeight";

    readonly private List<int> _dropRates = new List<int>();

    public event EventHandler OnUpdatedFromMachinations;

    public void OnEnable ()
    {
        //Manifest that defines what the SO uses from Machinations.
        Manifest = new MnObjectManifest
        {
            Name = "Drop Ratios",
            DiagramMappings = new List<DiagramMapping>
            {
                new DiagramMapping
                {
                    EditorElementBase = EnemyLifeBuffWeight,
                    PropertyName = M_ENEMY_LIFE_BUFF_WEIGHT,
                    DiagramElementID = 9135,
                    DefaultElementBase = new ElementBase(200, null)
                },
                new DiagramMapping
                {
                    EditorElementBase = ExplosionForceBuffWeight,
                    PropertyName = M_EXPLOSION_FORCE_BUFF_WEIGHT,
                    DiagramElementID = 9136,
                    DefaultElementBase = new ElementBase(100, null)
                },
                new DiagramMapping
                {
                    EditorElementBase = ExplosionRadiusBuffWeight,
                    PropertyName = M_EXPLOSION_RADIUS_BUFF_WEIGHT,
                    DiagramElementID = 9137,
                    DefaultElementBase = new ElementBase(100, null)
                },
                new DiagramMapping
                {
                    EditorElementBase = EnemyCooldownBuffWeight,
                    PropertyName = M_ENEMY_COOLDOWN_BUFF_WEIGHT,
                    DiagramElementID = 9139,
                    DefaultElementBase = new ElementBase(1, null)
                },
                new DiagramMapping
                {
                    EditorElementBase = EnemyDamageBuffWeight,
                    PropertyName = M_ENEMY_DAMAGE_BUFF_WEIGHT,
                    DiagramElementID = 9138,
                    DefaultElementBase = new ElementBase(1, null)
                },
                new DiagramMapping
                {
                    EditorElementBase = PlayerCooldownBuffWeight,
                    PropertyName = M_PLAYER_COOLDOWN_BUFF_WEIGHT,
                    DiagramElementID = 9127,
                    DefaultElementBase = new ElementBase(1, null)
                },
                new DiagramMapping
                {
                    EditorElementBase = PlayerProjectileSpeedBuffWeight,
                    PropertyName = M_PLAYER_PROJECTILE_SPEED_BUFF_WEIGHT,
                    DiagramElementID = 9049,
                    DefaultElementBase = new ElementBase(1, null)
                },
                new DiagramMapping
                {
                    EditorElementBase = PlayerSpeedBuffWeight,
                    PropertyName = M_PLAYER_SPEED_BUFF_WEIGHT,
                    DiagramElementID = 9051,
                    DefaultElementBase = new ElementBase(1, null)
                },
                new DiagramMapping
                {
                    EditorElementBase = PlayerLifeBuffWeight,
                    PropertyName = M_PLAYER_LIFE_BUFF_WEIGHT,
                    DiagramElementID = 9131,
                    DefaultElementBase = new ElementBase(1, null)
                },
                new DiagramMapping
                {
                    EditorElementBase = EnemySpeedBuffWeight,
                    PropertyName = M_ENEMY_SPEED_BUFF_WEIGHT,
                    DiagramElementID = 9304,
                    DefaultElementBase = new ElementBase(1, null)
                }
            }
        };

        //Register this SO with the MDL.
        MnDataLayer.EnrollScriptableObject(this, Manifest);
        UpdateDropRates();
    }

    public List<int> GetDropRates ()
    {
        return _dropRates;
    }

    public void OnDisable ()
    {
        EditorUtility.SetDirty(this);
    }

    #region IMnScriptableObject

    public MnObjectManifest Manifest { get; private set; }

    public ScriptableObject SO => this;

    /// <summary>
    /// Called when Machinations initialization has been completed.
    /// </summary>
    /// <param name="binders">The Binders for this Object.</param>
    public void MDLInitCompleteSO (Dictionary<string, ElementBinder> binders)
    {
        EnemyLifeBuffWeight = binders[M_ENEMY_LIFE_BUFF_WEIGHT].CurrentElement;
        ExplosionForceBuffWeight = binders[M_EXPLOSION_FORCE_BUFF_WEIGHT].CurrentElement;
        ExplosionRadiusBuffWeight = binders[M_EXPLOSION_RADIUS_BUFF_WEIGHT].CurrentElement;
        EnemyCooldownBuffWeight = binders[M_ENEMY_COOLDOWN_BUFF_WEIGHT].CurrentElement;
        EnemyDamageBuffWeight = binders[M_ENEMY_DAMAGE_BUFF_WEIGHT].CurrentElement;
        PlayerCooldownBuffWeight = binders[M_PLAYER_COOLDOWN_BUFF_WEIGHT].CurrentElement;
        PlayerProjectileSpeedBuffWeight = binders[M_PLAYER_PROJECTILE_SPEED_BUFF_WEIGHT].CurrentElement;
        PlayerSpeedBuffWeight = binders[M_PLAYER_SPEED_BUFF_WEIGHT].CurrentElement;
        PlayerLifeBuffWeight = binders[M_PLAYER_LIFE_BUFF_WEIGHT].CurrentElement;
        EnemySpeedBuffWeight = binders[M_ENEMY_SPEED_BUFF_WEIGHT].CurrentElement;
    }

    /// <summary>
    /// Called by the <see cref="MnDataLayer"/> when an element has been updated in the Machinations back-end.
    /// </summary>
    /// <param name="diagramMapping">The <see cref="DiagramMapping"/> of the modified element.</param>
    /// <param name="elementBase">The <see cref="ElementBase"/> that was sent from the backend.</param>
    public void MDLUpdateSO (DiagramMapping diagramMapping = null, ElementBase elementBase = null)
    {
        UpdateDropRates();
    }

    private void UpdateDropRates ()
    {
        L.D("Drop rates updated!");
        _dropRates.Clear();
        //Re-populate drop rate array.
        for (int i = 0; i < EnemyLifeBuffWeight.CurrentValue; i++)
            _dropRates.Add(1);
        for (int i = 0; i < ExplosionForceBuffWeight.CurrentValue; i++)
            _dropRates.Add(2);
        for (int i = 0; i < ExplosionRadiusBuffWeight.CurrentValue; i++)
            _dropRates.Add(3);
        for (int i = 0; i < EnemyCooldownBuffWeight.CurrentValue; i++)
            _dropRates.Add(4);
        for (int i = 0; i < EnemyDamageBuffWeight.CurrentValue; i++)
            _dropRates.Add(5);
        for (int i = 0; i < PlayerCooldownBuffWeight.CurrentValue; i++)
            _dropRates.Add(6);
        for (int i = 0; i < PlayerProjectileSpeedBuffWeight.CurrentValue; i++)
            _dropRates.Add(7);
        for (int i = 0; i < PlayerSpeedBuffWeight.CurrentValue; i++)
            _dropRates.Add(8);
        for (int i = 0; i < PlayerLifeBuffWeight.CurrentValue; i++)
            _dropRates.Add(9);
        for (int i = 0; i < EnemySpeedBuffWeight.CurrentValue; i++)
            _dropRates.Add(10);
    }

    #endregion

}