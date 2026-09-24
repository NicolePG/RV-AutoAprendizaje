using System.Collections.Generic;
using System.IO;
using TMPro;
using Unity.VRTemplate;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Attachment;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;

// Arma el Cuarto 4 (Laboratorio de ciencias) adentro del objeto "Cuarto4_Laboratorio".
// Se corre desde el menú: Escape Room > Construir los 4 cuartos. Borra lo que había adentro
// y lo rehace, así siempre queda igual. No colocar nada a mano adentro: se borra al reconstruir.
//
// Es el último cuarto del juego: un laboratorio de química de colegio (8 x 9.5 m, techo de
// 3.3), a oscuras por el apagón. Los muebles y equipos son modelos de Sketchfab (CC BY, créditos
// en Assets/Sketchfab/CREDITOS.txt) y de Poly Haven (CC0). Los carteles son modernos (señales
// estilo ISO 7010 y paneles con encabezado de color) y la pantalla de estado junto a la entrada
// dice en qué tarea va el jugador.
//
// LOS 5 ACERTIJOS, en cadena (cada uno destraba el siguiente), todos con las manos:
//  1. TABLERO ELÉCTRICO (TableroFusibles, PortaFusible, FusibleLab). Junto a la entrada, el
//     tablero antiguo con los portafusibles A, B y C; cada uno dice el consumo de su circuito.
//     Al lado, el panel "Cómo reponer los fusibles" explica la regla (el fusible de valor
//     inmediato superior) con un ejemplo y la escala de colores. En la caja de repuestos, seis
//     fusibles ordenados y rotulados. Solución: A = 10 A, B = 16 A, C = 6 A. Uno más chico
//     salta con un chispazo; uno más grande no sirve (los dos dan luz roja y se pueden sacar
//     y volver a probar). Con los tres vuelve la luz.
//  2. CAMPANA DE EXTRACCIÓN (CampanaExtraccion, VentanaCampana). El protocolo junto al gas dice:
//     bajar el vidrio de la campana y poner el extractor en 3. Destraba las llaves de gas.
//  3. LÍNEA DE GAS (LineaGas, ValvulaGas). Dos válvulas en la pared Este: se abren girando el
//     volante, dos vueltas. A mitad de la segunda salta EL SUSTO (SustoCamilla): se corta la
//     luz, la camilla rueda sola y al volver la luz el cuerpo tapado ya no está. Con las dos
//     abiertas se encienden los mecheros.
//  4. ENSAYO A LA LLAMA (Mechero, MuestraLlama, SecuenciaPalancas, PalancaCuchilla). Las tres
//     muestras de la práctica se meten en la llama: 1 = verde, 2 = amarillo, 3 = rojo. El atril
//     dice qué metal da cada color (Cu, Na, Li). Las palancas de la salida tienen esos metales y
//     se bajan en el orden de las muestras: Cu, Na, Li (el visor pide la de la muestra que sigue).
//  5. SALIDA DE EMERGENCIA (LectorTarjeta, TarjetaAcceso). Con las palancas en orden se abre el
//     casillero del docente; su tarjeta, acercada al lector junto a la puerta, abre la salida.
//     La tarjeta del alumno (sobre la mesada) no sirve.
//
// EL FINAL: detrás de la salida está el patio del colegio, al aire libre (PantallaVictoria). Al
// salir se hace de día, aparece el cartel "¡LOGRASTE SALIR DEL COLEGIO!", suena una fanfarria y
// hay un botón para volver a jugar.
public static class ConstructorCuarto4
{
    const float ANCHO = 8f;      // eje X: pared Oeste (0) a pared Este (8)
    const float FONDO = 9.5f;    // eje Z: entrada (0) al fondo (9.5)
    const float ALTO = 3.3f;     // altura del techo
    const float MURO = 0.12f;    // espesor de las paredes

    // El vano de entrada queda donde termina el Cuarto 3; la salida de emergencia, en el fondo
    const float ENTRADA_X0 = 0.8f, ENTRADA_X1 = 1.9f;
    const float SALIDA_X0 = 5.9f, SALIDA_X1 = 7.1f;
    const float ALTO_PUERTA = 2.1f;

    const float ALTO_FRISO = 1.5f;      // hasta dónde llega el azulejo verde
    const float PARED_O = 0.035f;       // cara del azulejo de la pared Oeste
    const float PARED_E = ANCHO - 0.035f;
    const float PARED_N = 0.035f;       // cara del azulejo de la pared de la entrada
    const float PARED_S = FONDO - 0.035f;
    const float MESADA = 0.92f;         // altura de la mesada del laboratorio

    const string CARPETA_SKETCHFAB = "Assets/Sketchfab";
    const string CARPETA_POLYHAVEN = "Assets/PolyHaven/Modelos";

    enum Apoyo { Piso, Centro, Pared }

    static Transform raizCuarto;
    static PanelObjetivo panelObjetivo;

    static Material mParedAlta, mFriso, mGuarda, mPiso, mTecho, mAcero, mAceroOscuro, mNegro, mBlanco,
                    mPapel, mSabana, mSabanaGris, mMancha, mSangre, mVerdeLuz, mRojoLuz, mLedApagado, mLuzTecho,
                    mTuboApagado, mVidrio, mGas, mCeramica, mLaton, mHumo, mEsfera, mVisor, mSal,
                    mPlastico, mPlasticoOscuro, mMadera, mAmbarLuz, mGrisCasillero, mPantalla, mCianOscuro,
                    mAmarilloSenal, mAzulSenal, mVerdeSenal, mRojoSenal, mLlamaExterior, mLlamaInterior,
                    mFus4, mFus6, mFus10, mFus16, mFus20, mFus25, mPatio, mMuroPatio;

    // Mallas hechas por código (se guardan como asset en Materials/Cuarto4)
    static Mesh mallaTriangulo, mallaLlama;

    // Lo que ControlEnergia prende o apaga según haya corriente o no
    static readonly List<Light> lucesCuarto = new List<Light>();
    static readonly List<GameObject> conEnergia = new List<GameObject>();

    // Lo que se arma en un lado y se conecta en otro
    static SustoCamilla susto;
    static readonly List<Mechero> mecheros = new List<Mechero>();

    // Lo llama el menú Escape Room > Construir los 4 cuartos (MenuEscapeRoom)
    public static void Construir()
    {
        var raiz = GameObject.Find("Cuarto4_Laboratorio");
        if (raiz == null)
        {
            raiz = new GameObject("Cuarto4_Laboratorio");
            Undo.RegisterCreatedObjectUndo(raiz, "Construir Cuarto 4");
        }
        // Después del Cuarto 3 (ver DisposicionCuartos)
        raiz.transform.SetPositionAndRotation(DisposicionCuartos.Cuarto4, DisposicionCuartos.Giro);
        raizCuarto = raiz.transform;

        for (int i = raiz.transform.childCount - 1; i >= 0; i--)
            Undo.DestroyObjectImmediate(raiz.transform.GetChild(i).gameObject);

        lucesCuarto.Clear();
        conEnergia.Clear();
        mecheros.Clear();
        CrearMateriales();
        mallaTriangulo = GuardarMalla(MallaTriangulo(), "C4_Malla_Triangulo");
        mallaLlama = GuardarMalla(MallaLlama(), "C4_Malla_Llama");

        Transform estructura = Grupo("Estructura", raiz.transform);
        Transform mobiliario = Grupo("Mobiliario", raiz.transform);
        Transform acertijos = Grupo("Acertijos", raiz.transform);
        Transform luces = Grupo("Luces", raiz.transform);

        ArmarEstructura(estructura);
        ArmarLamparas(luces);
        List<GameObject> sinEnergia = ArmarEmergencia(luces);
        ArmarPantallaEstado(raiz.transform);

        ArmarIsla(mobiliario);
        ArmarMueblesDePared(mobiliario);
        ArmarDecoracion(mobiliario);

        // Las tarjetas de acceso son assets (ItemData): el lector acepta un asset, no un texto
        ItemData tarjetaDocente = Item("ItemData_TarjetaDocente", "tarjeta_docente", "Tarjeta del docente",
            "Credencial del profesor del laboratorio. Autoriza la evacuación: abre la salida de emergencia.");
        ItemData tarjetaAlumno = Item("ItemData_TarjetaAlumno", "tarjeta_alumno", "Tarjeta de alumno",
            "Credencial de un alumno de 3° año. No tiene permiso para abrir la salida de emergencia.");

        // Los acertijos, en el orden en que los resuelve el jugador
        TableroFusibles tablero = ArmarTablero(acertijos);
        CampanaExtraccion campana = ArmarCampana(acertijos);
        susto = ArmarCamilla(acertijos, campana.luzInterior);
        LineaGas gas = ArmarLineaGas(acertijos);
        ArmarEnsayoLlama(acertijos, tarjetaAlumno);
        gas.mecheros = mecheros.ToArray();
        SecuenciaPalancas palancas = ArmarPalancas(acertijos);
        Door casillero = ArmarCasillero(acertijos, tarjetaDocente);
        Door puerta = ArmarPuertaSalida(raiz.transform);
        ArmarPatioSalida(raiz.transform, puerta);
        LectorTarjeta lector = ArmarLector(acertijos, tarjetaDocente);

        ControlEnergia control = ArmarControlEnergia(raiz.transform, sinEnergia);
        ArmarEntrada(raiz.transform, control);

        // La cadena: cada acertijo, al resolverse, destraba el siguiente y avanza la pantalla de
        // estado. Son eventos guardados en la escena: se ven y se pueden cambiar en el Inspector.
        UnityEventTools.AddVoidPersistentListener(tablero.alResolverse, new UnityAction(control.Encender));
        UnityEventTools.AddVoidPersistentListener(tablero.alResolverse, new UnityAction(campana.DarEnergia));
        Avisar(tablero.alResolverse, 1);
        UnityEventTools.AddVoidPersistentListener(campana.alResolverse, new UnityAction(gas.Desbloquear));
        Avisar(campana.alResolverse, 2);
        UnityEventTools.AddVoidPersistentListener(gas.alResolverse, new UnityAction(palancas.DarEnergia));
        Avisar(gas.alResolverse, 3);
        UnityEventTools.AddVoidPersistentListener(palancas.alResolverse, new UnityAction(casillero.Abrir));
        Avisar(palancas.alResolverse, 4);
        UnityEventTools.AddVoidPersistentListener(lector.alResolverse, new UnityAction(puerta.Abrir));
        Avisar(lector.alResolverse, 5);
        foreach (Object o in new Object[] { tablero, campana, gas, palancas, casillero, lector }) EditorUtility.SetDirty(o);

        AjustarAmbienteEditor();
        AsegurarInventario();
        PrepararParaQuest(raiz.transform);

        foreach (Transform hijo in raiz.transform)
            Undo.RegisterCreatedObjectUndo(hijo.gameObject, "Construir Cuarto 4");

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(raiz.scene);
        Selection.activeGameObject = raiz;
        Debug.Log("Cuarto 4 armado.");
    }

    // Lo llama el menú Escape Room > Llevar jugador al Cuarto 4 (MenuEscapeRoom): deja al
    // jugador parado en la entrada del laboratorio. Se deshace con Ctrl+Z.
    public static void LlevarJugador()
    {
        var raiz = GameObject.Find("Cuarto4_Laboratorio");
        if (raiz == null)
        {
            Debug.LogWarning("Primero hay que construir el Cuarto 4.");
            return;
        }
        var camara = Camera.main;
        if (camara == null)
        {
            Debug.LogWarning("No se encontró la cámara del jugador en la escena.");
            return;
        }

        // El jugador es el objeto de más arriba de todos los que tienen la cámara adentro
        Transform jugador = camara.transform;
        while (jugador.parent != null) jugador = jugador.parent;

        Undo.RecordObject(jugador, "Llevar jugador al Cuarto 4");
        // TransformPoint y no una suma, porque el cuarto está girado (ver DisposicionCuartos)
        jugador.position = raiz.transform.TransformPoint(new Vector3((ENTRADA_X0 + ENTRADA_X1) / 2f, 0f, 1f));
        jugador.rotation = raiz.transform.rotation;
        Selection.activeGameObject = jugador.gameObject;
    }

    // ------------------------------------------------------------------ estructura

    static void ArmarEstructura(Transform p)
    {
        // La losa del piso es la zona de teletransporte
        var losa = Cubo("Piso", p, new Vector3(ANCHO / 2f, -0.1f, FONDO / 2f), new Vector3(ANCHO, 0.2f, FONDO), mPiso, true);
        var area = losa.AddComponent<TeleportationArea>();
        int capaTeleport = InteractionLayerMask.GetMask("Teleport");
        if (capaTeleport == 0) capaTeleport = 1 << 31;
        area.interactionLayers = capaTeleport;

        Transform paredes = Grupo("Paredes", p);
        Cubo("Pared_Oeste", paredes, new Vector3(-MURO / 2f, ALTO / 2f, FONDO / 2f), new Vector3(MURO, ALTO, FONDO), mParedAlta, true);
        Cubo("Pared_Este", paredes, new Vector3(ANCHO + MURO / 2f, ALTO / 2f, FONDO / 2f), new Vector3(MURO, ALTO, FONDO), mParedAlta, true);
        Muro(paredes, "Pared_Entrada_Izq", -MURO, ENTRADA_X0, -MURO / 2f, 0f, ALTO);
        Muro(paredes, "Pared_Entrada_Der", ENTRADA_X1, ANCHO + MURO, -MURO / 2f, 0f, ALTO);
        Muro(paredes, "Dintel_Entrada", ENTRADA_X0, ENTRADA_X1, -MURO / 2f, ALTO_PUERTA, ALTO);
        Muro(paredes, "Pared_Fondo_Izq", -MURO, SALIDA_X0, FONDO + MURO / 2f, 0f, ALTO);
        Muro(paredes, "Pared_Fondo_Der", SALIDA_X1, ANCHO + MURO, FONDO + MURO / 2f, 0f, ALTO);
        Muro(paredes, "Dintel_Salida", SALIDA_X0, SALIDA_X1, FONDO + MURO / 2f, ALTO_PUERTA, ALTO);
        Cubo("Techo", p, new Vector3(ANCHO / 2f, ALTO + MURO / 2f, FONDO / 2f), new Vector3(ANCHO + MURO * 2f, MURO, FONDO + MURO * 2f), mTecho, true);

        // El azulejo verde de los laboratorios, hasta 1.5 m, con una guarda oscura arriba
        Transform friso = Grupo("Azulejo", p);
        FrisoLargo(friso, "Friso_Oeste", 0.02f, FONDO / 2f, FONDO);
        FrisoLargo(friso, "Friso_Este", ANCHO - 0.02f, FONDO / 2f, FONDO);
        FrisoAncho(friso, "Friso_Entrada_Izq", ENTRADA_X0 / 2f, 0.02f, ENTRADA_X0);
        FrisoAncho(friso, "Friso_Entrada_Der", (ENTRADA_X1 + ANCHO) / 2f, 0.02f, ANCHO - ENTRADA_X1);
        FrisoAncho(friso, "Friso_Fondo_Izq", SALIDA_X0 / 2f, FONDO - 0.02f, SALIDA_X0);
        FrisoAncho(friso, "Friso_Fondo_Der", (SALIDA_X1 + ANCHO) / 2f, FONDO - 0.02f, ANCHO - SALIDA_X1);

        // Zócalo sanitario oscuro abajo de todo, como en los laboratorios
        Cubo("Zocalo_Oeste", friso, new Vector3(0.04f, 0.05f, FONDO / 2f), new Vector3(0.015f, 0.1f, FONDO), mGuarda);
        Cubo("Zocalo_Este", friso, new Vector3(ANCHO - 0.04f, 0.05f, FONDO / 2f), new Vector3(0.015f, 0.1f, FONDO), mGuarda);
    }

    static void FrisoLargo(Transform p, string nombre, float x, float z, float largo)
    {
        Cubo(nombre, p, new Vector3(x, ALTO_FRISO / 2f, z), new Vector3(0.03f, ALTO_FRISO, largo), mFriso);
        Cubo(nombre + "_Guarda", p, new Vector3(x, ALTO_FRISO + 0.03f, z), new Vector3(0.04f, 0.06f, largo), mGuarda);
    }

    static void FrisoAncho(Transform p, string nombre, float x, float z, float largo)
    {
        if (largo <= 0f) return;
        Cubo(nombre, p, new Vector3(x, ALTO_FRISO / 2f, z), new Vector3(largo, ALTO_FRISO, 0.03f), mFriso);
        Cubo(nombre + "_Guarda", p, new Vector3(x, ALTO_FRISO + 0.03f, z), new Vector3(largo, 0.06f, 0.04f), mGuarda);
    }

    static void Muro(Transform p, string nombre, float x0, float x1, float z, float yBase, float yTope)
    {
        float ancho = x1 - x0, alto = yTope - yBase;
        if (ancho <= 0f || alto <= 0f) return;
        Cubo(nombre, p, new Vector3((x0 + x1) / 2f, yBase + alto / 2f, z), new Vector3(ancho, alto, MURO), mParedAlta, true);
    }

    // Seis lámparas fluorescentes de techo armadas con piezas (el modelo de Poly Haven tenía 18
    // mil triángulos por lámpara). Los tubos encendidos aparecen cuando vuelve la corriente.
    // La luz la dan tres luces puntuales, pocas a propósito (el Quest tiene un límite por objeto).
    static void ArmarLamparas(Transform p)
    {
        int n = 1;
        foreach (float z in new[] { 2.2f, 4.8f, 7.4f })
        {
            foreach (float x in new[] { 2.6f, 5.4f })
            {
                Transform l = Grupo("Lampara_" + n++, p);
                l.localPosition = new Vector3(x, ALTO - 0.002f, z);
                Cubo("Carcasa", l, new Vector3(0f, -0.025f, 0f), new Vector3(0.62f, 0.05f, 1.24f), mBlanco);
                Transform encendidos = Grupo("Tubos_Encendidos", l);
                foreach (float dx in new[] { -0.13f, 0.13f })
                {
                    Cilindro("Tubo", l, new Vector3(dx, -0.065f, 0f), new Vector3(0.028f, 0.58f, 0.028f), mTuboApagado)
                        .transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                    Cilindro("Tubo", encendidos, new Vector3(dx, -0.065f, 0f), new Vector3(0.03f, 0.585f, 0.03f), mLuzTecho)
                        .transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                }
                encendidos.gameObject.SetActive(false);
                conEnergia.Add(encendidos.gameObject);
            }
            lucesCuarto.Add(LuzPunto("Luz_Techo", p, new Vector3(ANCHO / 2f, ALTO - 0.35f, z), new Color(0.95f, 1f, 0.97f), 2.4f, 7.5f));
        }
    }

    // Luces verdes de emergencia en las cuatro esquinas: a batería, son lo único que se ve al
    // entrar. Tres parpadean. Se apagan cuando vuelve la corriente.
    static List<GameObject> ArmarEmergencia(Transform p)
    {
        var sinEnergia = new List<GameObject>();
        float[,] esquinas = { { 0.6f, 0.6f }, { ANCHO - 0.6f, 0.6f }, { 0.6f, FONDO - 0.6f }, { ANCHO - 0.6f, FONDO - 0.6f } };
        for (int i = 0; i < 4; i++)
        {
            Transform g = Grupo("Emergencia_" + (i + 1), p);
            g.localPosition = new Vector3(esquinas[i, 0], ALTO - 0.18f, esquinas[i, 1]);
            Cubo("Caja", g, Vector3.zero, new Vector3(0.18f, 0.1f, 0.12f), mAceroOscuro);
            Cubo("Foco", g, new Vector3(0f, -0.055f, 0f), new Vector3(0.14f, 0.01f, 0.08f), mVerdeLuz);
            Light luz = LuzPunto("Luz", g, new Vector3(0f, -0.15f, 0f), new Color(0.3f, 1f, 0.5f), 0.9f, 4.5f);
            if (i != 1) luz.gameObject.AddComponent<Parpadeo>();
            sinEnergia.Add(g.gameObject);
        }
        return sinEnergia;
    }

    // ------------------------------------------------------------------ pantalla de estado

    // Las cinco tareas del laboratorio, en orden, y qué hacer en cada una (dónde y cómo). Las
    // paredes se nombran como las ve el jugador al entrar: la campana a la izquierda y el gas a
    // la derecha.
    static readonly string[] TAREAS =
    {
        "SUMINISTRO ELÉCTRICO", "EXTRACCIÓN DE GASES", "LÍNEA DE GAS", "ENSAYO A LA LLAMA", "SALIDA DE EMERGENCIA"
    };
    static readonly string[] INDICACIONES =
    {
        "Tablero junto a la entrada: poné en cada portafusibles (A, B, C) el fusible que le corresponde.",
        "Campana (pared izquierda): bajá el vidrio y poné el extractor en 3.",
        "Llaves de gas (pared derecha): abrí V1 y V2 girando el volante hasta el tope.",
        "Mesada: el color de la llama dice el metal de cada muestra. Bajá sus palancas en orden 1, 2, 3.",
        "Casillero del docente abierto: su tarjeta abre la salida (lector junto a la puerta)."
    };

    // El monitor del sistema de seguridad del laboratorio, en la pared de la izquierda al entrar.
    // Lista las cinco tareas: las hechas en verde, la de ahora en ámbar con qué hay que hacer y
    // las que faltan en gris. Cambia sola con cada acertijo resuelto (PanelObjetivo, el mismo
    // script del Cuarto 2). La pantalla brilla sola: se lee a oscuras.
    static void ArmarPantallaEstado(Transform raiz)
    {
        Transform g = Grupo("Pantalla_Estado", raiz);
        g.localPosition = new Vector3(PARED_O, 1.7f, 1.05f);
        g.localRotation = Quaternion.LookRotation(Vector3.right);   // su +Z mira al cuarto
        Cubo("Soporte", g, new Vector3(0f, 0f, 0.015f), new Vector3(0.3f, 0.2f, 0.03f), mAceroOscuro);
        Cubo("Marco", g, new Vector3(0f, 0f, 0.045f), new Vector3(1.04f, 0.64f, 0.03f), mNegro);
        Cubo("Pantalla", g, new Vector3(0f, 0f, 0.0605f), new Vector3(0.99f, 0.59f, 0.002f), mPantalla);
        Cubo("Barra", g, new Vector3(0f, 0.25f, 0.0615f), new Vector3(0.99f, 0.09f, 0.001f), mCianOscuro);
        Texto("Titulo", g, new Vector3(0f, 0.25f, 0.063f), Vector3.forward,
              "<b>LABORATORIO DE CIENCIAS</b>  ·  ESTADO DE SEGURIDAD", new Vector2(0.92f, 0.05f), Color.white);
        TextMeshPro texto = Texto("Estado", g, new Vector3(0f, -0.045f, 0.063f), Vector3.forward, "",
                                  new Vector2(0.9f, 0.46f), Color.white);
        texto.alignment = TextAlignmentOptions.TopLeft;
        texto.fontSizeMax = 0.5f;   // que no cambie mucho de tamaño entre un paso y otro
        LuzPunto("Luz_Pantalla", g, new Vector3(0f, 0f, 0.45f), new Color(0.6f, 0.8f, 1f), 0.7f, 1.6f);

        panelObjetivo = g.gameObject.AddComponent<PanelObjetivo>();
        panelObjetivo.texto = texto;
        panelObjetivo.pasos = new string[TAREAS.Length + 1];
        for (int i = 0; i <= TAREAS.Length; i++) panelObjetivo.pasos[i] = Estado(i);
        texto.text = panelObjetivo.pasos[0];   // así se ve también en el editor, sin darle Play
        EditorUtility.SetDirty(panelObjetivo);
    }

    // El texto de la pantalla cuando el jugador va por la tarea "paso" (5 = terminó todo)
    static string Estado(int paso)
    {
        var s = new System.Text.StringBuilder();
        for (int i = 0; i < TAREAS.Length; i++)
        {
            if (i < paso)
                s.Append("<color=#4ade80>•  " + TAREAS[i] + "<pos=74%>COMPLETO</color>\n");
            else if (i == paso)
                s.Append("<color=#fbbf24><b>•  " + TAREAS[i] + "<pos=74%>EN CURSO</b></color>\n" +
                         "<size=80%><color=#e2e8f0>    " + INDICACIONES[i] + "</color></size>\n");
            else
                s.Append("<color=#64748b>•  " + TAREAS[i] + "<pos=74%>PENDIENTE</color>\n");
        }
        if (paso >= TAREAS.Length) s.Append("\n<color=#4ade80><b>EVACUACIÓN AUTORIZADA: la salida está abierta.</b></color>");
        return s.ToString();
    }

    static void Avisar(UnityEventBase evento, int paso)
    {
        if (panelObjetivo != null)
            UnityEventTools.AddIntPersistentListener(evento, new UnityAction<int>(panelObjetivo.MostrarPaso), paso);
    }

    // ------------------------------------------------------------------ mobiliario

    // La mesada química central: isla doble en L con piletas y cajones de los dos lados
    // (Sketchfab). La mesada queda a 0.92 m. Se le ponen dos cajas de colisión, una por brazo
    // de la L, para que el hueco de adentro de la L quede libre.
    static void ArmarIsla(Transform p)
    {
        GameObject isla = ModeloSketchfab("mesada_quimica", p, new Vector3(3.385f, 0f, 4.66f), 0f, 1.254f, Apoyo.Piso, false);
        if (isla == null)
        {
            Transform g = Grupo("Mesada_Simple", p);
            Cubo("Brazo_Largo", g, new Vector3(3.385f, MESADA / 2f, 3.625f), new Vector3(4.77f, MESADA, 2.05f), mBlanco, true);
            Cubo("Brazo_Corto", g, new Vector3(5.41f, MESADA / 2f, 5.685f), new Vector3(0.72f, MESADA, 2.07f), mBlanco, true);
            return;
        }
        var largo = isla.AddComponent<BoxCollider>();
        largo.center = new Vector3(0f, MESADA / 2f, 3.625f - 4.66f);
        largo.size = new Vector3(4.77f, MESADA, 2.05f);
        var corto = isla.AddComponent<BoxCollider>();
        corto.center = new Vector3(5.41f - 3.385f, MESADA / 2f, 5.685f - 4.66f);
        corto.size = new Vector3(0.72f, MESADA, 2.07f);

        // Lo que hay sobre la mesada: cristalería junto a las piletas y un microscopio
        ModeloSketchfab("cristaleria", p, new Vector3(1.75f, MESADA, 4.3f), 0f, 0.2f, Apoyo.Piso, false);
        ModeloSketchfab("microscopio", p, new Vector3(5.41f, MESADA, 5.9f), -90f, 0.38f, Apoyo.Piso, false);

        // Taburetes del lado de las piletas
        foreach (float x in new[] { 1.9f, 2.8f, 3.7f })
            ModeloPolyHaven("metal_stool_02", p, new Vector3(x, 0f, 5.1f), x * 40f, 0.65f, Apoyo.Piso, true);
    }

    // Lo que va contra las paredes: la mesa de acero de la entrada (con los repuestos), la
    // vitrina de reactivos, la pizarra de la clase y la tabla periódica
    static void ArmarMueblesDePared(Transform p)
    {
        // Mesa de acero contra la pared de la entrada, a la derecha del tablero eléctrico. Su caja
        // de colisión llega solo hasta la tapa (0.9 m): si envolviera también el estante de
        // arriba, los fusibles que están sobre la mesa quedarían adentro y el rayo no los alcanzaría.
        GameObject mesa = ModeloSketchfab("mesa_laboratorio", p, new Vector3(6.3f, 0f, 0.5f), 90f, 1.728f, Apoyo.Piso, false);
        if (mesa != null)
        {
            var col = mesa.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, 0.45f, 0f);
            col.size = new Vector3(0.88f, 0.9f, 1.9f);   // en los ejes de la mesa: 1.9 m es su largo
        }
        else Cubo("Mesa_Acero", p, new Vector3(6.3f, 0.45f, 0.5f), new Vector3(1.9f, 0.9f, 0.88f), mAcero, true);
        ModeloSketchfab("cristaleria", p, new Vector3(6.85f, 0.9f, 0.5f), 180f, 0.2f, Apoyo.Piso, false);

        // Vitrina de reactivos contra la pared Oeste, al fondo
        ModeloSketchfab("vitrina_laboratorio", p, new Vector3(PARED_O + 0.01f, 0f, 8.3f), 90f, 1.8f, Apoyo.Pared, true);

        // Tabla periódica en la pared del fondo. El modelo es un plano acostado: se lo para
        // contra la pared con el frente hacia el cuarto (su textura corre sobre la Z del modelo)
        GameObject fuente = AssetDatabase.LoadAssetAtPath<GameObject>(CARPETA_SKETCHFAB + "/tabla_periodica.glb");
        if (fuente != null)
        {
            Transform t = Grupo("Tabla_Periodica", p);
            t.localPosition = new Vector3(2.2f, 1.8f, PARED_S - 0.01f);
            t.localRotation = Quaternion.LookRotation(Vector3.right, Vector3.back);
            GameObject tabla = Instanciar(fuente, t);
            tabla.transform.localScale *= 0.04f;   // 30 x 20 unidades = 1.2 x 0.8 m
        }
    }

    // Seguridad y ambientación: ducha de emergencia con lavaojos, extintor, botiquín, reloj
    // parado a la hora del apagón, tachos de gas y la señalización de seguridad (ISO 7010)
    static void ArmarDecoracion(Transform p)
    {
        // Ducha de emergencia contra la pared Este, cerca de la salida (el caño contra la pared)
        ModeloSketchfab("ducha_emergencia", p, new Vector3(7.52f, 0f, 7.35f), -90f, 2.3f, Apoyo.Piso, false);
        CartelModerno(p, "Cartel_Ducha", new Vector3(PARED_E - 0.005f, 2.45f, 7.35f), Vector3.left, Senal.Seguridad,
                      "DUCHA Y LAVAOJOS", "de emergencia", 0.52f, 0.17f);

        // Extintor junto a la entrada (al lado de la salida está el casillero del docente)
        ModeloPolyHaven("korean_fire_extinguisher_01", p, new Vector3(0.42f, 0f, 0.28f), 0f, 0.55f, Apoyo.Piso, false);
        CartelModerno(p, "Cartel_Extintor", new Vector3(0.42f, 1.2f, PARED_N + 0.005f), Vector3.forward, Senal.Incendio,
                      "EXTINTOR", "", 0.3f, 0.12f);
        ModeloPolyHaven("medical_box", p, new Vector3(PARED_E - 0.02f, 1.55f, 8.7f), -90f, 0.3f, Apoyo.Pared, false);
        ModeloPolyHaven("propane_tank", p, new Vector3(7.55f, 0f, 5.3f), 20f, 0.75f, Apoyo.Piso, true);
        ModeloPolyHaven("propane_tank", p, new Vector3(7.5f, 0f, 5.85f), -35f, 0.75f, Apoyo.Piso, true);

        // Reloj de pared parado a las 4:40, la hora del apagón (como en el Cuarto 3)
        GameObject reloj = ModeloPolyHaven("wall_clock", p, new Vector3(4.1f, 2.55f, PARED_S - 0.005f), 180f, 0.34f, Apoyo.Pared, false);
        if (reloj != null)
        {
            GirarAguja(reloj.transform, "wall_clock_hours_hand", 4f * 30f + 40f * 0.5f, 306.8f);
            GirarAguja(reloj.transform, "wall_clock_minute_hand", 40f * 6f, 57f);
            GirarAguja(reloj.transform, "wall_clock_second_hand", 0f, 170.5f);
        }

        // Señales de seguridad
        CartelModerno(p, "Cartel_Antiparras", new Vector3(PARED_O + 0.005f, 2.1f, 4.95f), Vector3.right, Senal.Obligacion,
                      "USO OBLIGATORIO", "de antiparras y guardapolvo", 0.55f, 0.18f);
        CartelModerno(p, "Cartel_Gas", new Vector3(PARED_E - 0.005f, 2.1f, 2.35f), Vector3.left, Senal.Advertencia,
                      "PELIGRO: GAS INFLAMABLE", "No encender llamas fuera de la mesada", 0.6f, 0.18f);
    }

    static void GirarAguja(Transform reloj, string nombre, float anguloFinal, float anguloModelo)
    {
        Transform aguja = BuscarHijo(reloj, nombre);
        if (aguja != null) aguja.localRotation *= Quaternion.Euler(0f, 0f, anguloFinal - anguloModelo);
    }

    // ------------------------------------------------------------------ acertijo 1: tablero

    // El tablero eléctrico antiguo (Sketchfab) en la pared de la entrada, abierto. Adentro,
    // abajo, la fila con los portafusibles de los tres circuitos del laboratorio (A, B y C):
    // cada uno dice el consumo de su circuito. Al lado, el panel que explica la regla.
    static TableroFusibles ArmarTablero(Transform p)
    {
        Transform g = Grupo("Tablero_Electrico", p);
        g.localPosition = new Vector3(3.4f, 1.35f, PARED_N);

        // Modelo: 80 x 105 x 30 unidades, abierto hacia +Z, con el fondo interior a 15 unidades
        const float s = 0.75f / 105f;
        GameObject fuente = AssetDatabase.LoadAssetAtPath<GameObject>(CARPETA_SKETCHFAB + "/tablero_fusibles.glb");
        if (fuente != null)
        {
            GameObject caja = Instanciar(fuente, g);
            caja.transform.localScale *= s;
            caja.transform.localPosition = -new Vector3(2.5f, 0f, -30f) * s;   // la espalda contra la pared
            // Solo el fondo choca: si la caja de colisión envolviera el tablero entero, los
            // fusibles puestos quedarían adentro y no se podrían sacar con el rayo
            var col = g.gameObject.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, 0f, 0.01f);
            col.size = new Vector3(80f * s, 105f * s, 0.02f);
        }
        else
        {
            Cubo("Fondo", g, new Vector3(0f, 0f, 0.01f), new Vector3(0.57f, 0.75f, 0.02f), mAceroOscuro, true);
            foreach (float x in new[] { -0.28f, 0.28f })
                Cubo("Lateral", g, new Vector3(x, 0f, 0.107f), new Vector3(0.02f, 0.75f, 0.21f), mAceroOscuro, true);
            // La barra de abajo donde van los portafusibles (en el modelo ya viene)
            Cubo("Barra", g, new Vector3(0f, -44.75f * s, (0.02f + 20f * s) / 2f), new Vector3(0.53f, 7.5f * s, 20f * s - 0.02f), mNegro);
        }

        // Cartel con el visor, arriba del tablero
        Cubo("Placa_Visor", g, new Vector3(0f, 0.45f, 0.012f), new Vector3(0.6f, 0.13f, 0.02f), mAceroOscuro);
        Texto("Titulo", g, new Vector3(0f, 0.485f, 0.023f), Vector3.forward, "TABLERO GENERAL · LABORATORIO", new Vector2(0.56f, 0.035f), Color.white);
        TextMeshPro visor = Texto("Visor", g, new Vector3(0f, 0.43f, 0.023f), Vector3.forward, "CIRCUITOS OK: 0 / 3",
                                  new Vector2(0.52f, 0.05f), new Color(1f, 0.72f, 0.25f));

        // Luz piloto de batería, entre el tablero y el panel de la regla: los dos se ven a oscuras
        LuzPunto("Luz_Piloto", g, new Vector3(-0.45f, 0f, 0.55f), new Color(1f, 0.8f, 0.5f), 1.1f, 2.4f);

        string[] letras = { "A", "B", "C" };
        int[] consumos = { 8, 14, 5 };
        int[] amperajes = { 10, 16, 6 };   // el fusible normalizado inmediato superior a cada consumo
        // Los portafusibles van en el frente de la barra de abajo del modelo (de -48.5 a -41 de alto
        // y 5 unidades delante del fondo): detrás de ella quedaban medio tapados. De izquierda a
        // derecha para el que mira: A, B, C (su izquierda es el +X del tablero).
        var portas = new PortaFusible[3];
        for (int i = 0; i < 3; i++)
            portas[i] = ArmarPortaFusible(g, letras[i], consumos[i], amperajes[i], new Vector3((-2f + (1 - i) * 16f) * s, -44.75f * s, 20f * s));

        var tablero = g.gameObject.AddComponent<TableroFusibles>();
        tablero.portas = portas;
        tablero.visor = visor;
        tablero.datos = Datos("Cuarto4_Fusibles", "cuarto4_fusibles", TipoAcertijo.Fusibles, "A=10 B=16 C=6",
            "Cada portafusibles dice el consumo de su circuito (8, 14 y 5 A); va el fusible normalizado inmediato superior.",
            "ENERGÍA RESTABLECIDA", "FUSIBLE QUEMADO");
        tablero.sonidoAcierto = AssetDatabase.LoadAssetAtPath<AudioClip>(RUTA_ACIERTO);

        ArmarGuiaFusibles(p);
        ArmarRepuestos(p);
        return tablero;
    }

    // Un portafusibles de porcelana sobre la barra del tablero, con su letra y el consumo de su
    // circuito arriba, su lucecita al costado y un aro ámbar que late mientras está vacío (como
    // la cerradura del Cuarto 1: "acá va algo"). "fondo" es el punto de la barra donde va apoyado.
    static PortaFusible ArmarPortaFusible(Transform p, string letra, int consumo, int amperaje, Vector3 fondo)
    {
        Transform t = Grupo("Portafusible_" + letra, p);
        t.localPosition = fondo;
        GameObject aro = Cilindro("Aro_Guia", t, new Vector3(0f, 0f, 0.001f), new Vector3(0.064f, 0.001f, 0.064f), mAmbarLuz);
        aro.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        Cilindro("Base", t, new Vector3(0f, 0f, 0.012f), new Vector3(0.045f, 0.012f, 0.045f), mCeramica).transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        Cilindro("Anillo", t, new Vector3(0f, 0f, 0.025f), new Vector3(0.036f, 0.002f, 0.036f), mLaton).transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        Cilindro("Boca", t, new Vector3(0f, 0f, 0.0265f), new Vector3(0.024f, 0.001f, 0.024f), mNegro).transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        // La letra y el consumo del circuito, arriba de la barra, sobre el fondo del tablero
        Texto("Letra", t, new Vector3(0f, 0.052f, -0.03f), Vector3.forward,
              "<b>" + letra + "</b>\n<size=45%>consumo " + consumo + " A</size>", new Vector2(0.1f, 0.05f), Color.white);
        // La lucecita, a la derecha del portafusibles (para el que mira, la derecha es -X)
        GameObject luz = Cubo("Luz", t, new Vector3(-0.038f, 0f, 0.004f), new Vector3(0.012f, 0.012f, 0.006f), mLedApagado);

        // Donde queda el fusible: con su punta metida 1 cm en la boca, apuntando hacia la pared
        Transform encaje = Grupo("Encaje", t);
        encaje.localPosition = new Vector3(0f, 0f, 0.046f);
        encaje.localRotation = Quaternion.LookRotation(Vector3.back);

        var zona = t.gameObject.AddComponent<SphereCollider>();
        zona.isTrigger = true;
        zona.center = encaje.localPosition;
        zona.radius = 0.06f;   // se suelta el fusible cerca y entra solo
        var socket = t.gameObject.AddComponent<XRSocketInteractor>();
        socket.attachTransform = encaje;

        // El chispazo y el humo cuando se pone un fusible más chico
        Light chispa = LuzPunto("Chispa", t, new Vector3(0f, 0f, 0.06f), new Color(1f, 0.8f, 0.5f), 5f, 1f);
        chispa.enabled = false;
        Transform humo = Grupo("Humo", t);
        for (int i = 0; i < 3; i++)
            Esfera("Nube", humo, new Vector3((i - 1) * 0.015f, 0.03f + i * 0.025f, 0.05f), Vector3.one * (0.03f + i * 0.012f), mHumo);
        humo.gameObject.SetActive(false);

        var porta = t.gameObject.AddComponent<PortaFusible>();
        porta.amperaje = amperaje;
        porta.aro = aro;
        porta.luz = luz.GetComponent<Renderer>();
        porta.luzVerde = mVerdeLuz;
        porta.luzRoja = mRojoLuz;
        porta.luzApagada = mLedApagado;
        porta.chispa = chispa;
        porta.humo = humo.gameObject;
        return porta;
    }

    // El panel "Cómo reponer los fusibles", a la izquierda del tablero: moderno y en tres pasos,
    // con la tabla de consumos de los circuitos, un ejemplo de la regla (con valores que no son
    // la respuesta) y la escala de fusibles con los mismos colores de la caja de repuestos.
    // En sus ejes, +X es la izquierda del que lo mira.
    static void ArmarGuiaFusibles(Transform p)
    {
        Transform d = Grupo("Guia_Fusibles", p);
        d.localPosition = new Vector3(2.5f, 1.4f, PARED_N);
        const float A = 0.5f, H = 0.72f;
        Color oscuro = new Color(0.1f, 0.12f, 0.16f);
        Cubo("Placa", d, new Vector3(0f, 0f, 0.004f), new Vector3(A, H, 0.008f), mBlanco);
        Cubo("Encabezado", d, new Vector3(0f, H / 2f - 0.045f, 0.0085f), new Vector3(A, 0.09f, 0.001f), mAzulSenal);
        Texto("Titulo", d, new Vector3(0f, H / 2f - 0.045f, 0.0095f), Vector3.forward, "<b>CÓMO REPONER LOS FUSIBLES</b>",
              new Vector2(A - 0.05f, 0.05f), Color.white);

        TextMeshPro pasos = Texto("Pasos", d, new Vector3(0f, 0.17f, 0.0085f), Vector3.forward,
              "<color=#1d4ed8><b>1</b></color>   Mirá el <b>consumo</b> de cada circuito.\n" +
              "<color=#1d4ed8><b>2</b></color>   Elegí el fusible de valor <b>inmediato superior</b>:\n" +
              "      el primero que supera el consumo.\n" +
              "<color=#1d4ed8><b>3</b></color>   Soltalo cerca de su portafusibles y entra solo.\n" +
              "      <color=#15803d><b>Luz verde = correcto</b></color>",
              new Vector2(A - 0.05f, 0.18f), oscuro);
        pasos.alignment = TextAlignmentOptions.Left;

        // La tabla de los circuitos
        Cubo("Tabla_Fondo", d, new Vector3(0f, -0.02f, 0.0085f), new Vector3(A - 0.04f, 0.15f, 0.001f), mPapel);
        TextMeshPro tabla = Texto("Tabla", d, new Vector3(0f, -0.02f, 0.0095f), Vector3.forward,
              "<b><pos=2%>CIRCUITO<pos=30%>USO<pos=76%>CONSUMO</b>\n" +
              "<pos=8%>A<pos=30%>Iluminación<pos=80%>8 A\n" +
              "<pos=8%>B<pos=30%>Campana extractora<pos=80%>14 A\n" +
              "<pos=8%>C<pos=30%>Válvulas de gas<pos=80%>5 A",
              new Vector2(A - 0.08f, 0.13f), oscuro);
        tabla.alignment = TextAlignmentOptions.Left;

        Texto("Ejemplo", d, new Vector3(0f, -0.13f, 0.0085f), Vector3.forward,
              "<i>Ejemplo: un circuito que consume 3 A lleva el fusible de 4 A.</i>",
              new Vector2(A - 0.05f, 0.032f), new Color(0.3f, 0.32f, 0.36f));

        // La escala de fusibles, de menor a mayor: una ficha del color de cada uno con su valor
        int[] valores = { 4, 6, 10, 16, 20, 25 };
        for (int i = 0; i < valores.Length; i++)
        {
            float x = 0.2f - i * 0.08f;
            Cubo("Ficha_" + valores[i], d, new Vector3(x, -0.215f, 0.0085f), new Vector3(0.07f, 0.06f, 0.002f), ColorFusible(valores[i]));
            Texto("Valor_" + valores[i], d, new Vector3(x, -0.215f, 0.0102f), Vector3.forward, "<b>" + valores[i] + " A</b>",
                  new Vector2(0.062f, 0.04f), TextoSobreFusible(valores[i]));
        }
        Texto("Escala", d, new Vector3(0f, -0.28f, 0.0085f), Vector3.forward,
              "Fusibles de repuesto: en la caja de la mesa de acero", new Vector2(A - 0.05f, 0.024f), new Color(0.3f, 0.32f, 0.36f));
    }

    // La caja organizadora de repuestos sobre la mesa de acero: seis fusibles ordenados de menor a
    // mayor (de izquierda a derecha para el que mira), cada uno en su compartimento con una ficha
    // del mismo color y el valor bien grande, igual que la escala del panel. Tres sirven.
    static void ArmarRepuestos(Transform p)
    {
        Transform caja = Grupo("Caja_Repuestos", p);
        caja.localPosition = new Vector3(5.75f, 0.9f, 0.55f);
        const float COMPARTIMENTO = 0.066f;
        Cubo("Base", caja, new Vector3(0f, 0.006f, 0f), new Vector3(0.42f, 0.012f, 0.2f), mPlasticoOscuro, true);
        Cubo("Espuma", caja, new Vector3(0f, 0.016f, 0f), new Vector3(0.4f, 0.008f, 0.18f), mNegro);
        foreach (float z in new[] { -0.095f, 0.095f })
            Cubo("Borde", caja, new Vector3(0f, 0.022f, z), new Vector3(0.42f, 0.044f, 0.01f), mPlasticoOscuro);
        foreach (float x in new[] { -0.205f, 0.205f })
            Cubo("Borde", caja, new Vector3(x, 0.022f, 0f), new Vector3(0.01f, 0.044f, 0.2f), mPlasticoOscuro);

        // Cartelito parado detrás de la caja
        Cubo("Cartel", caja, new Vector3(0f, 0.075f, -0.1f), new Vector3(0.36f, 0.06f, 0.004f), mBlanco);
        Cubo("Cartel_Franja", caja, new Vector3(0f, 0.1f, -0.0975f), new Vector3(0.36f, 0.01f, 0.001f), mAzulSenal);
        Texto("Texto_Cartel", caja, new Vector3(0f, 0.07f, -0.0975f), Vector3.forward,
              "<b>FUSIBLES DE REPUESTO</b>\n<size=70%>ordenados de menor a mayor</size>", new Vector2(0.34f, 0.045f),
              new Color(0.1f, 0.12f, 0.16f));

        int[] amperajes = { 4, 6, 10, 16, 20, 25 };
        for (int i = 0; i < amperajes.Length; i++)
        {
            int a = amperajes[i];
            float x = (2.5f - i) * COMPARTIMENTO;   // +X es la izquierda del que mira: el 4 A queda a la izquierda
            if (i > 0)
                Cubo("Separador", caja, new Vector3(x + COMPARTIMENTO / 2f, 0.025f, 0f), new Vector3(0.003f, 0.03f, 0.18f), mPlasticoOscuro);
            // La ficha del frente: del color del fusible y con su valor
            Cubo("Ficha_" + a, caja, new Vector3(x, 0.024f, 0.1005f), new Vector3(COMPARTIMENTO - 0.008f, 0.03f, 0.002f), ColorFusible(a));
            Texto("Valor_" + a, caja, new Vector3(x, 0.024f, 0.1022f), Vector3.forward, "<b>" + a + " A</b>",
                  new Vector2(COMPARTIMENTO - 0.014f, 0.024f), TextoSobreFusible(a));
            ArmarFusible(p, a, new Vector3(5.75f + x, 0.9f + 0.032f, 0.55f));
        }
    }

    // Un fusible de cartucho: cuerpo de cerámica del color de su amperaje, dos tapas de bronce
    // y una etiqueta con el número. Se agarra del medio, con la punta hacia adelante.
    static void ArmarFusible(Transform p, int amperaje, Vector3 pos)
    {
        Transform f = Grupo("Fusible_" + amperaje + "A", p);
        f.localPosition = pos;
        GameObject cuerpo = Cilindro("Cuerpo", f, Vector3.zero, new Vector3(0.022f, 0.025f, 0.022f), ColorFusible(amperaje));
        cuerpo.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        foreach (float z in new[] { -0.028f, 0.028f })
            Cilindro("Tapa", f, new Vector3(0f, 0f, z), new Vector3(0.024f, 0.004f, 0.024f), mLaton).transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        Cubo("Etiqueta", f, new Vector3(0f, 0.0105f, 0f), new Vector3(0.02f, 0.002f, 0.022f), mPapel);
        Texto("Amperaje", f, new Vector3(0f, 0.0118f, 0f), Vector3.up, "<b>" + amperaje + " A</b>", new Vector2(0.021f, 0.013f),
              new Color(0.05f, 0.05f, 0.05f), Vector3.back);

        var col = f.gameObject.AddComponent<BoxCollider>();
        col.size = new Vector3(0.035f, 0.035f, 0.07f);
        Transform punto = PuntoDeAgarre(f, Vector3.zero, Vector3.forward, Vector3.up);
        HacerAgarrable(f.gameObject, punto, pos + Vector3.up * 0.1f, false);

        var fusible = f.gameObject.AddComponent<FusibleLab>();
        fusible.amperaje = amperaje;
    }

    static Material ColorFusible(int amperaje)
    {
        switch (amperaje)
        {
            case 4: return mFus4;
            case 6: return mFus6;
            case 10: return mFus10;
            case 16: return mFus16;
            case 20: return mFus20;
            default: return mFus25;
        }
    }

    // El color del número sobre la ficha de un fusible: oscuro sobre los claros (gris y amarillo)
    static Color TextoSobreFusible(int amperaje)
        => amperaje == 16 || amperaje == 25 ? new Color(0.08f, 0.08f, 0.1f) : Color.white;

    // ------------------------------------------------------------------ acertijo 2: campana

    // Campana de extracción contra la pared Oeste (z de 5.42 a 6.78). Abajo, el mueble de
    // laboratorio (Sketchfab); arriba, la campana armada con piezas porque su vidrio tiene que
    // deslizarse de verdad. A la derecha, el panel con la perilla del extractor (0 a 3).
    static CampanaExtraccion ArmarCampana(Transform p)
    {
        const float Z = 6.1f;           // centro de la campana
        const float FRENTE = 0.82f;     // cara de adelante (x)
        Transform g = Grupo("Campana", p);

        if (ModeloSketchfab("mueble_laboratorio", g, new Vector3(0.36f, 0f, Z), 90f, 0.88f, Apoyo.Pared, true) == null)
            Cubo("Mueble", g, new Vector3(0.6f, 0.44f, Z), new Vector3(0.44f, 0.88f, 1.06f), mBlanco, true);

        // Mesada negra, laterales, fondo, techo, frente de arriba y conducto al techo
        Cubo("Mesada", g, new Vector3(0.43f, 0.9f, Z), new Vector3(0.8f, 0.04f, 1.36f), mNegro, true);
        Cubo("Lateral_Izq", g, new Vector3(0.43f, 1.66f, Z - 0.63f), new Vector3(0.78f, 1.48f, 0.1f), mBlanco, true);
        Cubo("Lateral_Der", g, new Vector3(0.43f, 1.66f, Z + 0.63f), new Vector3(0.78f, 1.48f, 0.1f), mBlanco, true);
        Cubo("Fondo", g, new Vector3(PARED_O + 0.02f, 1.66f, Z), new Vector3(0.04f, 1.48f, 1.26f), mAcero);
        Cubo("Techo", g, new Vector3(0.43f, 2.44f, Z), new Vector3(0.82f, 0.08f, 1.36f), mBlanco, true);
        Cubo("Frente_Alto", g, new Vector3(FRENTE, 2.22f, Z), new Vector3(0.04f, 0.36f, 1.36f), mBlanco, true);
        Texto("Rotulo", g, new Vector3(FRENTE + 0.021f, 2.25f, Z), Vector3.right, "CAMPANA DE EXTRACCIÓN", new Vector2(1.1f, 0.09f),
              new Color(0.15f, 0.2f, 0.25f));
        Cilindro("Conducto", g, new Vector3(0.43f, (2.48f + ALTO) / 2f, Z), new Vector3(0.3f, (ALTO - 2.48f) / 2f, 0.3f), mAcero);
        Light luzInterior = LuzPunto("Luz_Interior", g, new Vector3(0.4f, 2.1f, Z), new Color(1f, 0.97f, 0.9f), 1.2f, 1.6f);
        ModeloSketchfab("cristaleria", g, new Vector3(0.38f, 0.92f, Z), 90f, 0.2f, Apoyo.Piso, false);

        // El vidrio corredizo: empieza arriba (abierto) y se baja con la mano hasta la mesada.
        // Va por detrás del frente alto, así su parte de arriba queda tapada al abrirlo.
        Transform vidrio = Grupo("Vidrio_Campana", g);
        vidrio.localPosition = new Vector3(FRENTE - 0.05f, 1.36f, Z);   // su borde de abajo
        Cubo("Vidrio", vidrio, new Vector3(0f, 0.35f, 0f), new Vector3(0.01f, 0.66f, 1.1f), mVidrio);
        Cubo("Marco_Abajo", vidrio, new Vector3(0f, 0.02f, 0f), new Vector3(0.03f, 0.04f, 1.14f), mAceroOscuro);
        Cubo("Marco_Arriba", vidrio, new Vector3(0f, 0.68f, 0f), new Vector3(0.03f, 0.04f, 1.14f), mAceroOscuro);
        foreach (float z in new[] { -0.555f, 0.555f })
            Cubo("Marco_Lado", vidrio, new Vector3(0f, 0.35f, z), new Vector3(0.03f, 0.7f, 0.03f), mAceroOscuro);
        Cubo("Manija", vidrio, new Vector3(0.035f, 0.03f, 0f), new Vector3(0.025f, 0.025f, 0.7f), mAcero);
        var colVidrio = vidrio.gameObject.AddComponent<BoxCollider>();
        colVidrio.center = new Vector3(0.015f, 0.35f, 0f);
        colVidrio.size = new Vector3(0.07f, 0.7f, 1.14f);
        vidrio.gameObject.AddComponent<XRSimpleInteractable>();
        vidrio.gameObject.AddComponent<ResaltarAlApuntar>();
        var ventana = vidrio.gameObject.AddComponent<VentanaCampana>();
        ventana.recorrido = 1.36f - 0.92f;

        // Panel de control sobre el lateral derecho: visor, lucecita y la perilla del extractor
        Transform panel = Grupo("Panel_Campana", g);
        panel.localPosition = new Vector3(FRENTE + 0.01f, 1.2f, Z + 0.63f);
        Cubo("Caja", panel, new Vector3(0.015f, 0f, 0f), new Vector3(0.03f, 0.4f, 0.16f), mAceroOscuro);
        Cubo("Pantalla", panel, new Vector3(0.031f, 0.12f, 0f), new Vector3(0.002f, 0.09f, 0.14f), mVisor);
        TextMeshPro visor = Texto("Visor", panel, new Vector3(0.033f, 0.12f, 0f), Vector3.right, "", new Vector2(0.13f, 0.08f),
                                  new Color(1f, 0.72f, 0.25f));
        GameObject luz = Cubo("Luz", panel, new Vector3(0.032f, 0.045f, 0f), new Vector3(0.004f, 0.016f, 0.016f), mLedApagado);
        Texto("Rotulo", panel, new Vector3(0.031f, 0.01f, 0f), Vector3.right, "EXTRACTOR", new Vector2(0.13f, 0.025f), Color.white);
        XRKnob perilla = ArmarPerilla(panel, new Vector3(0.03f, -0.09f, 0f));

        var campana = g.gameObject.AddComponent<CampanaExtraccion>();
        campana.ventana = ventana;
        campana.perilla = perilla;
        campana.visor = visor;
        campana.luz = luz.GetComponent<Renderer>();
        campana.luzVerde = mVerdeLuz;
        campana.luzRoja = mRojoLuz;
        campana.luzApagada = mLedApagado;
        campana.luzInterior = luzInterior;
        campana.extractor = AudioEn("Audio_Extractor", g, new Vector3(0.43f, 2.4f, Z));
        campana.extractor.loop = true;
        campana.datos = Datos("Cuarto4_Campana", "cuarto4_campana", TipoAcertijo.Ventilacion, "vidrio abajo + extractor 3",
            "El protocolo de la línea de gas: bajar el vidrio de la campana y poner el extractor en 3.", "EXTRACCIÓN OK", "");
        campana.sonidoAcierto = AssetDatabase.LoadAssetAtPath<AudioClip>(RUTA_ACIERTO);
        return campana;
    }

    // Perilla de 0 a 3 (un cuarto de vuelta por punto) que se agarra y se gira con la mano.
    // Es la XRKnob de la plantilla de VR de Unity: gira sobre su Y, que acá apunta al cuarto.
    static XRKnob ArmarPerilla(Transform p, Vector3 pos)
    {
        Transform b = Grupo("Perilla", p);
        b.localPosition = pos;
        b.localRotation = Quaternion.LookRotation(Vector3.up, Vector3.right);   // su Y hacia +X (el cuarto)
        Transform mando = Grupo("Mando", b);
        Cilindro("Cuerpo", mando, new Vector3(0f, 0.012f, 0f), new Vector3(0.05f, 0.012f, 0.05f), mNegro);
        Cubo("Marca", mando, new Vector3(0f, 0.025f, 0.016f), new Vector3(0.006f, 0.003f, 0.02f), mBlanco);
        // Los números alrededor: 0 arriba y en el sentido del reloj, visto de frente
        for (int i = 0; i < 4; i++)
        {
            float a = i * 90f * Mathf.Deg2Rad;
            Vector3 lugar = new Vector3(Mathf.Sin(a) * 0.042f, 0.001f, Mathf.Cos(a) * 0.042f);
            Texto("Numero_" + i, b, lugar, Vector3.up, i.ToString(), new Vector2(0.018f, 0.018f), Color.white, Vector3.forward);
        }

        var col = b.gameObject.AddComponent<SphereCollider>();
        col.center = new Vector3(0f, 0.015f, 0f);
        col.radius = 0.035f;
        var perilla = b.gameObject.AddComponent<XRKnob>();
        perilla.handle = mando;
        perilla.minAngle = 0f;
        perilla.maxAngle = 270f;
        perilla.clampedMotion = true;
        perilla.positionTrackedRadius = 0.03f;
        var so = new SerializedObject(perilla);
        so.FindProperty("m_AngleIncrement").floatValue = 90f;   // se traba en cada número
        so.FindProperty("m_Value").floatValue = 0f;
        so.ApplyModifiedPropertiesWithoutUndo();
        b.gameObject.AddComponent<ResaltarAlApuntar>();
        return perilla;
    }

    // ------------------------------------------------------------------ acertijo 3: gas

    // La línea de gas en la pared Este: dos bajadas de caño amarillo (el color normalizado del
    // gas), cada una con su válvula de compuerta (Sketchfab), su manómetro y su tarjeta de
    // bloqueo. Arriba, el protocolo de seguridad.
    static LineaGas ArmarLineaGas(Transform p)
    {
        Transform g = Grupo("Linea_Gas", p);
        const float X = PARED_E - 0.115f;   // eje de los caños, a 11.5 cm de la pared
        float[] zs = { 3.0f, 4.1f };
        float[] alturas = { 1.05f, 1.35f };

        // Caño de arriba que une las dos bajadas y sigue por el techo hacia las mesadas
        Cilindro("Colector", g, new Vector3(X, 2.95f, 3.55f), new Vector3(0.06f, 0.55f + 0.03f, 0.06f), mGas)
            .transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        Cilindro("Ramal_Techo", g, new Vector3((X + 4.4f) / 2f, 2.95f, 3.55f), new Vector3(0.05f, (X - 4.4f) / 2f, 0.05f), mGas)
            .transform.localRotation = Quaternion.Euler(0f, 0f, 90f);

        var valvulas = new ValvulaGas[2];
        for (int i = 0; i < 2; i++)
        {
            Vector3 eje = new Vector3(X, alturas[i], zs[i]);
            valvulas[i] = ArmarValvula(g, "V" + (i + 1), eje);
            // El caño, cortado donde está la válvula (las bridas miden 20 cm de punta a punta)
            float abajo = alturas[i] - 0.1f, arriba = alturas[i] + 0.1f;
            Cilindro("Cano_Bajo", g, new Vector3(X, abajo / 2f, zs[i]), new Vector3(0.06f, abajo / 2f, 0.06f), mGas);
            Cilindro("Cano_Alto", g, new Vector3(X, (arriba + 2.95f) / 2f, zs[i]), new Vector3(0.06f, (2.95f - arriba) / 2f, 0.06f), mGas);
            foreach (float y in new[] { 0.4f, 2.3f })
                Cubo("Grampa", g, new Vector3((X + PARED_E) / 2f, y, zs[i]), new Vector3(PARED_E - X, 0.03f, 0.08f), mAceroOscuro);
        }

        // El protocolo de seguridad (la pista del acertijo 2 y del 3): un panel moderno, con el
        // encabezado azul de las señales de obligación, los pasos numerados y el aviso en rojo
        Transform cartel = Grupo("Protocolo_Gas", g);
        cartel.localPosition = new Vector3(PARED_E - 0.005f, 1.95f, 3.55f);
        cartel.localRotation = Quaternion.LookRotation(Vector3.left);
        Cubo("Placa", cartel, new Vector3(0f, 0f, 0.004f), new Vector3(0.62f, 0.56f, 0.008f), mBlanco);
        Cubo("Encabezado", cartel, new Vector3(0f, 0.235f, 0.0085f), new Vector3(0.62f, 0.09f, 0.001f), mAzulSenal);
        Texto("Titulo", cartel, new Vector3(0f, 0.235f, 0.0095f), Vector3.forward,
              "<b>PROTOCOLO DE SEGURIDAD</b>\n<size=65%>LÍNEA DE GAS DEL LABORATORIO</size>", new Vector2(0.56f, 0.075f), Color.white);
        TextMeshPro pasosGas = Texto("Pasos", cartel, new Vector3(0f, -0.02f, 0.0085f), Vector3.forward,
              "<color=#1d4ed8><b>1</b></color>   Bajar el vidrio de la campana de extracción.\n\n" +
              "<color=#1d4ed8><b>2</b></color>   Poner el extractor en velocidad <b>3</b>.\n\n" +
              "<color=#1d4ed8><b>3</b></color>   Abrir <b>V1</b> y <b>V2</b>: girar el volante en sentido\n" +
              "      horario (a la derecha) hasta el tope.",
              new Vector2(0.56f, 0.3f), new Color(0.1f, 0.12f, 0.16f));
        pasosGas.alignment = TextAlignmentOptions.Left;
        Cubo("Aviso", cartel, new Vector3(0f, -0.23f, 0.0085f), new Vector3(0.62f, 0.07f, 0.001f), mRojoSenal);
        Texto("Texto_Aviso", cartel, new Vector3(0f, -0.23f, 0.0095f), Vector3.forward,
              "<b>SIN EXTRACCIÓN, LAS LLAVES QUEDAN BLOQUEADAS</b>", new Vector2(0.56f, 0.04f), Color.white);

        var linea = g.gameObject.AddComponent<LineaGas>();
        linea.valvulas = valvulas;
        linea.datos = Datos("Cuarto4_Gas", "cuarto4_gas", TipoAcertijo.Gas, "V1 + V2",
            "Con la campana andando, se abren las dos llaves girando el volante hasta el tope.", "GAS ABIERTO", "");
        linea.sonidoAcierto = AssetDatabase.LoadAssetAtPath<AudioClip>(RUTA_ACIERTO);

        // El susto salta a mitad de la segunda llave: el jugador tiene la mano ocupada y la
        // mesa de disección le queda a la espalda
        valvulas[1].puntoDelMedio = 0.5f;
        if (susto != null) UnityEventTools.AddVoidPersistentListener(valvulas[1].alMitad, new UnityAction(susto.Disparar));
        foreach (ValvulaGas v in valvulas) EditorUtility.SetDirty(v);
        return linea;
    }

    // Una válvula de compuerta montada en la bajada de gas, con el vástago hacia el cuarto y
    // el volante de frente al jugador. El volante se separa del modelo y cuelga de una perilla
    // XRKnob, que lo gira dos vueltas completas.
    static ValvulaGas ArmarValvula(Transform p, string nombre, Vector3 eje)
    {
        Transform g = Grupo("Valvula_" + nombre, p);
        g.localPosition = eje;

        // El modelo mide 59 unidades de la base a la punta del vástago; el caño pasa a 8.5 y el
        // volante está a 44.3. Se lo gira para que el caño quede vertical y el vástago mire al cuarto.
        const float s = 0.4f / 59.06f;
        Quaternion giro = Quaternion.LookRotation(Vector3.up, Vector3.left);
        Transform baseVolante = Grupo("Volante_Base", g);
        baseVolante.localPosition = giro * new Vector3(0f, 44.3f - 8.5f, 0f) * s;
        baseVolante.localRotation = giro;   // su Y apunta al cuarto: es el eje del volante
        Transform volante = Grupo("Volante", baseVolante);

        GameObject fuente = AssetDatabase.LoadAssetAtPath<GameObject>(CARPETA_SKETCHFAB + "/valvula_gas.glb");
        if (fuente != null)
        {
            GameObject modelo = Instanciar(fuente, g);
            // Se suma el giro al que ya trae la raíz (el paso de Z arriba a Y arriba de Sketchfab)
            modelo.transform.localRotation = giro * modelo.transform.localRotation;
            modelo.transform.localScale = modelo.transform.localScale * s;
            modelo.transform.localPosition = -(giro * new Vector3(0f, 8.5f, 0f)) * s;
            // Para que el volante gire solo, se lo saca del modelo y se lo cuelga de la perilla
            PrefabUtility.UnpackPrefabInstance(modelo, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            Transform rueda = BuscarHijo(modelo.transform, "helm_01");
            if (rueda != null) rueda.SetParent(volante, true);
        }
        else
        {
            Cilindro("Cuerpo", g, Vector3.zero, new Vector3(0.1f, 0.1f, 0.1f), mRojoLuz);
            Cilindro("Rueda", volante, Vector3.zero, new Vector3(0.18f, 0.012f, 0.18f), mRojoLuz);
        }

        var col = baseVolante.gameObject.AddComponent<SphereCollider>();
        col.radius = 0.1f;
        var perilla = baseVolante.gameObject.AddComponent<XRKnob>();
        perilla.handle = volante;
        perilla.minAngle = 0f;
        perilla.maxAngle = 720f;   // dos vueltas
        perilla.clampedMotion = true;
        perilla.positionTrackedRadius = 0.05f;
        var so = new SerializedObject(perilla);
        so.FindProperty("m_Value").floatValue = 0f;
        so.ApplyModifiedPropertiesWithoutUndo();
        baseVolante.gameObject.AddComponent<ResaltarAlApuntar>();

        // Manómetro arriba de la válvula, con la esfera hacia el cuarto
        Transform mano = Grupo("Manometro", g);
        mano.localPosition = new Vector3(-0.07f, 0.26f, 0f);
        Cilindro("Conexion", g, new Vector3(-0.035f, 0.26f, 0f), new Vector3(0.015f, 0.035f, 0.015f), mLaton)
            .transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        Cilindro("Caja", mano, Vector3.zero, new Vector3(0.11f, 0.015f, 0.11f), mAcero).transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        Cilindro("Esfera", mano, new Vector3(-0.016f, 0f, 0f), new Vector3(0.095f, 0.001f, 0.095f), mEsfera).transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        Texto("Escala", mano, new Vector3(-0.018f, -0.022f, 0f), Vector3.left, "GAS · bar", new Vector2(0.06f, 0.015f), new Color(0.1f, 0.1f, 0.1f));
        Transform aguja = Grupo("Aguja", mano);
        aguja.localPosition = new Vector3(-0.019f, 0f, 0f);
        aguja.localRotation = Quaternion.LookRotation(Vector3.left) * Quaternion.Euler(0f, 0f, 120f);
        Cubo("Punta", aguja, new Vector3(0f, 0.018f, 0f), new Vector3(0.004f, 0.038f, 0.002f), mRojoLuz);

        // Tarjetas colgadas del caño, debajo del volante: bloqueada o habilitada
        Transform bloqueo = Tarjeta(g, "Tarjeta_Bloqueo", mRojoLuz, "BLOQUEADA\n<size=70%>sin extracción</size>");
        Transform habilitada = Tarjeta(g, "Tarjeta_Habilitada", mVerdeLuz, "HABILITADA");
        Cartel(g, "Rotulo_" + nombre, new Vector3(-0.035f, 0.42f, 0f), Vector3.left, nombre, 0.1f, 0.07f, mNegro, Color.white);

        var valvula = g.gameObject.AddComponent<ValvulaGas>();
        valvula.volante = perilla;
        valvula.aguja = aguja;
        valvula.siseo = AudioEn("Audio_Siseo", g, Vector3.zero);
        valvula.siseo.loop = true;
        valvula.siseo.volume = 0.35f;
        valvula.tarjetaBloqueo = bloqueo.gameObject;
        valvula.tarjetaHabilitada = habilitada.gameObject;
        return valvula;
    }

    static Transform Tarjeta(Transform p, string nombre, Material color, string texto)
    {
        Transform t = Grupo(nombre, p);
        t.localPosition = new Vector3(-0.045f, -0.19f, 0f);
        t.localRotation = Quaternion.LookRotation(Vector3.left);
        Cubo("Carton", t, Vector3.zero, new Vector3(0.09f, 0.12f, 0.003f), color);
        Texto("Texto", t, new Vector3(0f, 0f, 0.002f), Vector3.forward, texto, new Vector2(0.08f, 0.1f), Color.white);
        Cubo("Hilo", t, new Vector3(0f, 0.075f, 0f), new Vector3(0.003f, 0.03f, 0.003f), mNegro);
        return t;
    }

    // ------------------------------------------------------------------ la mesa de disección y el susto

    // La mesa de disección (Sketchfab), con ruedas, junto a la línea de gas: le queda a la espalda
    // al jugador mientras abre las llaves. Encima, un cuerpo tapado con una sábana manchada de
    // sangre (Sketchfab). El susto (SustoCamilla): apagón, la camilla rueda sola y al volver la luz
    // el cuerpo ya no está: la sábana quedó en el piso y un rastro de sangre se aleja.
    // "Mesa_Rodante" (mesa, cuerpo, ficha y la sangre de la mesa) es lo que se mueve; las manchas
    // del piso, la sábana caída, el rastro y el foco del techo quedan fijos.
    static SustoCamilla ArmarCamilla(Transform p, Light luzCampana)
    {
        Transform g = Grupo("Camilla", p);
        g.localPosition = new Vector3(6.45f, 0f, 3.55f);
        Transform mesa = Grupo("Mesa_Rodante", g);
        if (ModeloSketchfab("mesa_diseccion", mesa, Vector3.zero, 0f, 0.9f, Apoyo.Piso, true) == null)
            Cubo("Tabla", mesa, new Vector3(0f, 0.45f, 0f), new Vector3(0.9f, 0.9f, 1.95f), mAcero, true);

        // Sangre seca sobre la mesa: queda a la vista cuando el cuerpo desaparece
        Mancha("Sangre_Mesa", mesa, new Vector3(0.05f, 0.885f, -0.2f), 0.6f, 1.1f, 15f);   // la tapa está a 0.882 (el borde, a 0.9)
        GameObject cuerpo = ArmarCuerpoCubierto(mesa);

        // La sábana tirada en el piso (aparece con el susto), donde estaba la cabecera
        GameObject caida = new GameObject("Sabana_Caida");
        caida.transform.SetParent(g, false);
        caida.transform.localPosition = new Vector3(0.1f, 0f, -1.1f);
        caida.transform.localRotation = Quaternion.Euler(0f, 18f, 0f);
        caida.AddComponent<MeshFilter>().sharedMesh = GuardarMalla(MallaSabanaCaida(), "C4_Malla_Sabana_Caida");
        caida.AddComponent<MeshRenderer>().sharedMaterial = mSabanaGris;

        // El rastro de sangre que se aleja de la camilla hacia el fondo (aparece con el susto)
        Transform rastro = Grupo("Rastro_Sangre", g);
        float[,] gotas = { { 0.25f, 1.25f, 0.34f }, { 0.32f, 1.7f, 0.26f }, { 0.2f, 2.15f, 0.3f },
                           { 0.36f, 2.6f, 0.22f }, { 0.25f, 3.05f, 0.25f }, { 0.4f, 3.5f, 0.18f } };
        for (int i = 0; i < gotas.GetLength(0); i++)
            Mancha("Gota_" + (i + 1), rastro, new Vector3(gotas[i, 0], 0.006f + i * 0.0003f, gotas[i, 1]),
                   gotas[i, 2], gotas[i, 2] * 1.6f, i * 37f);

        // Ficha de la práctica colgada de la mesa y sangre seca en el piso
        Transform ficha = Grupo("Ficha", mesa);
        ficha.localPosition = new Vector3(0f, 0.72f, -1.02f);
        Cubo("Carton", ficha, Vector3.zero, new Vector3(0.3f, 0.2f, 0.006f), mPapel);
        Texto("Texto", ficha, new Vector3(0f, 0f, -0.004f), Vector3.back, "PRÁCTICA\nSUSPENDIDA", new Vector2(0.27f, 0.16f), new Color(0.35f, 0.1f, 0.1f));
        Mancha("Mancha_1", g, new Vector3(0.45f, 0.004f, -0.3f), 0.55f, 0.45f, 30f);
        Mancha("Mancha_2", g, new Vector3(-0.3f, 0.0045f, 0.55f), 0.36f, 0.3f, 75f);

        // Foco a batería encima de la mesa: parpadea aunque no haya corriente
        Transform foco = Grupo("Foco_Camilla", g);
        foco.localPosition = new Vector3(0f, ALTO - 0.3f, 0f);
        Cilindro("Pantalla", foco, Vector3.zero, new Vector3(0.32f, 0.05f, 0.32f), mAceroOscuro);
        Cilindro("Bombilla", foco, new Vector3(0f, -0.06f, 0f), new Vector3(0.16f, 0.01f, 0.16f), mVerdeLuz);
        LuzPunto("Luz", foco, new Vector3(0f, -0.2f, 0f), new Color(0.55f, 1f, 0.68f), 1.1f, 3.5f).gameObject.AddComponent<Parpadeo>();

        var s = g.gameObject.AddComponent<SustoCamilla>();
        var luces = new List<Light>(lucesCuarto);
        if (luzCampana != null) luces.Add(luzCampana);
        s.luces = luces.ToArray();
        s.encendidos = conEnergia.ToArray();   // los tubos de las lámparas (ArmarLamparas ya los armó)
        s.cuerpo = cuerpo;
        s.aparecen = new[] { caida, rastro.gameObject };
        s.camilla = mesa;
        // Rueda hacia el fondo y gira un poco: nunca se acerca a la pared del gas (donde está el
        // jugador) ni choca con la mesada
        s.corrimiento = new Vector3(0f, 0f, 0.6f);
        s.giro = 10f;
        s.ruido = AudioEn("Audio_Ruedas", g, new Vector3(0f, 0.5f, 0f));
        s.ruido.volume = 1f;
        s.ruido.spatialBlend = 0.7f;   // se oye fuerte aunque la camilla esté a la espalda
        EditorUtility.SetDirty(s);
        return s;
    }

    // El cuerpo tapado con la sábana ensangrentada (Sketchfab). El modelo viene acostado en el
    // piso, con la sábana extendida y la cabeza hacia su +X: se lo achica para que la sábana
    // calce justo en la mesa y se lo gira con la cabeza hacia la entrada. Si falta el modelo,
    // se usa la sábana hecha por código, con la forma del cuerpo.
    static GameObject ArmarCuerpoCubierto(Transform mesa)
    {
        Transform c = Grupo("Cuerpo_Cubierto", mesa);
        c.localPosition = new Vector3(0f, 0.882f, 0f);   // sobre la tapa, adentro del borde
        GameObject fuente = AssetDatabase.LoadAssetAtPath<GameObject>(CARPETA_SKETCHFAB + "/cuerpo_cubierto.glb");
        if (fuente != null)
        {
            GameObject modelo = Instanciar(fuente, c);
            if (LimitesLocales(modelo, c, out Bounds b))
            {
                Vector3 ancla = b.center;
                ancla.y = b.min.y;
                modelo.transform.localPosition -= ancla;
                // 1.9 m de largo y 0.9 de ancho (la mesa mide 1.98 x 0.96 con el borde) y el alto en
                // proporción al largo
                float largo = 1.9f / b.size.x;
                c.localScale = new Vector3(largo, largo, 0.9f / b.size.z);
            }
            c.localRotation = Quaternion.Euler(0f, 90f, 0f);
            return c.gameObject;
        }

        GameObject sabana = new GameObject("Sabana");
        sabana.transform.SetParent(c, false);
        sabana.transform.localPosition = new Vector3(0f, 0.023f, 0f);
        sabana.AddComponent<MeshFilter>().sharedMesh = GuardarMalla(MallaSabana(), "C4_Malla_Sabana");
        sabana.AddComponent<MeshRenderer>().sharedMaterial = mSabana;
        return c.gameObject;
    }

    // Sábana sobre la mesa: una grilla de 26 x 50 puntos que se levanta con la forma del cuerpo
    // (cabeza, pecho, cadera, piernas y pies) y cae por los bordes de la mesa, como una tela.
    static Mesh MallaSabana()
    {
        const int NX = 26, NZ = 50;
        const float ANCHO_S = 1.3f, LARGO_S = 2.35f;     // un poco más grande que la mesa
        const float MEDIO_X = 0.485f, MEDIO_Z = 1.0f;    // mitad del ancho y del largo de la mesa
        var alturas = new float[NX, NZ];
        for (int i = 0; i < NX; i++)
            for (int j = 0; j < NZ; j++)
            {
                float x = (i / (NX - 1f) - 0.5f) * ANCHO_S;
                float z = (j / (NZ - 1f) - 0.5f) * LARGO_S;
                float h = 0.012f + Bulto(x, z);
                // Fuera de la mesa, la tela cae
                float afuera = Mathf.Max(Mathf.Abs(x) - MEDIO_X, Mathf.Abs(z) - MEDIO_Z);
                if (afuera > 0f) h = Mathf.Min(h, 0.012f) - Mathf.Min(0.32f, afuera * 3.2f);
                alturas[i, j] = h;
            }
        // Se suaviza unas veces: así las formas se juntan como una tela y no como bloques
        for (int paso = 0; paso < 3; paso++)
            for (int i = 1; i < NX - 1; i++)
                for (int j = 1; j < NZ - 1; j++)
                    alturas[i, j] = Mathf.Lerp(alturas[i, j], (alturas[i - 1, j] + alturas[i + 1, j] + alturas[i, j - 1] + alturas[i, j + 1]) / 4f, 0.5f);

        var puntos = new List<Vector3>();
        var uv = new List<Vector2>();
        var triangulos = new List<int>();
        for (int j = 0; j < NZ; j++)
            for (int i = 0; i < NX; i++)
            {
                puntos.Add(new Vector3((i / (NX - 1f) - 0.5f) * ANCHO_S, alturas[i, j], (j / (NZ - 1f) - 0.5f) * LARGO_S));
                uv.Add(new Vector2(i / (NX - 1f), j / (NZ - 1f)));
            }
        for (int j = 0; j < NZ - 1; j++)
            for (int i = 0; i < NX - 1; i++)
            {
                int a = j * NX + i;
                triangulos.AddRange(new[] { a, a + NX, a + 1, a + 1, a + NX, a + NX + 1 });
            }
        var malla = new Mesh { name = "Sabana" };
        malla.SetVertices(puntos);
        malla.SetUVs(0, uv);
        malla.SetTriangles(triangulos, 0);
        malla.RecalculateNormals();
        malla.RecalculateBounds();
        return malla;
    }

    // Cuánto levanta el cuerpo a la sábana en cada punto (la cabeza hacia la entrada, -Z)
    static float Bulto(float x, float z)
    {
        float h = 0f;
        h = Mathf.Max(h, Elipse(x, z, 0f, -0.8f, 0.12f, 0.13f, 0.22f));      // cabeza
        h = Mathf.Max(h, Elipse(x, z, 0f, -0.42f, 0.25f, 0.32f, 0.28f));     // pecho
        h = Mathf.Max(h, Elipse(x, z, 0f, 0.02f, 0.22f, 0.22f, 0.23f));      // cadera
        foreach (float lado in new[] { -0.1f, 0.1f })
        {
            h = Mathf.Max(h, Elipse(x, z, lado, 0.45f, 0.09f, 0.4f, 0.17f)); // piernas
            h = Mathf.Max(h, Elipse(x, z, lado, 0.9f, 0.07f, 0.07f, 0.25f)); // pies, para arriba
        }
        return h;
    }

    static float Elipse(float x, float z, float cx, float cz, float rx, float rz, float alto)
    {
        float d = ((x - cx) * (x - cx)) / (rx * rx) + ((z - cz) * (z - cz)) / (rz * rz);
        return d >= 1f ? 0f : alto * Mathf.Sqrt(1f - d);
    }

    // La sábana tirada en el piso: un montón bajo y arrugado
    static Mesh MallaSabanaCaida()
    {
        const int N = 18;
        var puntos = new List<Vector3>();
        var triangulos = new List<int>();
        var azar = new System.Random(4);
        for (int j = 0; j < N; j++)
            for (int i = 0; i < N; i++)
            {
                float x = (i / (N - 1f) - 0.5f) * 1.1f, z = (j / (N - 1f) - 0.5f) * 1.5f;
                float r = (x * x) / (0.55f * 0.55f) + (z * z) / (0.75f * 0.75f);
                float y = r >= 1f ? 0.004f : 0.004f + (1f - r) * 0.13f + (float)azar.NextDouble() * 0.035f * (1f - r);
                puntos.Add(new Vector3(x, y, z));
            }
        for (int j = 0; j < N - 1; j++)
            for (int i = 0; i < N - 1; i++)
            {
                int a = j * N + i;
                triangulos.AddRange(new[] { a, a + N, a + 1, a + 1, a + N, a + N + 1 });
            }
        var malla = new Mesh { name = "Sabana_Caida" };
        malla.SetVertices(puntos);
        malla.SetTriangles(triangulos, 0);
        malla.RecalculateNormals();
        malla.RecalculateBounds();
        return malla;
    }

    // Guarda una malla hecha por código como asset (si no, se perdería al cerrar la escena)
    static Mesh GuardarMalla(Mesh malla, string nombre)
    {
        string ruta = "Assets/Materials/Cuarto4/" + nombre + ".asset";
        var existente = AssetDatabase.LoadAssetAtPath<Mesh>(ruta);
        if (existente != null)
        {
            existente.Clear();
            EditorUtility.CopySerialized(malla, existente);
            EditorUtility.SetDirty(existente);
            return existente;
        }
        AssetDatabase.CreateAsset(malla, ruta);
        return malla;
    }

    // ------------------------------------------------------------------ acertijo 4: ensayo a la llama

    // Sobre el lado norte de la mesada: tres mecheros Bunsen (Poly Haven), la gradilla con las
    // tres muestras y la hoja de la práctica. En la pizarra de la pared Oeste quedó la clase.
    // En la mesada también quedó la tarjeta de un alumno (no abre la salida: solo la del docente).
    static void ArmarEnsayoLlama(Transform p, ItemData tarjetaAlumno)
    {
        Transform g = Grupo("Ensayo_Llama", p);
        const float Z = 2.85f;   // cerca del borde de la mesada: se llega a la llama sin estirarse
        int n = 1;
        foreach (float x in new[] { 3.35f, 3.85f, 4.35f })
            mecheros.Add(ArmarMechero(g, n++, new Vector3(x, MESADA, Z)));

        // Gradilla de madera con las tres asas paradas, la punta con la sal para arriba
        Transform gradilla = Grupo("Gradilla", g);
        gradilla.localPosition = new Vector3(4.9f, MESADA, Z);
        Cubo("Bloque", gradilla, new Vector3(0f, 0.025f, 0f), new Vector3(0.26f, 0.05f, 0.08f), mMadera, true);
        // Muestra 1 = cobre (verde), 2 = sodio (amarillo), 3 = litio (rojo)
        Color[] colores = { new Color(0.2f, 1f, 0.35f), new Color(1f, 0.78f, 0.1f), new Color(1f, 0.12f, 0.2f) };
        for (int i = 0; i < 3; i++)
        {
            float x = (i - 1) * 0.08f;
            Cilindro("Agujero", gradilla, new Vector3(x, 0.0505f, 0f), new Vector3(0.018f, 0.001f, 0.018f), mNegro);
            Texto("Numero", gradilla, new Vector3(x, 0.025f, -0.041f), Vector3.back, (i + 1).ToString(), new Vector2(0.04f, 0.04f), Color.white);
            ArmarMuestra(g, i + 1, colores[i], new Vector3(4.9f + x, MESADA + 0.09f, Z));
        }
        // Respaldo con el rótulo, por encima de los mangos (las varillas finas no lo tapan)
        Cubo("Respaldo", gradilla, new Vector3(0f, 0.13f, 0.043f), new Vector3(0.26f, 0.16f, 0.006f), mMadera);
        Texto("Rotulo", gradilla, new Vector3(0f, 0.185f, 0.039f), Vector3.back, "MUESTRAS", new Vector2(0.22f, 0.035f), Color.white);

        // La práctica, en un atril sobre la mesada a la izquierda de los mecheros: qué hacer y
        // la tabla de colores, justo donde se hace el ensayo (antes estaba solo en la pizarra)
        Transform atril = Grupo("Atril_Practica", g);
        atril.localPosition = new Vector3(2.72f, MESADA, Z + 0.05f);
        Cubo("Pie", atril, new Vector3(0f, 0.006f, 0.05f), new Vector3(0.34f, 0.012f, 0.14f), mAceroOscuro);
        Transform hoja = Grupo("Hoja", atril);
        hoja.localPosition = new Vector3(0f, 0.012f, 0f);
        hoja.localRotation = Quaternion.Euler(14f, 0f, 0f);   // echada un poco hacia atrás, como un atril
        Cubo("Tabla", hoja, new Vector3(0f, 0.21f, 0.006f), new Vector3(0.4f, 0.42f, 0.008f), mAceroOscuro);
        Cubo("Papel", hoja, new Vector3(0f, 0.21f, 0f), new Vector3(0.38f, 0.4f, 0.002f), mPapel);
        Texto("Texto", hoja, new Vector3(0f, 0.21f, -0.0015f), Vector3.back,
              "<b>PRÁCTICA N° 7 · ENSAYO A LA LLAMA</b>\n\n<align=left><size=80%>" +
              "1. Tomar una muestra de la gradilla (1, 2 o 3).\n" +
              "2. Meter la punta en la llama de un mechero.\n" +
              "3. Mirar el color de la llama y buscar el metal:</size></align>\n\n" +
              "<b><color=#2e7d32>VERDE = COBRE (Cu)</color>\n" +
              "<color=#b07d00>AMARILLA = SODIO (Na)</color>\n" +
              "<color=#c62828>ROJA = LITIO (Li)</color>\n" +
              "<color=#6a1b9a>VIOLETA = POTASIO (K)</color></b>\n\n" +
              "<size=75%>Con el metal de cada muestra se bajan las\npalancas del cierre de la salida (pared del fondo).</size>",
              new Vector2(0.35f, 0.37f), new Color(0.12f, 0.12f, 0.2f));

        // La tarjeta de un alumno, olvidada en la mesada
        ArmarTarjeta(g, "Alumno", tarjetaAlumno, "ALUMNO", "Lucas Ferreyra", "3° año B", mVerdeSenal,
                     new Vector3(2.25f, MESADA + 0.002f, Z - 0.1f), -12f, true);

        // La pizarra con la clase: la misma tabla, en grande
        if (ModeloSketchfab("whiteboard", g, new Vector3(PARED_O + 0.005f, 1.58f, 3.6f), 90f, 1.06f, Apoyo.Pared, false) == null)
            Cubo("Pizarra", g, new Vector3(PARED_O + 0.01f, 1.58f, 3.6f), new Vector3(0.02f, 1.06f, 1.65f), mBlanco);
        Texto("Clase", g, new Vector3(PARED_O + 0.035f, 1.6f, 3.6f), Vector3.right,
              "<b>ENSAYO A LA LLAMA</b>   <size=70%>3° año · química</size>\n\n" +
              "Cada metal tiñe la llama de un color:\n" +
              "<color=#c62828>Litio (Li): llama ROJA</color>\n" +
              "<color=#b07d00>Sodio (Na): llama AMARILLA</color>\n" +
              "<color=#2e7d32>Cobre (Cu): llama VERDE</color>\n" +
              "<color=#6a1b9a>Potasio (K): llama VIOLETA</color>\n\n" +
              "<b>Tarea:</b> identificar las muestras 1, 2 y 3",
              new Vector2(1.45f, 0.82f), new Color(0.1f, 0.15f, 0.45f));
    }

    // Un mechero Bunsen con su llama (apagada hasta que llegue el gas) y la zona de la llama
    // donde se meten las muestras. La llama son dos gotas (exterior e interior) con un material
    // que suma luz: se ve como fuego azul, y tiembla (Mechero).
    static Mechero ArmarMechero(Transform p, int numero, Vector3 pos)
    {
        const float ALTO_MECHERO = 0.16f;
        if (ModeloPolyHaven("bunsen_burner", p, pos, 30f, ALTO_MECHERO, Apoyo.Piso, false) == null)
        {
            Cilindro("Mechero_Base", p, pos + new Vector3(0f, 0.005f, 0f), new Vector3(0.09f, 0.005f, 0.09f), mAceroOscuro);
            Cilindro("Mechero_Tubo", p, pos + new Vector3(0f, 0.08f, 0f), new Vector3(0.025f, 0.075f, 0.025f), mAcero);
        }

        Transform m = Grupo("Mechero_" + numero, p);
        m.localPosition = pos + Vector3.up * ALTO_MECHERO;
        Transform llama = Grupo("Llama", m);
        GameObject exterior = Pieza("Llama_Exterior", llama, Vector3.zero, new Vector3(0.045f, 0.12f, 0.045f), mallaLlama, mLlamaExterior);
        GameObject interior = Pieza("Llama_Interior", llama, new Vector3(0f, 0.002f, 0f), new Vector3(0.022f, 0.05f, 0.022f), mallaLlama, mLlamaInterior);
        Light luz = LuzPunto("Luz", llama, new Vector3(0f, 0.06f, 0f), new Color(0.35f, 0.6f, 1f), 1.2f, 1.1f);

        // Zona generosa (10 cm alrededor de la llama): con el simulador o con la mano, alcanza con
        // acercar la punta; no hace falta acertarle a la llama exacta
        var zona = m.gameObject.AddComponent<SphereCollider>();
        zona.isTrigger = true;
        zona.center = new Vector3(0f, 0.08f, 0f);
        zona.radius = 0.1f;
        var mechero = m.gameObject.AddComponent<Mechero>();
        mechero.llama = llama.gameObject;
        mechero.partesLlama = new[] { interior.GetComponent<Renderer>(), exterior.GetComponent<Renderer>() };
        mechero.luz = luz;
        mechero.sonido = AudioEn("Audio_Mechero", m, Vector3.zero);
        mechero.sonido.loop = true;
        mechero.sonido.volume = 0.2f;
        return mechero;
    }

    // Un asa de muestra: mango con su número, varilla y la punta con la sal (polvo blanco,
    // igual en las tres). Se agarra del mango con la punta hacia adelante.
    static void ArmarMuestra(Transform p, int numero, Color colorLlama, Vector3 pos)
    {
        Transform a = Grupo("Muestra_" + numero, p);
        a.localPosition = pos;
        a.localRotation = Quaternion.LookRotation(Vector3.up, Vector3.back);   // parada, la punta arriba
        Cilindro("Mango", a, Vector3.zero, new Vector3(0.014f, 0.06f, 0.014f), mPlastico).transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        Cubo("Etiqueta", a, new Vector3(0f, 0.0075f, -0.02f), new Vector3(0.012f, 0.002f, 0.03f), mPapel);
        Texto("Numero", a, new Vector3(0f, 0.0088f, -0.02f), Vector3.up, numero.ToString(), new Vector2(0.012f, 0.014f),
              new Color(0.1f, 0.1f, 0.1f), Vector3.forward);
        Cilindro("Varilla", a, new Vector3(0f, 0f, 0.11f), new Vector3(0.003f, 0.05f, 0.003f), mAcero).transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        Esfera("Punta", a, new Vector3(0f, 0f, 0.165f), Vector3.one * 0.013f, mSal);

        var col = a.gameObject.AddComponent<BoxCollider>();
        col.center = new Vector3(0f, 0f, 0.05f);
        col.size = new Vector3(0.02f, 0.02f, 0.23f);
        Transform punto = PuntoDeAgarre(a, new Vector3(0f, 0f, -0.01f), Vector3.forward, Vector3.up);
        HacerAgarrable(a.gameObject, punto, pos + Vector3.up * 0.15f, false);
        a.gameObject.AddComponent<MuestraLlama>().colorLlama = colorLlama;
    }

    // Panel del cierre de la salida en la pared del fondo: tres palancas de cuchilla
    // (Sketchfab) con el símbolo de un metal cada una. Van en el orden de las muestras.
    static SecuenciaPalancas ArmarPalancas(Transform p)
    {
        Transform g = Grupo("Panel_Palancas", p);
        const float Z = PARED_S - 0.02f;
        Cubo("Tablero", g, new Vector3(4.1f, 1.3f, Z), new Vector3(2.3f, 1.05f, 0.02f), mAceroOscuro, true);
        Texto("Titulo", g, new Vector3(4.1f, 1.74f, Z - 0.011f), Vector3.back, "CIERRE DE LA SALIDA DE EMERGENCIA",
              new Vector2(2.1f, 0.08f), Color.white);
        // La regla, dicha sin números de palanca: antes decía "1 → 2 → 3" y las palancas tenían
        // P1, P2 y P3, así que parecía que había que bajarlas de izquierda a derecha
        Texto("Instruccion", g, new Vector3(4.1f, 1.645f, Z - 0.011f), Vector3.back,
              "Bajar primero la palanca del metal de la MUESTRA 1, después la de la MUESTRA 2 y al final la de la MUESTRA 3",
              new Vector2(2.1f, 0.05f), new Color(1f, 0.72f, 0.25f));
        Texto("Ayuda", g, new Vector3(4.1f, 1.58f, Z - 0.011f), Vector3.back,
              "El metal de cada muestra se descubre con el ensayo a la llama, en la mesada",
              new Vector2(1.6f, 0.035f), new Color(0.85f, 0.85f, 0.85f));

        // Cada palanca con su metal: símbolo grande y nombre (P1 = Na, P2 = Li, P3 = Cu)
        string[] simbolos = { "Na", "Li", "Cu" };
        string[] nombres = { "SODIO", "LITIO", "COBRE" };
        float[] xs = { 3.45f, 4.1f, 4.75f };
        var secuencia = g.gameObject.AddComponent<SecuenciaPalancas>();
        var palancas = new PalancaCuchilla[3];
        for (int i = 0; i < 3; i++)
        {
            palancas[i] = ArmarPalanca(g, i + 1, new Vector3(xs[i], 1.22f, Z - 0.011f), secuencia);
            Transform placa = Grupo("Placa_" + simbolos[i], g);
            placa.localPosition = new Vector3(xs[i], 0.87f, Z - 0.011f);
            Cubo("Chapa", placa, Vector3.zero, new Vector3(0.2f, 0.13f, 0.004f), mBlanco);
            Texto("Simbolo", placa, new Vector3(0f, 0.018f, -0.003f), Vector3.back, simbolos[i], new Vector2(0.16f, 0.07f), new Color(0.1f, 0.1f, 0.1f));
            Texto("Nombre", placa, new Vector3(0f, -0.04f, -0.003f), Vector3.back, nombres[i], new Vector2(0.17f, 0.03f), new Color(0.25f, 0.25f, 0.25f));
        }

        Cubo("Pantalla", g, new Vector3(5.07f, 1.32f, Z - 0.011f), new Vector3(0.3f, 0.15f, 0.002f), mVisor);
        TextMeshPro visor = Texto("Visor", g, new Vector3(5.07f, 1.32f, Z - 0.013f), Vector3.back, "", new Vector2(0.27f, 0.13f),
                                  new Color(1f, 0.72f, 0.25f));
        GameObject luz = Cubo("Luz", g, new Vector3(5.07f, 1.18f, Z - 0.013f), new Vector3(0.03f, 0.03f, 0.006f), mLedApagado);

        secuencia.palancas = palancas;
        secuencia.orden = new[] { 3, 1, 2 };   // muestra 1 = Cu (P3), 2 = Na (P1), 3 = Li (P2)
        secuencia.luz = luz.GetComponent<Renderer>();
        secuencia.luzVerde = mVerdeLuz;
        secuencia.luzRoja = mRojoLuz;
        secuencia.luzApagada = mLedApagado;
        secuencia.visor = visor;
        secuencia.datos = Datos("Cuarto4_Palancas", "cuarto4_palancas", TipoAcertijo.Secuencia, "Cu Na Li (P3 P1 P2)",
            "Ensayo a la llama: 1 verde = cobre, 2 amarillo = sodio, 3 rojo = litio.", "CIERRE LIBERADO", "SECUENCIA INCORRECTA");
        secuencia.sonidoAcierto = AssetDatabase.LoadAssetAtPath<AudioClip>(RUTA_ACIERTO);
        foreach (PalancaCuchilla pc in palancas) EditorUtility.SetDirty(pc);
        return secuencia;
    }

    // Una palanca de cuchilla de pared. El modelo (Sketchfab) está parado, de frente a su +Z,
    // con la manija arriba; la manija la mueve un hueso ("Bone.001_01") que gira sobre su X.
    static PalancaCuchilla ArmarPalanca(Transform p, int numero, Vector3 pos, SecuenciaPalancas secuencia)
    {
        Transform g = Grupo("Palanca_P" + numero, p);
        g.localPosition = pos;
        g.localRotation = Quaternion.Euler(0f, 180f, 0f);   // de frente al cuarto

        // 290 unidades de placa = 30 cm; la espalda de la placa está a 18.6 unidades
        const float s = 0.3f / 290f;
        Transform manija = null;
        GameObject fuente = AssetDatabase.LoadAssetAtPath<GameObject>(CARPETA_SKETCHFAB + "/palanca_corriente.glb");
        if (fuente != null)
        {
            GameObject modelo = Instanciar(fuente, g);
            modelo.transform.localScale *= s;
            modelo.transform.localPosition = new Vector3(0f, 0f, 18.6f * s);
            manija = BuscarHijo(modelo.transform, "Bone.001_01");
            // La malla se deforma con el hueso: que calcule su caja cada cuadro, así no desaparece al moverse
            foreach (SkinnedMeshRenderer piel in modelo.GetComponentsInChildren<SkinnedMeshRenderer>())
                piel.updateWhenOffscreen = true;
            // El modelo trae una animación: se apaga, porque la manija la mueve la mano (PalancaCuchilla)
            foreach (Animator animador in modelo.GetComponentsInChildren<Animator>()) animador.enabled = false;
            foreach (Animation animacion in modelo.GetComponentsInChildren<Animation>())
            {
                animacion.playAutomatically = false;
                animacion.enabled = false;
            }
        }

        BoxCollider col;
        if (manija != null)
        {
            // En las unidades del hueso (x 0.1 m): la manija va a lo largo de su +Y
            col = manija.gameObject.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, 1.3f, 0f);
            col.size = new Vector3(0.45f, 2.6f, 0.45f);
        }
        else
        {
            Cubo("Placa", g, new Vector3(0f, 0f, 0.01f), new Vector3(0.2f, 0.3f, 0.02f), mAcero);
            manija = Grupo("Manija", g);
            manija.localPosition = new Vector3(0f, 0f, 0.05f);
            Cubo("Brazo", manija, new Vector3(0f, 0.13f, 0f), new Vector3(0.03f, 0.26f, 0.03f), mRojoLuz);
            col = manija.gameObject.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, 0.13f, 0f);
            col.size = new Vector3(0.05f, 0.27f, 0.05f);
        }
        manija.gameObject.AddComponent<XRSimpleInteractable>();
        var palanca = manija.gameObject.AddComponent<PalancaCuchilla>();
        palanca.numero = numero;
        palanca.secuencia = secuencia;
        return palanca;
    }

    // ------------------------------------------------------------------ acertijo 5: la salida

    // El casillero del docente, en la pared del fondo, entre las palancas y la salida: metálico,
    // con su cerradura electrónica (luz roja). Se abre solo cuando las palancas quedan en orden
    // (su puerta es un Door, la misma de todo el juego) y la luz pasa a verde. Adentro, en el
    // estante, está la tarjeta del docente: aparece recién al abrirse, así nadie la saca con la
    // mano a través de la puerta. En sus ejes, +Z mira al cuarto y +X es la izquierda del que mira.
    static Door ArmarCasillero(Transform p, ItemData tarjetaDocente)
    {
        Transform g = Grupo("Casillero_Docente", p);
        g.localPosition = new Vector3(5.56f, 0f, PARED_S);
        g.localRotation = Quaternion.LookRotation(Vector3.back);
        const float A = 0.5f, H = 1.85f, P = 0.48f, ESTANTE = 1.2f;

        Cubo("Fondo", g, new Vector3(0f, H / 2f, 0.01f), new Vector3(A, H, 0.02f), mGrisCasillero, true);
        foreach (float x in new[] { -A / 2f + 0.01f, A / 2f - 0.01f })
            Cubo("Lado", g, new Vector3(x, H / 2f, P / 2f), new Vector3(0.02f, H, P), mGrisCasillero, true);
        Cubo("Techo", g, new Vector3(0f, H - 0.01f, P / 2f), new Vector3(A, 0.02f, P), mGrisCasillero, true);
        Cubo("Zocalo", g, new Vector3(0f, 0.035f, P / 2f), new Vector3(A, 0.07f, P), mAceroOscuro, true);
        Cubo("Estante", g, new Vector3(0f, ESTANTE, P / 2f), new Vector3(A - 0.04f, 0.015f, P - 0.03f), mGrisCasillero, true);
        Cilindro("Barral", g, new Vector3(0f, 1.66f, P / 2f), new Vector3(0.012f, (A - 0.04f) / 2f, 0.012f), mAcero)
            .transform.localRotation = Quaternion.Euler(0f, 0f, 90f);

        // La puerta: gira sobre la bisagra del lado de la salida (la derecha del que mira) y abre
        // hacia el cuarto sin tapar el panel de las palancas
        Transform bisagra = Grupo("Bisagra", g);
        bisagra.localPosition = new Vector3(-A / 2f, 0f, P);
        var puerta = bisagra.gameObject.AddComponent<Door>();
        puerta.anguloAbierto = -95f;
        puerta.duracion = 1.1f;
        Cubo("Hoja", bisagra, new Vector3(A / 2f, H / 2f + 0.03f, 0.01f), new Vector3(A - 0.01f, H - 0.08f, 0.02f), mGrisCasillero, true);
        for (int k = 0; k < 4; k++)
            Cubo("Ventilacion", bisagra, new Vector3(A / 2f, 1.62f + k * 0.03f, 0.0205f), new Vector3(0.3f, 0.01f, 0.002f), mNegro);
        Cubo("Placa", bisagra, new Vector3(A / 2f, 1.43f, 0.0205f), new Vector3(0.26f, 0.09f, 0.002f), mBlanco);
        Texto("Nombre", bisagra, new Vector3(A / 2f, 1.43f, 0.022f), Vector3.forward,
              "<b>DOCENTE</b>  ·  Laboratorio\n<size=65%>Se abre al liberar el cierre de la salida</size>",
              new Vector2(0.24f, 0.075f), new Color(0.1f, 0.12f, 0.16f));
        Cubo("Manija", bisagra, new Vector3(A - 0.05f, 1.0f, 0.035f), new Vector3(0.02f, 0.14f, 0.025f), mAcero);
        Cubo("Cerradura", bisagra, new Vector3(A - 0.05f, 1.17f, 0.027f), new Vector3(0.05f, 0.1f, 0.014f), mNegro);
        GameObject rojo = Cubo("Led_Rojo", bisagra, new Vector3(A - 0.05f, 1.2f, 0.0345f), new Vector3(0.014f, 0.014f, 0.002f), mRojoLuz);
        GameObject verde = Cubo("Led_Verde", bisagra, new Vector3(A - 0.05f, 1.2f, 0.0345f), new Vector3(0.014f, 0.014f, 0.002f), mVerdeLuz);
        verde.SetActive(false);

        // La tarjeta del docente, acostada sobre el estante (en los ejes del cuarto: el casillero está girado)
        Vector3 enEstante = g.localPosition + g.localRotation * new Vector3(0f, ESTANTE + 0.0095f, P / 2f);
        GameObject tarjeta = ArmarTarjeta(p, "Docente", tarjetaDocente, "DOCENTE", "Prof. Marta Ríos", "Laboratorio de Ciencias",
                                          mAzulSenal, enEstante, 0f, false);

        // Al abrirse: luz verde y la tarjeta a la vista
        UnityEventTools.AddBoolPersistentListener(puerta.alAbrirse, new UnityAction<bool>(rojo.SetActive), false);
        UnityEventTools.AddBoolPersistentListener(puerta.alAbrirse, new UnityAction<bool>(verde.SetActive), true);
        UnityEventTools.AddBoolPersistentListener(puerta.alAbrirse, new UnityAction<bool>(tarjeta.SetActive), true);
        EditorUtility.SetDirty(puerta);
        return puerta;
    }

    // Una tarjeta de acceso de PVC (8.6 x 5.4 cm): blanca, con la franja del color de su tipo,
    // la foto, el nombre y el chip. Se agarra como las herramientas (en el PC, con un clic) y se
    // sostiene parada, con el frente hacia el jugador. "visible": si arranca a la vista.
    static GameObject ArmarTarjeta(Transform p, string nombre, ItemData datos, string tipo, string titular, string detalle,
                                   Material franja, Vector3 pos, float giroY, bool visible)
    {
        Transform t = Grupo("Tarjeta_" + nombre, p);
        t.localPosition = pos;
        t.localRotation = Quaternion.Euler(0f, giroY, 0f);
        // En sus ejes: acostada, el frente hacia +Y y la parte de arriba de la tarjeta hacia +Z
        Cubo("Cuerpo", t, Vector3.zero, new Vector3(0.086f, 0.003f, 0.054f), mBlanco);
        Cubo("Franja", t, new Vector3(0f, 0.0016f, 0.018f), new Vector3(0.086f, 0.0005f, 0.018f), franja);
        Texto("Tipo", t, new Vector3(0f, 0.0022f, 0.018f), Vector3.up, "<b>" + tipo + "</b>", new Vector2(0.078f, 0.013f),
              Color.white, Vector3.forward);
        Cubo("Foto", t, new Vector3(-0.028f, 0.0016f, -0.01f), new Vector3(0.02f, 0.0005f, 0.024f), mPlasticoOscuro);
        Texto("Titular", t, new Vector3(0.011f, 0.0022f, -0.006f), Vector3.up, "<b>" + titular + "</b>\n<size=75%>" + detalle + "</size>",
              new Vector2(0.056f, 0.02f), new Color(0.1f, 0.1f, 0.12f), Vector3.forward);
        Cubo("Chip", t, new Vector3(0.028f, 0.0016f, -0.019f), new Vector3(0.012f, 0.0005f, 0.01f), mLaton);

        var col = t.gameObject.AddComponent<BoxCollider>();
        col.size = new Vector3(0.086f, 0.012f, 0.054f);   // más gruesa que la tarjeta: se agarra fácil
        // Se toma del borde de abajo, parada, con el frente mirando al jugador
        Transform punto = PuntoDeAgarre(t, new Vector3(0f, 0f, -0.02f), Vector3.down, Vector3.forward);
        HacerAgarrable(t.gameObject, punto, pos + Vector3.up * 0.1f, false);
        t.gameObject.AddComponent<TarjetaAcceso>().datos = datos;
        t.gameObject.SetActive(visible);
        return t.gameObject;
    }

    // El lector de tarjetas de la salida, en la pared junto a la puerta: un lector sin contacto con
    // su pantallita, el símbolo de acercar la tarjeta, una barra de luz y el cartel. La zona de
    // lectura es una caja invisible delante de él (LectorTarjeta).
    static LectorTarjeta ArmarLector(Transform p, ItemData tarjetaDocente)
    {
        Transform g = Grupo("Lector_Salida", p);
        g.localPosition = new Vector3(7.55f, 1.25f, PARED_S);
        g.localRotation = Quaternion.LookRotation(Vector3.back);   // su +Z mira al cuarto
        Cubo("Cuerpo", g, new Vector3(0f, 0f, 0.015f), new Vector3(0.11f, 0.18f, 0.03f), mNegro);
        Cubo("Pantalla", g, new Vector3(0f, 0.05f, 0.0305f), new Vector3(0.09f, 0.055f, 0.001f), mPantalla);
        TextMeshPro texto = Texto("Texto", g, new Vector3(0f, 0.05f, 0.0315f), Vector3.forward, "", new Vector2(0.085f, 0.05f),
                                  new Color(0.75f, 0.9f, 1f));
        // El símbolo de "acercar la tarjeta": un aro blanco con un punto
        Cilindro("Aro", g, new Vector3(0f, -0.022f, 0.0305f), new Vector3(0.05f, 0.0005f, 0.05f), mBlanco)
            .transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        Cilindro("Aro_Adentro", g, new Vector3(0f, -0.022f, 0.031f), new Vector3(0.042f, 0.0005f, 0.042f), mNegro)
            .transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        Cilindro("Punto", g, new Vector3(0f, -0.022f, 0.0315f), new Vector3(0.014f, 0.0005f, 0.014f), mBlanco)
            .transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        GameObject luz = Cubo("Luz", g, new Vector3(0f, -0.072f, 0.031f), new Vector3(0.08f, 0.008f, 0.002f), mAmbarLuz);
        Cubo("Rotulo", g, new Vector3(0f, -0.17f, 0.003f), new Vector3(0.24f, 0.08f, 0.004f), mVerdeSenal);
        Texto("Texto_Rotulo", g, new Vector3(0f, -0.17f, 0.0055f), Vector3.forward,
              "<b>SALIDA DE EMERGENCIA</b>\n<size=75%>Acercar tarjeta · solo personal docente</size>",
              new Vector2(0.22f, 0.065f), Color.white);

        var zona = g.gameObject.AddComponent<BoxCollider>();
        zona.isTrigger = true;
        zona.center = new Vector3(0f, 0f, 0.09f);
        zona.size = new Vector3(0.22f, 0.26f, 0.16f);
        var lector = g.gameObject.AddComponent<LectorTarjeta>();
        lector.tarjetaValida = tarjetaDocente;
        lector.pantalla = texto;
        lector.luz = luz.GetComponent<Renderer>();
        lector.luzVerde = mVerdeLuz;
        lector.luzRoja = mRojoLuz;
        lector.luzEspera = mAmbarLuz;
        lector.datos = Datos("Cuarto4_Acceso", "cuarto4_acceso", TipoAcertijo.Acceso, "tarjeta del docente",
            "La tarjeta del docente (en su casillero, que se abre al validar el análisis) abre la salida. La del alumno no.",
            "ACCESO CONCEDIDO", "ACCESO DENEGADO");
        lector.sonidoAcierto = AssetDatabase.LoadAssetAtPath<AudioClip>(RUTA_ACIERTO);
        return lector;
    }

    // La puerta de emergencia del fondo: marco, hoja de chapa con barra antipánico y el cartel
    // verde de SALIDA (a batería). Al abrirse se prende la luz blanca de afuera.
    static Door ArmarPuertaSalida(Transform raiz)
    {
        Transform g = Grupo("Puerta_Salida", raiz);
        g.localPosition = new Vector3(SALIDA_X0, 0f, FONDO);
        float ancho = SALIDA_X1 - SALIDA_X0;
        Cubo("Jamba_Izq", g, new Vector3(-0.04f, ALTO_PUERTA / 2f, 0f), new Vector3(0.08f, ALTO_PUERTA, 0.2f), mAceroOscuro, true);
        Cubo("Jamba_Der", g, new Vector3(ancho + 0.04f, ALTO_PUERTA / 2f, 0f), new Vector3(0.08f, ALTO_PUERTA, 0.2f), mAceroOscuro, true);
        Cubo("Dintel", g, new Vector3(ancho / 2f, ALTO_PUERTA + 0.04f, 0f), new Vector3(ancho + 0.16f, 0.08f, 0.2f), mAceroOscuro, true);
        Cubo("Cartel", g, new Vector3(ancho / 2f, ALTO_PUERTA + 0.26f, -0.1f), new Vector3(0.5f, 0.16f, 0.03f), mVerdeLuz);
        Texto("Texto_Cartel", g, new Vector3(ancho / 2f, ALTO_PUERTA + 0.26f, -0.117f), Vector3.back, "SALIDA", new Vector2(0.44f, 0.12f), Color.white);
        LuzPunto("Luz_Cartel", g, new Vector3(ancho / 2f, ALTO_PUERTA + 0.2f, -0.35f), new Color(0.3f, 1f, 0.5f), 0.5f, 2.2f);

        Transform bisagra = Grupo("Bisagra", g);
        var puerta = bisagra.gameObject.AddComponent<Door>();
        puerta.anguloAbierto = -95f;   // se abre hacia afuera del cuarto
        puerta.duracion = 1.4f;
        Cubo("Hoja", bisagra, new Vector3(ancho / 2f, ALTO_PUERTA / 2f, 0f), new Vector3(ancho, ALTO_PUERTA, 0.05f), mAcero, true);
        Cubo("Barra", bisagra, new Vector3(ancho / 2f, 1.05f, -0.07f), new Vector3(ancho - 0.25f, 0.06f, 0.05f), mAceroOscuro);
        Cubo("Franja", bisagra, new Vector3(ancho / 2f, 1.75f, -0.03f), new Vector3(ancho - 0.1f, 0.12f, 0.005f), mVerdeLuz);

        // Del otro lado de la puerta: la luz blanca de la salida, que se prende al abrirla
        Light afuera = LuzPunto("Luz_Afuera", g, new Vector3(ancho / 2f, 1.8f, 0.8f), new Color(1f, 1f, 0.97f), 3f, 6f);
        afuera.gameObject.SetActive(false);
        UnityEventTools.AddBoolPersistentListener(puerta.alAbrirse, new UnityAction<bool>(afuera.gameObject.SetActive), true);
        EditorUtility.SetDirty(puerta);
        return puerta;
    }

    // El final del juego: el patio del colegio, al aire libre, del otro lado de la salida de
    // emergencia. Muros bajos alrededor (se ve el cielo) y, en el muro del fondo, el cartel de la
    // victoria. Al pisar el patio (la zona de la entrada) PantallaVictoria hace de día, muestra el
    // cartel y el botón de volver a jugar, y suena la fanfarria. Mide 7 x 7 m y queda centrado con
    // la puerta; en los ejes del cuarto arranca en la cara de afuera de la pared del fondo.
    static void ArmarPatioSalida(Transform raiz, Door puerta)
    {
        const float X0 = 3f, X1 = 10f, LARGO = 7f, ALTO_MURO = 2.6f;
        const float Z0 = FONDO + MURO, Z1 = Z0 + LARGO;
        const float XC = (X0 + X1) / 2f, ZC = (Z0 + Z1) / 2f;
        Transform g = Grupo("Patio_Salida", raiz);

        // Piso de baldosa: también es zona de teletransporte
        var piso = Cubo("Piso_Patio", g, new Vector3(XC, -0.1f, ZC), new Vector3(X1 - X0, 0.2f, LARGO), mPatio, true);
        var area = piso.AddComponent<TeleportationArea>();
        int capaTeleport = InteractionLayerMask.GetMask("Teleport");
        if (capaTeleport == 0) capaTeleport = 1 << 31;
        area.interactionLayers = capaTeleport;

        // Muros bajos alrededor. El del lado de la puerta es la pared del laboratorio; solo falta
        // cerrar el tramo que pasa el ancho del cuarto
        Cubo("Muro_Oeste", g, new Vector3(X0 - MURO / 2f, ALTO_MURO / 2f, ZC), new Vector3(MURO, ALTO_MURO, LARGO), mMuroPatio, true);
        Cubo("Muro_Este", g, new Vector3(X1 + MURO / 2f, ALTO_MURO / 2f, ZC), new Vector3(MURO, ALTO_MURO, LARGO), mMuroPatio, true);
        Cubo("Muro_Fondo", g, new Vector3(XC, ALTO_MURO / 2f, Z1 + MURO / 2f), new Vector3(X1 - X0 + MURO * 2f, ALTO_MURO, MURO), mMuroPatio, true);
        Cubo("Muro_Cierre", g, new Vector3((ANCHO + X1) / 2f + MURO, ALTO_MURO / 2f, FONDO + MURO / 2f),
             new Vector3(X1 - ANCHO, ALTO_MURO, MURO), mMuroPatio, true);

        // La luz del día sobre el patio: se prende al abrirse la puerta. Antes no, porque las luces
        // sin sombra atraviesan las paredes e iluminarían el laboratorio a oscuras.
        Light sol = LuzPunto("Luz_Patio", g, new Vector3(XC, 3.6f, ZC + 0.8f), new Color(1f, 0.97f, 0.9f), 1.6f, 8f);
        sol.gameObject.SetActive(false);
        UnityEventTools.AddBoolPersistentListener(puerta.alAbrirse, new UnityAction<bool>(sol.gameObject.SetActive), true);

        // El cartel del muro del fondo: la placa oscura se ve siempre, los textos aparecen al ganar
        Cubo("Cartel_Placa", g, new Vector3(XC, 1.75f, Z1 - 0.02f), new Vector3(4.4f, 1.3f, 0.04f), mNegro);
        Transform cartel = Grupo("Cartel_Victoria", g);
        cartel.localPosition = new Vector3(XC, 0f, Z1 - 0.045f);
        Texto("Titulo", cartel, new Vector3(0f, 2.1f, 0f), Vector3.back, "<b>¡LOGRASTE SALIR DEL COLEGIO!</b>",
              new Vector2(4.1f, 0.42f), new Color(0.45f, 1f, 0.55f));
        Texto("Subtitulo", cartel, new Vector3(0f, 1.68f, 0f), Vector3.back, "Resolviste los cuatro cuartos y escapaste a tiempo",
              new Vector2(3.8f, 0.2f), Color.white);
        Texto("Gracias", cartel, new Vector3(0f, 1.35f, 0f), Vector3.back, "Gracias por jugar",
              new Vector2(2.2f, 0.16f), new Color(0.75f, 0.8f, 0.85f));
        LuzPunto("Luz_Cartel", cartel, new Vector3(0f, 1.9f, -0.9f), new Color(1f, 0.95f, 0.85f), 1.5f, 3.5f);

        // El botón de volver a jugar, sobre un pedestal en el medio del patio (aparece al ganar)
        Transform pedestal = Grupo("Boton_Volver", g);
        pedestal.localPosition = new Vector3(XC, 0f, Z1 - 1.8f);
        Cubo("Pedestal", pedestal, new Vector3(0f, 0.5f, 0f), new Vector3(0.36f, 1f, 0.36f), mAceroOscuro, true);
        Cubo("Placa", pedestal, new Vector3(0f, 0.8f, -0.182f), new Vector3(0.32f, 0.12f, 0.004f), mBlanco);
        Texto("Texto", pedestal, new Vector3(0f, 0.8f, -0.186f), Vector3.back, "<b>VOLVER A JUGAR</b>",
              new Vector2(0.3f, 0.1f), new Color(0.1f, 0.12f, 0.16f));
        GameObject tapa = Cilindro("Tapa", pedestal, new Vector3(0f, 1.02f, 0f), new Vector3(0.16f, 0.02f, 0.16f), mVerdeSenal, true);
        tapa.AddComponent<XRSimpleInteractable>();
        var boton = tapa.AddComponent<PressableButton>();
        boton.parteMovil = tapa.transform;
        boton.recorrido = 0.015f;
        tapa.AddComponent<ResaltarAlApuntar>();

        // El clima del patio: de día (nivel 1 = "con las luces prendidas")
        var clima = g.gameObject.AddComponent<ClimaCuarto>();
        clima.luzAmbienteEncendido = new Color(0.62f, 0.66f, 0.72f);
        clima.reflejosEncendido = 1f;
        clima.colorNieblaEncendido = new Color(0.7f, 0.78f, 0.88f);
        clima.densidadNieblaEncendido = 0.004f;
        clima.nivel = 1f;

        var victoria = g.gameObject.AddComponent<PantallaVictoria>();
        victoria.clima = clima;
        victoria.mostrarAlGanar = new[] { cartel.gameObject, pedestal.gameObject };
        UnityEventTools.AddVoidPersistentListener(boton.alPresionar, new UnityAction(victoria.VolverAJugar));

        // La zona apenas pasando la puerta: al pisarla, gana
        Transform zona = Grupo("Zona_Victoria", g);
        zona.localPosition = new Vector3((SALIDA_X0 + SALIDA_X1) / 2f, 1.1f, Z0 + 1.6f);
        var colision = zona.gameObject.AddComponent<BoxCollider>();
        colision.isTrigger = true;
        colision.size = new Vector3(3f, 2.2f, 1.6f);
        var disparador = zona.gameObject.AddComponent<DisparadorJugador>();
        UnityEventTools.AddVoidPersistentListener(disparador.alEntrar, new UnityAction(victoria.Ganar));

        foreach (Object o in new Object[] { area, boton, clima, victoria, disparador, puerta }) EditorUtility.SetDirty(o);
    }

    // ------------------------------------------------------------------ energía y entrada

    static ControlEnergia ArmarControlEnergia(Transform raiz, List<GameObject> sinEnergia)
    {
        Transform g = Grupo("Energia", raiz);
        var control = g.gameObject.AddComponent<ControlEnergia>();
        control.lucesDelCuarto = lucesCuarto.ToArray();
        control.objetosConEnergia = conEnergia.ToArray();
        control.objetosSinEnergia = sinEnergia.ToArray();
        control.efectosSinEnergia = raiz.GetComponentsInChildren<Parpadeo>();
        // Es el cuarto más oscuro: casi sin luz ambiental y con niebla hasta que vuelve la corriente
        control.ambienteSinEnergia = new Color(0.01f, 0.02f, 0.015f);
        control.ambienteConEnergia = new Color(0.46f, 0.5f, 0.48f);
        control.nieblaSinEnergia = new Color(0.008f, 0.022f, 0.014f);
        control.nieblaConEnergia = new Color(0.28f, 0.3f, 0.28f);
        control.densidadSinEnergia = 0.11f;
        control.densidadConEnergia = 0.006f;
        EditorUtility.SetDirty(control);
        return control;
    }

    // Zona invisible apenas pasando la entrada: al pisarla, el laboratorio pone su clima (la
    // luz ambiental y la niebla son de toda la escena)
    static void ArmarEntrada(Transform raiz, ControlEnergia control)
    {
        Transform zona = Grupo("Zona_Entrada", raiz);
        zona.localPosition = new Vector3((ENTRADA_X0 + ENTRADA_X1) / 2f, 1f, 0.75f);
        var colision = zona.gameObject.AddComponent<BoxCollider>();
        colision.isTrigger = true;
        colision.size = new Vector3(ENTRADA_X1 - ENTRADA_X0 + 0.8f, 2f, 1.4f);
        var disparador = zona.gameObject.AddComponent<DisparadorJugador>();
        UnityEventTools.AddVoidPersistentListener(disparador.alEntrar, new UnityAction(control.Reaplicar));
        EditorUtility.SetDirty(disparador);
    }

    static void AjustarAmbienteEditor()
    {
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.5f, 0.53f, 0.5f);
        RenderSettings.fog = false;
    }

    static void AsegurarInventario()
    {
        if (Object.FindAnyObjectByType<Inventory>() != null) return;
        var go = new GameObject("Inventory");
        go.AddComponent<Inventory>();
        Undo.RegisterCreatedObjectUndo(go, "Construir Cuarto 4");
    }

    // Ajustes para el Quest, como en el Cuarto 3: ningún objeto usa las sondas de luz horneadas
    // del Cuarto 1, y lo que nunca se mueve se marca para "static batching" (Unity junta esas
    // piezas y las dibuja de a muchas). Queda afuera todo lo que se mueve o se agarra.
    static void PrepararParaQuest(Transform raiz)
    {
        foreach (Renderer r in raiz.GetComponentsInChildren<Renderer>(true))
            r.lightProbeUsage = LightProbeUsage.Off;

        string[] seMueven = { "Puerta_Salida", "Camilla", "Fusible_", "Muestra_", "Vidrio_Campana", "Perilla",
                              "Valvula_", "Palanca_P", "Mechero_", "Tablero_Electrico", "Casillero_Docente",
                              "Tarjeta_", "Lector_Salida", "Boton_Volver", "Cartel_Victoria" };
        foreach (Transform t in raiz.GetComponentsInChildren<Transform>(true))
        {
            bool mueve = false;
            foreach (string prefijo in seMueven)
                if (EstaDentroDe(t, prefijo)) { mueve = true; break; }
            bool esTexto = t.GetComponent<TMP_Text>() != null;
            GameObjectUtility.SetStaticEditorFlags(t.gameObject, mueve || esTexto ? 0 : StaticEditorFlags.BatchingStatic);
        }
    }

    static bool EstaDentroDe(Transform t, string prefijo)
    {
        for (Transform x = t; x != null; x = x.parent)
            if (x.name.StartsWith(prefijo)) return true;
        return false;
    }

    // ------------------------------------------------------------------ datos de los acertijos

    const string RUTA_ACIERTO = "Assets/Samples/XR Interaction Toolkit/3.5.1/Hands Interaction Demo/DemoAssets/Audio/TeleportSelection.wav";

    // Un asset (ScriptableObject) por acertijo, como pide el documento. Si ya existe no se toca:
    // lo que el equipo cambie en el Inspector se respeta al reconstruir.
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
        datos.nombreCuarto = "Laboratorio de Ciencias";
        datos.tipo = tipo;
        datos.solucion = solucion;
        datos.pista = pista;
        datos.mensajeAcierto = acierto;
        datos.mensajeError = error;
        AssetDatabase.CreateAsset(datos, ruta);
        return datos;
    }

    // Un asset (ScriptableObject) por objeto, como pide el documento: las tarjetas de acceso.
    // Si ya existe no se toca.
    static ItemData Item(string archivo, string id, string nombre, string descripcion)
    {
        const string carpeta = "Assets/ScriptableObjects/Items";
        if (!AssetDatabase.IsValidFolder("Assets/ScriptableObjects")) AssetDatabase.CreateFolder("Assets", "ScriptableObjects");
        if (!AssetDatabase.IsValidFolder(carpeta)) AssetDatabase.CreateFolder("Assets/ScriptableObjects", "Items");

        string ruta = carpeta + "/" + archivo + ".asset";
        var item = AssetDatabase.LoadAssetAtPath<ItemData>(ruta);
        if (item != null) return item;

        item = ScriptableObject.CreateInstance<ItemData>();
        item.id = id;
        item.nombre = nombre;
        item.descripcion = descripcion;
        item.tipo = TipoItem.Tarjeta;
        AssetDatabase.CreateAsset(item, ruta);
        return item;
    }

    // ------------------------------------------------------------------ objetos que se agarran

    // Agarre firme como las herramientas del Cuarto 1: punto de agarre fijo, sigue a la mano sin
    // retraso, con el rayo viene a la mano, en el PC se toma y se suelta con un clic
    // (HerramientaEnMano) y al soltarlo cae con física. Si se pierde, aparece en "rescate".
    static XRGrabInteractable HacerAgarrable(GameObject go, Transform punto, Vector3 rescate, bool seguirMirada)
    {
        if (go.GetComponent<Collider>() == null) go.AddComponent<BoxCollider>();
        var cuerpo = go.AddComponent<Rigidbody>();
        cuerpo.mass = 0.2f;
        cuerpo.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        cuerpo.interpolation = RigidbodyInterpolation.Interpolate;
        cuerpo.isKinematic = true;   // quieto donde está hasta que lo agarren

        var agarre = go.AddComponent<XRGrabInteractable>();
        agarre.useDynamicAttach = false;
        agarre.movementType = XRBaseInteractable.MovementType.Instantaneous;
        agarre.farAttachMode = InteractableFarAttachMode.Near;
        agarre.attachTransform = punto;
        agarre.attachEaseInTime = 0.15f;   // al encajar viaja suave hasta su lugar

        var objeto = go.AddComponent<ObjetoAgarrable>();
        objeto.puntoDeRescate = raizCuarto.TransformPoint(rescate);
        objeto.zonaPermitida = new Bounds(raizCuarto.TransformPoint(new Vector3(ANCHO / 2f, ALTO / 2f, FONDO / 2f)),
                                          new Vector3(ANCHO, ALTO, FONDO));
        go.AddComponent<ResaltarAlApuntar>();
        go.AddComponent<HerramientaEnMano>().seguirMirada = seguirMirada;
        return agarre;
    }

    // Dónde lo toma la mano, con su adelante (+Z) y su arriba (+Y)
    static Transform PuntoDeAgarre(Transform objeto, Vector3 pos, Vector3 adelante, Vector3 arriba)
    {
        Transform punto = Grupo("Punto_Agarre", objeto);
        punto.localPosition = pos;
        punto.localRotation = Quaternion.LookRotation(adelante, arriba);
        return punto;
    }

    // ------------------------------------------------------------------ modelos

    // Modelo de Sketchfab (Assets/Sketchfab/<archivo>.glb). Devuelve el objeto que lo contiene,
    // o null si no está (y el que llama arma una versión simple).
    static GameObject ModeloSketchfab(string archivo, Transform padre, Vector3 pos, float giroY, float alto,
                                      Apoyo apoyo, bool colisiona)
        => Colocar(AssetDatabase.LoadAssetAtPath<GameObject>(CARPETA_SKETCHFAB + "/" + archivo + ".glb"),
                   archivo, padre, pos, giroY, alto, apoyo, colisiona);

    // Modelo de Poly Haven (Assets/PolyHaven/Modelos): el .gltf si está, si no el .fbx
    static GameObject ModeloPolyHaven(string nombre, Transform padre, Vector3 pos, float giroY, float alto,
                                      Apoyo apoyo, bool colisiona)
    {
        GameObject fuente = null;
        foreach (string ext in new[] { ".gltf", ".fbx" })
            if (fuente == null)
                fuente = AssetDatabase.LoadAssetAtPath<GameObject>(CARPETA_POLYHAVEN + "/" + nombre + "/" + nombre + "_1k" + ext);
        return Colocar(fuente, nombre, padre, pos, giroY, alto, apoyo, colisiona);
    }

    // Pone un modelo: lo escala a "alto" metros (0 = tamaño original) y lo apoya según "apoyo":
    // Piso = la base en pos; Centro = el centro en pos; Pared = la espalda en pos (su frente es +Z)
    static GameObject Colocar(GameObject fuente, string nombre, Transform padre, Vector3 pos, float giroY, float alto,
                              Apoyo apoyo, bool colisiona)
    {
        if (fuente == null)
        {
            Debug.LogWarning("Cuarto 4: no está el modelo " + nombre + ". Se usa una versión simple.");
            return null;
        }

        Transform contenedor = Grupo(nombre, padre);
        contenedor.localPosition = pos;
        GameObject modelo = Instanciar(fuente, contenedor);
        ReemplazarVidrios(modelo);

        // Las medidas se toman antes de girar el contenedor: así salen exactas en sus ejes
        if (LimitesLocales(modelo, contenedor, out Bounds b))
        {
            if (alto > 0f && b.size.y > 0.0001f)
            {
                modelo.transform.localScale *= alto / b.size.y;
                LimitesLocales(modelo, contenedor, out b);
            }
            Vector3 ancla = b.center;
            if (apoyo == Apoyo.Piso) ancla.y = b.min.y;
            else if (apoyo == Apoyo.Pared) ancla.z = b.min.z - 0.002f;
            modelo.transform.localPosition -= ancla;

            if (colisiona)
            {
                var col = contenedor.gameObject.AddComponent<BoxCollider>();
                col.center = b.center - ancla;
                col.size = b.size;
            }
        }
        contenedor.localRotation = Quaternion.Euler(0f, giroY, 0f);
        return contenedor.gameObject;
    }

    // Se respeta el giro que trae la raíz del modelo: en los de Sketchfab ahí está el paso de
    // "Z hacia arriba" a "Y hacia arriba"
    static GameObject Instanciar(GameObject fuente, Transform padre)
    {
        var go = PrefabUtility.InstantiatePrefab(fuente, padre) as GameObject;
        if (go == null) go = Object.Instantiate(fuente, padre);
        return go;
    }

    // Los vidrios con "transmisión" de glTF se ven blancos en el Quest: se cambian por el nuestro
    static void ReemplazarVidrios(GameObject modelo)
    {
        foreach (Renderer r in modelo.GetComponentsInChildren<Renderer>(true))
        {
            Material[] mats = r.sharedMaterials;
            bool cambio = false;
            for (int i = 0; i < mats.Length; i++)
                if (mats[i] != null && mats[i].name.ToLower().Contains("glass")) { mats[i] = mVidrio; cambio = true; }
            if (cambio) r.sharedMaterials = mats;
        }
    }

    // Medidas del modelo en los ejes de "espacio" (exactas aunque el cuarto esté girado)
    static bool LimitesLocales(GameObject modelo, Transform espacio, out Bounds b)
    {
        b = new Bounds();
        bool hay = false;
        foreach (Renderer r in modelo.GetComponentsInChildren<Renderer>())
        {
            Mesh malla = null;
            if (r is SkinnedMeshRenderer piel) malla = piel.sharedMesh;
            else if (r.TryGetComponent(out MeshFilter filtro)) malla = filtro.sharedMesh;
            if (malla == null) continue;
            Bounds mb = malla.bounds;
            Matrix4x4 m = espacio.worldToLocalMatrix * r.transform.localToWorldMatrix;
            for (int i = 0; i < 8; i++)
            {
                Vector3 esquina = mb.center + Vector3.Scale(mb.extents,
                    new Vector3((i & 1) == 0 ? -1f : 1f, (i & 2) == 0 ? -1f : 1f, (i & 4) == 0 ? -1f : 1f));
                Vector3 q = m.MultiplyPoint3x4(esquina);
                if (!hay) { b = new Bounds(q, Vector3.zero); hay = true; }
                else b.Encapsulate(q);
            }
        }
        return hay;
    }

    // Caja en el mundo de todo lo que se ve de un objeto
    static bool Limites(GameObject go, out Bounds b)
    {
        b = new Bounds();
        var renderers = go.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return false;
        b = renderers[0].bounds;
        foreach (var r in renderers) b.Encapsulate(r.bounds);
        return true;
    }

    static Transform BuscarHijo(Transform padre, string nombre)
    {
        foreach (Transform t in padre.GetComponentsInChildren<Transform>(true))
            if (t.name == nombre) return t;
        return null;
    }

    static Texture2D BuscarTextura(string prefijo)
    {
        var carpetas = new List<string>();
        foreach (var c in new[] { "Assets/PolyHaven", "Assets/Textures", "Assets/Modelos" })
            if (AssetDatabase.IsValidFolder(c)) carpetas.Add(c);
        if (carpetas.Count == 0) return null;
        foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", carpetas.ToArray()))
        {
            string ruta = AssetDatabase.GUIDToAssetPath(guid);
            if (Path.GetFileNameWithoutExtension(ruta).StartsWith(prefijo))
                return AssetDatabase.LoadAssetAtPath<Texture2D>(ruta);
        }
        return null;
    }

    // Busca un asset del proyecto por su nombre (por ejemplo el ItemData del medallón)
    static T BuscarAsset<T>(string nombre) where T : Object
    {
        var guids = AssetDatabase.FindAssets(nombre + " t:" + typeof(T).Name);
        return guids.Length == 0 ? null : AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guids[0]));
    }

    // ------------------------------------------------------------------ piezas básicas

    static Transform Grupo(string nombre, Transform padre)
    {
        var go = new GameObject(nombre);
        go.transform.SetParent(padre, false);
        return go.transform;
    }

    static GameObject Cubo(string n, Transform p, Vector3 pos, Vector3 tam, Material m, bool colisiona = false)
        => Primitiva(PrimitiveType.Cube, n, p, pos, tam, m, colisiona);

    static GameObject Cilindro(string n, Transform p, Vector3 pos, Vector3 tam, Material m, bool colisiona = false)
        => Primitiva(PrimitiveType.Cylinder, n, p, pos, tam, m, colisiona);

    static GameObject Esfera(string n, Transform p, Vector3 pos, Vector3 tam, Material m, bool colisiona = false)
        => Primitiva(PrimitiveType.Sphere, n, p, pos, tam, m, colisiona);

    static GameObject Primitiva(PrimitiveType tipo, string nombre, Transform padre, Vector3 pos, Vector3 tam, Material mat, bool colisiona)
    {
        var go = GameObject.CreatePrimitive(tipo);
        go.name = nombre;
        go.transform.SetParent(padre, false);
        go.transform.localPosition = pos;
        go.transform.localScale = tam;
        if (mat != null) go.GetComponent<Renderer>().sharedMaterial = mat;
        if (!colisiona) Object.DestroyImmediate(go.GetComponent<Collider>());
        return go;
    }

    // Texto 3D. "mira" = hacia dónde se lee (donde está el jugador), en los ejes del padre.
    // El tamaño de la letra se ajusta solo a la caja.
    static TextMeshPro Texto(string nombre, Transform padre, Vector3 pos, Vector3 mira, string texto, Vector2 caja, Color color,
                             Vector3? arriba = null)
    {
        var go = new GameObject(nombre);
        var t = go.AddComponent<TextMeshPro>();   // primero el componente: convierte el Transform en RectTransform
        go.transform.SetParent(padre, false);
        go.transform.localPosition = pos;
        go.transform.localRotation = Quaternion.LookRotation(-mira, arriba ?? Vector3.up);   // TMP se lee desde su -Z
        t.rectTransform.sizeDelta = caja;
        t.text = texto;
        t.alignment = TextAlignmentOptions.Center;
        t.color = color;
        t.enableAutoSizing = true;
        t.fontSizeMin = 0.01f;
        t.fontSizeMax = 3f;
        return t;
    }

    // Cartel: una chapa con un texto, de frente hacia "mira"
    static void Cartel(Transform p, string nombre, Vector3 pos, Vector3 mira, string texto, float ancho, float alto,
                       Material chapa, Color colorTexto)
    {
        Transform c = Grupo(nombre, p);
        c.localPosition = pos;
        c.localRotation = Quaternion.LookRotation(mira);
        Cubo("Chapa", c, new Vector3(0f, 0f, 0.004f), new Vector3(ancho, alto, 0.008f), chapa);
        Texto("Texto", c, new Vector3(0f, 0f, 0.0085f), Vector3.forward, texto, new Vector2(ancho * 0.9f, alto * 0.8f), colorTexto);
    }

    // Qué señal de seguridad es (colores de la norma ISO 7010)
    enum Senal { Advertencia, Obligacion, Seguridad, Incendio }

    // Cartel de seguridad moderno, al estilo ISO 7010: placa blanca con el símbolo a la izquierda
    // (triángulo amarillo = advertencia, círculo azul = obligación, cuadrado verde = seguridad,
    // cuadrado rojo = incendio), el texto a la derecha (título en negrita y la aclaración) y una
    // franja del color de la señal abajo. "mira" = hacia dónde queda el frente.
    static Transform CartelModerno(Transform p, string nombre, Vector3 pos, Vector3 mira, Senal tipo, string titulo,
                                   string texto, float ancho, float alto)
    {
        Transform c = Grupo(nombre, p);
        c.localPosition = pos;
        c.localRotation = Quaternion.LookRotation(mira);
        Cubo("Placa", c, new Vector3(0f, 0f, 0.003f), new Vector3(ancho, alto, 0.006f), mBlanco);

        Material color = tipo == Senal.Advertencia ? mAmarilloSenal : tipo == Senal.Obligacion ? mAzulSenal
                       : tipo == Senal.Seguridad ? mVerdeSenal : mRojoSenal;
        Cubo("Franja", c, new Vector3(0f, -alto / 2f + 0.006f, 0.0065f), new Vector3(ancho, 0.012f, 0.001f), color);

        // El símbolo, a la izquierda del que mira (+X del cartel)
        float lado = alto * 0.72f;
        var centro = new Vector3(ancho / 2f - alto / 2f, 0.004f, 0.0065f);
        switch (tipo)
        {
            case Senal.Advertencia:
                Pieza("Borde", c, centro, new Vector3(lado, lado, 1f), mallaTriangulo, mNegro);
                Pieza("Triangulo", c, centro + new Vector3(0f, -lado * 0.035f, 0.0005f), new Vector3(lado * 0.8f, lado * 0.8f, 1f), mallaTriangulo, color);
                Texto("Signo", c, centro + new Vector3(0f, -lado * 0.1f, 0.001f), Vector3.forward, "<b>!</b>", new Vector2(lado * 0.3f, lado * 0.42f), Color.black);
                break;
            case Senal.Obligacion:
                Cilindro("Circulo", c, centro, new Vector3(lado, 0.0005f, lado), color).transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                Texto("Signo", c, centro + new Vector3(0f, 0f, 0.001f), Vector3.forward, "<b>!</b>", new Vector2(lado * 0.4f, lado * 0.6f), Color.white);
                break;
            case Senal.Seguridad:
                // Ducha: la flor arriba y el agua cayendo
                Cubo("Cuadrado", c, centro, new Vector3(lado, lado, 0.001f), color);
                Cubo("Flor", c, centro + new Vector3(0f, lado * 0.28f, 0.001f), new Vector3(lado * 0.45f, lado * 0.08f, 0.001f), mBlanco);
                for (int i = -1; i <= 1; i++)
                    Cubo("Agua", c, centro + new Vector3(i * lado * 0.14f, -lado * 0.05f, 0.001f), new Vector3(lado * 0.05f, lado * 0.4f, 0.001f), mBlanco);
                break;
            default:
                // Extintor: el cilindro, la válvula y la manguera
                Cubo("Cuadrado", c, centro, new Vector3(lado, lado, 0.001f), color);
                Cubo("Cilindro", c, centro + new Vector3(0f, -lado * 0.08f, 0.001f), new Vector3(lado * 0.26f, lado * 0.56f, 0.001f), mBlanco);
                Cubo("Valvula", c, centro + new Vector3(0f, lado * 0.26f, 0.001f), new Vector3(lado * 0.14f, lado * 0.1f, 0.001f), mBlanco);
                Cubo("Manguera", c, centro + new Vector3(-lado * 0.16f, lado * 0.1f, 0.001f), new Vector3(lado * 0.05f, lado * 0.3f, 0.001f), mBlanco);
                break;
        }

        // El texto, a la derecha del símbolo
        float anchoTexto = ancho - alto - 0.02f;
        string contenido = "<b>" + titulo + "</b>" + (string.IsNullOrEmpty(texto) ? "" : "\n<size=70%>" + texto + "</size>");
        TextMeshPro t = Texto("Texto", c, new Vector3(-alto / 2f + 0.005f, 0.004f, 0.0065f), Vector3.forward, contenido,
                              new Vector2(anchoTexto, alto * 0.72f), new Color(0.08f, 0.08f, 0.1f));
        t.alignment = TextAlignmentOptions.Left;
        return c;
    }

    // Una pieza hecha con una malla nuestra (la llama, el triángulo de las señales)
    static GameObject Pieza(string nombre, Transform padre, Vector3 pos, Vector3 tam, Mesh malla, Material mat)
    {
        var go = new GameObject(nombre);
        go.transform.SetParent(padre, false);
        go.transform.localPosition = pos;
        go.transform.localScale = tam;
        go.AddComponent<MeshFilter>().sharedMesh = malla;
        go.AddComponent<MeshRenderer>().sharedMaterial = mat;
        return go;
    }

    // Triángulo equilátero de 1 de lado, parado en el plano XY con el centro en el origen. Se ve
    // de los dos lados (tiene las dos caras).
    static Mesh MallaTriangulo()
    {
        float h = Mathf.Sqrt(3f) / 2f;
        var arriba = new Vector3(0f, h * 2f / 3f, 0f);
        var izquierda = new Vector3(0.5f, -h / 3f, 0f);
        var derecha = new Vector3(-0.5f, -h / 3f, 0f);
        var malla = new Mesh { name = "Triangulo" };
        malla.SetVertices(new List<Vector3> { arriba, izquierda, derecha, arriba, izquierda, derecha });
        malla.SetTriangles(new[] { 0, 1, 2, 3, 5, 4 }, 0);
        malla.RecalculateNormals();
        malla.RecalculateBounds();
        return malla;
    }

    // Llama de mechero: una gota alargada (se arma girando un perfil alrededor del eje Y), de 1 de
    // alto y 1 de ancho, con la base en el origen. Se escala al tamaño que haga falta.
    static Mesh MallaLlama()
    {
        const int LADOS = 14, ANILLOS = 12;
        var puntos = new List<Vector3>();
        var triangulos = new List<int>();
        for (int j = 0; j <= ANILLOS; j++)
        {
            float t = j / (float)ANILLOS;
            // Redonda abajo, más ancha a un tercio de la altura y en punta arriba
            float radio = 0.5f * Mathf.Sin(Mathf.PI * (0.12f + 0.88f * t)) * (1f - 0.35f * t);
            for (int i = 0; i < LADOS; i++)
            {
                float a = i * Mathf.PI * 2f / LADOS;
                puntos.Add(new Vector3(Mathf.Cos(a) * radio, t, Mathf.Sin(a) * radio));
            }
        }
        for (int j = 0; j < ANILLOS; j++)
            for (int i = 0; i < LADOS; i++)
            {
                int a = j * LADOS + i, b = j * LADOS + (i + 1) % LADOS;
                triangulos.AddRange(new[] { a, a + LADOS, b, b, a + LADOS, b + LADOS });
            }
        // La base cerrada
        int centro = puntos.Count;
        puntos.Add(Vector3.zero);
        for (int i = 0; i < LADOS; i++)
            triangulos.AddRange(new[] { centro, i, (i + 1) % LADOS });

        var malla = new Mesh { name = "Llama" };
        malla.SetVertices(puntos);
        malla.SetTriangles(triangulos, 0);
        malla.RecalculateNormals();
        malla.RecalculateBounds();
        return malla;
    }

    // Una mancha de sangre sobre una superficie plana: un Quad acostado con la textura de sangre
    static void Mancha(string nombre, Transform p, Vector3 pos, float ancho, float largo, float giroY)
    {
        GameObject m = Primitiva(PrimitiveType.Quad, nombre, p, pos, new Vector3(ancho, largo, 1f), mSangre, false);
        m.transform.localRotation = Quaternion.Euler(90f, giroY, 0f);
        m.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
    }

    // La textura de la sangre, hecha por código y guardada como PNG (una vez): un manchón oscuro
    // con el borde irregular y gotas alrededor, transparente afuera
    static Texture2D TexturaSangre()
    {
        const string ruta = "Assets/Materials/Cuarto4/C4_Sangre.png";
        var existente = AssetDatabase.LoadAssetAtPath<Texture2D>(ruta);
        if (existente != null) return existente;

        const int N = 256;
        var azar = new System.Random(3);
        var manchas = new List<Vector3>();   // x, y y radio, en píxeles
        manchas.Add(new Vector3(128f, 128f, 62f));
        for (int i = 0; i < 14; i++)
            manchas.Add(new Vector3(45f + (float)azar.NextDouble() * 166f, 45f + (float)azar.NextDouble() * 166f,
                                    5f + (float)azar.NextDouble() * 20f));
        var tex = new Texture2D(N, N, TextureFormat.RGBA32, false);
        for (int y = 0; y < N; y++)
            for (int x = 0; x < N; x++)
            {
                float borde = 0.75f + 0.5f * Mathf.PerlinNoise(x * 0.05f, y * 0.05f);   // borde irregular
                float alfa = 0f;
                foreach (Vector3 m in manchas)
                {
                    float d = Vector2.Distance(new Vector2(x, y), new Vector2(m.x, m.y));
                    alfa = Mathf.Max(alfa, Mathf.Clamp01((m.z * borde - d) / 5f));
                }
                float oscuro = 0.5f + 0.5f * Mathf.PerlinNoise(x * 0.09f + 7f, y * 0.09f);
                tex.SetPixel(x, y, new Color(0.34f * oscuro, 0.015f, 0.015f, alfa * 0.93f));
            }
        tex.Apply();
        File.WriteAllBytes(ruta, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(ruta);
        var importador = (TextureImporter)AssetImporter.GetAtPath(ruta);
        importador.alphaIsTransparency = true;
        importador.wrapMode = TextureWrapMode.Clamp;
        importador.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Texture2D>(ruta);
    }

    static Light LuzPunto(string nombre, Transform padre, Vector3 pos, Color color, float intensidad, float rango)
    {
        var go = new GameObject(nombre);
        go.transform.SetParent(padre, false);
        go.transform.localPosition = pos;
        var l = go.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = color;
        l.intensity = intensidad;
        l.range = rango;
        l.shadows = LightShadows.None;
        l.lightmapBakeType = LightmapBakeType.Realtime;   // no entra en la luz horneada del Cuarto 1
        return l;
    }

    static AudioSource AudioEn(string nombre, Transform padre, Vector3 pos)
    {
        var go = new GameObject(nombre);
        go.transform.SetParent(padre, false);
        go.transform.localPosition = pos;
        var a = go.AddComponent<AudioSource>();
        a.playOnAwake = false;
        a.spatialBlend = 1f;
        return a;
    }

    // ------------------------------------------------------------------ materiales

    static void CrearMateriales()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Materials")) AssetDatabase.CreateFolder("Assets", "Materials");
        if (!AssetDatabase.IsValidFolder("Assets/Materials/Cuarto4")) AssetDatabase.CreateFolder("Assets/Materials", "Cuarto4");

        // Paredes pintadas arriba (solo el relieve de la textura, color liso) y azulejo verde abajo
        mParedAlta = Mat("C4_ParedAlta", new Color(0.9f, 0.93f, 0.92f), 0f, 0.2f, default, "painted_plaster_wall", 3f, 1.5f, false, true);
        mFriso = Mat("C4_Azulejo", new Color(0.55f, 0.86f, 0.74f), 0f, 0.65f, default, "long_white_tiles", 5f, 2f, true);
        mGuarda = Mat("C4_Guarda", new Color(0.12f, 0.24f, 0.2f), 0.2f, 0.4f);
        mPiso = Mat("C4_Piso", new Color(0.62f, 0.64f, 0.63f), 0.55f, 0.45f, default, "metal_plate", 4f, 4.7f);
        mTecho = Mat("C4_Techo", new Color(0.78f, 0.8f, 0.78f), 0f, 0.1f);

        mAcero = Mat("C4_Acero", new Color(0.62f, 0.65f, 0.66f), 0.8f, 0.6f);
        mAceroOscuro = Mat("C4_AceroOscuro", new Color(0.22f, 0.25f, 0.25f), 0.6f, 0.45f);
        mNegro = Mat("C4_Negro", new Color(0.05f, 0.06f, 0.06f), 0.3f, 0.35f);
        mBlanco = Mat("C4_Blanco", new Color(0.93f, 0.94f, 0.92f), 0f, 0.3f);
        mPapel = Mat("C4_Papel", new Color(0.92f, 0.9f, 0.84f), 0f, 0.1f);
        mMadera = Mat("C4_Madera", new Color(0.55f, 0.38f, 0.22f), 0f, 0.35f);
        mPlastico = Mat("C4_PlasticoAzul", new Color(0.15f, 0.3f, 0.6f), 0f, 0.5f);
        mPlasticoOscuro = Mat("C4_PlasticoOscuro", new Color(0.16f, 0.17f, 0.18f), 0f, 0.45f);
        mCeramica = Mat("C4_Ceramica", new Color(0.95f, 0.95f, 0.93f), 0f, 0.75f);
        mLaton = Mat("C4_Laton", new Color(0.78f, 0.6f, 0.3f), 0.9f, 0.6f);
        mGas = Mat("C4_GasAmarillo", new Color(0.95f, 0.78f, 0.1f), 0.3f, 0.55f);
        mEsfera = Mat("C4_Esfera", new Color(0.95f, 0.95f, 0.92f), 0f, 0.5f);
        mVisor = Mat("C4_Visor", new Color(0.02f, 0.04f, 0.03f), 0f, 0.8f);
        mSal = Mat("C4_Sal", new Color(0.95f, 0.95f, 0.95f), 0f, 0.2f);
        mGrisCasillero = Mat("C4_GrisCasillero", new Color(0.52f, 0.57f, 0.6f), 0.5f, 0.45f);

        // Las sábanas se ven de los dos lados (la tela cuelga y se le ve el revés). La gris es la
        // del cuerpo, cuando queda tirada en el piso después del susto.
        mSabana = Mat("C4_Sabana", new Color(0.86f, 0.87f, 0.83f), 0f, 0.08f);
        if (mSabana.HasProperty("_Cull")) mSabana.SetFloat("_Cull", 0f);
        mSabana.doubleSidedGI = true;
        mSabanaGris = Mat("C4_SabanaGris", new Color(0.3f, 0.29f, 0.29f), 0f, 0.1f);
        if (mSabanaGris.HasProperty("_Cull")) mSabanaGris.SetFloat("_Cull", 0f);
        mSabanaGris.doubleSidedGI = true;
        mMancha = Mat("C4_Mancha", new Color(0.16f, 0.07f, 0.06f), 0f, 0.15f);

        // Señales de seguridad y paneles: los colores de la norma ISO 7010
        mAmarilloSenal = Mat("C4_AmarilloSenal", new Color(1f, 0.8f, 0.05f), 0f, 0.4f);
        mAzulSenal = Mat("C4_AzulSenal", new Color(0.02f, 0.3f, 0.68f), 0f, 0.4f);
        mVerdeSenal = Mat("C4_VerdeSenal", new Color(0f, 0.55f, 0.3f), 0f, 0.4f);
        mRojoSenal = Mat("C4_RojoSenal", new Color(0.8f, 0.1f, 0.1f), 0f, 0.4f);

        // Pantallas (brillan solas: se leen a oscuras)
        mPantalla = Mat("C4_Pantalla", new Color(0.02f, 0.05f, 0.09f), 0f, 0.85f, new Color(0.015f, 0.04f, 0.08f));
        mCianOscuro = Mat("C4_CianOscuro", new Color(0.03f, 0.25f, 0.35f), 0f, 0.6f, new Color(0.02f, 0.18f, 0.26f));

        // Lo que brilla solo: luces de estado, tubos encendidos
        mVerdeLuz = Mat("C4_VerdeLuz", new Color(0.3f, 0.95f, 0.45f), 0f, 0.5f, new Color(0.2f, 1.1f, 0.35f));
        mRojoLuz = Mat("C4_RojoLuz", new Color(1f, 0.25f, 0.2f), 0f, 0.5f, new Color(1.1f, 0.15f, 0.1f));
        mLedApagado = Mat("C4_LedApagado", new Color(0.08f, 0.09f, 0.08f), 0f, 0.5f);
        mAmbarLuz = Mat("C4_AmbarLuz", new Color(1f, 0.65f, 0.15f), 0f, 0.5f, new Color(1.6f, 0.85f, 0.12f));
        mLuzTecho = Mat("C4_LuzTecho", new Color(1f, 1f, 0.97f), 0f, 0.5f, new Color(1.5f, 1.6f, 1.5f));
        mTuboApagado = Mat("C4_TuboApagado", new Color(0.8f, 0.82f, 0.82f), 0f, 0.6f);

        // Transparentes: vidrio, humo del chispazo del fusible y la sangre (con su textura)
        mVidrio = Transparente("C4_Vidrio", new Color(0.9f, 1f, 0.95f, 0.15f), default);
        mHumo = Transparente("C4_Humo", new Color(0.55f, 0.55f, 0.55f, 0.35f), default);
        mSangre = Transparente("C4_Sangre", Color.white, default);
        mSangre.SetTexture("_BaseMap", TexturaSangre());
        EditorUtility.SetDirty(mSangre);

        // La llama del mechero: suma luz, como el fuego
        mLlamaExterior = Aditivo("C4_LlamaExterior", new Color(0.2f, 0.4f, 1f, 0.6f));
        mLlamaInterior = Aditivo("C4_LlamaInterior", new Color(0.4f, 0.7f, 1f, 0.9f));

        // Fusibles con los colores normalizados de su amperaje
        mFus4 = Mat("C4_Fusible4", new Color(0.45f, 0.27f, 0.12f), 0f, 0.6f);
        mFus6 = Mat("C4_Fusible6", new Color(0.2f, 0.6f, 0.25f), 0f, 0.6f);
        mFus10 = Mat("C4_Fusible10", new Color(0.8f, 0.12f, 0.1f), 0f, 0.6f);
        mFus16 = Mat("C4_Fusible16", new Color(0.55f, 0.57f, 0.6f), 0f, 0.6f);
        mFus20 = Mat("C4_Fusible20", new Color(0.15f, 0.3f, 0.75f), 0f, 0.6f);
        mFus25 = Mat("C4_Fusible25", new Color(0.9f, 0.78f, 0.15f), 0f, 0.6f);

        // El patio de salida: baldosa gris de vereda y muros claros de ladrillo pintado
        mPatio = Mat("C4_Patio", new Color(0.56f, 0.56f, 0.53f), 0f, 0.3f);
        mMuroPatio = Mat("C4_MuroPatio", new Color(0.88f, 0.83f, 0.72f), 0f, 0.2f);
    }

    // Material que deja ver a través. El alpha va en el color; si se le pasa emisión, además brilla.
    static Material Transparente(string nombre, Color color, Color emision)
    {
        var m = Mat(nombre, color, 0f, 0.9f, emision);
        m.SetFloat("_Surface", 1f);
        m.SetFloat("_Blend", 0f);
        m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
        m.SetFloat("_ZWrite", 0f);
        m.SetOverrideTag("RenderType", "Transparent");
        m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        m.renderQueue = (int)RenderQueue.Transparent;
        EditorUtility.SetDirty(m);
        return m;
    }

    // Material que suma luz, sin sombras (aditivo): para la llama, que brilla y deja ver a través.
    // El alpha del color dice cuánto suma.
    static Material Aditivo(string nombre, Color color)
    {
        string ruta = "Assets/Materials/Cuarto4/" + nombre + ".mat";
        var shader = Shader.Find("Universal Render Pipeline/Unlit");
        var m = AssetDatabase.LoadAssetAtPath<Material>(ruta);
        if (m == null)
        {
            m = new Material(shader);
            AssetDatabase.CreateAsset(m, ruta);
        }
        else if (shader != null) m.shader = shader;
        m.SetColor("_BaseColor", color);
        m.SetFloat("_Surface", 1f);   // transparente
        m.SetFloat("_Blend", 2f);     // aditivo
        m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        m.SetFloat("_DstBlend", (float)BlendMode.One);
        m.SetFloat("_ZWrite", 0f);
        m.SetFloat("_Cull", 0f);      // se ve de los dos lados
        m.SetOverrideTag("RenderType", "Transparent");
        m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        m.renderQueue = (int)RenderQueue.Transparent;
        EditorUtility.SetDirty(m);
        return m;
    }

    // "polyHaven" es el nombre de una textura de Poly Haven: si está en el proyecto se le pone.
    // "tenir": la textura se tiñe con el color. "soloRelieve": solo el normal map, color liso.
    static Material Mat(string nombre, Color color, float metalico, float suavidad, Color emision = default,
                        string polyHaven = null, float tileX = 1f, float tileY = 1f, bool tenir = false, bool soloRelieve = false)
    {
        string ruta = "Assets/Materials/Cuarto4/" + nombre + ".mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(ruta);
        if (m == null)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            m = new Material(shader);
            AssetDatabase.CreateAsset(m, ruta);
        }
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", color);
        if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", metalico);
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", suavidad);
        if (emision != default(Color))
        {
            m.EnableKeyword("_EMISSION");
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            if (m.HasProperty("_EmissionColor")) m.SetColor("_EmissionColor", emision);
        }
        if (!string.IsNullOrEmpty(polyHaven))
        {
            var tiling = new Vector2(tileX, tileY);
            var baseMap = soloRelieve ? null : BuscarTextura(polyHaven + "_diff");
            if (soloRelieve && m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", null);
            if (baseMap != null && m.HasProperty("_BaseMap"))
            {
                m.SetTexture("_BaseMap", baseMap);
                m.SetTextureScale("_BaseMap", tiling);
                if (!tenir && m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", Color.white);
            }
            var normal = BuscarTextura(polyHaven + "_nor_gl");
            if (normal != null && m.HasProperty("_BumpMap"))
            {
                m.SetTexture("_BumpMap", normal);
                m.SetTextureScale("_BumpMap", tiling);
                m.EnableKeyword("_NORMALMAP");
            }
        }
        EditorUtility.SetDirty(m);
        return m;
    }
}
