using System;
using NDatabase.Odb.Core.Layers.Layer2.Meta;
using NDatabase.Odb.Core.Query.Execution;
using NDatabase.Odb.Core.Query.Values;
using NDatabase.Tool.Wrappers;

namespace NDatabase.Odb.Impl.Core.Query.Values
{
    /// <summary>
    ///   An action to count objects of a query
    /// </summary>
    /// <author>osmadja</author>
    [Serializable]
    public class CountAction : AbstractQueryFieldAction
    {
        private static readonly Decimal One = NDatabaseNumber.NewBigInteger(1);

        private Decimal _count;

        public CountAction(string alias) : base(alias, alias, false)
        {
            _count = NDatabaseNumber.NewBigInteger(0);
        }

        public override void Execute(OID oid, AttributeValuesMap values)
        {
            _count = NDatabaseNumber.Add(_count, One);
        }

        public virtual Decimal GetCount()
        {
            return _count;
        }

        public override object GetValue()
        {
            return _count;
        }

        public override void End()
        {
        }

        // Nothing to do
        public override void Start()
        {
        }

        // Nothing to do
        public override IQueryFieldAction Copy()
        {
            return new CountAction(Alias);
        }
    }
}
