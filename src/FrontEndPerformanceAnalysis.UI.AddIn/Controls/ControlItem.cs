using System;
using System.Collections.Generic;
using System.Linq;

namespace MyLoadTest.LoadRunnerFrontEndPerformanceAnalysis.UI.AddIn.Controls
{
    internal sealed class ControlItem<T> : IEquatable<ControlItem<T>>
    {
        #region Constants and Fields

        private static readonly EqualityComparer<T> ValueComparer = EqualityComparer<T>.Default;

        #endregion

        #region Constructors

        public ControlItem(T value, string text)
        {
            Value = value;
            Text = text;
        }

        public ControlItem(T value)
            : this(value, value.ToStringSafelyInvariant())
        {
            // Nothing to do
        }

        #endregion

        #region Public Properties

        public T Value
        {
            get;
            private set;
        }

        public string Text
        {
            get;
            private set;
        }

        #endregion

        #region Public Methods

        public override bool Equals(object obj)
        {
            return Equals(obj as ControlItem<T>);
        }

        public override int GetHashCode()
        {
            return ValueComparer.GetHashCode(Value);
        }

        public override string ToString()
        {
            return Text;
        }

        #endregion

        #region IEquatable<ControlItem<T>> Members

        public bool Equals(ControlItem<T> other)
        {
            if (ReferenceEquals(other, null))
            {
                return false;
            }

            if (ReferenceEquals(other, this))
            {
                return true;
            }

            return ValueComparer.Equals(other.Value, Value);
        }

        #endregion
    }
}