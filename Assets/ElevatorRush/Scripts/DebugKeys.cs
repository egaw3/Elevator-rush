using UnityEngine;
#if UNITY_EDITOR && ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace ElevatorRush
{
    /// <summary>
    /// Editor-only debug keys. F1 completes the current level, F2 then digits then Enter jumps to
    /// that level (Escape cancels), F3 resets the save, F4 adds 500 coins, F5 adds 5 completed
    /// levels, F6 adds 100 gems. Does nothing in player builds.
    /// </summary>
    public sealed class DebugKeys : MonoBehaviour
    {
#if UNITY_EDITOR && ENABLE_INPUT_SYSTEM
        const int MaxDigits = 5;
        const int DebugCoins = 500;
        const int DebugLevels = 5;
        const int DebugGems = 100;

        // Digit keys are not in 0-9 order in the Key enum, so list them explicitly.
        static readonly Key[] DigitKeys =
            { Key.Digit0, Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4, Key.Digit5, Key.Digit6, Key.Digit7, Key.Digit8, Key.Digit9 };
        static readonly Key[] NumpadKeys =
            { Key.Numpad0, Key.Numpad1, Key.Numpad2, Key.Numpad3, Key.Numpad4, Key.Numpad5, Key.Numpad6, Key.Numpad7, Key.Numpad8, Key.Numpad9 };

        GameManager game;
        bool typing;
        int typed;
        int digits;

        public void Init(GameManager gameManager)
        {
            game = gameManager;
        }

        void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null || game == null)
                return;

            if (typing)
            {
                ReadDigits(keyboard);
                return;
            }

            if (keyboard.f1Key.wasPressedThisFrame)
                game.DebugCompleteLevel();
            else if (keyboard.f2Key.wasPressedThisFrame)
            {
                typing = true;
                typed = 0;
                digits = 0;
                Debug.Log("[ElevatorRush] Type a level number and press Enter.");
            }
            else if (keyboard.f3Key.wasPressedThisFrame)
            {
                game.DebugResetSave();
                Debug.Log("[ElevatorRush] Save reset.");
            }
            else if (keyboard.f4Key.wasPressedThisFrame)
            {
                game.DebugAddCoins(DebugCoins);
                Debug.Log("[ElevatorRush] +" + DebugCoins + " coins.");
            }
            else if (keyboard.f5Key.wasPressedThisFrame)
            {
                game.DebugAddCompletedLevels(DebugLevels);
                Debug.Log("[ElevatorRush] +" + DebugLevels + " completed levels.");
            }
            else if (keyboard.f6Key.wasPressedThisFrame)
            {
                game.DebugAddGems(DebugGems);
                Debug.Log("[ElevatorRush] +" + DebugGems + " gems.");
            }
        }

        void ReadDigits(Keyboard keyboard)
        {
            if (keyboard.escapeKey.wasPressedThisFrame)
            {
                typing = false;
                return;
            }
            if (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame)
            {
                typing = false;
                if (digits > 0)
                {
                    Debug.Log("[ElevatorRush] Jumping to level " + typed);
                    game.DebugJumpToLevel(typed);
                }
                return;
            }
            for (int d = 0; d <= 9; d++)
            {
                if (digits >= MaxDigits)
                    break;
                if (keyboard[DigitKeys[d]].wasPressedThisFrame || keyboard[NumpadKeys[d]].wasPressedThisFrame)
                {
                    typed = typed * 10 + d;
                    digits++;
                }
            }
        }
#else
        public void Init(GameManager gameManager)
        {
        }
#endif
    }
}
