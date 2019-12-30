using System;
using System.Collections.Generic;

namespace AutoRef
{
    class StringEqualityComparer : IEqualityComparer<string>
    {
        #region Fields

        StringComparison _stringComparison;

        #endregion

        #region Constructors

        private StringEqualityComparer(StringComparison stringComparison) => _stringComparison = stringComparison;

        #endregion

        #region Properties

        public static StringEqualityComparer OrdinalIgnoreCase => new StringEqualityComparer(StringComparison.OrdinalIgnoreCase);

        #endregion

        #region Methods

        public bool Equals(string x, string y) => x.Equals(y, _stringComparison);

        public int GetHashCode(string obj) => obj.GetHashCode();

        #endregion
    }
}
