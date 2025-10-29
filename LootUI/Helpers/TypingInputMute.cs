using UnityEngine;
using TMPro;

[DefaultExecutionOrder(-50000)] // run very early so PlayerTyping/CanMove are set before PlayerControl.Update()
public sealed class TypingInputeMute : MonoBehaviour
{
    [Header("Assign your TMP_InputField")]
    public TMP_InputField input;

    [Header("Optional: only active while this root is visible")]
    public GameObject windowRoot;

    [Header("Debug")]
    public bool log;

    // per-instance focus tracker
    private bool _wasFocused;

    // static ref-count to support multiple fields/components safely
    private static int s_activeMutes;

    // cached originals (shared) so we only save/restore once
    private static bool   s_haveCache;
    private static KeyCode sF, sB, sL, sR, sSL, sSR, sJump;

    void Update()
    {
        // If you want this to only apply when a specific UI is visible
        if (windowRoot != null && !windowRoot.activeInHierarchy)
        {
            // if we were muting, unmute now
            if (_wasFocused) TryUnmute();
            // clear flags unless the game's global text box is open
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
            // assert typing + no-move every frame while focused
            GameData.PlayerTyping = true;
            if (GameData.PlayerControl != null)
            {
                GameData.PlayerControl.CanMove = false;
                GameData.PlayerControl.Autorun  = false; // stop carry-over movement
            }

            if (!_wasFocused)
            {
                TryMute(); // first frame of focus: mute movement keys
                if (log) Debug.Log($"[TypingInputeMute] {input.name} FOCUSED — typing TRUE, CanMove FALSE, movement keys muted");
            }
        }
        else
        {
            if (_wasFocused)
            {
                TryUnmute(); // first frame after blur: restore keys
                if (!(GameData.TextInput?.InputBox?.activeSelf ?? false))
                {
                    GameData.PlayerTyping = false;
                    if (GameData.PlayerControl != null) GameData.PlayerControl.CanMove = true;
                }
                if (log) Debug.Log($"[TypingInputeMute] {input.name} UNFOCUSED — typing FALSE, CanMove TRUE, movement keys restored");
            }
        }

        _wasFocused = focused;
    }

    private void TryMute()
    {
        // Increase global mute count; only on the first mute do we cache/override InputManager keys.
        if (s_activeMutes == 0)
        {
            CacheOriginalKeysIfNeeded();

            // Mute only movement-related keys; leaves mouse/UI intact.
            InputManager.Forward  = KeyCode.None;
            InputManager.Backward = KeyCode.None;
            InputManager.Left     = KeyCode.None;
            InputManager.Right    = KeyCode.None;
            InputManager.StrafeL  = KeyCode.None;
            InputManager.StrafeR  = KeyCode.None;
            InputManager.Jump     = KeyCode.None;

            if (log) Debug.Log("[TypingInputeMute] Movement keys muted; autorun off");
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
                // Restore movement keys exactly once when the last focus releases
                InputManager.Forward  = sF;
                InputManager.Backward = sB;
                InputManager.Left     = sL;
                InputManager.Right    = sR;
                InputManager.StrafeL  = sSL;
                InputManager.StrafeR  = sSR;
                InputManager.Jump     = sJump;

                if (log) Debug.Log("[TypingInputeMute] Movement keys restored");
            }
        }
    }

    private static void CacheOriginalKeysIfNeeded()
    {
        if (s_haveCache) return;

        sF   = InputManager.Forward;
        sB   = InputManager.Backward;
        sL   = InputManager.Left;
        sR   = InputManager.Right;
        sSL  = InputManager.StrafeL;
        sSR  = InputManager.StrafeR;
        sJump= InputManager.Jump;

        s_haveCache = true;
    }

    void OnDisable()
    {
        // If this component gets disabled while focused, clean up safely.
        if (_wasFocused) TryUnmute();

        // Only clear typing if no other text box is open
        if (!(GameData.TextInput?.InputBox?.activeSelf ?? false))
            GameData.PlayerTyping = false;
    }
}
