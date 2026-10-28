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
- **Eventos desacoplados.** Los sistemas se comunican por `GameEvents`: `Goal`, `PassCompleted`, `Interception`, `Save`, `AbilityUsed`, `PossessionChanged`, `MatchStateChanged`, y además `BallRecovered` (robo con J) y `ControlledPlayerChanged` (jugador que controla el humano), añadidos en la Fase 6, y `RecoverFailed` (intento de robo fallido), añadido en la Fase 8, y `PauseChanged` (pausa del partido), añadido con el menú de pausa, y `PassThrown`, `ShotTaken`, `DashUsed`, `BallBounced` y `GoldenGoalStarted`, añadidos para el sonido, y `AbilityNoEnergy` (habilidad pedida sin energía suficiente). HUD, puntuación, audio y estadísticas escuchan; no se referencian entre sí.
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

### Fase 8 — Campo, cámara y estamina (añadida a petición del usuario)
- Campo de 30×20 m (porterías de 3 m).
- Cámara que sigue la jugada con suavizado (no encuadra todo el campo) y flecha en el borde de la pantalla hacia la portería rival cuando no se ve.
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
  - Menú principal e intro (añadido a petición del usuario, fuera de las fases; código hecho, pendiente de verificación en el editor). Escena `Menu` (índice 0 del build, `Waterpulse/Build Menu Scene`) con la piscina del partido de fondo y la cámara girando despacio (`MenuConfigSO`). Intro: logo con fundido y "Prem qualsevol tecla" (tecla, clic, toque o mando); solo sale la primera vez en cada sesión. Menú: "Partit individual" (escena `Match`), "1 contra 1 local" visible pero desactivado con la etiqueta "Aviat" (el modo se hará más adelante) y "Opcions". Opcions: volumen general (se guarda con `VolumeSetting`; se aplica a todo el audio vía `AudioListener.volume`, aún no hay sonido), pantalla completa (no se guarda; en Web se pide al pulsar con `PressButton`), tabla de controles y "Esborrar rècord" con confirmación (añadido por Claude, aprobado). La pantalla final tiene también "Menú principal" (botón o Esc/Start). Navegación: WASD/flechas/stick, Enter/A, Esc/B atrás; el ratón selecciona al pasar por encima. Estilo compartido en `UiFactory` (Editor): botones azules del local, amarillo al seleccionar, paneles oscuros. No se renumeran las fases.
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
    - **Barra de tiro** (`ShotMeter`, Logic): la primera K empieza a cargar y el jugador se queda quieto y sube (`shotRiseHeight` 0,25 m, solo visual; bajado de 0,6 a petición del usuario). La barra se llena en `shotMeterDuration` (1 s). La segunda K tira apuntando con el stick de ese momento.
      - Zona verde: de `shotGreenStart` (0,85), `shotGreenWidth` (0,08) de ancho. En el verde, calidad 1 (el tiro de siempre). Fuera, como mucho `shotOffGreenMaxQuality` (0,7) al lado de la zona, y 0 en los extremos. Si la barra se llena, tira solo con calidad 0.
      - Con menos calidad el tiro sale más flojo (hasta `shotMinPowerFactor` 0,6) y desviado hacia un lado al azar (hasta `shotMaxErrorAngle` 12°), y puede irse fuera.
      - Mientras carga se le puede robar, y entonces se corta. Durante la carga no hay habilidad ni dash.
      - El Xut del Tràngol sigue siendo instantáneo.
    - **IA**: suelta en el verde con `DifficultySO.shotGreenChance` (50 %). Si falla, suelta entre `shotMissMinFill` (0,5) y el verde.
    - **HUD**: `ShotMeterView`, una barra vertical al lado del jugador del humano mientras carga. Solo la del equipo humano.
    - Controles: "K + K" y "B + B" en la tabla.
    - Limitación: la pelota no sube con el jugador (su Y sigue fija hasta la altura visual de la Fase 10).
  - Fase 8 (pedido por el usuario, código hecho, pendiente de verificación): modelo animado del usuario en los 3 jugadores de campo LOCAL. Los porteros y el visitante siguen con cápsulas.
    - **Modelo**: prefab `Prefabs/Waterpolo_TreadingWater` (o `GameConfigSO.homePlayerModel`), dentro del hijo `Visual`, con `playerModelOffset` (0, −0,825, 0) y `playerModelScale` 1,5 (subida de 1 a petición del usuario: se veían pequeños). El generador deja su Rigidbody cinemático y su collider desactivado.
    - **Animator Controller** del usuario (`Anims/AnimationController`): estados TreadingWater, SwimFast, Throw y StealTouch. Parámetros: `Speed` (float, velocidad / máxima), `Throw` (trigger, al pulsar pasar y al soltar la barra de tiro) y `Steal` (trigger, al intentar robar al alcance, salga bien o mal).
    - **`PlayerAnimator`** (Gameplay): va en el objeto del Animator y rellena esos parámetros. Recibe el evento `ReleaseBall` del clip Throw, que es cuando sale la pelota en el pase y en el tiro. Si el evento no llega en `throwReleaseTimeout` (1 s), sale igualmente.
    - Sin modelo, el pase y el tiro salen al instante.
    - **Pelota en la mano** (pedido por el usuario, solo visual): la pelota física sigue igual (delante del jugador, en el agua) y la esfera visible es una hija con `BallVisual`.
      - Mientras la lleva un jugador con modelo, se ve en su mano derecha (`ballHandBone`, con `ballHandOffset` para la palma) al flotar, nadar y lanzar. Decidido por Claude, ya que el usuario no tenía preferencia; se podría hacer que al nadar vaya empujada delante de la cara.
      - Al soltarla pasa de la mano a su trayectoria en `ballReleaseBlendTime` (0,1 s).
      - Con las cápsulas se ve donde está.
    - Indicador del jugador controlado (pedido por el usuario): un triángulo rojo con la punta hacia abajo sobre su cabeza, como en el FIFA (malla `Art/Meshes/ControlTriangle`, a 1,3 m sobre el agua). Sustituye al disco verde.
    - Solo para pruebas: `Waterpulse/Build Match Scene (sin rivales)` genera la escena sin el equipo visitante (ni portero). Tras un gol la pelota queda libre en el centro. `Build Match Scene` la deja normal.
    - El Xut del Tràngol sigue instantáneo y sin animación (pendiente de que lo decida el usuario).
  - Decisión: orientación del móvil **horizontal** (pregunta de la antigua Fase 8).
  - Fase 7 (aprobado): puntos del GDD en `ScoringConfigSO`; además, robo con J +20 (igual que interceptar). Multiplicador de combo por equipo: ×1 + 0,1 por acción positiva encadenada (pase, intercepción, robo, parada, gol), máximo ×2, se aplica a los puntos de esa posesión y vuelve a ×1 al cambiar la posesión. Combo de 3 pases: +30 al que marca tras 3 o más pases seguidos de su equipo en la misma posesión. Gol con habilidad (+50) = gol con el Xut del Tràngol. Habilidad con éxito: Xut = gol; Torpede = consigue la pelota durante el acelerón o hasta 0,5 s después, o la tenía y su equipo no la pierde. Gol en propia puerta: sin puntos. Récord personal = puntos totales del equipo LOCAL en un partido (`PlayerPrefs`, se guarda al acabar el tiempo); el primer partido siempre es récord. Pantalla final con los 8 jugadores por rol en catalán, MVP (más puntos de los dos equipos), totales, pases y habilidades con éxito.
  - Fase 6 (aprobado): energía por jugador (`PlayerEnergy` + `EnergyMeter`), empieza en 50. Recompensas: pase completado +5 al pasador; intercepción o robo +10 al que recupera; gol +10 al que marca (no en propia puerta). Atacant → Xut del Tràngol (L con pelota, 35, tiro a 1,6×; el portero solo lo para si le llega al cuerpo: alcance 0,6 m). Organitzador y Defensor → Torpede Blau (L, 25, 2× velocidad 1 s, dirección fija). Porteros sin habilidad. La IA rival usa el Xut cuando iba a tirar y el Torpede para llegar a pelotas sueltas a más de 6 m (`DifficultySO.useAbilities`). Efectos: estela en la pelota (Xut) o en el jugador (Torpede), salpicadura y nombre en pantalla 1 s. Texto provisional "Energia: N" del jugador controlado hasta el HUD de la Fase 9. L también en el botón Y del mando.
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
