using System;
using System.Collections.Generic;
using MachinationsUP.Engines.Unity;
using MachinationsUP.Integration.Binder;
using MachinationsUP.Integration.Elements;
using MachinationsUP.Integration.Inventory;
using UnityEngine;
using UnityEngine.EventSystems;

[CreateAssetMenu(menuName = "ScritableObjects/SomeScriptableObject")]
public class SomeScriptableObject : ScriptableObject, IMnScriptableObject
{

    public ElementBase MovementSpeed; //These are all visible in Unity's Property Inspector.
    public ElementBase SizeX;
    public ElementBase SizeY;
    public ElementBase SizeZ;
    public ElementBase ChangeDirectionTime;
    
    private const string M_MOVEMENTSPEED = "MovementSpeed";
    private const string M_SIZEX = "SizeX";
    private const string M_SIZEY = "SizeY";
    private const string M_CHANGE_DIRECTION_TIME = "ChangeDirectionTime";
    private const string M_SIZEZ = "SizeZ [SizeZ]";

    public event EventHandler OnUpdatedFromMachinations;
    
    public void OnEnable ()
    {
        //Manifest that defines what the Scriptable Object uses from Machinations.
        Manifest = new MnObjectManifest()
        {
            Name = "Rectangle's Properties",
            DiagramMappings = new List<DiagramMapping>
            {
                new DiagramMapping
                {
                    EditorElementBase = MovementSpeed,
                    PropertyName = M_MOVEMENTSPEED,
                    DiagramElementID = 102,
                    DefaultElementBase = new ElementBase(15, null)
                },
                new DiagramMapping
                {
                    EditorElementBase = ChangeDirectionTime,
                    PropertyName = M_CHANGE_DIRECTION_TIME,
                    DiagramElementID = 2100,
                    DefaultElementBase = new ElementBase(50, null)
                },
                new DiagramMapping
                {
                    EditorElementBase = SizeX,
                    PropertyName = M_SIZEX,
                    DiagramElementID = 103,
                    DefaultElementBase = new ElementBase(3, null)
                },
                new DiagramMapping
                {
                    EditorElementBase = SizeY,
                    PropertyName = M_SIZEY,
                    DiagramElementID = 201,
                    DefaultElementBase = new ElementBase(50, null)
                },
                new DiagramMapping
                {
                    PropertyName = M_SIZEZ,
                    DiagramElementID = 14000,
                    EditorElementBase = SizeZ
                }
            }
        };
    
        //Register this Scriptable Object with the MDL.
        //MnDataLayer.EnrollScriptableObject(this, Manifest);
    }
    
    #region IMachinationsScriptableObject

    public MnObjectManifest Manifest { get; private set; }

    public ScriptableObject SO => this;

    /// <summary>
    /// Called when Machinations initialization has been completed.
    /// </summary>
    /// <param name="binders">The Binders for this Object.</param>
    public void MDLInitCompleteSO (Dictionary<string, ElementBinder> binders)
    {
        //Once the values are fetched from Machinations, make sure they are associated
        //with this Scriptable Object.
        MovementSpeed = binders[M_MOVEMENTSPEED].CurrentElement;
        SizeX = binders[M_SIZEX].CurrentElement;
        SizeY = binders[M_SIZEY].CurrentElement;
        SizeZ = binders[M_SIZEZ].CurrentElement;
        ChangeDirectionTime = binders[M_CHANGE_DIRECTION_TIME].CurrentElement;
    }

    /// <summary>
    /// Called by the <see cref="MnDataLayer"/> when an element has been updated in the Machinations back-end.
    /// </summary>
    /// <param name="diagramMapping">The <see cref="DiagramMapping"/> of the modified element.</param>
    /// <param name="elementBase">The <see cref="ElementBase"/> that was sent from the backend.</param>
    public void MDLUpdateSO (DiagramMapping diagramMapping = null, ElementBase elementBase = null)
    {
        OnUpdatedFromMachinations?.Invoke(this, null);
    }

    #endregion
    
}
