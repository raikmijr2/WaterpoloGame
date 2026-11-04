using UnityEditor;
using UnityEngine;

namespace Waterpulse.EditorTools
{
    // Arreglo para cuando los menús no responden al ratón: si cambia la versión del paquet Input System (p. ej. al abrir
    // el proyecto con otra versión de Unity), sus archivos de acciones pueden quedar mal importados y la interfaz se queda
    // sin las acciones de apuntar y hacer clic. Esto los vuelve a importar.
    // Desde la línea de comandos: Unity -batchmode -executeMethod Waterpulse.EditorTools.InputActionsReimporter.Reimport -quit
    public static class InputActionsReimporter
    {
        static readonly string[] Paths =
        {
            "Packages/com.unity.inputsystem/InputSystem/Plugins/PlayerInput/DefaultInputActions.inputactions",
            "Assets/InputSystem_Actions.inputactions",
        };

        [MenuItem("Waterpulse/Reimportar accions d'entrada", priority = 40)]
        public static void Reimport()
        {
            foreach (string path in Paths)
            {
                if (AssetDatabase.LoadMainAssetAtPath(path) == null && !System.IO.File.Exists(path) && !path.StartsWith("Packages/")) continue;
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                Debug.Log($"[Waterpulse] Reimportat: {path}");
            }
        }
    }
}
