using System;
using System.Collections.Generic;
using MachinationsUP.Engines.Unity;
using MachinationsUP.Integration.Binder;
using MachinationsUP.Integration.Elements;
using MachinationsUP.Integration.Inventory;
using MachinationsUP.Logger;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(menuName = "ItemStats/Tank")]
public class TankStats : ScriptableObject, IMnScriptableObject
{

    //Machinations.
    
    public ElementBase Health;
    public ElementBase Speed;
    public ElementBase HealthBuff;
    public ElementBase SpeedBuff;

    public float CurrentHealthBuff;
    public float CurrentSpeedBuff;

    private const string M_HEALTH = "Health";
    private const string M_SPEED = "Speed";
    private const string M_HEALTH_BUFF = "HealthBuff";
    private const string M_SPEED_BUFF = "SpeedDeBuff";
    
    public event EventHandler OnUpdatedFromMachinations;

    public void OnEnable ()
    {
        //Manifest that defines what the SO uses from Machinations.
        Manifest = new MnObjectManifest
        {
            Name = "Player Tank Stats",
            DiagramMappings = new List<DiagramMapping>
            {
                new DiagramMapping
                {
                    EditorElementBase = Health,
                    PropertyName = M_HEALTH,
                    DiagramElementID = 8919,
                    DefaultElementBase = new ElementBase(550, null)
                },
                new DiagramMapping
                {
                    EditorElementBase = Speed,
                    PropertyName = M_SPEED,
                    DiagramElementID = 8908,
                    DefaultElementBase = new ElementBase(25, null)
                },
                //BUFFS.
                new DiagramMapping
                {
                    EditorElementBase = HealthBuff,
                    PropertyName = M_HEALTH_BUFF,
                    DiagramElementID = 9134,
                    DefaultElementBase = new ElementBase(10, null)
                },
                new DiagramMapping
                {
                    EditorElementBase = SpeedBuff,
                    PropertyName = M_SPEED_BUFF,
                    DiagramElementID = 100002,
                    DefaultElementBase = new ElementBase(10, null)
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
        Health = binders[M_HEALTH].CurrentElement;
        Speed = binders[M_SPEED].CurrentElement;
        HealthBuff = binders[M_HEALTH_BUFF].CurrentElement;
        SpeedBuff = binders[M_SPEED_BUFF].CurrentElement;
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