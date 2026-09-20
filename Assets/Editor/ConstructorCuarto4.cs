using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;

// Arma el Cuarto 4 (Laboratorio de ciencias) adentro del objeto "Cuarto4_Laboratorio".
// Se corre desde el menú: Escape Room > Construir Cuarto 4. Borra lo que había adentro
// y lo rehace, así siempre queda igual.
//
// Es el último cuarto, así que es el más grande (8 x 9.5 metros, techo de 3.3), el más
// oscuro y el que tiene los acertijos más largos.
//
// Estilo: laboratorio de colegio abandonado. Piso de chapa, azulejo verde hasta la mitad
// de la pared, muebles de acero y mucha cosa de laboratorio. Los modelos vienen de Poly
// Haven si están en el proyecto (Assets/PolyHaven); si no, cada mueble tiene su versión
// simple hecha con cubos.
//
// LOS TRES ACERTIJOS, encadenados (cada uno destraba el siguiente):
//
// 1) ELECTRICIDAD. El cuarto entra a oscuras, solo con las luces verdes de emergencia.
//    El tablero del fondo tiene tres encajes: A, B y C, sin decir cuánto aguanta cada
//    uno. La hoja de la mesa de trabajo tiene el consumo de cada circuito (A: 2 x 5A,
//    B: 4 x 5A, C: 3 x 5A), así que hay que multiplicar para saber que van 10, 20 y 15.
//    Sobre la mesa hay seis fusibles y solo tres sirven. Con los tres puestos vuelve la
//    luz del cuarto.
//
// 2) GAS. Con luz, se ven las dos llaves de la columna del medio. No se abren de un
//    toque: hay que apuntarles y sostener el gatillo unos segundos mientras el volante
//    da vueltas. Con las dos abiertas sale el gas: encienden los mecheros, salen chorros
//    de vapor y el cuarto se llena de niebla.
//
// 3) PALANCAS. La niebla hace visibles tres haces de luz que bajan del techo, uno sobre
//    cada palanca. Los haces se encienden de a uno, en un orden fijo (2, 3, 1, 3), y la
//    secuencia se repite sola. Hay que accionar las palancas en ese orden. Una palanca
//    equivocada prende la luz roja y hay que empezar de nuevo.
//
// FINAL. Con la secuencia bien, se habilita la ranura de al lado de la puerta. Tocándola
// con el medallón del Cuarto 2 en el inventario, la puerta de emergencia se abre.
public static class ConstructorCuarto4
{
    const float ANCHO = 8f;      // eje X: pared Oeste (0) a pared Este (8)
    const float FONDO = 9.5f;    // eje Z: entrada (0) al fondo (9.5)
    const float ALTO = 3.3f;     // altura del techo
    const float MURO = 0.12f;    // espesor de las paredes

    // El vano de entrada queda donde termina el pasillo que viene del Cuarto 2
    const float ENTRADA_X0 = 0.8f, ENTRADA_X1 = 1.9f;
    const float SALIDA_X0 = 5.9f, SALIDA_X1 = 7.1f;
    const float ALTO_PUERTA = 2.1f;

    const float ALTO_FRISO = 1.5f;   // hasta dónde llega el azulejo verde

    // Las tres palancas, a lo largo de la pared Este
    static readonly float[] Z_PALANCAS = { 3f, 4.75f, 6.5f };

    // El orden en que se encienden los haces de luz, y en el que van las palancas.
    // Cada palanca aparece UNA SOLA VEZ a propósito: la que ya quedó abajo no se vuelve
    // a tocar, así que con un número repetido el acertijo no se podría terminar.
    static readonly int[] SECUENCIA = { 2, 3, 1 };

    static Material mParedAlta, mFriso, mGuarda, mPiso, mTecho, mAcero, mAceroOscuro, mNegro,
                    mBlanco, mVerdeLuz, mRojo, mAmbar, mPantalla, mResaltado, mPapel,
                    mSabana, mPiel, mLuzTecho, mVidrioLab, mQuemadura, mLlama, mMancha,
                    mHaz, mVapor, mRojoLuz, mHueco, mPelo, mCartel, mCorte,
                    mLiquido, mVidrioTanque, mManguera, mPantallaVerde, mAmarillo,
                    mFusA, mFusB, mFusC, mFusX, mFusY, mFusZ;

    // Cartel de objetivos: cada paso del cuarto le avisa para que cambie el texto
    static PanelObjetivo panelObjetivo;

    // Todo lo que aparece cuando sale el gas: las llamas de los mecheros y los chorros
    // de vapor. Se va llenando mientras se arma el cuarto.
    static readonly List<GameObject> conGas = new List<GameObject>();

    [MenuItem("Escape Room/Construir Cuarto 4")]
    public static void Construir()
    {
        var raiz = GameObject.Find("Cuarto4_Laboratorio");
        if (raiz == null)
        {
            raiz = new GameObject("Cuarto4_Laboratorio");
            // Queda después del Cuarto 2, dejando lugar en el medio para el Cuarto 3
            raiz.transform.position = new Vector3(0f, 0f, 19f);
            Undo.RegisterCreatedObjectUndo(raiz, "Construir Cuarto 4");
        }

        for (int i = raiz.transform.childCount - 1; i >= 0; i--)
            Undo.DestroyObjectImmediate(raiz.transform.GetChild(i).gameObject);

        conGas.Clear();
        CrearMateriales();
        ConfigurarNormalesDeModelos();

        var estructura = Grupo("Estructura", raiz.transform);
        var mobiliario = Grupo("Mobiliario", raiz.transform);
        var luces = Grupo("Luces", raiz.transform);

        // Lo que ControlEnergia prende o apaga según haya corriente o no
        var lucesCuarto = new List<Light>();
        var conEnergia = new List<GameObject>();

        ArmarEstructura(estructura.transform, lucesCuarto, conEnergia);
        ArmarPanelObjetivo(raiz.transform);

        ArmarMesaQuimica(mobiliario.transform);
        ArmarMesaTrabajo(mobiliario.transform);
        ArmarEscritorioProfesor(mobiliario.transform);
        ArmarPizarra(mobiliario.transform, conEnergia);
        SustoCamilla susto = ArmarCamilla(mobiliario.transform);
        ArmarDecoracion(mobiliario.transform);
        ArmarTanques(mobiliario.transform);

        // Los acertijos, en el orden en que los va a resolver el jugador
        AcertijoSecuencia acertijo = ArmarPalancas(raiz.transform);
        ArmarLineaGas(raiz.transform, acertijo, susto);

        Door puerta = ArmarPuertaSalida(raiz.transform);
        ArmarRanura(raiz.transform, acertijo, puerta);

        List<GameObject> sinEnergia = ArmarEmergencia(luces.transform);
        var control = ArmarControlEnergia(raiz.transform, lucesCuarto, conEnergia, sinEnergia);
        ArmarPanelElectrico(raiz.transform, control);
        ArmarEntrada(raiz.transform, control);

        AjustarAmbienteEditor();
        AsegurarInventario();

        foreach (Transform hijo in raiz.transform)
            Undo.RegisterCreatedObjectUndo(hijo.gameObject, "Construir Cuarto 4");

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(raiz.scene);
        Selection.activeGameObject = raiz;
        Debug.Log("Cuarto 4 armado. Acordate de guardar la escena con Ctrl+S.");
    }

    // Atajo para probar el cuarto: todavía no existe el Cuarto 3 que lo conecte con el
    // resto, así que esto deja al jugador parado en la entrada del laboratorio.
    // Se deshace con Ctrl+Z.
    [MenuItem("Escape Room/Llevar jugador al Cuarto 4")]
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
            Debug.LogWarning("No se encontro la camara del jugador en la escena.");
            return;
        }

        // El jugador es el objeto de más arriba de todos los que tienen la cámara adentro
        Transform jugador = camara.transform;
        while (jugador.parent != null) jugador = jugador.parent;

        Undo.RecordObject(jugador, "Llevar jugador al Cuarto 4");
        jugador.position = raiz.transform.position + new Vector3(1.35f, 0f, 1f);
        Selection.activeGameObject = jugador.gameObject;
    }

    // ------------------------------------------------------------------ estructura

    static void ArmarEstructura(Transform p, List<Light> lucesCuarto, List<GameObject> conEnergia)
    {
        // La losa del piso es la que hace de zona de teletransporte
        var losa = Cubo("Piso", p, new Vector3(ANCHO / 2f, -0.1f, FONDO / 2f),
                        new Vector3(ANCHO, 0.2f, FONDO), mPiso, true);
        var area = losa.AddComponent<TeleportationArea>();
        int capaTeleport = InteractionLayerMask.GetMask("Teleport");
        if (capaTeleport == 0) capaTeleport = 1 << 31;
        area.interactionLayers = capaTeleport;

        var paredes = Grupo("Paredes", p);
        Cubo("Pared_Oeste", paredes.transform, new Vector3(-MURO / 2f, ALTO / 2f, FONDO / 2f),
             new Vector3(MURO, ALTO, FONDO), mParedAlta, true);
        Cubo("Pared_Este", paredes.transform, new Vector3(ANCHO + MURO / 2f, ALTO / 2f, FONDO / 2f),
             new Vector3(MURO, ALTO, FONDO), mParedAlta, true);

        // Pared de la entrada (viene del pasillo), partida por el vano
        Muro(paredes.transform, "Pared_Entrada_Izq", -MURO, ENTRADA_X0, -MURO / 2f, 0f, ALTO);
        Muro(paredes.transform, "Pared_Entrada_Der", ENTRADA_X1, ANCHO + MURO, -MURO / 2f, 0f, ALTO);
        Muro(paredes.transform, "Dintel_Entrada", ENTRADA_X0, ENTRADA_X1, -MURO / 2f, ALTO_PUERTA, ALTO);

        // Pared del fondo: ahí van el tablero, la ranura y la puerta de emergencia
        Muro(paredes.transform, "Pared_Fondo_Izq", -MURO, SALIDA_X0, FONDO + MURO / 2f, 0f, ALTO);
        Muro(paredes.transform, "Pared_Fondo_Der", SALIDA_X1, ANCHO + MURO, FONDO + MURO / 2f, 0f, ALTO);
        Muro(paredes.transform, "Dintel_Salida", SALIDA_X0, SALIDA_X1, FONDO + MURO / 2f, ALTO_PUERTA, ALTO);

        Cubo("Techo", p, new Vector3(ANCHO / 2f, ALTO + MURO / 2f, FONDO / 2f),
             new Vector3(ANCHO + MURO * 2f, MURO, FONDO + MURO * 2f), mTecho, true);

        // El azulejo verde: una chapa finita pegada a la pared, con una guarda oscura
        // arriba. Es lo que le da el aire de laboratorio viejo.
        var friso = Grupo("Azulejo", p);
        FrisoLargo(friso.transform, "Friso_Oeste", 0.02f, FONDO / 2f, 0.03f, FONDO);
        FrisoLargo(friso.transform, "Friso_Este", ANCHO - 0.02f, FONDO / 2f, 0.03f, FONDO);
        FrisoAncho(friso.transform, "Friso_Entrada_Izq", ENTRADA_X0 / 2f, 0.02f, ENTRADA_X0);
        FrisoAncho(friso.transform, "Friso_Entrada_Der", (ENTRADA_X1 + ANCHO) / 2f, 0.02f, ANCHO - ENTRADA_X1);
        FrisoAncho(friso.transform, "Friso_Fondo_Izq", SALIDA_X0 / 2f, FONDO - 0.02f, SALIDA_X0);
        FrisoAncho(friso.transform, "Friso_Fondo_Der", (SALIDA_X1 + ANCHO) / 2f, FONDO - 0.02f, ANCHO - SALIDA_X1);

        // Tres luminarias de tubo en el techo: la carcasa se ve siempre, el tubo
        // encendido y la luz solo cuando hay corriente
        int n = 1;
        foreach (float z in new[] { 2.2f, 4.9f, 7.6f })
        {
            var lum = Grupo("Luminaria_" + n, p);
            lum.transform.localPosition = new Vector3(ANCHO / 2f, ALTO - 0.04f, z);

            if (Modelo("mounted_fluorescent_lights", lum.transform, Vector3.zero, 90f, 0.12f, false, false) == null)
                Cubo("Carcasa", lum.transform, Vector3.zero, new Vector3(2.8f, 0.06f, 0.2f), mAceroOscuro);

            var tubo = Cubo("Tubo", lum.transform, new Vector3(0f, -0.04f, 0f),
                            new Vector3(2.6f, 0.012f, 0.09f), mLuzTecho);
            tubo.SetActive(false);
            conEnergia.Add(tubo);

            lucesCuarto.Add(LuzPunto("Luz", lum.transform, new Vector3(0f, -0.35f, 0f),
                                     new Color(0.95f, 1f, 0.97f), 3.6f, 12f));
            n++;
        }
    }

    // Tira de azulejo a lo largo (paredes Oeste y Este)
    static void FrisoLargo(Transform p, string nombre, float x, float z, float espesor, float largo)
    {
        Cubo(nombre, p, new Vector3(x, ALTO_FRISO / 2f, z), new Vector3(espesor, ALTO_FRISO, largo), mFriso);
        Cubo(nombre + "_Guarda", p, new Vector3(x, ALTO_FRISO + 0.03f, z),
             new Vector3(espesor + 0.01f, 0.06f, largo), mGuarda);
    }

    // Tira de azulejo a lo ancho (paredes de la entrada y del fondo)
    static void FrisoAncho(Transform p, string nombre, float x, float z, float largo)
    {
        if (largo <= 0f) return;
        Cubo(nombre, p, new Vector3(x, ALTO_FRISO / 2f, z), new Vector3(largo, ALTO_FRISO, 0.03f), mFriso);
        Cubo(nombre + "_Guarda", p, new Vector3(x, ALTO_FRISO + 0.03f, z),
             new Vector3(largo, 0.06f, 0.04f), mGuarda);
    }

    static void Muro(Transform p, string nombre, float x0, float x1, float z, float yBase, float yTope)
    {
        float ancho = x1 - x0;
        float alto = yTope - yBase;
        if (ancho <= 0f || alto <= 0f) return;
        Cubo(nombre, p, new Vector3((x0 + x1) / 2f, yBase + alto / 2f, z),
             new Vector3(ancho, alto, MURO), mParedAlta, true);
    }

    // ------------------------------------------------------------------ cartel de objetivos

    // Tablero de avisos al lado de la entrada: dice qué hay que hacer ahora y se
    // actualiza solo a medida que el jugador avanza. Tiene su propia lucecita para
    // que se lea aunque el cuarto esté a oscuras.
    static void ArmarPanelObjetivo(Transform raiz)
    {
        var g = Grupo("Panel_Objetivo", raiz);
        g.transform.localPosition = new Vector3(0.06f, 1.75f, 1f);
        g.transform.localEulerAngles = new Vector3(0f, 90f, 0f);

        Cubo("Marco", g.transform, Vector3.zero, new Vector3(1.15f, 0.72f, 0.04f), mAceroOscuro, true);
        Cubo("Tablero", g.transform, new Vector3(0f, 0f, 0.022f), new Vector3(1.09f, 0.66f, 0.01f), mBlanco);
        Texto("Titulo", g.transform, new Vector3(0f, 0.26f, 0.03f), Vector3.zero,
              "LABORATORIO", 0.26f, new Color(0.1f, 0.1f, 0.1f), 0.95f, 0.1f);
        Cubo("Linea", g.transform, new Vector3(0f, 0.2f, 0.028f), new Vector3(0.95f, 0.004f, 0.002f), mAceroOscuro);

        var texto = Texto("Texto_Objetivo", g.transform, new Vector3(0f, -0.06f, 0.03f), Vector3.zero,
                          "", 0.26f, new Color(0.15f, 0.15f, 0.15f), 1.03f, 0.48f);
        texto.lineSpacing = -12f;

        LuzPunto("Luz_Panel", g.transform, new Vector3(0f, 0.1f, 0.5f), new Color(1f, 0.95f, 0.85f), 1.4f, 1.8f);

        panelObjetivo = g.AddComponent<PanelObjetivo>();
        panelObjetivo.texto = texto;
        panelObjetivo.pasos = new[]
        {
            "SIN ENERGIA\n\nEl tablero del fondo pide tres fusibles: A, B y C.\nLa hoja de la mesa de trabajo dice cual va en cada uno.\nOjo: uno de los tres no esta sobre la mesa.",
            "VOLVIO LA LUZ\n\nLa linea de gas esta cerrada.\nAbri las dos llaves de la columna del medio:\nhay que sostenerlas hasta el tope.",
            "SALE EL GAS\n\nLa niebla dejo ver tres haces de luz\nsobre las palancas. Se encienden en un orden:\nmiralo bien y repetilo con las palancas.",
            "SECUENCIA CORRECTA\n\nSe habilito la ranura de al lado de la puerta.\nTocala llevando el medallon\nque sacaste del cajon del Cuarto 2.",
            "PUERTA ABIERTA\n\nSali del laboratorio: escapaste."
        };
        texto.text = panelObjetivo.pasos[0];   // así ya se ve en el editor, sin darle Play
        EditorUtility.SetDirty(panelObjetivo);
    }

    // Cartel numerado de cada paso, colgado justo arriba de donde hay que hacer algo.
    // La chapa es de color emisivo, así que se lee igual con el cuarto a oscuras, que es
    // como el jugador entra. Devuelve el texto de abajo, por si hay que cambiarlo después.
    static TextMeshPro CartelPaso(Transform p, string nombre, Vector3 pos, float giroY,
                                  string titulo, string instruccion)
    {
        var g = Grupo("Cartel_" + nombre, p);
        g.transform.localPosition = pos;
        g.transform.localEulerAngles = new Vector3(0f, giroY, 0f);

        Cubo("Marco", g.transform, Vector3.zero, new Vector3(1.05f, 0.42f, 0.04f), mAceroOscuro, true);
        Cubo("Chapa", g.transform, new Vector3(0f, 0f, 0.025f), new Vector3(0.98f, 0.35f, 0.01f), mCartel);

        Texto("Titulo", g.transform, new Vector3(0f, 0.09f, 0.035f), Vector3.zero,
              titulo, 0.42f, new Color(0.12f, 0.1f, 0.05f), 0.94f, 0.13f);

        var texto = Texto("Instruccion", g.transform, new Vector3(0f, -0.08f, 0.035f), Vector3.zero,
                          instruccion, 0.26f, new Color(0.16f, 0.13f, 0.06f), 0.94f, 0.15f);
        texto.lineSpacing = -12f;
        return texto;
    }

    static void AvisarPanel(UnityEventBase evento, int paso)
    {
        if (panelObjetivo == null) return;
        UnityEventTools.AddIntPersistentListener(evento, new UnityAction<int>(panelObjetivo.MostrarPaso), paso);
    }

    // ------------------------------------------------------------------ paso 1: tablero

    // Tablero de la pared del fondo con los tres encajes A, B y C. A propósito NO dice
    // cuánto aguanta cada uno: eso sale de la hoja de la mesa de trabajo.
    static void ArmarPanelElectrico(Transform raiz, ControlEnergia control)
    {
        var g = Grupo("Panel_Electrico", raiz);
        g.transform.localPosition = new Vector3(2.2f, 1.25f, FONDO - 0.07f);
        // Girado 180 para que su "adelante" (+Z) apunte hacia adentro del cuarto: todo lo
        // que tiene que verse va con z positivo, si no queda metido en la pared
        g.transform.localEulerAngles = new Vector3(0f, 180f, 0f);

        Cubo("Caja", g.transform, Vector3.zero, new Vector3(1.4f, 0.85f, 0.12f), mAceroOscuro, true);
        Cubo("Fondo_Caja", g.transform, new Vector3(0f, 0f, 0.065f), new Vector3(1.3f, 0.75f, 0.02f), mAcero);
        Texto("Titulo", g.transform, new Vector3(0f, 0.32f, 0.08f), Vector3.zero,
              "TABLERO GENERAL", 0.22f, new Color(0.9f, 0.95f, 0.9f), 1.2f, 0.09f);

        Modelo("power_box_01", raiz, new Vector3(0.7f, 1.2f, FONDO - 0.12f), 180f, 0.5f, false, false);

        CartelPaso(raiz, "Paso1", new Vector3(2.2f, 2.05f, FONDO - 0.07f), 180f,
                   "1 - ELECTRICIDAD", "Pone un fusible en cada letra: A, B y C");

        string[] letras = { "A", "B", "C" };
        string[] amperajes = { "10", "20", "15" };
        float[] xs = { -0.45f, 0f, 0.45f };

        var encajes = new Encaje[3];
        var lucesOk = new GameObject[3];
        var lucesMal = new GameObject[3];
        var audio = AudioEn("Audio_Acierto", g.transform);

        for (int i = 0; i < 3; i++)
        {
            var hueco = Grupo("Encaje_" + letras[i], g.transform);
            hueco.transform.localPosition = new Vector3(xs[i], -0.05f, 0.07f);

            // El tubo donde entra el fusible y las dos bornes de contacto
            Cilindro("Tubo", hueco.transform, Vector3.zero, new Vector3(0.09f, 0.02f, 0.09f), mAceroOscuro)
                .transform.localEulerAngles = new Vector3(90f, 0f, 0f);
            Cubo("Borne_Arriba", hueco.transform, new Vector3(0f, 0.07f, 0.01f),
                 new Vector3(0.07f, 0.02f, 0.03f), mAcero);
            Cubo("Borne_Abajo", hueco.transform, new Vector3(0f, -0.07f, 0.01f),
                 new Vector3(0.07f, 0.02f, 0.03f), mAcero);

            Texto("Letra", hueco.transform, new Vector3(0f, 0.17f, 0.02f), Vector3.zero,
                  letras[i], 0.34f, new Color(0.95f, 0.98f, 0.95f), 0.2f, 0.14f);

            // El hueco: el jugador le apunta con el fusible en la mano y lo toca.
            // La caja de colisión es grande a propósito, para no tener que apuntar fino.
            var ranura = Grupo("Ranura", hueco.transform);
            ranura.transform.localPosition = new Vector3(0f, 0f, 0.05f);
            var colision = ranura.AddComponent<BoxCollider>();
            colision.size = new Vector3(0.2f, 0.2f, 0.12f);

            // Dónde queda acomodado el fusible: metido en el tubo
            var punto = Grupo("Punto", ranura.transform);
            punto.transform.localPosition = new Vector3(0f, 0f, -0.02f);

            ranura.AddComponent<XRSimpleInteractable>();
            var encaje = ranura.AddComponent<Encaje>();
            encaje.punto = punto.transform;
            encaje.sonido = audio;
            encajes[i] = encaje;
            EditorUtility.SetDirty(encaje);

            // Dos lucecitas: verde si el fusible es el que va, roja si está equivocado.
            // Así se ve de una cuál de los tres huecos está mal.
            var luz = Cilindro("Luz_Ok", hueco.transform, new Vector3(-0.05f, 0.27f, 0.02f),
                               new Vector3(0.045f, 0.008f, 0.045f), mVerdeLuz);
            luz.transform.localEulerAngles = new Vector3(90f, 0f, 0f);
            luz.SetActive(false);
            lucesOk[i] = luz;

            var luzMal = Cilindro("Luz_Mal", hueco.transform, new Vector3(0.05f, 0.27f, 0.02f),
                                  new Vector3(0.045f, 0.008f, 0.045f), mRojoLuz);
            luzMal.transform.localEulerAngles = new Vector3(90f, 0f, 0f);
            luzMal.SetActive(false);
            lucesMal[i] = luzMal;
        }

        // Luz piloto de batería: queda prendida siempre, así el tablero se encuentra
        // aunque el cuarto esté a oscuras
        LuzPunto("Luz_Piloto", g.transform, new Vector3(0f, 0.48f, 0.45f),
                 new Color(0.65f, 1f, 0.75f), 1.2f, 2.8f);

        var panel = g.AddComponent<PanelFusibles>();
        panel.encajes = encajes;
        panel.amperajesCorrectos = amperajes;
        panel.lucesOk = lucesOk;
        panel.lucesMal = lucesMal;
        panel.audioAcierto = audio;

        // Con los tres fusibles puestos vuelve la corriente a todo el cuarto
        UnityEventTools.AddVoidPersistentListener(panel.alCompletar, new UnityAction(control.Encender));
        AvisarPanel(panel.alCompletar, 1);
        EditorUtility.SetDirty(panel);
    }

    // ------------------------------------------------------------------ mesa de trabajo

    // Mesa contra la pared Oeste con los seis fusibles sueltos: tres sirven y tres no.
    // Al lado, la hoja con el consumo de cada circuito, que es de donde sale el amperaje.
    static void ArmarMesaTrabajo(Transform p)
    {
        var g = Grupo("Mesa_Trabajo", p);
        g.transform.localPosition = new Vector3(0.85f, 0f, 6.8f);

        // El escritorio de metal mide 0.78 de alto, así que la tapa queda en 0.79
        if (Modelo("metal_office_desk", g.transform, Vector3.zero, 90f, 0.78f, true) == null)
            MesaDeAcero(g.transform, Vector3.zero, 0.8f, 1.7f, 0.765f);

        float tapa = 0.79f;

        // Cinco fusibles sobre la mesa: dos sirven y tres no. El tercero que sirve está
        // guardado en el cajón del escritorio del profesor, así hay que buscarlo.
        string[] amperajes = { "10", "20", "5", "30", "25" };
        Material[] colores = { mFusA, mFusB, mFusX, mFusY, mFusZ };
        for (int i = 0; i < amperajes.Length; i++)
        {
            float x = (i % 2 == 0) ? -0.17f : 0.09f;
            float z = -0.5f + (i / 2) * 0.45f;
            ArmarFusible(g.transform, new Vector3(x, tapa + 0.02f, z), amperajes[i], colores[i]);
        }

        Texto("Cartel_Mesa", g.transform, new Vector3(-0.17f, tapa + 0.003f, -0.78f), new Vector3(-90f, 90f, 0f),
              "FUSIBLES", 0.26f, new Color(0.35f, 0.33f, 0.3f), 0.3f, 0.06f);

        // La hoja del electricista: el consumo de cada circuito. Hay que multiplicar.
        var hoja = Grupo("Hoja_Circuitos", g.transform);
        hoja.transform.localPosition = new Vector3(0.13f, tapa + 0.002f, 0.62f);
        hoja.transform.localEulerAngles = new Vector3(0f, 10f, 0f);
        Cubo("Papel", hoja.transform, Vector3.zero, new Vector3(0.32f, 0.002f, 0.44f), mPapel);
        var texto = Texto("Texto", hoja.transform, new Vector3(0f, 0.003f, 0f), new Vector3(-90f, 90f, 0f),
                          "CIRCUITOS\n\n" +
                          "A  campana   2 x 5A\n" +
                          "B  luces     4 x 5A\n" +
                          "C  heladera  3 x 5A\n\n" +
                          "En cada letra va el\n" +
                          "fusible que da ese\n" +
                          "total. Ej: 2 x 5A = 10A\n\n" +
                          "El de la heladera\n" +
                          "quedo en el cajon\n" +
                          "del escritorio.",
                          0.25f, new Color(0.2f, 0.18f, 0.15f), 0.3f, 0.42f);
        texto.lineSpacing = -14f;

        // Lámpara de trabajo a pilas sobre la mesa: los fusibles se ven desde la entrada
        Cubo("Lampara", g.transform, new Vector3(0.24f, tapa + 0.16f, -0.72f),
             new Vector3(0.12f, 0.06f, 0.12f), mAceroOscuro);
        LuzPunto("Luz_Lampara", g.transform, new Vector3(0.1f, tapa + 0.3f, -0.4f),
                 new Color(0.8f, 1f, 0.85f), 1.4f, 2.6f);

        Modelo("metal_stool_02", p, new Vector3(1.85f, 0f, 6.4f), 25f, 0.62f, true);
    }

    // ------------------------------------------------------------------ escritorio

    // Escritorio del profesor, contra la pared del fondo. En el cajón de la derecha está
    // el fusible que falta en la mesa de trabajo, y en el de la izquierda la nota que
    // avisa cómo se abren las llaves de gas. Los dos cajones se abren con un toque.
    static void ArmarEscritorioProfesor(Transform p)
    {
        var g = Grupo("Escritorio_Profesor", p);
        g.transform.localPosition = new Vector3(0.55f, 0f, 8.3f);
        // Contra la pared Oeste y mirando al centro, para dejar libre el frente del tablero
        g.transform.localEulerAngles = new Vector3(0f, -90f, 0f);

        Cubo("Tapa", g.transform, new Vector3(0f, 0.76f, 0f), new Vector3(1.6f, 0.05f, 0.78f), mAcero, true);
        Cubo("Lateral_Izq", g.transform, new Vector3(-0.77f, 0.38f, 0f), new Vector3(0.05f, 0.76f, 0.78f), mAceroOscuro);
        Cubo("Lateral_Der", g.transform, new Vector3(0.77f, 0.38f, 0f), new Vector3(0.05f, 0.76f, 0.78f), mAceroOscuro);
        Cubo("Respaldo", g.transform, new Vector3(0f, 0.45f, 0.36f), new Vector3(1.55f, 0.62f, 0.04f), mAceroOscuro);
        Cubo("Separador", g.transform, new Vector3(0f, 0.38f, 0f), new Vector3(0.04f, 0.72f, 0.74f), mAceroOscuro);

        // Un papel y una taza olvidados arriba, para que se vea que alguien trabajaba acá
        Cubo("Carpeta", g.transform, new Vector3(0.35f, 0.79f, -0.1f), new Vector3(0.3f, 0.02f, 0.4f), mPapel);
        Modelo("SchoolChair_01", p, new Vector3(2.2f, 0f, 2.6f), 120f, 0.92f, true);

        // Cajón izquierdo: la nota del profesor
        var izq = ArmarCajon(g.transform, "Cajon_Izq", new Vector3(-0.38f, 0.5f, -0.4f));
        var dentroIzq = Grupo("Contenido", izq.transform);
        dentroIzq.transform.localEulerAngles = new Vector3(0f, 180f, 0f);
        var nota = Grupo("Nota", dentroIzq.transform);
        nota.transform.localPosition = new Vector3(0f, -0.055f, 0.26f);
        Cubo("Papel", nota.transform, Vector3.zero, new Vector3(0.26f, 0.002f, 0.34f), mPapel);
        var textoNota = Texto("Texto", nota.transform, new Vector3(0f, 0.003f, 0f), new Vector3(-90f, 90f, 0f),
                              "NOTA\n\n" +
                              "Las llaves de gas\n" +
                              "estan duras.\n\n" +
                              "Hay que agarrarlas\n" +
                              "y no soltar hasta\n" +
                              "que el manometro\n" +
                              "llegue al tope.",
                              0.24f, new Color(0.2f, 0.18f, 0.15f), 0.24f, 0.32f);
        textoNota.lineSpacing = -14f;

        // Cajón derecho: el fusible que falta
        var der = ArmarCajon(g.transform, "Cajon_Der", new Vector3(0.38f, 0.5f, -0.4f));
        var dentroDer = Grupo("Contenido", der.transform);
        dentroDer.transform.localEulerAngles = new Vector3(0f, 180f, 0f);
        ArmarFusible(dentroDer.transform, new Vector3(0f, -0.05f, 0.26f), "15", mFusC);

        // Lucecita de batería sobre el escritorio, para que se encuentre a oscuras
        LuzPunto("Luz_Escritorio", g.transform, new Vector3(0f, 1.2f, -0.3f),
                 new Color(0.7f, 1f, 0.8f), 1.1f, 2.8f);
    }

    // Cajón que se abre con un toque. Lo que va adentro tiene que ser hijo suyo.
    static GameObject ArmarCajon(Transform p, string nombre, Vector3 pos)
    {
        var cajon = Grupo(nombre, p);
        cajon.transform.localPosition = pos;
        // Girado 180 para que se abra hacia el frente del escritorio
        cajon.transform.localEulerAngles = new Vector3(0f, 180f, 0f);

        Cubo("Frente", cajon.transform, Vector3.zero, new Vector3(0.66f, 0.24f, 0.03f), mAcero, true);
        Cubo("Piso_Cajon", cajon.transform, new Vector3(0f, -0.09f, -0.34f), new Vector3(0.6f, 0.02f, 0.66f), mAceroOscuro);
        Cubo("Lado_A", cajon.transform, new Vector3(-0.3f, -0.03f, -0.34f), new Vector3(0.02f, 0.15f, 0.66f), mAceroOscuro);
        Cubo("Lado_B", cajon.transform, new Vector3(0.3f, -0.03f, -0.34f), new Vector3(0.02f, 0.15f, 0.66f), mAceroOscuro);
        Cubo("Tirador", cajon.transform, new Vector3(0f, 0.07f, 0.028f), new Vector3(0.42f, 0.014f, 0.025f), mAceroOscuro);

        cajon.AddComponent<XRSimpleInteractable>();
        var drawer = cajon.AddComponent<Drawer>();
        drawer.aperturaMaxima = 0.4f;
        drawer.abrirDeUnToque = true;
        Resaltar(cajon, cajon.transform.Find("Frente").GetComponent<Renderer>());
        EditorUtility.SetDirty(drawer);

        return cajon;
    }

    // Un fusible: cilindro de vidrio con casquillos de metal y el amperaje escrito.
    // Se agarra con la mano y se encaja en el tablero.
    static void ArmarFusible(Transform p, Vector3 pos, string amperaje, Material color)
    {
        var g = Grupo("Fusible_" + amperaje + "A", p);
        g.transform.localPosition = pos;

        // Acostado sobre la mesa: los cilindros de Unity tienen el eje en su Y, así que
        // se los gira 90 grados en X para que queden apuntando a lo largo del eje Z
        Cilindro("Cuerpo", g.transform, Vector3.zero, new Vector3(0.035f, 0.045f, 0.035f), color, true)
            .transform.localEulerAngles = new Vector3(90f, 0f, 0f);
        Cilindro("Casquillo_1", g.transform, new Vector3(0f, 0f, 0.05f),
                 new Vector3(0.038f, 0.012f, 0.038f), mAcero)
            .transform.localEulerAngles = new Vector3(90f, 0f, 0f);
        Cilindro("Casquillo_2", g.transform, new Vector3(0f, 0f, -0.05f),
                 new Vector3(0.038f, 0.012f, 0.038f), mAcero)
            .transform.localEulerAngles = new Vector3(90f, 0f, 0f);
        // El amperaje escrito arriba del fusible, para leerlo desde la mesa
        Texto("Numero", g.transform, new Vector3(0f, 0.019f, 0f), new Vector3(-90f, 90f, 0f),
              amperaje, 0.17f, new Color(0.1f, 0.1f, 0.1f), 0.08f, 0.05f);

        // Se lleva igual que la llave del Cuarto 2: un toque y el fusible acompaña al
        // jugador, sin sostener nada. Para ponerlo se toca el hueco del tablero.
        g.AddComponent<XRSimpleInteractable>();
        g.AddComponent<ObjetoLlevable>();
        Resaltar(g, g.transform.Find("Cuerpo").GetComponent<Renderer>());

        var datos = g.AddComponent<Fusible>();
        datos.amperaje = amperaje;
        EditorUtility.SetDirty(datos);
    }

    // ------------------------------------------------------------------ mesa de química

    // Mesa de las prácticas, con los tubos de ensayo y el microscopio. Es ambientación:
    // no hay que resolver nada acá, pero es lo que hace que el cuarto parezca un
    // laboratorio de verdad. El mechero se enciende cuando llega el gas.
    static void ArmarMesaQuimica(Transform p)
    {
        var g = Grupo("Mesa_Quimica", p);
        g.transform.localPosition = new Vector3(0.85f, 0f, 3.3f);

        // La tapa de la mesa queda a 0.9 (alto de mesada de laboratorio)
        MesaDeAcero(g.transform, Vector3.zero, 0.8f, 1.9f, 0.875f);
        float tapa = 0.9f;

        Modelo("chemistry_set", g.transform, new Vector3(0f, tapa, -0.5f), 90f, 0.3f, false);
        Modelo("industrial_microscope", g.transform, new Vector3(0.05f, tapa, 0.6f), 120f, 0.32f, false);
        // La máscara de gas, tirada en la mesada: es de la práctica que quedó a medias
        Modelo("old_gas_mask", g.transform, new Vector3(-0.1f, tapa, 0.2f), 200f, 0.16f, false);

        ArmarMechero(g.transform, new Vector3(0.12f, tapa, -0.05f));
    }

    // Mechero de laboratorio. La llama arranca apagada y se enciende con el gas.
    static void ArmarMechero(Transform p, Vector3 pos)
    {
        var g = Grupo("Mechero", p);
        g.transform.localPosition = pos;

        Cilindro("Base", g.transform, new Vector3(0f, 0.01f, 0f), new Vector3(0.11f, 0.01f, 0.11f), mAceroOscuro);
        Cilindro("Tubo", g.transform, new Vector3(0f, 0.08f, 0f), new Vector3(0.035f, 0.07f, 0.035f), mAcero);

        var llama = Grupo("Llama", g.transform);
        llama.transform.localPosition = new Vector3(0f, 0.19f, 0f);
        Cilindro("Fuego", llama.transform, Vector3.zero, new Vector3(0.03f, 0.045f, 0.03f), mLlama);
        LuzPunto("Luz", llama.transform, new Vector3(0f, 0.05f, 0f), new Color(0.45f, 0.75f, 1f), 0.9f, 1.6f);
        llama.SetActive(false);

        conGas.Add(llama);
    }

    // Pone el modelo del cuerpo acostado en la camilla.
    //
    // No se usa el helper Modelo() porque ese centra el modelo ANTES de girarlo: al
    // acostarlo después, el desplazamiento que había quedado adentro lo mandaba lejos
    // de la camilla, y por eso el cuerpo no se veía. Acá se gira primero y se recoloca
    // después, midiendo los límites ya girados.
    static GameObject ArmarCuerpoModelo(Transform padre)
    {
        var fuente = BuscarModelo("cuerpo_camilla");
        if (fuente == null) return null;

        var contenedor = Grupo("Cuerpo_Modelo", padre);
        // Acostado boca arriba y con la cabeza hacia la entrada.
        //
        // Los -90 en X no son a ojo: el propio archivo FBX declara que su eje de arriba
        // es la Y y que el frente es la Z (UpAxis 1, FrontAxis 2, los dos en positivo),
        // que es justo como los usa Unity. Girando -90 en X, la cabeza (+Y) queda
        // apuntando al -Z, o sea a la entrada, y la cara (+Z) queda mirando al techo.
        contenedor.transform.localEulerAngles = new Vector3(-90f, 0f, 0f);

        var modelo = (GameObject)PrefabUtility.InstantiatePrefab(fuente, contenedor.transform);
        modelo.transform.localPosition = Vector3.zero;
        modelo.transform.localRotation = Quaternion.identity;

        if (!Limites(modelo, out Bounds b)) return contenedor;

        // El modelo viene parado, así que su alto es lo que mide de pies a cabeza
        float alto = Mathf.Max(b.size.x, b.size.y, b.size.z);
        if (alto > 0.0001f) modelo.transform.localScale *= 1.72f / alto;

        // Recién ahora, ya girado y escalado, se lo corre para que el centro del cuerpo
        // caiga sobre la tabla de la camilla
        Limites(modelo, out b);
        Vector3 destino = padre.TransformPoint(new Vector3(0f, 0.13f, 0.05f));
        modelo.transform.position += destino - b.center;

        PintarCuerpo(modelo);
        return contenedor;
    }

    // Unity no siempre engancha la textura que viene al lado de un FBX, y el cuerpo
    // queda gris. Se le pone un material armado acá con esa misma textura.
    static void PintarCuerpo(GameObject modelo)
    {
        var textura = BuscarTextura("Zombie");
        if (textura == null) return;

        var material = Mat("C4_Cuerpo", Color.white, 0f, 0.12f);
        if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", textura);
        if (material.HasProperty("_MainTex")) material.SetTexture("_MainTex", textura);
        EditorUtility.SetDirty(material);

        foreach (var r in modelo.GetComponentsInChildren<Renderer>())
        {
            var mats = r.sharedMaterials;
            for (int i = 0; i < mats.Length; i++) mats[i] = material;
            r.sharedMaterials = mats;
        }
    }

    // La cabeza del cuerpo. Poly Haven no tiene modelos de personas (solo muebles,
    // herramientas y naturaleza), así que está armada a mano con formas simples, igual
    // que el resto del cuerpo.
    //
    // La cara mira al Este, que es el lado desde donde el jugador gira las llaves de gas:
    // acostado ya parece estar mirándolo, y cuando se incorpora lo sigue mirando, porque
    // el giro del susto es sobre el eje X y no cambia hacia dónde apunta la cara.
    static void ArmarCabeza(Transform p, Vector3 pos)
    {
        var g = Grupo("Cabeza", p);
        g.transform.localPosition = pos;

        Esfera("Craneo", g.transform, Vector3.zero, new Vector3(0.185f, 0.235f, 0.24f), mPiel);
        // Pómulos hundidos y mandíbula caída, como una cara seca
        Cubo("Pomulo_1", g.transform, new Vector3(0.06f, 0.035f, -0.075f), new Vector3(0.06f, 0.05f, 0.05f), mPiel);
        Cubo("Pomulo_2", g.transform, new Vector3(0.06f, 0.035f, 0.075f), new Vector3(0.06f, 0.05f, 0.05f), mPiel);
        Cubo("Mandibula", g.transform, new Vector3(0.05f, -0.07f, 0f), new Vector3(0.09f, 0.07f, 0.14f), mPiel);

        // Cuencas y boca: huecos negros, que es lo que le da la cara de muerto
        Esfera("Cuenca_1", g.transform, new Vector3(0.062f, 0.035f, -0.048f), new Vector3(0.07f, 0.06f, 0.06f), mHueco);
        Esfera("Cuenca_2", g.transform, new Vector3(0.062f, 0.035f, 0.048f), new Vector3(0.07f, 0.06f, 0.06f), mHueco);
        Cubo("Boca", g.transform, new Vector3(0.075f, -0.055f, 0f), new Vector3(0.05f, 0.055f, 0.075f), mHueco);
        Cubo("Nariz", g.transform, new Vector3(0.082f, -0.005f, 0f), new Vector3(0.03f, 0.035f, 0.03f), mHueco);

        // Pelo apelmazado hacia atrás
        Esfera("Pelo", g.transform, new Vector3(-0.035f, 0.03f, 0f), new Vector3(0.16f, 0.2f, 0.23f), mPelo);
    }

    // Mesa de acero simple, por si no está el modelo de Poly Haven
    static void MesaDeAcero(Transform p, Vector3 centro, float ancho, float largo, float alto)
    {
        var g = Grupo("Mesa", p);
        g.transform.localPosition = centro;
        Cubo("Tapa", g.transform, new Vector3(0f, alto, 0f), new Vector3(ancho, 0.05f, largo), mAcero, true);
        Cubo("Estante", g.transform, new Vector3(0f, 0.25f, 0f), new Vector3(ancho - 0.12f, 0.03f, largo - 0.2f), mAceroOscuro);
        foreach (float x in new[] { -ancho / 2f + 0.06f, ancho / 2f - 0.06f })
            foreach (float z in new[] { -largo / 2f + 0.08f, largo / 2f - 0.08f })
                Cubo("Pata", g.transform, new Vector3(x, alto / 2f, z), new Vector3(0.05f, alto, 0.05f), mAceroOscuro);
    }

    // ------------------------------------------------------------------ pizarra

    // Pizarra de la clase que nunca terminó. A oscuras no se lee; aparece cuando vuelve
    // la corriente y explica cómo se abre la línea de gas.
    static void ArmarPizarra(Transform p, List<GameObject> conEnergia)
    {
        var g = Grupo("Pizarra", p);
        g.transform.localPosition = new Vector3(0.06f, 2f, 5.3f);
        g.transform.localEulerAngles = new Vector3(0f, 90f, 0f);

        Cubo("Marco", g.transform, Vector3.zero, new Vector3(1.6f, 1.05f, 0.04f), mAceroOscuro, true);
        Cubo("Tablero", g.transform, new Vector3(0f, 0f, 0.022f), new Vector3(1.52f, 0.97f, 0.01f), mPantalla);
        Cubo("Bandeja", g.transform, new Vector3(0f, -0.55f, 0.05f), new Vector3(1.5f, 0.03f, 0.07f), mAceroOscuro);

        var texto = Texto("Texto_Diagrama", g.transform, new Vector3(0f, 0f, 0.03f), Vector3.zero,
                          "PRACTICA 4\n\n" +
                          "1. Abri las DOS llaves\n" +
                          "    de la columna.\n\n" +
                          "2. El gas hace ver la luz.\n" +
                          "    Mira en que orden cae.",
                          0.78f, new Color(0.85f, 0.95f, 0.85f), 1.46f, 0.92f);
        texto.lineSpacing = -14f;
        texto.alignment = TextAlignmentOptions.TopLeft;
        texto.gameObject.SetActive(false);
        conEnergia.Add(texto.gameObject);
    }

    // ------------------------------------------------------------------ camilla

    // Mesa de disección de biología, con la sábana encima y el cuerpo debajo.
    static SustoCamilla ArmarCamilla(Transform p)
    {
        var g = Grupo("Camilla", p);
        g.transform.localPosition = new Vector3(3.3f, 0f, 4.4f);

        // La tabla de acero, las patas y las cuatro ruedas
        Cubo("Tabla", g.transform, new Vector3(0f, 0.9f, 0f), new Vector3(0.78f, 0.05f, 1.95f), mAcero, true);
        Cubo("Canaleta", g.transform, new Vector3(0f, 0.86f, 0f), new Vector3(0.7f, 0.04f, 1.85f), mAceroOscuro);
        foreach (float x in new[] { -0.32f, 0.32f })
            foreach (float z in new[] { -0.82f, 0.82f })
            {
                Cubo("Pata", g.transform, new Vector3(x, 0.46f, z), new Vector3(0.04f, 0.84f, 0.04f), mAceroOscuro);
                Cilindro("Rueda", g.transform, new Vector3(x, 0.04f, z), new Vector3(0.08f, 0.015f, 0.08f), mNegro)
                    .transform.localEulerAngles = new Vector3(0f, 0f, 90f);
            }

        // El cuerpo, armado con cubos y una esfera. Va en dos partes: de la cintura para
        // abajo queda quieto, y de la cintura para arriba cuelga de un pivote, que es el
        // que se incorpora cuando salta el susto.
        var cuerpo = Grupo("Cuerpo", g.transform);
        cuerpo.transform.localPosition = new Vector3(0f, 0.93f, 0f);

        Cubo("Cadera", cuerpo.transform, new Vector3(0f, 0.09f, 0.09f), new Vector3(0.36f, 0.2f, 0.28f), mPiel);
        foreach (float x in new[] { -0.1f, 0.1f })
            Cubo("Pierna", cuerpo.transform, new Vector3(x, 0.08f, 0.58f), new Vector3(0.15f, 0.17f, 0.72f), mPiel);
        foreach (float x in new[] { -0.09f, 0.09f })
            Cubo("Pie", cuerpo.transform, new Vector3(x, 0.09f, 1f), new Vector3(0.12f, 0.2f, 0.14f), mPiel);

        // El pivote de la cintura: todo lo que está acá adentro se incorpora de golpe
        var torso = Grupo("Torso", cuerpo.transform);
        torso.transform.localPosition = new Vector3(0f, 0f, -0.05f);

        // El cuerpo de verdad: el modelo "cuerpo_camilla" que está en Assets/Modelos
        // (un zombi CC0 de OpenGameArt). Si por lo que sea no estuviera, se arma el
        // cuerpo con cubos como antes, así el cuarto nunca queda vacío.
        var modeloCuerpo = ArmarCuerpoModelo(torso.transform);

        if (modeloCuerpo == null)
        {
            ArmarCabeza(torso.transform, new Vector3(0f, 0.12f, -0.77f));
            Cubo("Cuello", torso.transform, new Vector3(0f, 0.08f, -0.63f), new Vector3(0.1f, 0.09f, 0.08f), mPiel);
            Cubo("Tronco", torso.transform, new Vector3(0f, 0.09f, -0.28f), new Vector3(0.4f, 0.2f, 0.62f), mPiel);

            // Las costillas marcadas: es lo que lo hace parecer un cuerpo y no un cajón
            for (int i = 0; i < 4; i++)
                Cubo("Costilla", torso.transform, new Vector3(0f, 0.185f, -0.42f + i * 0.08f),
                     new Vector3(0.33f - i * 0.02f, 0.025f, 0.035f), mPiel);
            Cubo("Esternon", torso.transform, new Vector3(0f, 0.19f, -0.3f), new Vector3(0.05f, 0.03f, 0.3f), mPiel);
            Cubo("Mancha_Pecho", torso.transform, new Vector3(0.06f, 0.196f, -0.2f),
                 new Vector3(0.22f, 0.004f, 0.26f), mMancha);

            // El corte en Y de la autopsia, cosido: es lo que lo vuelve un cuerpo de
            // laboratorio y no un maniquí
            foreach (float lado in new[] { -1f, 1f })
            {
                var rama = Cubo("Corte_Rama", torso.transform,
                                new Vector3(lado * 0.09f, 0.198f, -0.48f),
                                new Vector3(0.012f, 0.004f, 0.24f), mCorte);
                rama.transform.localEulerAngles = new Vector3(0f, lado * 26f, 0f);
            }
            Cubo("Corte_Centro", torso.transform, new Vector3(0f, 0.198f, -0.2f),
                 new Vector3(0.012f, 0.004f, 0.36f), mCorte);
            // Las puntadas cruzando el corte
            for (int i = 0; i < 9; i++)
                Cubo("Puntada", torso.transform, new Vector3(0f, 0.2f, -0.36f + i * 0.045f),
                     new Vector3(0.05f, 0.003f, 0.008f), mHueco);

            // La etiqueta del dedo del pie, como en las morgues
            var etiqueta = Grupo("Etiqueta_Pie", cuerpo.transform);
            etiqueta.transform.localPosition = new Vector3(0.09f, 0.02f, 1.1f);
            etiqueta.transform.localEulerAngles = new Vector3(0f, 0f, 12f);
            Cubo("Carton", etiqueta.transform, Vector3.zero, new Vector3(0.09f, 0.002f, 0.06f), mPapel);
            Cubo("Hilo", etiqueta.transform, new Vector3(0f, 0.002f, -0.05f),
                 new Vector3(0.004f, 0.002f, 0.05f), mPapel);

            Cubo("Brazo_Izq", torso.transform, new Vector3(-0.25f, 0.07f, -0.2f),
                 new Vector3(0.11f, 0.11f, 0.6f), mPiel);

            // El brazo derecho se salió de la sábana y cuelga por fuera de la camilla
            Cubo("Hombro", torso.transform, new Vector3(0.3f, 0.09f, -0.4f),
                 new Vector3(0.24f, 0.13f, 0.22f), mPiel);
            var brazo = Grupo("Brazo_Der", torso.transform);
            brazo.transform.localPosition = new Vector3(0.42f, 0.04f, -0.2f);
            brazo.transform.localEulerAngles = new Vector3(0f, 0f, -20f);
            Cubo("Antebrazo", brazo.transform, new Vector3(0.03f, -0.16f, 0f),
                 new Vector3(0.11f, 0.34f, 0.12f), mPiel);
            // La mano, con los dedos abiertos: es lo que queda colgando a la vista
            var mano = Grupo("Mano", brazo.transform);
            mano.transform.localPosition = new Vector3(0.06f, -0.34f, 0f);
            Cubo("Palma", mano.transform, Vector3.zero, new Vector3(0.09f, 0.1f, 0.1f), mPiel);
            for (int i = 0; i < 4; i++)
                Cubo("Dedo", mano.transform, new Vector3(0.01f, -0.08f, -0.034f + i * 0.023f),
                     new Vector3(0.05f, 0.075f, 0.017f), mPiel);
            Cubo("Pulgar", mano.transform, new Vector3(-0.03f, -0.04f, -0.055f),
                 new Vector3(0.05f, 0.05f, 0.02f), mPiel);
        }

        // La sábana: lo tapa del cuello a los pies, con las puntas colgando al costado
        var sabana = Grupo("Sabana", g.transform);
        Cubo("Manta", sabana.transform, new Vector3(0f, 1.15f, 0.22f), new Vector3(0.84f, 0.03f, 1.62f), mSabana);
        foreach (float x in new[] { -0.42f, 0.42f })
            Cubo("Caida", sabana.transform, new Vector3(x, 1.04f, 0.22f), new Vector3(0.03f, 0.24f, 1.62f), mSabana);
        Cubo("Caida_Pies", sabana.transform, new Vector3(0f, 1.04f, 1.03f), new Vector3(0.84f, 0.24f, 0.03f), mSabana);

        // La misma sábana pero tirada en el piso, para después del susto
        var caida = Grupo("Sabana_Caida", g.transform);
        caida.transform.localPosition = new Vector3(-0.72f, 0f, 0.1f);
        caida.transform.localEulerAngles = new Vector3(0f, 18f, 0f);
        Cubo("Tela_1", caida.transform, new Vector3(0f, 0.03f, 0f), new Vector3(0.8f, 0.06f, 1.1f), mSabana);
        Cubo("Tela_2", caida.transform, new Vector3(0.12f, 0.09f, -0.2f), new Vector3(0.5f, 0.08f, 0.6f), mSabana);
        Cubo("Tela_3", caida.transform, new Vector3(-0.15f, 0.08f, 0.3f), new Vector3(0.45f, 0.07f, 0.5f), mSabana);
        caida.SetActive(false);

        // Cartel de la práctica, colgado del borde de la camilla
        var ficha = Grupo("Ficha", g.transform);
        ficha.transform.localPosition = new Vector3(0f, 0.72f, -1f);
        Cubo("Carton", ficha.transform, Vector3.zero, new Vector3(0.3f, 0.2f, 0.006f), mPapel);
        Texto("Texto", ficha.transform, new Vector3(0f, 0f, -0.006f), new Vector3(0f, 180f, 0f),
              "PRACTICA\nSUSPENDIDA", 0.16f, new Color(0.35f, 0.1f, 0.1f), 0.28f, 0.18f);

        // Manchas secas en el piso, debajo y al costado de la camilla
        Cilindro("Mancha_1", g.transform, new Vector3(0.45f, 0.004f, -0.3f),
                 new Vector3(0.55f, 0.001f, 0.42f), mMancha);
        Cilindro("Mancha_2", g.transform, new Vector3(0.62f, 0.004f, 0.1f),
                 new Vector3(0.26f, 0.001f, 0.22f), mMancha);
        Cilindro("Mancha_3", g.transform, new Vector3(-0.3f, 0.004f, 0.55f),
                 new Vector3(0.34f, 0.001f, 0.3f), mMancha);

        // Foco de batería justo encima de la camilla: parpadea aunque no haya corriente,
        // así el cuerpo aparece y desaparece mientras el jugador cruza el cuarto
        var foco = Grupo("Foco_Camilla", g.transform);
        foco.transform.localPosition = new Vector3(0f, ALTO - 0.3f, 0f);
        Cilindro("Pantalla", foco.transform, Vector3.zero, new Vector3(0.32f, 0.05f, 0.32f), mAceroOscuro);
        Cilindro("Bombilla", foco.transform, new Vector3(0f, -0.06f, 0f),
                 new Vector3(0.16f, 0.01f, 0.16f), mVerdeLuz);
        var luzFoco = LuzPunto("Luz", foco.transform, new Vector3(0f, -0.2f, 0f),
                               new Color(0.55f, 1f, 0.68f), 1.2f, 4f);
        luzFoco.gameObject.AddComponent<Parpadeo>();

        // El susto: lo dispara la segunda llave de gas cuando va por la mitad del giro
        var destello = LuzPunto("Destello", g.transform, new Vector3(0f, 1.6f, -0.6f),
                                new Color(1f, 0.95f, 0.9f), 6f, 7f);
        destello.enabled = false;

        var susto = g.AddComponent<SustoCamilla>();
        susto.torso = torso.transform;
        susto.sabana = sabana;
        susto.sabanaCaida = caida;
        susto.destello = destello;
        susto.grito = AudioEn("Audio_Grito", g.transform);
        susto.grito.volume = 1f;
        susto.grito.spatialBlend = 0.6f;   // se escucha fuerte aunque el cuerpo esté al costado
        // Si en el proyecto hay un audio que se llame "Grito", se engancha solo. Así no
        // hay que volver a arrastrarlo cada vez que se reconstruye el cuarto.
        susto.grito.clip = BuscarAsset<AudioClip>("Grito");
        EditorUtility.SetDirty(susto);

        return susto;
    }

    // ------------------------------------------------------------------ paso 2: gas

    // La columna de gas del medio del cuarto, con las dos llaves. No se abren de un
    // toque: hay que sostener el gatillo mientras el volante da dos vueltas. Con las dos
    // abiertas sale el gas, se llena todo de niebla y se ven los haces de luz.
    static void ArmarLineaGas(Transform raiz, AcertijoSecuencia acertijo, SustoCamilla susto)
    {
        var g = Grupo("Linea_Gas", raiz);
        g.transform.localPosition = new Vector3(5.4f, 0f, 4.7f);

        // Caño vertical del piso al techo y un tramo que se va por el techo
        Cilindro("Caño_Vertical", g.transform, new Vector3(0f, ALTO / 2f, 0f),
                 new Vector3(0.12f, ALTO / 2f, 0.12f), mAcero, true);
        Cilindro("Caño_Techo", g.transform, new Vector3(0f, ALTO - 0.2f, -1.8f),
                 new Vector3(0.09f, 1.8f, 0.09f), mAcero)
            .transform.localEulerAngles = new Vector3(90f, 0f, 0f);
        Cubo("Brida", g.transform, new Vector3(0f, 0.06f, 0f), new Vector3(0.26f, 0.03f, 0.26f), mAceroOscuro);
        Cubo("Brida_Media", g.transform, new Vector3(0f, 1.25f, 0f), new Vector3(0.2f, 0.03f, 0.2f), mAceroOscuro);

        // Cartel del paso 2, colgado de la columna y mirando al centro del cuarto
        CartelPaso(g.transform, "Paso2", new Vector3(-0.1f, 2.25f, 0f), -90f,
                   "2 - GAS", "Sostene cada llave girando hasta el tope");

        // El sistema que cuenta las llaves y larga la niebla
        var gas = Grupo("Gas", raiz);
        var sistema = gas.AddComponent<SistemaGas>();
        sistema.llavesNecesarias = 2;
        sistema.sonido = AudioEn("Audio_Gas", gas.transform);

        // Los chorros de vapor que salen de las juntas cuando llega el gas
        foreach (float altura in new[] { 0.5f, 1.45f, 2.3f })
        {
            var chorro = Grupo("Vapor", g.transform);
            chorro.transform.localPosition = new Vector3(-0.1f, altura, 0f);
            chorro.transform.localEulerAngles = new Vector3(0f, 0f, 35f);
            Cilindro("Chorro", chorro.transform, new Vector3(0f, 0.28f, 0f),
                     new Vector3(0.13f, 0.28f, 0.13f), mVapor);
            Cilindro("Punta", chorro.transform, new Vector3(0f, 0.6f, 0f),
                     new Vector3(0.3f, 0.12f, 0.3f), mVapor);
            chorro.SetActive(false);
            conGas.Add(chorro);
        }

        ArmarLlaveGas(g.transform, sistema, "V1", 0.95f, null);
        // El susto salta con la segunda: el jugador está sosteniendo, de costado a la
        // camilla y sin poder reaccionar
        ArmarLlaveGas(g.transform, sistema, "V2", 1.75f, susto);

        sistema.objetosConGas = conGas.ToArray();

        // Con el gas abierto empieza la secuencia de luces del tercer acertijo
        if (acertijo != null)
            UnityEventTools.AddVoidPersistentListener(sistema.alAbrirTodo, new UnityAction(acertijo.Activar));
        AvisarPanel(sistema.alAbrirTodo, 2);
        EditorUtility.SetDirty(sistema);

        Modelo("propane_tank", raiz, new Vector3(5.95f, 0f, 5.6f), 0f, 0.62f, true);
    }

    // Volante de la llave de gas. Mira al Oeste (al centro del cuarto), así el jugador
    // queda de costado a la camilla mientras la gira.
    static void ArmarLlaveGas(Transform p, SistemaGas sistema, string nombre, float altura, SustoCamilla susto)
    {
        var g = Grupo("Llave_" + nombre, p);
        g.transform.localPosition = new Vector3(-0.16f, altura, 0f);

        // El volante va adentro de un grupo girado 90 grados en Z: así su eje (que en los
        // cilindros de Unity es la Y) apunta al Oeste, o sea hacia el centro del cuarto
        var volante = Grupo("Volante", g.transform);
        volante.transform.localEulerAngles = new Vector3(0f, 0f, 90f);

        var aro = Cilindro("Aro", volante.transform, Vector3.zero,
                           new Vector3(0.26f, 0.012f, 0.26f), mRojo, true);
        Cilindro("Centro", volante.transform, new Vector3(0f, 0.01f, 0f),
                 new Vector3(0.08f, 0.02f, 0.08f), mAceroOscuro);
        Cilindro("Eje", volante.transform, new Vector3(0f, -0.08f, 0f),
                 new Vector3(0.035f, 0.08f, 0.035f), mAceroOscuro);
        foreach (float a in new[] { 0f, 60f, 120f })
        {
            var rayo = Cubo("Rayo", volante.transform, Vector3.zero,
                            new Vector3(0.24f, 0.014f, 0.038f), mRojo);
            rayo.transform.localEulerAngles = new Vector3(0f, a, 0f);
        }

        // Manómetro: la aguja se mueve mientras la llave se abre, así se ve el avance
        var mano = Grupo("Manometro", g.transform);
        mano.transform.localPosition = new Vector3(0.02f, 0.26f, 0f);
        mano.transform.localEulerAngles = new Vector3(0f, -90f, 0f);
        Cilindro("Caja", mano.transform, Vector3.zero, new Vector3(0.14f, 0.015f, 0.14f), mAceroOscuro)
            .transform.localEulerAngles = new Vector3(90f, 0f, 0f);
        Cilindro("Esfera", mano.transform, new Vector3(0f, 0f, 0.016f), new Vector3(0.12f, 0.003f, 0.12f), mBlanco)
            .transform.localEulerAngles = new Vector3(90f, 0f, 0f);
        var aguja = Grupo("Aguja", mano.transform);
        aguja.transform.localPosition = new Vector3(0f, 0f, 0.02f);
        aguja.transform.localEulerAngles = new Vector3(0f, 0f, 60f);
        Cubo("Pieza", aguja.transform, new Vector3(0f, 0.025f, 0f), new Vector3(0.008f, 0.05f, 0.004f), mRojo);

        Texto("Etiqueta", g.transform, new Vector3(-0.02f, -0.19f, 0f), new Vector3(0f, -90f, 0f),
              nombre, 0.24f, new Color(0.95f, 0.95f, 0.9f), 0.16f, 0.07f);

        // Luz que se prende mientras el jugador la está girando
        var luzGirando = Cilindro("Luz_Girando", g.transform, new Vector3(-0.02f, 0.4f, 0f),
                                  new Vector3(0.05f, 0.008f, 0.05f), mAmbar);
        luzGirando.transform.localEulerAngles = new Vector3(0f, 0f, 90f);
        luzGirando.SetActive(false);

        g.AddComponent<XRSimpleInteractable>();
        var llave = g.AddComponent<LlaveDeGas>();
        llave.volante = volante.transform;
        llave.aguja = aguja.transform;
        llave.segundosParaAbrir = 3f;
        llave.vueltas = 2f;
        llave.luzGirando = luzGirando;
        llave.sonido = AudioEn("Audio_Llave", g.transform);
        Resaltar(g, aro.GetComponent<Renderer>());

        UnityEventTools.AddVoidPersistentListener(llave.alAbrir, new UnityAction(sistema.AbrirUna));
        if (susto != null)
            UnityEventTools.AddVoidPersistentListener(llave.alMitad, new UnityAction(susto.Disparar));
        EditorUtility.SetDirty(llave);
    }

    // ------------------------------------------------------------------ paso 3: palancas

    // Las tres palancas de la pared Este y los tres haces de luz que bajan del techo
    // sobre cada una. Los haces no se ven hasta que hay niebla; ahí empiezan a
    // encenderse de a uno y muestran el orden.
    static AcertijoSecuencia ArmarPalancas(Transform raiz)
    {
        var g = Grupo("Acertijo_Palancas", raiz);
        CartelPaso(g.transform, "Paso3", new Vector3(ANCHO - 0.08f, 2.15f, 4.75f), -90f,
                   "3 - SECUENCIA", "Accioná las palancas en el orden de las luces");

        var acertijo = g.AddComponent<AcertijoSecuencia>();
        acertijo.secuencia = SECUENCIA;
        acertijo.sonidoOk = AudioEn("Audio_Ok", g.transform);
        acertijo.sonidoError = AudioEn("Audio_Error", g.transform);

        var haces = new GameObject[3];
        var palancas = new Palanca[3];
        for (int i = 0; i < 3; i++)
        {
            float z = Z_PALANCAS[i];

            // Marca de quemado en el piso, debajo del haz
            Cilindro("Quemadura_" + (i + 1), g.transform, new Vector3(7.25f, 0.005f, z),
                     new Vector3(0.75f, 0.002f, 0.75f), mQuemadura);

            haces[i] = ArmarHaz(g.transform, i + 1, z);
            palancas[i] = ArmarPalanca(g.transform, acertijo, i + 1, z);
        }
        acertijo.haces = haces;
        acertijo.palancas = palancas;

        // Luces de aviso de la secuencia, arriba del cartel para que no se pisen
        var avisos = Grupo("Avisos", g.transform);
        avisos.transform.localPosition = new Vector3(ANCHO - 0.1f, 2.62f, 4.75f);
        var ok = Cilindro("Luz_Ok", avisos.transform, new Vector3(0f, 0f, -0.22f),
                          new Vector3(0.14f, 0.02f, 0.14f), mVerdeLuz);
        ok.transform.localEulerAngles = new Vector3(0f, 0f, 90f);
        ok.SetActive(false);
        // Roja emisiva: la otra no se veía con el cuarto a oscuras
        var mal = Cilindro("Luz_Error", avisos.transform, new Vector3(0f, 0f, 0.22f),
                           new Vector3(0.14f, 0.02f, 0.14f), mRojoLuz);
        mal.transform.localEulerAngles = new Vector3(0f, 0f, 90f);
        mal.SetActive(false);
        acertijo.luzOk = ok;
        acertijo.luzError = mal;

        EditorUtility.SetDirty(acertijo);
        return acertijo;
    }

    // Haz de luz que baja del techo. Se ve solo cuando hay niebla en el cuarto.
    static GameObject ArmarHaz(Transform p, int numero, float z)
    {
        var g = Grupo("Haz_" + numero, p);
        g.transform.localPosition = new Vector3(7.25f, 0f, z);

        Cilindro("Cono", g.transform, new Vector3(0f, ALTO / 2f, 0f),
                 new Vector3(0.62f, ALTO / 2f, 0.62f), mHaz);
        Cilindro("Charco", g.transform, new Vector3(0f, 0.012f, 0f),
                 new Vector3(0.9f, 0.004f, 0.9f), mHaz);
        LuzPunto("Luz", g.transform, new Vector3(0f, 0.7f, 0f), new Color(0.8f, 1f, 0.9f), 2f, 3.5f);

        g.SetActive(false);
        return g;
    }

    static Palanca ArmarPalanca(Transform p, AcertijoSecuencia acertijo, int numero, float z)
    {
        var g = Grupo("Palanca_P" + numero, p);
        g.transform.localPosition = new Vector3(ANCHO - 0.06f, 1.15f, z);
        // Girada -90: su "adelante" (+Z) apunta al Oeste, hacia adentro del cuarto
        g.transform.localEulerAngles = new Vector3(0f, -90f, 0f);

        Cubo("Base", g.transform, Vector3.zero, new Vector3(0.26f, 0.46f, 0.06f), mAceroOscuro, true);
        Cubo("Ranura", g.transform, new Vector3(0f, 0f, 0.035f), new Vector3(0.06f, 0.34f, 0.01f), mNegro);

        // El brazo cuelga desde el eje de arriba y baja al accionarlo
        var brazo = Grupo("Brazo", g.transform);
        brazo.transform.localPosition = new Vector3(0f, 0.16f, 0.05f);
        Cilindro("Barra", brazo.transform, new Vector3(0f, -0.12f, 0.02f),
                 new Vector3(0.03f, 0.12f, 0.03f), mAcero);
        var perilla = Esfera("Perilla", brazo.transform, new Vector3(0f, -0.25f, 0.02f),
                             new Vector3(0.08f, 0.08f, 0.08f), mRojo, true);

        Texto("Etiqueta", g.transform, new Vector3(0f, -0.29f, 0.04f), Vector3.zero,
              "P" + numero, 0.22f, new Color(0.95f, 0.95f, 0.9f), 0.22f, 0.09f);

        // Luz verde de la palanca: se prende cuando queda bien puesta y ahí ya no se toca
        var luzOk = Cilindro("Luz_Ok", g.transform, new Vector3(0f, 0.27f, 0.04f),
                             new Vector3(0.06f, 0.01f, 0.06f), mVerdeLuz);
        luzOk.transform.localEulerAngles = new Vector3(90f, 0f, 0f);
        luzOk.SetActive(false);

        brazo.AddComponent<XRSimpleInteractable>();
        var palanca = brazo.AddComponent<Palanca>();
        palanca.numero = numero;
        palanca.brazo = brazo.transform;
        palanca.acertijo = acertijo;
        palanca.luzOk = luzOk;
        palanca.sonido = AudioEn("Audio_Palanca", g.transform);
        Resaltar(brazo, perilla.GetComponent<Renderer>());
        EditorUtility.SetDirty(palanca);

        return palanca;
    }

    // ------------------------------------------------------------------ final: la ranura

    // Ranura al lado de la puerta. Se habilita con la secuencia de palancas y se abre
    // con el medallón que el jugador trae del Cuarto 2.
    static void ArmarRanura(Transform raiz, AcertijoSecuencia acertijo, Door puerta)
    {
        var g = Grupo("Ranura_Medallon", raiz);
        g.transform.localPosition = new Vector3(4.7f, 1.15f, FONDO - 0.07f);
        // Girada 180: lo que se tiene que ver va con z positivo
        g.transform.localEulerAngles = new Vector3(0f, 180f, 0f);

        Cubo("Placa", g.transform, Vector3.zero, new Vector3(0.3f, 0.42f, 0.05f), mAcero, true);
        Cilindro("Hueco", g.transform, new Vector3(0f, 0.06f, 0.03f), new Vector3(0.13f, 0.01f, 0.13f), mNegro)
            .transform.localEulerAngles = new Vector3(90f, 0f, 0f);
        Texto("Etiqueta", g.transform, new Vector3(0f, -0.14f, 0.028f), Vector3.zero,
              "MEDALLON", 0.2f, new Color(0.9f, 0.93f, 0.9f), 0.28f, 0.1f);

        // El medallón encajado: aparece recién cuando el jugador lo pone. Si lo trae en
        // la mano, en vez de este se acomoda el suyo en el mismo punto.
        var medallon = Cilindro("Medallon_Puesto", g.transform, new Vector3(0f, 0.06f, 0.038f),
                                new Vector3(0.11f, 0.006f, 0.11f), mAmbar);
        medallon.transform.localEulerAngles = new Vector3(90f, 0f, 0f);
        medallon.SetActive(false);

        var punto = Grupo("Punto_Medallon", g.transform);
        punto.transform.localPosition = new Vector3(0f, 0.06f, 0.042f);

        var luzOk = Cilindro("Luz_Lista", g.transform, new Vector3(0f, 0.17f, 0.028f),
                             new Vector3(0.035f, 0.006f, 0.035f), mVerdeLuz);
        luzOk.transform.localEulerAngles = new Vector3(90f, 0f, 0f);
        luzOk.SetActive(false);

        var luzMal = Cilindro("Luz_Error", g.transform, new Vector3(0f, -0.04f, 0.028f),
                              new Vector3(0.035f, 0.006f, 0.035f), mRojo);
        luzMal.transform.localEulerAngles = new Vector3(90f, 0f, 0f);
        luzMal.SetActive(false);

        g.AddComponent<XRSimpleInteractable>();
        var ranura = g.AddComponent<RanuraMedallon>();
        ranura.medallon = BuscarAsset<ItemData>("ItemData_ObjetoEspecial");
        ranura.medallonPuesto = medallon;
        ranura.punto = punto.transform;
        ranura.luzLista = luzOk;
        ranura.luzError = luzMal;
        ranura.sonidoOk = AudioEn("Audio_Ok", g.transform);
        ranura.sonidoError = AudioEn("Audio_Error", g.transform);
        // El cartel de arriba va cambiando de texto según cómo esté la ranura
        ranura.cartel = CartelPaso(raiz, "Paso4", new Vector3(4.7f, 1.68f, FONDO - 0.07f), 180f,
                                   "4 - MEDALLON", "Bloqueada: falta la secuencia de palancas");
        Resaltar(g, g.transform.Find("Placa").GetComponent<Renderer>());

        // La secuencia de palancas habilita la ranura, y la ranura abre la puerta
        if (acertijo != null)
            UnityEventTools.AddVoidPersistentListener(acertijo.OnSolved, new UnityAction(ranura.Habilitar));
        AvisarPanel(acertijo.OnSolved, 3);

        if (puerta != null)
            UnityEventTools.AddVoidPersistentListener(ranura.alAbrir, new UnityAction(puerta.Abrir));
        AvisarPanel(ranura.alAbrir, 4);
        EditorUtility.SetDirty(ranura);
    }

    // ------------------------------------------------------------------ puerta de emergencia

    static Door ArmarPuertaSalida(Transform raiz)
    {
        var g = Grupo("Puerta_Salida", raiz);
        g.transform.localPosition = new Vector3(SALIDA_X0, 0f, FONDO);

        float ancho = SALIDA_X1 - SALIDA_X0;
        Cubo("Jamba_Izq", g.transform, new Vector3(-0.04f, ALTO_PUERTA / 2f, 0f),
             new Vector3(0.08f, ALTO_PUERTA, 0.2f), mAceroOscuro, true);
        Cubo("Jamba_Der", g.transform, new Vector3(ancho + 0.04f, ALTO_PUERTA / 2f, 0f),
             new Vector3(0.08f, ALTO_PUERTA, 0.2f), mAceroOscuro, true);
        Cubo("Dintel", g.transform, new Vector3(ancho / 2f, ALTO_PUERTA + 0.04f, 0f),
             new Vector3(ancho + 0.16f, 0.08f, 0.2f), mAceroOscuro, true);

        // Cartel de salida de emergencia, encendido siempre (es el de la batería)
        Cubo("Cartel", g.transform, new Vector3(ancho / 2f, ALTO_PUERTA + 0.26f, -0.1f),
             new Vector3(0.5f, 0.16f, 0.03f), mAceroOscuro);
        Texto("Texto_Cartel", g.transform, new Vector3(ancho / 2f, ALTO_PUERTA + 0.26f, -0.12f),
              new Vector3(0f, 180f, 0f), "SALIDA", 0.14f, new Color(0.4f, 1f, 0.5f), 0.5f, 0.15f);

        var bisagra = Grupo("Bisagra", g.transform);
        var puerta = bisagra.AddComponent<Door>();
        puerta.anguloApertura = -95f;   // se abre hacia afuera del cuarto
        puerta.duracion = 1.4f;

        // Hoja de chapa con la barra antipánico
        Cubo("Hoja", bisagra.transform, new Vector3(ancho / 2f, ALTO_PUERTA / 2f, 0f),
             new Vector3(ancho, ALTO_PUERTA, 0.05f), mAcero, true);
        Cubo("Barra", bisagra.transform, new Vector3(ancho / 2f, 1.05f, -0.07f),
             new Vector3(ancho - 0.25f, 0.06f, 0.05f), mAceroOscuro);
        Cubo("Franja", bisagra.transform, new Vector3(ancho / 2f, 1.75f, -0.03f),
             new Vector3(ancho - 0.1f, 0.12f, 0.005f), mVerdeLuz);

        EditorUtility.SetDirty(puerta);
        return puerta;
    }

    // ------------------------------------------------------------------ decoración

    static void ArmarDecoracion(Transform p)
    {
        // Estanterías de acero contra la pared de la entrada, las dos del mismo tipo
        // para que se vean como un mueble de laboratorio y no como cosas sueltas
        Modelo("steel_frame_shelves_02", p, new Vector3(3.6f, 0f, 0.5f), 0f, 1.8f, true);
        Modelo("steel_frame_shelves_03", p, new Vector3(5.1f, 0f, 0.5f), 0f, 1.8f, true);
        Modelo("Shelf_01", p, new Vector3(6.6f, 0f, 0.55f), 0f, 1.6f, true);
        Modelo("drawer_cabinet", p, new Vector3(0.6f, 0f, 1.2f), 90f, 0.9f, true);
        Modelo("metal_tool_chest", p, new Vector3(7.5f, 0f, 1.9f), -90f, 0.9f, true);

        // La silla de ruedas en el medio del cuarto, de frente a la entrada
        Modelo("wheelchair_01", p, new Vector3(6.5f, 0f, 1.6f), 145f, 0.95f, true);

        // El carro de instrumental, pegado a la camilla
        Modelo("industrial_storage_cart", p, new Vector3(4.35f, 0f, 3.3f), 90f, 0.9f, true);

        // Tambores y bidones de químicos en el rincón
        Modelo("barrel_03", p, new Vector3(3.0f, 0f, 1.0f), 0f, 0.85f, true);
        Modelo("barrel_03", p, new Vector3(3.6f, 0f, 1.4f), 40f, 0.85f, true);
        Modelo("metal_jerrycan_green", p, new Vector3(2.6f, 0f, 1.5f), 25f, 0.36f, true);

        // Reflector de obra tirado en el piso, apuntando a la camilla
        var reflector = Modelo("portable_searchlight", p, new Vector3(4.5f, 0f, 5.8f), 210f, 0.4f, true);
        if (reflector != null)
            LuzPunto("Luz_Reflector", reflector.transform, new Vector3(0f, 0.3f, 0f),
                     new Color(0.75f, 1f, 0.8f), 1.1f, 3.5f);

        // Frascos de químicos sobre las mesas
        // Plantas: el laboratorio lleva años cerrado y la vegetación se metió adentro,
        // como en las fotos de referencia
        Modelo("potted_plant_02", p, new Vector3(3.15f, 0f, 0.85f), 20f, 0.9f, true);
        Modelo("potted_plant_02", p, new Vector3(0.5f, 0f, 4.55f), -30f, 0.85f, true);
        Modelo("potted_plant_04", p, new Vector3(3.8f, 0.92f, 9.05f), 0f, 0.3f, false);
        Modelo("potted_plant_04", p, new Vector3(0.62f, 0.9f, 2.6f), 40f, 0.28f, false);

        Modelo("bleach_bottle", p, new Vector3(1.12f, 0.79f, 6.3f), 30f, 0.26f, false);
        Modelo("plastic_bottle_gallon", p, new Vector3(0.65f, 0.9f, 2.65f), -20f, 0.3f, false);

        // Cosas que le dan vida al laboratorio sin estorbar el paso
        Modelo("portable_generator", p, new Vector3(7.6f, 0f, 1.15f), 200f, 0.6f, true);
        Modelo("cardboard_box_01", p, new Vector3(2.2f, 0f, 1.1f), 25f, 0.4f, true);
        Modelo("metal_toolbox", p, new Vector3(1.25f, 0.79f, 6.1f), 20f, 0.16f, false);
        Modelo("utility_box_01", p, new Vector3(0.12f, 1.9f, 7.2f), 90f, 0.4f, false, false);
        Modelo("industrial_wall_lamp", p, new Vector3(7.88f, 2.4f, 0.9f), -90f, 0.3f, false, false);
        Modelo("WetFloorSign_01", p, new Vector3(2.6f, 0f, 6.6f), 40f, 0.6f, true);
        Modelo("modular_pipes", p, new Vector3(0.12f, 2.75f, 4.75f), 0f, 0.3f, false, false);

        Modelo("medical_box", p, new Vector3(5.4f, 0f, 0.5f), 15f, 0.26f, true);
        Modelo("metal_trash_can", p, new Vector3(2.5f, 0f, 9f), 0f, 0.42f, true);
        Modelo("metal_stool_02", p, new Vector3(2.4f, 0f, 2.3f), 200f, 0.62f, true);

        // Matafuegos colgado de la pared Este
        Modelo("korean_fire_extinguisher_01", p, new Vector3(7.85f, 1f, 0.55f), -90f, 0.42f, false, false);

        // Lámparas enjauladas colgando del techo, una de ellas quemándose
        ArmarLamparaColgante(p, new Vector3(2.2f, 0f, 1.6f), true);
        ArmarLamparaColgante(p, new Vector3(5.2f, 0f, 8.2f), false);

        // El otro mechero, el de la mesa de trabajo
        ArmarMechero(p, new Vector3(1.15f, 0.79f, 7.3f));
    }

    // Lámpara enjaulada colgada del techo por un cable. Si "quemada" es true, parpadea.
    static void ArmarLamparaColgante(Transform p, Vector3 pos, bool quemada)
    {
        var g = Grupo("Lampara_Colgante", p);
        g.transform.localPosition = pos;

        Cilindro("Cable", g.transform, new Vector3(0f, ALTO - 0.3f, 0f),
                 new Vector3(0.012f, 0.3f, 0.012f), mNegro);

        if (Modelo("caged_hanging_light", g.transform, new Vector3(0f, ALTO - 0.72f, 0f), 0f, 0.36f, false, false) == null)
            Cilindro("Pantalla", g.transform, new Vector3(0f, ALTO - 0.72f, 0f),
                     new Vector3(0.22f, 0.1f, 0.22f), mAceroOscuro);

        var luz = LuzPunto("Luz", g.transform, new Vector3(0f, ALTO - 0.75f, 0f),
                           new Color(0.6f, 1f, 0.72f), quemada ? 1.1f : 0.8f, 4f);
        if (quemada) luz.gameObject.AddComponent<Parpadeo>();
    }

    // ------------------------------------------------------------------ tanques

    // El rincón de los especímenes: tres tanques de vidrio con líquido verde y algo
    // flotando adentro, las mangueras que los alimentan y la consola que los controla.
    // Es puro ambiente, no hay que resolver nada acá, pero es lo que hace que el cuarto
    // parezca un laboratorio raro y no un depósito.
    //
    // Todo brilla por material emisivo, no por luces: el verde se ve igual pero el
    // cuarto sigue tan oscuro como antes al entrar.
    static void ArmarTanques(Transform p)
    {
        var g = Grupo("Tanques", p);

        float[] zs = { 7.3f, 8.15f, 9f };
        for (int i = 0; i < zs.Length; i++)
            ArmarTanque(g.transform, i + 1, new Vector3(7.55f, 0f, zs[i]), i);

        // El caño que los alimenta, corriendo por arriba de los tres
        Cilindro("Caño_Madre", g.transform, new Vector3(7.55f, 2.2f, 8.15f),
                 new Vector3(0.09f, 1.1f, 0.09f), mManguera)
            .transform.localEulerAngles = new Vector3(90f, 0f, 0f);

        // Mangueras cruzando el techo hasta la columna de gas, como en un laboratorio real
        Cilindro("Manguera_Techo_1", g.transform, new Vector3(6.6f, 3.1f, 8.15f),
                 new Vector3(0.07f, 0.95f, 0.07f), mManguera)
            .transform.localEulerAngles = new Vector3(0f, 0f, 90f);
        Cilindro("Manguera_Techo_2", g.transform, new Vector3(5.5f, 3.1f, 6.6f),
                 new Vector3(0.07f, 1.7f, 0.07f), mManguera)
            .transform.localEulerAngles = new Vector3(90f, 0f, 0f);
        Cilindro("Bajada", g.transform, new Vector3(7.55f, 2.65f, 8.15f),
                 new Vector3(0.07f, 0.45f, 0.07f), mManguera);

        ArmarConsola(g.transform, new Vector3(3.8f, 0f, 9.05f), 185f);
        CartelRadiactivo(g.transform, new Vector3(7.9f, 2.2f, 6.9f), -90f);
        CartelRadiactivo(g.transform, new Vector3(4.0f, 1.9f, 0.06f), 0f);
    }

    // Un tanque: base de acero, cilindro de líquido verde, vidrio por fuera y un cuerpo
    // encogido flotando adentro.
    static void ArmarTanque(Transform p, int numero, Vector3 pos, int variante)
    {
        var g = Grupo("Tanque_" + numero, p);
        g.transform.localPosition = pos;

        Cilindro("Base", g.transform, new Vector3(0f, 0.09f, 0f), new Vector3(0.82f, 0.09f, 0.82f), mAceroOscuro, true);
        Cilindro("Aro_Bajo", g.transform, new Vector3(0f, 0.2f, 0f), new Vector3(0.78f, 0.03f, 0.78f), mAcero);
        Cilindro("Liquido", g.transform, new Vector3(0f, 0.86f, 0f), new Vector3(0.6f, 0.62f, 0.6f), mLiquido);
        Cilindro("Vidrio", g.transform, new Vector3(0f, 0.88f, 0f), new Vector3(0.68f, 0.66f, 0.68f), mVidrioTanque);
        Cilindro("Aro_Alto", g.transform, new Vector3(0f, 1.54f, 0f), new Vector3(0.78f, 0.03f, 0.78f), mAcero);
        Cilindro("Tapa", g.transform, new Vector3(0f, 1.62f, 0f), new Vector3(0.84f, 0.06f, 0.84f), mAceroOscuro, true);
        Cilindro("Boquilla", g.transform, new Vector3(0f, 1.78f, 0f), new Vector3(0.14f, 0.1f, 0.14f), mManguera);

        // El espécimen: una silueta encogida, casi negra contra el verde
        var cuerpo = Grupo("Especimen", g.transform);
        cuerpo.transform.localPosition = new Vector3(0f, 0.88f, 0f);
        cuerpo.transform.localEulerAngles = new Vector3(18f * variante, 40f * variante, 12f);
        Esfera("Cabeza", cuerpo.transform, new Vector3(0f, 0.16f, 0f), new Vector3(0.17f, 0.2f, 0.19f), mHueco);
        Esfera("Tronco", cuerpo.transform, new Vector3(0f, -0.02f, 0f), new Vector3(0.26f, 0.3f, 0.24f), mHueco);
        foreach (float x in new[] { -0.13f, 0.13f })
        {
            Cubo("Brazo", cuerpo.transform, new Vector3(x, 0.02f, 0.07f), new Vector3(0.06f, 0.22f, 0.06f), mHueco);
            Cubo("Pierna", cuerpo.transform, new Vector3(x * 0.6f, -0.2f, 0.05f), new Vector3(0.07f, 0.2f, 0.07f), mHueco);
        }

        // Burbujas subiendo
        for (int b = 0; b < 5; b++)
            Esfera("Burbuja", g.transform,
                   new Vector3(0.14f - b * 0.06f, 0.45f + b * 0.2f, 0.1f - b * 0.05f),
                   new Vector3(0.05f, 0.05f, 0.05f), mVidrioTanque);

        // Una luz muy floja: da presencia sin aclarar el cuarto
        LuzPunto("Luz", g.transform, new Vector3(0f, 0.9f, 0f), new Color(0.4f, 1f, 0.35f), 0.45f, 2.2f);

        // Etiqueta del tanque
        var chapa = Grupo("Etiqueta", g.transform);
        chapa.transform.localPosition = new Vector3(-0.42f, 0.55f, 0f);
        chapa.transform.localEulerAngles = new Vector3(0f, -90f, 0f);
        Cubo("Chapa", chapa.transform, Vector3.zero, new Vector3(0.3f, 0.12f, 0.01f), mAmarillo);
        Texto("Texto", chapa.transform, new Vector3(0f, 0f, 0.008f), Vector3.zero,
              "MUESTRA " + numero, 0.15f, new Color(0.1f, 0.1f, 0.1f), 0.28f, 0.1f);
    }

    // Consola de control de los tanques, con la pantalla verde encendida
    static void ArmarConsola(Transform p, Vector3 pos, float giroY)
    {
        var g = Grupo("Consola", p);
        g.transform.localPosition = pos;
        g.transform.localEulerAngles = new Vector3(0f, giroY, 0f);

        Cubo("Mueble", g.transform, new Vector3(0f, 0.43f, 0f), new Vector3(1.05f, 0.86f, 0.55f), mAceroOscuro, true);
        Cubo("Tapa", g.transform, new Vector3(0f, 0.88f, 0f), new Vector3(1.1f, 0.05f, 0.6f), mAcero);
        Cubo("Marco", g.transform, new Vector3(0f, 1.25f, -0.12f), new Vector3(0.92f, 0.62f, 0.07f), mNegro, true);
        Cubo("Pantalla", g.transform, new Vector3(0f, 1.25f, -0.16f), new Vector3(0.84f, 0.54f, 0.01f), mPantallaVerde);

        var texto = Texto("Texto", g.transform, new Vector3(0f, 1.25f, -0.17f), new Vector3(0f, 180f, 0f),
                          "ANALISIS EN CURSO\n\n" +
                          "MUESTRA 1   ESTABLE\n" +
                          "MUESTRA 2   ESTABLE\n" +
                          "MUESTRA 3   INESTABLE\n\n" +
                          "NO ABRIR LOS TANQUES",
                          0.32f, new Color(0.55f, 1f, 0.6f), 0.8f, 0.5f);
        texto.lineSpacing = -14f;

        // Botonera de adorno
        for (int i = 0; i < 4; i++)
            Cubo("Boton", g.transform, new Vector3(-0.3f + i * 0.2f, 0.9f, -0.15f),
                 new Vector3(0.09f, 0.02f, 0.09f), i == 2 ? mRojoLuz : mVerdeLuz);
    }

    // Cartel de radiactivo: la chapa amarilla con las tres aspas negras
    static void CartelRadiactivo(Transform p, Vector3 pos, float giroY)
    {
        var g = Grupo("Cartel_Radiactivo", p);
        g.transform.localPosition = pos;
        g.transform.localEulerAngles = new Vector3(0f, giroY, 0f);

        Cubo("Chapa", g.transform, Vector3.zero, new Vector3(0.5f, 0.5f, 0.02f), mAmarillo);
        Cilindro("Centro", g.transform, new Vector3(0f, 0.05f, -0.015f), new Vector3(0.07f, 0.005f, 0.07f), mNegro)
            .transform.localEulerAngles = new Vector3(90f, 0f, 0f);

        // Las tres aspas, a 120 grados una de otra
        for (int i = 0; i < 3; i++)
        {
            var aspa = Cubo("Aspa_" + (i + 1), g.transform, new Vector3(0f, 0.05f, -0.015f),
                            new Vector3(0.12f, 0.17f, 0.005f), mNegro);
            aspa.transform.localEulerAngles = new Vector3(0f, 0f, i * 120f);
            aspa.transform.localPosition += aspa.transform.up * 0.11f;
        }

        Texto("Texto", g.transform, new Vector3(0f, -0.18f, -0.016f), new Vector3(0f, 180f, 0f),
              "RADIACTIVO", 0.22f, new Color(0.1f, 0.1f, 0.1f), 0.46f, 0.1f);
    }

    // ------------------------------------------------------------------ luz de emergencia

    // Lo que se ve mientras no hay corriente: las luces verdosas de emergencia en las
    // esquinas del techo, casi todas parpadeando.
    static List<GameObject> ArmarEmergencia(Transform p)
    {
        var sinEnergia = new List<GameObject>();

        float[,] esquinas = { { 0.6f, 0.6f }, { ANCHO - 0.6f, 0.6f },
                              { 0.6f, FONDO - 0.6f }, { ANCHO - 0.6f, FONDO - 0.6f } };
        for (int i = 0; i < 4; i++)
        {
            var g = Grupo("Emergencia_" + (i + 1), p);
            g.transform.localPosition = new Vector3(esquinas[i, 0], ALTO - 0.18f, esquinas[i, 1]);

            Cubo("Caja", g.transform, Vector3.zero, new Vector3(0.18f, 0.1f, 0.12f), mAceroOscuro);
            Cubo("Foco", g.transform, new Vector3(0f, -0.055f, 0f), new Vector3(0.14f, 0.01f, 0.08f), mVerdeLuz);

            var luz = LuzPunto("Luz", g.transform, new Vector3(0f, -0.15f, 0f),
                               new Color(0.3f, 1f, 0.5f), 0.9f, 4.5f);
            if (i != 1) luz.gameObject.AddComponent<Parpadeo>();

            sinEnergia.Add(g);
        }

        return sinEnergia;
    }

    // Zona invisible en el vano de entrada. Cuando el jugador la pisa, el cuarto vuelve
    // a poner su propio clima: la luz ambiental y la niebla son de toda la escena, así
    // que si no, el laboratorio se vería con la luz que dejó prendida el cuarto anterior.
    static void ArmarEntrada(Transform raiz, ControlEnergia control)
    {
        // Va del lado de afuera, en el pasillo: así el laboratorio ya se ve oscuro desde
        // el vano, antes de entrar, y no se pierde el efecto
        var zona = Grupo("Zona_Entrada", raiz);
        zona.transform.localPosition = new Vector3((ENTRADA_X0 + ENTRADA_X1) / 2f, 1f, -0.9f);

        var colision = zona.AddComponent<BoxCollider>();
        colision.isTrigger = true;
        colision.size = new Vector3(ENTRADA_X1 - ENTRADA_X0 + 0.8f, 2f, 1.4f);

        var disparador = zona.AddComponent<DisparadorJugador>();
        UnityEventTools.AddVoidPersistentListener(disparador.alEntrar, new UnityAction(control.Reaplicar));
        EditorUtility.SetDirty(disparador);
    }

    static ControlEnergia ArmarControlEnergia(Transform raiz, List<Light> lucesCuarto,
                                              List<GameObject> conEnergia, List<GameObject> sinEnergia)
    {
        var g = Grupo("Energia", raiz);
        var control = g.AddComponent<ControlEnergia>();
        control.lucesDelCuarto = lucesCuarto.ToArray();
        control.objetosConEnergia = conEnergia.ToArray();
        control.objetosSinEnergia = sinEnergia.ToArray();
        control.efectosSinEnergia = raiz.GetComponentsInChildren<Parpadeo>();

        // Es el cuarto más oscuro de los cuatro: casi no hay luz ambiental y la niebla
        // cierra la vista a pocos metros hasta que vuelve la corriente
        control.ambienteSinEnergia = new Color(0.006f, 0.016f, 0.01f);
        control.ambienteConEnergia = new Color(0.48f, 0.52f, 0.48f);
        control.nieblaSinEnergia = new Color(0.008f, 0.022f, 0.014f);
        control.nieblaConEnergia = new Color(0.28f, 0.3f, 0.28f);
        control.densidadSinEnergia = 0.13f;
        control.densidadConEnergia = 0.008f;

        EditorUtility.SetDirty(control);
        return control;
    }

    // ------------------------------------------------------------------ varios

    static void AjustarAmbienteEditor()
    {
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
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

    // ------------------------------------------------------------------ modelos de Poly Haven

    // Pone un modelo de Poly Haven si está descargado en el proyecto y devuelve el objeto
    // que lo contiene; si no está, devuelve null y el que llama arma otra cosa.
    // El modelo se escala a su altura real, así no importa con qué escala venga el archivo.
    // "apoyar": true deja la base en el punto indicado; false lo centra (cosas colgadas).
    static GameObject Modelo(string nombrePolyHaven, Transform padre, Vector3 pos, float giroY,
                             float altoReal, bool colisiona, bool apoyar = true)
    {
        var fuente = BuscarModelo(nombrePolyHaven);
        if (fuente == null) return null;

        var contenedor = Grupo(nombrePolyHaven, padre);
        contenedor.transform.localPosition = pos;
        contenedor.transform.localEulerAngles = new Vector3(0f, giroY, 0f);

        var modelo = (GameObject)PrefabUtility.InstantiatePrefab(fuente, contenedor.transform);
        modelo.transform.localPosition = Vector3.zero;
        ArreglarMateriales(modelo);

        if (!Limites(modelo, out Bounds b)) return contenedor;

        if (altoReal > 0f && b.size.y > 0.0001f)
        {
            modelo.transform.localScale *= altoReal / b.size.y;
            Limites(modelo, out b);
        }

        Vector3 destino = contenedor.transform.position;
        modelo.transform.position += destino - new Vector3(b.center.x, apoyar ? b.min.y : b.center.y, b.center.z);

        if (colisiona)
        {
            Limites(modelo, out b);
            var col = contenedor.AddComponent<BoxCollider>();
            col.center = contenedor.transform.InverseTransformPoint(b.center);
            Vector3 t = b.size;
            if (Mathf.Abs(Mathf.Sin(giroY * Mathf.Deg2Rad)) > 0.7f) t = new Vector3(t.z, t.y, t.x);
            col.size = t;
        }

        return contenedor;
    }

    // Los vidrios de los modelos salen opacos y el follaje trae la transparencia aparte:
    // se reemplazan esos materiales por unos armados acá.
    static void ArreglarMateriales(GameObject modelo)
    {
        foreach (var r in modelo.GetComponentsInChildren<Renderer>())
        {
            var mats = r.sharedMaterials;
            bool cambio = false;
            for (int i = 0; i < mats.Length; i++)
            {
                if (mats[i] == null) continue;
                string n = mats[i].name;

                if (n.EndsWith("_glass"))
                {
                    mats[i] = mVidrioLab;
                    cambio = true;
                    continue;
                }

                var diff = BuscarTextura(n + "_diff");
                var alfa = BuscarTextura(n + "_alpha");
                if (alfa == null) alfa = BuscarTextura(n + "_opacity");
                if (diff != null && alfa != null)
                {
                    mats[i] = MaterialRecortado(n, diff, alfa, BuscarTextura(n + "_nor_gl"));
                    cambio = true;
                }
            }
            if (cambio) r.sharedMaterials = mats;
        }
    }

    static Material MaterialRecortado(string nombre, Texture2D diff, Texture2D alfa, Texture2D normal)
    {
        const string CARPETA = "Assets/Materials/Cuarto4/PolyHaven";
        if (!AssetDatabase.IsValidFolder(CARPETA))
            AssetDatabase.CreateFolder("Assets/Materials/Cuarto4", "PolyHaven");

        var textura = CombinarAlfa(diff, alfa, CARPETA + "/" + nombre + "_rgba.png");

        string ruta = CARPETA + "/" + nombre + ".mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(ruta);
        if (m == null)
        {
            m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(m, ruta);
        }

        m.SetTexture("_BaseMap", textura);
        m.SetColor("_BaseColor", Color.white);
        m.SetFloat("_Smoothness", 0.25f);
        m.SetFloat("_AlphaClip", 1f);
        m.SetFloat("_Cutoff", 0.5f);
        m.EnableKeyword("_ALPHATEST_ON");
        m.SetFloat("_Cull", 0f);
        m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.AlphaTest;

        if (normal != null)
        {
            ConfigurarComoNormal(normal);
            m.SetTexture("_BumpMap", normal);
            m.EnableKeyword("_NORMALMAP");
        }

        EditorUtility.SetDirty(m);
        return m;
    }

    static Texture2D CombinarAlfa(Texture2D color, Texture2D alfa, string ruta)
    {
        var existente = AssetDatabase.LoadAssetAtPath<Texture2D>(ruta);
        if (existente != null) return existente;

        color = HacerLegible(color);
        alfa = HacerLegible(alfa);
        if (color.width != alfa.width || color.height != alfa.height) return color;

        var pixeles = color.GetPixels();
        var transparencia = alfa.GetPixels();
        for (int i = 0; i < pixeles.Length; i++) pixeles[i].a = transparencia[i].r;

        var t = new Texture2D(color.width, color.height, TextureFormat.RGBA32, false);
        t.SetPixels(pixeles);
        t.Apply();
        File.WriteAllBytes(ruta, t.EncodeToPNG());
        Object.DestroyImmediate(t);

        AssetDatabase.ImportAsset(ruta);
        return AssetDatabase.LoadAssetAtPath<Texture2D>(ruta);
    }

    static Texture2D HacerLegible(Texture2D t)
    {
        string ruta = AssetDatabase.GetAssetPath(t);
        var importer = AssetImporter.GetAtPath(ruta) as TextureImporter;
        if (importer != null && !importer.isReadable)
        {
            importer.isReadable = true;
            importer.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Texture2D>(ruta);
    }

    static bool Limites(GameObject go, out Bounds b)
    {
        b = new Bounds();
        var renderers = go.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return false;
        b = renderers[0].bounds;
        foreach (var r in renderers) b.Encapsulate(r.bounds);
        return true;
    }

    static string[] CarpetasPolyHaven()
    {
        var carpetas = new List<string>();
        foreach (var c in new[] { "Assets/PolyHaven", "Assets/Textures", "Assets/Modelos" })
            if (AssetDatabase.IsValidFolder(c)) carpetas.Add(c);
        return carpetas.ToArray();
    }

    static GameObject BuscarModelo(string nombre)
    {
        var carpetas = CarpetasPolyHaven();
        if (carpetas.Length == 0) return null;

        foreach (var guid in AssetDatabase.FindAssets("t:GameObject", carpetas))
        {
            string ruta = AssetDatabase.GUIDToAssetPath(guid);
            if (!ruta.EndsWith(".fbx", System.StringComparison.OrdinalIgnoreCase)) continue;
            string archivo = Path.GetFileNameWithoutExtension(ruta);
            if (archivo == nombre || archivo.StartsWith(nombre + "_"))
                return AssetDatabase.LoadAssetAtPath<GameObject>(ruta);
        }
        return null;
    }

    static Texture2D BuscarTextura(string prefijo)
    {
        var carpetas = CarpetasPolyHaven();
        if (carpetas.Length == 0) return null;

        foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", carpetas))
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
        if (guids.Length == 0) return null;
        return AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guids[0]));
    }

    static void ConfigurarNormalesDeModelos()
    {
        var carpetas = CarpetasPolyHaven();
        if (carpetas.Length == 0) return;

        foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", carpetas))
        {
            string ruta = AssetDatabase.GUIDToAssetPath(guid);
            if (!Path.GetFileNameWithoutExtension(ruta).Contains("_nor_gl")) continue;
            var textura = AssetDatabase.LoadAssetAtPath<Texture2D>(ruta);
            if (textura != null) ConfigurarComoNormal(textura);
        }
    }

    static void ConfigurarComoNormal(Texture2D textura)
    {
        string ruta = AssetDatabase.GetAssetPath(textura);
        var importer = AssetImporter.GetAtPath(ruta) as TextureImporter;
        if (importer == null || importer.textureType == TextureImporterType.NormalMap) return;

        importer.textureType = TextureImporterType.NormalMap;
        importer.SaveAndReimport();
    }

    // ------------------------------------------------------------------ piezas básicas

    static GameObject Grupo(string nombre, Transform padre)
    {
        var go = new GameObject(nombre);
        go.transform.SetParent(padre, false);
        return go;
    }

    static GameObject Cubo(string n, Transform p, Vector3 pos, Vector3 esc, Material m, bool colisiona = false)
        => Primitiva(PrimitiveType.Cube, n, p, pos, esc, m, colisiona);

    static GameObject Cilindro(string n, Transform p, Vector3 pos, Vector3 esc, Material m, bool colisiona = false)
        => Primitiva(PrimitiveType.Cylinder, n, p, pos, esc, m, colisiona);

    static GameObject Esfera(string n, Transform p, Vector3 pos, Vector3 esc, Material m, bool colisiona = false)
        => Primitiva(PrimitiveType.Sphere, n, p, pos, esc, m, colisiona);

    static GameObject Primitiva(PrimitiveType tipo, string nombre, Transform padre,
                                Vector3 pos, Vector3 esc, Material mat, bool colisiona)
    {
        var go = GameObject.CreatePrimitive(tipo);
        go.name = nombre;
        go.transform.SetParent(padre, false);
        go.transform.localPosition = pos;
        go.transform.localScale = esc;
        if (mat != null) go.GetComponent<Renderer>().sharedMaterial = mat;

        var col = go.GetComponent<Collider>();
        if (!colisiona && col != null) Object.DestroyImmediate(col);
        return go;
    }

    static TextMeshPro Texto(string nombre, Transform padre, Vector3 pos, Vector3 rot,
                             string texto, float tamano, Color color, float ancho, float alto)
    {
        var go = new GameObject(nombre, typeof(RectTransform), typeof(TextMeshPro));
        go.transform.SetParent(padre, false);
        go.transform.localPosition = pos;
        go.transform.localEulerAngles = rot;
        // "rot" se piensa como si el texto se leyera desde su +Z, pero TextMeshPro se lee
        // desde su -Z: sin este giro todos los textos se verían en espejo
        go.transform.Rotate(0f, 180f, 0f, Space.Self);

        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(ancho, alto);

        var t = go.GetComponent<TextMeshPro>();
        t.text = texto;
        t.fontSize = tamano;
        t.color = color;
        t.alignment = TextAlignmentOptions.Center;
        return t;
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
        return l;
    }

    static AudioSource AudioEn(string nombre, Transform padre)
    {
        var go = new GameObject(nombre);
        go.transform.SetParent(padre, false);
        var a = go.AddComponent<AudioSource>();
        a.playOnAwake = false;
        a.spatialBlend = 1f;
        return a;
    }

    static void Resaltar(GameObject go, Renderer r)
    {
        var h = go.AddComponent<HoverHighlight>();
        h.renderer_ = r;
        h.materialResaltado = mResaltado;
        EditorUtility.SetDirty(h);
    }

    // ------------------------------------------------------------------ materiales

    static void CrearMateriales()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Materials"))
            AssetDatabase.CreateFolder("Assets", "Materials");
        if (!AssetDatabase.IsValidFolder("Assets/Materials/Cuarto4"))
            AssetDatabase.CreateFolder("Assets/Materials", "Cuarto4");

        // Paleta de laboratorio: verde claro arriba, azulejo verde abajo y acero.
        // Los nombres sueltos ("metal_plate", etc.) son texturas de Poly Haven: si están
        // en el proyecto se usan, y si no queda el color plano.
        // Pared de arriba pintada y lisa, sin el dibujo del hormigón: se usa solo el
        // relieve de la textura y el color queda limpio, que es lo que la hace moderna.
        // Abajo, el azulejo blanco largo de los laboratorios, teñido de verde suave.
        mParedAlta = Mat("C4_ParedAlta", new Color(0.9f, 0.93f, 0.92f), 0f, 0.2f,
                         default, "painted_plaster_wall", 3f, 1.5f, false, true);
        mFriso = Mat("C4_Azulejo", new Color(0.55f, 0.86f, 0.74f), 0f, 0.65f,
                     default, "long_white_tiles", 5f, 2f, true);
        mGuarda = Mat("C4_Guarda", new Color(0.12f, 0.24f, 0.2f), 0.2f, 0.4f);
        mPiso = Mat("C4_Piso", new Color(0.62f, 0.64f, 0.63f), 0.55f, 0.45f,
                    default, "metal_plate", 4f, 4.7f);
        mTecho = Mat("C4_Techo", new Color(0.72f, 0.74f, 0.72f), 0f, 0.1f);

        mAcero = Mat("C4_Acero", new Color(0.62f, 0.65f, 0.66f), 0.8f, 0.6f);
        mAceroOscuro = Mat("C4_AceroOscuro", new Color(0.22f, 0.25f, 0.25f), 0.6f, 0.45f);
        mNegro = Mat("C4_Negro", new Color(0.05f, 0.06f, 0.06f), 0.3f, 0.35f);
        mBlanco = Mat("C4_Blanco", new Color(0.93f, 0.94f, 0.92f), 0f, 0.3f);
        mPapel = Mat("C4_Papel", new Color(0.92f, 0.9f, 0.84f), 0f, 0.1f);
        mSabana = Mat("C4_Sabana", new Color(0.87f, 0.88f, 0.84f), 0f, 0.08f);
        // Los huecos de la cara (cuencas y boca), el pelo y los carteles de cada paso
        mHueco = Mat("C4_Hueco", new Color(0.02f, 0.02f, 0.02f), 0f, 0.05f);
        mPelo = Mat("C4_Pelo", new Color(0.09f, 0.08f, 0.07f), 0f, 0.1f);
        mCorte = Mat("C4_Corte", new Color(0.28f, 0.07f, 0.06f), 0f, 0.25f);
        mCartel = Mat("C4_Cartel", new Color(0.95f, 0.8f, 0.15f), 0f, 0.4f, new Color(0.45f, 0.35f, 0.04f));

        // Lo del laboratorio de especímenes: el líquido de los tanques, las mangueras
        // que lo llevan y las pantallas. Todo esto brilla por material emisivo, no por
        // luces: así se ve el verde pero el cuarto sigue igual de oscuro al entrar.
        mManguera = Mat("C4_Manguera", new Color(0.35f, 0.62f, 0.3f), 0f, 0.5f, new Color(0.12f, 0.35f, 0.12f));
        mPantallaVerde = Mat("C4_PantallaVerde", new Color(0.05f, 0.14f, 0.07f), 0f, 0.7f, new Color(0.1f, 0.5f, 0.18f));
        mAmarillo = Mat("C4_Amarillo", new Color(0.93f, 0.82f, 0.15f), 0f, 0.4f, new Color(0.3f, 0.25f, 0.02f));

        // La piel del cuerpo: gris verdoso, como de formol
        mPiel = Mat("C4_Piel", new Color(0.72f, 0.73f, 0.66f), 0f, 0.12f);
        mPantalla = Mat("C4_Pizarra", new Color(0.09f, 0.16f, 0.13f), 0f, 0.25f);
        mQuemadura = Mat("C4_Quemadura", new Color(0.1f, 0.1f, 0.09f), 0f, 0.05f);
        // Manchas secas alrededor de la camilla
        mMancha = Mat("C4_Mancha", new Color(0.16f, 0.07f, 0.06f), 0f, 0.15f);

        mVerdeLuz = Mat("C4_VerdeLuz", new Color(0.3f, 0.95f, 0.45f), 0f, 0.5f, new Color(0.2f, 1.1f, 0.35f));
        mRojo = Mat("C4_Rojo", new Color(0.65f, 0.14f, 0.12f), 0.2f, 0.4f);
        mRojoLuz = Mat("C4_RojoLuz", new Color(1f, 0.25f, 0.2f), 0f, 0.5f, new Color(1.1f, 0.15f, 0.1f));
        mAmbar = Mat("C4_Ambar", new Color(0.95f, 0.72f, 0.15f), 0f, 0.4f);
        mLlama = Mat("C4_Llama", new Color(0.4f, 0.7f, 1f), 0f, 0.6f, new Color(0.3f, 0.8f, 1.6f));
        mLuzTecho = Mat("C4_LuzTecho", new Color(1f, 1f, 0.97f), 0f, 0.5f, new Color(1.5f, 1.6f, 1.5f));
        mResaltado = Mat("C4_Resaltado", new Color(0.55f, 1f, 0.8f), 0f, 0.6f, new Color(0.25f, 0.9f, 0.6f));
        mVidrioLab = Transparente("C4_Vidrio", new Color(0.9f, 1f, 0.95f, 0.12f), default);
        // El líquido de los tanques: verde fuerte y translúcido, brillando por sí mismo
        mLiquido = Transparente("C4_Liquido", new Color(0.35f, 1f, 0.25f, 0.75f), new Color(0.2f, 0.9f, 0.15f));
        mVidrioTanque = Transparente("C4_VidrioTanque", new Color(0.8f, 1f, 0.9f, 0.1f), default);
        // El haz de luz que se ve dentro de la niebla, y el vapor que larga el gas
        mHaz = Transparente("C4_Haz", new Color(0.8f, 1f, 0.88f, 0.14f), new Color(0.35f, 0.7f, 0.5f));
        mVapor = Transparente("C4_Vapor", new Color(0.85f, 0.92f, 0.88f, 0.2f), default);

        // Los seis fusibles: los tres primeros son los que sirven
        mFusA = Mat("C4_Fusible10", new Color(0.85f, 0.2f, 0.15f), 0.1f, 0.6f);
        mFusB = Mat("C4_Fusible20", new Color(0.2f, 0.45f, 0.9f), 0.1f, 0.6f);
        mFusC = Mat("C4_Fusible15", new Color(0.25f, 0.75f, 0.35f), 0.1f, 0.6f);
        mFusX = Mat("C4_Fusible5", new Color(0.6f, 0.6f, 0.62f), 0.1f, 0.6f);
        mFusY = Mat("C4_Fusible30", new Color(0.9f, 0.55f, 0.15f), 0.1f, 0.6f);
        mFusZ = Mat("C4_Fusible25", new Color(0.55f, 0.35f, 0.75f), 0.1f, 0.6f);
    }

    // Material que deja ver a través: el vidrio del laboratorio, el vapor del gas y los
    // haces de luz. El alpha va en el color; si se le pasa emisión, además brilla.
    static Material Transparente(string nombre, Color color, Color emision)
    {
        var m = Mat(nombre, color, 0f, 0.9f, emision);
        m.SetFloat("_Surface", 1f);
        m.SetFloat("_Blend", 0f);
        m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        m.SetFloat("_ZWrite", 0f);
        m.SetOverrideTag("RenderType", "Transparent");
        m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        EditorUtility.SetDirty(m);
        return m;
    }

    // "polyHaven" es el nombre del asset en Poly Haven. Si sus texturas están en el
    // proyecto se le ponen al material; si no, queda el color plano.
    // "tenir": la textura se tiñe con el color. "soloRelieve": usa solo el normal map y
    // deja el color liso, como una pared pintada.
    static Material Mat(string nombre, Color color, float metalico, float suavidad, Color emision = default,
                        string polyHaven = null, float tileX = 1f, float tileY = 1f,
                        bool tenir = false, bool soloRelieve = false)
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
        if (m.HasProperty("_Color")) m.SetColor("_Color", color);
        if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", metalico);
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", suavidad);
        if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", suavidad);

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
                ConfigurarComoNormal(normal);
                m.SetTexture("_BumpMap", normal);
                m.SetTextureScale("_BumpMap", tiling);
                m.EnableKeyword("_NORMALMAP");
            }
        }

        EditorUtility.SetDirty(m);
        return m;
    }
}
