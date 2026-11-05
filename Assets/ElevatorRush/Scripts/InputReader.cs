using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace ElevatorRush
{
    /// <summary>
    /// The game's single input: hold anywhere to rise. Presses that start on UI do not count.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public sealed class InputReader : MonoBehaviour
    {
        public bool IsHolding { get; private set; }

        bool wasPointerPressed;
        bool pressStartedOnUi;

        void Update()
        {
            bool pointerPressed = ReadPointer();
            if (pointerPressed && !wasPointerPressed)
                pressStartedOnUi = IsPointerOverUi();
            if (!pointerPressed)
                pressStartedOnUi = false;
            wasPointerPressed = pointerPressed;

            IsHolding = (pointerPressed && !pressStartedOnUi) || ReadKey();
        }

        static bool ReadPointer()
        {
#if ENABLE_INPUT_SYSTEM
            return Pointer.current != null && Pointer.current.press.isPressed;
#else
            return Input.GetMouseButton(0);
#endif
        }

        static bool ReadKey()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.spaceKey.isPressed;
#else
            return Input.GetKey(KeyCode.Space);
#endif
        }

        static bool IsPointerOverUi()
        {
            var eventSystem = EventSystem.current;
            return eventSystem != null && eventSystem.IsPointerOverGameObject();
        }
    }
}
