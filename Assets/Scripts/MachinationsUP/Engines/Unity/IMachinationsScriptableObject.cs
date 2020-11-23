﻿using System.Collections.Generic;
using MachinationsUP.Integration.Binder;
using MachinationsUP.Integration.Elements;
using MachinationsUP.Integration.Inventory;
using UnityEngine;

namespace MachinationsUP.Engines.Unity
{
    /// <summary>
    /// Defines a contract that allows a Scriptable Object to be notified by Machinations.
    /// See <see cref="MachinationsDataLayer"/>.
    /// </summary>
    public interface IMachinationsScriptableObject
    {

        ScriptableObject SO { get; }
        
        MachinationsObjectManifest Manifest { get; }

        /// <summary>
        /// Called when Machinations initialization has been completed.
        /// </summary>
        /// <param name="binders">The Binders for this Object.</param>
        void MGLInitCompleteSO (Dictionary<string, ElementBinder> binders);

        /// <summary>
        /// Called by the <see cref="MachinationsDataLayer"/> when an element has been updated in the Machinations back-end.
        /// </summary>
        /// <param name="diagramMapping">The <see cref="DiagramMapping"/> of the modified element.</param>
        /// <param name="elementBase">The <see cref="ElementBase"/> that was sent from the backend.</param>
        void MGLUpdateSO (DiagramMapping diagramMapping = null, ElementBase elementBase = null);

    }
}