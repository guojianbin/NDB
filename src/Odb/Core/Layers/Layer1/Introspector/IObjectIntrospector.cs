using System.Collections.Generic;
using NDatabase2.Odb.Core.Layers.Layer2.Meta;

namespace NDatabase2.Odb.Core.Layers.Layer1.Introspector
{
    /// <summary>
    ///   Interface for ObjectInstropector.
    /// </summary>
    internal interface IObjectIntrospector
    {
        /// <summary>
        ///   retrieve object data
        /// </summary>
        /// <param name="object"> The object to get meta representation </param>
        /// <param name="ci"> The ClassInfo </param>
        /// <param name="recursive"> To indicate that introspection must be recursive </param>
        /// <param name="alreadyReadObjects"> A map with already read object, to avoid cyclic reference problem </param>
        /// <param name="callback"> </param>
        /// <returns> The object info </returns>
        AbstractObjectInfo GetMetaRepresentation(object @object, ClassInfo ci, bool recursive,
                                                 IDictionary<object, NonNativeObjectInfo> alreadyReadObjects,
                                                 IIntrospectionCallback callback);

        void Clear();
    }
}
