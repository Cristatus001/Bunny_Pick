using System.Collections.Generic;
using System.IO;
using BunnyPick.Data;
using BunnyPick.Gameplay;
using BunnyPick.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BunnyPick.EditorTools
{
    /// <summary>
    /// Builds the Bunny Pick scene from real assets ported from the reference HTML
    /// prototype (Assets/ImportedArt, Assets/ImportedAudio) plus a small amount of
    /// procedurally generated background dressing (sky gradient, sun, hint-ring glow).
    /// Re-run via the menu any time to regenerate the scene from scratch.
    /// </summary>
    public static class BunnyPickSceneBuilder
    {
        const string SpriteDir = "Assets/Sprites/Generated";
        const string ArtDir = "Assets/ImportedArt";
        const string AudioDir = "Assets/ImportedAudio";
        const string ScenePath = "Assets/Scenes/BunnyPick.unity";

        static readonly Color SkyColor = new Color32(0x8F, 0xD3, 0xF4, 0xFF);
        static readonly Color PanelColor = new Color32(0xFB, 0xF3, 0xDD, 0xFF);
        static readonly Color PinkColor = new Color32(0xFF, 0x6F, 0x91, 0xFF);
        static readonly Color TitleColor = new Color32(0xE8, 0x4C, 0x6A, 0xFF);
        static readonly Color TextDarkColor = new Color32(0x4A, 0x3B, 0x2E, 0xFF);
        static readonly Color HintRingColor = new Color32(0xFF, 0xCD, 0x00, 0xF3);
        static readonly Color GoldColor = new Color32(0xC8, 0x8A, 0x1E, 0xFF);

        // Multiplied onto bunny_character.png - parallel to GameData.BunnyColors - since that
        // single shared sprite replaced 6 separately hand-drawn color variants. Kept light/pale
        // so the bunny's own painted shading still reads through the tint.
        static readonly Color[] BunnyColorTints =
        {
            Color.white,                            // white - no tint, the sprite's own base color
            new Color32(0xFF, 0xC9, 0xDC, 0xFF),     // pink
            new Color32(0xC6, 0xE6, 0xFF, 0xFF),     // skyblue
            new Color32(0xFF, 0xF1, 0xB0, 0xFF),     // lemon
            new Color32(0xE2, 0xD3, 0xFF, 0xFF),     // lavender
            new Color32(0xC3, 0xF5, 0xD6, 0xFF),     // mint
        };

        class RealArt
        {
            public Sprite GameplayBg, Carrot, HeartFull, HeartEmpty;
            public Sprite IconSettings, IconInfo, IconExit, IconSoundOn, IconSoundOff;
            public Sprite PrefixBubble;
            public Sprite BadgeExtraLife, BadgeSlowTime, BadgeReveal, BadgeFreeze;
            public Sprite GloveOpen, GloveClosed;
            public Sprite MenuBg, PlayButton, HeartBackdrop;
            public Sprite CongratsBanner, RunOverBanner, RetrySprite, HomeSprite;
            public Sprite BunnyCharacter, BunnyCharacter2;
            public Sprite LoadingCard, PillLarge, PillSmall;
            public AudioClip SfxCatchCorrect, SfxWrongTap, SfxLifeLost, SfxPowerupPickup, SfxRunComplete, SfxGameover, SfxDigitTick, SfxButtonClick;
        }

        [MenuItem("Bunny Pick/Build Game Scene")]
        public static void BuildScene()
        {
            AssetDatabase.Refresh();

            Sprite circle = GetOrCreateCircleSprite();
            Sprite ring = GetOrCreateRingSprite();
            Sprite skyGradient = GetOrCreateSkyGradientSprite();
            RealArt art = ImportRealArt();

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            Camera mainCamera = BuildCamera();
            BuildBackground(skyGradient, circle, art);
            HoleView[] holeViews = BuildHoles(art, ring);
            BuildUi(art, holeViews, mainCamera);

            var gmGo = new GameObject("GameManager");
            var gm = gmGo.AddComponent<GameManager>();
            gm.audioSource = gmGo.AddComponent<AudioSource>();
            gm.sfxCatchCorrect = art.SfxCatchCorrect;
            gm.sfxWrongTap = art.SfxWrongTap;
            gm.sfxLifeLost = art.SfxLifeLost;
            gm.sfxPowerupPickup = art.SfxPowerupPickup;
            gm.sfxRunComplete = art.SfxRunComplete;
            gm.sfxGameover = art.SfxGameover;
            gm.sfxDigitTick = art.SfxDigitTick;
            gm.sfxButtonClick = art.SfxButtonClick;

            EnsureFolder("Assets/Scenes");
            EditorSceneManager.SaveScene(scene, ScenePath);

            var scenes = EditorBuildSettings.scenes;
            bool alreadyListed = false;
            foreach (var s in scenes) if (s.path == ScenePath) alreadyListed = true;
            if (!alreadyListed)
            {
                var list = new List<EditorBuildSettingsScene>(scenes) { new EditorBuildSettingsScene(ScenePath, true) };
                EditorBuildSettings.scenes = list.ToArray();
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"Bunny Pick scene built at {ScenePath}");
        }

        // ---------- Real asset import ----------

        static RealArt ImportRealArt()
        {
            var art = new RealArt
            {
                // ppu=119 (not the usual 108) makes this exactly as wide as the camera's visible
                // area (2120px / 119 = 17.8 world units, matching GameData.CanvasToWorld's 1920px
                // canvas at 10 world units tall) - gameplay_bg.png's own aspect ratio (2120x1280)
                // is narrower than bg_garden.png's old 1920x1080, so matching on height like the
                // old art did would leave gaps on the left/right; matching on width instead just
                // crops a bit off the top/bottom, which reads fine for a background.
                GameplayBg = ImportSprite("gameplay_bg.png", 119),
                MenuBg = ImportSprite("menu_bg.png", 100),
                PlayButton = ImportSprite("play_button.png", 100),
                HeartBackdrop = ImportSprite("heart_backdrop.png", 100),
                CongratsBanner = ImportSprite("congrats_banner.png", 100),
                RunOverBanner = ImportSprite("run_over_banner.png", 100),
                RetrySprite = ImportSprite("retry_button.png", 100),
                HomeSprite = ImportSprite("home_button.png", 100),
                LoadingCard = ImportSprite("loading_card.png", 100),
                PillLarge = ImportSprite("pill_large.png", 100),
                PillSmall = ImportSprite("pill_small.png", 100),
                // gameplay_bg.png paints the dirt holes directly into the background now (see
                // GameData.HolePositions), so there's no separate hole sprite to draw on top -
                // unlike the old bg_garden.png, which had plain grass and relied on hole.png for
                // the dirt mound at each hole.
                Carrot = ImportSprite("carrot.png", 100),
                HeartFull = ImportSprite("heart_full.png", 100),
                // Generated from heart_full.png itself (desaturated + lightened, same silhouette)
                // since no "lost life" counterpart was provided - the old icon_heart_empty.png
                // was a leftover from a completely different art style and didn't match at all.
                HeartEmpty = ImportSprite("heart_empty.png", 100),
                IconSettings = ImportSprite("icon_settings.png", 100),
                IconInfo = ImportSprite("icon_info.png", 100),
                IconExit = ImportSprite("icon_exit.png", 100),
                IconSoundOn = ImportSprite("icon_sound_on.png", 100),
                IconSoundOff = ImportSprite("icon_sound_off.png", 100),
                PrefixBubble = ImportSprite("prefix_bubble.png", 100),
                BadgeExtraLife = ImportSprite("badge_extralife.png", 100),
                BadgeSlowTime = ImportSprite("badge_slowtime.png", 100),
                BadgeReveal = ImportSprite("badge_reveal.png", 100),
                BadgeFreeze = ImportSprite("badge_freeze.png", 100),
                // Pivot near the fingertips so the hand's grab point lands exactly where it
                // swoops down to (see PopAnimation.ComputeCaught) instead of Unity's default
                // center pivot, which would offset the whole glove above the bunny. ppu 190 to
                // match the new, smaller bunny_character.png scale (see below).
                GloveOpen = ImportSprite("glove_open.png", 190, new Vector2(0.5f, 0.2f)),
                GloveClosed = ImportSprite("glove_closed.png", 190, new Vector2(0.5f, 0.2f)),
                SfxCatchCorrect = ImportAudio("sfx_catch_correct.wav"),
                SfxWrongTap = ImportAudio("sfx_wrong_tap.wav"),
                SfxLifeLost = ImportAudio("sfx_life_lost.wav"),
                SfxPowerupPickup = ImportAudio("sfx_powerup_pickup.wav"),
                SfxRunComplete = ImportAudio("sfx_run_complete.wav"),
                SfxGameover = ImportAudio("sfx_gameover.wav"),
                SfxDigitTick = ImportAudio("sfx_digit_tick.wav"),
                SfxButtonClick = ImportAudio("sfx_button_click.wav"),
            };

            // bunny_character.png has almost no padding around the art itself (feet sit right at
            // the image's bottom edge), so its pivot sits right at the very bottom rather than
            // the old sprites' 0.22. ppu=1620 makes it ~2.4 world units tall - big enough to read
            // clearly against gameplay_bg.png's painted holes (~1.8 units wide) without the two
            // closest holes' bunnies visually overlapping when both are active at once.
            art.BunnyCharacter = ImportSprite("bunny_character.png", 1620, new Vector2(0.5f, 0.02f));
            // bunny_character2.png is a head-and-chest portrait (no legs, unlike the full-body
            // bunny_character.png) that came with a rounded-rectangle photo-card background
            // baked in - cleaned up with an ML background-removal pass before being dropped in
            // here, since a flat color-distance cutout couldn't separate it from the bunny's own
            // soft-shaded fur. ppu=1203 matches bunny_character.png's ~2.4 world-unit height so
            // the two read as the same size when the game alternates between them.
            art.BunnyCharacter2 = ImportSprite("bunny_character2.png", 1203, new Vector2(0.5f, 0.05f));
            return art;
        }

        static Sprite ImportSprite(string filename, int ppu, Vector2? pivot = null)
        {
            string path = $"{ArtDir}/{filename}";
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            if (importer == null)
            {
                Debug.LogError($"Could not import {path} - is it present on disk?");
                return null;
            }
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = ppu;
            importer.filterMode = FilterMode.Bilinear;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            if (pivot.HasValue)
            {
                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                settings.spriteAlignment = (int)SpriteAlignment.Custom;
                settings.spritePivot = pivot.Value;
                importer.SetTextureSettings(settings);
            }
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        static AudioClip ImportAudio(string filename)
        {
            string path = $"{AudioDir}/{filename}";
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        }

        // ---------- Camera & background ----------

        static Camera BuildCamera()
        {
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 5f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = SkyColor;
            camGo.transform.position = new Vector3(0, 0, -10);
            camGo.AddComponent<AudioListener>();
            return cam;
        }

        static void BuildBackground(Sprite skyGradient, Sprite circle, RealArt art)
        {
            var root = new GameObject("Background");
            CreateProceduralSprite("Sky", root.transform, skyGradient, Color.white, new Vector3(0, 1f, 0), new Vector2(22f, 14f), -30);
            CreateProceduralSprite("Sun", root.transform, circle, new Color32(0xFF, 0xE0, 0x82, 0xFF), new Vector3(-6.5f, 3.2f, 0), new Vector2(2.6f, 2.6f), -20);
            CreateRealSprite("Garden", root.transform, art.GameplayBg, Vector3.zero, -10);
        }

        static SpriteRenderer CreateProceduralSprite(string name, Transform parent, Sprite sprite, Color color, Vector3 localPos, Vector2 size, int sortingOrder)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent);
            go.transform.localPosition = localPos;
            go.transform.localScale = new Vector3(size.x, size.y, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = color;
            sr.sortingOrder = sortingOrder;
            return sr;
        }

        static SpriteRenderer CreateRealSprite(string name, Transform parent, Sprite sprite, Vector3 localPos, int sortingOrder)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent);
            go.transform.localPosition = localPos;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = sortingOrder;
            return sr;
        }

        // ---------- Holes ----------

        static HoleView[] BuildHoles(RealArt art, Sprite ring)
        {
            var root = new GameObject("Holes");
            var views = new HoleView[GameData.HoleCount];
            for (int i = 0; i < GameData.HoleCount; i++)
            {
                views[i] = BuildHole(root.transform, i, GameData.HolePositions[i], art, ring);
            }
            return views;
        }

        static HoleView BuildHole(Transform parent, int index, Vector2 position, RealArt art, Sprite ring)
        {
            var holeRoot = new GameObject($"Hole_{index}");
            holeRoot.transform.SetParent(parent);
            holeRoot.transform.position = position;

            var bunnyPivotGo = new GameObject("BunnyPivot");
            bunnyPivotGo.transform.SetParent(holeRoot.transform);
            bunnyPivotGo.transform.localPosition = Vector3.zero;
            var bunnySr = bunnyPivotGo.AddComponent<SpriteRenderer>();
            bunnySr.sprite = art.BunnyCharacter;
            bunnySr.sortingOrder = 2;
            bunnyPivotGo.SetActive(false);

            var teddyPivotGo = new GameObject("TeddyPivot");
            teddyPivotGo.transform.SetParent(holeRoot.transform);
            teddyPivotGo.transform.localPosition = Vector3.zero;
            var teddySr = teddyPivotGo.AddComponent<SpriteRenderer>();
            // Power-up pickups use the same two bunny designs as the answer bunnies now (no more
            // separate teddy-bear art) - alternated by hole index since Teddy has no color field
            // to key off like Bunny does. The floating badge chip is what actually tells the
            // player it's a power-up, not the character underneath it.
            teddySr.sprite = index % 2 == 0 ? art.BunnyCharacter : art.BunnyCharacter2;
            teddySr.sortingOrder = 2;
            teddyPivotGo.SetActive(false);

            var handPivotGo = new GameObject("HandPivot");
            handPivotGo.transform.SetParent(holeRoot.transform);
            handPivotGo.transform.localPosition = Vector3.zero;
            var handSr = handPivotGo.AddComponent<SpriteRenderer>();
            handSr.sprite = art.GloveOpen;
            handSr.sortingOrder = 3;
            handPivotGo.SetActive(false);

            var hintRingGo = new GameObject("HintRing");
            hintRingGo.transform.SetParent(holeRoot.transform);
            hintRingGo.transform.localPosition = new Vector3(0, 51f * GameData.PxToWorld, -0.05f);
            hintRingGo.transform.localScale = new Vector3(159f * GameData.PxToWorld, 205f * GameData.PxToWorld, 1f);
            var hintRingSr = hintRingGo.AddComponent<SpriteRenderer>();
            hintRingSr.sprite = ring;
            hintRingSr.color = HintRingColor;
            hintRingSr.sortingOrder = 10;
            hintRingGo.SetActive(false);

            var collider = holeRoot.AddComponent<CircleCollider2D>();
            collider.radius = GameData.HoleHitRadii[index];
            collider.offset = new Vector2(0, GameData.HoleHitOffsetY);

            var holeView = holeRoot.AddComponent<HoleView>();
            holeView.holeIndex = index;
            holeView.bunnyPivot = bunnyPivotGo.transform;
            holeView.bunnySprite = bunnySr;
            holeView.bunnyColorTints = BunnyColorTints;
            holeView.bunnyCharacterSprites = new[] { art.BunnyCharacter, art.BunnyCharacter2 };
            holeView.teddyPivot = teddyPivotGo.transform;
            holeView.teddySprite = teddySr;
            holeView.handPivot = handPivotGo.transform;
            holeView.handSprite = handSr;
            holeView.handOpenSprite = art.GloveOpen;
            holeView.handClosedSprite = art.GloveClosed;
            holeView.hintRing = hintRingSr;

            return holeView;
        }

        // ---------- UI ----------

        static void BuildUi(RealArt art, HoleView[] holes, Camera mainCamera)
        {
            var esGo = new GameObject("EventSystem");
            esGo.AddComponent<EventSystem>();
            esGo.AddComponent<StandaloneInputModule>();

            var canvasGo = new GameObject("Canvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();

            BuildMainMenuPanel(canvasGo.transform, art);
            BuildHudPanel(canvasGo.transform, art);
            BuildGameOverPanel(canvasGo.transform, art);
            BuildLoadingPanel(canvasGo.transform, art);
            BuildChatBubblesAndBadges(canvasGo.transform, holes, art, mainCamera);
            var confirmExit = BuildConfirmExitPanel(canvasGo.transform);
            BuildInfoTab(canvasGo.transform);
            BuildSettingsUi(canvasGo.transform, art, confirmExit);
            BuildInfoButton(canvasGo.transform, art);
        }

        static RectTransform AddRect(GameObject go, Transform parent)
        {
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            if (rt == null) rt = go.AddComponent<RectTransform>();
            return rt;
        }

        // A full-screen, click-catching (and dimming) root for a modal popup - so while it's
        // open, taps anywhere outside its actual card content hit this scrim instead of falling
        // through to whatever HUD/menu buttons or hole colliders sit behind it. Returns the
        // CanvasGroup that should go on GO itself (driving the whole scrim+card as one unit) and
        // the transform the popup's actual visible content should be parented under.
        static GameObject CreateModalRoot(string name, Transform parent, out CanvasGroup canvasGroup)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = AddRect(go, parent);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var scrim = go.AddComponent<Image>();
            scrim.color = new Color(0f, 0f, 0f, 0.45f);

            canvasGroup = go.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            return go;
        }

        static Text CreateText(string name, Transform parent, string content, int fontSize, Color color, Vector2 anchoredPos, Vector2 size, FontStyle style = FontStyle.Normal)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = AddRect(go, parent);
            rt.sizeDelta = size;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPos;
            var text = go.AddComponent<Text>();
            text.text = content;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = color;
            return text;
        }

        static Button CreateButton(string name, Transform parent, Sprite sprite, string label, Vector2 anchoredPos, Vector2 size, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = AddRect(go, parent);
            rt.sizeDelta = size;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPos;
            var img = go.AddComponent<Image>();
            img.sprite = sprite;
            img.type = Image.Type.Sliced;
            img.color = color;
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            CreateText($"{name}Label", go.transform, label, 28, Color.white, Vector2.zero, size, FontStyle.Bold);
            return btn;
        }

        // For buttons that are themselves fully-rendered art (their own label/styling already
        // baked in) rather than a plain sliced background + generated text label.
        static Button CreateImageButton(string name, Transform parent, Sprite sprite, Vector2 anchoredPos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = AddRect(go, parent);
            rt.sizeDelta = size;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPos;
            var img = go.AddComponent<Image>();
            img.sprite = sprite;
            img.preserveAspect = true;
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            return btn;
        }

        static Toggle CreateToggle(string name, Transform parent, string label, Vector2 anchoredPos, Sprite onIcon, Sprite offIcon, out Image toggleIconOut)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = AddRect(go, parent);
            rt.sizeDelta = new Vector2(260, 50);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPos;

            var toggle = go.AddComponent<Toggle>();

            var iconGo = new GameObject("Icon", typeof(RectTransform));
            var iconRt = AddRect(iconGo, go.transform);
            iconRt.sizeDelta = new Vector2(40, 40);
            iconRt.anchorMin = iconRt.anchorMax = new Vector2(0f, 0.5f);
            iconRt.anchoredPosition = new Vector2(24, 0);
            var iconImg = iconGo.AddComponent<Image>();
            iconImg.sprite = onIcon;
            toggleIconOut = iconImg;

            var bgGo = new GameObject("Background", typeof(RectTransform));
            var bgRt = AddRect(bgGo, go.transform);
            bgRt.sizeDelta = new Vector2(28, 28);
            bgRt.anchorMin = bgRt.anchorMax = new Vector2(1f, 0.5f);
            bgRt.anchoredPosition = new Vector2(-18, 0);
            var bgImg = bgGo.AddComponent<Image>();
            bgImg.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
            bgImg.type = Image.Type.Sliced;
            bgImg.color = Color.white;

            var checkGo = new GameObject("Checkmark", typeof(RectTransform));
            var checkRt = AddRect(checkGo, bgGo.transform);
            checkRt.sizeDelta = new Vector2(20, 20);
            checkRt.anchorMin = checkRt.anchorMax = new Vector2(0.5f, 0.5f);
            var checkImg = checkGo.AddComponent<Image>();
            checkImg.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            checkImg.color = PinkColor;

            toggle.targetGraphic = bgImg;
            toggle.graphic = checkImg;
            toggle.isOn = true;

            CreateText($"{name}Label", go.transform, label, 22, TextDarkColor, new Vector2(-40, 0), new Vector2(150, 40));

            return toggle;
        }

        static MainMenuUI BuildMainMenuPanel(Transform canvasParent, RealArt art)
        {
            var panelGo = new GameObject("MainMenuPanel", typeof(RectTransform));
            var panelRt = AddRect(panelGo, canvasParent);
            panelRt.anchorMin = Vector2.zero;
            panelRt.anchorMax = Vector2.one;
            panelRt.offsetMin = Vector2.zero;
            panelRt.offsetMax = Vector2.zero;
            // menu_bg.png already has the "Bunny Pick" logo, bunnies and scenery painted in,
            // so it replaces the old generic card + separately-drawn title/carrot entirely.
            var panelImg = panelGo.AddComponent<Image>();
            panelImg.sprite = art.MenuBg;

            // Position measured directly from the art (play_button.png's spot in the composed
            // menu_bg.png splash), expressed against the Canvas's 1280x720 reference resolution.
            var playButton = CreateImageButton("PlayButton", panelGo.transform, art.PlayButton, new Vector2(31, -148), new Vector2(110, 156));

            CreateText("Subtitle", panelGo.transform, "Learn IUPAC number prefixes the fun way!", 20, TextDarkColor, new Vector2(0, 330), new Vector2(900, 40));
            var statsText = CreateText("Stats", panelGo.transform, "No runs yet - tap Play to start!", 16, TextDarkColor, new Vector2(0, 300), new Vector2(900, 30));

            var canvasGroup = panelGo.AddComponent<CanvasGroup>();
            var ui = panelGo.AddComponent<MainMenuUI>();
            ui.root = canvasGroup;
            ui.playButton = playButton;
            ui.statsText = statsText;
            return ui;
        }

        static HudUI BuildHudPanel(Transform canvasParent, RealArt art)
        {
            var hudGo = new GameObject("HudPanel", typeof(RectTransform));
            var hudRt = AddRect(hudGo, canvasParent);
            hudRt.anchorMin = Vector2.zero;
            hudRt.anchorMax = Vector2.one;
            hudRt.offsetMin = Vector2.zero;
            hudRt.offsetMax = Vector2.zero;

            var livesContainer = new GameObject("LivesContainer", typeof(RectTransform));
            var livesRt = AddRect(livesContainer, hudGo.transform);
            livesRt.sizeDelta = new Vector2(220, 70);
            livesRt.anchorMin = livesRt.anchorMax = new Vector2(0f, 1f);
            livesRt.anchoredPosition = new Vector2(115, -44);

            // Decorative pill behind the 3 heart icons - heart_button.png bakes all 3 hearts in
            // as always-full, so it can't show a life being lost on its own; it sits behind the
            // existing full/empty icons below, which still do that part like before.
            var backdropGo = new GameObject("Backdrop", typeof(RectTransform));
            var backdropRt = AddRect(backdropGo, livesContainer.transform);
            backdropRt.sizeDelta = livesRt.sizeDelta;
            backdropRt.anchorMin = backdropRt.anchorMax = new Vector2(0.5f, 0.5f);
            var backdropImg = backdropGo.AddComponent<Image>();
            backdropImg.sprite = art.HeartBackdrop;
            backdropImg.preserveAspect = true;

            var lifeIcons = new Image[GameData.MaxLives];
            for (int i = 0; i < lifeIcons.Length; i++)
            {
                var iconGo = new GameObject($"Life_{i}", typeof(RectTransform));
                var iconRt = AddRect(iconGo, livesContainer.transform);
                iconRt.sizeDelta = new Vector2(44, 44);
                iconRt.anchorMin = iconRt.anchorMax = new Vector2(0.5f, 0.5f);
                iconRt.anchoredPosition = new Vector2((i - 1) * 50, 0);
                var img = iconGo.AddComponent<Image>();
                img.sprite = art.HeartFull;
                lifeIcons[i] = img;
            }

            // Matches pill_large.png's own aspect (552x171 = ~3.23) instead of the old generic
            // sliced panel, so it sits alongside the Loading screen's identically-styled title
            // pill rather than looking like a leftover placeholder box.
            var bannerGo = new GameObject("PromptBanner", typeof(RectTransform));
            var bannerRt = AddRect(bannerGo, hudGo.transform);
            bannerRt.sizeDelta = new Vector2(380, 118);
            bannerRt.anchorMin = bannerRt.anchorMax = new Vector2(0.5f, 1f);
            bannerRt.anchoredPosition = new Vector2(0, -65);
            var bannerImg = bannerGo.AddComponent<Image>();
            bannerImg.sprite = art.PillLarge;
            bannerImg.preserveAspect = true;
            CreateText("Label", bannerGo.transform, "The number is", 20, TextDarkColor, new Vector2(-70, 0), new Vector2(200, 54));
            var digitText = CreateText("Digit", bannerGo.transform, "0", 34, TextDarkColor, new Vector2(110, 0), new Vector2(80, 54), FontStyle.Bold);

            var counterGo = new GameObject("QuestionCounter", typeof(RectTransform));
            var counterRt = AddRect(counterGo, hudGo.transform);
            counterRt.sizeDelta = new Vector2(220, 60);
            counterRt.anchorMin = counterRt.anchorMax = new Vector2(0.5f, 1f);
            counterRt.anchoredPosition = new Vector2(0, -164);
            var counterImg = counterGo.AddComponent<Image>();
            counterImg.sprite = art.PillSmall;
            counterImg.preserveAspect = true;
            var questionCounterText = CreateText("Text", counterGo.transform, "Question 1 / 20", 15, TextDarkColor, Vector2.zero, new Vector2(200, 50));

            var trayGo = new GameObject("PowerupTray", typeof(RectTransform));
            var trayRt = AddRect(trayGo, hudGo.transform);
            trayRt.sizeDelta = new Vector2(200, 56);
            trayRt.anchorMin = trayRt.anchorMax = new Vector2(1f, 1f);
            trayRt.anchoredPosition = new Vector2(-120, -40);

            var chips = new HudUI.PowerupChip[3];
            chips[0] = BuildPowerupChip(trayGo.transform, 0, "Snail Pace", art.BadgeSlowTime, PowerupType.SlowTime);
            chips[1] = BuildPowerupChip(trayGo.transform, 1, "Reveal", art.BadgeReveal, PowerupType.Reveal);
            chips[2] = BuildPowerupChip(trayGo.transform, 2, "Nap Time", art.BadgeFreeze, PowerupType.Freeze);

            var canvasGroup = hudGo.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;

            var ui = hudGo.AddComponent<HudUI>();
            ui.root = canvasGroup;
            ui.lifeIcons = lifeIcons;
            ui.heartFullSprite = art.HeartFull;
            ui.heartEmptySprite = art.HeartEmpty;
            ui.digitText = digitText;
            ui.questionCounterText = questionCounterText;
            ui.powerupChips = chips;
            return ui;
        }

        static HudUI.PowerupChip BuildPowerupChip(Transform parent, int index, string label, Sprite badge, PowerupType type)
        {
            var chipGo = new GameObject($"Chip_{type}", typeof(RectTransform));
            var chipRt = AddRect(chipGo, parent);
            chipRt.sizeDelta = new Vector2(180, 48);
            chipRt.anchorMin = chipRt.anchorMax = new Vector2(0.5f, 1f);
            chipRt.anchoredPosition = new Vector2(0, -index * 54);
            var chipImg = chipGo.AddComponent<Image>();
            chipImg.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
            chipImg.type = Image.Type.Sliced;
            chipImg.color = PanelColor;

            var iconGo = new GameObject("Icon", typeof(RectTransform));
            var iconRt = AddRect(iconGo, chipGo.transform);
            iconRt.sizeDelta = new Vector2(36, 36);
            iconRt.anchorMin = iconRt.anchorMax = new Vector2(0f, 0.5f);
            iconRt.anchoredPosition = new Vector2(26, 0);
            var iconImg = iconGo.AddComponent<Image>();
            iconImg.sprite = badge;
            iconImg.preserveAspect = true;

            CreateText("Label", chipGo.transform, label, 16, TextDarkColor, new Vector2(20, 0), new Vector2(120, 40));

            chipGo.SetActive(false);
            return new HudUI.PowerupChip { root = chipGo, type = type };
        }

        static GameOverUI BuildGameOverPanel(Transform canvasParent, RealArt art)
        {
            // congrats_banner.png / run_over_banner.png (798x1108) are the win/loss versions of
            // the same card art - same ribbon-and-zigzag-card composition, just a different
            // baked header ("CONGRATS!!" vs "RUN OVER"), and neither has Retry/Home baked in
            // (unlike an earlier draft of this art), so those are real buttons placed in the
            // empty card below. Child positions are measured against this art at BannerScale.
            const float BannerScale = 0.6f;
            var panelGo = new GameObject("GameOverPanel", typeof(RectTransform));
            var panelRt = AddRect(panelGo, canvasParent);
            panelRt.sizeDelta = new Vector2(798, 1108) * BannerScale;
            panelRt.anchorMin = panelRt.anchorMax = new Vector2(0.5f, 0.5f);
            var panelImg = panelGo.AddComponent<Image>();
            panelImg.sprite = art.CongratsBanner;
            panelImg.preserveAspect = true;

            var titleText = CreateText("Title", panelGo.transform, "Run Complete!", 34, TitleColor, new Vector2(0, 43), new Vector2(345, 58), FontStyle.Bold);
            var starsText = CreateText("Stars", panelGo.transform, "", 25, GoldColor, new Vector2(0, -13), new Vector2(345, 41), FontStyle.Bold);
            var detailText = CreateText("Detail", panelGo.transform, "", 18, TextDarkColor, new Vector2(0, -75), new Vector2(345, 92));
            var factText = CreateText("Fact", panelGo.transform, "", 15, TextDarkColor, new Vector2(0, -144), new Vector2(345, 69));
            var retryButton = CreateImageButton("RetryButton", panelGo.transform, art.RetrySprite, new Vector2(-79, -248), new Vector2(149, 100));
            var homeButton = CreateImageButton("HomeButton", panelGo.transform, art.HomeSprite, new Vector2(76, -248), new Vector2(149, 100));

            var canvasGroup = panelGo.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;

            var ui = panelGo.AddComponent<GameOverUI>();
            ui.root = canvasGroup;
            ui.bannerImage = panelImg;
            ui.winBanner = art.CongratsBanner;
            ui.loseBanner = art.RunOverBanner;
            ui.titleText = titleText;
            ui.starsText = starsText;
            ui.detailText = detailText;
            ui.factText = factText;
            ui.retryButton = retryButton;
            ui.homeButton = homeButton;
            return ui;
        }

        static void BuildLoadingPanel(Transform canvasParent, RealArt art)
        {
            // pill_large.png (title) and loading_card.png (fact) - a matching pair from the same
            // parchment/pill art set used for the Game Over banners, stacked with the pill
            // overlapping the card's top edge like the reference mockup for this screen.
            var panelGo = new GameObject("LoadingPanel", typeof(RectTransform));
            var panelRt = AddRect(panelGo, canvasParent);
            panelRt.anchorMin = panelRt.anchorMax = new Vector2(0.5f, 0.5f);
            panelRt.sizeDelta = Vector2.zero;

            var pillGo = new GameObject("TitlePill", typeof(RectTransform));
            var pillRt = AddRect(pillGo, panelGo.transform);
            pillRt.sizeDelta = new Vector2(330, 102);
            pillRt.anchorMin = pillRt.anchorMax = new Vector2(0.5f, 0.5f);
            pillRt.anchoredPosition = new Vector2(0, 185);
            var pillImg = pillGo.AddComponent<Image>();
            pillImg.sprite = art.PillLarge;
            pillImg.preserveAspect = true;
            CreateText("Title", pillGo.transform, "Digging holes...", 24, TextDarkColor, Vector2.zero, new Vector2(300, 80), FontStyle.Bold);

            var cardGo = new GameObject("Card", typeof(RectTransform));
            var cardRt = AddRect(cardGo, panelGo.transform);
            cardRt.sizeDelta = new Vector2(360, 378);
            cardRt.anchorMin = cardRt.anchorMax = new Vector2(0.5f, 0.5f);
            cardRt.anchoredPosition = new Vector2(0, -40);
            var cardImg = cardGo.AddComponent<Image>();
            cardImg.sprite = art.LoadingCard;
            cardImg.preserveAspect = true;
            var factText = CreateText("Fact", cardGo.transform, "", 16, TextDarkColor, Vector2.zero, new Vector2(300, 300));

            var canvasGroup = panelGo.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;

            var ui = panelGo.AddComponent<LoadingScreenUI>();
            ui.root = canvasGroup;
            ui.factText = factText;
        }

        static void BuildChatBubblesAndBadges(Transform canvasParent, HoleView[] holes, RealArt art, Camera mainCamera)
        {
            var bubbleContainer = new GameObject("ChatBubbles", typeof(RectTransform));
            AddRect(bubbleContainer, canvasParent);
            var badgeContainer = new GameObject("BadgeChips", typeof(RectTransform));
            AddRect(badgeContainer, canvasParent);

            for (int i = 0; i < holes.Length; i++)
            {
                BuildChatBubble(bubbleContainer.transform, i, art, mainCamera);
                BuildBadgeChip(badgeContainer.transform, i, art, mainCamera);
            }
        }

        static void BuildChatBubble(Transform parent, int holeIndex, RealArt art, Camera mainCamera)
        {
            var rootGo = new GameObject($"ChatBubble_{holeIndex}", typeof(RectTransform));
            var rootRt = AddRect(rootGo, parent);
            rootRt.sizeDelta = new Vector2(160, 90);
            rootRt.anchorMin = rootRt.anchorMax = new Vector2(0.5f, 0.5f);

            var bodyGo = new GameObject("Body", typeof(RectTransform));
            var bodyRt = AddRect(bodyGo, rootGo.transform);
            bodyRt.anchorMin = Vector2.zero;
            bodyRt.anchorMax = Vector2.one;
            bodyRt.offsetMin = Vector2.zero;
            bodyRt.offsetMax = Vector2.zero;
            var bodyImg = bodyGo.AddComponent<Image>();
            bodyImg.sprite = art.PrefixBubble;
            bodyImg.preserveAspect = true;
            bodyImg.raycastTarget = false;

            var textGo = new GameObject("Text", typeof(RectTransform));
            var textRt = AddRect(textGo, bodyGo.transform);
            textRt.anchorMin = new Vector2(0.12f, 0.35f);
            textRt.anchorMax = new Vector2(0.92f, 0.88f);
            textRt.offsetMin = Vector2.zero;
            textRt.offsetMax = Vector2.zero;
            var text = textGo.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 26;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;

            var cg = rootGo.AddComponent<CanvasGroup>();
            cg.alpha = 0f;
            cg.blocksRaycasts = false;
            cg.interactable = false;

            var follower = rootGo.AddComponent<ChatBubbleFollower>();
            follower.holeIndex = holeIndex;
            follower.bubbleRoot = rootRt;
            follower.visibilityGroup = cg;
            follower.bodyImage = bodyImg;
            follower.text = text;
            follower.worldCamera = mainCamera;
        }

        static void BuildBadgeChip(Transform parent, int holeIndex, RealArt art, Camera mainCamera)
        {
            var rootGo = new GameObject($"BadgeChip_{holeIndex}", typeof(RectTransform));
            var rootRt = AddRect(rootGo, parent);
            rootRt.sizeDelta = new Vector2(64, 64);
            rootRt.anchorMin = rootRt.anchorMax = new Vector2(0.5f, 0.5f);

            var chipImg = rootGo.AddComponent<Image>();
            chipImg.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
            chipImg.type = Image.Type.Sliced;
            chipImg.color = new Color32(0xFF, 0xFA, 0xEB, 0xFF);
            chipImg.raycastTarget = false;

            var iconGo = new GameObject("Icon", typeof(RectTransform));
            var iconRt = AddRect(iconGo, rootGo.transform);
            iconRt.anchorMin = new Vector2(0.15f, 0.15f);
            iconRt.anchorMax = new Vector2(0.85f, 0.85f);
            iconRt.offsetMin = Vector2.zero;
            iconRt.offsetMax = Vector2.zero;
            var iconImg = iconGo.AddComponent<Image>();
            iconImg.preserveAspect = true;
            iconImg.raycastTarget = false;

            var cg = rootGo.AddComponent<CanvasGroup>();
            cg.alpha = 0f;
            cg.blocksRaycasts = false;
            cg.interactable = false;

            var follower = rootGo.AddComponent<BadgeChipFollower>();
            follower.holeIndex = holeIndex;
            follower.chipRoot = rootRt;
            follower.visibilityGroup = cg;
            follower.badgeImage = iconImg;
            follower.worldCamera = mainCamera;
            follower.badgeExtraLife = art.BadgeExtraLife;
            follower.badgeSlowTime = art.BadgeSlowTime;
            follower.badgeReveal = art.BadgeReveal;
            follower.badgeFreeze = art.BadgeFreeze;
        }

        static ConfirmExitUI BuildConfirmExitPanel(Transform canvasParent)
        {
            Sprite bg = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
            Sprite uiSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

            var rootGo = CreateModalRoot("ConfirmExitPanel", canvasParent, out var cg);

            var cardGo = new GameObject("Card", typeof(RectTransform));
            var cardRt = AddRect(cardGo, rootGo.transform);
            cardRt.sizeDelta = new Vector2(440, 320);
            cardRt.anchorMin = cardRt.anchorMax = new Vector2(0.5f, 0.5f);
            var cardImg = cardGo.AddComponent<Image>();
            cardImg.sprite = bg;
            cardImg.type = Image.Type.Sliced;
            cardImg.color = PanelColor;

            var titleText = CreateText("Title", cardGo.transform, "Exit run?", 32, TitleColor, new Vector2(0, 100), new Vector2(380, 50), FontStyle.Bold);
            var detailText = CreateText("Detail", cardGo.transform, "Your progress in this run will be lost.", 18, TextDarkColor, new Vector2(0, 25), new Vector2(380, 60));
            var confirmButton = CreateButton("ExitButton", cardGo.transform, uiSprite, "Exit", new Vector2(0, -50), new Vector2(180, 56), new Color32(0xE8, 0x4C, 0x4C, 0xFF));
            var cancelButton = CreateButton("CancelButton", cardGo.transform, uiSprite, "Cancel", new Vector2(0, -120), new Vector2(180, 56), new Color32(0xAF, 0xAF, 0xAF, 0xFF));

            var ui = rootGo.AddComponent<ConfirmExitUI>();
            ui.root = cg;
            ui.titleText = titleText;
            ui.detailText = detailText;
            ui.confirmButton = confirmButton;
            ui.cancelButton = cancelButton;
            return ui;
        }

        static void BuildInfoTab(Transform canvasParent)
        {
            Sprite bg = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
            Sprite uiSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

            var rootGo = CreateModalRoot("InfoTabPanel", canvasParent, out var cg);

            var cardGo = new GameObject("Card", typeof(RectTransform));
            var cardRt = AddRect(cardGo, rootGo.transform);
            cardRt.sizeDelta = new Vector2(700, 560);
            cardRt.anchorMin = cardRt.anchorMax = new Vector2(0.5f, 0.5f);
            var cardImg = cardGo.AddComponent<Image>();
            cardImg.sprite = bg;
            cardImg.type = Image.Type.Sliced;
            cardImg.color = PanelColor;

            var closeXButton = CreateButton("CloseX", cardGo.transform, uiSprite, "X", new Vector2(320, 250), new Vector2(48, 48), new Color32(0xAF, 0xAF, 0xAF, 0xFF));

            CreateText("Title", cardGo.transform, "How to Play", 30, TitleColor, new Vector2(0, 235), new Vector2(600, 45), FontStyle.Bold);
            string howTo =
                "- A number (0-9) appears at the top. It stands for an IUPAC counting prefix.\n" +
                "- Bunnies pop out of the holes with speech bubbles showing different prefixes.\n" +
                "- Tap the ONE bunny whose bubble matches the number shown.\n" +
                "- Catch the right bunny 20 times to finish. A wrong pick costs a life - you have 3!\n" +
                "- Grab teddy bears for power-ups: extra life, slow time, reveal, or freeze.";
            CreateText("HowToList", cardGo.transform, howTo, 17, TextDarkColor, new Vector2(0, 130), new Vector2(620, 190));

            CreateText("LegendTitle", cardGo.transform, "IUPAC Prefix Legend", 22, TitleColor, new Vector2(0, 10), new Vector2(600, 35), FontStyle.Bold);
            string legend = "0 nil   1 un   2 bi   3 tri   4 quad\n5 pent   6 hex   7 sept   8 oct   9 enn";
            CreateText("LegendGrid", cardGo.transform, legend, 18, TextDarkColor, new Vector2(0, -50), new Vector2(600, 70));

            var gotItButton = CreateButton("GotItButton", cardGo.transform, uiSprite, "Got it!", new Vector2(0, -230), new Vector2(200, 56), PinkColor);

            var ui = rootGo.AddComponent<InfoTabUI>();
            ui.root = cg;
            ui.closeXButton = closeXButton;
            ui.gotItButton = gotItButton;
        }

        static void BuildSettingsUi(Transform canvasParent, RealArt art, ConfirmExitUI confirmExit)
        {
            var rootGo = CreateModalRoot("SettingsPopover", canvasParent, out var cg);

            var panelGo = new GameObject("Card", typeof(RectTransform));
            var panelRt = AddRect(panelGo, rootGo.transform);
            panelRt.sizeDelta = new Vector2(300, 160);
            panelRt.anchorMin = panelRt.anchorMax = new Vector2(1f, 1f);
            panelRt.anchoredPosition = new Vector2(-190, -140);
            var panelImg = panelGo.AddComponent<Image>();
            panelImg.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
            panelImg.type = Image.Type.Sliced;
            panelImg.color = PanelColor;

            var soundToggle = CreateToggle("SoundToggle", panelGo.transform, "Music", new Vector2(0, 35), art.IconSoundOn, art.IconSoundOff, out Image musicIcon);

            var exitRowGo = new GameObject("ExitRow", typeof(RectTransform));
            var exitRowRt = AddRect(exitRowGo, panelGo.transform);
            exitRowRt.sizeDelta = new Vector2(260, 50);
            exitRowRt.anchorMin = exitRowRt.anchorMax = new Vector2(0.5f, 0.5f);
            exitRowRt.anchoredPosition = new Vector2(0, -35);
            var exitBtn = exitRowGo.AddComponent<Button>();
            var exitBg = exitRowGo.AddComponent<Image>();
            exitBg.color = new Color(0, 0, 0, 0);
            exitBtn.targetGraphic = exitBg;

            var exitIconGo = new GameObject("Icon", typeof(RectTransform));
            var exitIconRt = AddRect(exitIconGo, exitRowGo.transform);
            exitIconRt.sizeDelta = new Vector2(40, 40);
            exitIconRt.anchorMin = exitIconRt.anchorMax = new Vector2(0f, 0.5f);
            exitIconRt.anchoredPosition = new Vector2(24, 0);
            var exitIconImg = exitIconGo.AddComponent<Image>();
            exitIconImg.sprite = art.IconExit;

            CreateText("ExitLabel", exitRowGo.transform, "Exit", 22, TextDarkColor, new Vector2(30, 0), new Vector2(150, 40));

            var settingsPanel = rootGo.AddComponent<SettingsPanelUI>();
            settingsPanel.root = cg;
            settingsPanel.soundToggle = soundToggle;
            settingsPanel.musicToggleIcon = musicIcon;
            settingsPanel.soundOnSprite = art.IconSoundOn;
            settingsPanel.soundOffSprite = art.IconSoundOff;
            settingsPanel.exitButton = exitBtn;
            settingsPanel.confirmExit = confirmExit;

            var btnGo = new GameObject("SettingsButton", typeof(RectTransform));
            var btnRt = AddRect(btnGo, canvasParent);
            btnRt.sizeDelta = new Vector2(64, 64);
            btnRt.anchorMin = btnRt.anchorMax = new Vector2(1f, 1f);
            btnRt.anchoredPosition = new Vector2(-40, -40);
            var btnImg = btnGo.AddComponent<Image>();
            btnImg.sprite = art.IconSettings;
            var btn = btnGo.AddComponent<Button>();
            btn.targetGraphic = btnImg;

            var btnCg = btnGo.AddComponent<CanvasGroup>();
            var buttonUi = btnGo.AddComponent<SettingsButtonUI>();
            buttonUi.root = btnCg;
            buttonUi.button = btn;
            buttonUi.settingsPanel = settingsPanel;
        }

        static void BuildInfoButton(Transform canvasParent, RealArt art)
        {
            var infoTabUi = canvasParent.Find("InfoTabPanel")?.GetComponent<InfoTabUI>();

            var btnGo = new GameObject("InfoButton", typeof(RectTransform));
            var btnRt = AddRect(btnGo, canvasParent);
            btnRt.sizeDelta = new Vector2(64, 64);
            btnRt.anchorMin = btnRt.anchorMax = new Vector2(1f, 1f);
            btnRt.anchoredPosition = new Vector2(-112, -40);
            var btnImg = btnGo.AddComponent<Image>();
            btnImg.sprite = art.IconInfo;
            var btn = btnGo.AddComponent<Button>();
            btn.targetGraphic = btnImg;

            var cg = btnGo.AddComponent<CanvasGroup>();
            var ui = btnGo.AddComponent<InfoButtonUI>();
            ui.root = cg;
            ui.button = btn;
            ui.infoTab = infoTabUi;
        }

        // ---------- Generated (procedural) sprite assets ----------

        static Sprite GetOrCreateCircleSprite()
        {
            string path = $"{SpriteDir}/Circle.png";
            if (!File.Exists(ToAbsolute(path)))
            {
                const int size = 128;
                var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
                var center = new Vector2(size / 2f, size / 2f);
                float radius = size / 2f - 2f;
                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        float dist = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center);
                        float alpha = Mathf.Clamp01((radius - dist) + 1.5f);
                        tex.SetPixel(x, y, new Color(1, 1, 1, alpha));
                    }
                }
                tex.Apply();
                WritePng(tex, path);
            }
            return ImportGeneratedSprite(path, 128);
        }

        static Sprite GetOrCreateRingSprite()
        {
            string path = $"{SpriteDir}/Ring.png";
            if (!File.Exists(ToAbsolute(path)))
            {
                const int size = 128;
                var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
                var center = new Vector2(size / 2f, size / 2f);
                float outerR = size / 2f - 2f;
                float innerR = outerR - 12f;
                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        float dist = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center);
                        float alphaOuter = Mathf.Clamp01((outerR - dist) + 1.5f);
                        float alphaInner = Mathf.Clamp01((dist - innerR) + 1.5f);
                        tex.SetPixel(x, y, new Color(1, 1, 1, Mathf.Min(alphaOuter, alphaInner)));
                    }
                }
                tex.Apply();
                WritePng(tex, path);
            }
            return ImportGeneratedSprite(path, 128);
        }

        static Sprite GetOrCreateSkyGradientSprite()
        {
            string path = $"{SpriteDir}/SkyGradient.png";
            if (!File.Exists(ToAbsolute(path)))
            {
                const int size = 128;
                var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
                Color top = new Color32(0x5B, 0xB8, 0xEA, 0xFF);
                Color bottom = new Color32(0xDD, 0xF3, 0xFF, 0xFF);
                var pixels = new Color[size * size];
                for (int y = 0; y < size; y++)
                {
                    float t = y / (float)(size - 1);
                    Color c = Color.Lerp(bottom, top, t);
                    for (int x = 0; x < size; x++) pixels[y * size + x] = c;
                }
                tex.SetPixels(pixels);
                tex.Apply();
                WritePng(tex, path);
            }
            return ImportGeneratedSprite(path, 128);
        }

        static void WritePng(Texture2D tex, string assetPath)
        {
            EnsureFolder(SpriteDir);
            File.WriteAllBytes(ToAbsolute(assetPath), tex.EncodeToPNG());
        }

        static Sprite ImportGeneratedSprite(string assetPath, int pixelsPerUnit)
        {
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            var importer = (TextureImporter)AssetImporter.GetAtPath(assetPath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = pixelsPerUnit;
            importer.filterMode = FilterMode.Bilinear;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            string leaf = Path.GetFileName(path);
            if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }

        static string ToAbsolute(string assetPath)
        {
            string projectRoot = Path.GetDirectoryName(Application.dataPath);
            return Path.Combine(projectRoot, assetPath);
        }
    }
}
