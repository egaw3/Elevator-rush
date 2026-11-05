using UnityEngine;

namespace ElevatorRush
{
    /// <summary>
    /// Keeps icons, bars and rings facing the camera. Bootstrap sets <see cref="Target"/> once.
    /// </summary>
    public sealed class Billboard : MonoBehaviour
    {
        public static Transform Target;

        void LateUpdate()
        {
            if (Target != null)
                transform.rotation = Target.rotation;
        }
    }
}
