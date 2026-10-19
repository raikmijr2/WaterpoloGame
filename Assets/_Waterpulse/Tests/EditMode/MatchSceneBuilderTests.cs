using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Waterpulse.Config;
using Waterpulse.EditorTools;
using Waterpulse.Gameplay;

namespace Waterpulse.Tests.EditMode
{
    // El generador debe poder ejecutarse varias veces sin duplicar objetos ni assets
    public class MatchSceneBuilderTests
    {
        [Test]
        public void Build_Twice_DoesNotDuplicateObjects()
        {
            MatchSceneBuilder.Build();
            MatchSceneBuilder.Build();

            Assert.AreEqual(1, Object.FindObjectsByType<PlayerController>(FindObjectsSortMode.None).Length, "jugadores");
            Assert.AreEqual(1, Object.FindObjectsByType<PoolBounds>(FindObjectsSortMode.None).Length, "piscinas");
            Assert.AreEqual(1, Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).Length, "cámaras");
            int goals = Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Count(t => t.name.StartsWith("Goal_"));
            Assert.AreEqual(2, goals, "porterías");
        }

        [Test]
        public void Build_Twice_KeepsSingleConfigAsset()
        {
            MatchSceneBuilder.Build();
            MatchSceneBuilder.Build();

            Assert.AreEqual(1, AssetDatabase.FindAssets("t:" + nameof(GameConfigSO)).Length);
        }

        [Test]
        public void Build_WiresConfigReferences()
        {
            MatchSceneBuilder.Build();

            var config = AssetDatabase.LoadAssetAtPath<GameConfigSO>(MatchSceneBuilder.ConfigPath);
            Assert.IsNotNull(config);
            AssertRef(Object.FindFirstObjectByType<PlayerController>(), "config", config);
            AssertRef(Object.FindFirstObjectByType<PoolBounds>(), "config", config);
            AssertRef(Object.FindFirstObjectByType<MatchCamera>(), "config", config);
            AssertRef(Object.FindFirstObjectByType<MatchCamera>(), "pool", Object.FindFirstObjectByType<PoolBounds>());
        }

        static void AssertRef(Object target, string field, Object expected)
        {
            Assert.IsNotNull(target, $"falta {field}");
            var value = new SerializedObject(target).FindProperty(field).objectReferenceValue;
            Assert.AreEqual(expected, value, $"{target.GetType().Name}.{field}");
        }
    }
}
