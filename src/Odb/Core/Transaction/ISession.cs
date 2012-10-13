using System;
using NDatabase2.Odb.Core.Layers.Layer2.Meta;
using NDatabase2.Odb.Core.Layers.Layer3;

namespace NDatabase2.Odb.Core.Transaction
{
    internal interface ISession : IComparable
    {
        IOdbCache GetCache();

        IReadObjectsCache GetTmpCache();

        void Rollback();

        void Close();

        void ClearCache();

        bool IsRollbacked();

        void Clear();

        IStorageEngine GetStorageEngine();

        bool TransactionIsPending();

        void Commit();

        ITransaction GetTransaction();

        void SetFileSystemInterfaceToApplyTransaction(IFileSystemInterface fsi);

        string GetBaseIdentification();

        MetaModel GetMetaModel();

        void SetMetaModel(MetaModel metaModel2);

        string GetId();

        void RemoveObjectFromCache(object @object);

        /// <summary>
        ///   Add these information on a session cache.
        /// </summary>
        /// <remarks>
        ///   Add these information on a session cache.
        /// </remarks>
        void AddObjectToCache(OID oid, object @object, ObjectInfoHeader oih);
    }
}
