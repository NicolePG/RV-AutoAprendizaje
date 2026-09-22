using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit.Attachment;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

// Segunda parte del constructor del Cuarto 3: la noche y los acertijos (la primera parte,
// ConstructorCuarto3.cs, arma la sala). Se corre con el mismo menú: Escape Room > Construir Cuarto 3.
//
// LA SALA ES DE NOCHE. Al entrar está a oscuras: solo se ven la luz azulada de la luna por la
// persiana, el cartel verde de salida y el haz de una linterna que quedó prendida sobre las
// cajas de la entrada. Ese haz cae justo sobre el tablero de luces: es la primera pista.
// La linterna se agarra con la mano (como el destornillador del Cuarto 1) y alumbra hacia donde
// se apunta.
//
// LOS 4 ACERTIJOS, en cadena (cada uno destraba el siguiente), cada uno con su pista en la sala:
//  1. LUCES (TableroLuces). 4 interruptores que cambian VARIAS lámparas a la vez. El tablero dice
//     "¿Falla? Ver registro de mantenimiento en el escritorio del profesor": ahí hay una hoja del
//     técnico ("el 2 quedó cruzado, no usarlo"). Solución: 1 + 3 + 4. La hoja se lee con la linterna.
//  2. RED (RedSala, TomaDeRed, FichaRed). De la torre de C2, C3 y C5 sale su cable de red, con la
//     ficha suelta sobre la mesada. Cada ficha se lleva en la mano a su toma del panel de red (al
//     final de la mesada) según el "Mapa de la red", colgado al lado: C3 = R1, C2 = R4, C5 = R6.
//  3. COMPUTADORAS (ComputadoraSala, ControlComputadoras). Se prenden tocando la torre. C1 da
//     pantalla azul y C4 no tiene señal (señuelos del GDD). C2, C3 y C5, con red, muestran su IP y
//     un fragmento de la clave (no su lugar).
//  4. CONSOLA (ConsolaAcceso). Pide los 3 fragmentos ordenados por IP de menor a mayor:
//     C5 (.11) = 7, C3 (.14) = E, C2 (.27) = 4 → 7E4 (el asset Cuarto3_Consola). Luces en
//     cascada y se abre la puerta al Laboratorio.
//
// EL SUSTO (SustoSillas), versión segura del GDD: tocar una de las 3 sillas con la mano o pasar al
// lado de una la arrastra con un chirrido y C1 muestra una cara. Una sola vez.
public static partial class ConstructorCuarto3
{
    const string RUTA_CLIC = "Assets/VRTemplateAssets/Audio/Button_22_click.wav";
    const string RUTA_ACIERTO = "Assets/Samples/XR Interaction Toolkit/3.5.1/Hands Interaction Demo/DemoAssets/Audio/TeleportSelection.wav";

    // Las 5 computadoras (C1 a C5): qué son, su IP, qué fragmento de la clave muestran (su lugar
    // al ordenarlas por IP) y en qué toma del panel va su cable (0 = no tiene cable)
    static readonly ComputadoraSala.Tipo[] TIPO_PC =
    {
        ComputadoraSala.Tipo.PantallaAzul, ComputadoraSala.Tipo.Clave, ComputadoraSala.Tipo.Clave,
        ComputadoraSala.Tipo.SinSenal, ComputadoraSala.Tipo.Clave
    };
    static readonly string[] IP_PC = { "192.168.0.21", "192.168.0.27", "192.168.0.14", "192.168.0.30", "192.168.0.11" };
    static readonly int[] ORDEN_PC = { 0, 3, 2, 0, 1 };   // C5 (.11) 1.º, C3 (.14) 2.º, C2 (.27) 3.º
    static readonly int[] TOMA_PC = { 0, 4, 1, 0, 6 };    // C2 -> R4, C3 -> R1, C5 -> R6
    const int CANTIDAD_TOMAS = 6;

    // Lo que arma la primera parte y usan los acertijos
    static readonly List<TableroLuces.Lampara> lamparasTecho = new List<TableroLuces.Lampara>();
    static readonly List<ComputadoraSala> computadorasSala = new List<ComputadoraSala>();
    static Transform raizCuarto;
    static Light luzTechoA, luzTechoB, luzPantallas;
    static Parpadeo parpadeoTecho;
    static BrilloSegunLuz brilloTecho;
    static ClimaCuarto clima;
    static ReflectionProbe sondaReflejos;
    static Door puertaSalida;
    static GameObject[] sillasSusto;
    static ConsolaAcceso consola;
    static GameObject luzLuna;

    static Material mPantallaApagada, mPantallaArranque, mPantallaSistema, mPantallaAzul, mPantallaCara,
                    mLedApagado, mIndicadorApagado, mLente, mNoche, mLama, mTecla, mPanelRed, mPuerto,
                    mCableAzul, mCableVerde, mCableNaranja;

    static void ArmarAcertijos(Transform raiz)
    {
        raizCuarto = raiz;
        Transform g = Grupo("Acertijos", raiz);

        // Un asset (ScriptableObject) por acertijo, como pide el documento. La clave de la consola
        // sale de su asset: si se cambia ahí, las pantallas muestran la nueva sola.
        PuzzleData datosLuces = Datos("Cuarto3_Luces", "cuarto3_luces", TipoAcertijo.Luces, "1+3+4",
            "El registro de mantenimiento (escritorio del profesor) dice que el interruptor 2 está cruzado.", "LUZ", "");
        PuzzleData datosRed = Datos("Cuarto3_Red", "cuarto3_red", TipoAcertijo.Red, "C3=R1 C2=R4 C5=R6",
            "El 'Mapa de la red', junto al panel, dice a qué toma va el cable de cada computadora.", "RED ACTIVA", "TOMA EQUIVOCADA");
        PuzzleData datosPCs = Datos("Cuarto3_Computadoras", "cuarto3_computadoras", TipoAcertijo.Computadoras, "C2 C3 C5",
            "Solo C2, C3 y C5 funcionan. Con red, cada una muestra su IP y un fragmento de la clave.", "CLAVE COMPLETA", "");
        PuzzleData datosConsola = Datos("Cuarto3_Consola", "cuarto3_consola", TipoAcertijo.Codigo, "7E4",
            "Los 3 fragmentos, ordenados por la IP de su computadora, de menor a mayor.", "ACCESO CONCEDIDO", "ACCESO DENEGADO");
        AudioClip acierto = AssetDatabase.LoadAssetAtPath<AudioClip>(RUTA_ACIERTO);

        ArmarNoche(g);
        ArmarLinterna(g);
        ArmarRegistroMantenimiento(g);
        TableroLuces tablero = ArmarTableroLuces(g, datosLuces, acierto);
        RedSala red = ArmarRed(g, datosRed, acierto);
        ControlComputadoras control = ArmarControlComputadoras(g, datosPCs, datosConsola, acierto);
        consola.datos = datosConsola;
        consola.sonidoAcierto = acierto;
        LucesEnCascada cascada = ArmarCascada(g);
        ArmarSusto();
        PanelObjetivo objetivos = ArmarCartelObjetivos(g);

        // La cadena: cada acertijo, al resolverse, destraba lo que sigue y avanza el cartel.
        // Son eventos guardados en la escena: se ven y se pueden cambiar en el Inspector.
        UnityEventTools.AddVoidPersistentListener(tablero.alResolverse, new UnityAction(red.DarEnergia));
        UnityEventTools.AddVoidPersistentListener(tablero.alResolverse, new UnityAction(control.DarEnergia));
        UnityEventTools.AddVoidPersistentListener(tablero.alResolverse, new UnityAction(consola.DarEnergia));
        UnityEventTools.AddIntPersistentListener(tablero.alResolverse, objetivos.MostrarPaso, 1);
        UnityEventTools.AddIntPersistentListener(red.alResolverse, objetivos.MostrarPaso, 2);
        UnityEventTools.AddIntPersistentListener(control.alResolverse, objetivos.MostrarPaso, 3);
        UnityEventTools.AddVoidPersistentListener(consola.alResolverse, new UnityAction(cascada.Encender));
        UnityEventTools.AddIntPersistentListener(consola.alResolverse, objetivos.MostrarPaso, 4);
        if (puertaSalida != null)
            UnityEventTools.AddVoidPersistentListener(cascada.alTerminar, new UnityAction(puertaSalida.Abrir));

        foreach (Object o in new Object[] { tablero, red, control, consola, cascada, objetivos }) EditorUtility.SetDirty(o);
    }

    // Asset con los datos de un acertijo. Si ya existe no se toca: lo que el equipo cambie en el
    // Inspector (por ejemplo, la clave) se respeta al reconstruir.
    static PuzzleData Datos(string archivo, string id, TipoAcertijo tipo, string solucion, string pista, string acierto, string error)
    {
        const string carpeta = "Assets/ScriptableObjects/Puzzles";
        if (!AssetDatabase.IsValidFolder("Assets/ScriptableObjects")) AssetDatabase.CreateFolder("Assets", "ScriptableObjects");
        if (!AssetDatabase.IsValidFolder(carpeta)) AssetDatabase.CreateFolder("Assets/ScriptableObjects", "Puzzles");

        string ruta = carpeta + "/" + archivo + ".asset";
        var datos = AssetDatabase.LoadAssetAtPath<PuzzleData>(ruta);
        if (datos != null) return datos;

        datos = ScriptableObject.CreateInstance<PuzzleData>();
        datos.id = id;
        datos.nombreCuarto = "Sala de Computación";
        datos.tipo = tipo;
        datos.solucion = solucion;
        datos.pista = pista;
        datos.mensajeAcierto = acierto;
        datos.mensajeError = error;
        AssetDatabase.CreateAsset(datos, ruta);
        return datos;
    }

    // Objeto que se agarra con la mano como las herramientas del Cuarto 1: agarre firme con un
    // punto fijo (Punto_Agarre), sigue a la mano sin retraso, con el rayo viene a la mano, en el PC
    // se toma y se suelta con un clic (HerramientaEnMano) y, al soltarlo, cae con física. Si se
    // pierde (se sale de la sala o queda metido en un mueble), aparece en "rescate".
    static XRGrabInteractable HacerAgarrable(GameObject go, Transform punto, Vector3 rescate, bool seguirMirada)
    {
        if (go.GetComponent<Collider>() == null) go.AddComponent<BoxCollider>();
        var cuerpo = go.AddComponent<Rigidbody>();
        cuerpo.mass = 0.3f;
        cuerpo.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        cuerpo.interpolation = RigidbodyInterpolation.Interpolate;
        cuerpo.isKinematic = true;   // quieto donde está hasta que lo agarren (ObjetoAgarrable le da física al soltarlo)

        var agarre = go.AddComponent<XRGrabInteractable>();
        agarre.useDynamicAttach = false;
        agarre.movementType = XRBaseInteractable.MovementType.Instantaneous;
        agarre.farAttachMode = InteractableFarAttachMode.Near;
        agarre.attachTransform = punto;
        agarre.attachEaseInTime = 0.2f;   // al encajar en una toma viaja suave hasta su lugar

        var objeto = go.AddComponent<ObjetoAgarrable>();
        objeto.puntoDeRescate = raizCuarto.TransformPoint(rescate);
        objeto.zonaPermitida = new Bounds(raizCuarto.TransformPoint(new Vector3(ANCHO / 2f, ALTO / 2f, FONDO / 2f)),
                                          new Vector3(ANCHO, ALTO, FONDO));
        go.AddComponent<ResaltarAlApuntar>();
        go.AddComponent<HerramientaEnMano>().seguirMirada = seguirMirada;
        return agarre;
    }

    // Punto de agarre: dónde lo toma la mano, con su adelante (azul, +Z) y su arriba (verde, +Y)
    static Transform PuntoDeAgarre(Transform objeto, Vector3 pos, Vector3 adelante, Vector3 arriba)
    {
        Transform punto = Grupo("Punto_Agarre", objeto);
        punto.localPosition = pos;
        punto.localRotation = Quaternion.LookRotation(adelante, arriba);
        return punto;
    }

    // ------------------------------------------------------------------ la noche

    // Dos ventanas en la pared derecha con la persiana baja y el cielo oscuro detrás, y una luz
    // azulada de luna que entra por la del fondo. Se apaga cuando vuelve la luz de la sala.
    static void ArmarNoche(Transform p)
    {
        Transform g = Grupo("Noche", p);
        Ventana(g, "Ventana_1", 1.3f);
        Ventana(g, "Ventana_2", 5.7f);

        Transform luna = Grupo("Luz_Luna", g);
        luna.localPosition = new Vector3(ANCHO - 0.25f, 2.35f, 5.7f);
        luna.localRotation = Quaternion.LookRotation(new Vector3(-3f, -2.3f, -0.8f));
        Light luz = luna.gameObject.AddComponent<Light>();
        luz.type = LightType.Spot;
        luz.color = new Color(0.45f, 0.58f, 1f);
        luz.intensity = 2.2f;
        luz.range = 8f;
        luz.spotAngle = 75f;
        luz.innerSpotAngle = 30f;
        luz.shadows = LightShadows.None;
        luz.lightmapBakeType = LightmapBakeType.Realtime;
        luzLuna = luna.gameObject;
    }

    // Ventana en la pared derecha (x = ANCHO), mirando al cuarto: cielo nocturno, marco, persiana
    // de lamas inclinadas y alféizar. Poly Haven no tiene ventanas: se arma con piezas.
    static void Ventana(Transform p, string nombre, float z)
    {
        Transform v = Grupo(nombre, p);
        v.localPosition = new Vector3(ANCHO, 1.85f, z);
        v.localRotation = Quaternion.LookRotation(Vector3.left);   // su +Z mira al cuarto

        Cubo("Cielo", v, new Vector3(0f, 0f, 0.003f), new Vector3(1f, 1.1f, 0.004f), mNoche);
        Cubo("Marco_Arriba", v, new Vector3(0f, 0.575f, 0.02f), new Vector3(1.1f, 0.05f, 0.04f), mMarco);
        Cubo("Marco_Abajo", v, new Vector3(0f, -0.575f, 0.02f), new Vector3(1.1f, 0.05f, 0.04f), mMarco);
        Cubo("Marco_Izq", v, new Vector3(-0.525f, 0f, 0.02f), new Vector3(0.05f, 1.2f, 0.04f), mMarco);
        Cubo("Marco_Der", v, new Vector3(0.525f, 0f, 0.02f), new Vector3(0.05f, 1.2f, 0.04f), mMarco);
        Cubo("Alfeizar", v, new Vector3(0f, -0.61f, 0.06f), new Vector3(1.16f, 0.03f, 0.12f), mMelamina);
        for (int i = 0; i < 14; i++)
        {
            GameObject lama = Cubo("Lama", v, new Vector3(0f, -0.5f + i * 0.077f, 0.03f), new Vector3(0.99f, 0.012f, 0.06f), mLama);
            lama.transform.localRotation = Quaternion.Euler(55f, 0f, 0f);
        }
    }

    // La linterna que alguien dejó prendida encima de las cajas de la entrada: su haz cae sobre el
    // tablero de luces. Se agarra con la mano, del asa, y alumbra hacia donde se apunta (en el PC,
    // hacia donde se mira). Al soltarla cae con física.
    static void ArmarLinterna(Transform p)
    {
        Vector3 pos = new Vector3(6.42f, 0.684f, 1.93f);   // encima de las cajas apiladas
        const float giro = -121f;                          // apuntando al tablero de luces
        Vector3 lente = new Vector3(0f, 0.106f, 0.16f);    // dónde está la lente en el modelo
        GameObject linterna = Modelo("vintage_flashlight", p, pos, giro, 0f, Apoyo.Piso, true);
        if (linterna == null)
        {
            Transform t = Grupo("Linterna", p);
            t.localPosition = pos;
            t.localRotation = Quaternion.Euler(0f, giro, 0f);
            linterna = t.gameObject;
            GameObject cuerpo = Cilindro("Cuerpo", t, new Vector3(0f, 0.04f, 0f), new Vector3(0.05f, 0.13f, 0.05f), mPlasticoNegro);
            cuerpo.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            var caja = t.gameObject.AddComponent<BoxCollider>();
            caja.center = new Vector3(0f, 0.04f, 0f);
            caja.size = new Vector3(0.05f, 0.05f, 0.26f);
            lente = new Vector3(0f, 0.04f, 0.13f);
        }
        linterna.name = "Linterna";

        // La lente brilla
        foreach (Renderer r in linterna.GetComponentsInChildren<Renderer>())
        {
            Material[] mats = r.sharedMaterials;
            for (int i = 0; i < mats.Length; i++) if (mats[i] == mVidrio) mats[i] = mLente;
            r.sharedMaterials = mats;
        }

        // Se toma del asa, a la altura de la lente, con la lente hacia adelante (+Z del modelo):
        // la mano la sostiene como una linterna de verdad y el haz sale hacia donde apunta
        Transform punto = PuntoDeAgarre(linterna.transform, new Vector3(0f, lente.y - 0.006f, -0.05f), Vector3.forward, Vector3.up);
        HacerAgarrable(linterna, punto, new Vector3(5.9f, 1f, 3.4f), true);

        // El haz sale de la lente, apenas hacia arriba (apoyada en las cajas, así llega al tablero)
        Transform haz = Grupo("Haz", linterna.transform);
        haz.localPosition = lente;
        haz.localRotation = Quaternion.Euler(-7f, 0f, 0f);
        Light luz = haz.gameObject.AddComponent<Light>();
        luz.type = LightType.Spot;
        luz.color = new Color(1f, 0.93f, 0.8f);
        luz.intensity = 3.5f;
        luz.range = 8f;
        luz.spotAngle = 44f;
        luz.innerSpotAngle = 18f;
        luz.shadows = LightShadows.None;
        luz.lightmapBakeType = LightmapBakeType.Realtime;
    }

    // La pista del acertijo 1: una hoja del técnico sobre el escritorio del profesor. La tinta es
    // oscura: a oscuras casi no se ve y con la linterna se lee bien (el papel se ilumina).
    static void ArmarRegistroMantenimiento(Transform p)
    {
        Transform g = Grupo("Registro_Mantenimiento", p);
        g.localPosition = new Vector3(5.62f, 0.7895f, 3.35f);   // en el escritorio, del lado de los alumnos
        // Hoja A4 acostada, con el texto hacia el fondo de la sala (se lee parado frente al escritorio)
        Cubo("Hoja", g, Vector3.zero, new Vector3(0.297f, 0.001f, 0.21f), mPapel);
        Texto("Texto", g, new Vector3(0f, 0.0008f, 0f), Vector3.up,
              "<b>REGISTRO DE MANTENIMIENTO</b>\nSala de Computación\n\n" +
              "12/09 — Tablero de luces:\nel interruptor 2 quedó cruzado\ncon el circuito del proyector.\n\n" +
              "<b>NO USAR EL 2.</b> Con los otros\ntres se enciende toda la sala.\n\n— Técnico: R. Quispe",
              new Vector2(0.19f, 0.27f), new Color(0.12f, 0.12f, 0.15f), Vector3.right);
    }

    // ------------------------------------------------------------------ acertijo 1: luces

    static TableroLuces ArmarTableroLuces(Transform p, PuzzleData datos, AudioClip acierto)
    {
        Transform g = Grupo("Tablero_Luces", p);
        g.localPosition = new Vector3(3.2f, 1.2f, 0f);   // en la pared de la entrada, junto a la puerta
        Cubo("Placa", g, new Vector3(0f, 0f, 0.015f), new Vector3(0.4f, 0.5f, 0.03f), mPlaca, true);
        Texto("Titulo", g, new Vector3(0f, 0.215f, 0.0305f), Vector3.forward, "ILUMINACIÓN", new Vector2(0.34f, 0.04f), Color.white);

        // Mapa de las 6 lámparas, como un plano con el fondo de la sala arriba: una lucecita por
        // lámpara. Quien mira el tablero está de espaldas a la sala, así que el lado de las
        // computadoras (izquierda al entrar) queda a su derecha, como en un plano puesto al revés.
        Color gris = new Color(0.65f, 0.67f, 0.7f);
        Cubo("Mapa", g, new Vector3(0f, 0.075f, 0.031f), new Vector3(0.2f, 0.2f, 0.002f), mPuerto);
        Texto("Mapa_Fondo", g, new Vector3(0f, 0.165f, 0.0325f), Vector3.forward, "FONDO", new Vector2(0.12f, 0.018f), gris);
        Texto("Mapa_Entrada", g, new Vector3(0f, -0.015f, 0.0325f), Vector3.forward, "ENTRADA", new Vector2(0.12f, 0.018f), gris);
        Texto("Mapa_PC", g, new Vector3(-0.14f, 0.075f, 0.0325f), Vector3.forward, "PC", new Vector2(0.05f, 0.025f), gris);

        var tablero = g.gameObject.AddComponent<TableroLuces>();
        tablero.lamparas = lamparasTecho.ToArray();
        for (int i = 0; i < tablero.lamparas.Length; i++)
        {
            TableroLuces.Lampara lampara = tablero.lamparas[i];
            float x = lampara.lado == 0 ? -0.06f : 0.06f;   // lado de las computadoras a la derecha de quien mira
            float y = 0.015f + (i % 3) * 0.06f;             // la de adelante abajo, la del fondo arriba
            lampara.indicador = Cubo("Luz_Lampara_" + (i + 1), g, new Vector3(x, y, 0.0325f),
                                     new Vector3(0.045f, 0.03f, 0.003f), mIndicadorApagado).GetComponent<Renderer>();
        }

        // Interruptores 1 a 4, de izquierda a derecha para quien mira el tablero
        tablero.interruptores = new Interruptor[4];
        for (int i = 0; i < 4; i++)
            tablero.interruptores[i] = CrearInterruptor(g, i + 1, new Vector3(0.12f - i * 0.08f, -0.13f, 0.03f));

        // La pista: dónde buscar si no enciende
        Texto("Aviso", g, new Vector3(0f, -0.21f, 0.0305f), Vector3.forward,
              "¿FALLA? VER EL REGISTRO DE MANTENIMIENTO\nEN EL ESCRITORIO DEL PROFESOR", new Vector2(0.37f, 0.045f),
              new Color(1f, 0.72f, 0.25f));

        // Qué lámparas cambia cada interruptor (0-2 izquierda, 3-5 derecha, de adelante al fondo).
        // Solución única: 1 + 3 + 4. El 2 es el "cruzado" del registro de mantenimiento.
        tablero.circuitos = new[] { Circuito(0, 1, 4), Circuito(0, 2, 3), Circuito(1, 3, 4), Circuito(1, 2, 4, 5) };

        tablero.lucesPorLado = new[] { luzTechoA, luzTechoB };
        tablero.intensidadMaxima = 2.2f;
        tablero.tuboPrendido = mTuboEncendido;
        tablero.tuboApagado = mTuboApagado;
        tablero.indicadorPrendido = mLedVerde;
        tablero.indicadorApagado = mIndicadorApagado;
        tablero.clima = clima;
        tablero.sondaReflejos = sondaReflejos;
        tablero.activarAlResolver = new Behaviour[] { parpadeoTecho, brilloTecho };
        tablero.apagarAlResolver = new[] { luzLuna };
        tablero.datos = datos;
        tablero.sonidoAcierto = acierto;
        return tablero;
    }

    static TableroLuces.Circuito Circuito(params int[] lamparas) => new TableroLuces.Circuito { lamparas = lamparas };

    // Interruptor de tecla basculante en su placa blanca, con su número y una lucecita
    static Interruptor CrearInterruptor(Transform p, int numero, Vector3 pos)
    {
        Transform t = Grupo("Interruptor_" + numero, p);
        t.localPosition = pos;
        Cubo("Placa", t, new Vector3(0f, 0f, 0.003f), new Vector3(0.056f, 0.086f, 0.006f), mCanaleta);
        Transform tecla = Grupo("Tecla", t);
        tecla.localPosition = new Vector3(0f, 0f, 0.012f);
        Cubo("Basculante", tecla, Vector3.zero, new Vector3(0.03f, 0.05f, 0.012f), mPlasticoPC);
        GameObject luz = Cubo("Luz", t, new Vector3(0f, -0.034f, 0.0065f), new Vector3(0.008f, 0.005f, 0.002f), mIndicadorApagado);
        Texto("Numero", t, new Vector3(0f, 0.058f, 0.0008f), Vector3.forward, numero.ToString(), new Vector2(0.03f, 0.022f), Color.white);

        var col = t.gameObject.AddComponent<BoxCollider>();
        col.center = new Vector3(0f, 0f, 0.012f);
        col.size = new Vector3(0.06f, 0.09f, 0.03f);
        t.gameObject.AddComponent<XRSimpleInteractable>();

        var interruptor = t.gameObject.AddComponent<Interruptor>();
        interruptor.tecla = tecla;
        interruptor.luz = luz.GetComponent<Renderer>();
        interruptor.luzPrendida = mLedVerde;
        interruptor.luzApagada = mIndicadorApagado;
        interruptor.sonido = AssetDatabase.LoadAssetAtPath<AudioClip>(RUTA_CLIC);
        t.gameObject.AddComponent<ResaltarAlApuntar>();
        return interruptor;
    }

    // ------------------------------------------------------------------ acertijo 2: red

    // Panel de red en la pared izquierda, justo después de la mesada, con 6 tomas (R1 a R6) y el
    // "Mapa de la red" colgado al lado. De la torre de cada computadora que funciona sale su cable.
    static RedSala ArmarRed(Transform p, PuzzleData datos, AudioClip acierto)
    {
        Transform g = Grupo("Panel_Red", p);
        // Contra la pared izquierda (despegado 1.7 cm por la franja), mirando al cuarto (+X)
        g.localPosition = new Vector3(0.017f, 1.1f, 5.95f);
        g.localRotation = Quaternion.LookRotation(Vector3.right);
        Cubo("Caja", g, new Vector3(0f, 0f, 0.02f), new Vector3(0.66f, 0.26f, 0.04f), mPanelRed, true);
        Texto("Titulo", g, new Vector3(0f, 0.1f, 0.0405f), Vector3.forward, "PANEL DE RED  ·  SALA 3", new Vector2(0.5f, 0.03f), Color.white);

        var red = g.gameObject.AddComponent<RedSala>();
        red.tomas = new TomaDeRed[CANTIDAD_TOMAS];
        Color gris = new Color(0.72f, 0.74f, 0.77f);
        for (int i = 0; i < CANTIDAD_TOMAS; i++)
        {
            // R1 a la izquierda de quien mira el panel. Separadas 10 cm: las zonas de encaje no se pisan.
            Transform toma = Grupo("Toma_R" + (i + 1), g);
            toma.localPosition = new Vector3(0.25f - i * 0.1f, -0.01f, 0.04f);
            Cubo("Tapa", toma, new Vector3(0f, 0f, 0.002f), new Vector3(0.05f, 0.065f, 0.004f), mCanaleta);
            Cubo("Boca", toma, new Vector3(0f, 0f, 0.0045f), new Vector3(0.024f, 0.02f, 0.002f), mPuerto);
            GameObject luz = Cubo("Luz", toma, new Vector3(0f, 0.045f, 0.002f), new Vector3(0.014f, 0.007f, 0.004f), mLedApagado);
            Texto("Nombre", toma, new Vector3(0f, -0.047f, 0.0025f), Vector3.forward, "R" + (i + 1), new Vector2(0.05f, 0.02f), gris);

            // Donde queda la ficha enchufada: su punta entra en la boca (apunta hacia la pared)
            Transform encaje = Grupo("Encaje", toma);
            encaje.localPosition = new Vector3(0f, 0f, 0.03f);
            encaje.localRotation = Quaternion.LookRotation(Vector3.back);

            var zona = toma.gameObject.AddComponent<SphereCollider>();
            zona.isTrigger = true;
            zona.center = new Vector3(0f, 0f, 0.04f);
            zona.radius = 0.05f;
            var socket = toma.gameObject.AddComponent<XRSocketInteractor>();
            socket.attachTransform = encaje;

            var tomaDeRed = toma.gameObject.AddComponent<TomaDeRed>();
            tomaDeRed.luz = luz.GetComponent<Renderer>();
            red.tomas[i] = tomaDeRed;
        }

        // El mapa de la red, colgado al lado del panel: IP y toma de cada computadora
        Afiche(p, "Mapa_Red", new Vector3(0f, 1.47f, 6.75f), Vector3.right,
               "<b>MAPA DE LA RED</b>\n\n<align=left>" +
               "<pos=6%>EQUIPO<pos=34%>IP<pos=80%>TOMA\n" +
               "<pos=6%>C1<pos=34%>192.168.0.21<pos=80%>—\n" +
               "<pos=6%>C2<pos=34%>192.168.0.27<pos=80%>R4\n" +
               "<pos=6%>C3<pos=34%>192.168.0.14<pos=80%>R1\n" +
               "<pos=6%>C4<pos=34%>192.168.0.30<pos=80%>—\n" +
               "<pos=6%>C5<pos=34%>192.168.0.11<pos=80%>R6</align>\n\n" +
               "<size=80%>C1 y C4: dados de baja</size>",
               0.46f, 0.52f);

        // Un cable por cada computadora que funciona, saliendo de su torre
        var fichas = new List<FichaRed>();
        Material[] colores = { mCableAzul, mCableVerde, mCableNaranja };
        for (int i = 0; i < computadorasSala.Count && i < TOMA_PC.Length; i++)
        {
            if (TOMA_PC[i] == 0) continue;
            FichaRed ficha = CableDeRed(p, computadorasSala[i], colores[fichas.Count % colores.Length], fichas.Count);
            if (ficha == null) continue;
            ficha.tomaCorrecta = red.tomas[TOMA_PC[i] - 1];
            fichas.Add(ficha);
        }
        red.fichas = fichas.ToArray();
        red.luzVerde = mLedVerde;
        red.luzRoja = mLedRojo;
        red.luzApagada = mLedApagado;
        red.datos = datos;
        red.sonidoAcierto = acierto;
        return red;
    }

    // El cable de red de una computadora. Sale de atrás de la torre, por el costado de afuera del
    // puesto, y termina en una ficha que queda sobre la mesada, cerca del borde de adelante. La
    // ficha se agarra con la mano (la punta hacia adelante) y se suelta cerca de su toma.
    // El cable lo dibuja CableVisual: cruza la mesada, va apoyado a lo largo del borde hasta
    // donde esté la ficha y de ahí cuelga hasta la mano. "numero" separa los cables (0, 1, 2) para
    // que vayan uno al lado del otro por el borde y no uno encima del otro.
    static FichaRed CableDeRed(Transform p, ComputadoraSala pc, Material color, int numero)
    {
        Transform g = pc.transform;
        Transform torre = BuscarHijo(g, "Torre") ?? BuscarHijo(g, "Gabinete");
        if (torre == null || !LimitesLocales(torre.gameObject, g, out Bounds b)) return null;

        // El costado de la torre que da hacia afuera del puesto (el otro da al monitor)
        float lado = b.center.z < 0f ? -1f : 1f;
        float zCostado = lado < 0f ? b.min.z : b.max.z;
        float zFicha = zCostado + lado * 0.045f;
        float xLinea = 0.69f - numero * 0.011f;   // por dónde corre el cable a lo largo de la mesada

        // De dónde sale el cable: abajo y atrás, por ese costado de la torre
        Transform salida = Grupo("Salida_Red", g);
        salida.localPosition = new Vector3(b.min.x + 0.03f, ALTO_MESADA + 0.02f, zCostado + lado * 0.012f);

        // El borde por donde va apoyado: desde el frente de su puesto hasta la punta de la mesada,
        // que es donde está el panel de red
        Transform cables = p.Find("Cables") ?? Grupo("Cables", p);
        Transform desde = Grupo("Cable_" + pc.nombre + "_Borde", cables);
        desde.position = g.TransformPoint(new Vector3(xLinea, ALTO_MESADA + 0.004f, zFicha));
        Transform hasta = Grupo("Cable_" + pc.nombre + "_Punta", cables);
        hasta.localPosition = new Vector3(xLinea, ALTO_MESADA + 0.004f, FIN_MESADA - 0.02f);

        // La ficha, acostada sobre la mesada, con la punta hacia el cuarto y la cola justo donde
        // empieza el borde: así el cable arranca derecho
        Transform f = Grupo("Ficha_" + pc.nombre, p);
        f.position = g.TransformPoint(new Vector3(xLinea + 0.021f, ALTO_MESADA + 0.009f, zFicha));
        f.rotation = g.rotation * Quaternion.Euler(0f, 90f, 0f);
        Cubo("Punta", f, new Vector3(0f, 0f, 0.02f), new Vector3(0.016f, 0.013f, 0.02f), mCanaleta);
        Cubo("Capuchon", f, new Vector3(0f, 0f, -0.005f), new Vector3(0.02f, 0.017f, 0.03f), color);
        Transform fin = Grupo("Salida_Cable", f);
        fin.localPosition = new Vector3(0f, 0f, -0.021f);
        Cubo("Etiqueta", f, new Vector3(0f, 0.0095f, -0.006f), new Vector3(0.03f, 0.002f, 0.022f), mPapel);
        Texto("Etiqueta_Texto", f, new Vector3(0f, 0.011f, -0.006f), Vector3.up, pc.nombre, new Vector2(0.026f, 0.018f),
              new Color(0.1f, 0.1f, 0.12f), Vector3.forward);

        var col = f.gameObject.AddComponent<BoxCollider>();
        col.center = new Vector3(0f, 0.011f, 0f);      // apoyada justo sobre la mesada
        col.size = new Vector3(0.05f, 0.04f, 0.08f);   // más grande que la ficha: fácil de agarrar
        Transform punto = PuntoDeAgarre(f, new Vector3(0f, 0f, -0.005f), Vector3.forward, Vector3.up);
        HacerAgarrable(f.gameObject, punto, raizCuarto.InverseTransformPoint(f.position) + Vector3.up * 0.1f, false);

        var ficha = f.gameObject.AddComponent<FichaRed>();
        ficha.computadora = pc;

        // El cable dibujado de la torre a la ficha
        var linea = f.gameObject.AddComponent<LineRenderer>();
        linea.sharedMaterial = color;
        linea.startWidth = linea.endWidth = 0.008f;
        linea.numCapVertices = 2;
        linea.numCornerVertices = 2;
        linea.generateLightingData = true;
        linea.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        var cable = f.gameObject.AddComponent<CableVisual>();
        cable.inicio = salida;
        cable.fin = fin;
        cable.bordeDesde = desde;
        cable.bordeHasta = hasta;
        cable.alturaPiso = raizCuarto.position.y;
        return ficha;
    }

    // ------------------------------------------------------------------ acertijo 3: computadoras

    // Deja una computadora lista para el acertijo: la torre se puede tocar, la pantalla arranca
    // apagada y lleva un texto encima, y (C1) la cara del susto. Lo llama ArmarComputadora.
    static void ConfigurarComputadora(Transform g, int numero)
    {
        Transform pantallaT = BuscarHijo(g, "Pantalla");
        Transform torre = BuscarHijo(g, "Torre") ?? BuscarHijo(g, "Gabinete");
        Transform led = BuscarHijo(g, "Led") ?? BuscarHijo(g, "Led_Encendido");
        Renderer pantalla = pantallaT != null ? pantallaT.GetComponent<Renderer>() : null;
        if (pantalla == null || torre == null)
        {
            Debug.LogWarning("Cuarto 3: a la computadora C" + numero + " le falta la pantalla o la torre.");
            return;
        }

        pantalla.sharedMaterial = mPantallaApagada;
        Renderer luz = led != null ? led.GetComponent<Renderer>() : null;
        if (luz != null) luz.sharedMaterial = mLedApagado;

        if (torre.GetComponent<Collider>() == null) torre.gameObject.AddComponent<BoxCollider>();   // toma la forma de la torre
        var interactable = torre.gameObject.AddComponent<XRSimpleInteractable>();
        torre.gameObject.AddComponent<ResaltarAlApuntar>();

        Transform marco = MarcoDePantalla(pantalla, g, out Vector2 tam);
        TextMeshPro texto = Texto("Pantalla_Texto", marco, new Vector3(0f, 0f, -0.0005f), Vector3.back, "", tam * 0.86f, Color.white);

        var pc = g.gameObject.AddComponent<ComputadoraSala>();
        pc.tipo = TIPO_PC[numero - 1];
        pc.nombre = "C" + numero;
        pc.ip = IP_PC[numero - 1];
        pc.orden = ORDEN_PC[numero - 1];
        pc.torre = interactable;
        pc.pantalla = pantalla;
        pc.texto = texto;
        pc.luzEncendido = luz;
        pc.pantallaApagada = mPantallaApagada;
        pc.pantallaArranque = mPantallaArranque;
        pc.pantallaSistema = mPantallaSistema;
        pc.pantallaAzul = mPantallaAzul;
        pc.pantallaCara = mPantallaCara;
        pc.luzPrendida = mLedVerde;
        pc.luzApagada = mLedApagado;
        if (numero == 1) pc.cara = ArmarCara(marco, tam);
        computadorasSala.Add(pc);
    }

    // Un objeto vacío en el centro de la pantalla, 2 mm por delante y paralelo a ella, con su -Z
    // mirando hacia afuera (así el texto que se pone adentro se lee de frente y no se mete detrás
    // de la pantalla). El ancho se mide en la dirección horizontal en la que más se extiende la
    // pantalla: así sirve aunque el monitor del modelo esté girado. Devuelve el ancho y el alto.
    static Transform MarcoDePantalla(Renderer pantalla, Transform computadora, out Vector2 tam)
    {
        Vector3 haciaElCuarto = computadora.TransformDirection(Vector3.right);
        Mesh malla = pantalla.TryGetComponent(out MeshFilter filtro) ? filtro.sharedMesh : null;
        var puntos = new List<Vector3>();
        if (malla != null)
            foreach (Vector3 v in malla.vertices) puntos.Add(pantalla.transform.TransformPoint(v));

        Vector3 ancho = computadora.TransformDirection(Vector3.forward);   // por si no hay malla
        float mayor = 0f;
        for (int i = 0; i < puntos.Count; i++)
            for (int j = i + 1; j < puntos.Count; j++)
            {
                Vector3 d = puntos[j] - puntos[i];
                d.y = 0f;
                if (d.sqrMagnitude > mayor) { mayor = d.sqrMagnitude; ancho = d; }
            }
        ancho.y = 0f;
        ancho.Normalize();

        // La pantalla está parada: su frente es horizontal y perpendicular al ancho, hacia el cuarto
        Vector3 normal = Vector3.Cross(Vector3.up, ancho).normalized;
        if (Vector3.Dot(normal, haciaElCuarto) < 0f) normal = -normal;

        Vector3 centro = pantalla.bounds.center;
        Quaternion giro = Quaternion.LookRotation(-normal, Vector3.up);
        Vector3 derecha = giro * Vector3.right;
        float minX = float.MaxValue, maxX = float.MinValue, minY = float.MaxValue, maxY = float.MinValue;
        foreach (Vector3 punto in puntos)
        {
            Vector3 d = punto - centro;
            float x = Vector3.Dot(d, derecha), y = Vector3.Dot(d, Vector3.up);
            minX = Mathf.Min(minX, x); maxX = Mathf.Max(maxX, x);
            minY = Mathf.Min(minY, y); maxY = Mathf.Max(maxY, y);
        }
        tam = puntos.Count > 0 ? new Vector2(Mathf.Max(0.05f, maxX - minX), Mathf.Max(0.05f, maxY - minY)) : new Vector2(0.3f, 0.2f);

        Transform marco = new GameObject("Marco_Pantalla").transform;
        marco.SetPositionAndRotation(centro + normal * 0.002f, giro);
        marco.SetParent(computadora, true);
        return marco;
    }

    // La cara del susto: dos ojos y una boca negros sobre la pantalla blanca (empieza escondida)
    static GameObject ArmarCara(Transform marco, Vector2 tam)
    {
        Transform cara = Grupo("Cara", marco);
        Cubo("Ojo_Izq", cara, new Vector3(-tam.x * 0.17f, tam.y * 0.12f, -0.001f), new Vector3(tam.x * 0.1f, tam.y * 0.24f, 0.001f), mHueco);
        Cubo("Ojo_Der", cara, new Vector3(tam.x * 0.17f, tam.y * 0.12f, -0.001f), new Vector3(tam.x * 0.1f, tam.y * 0.24f, 0.001f), mHueco);
        Cubo("Boca", cara, new Vector3(0f, -tam.y * 0.22f, -0.001f), new Vector3(tam.x * 0.3f, tam.y * 0.14f, 0.001f), mHueco);
        cara.gameObject.SetActive(false);
        return cara.gameObject;
    }

    static ControlComputadoras ArmarControlComputadoras(Transform p, PuzzleData datos, PuzzleData clave, AudioClip acierto)
    {
        var control = Grupo("Computadoras", p).gameObject.AddComponent<ControlComputadoras>();
        control.computadoras = computadorasSala.ToArray();
        control.resplandor = luzPantallas;
        control.datos = datos;
        control.sonidoAcierto = acierto;
        foreach (ComputadoraSala pc in computadorasSala)
        {
            if (pc.tipo == ComputadoraSala.Tipo.Clave) pc.clave = clave;
            EditorUtility.SetDirty(pc);
        }
        return control;
    }

    // ------------------------------------------------------------------ acertijo 4: consola

    // Consola de acceso al Laboratorio, junto a la puerta de salida: columna de acero con un
    // cabezal inclinado hacia el jugador, pantalla, luz de estado y un teclado hexadecimal
    // (0-9, A-F) con una tecla BORRAR. Cada tecla llama a ConsolaAcceso.Ingresar con su carácter.
    static void ArmarConsola(Transform p)
    {
        Transform g = Grupo("Consola", p);
        g.localPosition = new Vector3(2.75f, 0f, FONDO - 0.3f);
        Cubo("Base", g, new Vector3(0f, 0.01f, 0f), new Vector3(0.44f, 0.02f, 0.34f), mAceroOscuro, true);
        Cubo("Columna", g, new Vector3(0f, 0.51f, 0.03f), new Vector3(0.26f, 0.98f, 0.2f), mAcero, true);

        // Cabezal inclinado 35° hacia el jugador (su frente es -Z, hacia el cuarto)
        Transform cabezal = Grupo("Cabezal", g);
        cabezal.localPosition = new Vector3(0f, 1.08f, 0f);
        cabezal.localRotation = Quaternion.Euler(35f, 0f, 0f);
        Cubo("Carcasa", cabezal, Vector3.zero, new Vector3(0.56f, 0.64f, 0.07f), mAceroOscuro, true);
        GameObject fondo = Cubo("Pantalla", cabezal, new Vector3(0f, 0.2f, -0.037f), new Vector3(0.46f, 0.16f, 0.004f), mPantallaApagada);
        TextMeshPro texto = Texto("Pantalla_Texto", cabezal, new Vector3(0f, 0.2f, -0.0395f), Vector3.back, "",
                                  new Vector2(0.42f, 0.14f), new Color(1f, 0.72f, 0.25f));
        GameObject luz = Cubo("Luz_Estado", cabezal, new Vector3(0.24f, 0.29f, -0.036f), new Vector3(0.018f, 0.018f, 0.004f), mLedApagado);

        consola = g.gameObject.AddComponent<ConsolaAcceso>();
        consola.pantalla = texto;
        consola.fondo = fondo.GetComponent<Renderer>();
        consola.fondoApagado = mPantallaApagada;
        consola.fondoEncendido = mPantallaSistema;
        consola.luz = luz.GetComponent<Renderer>();
        consola.luzRoja = mLedRojo;
        consola.luzVerde = mLedVerde;
        consola.luzApagada = mLedApagado;
        consola.indicacion = "Fragmentos ordenados por IP, de menor a mayor";

        // Teclado hexadecimal de 4 x 4 y la tecla BORRAR debajo
        string[] filas = { "123A", "456B", "789C", "0DEF" };
        for (int f = 0; f < filas.Length; f++)
            for (int c = 0; c < 4; c++)
            {
                string caracter = filas[f][c].ToString();
                PressableButton tecla = Tecla(cabezal, caracter, new Vector3(-0.12f + c * 0.08f, 0.06f - f * 0.064f, -0.035f),
                                              new Vector2(0.066f, 0.05f), caracter);
                UnityEventTools.AddStringPersistentListener(tecla.alPresionar, consola.Ingresar, caracter);
                EditorUtility.SetDirty(tecla);
            }
        PressableButton borrar = Tecla(cabezal, "Borrar", new Vector3(0f, -0.215f, -0.035f), new Vector2(0.3f, 0.045f), "BORRAR");
        UnityEventTools.AddVoidPersistentListener(borrar.alPresionar, new UnityAction(consola.Borrar));
        EditorUtility.SetDirty(borrar);
    }

    // Una tecla de la consola. Está girada para que su "arriba" apunte hacia afuera del panel: así
    // PressableButton la hunde hacia adentro al presionarla. La letra va sobre la tapa.
    static PressableButton Tecla(Transform cabezal, string nombre, Vector3 pos, Vector2 tam, string etiqueta)
    {
        Transform t = Grupo("Tecla_" + nombre, cabezal);
        t.localPosition = pos;
        t.localRotation = Quaternion.Euler(-90f, 0f, 0f);
        Cubo("Base", t, new Vector3(0f, 0.003f, 0f), new Vector3(tam.x + 0.006f, 0.006f, tam.y + 0.006f), mPlasticoNegro);

        Transform movil = Grupo("Movil", t);
        movil.localPosition = new Vector3(0f, 0.006f, 0f);
        Cubo("Tapa", movil, new Vector3(0f, 0.006f, 0f), new Vector3(tam.x, 0.012f, tam.y), mTecla);
        // La letra se lee desde afuera (+Y de la tecla), con su arriba hacia arriba del panel (+Z de la tecla)
        Texto("Letra", movil, new Vector3(0f, 0.0125f, 0f), Vector3.up, etiqueta, new Vector2(tam.x * 0.85f, tam.y * 0.8f),
              Color.white, Vector3.forward);

        var col = t.gameObject.AddComponent<BoxCollider>();
        col.center = new Vector3(0f, 0.012f, 0f);
        col.size = new Vector3(tam.x + 0.006f, 0.03f, tam.y + 0.006f);
        t.gameObject.AddComponent<XRSimpleInteractable>();
        var audio = t.gameObject.AddComponent<AudioSource>();
        audio.playOnAwake = false;
        audio.spatialBlend = 1f;
        var boton = t.gameObject.AddComponent<PressableButton>();
        boton.parteMovil = movil;
        boton.recorrido = 0.005f;   // el pitido lo pone la consola
        t.gameObject.AddComponent<ResaltarAlApuntar>();
        return boton;
    }

    // Balizas verdes en el piso, de la consola a la puerta: se prenden en cascada al dar el acceso
    static LucesEnCascada ArmarCascada(Transform p)
    {
        Transform g = Grupo("Cascada", p);
        var luces = new GameObject[6];
        for (int i = 0; i < luces.Length; i++)
        {
            luces[i] = Cubo("Baliza_" + (i + 1), g, new Vector3((SALIDA_X0 + SALIDA_X1) / 2f, 0.0025f, 5.95f + i * 0.28f),
                            new Vector3(0.16f, 0.003f, 0.16f), mLedVerde);
            luces[i].SetActive(false);
        }
        var cascada = g.gameObject.AddComponent<LucesEnCascada>();
        cascada.luces = luces;
        return cascada;
    }

    // ------------------------------------------------------------------ susto y cartel

    // Cada silla del susto se puede "tocar" (XR Simple Interactable, solo con su caja de colisión)
    // y tiene alrededor una zona invisible que avisa si el jugador llega al lado.
    static void ArmarSusto()
    {
        if (sillasSusto == null) return;
        var sillas = new List<Transform>();
        Transform grupo = null;
        foreach (GameObject silla in sillasSusto)
        {
            if (silla == null) continue;
            grupo = silla.transform.parent;
            var interactable = silla.AddComponent<XRSimpleInteractable>();
            Collider col = silla.GetComponent<Collider>();
            if (col != null)
            {
                interactable.colliders.Clear();
                interactable.colliders.Add(col);
            }
            sillas.Add(silla.transform);
        }
        if (grupo == null) return;

        var susto = grupo.gameObject.AddComponent<SustoSillas>();
        susto.sillas = sillas.ToArray();
        susto.pantalla = computadorasSala.Count > 0 ? computadorasSala[0] : null;   // C1

        for (int i = 0; i < sillas.Count; i++)
        {
            Transform zona = Grupo("Zona_Susto_S" + (i + 1), grupo);
            Vector3 donde = sillas[i].position;
            zona.position = new Vector3(donde.x, grupo.position.y + 1f, donde.z);
            var col = zona.gameObject.AddComponent<BoxCollider>();
            col.isTrigger = true;
            col.size = new Vector3(1f, 2f, 1f);
            var disparador = zona.gameObject.AddComponent<DisparadorJugador>();
            UnityEventTools.AddIntPersistentListener(disparador.alEntrar, susto.DispararPorZona, i);
            EditorUtility.SetDirty(disparador);
        }
        EditorUtility.SetDirty(susto);
    }

    // Cartel de objetivos junto a la entrada, encima del tablero de luces (el mismo PanelObjetivo
    // de los Cuartos 2 y 4): dice qué hay que hacer y cambia solo con cada acertijo resuelto.
    static PanelObjetivo ArmarCartelObjetivos(Transform p)
    {
        Transform g = Grupo("Cartel_Objetivos", p);
        g.localPosition = new Vector3(3.2f, 1.82f, 0f);
        Cubo("Tablero", g, new Vector3(0f, 0f, 0.01f), new Vector3(0.52f, 0.26f, 0.02f), mPlaca);
        Cubo("Linea", g, new Vector3(0f, -0.12f, 0.0215f), new Vector3(0.52f, 0.012f, 0.003f), mFranja);
        TextMeshPro texto = Texto("Texto", g, new Vector3(0f, 0.005f, 0.0205f), Vector3.forward, "", new Vector2(0.47f, 0.2f), Color.white);

        var panel = g.gameObject.AddComponent<PanelObjetivo>();
        panel.texto = texto;
        panel.pasos = new[]
        {
            "SALA SIN LUZ\n<size=70%>Busca cómo encender las luces</size>",
            "HAY LUZ, PERO NO HAY RED\n<size=70%>Conecta el cable de cada computadora\nen el panel de red (mira el mapa)</size>",
            "RED CONECTADA\n<size=70%>Enciende las computadoras\ny anota los fragmentos</size>",
            "CLAVE COMPLETA\n<size=70%>Ingrésala en la consola de acceso</size>",
            "ACCESO CONCEDIDO\n<size=70%>Sigue al laboratorio</size>"
        };
        return panel;
    }

    // ------------------------------------------------------------------ materiales

    static void CrearMaterialesAcertijos()
    {
        mPantallaApagada = Mat("C3_PantallaApagada", new Color(0.015f, 0.017f, 0.02f), 0f, 0.85f);
        mPantallaArranque = Mat("C3_PantallaArranque", new Color(0.02f, 0.03f, 0.05f), 0f, 0.85f, new Color(0.02f, 0.04f, 0.08f));
        mPantallaSistema = Mat("C3_PantallaSistema", new Color(0.01f, 0.03f, 0.02f), 0f, 0.85f, new Color(0.01f, 0.06f, 0.035f));
        mPantallaAzul = Mat("C3_PantallaAzul", new Color(0.05f, 0.2f, 0.7f), 0f, 0.8f, new Color(0.05f, 0.25f, 0.9f));
        mPantallaCara = Mat("C3_PantallaCara", new Color(0.8f, 0.82f, 0.78f), 0f, 0.5f, new Color(1f, 1f, 0.95f));
        mLedApagado = Mat("C3_LedApagado", new Color(0.08f, 0.09f, 0.08f), 0f, 0.5f);
        mIndicadorApagado = Mat("C3_IndicadorApagado", new Color(0.3f, 0.05f, 0.04f), 0f, 0.5f, new Color(0.25f, 0.03f, 0.02f));
        mLente = Mat("C3_Lente", new Color(1f, 0.95f, 0.85f), 0f, 0.9f, new Color(2.2f, 2f, 1.6f));
        mNoche = Mat("C3_CieloNoche", new Color(0.01f, 0.015f, 0.04f), 0f, 0.3f, new Color(0.015f, 0.025f, 0.07f));
        mLama = Mat("C3_Persiana", new Color(0.75f, 0.76f, 0.77f), 0.2f, 0.4f);
        mTecla = Mat("C3_Tecla", new Color(0.22f, 0.23f, 0.25f), 0f, 0.4f);
        mPanelRed = Mat("C3_PanelRed", new Color(0.16f, 0.17f, 0.19f), 0.4f, 0.45f);
        mPuerto = Mat("C3_Puerto", new Color(0.02f, 0.02f, 0.02f), 0f, 0.2f);
        mCableAzul = Mat("C3_CableAzul", new Color(0.15f, 0.35f, 0.85f), 0f, 0.45f);
        mCableVerde = Mat("C3_CableVerde", new Color(0.2f, 0.7f, 0.3f), 0f, 0.45f);
        mCableNaranja = Mat("C3_CableNaranja", new Color(0.95f, 0.5f, 0.1f), 0f, 0.45f);
    }
}
