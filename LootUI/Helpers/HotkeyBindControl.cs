using System.Collections.Generic;
using System.Text;
using BepInEx.Configuration;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LootManager
{
    public sealed class HotkeyBindControl : MonoBehaviour, IPointerClickHandler
    {
        [Header("UI")]
        [SerializeField] private TextMeshProUGUI labelText;
        [SerializeField] private Behaviour listeningHighlight;

        [Header("Strings")]
        [SerializeField] private string listeningPrompt = "Press a key...";
        [SerializeField] private string unboundText = "Unbound";

        private bool _listening;

        private System.Func<KeyboardShortcut> _getter;
        private System.Action<KeyboardShortcut> _setter;
        private System.Action _saver;

        public void Configure(System.Func<KeyboardShortcut> getter, System.Action<KeyboardShortcut> setter, System.Action saver)
        {
            _getter = getter;
            _setter = setter;
            _saver  = saver;
            RefreshLabel();
            SetListeningVisual(false);
        }

        public void SetLabel(TextMeshProUGUI label) => labelText = label;
        public void SetListeningHighlight(Behaviour outline) => listeningHighlight = outline;

        private void Awake()
        {
            RefreshLabel();
            SetListeningVisual(false);
        }

        private void OnEnable()
        {
            RefreshLabel();
            SetListeningVisual(false);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            BeginListening();
        }

        public void BeginListening()
        {
            _listening = true;
            SetListeningVisual(true);
            if (labelText) labelText.text = listeningPrompt;
            EventSystem.current?.SetSelectedGameObject(gameObject);
        }

        public void CancelListening()
        {
            _listening = false;
            SetListeningVisual(false);
            RefreshLabel();
        }

        private void SetListeningVisual(bool on)
        {
            if (listeningHighlight != null) listeningHighlight.enabled = on;
        }

        private void RefreshLabel()
        {
            if (!labelText) return;
            var ks = _getter != null ? _getter() : default;
            labelText.text = ToHumanString(ks);
        }

        private void Update()
        {
            if (!_listening) return;
			if (GameData.PlayerTyping || TypingInputMute.IsAnyActive) return;
          

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                CancelListening();
                return;
            }

            if (Input.GetKeyDown(KeyCode.Backspace) || Input.GetKeyDown(KeyCode.Delete))
            {
                SetBinding(default);
                CancelListening();
                return;
            }

            foreach (KeyCode key in System.Enum.GetValues(typeof(KeyCode)))
            {
                if (Input.GetKeyDown(key))
                {
                    if (key == KeyCode.LeftControl || key == KeyCode.RightControl ||
                        key == KeyCode.LeftShift || key == KeyCode.RightShift ||
                        key == KeyCode.LeftAlt || key == KeyCode.RightAlt ||
                        key == KeyCode.LeftCommand || key == KeyCode.RightCommand ||
                        key == KeyCode.LeftWindows || key == KeyCode.RightWindows ||
                        key == KeyCode.None)
                        continue;

                    var mods = new List<KeyCode>(4);
                    if (Input.GetKey(KeyCode.LeftControl))  mods.Add(KeyCode.LeftControl);
                    if (Input.GetKey(KeyCode.RightControl)) mods.Add(KeyCode.RightControl);
                    if (Input.GetKey(KeyCode.LeftShift))    mods.Add(KeyCode.LeftShift);
                    if (Input.GetKey(KeyCode.RightShift))   mods.Add(KeyCode.RightShift);
                    if (Input.GetKey(KeyCode.LeftAlt))      mods.Add(KeyCode.LeftAlt);
                    if (Input.GetKey(KeyCode.RightAlt))     mods.Add(KeyCode.RightAlt);
                    if (Input.GetKey(KeyCode.LeftCommand))  mods.Add(KeyCode.LeftCommand);
                    if (Input.GetKey(KeyCode.RightCommand)) mods.Add(KeyCode.RightCommand);
                    if (Input.GetKey(KeyCode.LeftWindows))  mods.Add(KeyCode.LeftWindows);
                    if (Input.GetKey(KeyCode.RightWindows)) mods.Add(KeyCode.RightWindows);

                    KeyboardShortcut ks;
                    switch (mods.Count)
                    {
                        case 0:
                            ks = new KeyboardShortcut(key);
                            break;
                        case 1:
                            ks = new KeyboardShortcut(key, mods[0]);
                            break;
                        case 2:
                            ks = new KeyboardShortcut(key, mods[0], mods[1]);
                            break;
                        case 3:
                            ks = new KeyboardShortcut(key, mods[0], mods[1], mods[2]);
                            break;
                        default:
                            ks = new KeyboardShortcut(key, mods[0], mods[1], mods[2], mods[3]);
                            break;
                    }

                    SetBinding(ks);
                    CancelListening();
                    break;
                }
            }
        }

        private void SetBinding(KeyboardShortcut ks)
        {
            if (_setter == null) return;
            _setter(ks);
            _saver?.Invoke();
            RefreshLabel();
        }

        private string ToHumanString(KeyboardShortcut ks)
        {
            if (ks.MainKey == KeyCode.None)
                return unboundText;

            var sb = new StringBuilder(32);

            if (ks.Modifiers != null)
            {
                foreach (var m in ks.Modifiers)
                {
                    if (m == KeyCode.None) continue;
                    if (sb.Length > 0) sb.Append(" + ");
                    sb.Append(NiceKey(m));
                }
            }

            if (sb.Length > 0) sb.Append(" + ");
            sb.Append(NiceKey(ks.MainKey));
            return sb.ToString();
        }

        private static string NiceKey(KeyCode code)
        {
            switch (code)
            {
                case KeyCode.LeftControl:  return "LeftCtrl";
                case KeyCode.RightControl: return "RightCtrl";
                case KeyCode.LeftAlt:      return "LeftAlt";
                case KeyCode.RightAlt:     return "RightAlt";
                case KeyCode.LeftShift:    return "LeftShift";
                case KeyCode.RightShift:   return "RightShift";
                case KeyCode.LeftCommand:  return "LeftCmd";
                case KeyCode.RightCommand: return "RightCmd";
                case KeyCode.LeftWindows:  return "LeftWin";
                case KeyCode.RightWindows: return "RightWin";
                default: return code.ToString();
            }
        }
    }
}
