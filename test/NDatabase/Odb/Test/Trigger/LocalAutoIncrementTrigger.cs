using System;
using System.Threading;
using NDatabase.Odb;
using NDatabase2.Odb;
using NDatabase2.Odb.Core;
using NDatabase2.Odb.Core.Query.Criteria;
using NDatabase2.Odb.Core.Trigger;

namespace Test.NDatabase.Odb.Test.Trigger
{
    public class LocalAutoIncrementTrigger : InsertTrigger
    {
        public override void AfterInsert(object @object, OID oid)
        {
        }

        // nothing
        public override bool BeforeInsert(object @object)
        {
            if (@object.GetType() != typeof (ObjectWithAutoIncrementId))
                return false;
            var o = (ObjectWithAutoIncrementId) @object;
            var mutex = new Mutex();
            try
            {
                try
                {
                    mutex.GetAccessControl();
                    var id = GetNextId("test");
                    o.SetId(id);
                    // System.out.println("setting new id "+ id);
                    return true;
                }
                catch (Exception e)
                {
                    throw new OdbRuntimeException(NDatabaseError.InternalError, e);
                }
            }
            finally
            {
                mutex.ReleaseMutex();
            }
        }

        /// <summary>
        ///   Actually gets the next id Gets the object of type ID from the database
        ///   with the specific name.
        /// </summary>
        /// <remarks>
        ///   Actually gets the next id Gets the object of type ID from the database
        ///   with the specific name. Then increment the id value and returns. If
        ///   object does not exist, creates t.
        /// </remarks>
        /// <param name="idName"> </param>
        /// <returns> </returns>
        private long GetNextId(string idName)
        {
            var odb = Odb;
            var objects = odb.GetObjects<ID>(new CriteriaQuery<ID>( Where.Equal("idName", idName)));
            if (objects.Count == 0)
            {
                var id1 = new ID(idName, 1);
                odb.Store(id1);
                return 1;
            }
            var id = objects.GetFirst();
            var lid = id.GetNext();
            odb.Store(id);
            return lid;
        }
    }
}
