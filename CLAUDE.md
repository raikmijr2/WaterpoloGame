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
  - Dependencias: `Logic` → nada · `Config` → nada · `Core` → Config · `Gameplay` → Logic, Config, Core · `AI` → Core, Config, Gameplay · `Brains` → Core, Config, Gameplay, AI, InputSystem · `Abilities` → Core, Config, Logic, Gameplay · `UI` → Core, Config, Logic, Gameplay, InputSystem, uGUI · `Audio` → Core, Config · `Editor` → todas (solo editor) · `Tests.EditMode` → todas las anteriores (incluido Editor, para testear los generadores).
  - El namespace de `Scripts/Editor/` es `Waterpulse.EditorTools` (un namespace `...Editor` taparía la clase `UnityEditor.Editor`).
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
| Control del humano | Estilo FIFA: controla al jugador de su equipo que tiene la pelota (decisión del usuario, prevalece sobre el GDD) |

**Ganador = más goles.** Los puntos son estadística individual (estilo Rocket League) para el MVP y el récord personal; nunca deciden el resultado.

---

## Decisiones pendientes (pregúntame antes de implementarlas)

- [x] Reanudación tras gol → la pelota vuelve al centro y la recibe el equipo que ha encajado (su jugador de campo más cercano al centro).
- [x] Balón fuera de la piscina → no puede salir: rebota en las paredes.
- [ ] Límite de tiempo de posesión para tirar
- [x] Empate al final → gol de oro: se sigue jugando y gana quien marque primero.
- [x] Duración exacta del partido → 150 s (2:30), configurable. El reloj solo corre durante el juego (se para en goles y saques).

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
- Pelota con altura **solo visual**: parábola en pases y tiros, bote al tocar el agua y frenada, como en la vida real (pedido en la Fase 2). La física sigue en el plano XZ.
- **Hecho cuando:** el partido se siente vivo sin perder legibilidad.

### Fase 10 — Balance
- Panel de depuración (solo en el editor) para ver energía, puntos y estados de IA en tiempo real.
- Propón ajustes de valores en Config según lo que observemos jugando. No cambies valores sin decirme cuáles y por qué.

---

## Estado

- Fase actual: **Fase 6 — EN CURSO**
- Fases completadas: Fase 0, Fase 1, Fase 2, Fase 3, Fase 4, Fase 5
- Notas:
  - Pendiente para el final (Fase 10): el usuario ha visto cosas a mejorar tras la Fase 5 y prefiere tratarlas al final. Pedirle la lista al llegar a la Fase 10.
  - Fase 5 (aprobado): `MatchStateMachine` y `MatchTimer` puros en Core; `MatchFlow` (Gameplay) los conecta con la escena y sustituye a `GoalRestart` y `MatchScore`. Primer saque del local; tras gol saca quien encaja. En cada saque todos vuelven a su posición (`KickoffLayout`, valores en config) y se quedan quietos 1,5 s ("Preparats…"); "GOL!" 1,5 s; "Final del partit" 1,5 s antes de la pantalla final. Los jugadores solo obedecen a su cerebro en `Playing`. Pantalla final básica con resultado, ganador y "Tornar a jugar" (botón o J/Enter), que recarga la escena. Nombres de equipo en config ("LOCAL", "VISITANT").
  - Fase 4 (aprobado): al defender, el control salta al jugador de campo propio más cercano a la pelota (margen 1,5 m, espera 0,5 s). Recuperar con J: alcance 1,2 m, 50 % al límite y 75 % pegado, 0,8 s de espera si falla. Intercepción automática: 40 % si el pase pasa a menos de 0,8 m de un rival (una tirada por rival y pase); los tiros no se interceptan. Los compañeros del humano solo se colocan y marcan. IA rival: avanza, pasa si la presionan (a menos de 1,5 m) y tira a menos de 6 m con la línea libre, a un palo al azar. Dificultad única "Competidor" (`DifficultySO`), sin habilidades.
  - Fase 4 (añadido y validado): tras ganar la pelota hay 0,5 s en los que no te la pueden quitar (`recoverImmunity`, 0 = desactivado), para evitar robos en cadena; y a los porteros no se les puede quitar la pelota.
  - Fase 4: desaparece la restricción temporal `canTakeBall` de la Fase 3.
  - Fase 4 (corrección tras prueba): solo un jugador va a por el poseedor; `TeamAI` reparte un marcaje único por atacante (nunca al poseedor) y solo cuando el equipo defiende (sin posesión de nadie, todos en formación; así se evita el bucle de marcajes cruzados). Presionador, perseguidor e interceptor van a máxima velocidad. Los compañeros del humano tienen un `TeamAI` pasivo: solo marcajes.
  - Fase 3 (aprobado): gorros local azul, visitante blanco, porteros rojo; bañador del color del equipo. Parada por probabilidad (`SaveOdds`): 80 % al cuerpo, 0 % a 1,2 m; el portero ya no bloquea físicamente. Tras parar, saca al compañero libre más cercano a ~1 s. Colocación por rol según GDD sección 7 (con posesión se abren; sin posesión defienden y marcan al rival cercano). Las diferencias de estadísticas por rol se dejan para la Fase 10; los roles aún no se distinguen visualmente (pendiente de arte).
  - Decisión: **control estilo FIFA**. El humano controla siempre al jugador de su equipo que tiene la pelota: al pasar, el control pasa al receptor. Prevalece sobre la sección 6 del GDD ("controla un sol personatge"). Al defender, el control salta automáticamente al más cercano a la pelota (ver Fase 4).
  - Fase 2 (aprobado): el local ataca hacia +X (derecha) todo el partido. Tiro: sin stick al centro de la portería; con stick arriba/abajo hacia ese palo. Pase: al compañero más cercano dentro de ±60° de la dirección del stick (sin stick, hacia donde mira); si no hay nadie, la pelota sale en esa dirección y queda libre. Valores: pase 10 m/s, tiro 14 m/s, radio de recogida 0,6 m, damping de la pelota suelta 1,5.
  - Fase 2: los jugadores recogen la pelota por distancia (sin colisión física con ella). Cualquier choque de una pelota en vuelo la deja libre. Gol = la pelota cruza entera la línea (trigger dentro de la red), también si entra llevada por un jugador. `GoalRestart` era provisional: lo sustituyó `MatchFlow` en la Fase 5.
  - Fase 1 (aprobado): piscina 20×12 m, porterías de 3 m; velocidad 4 m/s, aceleración 8 m/s², `linearDamping` 2; cámara inclinada ~60° que encuadra todo el campo; HumanBrain lee WASD y también el stick izquierdo del mando.
  - Fase 1: las paredes de fondo dejan libre la boca de la portería; la red (fondo y laterales) tiene colisión. Sin input, el `linearDamping` frena (deriva); con input, el damping es 0 y manda la aceleración.
  - Decisión: el juego es **3D** (URP con Universal Renderer, `Rigidbody` 3D en el plano XZ). Prevalece sobre "2D o 2.5D" del GDD.
  - Decisión: `IPlayerBrain` y `PlayerIntent` van en `Core` para evitar el ciclo Gameplay ↔ Brains.
  - Repo git en la raíz del proyecto (movido desde la carpeta anidada `waterpoloPorotype/`).
  - Paquetes quitados: AI Navigation, Visual Scripting, Multiplayer Center, Collab Proxy, Timeline.
  - Borrados `Assets/Prueba.cs` y `SampleScene` de la plantilla.
