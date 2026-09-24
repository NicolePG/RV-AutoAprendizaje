# Escape del Laboratorio — Escape Room VR

Contexto del proyecto para trabajar con Claude Code. Leer esto antes de escribir código.

---

## 1. Qué es esto

Proyecto universitario de la materia **Realidad Virtual y Aumentada**. Es un
prototipo VR de escape room de **4 cuartos**, hecho en **Unity** para **Oculus
Quest**, por una pareja de estudiantes, con fecha de entrega el **29 de
septiembre de 2026**.

No es un juego grande. La consigna es explícita: **una mecánica principal bien
hecha vale más que diez mecánicas a medias**. La nota se basa en idea, diseño,
implementación, organización y pruebas, no en gráficos.

**Concepto (*Protocolo Apagón*):** un corte de energía deja al jugador atrapado
dentro de un colegio cerrado hace tiempo. Debe resolver un acertijo en cada cuarto
para abrir la siguiente puerta y escapar antes de que se acabe el tiempo.

**Documento de diseño:** el GDD del equipo (*Protocolo Apagón — Documento de
Diseño*) tiene el detalle de cada cuarto, sus coordenadas y los sustos. Si este
archivo y el GDD dicen cosas distintas, **manda el GDD**.

**Mecánica principal:** resolver acertijos manipulando objetos físicos con los
controles del Quest.

---

## 2. Requisitos obligatorios de la materia

Estos puntos se evalúan de forma directa y hay que poder defenderlos en la
presentación:

| Requisito | Estado esperado |
|---|---|
| Interacción con controles VR | Agarrar, encajar, presionar, jalar |
| Movimiento en el entorno | Teletransportación (nunca joystick continuo) |
| Mecánica principal clara | Resolver acertijos con las manos |
| UI y feedback al usuario | Menús, reloj, luces y sonidos de acierto/error |
| **Scriptable Objects justificados** | `PuzzleData`, `ItemData`, `RoomData` |
| **Sistema de guardado y carga** | JSON persistente, se demuestra en vivo |
| Pruebas con XR Device Simulator | Durante todo el desarrollo |
| Pruebas en Oculus Quest real | En las sesiones de clase |
| Control de versiones con Git | Ramas feature, commits pequeños, ambos aportan |

Sobre los Scriptable Objects: **no vale crearlos solo para cumplir**. Hay que
poder explicar qué problema resuelven. En este proyecto evitan el clásico
`if (tipoAcertijo == "codigo")` y permiten crear acertijos y objetos nuevos como
assets, sin tocar código.

Sobre el guardado: debe funcionar el ciclo **jugar → guardar → cerrar → cargar →
recuperar el progreso**. No basta con guardar en memoria mientras corre el juego.

---

## 3. Entorno técnico

- Unity con **XR Interaction Toolkit** (XRI)
- **XR Plugin Management** con OpenXR, target Android (Quest)
- **XR Device Simulator** habilitado para probar sin visor
- Los desarrolladores trabajan en Windows

---

## 4. Diseño del juego

### Core loop

```
Explorar el cuarto
  → encontrar pista u objeto
  → interactuar con las manos
  → resolver el acertijo
  → se abre la puerta y se guarda el progreso
  → siguiente cuarto
```

Salidas del ciclo: resolver el cuarto 4 gana la partida; que el temporizador
llegue a cero muestra la pantalla de tiempo agotado, con opción de cargar la
última partida.

### Los 4 cuartos

| # | Cuarto | Acertijo | Interacción XRI |
|---|---|---|---|
| 1 | Recepción (portería) | Llave en un cajón bajo el mostrador; la cerradura tiene una tapa atornillada | Grab + Socket, cajones, botones |
| 2 | Dirección | Código de 4 dígitos: reloj real (1 de 5 idénticos), libro y cuadro | Teclado (Simple Interactable) |
| 3 | Sala de computación | 3 de las 5 computadoras muestran un carácter; se marcan en la consola | Botones |
| 4 | Laboratorio de ciencias | Fusibles → campana de extracción → llaves de gas → ensayo a la llama con 3 palancas → tarjeta del docente en el lector de la salida | Grab + Socket, perillas, palancas, trigger |

El cuarto 1 funciona como tutorial: enseña a agarrar sin decirlo.

**Cuarto 1 paso a paso** (cada paso enseña una mecánica):
1. Empieza a oscuras (solo luz de emergencia). Subir la **palanca** del interruptor
   general, debajo del tablero eléctrico → vuelve la energía: luz y computadora (`Lever`).
2. **Presionar PLAY** en la computadora → mensaje con la pista (`PressableButton`).
3. El póster muestra el escudo (rombo azul) → **abrir cajones** (`Drawer`): rombo =
   llave, cuadrado = destornillador, círculo = señuelo.
4. Con el destornillador **quitar los 2 tornillos**: apoyar la punta (el tornillo se
   pone ámbar) y **girar** (muñeca en el Quest, mouse en círculos en el PC). Cada
   tornillo sale con 2 vueltas y cae la tapa del panel (`Destornillador`, `Screw`, `ScrewedPanel`).
5. Brilla un **aro ámbar** en la cerradura. **Soltar la llave cerca** → viaja sola a
   su lugar, entra, gira, clic; panel verde "ABIERTO" y la puerta se abre sola con
   pestillo y frenando al final; se enciende la luz del pasillo
   (`KeyPuzzle` + `PuzzleData` + `Door`).

**Cuarto 3 — Sala de Computación (en construcción):** lo arma
`Assets/Editor/ConstructorCuarto3.cs` (para probarlo: **Escape Room › Llevar jugador al
Cuarto 3**). Mide 8 × 7,52 m con techo de 3,2 m y
ocupa el lugar del pasillo provisional, que el constructor apaga. La sala la arma
`ConstructorCuarto3.cs` y la noche con los acertijos `ConstructorCuarto3Acertijos.cs`.
Es **de noche**: entra a oscuras (luna por la persiana, cartel de salida y una linterna
prendida sobre las cajas, cuyo haz cae sobre el tablero). La linterna se agarra como las
herramientas del Cuarto 1 (`HerramientaEnMano` con `seguirMirada`) y alumbra hacia donde
se apunta. Cuatro acertijos en cadena, cada uno con su pista en la sala:
1. **Luces** (`TableroLuces`, `Interruptor`): 4 interruptores que cambian varias lámparas
   a la vez; hay que dejar las 6 prendidas. El tablero manda a leer el **registro de
   mantenimiento** (hoja sobre el escritorio del profesor): "el 2 quedó cruzado, no usarlo".
   Solución única: 1 + 3 + 4.
2. **Red** (`RedSala`, `TomaDeRed`, `FichaRed`, `CableVisual`): de la torre de C2, C3 y C5
   sale su cable, con la ficha sobre la mesada. Cada ficha se agarra y se suelta cerca de su
   toma del panel de red (pared izquierda, al final de la mesada), según el "Mapa de la red"
   colgado al lado: C3 = R1, C2 = R4, C5 = R6. Luz verde o roja en cada toma.
3. **Computadoras** (`ComputadoraSala`, `ControlComputadoras`): C1 (pantalla azul) y C4
   (sin señal) son señuelos. Con su cable conectado, C2, C3 y C5 muestran su IP y un
   fragmento de la clave.
4. **Consola** (`ConsolaAcceso`): teclado hexadecimal; pide los fragmentos ordenados por IP
   de menor a mayor: C5 (.11) = 7, C3 (.14) = E, C2 (.27) = 4 → clave `7E4` (asset
   `Cuarto3_Consola`). Luces en cascada y se abre la puerta al Laboratorio.
Susto (`SustoSillas`, versión segura): tocar o pasar junto a S1, S2 o S3. Su luz
ambiental la pone `ClimaCuarto` (noche → luz a medida que se prenden lámparas).

**Cuarto 4 — Laboratorio de Ciencias:** lo arma `Assets/Editor/ConstructorCuarto4.cs`
(8 × 9,5 m, techo de 3,3 m). Laboratorio de química de colegio con muebles y equipos de Sketchfab
(mesada química en L, mesa de acero, vitrina, mesa de disección con un cuerpo tapado, tablero de
fusibles, válvulas, palancas de cuchilla, ducha de emergencia) y Poly Haven (mecheros Bunsen,
taburetes). Los carteles son modernos: señales estilo ISO 7010 (`CartelModerno`) y paneles con
encabezado de color. Empieza a oscuras. La **pantalla de estado** junto a la entrada
(`PanelObjetivo`) lista las 5 tareas y dice qué hacer en la actual. Cinco acertijos en cadena:
1. **Tablero** (`TableroFusibles`, `PortaFusible`, `FusibleLab`): cada portafusibles (A, B, C) dice
   el consumo de su circuito (8, 14 y 5 A) y un aro ámbar marca dónde va el fusible. El panel
   "Cómo reponer los fusibles" explica la regla (valor inmediato superior) con un ejemplo y la
   escala de colores; los repuestos están ordenados y rotulados. Se encajan 10, 16 y 6 A; uno chico
   se quema. Vuelve la luz.
2. **Campana de extracción** (`CampanaExtraccion`, `VentanaCampana`): bajar el vidrio con la mano
   y girar la perilla del extractor a 3 (protocolo junto al gas). Destraba las llaves de gas.
3. **Gas** (`LineaGas`, `ValvulaGas`): dos válvulas que se abren girando el volante dos vueltas
   (`XRKnob`). A mitad de V2 salta el susto (`SustoCamilla`): apagón, la camilla rueda sola y al
   volver la luz el cuerpo tapado ya no está (queda la sábana y un rastro de sangre). Se prenden
   los mecheros.
4. **Ensayo a la llama** (`Mechero`, `MuestraLlama`, `SecuenciaPalancas`, `PalancaCuchilla`): las
   muestras 1, 2 y 3 tiñen la llama de verde, amarillo y rojo; el atril y la pizarra dicen Cu, Na,
   Li. Las palancas (Na, Li, Cu) se bajan en el orden de las muestras: Cu, Na, Li. El visor pide
   "el metal de la muestra 1", después la 2 y la 3. Abre el casillero del docente.
5. **Salida** (`LectorTarjeta`, `TarjetaAcceso`, `ItemData`): en el casillero está la tarjeta del
   docente; se acerca al lector junto a la puerta y se abre. La tarjeta del alumno (sobre la
   mesada) da "acceso denegado". El lector compara assets `ItemData`, no textos.
Diferencia con el GDD (decisión del equipo, 24/09): la ranura del objeto especial del Cuarto 2 se
cambió por la tarjeta del docente, porque si el jugador no agarraba el objeto no podía volver a
buscarlo (las puertas se cierran).

**Menú Escape Room** (`MenuEscapeRoom.cs`): **Construir los 4 cuartos** arma toda la escena
con los constructores de cada cuarto, en orden (1 → 2 → 4 → 3), la guarda y hornea la luz
una sola vez al final (esperar la barra antes de dar Play). **Llevar jugador al Cuarto 1, 2,
3 o 4** pone al jugador en la entrada de ese cuarto para probarlo directo.

**Objetos agarrables** (regla para todos los cuartos): agarre firme, como una mano
que toma bien una herramienta. `XRGrabInteractable` con **punto de agarre fijo**
(`PuntoDeAgarre` en el builder: mango, cabeza de la llave...), movimiento
**Instantaneous** (sin retraso ni temblor) y **far attach = Near** (con el rayo, el
objeto viene a la mano). Rigidbody con detección continua. `ObjetoAgarrable`: quieto
dentro de cajones, al soltarlo sale de la mesa si quedó metido, y si se pierde aparece
sobre el mostrador. `ResaltarAlApuntar`: tono ámbar al apuntarlo (también en cajones,
botones y palancas). Los muebles huecos llevan **relleno invisible** (`Relleno`) para
que nada se cuele adentro.

`LimitesDelJugador` (en el XR Origin) evita que la cabeza atraviese paredes y
mantiene la altura de los ojos entre 0.8 y 1.85 m. `ControlesDePC` agrega, solo
para probar en el PC con el simulador: **CTRL o C** para agacharse/levantarse (un
toque, no se mantiene) y **SHIFT** para correr. Las **herramientas** (llave y
destornillador) llevan `HerramientaEnMano`: en el PC se agarran con **un clic** y
otro clic las suelta, y quedan derechas mirando al frente. Todo lo demás (cajones,
palanca, botones, notas) se agarra como siempre: mantener apretado y soltar.
En el Quest nada de esto se activa: ahí el jugador se agacha de verdad, agarra
manteniendo el grip y se mueve con teletransporte, como pide el GDD.

Cada cuarto mide **4 × 4 m con techo a 2.6 m**. La pared de entrada es la norte y
la salida está en el muro sur. Lo que se agarra o presiona va entre **0.9 y 1.3 m**
de altura.

### Decisiones de diseño ya tomadas

- **Todo en una sola escena.** Los 4 cuartos están conectados por puertas. Así
  el temporizador y el inventario no se pierden al avanzar.
- **Teletransportación únicamente.** El público objetivo tiene poca experiencia
  en VR y se marea con facilidad.
- **Gray box primero.** Se construye todo con cubos grises y recién al final se
  cambian por modelos.
- **Arte de Poly Haven.** Texturas y modelos CC0 en resolución 1k (por el Quest),
  guardados en `Assets/PolyHaven`. Los `.gltf` se importan con el paquete glTFast.
  Los carga el builder del cuarto (`Assets/Editor/Cuarto1Builder.cs`): si un
  modelo falta, el builder usa la versión gray box. Nunca colocar arte a mano en
  un cuarto que tiene builder, porque se borra al reconstruirlo.
- **Modelos de Sketchfab** para lo que Poly Haven no tiene (PC modernas, rack,
  sillas de oficina, proyector, impresora, pizarra del Cuarto 3): en
  `Assets/Sketchfab`, formato GLB con texturas de 1k. Son **CC BY**: hay que dar
  crédito al autor. Cada modelo nuevo se anota en `Assets/Sketchfab/CREDITOS.txt`
  y esos créditos se muestran en la presentación.
- **Iluminación horneada.** El builder marca como estático lo que nunca se mueve
  y hornea la luz al final (hay que esperar la barra antes de dar Play). Lo que
  se mueve o cambia (cajones, agarrables, botones, tornillos, puerta) **no** debe
  ser estático: se ilumina con las sondas de luz. Las luces que cambian durante
  el juego (como la de la cerradura) van en tiempo real, no horneadas.
- **Rendimiento en el Quest.** Presupuesto aproximado: 250 mil triángulos por cuarto y
  pocas luces en tiempo real. `GestorDeCuartos` (objeto "Optimizacion", lo agrega el
  constructor del Cuarto 3 al construir los 4 cuartos) dibuja
  solo el cuarto del jugador, los de al lado y los que se ven por puertas abiertas. Los FBX
  de Poly Haven se importan con Mesh LOD (`MallasLivianas`). Antes de sumar un modelo, mirar
  cuántos triángulos tiene: lo decorativo pesado (plantas, cajas, lámparas de techo) va en
  versión liviana o armado con piezas.
- **Sin menús de ayuda.** Las pistas están en el propio cuarto.
- **Tres sustos que suben de intensidad** (GDD, sección 6): reloj falso, silla y
  mesa de disección. Van en **versión segura**: nunca se activan por chocar con
  el cuerpo (en una feria el jugador podría tropezar de verdad). Se activan al
  tocar con la mano o al teletransportarse cerca.
- Partida de **10 a 15 minutos**, temporizador de 15 minutos.

---

## 5. Arquitectura propuesta

```
Assets/
├── Scripts/
│   ├── Core/
│   │   ├── GameManager.cs        // cuarto actual, estados, victoria/derrota
│   │   ├── TimerController.cs    // cuenta regresiva, evento al llegar a cero
│   │   └── RoomController.cs     // un cuarto: sus acertijos y su puerta
│   ├── Puzzles/
│   │   ├── PuzzleBase.cs         // clase abstracta: OnSolved, PuzzleData
│   │   ├── KeyPuzzle.cs          // cuarto 1
│   │   ├── KeypadPuzzle.cs       // cuarto 2
│   │   ├── FusePuzzle.cs         // cuarto 3
│   │   └── LeverPuzzle.cs        // cuarto 4
│   ├── Interaction/
│   │   ├── Door.cs               // se abre cuando su acertijo está resuelto
│   │   ├── Lever.cs
│   │   └── KeypadButton.cs
│   ├── Inventory/
│   │   ├── Inventory.cs          // 2 o 3 espacios, guarda ItemData
│   │   └── PickableItem.cs       // vincula un GameObject con su ItemData
│   ├── Data/
│   │   ├── PuzzleData.cs         // ScriptableObject
│   │   ├── ItemData.cs           // ScriptableObject
│   │   └── RoomData.cs           // ScriptableObject
│   ├── SaveSystem/
│   │   ├── SaveData.cs           // clase serializable
│   │   └── SaveManager.cs        // Save() / Load() / HasSave()
│   └── UI/
│       ├── MainMenu.cs
│       ├── PauseMenu.cs
│       └── WristHUD.cs           // reloj en la muñeca izquierda
├── ScriptableObjects/
│   ├── Puzzles/                  // un asset por acertijo
│   └── Items/                    // Llave.asset, Fusible.asset, etc.
├── Prefabs/
├── Scenes/
│   └── EscapeRoom.unity          // los 4 cuartos
└── Audio/
```

### Scriptable Objects

```csharp
// PuzzleData: id, nombreCuarto, pista, tipo (enum), solucion (string),
//             mensajeAcierto, mensajeError
// ItemData:   id, nombre, icono, descripcion, tipo (enum)
// RoomData:   numeroCuarto, nombre, puzzleData asociado, tiempoSugerido
```

Cada acertijo del juego es un asset. Agregar un quinto acertijo no debería
requerir tocar `PuzzleBase`.

### Sistema de guardado

`SaveData` serializable a JSON en `Application.persistentDataPath`:

```csharp
int cuartoActual;
List<string> acertijosResueltos;   // ids de PuzzleData
List<string> itemsEnInventario;    // ids de ItemData
float tiempoRestante;
Vector3 posicionJugador;
```

Se guarda automáticamente cada vez que se abre una puerta, y manualmente desde
el menú de pausa.

---

## 6. Flujo de trabajo con Git

- `main` siempre estable
- Ramas por funcionalidad: `feature/interaccion-objetos`, `feature/sistema-guardado`,
  `feature/inventario`, `feature/menu`
- Commits pequeños y descriptivos: `feat: agrega interacción con llave`,
  `fix: corrige carga del inventario`
- `.gitignore` de Unity obligatorio
- Nunca force push sobre `main`
- **Ambos integrantes deben tener commits propios**: el historial se usa como
  evidencia de participación

En Trello nada pasa de Doing a Done directo. El flujo es
**Doing → To Test → Testing → Done**: uno desarrolla y el otro prueba.

---

## 7. Orden de implementación

**Semana 1 (hasta el 19 de septiembre)**
1. Repo con .gitignore de Unity
2. Proyecto configurado con XRI y XR Device Simulator funcionando
3. Gray box de los 4 cuartos con puertas
4. Primera interacción: agarrar un objeto con feedback
5. `Door.cs` reutilizable

**Semana 2 (hasta el 25 de septiembre)**
6. Teletransportación y giro por pasos
7. `PuzzleData` e `ItemData` con sus assets
8. Inventario
9. Los 4 acertijos, uno por uno
10. `GameManager` y `TimerController`
11. Sistema de guardado y carga

**Semana 3 (hasta el 28 de septiembre)**
12. Menús, HUD del reloj, pantallas de victoria y derrota
13. Sonidos y feedback visual
14. Pruebas en Quest, corrección de errores, optimización
15. Build final y ensayo de la presentación

---

## 8. Cómo quiero trabajar con Claude Code

- **Un script a la vez.** Preferible terminar `Door.cs` y probarlo antes de pasar
  al siguiente.
- **Explicar en español simple** qué hace cada script y dónde va en la escena,
  incluyendo qué componentes arrastrar en el Inspector.
- **Código mínimo y comentado.** Nada de patrones complicados ni abstracciones
  que después no podamos defender en la presentación.
- **No ampliar el alcance.** Si algo no está en este documento, preguntarlo antes
  de implementarlo.
- Al terminar una funcionalidad, sugerir el mensaje de commit.

---

## 9. Feedback esperado en el juego

- Acierto: luz verde, sonido corto, la puerta se abre
- Error: luz roja y sonido grave
- Objeto agarrable: contorno resaltado al acercar la mano
- Objeto guardado en el inventario: vibración del control
- Progreso guardado: aviso breve en la muñeca
