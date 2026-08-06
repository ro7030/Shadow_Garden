#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;

namespace ShadowGarden.Presentation.Editor
{
    /// <summary>Imports the final authored art and builds replaceable presentation bindings.</summary>
    public static class FinalPresentationAssetBuilder
    {
        private const string DataRoot = "Assets/Game/PresentationData";
        private const string ResourceRoot = "Assets/Resources/Presentation";

        [MenuItem("Shadow Garden/Presentation/Build Final Asset Library")]
        public static void Build()
        {
            ConfigureSprites();
            ConfigureAudio();
            BuildAtlases();
            EnsureFolder(DataRoot);
            EnsureFolder(ResourceRoot);
            EnsureFolder(ResourceRoot + "/Worlds");
            EnsureFolder(ResourceRoot + "/Bindings");

            var moa = LoadOrCreate<MoaAnimationSetAsset>(DataRoot + "/MoaAnimationSet.asset");
            AssignMoa(moa);

            var fx = LoadOrCreate<GameplayFxSetAsset>(DataRoot + "/GameplayFxSet.asset");
            AssignFx(fx);

            var audio = LoadOrCreate<AudioSetAsset>(DataRoot + "/AudioSet.asset");
            AssignAudio(audio);

            var worlds = new WorldArtSetAsset[3];
            for (var world = 1; world <= 3; world++)
            {
                worlds[world - 1] = LoadOrCreate<WorldArtSetAsset>(
                    $"{ResourceRoot}/Worlds/WorldArtSet_W0{world}.asset");
                AssignWorld(worlds[world - 1], world, audio);
            }

            var catalog = LoadOrCreate<InGameAssetCatalogAsset>(
                ResourceRoot + "/InGameAssetCatalog.asset");
            AssignCatalog(catalog, moa, fx, audio);
            BuildUiPrefabs(catalog);

            for (var world = 1; world <= 3; world++)
            {
                for (var stage = 1; stage <= 4; stage++)
                {
                    var id = $"{world}-{stage}";
                    var binding = LoadOrCreate<StagePresentationBindingAsset>(
                        $"{ResourceRoot}/Bindings/Stage_{world}_{stage}.asset");
                    binding.stageId = id;
                    binding.worldArt = worlds[world - 1];
                    EditorUtility.SetDirty(binding);
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            PresentationAssetLibrary.ResetCache();
            Debug.Log("Shadow Garden final presentation asset library built: 3 worlds / 12 bindings.");
        }

        private static void ConfigureSprites()
        {
            var guids = AssetDatabase.FindAssets(
                "t:Texture2D",
                new[] { "Assets/Game/Art/Common", "Assets/Game/Art/Worlds" });
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.EndsWith(".png", StringComparison.OrdinalIgnoreCase)) continue;
                if (AssetImporter.GetAtPath(path) is not TextureImporter importer) continue;

                var isUi = path.Contains("/UI/");
                var isBottomPivot = path.Contains("/Moa/") ||
                                    path.Contains("/Gameplay/") ||
                                    path.Contains("/Goals/") ||
                                    path.Contains("/Decor/");
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = isUi ? 100f : 128f;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.filterMode = FilterMode.Bilinear;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.textureCompression = TextureImporterCompression.Compressed;
                importer.maxTextureSize = 2048;
                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                settings.spriteAlignment = (int)SpriteAlignment.Custom;
                settings.spritePivot = isBottomPivot ? new Vector2(0.5f, 0.06f) : new Vector2(0.5f, 0.5f);
                if (isUi)
                {
                    settings.spriteBorder = new Vector4(18f, 18f, 18f, 18f);
                }

                importer.SetTextureSettings(settings);

                importer.SaveAndReimport();
            }
        }

        private static void ConfigureAudio()
        {
            var guids = AssetDatabase.FindAssets("t:AudioClip", new[] { "Assets/Game/Audio" });
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (AssetImporter.GetAtPath(path) is not AudioImporter importer) continue;
                var longForm = path.Contains("/Music/") || path.Contains("/Ambience/");
                importer.forceToMono = false;
                importer.loadInBackground = longForm;
                importer.defaultSampleSettings = new AudioImporterSampleSettings
                {
                    loadType = longForm ? AudioClipLoadType.CompressedInMemory : AudioClipLoadType.DecompressOnLoad,
                    compressionFormat = longForm ? AudioCompressionFormat.Vorbis : AudioCompressionFormat.PCM,
                    quality = longForm ? 0.52f : 1f,
                    sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate,
                    preloadAudioData = true
                };
                importer.SaveAndReimport();
            }
        }

        private static void BuildAtlases()
        {
            const string atlasRoot = "Assets/Game/Atlases";
            EnsureFolder(atlasRoot);
            CreateAtlas(atlasRoot + "/CommonGameplay.spriteatlas", new[]
            {
                "Assets/Game/Art/Common/Gameplay",
                "Assets/Game/Art/Common/Channels",
                "Assets/Game/Art/Common/Moa"
            });
            CreateAtlas(atlasRoot + "/World01.spriteatlas", new[] { "Assets/Game/Art/Worlds/W01" });
            CreateAtlas(atlasRoot + "/World02.spriteatlas", new[] { "Assets/Game/Art/Worlds/W02" });
            CreateAtlas(atlasRoot + "/World03.spriteatlas", new[] { "Assets/Game/Art/Worlds/W03" });
            CreateAtlas(atlasRoot + "/UI.spriteatlas", new[] { "Assets/Game/Art/Common/UI" });
            CreateAtlas(atlasRoot + "/VFX.spriteatlas", new[] { "Assets/Game/Art/Common/VFX" });
        }

        private static void CreateAtlas(string path, string[] folders)
        {
            var atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(path);
            if (atlas == null)
            {
                atlas = new SpriteAtlas();
                AssetDatabase.CreateAsset(atlas, path);
            }

            var current = atlas.GetPackables();
            if (current != null && current.Length > 0) atlas.Remove(current);
            var packables = folders
                .Select(folder => AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(folder))
                .Where(asset => asset != null)
                .ToArray();
            atlas.Add(packables);
            atlas.SetPackingSettings(new SpriteAtlasPackingSettings
            {
                blockOffset = 1,
                enableRotation = false,
                enableTightPacking = false,
                padding = 4
            });
            atlas.SetTextureSettings(new SpriteAtlasTextureSettings
            {
                readable = false,
                generateMipMaps = false,
                sRGB = true,
                filterMode = FilterMode.Bilinear
            });
            atlas.SetPlatformSettings(new TextureImporterPlatformSettings
            {
                name = "WebGL",
                overridden = true,
                maxTextureSize = 2048,
                format = TextureImporterFormat.Automatic,
                compressionQuality = 65
            });
            EditorUtility.SetDirty(atlas);
        }

        private static void AssignMoa(MoaAnimationSetAsset asset)
        {
            const string root = "Assets/Game/Art/Common/Moa/";
            asset.frontA = Sprite(root + "Moa_Front_A.png");
            asset.frontB = Sprite(root + "Moa_Front_B.png");
            asset.backA = Sprite(root + "Moa_Back_A.png");
            asset.backB = Sprite(root + "Moa_Back_B.png");
            asset.sideA = Sprite(root + "Moa_Side_A.png");
            asset.sideB = Sprite(root + "Moa_Side_B.png");
            asset.neutral = Sprite(root + "Moa_Portrait_Neutral.png");
            asset.curious = Sprite(root + "Moa_Portrait_Curious.png");
            asset.surprised = Sprite(root + "Moa_Portrait_Surprised.png");
            asset.worried = Sprite(root + "Moa_Portrait_Worried.png");
            asset.determined = Sprite(root + "Moa_Portrait_Determined.png");
            asset.relieved = Sprite(root + "Moa_Portrait_Relieved.png");
            asset.holdSeed = Sprite(root + "Moa_Pose_HoldSeed.png");
            asset.adjustCloak = Sprite(root + "Moa_Pose_AdjustCloak.png");
            asset.observe = Sprite(root + "Moa_Pose_Observe.png");
            asset.rotateLamp = Sprite(root + "Moa_Pose_RotateLamp.png");
            asset.stepForward = Sprite(root + "Moa_Pose_FirstStep.png");
            asset.celebrateQuietly = Sprite(root + "Moa_Pose_Relieved.png");
            EditorUtility.SetDirty(asset);
        }

        private static void AssignFx(GameplayFxSetAsset asset)
        {
            const string root = "Assets/Game/Art/Common/VFX/";
            asset.singleShadow = Sprite(root + "Shadow_Single.png");
            asset.overlapHazard = Sprite(root + "Shadow_Overlap.png");
            asset.cliffRim = Sprite(root + "Cliff_Tile.png");
            asset.rotateSweep = Sprite(root + "VFX_RotateSweep.png");
            asset.dangerPulse = Sprite(root + "VFX_DangerPulse.png");
            asset.doorGlow = Sprite(root + "VFX_DoorGlow.png");
            asset.flowerPetal = Sprite(root + "VFX_FlowerPetal.png");
            asset.fallDust = Sprite(root + "VFX_FallDust.png");
            asset.vacuumSwirl = Sprite(root + "VFX_TimeVacuum.png");
            asset.completionGlow = Sprite(root + "VFX_CompletionGlow.png");
            EditorUtility.SetDirty(asset);
        }

        private static void AssignAudio(AudioSetAsset asset)
        {
            asset.commonMotif = Audio("Assets/Game/Audio/Music/BGM_Lobby_GardenMap.mp3");
            asset.orchardLayer = Audio("Assets/Game/Audio/Music/BGM_W01_SunsetOrchard.mp3");
            asset.canyonLayer = Audio("Assets/Game/Audio/Music/BGM_W02_WindChimeCanyon.mp3");
            asset.greenhouseLayer = Audio("Assets/Game/Audio/Music/BGM_W03_StarrootGreenhouse.mp3");
            asset.orchardAmbience = Audio("Assets/Game/Audio/Ambience/AMB_Orchard.wav");
            asset.canyonAmbience = Audio("Assets/Game/Audio/Ambience/AMB_Canyon.wav");
            asset.greenhouseAmbience = Audio("Assets/Game/Audio/Ambience/AMB_Greenhouse.wav");
            asset.move = Audio("Assets/Game/Audio/SFX/SFX_Move.wav");
            asset.rotate = Audio("Assets/Game/Audio/SFX/SFX_Rotate.wav");
            asset.shadowCell = Audio("Assets/Game/Audio/SFX/SFX_ShadowCell.wav");
            asset.warning30 = Audio("Assets/Game/Audio/SFX/SFX_Warning30.wav");
            asset.warning10 = Audio("Assets/Game/Audio/SFX/SFX_Warning10.wav");
            asset.blocked = Audio("Assets/Game/Audio/SFX/SFX_Blocked.wav");
            asset.overlapDeath = Audio("Assets/Game/Audio/SFX/SFX_OverlapDeath.wav");
            asset.cliffDeath = Audio("Assets/Game/Audio/SFX/SFX_CliffDeath.wav");
            asset.timeDeath = Audio("Assets/Game/Audio/SFX/SFX_TimeDeath.wav");
            asset.doorOpen = Audio("Assets/Game/Audio/SFX/SFX_DoorOpen.wav");
            asset.doorPass = Audio("Assets/Game/Audio/SFX/SFX_DoorPass.wav");
            asset.flowerBloom = Audio("Assets/Game/Audio/SFX/SFX_FlowerBloom.wav");
            asset.complete = Audio("Assets/Game/Audio/SFX/SFX_Complete.wav");
            asset.uiMove = Audio("Assets/Game/Audio/SFX/SFX_UiMove.wav");
            asset.uiSubmit = Audio("Assets/Game/Audio/SFX/SFX_UiSubmit.wav");
            EditorUtility.SetDirty(asset);
        }

        private static void AssignWorld(WorldArtSetAsset asset, int world, AudioSetAsset audio)
        {
            var id = $"W0{world}";
            var root = $"Assets/Game/Art/Worlds/{id}";
            asset.worldNumber = world;
            asset.worldName = world switch { 2 => "바람종 협곡", 3 => "별뿌리 온실", _ => "노을 과수원" };
            asset.background = Sprite($"{root}/Backgrounds/{id}_Background.png");
            asset.safeTile = Sprite($"{root}/Tiles/{id}_Tile_Safe.png");
            asset.safeTileVariant = Sprite($"{root}/Tiles/{id}_Tile_Variant.png");
            asset.safeTileFlora = Sprite($"{root}/Tiles/{id}_Tile_Flora.png");
            asset.safeTileFeature = Sprite($"{root}/Tiles/{id}_Tile_Feature.png");
            asset.boardFrame = Sprite($"{root}/Tiles/{id}_Tile_SpecialA.png");
            asset.boardVoid = Sprite($"{root}/Tiles/{id}_Tile_SpecialB.png");
            asset.cliffTile = Sprite("Assets/Game/Art/Common/VFX/Cliff_Tile.png");
            asset.backDecor = Enumerable.Range(0, 3)
                .Select(index => Sprite($"{root}/Decor/{id}_Decor_{(char)('A' + index)}.png"))
                .ToArray();
            asset.frontDecor = Enumerable.Range(0, 3)
                .Select(index => Sprite($"{root}/Decor/{id}_Front_{(char)('A' + index)}.png"))
                .ToArray();
            asset.environmentReaction = Sprite($"{root}/Decor/{id}_EnvironmentReaction.png");
            asset.doorClosed = Sprite($"{root}/Goals/{id}_Door_Closed.png");
            asset.doorOpen = Sprite($"{root}/Goals/{id}_Door_Open.png");
            asset.flowerClosed = Sprite($"{root}/Goals/{id}_Flower_Closed.png");
            asset.flowerBloom = Sprite($"{root}/Goals/{id}_Flower_Bloom.png");
            asset.ambienceLoop = audio.GetWorldAmbience(world);
            asset.ambientTint = Color.white;
            asset.safeTint = Color.white;
            asset.shadowTint = world switch
            {
                2 => new Color(0.05f, 0.18f, 0.22f, 1f),
                3 => new Color(0.14f, 0.08f, 0.25f, 1f),
                _ => new Color(0.08f, 0.13f, 0.22f, 1f)
            };
            asset.reactionTint = world switch
            {
                2 => new Color(0.64f, 0.96f, 0.92f, 0.92f),
                3 => new Color(0.78f, 0.68f, 1f, 0.94f),
                _ => new Color(1f, 0.86f, 0.56f, 0.92f)
            };
            EditorUtility.SetDirty(asset);
        }

        private static void AssignCatalog(
            InGameAssetCatalogAsset asset,
            MoaAnimationSetAsset moa,
            GameplayFxSetAsset fx,
            AudioSetAsset audio)
        {
            asset.moa = moa;
            asset.gameplayFx = fx;
            asset.audio = audio;
            const string gameplay = "Assets/Game/Art/Common/Gameplay/";
            const string channels = "Assets/Game/Art/Common/Channels/";
            const string ui = "Assets/Game/Art/Common/UI/";
            asset.lampBody = Sprite(gameplay + "Lamp_Body.png");
            asset.lampArrow = Sprite(channels + "Lamp_Arrow.png");
            asset.pillarLow = Sprite(gameplay + "Pillar_Low.png");
            asset.pillarMedium = Sprite(gameplay + "Pillar_Medium.png");
            asset.pillarHigh = Sprite(gameplay + "Pillar_High.png");
            asset.channelCircle = Sprite(channels + "Channel_Circle.png");
            asset.channelTriangle = Sprite(channels + "Channel_Triangle.png");
            asset.channelStar = Sprite(channels + "Channel_Star.png");
            asset.channelDiamond = Sprite(channels + "Channel_Diamond.png");
            asset.panel = Sprite(ui + "UI_Panel_Dark.png");
            asset.panelLight = Sprite(ui + "UI_Panel_Light.png");
            asset.buttonPrimary = Sprite(ui + "UI_Button_Primary.png");
            asset.buttonSecondary = Sprite(ui + "UI_Button_Secondary.png");
            asset.buttonFocus = Sprite(ui + "UI_Focus.png");
            asset.worldCardFrame = Sprite(ui + "UI_WorldCard.png");
            asset.keyCap = Sprite(ui + "UI_KeyCap.png");
            asset.iconPause = Sprite(ui + "Icon_Pause.png");
            asset.iconDoor = Sprite(ui + "Icon_Door.png");
            asset.iconFlower = Sprite(ui + "Icon_Flower.png");
            asset.iconRetry = Sprite(ui + "Icon_Retry.png");
            asset.iconWorldMap = Sprite(ui + "Icon_WorldMap.png");
            asset.iconLock = Sprite(ui + "Icon_Lock.png");
            asset.iconCheck = Sprite(ui + "Icon_Check.png");
            asset.iconDanger = Sprite(ui + "Icon_Danger.png");
            EditorUtility.SetDirty(asset);
        }

        private static void BuildUiPrefabs(InGameAssetCatalogAsset catalog)
        {
            var path = ResourceRoot + "/Prefabs";
            EnsureFolder(path);

            var panel = new GameObject("FinalPanel", typeof(RectTransform), typeof(UnityEngine.UI.Image));
            var panelImage = panel.GetComponent<UnityEngine.UI.Image>();
            panelImage.sprite = catalog.panel;
            panelImage.type = UnityEngine.UI.Image.Type.Sliced;
            panelImage.color = Color.white;
            panelImage.raycastTarget = false;
            PrefabUtility.SaveAsPrefabAsset(panel, path + "/FinalPanel.prefab");
            UnityEngine.Object.DestroyImmediate(panel);

            var button = new GameObject(
                "FinalButton",
                typeof(RectTransform),
                typeof(UnityEngine.UI.Image),
                typeof(UnityEngine.UI.Button),
                typeof(UnityEngine.UI.Outline),
                typeof(UiFocusOutline));
            var buttonRt = button.GetComponent<RectTransform>();
            buttonRt.sizeDelta = new Vector2(UiTheme.ButtonWidth, 56f);
            var buttonImage = button.GetComponent<UnityEngine.UI.Image>();
            buttonImage.sprite = catalog.buttonPrimary;
            buttonImage.type = UnityEngine.UI.Image.Type.Sliced;
            var outline = button.GetComponent<UnityEngine.UI.Outline>();
            outline.effectColor = UiTheme.Mint;
            outline.effectDistance = new Vector2(UiTheme.FocusOutline, UiTheme.FocusOutline);
            outline.enabled = false;

            var label = new GameObject("Label", typeof(RectTransform), typeof(TMPro.TextMeshProUGUI));
            label.transform.SetParent(button.transform, false);
            var labelRt = label.GetComponent<RectTransform>();
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.offsetMin = Vector2.zero;
            labelRt.offsetMax = Vector2.zero;
            var labelText = label.GetComponent<TMPro.TextMeshProUGUI>();
            labelText.text = "버튼";
            labelText.font = UiTypography.Bold;
            labelText.fontSize = UiTheme.ButtonFont;
            labelText.alignment = TMPro.TextAlignmentOptions.Center;
            labelText.color = UiTheme.Ivory;
            labelText.raycastTarget = false;

            var focus = new GameObject("FocusFrame", typeof(RectTransform), typeof(UnityEngine.UI.Image));
            focus.transform.SetParent(button.transform, false);
            var focusRt = focus.GetComponent<RectTransform>();
            focusRt.anchorMin = Vector2.zero;
            focusRt.anchorMax = Vector2.one;
            focusRt.offsetMin = new Vector2(-5f, -5f);
            focusRt.offsetMax = new Vector2(5f, 5f);
            var focusImage = focus.GetComponent<UnityEngine.UI.Image>();
            focusImage.sprite = catalog.buttonFocus;
            focusImage.type = UnityEngine.UI.Image.Type.Sliced;
            focusImage.raycastTarget = false;
            focus.SetActive(false);
            focus.transform.SetAsFirstSibling();
            PrefabUtility.SaveAsPrefabAsset(button, path + "/FinalButton.prefab");
            UnityEngine.Object.DestroyImmediate(button);
        }

        private static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;
            EnsureFolder(Path.GetDirectoryName(path)?.Replace('\\', '/'));
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static Sprite Sprite(string path) => AssetDatabase.LoadAssetAtPath<Sprite>(path);
        private static AudioClip Audio(string path) => AssetDatabase.LoadAssetAtPath<AudioClip>(path);

        private static void EnsureFolder(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || AssetDatabase.IsValidFolder(path)) return;
            var normalized = path.Replace('\\', '/');
            var parent = normalized.Substring(0, normalized.LastIndexOf('/'));
            var name = normalized.Substring(normalized.LastIndexOf('/') + 1);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }
    }
}
#endif
