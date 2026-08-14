using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using System;
using System.Collections;
using MutationChess.Battle;
using MutationChess.Core;

namespace MutationChess.UI
{
    public class BattleIntroUI : MonoBehaviour
    {
        [Header("Overlay")]
        [SerializeField] private CanvasGroup overlayCanvasGroup;
        [SerializeField] private Image backgroundImage;

        [Header("Player Info")]
        [SerializeField] private RectTransform playerInfoGroup;
        [SerializeField] private CanvasGroup playerInfoCanvasGroup;
        [SerializeField] private TMP_Text playerNameText;
        [SerializeField] private TMP_Text playerHpText;
        [SerializeField] private Image playerImage;

        [Header("Enemy Info")]
        [SerializeField] private RectTransform enemyInfoGroup;
        [SerializeField] private CanvasGroup enemyInfoCanvasGroup;
        [SerializeField] private TMP_Text enemyNameText;
        [SerializeField] private TMP_Text enemyHpText;
        [SerializeField] private Image enemyImage;

        [Header("Battle Start Text")]
        [SerializeField] private TMP_Text battleStartText;
        [SerializeField] private CanvasGroup battleStartCanvasGroup;

        [Header("Animation Settings")]
        [SerializeField] private float bgFadeInDuration = 0.4f;
        [SerializeField] private float infoZoomInDuration = 0.6f;
        [SerializeField] private float infoHoldDuration = 1.0f;
        [SerializeField] private float infoDissipateDuration = 0.6f;
        [SerializeField] private float battleStartZoomDuration = 0.7f;
        [SerializeField] private float battleStartHoldDuration = 0.6f;
        [SerializeField] private float battleStartDissipateDuration = 0.5f;
        [SerializeField] private float totalFadeOutDuration = 0.4f;

        private void Awake()
        {
            gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            // 终止所有关联的 DOTween 动画，避免销毁后仍访问 RectTransform/CanvasGroup
            if (overlayCanvasGroup != null) DOTween.Kill(overlayCanvasGroup);
            if (backgroundImage != null) DOTween.Kill(backgroundImage);
            if (playerInfoGroup != null) DOTween.Kill(playerInfoGroup);
            if (playerInfoCanvasGroup != null) DOTween.Kill(playerInfoCanvasGroup);
            if (enemyInfoGroup != null) DOTween.Kill(enemyInfoGroup);
            if (enemyInfoCanvasGroup != null) DOTween.Kill(enemyInfoCanvasGroup);
            if (battleStartText != null)
            {
                DOTween.Kill(battleStartText);
                DOTween.Kill(battleStartText.transform);
            }
        }

        public void ShowIntro(string playerName, int playerHp, int playerMaxHp,
                              string enemyName, int enemyHp, int enemyMaxHp,
                              Sprite enemySprite, Action onComplete)
        {
            gameObject.SetActive(true);
            StartCoroutine(IntroSequence(playerName, playerHp, playerMaxHp,
                                         enemyName, enemyHp, enemyMaxHp,
                                         enemySprite, onComplete));
        }

        private IEnumerator IntroSequence(string pName, int pHp, int pMaxHp,
                                          string eName, int eHp, int eMaxHp,
                                          Sprite eSprite, Action onComplete)
        {
            overlayCanvasGroup.alpha = 1f;
            backgroundImage.color = new Color(0f, 0f, 0f, 0f);

            playerNameText.text = pName;
            playerHpText.text = $"{pHp}/{pMaxHp}";
            playerImage.sprite = Resources.Load<Sprite>(ResourcePaths.Player_player);
            playerInfoGroup.localScale = Vector3.one * 3.5f;
            playerInfoCanvasGroup.alpha = 1f;

            enemyNameText.text = eName;
            enemyHpText.text = $"{eHp}/{eMaxHp}";
            enemyImage.sprite = eSprite;
            enemyInfoGroup.localScale = Vector3.one * 3.5f;
            enemyInfoCanvasGroup.alpha = 1f;

            battleStartText.text = "";
            battleStartCanvasGroup.alpha = 0f;
            battleStartText.transform.localScale = Vector3.one;
            battleStartText.transform.localRotation = Quaternion.identity;

            // Phase 1
            Sequence phase1 = DOTween.Sequence();
            phase1.Join(backgroundImage.DOFade(0.85f, bgFadeInDuration));
            phase1.Join(playerInfoGroup.DOScale(1f, infoZoomInDuration).SetEase(Ease.OutBack));
            phase1.Join(enemyInfoGroup.DOScale(1f, infoZoomInDuration).SetEase(Ease.OutBack));
            yield return phase1.WaitForCompletion();

            // Phase 2: Hold
            yield return new WaitForSeconds(infoHoldDuration);

            // Phase 3: Dissipate
            Vector2 playerOrigPos = playerInfoGroup.anchoredPosition;
            Vector2 enemyOrigPos = enemyInfoGroup.anchoredPosition;
            float playerRandRotZ = UnityEngine.Random.Range(-20f, -5f);
            float enemyRandRotZ = UnityEngine.Random.Range(5f, 20f);

            Sequence phase3 = DOTween.Sequence();
            phase3.Join(playerInfoGroup.DOScale(1.4f, infoDissipateDuration).SetEase(Ease.InQuad));
            phase3.Join(playerInfoCanvasGroup.DOFade(0f, infoDissipateDuration));
            phase3.Join(playerInfoGroup.DOAnchorPos(playerOrigPos + new Vector2(-30f, 60f), infoDissipateDuration).SetEase(Ease.InQuad));
            phase3.Join(playerInfoGroup.DORotate(new Vector3(0f, 0f, playerRandRotZ), infoDissipateDuration).SetEase(Ease.InQuad));

            phase3.Join(enemyInfoGroup.DOScale(1.4f, infoDissipateDuration).SetEase(Ease.InQuad));
            phase3.Join(enemyInfoCanvasGroup.DOFade(0f, infoDissipateDuration));
            phase3.Join(enemyInfoGroup.DOAnchorPos(enemyOrigPos + new Vector2(30f, 60f), infoDissipateDuration).SetEase(Ease.InQuad));
            phase3.Join(enemyInfoGroup.DORotate(new Vector3(0f, 0f, enemyRandRotZ), infoDissipateDuration).SetEase(Ease.InQuad));
            yield return phase3.WaitForCompletion();

            playerInfoGroup.anchoredPosition = playerOrigPos;
            enemyInfoGroup.anchoredPosition = enemyOrigPos;
            playerInfoGroup.localRotation = Quaternion.identity;
            enemyInfoGroup.localRotation = Quaternion.identity;

            // Phase 4: Battle Start
            battleStartText.text = "BATTLE START!";
            battleStartText.transform.localScale = Vector3.one * 4f;
            battleStartCanvasGroup.alpha = 1f;
            battleStartText.color = new Color(battleStartText.color.r, battleStartText.color.g, battleStartText.color.b, 1f);

            yield return battleStartText.transform.DOScale(1f, battleStartZoomDuration).SetEase(Ease.OutBack).WaitForCompletion();

            // Phase 5: Hold
            yield return new WaitForSeconds(battleStartHoldDuration);

            // Phase 6: Dissipate + fade out
            float bsRandRotZ = UnityEngine.Random.Range(-15f, 15f);
            Vector3 bsOrigPos = battleStartText.transform.localPosition;

            Sequence phase6 = DOTween.Sequence();
            phase6.Join(battleStartText.transform.DOScale(1.5f, battleStartDissipateDuration).SetEase(Ease.InQuad));
            phase6.Join(battleStartText.DOFade(0f, battleStartDissipateDuration));
            phase6.Join(battleStartText.transform.DOLocalMove(bsOrigPos + new Vector3(0f, 80f, 0f), battleStartDissipateDuration).SetEase(Ease.InQuad));
            phase6.Join(battleStartText.transform.DORotate(new Vector3(0f, 0f, bsRandRotZ), battleStartDissipateDuration).SetEase(Ease.InQuad));
            phase6.Join(backgroundImage.DOFade(0f, totalFadeOutDuration));
            yield return phase6.WaitForCompletion();

            battleStartText.transform.localPosition = bsOrigPos;
            battleStartText.transform.localRotation = Quaternion.identity;

            gameObject.SetActive(false);
            onComplete?.Invoke();
        }
    }
}