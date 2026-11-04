# Crèdits de la interfície

## Fonts

| Fitxer | Font | Autors | Llicència |
| --- | --- | --- | --- |
| `Assets/_Waterpulse/Art/Fonts/BarlowCondensed-Bold.ttf` | Barlow Condensed Bold | The Barlow Project Authors (Jeremy Tribby) | [SIL Open Font License 1.1](../Assets/_Waterpulse/Art/Fonts/OFL-BarlowCondensed.txt) |
| `Assets/_Waterpulse/Art/Fonts/Barlow-SemiBold.ttf` | Barlow SemiBold | The Barlow Project Authors (Jeremy Tribby) | [SIL Open Font License 1.1](../Assets/_Waterpulse/Art/Fonts/OFL-Barlow.txt) |

Descarregades del repositori oficial de Google Fonts ([github.com/google/fonts](https://github.com/google/fonts), carpetes `ofl/barlowcondensed` i `ofl/barlow`). L'OFL permet fer-les servir, incrustar-les i distribuir-les amb el joc, també en projectes comercials. L'única condició és mantenir-ne la llicència al costat dels fitxers (`OFL-*.txt`) i no vendre-les per separat.

## Formes de la interfície

Els panells arrodonits, les vores, les barres en càpsula i les icones (llamp, onades i fletxa) de `Assets/_Waterpulse/Art/UI/` són pròpies del projecte. Les genera `Waterpulse/Generate UI Sprites` (`UiSpriteGenerator`). Per canviar-ne una, poseu-hi un PNG amb el mateix nom: el generador no el trepitja.

## Models 3D

| Fitxer | Origen | Autor | Llicència |
| --- | --- | --- | --- |
| `Assets/_Waterpulse/Art/Models/ScifiWalls/ScifiModularWalls.obj` | [Scifi Modular Walls](https://opengameart.org/content/scifi-modular-walls) (OpenGameArt) | Copabrangorandel | [CC0](https://creativecommons.org/publicdomain/zero/1.0/) |

Els panells del Mur de Ferro (habilitat del porter). Es va baixar només el `.obj`, que és text, i se'n va treure la referència al `.mtl`, que no ve inclòs: el material metàl·lic el posa el generador del partit.
