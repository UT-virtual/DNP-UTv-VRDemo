using UnityEngine;

namespace DNPDemo.Interaction.Selection
{
    /// <summary>One sphere of a range selection: where the pen tip was, and the selection radius at that moment. World space.</summary>
    public readonly struct SelectionSample
    {
        public readonly Vector3 Position;
        public readonly float Radius;

        public SelectionSample(Vector3 position, float radius)
        {
            Position = position;
            Radius = radius;
        }

        public override string ToString() => $"({Position.x:F3}, {Position.y:F3}, {Position.z:F3}) r={Radius:F3}";
    }
}
