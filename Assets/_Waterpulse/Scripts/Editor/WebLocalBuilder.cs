using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Waterpulse.EditorTools
{
    // Build Web para el concurso (Premis LUDI): el juego se tiene que poder jugar "en local, sense cap necessitat de
    // connexió externa, obrint el fitxer index.html". Un build Web normal de Unity no lo cumple: el navegador no deja
    // cargar sus archivos (.data, .wasm, .framework.js) desde file:// sin un servidor.
    // Este menú hace el build sin compresión y después mete los cuatro archivos dentro de un solo index.html
    // (en base64): al abrirlo, los convierte en URLs blob: y se los pasa al cargador de Unity, que ya no pide nada fuera
    public static class WebLocalBuilder
    {
        public const string BuildDir = "Builds/Web";             // build normal de Unity (necesita servidor)
        public const string LocalDir = "Builds/WebLocal";        // un solo index.html que se abre con doble clic
        const string ProductTitle = "Waterpulse";
        const string LoadingText = "Carregant…";

        [MenuItem("Waterpulse/Build Web (index.html local)", priority = 20)]
        static void BuildFromMenu()
        {
            string html = Build();
            if (html != null) EditorUtility.RevealInFinder(html);
        }

        // Para la línea de comandos: Unity -batchmode -executeMethod Waterpulse.EditorTools.WebLocalBuilder.BuildBatch -quit
        public static void BuildBatch()
        {
            if (Build() == null) EditorApplication.Exit(1);
        }

        // Devuelve la ruta del index.html local, o null si el build falla
        public static string Build()
        {
            // Sin compresión: los .br/.gz necesitan cabeceras de un servidor. Sin caché: IndexedDB no sirve desde file://
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
            PlayerSettings.WebGL.decompressionFallback = false;
            PlayerSettings.WebGL.dataCaching = false;
            PlayerSettings.WebGL.nameFilesAsHashes = false;

            var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            if (scenes.Length == 0)
            {
                Debug.LogError("[Waterpulse] No hay escenas en Build Settings: ejecuta Waterpulse/Build Menu Scene y Build Match Scene.");
                return null;
            }

            var report = BuildPipeline.BuildPlayer(scenes, BuildDir, BuildTarget.WebGL, BuildOptions.None);
            if (report.summary.result != BuildResult.Succeeded)
            {
                Debug.LogError($"[Waterpulse] El build Web ha fallado: {report.summary.result}. ¿Está instalado el módulo Web Build Support?");
                return null;
            }

            string html = Pack(BuildDir, LocalDir);
            Debug.Log($"[Waterpulse] Build Web local listo: {Path.GetFullPath(html)} (se abre con doble clic, sin servidor).");
            return html;
        }

        // Junta el build de BuildDir en un solo index.html en outDir. Devuelve su ruta
        public static string Pack(string buildDir, string outDir)
        {
            string build = Path.Combine(buildDir, "Build");
            string loader = Single(build, "*.loader.js");
            string framework = Single(build, "*.framework.js");
            string data = Single(build, "*.data");
            string wasm = Single(build, "*.wasm");

            var sb = new StringBuilder();
            sb.Append("<!DOCTYPE html>\n<html lang=\"ca\">\n<head>\n<meta charset=\"utf-8\">\n");
            sb.Append("<meta name=\"viewport\" content=\"width=device-width, height=device-height, initial-scale=1.0, user-scalable=no, shrink-to-fit=yes\">\n");
            sb.Append($"<title>{ProductTitle}</title>\n");
            sb.Append("<style>html,body{margin:0;width:100%;height:100%;overflow:hidden;background:#0a0f15}");
            sb.Append("#unity-canvas{width:100%;height:100%;display:block;background:#0a0f15;touch-action:none}");
            sb.Append("#loading{position:fixed;inset:0;display:flex;flex-direction:column;align-items:center;justify-content:center;gap:14px;");
            sb.Append("color:#e7eef4;font:600 20px system-ui,sans-serif;letter-spacing:.08em;text-transform:uppercase}");
            sb.Append("#bar{width:min(320px,70vw);height:6px;background:#22313f}#fill{width:0;height:100%;background:#d4ff3f}</style>\n");
            sb.Append("</head>\n<body>\n<canvas id=\"unity-canvas\" tabindex=\"-1\"></canvas>\n");
            sb.Append($"<div id=\"loading\"><div>{LoadingText}</div><div id=\"bar\"><div id=\"fill\"></div></div></div>\n");

            // Cargador de Unity tal cual (define createUnityInstance)
            sb.Append("<script>\n").Append(File.ReadAllText(loader)).Append("\n</script>\n");

            sb.Append("<script>\n");
            sb.Append("function toBlobUrl(b64, type) {\n");
            sb.Append("  var bin = atob(b64), bytes = new Uint8Array(bin.length);\n");
            sb.Append("  for (var i = 0; i < bin.length; i++) bytes[i] = bin.charCodeAt(i);\n");
            sb.Append("  return URL.createObjectURL(new Blob([bytes], { type: type }));\n}\n");
            AppendBase64Var(sb, "DATA", data);
            AppendBase64Var(sb, "FRAMEWORK", framework);
            AppendBase64Var(sb, "CODE", wasm);
            sb.Append("var canvas = document.getElementById('unity-canvas');\n");
            sb.Append("createUnityInstance(canvas, {\n");
            sb.Append("  dataUrl: toBlobUrl(DATA, 'application/octet-stream'),\n");
            sb.Append("  frameworkUrl: toBlobUrl(FRAMEWORK, 'application/javascript'),\n");
            sb.Append("  codeUrl: toBlobUrl(CODE, 'application/wasm'),\n");
            sb.Append("  streamingAssetsUrl: 'StreamingAssets',\n");
            sb.Append($"  companyName: {Js(PlayerSettings.companyName)},\n");
            sb.Append($"  productName: {Js(PlayerSettings.productName)},\n");
            sb.Append($"  productVersion: {Js(PlayerSettings.bundleVersion)},\n");
            sb.Append("  matchWebGLToCanvasSize: true\n");
            sb.Append("}, function (p) { document.getElementById('fill').style.width = (100 * p) + '%'; })\n");
            sb.Append(".then(function () { document.getElementById('loading').remove(); DATA = FRAMEWORK = CODE = null; })\n");
            sb.Append(".catch(function (e) { document.getElementById('loading').firstChild.textContent = 'Error: ' + e; });\n");
            sb.Append("</script>\n</body>\n</html>\n");

            Directory.CreateDirectory(outDir);
            string html = Path.Combine(outDir, "index.html");
            File.WriteAllText(html, sb.ToString(), new UTF8Encoding(false));
            return html;
        }

        static void AppendBase64Var(StringBuilder sb, string name, string path)
        {
            sb.Append("var ").Append(name).Append(" = \"").Append(Convert.ToBase64String(File.ReadAllBytes(path))).Append("\";\n");
        }

        static string Js(string text) => "\"" + (text ?? "").Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";

        static string Single(string dir, string pattern)
        {
            var files = Directory.GetFiles(dir, pattern);
            if (files.Length != 1) throw new FileNotFoundException($"Se esperaba un archivo {pattern} en {dir} y hay {files.Length}");
            return files[0];
        }
    }
}
