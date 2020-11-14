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



    public void OnEnable ()
    {
        //Manifest that defines what the SO uses from Machinations.
        Manifest = new MachiObjectManifest
        {
            Name =  "Sound Event",
            DiagramMappings = new List<DiagramMapping>
            {
                new DiagramMapping
                {
                    PropertyName = M_MAX_SOUND_HIGH_HEALTH,
                    DiagramElementID = 250,
                    DefaultElementBase = new ElementBase(4, null),
                    OverrideElementBase = new ElementBase(4, null)
                },
                new DiagramMapping
                {
                    PropertyName = M_MAX_SOUND_LOW_HEALTH,
                    DiagramElementID = 251,
                    DefaultElementBase = new ElementBase(6, null),
                    OverrideElementBase = new ElementBase(6, null)
                }
            },
            CommonStatesAssociations = new List<StatesAssociation>
            {
                new StatesAssociation("Exploring", new List<GameStates>() {GameStates.Exploring})
            }
        };
        
        //Register this SO with the MGL.
        MachinationsDataLayer.EnrollScriptableObject(this, Manifest);
    }

    #region IMachinationsScriptableObject
    
    public MachiObjectManifest Manifest { get; private set; }
    
    public ScriptableObject SO => this;

    /// <summary>
    /// Called when Machinations initialization has been completed.
    /// </summary>
    /// <param name="binders">The Binders for this Object.</param>
    public void MGLInitCompleteSO (Dictionary<string, ElementBinder> binders)
    {
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

    override public void Play (AudioSource source)
    {
        if (clips.Length == 0) return;

        /*
        //If the Binders were received.
        if (_binders != null && _binders[M_MAX_SOUND_HIGH_HEALTH] != null)
        {
            //Set volume for...
            //More than 50% life.
            if (PlayerControlledTank.PlayerControlledTankHealth.m_CurrentHealth >
                (float)PlayerControlledTank.PlayerControlledTankHealth.m_TankStats.Health.CurrentValue / 2)
            {
                //Debug.Log("MGLUpdateSO: PlayerControlledTankHealth > 50");
                volume.minValue = (_binders[M_MAX_SOUND_HIGH_HEALTH].Value - 2) / 100f;
                volume.maxValue = _binders[M_MAX_SOUND_HIGH_HEALTH].Value / 100f;
            }
            else //Less than 50% life.
            {
                //Debug.Log("MGLUpdateSO: PlayerControlledTankHealth < 50");
                volume.minValue = (_binders[M_MAX_SOUND_LOW_HEALTH].Value - 2) / 100f;
                volume.maxValue = _binders[M_MAX_SOUND_LOW_HEALTH].Value / 100f;
            }
        }
        */

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