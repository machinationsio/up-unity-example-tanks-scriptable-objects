using System.Collections.Generic;
using MachinationsUP.Engines.Unity;
using MachinationsUP.GameEngineAPI.Game;
using MachinationsUP.GameEngineAPI.GameObject;
using MachinationsUP.GameEngineAPI.States;
using MachinationsUP.Integration.Binder;
using MachinationsUP.Integration.Elements;
using MachinationsUP.Integration.Inventory;
using UnityEngine;

[CreateAssetMenu(menuName = "ItemStats/TankEnemy")]
public class TankStatsEnemy : ScriptableObject, IMachinationsScriptableObject
{

    public ElementBase Health;
    public ElementBase Speed;

    //Machinations.

    //Tracked Machinations Elements.
    private const string M_HEALTH = "Health";
    private const string M_SPEED = "Speed";

    //Binders used to transfer information to this SO.
    private Dictionary<string, ElementBinder> _binders;

    //Manifest that defines what the SO uses from Machinations.
    static readonly private MachiObjectManifest _manifest = new MachiObjectManifest
    {
        Name = "Enemy Tank Stats",
        DiagramMappings = new List<DiagramMapping>
        {
            new DiagramMapping
            {
                PropertyName = M_HEALTH,
                DiagramElementID = 230,
                DefaultElementBase = new ElementBase(105)
            },
            new DiagramMapping
            {
                PropertyName = M_SPEED,
                DiagramElementID = 900,
                DefaultElementBase = new ElementBase(25)
            }
        }
    };

    public void OnEnable ()
    {
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
        Health = _binders[M_HEALTH].CurrentElement;
        Speed = _binders[M_SPEED].CurrentElement;
    }

    #endregion

}