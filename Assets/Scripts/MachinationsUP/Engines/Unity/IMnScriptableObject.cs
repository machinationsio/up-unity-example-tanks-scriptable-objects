using System;
using System.Collections.Generic;
using MachinationsUP.Integration.Binder;
using MachinationsUP.Integration.Elements;
using MachinationsUP.Integration.Inventory;
using UnityEngine;

namespace MachinationsUP.Engines.Unity
{
    /// <summary>
    /// Defines a contract that allows a Scriptable Object to be notified by Machinations.
    /// See <see cref="MnDataLayer"/>.
    /// </summary>
    public interface IMnScriptableObject
    {

        /// <summary>
        /// If not null, you MUST invoke this in MDLUpdateSO.
        /// </summary>
        event EventHandler OnUpdatedFromMachinations;
        
        ScriptableObject SO { get; }
        
        MnObjectManifest Manifest { get; }

        /// <summary>
        /// Called when Machinations initialization has been completed.
        /// </summary>
        /// <param name="binders">The Binders for this Object.</param>
        void MDLInitCompleteSO (Dictionary<string, ElementBinder> binders);

        /// <summary>
        /// Called by the <see cref="MnDataLayer"/> when an element has been updated in the Machinations back-end.
        /// </summary>
        /// <param name="diagramMapping">The <see cref="DiagramMapping"/> of the modified element.</param>
        /// <param name="elementBase">The <see cref="ElementBase"/> that was sent from the backend.</param>
        void MDLUpdateSO (DiagramMapping diagramMapping = null, ElementBase elementBase = null);

    }
}