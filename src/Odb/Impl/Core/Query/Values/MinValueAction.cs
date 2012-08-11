using System;
using NDatabase.Odb.Core.Layers.Layer2.Meta;
using NDatabase.Odb.Core.Query.Execution;
using NDatabase.Odb.Core.Query.Values;

namespace NDatabase.Odb.Impl.Core.Query.Values
{
    /// <summary>
    ///   An action to compute the max value of a field
    /// </summary>
    /// <author>osmadja</author>
    [Serializable]
    public class MinValueAction : AbstractQueryFieldAction
    {
        private Decimal _minValue;
        private OID _oidOfMinValues;

        public MinValueAction(string attributeName, string alias) : base(attributeName, alias, false)
        {
            _minValue = new Decimal(long.MaxValue);
            _oidOfMinValues = null;
        }

        public override void Execute(OID oid, AttributeValuesMap values)
        {
            var number = (Decimal) values[AttributeName];
            var bd = ValuesUtil.Convert(number);
            if (_minValue.CompareTo(bd) > 0)
            {
                _oidOfMinValues = oid;
                _minValue = bd;
            }
        }

        public override object GetValue()
        {
            return _minValue;
        }

        public override void End()
        {
        }

        public override void Start()
        {
        }

        public virtual OID GetOidOfMinValues()
        {
            return _oidOfMinValues;
        }

        public override IQueryFieldAction Copy()
        {
            return new MinValueAction(AttributeName, Alias);
        }
    }
}
