using System.Collections.Generic;
using MachinationsUP.Integration.Binder;
using MachinationsUP.Integration.Inventory;

namespace MachinationsUP.Engines.Unity
{
    
    /// <summary>
    /// Used by <see cref="MnDataLayer"/> to keep track of enrolled Scriptable Objects.
    /// </summary>
    public class EnrolledScriptableObject
    {

        /// <summary>
        /// The <see cref="IMnScriptableObject"/> represented by this class.
        /// </summary>
        public IMnScriptableObject MScriptableObject;
        
        /// <summary>
        /// The <see cref="MnObjectManifest"/> defining what
        /// the <see cref="IMnScriptableObject"/> needs.
        /// </summary>
        public MnObjectManifest Manifest;
        
        /// <summary>
        /// The Binders used by the <see cref="IMnScriptableObject"/>.
        /// They can be set only AFTER <see cref="MnDataLayer"/> initialization.
        /// Dictionary of Game Object Property Name and ElementBinder. 
        /// </summary>
        public Dictionary<string, ElementBinder> Binders;

    }
}