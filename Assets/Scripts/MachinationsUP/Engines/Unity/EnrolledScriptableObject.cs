using System.Collections.Generic;
using MachinationsUP.Integration.Binder;
using MachinationsUP.Integration.Inventory;

namespace MachinationsUP.Engines.Unity
{
    
    /// <summary>
    /// Used by <see cref="MachinationsDataLayer"/> to keep track of enrolled Scriptable Objects.
    /// </summary>
    public class EnrolledScriptableObject
    {

        /// <summary>
        /// The <see cref="IMachinationsScriptableObject"/> represented by this class.
        /// </summary>
        public IMachinationsScriptableObject MScriptableObject;
        
        /// <summary>
        /// The <see cref="MachiObjectManifest"/> defining what
        /// the <see cref="IMachinationsScriptableObject"/> needs.
        /// </summary>
        public MachiObjectManifest Manifest;
        
        /// <summary>
        /// The Binders used by the <see cref="IMachinationsScriptableObject"/>.
        /// They can be set only AFTER <see cref="MachinationsDataLayer"/> initialization.
        /// Dictionary of Game Object Property Name and ElementBinder. 
        /// </summary>
        public Dictionary<string, ElementBinder> Binders;

    }
}