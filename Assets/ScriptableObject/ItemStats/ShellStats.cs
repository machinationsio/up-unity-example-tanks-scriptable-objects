using System;
using System.Collections.Generic;
using MachinationsUP.Engines.Unity;
using MachinationsUP.Integration.Binder;
using MachinationsUP.Integration.Elements;
using MachinationsUP.Integration.Inventory;
using UnityEngine;

[CreateAssetMenu(menuName = "ItemStats/Shell")]
public class ShellStats : ScriptableObject, IMachinationsScriptableObject
{

    public float Damage;
    public float Radius;
    public float Force;
    public float Speed;
    public float ShotCooldown;
    public ElementBase DamageNew;
    public ElementBase RadiusNew;

    //Machinations.

    //Tracked Machinations Elements.
    private const string M_DAMAGE = "Damage";
    private const string M_RADIUS = "Radius";
    private const string M_FORCE = "Force";
    private const string M_SPEED = "Speed";
    private const string M_COOLDOWN = "Cooldown";

    //Binders used to transfer information to this SO.
    private Dictionary<string, ElementBinder> _binders;

    //Manifest that defines what the SO uses from Machinations.
    static readonly private MachinationsGameObjectManifest _manifest = new MachinationsGameObjectManifest
    {
        GameObjectName = "Shell Stats",
        PropertiesToSync = new List<DiagramMapping>
        {
            new DiagramMapping
            {
                GameObjectPropertyName = M_DAMAGE,
                DiagramElementID = 219,
                DefaultElementBase = new ElementBase(10)
            },
            new DiagramMapping
            {
                GameObjectPropertyName = M_RADIUS,
                DiagramElementID = 401,
                DefaultElementBase = new ElementBase(25)
            },
            new DiagramMapping
            {
                GameObjectPropertyName = M_FORCE,
                DiagramElementID = 400,
                DefaultElementBase = new ElementBase(90)
            },
            new DiagramMapping
            {
                GameObjectPropertyName = M_SPEED,
                DiagramElementID = 402,
                DefaultElementBase = new ElementBase(90)
            },
            new DiagramMapping
            {
                GameObjectPropertyName = M_COOLDOWN,
                DiagramElementID = 912,
                DefaultElementBase = new ElementBase(0)
            }
        }
    };

    public void OnEnable ()
    {
        Debug.Log("SO ShellStats OnEnable.");
        //Register this SO with the MGL.
        MachinationsGameLayer.EnrollScriptableObject(this, _manifest);
        
        //Initialize ElementBases with proper values.
        DamageNew.MaxValue = 400;
    }

    public void OnValidate ()
    {
        
        //FIND A WAY TO USE ELEMENT-BASE INSTEAD OF FLOAT.
        /*
        
        if (Math.Abs(prevDamage - Damage) > 0)
        {
            Debug.Log("Damage Changed from: " + prevDamage + " to " + Damage);
            //Only notifying MGL if it is Initialized.
            if (MachinationsGameLayer.IsInitialized)
                MachinationsGameLayer.Instance.EmitGameUpdateDiagramElementsRequest(_manifest.GetDiagramMapping(M_DAMAGE), Damage);
        }
        if (Math.Abs(prevForce - Force) > 0)
        {
            Debug.Log("Force Changed from: " + prevForce + " to " + Force);
            //Only notifying MGL if it is Initialized.
            if (MachinationsGameLayer.IsInitialized)
                MachinationsGameLayer.Instance.EmitGameUpdateDiagramElementsRequest(_manifest.GetDiagramMapping(M_FORCE), Force);
        }
        if (Math.Abs(prevRadius - Radius) > 0)
        {
            Debug.Log("Radius Changed from: " + prevRadius + " to " + Radius);
            //Only notifying MGL if it is Initialized.
            if (MachinationsGameLayer.IsInitialized)
                MachinationsGameLayer.Instance.EmitGameUpdateDiagramElementsRequest(_manifest.GetDiagramMapping(M_RADIUS), Radius);
        }

        prevDamage = Damage;
        prevForce = Force;
        prevRadius = Radius;
        */
    }

    #region IMachinationsScriptableObject

    /// <summary>
    /// Called when Machinations initialization has been completed.
    /// </summary>
    /// <param name="binders">The Binders for this Object.</param>
    public void MGLInitCompleteSO (Dictionary<string, ElementBinder> binders)
    {
        _binders = binders;
        MGLUpdateSO();
    }

    /// <summary>
    /// Called by the <see cref="MachinationsGameLayer"/> when an element has been updated in the Machinations back-end.
    /// </summary>
    /// <param name="diagramMapping">The <see cref="DiagramMapping"/> of the modified element.</param>
    /// <param name="elementBase">The <see cref="ElementBase"/> that was sent from the backend.</param>
    public void MGLUpdateSO (DiagramMapping diagramMapping = null, ElementBase elementBase = null)
    {
        Damage = _binders[M_DAMAGE].Value;
        Radius = _binders[M_RADIUS].Value;
        Force = _binders[M_FORCE].Value;
        Speed = _binders[M_SPEED].Value;
        ShotCooldown = _binders[M_COOLDOWN].Value;

        DamageNew = _binders[M_DAMAGE].CurrentElement;
        RadiusNew = _binders[M_RADIUS].CurrentElement;
    }

    #endregion

}