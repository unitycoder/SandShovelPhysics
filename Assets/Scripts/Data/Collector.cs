using UnityEngine;

namespace DiggingTest
{
    public struct Collector
    {
        public Transform point;
        public Vector3 prevPos; // worldspace
        public Vector3 amount;
        public bool isUnderground;
        public bool hasSand;
    }
}
