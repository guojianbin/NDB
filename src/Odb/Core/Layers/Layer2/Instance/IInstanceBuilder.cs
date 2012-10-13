using NDatabase2.Odb.Core.Layers.Layer2.Meta;
using NDatabase2.Odb.Core.Layers.Layer3;

namespace NDatabase2.Odb.Core.Layers.Layer2.Instance
{
    internal interface IInstanceBuilder
    {
        /// <summary>
        ///   Builds a Non Native Object instance
        /// </summary>
        /// <returns> The instance </returns>
        object BuildOneInstance(NonNativeObjectInfo objectInfo, IOdbCache cache);

        /// <summary>
        ///   Returns the session id of this instance builder (odb database identifier)
        /// </summary>
        string GetSessionId();

        object BuildOneInstance(NonNativeObjectInfo objectInfo);
    }
}