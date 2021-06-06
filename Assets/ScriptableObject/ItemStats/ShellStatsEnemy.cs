﻿using System;
using System.Collections.Generic;
using MachinationsUP.Engines.Unity;
using MachinationsUP.Integration.Binder;
using MachinationsUP.Integration.Elements;
using MachinationsUP.Integration.Inventory;
using UnityEditor;
using UnityEngine;

[CreateAssetMenu(menuName = "ItemStats/ShellEnemy")]
public class ShellStatsEnemy : ScriptableObject, IMnScriptableObject
{

    //Machinations.

    public ElementBase Damage;
    public ElementBase Radius;
    public ElementBase Force;
    public ElementBase Speed;
    public ElementBase ShotCooldown;
    public ElementBase ShotCooldownBuff;
    public ElementBase ExplosionForceBuff;
    public ElementBase ExplosionRadiusBuff;
    public ElementBase DamageBuff;

    public float CurrentShotCooldownBuff;
    public float CurrentExplosionForceBuff;
    public float CurrentExplosionRadiusBuff;
    public float CurrentDamageBuff;

    private const string M_DAMAGE = "Damage";
    private const string M_RADIUS = "Radius";
    private const string M_FORCE = "Force";
    private const string M_SPEED = "Speed";
    private const string M_COOLDOWN = "Cooldown";
    private const string M_COOLDOWN_BUFF = "CooldownBuff";
    private const string M_EXPLOSION_FORCE_BUFF = "ExplosionForceBuff";
    private const string M_EXPLOSION_RADIUS_BUFF = "ExplosionRadiusBuff";
    private const string M_DAMAGE_BUFF = "DamageBuff";

    public event EventHandler OnUpdatedFromMachinations;

    public void OnEnable ()
    {
        //Manifest that defines what the SO uses from Machinations.
        Manifest = new MnObjectManifest
        {
            Name = "Shell Stats Enemy",
            DiagramMappings = new List<DiagramMapping>
            {
                new DiagramMapping
                {
                    EditorElementBase = Damage,
                    PropertyName = M_DAMAGE,
                    DiagramElementID = 8916,
                    DefaultElementBase = new ElementBase(10, null)
                },
                new DiagramMapping
                {
                    EditorElementBase = Radius,
                    PropertyName = M_RADIUS,
                    DiagramElementID = 8914,
                    DefaultElementBase = new ElementBase(25, null)
                },
                new DiagramMapping
                {
                    EditorElementBase = Force,
                    PropertyName = M_FORCE,
                    DiagramElementID = 8913,
                    DefaultElementBase = new ElementBase(90, null)
                },
                new DiagramMapping
                {
                    EditorElementBase = Speed,
                    PropertyName = M_SPEED,
                    DiagramElementID = 8915,
                    DefaultElementBase = new ElementBase(90, null)
                },
                new DiagramMapping
                {
                    EditorElementBase = ShotCooldown,
                    PropertyName = M_COOLDOWN,
                    DiagramElementID = 8918,
                    DefaultElementBase = new ElementBase(0, null)
                },
                //BUFFS.
                new DiagramMapping
                {
                    EditorElementBase = ShotCooldownBuff,
                    PropertyName = M_COOLDOWN_BUFF,
                    DiagramElementID = 9126,
                    DefaultElementBase = new ElementBase(1, null)
                },
                new DiagramMapping
                {
                    EditorElementBase = ExplosionForceBuff,
                    PropertyName = M_EXPLOSION_FORCE_BUFF,
                    DiagramElementID = 9237,
                    CrossManifestName = "ExplosionForce",
                    DefaultElementBase = new ElementBase(0, null)
                },
                new DiagramMapping
                {
                    EditorElementBase = ExplosionRadiusBuff,
                    PropertyName = M_EXPLOSION_RADIUS_BUFF,
                    DiagramElementID = 9238,
                    CrossManifestName = "ExplosionRadius",
                    DefaultElementBase = new ElementBase(0, null)
                },
                new DiagramMapping
                {
                    EditorElementBase = DamageBuff,
                    PropertyName = M_DAMAGE_BUFF,
                    DiagramElementID = 9125,
                    DefaultElementBase = new ElementBase(0, null)
                }
            }
        };

        //Register this SO with the MDL.
        MnDataLayer.EnrollScriptableObject(this, Manifest);
    }

    public void OnDisable ()
    {
        EditorUtility.SetDirty(this);
    }

    #region IMachinationsScriptableObject

    public ScriptableObject SO => this;

    public MnObjectManifest Manifest { get; private set; }

    /// <summary>
    /// Called when Machinations initialization has been completed.
    /// </summary>
    /// <param name="binders">The Binders for this Object.</param>
    public void MDLInitCompleteSO (Dictionary<string, ElementBinder> binders)
    {
        Damage = binders[M_DAMAGE].CurrentElement;
        Radius = binders[M_RADIUS].CurrentElement;
        Force = binders[M_FORCE].CurrentElement;
        Speed = binders[M_SPEED].CurrentElement;
        ShotCooldown = binders[M_COOLDOWN].CurrentElement;
        ShotCooldownBuff = binders[M_COOLDOWN_BUFF].CurrentElement;
        ExplosionForceBuff = binders[M_EXPLOSION_FORCE_BUFF].CurrentElement;
        ExplosionRadiusBuff = binders[M_EXPLOSION_RADIUS_BUFF].CurrentElement;
        DamageBuff = binders[M_DAMAGE_BUFF].CurrentElement;
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