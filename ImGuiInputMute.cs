using UnityEngine;

namespace LootManager
{
    /// <summary>
    /// Watches ImGuiRenderer.WantTextInput each frame.
    /// While an ImGui input field is focused:
    ///   - Sets GameData.PlayerTyping = true  (blocks game hotkeys)
    ///   - Zeros InputManager movement keys    (stops the player walking)
    ///   - Disables CanMove                    (belt-and-suspenders)
    /// Restores everything the frame after focus is lost.
    /// </summary>
    internal sealed class ImGuiInputMute : MonoBehaviour
    {
        internal ImGuiRenderer Renderer;

        private bool _wasMuted;

        // Cached original key bindings
        private static bool    _haveCached;
        private static KeyCode _fwd, _bwd, _left, _right, _strafeL, _strafeR, _jump, _map;

        private void Update()
        {
            bool wantText = Renderer != null && Renderer.WantTextInput;

            if (wantText && !_wasMuted)
                Mute();
            else if (!wantText && _wasMuted)
                Unmute();
        }

        private void OnDisable()
        {
            if (_wasMuted) Unmute();
        }

        private void Mute()
        {
            _wasMuted = true;

            GameData.PlayerTyping = true;

            if (GameData.PlayerControl != null)
            {
                GameData.PlayerControl.CanMove = false;
                GameData.PlayerControl.Autorun = false;
            }

            CacheKeysIfNeeded();

            InputManager.Forward  = KeyCode.None;
            InputManager.Backward = KeyCode.None;
            InputManager.Left     = KeyCode.None;
            InputManager.Right    = KeyCode.None;
            InputManager.StrafeL  = KeyCode.None;
            InputManager.StrafeR  = KeyCode.None;
            InputManager.Jump     = KeyCode.None;
            InputManager.Map      = KeyCode.None;
        }

        private void Unmute()
        {
            _wasMuted = false;

            // Only clear PlayerTyping if the game's own text box isn't open
            if (!(GameData.TextInput?.InputBox?.activeSelf ?? false))
            {
                GameData.PlayerTyping = false;
                if (GameData.PlayerControl != null)
                    GameData.PlayerControl.CanMove = true;
            }

            if (_haveCached)
            {
                InputManager.Forward  = _fwd;
                InputManager.Backward = _bwd;
                InputManager.Left     = _left;
                InputManager.Right    = _right;
                InputManager.StrafeL  = _strafeL;
                InputManager.StrafeR  = _strafeR;
                InputManager.Jump     = _jump;
                InputManager.Map      = _map;
            }
        }

        private static void CacheKeysIfNeeded()
        {
            if (_haveCached) return;

            _fwd     = InputManager.Forward;
            _bwd     = InputManager.Backward;
            _left    = InputManager.Left;
            _right   = InputManager.Right;
            _strafeL = InputManager.StrafeL;
            _strafeR = InputManager.StrafeR;
            _jump    = InputManager.Jump;
            _map     = InputManager.Map;

            _haveCached = true;
        }
    }
}