using System;
using System.Collections;
using NDatabase.Btree;
using NDatabase.Odb.Core.Layers.Layer2.Meta;
using NDatabase.Odb.Core.Layers.Layer3;
using NDatabase.Odb.Core.Transaction;
using NDatabase.Odb.Impl.Core.Btree;
using NDatabase.Odb.Impl.Tool;
using NDatabase.Tool;
using NDatabase.Tool.Wrappers;

namespace NDatabase.Odb.Core.Query.Execution
{
    /// <summary>
    ///   <p>Generic query executor</p> .
    /// </summary>
    /// <remarks>
    ///   <p>Generic query executor. This class does all the job of iterating in the
    ///     object list and call particular query matching to check if the object must be
    ///     included in the query result.</p> <p>If the query has index, An execution plan is calculated to optimize the
    ///                                         execution. The query execution plan is calculated by subclasses (using
    ///                                         abstract method getExecutionPlan).</P>
    /// </remarks>
    public abstract class GenericQueryExecutor : IMultiClassQueryExecutor
    {
        public static readonly string LogId = "GenericQueryExecutor";
        
        /// <summary>
        ///   The class of the object being fetched
        /// </summary>
        protected ClassInfo ClassInfo;

        protected NonNativeObjectInfo CurrentNnoi;
        protected OID CurrentOid;

        /// <summary>
        ///   The next object position
        /// </summary>
        protected OID NextOID;

        /// <summary>
        ///   The object used to read object data from database
        /// </summary>
        protected IObjectReader ObjectReader;

        /// <summary>
        ///   The query being executed
        /// </summary>
        protected IQuery Query;

        /// <summary>
        ///   The current database session
        /// </summary>
        protected ISession Session;

        /// <summary>
        ///   The storage engine
        /// </summary>
        protected IStorageEngine StorageEngine;

        /// <summary>
        ///   Used for multi class executor to indicate not to execute start and end method of query result action
        /// </summary>
        private bool _executeStartAndEndOfQueryAction;

        /// <summary>
        ///   The key for ordering
        /// </summary>
        private IOdbComparable _orderByKey;

        /// <summary>
        ///   A boolean to indicate if query must be ordered
        /// </summary>
        private bool _queryHasOrderBy;

        protected GenericQueryExecutor(IQuery query, IStorageEngine engine)
        {
            Query = query;
            StorageEngine = engine;
            ObjectReader = StorageEngine.GetObjectReader();
            Session = StorageEngine.GetSession(true);
            _executeStartAndEndOfQueryAction = true;
        }

        #region IMultiClassQueryExecutor Members

        public virtual IObjects<T> Execute<T>(bool inMemory, int startIndex, int endIndex, bool returnObjects,
                                              IMatchingObjectAction queryResultAction)
        {
            if (StorageEngine.IsClosed())
            {
                throw new OdbRuntimeException(
                    NDatabaseError.OdbIsClosed.AddParameter(StorageEngine.GetBaseIdentification().GetIdentification()));
            }

            if (Session.IsRollbacked())
                throw new OdbRuntimeException(NDatabaseError.OdbHasBeenRollbacked);

            // When used as MultiClass Executor, classInfo is already set
            if (ClassInfo == null)
            {
                // Class to execute query on
                var fullClassName = QueryManager.GetFullClassName(Query);

                // If the query class does not exist in meta model, return an empty
                // collection
                if (!Session.GetMetaModel().ExistClass(fullClassName))
                {
                    queryResultAction.Start();
                    queryResultAction.End();
                    Query.SetExecutionPlan(new EmptyExecutionPlan());
                    return queryResultAction.GetObjects<T>();
                }

                ClassInfo = Session.GetMetaModel().GetClassInfo(fullClassName, true);
            }

            // Get the query execution plan
            var executionPlan = GetExecutionPlan();
            executionPlan.Start();

            try
            {
                if (executionPlan.UseIndex() && OdbConfiguration.UseIndex())
                    return ExecuteUsingIndex<T>(executionPlan.GetIndex(), inMemory, startIndex, endIndex, returnObjects,
                                                queryResultAction);

                // When query must be applied to a single object
                if (Query.IsForSingleOid())
                    return ExecuteForOneOid<T>(inMemory, returnObjects, queryResultAction);

                return ExecuteFullScan<T>(inMemory, startIndex, endIndex, returnObjects, queryResultAction);
            }
            finally
            {
                executionPlan.End();
            }
        }

        public virtual bool ExecuteStartAndEndOfQueryAction()
        {
            return _executeStartAndEndOfQueryAction;
        }

        public virtual void SetExecuteStartAndEndOfQueryAction(bool yes)
        {
            _executeStartAndEndOfQueryAction = yes;
        }

        public virtual IStorageEngine GetStorageEngine()
        {
            return StorageEngine;
        }

        public virtual IQuery GetQuery()
        {
            return Query;
        }

        public virtual void SetClassInfo(ClassInfo classInfo)
        {
            ClassInfo = classInfo;
        }

        #endregion

        public abstract IQueryExecutionPlan GetExecutionPlan();

        public abstract void PrepareQuery();

        public abstract IComparable ComputeIndexKey(ClassInfo ci, ClassInfoIndex index);

        /// <summary>
        ///   This can be a NonNAtiveObjectInf or AttributeValuesMap
        /// </summary>
        /// <returns> </returns>
        public abstract object GetCurrentObjectMetaRepresentation();

        /// <summary>
        ///   Check if the object with oid matches the query, returns true This method must compute the next object oid and the orderBy key if it exists!
        /// </summary>
        /// <param name="oid"> The object position </param>
        /// <param name="loadObjectInfo"> To indicate if object must loaded (when the query indicator 'in memory' is false, we do not need to load object, only ids) </param>
        /// <param name="inMemory"> To indicate if object must be actually loaded to memory </param>
        public abstract bool MatchObjectWithOid(OID oid, bool loadObjectInfo, bool inMemory);

        /// <summary>
        ///   Query execution full scan <pre>startIndex &amp; endIndex
        ///                               A B C D E F G H I J K L
        ///                               [1,3] : nb &gt;=1 &amp;&amp; nb&lt;3
        ///                               1)
        ///                               analyze A
        ///                               nb = 0
        ///                               nb E [1,3] ? no
        ///                               r=[]
        ///                               2)
        ///                               analyze B
        ///                               nb = 1
        ///                               nb E [1,3] ? yes
        ///                               r=[B]
        ///                               3) analyze C
        ///                               nb = 2
        ///                               nb E [1,3] ? yes
        ///                               r=[B,C]
        ///                               4) analyze C
        ///                               nb = 3
        ///                               nb E [1,3] ? no and 3&gt; upperBound([1,3]) =&gt; exit</pre>
        /// </summary>
        /// <param name="inMemory"> </param>
        /// <param name="startIndex"> </param>
        /// <param name="endIndex"> </param>
        /// <param name="returnObjects"> </param>
        /// <param name="queryResultAction"> </param>
        private IObjects<T> ExecuteFullScan<T>(bool inMemory, int startIndex, int endIndex, bool returnObjects,
                                               IMatchingObjectAction queryResultAction)
        {
            if (StorageEngine.IsClosed())
            {
                throw new OdbRuntimeException(
                    NDatabaseError.OdbIsClosed.AddParameter(StorageEngine.GetBaseIdentification().GetIdentification()));
            }
            var nbObjects = ClassInfo.GetNumberOfObjects();

            if (OdbConfiguration.IsDebugEnabled(LogId))
                DLogger.Debug(string.Format("loading {0} instance(s) of {1}", nbObjects, ClassInfo.GetFullClassName()));

            if (ExecuteStartAndEndOfQueryAction())
                queryResultAction.Start();

            OID currentOID = null;

            // TODO check if all instances are in the cache! and then load from the cache
            NextOID = ClassInfo.GetCommitedZoneInfo().First;

            if (nbObjects > 0 && NextOID == null)
            {
                // This means that some changes have not been commited!
                // Take next position from uncommited zone
                NextOID = ClassInfo.GetUncommittedZoneInfo().First;
            }

            PrepareQuery();
            if (Query != null)
                _queryHasOrderBy = Query.HasOrderBy();

            var monitorMemory = OdbConfiguration.IsMonitoringMemory();
            // used when startIndex and endIndex are not negative
            var nbObjectsInResult = 0;

            for (var i = 0; i < nbObjects; i++)
            {
                //Console.WriteLine(i);
                if (monitorMemory && i % 10000 == 0)
                    MemoryMonitor.DisplayCurrentMemory(string.Empty + (i + 1), true);

                // Reset the order by key
                _orderByKey = null;
                var prevOID = currentOID;
                currentOID = NextOID;

                // This is an error
                if (currentOID == null)
                {
                    if (OdbConfiguration.ThrowExceptionWhenInconsistencyFound())
                    {
                        throw new OdbRuntimeException(
                            NDatabaseError.NullNextObjectOid.AddParameter(ClassInfo.GetFullClassName()).AddParameter(i).
                                AddParameter(nbObjects).AddParameter(prevOID));
                    }

                    break;
                }

                // If there is an endIndex condition
                if (endIndex != -1 && nbObjectsInResult >= endIndex)
                    break;

                // If there is a startIndex condition
                bool objectInRange;
                if (startIndex != -1 && nbObjectsInResult < startIndex)
                    objectInRange = false;
                else
                    objectInRange = true;

                // There is no query
                if (!inMemory && Query == null)
                {
                    nbObjectsInResult++;

                    // keep object position if we must
                    if (objectInRange)
                    {
                        _orderByKey = BuildOrderByKey(CurrentNnoi);
                        // TODO Where is the key for order by
                        queryResultAction.ObjectMatch(NextOID, _orderByKey);
                    }

                    NextOID = ObjectReader.GetNextObjectOID(currentOID);
                }
                else
                {
                    var objectMatches = MatchObjectWithOid(currentOID, returnObjects, inMemory);

                    if (objectMatches)
                    {
                        nbObjectsInResult++;

                        if (objectInRange)
                        {
                            if (_queryHasOrderBy)
                                _orderByKey = BuildOrderByKey(GetCurrentObjectMetaRepresentation());

                            queryResultAction.ObjectMatch(currentOID, GetCurrentObjectMetaRepresentation(), _orderByKey);
                        }
                    }
                }
            }

            if (ExecuteStartAndEndOfQueryAction())
                queryResultAction.End();

            return queryResultAction.GetObjects<T>();
        }

        /// <summary>
        ///   Execute query using index
        /// </summary>
        /// <param name="index"> </param>
        /// <param name="inMemory"> </param>
        /// <param name="startIndex"> </param>
        /// <param name="endIndex"> </param>
        /// <param name="returnObjects"> </param>
        /// <param name="queryResultAction"> </param>
        private IObjects<T> ExecuteUsingIndex<T>(ClassInfoIndex index, bool inMemory, int startIndex, int endIndex,
                                                 bool returnObjects, IMatchingObjectAction queryResultAction)
        {
            // Index that have not been used yet do not have persister!
            if (index.BTree.GetPersister() == null)
                index.BTree.SetPersister(new LazyOdbBtreePersister(StorageEngine));

            var nbObjects = ClassInfo.GetNumberOfObjects();
            var btreeSize = index.BTree.GetSize();

            // the two values should be equal
            if (nbObjects != btreeSize)
            {
                var classInfo = StorageEngine.GetSession(true).GetMetaModel().GetClassInfoFromId(index.ClassInfoId);

                throw new OdbRuntimeException(
                    NDatabaseError.IndexIsCorrupted.AddParameter(index.Name).AddParameter(classInfo.GetFullClassName()).
                        AddParameter(nbObjects).AddParameter(btreeSize));
            }

            if (OdbConfiguration.IsDebugEnabled(LogId))
                DLogger.Debug(string.Format("loading {0} instance(s) of {1}", nbObjects, ClassInfo.GetFullClassName()));

            if (ExecuteStartAndEndOfQueryAction())
                queryResultAction.Start();

            PrepareQuery();
            if (Query != null)
                _queryHasOrderBy = Query.HasOrderBy();

            var tree = index.BTree;
            var isUnique = index.IsUnique;

            // Iterator iterator = new BTreeIterator(tree,
            // OrderByConstants.ORDER_BY_ASC);
            var key = ComputeIndexKey(ClassInfo, index);
            IList list = null;

            // If index is unique, get the object
            if (isUnique)
            {
                var treeSingle = (IBTreeSingleValuePerKey) tree;
                var value = treeSingle.Search(key);
                if (value != null)
                    list = new ArrayList {value};
            }
            else
            {
                var treeMultiple = (IBTreeMultipleValuesPerKey) tree;
                list = treeMultiple.Search(key);
            }

            if (list != null)
            {
                foreach (OID oid in list)
                {
                    // FIXME Why calling this method
                    var position = ObjectReader.GetObjectPositionFromItsOid(oid, true, true);
                    _orderByKey = null;

                    var objectMatches = MatchObjectWithOid(oid, returnObjects, inMemory);
                    if (objectMatches)
                        queryResultAction.ObjectMatch(oid, GetCurrentObjectMetaRepresentation(), _orderByKey);
                }

                queryResultAction.End();
                return queryResultAction.GetObjects<T>();
            }

            if (ExecuteStartAndEndOfQueryAction())
                queryResultAction.End();

            return queryResultAction.GetObjects<T>();
        }

        /// <summary>
        ///   Execute query using index
        /// </summary>
        /// <param name="inMemory"> </param>
        /// <param name="returnObjects"> </param>
        /// <param name="queryResultAction"> </param>
        private IObjects<T> ExecuteForOneOid<T>(bool inMemory, bool returnObjects,
                                                IMatchingObjectAction queryResultAction)
        {
            if (OdbConfiguration.IsDebugEnabled(LogId))
            {
                DLogger.Debug(string.Format("loading Object with oid {0} - class {1}", Query.GetOidOfObjectToQuery(),
                                            ClassInfo.GetFullClassName()));
            }
            if (ExecuteStartAndEndOfQueryAction())
                queryResultAction.Start();

            PrepareQuery();
            var oid = Query.GetOidOfObjectToQuery();

            // FIXME Why calling this method
            var position = ObjectReader.GetObjectPositionFromItsOid(oid, true, true);
            var objectMatches = MatchObjectWithOid(oid, returnObjects, inMemory);

            queryResultAction.ObjectMatch(oid, GetCurrentObjectMetaRepresentation(), _orderByKey);
            queryResultAction.End();

            return queryResultAction.GetObjects<T>();
        }

        /// <summary>
        ///   TODO very bad.
        /// </summary>
        /// <remarks>
        ///   TODO very bad. Should remove the instanceof
        /// </remarks>
        /// <param name="object"> </param>
        /// <returns> </returns>
        public virtual IOdbComparable BuildOrderByKey(object @object)
        {
            var attributeValuesMap = @object as AttributeValuesMap;

            if (attributeValuesMap != null)
                return BuildOrderByKey(attributeValuesMap);

            return BuildOrderByKey((NonNativeObjectInfo) @object);
        }

        public virtual IOdbComparable BuildOrderByKey(NonNativeObjectInfo nnoi)
        {
            // TODO cache the attributes ids to compute them only once
            return IndexTool.BuildIndexKey("OrderBy", nnoi, QueryManager.GetOrderByAttributeIds(ClassInfo, Query));
        }

        public virtual IOdbComparable BuildOrderByKey(AttributeValuesMap values)
        {
            return IndexTool.BuildIndexKey("OrderBy", values, Query.GetOrderByFieldNames());
        }
    }
}
