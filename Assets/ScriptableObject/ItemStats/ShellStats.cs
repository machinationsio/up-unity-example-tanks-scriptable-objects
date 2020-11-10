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
    
    public ElementBase Damage;
    public ElementBase Radius;
    public ElementBase  Force;
    public ElementBase  Speed;
    public ElementBase  ShotCooldown;

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
    static readonly private MachiObjectManifest _manifest = new MachiObjectManifest
    {
        Name = "Shell Stats",
        DiagramMappings = new List<DiagramMapping>
        {
            new DiagramMapping
            {
                PropertyName = M_DAMAGE,
                DiagramElementID = 219,
                DefaultElementBase = new ElementBase(10)
            },
            new DiagramMapping
            {
                PropertyName = M_RADIUS,
                DiagramElementID = 401,
                DefaultElementBase = new ElementBase(25)
            },
            new DiagramMapping
            {
                PropertyName = M_FORCE,
                DiagramElementID = 400,
                DefaultElementBase = new ElementBase(90)
            },
            new DiagramMapping
            {
                PropertyName = M_SPEED,
                DiagramElementID = 402,
                DefaultElementBase = new ElementBase(90)
            },
            new DiagramMapping
            {
                PropertyName = M_COOLDOWN,
                DiagramElementID = 912,
                DefaultElementBase = new ElementBase(0)
            }
        }
    };

    public void OnEnable ()
    {
        Debug.Log("SO ShellStats OnEnable.");
        //Register this SO with the MGL.
        MachinationsDataLayer.EnrollScriptableObject(this, _manifest);
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
    /// Called by the <see cref="MachinationsDataLayer"/> when an element has been updated in the Machinations back-end.
    /// </summary>
    /// <param name="diagramMapping">The <see cref="DiagramMapping"/> of the modified element.</param>
    /// <param name="elementBase">The <see cref="ElementBase"/> that was sent from the backend.</param>
    public void MGLUpdateSO (DiagramMapping diagramMapping = null, ElementBase elementBase = null)
    {
        Damage = _binders[M_DAMAGE].CurrentElement;
        Radius = _binders[M_RADIUS].CurrentElement;
        Force = _binders[M_FORCE].CurrentElement;
        Speed = _binders[M_SPEED].CurrentElement;
        ShotCooldown = _binders[M_COOLDOWN].CurrentElement;
    }

    #endregion

}