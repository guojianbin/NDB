using System;

namespace NDatabase2.Btree
{
    /// <summary>
    ///   The interface for btree nodes that accept multiple values for each key
    /// </summary>
    public interface IBTreeNodeOneValuePerKey : IBTreeNode
    {
        object GetValueAt(int index);

        object Search(IComparable key);
    }
}