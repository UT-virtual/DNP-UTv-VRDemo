using UnityEngine;

namespace DNPDemo
{
    /// <summary>
    /// Put on a <c>[SerializeReference]</c> field (or list) to pick its concrete type from a dropdown in the inspector.
    /// Concrete types must be <c>[Serializable]</c>, non-abstract and have a parameterless constructor.
    /// </summary>
    public sealed class SubclassSelectorAttribute : PropertyAttribute
    {
    }
}
