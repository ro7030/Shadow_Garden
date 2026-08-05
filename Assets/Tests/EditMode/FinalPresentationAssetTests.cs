using System.IO;
using System.Linq;
using NUnit.Framework;
using ShadowGarden.Presentation;
using TMPro;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.U2D;

namespace ShadowGarden.Tests.EditMode
{
    public sealed class FinalPresentationAssetTests
    {
        [SetUp]
        public void ResetLibrary() => PresentationAssetLibrary.ResetCache();

        [Test]
        public void Catalog_And_All_Stage_Bindings_Are_Complete()
        {
            var catalog = PresentationAssetLibrary.Catalog;
            Assert.IsNotNull(catalog);
            Assert.IsNotNull(catalog.moa);
            Assert.IsNotNull(catalog.gameplayFx);
            Assert.IsNotNull(catalog.audio);
            Assert.IsNotNull(catalog.lampBody);
            Assert.IsNotNull(catalog.pillarLow);
            Assert.IsNotNull(catalog.pillarMedium);
            Assert.IsNotNull(catalog.pillarHigh);
            Assert.IsNotNull(catalog.channelCircle);
            Assert.IsNotNull(catalog.channelTriangle);
            Assert.IsNotNull(catalog.channelStar);
            Assert.IsNotNull(catalog.channelDiamond);
            Assert.IsNotNull(catalog.panel);
            Assert.IsNotNull(catalog.buttonPrimary);
            Assert.IsNotNull(catalog.iconPause);

            for (var world = 1; world <= 3; world++)
            for (var stage = 1; stage <= 4; stage++)
            {
                var id = $"{world}-{stage}";
                var art = PresentationAssetLibrary.ForStage(id);
                Assert.IsNotNull(art, id);
                Assert.AreEqual(world, art.worldNumber, id);
                Assert.IsNotNull(art.background, id);
                Assert.IsNotNull(art.safeTile, id);
                Assert.IsNotNull(art.boardVoid, id);
                Assert.IsNotNull(art.doorClosed, id);
                Assert.IsNotNull(art.doorOpen, id);
                Assert.IsNotNull(art.flowerClosed, id);
                Assert.IsNotNull(art.flowerBloom, id);
            }
        }

        [Test]
        public void Pillar_Family_Has_Equal_Diameters_And_Clear_Height_Steps()
        {
            var catalog = PresentationAssetLibrary.Catalog;
            var low = AlphaBounds(catalog.pillarLow);
            var medium = AlphaBounds(catalog.pillarMedium);
            var high = AlphaBounds(catalog.pillarHigh);

            Assert.That(low.width, Is.EqualTo(high.width).Within(1), "Low pillar diameter");
            Assert.That(medium.width, Is.EqualTo(high.width).Within(1), "Medium pillar diameter");
            Assert.GreaterOrEqual(medium.height - low.height, 40, "Low/medium height must read immediately");
            Assert.GreaterOrEqual(high.height - medium.height, 40, "Medium/high height must read immediately");
        }

        private static RectInt AlphaBounds(Sprite sprite)
        {
            Assert.IsNotNull(sprite);
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            Assert.IsTrue(texture.LoadImage(File.ReadAllBytes(AssetDatabase.GetAssetPath(sprite))));
            var minX = texture.width;
            var minY = texture.height;
            var maxX = -1;
            var maxY = -1;
            for (var y = 0; y < texture.height; y++)
            for (var x = 0; x < texture.width; x++)
            {
                if (texture.GetPixel(x, y).a <= 0.01f) continue;
                minX = Mathf.Min(minX, x);
                minY = Mathf.Min(minY, y);
                maxX = Mathf.Max(maxX, x);
                maxY = Mathf.Max(maxY, y);
            }

            Object.DestroyImmediate(texture);
            return new RectInt(minX, minY, maxX - minX + 1, maxY - minY + 1);
        }

        [Test]
        public void Runtime_Sprites_Follow_Ppu_Filter_And_Pivot_Contract()
        {
            var guids = AssetDatabase.FindAssets(
                "t:Texture2D",
                new[] { "Assets/Game/Art/Common", "Assets/Game/Art/Worlds" });
            Assert.Greater(guids.Length, 40);
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.EndsWith(".png")) continue;
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                Assert.IsNotNull(importer, path);
                Assert.AreEqual(TextureImporterType.Sprite, importer.textureType, path);
                Assert.AreEqual(path.Contains("/UI/") ? 100f : 128f, importer.spritePixelsPerUnit, 0.01f, path);
                Assert.AreEqual(FilterMode.Bilinear, importer.filterMode, path);
                Assert.IsFalse(importer.mipmapEnabled, path);
            }
        }

        [Test]
        public void Six_Atlases_Use_No_Rotation_And_Four_Pixel_Padding()
        {
            var paths = new[] { "CommonGameplay", "World01", "World02", "World03", "UI", "VFX" };
            foreach (var name in paths)
            {
                var atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>($"Assets/Game/Atlases/{name}.spriteatlas");
                Assert.IsNotNull(atlas, name);
                var settings = atlas.GetPackingSettings();
                Assert.IsFalse(settings.enableRotation, name);
                Assert.GreaterOrEqual(settings.padding, 4, name);
            }
        }

        [Test]
        public void World_Art_Uses_Exact_Backgrounds_And_Independent_Foreground_Assets()
        {
            for (var world = 1; world <= 3; world++)
            {
                var art = PresentationAssetLibrary.ForStage($"{world}-1");
                Assert.IsNotNull(art, $"W0{world}");
                Assert.IsNotNull(art.background, $"W0{world} background");
                Assert.AreEqual(2048, art.background.texture.width, $"W0{world} background width");
                Assert.AreEqual(1152, art.background.texture.height, $"W0{world} background height");
                Assert.AreEqual(3, art.frontDecor?.Length ?? 0, $"W0{world} front decor count");
                Assert.IsTrue(art.frontDecor.All(sprite => sprite != null), $"W0{world} front decor missing");
                Assert.IsNotNull(art.environmentReaction, $"W0{world} reaction");
                Assert.IsFalse(art.frontDecor.Any(front => art.backDecor.Contains(front)),
                    $"W0{world} foreground must not reuse reversed background decor");
                foreach (var front in art.frontDecor)
                foreach (var back in art.backDecor)
                {
                    var frontBytes = File.ReadAllBytes(AssetDatabase.GetAssetPath(front));
                    var backBytes = File.ReadAllBytes(AssetDatabase.GetAssetPath(back));
                    Assert.IsFalse(frontBytes.SequenceEqual(backBytes),
                        $"W0{world} foreground must be independently authored, not a copied back decor");
                }
            }
        }

        [Test]
        public void Gameplay_Fx_Has_Distinct_Door_Flower_Dust_And_Danger_Sprites()
        {
            var fx = PresentationAssetLibrary.Catalog.gameplayFx;
            Assert.IsNotNull(fx);
            var authored = new[]
            {
                fx.dangerPulse,
                fx.doorGlow,
                fx.flowerPetal,
                fx.fallDust,
                fx.completionGlow
            };
            Assert.IsTrue(authored.All(sprite => sprite != null));
            Assert.AreEqual(authored.Length, authored.Distinct().Count(),
                "Documented gameplay beats need independent authored sprites.");
        }

        [Test]
        public void Final_Ui_Base_Prefabs_Are_Available()
        {
            var panel = Resources.Load<GameObject>("Presentation/Prefabs/FinalPanel");
            var button = Resources.Load<GameObject>("Presentation/Prefabs/FinalButton");
            Assert.IsNotNull(panel);
            Assert.IsNotNull(button);
            Assert.IsFalse(panel.GetComponentsInChildren<Component>(true).Any(component => component == null));
            Assert.IsFalse(button.GetComponentsInChildren<Component>(true).Any(component => component == null));
            Assert.IsNotNull(button.GetComponent<UiFocusOutline>());
        }

        [Test]
        public void Presenters_Do_Not_Allocate_Texture_Or_Material_At_Runtime()
        {
            var root = Path.GetFullPath(Path.Combine(Application.dataPath, "Scripts", "Presentation"));
            foreach (var file in new[] { "BoardPresenter.cs", "PlayerPresenter.cs" })
            {
                var source = File.ReadAllText(Path.Combine(root, file));
                StringAssert.DoesNotContain("new Texture2D", source, file);
                StringAssert.DoesNotContain("new Material", source, file);
                StringAssert.DoesNotContain("CreatePrimitive", source, file);
            }
        }

        [Test]
        public void Navigation_Glyphs_Exist_In_Runtime_Font_Chain()
        {
            var fonts = new[] { UiTypography.Regular, UiTypography.Bold, UiTypography.Symbols, UiTypography.Ornaments }
                .Where(font => font != null)
                .Distinct()
                .ToArray();
            Assert.Greater(fonts.Length, 0);
            foreach (var character in UiTypography.SymbolCorpus)
            {
                Assert.IsTrue(fonts.Any(font => font.HasCharacter(character)), $"Missing TMP glyph U+{(int)character:X4}");
            }
        }

        [Test]
        public void Main_Scene_EventSystem_Has_Resolvable_Ui_Input_Actions()
        {
            var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(
                "Assets/Scenes/Main.unity",
                UnityEditor.SceneManagement.OpenSceneMode.Single);
            Assert.IsTrue(scene.IsValid());

            var eventSystem = Object.FindFirstObjectByType<EventSystem>();
            var module = Object.FindFirstObjectByType<InputSystemUIInputModule>();
            Assert.IsNotNull(eventSystem);
            Assert.IsNotNull(module);
            Assert.IsNotNull(module.actionsAsset);
            Assert.IsNotNull(module.point);
            Assert.IsNotNull(module.move);
            Assert.IsNotNull(module.submit);
            Assert.IsNotNull(module.leftClick);
        }
    }
}
