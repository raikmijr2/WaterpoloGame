# Waterpulse — Game Design Document

Oct 15, 2026 · @Eric

> Transcripció del PDF original "Waterpulse — Game Design Document".

## 1. Visió general

Waterpulse és un joc de waterpolo arcade per a navegador: dos equips de 3+1 jugadors competeixen per marcar més gols en partits de 2–3 minuts, amb habilitats especials que converteixen les jugades en accions espectaculars.

La jugabilitat parteix de les accions bàsiques del waterpolo (nedar, passar, interceptar, xutar i aturar). La intensitat s'inspira en Inazuma Eleven, però amb poques habilitats, controls intuïtius i decisions tàctiques fàcils d'entendre. Algú que no conegui el waterpolo ha de poder jugar de seguida i tenir motius per repetir.

| Camp | Valor |
| --- | --- |
| Títol provisional | Waterpulse |
| Gènere | Esportiu, arcade, competitiu |
| Públic objectiu | 25–35 anys, aficionats a l'esport i jugadors casuals |
| Plataforma | Navegador web (ordinador, mòbil i tauleta) |
| Durada del partit | 2–3 minuts |
| Format d'equip | 3 jugadors de camp + 1 porter |
| Mode del prototip | 1 partit contra la IA, en una única piscina |
| Referents | Inazuma Eleven (espectacle d'habilitats), Rocket League (estadístiques de partit) |
| Context | Candidatura als Premis Lúdic de Catalunya (temàtica: esport poc conegut) |

## 2. Pilars de disseny

Quatre principis guien totes les decisions: si una funció no reforça cap d'aquests pilars, queda fora del prototip.

1. **Entendre-ho en un minut.** Quatre accions (moure, passar/recuperar, xutar, habilitat) i regles mínimes. No cal conèixer el waterpolo.
2. **Espectacle amb decisió.** Les habilitats són vistoses però no garanteixen l'èxit: el que compta és triar el moment.
3. **Llegibilitat abans que realisme.** Sempre s'ha de veure qui té la pilota, on és la porteria i quan s'activa una habilitat.
4. **Partides curtes que conviden a repetir.** 2–3 minuts, puntuació, estadístiques i rècord personal.

## 3. Jugabilitat i regles

Guanya l'equip que marca més gols abans que s'acabi el cronòmetre; tota la resta del partit gira al voltant de guanyar i mantenir la possessió.

**Càmera.** Vista zenital o lleugerament inclinada sobre la piscina. Es veuen alhora companys, rivals, pilota i porteries, cosa que simplifica controls i desenvolupament.

**Equips.** Cada equip té 3 jugadors de camp i 1 porter. És una simplificació arcade del waterpolo real (6+1) per afavorir partides ràpides i fàcils de seguir.

### Regles bàsiques

- Guanya qui marca més gols quan s'acaba el temps.
- Amb la pilota, el jugador pot passar a un company o xutar a porteria.
- Els rivals poden interceptar passades o recuperar la pilota.
- Cada jugador té una habilitat especial que consumeix energia.
- Les habilitats no garanteixen l'èxit: cal activar-les en el moment adequat.

### Bucle de joc

- **Atac:** nedar amb la pilota → passar per obrir espai → xutar (normal o amb habilitat) → gol o aturada.
- **Defensa:** marcar l'atacant → interceptar o recuperar → muralla o aturada del porter.
- **Transició:** quan es perd la pilota, el rol s'inverteix i el multiplicador de combo torna a zero.

**Cada possessió acaba en gol, aturada o intercepció** (bucle de possessió · 4 fases):

1. **Tenim la pilota** — nedar i buscar espai → (passades: +energia i combo) →
2. **Atacar** — passar o xutar → (xut o passada) →
3. **Perdem la pilota** — gol, aturada o intercepció → (el combo torna a zero) →
4. **Defensar** — interceptar o recuperar → (recuperació) → torna a 1.

L'atac acumula energia i combo; perdre la pilota posa el combo a zero i obliga a defensar per recuperar-la.

## 4. Energia i habilitats especials

Cada jugador té una barra de 100 punts d'energia que es gasta en habilitats i es recupera jugant bé; el prototip només n'implementa dues (Xut del Tràngol i Torpede Blau).

### Energia

| Acció | Energia recuperada |
| --- | --- |
| Completar una passada | +5 |
| Recuperar una pilota | +10 |
| Marcar un gol | +10 |

Costos i recompenses s'han d'ajustar en proves: les habilitats no s'han de poder activar constantment, però tampoc s'ha d'esperar massa per fer-les servir.

### Habilitats

| Habilitat | Tipus | Efecte | Durada | Cost | Contrajoc | Prototip |
| --- | --- | --- | --- | --- | --- | --- |
| Xut del Tràngol | Atac | Llançament carregat molt potent que supera part de la defensa | Instantani | 35 | El porter el pot aturar si l'anticipa | Sí |
| Passada Llampec | Atac | Passada ràpida i precisa, més difícil d'interceptar | Instantani | 20 | Muralla d'Aigua | No |
| Torpede Blau | Moviment | Acceleració explosiva per escapar, atacar o tornar a defensar | ~1 s | 25 | Mal dirigit, allunya el jugador de la jugada | Sí |
| Muralla d'Aigua | Defensa | Barrera que bloqueja un xut especial o una passada | ~1,2 s | 30 | Si s'activa aviat, l'atacant espera o canvia de banda | No |
| Reflex Felí | Porter | Estirada que augmenta molt la probabilitat d'aturar el pròxim xut | Pròxim xut | 35 | Un xut ben col·locat pot entrar igualment | No |
| Remolí Recuperador | Recuperació | Augmenta temporalment el radi d'intercepció | ~0,8 s | 25 | No roba la pilota a qualsevol distància | No |

Cap habilitat és una victòria garantida: cadascuna té una resposta, i el temps d'activació és la decisió clau.

## 5. Rols dels jugadors

Cada rol té una fortalesa clara i una habilitat afí; les diferències són petites (moviment, precisió, intercepció), sense estadístiques complicades.

| Rol | Fortalesa | Habilitat afí |
| --- | --- | --- |
| Atacant | Millor xut i més capacitat ofensiva | Xut del Tràngol |
| Organitzador | Passades més precises i recuperació d'energia eficient | Passada Llampec |
| Defensor | Més capacitat d'intercepció i de bloqueig | Muralla d'Aigua |
| Porter | Essencial per evitar gols | Reflex Felí |

El prototip no inclou fitxatges, nivells ni atributs detallats.

## 6. Controls

Els controls han de permetre jugar en menys d'un minut, amb la mateixa lògica a totes les plataformes.

| Acció | Ordinador | Mòbil i tauleta |
| --- | --- | --- |
| Moure el jugador | W, A, S, D | Joystick virtual (esquerra) |
| Passar / intentar recuperar (segons possessió) | J | Botó gran 1 (dreta) |
| Xutar (amb pilota) | K | Botó gran 2 (dreta) |
| Activar habilitat | L | Botó gran 3 (dreta) |

El jugador controla un sol personatge; els companys es mouen sols segons la posició de la pilota i el seu rol. El canvi automàtic al company més ben situat queda per a una versió posterior.

## 7. Intel·ligència artificial

El prototip necessita una IA bàsica que persegueixi la pilota, defensi i xuti; la dificultat s'ajusta amb velocitat i agressivitat, no amb trampes.

| Agent | Amb possessió pròpia | Sense possessió |
| --- | --- | --- |
| Companys | Es col·loquen segons la pilota i el rol per oferir línia de passada | Tornen a defensar i marquen rivals propers |
| Rivals de camp | Avancen, passen i xuten | Persegueixen la pilota i intenten interceptar |
| Porters | Inicien la jugada amb una passada | Es mouen davant la porteria i intenten aturar |

La dificultat (secció 9) modifica la velocitat dels rivals, com d'activa és la defensa i si la IA fa servir habilitats.

## 8. Puntuació, estadístiques i rejugabilitat

Els gols decideixen qui guanya; els punts mesuren qui ha jugat millor, com a Rocket League, i alimenten el rècord personal.

### Punts per jugador

| Acció | Punts |
| --- | --- |
| Marcar un gol | +100 |
| Marcar amb una habilitat especial | +50 addicionals |
| Fer tres passades consecutives abans de marcar | +30 de combo |
| Aturar un xut | +25 |
| Interceptar una passada rival | +20 |
| Completar una passada | +10 |

**Multiplicador de combo.** Puja quan s'encadenen accions positives sense perdre la pilota i torna a zero quan es perd la possessió.

### Pantalla final

- Resultat (gols a favor i en contra): determina guanyador i perdedor.
- Puntuació total i per jugador, amb el millor jugador del partit destacat.
- Passades completades i habilitats utilitzades amb èxit.
- Rècord personal, guardat localment al navegador.
- Opció de reiniciar el partit.

## 9. Progressió i dificultat

El prototip implementa una partida completa amb dificultat ajustable; els tres nivells i les recompenses són desenvolupament futur.

| Nivell | Rivals | Ajudes i objectius |
| --- | --- | --- |
| Aprenent | Lents | Instruccions en pantalla |
| Competidor | Defensa més activa i ús d'habilitats | — |
| Llegenda de l'aigua | Més ràpids | Objectius de puntuació més alts |

La motivació a llarg termini ve dels rècords, les medalles i les aparences desbloquejables. No hi ha millores permanents de potència, per mantenir el joc just.

## 10. Identitat catalana i relació amb 3Cat

La identitat catalana arriba per l'ambientació (clubs, piscines i veu en català), amb una identitat visual pròpia; qualsevol element oficial de 3Cat queda com a col·laboració futura.

- **Competició fictícia** entre clubs inspirats en l'esport de base del país, amb noms i escuts originals.
- **Escenaris** que recorden piscines municipals i instal·lacions aquàtiques catalanes, sense reproduir espais reals.
- **So:** públic, xiulets arbitrals, esquitxades i comentaris breus en català.
- **Valors:** joc net, cooperació, esforç i respecte pel rival.
- **Apartat divulgatiu opcional** amb les regles bàsiques del waterpolo real.
- **Interfície tipus retransmissió:** marcador, cronòmetre i repeticions breus.

Marques, logotips, imatges o veus oficials de 3Cat requeririen permisos; el prototip no en fa servir.

## 11. Estil visual, interfície i àudio

Estil 2D o 2.5D amb personatges expressius i aigua exagerada només quan hi ha habilitats; la prioritat és la llegibilitat, no el realisme.

| Element | Direcció |
| --- | --- |
| Camp | Piscina clara, línies visibles, porteries fàcils d'identificar |
| Personatges | Dissenys diferenciats per posició; gorres de colors contrastats per equip |
| Efectes | Esteles, esquitxades i nom de l'habilitat en pantalla quan s'activa |
| Interfície (HUD) | Marcador, temps restant, barra d'energia, indicador de possessió |
| Àudio | Públic, xiulets, esquitxades, comentaris breus en català |

No es fa simulació realista d'aigua: seria cara i no millora la jugabilitat. El jugador sempre ha de distingir qui té la pilota, on és la porteria i quan s'activa una habilitat.

## 12. Abast del prototip (vertical slice)

El vertical slice demostra el bucle principal en un partit curt, complet i funcional: poques mecàniques ben fetes abans que moltes a mitges.

### Inclòs

- [ ] Piscina amb dues porteries, límits i marcador
- [ ] Dos equips: jugador controlable, companys, rivals i porters
- [ ] Passada, xut, aturada i canvi de possessió
- [ ] Xut del Tràngol i Torpede Blau, amb efectes visuals
- [ ] IA bàsica: perseguir la pilota, defensar i xutar
- [ ] Partit complet amb cronòmetre, gols, pantalla final i reinici
- [ ] Puntuació i rècord guardat localment
- [ ] Controls de teclat i botons tàctils per a mòbil

### Fora de la primera versió

- Multijugador en línia
- Campionats amb molts equips
- Les sis habilitats implementades i equilibrades
- Fitxatges, botiga i personalització extensa
- Física realista de natació i pilota
- Narrativa llarga o cinemàtiques

## 13. Riscos, decisions obertes i pla de proves

El risc principal és l'equilibri energia–habilitats; el text original deixa obertes algunes regles de partit que cal fixar abans de programar.

### Decisions obertes

- [ ] Com es reprèn el joc després d'un gol (sacada des del centre, possessió per a qui l'ha rebut?)
- [ ] Què passa si la pilota surt de la piscina
- [ ] Hi ha límit de temps de possessió per xutar, com al waterpolo real?
- [ ] Què passa amb un empat al final del temps (pròrroga, gol d'or, empat vàlid)
- [ ] Durada exacta del partit dins de 2–3 minuts
- [ ] Tecnologia web per al prototip

### Riscos

| Risc | Mitigació |
| --- | --- |
| Habilitats massa freqüents o massa escasses | Ajustar costos i recompenses d'energia en proves |
| Controls tàctils imprecisos en pantalles petites | Botons grans i provar aviat en mòbil real |
| IA massa simple o massa injusta | Dificultat ajustable per velocitat i agressivitat |
| Poca llegibilitat amb efectes d'aigua | Efectes només en habilitats; indicador de possessió sempre visible |

### Pla de proves

- Un jugador nou entén els controls en menys d'un minut.
- Freqüència d'ús d'habilitats per partit, per ajustar l'energia.
- La pantalla final reflecteix qui ha jugat millor segons els punts.
