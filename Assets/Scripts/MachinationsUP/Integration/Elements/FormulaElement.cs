using System;
using System.Runtime.Serialization;
using System.Xml.Serialization;
using MachinationsUP.Engines.Unity;
using MachinationsUP.Integration.Binder;
using MachinationsUP.Integration.Elements.Formula;
using MachinationsUP.Integration.Inventory;
using UnityEngine;

namespace MachinationsUP.Integration.Elements
{
    /// <summary>
    /// Wraps a Machinations Formula, allowing Element-related access to it.
    /// <see cref="MnFormula"/>
    /// </summary>
    [DataContract(Name = "MachinationsFormula", Namespace = "http://www.machinations.io")]
    public class FormulaElement : ElementBase
    {

        private MnFormula _mFormula;

        /// <summary>
        /// Machinations Formula that is used to determine this Element's value.
        /// </summary>
        [DataMember()]
        public MnFormula MFormula
        {
            get => _mFormula;
            private set => _mFormula = value;
        }

        /// <summary>
        /// If to recalculate the BaseValue from the Formula at reset.
        /// </summary>
        [DataMember()]
        public bool RerunFormulaAtReset { get; private set; }

        /// <summary>
        /// If to recalculate the BaseValue from the Formula each time the CurrentValue is queried.
        /// </summary>
        [DataMember()]
        public bool RerunFormulaAtEveryAccess { get; private set; }

        /// <summary>
        /// Formula that this Element uses.
        /// </summary>
        [DataMember()]
        public string FormulaString { get; private set; }

        //For Serialization only!
        public FormulaElement ()
        {
        }

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="formulaString">Formula to use.</param>
        /// <param name="mapping">The DiagramMapping associated with this ElementBase.</param>
        /// <param name="rerunFormulaAtReset">If to recalculate the BaseValue from the Formula at reset.</param>
        /// <param name="rerunFormulaAtEveryAccess">If to recalculate the BaseValue from the Formula each time the CurrentValue is queried.</param>
        /// /// <param name="parentBinder">Parent Element Binder.</param>
        public FormulaElement (string formulaString, DiagramMapping mapping, bool rerunFormulaAtReset = true,
            bool rerunFormulaAtEveryAccess = true,
            ElementBinder parentBinder = null) :
            base(-1, mapping, parentBinder)
        {
            RerunFormulaAtReset = rerunFormulaAtReset;
            RerunFormulaAtEveryAccess = rerunFormulaAtEveryAccess;
            InitFromFormulaString(formulaString);
        }

        private void InitFromFormulaString (string formulaString)
        {
            FormulaString = formulaString;
            try
            {
                MFormula = new MnFormula(FormulaString);
            }
            catch (Exception e)
            {
                throw e;
            }

            BaseValue = MFormula.Run();
            base.Reset();
        }

        /// <summary>
        /// The Current Value of this Element. May change depending on <see cref="RerunFormulaAtReset"/> and
        /// <see cref="RerunFormulaAtEveryAccess"/>.
        /// </summary>
        [XmlIgnore]
        override public int CurrentValue
        {
            get
            {
                if (RerunFormulaAtEveryAccess)
                {
                    BaseValue = MFormula.Run();
                    base.Reset();
                }

                return _currentValue;
            }
            protected set
            {
                _currentValue = value;
                _serializableValue = value;
            }
        }

        /// <summary>
        /// Resets the Current Value to the Base Value, potentially re-running the Formula.
        /// </summary>
        override protected void Reset ()
        {
            if (RerunFormulaAtReset)
                BaseValue = MFormula.Run();
            base.Reset();
        }

        /// <summary>
        /// Changes the Value of the FormulaElement to the given one.
        /// IMPORTANT: it overrides the ElementBase function. This will set the BaseValue and trigger base.Reset().
        /// </summary>
        /// <param name="value">New Value.</param>
        override public void ChangeValueTo (int value)
        {
            //Since the value is changed to an int (can happen when set from the editor!), putting it directly INSTEAD OF formula.
            //In the future, when we allow for the Formula to be edited, this functionality may be changed.
            InitFromFormulaString(value.ToString());
        }

        /// <summary>
        /// Ovewrites this ELementBase's data with the provided one.
        /// </summary>
        /// <param name="with">New data.</param>
        override public void Overwrite (ElementBase with)
        {
            InitFromFormulaString(((FormulaElement) with).FormulaString);
        }

        /// <summary>
        /// Returns a duplicate of this Element Base. Required in <see cref="MnDataLayer"/> in CreateElement.
        /// </summary>
        /// <returns></returns>
        override public ElementBase Clone (ElementBinder parentBinder)
        {
            return new FormulaElement(FormulaString, DiagMapping, RerunFormulaAtReset, RerunFormulaAtEveryAccess, parentBinder);
        }

        override public string ToString ()
        {
            return "FormulaElement: Formula: " + MFormula + "CurrentValue: " + CurrentValue + " BaseValue: " + BaseValue + " MinValue: " +
                   MinValue + " MaxValue: " + MaxValue;
        }

    }
}