using UnityEngine;

namespace Math6Companion.Core
{
    /// <summary>
    /// Quản lý âm thanh tổng, nhạc nền (BGM) và hiệu ứng âm thanh (SFX) cho toàn bộ game.
    /// Tồn tại xuyên suốt vòng đời ứng dụng (DontDestroyOnLoad).
    /// </summary>
    /// <summary>
    /// Quản lý âm thanh tổng, nhạc nền (BGM) và hiệu ứng âm thanh (SFX) cho toàn bộ game.
    /// Tồn tại xuyên suốt vòng đời ứng dụng (DontDestroyOnLoad).
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        private static AudioManager instance;
        public static AudioManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindAnyObjectByType<AudioManager>();
                    if (instance == null)
                    {
                        var go = new GameObject("[AudioManager]");
                        instance = go.AddComponent<AudioManager>();
                    }
                }
                return instance;
            }
            private set => instance = value;
        }

        [Header("Audio Sources")]
        [SerializeField] private AudioSource bgmSource;
        [SerializeField] private AudioSource sfxSource;

        [Header("Default Audio Clips (Optional)")]
        [SerializeField] private AudioClip defaultBgmClip;
        [SerializeField] private AudioClip buttonClickSfx;
        [SerializeField] private AudioClip correctSfx;
        [SerializeField] private AudioClip wrongSfx;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);

            if (bgmSource == null)
            {
                bgmSource = gameObject.AddComponent<AudioSource>();
                bgmSource.loop = true;
                bgmSource.playOnAwake = false;
            }

            if (sfxSource == null)
            {
                sfxSource = gameObject.AddComponent<AudioSource>();
                sfxSource.loop = false;
                sfxSource.playOnAwake = false;
            }

            // Tự động tạo âm thanh procedural nếu dự án chưa có file âm thanh sẵn
            GenerateDefaultProceduralAudio();
        }

        private void Start()
        {
            ApplyVolumeSettings();

            if (defaultBgmClip != null && !bgmSource.isPlaying)
            {
                PlayBGM(defaultBgmClip);
            }
        }

        private void GenerateDefaultProceduralAudio()
        {
            if (buttonClickSfx == null)
            {
                buttonClickSfx = CreateProceduralTone("Click_SFX", 780f, 0.06f, 35f);
            }

            if (correctSfx == null)
            {
                correctSfx = CreateProceduralChime("Correct_SFX", 659f, 880f, 0.35f);
            }

            if (wrongSfx == null)
            {
                wrongSfx = CreateProceduralTone("Wrong_SFX", 220f, 0.35f, 10f);
            }

            if (defaultBgmClip == null)
            {
                defaultBgmClip = CreateProceduralBgm("Ambient_Study_BGM");
            }
        }

        public void ApplyVolumeSettings()
        {
            if (SessionManager.Instance != null)
            {
                float master = SessionManager.Instance.masterVolume;
                float bgm = SessionManager.Instance.bgmVolume;
                float sfx = SessionManager.Instance.sfxVolume;

                if (bgmSource != null) bgmSource.volume = master * bgm;
                if (sfxSource != null) sfxSource.volume = master * sfx;
            }
        }

        public void PlayBGM(AudioClip clip)
        {
            if (clip == null || bgmSource == null) return;

            bgmSource.clip = clip;
            ApplyVolumeSettings();
            bgmSource.Play();
        }

        public void PlaySFX(AudioClip clip)
        {
            if (clip == null || sfxSource == null) return;

            ApplyVolumeSettings();
            sfxSource.PlayOneShot(clip);
        }

        public void PlayButtonClick()
        {
            if (buttonClickSfx != null) PlaySFX(buttonClickSfx);
        }

        public void PlayCorrectSound()
        {
            if (correctSfx != null) PlaySFX(correctSfx);
        }

        public void PlayWrongSound()
        {
            if (wrongSfx != null) PlaySFX(wrongSfx);
        }

        public void PlayPreviewTone()
        {
            if (correctSfx != null) PlaySFX(correctSfx);
            else PlayButtonClick();
        }

        #region Procedural Audio Synthesis
        private static AudioClip CreateProceduralTone(string name, float freq, float duration, float decayRate)
        {
            int sampleRate = 44100;
            int sampleCount = Mathf.Max(1, (int)(sampleRate * duration));
            float[] samples = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleRate;
                float envelope = Mathf.Exp(-t * decayRate);
                float sin = Mathf.Sin(2f * Mathf.PI * freq * t);
                samples[i] = sin * envelope * 0.4f;
            }

            var clip = AudioClip.Create(name, sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private static AudioClip CreateProceduralChime(string name, float f1, float f2, float duration)
        {
            int sampleRate = 44100;
            int sampleCount = Mathf.Max(1, (int)(sampleRate * duration));
            float[] samples = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleRate;
                float envelope = Mathf.Exp(-t * 8f);
                float sin1 = Mathf.Sin(2f * Mathf.PI * f1 * t);
                float sin2 = Mathf.Sin(2f * Mathf.PI * f2 * t);
                samples[i] = (sin1 * 0.3f + sin2 * 0.2f) * envelope;
            }

            var clip = AudioClip.Create(name, sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private static AudioClip CreateProceduralBgm(string name)
        {
            int sampleRate = 22050;
            float duration = 12.0f; // Vòng lặp 12s êm dịu
            int sampleCount = Mathf.Max(1, (int)(sampleRate * duration));
            float[] samples = new float[sampleCount];

            // Hòa âm dịu nhẹ: Đô (261Hz) -> Mi (329Hz) -> La (440Hz) -> Sol (392Hz)
            float[] chordRoots = new float[] { 261.63f, 329.63f, 440.00f, 392.00f };
            float barDuration = duration / chordRoots.Length;

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleRate;
                int chordIdx = Mathf.Clamp((int)(t / barDuration), 0, chordRoots.Length - 1);
                float root = chordRoots[chordIdx];

                // Sóng âm nền pad êm dịu
                float pad = Mathf.Sin(2f * Mathf.PI * root * t) * 0.08f
                          + Mathf.Sin(2f * Mathf.PI * root * 1.5f * t) * 0.04f;

                // Nốt chuông rải nhẹ
                float arpTime = t % 0.75f;
                float arpEnv = Mathf.Exp(-arpTime * 6f);
                float arpFreq = root * (1f + ((int)(t * 1.33f) % 3) * 0.25f);
                float arp = Mathf.Sin(2f * Mathf.PI * arpFreq * t) * arpEnv * 0.06f;

                samples[i] = Mathf.Clamp(pad + arp, -1f, 1f);
            }

            var clip = AudioClip.Create(name, sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
        #endregion
    }
}
