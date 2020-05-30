using MachinationsUP.Integration.Elements;
using MachinationsUP.Integration.Inventory;

namespace MachinationsUP.Engines.Unity
{
    /// <summary>
    /// Defines a contract that allows a Scriptable Object to be notified by Machinations.
    /// See <see cref="MachinationsUP.Engines.Unity.MachinationsGameLayer"/>.
    /// </summary>
    public interface IMachinationsScriptableObject
    {

        /// <summary>
        /// Notification when Machinations initialization has been completed.
        /// </summary>
        void MGLInitCompleteSO ();

        /// <summary>
        /// Notification when an element has been updated in the Machinations back-end.
        /// </summary>
        /// <param name="diagramMapping">Coordinates of the modifications.</param>
        /// <param name="elementBase">The modification.</param>
        void MGLUpdateSO (DiagramMapping diagramMapping = null, ElementBase elementBase = null);

    }
}