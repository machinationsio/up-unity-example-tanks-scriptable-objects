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
    private const string M_MAX_SOUND_HIGH_HEALTH = "MaxSoundHighHealth";

    private const string M_MAX_SOUND_LOW_HEALTH = "MaxSoundLowHealth";

    //Binders used to transfer information to this SO.
    private Dictionary<string, ElementBinder> _binders;

    //Manifest that defines what the SO uses from Machinations.
    static readonly private MachinationsGameObjectManifest _manifest = new MachinationsGameObjectManifest
    {
        PropertiesToSync = new List<DiagramMapping>
        {
            new DiagramMapping
            {
                GameObjectPropertyName = M_MAX_SOUND_HIGH_HEALTH,
                DiagramElementID = 250,
                DefaultElementBase = new ElementBase(4)
            },
            new DiagramMapping
            {
                GameObjectPropertyName = M_MAX_SOUND_LOW_HEALTH,
                DiagramElementID = 251,
                DefaultElementBase = new ElementBase(6)
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
        _binders[M_MAX_SOUND_LOW_HEALTH].UpdateStates(GameStates.Exploring, GameObjectStates.Undefined);
        _binders[M_MAX_SOUND_HIGH_HEALTH].UpdateStates(GameStates.Exploring, GameObjectStates.Undefined);
        MGLUpdateSO();
    }

    /// <summary>
    /// Called by the <see cref="MachinationsGameLayer"/> when an element has been updated in the Machinations back-end.
    /// </summary>
    /// <param name="diagramMapping">The <see cref="DiagramMapping"/> of the modified element.</param>
    /// <param name="elementBase">The <see cref="ElementBase"/> that was sent from the backend.</param>
    public void MGLUpdateSO (DiagramMapping diagramMapping = null, ElementBase elementBase = null)
    {
        //No update is necessary. Sound volume will be taken directly from Binders
        //when the sound is played.
    }

    #endregion

    override public void Play (AudioSource source)
    {
        if (clips.Length == 0) return;

        //If the Binders were received.
        if (_binders != null && _binders[M_MAX_SOUND_HIGH_HEALTH] != null)
        {
            //Set volume for...
            //More than 50% life.
            if (PlayerControlledTank.PlayerControlledTankHealth.m_CurrentHealth >
                PlayerControlledTank.PlayerControlledTankHealth.m_TankStats.Health / 2)
            {
                Debug.Log("MGLUpdateSO: PlayerControlledTankHealth > 50");
                volume.minValue = (_binders[M_MAX_SOUND_HIGH_HEALTH].Value - 2) / 100f;
                volume.maxValue = _binders[M_MAX_SOUND_HIGH_HEALTH].Value / 100f;
            }
            else //Less than 50% life.
            {
                Debug.Log("MGLUpdateSO: PlayerControlledTankHealth < 50");
                volume.minValue = (_binders[M_MAX_SOUND_LOW_HEALTH].Value - 2) / 100f;
                volume.maxValue = _binders[M_MAX_SOUND_LOW_HEALTH].Value / 100f;
            }
        }

        source.clip = clips[Random.Range(0, clips.Length)];
        source.volume = Random.Range(volume.minValue, volume.maxValue);
        source.pitch = Random.Range(pitch.minValue, pitch.maxValue);

        if (PlayerControlledTank.PlayerControlledTankHealth == null)
        {
            Debug.Log("MGLUpdateSO: NULL PLAYER TANK");
            return;
        }

        source.Play();
    }

}