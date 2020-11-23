using System.Collections.Generic;
using MachinationsUP.Engines.Unity;
using MachinationsUP.Integration.Binder;
using MachinationsUP.Integration.Elements;
using MachinationsUP.Integration.Inventory;
using UnityEngine;

[CreateAssetMenu(menuName = "ScritableObjects/SomeScriptableObject")]
public class SomeScriptableObject : ScriptableObject, IMachinationsScriptableObject
{

    public ElementBase MovementSpeed; //These are all visible in Unity's Property Inspector.
    public ElementBase SizeX;
    public ElementBase SizeY;
    public ElementBase ChangeDirectionTime;
    
    private const string M_MOVEMENTSPEED = "MovementSpeed";
    private const string M_SIZEX = "SizeX";
    private const string M_SIZEY = "SizeY";
    private const string M_CHANGE_DIRECTION_TIME = "ChangeDirectionTime";
    //
    // public void OnEnable ()
    // {
    //     //Manifest that defines what the Scriptable Object uses from Machinations.
    //     Manifest = new MachinationsObjectManifest
    //     {
    //         Name = "Rectangle's Properties",
    //         DiagramMappings = new List<DiagramMapping>
    //         {
    //             new DiagramMapping
    //             {
    //                 GameElementBase = MovementSpeed,
    //                 PropertyName = M_MOVEMENTSPEED,
    //                 DiagramElementID = 102,
    //                 DefaultElementBase = new ElementBase(15, null)
    //             },
    //             new DiagramMapping
    //             {
    //                 GameElementBase = SizeX,
    //                 PropertyName = M_SIZEX,
    //                 DiagramElementID = 103,
    //                 DefaultElementBase = new ElementBase(3, null)
    //             },
    //             new DiagramMapping
    //             {
    //                 GameElementBase = ChangeDirectionTime,
    //                 PropertyName = M_CHANGE_DIRECTION_TIME,
    //                 DiagramElementID = 2100,
    //                 DefaultElementBase = new ElementBase(50, null)
    //             },
    //             new DiagramMapping
    //             {
    //                 GameElementBase = SizeY,
    //                 PropertyName = M_SIZEY,
    //                 DiagramElementID = 201,
    //                 DefaultElementBase = new ElementBase(50, null)
    //             }
    //         }
    //     };
    //
    //     //Register this Scriptable Object with the MDL.
    //     MachinationsDataLayer.EnrollScriptableObject(this, Manifest);
    // }
    
    #region IMachinationsScriptableObject

    public MachinationsObjectManifest Manifest { get; private set; }

    public ScriptableObject SO => this;

    /// <summary>
    /// Called when Machinations initialization has been completed.
    /// </summary>
    /// <param name="binders">The Binders for this Object.</param>
    public void MGLInitCompleteSO (Dictionary<string, ElementBinder> binders)
    {
        //Once the values are fetched from Machinations, make sure they are associated
        //with this Scriptable Object.
        MovementSpeed = binders[M_MOVEMENTSPEED].CurrentElement;
        SizeX = binders[M_SIZEX].CurrentElement;
        SizeY = binders[M_SIZEY].CurrentElement;
        ChangeDirectionTime = binders[M_CHANGE_DIRECTION_TIME].CurrentElement;
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
