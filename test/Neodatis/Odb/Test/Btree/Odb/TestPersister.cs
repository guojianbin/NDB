using NDatabase.Btree;
using NDatabase.Btree.Impl;
using NDatabase.Odb.Impl.Core.Btree;
using NDatabase.Odb.Impl.Core.Layers.Layer3.Engine;
using NUnit.Framework;
using Test.Odb.Test;

namespace Btree.Odb
{
    [TestFixture]
    public class TestPersister : ODBTest
    {
        /// <exception cref="System.Exception"></exception>
        [Test]
        public virtual void Test1()
        {
            DeleteBase("btree45.neodatis");
            var odb = Open("btree45.neodatis");
            var storageEngine = Dummy.GetEngine(odb);
            var persister = new LazyOdbBtreePersister(storageEngine);
            IBTreeMultipleValuesPerKey tree = new OdbBtreeMultiple("t", 3, persister);
            tree.Insert(1, new MyObject("Value 1"));
            tree.Insert(20, new MyObject("Value 20"));
            tree.Insert(25, new MyObject("Value 25"));
            tree.Insert(29, new MyObject("Value 29"));
            tree.Insert(21, new MyObject("Value 21"));
            AssertEquals(5, tree.GetRoot().GetNbKeys());
            AssertEquals(0, tree.GetRoot().GetNbChildren());
            AssertEquals(21, tree.GetRoot().GetMedian().GetKey());
            AssertEquals("[Value 21]", tree.GetRoot().GetMedian().GetValue().ToString());
            AssertEquals(0, tree.GetRoot().GetNbChildren());
            // println(tree.getRoot());
            tree.Insert(45, new MyObject("Value 45"));
            AssertEquals(2, tree.GetRoot().GetNbChildren());
            AssertEquals(1, tree.GetRoot().GetNbKeys());
            AssertEquals(21, tree.GetRoot().GetKeyAt(0));
            AssertEquals("[Value 21]", tree.GetRoot().GetValueAsObjectAt(0).ToString());
            persister.Close();
            odb = Open("btree45.neodatis");
            storageEngine = Dummy.GetEngine(odb);
            persister = new LazyOdbBtreePersister(storageEngine);
            tree = (IBTreeMultipleValuesPerKey) persister.LoadBTree(tree.GetId());
            AssertEquals(6, tree.GetSize());
            // println(tree.getRoot());
            var o = (MyObject) tree.Search(20)[0];
            AssertEquals("Value 20", o.GetName());
            o = (MyObject) tree.Search(29)[0];
            AssertEquals("Value 29", o.GetName());
            o = (MyObject) tree.Search(45)[0];
            AssertEquals("Value 45", o.GetName());
            odb.Close();
            DeleteBase("btree45.neodatis");
        }

        /// <exception cref="System.Exception"></exception>
        [Test]
        public virtual void TestDirectSave()
        {
            DeleteBase("btree46.neodatis");
            var odb = Open("btree46.neodatis");
            IBTree tree = new OdbBtreeMultiple("t", 3, new InMemoryPersister());
            IBTreeNodeMultipleValuesPerKey node = new OdbBtreeNodeMultiple(tree);
            odb.Store(node);
            for (var i = 0; i < 4; i++)
            {
                node.SetKeyAndValueAt(new KeyAndValue(i + 1, "String" + (i + 1)), i);
                odb.Store(node);
            }
            odb.Close();
            DeleteBase("btree46.neodatis");
        }
    }

    internal class MyObject
    {
        private readonly string name;

        public MyObject(string name)
        {
            this.name = name;
        }

        public virtual string GetName()
        {
            return name;
        }

        public override string ToString()
        {
            return name;
        }
    }
}
