using System;
using System.Collections.Generic;
using MachinationsUP.Engines.Unity;
using MachinationsUP.Integration.Binder;
using MachinationsUP.Integration.Elements;
using MachinationsUP.Integration.Inventory;
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

    public ElementBase MEnemyLifeIncreaseWeight;

    public int EnemyLifeIncreasedWeight;
    public int ExplosionForceWeight;
    public int ExplosionRadiusWeight;
    public int EnemyCooldownDecreasedWeight;
    public int EnemyDamageIncreasedWeight;
    public int PlayerCooldownIncreasedWeight;
    public int PlayerProjectileSpeedDecreasedWeight;
    public int PlayerSpeedDecreasedWeight;
    public int PlayerLifeDecreasedWeight;
    public int EnemySpeedIncreasedWeight;

    private const string M_ENEMY_LIFE_INCREASED = "EnemyLifeIncreasedWeight";

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
                    EditorElementBase = MEnemyLifeIncreaseWeight,
                    PropertyName = M_ENEMY_LIFE_INCREASED,
                    DiagramElementID = 9135,
                    DefaultElementBase = new ElementBase(10, null)
                },
            }
        };

        for (int i = 0; i < EnemyLifeIncreasedWeight; i++)
            _dropRates.Add(1);
        for (int i = 0; i < ExplosionForceWeight; i++)
            _dropRates.Add(2);
        for (int i = 0; i < ExplosionRadiusWeight; i++)
            _dropRates.Add(3);
        for (int i = 0; i < EnemyCooldownDecreasedWeight; i++)
            _dropRates.Add(4);
        for (int i = 0; i < EnemyDamageIncreasedWeight; i++)
            _dropRates.Add(5);
        for (int i = 0; i < PlayerCooldownIncreasedWeight; i++)
            _dropRates.Add(6);
        for (int i = 0; i < PlayerProjectileSpeedDecreasedWeight; i++)
            _dropRates.Add(7);
        for (int i = 0; i < PlayerSpeedDecreasedWeight; i++)
            _dropRates.Add(8);
        for (int i = 0; i < PlayerLifeDecreasedWeight; i++)
            _dropRates.Add(9);
        for (int i = 0; i < EnemySpeedIncreasedWeight; i++)
            _dropRates.Add(10);

        //Register this SO with the MDL.
        MnDataLayer.EnrollScriptableObject(this, Manifest);
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
        MEnemyLifeIncreaseWeight = binders[M_ENEMY_LIFE_INCREASED].CurrentElement;
    }

    /// <summary>
    /// Called by the <see cref="MnDataLayer"/> when an element has been updated in the Machinations back-end.
    /// </summary>
    /// <param name="diagramMapping">The <see cref="DiagramMapping"/> of the modified element.</param>
    /// <param name="elementBase">The <see cref="ElementBase"/> that was sent from the backend.</param>
    public void MDLUpdateSO (DiagramMapping diagramMapping = null, ElementBase elementBase = null)
    {
    }

    #endregion

}