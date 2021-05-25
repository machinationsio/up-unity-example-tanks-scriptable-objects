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

    D1EnemyLifeIncreasedWeight = 1,
    D2ExplosionForceWeight = 2,
    D3ExplosionRadiusWeight = 3,
    D4EnemyCooldownDecreasedWeight = 4,
    D5EnemyDamageIncreasedWeight = 5,
    D6PlayerCooldownIncreasedWeight = 6,
    D7PlayerProjectileSpeedDecreasedWeight = 7,
    D8PlayerSpeedDecreasedWeight = 8,
    D9PlayerLifeDecreasedWeight = 9,

}

[CreateAssetMenu(menuName = "ItemStats/DropRatios")]
public class BuffDropRatios : ScriptableObject, IMnScriptableObject
{

    //Machinations.

    public ElementBase Damage;

    public int D1EnemyLifeIncreasedWeight;
    public int D2ExplosionForceWeight;
    public int D3ExplosionRadiusWeight;
    public int D4EnemyCooldownDecreasedWeight;
    public int D5EnemyDamageIncreasedWeight;
    public int D6PlayerCooldownIncreasedWeight;
    public int D7PlayerProjectileSpeedDecreasedWeight;
    public int D8PlayerSpeedDecreasedWeight;
    public int D9PlayerLifeDecreasedWeight;

    private const string M_DAMAGE = "Damage";

    readonly private List<int> _dropRates = new List<int>();

    public event EventHandler OnUpdatedFromMachinations;

    public void OnEnable ()
    {
        //Manifest that defines what the SO uses from Machinations.
        Manifest = new MnObjectManifest
        {
            Name = "Shell Stats",
            DiagramMappings = new List<DiagramMapping>
            {
                new DiagramMapping
                {
                    EditorElementBase = Damage,
                    PropertyName = M_DAMAGE,
                    DiagramElementID = 219,
                    DefaultElementBase = new ElementBase(10, null)
                },
            }
        };

        for (int i = 0; i < D1EnemyLifeIncreasedWeight; i++)
            _dropRates.Add(1);
        for (int i = 0; i < D2ExplosionForceWeight; i++)
            _dropRates.Add(2);
        for (int i = 0; i < D3ExplosionRadiusWeight; i++)
            _dropRates.Add(3);
        for (int i = 0; i < D4EnemyCooldownDecreasedWeight; i++)
            _dropRates.Add(4);
        for (int i = 0; i < D5EnemyDamageIncreasedWeight; i++)
            _dropRates.Add(5);
        for (int i = 0; i < D6PlayerCooldownIncreasedWeight; i++)
            _dropRates.Add(6);
        for (int i = 0; i < D7PlayerProjectileSpeedDecreasedWeight; i++)
            _dropRates.Add(7);
        for (int i = 0; i < D8PlayerSpeedDecreasedWeight; i++)
            _dropRates.Add(8);
        for (int i = 0; i < D9PlayerLifeDecreasedWeight; i++)
            _dropRates.Add(9);

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
        Damage = binders[M_DAMAGE].CurrentElement;
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