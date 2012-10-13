using NDatabase2.Odb.Core.Layers.Layer2.Meta;
using NDatabase2.Odb.Core.Trigger;

namespace NDatabase2.Odb.Core.Layers.Layer3.Engine
{
    internal interface INonNativeObjectWriter
    {
        /// <param name="oid"> The Oid of the object to be inserted </param>
        /// <param name="nnoi"> The object meta representation The object to be inserted in the database </param>
        /// <param name="isNewObject"> To indicate if object is new </param>
        /// <returns> The position of the inserted object </returns>
        OID InsertNonNativeObject(OID oid, NonNativeObjectInfo nnoi, bool isNewObject);

        OID WriteNonNativeObjectInfo(OID existingOid, NonNativeObjectInfo objectInfo, long position,
                                     bool writeDataInTransaction, bool isNewObject);

        void SetTriggerManager(ITriggerManager triggerManager);

        /// <summary>
        ///   Updates an object.
        /// </summary>
        /// <remarks>
        ///   Updates an object. <pre>Try to update in place. Only change what has changed. This is restricted to particular types (fixed size types). If in place update is
        ///                        not possible, then deletes the current object and creates a new at the end of the database file and updates
        ///                        OID object position.
        ///                        &#064;param object The object to be updated
        ///                        &#064;param forceUpdate when true, no verification is done to check if update must be done.
        ///                        &#064;return The oid of the object, as a negative number
        ///                        &#064;</pre>
        /// </remarks>
        OID UpdateNonNativeObjectInfo(NonNativeObjectInfo nnoi, bool forceUpdate);

        void AfterInit();
    }
}