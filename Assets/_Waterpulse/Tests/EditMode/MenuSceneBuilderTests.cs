using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Waterpulse.Config;
using Waterpulse.EditorTools;
using Waterpulse.Gameplay;
using Waterpulse.UI;

namespace Waterpulse.Tests.EditMode
{
    // El generador del menú debe poder ejecutarse varias veces sin duplicar objetos ni assets
    public class MenuSceneBuilderTests
    {
        [Test]
        public void Build_Twice_DoesNotDuplicateObjects()
        {
            MenuSceneBuilder.Build();
            MenuSceneBuilder.Build();

            Assert.AreEqual(1, Count<MenuFlow>(), "MenuFlow");
            Assert.AreEqual(1, Count<IntroScreen>(), "intro");
            Assert.AreEqual(1, Count<OptionsMenu>(), "opciones");
            Assert.AreEqual(1, Count<MenuCamera>(), "cámara del menú");
            Assert.AreEqual(1, Count<Camera>(), "cámaras");
            Assert.AreEqual(1, Count<EventSystem>(), "EventSystem");
            Assert.AreEqual(1, Count<PoolBounds>(), "piscina de fondo");
            Assert.AreEqual(0, Count<PlayerController>(), "sin jugadores");
            Assert.AreEqual(0, Count<BallController>(), "la pelota del fondo es solo decorado");
            Assert.AreEqual(1, AssetDatabase.FindAssets("t:" + nameof(MenuConfigSO)).Length, "MenuConfig");
        }

        [Test]
        public void Build_WiresEveryReference()
        {
            MenuSceneBuilder.Build();

            var config = AssetDatabase.LoadAssetAtPath<MenuConfigSO>(MenuSceneBuilder.ConfigPath);
            Assert.IsNotNull(config, "MenuConfig.asset");
            foreach (var component in new MonoBehaviour[]
                     { Object.FindFirstObjectByType<MenuFlow>(), Object.FindFirstObjectByType<IntroScreen>(),
                       Object.FindFirstObjectByType<OptionsMenu>(), Object.FindFirstObjectByType<MenuCamera>() })
            {
                AssertRef(component, "config", config);
                AssertNoMissingReferences(component);
            }
            AssertRef(Object.FindFirstObjectByType<MenuCamera>(), "pool", Object.FindFirstObjectByType<PoolBounds>());

            // Usuarios y rànquing: su config, la de puntuación (claves del récord) y nada vacío
            var users = AssetDatabase.LoadAssetAtPath<UsersConfigSO>(MatchSceneBuilder.UsersConfigPath);
            var scoring = AssetDatabase.LoadAssetAtPath<ScoringConfigSO>(MatchSceneBuilder.ScoringConfigPath);
            Assert.IsNotNull(users, "UsersConfig.asset");
            foreach (var component in new MonoBehaviour[] { Object.FindFirstObjectByType<UsersMenu>(), Object.FindFirstObjectByType<RankingView>() })
            {
                AssertRef(component, "config", users);
                AssertRef(component, "scoringConfig", scoring);
                AssertNoMissingReferences(component);
            }
            AssertRef(Object.FindFirstObjectByType<MenuFlow>(), "users", Object.FindFirstObjectByType<UsersMenu>());
        }

        [Test]
        public void Build_HasMenuAudio_AndSoundOnEveryButton()
        {
            MenuSceneBuilder.Build();

            var directors = Object.FindObjectsByType<Waterpulse.Audio.AudioDirector>(FindObjectsSortMode.None);
            Assert.AreEqual(1, directors.Length, "AudioDirector");
            Assert.AreEqual((int)Waterpulse.Audio.AudioScene.Menu, new SerializedObject(directors[0]).FindProperty("scene").intValue);
            Assert.IsNotNull(new SerializedObject(directors[0]).FindProperty("config").objectReferenceValue, "AudioConfig");

            // Las filas de usuarios se clonan de la plantilla: también llevan sonido
            foreach (var selectable in Object.FindObjectsByType<Selectable>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (selectable is InputField) continue;
                Assert.IsNotNull(selectable.GetComponent<Waterpulse.Audio.UiSound>(), selectable.name);
            }
        }

        [Test]
        public void Build_UsersPage_TemplateHiddenAndNameFieldWithoutNavigation()
        {
            MenuSceneBuilder.Build();

            var so = new SerializedObject(Object.FindFirstObjectByType<UsersMenu>());
            var slot = (UserSlot)so.FindProperty("slotTemplate").objectReferenceValue;
            Assert.IsFalse(slot.gameObject.activeSelf, "la plantilla no se ve: se clona por usuario");
            AssertNoMissingReferences(slot);

            // Con el Input System, escribir W, A, S o D en el campo movería la selección si tuviera navegación
            var input = (InputField)so.FindProperty("nameInput").objectReferenceValue;
            Assert.AreEqual(Navigation.Mode.None, input.navigation.mode);
            var users = AssetDatabase.LoadAssetAtPath<UsersConfigSO>(MatchSceneBuilder.UsersConfigPath);
            Assert.AreEqual(users.maxNameLength, input.characterLimit, "longitud máxima del nombre");
        }

        [Test]
        public void Build_LocalVersusIsVisibleButDisabled()
        {
            MenuSceneBuilder.Build();

            var versus = Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                               .Single(b => b.name == "LocalVersusButton");
            Assert.IsFalse(versus.interactable, "el modo aún no existe");
            Assert.IsTrue(versus.GetComponentsInChildren<Text>(true).Any(t => t.text == "Aviat"), "etiqueta Aviat");
        }

        [Test]
        public void Build_FullscreenButtonActsOnPress()
        {
            MenuSceneBuilder.Build();

            var button = new SerializedObject(Object.FindFirstObjectByType<OptionsMenu>()).FindProperty("fullscreenButton").objectReferenceValue;
            Assert.IsInstanceOf<PressButton>(button, "en Web la pantalla completa se pide al pulsar, no al soltar");
        }

        [Test]
        public void Build_SceneNamesInConfigMatchTheGeneratedScenes()
        {
            MenuSceneBuilder.Build();

            var config = AssetDatabase.LoadAssetAtPath<MenuConfigSO>(MenuSceneBuilder.ConfigPath);
            Assert.AreEqual(Path.GetFileNameWithoutExtension(MatchSceneBuilder.ScenePath), config.matchScene, "partido");
            Assert.AreEqual(Path.GetFileNameWithoutExtension(MenuSceneBuilder.ScenePath), config.menuScene, "menú");
        }

        [Test]
        public void BothBuilders_RegisterMenuFirstThenMatch()
        {
            var expected = new[] { MenuSceneBuilder.ScenePath, MatchSceneBuilder.ScenePath };

            MatchSceneBuilder.Build();
            MenuSceneBuilder.Build();
            CollectionAssert.AreEqual(expected, EditorBuildSettings.scenes.Select(s => s.path).ToArray(), "menú y luego partido");

            MatchSceneBuilder.Build();
            CollectionAssert.AreEqual(expected, EditorBuildSettings.scenes.Select(s => s.path).ToArray(), "regenerar el partido no quita el menú");
            Assert.IsTrue(EditorBuildSettings.scenes.All(s => s.enabled), "todas activas");
        }

        static int Count<T>() where T : Object => Object.FindObjectsByType<T>(FindObjectsSortMode.None).Length;

        static void AssertRef(Object target, string field, Object expected)
        {
            Assert.IsNotNull(target, $"falta {field}");
            var value = new SerializedObject(target).FindProperty(field).objectReferenceValue;
            Assert.AreEqual(expected, value, $"{target.GetType().Name}.{field}");
        }

        // Ninguna referencia del inspector se queda vacía
        static void AssertNoMissingReferences(Object target)
        {
            var property = new SerializedObject(target).GetIterator();
            while (property.NextVisible(true))
            {
                if (property.propertyType != SerializedPropertyType.ObjectReference || property.name == "m_Script") continue;
                Assert.IsNotNull(property.objectReferenceValue, $"{target.GetType().Name}.{property.name}");
            }
        }
    }
}
