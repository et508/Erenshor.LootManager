using UnityEngine;
using TMPro;

namespace LootManager
{
    [DefaultExecutionOrder(-50000)]
    public sealed class TypingInputMute : MonoBehaviour
    {
        [Header("Assign your TMP_InputField")]
        public TMP_InputField input;

        [Header("Optional: only active while this root is visible")]
        public GameObject windowRoot;

        [Header("Debug")]
        public bool log;

        private bool _wasFocused;

        private static int  s_activeMutes;
        public  static bool IsAnyActive => s_activeMutes > 0;

        private static bool    s_haveCache;
        private static KeyCode sF, sB, sL, sR, sSL, sSR, sJump, sM;

        private void Update()
        {
            if (windowRoot != null && !windowRoot.activeInHierarchy)
            {
                if (_wasFocused) TryUnmute();

                if (!(GameData.TextInput?.InputBox?.activeSelf ?? false))
                {
                    if (GameData.PlayerTyping) GameData.PlayerTyping = false;
                    if (GameData.PlayerControl != null) GameData.PlayerControl.CanMove = true;
                }

                _wasFocused = false;
                return;
            }

            if (input == null) return;

            bool focused = input.isFocused;

            if (focused)
            {
                GameData.PlayerTyping = true;

                if (GameData.PlayerControl != null)
                {
                    GameData.PlayerControl.CanMove = false;
                    GameData.PlayerControl.Autorun = false;
                }

                if (!_wasFocused)
                {
                    TryMute();
                    if (log) Debug.Log($"[TypingInputMute] {input.name} FOCUSED — typing TRUE, CanMove FALSE, movement keys muted");
                }
            }
            else
            {
                if (_wasFocused)
                {
                    TryUnmute();

                    if (!(GameData.TextInput?.InputBox?.activeSelf ?? false))
                    {
                        GameData.PlayerTyping = false;
                        if (GameData.PlayerControl != null) GameData.PlayerControl.CanMove = true;
                    }

                    if (log) Debug.Log($"[TypingInputMute] {input.name} UNFOCUSED — typing FALSE, CanMove TRUE, movement keys restored");
                }
            }

            _wasFocused = focused;
        }

        private void TryMute()
        {
            if (s_activeMutes == 0)
            {
                CacheOriginalKeysIfNeeded();

                InputManager.Forward  = KeyCode.None;
                InputManager.Backward = KeyCode.None;
                InputManager.Left     = KeyCode.None;
                InputManager.Right    = KeyCode.None;
                InputManager.StrafeL  = KeyCode.None;
                InputManager.StrafeR  = KeyCode.None;
                InputManager.Jump     = KeyCode.None;
                InputManager.Map      = KeyCode.None;

                if (log) Debug.Log("[TypingInputMute] Movement keys muted; autorun off");
            }

            s_activeMutes++;
        }

        private void TryUnmute()
        {
            if (s_activeMutes > 0)
            {
                s_activeMutes--;
                if (s_activeMutes == 0)
                {
                    InputManager.Forward  = sF;
                    InputManager.Backward = sB;
                    InputManager.Left     = sL;
                    InputManager.Right    = sR;
                    InputManager.StrafeL  = sSL;
                    InputManager.StrafeR  = sSR;
                    InputManager.Jump     = sJump;
                    InputManager.Map      = sM;

                    if (log) Debug.Log("[TypingInputMute] Movement keys restored");
                }
            }
        }

        private static void CacheOriginalKeysIfNeeded()
        {
            if (s_haveCache) return;

            sF    = InputManager.Forward;
            sB    = InputManager.Backward;
            sL    = InputManager.Left;
            sR    = InputManager.Right;
            sSL   = InputManager.StrafeL;
            sSR   = InputManager.StrafeR;
            sJump = InputManager.Jump;
            sM    = InputManager.Map;

            s_haveCache = true;
        }

        private void OnDisable()
        {
            if (_wasFocused) TryUnmute();

            if (!(GameData.TextInput?.InputBox?.activeSelf ?? false))
                GameData.PlayerTyping = false;
        }
    }
}
