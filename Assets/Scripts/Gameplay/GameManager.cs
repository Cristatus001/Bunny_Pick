using System;
using System.Collections;
using System.Collections.Generic;
using BunnyPick.Data;
using UnityEngine;

namespace BunnyPick.Gameplay
{
    /// <summary>
    /// Core game logic, ported directly from the reference prototype's js/engine.js so the
    /// rules match exactly: a wave of bunnies (target + distractors) pops in staggered
    /// batches of 1-2; only a WRONG tap costs a life (a bunny that simply times out just
    /// retreats and the same digit gets a fresh wave, for free); a correct tap fades the
    /// bunny out before the next question starts; teddy bears are an independent power-up
    /// pickup (extraLife / slowTime / reveal / freeze) that can share the run alongside any
    /// wave. See engine.js `Game.prototype.update`/`_spawnWave`/`_tapBunny`/`_tapTeddy`.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Audio")]
        public AudioSource audioSource;
        public AudioClip sfxCatchCorrect;
        public AudioClip sfxWrongTap;
        public AudioClip sfxLifeLost;
        public AudioClip sfxPowerupPickup;
        public AudioClip sfxRunComplete;
        public AudioClip sfxGameover;
        public AudioClip sfxDigitTick;
        public AudioClip sfxButtonClick;

        public const string BestProgressKey = "BunnyPick.BestProgress";
        public const string BestLivesKey = "BunnyPick.BestLives";
        public const string WinsKey = "BunnyPick.Wins";
        const float LoadingScreenDuration = 2f;

        public static event Action<GameState> OnStateChanged;
        public static event Action<int> OnLivesChanged;
        public static event Action<int> OnDigitChanged;
        public static event Action<int, int> OnQuestionProgress; // current (1-based), total
        public static event Action OnPowerupsChanged;
        public static event Action<bool, int, int> OnRunEnded; // won, progress, lives
        public static event Action<PersonalBestData> OnPersonalBestChanged;
        public static event Action<string> OnLoadingFactChanged;
        public static event Action OnAutoOpenInfo;

        public GameState State { get; private set; } = GameState.MainMenu;
        public HoleData[] Holes { get; private set; }
        public int Lives { get; private set; }
        public int Progress { get; private set; }
        public int CurrentDigit { get; private set; } = -1;
        public string CurrentPrefix { get; private set; }
        public ActivePowerups Powerups { get; private set; } = new ActivePowerups();

        float _elapsed;
        float _missRetryAt;
        readonly List<(int holeIndex, string color, string prefix, bool isTarget)> _pendingBunnies = new List<(int, string, string, bool)>();
        readonly List<int> _pendingBatchSizes = new List<int>();
        float _popTimer;
        GameData.SpeedParams _waveSpeedParams;
        float _waveBunnyLifetime;
        Coroutine _transitionRoutine;

        void Awake()
        {
            Instance = this;
            Holes = new HoleData[GameData.HoleCount];
            for (int i = 0; i < Holes.Length; i++) Holes[i] = new HoleData { Index = i };
        }

        void Start()
        {
            OnPersonalBestChanged?.Invoke(ReadPersonalBest());
        }

        static PersonalBestData ReadPersonalBest()
        {
            return new PersonalBestData
            {
                BestProgress = PlayerPrefs.GetInt(BestProgressKey, 0),
                BestLives = PlayerPrefs.GetInt(BestLivesKey, 0),
                Wins = PlayerPrefs.GetInt(WinsKey, 0),
            };
        }

        public void PlayButtonClick() => PlaySfx(sfxButtonClick);

        void PlaySfx(AudioClip clip, float volume = 1f)
        {
            if (clip != null && audioSource != null) audioSource.PlayOneShot(clip, volume);
        }

        // ---------------------------------------------------------------------
        // Screen-flow entry points (each goes through a brief Loading transition,
        // matching the reference's Start/Retry -> loading -> gameplay flow).
        // ---------------------------------------------------------------------

        public void StartGame(bool showInfoIntro = false)
        {
            if (_transitionRoutine != null) StopCoroutine(_transitionRoutine);
            _transitionRoutine = StartCoroutine(LoadingThen(() =>
            {
                BeginRun();
                if (showInfoIntro) OnAutoOpenInfo?.Invoke();
            }));
        }

        public void ReturnToMenu()
        {
            if (_transitionRoutine != null) StopCoroutine(_transitionRoutine);
            _transitionRoutine = StartCoroutine(LoadingThen(BackToMenu));
        }

        IEnumerator LoadingThen(Action after)
        {
            State = GameState.Loading;
            OnStateChanged?.Invoke(State);
            string fact = GameData.ChemistryFacts[UnityEngine.Random.Range(0, GameData.ChemistryFacts.Length)];
            OnLoadingFactChanged?.Invoke(fact);
            yield return new WaitForSeconds(LoadingScreenDuration);
            _transitionRoutine = null;
            after();
        }

        void BackToMenu()
        {
            State = GameState.MainMenu;
            ClearAllHoles();
            OnStateChanged?.Invoke(State);
            OnPersonalBestChanged?.Invoke(ReadPersonalBest());
        }

        void BeginRun()
        {
            State = GameState.Playing;
            Lives = GameData.MaxLives;
            Progress = 0;
            _elapsed = 0f;
            _missRetryAt = 0f;
            Powerups = new ActivePowerups();
            ClearAllHoles();

            OnStateChanged?.Invoke(State);
            OnLivesChanged?.Invoke(Lives);
            OnPowerupsChanged?.Invoke();

            RollNewDigit();
            SpawnWave();
        }

        void ClearAllHoles()
        {
            foreach (var h in Holes)
            {
                h.Bunny = null;
                h.Teddy = null;
            }
        }

        void RollNewDigit()
        {
            CurrentDigit = UnityEngine.Random.Range(0, 10);
            CurrentPrefix = GameData.GetPrefixForDigit(CurrentDigit);
            OnDigitChanged?.Invoke(CurrentDigit);
            OnQuestionProgress?.Invoke(Mathf.Min(Progress + 1, GameData.TotalQuestions), GameData.TotalQuestions);
            PlaySfx(sfxDigitTick);
        }

        // ---------------------------------------------------------------------
        // Wave spawning - ported from engine.js _spawnWave/_popNextBunnyBatch.
        // ---------------------------------------------------------------------

        void SpawnWave()
        {
            foreach (var h in Holes) h.Bunny = null; // teddies persist across question boundaries

            var speed = GameData.GetSpeedParams(Progress, GameData.TotalQuestions);
            _waveSpeedParams = speed;
            _waveBunnyLifetime = speed.bunnyLifetime;

            var candidateHoles = new List<int>();
            for (int i = 0; i < Holes.Length; i++)
            {
                if (Holes[i].Teddy == null) candidateHoles.Add(i);
            }
            Shuffle(candidateHoles);
            int slotCount = Mathf.Min(speed.activeCount, candidateHoles.Count);
            var holeIdx = candidateHoles.GetRange(0, slotCount);

            var descriptors = new List<(int holeIndex, string color, string prefix, bool isTarget)>
            {
                (holeIdx[0], RandomBunnyColor(), CurrentPrefix, true)
            };

            var otherDigits = new List<int>();
            for (int d = 0; d < 10; d++) if (d != CurrentDigit) otherDigits.Add(d);
            Shuffle(otherDigits);
            for (int i = 1; i < holeIdx.Count; i++)
            {
                int digit = otherDigits[i - 1];
                descriptors.Add((holeIdx[i], RandomBunnyColor(), GameData.GetPrefixForDigit(digit), false));
            }

            Shuffle(descriptors);
            _pendingBunnies.Clear();
            _pendingBunnies.AddRange(descriptors);
            _pendingBatchSizes.Clear();
            int remaining = descriptors.Count;
            while (remaining > 0)
            {
                int size = Mathf.Min(UnityEngine.Random.value < 0.5f ? 1 : 2, remaining);
                _pendingBatchSizes.Add(size);
                remaining -= size;
            }
            _popTimer = 0f;
            PopNextBunnyBatch();

            if (UnityEngine.Random.value < GameData.TeddySpawnChance)
            {
                var reserved = new HashSet<int>(holeIdx);
                var emptyHoles = new List<int>();
                for (int h = 0; h < Holes.Length; h++)
                {
                    if (!reserved.Contains(h) && Holes[h].Teddy == null) emptyHoles.Add(h);
                }
                if (emptyHoles.Count > 0)
                {
                    int pick = emptyHoles[UnityEngine.Random.Range(0, emptyHoles.Count)];
                    var effect = GameData.PowerupTypes[UnityEngine.Random.Range(0, GameData.PowerupTypes.Length)];
                    Holes[pick].Teddy = new Teddy { HoleIndex = pick, EffectType = effect, Lifetime = GameData.TeddyLifetime };
                }
            }
        }

        static string RandomBunnyColor() => GameData.BunnyColors[UnityEngine.Random.Range(0, GameData.BunnyColors.Length)];

        void PopNextBunnyBatch()
        {
            if (_pendingBunnies.Count == 0) return;
            int n = _pendingBatchSizes.Count > 0 ? _pendingBatchSizes[0] : 1;
            if (_pendingBatchSizes.Count > 0) _pendingBatchSizes.RemoveAt(0);

            for (int i = 0; i < n && _pendingBunnies.Count > 0; i++)
            {
                var d = _pendingBunnies[0];
                _pendingBunnies.RemoveAt(0);
                Holes[d.holeIndex].Bunny = new Bunny
                {
                    HoleIndex = d.holeIndex,
                    Color = d.color,
                    Prefix = d.prefix,
                    IsTarget = d.isTarget,
                    Lifetime = _waveBunnyLifetime,
                };
            }

            if (_pendingBunnies.Count > 0)
            {
                var speed = _waveSpeedParams;
                _popTimer = speed.spawnInterval * (0.2f + UnityEngine.Random.value * 0.25f);
            }
        }

        static void Shuffle<T>(List<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        // ---------------------------------------------------------------------
        // Per-frame update - ported from engine.js Game.prototype.update(dt).
        // ---------------------------------------------------------------------

        void Update()
        {
            if (State != GameState.Playing) return;
            float dt = Time.deltaTime;
            if (dt <= 0f) return;

            _elapsed += dt;

            if (_missRetryAt > 0f && _elapsed >= _missRetryAt)
            {
                _missRetryAt = 0f;
                SpawnWave();
                return;
            }

            bool powerupsChanged = false;
            if (Powerups.FreezeUntil > 0f && _elapsed >= Powerups.FreezeUntil) { Powerups.FreezeUntil = 0f; powerupsChanged = true; }
            if (Powerups.SlowTimeUntil > 0f && _elapsed >= Powerups.SlowTimeUntil) { Powerups.SlowTimeUntil = 0f; powerupsChanged = true; }
            if (Powerups.RevealUntil > 0f && _elapsed >= Powerups.RevealUntil) { Powerups.RevealUntil = 0f; Powerups.RevealHoleIndex = -1; powerupsChanged = true; }
            if (powerupsChanged) OnPowerupsChanged?.Invoke();

            bool frozen = Powerups.FreezeUntil > _elapsed;
            bool slowed = Powerups.SlowTimeUntil > _elapsed;
            float effDt = frozen ? 0f : slowed ? dt * GameData.SlowTimeFactor : dt;

            bool targetMissed = false;
            bool completedThisFrame = false;

            foreach (var hole in Holes)
            {
                if (hole.Bunny != null)
                {
                    var b = hole.Bunny;
                    if (b.State == BunnyState.Caught)
                    {
                        b.FadeAge += dt;
                        if (b.FadeAge >= GameData.FadeDuration)
                        {
                            hole.Bunny = null;
                            completedThisFrame = true;
                        }
                    }
                    else if (b.State == BunnyState.Active)
                    {
                        b.Age += effDt;
                        if (b.Age >= b.Lifetime)
                        {
                            bool wasTarget = b.IsTarget;
                            b.State = BunnyState.Retreating;
                            b.RetreatAge = 0f;
                            if (wasTarget) targetMissed = true;
                        }
                    }
                    else if (b.State == BunnyState.Retreating)
                    {
                        b.RetreatAge += dt;
                        if (b.RetreatAge >= GameData.RetreatDuration) hole.Bunny = null;
                    }
                }

                if (hole.Teddy != null)
                {
                    var t = hole.Teddy;
                    if (t.State == TeddyState.Active)
                    {
                        t.Age += effDt;
                        if (t.Age >= t.Lifetime)
                        {
                            t.State = TeddyState.Retreating;
                            t.RetreatAge = 0f;
                        }
                    }
                    else if (t.State == TeddyState.Retreating)
                    {
                        t.RetreatAge += dt;
                        if (t.RetreatAge >= GameData.RetreatDuration) hole.Teddy = null;
                    }
                    else if (t.State == TeddyState.Collected)
                    {
                        t.CollectAge += dt;
                        if (t.CollectAge >= GameData.CollectDuration) hole.Teddy = null;
                    }
                }
            }

            if (_pendingBunnies.Count > 0)
            {
                _popTimer -= effDt;
                if (_popTimer <= 0f) PopNextBunnyBatch();
            }

            if (completedThisFrame)
            {
                Progress += 1;
                if (Progress >= GameData.TotalQuestions)
                {
                    State = GameState.Complete;
                    RecordRunResult(true);
                    OnStateChanged?.Invoke(State);
                    OnRunEnded?.Invoke(true, Progress, Lives);
                    PlaySfx(sfxRunComplete);
                    return;
                }
                RollNewDigit();
                SpawnWave();
                return;
            }

            if (targetMissed)
            {
                _missRetryAt = _elapsed + GameData.RetreatDuration + 0.05f;
            }
        }

        // ---------------------------------------------------------------------
        // Player-triggered actions - ported from engine.js tapHole/_tapBunny/_tapTeddy.
        // ---------------------------------------------------------------------

        public bool TapHole(int holeIndex)
        {
            if (State != GameState.Playing) return false;
            var hole = Holes[holeIndex];
            if (hole.Bunny != null) return TapBunny(hole);
            if (hole.Teddy != null) return TapTeddy(hole);
            return false;
        }

        bool TapBunny(HoleData hole)
        {
            var bunny = hole.Bunny;
            if (bunny == null || bunny.State != BunnyState.Active) return false;

            if (bunny.IsTarget)
            {
                bunny.State = BunnyState.Caught;
                bunny.FadeAge = 0f;
                PlaySfx(sfxCatchCorrect);
                return true;
            }

            bunny.State = BunnyState.Retreating;
            bunny.RetreatAge = 0f;
            Lives -= 1;
            OnLivesChanged?.Invoke(Lives);
            PlaySfx(sfxWrongTap);
            PlaySfx(sfxLifeLost, 0.8f);
            if (Lives <= 0)
            {
                State = GameState.GameOver;
                RecordRunResult(false);
                OnStateChanged?.Invoke(State);
                OnRunEnded?.Invoke(false, Progress, Lives);
                PlaySfx(sfxGameover);
            }
            return true;
        }

        bool TapTeddy(HoleData hole)
        {
            var teddy = hole.Teddy;
            if (teddy == null || teddy.State != TeddyState.Active) return false;

            teddy.State = TeddyState.Collected;
            teddy.CollectAge = 0f;
            ApplyPowerup(teddy.EffectType);
            PlaySfx(sfxPowerupPickup);
            return true;
        }

        void ApplyPowerup(PowerupType type)
        {
            switch (type)
            {
                case PowerupType.ExtraLife:
                    Lives = Mathf.Min(GameData.MaxLives, Lives + 1);
                    OnLivesChanged?.Invoke(Lives);
                    break;
                case PowerupType.SlowTime:
                    Powerups.SlowTimeUntil = _elapsed + GameData.SlowTimeDuration;
                    break;
                case PowerupType.Reveal:
                    Powerups.RevealUntil = _elapsed + GameData.RevealDuration;
                    Powerups.RevealHoleIndex = FindTargetHoleIndex();
                    break;
                case PowerupType.Freeze:
                    Powerups.FreezeUntil = _elapsed + GameData.FreezeDuration;
                    break;
            }
            OnPowerupsChanged?.Invoke();
        }

        int FindTargetHoleIndex()
        {
            for (int i = 0; i < Holes.Length; i++)
            {
                var b = Holes[i].Bunny;
                if (b != null && b.IsTarget && b.State == BunnyState.Active) return i;
            }
            return -1;
        }

        void RecordRunResult(bool won)
        {
            int bestProgress = PlayerPrefs.GetInt(BestProgressKey, 0);
            int bestLives = PlayerPrefs.GetInt(BestLivesKey, 0);
            int wins = PlayerPrefs.GetInt(WinsKey, 0);

            bool improved = Progress > bestProgress || (Progress == bestProgress && Lives > bestLives);
            if (improved)
            {
                bestProgress = Progress;
                bestLives = Lives;
                PlayerPrefs.SetInt(BestProgressKey, bestProgress);
                PlayerPrefs.SetInt(BestLivesKey, bestLives);
            }
            if (won)
            {
                wins += 1;
                PlayerPrefs.SetInt(WinsKey, wins);
            }
            PlayerPrefs.Save();
            OnPersonalBestChanged?.Invoke(new PersonalBestData { BestProgress = bestProgress, BestLives = bestLives, Wins = wins });
        }
    }
}
