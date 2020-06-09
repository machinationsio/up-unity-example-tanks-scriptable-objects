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

    //Machinations.

    //Tracked Machinations Elements.
    private const string M_DAMAGE = "Damage";
    private const string M_RADIUS = "Radius";
    private const string M_FORCE = "Force";

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
                DiagramElementID = 1,
                DefaultElementBase = new ElementBase(105)
            },
            new DiagramMapping
            {
                GameObjectPropertyName = M_RADIUS,
                DiagramElementID = 2,
                DefaultElementBase = new ElementBase(25)
            },
            new DiagramMapping
            {
                GameObjectPropertyName = M_FORCE,
                DiagramElementID = 3,
                DefaultElementBase = new ElementBase(25)
            }
        }
    };

    public void OnEnable ()
    {
        Debug.Log("SO ShellStats OnEnable.");
        //Register this SO with the MGL.
        MachinationsGameLayer.EnrollScriptableObject(this, _manifest);
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
        Radius = _binders[M_FORCE].Value;
    }
    
    #endregion

}