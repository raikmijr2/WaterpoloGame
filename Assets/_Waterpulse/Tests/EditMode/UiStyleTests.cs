using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Waterpulse.EditorTools;
using Waterpulse.UI;

namespace Waterpulse.Tests.EditMode
{
    // Estilo de la interfaz: formas generadas, fuentes Barlow y botones que cambian el color del texto al seleccionarlos
    public class UiStyleTests
    {
        [Test]
        public void EnsureSprites_CreatesEveryShapeOnce_AsSprites()
        {
            UiSpriteGenerator.EnsureSprites();
            Assert.AreEqual(0, UiSpriteGenerator.EnsureSprites(), "la segunda vez no crea nada");
            foreach (string name in UiSpriteGenerator.SpriteNames())
                Assert.IsNotNull(UiSpriteGenerator.Load(name), name);
            Assert.Greater(UiSpriteGenerator.Load(UiSpriteGenerator.Rounded).border.x, 0f, "los paneles se estiran con 9-slice");
            Assert.Greater(UiSpriteGenerator.Load(UiSpriteGenerator.Pill).border.x, 0f, "las barras también");
        }

        [Test]
        public void MenuBuild_EveryTextUsesBarlow()
        {
            Assume.That(AssetDatabase.LoadAssetAtPath<Font>(UiFactory.DisplayFontPath), Is.Not.Null, "faltan las fuentes en Art/Fonts");
            MenuSceneBuilder.Build();

            foreach (var text in Object.FindObjectsByType<Text>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                StringAssert.StartsWith("Barlow", text.font.name, text.name);
        }

        [Test]
        public void MenuBuild_EveryButtonChangesItsTextColorWhenSelected()
        {
            MenuSceneBuilder.Build();

            var buttons = Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Assert.IsNotEmpty(buttons);
            foreach (var button in buttons)
            {
                var textColor = button.GetComponent<SelectionTextColor>();
                Assert.IsNotNull(textColor, button.name);
                Assert.AreEqual(button.GetComponentInChildren<Text>(true), new SerializedObject(textColor).FindProperty("label").objectReferenceValue, button.name);
            }
        }
    }
}
