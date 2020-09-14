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

    private float prevDamage;
    private float prevRadius;
    private float prevForce;

    //Machinations.

    //Tracked Machinations Elements.
    private const string M_DAMAGE = "Damage";
    private const string M_RADIUS = "Radius";
    private const string M_FORCE = "Force";
    private const string M_SPEED = "Speed";

    //Binders used to transfer information to this SO.
    private Dictionary<string, ElementBinder> _binders;

    //Manifest that defines what the SO uses from Machinations.
    static readonly private MachinationsGameObjectManifest _manifest = new MachinationsGameObjectManifest
    {
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
                DiagramElementID = 225,
                DefaultElementBase = new ElementBase(25)
            },
            new DiagramMapping
            {
                GameObjectPropertyName = M_FORCE,
                DiagramElementID = 107,
                DefaultElementBase = new ElementBase(90)
            },
            new DiagramMapping
            {
                GameObjectPropertyName = M_SPEED,
                DiagramElementID = 1000,
                DefaultElementBase = new ElementBase(90)
            }
        }
    };

    public void OnEnable ()
    {
        Debug.Log("SO ShellStats OnEnable.");
        //Register this SO with the MGL.
        MachinationsGameLayer.EnrollScriptableObject(this, _manifest);
    }

    public void OnValidate ()
    {
        if (Math.Abs(prevDamage - Damage) > 0)
        {
            Debug.Log("Damage Changed.");
        }

        prevDamage = Damage;
        prevForce = Force;
        prevRadius = Radius;
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
    }

    #endregion

}