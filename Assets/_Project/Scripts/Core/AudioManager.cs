using System.Collections.Generic;
using UnityEngine;

namespace MutationChess.Core
{
    /// <summary>
    /// 全局音效管理器（运行时自动创建，无需场景接线）。
    /// 音效素材位于 Resources/Audio/：
    ///   relic_acquire(获得遗物) / relic_tick(遗物每次触发,轻响限频) /
    ///   hidden_awaken(隐藏效果觉醒) / synergy_combo(遗物共鸣激活) / faction_unlock(阵营解锁)。
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
        }

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

        /// <summary>播放音效（带轻微随机音高，避免机械重复感）。</summary>
        public void Play(string clipName, float volume = 1f, float pitchMin = 0.95f, float pitchMax = 1.05f)
        {
            if (source == null) return;
            AudioClip clip = LoadClip(clipName);
            if (clip == null) return;
            source.pitch = Random.Range(pitchMin, pitchMax);
            source.PlayOneShot(clip, volume);
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

        void OnDestroy()
        {
            if (_instance == this)
                _instance = null;
        }
    }
}
