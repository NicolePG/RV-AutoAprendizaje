using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;

// Arma el Cuarto 3 (Sala de Computación) completo adentro del objeto "Cuarto3_Computacion".
// Se corre desde el menú: Escape Room > Construir los 4 cuartos. Borra lo que había adentro y lo
// rehace, así siempre queda igual. No colocar nada a mano adentro: se borra al reconstruir.
//
// Este archivo arma la sala (estructura, muebles, luces y ambiente). La noche, los 4 acertijos
// (luces, red, computadoras y consola) y el susto de las sillas están en la otra parte del
// constructor: ConstructorCuarto3Acertijos.cs.
//
// Va entre el Cuarto 2 y el Cuarto 4, en el lugar del pasillo provisional (que se apaga).
// Se arma mirando hacia +Z, igual que los Cuartos 2 y 4: la entrada (que viene de la Dirección)
// en z = 0 y la salida al Laboratorio al fondo (z = FONDO). Visto al entrar, x = 0 queda a la
// izquierda. Dónde va en la escena está en DisposicionCuartos.
//
// Distribución (GDD, sección 9, agrandado como los otros cuartos):
//  - Izquierda: mesada de melamina con las 5 computadoras C1..C5 (monitores de tubo).
//  - Centro: pupitres con las sillas dadas vuelta encima, como un colegio que cerró.
//  - Derecha: escritorio del profesor, pantalla de proyector y un reloj parado a las 4:40
//    (la hora del apagón, igual que los relojes de la Dirección).
//  - Junto a la entrada: rack de red con cables que suben a un hueco del techo (se cayó la
//    placa), escombros y la silla S1 volcada en el paso.
//  - Fondo: consola de acceso, puerta al Laboratorio, pizarra y un depósito con cajas.
//  - Solo tres sillas en el piso, S1, S2 y S3: son las del susto (se agrega después).
//
// Arte: modelos y texturas de Poly Haven (Assets/PolyHaven, CC0) y, para lo moderno, modelos
// de Sketchfab (Assets/Sketchfab, CC BY: los autores están en Assets/Sketchfab/CREDITOS.txt):
// las PC, el rack de servidores, las sillas de oficina, el proyector, la impresora y la pizarra.
// Si falta un modelo de Sketchfab se usa el diseño de antes. Lo que no está en ninguno de los
// dos sitios (la mesada, la puerta, la consola) se arma con piezas.
public static partial class ConstructorCuarto3
{
    const string NOMBRE_RAIZ = "Cuarto3_Computacion";
    const string CARPETA_MODELOS = "Assets/PolyHaven/Modelos";
    const string CARPETA_TEXTURAS = "Assets/PolyHaven/Texturas";
    const string CARPETA_SKETCHFAB = "Assets/Sketchfab";
    const string CARPETA_MATERIALES = "Assets/Materials/Cuarto3";

    const float ANCHO = 8f;       // eje X: pared izquierda (0) a pared derecha (8)
    const float FONDO = 7.52f;    // eje Z: entrada (0) al fondo (7.52)
    const float ALTO = 3.2f;      // altura del techo
    const float MURO = 0.12f;     // espesor de las paredes

    // Huecos de las puertas: coinciden con la salida del Cuarto 2 y la entrada del Cuarto 4
    const float ENTRADA_X0 = 4.4f, ENTRADA_X1 = 5.6f;
    const float SALIDA_X0 = 0.8f, SALIDA_X1 = 1.9f;
    const float ALTO_PUERTA = 2.1f;

    const float ALTO_ZOCALO = 1.1f;    // hasta dónde llega la pintura oscura de abajo
    const float ALTO_MESADA = 0.76f;
    const float FIN_MESADA = 5.3f;     // la mesada va de z = 0.5 hasta acá; después está el panel de red
    const float FONDO_MESADA = 0.75f;

    // Dónde va cada computadora a lo largo de la mesada (C1 junto a la entrada, C5 hacia el fondo)
    static readonly float[] Z_PUESTOS = { 1.0f, 1.95f, 2.9f, 3.85f, 4.8f };

    static readonly float[] X_PUPITRES = { 2.9f, 4.2f };
    static readonly float[] Z_PUPITRES = { 2.2f, 3.4f, 4.6f };

    // Alturas de las repisas del estante metálico (steel_frame_shelves_02, medidas en el modelo)
    static readonly float[] REPISAS = { 0.13f, 0.64f, 1.15f, 1.65f };
    const float ALTO_ESTANTE = 2.14f;

    // Cómo se apoya un modelo en el punto que se le da
    enum Apoyo { Piso, Centro, Pared, Techo }

    static Material mParedAlta, mParedBaja, mFranja, mPiso, mTecho, mPerfil, mMarco, mPuerta,
                    mAcero, mAceroOscuro, mMelamina, mCanto, mPlasticoPC, mPlasticoNegro, mTeclas,
                    mPantallaEspera, mPantallaConsola, mLedVerde, mLedAmbar, mLedRojo, mSalida,
                    mTuboEncendido, mTuboApagado, mVidrio, mHueco, mPapel, mCable, mCanaleta,
                    mPizarra, mPlaca, mCarton, mCinta;

    // Tubos de la lámpara que parpadea: se conectan a su luz al final
    static readonly List<Renderer> tubosParpadeantes = new List<Renderer>();

    // Lo llama el menú Escape Room > Construir los 4 cuartos (MenuEscapeRoom)
    public static void Construir()
    {
        var raiz = GameObject.Find(NOMBRE_RAIZ);
        if (raiz == null)
        {
            raiz = new GameObject(NOMBRE_RAIZ);
            Undo.RegisterCreatedObjectUndo(raiz, "Construir Cuarto 3");
        }
        // Entre el Cuarto 2 y el Cuarto 4 (ver DisposicionCuartos)
        raiz.transform.SetPositionAndRotation(DisposicionCuartos.Cuarto3, DisposicionCuartos.Giro);

        // Se borra lo que haya adentro para reconstruir el cuarto desde cero
        for (int i = raiz.transform.childCount - 1; i >= 0; i--)
            Undo.DestroyObjectImmediate(raiz.transform.GetChild(i).gameObject);

        ApagarPasilloProvisional();
        tubosParpadeantes.Clear();
        computadorasSala.Clear();
        CrearMateriales();

        Transform estructura = Grupo("Estructura", raiz.transform);
        Transform mobiliario = Grupo("Mobiliario", raiz.transform);
        Transform luces = Grupo("Luces", raiz.transform);

        ArmarEstructura(estructura);
        ArmarPintura(estructura);
        ArmarCielorraso(estructura);

        ArmarPuestos(mobiliario);
        ArmarPupitres(mobiliario);
        ArmarProfesor(mobiliario);
        ArmarRack(mobiliario);
        ArmarDeposito(mobiliario);
        ArmarEscombros(mobiliario);
        ArmarSillas(mobiliario);
        ArmarPizarra(mobiliario);
        ArmarConsola(mobiliario);
        ArmarCarteles(mobiliario);
        ArmarPuertaSalida(raiz.transform);

        ArmarLuces(luces);
        ArmarClima(raiz.transform);
        ArmarAcertijos(raiz.transform);   // la noche, los 4 acertijos y el susto (ConstructorCuarto3Acertijos.cs)
        PrepararParaQuest(raiz.transform);

        foreach (Transform hijo in raiz.transform)
            Undo.RegisterCreatedObjectUndo(hijo.gameObject, "Construir Cuarto 3");

        AssetDatabase.SaveAssets();
        OptimizarEscena.Preparar();   // que el juego dibuje solo los cuartos que se ven
        EditorSceneManager.MarkSceneDirty(raiz.scene);
        Selection.activeGameObject = raiz;
        Debug.Log("Cuarto 3 armado. Guarda la escena con Ctrl+S.");
    }

    // Atajo para probar el cuarto sin jugar los anteriores: deja al jugador parado apenas
    // pasando la entrada, mirando hacia el fondo. Se deshace con Ctrl+Z.
    // Lo llama el menú Escape Room > Llevar jugador al Cuarto 3 (MenuEscapeRoom)
    public static void LlevarJugador()
    {
        var raiz = GameObject.Find(NOMBRE_RAIZ);
        if (raiz == null)
        {
            Debug.LogWarning("Primero hay que construir el Cuarto 3.");
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

        Undo.RecordObject(jugador, "Llevar jugador al Cuarto 3");
        // TransformPoint y no una suma, porque el cuarto está girado (ver DisposicionCuartos)
        // A 60 cm de la entrada: adentro de la zona del clima y afuera de la zona del susto de la silla S1
        jugador.position = raiz.transform.TransformPoint(new Vector3((ENTRADA_X0 + ENTRADA_X1) / 2f, 0f, 0.6f));
        jugador.rotation = raiz.transform.rotation;
        Selection.activeGameObject = jugador.gameObject;
    }

    // El pasillo provisional ocupaba justo este lugar mientras no existía el Cuarto 3.
    // Se apaga (no se borra): si hiciera falta, se vuelve a prender desde el Inspector.
    static void ApagarPasilloProvisional()
    {
        var pasillo = GameObject.Find("Pasillo_Provisional");
        if (pasillo == null) return;
        Undo.RecordObject(pasillo, "Construir Cuarto 3");
        pasillo.SetActive(false);
        Debug.Log("Se apagó el Pasillo_Provisional: ahora en su lugar está el Cuarto 3.");
    }

    // ------------------------------------------------------------------ estructura

    static void ArmarEstructura(Transform p)
    {
        // El piso es también la zona de teletransporte. Se estira por debajo de las paredes
        // hasta tocar el piso del Cuarto 2 (z = -0.24) y el del Cuarto 4 (z = FONDO + 0.24):
        // así no queda un hueco en el umbral de cada puerta.
        float z0 = -2f * MURO, z1 = FONDO + 2f * MURO;
        GameObject piso = Cubo("Piso", p, new Vector3(ANCHO / 2f, -0.1f, (z0 + z1) / 2f),
                               new Vector3(ANCHO + 2f * MURO, 0.2f, z1 - z0), mPiso, true);
        var area = piso.AddComponent<TeleportationArea>();
        int capaTeleport = InteractionLayerMask.GetMask("Teleport");
        if (capaTeleport == 0) capaTeleport = 1 << 31;
        area.interactionLayers = capaTeleport;

        Transform paredes = Grupo("Paredes", p);
        Cubo("Pared_Izquierda", paredes, new Vector3(-MURO / 2f, ALTO / 2f, FONDO / 2f),
             new Vector3(MURO, ALTO, FONDO), mParedAlta, true);
        Cubo("Pared_Derecha", paredes, new Vector3(ANCHO + MURO / 2f, ALTO / 2f, FONDO / 2f),
             new Vector3(MURO, ALTO, FONDO), mParedAlta, true);
        // Pared de la entrada (pegada a la del fondo del Cuarto 2), partida por el vano
        MuroConVano(paredes, "Pared_Entrada", -MURO / 2f, ENTRADA_X0, ENTRADA_X1);
        // Pared del fondo (pegada a la de la entrada del Cuarto 4), partida por la puerta
        MuroConVano(paredes, "Pared_Fondo", FONDO + MURO / 2f, SALIDA_X0, SALIDA_X1);

        Cubo("Techo", p, new Vector3(ANCHO / 2f, ALTO + MURO / 2f, FONDO / 2f),
             new Vector3(ANCHO + 2f * MURO, MURO, FONDO + 2f * MURO), mTecho, true);
    }

    static void MuroConVano(Transform p, string nombre, float z, float x0, float x1)
    {
        Muro(p, nombre + "_Izq", -MURO, x0, z, 0f, ALTO);
        Muro(p, nombre + "_Der", x1, ANCHO + MURO, z, 0f, ALTO);
        Muro(p, nombre + "_Dintel", x0, x1, z, ALTO_PUERTA, ALTO);
    }

    static void Muro(Transform p, string nombre, float x0, float x1, float z, float y0, float y1)
    {
        Cubo(nombre, p, new Vector3((x0 + x1) / 2f, (y0 + y1) / 2f, z),
             new Vector3(x1 - x0, y1 - y0, MURO), mParedAlta, true);
    }

    // Pintura de las paredes: abajo gris azulado oscuro hasta 1.10 m, una franja ámbar (el
    // color de acento del juego, como en el Cuarto 1) y arriba gris claro. Son chapas finitas
    // pegadas a la pared: la de abajo sobresale 1 cm y la franja 1.6 cm, así ninguna cara
    // queda en el mismo plano que otra (si no, parpadean al moverse).
    static void ArmarPintura(Transform p)
    {
        Transform g = Grupo("Pintura", p);
        PinturaLateral(g, "Izquierda", 0f, 1f);
        PinturaLateral(g, "Derecha", ANCHO, -1f);
        // Entrada y fondo, cortadas donde están las puertas
        PinturaFrontal(g, "Entrada_A", 0f, ENTRADA_X0, 0f, 1f);
        PinturaFrontal(g, "Entrada_B", ENTRADA_X1, ANCHO, 0f, 1f);
        PinturaFrontal(g, "Fondo_A", 0f, SALIDA_X0, FONDO, -1f);
        PinturaFrontal(g, "Fondo_B", SALIDA_X1, ANCHO, FONDO, -1f);
    }

    // Pared a lo largo de Z, en x = xPared. "adentro" = +1 si el cuarto queda hacia +X.
    static void PinturaLateral(Transform p, string nombre, float xPared, float adentro)
    {
        Cubo("Zocalo_" + nombre, p, new Vector3(xPared + adentro * 0.005f, ALTO_ZOCALO / 2f, FONDO / 2f),
             new Vector3(0.01f, ALTO_ZOCALO, FONDO), mParedBaja);
        Cubo("Franja_" + nombre, p, new Vector3(xPared + adentro * 0.008f, ALTO_ZOCALO + 0.02f, FONDO / 2f),
             new Vector3(0.016f, 0.04f, FONDO), mFranja);
    }

    // Pared a lo largo de X, en z = zPared, de x0 a x1. "adentro" = +1 si el cuarto queda hacia +Z.
    static void PinturaFrontal(Transform p, string nombre, float x0, float x1, float zPared, float adentro)
    {
        Cubo("Zocalo_" + nombre, p, new Vector3((x0 + x1) / 2f, ALTO_ZOCALO / 2f, zPared + adentro * 0.005f),
             new Vector3(x1 - x0, ALTO_ZOCALO, 0.01f), mParedBaja);
        Cubo("Franja_" + nombre, p, new Vector3((x0 + x1) / 2f, ALTO_ZOCALO + 0.02f, zPared + adentro * 0.008f),
             new Vector3(x1 - x0, 0.04f, 0.016f), mFranja);
    }

    // Cielorraso de placas: grilla de perfiles de aluminio cada 1 m a lo ancho y 1.25 m a lo
    // largo, con seis lámparas de tubos. Cuatro están prendidas, una parpadea y una está
    // quemada. Sobre el rack falta una placa: se cayó y está en el piso, con los escombros.
    static void ArmarCielorraso(Transform p)
    {
        Transform g = Grupo("Cielorraso", p);
        float y = ALTO - 0.006f;
        for (int i = 1; i < 8; i++)
            Cubo("Perfil_X" + i, g, new Vector3(i, y, FONDO / 2f), new Vector3(0.025f, 0.012f, FONDO), mPerfil);
        for (int i = 1; i <= 6; i++)
            Cubo("Perfil_Z" + i, g, new Vector3(ANCHO / 2f, y, i * 1.25f), new Vector3(ANCHO, 0.012f, 0.025f), mPerfil);

        // Cada lámpara va al centro de una placa. De noche están todas apagadas: las prende el
        // tablero de luces (acertijo 1). Quedan guardadas en este orden: izquierda de adelante
        // hacia el fondo (0, 1, 2) y derecha de adelante hacia el fondo (3, 4, 5).
        // La del medio a la derecha (4) es la que parpadea una vez resuelto el tablero.
        float[] xs = { 2.5f, 5.5f };
        float[] zs = { 1.875f, 4.375f, 6.875f };
        lamparasTecho.Clear();
        for (int ix = 0; ix < xs.Length; ix++)
            for (int iz = 0; iz < zs.Length; iz++)
                ArmarLampara(g, new Vector3(xs[ix], ALTO - 0.002f, zs[iz]), ix, ix == 1 && iz == 1);

        // Hueco de la placa caída (sobre el rack): se ve negro desde abajo, con cables colgando
        Cubo("Hueco_Placa", g, new Vector3(6.5f, ALTO - 0.001f, 0.625f), new Vector3(0.975f, 0.004f, 1.225f), mHueco);
        Cable(g, new Vector3(6.25f, ALTO, 0.8f), new Vector3(6.3f, 2.35f, 0.86f), 0.012f);
        Cable(g, new Vector3(6.72f, ALTO, 0.95f), new Vector3(6.64f, 2.6f, 1.0f), 0.01f);
    }

    // Lámpara fluorescente de techo armada con piezas: carcasa de chapa, reflector blanco, dos
    // tubos y sus cabezales. Antes era el modelo de Poly Haven, pero tenía 18 mil triángulos por
    // lámpara (más de 100 mil las seis): demasiado para el Quest en algo que se ve a 3 m de
    // altura. Así cada una tiene unos 200. Los tubos empiezan apagados (los prende el tablero).
    static void ArmarLampara(Transform p, Vector3 pos, int lado, bool parpadea)
    {
        Transform t = Grupo("Lampara_" + (lamparasTecho.Count + 1), p);
        t.localPosition = pos;
        Cubo("Carcasa", t, new Vector3(0f, -0.02f, 0f), new Vector3(0.96f, 0.04f, 0.66f), mPerfil);
        Cubo("Reflector", t, new Vector3(0f, -0.0405f, 0f), new Vector3(0.9f, 0.001f, 0.6f), mMelamina);
        Cubo("Cabezal_A", t, new Vector3(-0.445f, -0.058f, 0f), new Vector3(0.03f, 0.036f, 0.42f), mPerfil);
        Cubo("Cabezal_B", t, new Vector3(0.445f, -0.058f, 0f), new Vector3(0.03f, 0.036f, 0.42f), mPerfil);

        var tubos = new List<Renderer>();
        foreach (float z in new[] { -0.14f, 0.14f })
        {
            GameObject tubo = Cilindro("Tubo", t, new Vector3(0f, -0.065f, z), new Vector3(0.028f, 0.43f, 0.028f), mTuboApagado);
            tubo.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);   // acostado, de cabezal a cabezal
            tubos.Add(tubo.GetComponent<Renderer>());
        }
        if (parpadea) tubosParpadeantes.AddRange(tubos);
        lamparasTecho.Add(new TableroLuces.Lampara { tubos = tubos.ToArray(), lado = lado });
    }

    // ------------------------------------------------------------------ puestos de computación

    static void ArmarPuestos(Transform p)
    {
        Transform g = Grupo("Puestos", p);
        float z0 = 0.5f, z1 = FIN_MESADA;

        // Mesada de melamina de 4.8 m contra la pared izquierda, con un canto oscuro al frente
        // y laterales cada dos puestos (entre los gabinetes)
        Cubo("Mesada", g, new Vector3(FONDO_MESADA / 2f, ALTO_MESADA - 0.015f, (z0 + z1) / 2f),
             new Vector3(FONDO_MESADA, 0.03f, z1 - z0), mMelamina, true);
        Cubo("Mesada_Canto", g, new Vector3(FONDO_MESADA + 0.002f, ALTO_MESADA - 0.015f, (z0 + z1) / 2f),
             new Vector3(0.004f, 0.03f, z1 - z0), mCanto);
        foreach (float z in new[] { z0 + 0.015f, 2.425f, 4.325f, z1 - 0.015f })
            Cubo("Lateral", g, new Vector3(FONDO_MESADA / 2f, (ALTO_MESADA - 0.03f) / 2f, z),
                 new Vector3(FONDO_MESADA - 0.03f, ALTO_MESADA - 0.03f, 0.03f), mMelamina, true);

        // Canaleta blanca de cables en la pared, por detrás de los monitores
        Cubo("Canaleta", g, new Vector3(0.037f, 0.95f, (z0 + z1) / 2f), new Vector3(0.05f, 0.05f, z1 - z0), mCanaleta);

        for (int i = 0; i < Z_PUESTOS.Length; i++)
            ArmarComputadora(g, i + 1, Z_PUESTOS[i]);
    }

    // Un puesto: la PC (monitor, torre y teclado) y la placa con su nombre. Todo queda dentro
    // de "Computadora_C1", etc., para agregarle el acertijo después: la pantalla se llama
    // "Pantalla" y el botón de encendido "Boton_Encendido".
    static void ArmarComputadora(Transform p, int numero, float z)
    {
        string nombre = "C" + numero;
        Transform g = Grupo("Computadora_" + nombre, p);
        g.localPosition = new Vector3(0f, 0f, z);

        if (!ArmarPCModerna(g, ALTO_MESADA)) ArmarPCAntigua(g, ALTO_MESADA);
        ConfigurarComputadora(g, numero);   // la deja lista para el acertijo 3 (ConstructorCuarto3Acertijos.cs)

        // Placa con el nombre en la pared, encima del monitor
        Cubo("Placa", g, new Vector3(0.005f, 1.52f, 0f), new Vector3(0.008f, 0.1f, 0.16f), mPlaca);
        Texto("Placa_Texto", g, new Vector3(0.0095f, 1.52f, 0f), Vector3.right, nombre,
              new Vector2(0.14f, 0.08f), Color.white);
    }

    // PC moderna de Sketchfab ("Desktop PC", de R-LAB): monitor plano, torre con su luz de
    // encendido y teclado, en un solo modelo. Viene en unidades propias: con 46 cm de alto
    // (del teclado a la punta del monitor) el monitor queda de 24" y todo entra en la mesada.
    // Su frente es +Z: girada 90° mira al cuarto. Devuelve false si el modelo no está.
    static bool ArmarPCModerna(Transform g, float mesa)
    {
        GameObject pc = ModeloSketchfab("desktop_pc", g, new Vector3(0.37f, mesa, 0f), 90f, 0.46f);
        if (pc == null) return false;

        // Se les pone nombre a las piezas que usa el acertijo: la pantalla (arranca apagada), la
        // lucecita de encendido y la torre (tocarla prende la computadora)
        foreach (Renderer r in pc.GetComponentsInChildren<Renderer>())
        {
            Material[] mats = r.sharedMaterials;
            for (int i = 0; i < mats.Length; i++)
            {
                if (mats[i] == null) continue;
                if (mats[i].name.Contains("Screen"))
                {
                    mats[i] = mPantallaApagada;
                    r.sharedMaterials = mats;
                    r.gameObject.name = "Pantalla";
                }
                else if (mats[i].name.Contains("Emission"))
                {
                    mats[i] = mLedApagado;
                    r.sharedMaterials = mats;
                    r.gameObject.name = "Led";
                }
                else if (r.gameObject.name.StartsWith("Cube.001"))
                {
                    r.gameObject.name = "Torre";
                }
            }
        }
        return true;
    }

    // La PC de antes (por si falta el modelo de Sketchfab): monitor de tubo de Poly Haven,
    // teclado y mouse armados con piezas y el gabinete bajo la mesada
    static void ArmarPCAntigua(Transform g, float mesa)
    {
        // Monitor CRT: television_02 mide 40 x 41 cm, lo que mide un monitor de 15". Su frente
        // es +Z: girado 90° mira al cuarto. Queda a 7 cm de la pared, detrás pasa la canaleta.
        GameObject monitor = Modelo("television_02", g, new Vector3(0.25f, mesa, 0f), 90f, 0f);
        if (monitor == null)
        {
            Transform t = Grupo("Monitor", g);
            t.localPosition = new Vector3(0.25f, mesa, 0f);
            t.localRotation = Quaternion.Euler(0f, 90f, 0f);
            monitor = t.gameObject;
            Cubo("Caja", t, new Vector3(0f, 0.2f, 0f), new Vector3(0.4f, 0.4f, 0.35f), mPlasticoPC);
        }
        // Pantalla que brilla (en espera: verde muy tenue). El acertijo la va a cambiar.
        // Medida sobre el modelo: centrada, a 26 cm de la base, 27 x 20 cm, 2 mm delante del vidrio.
        Cubo("Pantalla", monitor.transform, new Vector3(0f, 0.26f, 0.177f), new Vector3(0.27f, 0.2f, 0.003f), mPantallaEspera);

        // Teclado y mouse (Poly Haven no tiene): delante del monitor
        Cubo("Teclado", g, new Vector3(0.56f, mesa + 0.011f, 0f), new Vector3(0.15f, 0.022f, 0.44f), mPlasticoNegro);
        Cubo("Teclado_Teclas", g, new Vector3(0.555f, mesa + 0.0235f, 0f), new Vector3(0.12f, 0.003f, 0.41f), mTeclas);
        Cubo("Alfombrilla", g, new Vector3(0.56f, mesa + 0.0015f, 0.34f), new Vector3(0.2f, 0.003f, 0.22f), mCanto);
        Cubo("Mouse", g, new Vector3(0.56f, mesa + 0.018f, 0.34f), new Vector3(0.1f, 0.03f, 0.06f), mPlasticoNegro);

        // Gabinete bajo la mesada, con la lectora y el botón de encendido al frente (lo va a usar el acertijo)
        Cubo("Gabinete", g, new Vector3(0.4f, 0.21f, 0.27f), new Vector3(0.44f, 0.42f, 0.19f), mPlasticoPC, true);
        Cubo("Gabinete_Lectora", g, new Vector3(0.6215f, 0.35f, 0.27f), new Vector3(0.003f, 0.035f, 0.15f), mPlasticoNegro);
        GameObject boton = Cilindro("Boton_Encendido", g, new Vector3(0.6225f, 0.27f, 0.27f),
                                    new Vector3(0.022f, 0.0025f, 0.022f), mPlasticoNegro);
        boton.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        Cubo("Led_Encendido", g, new Vector3(0.6215f, 0.235f, 0.27f), new Vector3(0.003f, 0.006f, 0.006f), mCanto);
    }

    // ------------------------------------------------------------------ aula

    // Seis pupitres mirando a la pantalla del proyector (hacia +X), con la silla dada vuelta
    // encima, como se dejan al terminar el año. Uno quedó un poco torcido.
    static void ArmarPupitres(Transform p)
    {
        Transform g = Grupo("Pupitres", p);
        int n = 0;
        foreach (float x in X_PUPITRES)
            foreach (float z in Z_PUPITRES)
            {
                n++;
                Transform pupitre = Grupo("Pupitre_" + n, g);
                pupitre.localPosition = new Vector3(x, 0f, z);
                // El alumno se sienta del lado +Z del modelo: con -90° queda mirando hacia +X
                pupitre.localRotation = Quaternion.Euler(0f, n == 4 ? -81f : -90f, 0f);

                if (Modelo("SchoolDesk_01", pupitre, Vector3.zero, 0f, 0f, Apoyo.Piso, true) == null)
                    Cubo("Tabla", pupitre, new Vector3(0f, 0.86f, 0f), new Vector3(0.71f, 0.04f, 0.55f), mMelamina, true);

                // Silla dada vuelta: el asiento (a 0.52 m en el modelo) apoyado sobre la tabla
                // (a 0.88 m) y el respaldo colgando por detrás del pupitre
                ModeloLibre("SchoolChair_01", pupitre, new Vector3(0f, 0.882f + 0.517f + 0.003f, 0f), new Vector3(0f, 0f, 180f));
            }
    }

    // El frente de la clase: escritorio del profesor mirando a los pupitres, la pantalla del
    // proyector detrás y un reloj parado a las 4:40.
    static void ArmarProfesor(Transform p)
    {
        Transform g = Grupo("Profesor", p);
        const float X = 5.9f, Z = 3.4f;
        // El profesor se sienta del lado de los cajones (+Z del modelo): con 90° ese lado queda
        // hacia la pantalla y mira a los alumnos. El escritorio mide 2 m de largo.
        if (Modelo("metal_office_desk", g, new Vector3(X, 0f, Z), 90f, 0f, Apoyo.Piso, true) == null)
            Cubo("Escritorio", g, new Vector3(X, 0.39f, Z), new Vector3(0.95f, 0.78f, 2f), mAcero, true);

        const float tapa = 0.789f;
        Modelo("classic_laptop", g, new Vector3(X + 0.1f, tapa, Z - 0.25f), 90f, 0.24f);
        Modelo("clipboard", g, new Vector3(X + 0.05f, tapa, Z + 0.3f), 70f, 0f);
        // Impresora de Sketchfab en la punta del escritorio, con el frente hacia el profesor
        if (ModeloSketchfab("printer_low_poly", g, new Vector3(X - 0.05f, tapa, Z - 0.7f), 90f, 0f) == null)
            Modelo("stationery_supplies", g, new Vector3(X - 0.25f, tapa, Z - 0.75f), 100f, 0f);

        // Pantalla de proyector con trípode, contra la pared: su tela mira a +Z, con -90° mira al aula
        Modelo("projector_screen", g, new Vector3(7.4f, 0f, Z), -90f, 2.2f);
        // Proyector de Sketchfab colgado del techo sobre los pupitres, con la lente (+Z) hacia la
        // pantalla. Va entre dos perfiles del cielorraso (x = 4 y 5, z = 2.5 y 3.75).
        ModeloSketchfab("projector", g, new Vector3(4.5f, ALTO - 0.002f, Z), 90f, 0f, Apoyo.Techo);
        ArmarRelojParado(g, new Vector3(ANCHO, 2.55f, Z));
    }

    // Reloj de pared parado a las 4:40, la hora del apagón. Las agujas del modelo marcan las
    // 10:09:28 (medido en el Cuarto 1): se giran lo que falta, en sentido horario sobre su Z.
    static void ArmarRelojParado(Transform p, Vector3 posPared)
    {
        GameObject reloj = Modelo("wall_clock", p, posPared, -90f, 0.34f, Apoyo.Pared);
        if (reloj == null) return;
        GirarAguja(reloj.transform, "wall_clock_hours_hand", 4f * 30f + 40f * 0.5f, 306.8f);
        GirarAguja(reloj.transform, "wall_clock_minute_hand", 40f * 6f, 57f);
        GirarAguja(reloj.transform, "wall_clock_second_hand", 0f, 170.5f);
    }

    static void GirarAguja(Transform reloj, string nombre, float anguloFinal, float anguloModelo)
    {
        Transform aguja = BuscarHijo(reloj, nombre);
        if (aguja != null) aguja.localRotation *= Quaternion.Euler(0f, 0f, anguloFinal - anguloModelo);
    }

    // ------------------------------------------------------------------ rack y depósito

    // Rack de red junto a la entrada, con un mazo de cables que sube al hueco del techo.
    // Es el "Server Rack" de Sketchfab (de Spellkaze): 2 m de alto, 65 cm de lado, su frente es
    // -Z, así que girado 180° mira al cuarto. Si no está, se arma el estante con los equipos.
    static void ArmarRack(Transform p)
    {
        Transform g = Grupo("Rack_Red", p);
        const float X = 6.95f;
        if (ModeloSketchfab("server_rack", g, new Vector3(X, 0f, 0.345f), 180f, 0f, Apoyo.Piso, true) != null)
        {
            Cable(g, new Vector3(X - 0.15f, 2f, 0.3f), new Vector3(X - 0.3f, ALTO, 0.45f), 0.05f);
            Cable(g, new Vector3(X + 0.1f, 2f, 0.25f), new Vector3(X - 0.1f, ALTO, 0.6f), 0.03f);
            return;
        }
        ArmarEstanteRed(g, X, 0.26f);
    }

    // El rack de antes: estante metálico de Poly Haven con los equipos de la red del colegio
    // (switches con sus luces, un UPS y una placa madre suelta)
    static void ArmarEstanteRed(Transform g, float X, float Z)
    {
        if (Modelo("steel_frame_shelves_02", g, new Vector3(X, 0f, Z), 0f, 0f, Apoyo.Piso, true) == null)
            Cubo("Estante", g, new Vector3(X, ALTO_ESTANTE / 2f, Z), new Vector3(0.59f, ALTO_ESTANTE, 0.5f), mAceroOscuro, true);

        Cubo("UPS", g, new Vector3(X, REPISAS[0] + 0.17f, Z + 0.02f), new Vector3(0.2f, 0.34f, 0.4f), mPlasticoNegro);
        Cubo("UPS_Led", g, new Vector3(X, REPISAS[0] + 0.3f, Z + 0.2215f), new Vector3(0.012f, 0.012f, 0.003f), mLedVerde);
        Modelo("circuit_board", g, new Vector3(X, REPISAS[1], Z), 12f, 0f);
        Switch(g, new Vector3(X, REPISAS[2], Z + 0.03f), 0);
        Switch(g, new Vector3(X, REPISAS[2] + 0.045f, Z + 0.03f), 1);
        Switch(g, new Vector3(X, REPISAS[3], Z + 0.03f), 2);

        // Mazo de cables que sube del rack al hueco del techo, y uno suelto colgando
        Cable(g, new Vector3(X - 0.12f, ALTO_ESTANTE, Z + 0.1f), new Vector3(X - 0.3f, ALTO, Z + 0.3f), 0.05f);
        Cable(g, new Vector3(X + 0.05f, REPISAS[2] + 0.06f, Z + 0.17f), new Vector3(X - 0.2f, REPISAS[1] + 0.3f, Z + 0.26f), 0.015f);
    }

    // Switch de red: caja negra con una fila de luces al frente (verdes, ámbar y alguna apagada)
    static void Switch(Transform p, Vector3 apoyo, int variante)
    {
        Transform s = Grupo("Switch_" + (variante + 1), p);
        s.localPosition = apoyo;
        Cubo("Caja", s, new Vector3(0f, 0.0225f, 0f), new Vector3(0.44f, 0.045f, 0.28f), mPlasticoNegro);
        Cubo("Puertos", s, new Vector3(0f, 0.015f, 0.1405f), new Vector3(0.38f, 0.014f, 0.001f), mHueco);
        for (int i = 0; i < 10; i++)
        {
            if ((i + variante) % 4 == 3) continue;
            Material led = (i + variante) % 3 == 0 ? mLedAmbar : mLedVerde;
            Cubo("Led", s, new Vector3(-0.18f + i * 0.04f, 0.032f, 0.1415f), new Vector3(0.008f, 0.006f, 0.003f), led);
        }
    }

    // Depósito al fondo a la derecha: dos estantes con cajas, monitores viejos amontonados y
    // una bolsa de basura. Junto a la entrada, un archivero.
    static void ArmarDeposito(Transform p)
    {
        Transform g = Grupo("Deposito", p);
        float z = FONDO - 0.26f;
        foreach (float x in new[] { 6.75f, 7.45f })
            if (Modelo("steel_frame_shelves_02", g, new Vector3(x, 0f, z), 180f, 0f, Apoyo.Piso, true) == null)
                Cubo("Estante", g, new Vector3(x, ALTO_ESTANTE / 2f, z), new Vector3(0.59f, ALTO_ESTANTE, 0.5f), mAceroOscuro, true);

        // Cajas de archivo en las repisas, armadas con piezas (el modelo de Poly Haven tiene 17 mil
        // triángulos por caja; en el fondo del depósito no se nota la diferencia). Miden 39 x 50 cm:
        // giradas casi 90° entran en la repisa de 50 cm de fondo.
        CajaSimple(g, new Vector3(6.75f, REPISAS[0], z), 88f);
        CajaSimple(g, new Vector3(7.45f, REPISAS[0], z), 95f);
        CajaSimple(g, new Vector3(6.75f, REPISAS[2], z), 92f);
        CajaSimple(g, new Vector3(7.45f, REPISAS[1], z), 84f);

        // Dos televisores de tubo viejos, uno encima del otro, con la pantalla hacia el cuarto
        Modelo("Television_01", g, new Vector3(5.95f, 0f, FONDO - 0.35f), 172f, 0f);
        Modelo("Television_01", g, new Vector3(5.97f, 0.457f, FONDO - 0.37f), 186f, 0f);
        Modelo("trashbag", g, new Vector3(5.3f, 0f, FONDO - 0.4f), 30f, 0f);

        ArmarArchivero(g, new Vector3(1.45f, 0f, 0.26f));
    }

    static void Caja(Transform p, Vector3 pos, float giro) => Modelo("cardboard_box_01", p, pos, giro, 0f);

    // Caja de cartón liviana: el cartón y la cinta de arriba
    static void CajaSimple(Transform p, Vector3 pos, float giro)
    {
        Transform c = Grupo("Caja_Archivo", p);
        c.localPosition = pos;
        c.localRotation = Quaternion.Euler(0f, giro, 0f);
        Cubo("Carton", c, new Vector3(0f, 0.15f, 0f), new Vector3(0.39f, 0.3f, 0.5f), mCarton);
        Cubo("Cinta", c, new Vector3(0f, 0.3005f, 0f), new Vector3(0.06f, 0.001f, 0.5f), mCinta);
    }

    // Archivero metálico de 4 cajones contra la pared de la entrada, armado con piezas (antes era
    // un modelo de 26 mil triángulos; así tiene menos de 200)
    static void ArmarArchivero(Transform p, Vector3 pos)
    {
        Transform a = Grupo("Archivero", p);
        a.localPosition = pos;
        Cubo("Cuerpo", a, new Vector3(0f, 0.66f, 0f), new Vector3(0.46f, 1.32f, 0.5f), mPuerta, true);
        for (int i = 0; i < 4; i++)
        {
            float y = 0.19f + i * 0.315f;
            Cubo("Cajon", a, new Vector3(0f, y, 0.253f), new Vector3(0.41f, 0.29f, 0.008f), mPuerta);
            Cubo("Manija", a, new Vector3(0f, y + 0.07f, 0.265f), new Vector3(0.12f, 0.018f, 0.018f), mAcero);
            Cubo("Etiqueta", a, new Vector3(0f, y + 0.02f, 0.2575f), new Vector3(0.08f, 0.035f, 0.001f), mPapel);
        }
    }

    // Junto a la entrada quedó un desorden: la placa del techo que se cayó, cajas tiradas y un
    // cartel de piso mojado. Con la silla S1 volcada deja el paso medio tapado (como pide el
    // GDD) pero sin bloquearlo: se rodea por los costados.
    static void ArmarEscombros(Transform p)
    {
        Transform g = Grupo("Escombros", p);
        GameObject placa = Cubo("Placa_Caida", g, new Vector3(6.05f, 0.04f, 1.05f), new Vector3(0.8f, 0.015f, 0.6f), mTecho);
        placa.transform.localRotation = Quaternion.Euler(3f, 20f, -4f);
        // Las dos cajas apiladas donde quedó la linterna (el modelo de Poly Haven, que se ve de cerca)
        Caja(g, new Vector3(6.45f, 0f, 1.95f), 20f);
        Caja(g, new Vector3(6.42f, 0.342f, 1.93f), 55f);
        CajaSimple(g, new Vector3(7.0f, 0f, 2.1f), -35f);
        Modelo("WetFloorSign_01", g, new Vector3(5.9f, 0f, 2.05f), 30f, 0f);
        // Planta de 9 mil triángulos (la de antes, potted_plant_02, tenía 70 mil)
        Modelo("potted_plant_04", g, new Vector3(7.55f, 0f, 1.35f), 0f, 0.84f, Apoyo.Piso, true);

        // Hojas sueltas por el piso: x, z y giro
        float[,] hojas = { { 2.3f, 3.9f, 20f }, { 3.6f, 5.35f, -35f }, { 1.6f, 2.45f, 70f },
                           { 4.95f, 2.9f, 10f }, { 2.8f, 6.2f, -60f }, { 5.1f, 5.6f, 40f } };
        for (int i = 0; i < hojas.GetLength(0); i++)
        {
            GameObject hoja = Cubo("Hoja", g, new Vector3(hojas[i, 0], 0.001f, hojas[i, 1]),
                                   new Vector3(0.21f, 0.001f, 0.297f), mPapel);
            hoja.transform.localRotation = Quaternion.Euler(0f, hojas[i, 2], 0f);
        }
    }

    // Las tres sillas del GDD, las únicas que quedaron en el piso (el susto se agrega después):
    //  S1: volcada en el paso de la entrada.
    //  S2: la de un puesto, corrida al medio del pasillo de las computadoras.
    //  S3: la del profesor, arrastrada lejos de su escritorio.
    static void ArmarSillas(Transform p)
    {
        Transform g = Grupo("Sillas", p);

        GameObject s1 = ModeloLibre("SchoolChair_01", g, new Vector3(5.1f, 0f, 1.7f), new Vector3(0f, 35f, 90f));
        if (s1 != null)
        {
            s1.name = "Silla_S1";
            ApoyarEnElPiso(s1);
            ColliderDelModelo(s1);
        }

        // S2 y S3 son sillas de oficina con rueditas ("Office Chair Modern" de Sketchfab, de
        // thethieme; se sienta mirando a +Z, como la escolar). Si no está, van escolares.
        GameObject s2 = SillaDeOficina(g, new Vector3(1.9f, 0f, 3.3f), 140f);
        if (s2 != null) s2.name = "Silla_S2";

        GameObject s3 = SillaDeOficina(g, new Vector3(6.4f, 0f, 5.2f), -150f);
        if (s3 != null) s3.name = "Silla_S3";

        sillasSusto = new[] { s1, s2, s3 };
    }

    static GameObject SillaDeOficina(Transform p, Vector3 pos, float giro)
    {
        GameObject silla = ModeloSketchfab("office_chair_modern", p, pos, giro, 0f, Apoyo.Piso, true);
        return silla != null ? silla : Modelo("SchoolChair_01", p, pos, giro, 0f, Apoyo.Piso, true);
    }

    // ------------------------------------------------------------------ fondo: pizarra, consola y puerta

    // Pizarra blanca en la pared del fondo, con lo último que se escribió en clase.
    // Es la "Whiteboard" de Sketchfab (de tboiston), que viene en centímetros: se escala a su
    // alto real (1.06 m). Su frente es +Z: girada 180° mira al cuarto. Queda 1.6 cm despegada
    // de la pared para no meterse en la franja ámbar. Si no está, se arma una con piezas.
    static void ArmarPizarra(Transform p)
    {
        Transform g = Grupo("Pizarra", p);
        const float X = 4.2f, Y = 1.55f;
        if (ModeloSketchfab("whiteboard", g, new Vector3(X, 1.6f, FONDO - 0.016f), 180f, 1.06f, Apoyo.Pared) != null)
            return;

        Cubo("Marco", g, new Vector3(X, Y, FONDO - 0.012f), new Vector3(1.64f, 1.04f, 0.024f), mPerfil);
        Cubo("Tabla", g, new Vector3(X, Y, FONDO - 0.026f), new Vector3(1.6f, 1.0f, 0.004f), mPizarra);
        Cubo("Bandeja", g, new Vector3(X, Y - 0.54f, FONDO - 0.05f), new Vector3(1.4f, 0.02f, 0.06f), mPerfil);
        Texto("Pizarra_Texto", g, new Vector3(X, Y + 0.05f, FONDO - 0.0285f), Vector3.back,
              "REDES  ·  CLASE 12\n\nIP  ·  MÁSCARA  ·  PUERTA DE ENLACE\n\nLunes: examen práctico",
              new Vector2(1.45f, 0.8f), new Color(0.12f, 0.2f, 0.5f));
    }

    // Puerta al Laboratorio, en el hueco de la pared del fondo: de chapa pintada, con una
    // ventanita y barra antipánico. Queda CERRADA: la va a abrir la consola cuando se resuelva
    // el acertijo (Door.Abrir). Se abre hacia el Laboratorio, alejándose del jugador.
    static void ArmarPuertaSalida(Transform raiz)
    {
        Transform g = Grupo("Puerta_Salida", raiz);

        // Marco oscuro: va de 4 cm adentro de este cuarto hasta pasar la pared del Laboratorio,
        // y tapa los cantos de las dos paredes. Queda 2 mm adentro del hueco para no coincidir
        // con esos cantos (si dos caras quedan en el mismo plano, parpadean).
        float zMarco = FONDO + 0.11f;
        const float fondoMarco = 0.3f;
        Cubo("Marco_Izq", g, new Vector3(SALIDA_X0 + 0.032f, 1.049f, zMarco), new Vector3(0.06f, 2.098f, fondoMarco), mMarco, true);
        Cubo("Marco_Der", g, new Vector3(SALIDA_X1 - 0.032f, 1.049f, zMarco), new Vector3(0.06f, 2.098f, fondoMarco), mMarco, true);
        Cubo("Marco_Arriba", g, new Vector3((SALIDA_X0 + SALIDA_X1) / 2f, 2.073f, zMarco),
             new Vector3(SALIDA_X1 - SALIDA_X0 - 0.004f, 0.05f, fondoMarco), mMarco, true);

        // Bisagra en el borde izquierdo del hueco, en el medio del espesor de la pared.
        // Door la gira: todo lo que es parte de la hoja va adentro.
        float xBisagra = SALIDA_X0 + 0.062f;
        float anchoHoja = (SALIDA_X1 - 0.062f) - xBisagra - 0.004f;   // 4 mm de luz del lado de la cerradura
        Transform bisagra = Grupo("Puerta_Bisagra", g);
        bisagra.localPosition = new Vector3(xBisagra, 0f, FONDO + MURO / 2f);
        Door puerta = bisagra.gameObject.AddComponent<Door>();
        puertaSalida = puerta;
        puerta.anguloAbierto = -90f;   // negativo: la hoja gira hacia +Z, hacia el Laboratorio
        puerta.anguloPestillo = 3f;
        puerta.pausa = 0.3f;
        puerta.duracion = 2f;

        // Cuando el jugador ya está en el Laboratorio (casi dos metros pasando la puerta), la
        // puerta se cierra sola detrás de él con la misma animación, como la del Cuarto 2.
        Transform zona = Grupo("Zona_Salida", g);
        zona.localPosition = new Vector3((SALIDA_X0 + SALIDA_X1) / 2f, 1.1f, FONDO + 2f);
        var colisionZona = zona.gameObject.AddComponent<BoxCollider>();
        colisionZona.isTrigger = true;
        colisionZona.size = new Vector3(SALIDA_X1 - SALIDA_X0 + 0.8f, 2.2f, 1.4f);
        var disparador = zona.gameObject.AddComponent<DisparadorJugador>();
        UnityEventTools.AddVoidPersistentListener(disparador.alEntrar, new UnityAction(puerta.Cerrar));
        EditorUtility.SetDirty(disparador);

        // Hoja de 4.5 cm con una ventanita angosta: se arma en cuatro partes alrededor del vidrio
        const float esp = 0.045f, y0 = 0.01f, y1 = 2.04f;
        const float vx0 = 0.66f, vx1 = 0.78f, vy0 = 1.15f, vy1 = 1.8f;
        Hoja(bisagra, "Hoja_A", 0.002f, vx0, y0, y1, esp);
        Hoja(bisagra, "Hoja_B", vx1, anchoHoja, y0, y1, esp);
        Hoja(bisagra, "Hoja_C", vx0, vx1, y0, vy0, esp);
        Hoja(bisagra, "Hoja_D", vx0, vx1, vy1, y1, esp);
        Cubo("Vidrio", bisagra, new Vector3((vx0 + vx1) / 2f, (vy0 + vy1) / 2f, 0f),
             new Vector3(vx1 - vx0, vy1 - vy0, 0.01f), mVidrio, true);

        // Barra antipánico del lado de este cuarto (-Z)
        Cubo("Barra", bisagra, new Vector3(anchoHoja * 0.55f, 1.0f, -0.055f), new Vector3(anchoHoja * 0.7f, 0.045f, 0.04f), mAcero);
        Cubo("Barra_Soporte_A", bisagra, new Vector3(anchoHoja * 0.22f, 1.0f, -0.034f), new Vector3(0.05f, 0.07f, 0.024f), mAceroOscuro);
        Cubo("Barra_Soporte_B", bisagra, new Vector3(anchoHoja * 0.88f, 1.0f, -0.034f), new Vector3(0.05f, 0.07f, 0.024f), mAceroOscuro);

        // Cartel verde de SALIDA sobre la puerta
        Transform cartel = Grupo("Cartel_Salida", g);
        cartel.localPosition = new Vector3((SALIDA_X0 + SALIDA_X1) / 2f, 2.33f, FONDO);
        cartel.localRotation = Quaternion.LookRotation(Vector3.back);
        Cubo("Caja", cartel, new Vector3(0f, 0f, 0.025f), new Vector3(0.44f, 0.16f, 0.05f), mSalida);
        Texto("Texto", cartel, new Vector3(0f, 0f, 0.0505f), Vector3.forward, "SALIDA", new Vector2(0.38f, 0.12f), Color.white);
    }

    static void Hoja(Transform p, string nombre, float x0, float x1, float y0, float y1, float espesor)
    {
        Cubo(nombre, p, new Vector3((x0 + x1) / 2f, (y0 + y1) / 2f, 0f), new Vector3(x1 - x0, y1 - y0, espesor), mPuerta, true);
    }

    // Carteles, afiches y seguridad
    static void ArmarCarteles(Transform p)
    {
        Transform g = Grupo("Carteles", p);
        // Nombre de la sala sobre la entrada (se ve al darse vuelta) y a qué da la consola
        Cartel(g, "Cartel_Sala", new Vector3((ENTRADA_X0 + ENTRADA_X1) / 2f, 2.45f, 0f), Vector3.forward,
               "SALA DE COMPUTACIÓN", 1.5f, 0.24f);
        Cartel(g, "Cartel_Laboratorio", new Vector3(2.75f, 1.85f, FONDO), Vector3.back, "LABORATORIO", 0.9f, 0.2f);

        // Afiche con las normas de la sala (el mapa de la red está junto al panel de red)
        Afiche(g, "Afiche_Normas", new Vector3(2.6f, 1.55f, 0f), Vector3.forward,
               "NORMAS DE LA SALA\n\n1. No comer ni beber\n2. No desconectar cables\n3. Guardar el trabajo\n4. Apagar el equipo al salir",
               0.5f, 0.7f);

        // Seguridad: extintor y alarma junto a la entrada, cámara en la esquina del fondo
        Modelo("korean_fire_extinguisher_01", g, new Vector3(3.9f, 1.0f, 0.016f), 0f, 0.55f, Apoyo.Pared);   // delante de la franja
        Modelo("fire_alarm", g, new Vector3(3.65f, 1.5f, 0f), 0f, 0f, Apoyo.Pared);
        ModeloLibre("security_camera_01", g, new Vector3(7.78f, 2.9f, FONDO - 0.22f), new Vector3(15f, -135f, 0f));
    }

    // Cartel moderno como los del Cuarto 1: placa oscura, línea ámbar abajo y letras blancas.
    // pos = punto de la pared donde va el centro; mira = hacia dónde se lee (hacia el cuarto).
    static void Cartel(Transform p, string nombre, Vector3 pos, Vector3 mira, string texto, float ancho, float alto)
    {
        Transform c = Grupo(nombre, p);
        c.localPosition = pos;
        c.localRotation = Quaternion.LookRotation(mira);
        Cubo("Placa", c, new Vector3(0f, 0f, 0.01f), new Vector3(ancho, alto, 0.02f), mPlaca);
        Cubo("Linea", c, new Vector3(0f, -alto / 2f + 0.012f, 0.0215f), new Vector3(ancho, 0.016f, 0.003f), mFranja);
        TextMeshPro t = Texto("Texto", c, new Vector3(0f, 0.008f, 0.0205f), Vector3.forward, texto,
                              new Vector2(ancho - 0.1f, alto - 0.07f), Color.white);
        t.characterSpacing = 6;
    }

    // Afiche de papel pegado a la pared
    static void Afiche(Transform p, string nombre, Vector3 pos, Vector3 mira, string texto, float ancho, float alto)
    {
        Transform a = Grupo(nombre, p);
        a.localPosition = pos;
        a.localRotation = Quaternion.LookRotation(mira);
        Cubo("Papel", a, new Vector3(0f, 0f, 0.001f), new Vector3(ancho, alto, 0.002f), mPapel);
        Texto("Texto", a, new Vector3(0f, 0f, 0.0025f), Vector3.forward, texto,
              new Vector2(ancho - 0.06f, alto - 0.08f), new Color(0.15f, 0.16f, 0.2f));
    }

    // ------------------------------------------------------------------ luces y clima

    // Luces en tiempo real (el Quest aguanta pocas): dos del techo, una por lado de la sala, que
    // prende el tablero de luces (acertijo 1) según cuántas lámparas de su lado estén prendidas;
    // el resplandor verdoso de los monitores (se prende con la primera computadora); y el verde
    // del cartel de salida, que tiene batería y se ve aun de noche. La luna y la linterna las
    // arma ConstructorCuarto3Acertijos. El resto lo dan los materiales que brillan solos y la luz
    // ambiental del cuarto (ClimaCuarto).
    static void ArmarLuces(Transform p)
    {
        Color blanco = new Color(0.92f, 0.96f, 1f);
        luzTechoA = Luz(p, "Luz_Techo_A", new Vector3(2.5f, ALTO - 0.25f, 3.1f), blanco, 2.2f, 8f);
        luzTechoA.enabled = false;   // de noche

        luzTechoB = Luz(p, "Luz_Techo_B", new Vector3(5.5f, ALTO - 0.25f, 4.4f), blanco, 2.2f, 7.5f);
        luzTechoB.enabled = false;
        // Una vez resuelto el tablero, esta luz titila junto con su lámpara (la del medio a la
        // derecha). Mientras tanto están apagados: las luces las maneja el tablero.
        parpadeoTecho = luzTechoB.gameObject.AddComponent<Parpadeo>();
        parpadeoTecho.intensidadMinima = 1f;
        parpadeoTecho.intensidadMaxima = 2.2f;
        parpadeoTecho.enabled = false;
        brilloTecho = luzTechoB.gameObject.AddComponent<BrilloSegunLuz>();
        brilloTecho.piezas = tubosParpadeantes.ToArray();
        brilloTecho.intensidadMaxima = 2.2f;
        brilloTecho.enabled = false;
        EditorUtility.SetDirty(parpadeoTecho);
        EditorUtility.SetDirty(brilloTecho);

        luzPantallas = Luz(p, "Luz_Pantallas", new Vector3(1.0f, 1.3f, 2.9f), new Color(0.45f, 1f, 0.8f), 0.8f, 3.8f);
        luzPantallas.enabled = false;
        Luz(p, "Luz_Salida", new Vector3((SALIDA_X0 + SALIDA_X1) / 2f, 2.3f, FONDO - 0.25f), new Color(0.3f, 1f, 0.5f), 0.6f, 2.4f);
    }

    static Light Luz(Transform p, string nombre, Vector3 pos, Color color, float intensidad, float alcance)
    {
        Transform t = Grupo(nombre, p);
        t.localPosition = pos;
        Light luz = t.gameObject.AddComponent<Light>();
        luz.type = LightType.Point;
        luz.color = color;
        luz.intensity = intensidad;
        luz.range = alcance;
        luz.shadows = LightShadows.None;
        luz.lightmapBakeType = LightmapBakeType.Realtime;   // no entra en la luz horneada del Cuarto 1
        return luz;
    }

    // Zona invisible apenas pasando la entrada: al entrar el jugador, el cuarto pone su clima
    // (luz ambiental, reflejos y niebla), que es de toda la escena. Arranca de noche; el tablero
    // de luces lo va aclarando. Y una sonda de reflejos que saca una foto del cuarto (a oscuras al
    // empezar y otra vez cuando vuelve la luz), para que metales y vidrios reflejen este cuarto.
    static void ArmarClima(Transform raiz)
    {
        Transform zona = Grupo("Zona_Clima", raiz);
        zona.localPosition = new Vector3((ENTRADA_X0 + ENTRADA_X1) / 2f, 1f, 0.8f);
        var colision = zona.gameObject.AddComponent<BoxCollider>();
        colision.isTrigger = true;
        colision.size = new Vector3(ENTRADA_X1 - ENTRADA_X0 + 0.8f, 2f, 1.2f);

        clima = zona.gameObject.AddComponent<ClimaCuarto>();
        var disparador = zona.gameObject.AddComponent<DisparadorJugador>();
        UnityEventTools.AddVoidPersistentListener(disparador.alEntrar, new UnityAction(clima.Aplicar));
        EditorUtility.SetDirty(disparador);

        Transform sonda = Grupo("Sonda_Reflejos", raiz);
        sonda.localPosition = new Vector3(ANCHO / 2f, 1.6f, FONDO / 2f);
        var reflejo = sonda.gameObject.AddComponent<ReflectionProbe>();
        sondaReflejos = reflejo;
        reflejo.mode = ReflectionProbeMode.Realtime;
        reflejo.refreshMode = ReflectionProbeRefreshMode.ViaScripting;   // la saca el tablero de luces
        reflejo.timeSlicingMode = ReflectionProbeTimeSlicingMode.NoTimeSlicing;
        reflejo.size = new Vector3(ANCHO, ALTO, FONDO);
        reflejo.boxProjection = true;
        reflejo.resolution = 128;
    }

    // Ajustes finales para que se vea bien y ande liviano en el Quest:
    //  - Ningún objeto usa las sondas de luz horneadas del Cuarto 1 (quedan lejos y darían una
    //    luz equivocada): usan la luz ambiental que pone ClimaCuarto.
    //  - Lo que nunca se mueve se marca para "static batching": Unity junta esas piezas y las
    //    dibuja de a muchas. NO se marca para la luz horneada: este cuarto usa luces en tiempo
    //    real, y si entrara en el horneado del Cuarto 1 quedaría a oscuras.
    //  - Quedan afuera lo que se mueve o va a cambiar con los acertijos (puerta, sillas,
    //    computadoras, consola), los textos y la lámpara que parpadea.
    static void PrepararParaQuest(Transform raiz)
    {
        foreach (Renderer r in raiz.GetComponentsInChildren<Renderer>(true))
            r.lightProbeUsage = LightProbeUsage.Off;

        foreach (Transform t in raiz.GetComponentsInChildren<Transform>(true))
        {
            bool cambia = EstaDentroDe(t, "Puerta_Bisagra") || EstaDentroDe(t, "Sillas") ||
                          EstaDentroDe(t, "Computadora_") || EstaDentroDe(t, "Consola") ||
                          EstaDentroDe(t, "Tablero_Luces") || EstaDentroDe(t, "Panel_Red") ||
                          EstaDentroDe(t, "Ficha_") || EstaDentroDe(t, "Linterna") ||
                          EstaDentroDe(t, "Cascada") || EstaDentroDe(t, "Lampara_");
            bool esTexto = t.GetComponent<TMP_Text>() != null;
            Renderer r = t.GetComponent<Renderer>();
            bool parpadea = r != null && tubosParpadeantes.Contains(r);
            GameObjectUtility.SetStaticEditorFlags(t.gameObject,
                cambia || esTexto || parpadea ? (StaticEditorFlags)0 : StaticEditorFlags.BatchingStatic);
        }
    }

    static bool EstaDentroDe(Transform t, string prefijo)
    {
        for (Transform x = t; x != null; x = x.parent)
            if (x.name.StartsWith(prefijo)) return true;
        return false;
    }

    // ------------------------------------------------------------------ modelos de Poly Haven

    // Pone un modelo de Poly Haven y devuelve el objeto que lo contiene (o null si no está en
    // el proyecto: el que llama arma una versión simple).
    //  - alto: altura real en metros (se escala parejo); 0 = tamaño original del archivo.
    //  - giroY: hacia dónde mira. El frente de los modelos de Poly Haven es su +Z.
    //  - apoyo: Piso = la base en "pos"; Centro = el centro en "pos"; Pared = la espalda en
    //    "pos" (2 mm despegada); Techo = la parte de arriba en "pos".
    static GameObject Modelo(string nombre, Transform padre, Vector3 pos, float giroY, float alto,
                             Apoyo apoyo = Apoyo.Piso, bool colisiona = false)
        => Colocar(BuscarModelo(nombre), nombre, padre, pos, giroY, alto, apoyo, colisiona);

    // Igual que Modelo, pero con un modelo de Sketchfab (Assets/Sketchfab/<archivo>.glb).
    // Estos tienen licencia CC BY: sus autores están en Assets/Sketchfab/CREDITOS.txt.
    static GameObject ModeloSketchfab(string archivo, Transform padre, Vector3 pos, float giroY, float alto,
                                      Apoyo apoyo = Apoyo.Piso, bool colisiona = false)
        => Colocar(AssetDatabase.LoadAssetAtPath<GameObject>(CARPETA_SKETCHFAB + "/" + archivo + ".glb"),
                   archivo, padre, pos, giroY, alto, apoyo, colisiona);

    static GameObject Colocar(GameObject fuente, string nombre, Transform padre, Vector3 pos, float giroY, float alto,
                              Apoyo apoyo, bool colisiona)
    {
        if (fuente == null)
        {
            Debug.LogWarning("Cuarto 3: no está el modelo " + nombre + ". Se usa una versión simple.");
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
            else if (apoyo == Apoyo.Techo) ancla.y = b.max.y;
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

    // Pone un modelo tal cual viene (su origen en "pos"), con cualquier giro: para cosas
    // volcadas o dadas vuelta, que no se apoyan derechas.
    static GameObject ModeloLibre(string nombre, Transform padre, Vector3 pos, Vector3 giro, float escala = 1f)
    {
        GameObject fuente = BuscarModelo(nombre);
        if (fuente == null)
        {
            Debug.LogWarning("Cuarto 3: no está el modelo " + nombre + " de Poly Haven.");
            return null;
        }
        Transform contenedor = Grupo(nombre, padre);
        contenedor.localPosition = pos;
        contenedor.localRotation = Quaternion.Euler(giro);
        contenedor.localScale = Vector3.one * escala;
        ReemplazarVidrios(Instanciar(fuente, contenedor));
        return contenedor.gameObject;
    }

    // Baja o sube un modelo puesto con ModeloLibre hasta que su punto más bajo toque el piso
    static void ApoyarEnElPiso(GameObject contenedor)
    {
        Transform padre = contenedor.transform.parent;
        if (LimitesLocales(contenedor.transform.GetChild(0).gameObject, padre, out Bounds b))
            contenedor.transform.localPosition += Vector3.down * b.min.y;
    }

    // Caja de colisión con la forma del modelo, en sus propios ejes (sirve aunque esté volcado)
    static void ColliderDelModelo(GameObject contenedor)
    {
        if (!LimitesLocales(contenedor.transform.GetChild(0).gameObject, contenedor.transform, out Bounds b)) return;
        var col = contenedor.AddComponent<BoxCollider>();
        col.center = b.center;
        col.size = b.size;
    }

    static GameObject BuscarModelo(string nombre)
    {
        foreach (string extension in new[] { ".gltf", ".fbx" })
        {
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(CARPETA_MODELOS + "/" + nombre + "/" + nombre + "_1k" + extension);
            if (go != null) return go;
        }
        return null;
    }

    // Se respeta el giro que trae la raíz del modelo: en los de Sketchfab ahí está el paso de
    // "Z hacia arriba" a "Y hacia arriba". Si se pusiera en cero, quedarían acostados.
    static GameObject Instanciar(GameObject fuente, Transform padre)
    {
        var go = PrefabUtility.InstantiatePrefab(fuente, padre) as GameObject;
        if (go == null) go = Object.Instantiate(fuente, padre);
        return go;
    }

    // Los vidrios de los modelos glTF usan "transmisión", que en el Quest se ve como una capa
    // blanca: se cambian por un vidrio transparente propio (igual que en el Cuarto 1).
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

    // Medidas del modelo en los ejes de "espacio" (la caja que lo envuelve, exacta aunque el
    // cuarto esté girado)
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

    static Transform BuscarHijo(Transform padre, string nombre)
    {
        foreach (Transform t in padre.GetComponentsInChildren<Transform>(true))
            if (t.name == nombre) return t;
        return null;
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

    static GameObject Primitiva(PrimitiveType tipo, string nombre, Transform padre, Vector3 pos, Vector3 tam,
                                Material mat, bool colisiona)
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

    // Cable: un cilindro finito que va del punto "a" al "b"
    static void Cable(Transform p, Vector3 a, Vector3 b, float grosor)
    {
        GameObject c = Cilindro("Cable", p, (a + b) / 2f, new Vector3(grosor, Vector3.Distance(a, b) / 2f, grosor), mCable);
        c.transform.localRotation = Quaternion.FromToRotation(Vector3.up, b - a);
    }

    // Texto 3D. "mira" = hacia dónde se lee (hacia donde está el jugador), en los ejes del padre.
    static TextMeshPro Texto(string nombre, Transform padre, Vector3 pos, Vector3 mira, string texto, Vector2 caja, Color color,
                             Vector3? arriba = null)
    {
        var go = new GameObject(nombre);
        var t = go.AddComponent<TextMeshPro>();   // primero el componente: convierte el Transform en RectTransform
        go.transform.SetParent(padre, false);
        go.transform.localPosition = pos;
        // TextMeshPro se lee desde su -Z: se lo gira para que ese lado mire hacia "mira"
        go.transform.localRotation = Quaternion.LookRotation(-mira, arriba ?? Vector3.up);
        t.rectTransform.sizeDelta = caja;
        t.text = texto;
        t.alignment = TextAlignmentOptions.Center;
        t.color = color;
        t.enableAutoSizing = true;   // el texto se ajusta solo al tamaño de la caja
        t.fontSizeMin = 0.01f;
        t.fontSizeMax = 3f;
        return t;
    }

    // ------------------------------------------------------------------ materiales

    static void CrearMateriales()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Materials")) AssetDatabase.CreateFolder("Assets", "Materials");
        if (!AssetDatabase.IsValidFolder(CARPETA_MATERIALES)) AssetDatabase.CreateFolder("Assets/Materials", "Cuarto3");

        // Paredes: yeso pintado de Poly Haven. Solo su relieve, con el color liso: pintura moderna.
        mParedAlta = Mat("C3_ParedAlta", new Color(0.83f, 0.84f, 0.85f), 0f, 0.22f, default, "painted_plaster_wall", 3f, 1.6f, false, true);
        mParedBaja = Mat("C3_ParedBaja", new Color(0.2f, 0.26f, 0.31f), 0f, 0.3f, default, "painted_plaster_wall", 4f, 0.6f, false, true);
        mFranja = Mat("C3_Franja", new Color(0.95f, 0.6f, 0.12f), 0f, 0.45f, new Color(0.18f, 0.1f, 0.01f));
        // Piso técnico: baldosas de Poly Haven (1.9 m cada textura), teñidas de gris
        mPiso = Mat("C3_Piso", new Color(0.7f, 0.72f, 0.76f), 0f, 0.35f, default, "interior_tiles",
                    (ANCHO + 2f * MURO) / 1.9f, (FONDO + 4f * MURO) / 1.9f, true);
        // Cielorraso blanco con el relieve del techo de Poly Haven
        mTecho = Mat("C3_Techo", new Color(0.86f, 0.86f, 0.84f), 0f, 0.1f, default, "ceiling_interior", 4f, 3.8f, false, true);
        mPerfil = Mat("C3_Perfil", new Color(0.8f, 0.81f, 0.82f), 0.5f, 0.5f);

        mMarco = Mat("C3_Marco", new Color(0.14f, 0.15f, 0.17f), 0.3f, 0.4f);
        mPuerta = Mat("C3_Puerta", new Color(0.36f, 0.42f, 0.48f), 0.45f, 0.45f);
        mAcero = Mat("C3_Acero", new Color(0.7f, 0.72f, 0.74f), 0.85f, 0.6f);
        mAceroOscuro = Mat("C3_AceroOscuro", new Color(0.2f, 0.22f, 0.24f), 0.6f, 0.45f);
        mMelamina = Mat("C3_Melamina", new Color(0.8f, 0.8f, 0.78f), 0f, 0.4f);
        mCanto = Mat("C3_Canto", new Color(0.18f, 0.19f, 0.21f), 0f, 0.35f);
        mPlasticoPC = Mat("C3_PlasticoPC", new Color(0.8f, 0.77f, 0.7f), 0f, 0.3f);
        mPlasticoNegro = Mat("C3_PlasticoNegro", new Color(0.07f, 0.07f, 0.08f), 0f, 0.35f);
        mTeclas = Mat("C3_Teclas", new Color(0.17f, 0.17f, 0.18f), 0f, 0.25f);
        mCanaleta = Mat("C3_Canaleta", new Color(0.9f, 0.9f, 0.88f), 0f, 0.35f);
        mCable = Mat("C3_Cable", new Color(0.05f, 0.05f, 0.05f), 0f, 0.3f);
        mHueco = Mat("C3_Hueco", new Color(0.01f, 0.01f, 0.01f), 0f, 0f);
        mPapel = Mat("C3_Papel", new Color(0.93f, 0.92f, 0.88f), 0f, 0.1f);
        mCarton = Mat("C3_Carton", new Color(0.56f, 0.42f, 0.27f), 0f, 0.12f);
        mCinta = Mat("C3_Cinta", new Color(0.66f, 0.54f, 0.36f), 0f, 0.55f);
        mPizarra = Mat("C3_Pizarra", new Color(0.95f, 0.96f, 0.96f), 0f, 0.75f);
        mPlaca = Mat("C3_Placa", new Color(0.13f, 0.14f, 0.16f), 0.2f, 0.4f);

        // Lo que brilla solo: las pantallas (en espera y la de la consola), las luces de los
        // equipos, el cartel de salida y los tubos del techo
        mPantallaEspera = Mat("C3_PantallaEspera", new Color(0.02f, 0.06f, 0.05f), 0f, 0.8f, new Color(0.03f, 0.16f, 0.12f));
        mPantallaConsola = Mat("C3_PantallaConsola", new Color(0.05f, 0.03f, 0f), 0f, 0.8f, new Color(0.3f, 0.17f, 0.02f));
        mLedVerde = Mat("C3_LedVerde", new Color(0.3f, 1f, 0.4f), 0f, 0.5f, new Color(0.3f, 1.4f, 0.45f));
        mLedAmbar = Mat("C3_LedAmbar", new Color(1f, 0.7f, 0.2f), 0f, 0.5f, new Color(1.3f, 0.8f, 0.15f));
        mLedRojo = Mat("C3_LedRojo", new Color(1f, 0.2f, 0.15f), 0f, 0.5f, new Color(1.4f, 0.15f, 0.1f));
        mSalida = Mat("C3_Salida", new Color(0.1f, 0.6f, 0.25f), 0f, 0.5f, new Color(0.1f, 0.85f, 0.35f));
        mTuboEncendido = Mat("C3_TuboEncendido", new Color(1f, 1f, 1f), 0f, 0.6f, new Color(1.3f, 1.35f, 1.4f));
        mTuboApagado = Mat("C3_TuboApagado", new Color(0.35f, 0.36f, 0.37f), 0f, 0.8f);
        mVidrio = Transparente("C3_Vidrio", new Color(0.8f, 0.9f, 0.95f, 0.15f));
        CrearMaterialesAcertijos();
    }

    // Material que deja ver a través (vidrios). El alpha va en el color.
    static Material Transparente(string nombre, Color color)
    {
        Material m = Mat(nombre, color, 0f, 0.9f);
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

    // Material URP. "polyHaven" es el nombre de una textura de Poly Haven (en Assets/PolyHaven/Texturas):
    // si está se le pone al material, y si no queda el color liso.
    //  - tenir: la textura se tiñe con el color.
    //  - soloRelieve: usa solo el relieve (normal map) y deja el color liso, como una pared pintada.
    static Material Mat(string nombre, Color color, float metalico, float suavidad, Color emision = default,
                        string polyHaven = null, float tileX = 1f, float tileY = 1f,
                        bool tenir = false, bool soloRelieve = false)
    {
        string ruta = CARPETA_MATERIALES + "/" + nombre + ".mat";
        Material m = AssetDatabase.LoadAssetAtPath<Material>(ruta);
        if (m == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            m = new Material(shader);
            AssetDatabase.CreateAsset(m, ruta);
        }

        m.SetColor("_BaseColor", color);
        m.SetFloat("_Metallic", metalico);
        m.SetFloat("_Smoothness", suavidad);

        if (emision != default)
        {
            m.EnableKeyword("_EMISSION");
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            m.SetColor("_EmissionColor", emision);
        }

        if (!string.IsNullOrEmpty(polyHaven))
        {
            var tiling = new Vector2(tileX, tileY);
            Texture2D color_ = soloRelieve ? null : Textura(polyHaven, "diff");
            m.SetTexture("_BaseMap", color_);
            m.SetTextureScale("_BaseMap", tiling);
            if (color_ != null && !tenir) m.SetColor("_BaseColor", Color.white);

            Texture2D normal = Textura(polyHaven, "nor_gl");
            if (normal != null)
            {
                ComoNormal(normal);
                m.SetTexture("_BumpMap", normal);
                m.SetTextureScale("_BumpMap", tiling);
                m.EnableKeyword("_NORMALMAP");
            }
        }

        EditorUtility.SetDirty(m);
        return m;
    }

    static Texture2D Textura(string nombre, string mapa)
        => AssetDatabase.LoadAssetAtPath<Texture2D>(CARPETA_TEXTURAS + "/" + nombre + "_" + mapa + "_1k.jpg");

    // Las texturas de relieve tienen que importarse como "Normal map" o se ven mal
    static void ComoNormal(Texture2D textura)
    {
        var importer = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(textura)) as TextureImporter;
        if (importer == null || importer.textureType == TextureImporterType.NormalMap) return;
        importer.textureType = TextureImporterType.NormalMap;
        importer.SaveAndReimport();
    }
}
