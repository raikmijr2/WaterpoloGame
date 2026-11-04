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
- **Input unificado.** Todo jugador (humano o IA) se controla mediante `IPlayerBrain`, que devuelve un `PlayerIntent { Vector2 Move; bool Pass; bool Shoot; bool Ability; bool Dash; }` (`Dash` añadido en la Fase 8; nadar lento = `Move` con poca magnitud). `PlayerController` solo ejecuta intenciones y no sabe quién lo controla.
- **Eventos desacoplados.** Los sistemas se comunican por `GameEvents`: `Goal`, `PassCompleted`, `Interception`, `Save`, `AbilityUsed`, `PossessionChanged`, `MatchStateChanged`, y además `BallRecovered` (robo con J) y `ControlledPlayerChanged` (jugador que controla el humano), añadidos en la Fase 6, y `RecoverFailed` (intento de robo fallido), añadido en la Fase 8, y `PauseChanged` (pausa del partido), añadido con el menú de pausa, y `PassThrown`, `ShotTaken`, `DashUsed`, `BallBounced` y `GoldenGoalStarted`, añadidos para el sonido, y `AbilityNoEnergy` (habilidad pedida sin energía suficiente), y `WallLanded` (el Mur de Ferro cae en el agua). HUD, puntuación, audio y estadísticas escuchan; no se referencian entre sí.
- **Pelota con estados explícitos:** `Free`, `Held(owner)`, `InFlight(kind: Pass|Shot, from, target)`. Solo `BallController` cambia el estado.
- **Física:** `Rigidbody` (3D) con `useGravity = false`, posición Y y rotaciones X/Z congeladas, y `linearDamping` configurable para la sensación de agua. `PlayerIntent.Move` (Vector2) se aplica en el plano XZ. No se simula el agua.
- **Assembly Definitions:** una por carpeta de scripts. `Logic` no depende de nada del juego; `Gameplay` depende de `Logic`, `Config` y `Core`.
  - `IPlayerBrain` y `PlayerIntent` viven en `Core` (no en `Brains`) para evitar la dependencia circular Gameplay ↔ Brains. `Brains` contiene las implementaciones (`HumanBrain`, `AIBrain`).
  - Dependencias: `Logic` → nada · `Config` → nada · `Core` → Config · `Gameplay` → Logic, Config, Core · `AI` → Core, Config, Gameplay · `Brains` → Core, Config, Gameplay, AI, InputSystem · `Abilities` → Core, Config, Logic, Gameplay · `UI` → Core, Config, Logic, Gameplay, InputSystem, uGUI · `Audio` → Core, Config, uGUI (para `UiSound`) · `Editor` → todas (solo editor) · `Tests.EditMode` → todas las anteriores (incluido Editor, para testear los generadores).
  - El namespace de `Scripts/Editor/` es `Waterpulse.EditorTools` (un namespace `...Editor` taparía la clase `UnityEditor.Editor`).
- **Rendimiento Web:** nada de reflexión pesada, threads ni APIs no soportadas en WebGL.

### Estructura de carpetas

```
Assets/_Waterpulse/
├── Scripts/
│   ├── Core/          GameEvents, MatchStateMachine, MatchTimer, IPlayerBrain, PlayerIntent, PlayerSession, AudioVolumes
│   ├── Config/        GameConfigSO, AbilityDefinitionSO, ScoringConfigSO, EnergyConfigSO, DifficultySO, MenuConfigSO, UsersConfigSO, AudioConfigSO
│   ├── Logic/         EnergyMeter, ScoreTracker, ComboMultiplier, MatchStats, RecordStorage, VolumeSetting, UserRegistry, Leaderboard
│   ├── Gameplay/      PlayerController, BallController, Goal, PoolBounds, GoalkeeperController, MatchPause, UserRecords
│   ├── Brains/        HumanBrain, AIBrain
│   ├── AI/            TeamAI, RolePositioning
│   ├── Abilities/     AbilityBehaviour, XutTragolAbility, TorpedeBlauAbility
│   ├── UI/            HUD (ScoreboardView, PlayerHudView…), ResultsScreen, TouchControls, MenuFlow, IntroScreen, OptionsMenu, PauseMenu, UsersMenu, RankingView, SelectionTextColor
│   ├── Audio/         AudioDirector, UiSound
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

**Ganador = más puntos de gol** (decidido por el usuario; antes, más goles). Cada gol vale (100, o 150 con el Xut del Tràngol) × el multiplicador de posesión, más 50 si es contraataque; en propia puerta, 100. Con empate a puntos al acabar el tiempo, gol de oro. Los demás puntos (pases, robos, paradas) son estadística individual para el MVP, el récord y el rànquing; no deciden el resultado.

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

### Fase 8 — Campo, cámara y estamina (añadida a petición del usuario)
- Campo de 30×20 m (porterías de 3 m).
- Cámara que sigue la jugada con suavizado (no encuadra todo el campo) y flecha en el borde de la pantalla hacia la portería rival cuando no se ve (quitada después a petición del usuario).
- Estamina por jugador, distinta de la energía de habilidades; dash (Shift) y nadar lento (Espacio). La IA rival también usa el dash.
- **Hecho cuando:** el campo es más grande, la cámara acompaña la jugada sin perder de vista la portería rival, y el dash y el nado lento se sienten bien.

### Fase 9 — HUD y controles táctiles
- HUD: marcador, tiempo, barra de energía, barra de estamina e indicador de posesión.
- `TouchControls` con `OnScreenStick` a la izquierda y tres `OnScreenButton` grandes a la derecha (pasar/recuperar, tirar, habilidad) más un cuarto más pequeño para el dash, visibles solo en dispositivos táctiles. Nadar lento = inclinar poco el joystick.
- Canvas responsive con **prioridad horizontal** (decidido).
- **Hecho cuando:** se puede jugar un partido entero desde el móvil en el build Web.

### Fase 10 — Pulido
- `AudioDirector`: público, silbatos, salpicaduras y huecos para comentarios cortos en catalán.
- Feedback de juego: pequeños shakes, pausas breves al marcar, partículas de agua.
- Pelota con altura **solo visual**: parábola en pases y tiros, bote al tocar el agua y frenada, como en la vida real (pedido en la Fase 2). La física sigue en el plano XZ.
- **Hecho cuando:** el partido se siente vivo sin perder legibilidad.

### Fase 11 — Balance
- Panel de depuración (solo en el editor) para ver energía, puntos y estados de IA en tiempo real.
- Propón ajustes de valores en Config según lo que observemos jugando. No cambies valores sin decirme cuáles y por qué.

---

## Estado

- Fase actual: **Fase 8 — EN CURSO** (código hecho, pendiente de verificación en el editor)
- Fases completadas: Fase 0, Fase 1, Fase 2, Fase 3, Fase 4, Fase 5, Fase 6, Fase 7
- Notas:
  - Pendiente para el final (Fase 11): el usuario ha visto cosas a mejorar tras la Fase 5 y prefiere tratarlas al final. Pedirle la lista al llegar a la Fase 11.
  - Fases renumeradas tras la Fase 7: nueva Fase 8 (campo, cámara y estamina); HUD y táctil pasa a Fase 9, Pulido a Fase 10 y Balance a Fase 11.
  - Menú principal e intro (añadido a petición del usuario, fuera de las fases; código hecho, pendiente de verificación en el editor). Escena `Menu` (índice 0 del build, `Waterpulse/Build Menu Scene`) con la piscina del partido de fondo y la cámara girando despacio (`MenuConfigSO`). Intro: logo con fundido y "Prem qualsevol tecla" (tecla, clic, toque o mando); solo sale la primera vez en cada sesión. Menú: "Partit individual" (escena `Match`), "1 contra 1 local" (quitado del menú a petición del usuario: no se hace para el concurso; local 1–2 días, online semanas y no cumple "sin conexión") y "Opcions". Opcions: volumen general (se guarda con `VolumeSetting`; se aplica a todo el audio vía `AudioListener.volume`, aún no hay sonido), pantalla completa (no se guarda; en Web se pide al pulsar con `PressButton`), tabla de controles y "Esborrar rècord" con confirmación (añadido por Claude, aprobado). La pantalla final tiene también "Menú principal" (botón o Esc/Start). Navegación: WASD/flechas/stick, Enter/A, Esc/B atrás; el ratón selecciona al pasar por encima. Estilo compartido en `UiFactory` (Editor): botones azules del local, amarillo al seleccionar, paneles oscuros. No se renumeran las fases.
  - Pausa, usuarios, rànquing y resumen (añadido a petición del usuario, fuera de las fases; código hecho, pendiente de verificación en el editor).
    - **Pausa** (`MatchPause` + `PauseMenu`): Esc, P o Start, o el botón II del HUD (ratón y táctil, sin navegación para que el A del mando no lo pulse). No en "Final del partit" ni en el resumen. `Time.timeScale = 0` y `GameEvents.PauseChanged`, para que `PlayerController` y `ControlSwitcher` ignoren el input. Al continuar espera `resumeDelay` (0,2 s) para que la tecla de confirmar no sea un pase. Pantallas: Continuar · Opcions (volumen, pantalla completa, controles) · Reiniciar partit · Menú principal, estos dos con confirmación. Esc/B atrás; P/Start cierran.
    - **Usuarios** (decidido): solo nombre, sin contraseña, guardados en el navegador (`PlayerPrefs`, sin servidor). "Qui juga?" sale siempre tras la intro; la sesión (`PlayerSession`) dura hasta recargar la página y al volver de un partido no se vuelve a preguntar. Nombre de 1 a 12 caracteres (sin espacios de los lados), sin repetir sin distinguir mayúsculas, sin caracteres de control ni `<` `>`; máximo 10 usuarios (`UsersConfigSO`). Se puede borrar un usuario (X roja, con confirmación) y se borra su récord. Se puede jugar como convidat, sin récord ni rànquing. El campo del nombre no tiene navegación (si no, W/A/S/D moverían la selección): se confirma con Enter o "Crear".
    - **Récord por usuario** (`ScoringConfigSO.userRecordKeyFormat`): sustituye al récord global, que se descarta. "Esborrar el meu rècord" en Opcions borra el del usuario actual. Abrir `Match` directamente en el editor juega como convidat.
    - **Rànquing** (decidido): Top 10 con la mejor puntuación de cada usuario; los empates comparten posición (orden alfabético); el usuario actual sale resaltado.
    - **Resumen final** (decidido): marcador, ganador y una tabla por equipo (goles, pases, recuperaciones = intercepciones + robos, paradas, puntos, total; MVP en amarillo; habilidades con éxito), récord del usuario y su posición en el rànquing. La ★ se cambia por "MVP" (la fuente del build Web puede no tenerla).
  - Sonido (adelantado de la Fase 10 a petición del usuario; solo la parte de audio, sin shakes, partículas ni altura de la pelota; código hecho, pendiente de verificación en el editor).
    - **Sonidos reales** (cambio pedido tras probarlo: los sintetizados sonaban a 8 bits): 22 grabaciones **CC0** en `.ogg` en `Audio/`, de Freesound, del pack de agua de OpenGameArt y de Kenney. Créditos en `docs/audio-credits.md`. Se descargó solo audio de fuentes conocidas y se comprobó cada archivo (Ogg Vorbis y licencia CC0).
    - **`SoundGenerator`** (`Waterpulse/Generate Sounds`, que también llaman los dos generadores de escena) asigna en `AudioConfigSO` el archivo de cada nombre (`.ogg`, `.wav` o `.mp3`). Solo sintetiza un `.wav` provisional (`SoundSynth`) si falta el archivo, y solo rellena los clips vacíos.
    - **Ambiente de waterpolo, no de fútbol** (corrección del usuario):
      - **Fondo**: agua de una piscina de competición (nadadores haciendo largos) y público de una competición de natación, en dos capas.
      - **Reacciones del público**: de público pequeño en recinto cerrado.
      - **Árbitro**: pita los goles y la reanudación.
      - **Bocina del marcador**: marca el final del tiempo y el paso al gol de oro, seguida del silbato largo.
      - **Robo**: golpe de agua.
    - **`AudioDirector`** (uno por escena) escucha `GameEvents`. Las grabaciones de fondo no son bucles perfectos y se funden consigo mismas al acabar (`CrossfadeLoop`, `ambienceCrossfade` 2 s). Las reacciones del público duran como mucho `crowdReactionMaxDuration` (4 s) y se apagan con un fundido. Gol: silbato y ovación si marca el equipo del jugador, "aww" si marca el rival. Parada: golpe y "aww". Además, esquitxades de pase y tiro, recepción, robo, robo fallido, dash, "clonc" metálico en palos y paredes, un sonido por habilidad y jingle de victoria o derrota en el resumen. Sonido 2D, con una pequeña variación de tono en los efectos.
    - **Música**: menú (tranquila) y partido (enérgica), chiptune en bucle. En la pausa siguen la música y la interfaz y se paran el público y los efectos.
    - **Interfaz**: `UiSound` en cada botón y deslizador (al moverse y al pulsar).
    - **Opcions**: General, Música y Efectes, cada uno en una fila (una sola columna: con los deslizadores, izquierda y derecha cambian el valor). Se guardan; música y efectos pasan por `AudioVolumes`.
    - **Comentarios en catalán**: huecos (gol, parada, gol de oro, final) en `AudioConfigSO`, vacíos.
  - Estética "retransmisión deportiva" y agua (pedido por el usuario; adelanta del HUD de la Fase 9 el marcador, las barras de energía y estamina y el indicador de posesión; los controles táctiles siguen en la Fase 9; código hecho, pendiente de verificación en el editor).
    - **Agua** (decidido): el agua y el fondo de la piscina ocupan justo el campo (30×20 m), sin sobresalir en el partido ni en el fondo del menú. Las redes quedan fuera del agua.
    - **Recursos**:
      - Fuentes Barlow Condensed Bold (títulos, botones, números) y Barlow SemiBold (textos), de Google Fonts, licencia OFL (en `Art/Fonts`, con su `OFL-*.txt`).
      - Formas generadas por `UiSpriteGenerator` (`Waterpulse/Generate UI Sprites`, lo llaman los dos generadores) en `Art/UI`: panel redondeado, borde, cápsula e iconos de rayo, ondas y flecha. Solo crea las que faltan.
      - Créditos en `docs/ui-credits.md`.
    - **Estilo (`UiFactory`)**: paneles azul marino translúcidos con borde fino y franja amarilla arriba; títulos en mayúsculas. Botones oscuros que, seleccionados, se vuelven amarillos con el texto oscuro (`SelectionTextColor`).
    - **HUD**:
      - **Marcador**: bloques de color de cada equipo, goles a los lados del tiempo (amarillo, o "GOL D'OR") y un punto amarillo bajo el equipo con la posesión (`ScoreboardView`).
      - **Bàners**: para los avisos y para el nombre de la habilidad.
      - **Panel del jugador** abajo a la izquierda (`PlayerHudView`, sustituye a `EnergyReadout`): rol, barra de energía con una marca en el coste de su habilidad y la tecla L encendida cuando llega, y barra de estamina.
      - **Flecha a la portería**: la flecha es una imagen, porque la fuente del build Web puede no tener ►.
    - Sin la negrita sintética de Unity: las fuentes ya son negritas.
  - Correcciones tras probar (código hecho, pendiente de verificación en el editor):
    - **Fluidez**: la cámara seguía la posición física de la pelota (`body.position`), que solo cambia 50 veces por segundo, mientras todo se dibuja interpolado; iba a saltos y hacía temblar la imagen. Ahora sigue la posición dibujada (`transform.position`). La pelota en posesión se coloca con la pose física del jugador (`BodyPosition` y `BodyForward`), para que pelota y jugador se interpolen igual.
    - **"El Xut del Tràngol solo funciona la primera vez"**: no era un error. Empieza con 50 de energía y cuesta 35, y luego hacen falta 20 más (unas 4 pasadas). Decidido: no se cambian los valores (quedan para la Fase 11).
      - Al pedir la habilidad sin energía, `AbilityBehaviour` lanza `AbilityNoEnergy`.
      - La tecla L del panel parpadea en rojo con un latido (`PlayerHudView`) y suena `ability_no_energy` (Kenney, CC0). Solo para el jugador del humano.
  - Fase 8 (aprobado): campo 30×20 m. Cámara: sigue la pelota con suavizado y algo de adelanto hacia donde se ataca, encuadra ~16–18 m de largo, no se sale de la piscina; flecha en el borde de la pantalla hacia la portería rival cuando no se ve (compensa el pilar 3 del GDD, "siempre se ve dónde está la portería"). Estamina 0–100 por jugador, independiente de la energía, se recarga sola (~20/s). Dash (Shift): sirve para recuperar velocidad (cambio pedido tras probarlo): fuerza = 1 − velocidad actual / máxima; parado sale a `dashSpeedMultiplier` × máxima y a velocidad máxima no hace nada; por debajo de `dashMinStrength` (0,1) no hay dash ni se gasta estamina (`DashPower`, Logic). Cuesta ~30, impulso de 0,12 s en la dirección del stick (bajado de 0,25 s tras probarlo: era exagerado), con pelota sin perderla, 0,5 s de espera entre dashes. Nadar lento (Espacio, mantener): velocidad máxima al 40 %, no gasta estamina; si vas más rápido de lo que pide el stick se frena casi en seco (`slowBrakeDeceleration` 40 m/s², pedido tras probarlo). Afecta también a la IA al llegar a su sitio (frena siguiendo la rampa de llegada en vez de pasarse). La IA rival usa el dash para alcanzar pelotas sueltas, al poseedor o para cortar pases (si el objetivo está a más de 4 m); para pelotas sueltas a más de 6 m prefiere el Torpede Blau si tiene energía. Dash en mando: RB; nado lento en mando: LB. El tiro apunta según la dirección del stick, no su inclinación (nadar lento no cambia la puntería). El Torpede Blau se mantiene (largo y espectacular; el dash es corto y básico). Se amplían las acciones del pilar 1 del GDD (decisión del usuario): en móvil el dash es un 4º botón pequeño y el nado lento es inclinar poco el joystick.
  - Fase 8 (añadido a petición del usuario): cambio manual de jugador con Q (LT en el mando) al jugador de campo propio más cercano a la pelota; si ya eres tú, al siguiente. No funciona con la pelota. Convive con el cambio automático al defender, que tras un cambio manual espera `manualSwitchLock` (2 s). El botón táctil se decide en la Fase 9.
  - Fase 8 (añadido a petición del usuario): pequeño temblor de cámara cuando el humano falla un robo con J (`cameraShakeDuration` 0,15 s, `cameraShakeAmplitude` 0,12 m); no con los intentos de la IA ni si pulsas J fuera de alcance.
  - Fase 8 (corrección tras prueba): la IA con pelota ya no se queda parada. Si le tapan la línea de tiro en zona de tiro se mueve en lateral (`aiRepositionStep` 2 m); pasa sin presión si un compañero con línea limpia está ≥ 3 m más adelantado (`aiPassAdvanceGain`); y si lleva más de `aiMaxHoldTime` (3 s) con la pelota, tira (en zona de tiro) o pasa al mejor compañero.
  - Fase 8 (corrección tras prueba): intercepción de pases según lo cerca que pasa la pelota (`InterceptOdds`, Logic): 90 % si le llega al cuerpo (jugador + pelota, ~0,55 m), bajando al 30 % en el borde (0,8 m). La tirada se hace al llegar al cuerpo o en el punto más cercano del recorrido, no al entrar en el alcance (antes era un 40 % fijo decidido demasiado pronto y la pelota "atravesaba" a los defensores). Sustituye a `interceptChance` por `interceptChanceAtBody` e `interceptChanceAtEdge`.
  - Fase 8 (añadido a petición del usuario): solo se puede robar con J desde delante del que lleva la pelota (`recoverFrontAngle` 180°, `RecoverGeometry`); desde su espalda no hay intento (ni espera, ni penalización, ni temblor). La IA rival tampoco lo intenta desde detrás.
  - Fase 8 (pedido por el usuario, código hecho, pendiente de verificación): movimiento más de waterpolo, pase con saltito y barra de tiro estilo NBA.
    - **Movimiento sin deslizar**: `acceleration` 30 y `linearDamping` 15 (antes 8 y 2): llega enseguida a su velocidad y casi no deriva al soltar.
    - **Pase**: el pasador se para en seco `passFreezeDuration` (0,3 s) y da un saltito visual de `passHopHeight` (0,3 m). La pelota sale hacia donde apuntaba al pulsar: con modelo, en el evento de la animación; sin modelo, al momento (el retraso fijo `passReleaseDelay` se quitó a petición del usuario).
    - **Barra de tiro** (`ShotMeter`, Logic): la primera K empieza a cargar y el jugador se queda quieto y sube (`shotRiseHeight` 0,25 m, solo visual; bajado de 0,6 a petición del usuario). La barra se llena en `shotMeterDuration` (0,4 s de juego, unos 1,3 s reales con la cámara lenta; bajado de 1 s a petición del usuario: con la cámara lenta iba muy lenta). La segunda K tira apuntando con el stick de ese momento.
      - Zona verde: de `shotGreenStart` (0,85), `shotGreenWidth` (0,08) de ancho. En el verde, calidad 1 (el tiro de siempre). Fuera, como mucho `shotOffGreenMaxQuality` (0,7) al lado de la zona, y 0 en los extremos. Si la barra se llena, tira solo con calidad 0.
      - Con menos calidad el tiro sale más flojo (hasta `shotMinPowerFactor` 0,6) y desviado hacia un lado al azar (hasta `shotMaxErrorAngle` 12°), y puede irse fuera.
      - Mientras carga o lanza (pase o tiro) no se le puede robar (cambiado a petición del usuario: "el xut només el pot parar el porter"); antes se le podía robar y se cortaba. Durante la carga no hay habilidad ni dash.
      - El Xut del Tràngol sigue siendo instantáneo.
    - **IA**: suelta en el verde con `DifficultySO.shotGreenChance` (50 %). Si falla, suelta entre `shotMissMinFill` (0,5) y el verde.
    - **HUD**: `ShotMeterView`, una barra vertical al lado del jugador del humano mientras carga. Solo la del equipo humano.
    - Controles: "K + K" y "B + B" en la tabla.
    - Limitación: la pelota no sube con el jugador (su Y sigue fija hasta la altura visual de la Fase 10).
  - Fase 8 (pedido por el usuario, código hecho, pendiente de verificación): modelo animado del usuario en los jugadores de campo de los dos equipos. Los porteros siguen con cápsulas.
    - **Portero LOCAL** (añadido después, portería de la izquierda): prefab `Prefabs/Waterpolo_GoalSave` (o `GameConfigSO.homeGoalkeeperModel`), con `AnimControllerPortero` (TreadingWater, GoalSave y Throw), y la misma escala y posición que los demás. Parámetros: `Save` (trigger, al parar; `PlayerAnimator` escucha `GameEvents.Save`), `Throw` (al sacar) y `Speed`. **Portero rival** (portería de la derecha): prefab `Prefabs/Waterpolo_GoalSave 1` (o `awayGoalkeeperModel`), con `AnimControllerPorteroEnemy` y los mismos parámetros. `PlayerAnimator` solo toca los parámetros que tiene cada Animator.
    - **Rival** (añadido después): prefab `Prefabs/WaterpoloEnemy` (o `GameConfigSO.awayPlayerModel`), con su Animator Controller (`waterpolo_animaciones_enemigo/.../AnimationControllerEnemy`), los mismos parámetros y la misma escala y posición que el local.
    - **Modelo**: prefab `Prefabs/Waterpolo_TreadingWater` (o `GameConfigSO.homePlayerModel`), dentro del hijo `Visual`, con `playerModelOffset` (0, −0,825, 0) y `playerModelScale` 1,5 (subida de 1 a petición del usuario: se veían pequeños). El generador deja su Rigidbody cinemático y su collider desactivado.
    - **Animator Controller** del usuario (`Anims/AnimationController`): estados TreadingWater, SwimFast, Throw y StealTouch. Parámetros: `Speed` (float, velocidad / máxima), `Throw` (trigger, al pulsar pasar y al soltar la barra de tiro) y `Steal` (trigger, al intentar robar al alcance, salga bien o mal).
    - **`PlayerAnimator`** (Gameplay): va en el objeto del Animator y rellena esos parámetros. Recibe el evento `ReleaseBall` del clip Throw, que es cuando sale la pelota en el pase y en el tiro. Si el evento no llega en `throwReleaseTimeout` (1 s), sale igualmente.
    - Sin modelo, el pase y el tiro salen al instante.
    - **Pelota en la mano** (pedido por el usuario, solo visual): la pelota física sigue igual (delante del jugador, en el agua) y la esfera visible es una hija con `BallVisual`.
      - Mientras la lleva un jugador con modelo, va **en el agua delante de su cara, empujada** (cambio pedido por el usuario; antes iba siempre en la mano), también parado (decidido): a `ballDribbleDistance` (0,6 m) delante, con un vaivén adelante y atrás de `ballDribbleSurge` (0,08 m) a `ballDribbleSurgeFrequency` (2,5 por segundo) que crece con la velocidad (parado no se mueve).
      - Solo va en su mano derecha (`ballHandBone`, con `ballHandOffset` para la palma) mientras carga el tiro o pasa, hasta soltarla (`PlayerController.BallInHand`). Sube del agua a la mano en `ballGrabBlendTime` (0,1 s).
      - Al soltarla pasa de la mano a su trayectoria en `ballReleaseBlendTime` (0,1 s).
      - Con las cápsulas se ve donde está.
    - Indicador del jugador controlado (pedido por el usuario): un triángulo rojo con la punta hacia abajo sobre su cabeza, como en el FIFA (malla `Art/Meshes/ControlTriangle`, a 1,3 m sobre el agua). Sustituye al disco verde. **Sustituido** por los arcos en el agua (ver "Marcas sobre los jugadores"); la malla queda en el proyecto sin usar.
    - Solo para pruebas: `Waterpulse/Build Match Scene (sin rivales)` genera la escena sin el equipo visitante (ni portero). Tras un gol la pelota queda libre en el centro. `Build Match Scene` la deja normal.
    - El Xut del Tràngol sigue instantáneo y sin animación (pendiente de que lo decida el usuario).
  - Marcas sobre los jugadores y paleta "volt" (pedido por el usuario, fuera de las fases, a partir de una maqueta de HUD aprobada; código hecho, pendiente de verificación en el editor: hay que ejecutar `Waterpulse/Build Match Scene` y `Waterpulse/Build Menu Scene`).
    - **Mini placa** (`PlayerPlate`, creada por `PlayerMarkersView` desde una plantilla) sobre cada jugador de campo del equipo humano: rol abreviado (ATA/ORG/DEF), barra de energía con una marca en el coste de su habilidad. **Lista = energía ≥ coste de su habilidad** (decidido; no al 100 %). Lista solo se nota en que la barra se resalta un poco (`EnergyHighlight`: de volt apagado a volt y un resplandor suave), en la placa y en el panel; sin más avisos (pedido por el usuario). Los rivales no llevan placa.
    - **Flecha de la pelota** (chevron) sobre quien la lleva: blanca si es del equipo humano (encima de su placa), coral si es del rival.
    - **Destino del pase**: con la pelota, cuatro esquinas alrededor del compañero que la recibiría si pasas ahora, y su placa crece (×1,28). Usa `BallController.ReceiverFor`, el mismo cálculo que `Pass`.
    - **Siguiente para Q**: sin la pelota, las esquinas y la tecla Q marcan a quién pasaría el control. `SwitchTargeting` (Gameplay, con tests) es el cálculo que usan `ControlSwitcher` y el HUD.
    - **Jugador controlado**: tres arcos blancos en el agua bajo sus pies con una punta hacia donde mira (`SpriteRenderer` tumbado, `control_arcs` de `UiSpriteGenerator`). Sustituye al triángulo rojo (decidido).
    - **Panel del jugador** (rediseñado como la ficha de la maqueta, pedido por el usuario): franja del equipo, rol abreviado en grande, nombre del rol, "ENERGIA" con la cifra y la barra (marca del coste), y "ESTAMINA" en una línea fina blanca. Sin iconos ni tecla L: al pedir la habilidad sin energía parpadea en rojo el carril de la barra (antes, la tecla).
    - **Flecha a la portería quitada** (pedido por el usuario: "no cal que m'indiquis on és la porteria"). Se borra `GoalIndicator`.
    - **Paleta** (decidido, la de la maqueta): el amarillo del estilo pasa a volt `#D4FF3F` (acento, energía, selección de botones, logo y barra de tiro); en el HUD el local es blanco `#F2F6FA` y el visitante coral `#FF6B5A` (marcador y marcas). Los gorros 3D no cambian (local azul, visitante blanco): pendiente de decidir si se igualan.
    - Fuera de este cambio (no existen en el juego): porter controlable con parada al 100 %, reloj de posesión de 30 s y exclusiones.
  - Habilidades para todos (decidido por el usuario, cambia la Fase 6; código hecho, pendiente de verificación en el editor): todos los jugadores de campo, sea cual sea su rol, tienen el Xut del Tràngol y el Torpede Blau con la misma tecla L (Y en el mando): **con la pelota, Xut del Tràngol (35); sin ella, Torpede Blau (25)**. El Torpede ya no se puede usar con la pelota. `PlayerController` tiene `PowerShot` y `Boost` en lugar de `Ability`; cada jugador lleva los dos componentes y cada uno solo responde en su caso. La IA rival puede usar las dos (el Xut al ir a tirar, el Torpede para pelotas sueltas lejanas). La barra de energía del HUD marca el coste del Xut, el ataque especial. Porteros, sin habilidades.
  - Partículas de agua (adelantado de la Fase 10 a petición del usuario; solo las partículas, sin shakes ni altura de la pelota; código hecho, pendiente de verificación en el editor). Solo visuales: escuchan eventos y la velocidad, no cambian nada del juego.
    - **`PlayerWaterFx`** (Gameplay, en los 8 jugadores): espuma detrás al nadar, más cuanto más rápido (nada por debajo del 25 % de la velocidad máxima); esquitxada hacia atrás y anillo de espuma al hacer el dash (`DashUsed`); esquitxades laterales mientras dura el Torpede Blau.
    - **Habilidades**: el Xut del Tràngol lanza una columna de agua alta; el Torpede, la salpicadura de siempre.
    - **`BallWaterFx`** (Gameplay, en la pelota): esquitxada pequeña al rebotar (`BallBounced`) y al recibir un pase (`PassCompleted`), y una grande en la red al marcar (`Goal`).
    - Todas las partículas usan la gota de borde suave `fx_drop` (`UiSpriteGenerator`) con el material transparente `Splash`; antes se veían cuadrados. Las recetas (`Fx`) están como constantes visuales en `MatchSceneBuilder`.
  - Dash, portero, cámara lenta y onadas (pedido por el usuario, fuera de las fases; código hecho, pendiente de verificación en el editor):
    - **Dash siempre**: cada Shift da un impulso a `dashSpeedMultiplier` × la velocidad máxima durante `dashDuration` (0,075 s; la mitad de los 0,15 s que tenía, a petición del usuario), también yendo a tope (decidido; antes solo servía para recuperar velocidad). Se borra `DashPower` y `dashMinStrength`. La IA lo usa toda: rivales y también los compañeros del humano para llegar a su sitio si está a más de `aiDashDistance`.
    - **Saque del portero propio** (pedido por el usuario): cuando tu portero se queda la pelota (parada, muro o pelota suelta), el control pasa a él y eliges con el stick a quién pasar (J o clic derecho); ya no saca solo. Tras el saque vuelve el cambio automático. El portero rival sigue sacando solo.
    - **Coger al portero**: E (RT en el mando) coge al portero propio en cualquier momento, también durante la cámara lenta; otra E (o Q) vuelve al jugador de campo más cercano a la pelota. No con la pelota. Mientras llevas al portero no hay cambio automático. El portero del humano se mueve libre (no está limitado a su área).
    - **Mur de Ferro** (antes Mur d'Aigua; `MurAiguaAbility` + `WaterWall`, `Ability_MurAigua`; cambiado a petición del usuario): habilidad de los porteros, con L. Cuesta 100 y dura **5 s**. Un muro de metal cae desde `wallDropHeight` (6 m) en `wallDropTime` (0,25 s) justo delante de los postes (`WaterWallInset` 0,35 m; el portero queda delante): 4 × 2 paneles del modelo **Scifi Modular Walls** (CC0, Copabrangorandel, OpenGameArt; `Art/Models/ScifiWalls`, créditos en `docs/ui-credits.md`; rejas abajo y cruces arriba) de 0,95 m, entre dos pilares oscuros y con una franja de aviso amarilla y negra (`HazardStripes.png`, generada). Sin el modelo, una placa lisa y para todos los tiros desde que se activa (el portero se queda la pelota y cuenta como parada). Al tocar el agua: cortina de agua y neblina, **onada de verdad en el agua** (shader `WaterWaves`, globales `_ShockA`/`_ShockB`: cresta de `wallWaveHeight` 0,55 m con espuma que avanza a `wallWaveSpeed` 9 m/s durante `wallWaveLife` 1,6 s) con una rociada que viaja con la cresta, anillo de espuma, temblor de cámara fuerte (0,45 s, 0,4 m), golpe metálico grave (`wallImpact`, el "clonc" de los palos a `wallImpactPitch` 0,6) y **empuja** a los jugadores de campo rivales a menos de `wallPushRadius` (5 m) de la portería hasta `wallPushMargin` (2,5 m) más allá en `wallPushDuration` (0,6 s), por los aires (salto visual de 0,9 m y una vuelta) y con una esquitxada cada uno (`WallPush`, con tests; si llevan la pelota, se la quedan). Los jugadores no chocan con él. Al acabar se hunde. El generador pasa el asset ya creado a 5 s y al nuevo nombre una sola vez (`generatorVersion`). Energía propia del portero (`EnergyConfig_Porter`): empieza en 0 y se llena sola en ~60 s (`passiveRechargePerSecond`, decidido). El portero rival (IA) lo levanta solo cuando le tiran y la tiene llena (decidido); el del humano, solo si lo controlas. Placa y panel del portero con esa energía.
    - **Cámara lenta al tirar** (`ShotSlowMotion`): mientras cualquiera (también el rival, decidido) carga un tiro, el tiempo va a `shotSlowMotionScale` (0,3). Punto de mira en la portería (`ShotMeterView`, sprite `reticle`) que se mueve con arriba/abajo a `shotAimSpeed` (en tiempo real); el tiro va a ese punto (antes solo centro o palos). La barra de tiro también va más lenta. Al salir de la escena el tiempo vuelve a 1.
    - **Los defensas no paran tiros**: ya era así (la pelota ignora físicamente a todos los jugadores y solo el portero para por probabilidad); no se cambia nada.
    - **Onadas** (solo visuales, la física sigue plana): shader `Art/Shaders/WaterWaves` (tres ondas suaves, ~3,5 cm) sobre una malla subdividida `Art/Meshes/WaterGrid` (150 × 100 cuadros). Sin el shader, el agua plana de antes.
  - Concurso Premis LUDI 2026 de 3Cat (requisitos de las bases, https://premisludi.cat/bases/): prototipo jugable en navegador (Chrome, Safari, Firefox) adaptado a móvil, tableta y ordenador; tiene que funcionar **en local abriendo `index.html`, sin conexión**; juego y documentación en catalán; tráiler de 1 minuto como máximo; declarar assets de terceros y el uso de IA. Plazo: 5 de noviembre de 2026 a las 14:00.
    - **Build Web local** (`WebLocalBuilder`, menú `Waterpulse/Build Web (index.html local)`, o `-executeMethod Waterpulse.EditorTools.WebLocalBuilder.BuildBatch`): build WebGL sin compresión ni caché en `Builds/Web` y después un solo `Builds/WebLocal/index.html` con los archivos del build dentro (base64 → URLs blob:), que se abre con doble clic. Requiere el módulo **Web Build Support** de Unity.
    - **Plantilla Web `Waterpulse`** (`Assets/WebGLTemplates/Waterpulse`, la carpeta que exige Unity; se elige en Player Settings > Web > WebGL Template), para el build normal de itch.io. La de Unity (Default) dejaba el juego fijo a 960×540, también en pantalla completa. Esta ocupa todo el espacio (ventana, marco de itch o pantalla completa) con la resolución ajustada (`matchWebGLToCanvasSize`), con el mismo aspecto que el `index.html` local, y da el foco al juego al tocarlo. El `index.html` local no la usa: escribe su propio HTML.
    - **Controles táctiles** (Fase 9, según la guía de botones del equipo; `TouchControls`): joystick dinámico (`TouchJoystick`) en la mitad izquierda: aparece donde se pone el dedo; poca inclinación = nadar lento. A la derecha, tres botones grandes en arco (`TouchActionButton`) que cambian de función: **atacando** PASSA · XUTA · DASH (doble toque a XUTA = súper xut; XUTA espera 0,25 s el posible segundo toque antes de cargar; cargando, XUTA suelta y el joystick apunta); **defendiendo** CANVI · PORTER · ROBA (doble toque a ROBA = Torpede; añadido) y un DASH pequeño (añadido); **con el portero** CANVI/PASSA · CAMP · MUR (el botón de robar levanta el muro). Las pulsaciones van a `TouchInput` (Core), que leen `HumanBrain` y `ControlSwitcher` junto al teclado y el mando. Se ven solo en móvil y tableta (`Application.isMobilePlatform`, o pantalla táctil sin ratón para los iPad); en el editor se pueden forzar con `Waterpulse/Forçar controls de mòbil (editor)`. En vertical sale "GIRA EL MÒBIL" (añadido). Aspecto: blancos translúcidos (decidido); sprites del pack **Mobile Controls de Kenney (CC0)** en `Art/UI/Mobile` si están, si no, los círculos de `UiSpriteGenerator`.
    - **Guía de controles** (`ControlHints`, pedida por el usuario): franja abajo con solo las teclas que sirven ahora (con la pelota, cargando el tiro, defendiendo o con el portero). No sale en táctil. Pendiente: niveles de dificultad Fàcil/Difícil (la guía solo en Fàcil) y avisos de puntos en pantalla ("SÚPER GOL! +150").
  - Puntuación con multiplicador y dificultad (pedido por el usuario, cambia la Fase 7; código hecho, pendiente de verificación en el editor):
    - **Multiplicador de posesión** por equipo (`ScoreTracker`, sustituye al combo y a `ComboMultiplier`; valores en `ScoringConfigSO`): empieza en ×1; +0,1 por pase completado, +0,2 al recuperar (intercepción o robo); rachas de 3 pases (+0,2) y de 5 (+0,3); máximo ×3 (decidido). **Solo multiplica el gol** (decidido): gol = 100 × multiplicador, súper gol = 150 × multiplicador; pases, robos y paradas dan sus puntos fijos. Al perder la pelota vuelve a ×1 ("MULTIPLICADOR PERDUT") y al marcar se gasta. Se quita el +30 por 3 pases (lo sustituyen las rachas).
    - **Contraataque** (decidido): marcar a menos de 5 s de recuperar la pelota o de una parada da +50 (sin multiplicar). Así marcar rápido también tiene premio.
    - **HUD** (`ScoreFeedView`): "MULTIPLICADOR ×1,6" debajo del marcador (late al subir, apagado a ×1) y avisos a la derecha que suben y se desvanecen ("PASSADA +10 · ×1,1", "RATXA DE 3 PASSADES!", "SÚPER GOL! 150 × 1,6 = +240", "CONTRAATAC +50", "MULTIPLICADOR PERDUT" en rojo). Solo los del equipo humano.
    - **Dificultad** (decidido: dos niveles). "Partit individual" abre "Tria la dificultat" (Fàcil / Difícil); el elegido va en `PlayerSession.DifficultyIndex` y lo usan `TeamAI`, el portero rival (el muro solo si el nivel usa habilidades) y la guía de controles. **Fàcil** (`Difficulty_Facil`): reacción 0,6 s, agresividad 0,3, velocidad ×0,85, sin habilidades, 25 % de verde, con guía de controles. **Difícil** (`Difficulty_Dificil`): reacción 0,2 s, agresividad 0,8, velocidad ×1, con habilidades, 70 % de verde, sin guía. Se borra `Difficulty_Competidor`. Abrir `Match` directamente en el editor juega en Fàcil.
  - Puntos para ganar (decidido por el usuario): solo los puntos de gol deciden el partido (`MatchStateMachine.RegisterGoal(equipo, puntos)`, `HomePoints`/`AwayPoints`; `MatchScoring.GoalScored` se los pasa a `MatchFlow`). Marcador: puntos en grande y goles en pequeño debajo ("2 GOLS"); el resumen igual. Empate a puntos → gol de oro.
  - Correcciones tras probar: los botones táctiles solo en móvil y tableta (`Application.isMobilePlatform`), no en un ordenador con pantalla táctil; la guía de controles es texto. Llevando al portero (E), la cámara le sigue a él.
  - **Gradas** (del equipo: `Gradas`, gradas low-poly con público de huevos que saltan): se ponían a mano en las escenas y se perdían al regenerarlas (el partido ya las había perdido en el GitHub). Ahora las crea `BuildPool` en el partido y en el menú, alrededor del agua con su pasillo (`gap`) y a `StandsClearance` (0,7 m) de la pared, como las había ajustado el equipo, con la primera fila a 0,6 m (`StandsBaseHeight`; el equipo tenía 1 m y se veían algo altas). En el menú, un pabellón (suelo alrededor del agua y cuatro paredes de 16 m, sin sombras) a `cameraDistance` + 4 m del centro, para que no se vea el vacío y nunca tape la cámara que da vueltas (`MenuSceneBuilder.BuildHall`). **En el build desaparecían** (corregido): `Gradas` copiaba el material por defecto del pipeline, que en URP solo existe en el editor; ahora el generador le da `baseMaterial` (`Art/Materials/Stands.mat`, URP Lit). `Gradas.cs` se mueve a `Scripts/Gameplay` (mismo código y .meta) para que el generador lo pueda usar. **Nada se pone a mano en las escenas**: lo que no está en un generador se pierde.
  - **Controles con ratón** (pedido por el usuario): en Opcions (menú y pausa), botón "Controls: Teclat / Ratolí" (por defecto Ratolí mientras no se haya elegido, decidido; `ControlSchemeSetting` en Logic, guardado con `controlSchemeKey`; `ControlPreferences.UseMouse` en Core). Con ratón, además del teclado: **clic derecho = J** (pasar o robar), **clic izquierdo = K** (tirar; espera `mouseDoubleClickWindow`, 0,25 s, por si es doble; cargando el tiro suelta al momento), **doble clic izquierdo = L** (súper xut; sin pelota, Torpede; con el portero, el muro). Los clics sobre botones de la pantalla no cuentan (`Brains` usa `UnityEngine.UI` para saberlo). La guía de controles enseña los clics en modo ratón.
  - **Celebración del gol** (pedida por el usuario: "més emoció", estilo Rocket League; solo aspecto y sonido):
    - **Confeti** (`GoalCelebration`): un cañón en cada esquina de la piscina, con los colores del equipo que marca (local: blanco, volt y azul; visitante: coral, blanco y amarillo). Salen hacia arriba y con poca fuerza (5–8,5 m/s; 100 + 40 por cañón), así caen cerca de su esquina y no llenan toda la piscina (corrección tras probarlo). Cuadraditos que revolotean, caen frenados como papel, se posan en el agua (colisión con un plano a 5 cm) y se quedan flotando hasta desvanecerse (9–11 s de vida). Material `Confetti` (sin luz, dos caras).
    - **Cámara** (`MatchCamera`): temblor fuerte (`goalShakeDuration` 0,7 s, `goalShakeAmplitude` 0,45 m) y golpe de zoom (`goalZoomPunch` 7°, vuelve en `goalZoomDuration` 0,9 s).
    - **Público**: las gradas saltan hasta ×3 de alto y ×1,7 de rápido y vuelven a la normalidad en `goalCrowdHypeDuration` (4 s) (`Gradas.JumpBoost` / `SpeedBoost`).
    - **Pantalla** (`GoalFlashView`): destello volt que se apaga en 0,35 s y el bàner "GOL!" entra grande con un rebote.
    - **Agua**: la esquitxada de la red, más grande (180 gotas, más alta).
    - **Sonido**: en nuestro gol, además del silbato y la ovación, dos cañones de confeti, aplausos con fuegos artificiales y su crepitar; en el del rival, el "oooh" y un cañón. Los aplausos (9 s, subidos y comprimidos tras probarlo: no se oían) van por su propio canal (`goalApplauseLevel`), y la música baja al 30 % unos 4 s y vuelve en 1,5 s (`goalMusicDuck`). Grabaciones CC0 de OpenGameArt (`goal_applause`, `goal_cannon`, `goal_fireworks`; créditos en `docs/audio-credits.md`).
  - **IA en triángulo** (pedido por el usuario: "es massa fàcil anar recte i marcar"; código hecho, pendiente de verificación en el editor: ejecutar `Waterpulse/Build Match Scene`):
    - **Desmarque** (`SupportSpots`, AI, con tests; los dos equipos): cuando un jugador de campo lleva la pelota, sus dos compañeros forman un triángulo. **Punta**: `aiPointAhead` (4 m) por delante, en el lado con más sitio. **Suport**: `aiWingBack` (1,5 m) por detrás y abierto `aiWingSide` (5 m). Cada uno prueba `aiSupportCandidates` (8) puntos a `aiSupportSearchRadius` (2 m) de su hueco. Gana el que tiene la línea de pase limpia, está más lejos de los defensores y tiene tiro, sin alejarse del triángulo. Solo cambia si mejora `aiSupportSwitchMargin`. `TeamAI` reparte los huecos y cuenta los pases de la posesión.
    - **Con la pelota** (`AttackDecision`, reescrito):
      - Tira si está **desmarcado** (ningún defensor a menos de `aiOpenRadius`, 2,5 m) en zona de tiro con la línea libre y su equipo ya ha hecho `DifficultySO.minPassesBeforeShot` pases (en contraataque, sin defensores delante, tira igual).
      - Si no, pasa al compañero más desmarcado, esperando antes `aiMinHoldBeforePass` (0,5 s) salvo si le presionan.
      - **Tiro "porque sí"**: al recibir se sortea `DifficultySO.speculativeShotChance`, y si sale tira en zona de tiro aunque no esté libre.
      - Se mantiene el tiempo máximo con la pelota.
    - **Presión doble** (solo el rival): si el poseedor está a `aiDoublePressRange` (7 m) o menos de su portería, va un segundo defensor, a un lado (`aiDoublePressAngle` 60°), y queda un atacante sin marcar.
    - **Dificultad** (decidido): **Fàcil**, 20 % de tiro "porque sí" y 0 pases mínimos; **Difícil**, 8 % y 2 pases. El generador los rellena una sola vez en los niveles ya creados (`generatorVersion`).
  - Decisión: orientación del móvil **horizontal** (pregunta de la antigua Fase 8).
  - Fase 7 (aprobado): puntos del GDD en `ScoringConfigSO`; además, robo con J +20 (igual que interceptar). Multiplicador de combo por equipo: ×1 + 0,1 por acción positiva encadenada (pase, intercepción, robo, parada, gol), máximo ×2, se aplica a los puntos de esa posesión y vuelve a ×1 al cambiar la posesión. Combo de 3 pases: +30 al que marca tras 3 o más pases seguidos de su equipo en la misma posesión. Gol con habilidad (+50) = gol con el Xut del Tràngol. Habilidad con éxito: Xut = gol; Torpede = consigue la pelota durante el acelerón o hasta 0,5 s después, o la tenía y su equipo no la pierde. Gol en propia puerta: sin puntos. Récord personal = puntos totales del equipo LOCAL en un partido (`PlayerPrefs`, se guarda al acabar el tiempo); el primer partido siempre es récord. Pantalla final con los 8 jugadores por rol en catalán, MVP (más puntos de los dos equipos), totales, pases y habilidades con éxito.
  - Fase 6 (aprobado): energía por jugador (`PlayerEnergy` + `EnergyMeter`), empieza en 50. Recompensas: pase completado +5 al pasador; intercepción o robo +10 al que recupera; gol +10 al que marca (no en propia puerta). Atacant → Xut del Tràngol (L con pelota, 35, tiro a 1,6×; el portero solo lo para si le llega al cuerpo: alcance 0,6 m). Organitzador y Defensor → Torpede Blau (L, 25, 2× velocidad 1 s, dirección fija). **Cambiado después:** ahora todos los jugadores de campo tienen las dos (ver "Habilidades para todos"). Porteros sin habilidad. La IA rival usa el Xut cuando iba a tirar y el Torpede para llegar a pelotas sueltas a más de 6 m (`DifficultySO.useAbilities`). Efectos: estela en la pelota (Xut) o en el jugador (Torpede), salpicadura y nombre en pantalla 1 s. Texto provisional "Energia: N" del jugador controlado hasta el HUD de la Fase 9. L también en el botón Y del mando.
  - Fase 5 (aprobado): `MatchStateMachine` y `MatchTimer` puros en Core; `MatchFlow` (Gameplay) los conecta con la escena y sustituye a `GoalRestart` y `MatchScore`. Primer saque del local; tras gol saca quien encaja. En cada saque todos vuelven a su posición (`KickoffLayout`, valores en config) y se quedan quietos 1,5 s ("Preparats…"); "GOL!" 1,5 s; "Final del partit" 1,5 s antes de la pantalla final. Los jugadores solo obedecen a su cerebro en `Playing`. Pantalla final básica con resultado, ganador y "Tornar a jugar" (botón o J/Enter), que recarga la escena. Nombres de equipo en config ("LOCAL", "VISITANT").
  - Fase 4 (aprobado): al defender, el control salta al jugador de campo propio más cercano a la pelota (margen 1,5 m, espera 0,5 s). Recuperar con J: alcance 1,2 m, 50 % al límite y 75 % pegado, 0,8 s de espera si falla. Intercepción automática: 40 % si el pase pasa a menos de 0,8 m de un rival (una tirada por rival y pase); los tiros no se interceptan. Los compañeros del humano solo se colocan y marcan. IA rival: avanza, pasa si la presionan (a menos de 1,5 m) y tira a menos de 6 m con la línea libre, a un palo al azar. Dificultad única "Competidor" (`DifficultySO`), sin habilidades.
  - Fase 4 (añadido y validado): tras ganar la pelota hay 0,5 s en los que no te la pueden quitar (`recoverImmunity`, 0 = desactivado), para evitar robos en cadena; y a los porteros no se les puede quitar la pelota.
  - Fase 4: desaparece la restricción temporal `canTakeBall` de la Fase 3.
  - Fase 4 (corrección tras prueba): solo un jugador va a por el poseedor; `TeamAI` reparte un marcaje único por atacante (nunca al poseedor) y solo cuando el equipo defiende (sin posesión de nadie, todos en formación; así se evita el bucle de marcajes cruzados). Presionador, perseguidor e interceptor van a máxima velocidad. Los compañeros del humano tienen un `TeamAI` pasivo: solo marcajes.
  - Fase 3 (aprobado): gorros local azul, visitante blanco, porteros rojo; bañador del color del equipo. Parada por probabilidad (`SaveOdds`): 80 % al cuerpo, 0 % a 1,2 m; el portero ya no bloquea físicamente. Tras parar, saca al compañero libre más cercano a ~1 s. Colocación por rol según GDD sección 7 (con posesión se abren; sin posesión defienden y marcan al rival cercano). Las diferencias de estadísticas por rol se dejan para la Fase 11 (Balance); los roles aún no se distinguen visualmente (pendiente de arte).
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
