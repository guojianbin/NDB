namespace NDatabase2.Odb.Core.Trigger
{
    /// <summary>
    ///   A simple base class for all triggers.
    /// </summary>
    public abstract class Trigger
    {
        public virtual IOdb Odb { get; set; }
    }
}
