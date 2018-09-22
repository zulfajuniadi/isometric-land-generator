using System.Collections.Generic;
using UnityEngine;

namespace TilemapGenerator
{
    public interface ITileRenderer : System.IDisposable
    {
        void AddInstances(IEnumerable<Vector4> instances);
        void RemoveInstances(IEnumerable<Vector4> instances);
        void Tick();
    }
}
