﻿using System;
using System.Collections.Generic;
using MachinationsUP.Engines.Unity;
using MachinationsUP.Integration.Binder;
using MachinationsUP.Integration.Elements;
using MachinationsUP.Integration.Inventory;
using UnityEditor;
using UnityEngine;

[CreateAssetMenu(menuName = "ItemStats/ShellEnemy")]
public class ShellStatsEnemy : ScriptableObject, IMachinationsScriptableObject
{

    //Machinations.
    
    public ElementBase Damage;
    public ElementBase Radius;
    public ElementBase Force;
    public ElementBase Speed;
    public ElementBase ShotCooldown;

    private const string M_DAMAGE = "Damage";
    private const string M_RADIUS = "Radius";
    private const string M_FORCE = "Force";
    private const string M_SPEED = "Speed";
    private const string M_COOLDOWN = "Cooldown";
    
    public void OnEnable ()
    {
        //Manifest that defines what the SO uses from Machinations.
        Manifest = new MachinationsObjectManifest
        {
            Name = "Shell Stats Enemy",
            DiagramMappings = new List<DiagramMapping>
            {
                new DiagramMapping
                {
                    GameElementBase = Damage,
                    PropertyName = M_DAMAGE,
                    DiagramElementID = 901,
                    DefaultElementBase = new ElementBase(10, null)
                },
                new DiagramMapping
                {
                    GameElementBase = Radius,
                    PropertyName = M_RADIUS,
                    DiagramElementID = 225,
                    DefaultElementBase = new ElementBase(25, null)
                },
                new DiagramMapping
                {
                    GameElementBase = Force,
                    PropertyName = M_FORCE,
                    DiagramElementID = 107,
                    DefaultElementBase = new ElementBase(90, null)
                },
                new DiagramMapping
                {
                    GameElementBase = Speed,
                    PropertyName = M_SPEED,
                    DiagramElementID = 1000,
                    DefaultElementBase = new ElementBase(90, null)
                },
                new DiagramMapping
                {
                    GameElementBase = ShotCooldown,
                    PropertyName = M_COOLDOWN,
                    DiagramElementID = 245,
                    DefaultElementBase = new ElementBase(0, null)
                }
            }
        };

        //Register this SO with the MGL.
        MachinationsDataLayer.EnrollScriptableObject(this, Manifest);
    }
    
    public void OnDisable ()
    {
        EditorUtility.SetDirty(this);
    }

    #region IMachinationsScriptableObject
    
    public ScriptableObject SO => this;
    
    public MachinationsObjectManifest Manifest { get; private set; }

    /// <summary>
    /// Called when Machinations initialization has been completed.
    /// </summary>
    /// <param name="binders">The Binders for this Object.</param>
    public void MGLInitCompleteSO (Dictionary<string, ElementBinder> binders)
    {
        Damage = binders[M_DAMAGE].CurrentElement;
        Radius = binders[M_RADIUS].CurrentElement;
        Force = binders[M_FORCE].CurrentElement;
        Speed = binders[M_SPEED].CurrentElement;
        ShotCooldown = binders[M_COOLDOWN].CurrentElement;
    }

    /// <summary>
    /// Called by the <see cref="MachinationsDataLayer"/> when an element has been updated in the Machinations back-end.
    /// </summary>
    /// <param name="diagramMapping">The <see cref="DiagramMapping"/> of the modified element.</param>
    /// <param name="elementBase">The <see cref="ElementBase"/> that was sent from the backend.</param>
    public void MGLUpdateSO (DiagramMapping diagramMapping = null, ElementBase elementBase = null)
    {
    }

    #endregion

}