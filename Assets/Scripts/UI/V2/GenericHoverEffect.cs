using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;

namespace Sveliaty.UI.V2
{
    /// <summary>
    /// Componente genérico para añadir un efecto de hover (escalado) a cualquier elemento de UI usando DOTween.
    /// </summary>
    public class GenericHoverEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("Settings")]
        public float hoverScale = 1.05f;
        public float duration = 0.2f;
        public Ease easeType = Ease.OutQuad;

        private Vector3 _originalScale;
        private Tween _currentTween;

        private void Awake()
        {
            _originalScale = transform.localScale;
            // Si el objeto empieza con escala 0 (por una animación de entrada), 
            // asumimos que su escala base real debería ser 1.
            if (_originalScale.sqrMagnitude < 0.001f)
            {
                _originalScale = Vector3.one;
            }
        }

        public void SetOriginalScale(Vector3 scale)
        {
            _originalScale = scale;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _currentTween?.Kill();
            _currentTween = transform.DOScale(_originalScale * hoverScale, duration)
                .SetEase(easeType)
                .SetUpdate(true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _currentTween?.Kill();
            _currentTween = transform.DOScale(_originalScale, duration)
                .SetEase(easeType)
                .SetUpdate(true);
        }

        private void OnDisable()
        {
            _currentTween?.Kill();
            transform.localScale = _originalScale;
        }
    }
}
