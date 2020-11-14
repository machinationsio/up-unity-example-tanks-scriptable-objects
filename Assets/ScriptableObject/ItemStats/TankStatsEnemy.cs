﻿using System.Collections.Generic;
using MachinationsUP.Engines.Unity;
using MachinationsUP.GameEngineAPI.Game;
using MachinationsUP.GameEngineAPI.GameObject;
using MachinationsUP.GameEngineAPI.States;
using MachinationsUP.Integration.Binder;
using MachinationsUP.Integration.Elements;
using MachinationsUP.Integration.Inventory;
using MachinationsUP.Logger;
using UnityEditor;
using UnityEngine;

[CreateAssetMenu(menuName = "ItemStats/TankEnemy")]
public class TankStatsEnemy : ScriptableObject, IMachinationsScriptableObject
{

    //Machinations.
    
    public ElementBase Health;
    public ElementBase Speed;

    private const string M_HEALTH = "Health";
    private const string M_SPEED = "Speed";

    public void OnEnable ()
    {
        //Manifest that defines what the SO uses from Machinations.
        Manifest = new MachiObjectManifest
        {
            Name = "Enemy Tank Stats",
            DiagramMappings = new List<DiagramMapping>
            {
                new DiagramMapping
                {
                    GameElementBase = Health,
                    PropertyName = M_HEALTH,
                    DiagramElementID = 230,
                    DefaultElementBase = new ElementBase(105, null)
                },
                new DiagramMapping
                {
                    GameElementBase = Speed,
                    PropertyName = M_SPEED,
                    DiagramElementID = 900,
                    DefaultElementBase = new ElementBase(25, null)
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
    
    public MachiObjectManifest Manifest { get; private set; }

    /// <summary>
    /// Called when Machinations initialization has been completed.
    /// </summary>
    /// <param name="binders">The Binders for this Object.</param>
    public void MGLInitCompleteSO (Dictionary<string, ElementBinder> binders)
    {
        Health = binders[M_HEALTH].CurrentElement;
        Speed = binders[M_SPEED].CurrentElement;
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