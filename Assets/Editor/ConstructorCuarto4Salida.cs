using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;

// Segunda parte del constructor del Cuarto 4: EL FINAL DEL JUEGO (la primera parte,
// ConstructorCuarto4.cs, arma el laboratorio). Se corre con el mismo menú: Escape Room >
// Construir los 4 cuartos.
//
// Detrás de la salida de emergencia del laboratorio está el pasillo principal del colegio
// (3.8 x 12 m, techo de 3.2), moderno: piso de terrazo, paredes blancas, una pared de listones de
// madera, cielorraso acústico con tiras de luz LED, casilleros, sillones y una jardinera. Al fondo,
// la puerta principal: una pared de vidrio con marco de aluminio y puertas corredizas automáticas.
// Afuera es de día: la explanada de la entrada, con jardineras, un banco, los apliques de la
// fachada, la reja del colegio y el cielo de verdad (una foto 360° de Poly Haven, HDRI).
//
// LA SECUENCIA:
//  1. Se abre la salida de emergencia: se prende el pasillo (hasta ahí está apagado, así no se
//     dibuja mientras se resuelve el laboratorio) y entra luz de día por la puerta principal.
//  2. El jugador pisa el pasillo y gana (PantallaVictoria): la pantalla colgada del techo muestra
//     "¡GANASTE!" y el tiempo, suena la fanfarria y las luces del techo se prenden en cascada
//     hacia la salida (LucesEnCascada).
//  3. Al terminar la cascada se abre sola la puerta principal (PuertaCorrediza).
//  4. Junto a la puerta, el tótem con dos botones: VOLVER A JUGAR (empieza de nuevo en el Cuarto 1)
//     y MENÚ PRINCIPAL (vuelve al menú de inicio).
//
// Los sillones, las jardineras, los arbustos, el banco, el reloj, el extintor y la alarma son
// modelos de Poly Haven (CC0). Poly Haven no tiene casilleros, puertas de vidrio, pantallas, rejas
// ni apliques modernos: esos se arman con piezas, con materiales de Poly Haven.
//
// Las medidas son "del pasillo": el origen está en el centro de la salida de emergencia, del lado
// del pasillo; +Z avanza hacia la puerta principal y +X va hacia la derecha del que entra.
public static partial class ConstructorCuarto4
{
    const float P_IZQ = -0.75f;   // cara de la pared izquierda: la salida de emergencia abre contra ella
    const float P_DER = 3.05f;    // cara de la pared derecha
    const float P_CX = (P_IZQ + P_DER) / 2f;
    const float P_LARGO = 12f;    // de la salida de emergencia a la puerta principal
    const float P_TECHO = 3.2f;
    const float P_ANCHO_PUERTA = 1.8f, P_ALTO_PUERTA = 2.3f;   // el vano de las puertas corredizas

    // La explanada de afuera y la fachada del colegio
    const float E_IZQ = -4.5f, E_DER = 6.8f, E_FONDO = P_LARGO + 10f;
    const float F_GROSOR = 0.3f, F_ALTO = 7.2f;

    // La foto 360° del cielo y hacia dónde se gira (grados), para que por la puerta se vean los árboles
    const string RUTA_CIELO = "Assets/PolyHaven/HDRI/museumplein_2k.hdr";
    const float GIRO_CIELO = 0f;

    static Material mSPiso, mSPared, mSFachada, mSMadera, mSTecho, mSAluminio, mSAluminioClaro, mSVidrio,
                    mSVidrioOscuro, mSCasillero, mSConcreto, mSAlfombra, mSPantalla, mSVerdeSalida, mSAzulBoton;

    // Lo llama Construir() después de armar la salida de emergencia ("salida")
    static void ArmarPasilloSalida(Transform raiz, Door salida)
    {
        CrearMaterialesSalida();
        PonerCielo();

        Transform g = Grupo("Pasillo_Salida", raiz);
        g.localPosition = new Vector3((SALIDA_X0 + SALIDA_X1) / 2f, 0f, FONDO + MURO);

        // Todo el pasillo y lo de afuera empiezan apagados: se prenden al abrirse la salida de
        // emergencia. Así no se dibujan (ni gastan luces) mientras se resuelve el laboratorio.
        Transform c = Grupo("Contenido", g);
        PasilloEstructura(c);
        LucesEnCascada cascada = PasilloLuces(c);
        PasilloCasilleros(c);
        PasilloSalaDeEspera(c);
        PasilloDetalles(c);
        GameObject textosPantalla = PantallaColgante(c, out TextMeshPro tiempo);
        PuertaCorrediza puertaPrincipal = PuertaPrincipal(c);
        PressableButton boton = Totem(c, out PressableButton botonMenu, out GameObject textosTotem);
        Exterior(c);
        c.gameObject.SetActive(false);
        UnityEventTools.AddBoolPersistentListener(salida.alAbrirse, new UnityAction<bool>(c.gameObject.SetActive), true);

        // El clima del pasillo: de día, con la luz que entra por la puerta principal
        var clima = g.gameObject.AddComponent<ClimaCuarto>();
        clima.luzAmbienteEncendido = new Color(0.46f, 0.48f, 0.52f);
        clima.reflejosEncendido = 0.6f;
        clima.colorNieblaEncendido = new Color(0.78f, 0.83f, 0.9f);
        clima.densidadNieblaEncendido = 0.004f;
        clima.nivel = 1f;

        var victoria = g.gameObject.AddComponent<PantallaVictoria>();
        victoria.clima = clima;
        victoria.mostrarAlGanar = new[] { textosPantalla, textosTotem };
        victoria.textoTiempo = tiempo;

        // La cadena del final: gana → cascada de luces → se abre la puerta principal. Los botones del
        // tótem vuelven a empezar desde el Cuarto 1 (verde) o al menú de inicio (azul).
        UnityEventTools.AddVoidPersistentListener(victoria.alGanar, new UnityAction(cascada.Encender));
        UnityEventTools.AddVoidPersistentListener(cascada.alTerminar, new UnityAction(puertaPrincipal.Abrir));
        UnityEventTools.AddVoidPersistentListener(boton.alPresionar, new UnityAction(victoria.VolverAJugar));
        UnityEventTools.AddVoidPersistentListener(botonMenu.alPresionar, new UnityAction(victoria.IrAlMenu));

        // La zona apenas pasando la salida de emergencia: al pisarla, gana
        Transform zona = Grupo("Zona_Victoria", c);
        zona.localPosition = new Vector3(P_CX, 1.1f, 1f);
        var colision = zona.gameObject.AddComponent<BoxCollider>();
        colision.isTrigger = true;
        colision.size = new Vector3(P_DER - P_IZQ, 2.2f, 1.4f);
        var disparador = zona.gameObject.AddComponent<DisparadorJugador>();
        UnityEventTools.AddVoidPersistentListener(disparador.alEntrar, new UnityAction(victoria.Ganar));

        foreach (Object o in new Object[] { clima, victoria, cascada, puertaPrincipal, boton, botonMenu, disparador, salida })
            EditorUtility.SetDirty(o);
    }

    // Lo llama el menú Escape Room > Llevar jugador al pasillo final (MenuEscapeRoom). Prende el
    // pasillo (en el juego se prende al abrirse la salida de emergencia) y deja al jugador recién
    // entrado: al dar Play gana enseguida, y se prueban el final y el botón de volver a jugar.
    // Se deshace con Ctrl+Z.
    public static void LlevarJugadorAlFinal()
    {
        var pasillo = GameObject.Find("Cuarto4_Laboratorio/Pasillo_Salida");
        Transform contenido = pasillo != null ? pasillo.transform.Find("Contenido") : null;
        if (contenido == null)
        {
            Debug.LogWarning("Primero hay que construir los cuartos (Escape Room > Construir los 4 cuartos).");
            return;
        }
        var camara = Camera.main;
        if (camara == null)
        {
            Debug.LogWarning("No se encontró la cámara del jugador en la escena.");
            return;
        }
        Transform jugador = camara.transform;
        while (jugador.parent != null) jugador = jugador.parent;

        Undo.RecordObject(contenido.gameObject, "Llevar jugador al pasillo final");
        contenido.gameObject.SetActive(true);
        Undo.RecordObject(jugador, "Llevar jugador al pasillo final");
        jugador.position = pasillo.transform.TransformPoint(new Vector3(0f, 0f, 1f));
        jugador.rotation = pasillo.transform.rotation;
        Selection.activeGameObject = jugador.gameObject;
    }

    // ------------------------------------------------------------------ el pasillo

    static void PasilloEstructura(Transform p)
    {
        float ancho = P_DER - P_IZQ;
        GameObject piso = Cubo("Piso", p, new Vector3(P_CX, -0.1f, P_LARGO / 2f), new Vector3(ancho, 0.2f, P_LARGO), mSPiso, true);
        ZonaDeTeleport(piso);
        Cubo("Pared_Izquierda", p, new Vector3(P_IZQ - MURO / 2f, P_TECHO / 2f, P_LARGO / 2f), new Vector3(MURO, P_TECHO, P_LARGO), mSPared, true);
        Cubo("Pared_Derecha", p, new Vector3(P_DER + MURO / 2f, P_TECHO / 2f, P_LARGO / 2f), new Vector3(MURO, P_TECHO, P_LARGO), mSPared, true);

        // La pared del laboratorio (la de la salida de emergencia) termina antes que el pasillo:
        // se cierra el tramo que falta
        float finLaboratorio = ANCHO + MURO - (SALIDA_X0 + SALIDA_X1) / 2f;
        Cubo("Pared_Cierre", p, new Vector3((finLaboratorio + P_DER + MURO) / 2f, P_TECHO / 2f, -MURO / 2f),
             new Vector3(P_DER + MURO - finLaboratorio, P_TECHO, MURO), mSPared, true);
        Cubo("Techo", p, new Vector3(P_CX, P_TECHO + MURO / 2f, P_LARGO / 2f), new Vector3(ancho + MURO * 2f, MURO, P_LARGO), mSTecho, true);

        // Zócalo de aluminio oscuro abajo de las paredes
        Cubo("Zocalo_Izq", p, new Vector3(P_IZQ + 0.006f, 0.05f, P_LARGO / 2f), new Vector3(0.012f, 0.1f, P_LARGO), mSAluminio);
        Cubo("Zocalo_Der", p, new Vector3(P_DER - 0.006f, 0.05f, P_LARGO / 2f), new Vector3(0.012f, 0.1f, P_LARGO), mSAluminio);
    }

    // Dos tiras de luz LED a lo largo del techo, cortadas en 6 tramos. Apagadas se ven grises; al
    // ganar se prenden de a un tramo, de la entrada hacia la salida, con una luz de verdad cada dos
    // tramos. La luz de día que entra por la puerta principal está prendida desde que se abre la
    // salida de emergencia.
    static LucesEnCascada PasilloLuces(Transform p)
    {
        Transform g = Grupo("Luces_Techo", p);
        LuzPunto("Luz_Dia_Entrada", g, new Vector3(P_CX, 2.2f, P_LARGO - 0.8f), new Color(1f, 0.97f, 0.92f), 1.6f, 7f);

        const int TRAMOS = 6;
        const float LARGO_TRAMO = 1.7f;
        float[] filas = { P_CX - 0.75f, P_CX + 0.75f };
        var tramos = new GameObject[TRAMOS];
        for (int i = 0; i < TRAMOS; i++)
        {
            float z = 1.2f + i * 1.9f;
            foreach (float x in filas)
            {
                Cubo("Tira_Marco", g, new Vector3(x, P_TECHO - 0.012f, z), new Vector3(0.1f, 0.024f, LARGO_TRAMO + 0.04f), mSAluminio);
                Cubo("Tira_Apagada", g, new Vector3(x, P_TECHO - 0.026f, z), new Vector3(0.07f, 0.004f, LARGO_TRAMO), mTuboApagado);
            }
            Transform tramo = Grupo("Tramo_" + (i + 1), g);
            foreach (float x in filas)
                Cubo("Tira_Encendida", tramo, new Vector3(x, P_TECHO - 0.029f, z), new Vector3(0.07f, 0.004f, LARGO_TRAMO), mLuzTecho);
            if (i % 2 == 1)
                LuzPunto("Luz", tramo, new Vector3(P_CX, P_TECHO - 0.4f, z - 0.95f), new Color(1f, 0.98f, 0.95f), 1.15f, 6.5f);
            tramo.gameObject.SetActive(false);
            tramos[i] = tramo.gameObject;
        }

        var cascada = g.gameObject.AddComponent<LucesEnCascada>();
        cascada.luces = tramos;
        cascada.intervalo = 0.3f;
        return cascada;
    }

    // Fila de 20 casilleros metálicos contra la pared izquierda, como los de un colegio: cuerpo
    // gris, puertas azul petróleo con rejillas de ventilación, manija y número
    static void PasilloCasilleros(Transform p)
    {
        Transform g = Grupo("Casilleros", p);
        const int CANTIDAD = 20;
        const float Z0 = 1.6f, ANCHO_C = 0.4f, ALTO_C = 1.9f, PROFUNDO = 0.45f;
        float largo = CANTIDAD * ANCHO_C;
        float frente = P_IZQ + PROFUNDO;
        float zc = Z0 + largo / 2f;

        Cubo("Cuerpo", g, new Vector3(P_IZQ + PROFUNDO / 2f, 0.1f + (ALTO_C - 0.1f) / 2f, zc), new Vector3(PROFUNDO, ALTO_C - 0.1f, largo), mGrisCasillero, true);
        Cubo("Zocalo", g, new Vector3(P_IZQ + PROFUNDO / 2f - 0.02f, 0.05f, zc), new Vector3(PROFUNDO - 0.04f, 0.1f, largo), mNegro, true);
        Cubo("Tapa", g, new Vector3(P_IZQ + PROFUNDO / 2f + 0.01f, ALTO_C + 0.01f, zc), new Vector3(PROFUNDO + 0.02f, 0.02f, largo + 0.02f), mSAluminio);

        for (int i = 0; i < CANTIDAD; i++)
        {
            int numero = 101 + i;
            Transform casillero = Grupo("Casillero_" + numero, g);
            casillero.localPosition = new Vector3(frente, 0f, Z0 + ANCHO_C * (i + 0.5f));
            Cubo("Puerta", casillero, new Vector3(0.008f, 0.1f + (ALTO_C - 0.1f) / 2f, 0f), new Vector3(0.016f, ALTO_C - 0.16f, ANCHO_C - 0.03f), mSCasillero);
            for (int k = 0; k < 4; k++)
                Cubo("Rejilla", casillero, new Vector3(0.017f, ALTO_C - 0.22f - k * 0.035f, 0f), new Vector3(0.003f, 0.012f, ANCHO_C * 0.55f), mNegro);
            Cubo("Manija", casillero, new Vector3(0.026f, 1.05f, -ANCHO_C * 0.3f), new Vector3(0.02f, 0.12f, 0.025f), mSAluminioClaro);
            Texto("Numero", casillero, new Vector3(0.0175f, 1.5f, 0f), Vector3.right, numero.ToString(), new Vector2(0.09f, 0.04f),
                  new Color(0.92f, 0.94f, 0.95f));
        }
    }

    // La pared de listones de madera de la derecha (el toque moderno), con una línea de luz arriba,
    // y la sala de espera adelante: dos sillones y una jardinera entre ellos
    static void PasilloSalaDeEspera(Transform p)
    {
        Transform g = Grupo("Sala_Espera", p);
        const float Z0 = 2.4f, Z1 = 8.6f, ALTO_L = 2.7f;
        Cubo("Fondo_Listones", g, new Vector3(P_DER - 0.01f, ALTO_L / 2f, (Z0 + Z1) / 2f), new Vector3(0.02f, ALTO_L, Z1 - Z0), mNegro);
        for (float z = Z0 + 0.06f; z < Z1 - 0.03f; z += 0.12f)
            Cubo("Liston", g, new Vector3(P_DER - 0.04f, ALTO_L / 2f, z), new Vector3(0.04f, ALTO_L, 0.06f), mSMadera);
        Cubo("Linea_Luz", g, new Vector3(P_DER - 0.04f, ALTO_L + 0.015f, (Z0 + Z1) / 2f), new Vector3(0.05f, 0.03f, Z1 - Z0), mLuzTecho);

        ModeloPolyHaven("modern_arm_chair_01", g, new Vector3(P_DER - 0.6f, 0f, 3.9f), -90f, 0f, Apoyo.Piso, true);
        ModeloPolyHaven("modern_arm_chair_01", g, new Vector3(P_DER - 0.6f, 0f, 7.1f), -90f, 0f, Apoyo.Piso, true);
        Jardinera(g, new Vector3(P_DER - 0.35f, 0f, 5.5f), 90f);
    }

    // Jardinera de Poly Haven con tres arbustos adentro (un poco hundidos en la tierra y agrandados,
    // para que se vea llena)
    static void Jardinera(Transform p, Vector3 pos, float giroY)
    {
        GameObject maceta = ModeloPolyHaven("planter_box_02", p, pos, giroY, 0f, Apoyo.Piso, true);
        if (maceta == null || !LimitesLocales(maceta, maceta.transform, out Bounds b)) return;
        float tierra = b.max.y - 0.06f;
        float[] filas = { -0.1f, 0f, 0.1f };
        float[] giros = { 4f, 183f, 358f };
        for (int i = 0; i < filas.Length; i++)
        {
            GameObject arbusto = ModeloPolyHaven("shrub_03", maceta.transform, new Vector3((i - 1) * 0.04f, tierra, filas[i]),
                                                 giros[i], 0f, Apoyo.Piso, false);
            if (arbusto != null) arbusto.transform.localScale = new Vector3(0.95f, 1.35f, 1.2f);
        }
    }

    // Reloj arriba de los casilleros, extintor con su cartel y la alarma de incendio, la alfombra de
    // la entrada y el cartel verde de SALIDA colgado frente a la puerta principal
    static void PasilloDetalles(Transform p)
    {
        Transform g = Grupo("Detalles", p);
        ModeloPolyHaven("wall_clock", g, new Vector3(P_IZQ, 2.45f, 5.6f), 90f, 0.34f, Apoyo.Pared, false);

        ModeloPolyHaven("korean_fire_extinguisher_01", g, new Vector3(P_IZQ + 0.2f, 0f, 10.8f), 90f, 0f, Apoyo.Piso, true);
        CartelModerno(g, "Cartel_Extintor", new Vector3(P_IZQ + 0.004f, 1.45f, 10.8f), Vector3.right, Senal.Incendio,
                      "EXTINTOR", "Clase ABC", 0.34f, 0.14f);
        ModeloPolyHaven("fire_alarm", g, new Vector3(P_IZQ, 1.35f, 11.35f), 90f, 0f, Apoyo.Pared, false);

        Cubo("Alfombra", g, new Vector3(P_CX, 0.006f, P_LARGO - 0.9f), new Vector3(P_ANCHO_PUERTA + 0.4f, 0.012f, 1.3f), mSAlfombra);

        Transform salida = Grupo("Cartel_Salida", g);
        salida.localPosition = new Vector3(P_CX, 2.75f, P_LARGO - 0.6f);
        Cubo("Caja", salida, Vector3.zero, new Vector3(0.62f, 0.2f, 0.05f), mBlanco);
        Cubo("Frente", salida, new Vector3(0f, 0f, -0.026f), new Vector3(0.58f, 0.16f, 0.002f), mSVerdeSalida);
        Texto("Texto", salida, new Vector3(0f, 0f, -0.028f), Vector3.back, "<b>SALIDA</b>", new Vector2(0.5f, 0.12f), Color.white);
        float colgante = P_TECHO - 2.75f - 0.1f;
        foreach (float x in new[] { -0.25f, 0.25f })
            Cilindro("Colgante", salida, new Vector3(x, 0.1f + colgante / 2f, 0f), new Vector3(0.008f, colgante / 2f, 0.008f), mSAluminio);
    }

    // La pantalla grande colgada del techo, a 5 m de la entrada, de frente al que llega. Apagada es
    // un vidrio negro; al ganar muestra "¡GANASTE!", que logró salir y el tiempo que tardó.
    static GameObject PantallaColgante(Transform p, out TextMeshPro tiempo)
    {
        Transform g = Grupo("Pantalla_Ganaste", p);
        const float ALTURA = 2.55f;
        g.localPosition = new Vector3(P_CX, ALTURA, 5f);
        Cubo("Marco", g, Vector3.zero, new Vector3(2.66f, 0.84f, 0.07f), mNegro);
        Cubo("Pantalla", g, new Vector3(0f, 0f, -0.036f), new Vector3(2.56f, 0.74f, 0.002f), mSPantalla);
        float cable = P_TECHO - ALTURA - 0.42f;
        foreach (float x in new[] { -1f, 1f })
            Cilindro("Cable", g, new Vector3(x, 0.42f + cable / 2f, 0f), new Vector3(0.008f, cable / 2f, 0.008f), mSAluminio);

        Transform textos = Grupo("Textos", g);
        textos.localPosition = new Vector3(0f, 0f, -0.038f);
        Texto("Ganaste", textos, new Vector3(0f, 0.16f, 0f), Vector3.back, "<b>¡GANASTE!</b>", new Vector2(2.3f, 0.3f), new Color(0.35f, 1f, 0.5f));
        Texto("Subtitulo", textos, new Vector3(0f, -0.05f, 0f), Vector3.back, "Lograste salir del colegio", new Vector2(2.2f, 0.12f), Color.white);
        // Lo completa PantallaVictoria con el tiempo del reloj
        tiempo = Texto("Tiempo", textos, new Vector3(0f, -0.21f, 0f), Vector3.back, "Tu tiempo: --:--", new Vector2(2.2f, 0.085f),
                       new Color(0.96f, 0.65f, 0.14f));
        return textos.gameObject;
    }

    // La puerta principal del colegio, en el fondo del pasillo: una pared de vidrio con marco de
    // aluminio oscuro, dos paños fijos a los costados, un paño alto arriba y, en el medio, las dos
    // hojas corredizas automáticas, con la caja del motor y su sensor arriba. Las hojas corren por
    // delante (del lado del pasillo) de los paños fijos.
    static PuertaCorrediza PuertaPrincipal(Transform p)
    {
        Transform g = Grupo("Puerta_Principal", p);
        g.localPosition = new Vector3(P_CX, 0f, P_LARGO);
        const float PERFIL = 0.06f;
        float ancho = P_DER - P_IZQ;
        float mitad = ancho / 2f;
        float vano = P_ANCHO_PUERTA / 2f;

        // Perfiles: los de los bordes van a todo lo hondo; los del vano, finos y corridos hacia
        // afuera, para que las hojas pasen por delante sin chocarlos
        foreach (float s in new[] { -1f, 1f })
        {
            Cubo("Perfil_Borde", g, new Vector3(s * (mitad - PERFIL / 2f), P_TECHO / 2f, 0f), new Vector3(PERFIL, P_TECHO, 0.12f), mSAluminio, true);
            Cubo("Perfil_Vano", g, new Vector3(s * (vano + PERFIL / 2f), P_ALTO_PUERTA / 2f, 0.025f), new Vector3(PERFIL, P_ALTO_PUERTA, 0.05f), mSAluminio, true);
        }
        Cubo("Travesano", g, new Vector3(0f, P_ALTO_PUERTA + PERFIL / 2f, 0.01f), new Vector3(ancho, PERFIL, 0.1f), mSAluminio, true);
        Cubo("Umbral", g, new Vector3(0f, 0.008f, 0f), new Vector3(ancho, 0.016f, 0.16f), mSAluminio);

        float anchoPano = (mitad - PERFIL) - (vano + PERFIL);
        foreach (float s in new[] { -1f, 1f })
        {
            float x = s * (vano + PERFIL + anchoPano / 2f);
            Cubo("Pano_Fijo", g, new Vector3(x, P_ALTO_PUERTA / 2f, 0.025f), new Vector3(anchoPano, P_ALTO_PUERTA, 0.012f), mSVidrio, true);
            // La franja blanca a la altura de la vista, para que el vidrio se note
            Cubo("Franja", g, new Vector3(x, 1.5f, 0.018f), new Vector3(anchoPano, 0.05f, 0.002f), mBlanco);
        }
        float altoPano = P_TECHO - P_ALTO_PUERTA - PERFIL;
        Cubo("Pano_Alto", g, new Vector3(0f, P_ALTO_PUERTA + PERFIL + altoPano / 2f, 0.025f), new Vector3(ancho - PERFIL * 2f, altoPano, 0.012f), mSVidrio, true);

        // La caja del motor sobre el vano, del lado del pasillo, con el sensor abajo
        Cubo("Caja_Motor", g, new Vector3(0f, P_ALTO_PUERTA + 0.13f, -0.13f), new Vector3(ancho - PERFIL * 2f, 0.2f, 0.14f), mSAluminio);
        Cubo("Sensor", g, new Vector3(0f, P_ALTO_PUERTA + 0.015f, -0.17f), new Vector3(0.2f, 0.03f, 0.05f), mNegro);
        Cubo("Sensor_Luz", g, new Vector3(0.07f, P_ALTO_PUERTA + 0.015f, -0.196f), new Vector3(0.012f, 0.012f, 0.002f), mVerdeLuz);

        // Las dos hojas corredizas: vidrio con un marco fino de aluminio y la franja blanca
        Transform hojas = Grupo("Puertas_Corredizas", g);
        hojas.localPosition = new Vector3(0f, 0f, -0.035f);
        var puerta = hojas.gameObject.AddComponent<PuertaCorrediza>();
        puerta.hojaIzquierda = HojaCorrediza(hojas, "Hoja_Izquierda", -vano / 2f, vano);
        puerta.hojaDerecha = HojaCorrediza(hojas, "Hoja_Derecha", vano / 2f, vano);
        puerta.recorrido = vano;
        return puerta;
    }

    static Transform HojaCorrediza(Transform padre, string nombre, float x, float ancho)
    {
        Transform hoja = Grupo(nombre, padre);
        hoja.localPosition = new Vector3(x, 0f, 0f);
        const float MARCO = 0.04f;
        float alto = P_ALTO_PUERTA - 0.02f;
        Cubo("Vidrio", hoja, new Vector3(0f, 0.01f + alto / 2f, 0f), new Vector3(ancho, alto, 0.012f), mSVidrio, true);
        Cubo("Marco_Arriba", hoja, new Vector3(0f, 0.01f + alto - MARCO / 2f, 0f), new Vector3(ancho, MARCO, 0.03f), mSAluminio);
        Cubo("Marco_Abajo", hoja, new Vector3(0f, 0.01f + 0.05f, 0f), new Vector3(ancho, 0.1f, 0.03f), mSAluminio);
        Cubo("Marco_Izq", hoja, new Vector3(-ancho / 2f + MARCO / 2f, 0.01f + alto / 2f, 0f), new Vector3(MARCO, alto, 0.03f), mSAluminio);
        Cubo("Marco_Der", hoja, new Vector3(ancho / 2f - MARCO / 2f, 0.01f + alto / 2f, 0f), new Vector3(MARCO, alto, 0.03f), mSAluminio);
        Cubo("Franja", hoja, new Vector3(0f, 1.5f, -0.007f), new Vector3(ancho - MARCO * 2f, 0.05f, 0.002f), mBlanco);
        return hoja;
    }

    // El tótem digital junto a la puerta principal, como los de información de un edificio moderno:
    // pantalla arriba, consola inclinada con dos botones (verde: VOLVER A JUGAR; azul: MENÚ
    // PRINCIPAL) y sus carteles abajo. Queda un poco girado hacia el centro del pasillo. La
    // pantalla se prende al ganar.
    static PressableButton Totem(Transform p, out PressableButton botonMenu, out GameObject textos)
    {
        Transform g = Grupo("Totem_Volver", p);
        g.localPosition = new Vector3(P_DER - 0.6f, 0f, P_LARGO - 1.9f);
        g.localRotation = Quaternion.Euler(0f, 20f, 0f);   // su frente es -Z: mira al que llega por el pasillo

        Cubo("Base", g, new Vector3(0f, 0.02f, 0f), new Vector3(0.56f, 0.04f, 0.36f), mSAluminio, true);
        Cubo("Cuerpo", g, new Vector3(0f, 0.84f, 0f), new Vector3(0.46f, 1.6f, 0.14f), mNegro, true);
        foreach (float s in new[] { -1f, 1f })
            Cubo("Canto", g, new Vector3(s * 0.235f, 0.84f, 0f), new Vector3(0.012f, 1.6f, 0.145f), mSAluminioClaro);
        Cubo("Pantalla", g, new Vector3(0f, 1.37f, -0.071f), new Vector3(0.4f, 0.42f, 0.002f), mSPantalla);

        Transform t = Grupo("Textos", g);
        t.localPosition = new Vector3(0f, 1.37f, -0.073f);
        Texto("Titulo", t, new Vector3(0f, 0.13f, 0f), Vector3.back, "<b>¿OTRA PARTIDA?</b>", new Vector2(0.34f, 0.06f), Color.white);
        Texto("Ayuda", t, new Vector3(0f, -0.005f, 0f), Vector3.back,
              "<color=#4ade80>Verde:</color> jugar de nuevo desde\nel Cuarto 1, con 15 minutos\n<color=#60a5fa>Azul:</color> volver al menú principal",
              new Vector2(0.34f, 0.14f), new Color(0.75f, 0.8f, 0.86f));
        Texto("Gracias", t, new Vector3(0f, -0.155f, 0f), Vector3.back, "Gracias por jugar", new Vector2(0.28f, 0.04f),
              new Color(0.96f, 0.65f, 0.14f));
        textos = t.gameObject;

        // La consola inclinada hacia el jugador, con los dos botones (bajan perpendiculares a ella)
        Transform consola = Grupo("Consola", g);
        consola.localPosition = new Vector3(0f, 1.0f, -0.13f);
        consola.localRotation = Quaternion.Euler(-25f, 0f, 0f);
        Cubo("Borde", consola, new Vector3(0f, -0.002f, 0f), new Vector3(0.47f, 0.045f, 0.27f), mSAluminioClaro);
        Cubo("Tablero", consola, Vector3.zero, new Vector3(0.46f, 0.05f, 0.26f), mNegro, true);
        PressableButton volver = BotonDeTotem(consola, "Boton_Volver", -0.1f, mVerdeLuz);
        botonMenu = BotonDeTotem(consola, "Boton_Menu", 0.1f, mSAzulBoton);

        // Los carteles de cada botón, debajo de la consola (justo debajo, su borde los taparía)
        Texto("Texto_Volver", g, new Vector3(-0.105f, 0.61f, -0.071f), Vector3.back, "<b>VOLVER\nA JUGAR</b>", new Vector2(0.19f, 0.09f),
              new Color(0.35f, 1f, 0.5f));
        Texto("Detalle_Volver", g, new Vector3(-0.105f, 0.54f, -0.071f), Vector3.back, "Desde el Cuarto 1", new Vector2(0.19f, 0.03f),
              new Color(0.75f, 0.8f, 0.86f));
        Texto("Texto_Menu", g, new Vector3(0.105f, 0.61f, -0.071f), Vector3.back, "<b>MENÚ\nPRINCIPAL</b>", new Vector2(0.19f, 0.09f),
              new Color(0.38f, 0.65f, 0.98f));
        Texto("Detalle_Menu", g, new Vector3(0.105f, 0.54f, -0.071f), Vector3.back, "Pantalla de inicio", new Vector2(0.19f, 0.03f),
              new Color(0.75f, 0.8f, 0.86f));
        return volver;
    }

    // Un botón redondo de la consola del tótem: el aro y la tapa que se hunde al presionarla
    static PressableButton BotonDeTotem(Transform consola, string nombre, float x, Material color)
    {
        Transform grupo = Grupo(nombre, consola);
        grupo.localPosition = new Vector3(x, 0.025f, 0f);
        Cilindro("Aro", grupo, new Vector3(0f, 0.004f, 0f), new Vector3(0.13f, 0.004f, 0.13f), mSAluminioClaro);
        GameObject tapa = Cilindro("Tapa", grupo, new Vector3(0f, 0.018f, 0f), new Vector3(0.105f, 0.014f, 0.105f), color, true);
        tapa.AddComponent<XRSimpleInteractable>();
        var boton = tapa.AddComponent<PressableButton>();
        boton.parteMovil = tapa.transform;
        boton.recorrido = 0.01f;
        tapa.AddComponent<ResaltarAlApuntar>();
        return boton;
    }

    // ------------------------------------------------------------------ afuera

    // Afuera del colegio, de día: la explanada de la entrada; la fachada de dos pisos (con una franja
    // de ventanas y un revestimiento de madera); el alero sobre la puerta con el nombre del colegio;
    // dos apliques de pared, dos jardineras, un banco y la reja alrededor. Más allá, la foto 360°.
    static void Exterior(Transform p)
    {
        Transform g = Grupo("Exterior", p);
        float z0 = P_LARGO;                   // la línea de la puerta principal
        float zF = P_LARGO + F_GROSOR;        // la cara de afuera de la fachada

        GameObject piso = Cubo("Piso_Explanada", g, new Vector3((E_IZQ + E_DER) / 2f, -0.1f, (z0 + E_FONDO) / 2f),
                               new Vector3(E_DER - E_IZQ, 0.2f, E_FONDO - z0), mSConcreto, true);
        ZonaDeTeleport(piso);

        // La fachada: a los costados de la entrada y arriba de ella. La puerta queda metida 30 cm.
        float zc = z0 + F_GROSOR / 2f;
        Cubo("Fachada_Izq", g, new Vector3((E_IZQ + P_IZQ) / 2f, F_ALTO / 2f, zc), new Vector3(P_IZQ - E_IZQ, F_ALTO, F_GROSOR), mSFachada, true);
        Cubo("Fachada_Der", g, new Vector3((P_DER + E_DER) / 2f, F_ALTO / 2f, zc), new Vector3(E_DER - P_DER, F_ALTO, F_GROSOR), mSFachada, true);
        Cubo("Fachada_Arriba", g, new Vector3(P_CX, (P_TECHO + F_ALTO) / 2f, zc), new Vector3(P_DER - P_IZQ, F_ALTO - P_TECHO, F_GROSOR), mSFachada, true);
        Cubo("Remate", g, new Vector3((E_IZQ + E_DER) / 2f, F_ALTO + 0.05f, zc), new Vector3(E_DER - E_IZQ + 0.1f, 0.1f, F_GROSOR + 0.06f), mSAluminio);
        Cubo("Revestimiento_Madera", g, new Vector3(-2.2f, 1.8f, zF + 0.015f), new Vector3(2.4f, 3.6f, 0.03f), mSMadera);

        // El segundo piso: una franja de ventanas oscuras con sus montantes
        float anchoVentanas = E_DER - E_IZQ - 0.6f;
        Cubo("Ventanas", g, new Vector3((E_IZQ + E_DER) / 2f, 5.3f, zF + 0.01f), new Vector3(anchoVentanas, 2f, 0.02f), mSVidrioOscuro);
        Cubo("Ventanas_Dintel", g, new Vector3((E_IZQ + E_DER) / 2f, 6.33f, zF + 0.025f), new Vector3(anchoVentanas + 0.1f, 0.06f, 0.05f), mSAluminio);
        Cubo("Ventanas_Alfeizar", g, new Vector3((E_IZQ + E_DER) / 2f, 4.27f, zF + 0.035f), new Vector3(anchoVentanas + 0.1f, 0.06f, 0.07f), mSAluminio);
        for (float x = E_IZQ + 0.3f; x <= E_DER - 0.29f; x += anchoVentanas / 8f)
            Cubo("Montante", g, new Vector3(x, 5.3f, zF + 0.025f), new Vector3(0.06f, 2f, 0.04f), mSAluminio);

        // El alero sobre la entrada, con dos columnas finas, luces empotradas y el nombre del colegio
        const float ALTO_ALERO = 3.5f, SALIENTE = 2.3f;
        float anchoAlero = P_DER - P_IZQ + 0.8f;
        Cubo("Alero", g, new Vector3(P_CX, ALTO_ALERO, zF + SALIENTE / 2f), new Vector3(anchoAlero, 0.18f, SALIENTE), mBlanco, true);
        Cubo("Frente_Alero", g, new Vector3(P_CX, ALTO_ALERO, zF + SALIENTE + 0.01f), new Vector3(anchoAlero, 0.32f, 0.02f), mSAluminio);
        Texto("Nombre_Colegio", g, new Vector3(P_CX, ALTO_ALERO, zF + SALIENTE + 0.022f), Vector3.forward, "<b>C O L E G I O</b>",
              new Vector2(anchoAlero - 0.4f, 0.2f), Color.white);
        foreach (float s in new[] { -1f, 1f })
            Cubo("Columna", g, new Vector3(P_CX + s * (anchoAlero / 2f - 0.2f), (ALTO_ALERO - 0.09f) / 2f, zF + SALIENTE - 0.15f),
                 new Vector3(0.1f, ALTO_ALERO - 0.09f, 0.1f), mSAluminio, true);
        for (int i = -1; i <= 1; i++)
            Cilindro("Luz_Alero", g, new Vector3(P_CX + i * 1.3f, ALTO_ALERO - 0.092f, zF + SALIENTE / 2f), new Vector3(0.16f, 0.004f, 0.16f), mLuzTecho);

        // Apliques modernos a los dos lados de la entrada: una caja de aluminio oscuro que alumbra
        // hacia arriba y hacia abajo (dos líneas de luz)
        Aplique(g, new Vector3(-2.2f, 2.4f, zF + 0.03f));
        Aplique(g, new Vector3(P_DER + 1.4f, 2.4f, zF));

        // Jardineras a los costados del camino y el banco de la explanada, mirando al colegio
        Jardinera(g, new Vector3(-2.4f, 0f, zF + 2.4f), 0f);
        Jardinera(g, new Vector3(P_DER + 1.8f, 0f, zF + 2.4f), 0f);
        ModeloPolyHaven("modular_street_seating", g, new Vector3(P_CX, 0f, E_FONDO - 2.6f), 180f, 0f, Apoyo.Piso, true);

        // La reja del colegio alrededor de la explanada
        Reja(g, new Vector3(E_IZQ, 0f, zF), new Vector3(E_IZQ, 0f, E_FONDO));
        Reja(g, new Vector3(E_IZQ, 0f, E_FONDO), new Vector3(E_DER, 0f, E_FONDO));
        Reja(g, new Vector3(E_DER, 0f, E_FONDO), new Vector3(E_DER, 0f, zF));

        // El sol: una luz grande sobre la explanada (una luz direccional iluminaría también el
        // interior de los cuartos, porque las luces sin sombra atraviesan las paredes)
        LuzPunto("Luz_Sol", g, new Vector3(P_CX, 6f, E_FONDO - 4f), new Color(1f, 0.96f, 0.88f), 2.6f, 16f);
    }

    static void Aplique(Transform p, Vector3 pared)
    {
        Transform g = Grupo("Aplique", p);
        g.localPosition = pared;
        Cubo("Caja", g, new Vector3(0f, 0f, 0.06f), new Vector3(0.1f, 0.42f, 0.12f), mSAluminio);
        Cubo("Luz_Arriba", g, new Vector3(0f, 0.212f, 0.06f), new Vector3(0.07f, 0.004f, 0.09f), mLuzTecho);
        Cubo("Luz_Abajo", g, new Vector3(0f, -0.212f, 0.06f), new Vector3(0.07f, 0.004f, 0.09f), mLuzTecho);
    }

    // Reja de "a" a "b": muro bajo de hormigón, barrotes verticales de aluminio, postes cada 2 m y
    // la baranda de arriba. Una caja invisible no deja pasar al jugador entre los barrotes.
    static void Reja(Transform p, Vector3 a, Vector3 b)
    {
        Transform g = Grupo("Reja", p);
        g.localPosition = a;
        g.localRotation = Quaternion.LookRotation(b - a);   // la reja avanza por su +Z
        float largo = Vector3.Distance(a, b);

        Cubo("Muro_Bajo", g, new Vector3(0f, 0.25f, largo / 2f), new Vector3(0.25f, 0.5f, largo), mSConcreto, true);
        Cubo("Baranda", g, new Vector3(0f, 1.63f, largo / 2f), new Vector3(0.05f, 0.04f, largo), mSAluminio);
        for (float z = 0.12f; z < largo - 0.05f; z += 0.14f)
            Cubo("Barrote", g, new Vector3(0f, 1.06f, z), new Vector3(0.02f, 1.12f, 0.035f), mSAluminio);
        for (float z = 0f; z <= largo + 0.01f; z += 2f)
            Cubo("Poste", g, new Vector3(0f, 0.9f, z), new Vector3(0.08f, 1.8f, 0.08f), mSAluminio);

        Transform bloqueo = Grupo("Bloqueo", g);
        var caja = bloqueo.gameObject.AddComponent<BoxCollider>();
        caja.center = new Vector3(0f, 0.9f, largo / 2f);
        caja.size = new Vector3(0.1f, 1.8f, largo);
    }

    // El cielo: una foto 360° de una plaza con árboles (Poly Haven, CC0) como "skybox" de la
    // escena. Adentro de los cuartos no se ve (están cerrados): solo por la puerta principal y en la
    // explanada. La luz ambiental la sigue poniendo cada cuarto (ClimaCuarto), no el cielo.
    static void PonerCielo()
    {
        var importador = AssetImporter.GetAtPath(RUTA_CIELO) as TextureImporter;
        if (importador == null)
        {
            Debug.LogWarning("Cuarto 4: no está el cielo " + RUTA_CIELO + ". Afuera queda el cielo de Unity.");
            return;
        }
        // Sin mipmaps (con mipmaps se ve una costura vertical en el cielo) y a 2k como máximo
        if (importador.mipmapEnabled || importador.maxTextureSize != 2048 || importador.wrapModeV != TextureWrapMode.Clamp)
        {
            importador.mipmapEnabled = false;
            importador.maxTextureSize = 2048;
            importador.wrapModeU = TextureWrapMode.Repeat;
            importador.wrapModeV = TextureWrapMode.Clamp;
            importador.SaveAndReimport();
        }

        const string ruta = "Assets/Materials/Cuarto4/C4_Cielo.mat";
        var shader = Shader.Find("Skybox/Panoramic");
        var m = AssetDatabase.LoadAssetAtPath<Material>(ruta);
        if (m == null)
        {
            m = new Material(shader);
            AssetDatabase.CreateAsset(m, ruta);
        }
        m.SetTexture("_MainTex", AssetDatabase.LoadAssetAtPath<Texture2D>(RUTA_CIELO));
        m.SetFloat("_Mapping", 1f);     // la foto viene "estirada" (latitud-longitud)
        m.SetFloat("_ImageType", 0f);   // de 360 grados
        m.SetFloat("_Exposure", 0.9f);
        m.SetFloat("_Rotation", GIRO_CIELO);
        EditorUtility.SetDirty(m);
        RenderSettings.skybox = m;
    }

    static void ZonaDeTeleport(GameObject piso)
    {
        var area = piso.AddComponent<TeleportationArea>();
        int capa = InteractionLayerMask.GetMask("Teleport");
        if (capa == 0) capa = 1 << 31;
        area.interactionLayers = capa;
        EditorUtility.SetDirty(area);
    }

    // Las texturas de terrazo, cielorraso y hormigón de Poly Haven son marrones: en el piso, el
    // techo y la explanada se usa solo su relieve, con un color gris o blanco liso (más moderno)
    static void CrearMaterialesSalida()
    {
        mSPiso = Mat("C4_SalidaPiso", new Color(0.7f, 0.71f, 0.72f), 0f, 0.6f, default, "terrazzo_tiles", 2f, 6f, false, true);
        mSPared = Mat("C4_SalidaPared", new Color(0.95f, 0.95f, 0.93f), 0f, 0.15f, default, "white_stucco", 6f, 2f, true);
        mSFachada = Mat("C4_SalidaFachada", new Color(0.9f, 0.9f, 0.88f), 0f, 0.15f, default, "white_stucco", 5f, 4f, true);
        mSMadera = Mat("C4_SalidaMadera", Color.white, 0f, 0.35f, default, "kitchen_wood", 0.3f, 1f);
        mSTecho = Mat("C4_SalidaTecho", new Color(0.93f, 0.93f, 0.92f), 0f, 0.1f, default, "ceiling_interior", 2f, 6f, false, true);
        mSConcreto = Mat("C4_SalidaConcreto", new Color(0.62f, 0.62f, 0.6f), 0f, 0.2f, default, "concrete_panels", 4f, 4f, false, true);
        mSAlfombra = Mat("C4_SalidaAlfombra", new Color(0.32f, 0.33f, 0.35f), 0f, 0.05f, default, "poly_wool_herringbone", 2f, 1.3f, true);

        mSAluminio = Mat("C4_SalidaAluminio", new Color(0.16f, 0.17f, 0.18f), 0.8f, 0.55f);
        mSAluminioClaro = Mat("C4_SalidaAluminioClaro", new Color(0.78f, 0.8f, 0.82f), 0.9f, 0.7f);
        mSCasillero = Mat("C4_SalidaCasillero", new Color(0.07f, 0.38f, 0.5f), 0.25f, 0.5f);
        mSVidrioOscuro = Mat("C4_SalidaVidrioOscuro", new Color(0.04f, 0.06f, 0.08f), 0.2f, 0.85f);
        mSVidrio = Transparente("C4_SalidaVidrio", new Color(0.82f, 0.93f, 0.96f, 0.16f), default);

        // Pantallas mate (si brillan, reflejan el cielo y no se leen) y el verde del cartel de SALIDA
        mSPantalla = Mat("C4_SalidaPantalla", new Color(0.02f, 0.035f, 0.06f), 0f, 0.25f, new Color(0.012f, 0.03f, 0.06f));
        mSVerdeSalida = Mat("C4_SalidaVerde", new Color(0f, 0.42f, 0.2f), 0f, 0.4f, new Color(0f, 0.3f, 0.13f));
        mSAzulBoton = Mat("C4_SalidaAzulBoton", new Color(0.25f, 0.55f, 1f), 0f, 0.5f, new Color(0.12f, 0.3f, 0.75f));
    }
}
