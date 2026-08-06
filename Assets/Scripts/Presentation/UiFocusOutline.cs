using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace ShadowGarden.Presentation
{
    /// <summary>Shows the mint focus treatment while EventSystem selection points at this button.</summary>
    public sealed class UiFocusOutline : MonoBehaviour,
        ISelectHandler,
        IDeselectHandler,
        IPointerEnterHandler,
        IPointerClickHandler,
        ISubmitHandler
    {
        private Outline _outline;
        private MainGameplayHost _gameplay;

        private void Awake()
        {
            _outline = GetComponent<Outline>();
            _gameplay = Object.FindFirstObjectByType<MainGameplayHost>();
        }

        private void OnEnable()
        {
            Sync();
        }

        public void OnSelect(BaseEventData eventData)
        {
            Set(true);
            if (NavigationPressedThisFrame()) Gameplay?.PlayUiMove();
        }

        public void OnDeselect(BaseEventData eventData) => Set(false);

        public void OnPointerEnter(PointerEventData eventData)
        {
            // Keep keyboard/gamepad selection in sync with hover so ColorTint selected + outline show.
            if (EventSystem.current != null &&
                EventSystem.current.currentSelectedGameObject != gameObject)
            {
                EventSystem.current.SetSelectedGameObject(gameObject);
            }

            Gameplay?.PlayUiMove();
        }

        public void OnPointerClick(PointerEventData eventData) => Gameplay?.PlayUiSubmit();

        public void OnSubmit(BaseEventData eventData) => Gameplay?.PlayUiSubmit();

        private MainGameplayHost Gameplay =>
            _gameplay != null ? _gameplay : _gameplay = Object.FindFirstObjectByType<MainGameplayHost>();

        private static bool NavigationPressedThisFrame()
        {
            var keyboard = Keyboard.current;
            return keyboard != null &&
                   (keyboard.wKey.wasPressedThisFrame || keyboard.aKey.wasPressedThisFrame ||
                    keyboard.sKey.wasPressedThisFrame || keyboard.dKey.wasPressedThisFrame ||
                    keyboard.upArrowKey.wasPressedThisFrame || keyboard.downArrowKey.wasPressedThisFrame ||
                    keyboard.leftArrowKey.wasPressedThisFrame || keyboard.rightArrowKey.wasPressedThisFrame);
        }

        private void Sync()
        {
            var selected = EventSystem.current != null &&
                           EventSystem.current.currentSelectedGameObject == gameObject;
            Set(selected);
        }

        private void Set(bool on)
        {
            if (_outline == null)
            {
                _outline = GetComponent<Outline>();
            }

            if (_outline != null)
            {
                _outline.enabled = on;
            }

            var frame = transform.Find("FocusFrame");
            if (frame != null)
            {
                frame.gameObject.SetActive(on);
            }
        }
    }
}
