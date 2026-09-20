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
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;

// Arma el Cuarto 4 (Laboratorio de ciencias) adentro del objeto "Cuarto4_Laboratorio".
// Se corre desde el menú: Escape Room > Construir Cuarto 4. Borra lo que había adentro
// y lo rehace, así siempre queda igual.
//
// Mide lo mismo que el Cuarto 1 y el Cuarto 2: 6 x 7 metros y 3.05 de techo.
//
// Estilo: laboratorio de colegio. Piso de chapa, azulejo verde hasta la mitad de la
// pared y verde claro arriba, muebles de acero. Entra a oscuras, con la luz verdosa de
// emergencia, y recién se ve normal cuando vuelve la corriente. Los modelos vienen de
// Poly Haven si están en el proyecto (Assets/PolyHaven); si no están, cada mueble tiene
// su versión simple hecha con cubos.
//
// El cuarto se resuelve en tres pasos, como dice el documento de diseño:
// 1) ELECTRICIDAD: el panel de la pared del fondo pide tres fusibles (A, B, C). En la
//    mesa de trabajo hay seis y solo tres tienen el amperaje correcto. Con los tres
//    puestos vuelve la luz. -> ya funciona (PanelFusibles)
// 2) GAS: las dos llaves de la línea de gas se abren sosteniéndolas unos segundos.
//    -> por ahora están puestas en su lugar, sin la lógica
// 3) PALANCAS: la pizarra dice el orden de las tres palancas y después se habilita la
//    ranura del medallón que viene del Cuarto 2. -> por ahora están puestas, sin la lógica
public static class ConstructorCuarto4
{
    const float ANCHO = 6f;    // eje X: pared Oeste (0) a pared Este (6)
    const float FONDO = 7f;    // eje Z: entrada (0) al fondo (7)
    const float ALTO = 3.05f;  // altura del techo
    const float MURO = 0.12f;  // espesor de las paredes

    const float ENTRADA_X0 = 0.8f, ENTRADA_X1 = 1.9f;  // vano que viene del Cuarto 3
    const float SALIDA_X0 = 4.4f, SALIDA_X1 = 5.6f;    // vano de la puerta de emergencia
    const float ALTO_PUERTA = 2.1f;

    const float ALTO_FRISO = 1.45f;   // hasta dónde llega el azulejo verde

    static Material mParedAlta, mFriso, mGuarda, mPiso, mTecho, mAcero, mAceroOscuro, mNegro,
                    mBlanco, mVerdeLuz, mRojo, mAmbar, mPantalla, mResaltado, mPapel,
                    mSabana, mPiel, mLuzTecho, mVidrioLab, mQuemadura, mLlama,
                    mFusA, mFusB, mFusC, mFusX, mFusY, mFusZ;

    // Cartel de objetivos: cada paso del cuarto le avisa para que cambie el texto
    static PanelObjetivo panelObjetivo;

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
        ArmarPizarra(mobiliario.transform, conEnergia);
        ArmarCamilla(mobiliario.transform);
        ArmarLineaGas(mobiliario.transform);
        ArmarPalancas(mobiliario.transform);
        ArmarDecoracion(mobiliario.transform);

        ArmarPuertaSalida(raiz.transform);
        ArmarRanura(raiz.transform);

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
        jugador.position = raiz.transform.position + new Vector3(1.35f, 0f, 0.9f);
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

        // Pared de la entrada (viene del Cuarto 3), partida por el vano
        Muro(paredes.transform, "Pared_Entrada_Izq", -MURO, ENTRADA_X0, -MURO / 2f, 0f, ALTO);
        Muro(paredes.transform, "Pared_Entrada_Der", ENTRADA_X1, ANCHO + MURO, -MURO / 2f, 0f, ALTO);
        Muro(paredes.transform, "Dintel_Entrada", ENTRADA_X0, ENTRADA_X1, -MURO / 2f, ALTO_PUERTA, ALTO);

        // Pared del fondo: ahí van el panel eléctrico y la puerta de emergencia
        Muro(paredes.transform, "Pared_Fondo_Izq", -MURO, SALIDA_X0, FONDO + MURO / 2f, 0f, ALTO);
        Muro(paredes.transform, "Pared_Fondo_Der", SALIDA_X1, ANCHO + MURO, FONDO + MURO / 2f, 0f, ALTO);
        Muro(paredes.transform, "Dintel_Salida", SALIDA_X0, SALIDA_X1, FONDO + MURO / 2f, ALTO_PUERTA, ALTO);

        Cubo("Techo", p, new Vector3(ANCHO / 2f, ALTO + MURO / 2f, FONDO / 2f),
             new Vector3(ANCHO + MURO * 2f, MURO, FONDO + MURO * 2f), mTecho, true);

        // El azulejo verde: una chapa finita pegada a la pared, de 0 a 1.45 de alto,
        // con una guarda oscura arriba. Es lo que le da el aire de laboratorio.
        var friso = Grupo("Azulejo", p);
        FrisoLargo(friso.transform, "Friso_Oeste", 0.02f, FONDO / 2f, 0.03f, FONDO);
        FrisoLargo(friso.transform, "Friso_Este", ANCHO - 0.02f, FONDO / 2f, 0.03f, FONDO);
        FrisoAncho(friso.transform, "Friso_Entrada_Izq", ENTRADA_X0 / 2f, 0.02f, ENTRADA_X0);
        FrisoAncho(friso.transform, "Friso_Entrada_Der", (ENTRADA_X1 + ANCHO) / 2f, 0.02f, ANCHO - ENTRADA_X1);
        FrisoAncho(friso.transform, "Friso_Fondo_Izq", SALIDA_X0 / 2f, FONDO - 0.02f, SALIDA_X0);
        FrisoAncho(friso.transform, "Friso_Fondo_Der", (SALIDA_X1 + ANCHO) / 2f, FONDO - 0.02f, ANCHO - SALIDA_X1);

        // Dos luminarias de tubo en el techo: la carcasa se ve siempre, el tubo
        // encendido y la luz solo cuando hay corriente
        int n = 1;
        foreach (float z in new[] { 2.2f, 4.8f })
        {
            var lum = Grupo("Luminaria_" + n, p);
            lum.transform.localPosition = new Vector3(ANCHO / 2f, ALTO - 0.04f, z);

            if (Modelo("mounted_fluorescent_lights", lum.transform, Vector3.zero, 90f, 0.12f, false, false) == null)
                Cubo("Carcasa", lum.transform, Vector3.zero, new Vector3(2.6f, 0.06f, 0.2f), mAceroOscuro);

            var tubo = Cubo("Tubo", lum.transform, new Vector3(0f, -0.04f, 0f),
                            new Vector3(2.4f, 0.012f, 0.09f), mLuzTecho);
            tubo.SetActive(false);
            conEnergia.Add(tubo);

            lucesCuarto.Add(LuzPunto("Luz", lum.transform, new Vector3(0f, -0.35f, 0f),
                                     new Color(0.95f, 1f, 0.97f), 3.4f, 10f));
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
        g.transform.localPosition = new Vector3(0.06f, 1.75f, 0.85f);
        g.transform.localEulerAngles = new Vector3(0f, 90f, 0f);

        Cubo("Marco", g.transform, Vector3.zero, new Vector3(1.1f, 0.7f, 0.04f), mAceroOscuro, true);
        Cubo("Tablero", g.transform, new Vector3(0f, 0f, 0.022f), new Vector3(1.04f, 0.64f, 0.01f), mBlanco);
        Texto("Titulo", g.transform, new Vector3(0f, 0.25f, 0.03f), Vector3.zero,
              "LABORATORIO", 0.26f, new Color(0.1f, 0.1f, 0.1f), 0.9f, 0.1f);
        Cubo("Linea", g.transform, new Vector3(0f, 0.19f, 0.028f), new Vector3(0.9f, 0.004f, 0.002f), mAceroOscuro);

        var texto = Texto("Texto_Objetivo", g.transform, new Vector3(0f, -0.06f, 0.03f), Vector3.zero,
                          "", 0.27f, new Color(0.15f, 0.15f, 0.15f), 0.98f, 0.46f);
        texto.lineSpacing = -12f;

        LuzPunto("Luz_Panel", g.transform, new Vector3(0f, 0.1f, 0.5f), new Color(1f, 0.95f, 0.85f), 1.4f, 1.6f);

        panelObjetivo = g.AddComponent<PanelObjetivo>();
        panelObjetivo.texto = texto;
        panelObjetivo.pasos = new[]
        {
            "SIN ENERGIA\n\nEl panel de la pared del fondo\npide tres fusibles.\nEstan en la mesa de trabajo.",
            "VOLVIO LA LUZ\n\nAbri las dos llaves de gas\nde la columna del medio.\nHay que sostenerlas unos segundos.",
            "GAS ABIERTO\n\nLos mecheros encendieron.\nLa pizarra dice en que orden\nvan las tres palancas.",
            "SECUENCIA CORRECTA\n\nSe habilito la ranura\nque esta junto a la puerta.\nPone ahi el medallon del Cuarto 2.",
            "PUERTA ABIERTA\n\nSali del laboratorio."
        };
        texto.text = panelObjetivo.pasos[0];   // así ya se ve en el editor, sin darle Play
        EditorUtility.SetDirty(panelObjetivo);
    }

    static void AvisarPanel(UnityEventBase evento, int paso)
    {
        if (panelObjetivo == null) return;
        UnityEventTools.AddIntPersistentListener(evento, new UnityAction<int>(panelObjetivo.MostrarPaso), paso);
    }

    // ------------------------------------------------------------------ paso 1: panel eléctrico

    // Panel de la pared del fondo con los tres encajes A, B y C. Al lado de cada encaje
    // dice el amperaje que pide. Con los tres fusibles correctos vuelve la corriente.
    static void ArmarPanelElectrico(Transform raiz, ControlEnergia control)
    {
        var g = Grupo("Panel_Electrico", raiz);
        g.transform.localPosition = new Vector3(1.9f, 1.25f, FONDO - 0.07f);
        // Girado 180 para que su "adelante" (+Z) apunte hacia adentro del cuarto: todo lo
        // que tiene que verse va con z positivo, si no queda metido en la pared
        g.transform.localEulerAngles = new Vector3(0f, 180f, 0f);

        Cubo("Caja", g.transform, Vector3.zero, new Vector3(1.3f, 0.8f, 0.12f), mAceroOscuro, true);
        Cubo("Fondo_Caja", g.transform, new Vector3(0f, 0f, 0.065f), new Vector3(1.2f, 0.7f, 0.02f), mAcero);
        Texto("Titulo", g.transform, new Vector3(0f, 0.3f, 0.08f), Vector3.zero,
              "TABLERO GENERAL", 0.22f, new Color(0.9f, 0.95f, 0.9f), 1.1f, 0.09f);

        // Modelo de Poly Haven al costado, como caja de la instalación
        Modelo("power_box_01", raiz, new Vector3(0.55f, 1.2f, FONDO - 0.12f), 180f, 0.5f, false, false);

        string[] letras = { "A", "B", "C" };
        string[] amperajes = { "10", "20", "15" };
        Material[] colores = { mFusA, mFusB, mFusC };
        float[] xs = { -0.42f, 0f, 0.42f };

        var encajes = new XRSocketInteractor[3];
        var lucesOk = new GameObject[3];

        for (int i = 0; i < 3; i++)
        {
            var hueco = Grupo("Encaje_" + letras[i], g.transform);
            hueco.transform.localPosition = new Vector3(xs[i], -0.05f, 0.07f);

            // El tubo donde entra el fusible y las dos bornes de contacto
            Cilindro("Tubo", hueco.transform, Vector3.zero, new Vector3(0.09f, 0.02f, 0.09f), mAceroOscuro)
                .transform.localEulerAngles = new Vector3(90f, 0f, 0f);
            Cubo("Borne_Arriba", hueco.transform, new Vector3(0f, 0.07f, 0.01f),
                 new Vector3(0.07f, 0.02f, 0.03f), colores[i]);
            Cubo("Borne_Abajo", hueco.transform, new Vector3(0f, -0.07f, 0.01f),
                 new Vector3(0.07f, 0.02f, 0.03f), colores[i]);

            Texto("Letra", hueco.transform, new Vector3(0f, 0.16f, 0.02f), Vector3.zero,
                  letras[i], 0.3f, new Color(0.95f, 0.98f, 0.95f), 0.2f, 0.12f);
            Texto("Amperaje", hueco.transform, new Vector3(0f, -0.16f, 0.02f), Vector3.zero,
                  amperajes[i] + "A", 0.22f, new Color(1f, 0.85f, 0.35f), 0.24f, 0.09f);

            // El encaje propiamente dicho: un disparador donde se suelta el fusible
            var ranura = Grupo("Ranura", hueco.transform);
            ranura.transform.localPosition = new Vector3(0f, 0f, 0.04f);
            var colision = ranura.AddComponent<SphereCollider>();
            colision.isTrigger = true;
            colision.radius = 0.07f;
            encajes[i] = ranura.AddComponent<XRSocketInteractor>();

            // Lucecita verde: se prende cuando el fusible puesto es el correcto
            var luz = Cilindro("Luz_Ok", hueco.transform, new Vector3(0f, 0.24f, 0.02f),
                               new Vector3(0.03f, 0.008f, 0.03f), mVerdeLuz);
            luz.transform.localEulerAngles = new Vector3(90f, 0f, 0f);
            luz.SetActive(false);
            lucesOk[i] = luz;
        }

        // Luz piloto de batería: queda prendida siempre, así el panel se encuentra
        // aunque el cuarto esté a oscuras
        LuzPunto("Luz_Piloto", g.transform, new Vector3(0f, 0.45f, 0.45f),
                 new Color(0.65f, 1f, 0.75f), 1.3f, 2.8f);

        var audio = AudioEn("Audio_Acierto", g.transform);

        var panel = g.AddComponent<PanelFusibles>();
        panel.encajes = encajes;
        panel.amperajesCorrectos = amperajes;
        panel.lucesOk = lucesOk;
        panel.audioAcierto = audio;

        // Con los tres fusibles puestos vuelve la corriente a todo el cuarto
        UnityEventTools.AddVoidPersistentListener(panel.alCompletar, new UnityAction(control.Encender));
        AvisarPanel(panel.alCompletar, 1);
        EditorUtility.SetDirty(panel);
    }

    // ------------------------------------------------------------------ mesa de trabajo

    // Mesa contra la pared Oeste con los seis fusibles sueltos: tres sirven y tres no.
    // Al lado, la hoja de la instalación que dice qué amperaje va en cada encaje.
    static void ArmarMesaTrabajo(Transform p)
    {
        var g = Grupo("Mesa_Trabajo", p);
        g.transform.localPosition = new Vector3(0.95f, 0f, 4.7f);

        // El escritorio de metal mide 0.78 de alto, así que la tapa queda en 0.79
        if (Modelo("metal_office_desk", g.transform, Vector3.zero, 90f, 0.78f, true) == null)
            MesaDeAcero(g.transform, Vector3.zero, 0.8f, 1.7f, 0.765f);

        float tapa = 0.79f;

        // Los seis fusibles, en dos filas. Los tres primeros son los correctos.
        string[] amperajes = { "10", "20", "15", "5", "30", "25" };
        Material[] colores = { mFusA, mFusB, mFusC, mFusX, mFusY, mFusZ };
        for (int i = 0; i < 6; i++)
        {
            float x = (i % 2 == 0) ? -0.16f : 0.1f;
            float z = -0.55f + (i / 2) * 0.5f;
            ArmarFusible(g.transform, new Vector3(x, tapa + 0.02f, z), amperajes[i], colores[i]);
        }

        // Hoja de la instalación apoyada en la mesa
        var hoja = Grupo("Hoja_Instalacion", g.transform);
        hoja.transform.localPosition = new Vector3(0.14f, tapa + 0.002f, 0.55f);
        hoja.transform.localEulerAngles = new Vector3(0f, 12f, 0f);
        Cubo("Papel", hoja.transform, Vector3.zero, new Vector3(0.24f, 0.002f, 0.32f), mPapel);
        var texto = Texto("Texto", hoja.transform, new Vector3(0f, 0.003f, 0f), new Vector3(-90f, 0f, 0f),
                          "TABLERO\n\nA = 10A\nB = 20A\nC = 15A\n\nLos demas\nno entran.",
                          0.22f, new Color(0.2f, 0.18f, 0.15f), 0.22f, 0.3f);
        texto.lineSpacing = -16f;

        // Lámpara de trabajo a pilas sobre la mesa: los fusibles se ven desde la entrada
        Cubo("Lampara", g.transform, new Vector3(0.24f, tapa + 0.16f, -0.75f),
             new Vector3(0.12f, 0.06f, 0.12f), mAceroOscuro);
        LuzPunto("Luz_Lampara", g.transform, new Vector3(0.1f, tapa + 0.3f, -0.4f),
                 new Color(0.8f, 1f, 0.85f), 1.5f, 2.6f);

        Modelo("metal_stool_02", p, new Vector3(1.8f, 0f, 4.4f), 25f, 0.62f, true);
    }

    // Un fusible: cilindro de vidrio con casquillos de metal y el amperaje escrito.
    // Se agarra con la mano y se encaja en el panel.
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
        Texto("Numero", g.transform, new Vector3(0f, 0.019f, 0f), new Vector3(-90f, 0f, 0f),
              amperaje, 0.17f, new Color(0.1f, 0.1f, 0.1f), 0.08f, 0.05f);

        var agarre = g.AddComponent<XRGrabInteractable>();
        var rb = g.GetComponent<Rigidbody>();
        if (rb != null) { rb.isKinematic = true; rb.useGravity = false; }
        Resaltar(g, g.transform.Find("Cuerpo").GetComponent<Renderer>());

        var datos = g.AddComponent<Fusible>();
        datos.amperaje = amperaje;

        EditorUtility.SetDirty(agarre);
        EditorUtility.SetDirty(datos);
    }

    // ------------------------------------------------------------------ mesa de química

    // Mesa de las prácticas, con los tubos de ensayo y el microscopio. Es ambientación:
    // no hay que resolver nada acá, pero es lo que hace que el cuarto parezca un
    // laboratorio de verdad.
    static void ArmarMesaQuimica(Transform p)
    {
        var g = Grupo("Mesa_Quimica", p);
        g.transform.localPosition = new Vector3(0.95f, 0f, 2f);

        // La tapa de la mesa queda a 0.9 (alto de mesada de laboratorio)
        MesaDeAcero(g.transform, Vector3.zero, 0.8f, 1.8f, 0.875f);
        float tapa = 0.9f;

        Modelo("chemistry_set", g.transform, new Vector3(0f, tapa, -0.45f), 90f, 0.3f, false);
        Modelo("industrial_microscope", g.transform, new Vector3(0.05f, tapa, 0.55f), 120f, 0.32f, false);
        // La máscara de gas, tirada en la mesada: es de la práctica que quedó a medias
        Modelo("old_gas_mask", g.transform, new Vector3(-0.1f, tapa, 0.15f), 200f, 0.16f, false);

        // Un mechero apagado: se enciende cuando haya gas (paso 2)
        ArmarMechero(g.transform, new Vector3(0.12f, tapa, -0.05f));
    }

    // Mechero de laboratorio. La llama arranca apagada.
    static void ArmarMechero(Transform p, Vector3 pos)
    {
        var g = Grupo("Mechero", p);
        g.transform.localPosition = pos;

        Cilindro("Base", g.transform, new Vector3(0f, 0.01f, 0f), new Vector3(0.11f, 0.01f, 0.11f), mAceroOscuro);
        Cilindro("Tubo", g.transform, new Vector3(0f, 0.08f, 0f), new Vector3(0.035f, 0.07f, 0.035f), mAcero);

        var llama = Cilindro("Llama", g.transform, new Vector3(0f, 0.19f, 0f),
                             new Vector3(0.03f, 0.04f, 0.03f), mLlama);
        llama.name = "Llama";
        llama.SetActive(false);   // se prende en el paso 2, cuando se abre el gas
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

    // Pizarra de la clase que nunca terminó: tiene el orden de las palancas. A oscuras
    // no se lee; aparece cuando vuelve la corriente.
    static void ArmarPizarra(Transform p, List<GameObject> conEnergia)
    {
        var g = Grupo("Pizarra", p);
        g.transform.localPosition = new Vector3(0.06f, 1.95f, 3.4f);
        g.transform.localEulerAngles = new Vector3(0f, 90f, 0f);

        Cubo("Marco", g.transform, Vector3.zero, new Vector3(1.5f, 1f, 0.04f), mAceroOscuro, true);
        Cubo("Tablero", g.transform, new Vector3(0f, 0f, 0.022f), new Vector3(1.42f, 0.92f, 0.01f), mPantalla);
        Cubo("Bandeja", g.transform, new Vector3(0f, -0.52f, 0.05f), new Vector3(1.4f, 0.03f, 0.07f), mAceroOscuro);

        var texto = Texto("Texto_Diagrama", g.transform, new Vector3(0f, 0f, 0.03f), Vector3.zero,
                          "PRACTICA 4 - MECHEROS\n\n" +
                          "Orden de encendido:\n\n" +
                          "1o   P2        2o   P3        3o   P1",
                          0.52f, new Color(0.85f, 0.95f, 0.85f), 1.36f, 0.86f);
        texto.lineSpacing = -14f;
        texto.gameObject.SetActive(false);
        conEnergia.Add(texto.gameObject);
    }

    // ------------------------------------------------------------------ camilla

    // Mesa de disección de biología, con la sábana encima y el cuerpo debajo. Por ahora
    // es solo ambientación: el susto (la sábana que se cae) se conecta cuando se arme
    // el paso 2, que es cuando el documento de diseño dice que pasa.
    static void ArmarCamilla(Transform p)
    {
        var g = Grupo("Camilla", p);
        g.transform.localPosition = new Vector3(2.5f, 0f, 3.2f);

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

        // El cuerpo, armado con cubos y una esfera. La cabeza queda destapada, mirando
        // a la entrada: es lo primero que se ve al entrar al cuarto.
        var cuerpo = Grupo("Cuerpo", g.transform);
        cuerpo.transform.localPosition = new Vector3(0f, 0.93f, 0f);

        Esfera("Cabeza", cuerpo.transform, new Vector3(0f, 0.11f, -0.82f), new Vector3(0.19f, 0.24f, 0.24f), mPiel);
        Cubo("Cuello", cuerpo.transform, new Vector3(0f, 0.08f, -0.68f), new Vector3(0.11f, 0.1f, 0.08f), mPiel);
        Cubo("Torso", cuerpo.transform, new Vector3(0f, 0.1f, -0.33f), new Vector3(0.42f, 0.22f, 0.62f), mPiel);
        Cubo("Cadera", cuerpo.transform, new Vector3(0f, 0.09f, 0.09f), new Vector3(0.36f, 0.2f, 0.28f), mPiel);
        foreach (float x in new[] { -0.1f, 0.1f })
            Cubo("Pierna", cuerpo.transform, new Vector3(x, 0.08f, 0.58f), new Vector3(0.15f, 0.17f, 0.72f), mPiel);
        foreach (float x in new[] { -0.09f, 0.09f })
            Cubo("Pie", cuerpo.transform, new Vector3(x, 0.09f, 1f), new Vector3(0.12f, 0.2f, 0.14f), mPiel);
        foreach (float x in new[] { -0.26f, 0.26f })
            Cubo("Brazo", cuerpo.transform, new Vector3(x, 0.08f, -0.25f), new Vector3(0.13f, 0.13f, 0.6f), mPiel);

        // La sábana: lo tapa del cuello a los pies, con las puntas colgando al costado
        var sabana = Grupo("Sabana", g.transform);
        sabana.transform.localPosition = new Vector3(0f, 0f, 0f);
        Cubo("Manta", sabana.transform, new Vector3(0f, 1.15f, 0.22f), new Vector3(0.84f, 0.03f, 1.62f), mSabana);
        foreach (float x in new[] { -0.42f, 0.42f })
            Cubo("Caida", sabana.transform, new Vector3(x, 1.04f, 0.22f), new Vector3(0.03f, 0.24f, 1.62f), mSabana);
        Cubo("Caida_Pies", sabana.transform, new Vector3(0f, 1.04f, 1.03f), new Vector3(0.84f, 0.24f, 0.03f), mSabana);

        // Cartel de la práctica, colgado del borde de la camilla
        var ficha = Grupo("Ficha", g.transform);
        ficha.transform.localPosition = new Vector3(0f, 0.72f, -1f);
        Cubo("Carton", ficha.transform, Vector3.zero, new Vector3(0.3f, 0.2f, 0.006f), mPapel);
        Texto("Texto", ficha.transform, new Vector3(0f, 0f, -0.006f), new Vector3(0f, 180f, 0f),
              "PRACTICA\nSUSPENDIDA", 0.16f, new Color(0.35f, 0.1f, 0.1f), 0.28f, 0.18f);
    }

    // ------------------------------------------------------------------ paso 2: línea de gas

    // La columna de gas en el medio del cuarto, con las dos llaves (V1 abajo y V2 arriba).
    // Por ahora las llaves están puestas y resaltan al acercar la mano, pero todavía no
    // abren nada: eso es el paso 2, que se arma después.
    static void ArmarLineaGas(Transform p)
    {
        var g = Grupo("Linea_Gas", p);
        g.transform.localPosition = new Vector3(4.1f, 0f, 3.4f);

        // Caño vertical del piso al techo y un tramo que se va por el techo
        Cilindro("Caño_Vertical", g.transform, new Vector3(0f, ALTO / 2f, 0f),
                 new Vector3(0.1f, ALTO / 2f, 0.1f), mAcero, true);
        Cilindro("Caño_Techo", g.transform, new Vector3(0f, ALTO - 0.2f, -1.5f),
                 new Vector3(0.08f, 1.5f, 0.08f), mAcero)
            .transform.localEulerAngles = new Vector3(90f, 0f, 0f);
        Cubo("Brida", g.transform, new Vector3(0f, 0.06f, 0f), new Vector3(0.24f, 0.03f, 0.24f), mAceroOscuro);

        // Cartel de gas, mirando al centro del cuarto (hacia el Oeste)
        var cartel = Grupo("Cartel_Gas", g.transform);
        cartel.transform.localPosition = new Vector3(-0.06f, 2.05f, 0f);
        cartel.transform.localEulerAngles = new Vector3(0f, -90f, 0f);
        Cubo("Chapa", cartel.transform, Vector3.zero, new Vector3(0.3f, 0.16f, 0.006f), mAmbar);
        Texto("Texto", cartel.transform, new Vector3(0f, 0f, 0.006f), Vector3.zero,
              "GAS", 0.3f, new Color(0.1f, 0.1f, 0.1f), 0.28f, 0.14f);

        ArmarLlaveGas(g.transform, "V1", 1f);
        ArmarLlaveGas(g.transform, "V2", 1.5f);
    }

    // Volante de la llave de gas: se agarra y se sostiene girando. Mira hacia el centro
    // del cuarto (hacia el Oeste) para que el jugador quede de costado a la camilla.
    static void ArmarLlaveGas(Transform p, string nombre, float altura)
    {
        var g = Grupo("Llave_" + nombre, p);
        g.transform.localPosition = new Vector3(-0.14f, altura, 0f);

        // El volante va adentro de un grupo girado 90 grados en Z: así su eje (que en los
        // cilindros de Unity es la Y) apunta al Oeste, o sea hacia el centro del cuarto
        var volante = Grupo("Volante", g.transform);
        volante.transform.localEulerAngles = new Vector3(0f, 0f, 90f);

        var aro = Cilindro("Aro", volante.transform, Vector3.zero,
                           new Vector3(0.24f, 0.012f, 0.24f), mRojo, true);
        Cilindro("Centro", volante.transform, new Vector3(0f, 0.01f, 0f),
                 new Vector3(0.07f, 0.02f, 0.07f), mAceroOscuro);
        Cilindro("Eje", volante.transform, new Vector3(0f, -0.07f, 0f),
                 new Vector3(0.03f, 0.07f, 0.03f), mAceroOscuro);
        foreach (float a in new[] { 0f, 60f, 120f })
        {
            var rayo = Cubo("Rayo", volante.transform, Vector3.zero,
                            new Vector3(0.22f, 0.014f, 0.035f), mRojo);
            rayo.transform.localEulerAngles = new Vector3(0f, a, 0f);
        }

        Texto("Etiqueta", g.transform, new Vector3(-0.02f, -0.17f, 0f), new Vector3(0f, -90f, 0f),
              nombre, 0.22f, new Color(0.95f, 0.95f, 0.9f), 0.14f, 0.06f);

        g.AddComponent<XRSimpleInteractable>();
        Resaltar(g, aro.GetComponent<Renderer>());
    }

    // ------------------------------------------------------------------ paso 3: palancas

    // Las tres palancas de la pared Este. El orden lo dice la pizarra. Por ahora están
    // puestas y se pueden tocar, pero la secuencia se arma en el paso 3.
    static void ArmarPalancas(Transform p)
    {
        var g = Grupo("Palancas", p);

        // Marcas de quemado en el piso, debajo de las palancas: la otra pista del orden
        foreach (float z in new[] { 2f, 3.2f, 4.4f })
            Cubo("Quemadura", g.transform, new Vector3(5.3f, 0.005f, z),
                 new Vector3(0.5f, 0.002f, 0.5f), mQuemadura);

        ArmarPalanca(g.transform, "P1", 2f);
        ArmarPalanca(g.transform, "P2", 3.2f);
        ArmarPalanca(g.transform, "P3", 4.4f);
    }

    static void ArmarPalanca(Transform p, string nombre, float z)
    {
        var g = Grupo("Palanca_" + nombre, p);
        g.transform.localPosition = new Vector3(ANCHO - 0.06f, 1.1f, z);
        // Girada -90: su "adelante" (+Z) apunta al Oeste, hacia adentro del cuarto
        g.transform.localEulerAngles = new Vector3(0f, -90f, 0f);

        Cubo("Base", g.transform, Vector3.zero, new Vector3(0.22f, 0.4f, 0.06f), mAceroOscuro, true);
        Cubo("Ranura", g.transform, new Vector3(0f, 0f, 0.035f), new Vector3(0.05f, 0.3f, 0.01f), mNegro);

        // El brazo cuelga desde el eje de arriba: cuando se arme el paso 3 va a girar acá
        var brazo = Grupo("Brazo", g.transform);
        brazo.transform.localPosition = new Vector3(0f, 0.14f, 0.05f);
        Cilindro("Barra", brazo.transform, new Vector3(0f, -0.11f, 0.02f),
                 new Vector3(0.028f, 0.11f, 0.028f), mAcero);
        var perilla = Esfera("Perilla", brazo.transform, new Vector3(0f, -0.23f, 0.02f),
                             new Vector3(0.07f, 0.07f, 0.07f), mRojo, true);

        Texto("Etiqueta", g.transform, new Vector3(0f, -0.25f, 0.04f), Vector3.zero,
              nombre, 0.2f, new Color(0.95f, 0.95f, 0.9f), 0.2f, 0.08f);

        brazo.AddComponent<XRSimpleInteractable>();
        Resaltar(brazo, perilla.GetComponent<Renderer>());
    }

    // ------------------------------------------------------------------ ranura del medallón

    // Ranura al lado de la puerta: acá va el medallón que el jugador trae del Cuarto 2.
    // El socket ya está puesto; falta que el paso 3 lo habilite y que abra la puerta.
    static void ArmarRanura(Transform raiz)
    {
        var g = Grupo("Ranura_Medallon", raiz);
        g.transform.localPosition = new Vector3(3.95f, 1.1f, FONDO - 0.07f);
        // Girada 180: lo que se tiene que ver va con z positivo
        g.transform.localEulerAngles = new Vector3(0f, 180f, 0f);

        Cubo("Placa", g.transform, Vector3.zero, new Vector3(0.24f, 0.34f, 0.04f), mAcero, true);
        Cubo("Hueco", g.transform, new Vector3(0f, 0.05f, 0.025f), new Vector3(0.1f, 0.11f, 0.02f), mNegro);
        Texto("Etiqueta", g.transform, new Vector3(0f, -0.11f, 0.024f), Vector3.zero,
              "LLAVE\nMAESTRA", 0.14f, new Color(0.9f, 0.93f, 0.9f), 0.22f, 0.1f);

        var hueco = Grupo("Socket", g.transform);
        hueco.transform.localPosition = new Vector3(0f, 0.05f, 0.06f);
        var colision = hueco.AddComponent<SphereCollider>();
        colision.isTrigger = true;
        colision.radius = 0.08f;
        hueco.AddComponent<XRSocketInteractor>();

        var luz = Cilindro("Luz_Lista", g.transform, new Vector3(0f, 0.15f, 0.024f),
                           new Vector3(0.03f, 0.006f, 0.03f), mVerdeLuz);
        luz.transform.localEulerAngles = new Vector3(90f, 0f, 0f);
        luz.SetActive(false);
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
        // Estantería de acero contra la pared de la entrada
        Modelo("steel_frame_shelves_01", p, new Vector3(4.3f, 0f, 0.45f), 0f, 1.7f, true);

        // Cosas del laboratorio apoyadas en el piso, junto a la estantería
        Modelo("medical_box", p, new Vector3(5.2f, 0f, 0.45f), 15f, 0.26f, true);
        Modelo("propane_tank", p, new Vector3(4.6f, 0f, 4.1f), 0f, 0.62f, true);
        Modelo("metal_trash_can", p, new Vector3(0.4f, 0f, 6.4f), 0f, 0.42f, true);
        Modelo("metal_stool_02", p, new Vector3(3.2f, 0f, 1.4f), 200f, 0.62f, true);

        // Matafuegos colgado de la pared Este
        Modelo("korean_fire_extinguisher_01", p, new Vector3(5.85f, 0.85f, 1.1f), -90f, 0.42f, false, false);

        // Otro mechero en la mesa de trabajo, para que se vea que la línea alimenta a los dos
        ArmarMechero(p, new Vector3(1.25f, 0.79f, 5.25f));
    }

    // ------------------------------------------------------------------ luz de emergencia

    // Lo que se ve mientras no hay corriente: las luces verdosas de emergencia en las
    // esquinas del techo, dos de ellas parpadeando.
    static List<GameObject> ArmarEmergencia(Transform p)
    {
        var sinEnergia = new List<GameObject>();

        float[,] esquinas = { { 0.5f, 0.5f }, { ANCHO - 0.5f, 0.5f }, { 0.5f, FONDO - 0.5f }, { ANCHO - 0.5f, FONDO - 0.5f } };
        for (int i = 0; i < 4; i++)
        {
            var g = Grupo("Emergencia_" + (i + 1), p);
            g.transform.localPosition = new Vector3(esquinas[i, 0], ALTO - 0.18f, esquinas[i, 1]);

            Cubo("Caja", g.transform, Vector3.zero, new Vector3(0.18f, 0.1f, 0.12f), mAceroOscuro);
            Cubo("Foco", g.transform, new Vector3(0f, -0.055f, 0f), new Vector3(0.14f, 0.01f, 0.08f), mVerdeLuz);

            var luz = LuzPunto("Luz", g.transform, new Vector3(0f, -0.15f, 0f),
                               new Color(0.35f, 1f, 0.55f), 1.6f, 6f);
            // Dos de las cuatro parpadean, como un tubo a punto de quemarse
            if (i % 2 == 0) luz.gameObject.AddComponent<Parpadeo>();

            sinEnergia.Add(g);
        }

        return sinEnergia;
    }

    // Zona invisible en el vano de entrada. Cuando el jugador la pisa, el cuarto vuelve
    // a poner su propio clima: la luz ambiental y la niebla son de toda la escena, así
    // que si no, el laboratorio se vería con la luz que dejó prendida el cuarto anterior.
    static void ArmarEntrada(Transform raiz, ControlEnergia control)
    {
        var zona = Grupo("Zona_Entrada", raiz);
        zona.transform.localPosition = new Vector3((ENTRADA_X0 + ENTRADA_X1) / 2f, 1f, 0.5f);

        var colision = zona.AddComponent<BoxCollider>();
        colision.isTrigger = true;
        colision.size = new Vector3(ENTRADA_X1 - ENTRADA_X0 + 0.6f, 2f, 0.6f);

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

        // En este cuarto el clima a oscuras es verdoso, no azul como en la Dirección
        control.ambienteSinEnergia = new Color(0.02f, 0.045f, 0.03f);
        control.ambienteConEnergia = new Color(0.5f, 0.53f, 0.5f);
        control.nieblaSinEnergia = new Color(0.02f, 0.05f, 0.035f);
        control.nieblaConEnergia = new Color(0.3f, 0.32f, 0.3f);
        control.densidadSinEnergia = 0.07f;
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
        mParedAlta = Mat("C4_ParedAlta", new Color(0.83f, 0.89f, 0.82f), 0f, 0.1f,
                         default, "plastered_wall_04", 3f, 1.5f, false, true);
        mFriso = Mat("C4_Azulejo", new Color(0.42f, 0.72f, 0.6f), 0f, 0.55f,
                     default, "square_tiles_02", 6f, 2f, true);
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
        // La piel del cuerpo: gris verdoso, como de formol
        mPiel = Mat("C4_Piel", new Color(0.72f, 0.73f, 0.66f), 0f, 0.12f);
        mPantalla = Mat("C4_Pizarra", new Color(0.09f, 0.16f, 0.13f), 0f, 0.25f);
        mQuemadura = Mat("C4_Quemadura", new Color(0.1f, 0.1f, 0.09f), 0f, 0.05f);

        mVerdeLuz = Mat("C4_VerdeLuz", new Color(0.3f, 0.95f, 0.45f), 0f, 0.5f, new Color(0.2f, 1.1f, 0.35f));
        mRojo = Mat("C4_Rojo", new Color(0.65f, 0.14f, 0.12f), 0.2f, 0.4f);
        mAmbar = Mat("C4_Ambar", new Color(0.95f, 0.72f, 0.15f), 0f, 0.4f);
        mLlama = Mat("C4_Llama", new Color(0.4f, 0.7f, 1f), 0f, 0.6f, new Color(0.3f, 0.8f, 1.6f));
        mLuzTecho = Mat("C4_LuzTecho", new Color(1f, 1f, 0.97f), 0f, 0.5f, new Color(1.5f, 1.6f, 1.5f));
        mResaltado = Mat("C4_Resaltado", new Color(0.55f, 1f, 0.8f), 0f, 0.6f, new Color(0.25f, 0.9f, 0.6f));
        mVidrioLab = MaterialVidrio();

        // Los seis fusibles: los tres primeros son los que sirven
        mFusA = Mat("C4_Fusible10", new Color(0.85f, 0.2f, 0.15f), 0.1f, 0.6f);
        mFusB = Mat("C4_Fusible20", new Color(0.2f, 0.45f, 0.9f), 0.1f, 0.6f);
        mFusC = Mat("C4_Fusible15", new Color(0.25f, 0.75f, 0.35f), 0.1f, 0.6f);
        mFusX = Mat("C4_Fusible5", new Color(0.6f, 0.6f, 0.62f), 0.1f, 0.6f);
        mFusY = Mat("C4_Fusible30", new Color(0.9f, 0.55f, 0.15f), 0.1f, 0.6f);
        mFusZ = Mat("C4_Fusible25", new Color(0.55f, 0.35f, 0.75f), 0.1f, 0.6f);
    }

    // Vidrio de laboratorio: casi transparente
    static Material MaterialVidrio()
    {
        var m = Mat("C4_Vidrio", new Color(0.9f, 1f, 0.95f, 0.12f), 0f, 0.95f);
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
