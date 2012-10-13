using NDatabase2.Odb.Core.Layers.Layer2.Meta;

namespace NDatabase2.Odb.Core.Query.Execution
{
    internal sealed class EmptyExecutionPlan : IQueryExecutionPlan
    {
        #region IQueryExecutionPlan Members

        public void End()
        {
        }

        public string GetDetails()
        {
            return "empty plan";
        }

        public long GetDuration()
        {
            return 0;
        }

        public ClassInfoIndex GetIndex()
        {
            return null;
        }

        public void Start()
        {
        }

        public bool UseIndex()
        {
            return false;
        }

        #endregion
    }
}
