using UnityEngine;
using MutationChess.Core;
using TMPro;
using UnityEngine.UI;

namespace MutationChess.Battle
{
    public enum EnemyIntentType
    {
        Attack,
        Defend,
        Special,
        Buff,
        Wait
    }

    public class EnemyIntentUI : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private GameObject intentPanel;
        [SerializeField] private Image intentIcon;
        [SerializeField] private TMP_Text intentText;

        [Header("")]
        [SerializeField] private Sprite attackIcon;
        [SerializeField] private Sprite defendIcon;
        [SerializeField] private Sprite specialIcon;
        [SerializeField] private Sprite buffIcon;
        [SerializeField] private Sprite waitIcon;

        void Start()
        {
            if (intentPanel != null)
                intentPanel.SetActive(false);
            else
                GameLogger.LogError("EnemyIntentUI: intentPanel ");
        }

        public void ShowIntent(EnemyIntentType intent, int value)
        {
            if (intentPanel != null)
            {
                intentPanel.SetActive(true);
            }
            else
            {
                GameLogger.LogError("intentPanel ");
                return;
            }

            if (intentIcon != null)
            {
                Sprite selectedSprite = GetSpriteForIntent(intent);

                if (selectedSprite != null)
                {
                    intentIcon.sprite = selectedSprite;
                    intentIcon.enabled = true;
                    intentIcon.color = Color.white;
                }
                else
                {
                    intentIcon.enabled = false;
                    GameLogger.LogWarning($" {intent}  Inspector");
                }
            }
            else
            {
                GameLogger.LogError("intentIcon ");
            }

            if (intentText != null)
            {
                if (intent == EnemyIntentType.Wait)
                {
                    intentText.text = "";
                }
                else
                {
                    intentText.text = value.ToString();
                }
            }
            else
            {
                GameLogger.LogError("intentText ");
            }
        }

        private Sprite GetSpriteForIntent(EnemyIntentType intent)
        {
            switch (intent)
            {
                case EnemyIntentType.Attack:
                    return attackIcon;
                case EnemyIntentType.Defend:
                    return defendIcon;
                case EnemyIntentType.Special:
                    return specialIcon;
                case EnemyIntentType.Buff:
                    return buffIcon;
                case EnemyIntentType.Wait:
                    return waitIcon;
                default:
                    return null;
            }
        }

        public void HideIntent()
        {
            if (intentPanel != null)
            {
                intentPanel.SetActive(false);
            }
        }

        void OnValidate()
        {
            if (attackIcon == null) GameLogger.LogWarning("Attack Icon ");
            if (defendIcon == null) GameLogger.LogWarning("Defend Icon ");
            if (specialIcon == null) GameLogger.LogWarning("Special Icon ");
            if (buffIcon == null) GameLogger.LogWarning("Buff Icon ");
            if (waitIcon == null) GameLogger.LogWarning("Wait Icon ");
            if (intentPanel == null) GameLogger.LogWarning("Intent Panel ");
            if (intentIcon == null) GameLogger.LogWarning("Intent Icon ");
            if (intentText == null) GameLogger.LogWarning("Intent Text ");
        }
    }
}


