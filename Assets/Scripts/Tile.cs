using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;
using DG.Tweening;

namespace FluencyDrive
{
    /// <summary>
    /// Represents an individual tile in the match grid.
    /// Handles selection, visual states, and letter reveal animations.
    /// </summary>
    public class Tile : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("References")]
        [SerializeField] private Image tileBackground;
        [SerializeField] private Image tileIcon;
        [SerializeField] private Text letterText;
        [SerializeField] private ParticleSystem matchParticles;

        [Header("Colors")]
        [SerializeField] private Color defaultColor = new Color(0.3f, 0.6f, 0.9f);
        [SerializeField] private Color selectedColor = new Color(0.9f, 0.7f, 0.2f);
        [SerializeField] private Color matchedColor = new Color(0.4f, 0.9f, 0.4f);
        [SerializeField] private Color hoverColor = new Color(0.5f, 0.7f, 0.95f);

        [Header("Animation Settings")]
        [SerializeField] private float selectScale = 1.15f;
        [SerializeField] private float animationDuration = 0.2f;

        // Tile properties
        public int TileType { get; private set; }
        public char Letter { get; private set; }
        public Vector2Int GridPosition { get; private set; }
        public bool IsMatched { get; private set; }
        public bool IsSelected { get; private set; }

        // Events
        public event Action<Tile> OnTileClicked;

        private Vector3 originalScale;
        private bool isInteractable = true;

        private void Awake()
        {
            originalScale = transform.localScale;
            
            if (letterText != null)
                letterText.gameObject.SetActive(false);
        }

        /// <summary>
        /// Initialize the tile with type, letter, and grid position.
        /// </summary>
        public void Initialize(int tileType, char letter, Vector2Int gridPosition, Sprite icon = null)
        {
            TileType = tileType;
            Letter = letter;
            GridPosition = gridPosition;
            IsMatched = false;
            IsSelected = false;

            if (tileIcon != null && icon != null)
                tileIcon.sprite = icon;

            // Set color based on tile type for visual variety
            tileBackground.color = GetColorForType(tileType);
            
            if (letterText != null)
            {
                letterText.text = letter.ToString().ToUpper();
                letterText.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Get a distinct color for each tile type.
        /// </summary>
        private Color GetColorForType(int type)
        {
            Color[] typeColors = new Color[]
            {
                new Color(0.94f, 0.35f, 0.35f), // Red
                new Color(0.35f, 0.70f, 0.94f), // Blue
                new Color(0.35f, 0.94f, 0.45f), // Green
                new Color(0.94f, 0.85f, 0.35f), // Yellow
                new Color(0.75f, 0.35f, 0.94f), // Purple
                new Color(0.94f, 0.55f, 0.35f), // Orange
            };

            return typeColors[type % typeColors.Length];
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!isInteractable || IsMatched) return;
            OnTileClicked?.Invoke(this);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!isInteractable || IsMatched || IsSelected) return;
            
            tileBackground.color = hoverColor;
            transform.DOScale(originalScale * 1.05f, animationDuration * 0.5f);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (!isInteractable || IsMatched || IsSelected) return;
            
            tileBackground.color = GetColorForType(TileType);
            transform.DOScale(originalScale, animationDuration * 0.5f);
        }

        /// <summary>
        /// Select this tile visually.
        /// </summary>
        public void Select()
        {
            if (IsMatched) return;

            IsSelected = true;
            tileBackground.color = selectedColor;
            
            transform.DOKill();
            transform.DOScale(originalScale * selectScale, animationDuration)
                .SetEase(Ease.OutBack);

            // Optional: play selection sound
            AudioManager.Instance?.PlaySound("TileSelect");
        }

        /// <summary>
        /// Deselect this tile.
        /// </summary>
        public void Deselect()
        {
            if (IsMatched) return;

            IsSelected = false;
            tileBackground.color = GetColorForType(TileType);
            
            transform.DOKill();
            transform.DOScale(originalScale, animationDuration)
                .SetEase(Ease.OutQuad);
        }

        /// <summary>
        /// Mark this tile as matched and reveal its letter.
        /// </summary>
        public void SetMatched(Action onComplete = null)
        {
            IsMatched = true;
            IsSelected = false;
            isInteractable = false;

            // Play match animation sequence
            Sequence matchSequence = DOTween.Sequence();

            // Pop effect
            matchSequence.Append(transform.DOScale(originalScale * 1.3f, 0.15f).SetEase(Ease.OutQuad));
            matchSequence.Append(transform.DOScale(originalScale, 0.1f).SetEase(Ease.InQuad));

            // Change color to matched
            matchSequence.Join(tileBackground.DOColor(matchedColor, 0.2f));

            // Reveal the letter
            matchSequence.AppendCallback(() => RevealLetter());

            // Play particles
            if (matchParticles != null)
            {
                matchSequence.AppendCallback(() => matchParticles.Play());
            }

            matchSequence.OnComplete(() => onComplete?.Invoke());

            // Play match sound
            AudioManager.Instance?.PlaySound("TileMatch");
        }

        /// <summary>
        /// Reveal the hidden letter on this tile.
        /// </summary>
        private void RevealLetter()
        {
            if (letterText == null) return;

            letterText.gameObject.SetActive(true);
            letterText.transform.localScale = Vector3.zero;
            
            letterText.transform.DOScale(1f, 0.3f).SetEase(Ease.OutBack);

            // Fade out the icon if present
            if (tileIcon != null)
            {
                tileIcon.DOFade(0.3f, 0.3f);
            }
        }

        /// <summary>
        /// Animate this tile flying to a target position (for word assembly).
        /// </summary>
        public void AnimateToPosition(Vector3 targetPosition, float delay, Action onComplete = null)
        {
            Sequence flySequence = DOTween.Sequence();
            flySequence.SetDelay(delay);

            // Scale up slightly
            flySequence.Append(transform.DOScale(originalScale * 0.8f, 0.2f));
            
            // Move to target
            flySequence.Append(transform.DOMove(targetPosition, 0.5f).SetEase(Ease.InOutQuad));
            
            // Settle into place
            flySequence.Append(transform.DOScale(originalScale * 0.6f, 0.1f).SetEase(Ease.OutQuad));

            flySequence.OnComplete(() => onComplete?.Invoke());
        }

        /// <summary>
        /// Shake animation for invalid match.
        /// </summary>
        public void ShakeInvalid()
        {
            transform.DOKill();
            transform.DOShakePosition(0.3f, 10f, 20);
            
            // Flash red briefly
            Sequence colorSequence = DOTween.Sequence();
            colorSequence.Append(tileBackground.DOColor(Color.red, 0.1f));
            colorSequence.Append(tileBackground.DOColor(GetColorForType(TileType), 0.2f));

            AudioManager.Instance?.PlaySound("InvalidMatch");
        }

        /// <summary>
        /// Reset tile to initial state.
        /// </summary>
        public void ResetTile()
        {
            IsMatched = false;
            IsSelected = false;
            isInteractable = true;
            
            transform.localScale = originalScale;
            tileBackground.color = GetColorForType(TileType);
            
            if (letterText != null)
                letterText.gameObject.SetActive(false);
            
            if (tileIcon != null)
                tileIcon.DOFade(1f, 0f);
        }

        /// <summary>
        /// Set whether the tile can be interacted with.
        /// </summary>
        public void SetInteractable(bool interactable)
        {
            isInteractable = interactable;
        }

        private void OnDestroy()
        {
            transform.DOKill();
        }
    }
}
