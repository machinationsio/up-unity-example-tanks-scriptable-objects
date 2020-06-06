using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using MachinationsUP.Engines.Unity;
using MachinationsUP.GameEngineAPI.Game;
using MachinationsUP.GameEngineAPI.GameObject;
using MachinationsUP.GameEngineAPI.States;
using MachinationsUP.Integration.Binder;
using MachinationsUP.Integration.Elements;
using MachinationsUP.Integration.GameObject;
using MachinationsUP.Integration.Inventory;
using UnityEditor;
using Random = UnityEngine.Random;

[CreateAssetMenu(menuName = "Audio Events/Simple")]
public class SimpleAudioEvent : AudioEvent, IMachinationsScriptableObject
{

    public AudioClip[] clips;

    public RangedFloat volume;

    [MinMaxRange(0, 2)] public RangedFloat pitch;

    //Machinations.

    //Tracked Machinations Elements.
    private const string M_HEALTH = "Health";
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
                GameObjectPropertyName = M_HEALTH,
                DiagramElementID = 19,
                DefaultElementBase = new ElementBase(105)
            },
            new DiagramMapping
            {
                GameObjectPropertyName = M_SPEED,
                DiagramElementID = 102,
                DefaultElementBase = new ElementBase(25)
            }
        },
        CommonStatesAssociations = new List<StatesAssociation>
        {
            new StatesAssociation("Exploring", new List<GameStates>() {GameStates.Exploring})
        }
    };

    public void OnEnable ()
    {
        Debug.Log("SO SimpleAudioEvent OnEnable.");
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
        //TODO: implement in this game proper state switching based on Night/Day. Until then, using hardcoded States.
        _binders[M_SPEED].UpdateStates(GameStates.Exploring, GameObjectStates.Undefined);
        _binders[M_HEALTH].UpdateStates(GameStates.Exploring, GameObjectStates.Undefined);
        MGLUpdateSO();
    }

    /// <summary>
    /// Called by the <see cref="MachinationsGameLayer"/> when an element has been updated in the Machinations back-end.
    /// </summary>
    /// <param name="diagramMapping">The <see cref="DiagramMapping"/> of the modified element.</param>
    /// <param name="elementBase">The <see cref="ElementBase"/> that was sent from the backend.</param>
    public void MGLUpdateSO (DiagramMapping diagramMapping = null, ElementBase elementBase = null)
    {
        //TODO: the code below is hack-ish because we don't yet have a diagram for this game.
        //This game uses Ruby diagram for now. Ideally, you'd want to interpret this based on
        //diagramMapping.Binder, since that binder is what you have here and it's already
        //set up to represent the current Game (Object) State.
    }

    #endregion

    override public void Play (AudioSource source)
    {
        if (clips.Length == 0) return;

        source.clip = clips[Random.Range(0, clips.Length)];
        source.volume = Random.Range(volume.minValue, volume.maxValue);
        source.pitch = Random.Range(pitch.minValue, pitch.maxValue);
        
        if (PlayerControlledTank.PlayerControlledTankHealth == null)
        {
            Debug.Log("MGLUpdateSO: NULL PLAYER TANK");
            return;
        }

        //More than 50% life.
        if (PlayerControlledTank.PlayerControlledTankHealth.m_CurrentHealth >
            PlayerControlledTank.PlayerControlledTankHealth.m_TankStats.Health / 2)
        {
            Debug.Log("MGLUpdateSO: PlayerControlledTankHealth > 50");
            volume.minValue = _binders[M_SPEED].Value / 100f; 
            volume.maxValue = _binders[M_SPEED].Value / 100f;
        }
        else
        {
            Debug.Log("MGLUpdateSO: PlayerControlledTankHealth < 50");
            volume.minValue = _binders[M_HEALTH].Value / 100f;
            volume.maxValue = _binders[M_HEALTH].Value / 100f;
        }
        
        source.Play();
    }

}