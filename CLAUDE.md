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

**Concepto:** el jugador despierta encerrado en un laboratorio abandonado y sin
energía. Debe resolver un acertijo en cada cuarto para abrir la siguiente puerta
y escapar antes de que se acabe el tiempo.

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
| 1 | Recepción | Llave escondida bajo el mostrador, va a la cerradura | Grab + Socket |
| 2 | Oficina | Código de 4 dígitos repartido en reloj, libro y cuadro | Botones (Simple Interactable) |
| 3 | Sala de máquinas | 3 fusibles en el panel eléctrico devuelven la energía | Grab + Socket |
| 4 | Laboratorio | 3 palancas en el orden que indica una pista, más un objeto del cuarto 2 | Palancas + inventario |

El cuarto 1 funciona como tutorial: enseña a agarrar sin decirlo.

### Decisiones de diseño ya tomadas

- **Todo en una sola escena.** Los 4 cuartos están conectados por puertas. Así
  el temporizador y el inventario no se pierden al avanzar.
- **Teletransportación únicamente.** El público objetivo tiene poca experiencia
  en VR y se marea con facilidad.
- **Gray box primero.** Se construye todo con cubos grises y recién al final se
  cambian por modelos.
- **Sin menús de ayuda.** Las pistas están en el propio cuarto.
- **Nada de sustos fuertes.** El público es general.
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
