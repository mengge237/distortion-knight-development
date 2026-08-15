using System.Collections.Generic;
using UnityEngine;

namespace MutationChess.Core
{
    /// <summary>
    /// 全局音效管理器（运行时自动创建，无需场景接线）。
    /// 音效素材位于 Resources/Audio/：
    ///   relic_acquire(获得遗物) / relic_tick(遗物每次触发,轻响限频) /
    ///   hidden_awaken(隐藏效果觉醒) / synergy_combo(遗物共鸣激活) / faction_unlock(阵营解锁) /
    ///   coin_slide(金币滑落) / ui_click(按钮点击) / ui_panel(面板弹出) /
    ///   boss_pick_blood/frost/slime/corrupt/chain/memory(Boss遗物选取主题音效,可在设置中关闭)。
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        private static AudioManager _instance;
        public static AudioManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<AudioManager>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("AudioManager");
                        _instance = go.AddComponent<AudioManager>();
                    }
                }
                return _instance;
            }
        }

        private AudioSource source;
        private static Dictionary<string, AudioClip> clipCache = new Dictionary<string, AudioClip>();
        private float lastRelicTickTime;

        /// <summary>音效音量（0~1，由设置面板 SFX 滑条控制）。</summary>
        private static float sfxVolume = 0.8f;

        /// <summary>Boss 遗物选取主题音效开关（可在设置中关闭）。</summary>
        private static bool bossRelicPickSfxEnabled = true;

        void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);

            source = GetComponent<AudioSource>();
            if (source == null)
                source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f; // 2D 音效

            LoadVolumeSettings();
        }

        /// <summary>从 PlayerPrefs 恢复音量与 Boss 遗物音效开关（设置面板保存的值）。</summary>
        private static void LoadVolumeSettings()
        {
            sfxVolume = PlayerPrefs.GetFloat("SFXVolume", 0.8f);
            bossRelicPickSfxEnabled = PlayerPrefs.GetInt("BossRelicPickSfx", 1) == 1;
        }

        /// <summary>设置音效音量（SettingsManager 的 SFX 滑条调用）。</summary>
        public static void SetSFXVolume(float v)
        {
            sfxVolume = Mathf.Clamp01(v);
            PlayerPrefs.SetFloat("SFXVolume", sfxVolume);
            PlayerPrefs.Save();
        }

        /// <summary>设置 Boss 遗物选取主题音效开关（SettingsManager 开关调用）。</summary>
        public static void SetBossRelicPickSfxEnabled(bool enabled)
        {
            bossRelicPickSfxEnabled = enabled;
            PlayerPrefs.SetInt("BossRelicPickSfx", enabled ? 1 : 0);
            PlayerPrefs.Save();
        }

        public static bool IsBossRelicPickSfxEnabled() => bossRelicPickSfxEnabled;

        private static AudioClip LoadClip(string clipName)
        {
            if (clipCache.TryGetValue(clipName, out AudioClip cached) && cached != null)
                return cached;

            AudioClip clip = Resources.Load<AudioClip>("Audio/" + clipName);
            if (clip == null)
            {
                GameLogger.LogWarning($"[AudioManager] 音效加载失败：Audio/{clipName}");
                return null;
            }
            clipCache[clipName] = clip;
            return clip;
        }

        /// <summary>播放音效（带轻微随机音高，避免机械重复感；受 SFX 音量滑条缩放）。</summary>
        public void Play(string clipName, float volume = 1f, float pitchMin = 0.95f, float pitchMax = 1.05f)
        {
            if (source == null) return;
            AudioClip clip = LoadClip(clipName);
            if (clip == null) return;
            source.pitch = Random.Range(pitchMin, pitchMax);
            source.PlayOneShot(clip, volume * sfxVolume);
        }

        /// <summary>获得遗物：清脆铃铛。</summary>
        public void PlayRelicAcquired() => Play("relic_acquire", 0.9f);

        /// <summary>遗物效果每次触发：轻响，0.25 秒限频防止战斗刷屏。</summary>
        public void PlayRelicTick()
        {
            if (Time.time - lastRelicTickTime < 0.25f) return;
            lastRelicTickTime = Time.time;
            Play("relic_tick", 0.35f, 0.95f, 1.1f);
        }

        /// <summary>隐藏效果觉醒：神秘上扬扫频（协同反应的关键反馈时刻）。</summary>
        public void PlayHiddenAwaken() => Play("hidden_awaken", 1f, 0.97f, 1.03f);

        /// <summary>遗物共鸣组合激活：琶音庆典。</summary>
        public void PlayComboActivated() => Play("synergy_combo", 1f);

        /// <summary>阵营解锁：低沉铜锣+心跳重音。</summary>
        public void PlayFactionUnlocked() => Play("faction_unlock", 1f);

        /// <summary>金币滑落：钱币碰撞叮当串+落堆闷响。</summary>
        public void PlayCoinSlide(float volume = 0.9f) => Play("coin_slide", volume, 0.97f, 1.03f);

        /// <summary>
        /// Boss 遗物选取主题音效（按遗物主题映射，可在设置中关闭）：
        ///   鲜血→血流声 / 寒霜→冰晶碎裂 / 粘液→黏液挤压 / 腐化→内脏涌动 /
        ///   不舍→锁链摇晃 / 记忆→玻璃共鸣；未知遗物回退通用获得音效。
        /// </summary>
        public void PlayBossRelicPick(string relicId)
        {
            if (!bossRelicPickSfxEnabled) return;

            string clip = relicId switch
            {
                RelicIds.Boss_BloodVein => "boss_pick_blood",
                RelicIds.Boss_FrostHeart => "boss_pick_frost",
                RelicIds.Boss_SlimeGland => "boss_pick_slime",
                RelicIds.Slime_SlimeHeart => "boss_pick_slime",
                RelicIds.Boss_CorruptLiver => "boss_pick_corrupt",
                RelicIds.Boss_ReluctantChain => "boss_pick_chain",
                RelicIds.Boss_MemoryLens => "boss_pick_memory",
                _ => null
            };

            if (clip == null)
            {
                PlayRelicAcquired(); // 未知 Boss 遗物：回退通用获得音效
                return;
            }
            Play(clip, 1f, 0.97f, 1.03f);
        }

        /// <summary>通用按钮点击：轻短嗒声。</summary>
        public void PlayUIClick(float volume = 0.45f) => Play("ui_click", volume, 0.95f, 1.08f);

        /// <summary>面板弹出：柔和下扫"唰"声。</summary>
        public void PlayUIPanel(float volume = 0.55f) => Play("ui_panel", volume);

        void OnDestroy()
        {
            if (_instance == this)
                _instance = null;
        }
    }
}
