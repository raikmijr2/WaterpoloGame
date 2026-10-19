# Waterpulse — Instrucciones para Claude Code

Juego de waterpolo arcade en **Unity 6** con build **Web** (PC, móvil y tablet). Partidos de 2–3 minutos, 3 jugadores de campo + 1 portero por equipo, habilidades especiales con energía. El diseño completo está en `docs/gdd.md`: léelo antes de cada fase y trátalo como la fuente de verdad.

---

## Forma de trabajar (obligatorio)

1. **Una fase cada vez.** Trabaja solo en la fase marcada como `EN CURSO` en la sección *Estado*. No adelantes trabajo de fases posteriores.
2. **Plan primero.** Al empezar una fase, lee `docs/gdd.md` y el código existente, y presenta un plan: archivos a crear o modificar, clases y responsabilidades, y lo que tendré que hacer yo en el editor. Espera mi aprobación antes de escribir código.
3. **No inventes reglas.** Si algo no está en el GDD ni en este archivo (o aparece en *Decisiones pendientes*), pregúntame. No decidas en silencio.
4. **Cierre de fase.** Al terminar:
   - Ejecuta los tests EditMode y confirma que pasan.
   - Dame una checklist de prueba manual en el editor (qué pulsar y qué debería pasar).
   - Lista lo que tengo que hacer yo en el editor, si hay algo.
   - Actualiza la sección *Estado* de este archivo.
   - Propón el mensaje de commit.
   - **Detente y espera** a que confirme que funciona antes de pasar a la siguiente fase.
5. **Si algo falla**, explica la causa probable antes de cambiar código. No reescribas sistemas enteros para arreglar un bug pequeño.

---

## Reglas técnicas

- **Unity 6, URP (3D, Universal Renderer), New Input System, Unity Test Framework.** El juego es **3D**: la jugabilidad ocurre en el plano XZ del agua y la cámara es cenital o ligeramente inclinada. C# con nombres en inglés; textos visibles del juego en catalán.
- **Nunca edites a mano archivos `.unity`, `.prefab` ni `.asset`.** Para crear escenas, prefabs o assets de configuración, escribe **scripts de Editor** en `Scripts/Editor/` con menús `Waterpulse/...` que lo generen. Deben poder ejecutarse varias veces sin duplicar objetos.
- **Cero números mágicos.** Velocidades, costes, duraciones, puntos y tiempos van en ScriptableObjects de `Scripts/Config/`, con sus valores por defecto tomados del GDD.
- **Lógica pura separada.** Energía, puntuación, combo, estadísticas y récord son clases C# normales en `Scripts/Logic/`, sin `MonoBehaviour` ni `UnityEngine` salvo `Mathf`, y con tests EditMode.
- **Input unificado.** Todo jugador (humano o IA) se controla mediante `IPlayerBrain`, que devuelve un `PlayerIntent { Vector2 Move; bool Pass; bool Shoot; bool Ability; }`. `PlayerController` solo ejecuta intenciones y no sabe quién lo controla.
- **Eventos desacoplados.** Los sistemas se comunican por `GameEvents`: `Goal`, `PassCompleted`, `Interception`, `Save`, `AbilityUsed`, `PossessionChanged`, `MatchStateChanged`. HUD, puntuación, audio y estadísticas escuchan; no se referencian entre sí.
- **Pelota con estados explícitos:** `Free`, `Held(owner)`, `InFlight(kind: Pass|Shot, from, target)`. Solo `BallController` cambia el estado.
- **Física:** `Rigidbody` (3D) con `useGravity = false`, posición Y y rotaciones X/Z congeladas, y `linearDamping` configurable para la sensación de agua. `PlayerIntent.Move` (Vector2) se aplica en el plano XZ. No se simula el agua.
- **Assembly Definitions:** una por carpeta de scripts. `Logic` no depende de nada del juego; `Gameplay` depende de `Logic`, `Config` y `Core`.
  - `IPlayerBrain` y `PlayerIntent` viven en `Core` (no en `Brains`) para evitar la dependencia circular Gameplay ↔ Brains. `Brains` contiene las implementaciones (`HumanBrain`, `AIBrain`).
  - Dependencias: `Logic` → nada · `Config` → nada · `Core` → Config · `Gameplay` → Logic, Config, Core · `AI` → Core, Config, Gameplay · `Brains` → Core, Config, Gameplay, AI, InputSystem · `Abilities` → Core, Config, Logic, Gameplay · `UI` → Core, Config, Logic, Gameplay, InputSystem, uGUI · `Audio` → Core, Config · `Editor` → todas (solo editor) · `Tests.EditMode` → Logic.
- **Rendimiento Web:** nada de reflexión pesada, threads ni APIs no soportadas en WebGL.

### Estructura de carpetas

```
Assets/_Waterpulse/
├── Scripts/
│   ├── Core/          GameEvents, MatchStateMachine, MatchTimer, IPlayerBrain, PlayerIntent
│   ├── Config/        GameConfigSO, AbilityDefinitionSO, ScoringConfigSO, EnergyConfigSO, DifficultySO
│   ├── Logic/         EnergyMeter, ScoreTracker, ComboMultiplier, MatchStats, RecordStorage
│   ├── Gameplay/      PlayerController, BallController, Goal, PoolBounds, GoalkeeperController
│   ├── Brains/        HumanBrain, AIBrain
│   ├── AI/            TeamAI, RolePositioning
│   ├── Abilities/     AbilityBehaviour, XutTragolAbility, TorpedeBlauAbility
│   ├── UI/            HUD, ResultsScreen, TouchControls
│   ├── Audio/         AudioDirector
│   └── Editor/        Generadores de escenas, prefabs y assets
├── Tests/EditMode/
├── Config/            Assets .asset de configuración
├── Prefabs/  Scenes/  Art/  Audio/
```

---

## Valores por defecto (del GDD, ajustables en Config)

| Concepto | Valor |
| --- | --- |
| Energía máxima | 100 |
| Energía: pase completado / recuperación / gol | +5 / +10 / +10 |
| Xut del Tràngol | 35 de energía, tiro cargado muy potente; el portero puede pararlo |
| Torpede Blau | 25 de energía, acelerón de ~1 s |
| Puntos: gol / gol con habilidad / combo 3 pases / parada / intercepción / pase | +100 / +50 extra / +30 / +25 / +20 / +10 |
| Combo | Multiplicador que sube con acciones positivas encadenadas y vuelve a 0 al perder la posesión |
| Duración del partido | Entre 120 y 180 s (configurable) |
| Controles PC | WASD mover · J pasar/recuperar · K tirar · L habilidad |

**Ganador = más goles.** Los puntos son estadística individual (estilo Rocket League) para el MVP y el récord personal; nunca deciden el resultado.

---

## Decisiones pendientes (pregúntame antes de implementarlas)

- [ ] Reanudación tras gol (¿saca el equipo que lo ha recibido, desde el centro?)
- [ ] Balón fuera de la piscina
- [ ] Límite de tiempo de posesión para tirar
- [ ] Empate al final (gol de oro, prórroga o empate válido)
- [ ] Duración exacta del partido

---

## Fases

### Fase 0 — Proyecto base
- Revisa que el proyecto use URP (3D), Input System y Test Framework; dime qué paquetes faltan.
- Crea la estructura de carpetas y las Assembly Definitions.
- Configura (o dime cómo configurar) Force Text, Visible Meta Files y `.gitignore` de Unity.
- Un test EditMode trivial que pase.
- **Hecho cuando:** compila sin errores, el test pasa y se puede hacer un build Web vacío.

### Fase 1 — Piscina y movimiento
- Generador de Editor `Waterpulse/Build Match Scene`: piscina, límites, dos porterías y cámara cenital que vea todo el campo.
- `PlayerIntent`, `IPlayerBrain`, `HumanBrain` (WASD) y `PlayerController` con movimiento con inercia de agua.
- `GameConfigSO` con velocidad, aceleración y damping.
- **Hecho cuando:** muevo un jugador por la piscina con WASD, no sale de los límites y se siente "en el agua".

### Fase 2 — Pelota, pase, tiro y gol
- `BallController` con sus tres estados. Recoger la pelota al tocarla si está libre.
- Pase (J) al compañero más cercano en la dirección de movimiento; tiro (K) hacia la portería rival.
- Detección de gol, marcador básico y reanudación (según la decisión pendiente).
- Eventos `PassCompleted`, `Goal`, `PossessionChanged`.
- **Hecho cuando:** puedo pasar, tirar y marcar, y el marcador sube.

### Fase 3 — Equipos 3+1 y porteros
- Generador que crea los 8 jugadores (dos equipos de 3 + portero) con colores de gorro contrastados.
- `RolePositioning`: los compañeros no controlados se colocan según la posición del balón y su rol (Atacante, Organizador, Defensor).
- `GoalkeeperController`: se mueve en su área siguiendo el balón e intenta parar tiros. Evento `Save`.
- **Hecho cuando:** mi equipo se coloca solo y los porteros paran algunos tiros, no todos.

### Fase 4 — IA rival
- `AIBrain` y `TeamAI`: perseguir la pelota libre, presionar al poseedor, intentar interceptar pases, pasar y tirar cuando hay ocasión.
- Recuperar la pelota con J al defender. Eventos `Interception` y `PossessionChanged`.
- `DifficultySO` con velocidad de reacción y agresividad (un solo nivel por ahora).
- **Hecho cuando:** puedo jugar un partido contra la IA y me marca y le marco.

### Fase 5 — Flujo de partido
- `MatchStateMachine` (`Kickoff → Playing → GoalScored → Kickoff … → FullTime → Results`) y `MatchTimer`.
- Pantalla final con resultado y botón de reiniciar.
- **Hecho cuando:** el partido empieza, dura lo configurado, termina y se puede reiniciar.

### Fase 6 — Energía y habilidades
- `EnergyMeter` (Logic, con tests) y recarga según los eventos.
- `AbilityDefinitionSO` y `AbilityBehaviour`; implementar **solo** Xut del Tràngol y Torpede Blau (tecla L).
- Efectos visuales simples: estela, salpicadura y nombre de la habilidad en pantalla.
- **Hecho cuando:** las habilidades gastan energía, solo se activan con energía suficiente y se entienden visualmente.

### Fase 7 — Puntuación, estadísticas y récord
- `ScoreTracker`, `ComboMultiplier` y `MatchStats` (Logic, con tests), alimentados por eventos.
- Pantalla final completa: goles a favor y en contra, puntos por jugador, MVP, pases completados, habilidades usadas con éxito y récord personal.
- `RecordStorage` con `PlayerPrefs`.
- **Hecho cuando:** los puntos cuadran con la tabla y el récord persiste al recargar la página.

### Fase 8 — HUD y controles táctiles
- HUD: marcador, tiempo, barra de energía e indicador de posesión.
- `TouchControls` con `OnScreenStick` a la izquierda y tres `OnScreenButton` grandes a la derecha, visibles solo en dispositivos táctiles.
- Canvas responsive para móvil vertical y horizontal (pregúntame cuál priorizar).
- **Hecho cuando:** se puede jugar un partido entero desde el móvil en el build Web.

### Fase 9 — Pulido
- `AudioDirector`: público, silbatos, salpicaduras y huecos para comentarios cortos en catalán.
- Feedback de juego: pequeños shakes, pausas breves al marcar, partículas de agua.
- **Hecho cuando:** el partido se siente vivo sin perder legibilidad.

### Fase 10 — Balance
- Panel de depuración (solo en el editor) para ver energía, puntos y estados de IA en tiempo real.
- Propón ajustes de valores en Config según lo que observemos jugando. No cambies valores sin decirme cuáles y por qué.

---

## Estado

- Fase actual: **Fase 1 — EN CURSO**
- Fases completadas: Fase 0
- Notas:
  - Fase 1 (aprobado): piscina 20×12 m, porterías de 3 m; velocidad 4 m/s, aceleración 8 m/s², `linearDamping` 2; cámara inclinada ~60° que encuadra todo el campo; HumanBrain lee WASD y también el stick izquierdo del mando.
  - Decisión: el juego es **3D** (URP con Universal Renderer, `Rigidbody` 3D en el plano XZ). Prevalece sobre "2D o 2.5D" del GDD.
  - Decisión: `IPlayerBrain` y `PlayerIntent` van en `Core` para evitar el ciclo Gameplay ↔ Brains.
  - Repo git en la raíz del proyecto (movido desde la carpeta anidada `waterpoloPorotype/`).
  - Paquetes quitados: AI Navigation, Visual Scripting, Multiplayer Center, Collab Proxy, Timeline.
  - Borrados `Assets/Prueba.cs` y `SampleScene` de la plantilla.
