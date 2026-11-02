using UnityEditor;
using UnityEngine;
using Waterpulse.UI;

namespace Waterpulse.EditorTools
{
    // Para probar los controles de móvil en el ordenador: con la opción marcada, al darle a Play salen los controles
    // táctiles (se tocan con el ratón) y no la guía de teclado. Solo en el editor; el build decide solo
    public static class ForceTouchControlsMenu
    {
        const string MenuPath = "Waterpulse/Forçar controls de mòbil (editor)";

        [MenuItem(MenuPath, priority = 30)]
        static void Toggle()
        {
            bool on = PlayerPrefs.GetInt(TouchControls.ForceInEditorKey, 0) == 1;
            PlayerPrefs.SetInt(TouchControls.ForceInEditorKey, on ? 0 : 1);
            PlayerPrefs.Save();
            Debug.Log($"[Waterpulse] Controls de mòbil forçats a l'editor: {(on ? "no" : "sí")}");
        }

        [MenuItem(MenuPath, true)]
        static bool Validate()
        {
            Menu.SetChecked(MenuPath, PlayerPrefs.GetInt(TouchControls.ForceInEditorKey, 0) == 1);
            return true;
        }
    }
}
