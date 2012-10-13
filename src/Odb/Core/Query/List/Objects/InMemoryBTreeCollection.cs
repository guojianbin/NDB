using NDatabase2.Btree;
using NDatabase2.Btree.Impl.Multiplevalue;

namespace NDatabase2.Odb.Core.Query.List.Objects
{
    /// <summary>
    ///   An implementation of an ordered Collection based on a BTree implementation that holds all objects in memory
    /// </summary>
    /// <author>osmadja</author>
    
    public sealed class InMemoryBTreeCollection<T> : AbstractBTreeCollection<T>
    {
        public InMemoryBTreeCollection() : base(OrderByConstants.OrderByAsc)
        {
        }

        public InMemoryBTreeCollection(OrderByConstants orderByType) : base(orderByType)
        {
        }

        public override IBTree BuildTree(int degree)
        {
            return new InMemoryBTreeMultipleValuesPerKey("default", degree);
        }
    }
}
