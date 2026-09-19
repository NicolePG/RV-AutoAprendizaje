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

// Arma el Cuarto 2 (Dirección) completo adentro del objeto "Cuarto2_Oficina",
// con todo ya conectado. Se corre desde el menú: Escape Room > Construir Cuarto 2.
// Borra el contenido anterior del cuarto y lo rehace, así siempre queda igual.
//
// Estilo: oficina moderna (paredes claras, piso de parquet, madera de nogal y acero
// negro). Los muebles y texturas vienen de Poly Haven si están en el proyecto
// (carpeta Assets/PolyHaven); si no están, cada mueble tiene una versión simple.
//
// El acertijo:
// 1) El cuarto entra sin energía, a oscuras y con clima de terror. Hay que subir la
//    llave del tablero eléctrico, junto a la entrada. Ahí se prende todo.
// 2) Los cinco relojes están parados a las 4:40. La bitácora (sobre el aparador) da
//    la hora de cada campana y el orden; la placa del cuadro dice qué reloj es cada
//    campana. Se gira la manecilla de cada reloj hasta su hora y muestra su dígito.
//    El reloj D no figura en ninguna lista: es el señuelo y dispara el susto.
// 3) Los dígitos en el orden de la bitácora dan 3719, que abre la puerta.
public static class ConstructorCuarto2
{
    const float ANCHO = 6f;    // eje X: pared Oeste (0) a pared Este (6)
    const float FONDO = 7f;    // eje Z: entrada (0) al fondo (7)
    const float ALTO = 3.05f;  // altura del techo
    const float MURO = 0.12f;  // espesor de las paredes

    const float ENTRADA_X0 = 0.8f, ENTRADA_X1 = 1.9f;  // hueco de la puerta que viene del Cuarto 1
    const float SALIDA_X0 = 4.4f, SALIDA_X1 = 5.6f;    // hueco de la puerta que va al Cuarto 3
    const float ALTO_PUERTA = 2.1f;

    const float ALTURA_RELOJ = 1.55f;

    static Material mPared, mParedRelojes, mParedPuerta, mMaderaClara, mPantallaPC, mTecho, mPiso, mMadera, mNegro, mBlanco, mMetal, mBronce,
                    mEsfera, mVerde, mRojo, mPantalla, mResaltado, mGrieta, mAlfombra,
                    mPapel, mTela, mLuzTecho, mDigito, mLibroA, mLibroB, mLibroC;

    // Cartel de objetivos: cada paso del acertijo le avisa para que cambie el texto
    static PanelObjetivo panelObjetivo;

    [MenuItem("Escape Room/Construir Cuarto 2")]
    public static void Construir()
    {
        var raiz = GameObject.Find("Cuarto2_Oficina");
        if (raiz == null)
        {
            raiz = new GameObject("Cuarto2_Oficina");
            raiz.transform.position = new Vector3(0f, 0f, 4f);
            Undo.RegisterCreatedObjectUndo(raiz, "Construir Cuarto 2");
        }

        // Se borra lo que haya adentro para reconstruir el cuarto desde cero
        for (int i = raiz.transform.childCount - 1; i >= 0; i--)
            Undo.DestroyObjectImmediate(raiz.transform.GetChild(i).gameObject);

        CrearMateriales();
        ConfigurarNormalesDeModelos();

        var estructura = Grupo("Estructura", raiz.transform);
        var mobiliario = Grupo("Mobiliario", raiz.transform);
        var relojes = Grupo("Relojes", raiz.transform);
        var luces = Grupo("Luces", raiz.transform);

        // Lo que ControlEnergia prende o apaga según haya energía o no
        var lucesCuarto = new List<Light>();
        var conEnergia = new List<GameObject>();

        ArmarEstructura(estructura.transform, lucesCuarto, conEnergia);
        ArmarPanelObjetivo(raiz.transform);
        ArmarEscritorio(mobiliario.transform, lucesCuarto);
        ArmarAparador(mobiliario.transform);
        ArmarEstanteria(mobiliario.transform);
        ArmarSala(mobiliario.transform);

        conEnergia.AddRange(ArmarRelojes(relojes.transform));
        List<GameObject> sinEnergia = ArmarTerror(luces.transform);

        CerraduraLlave cerradura = ArmarPuertaSalida(raiz.transform);
        conEnergia.Add(ArmarTeclado(raiz.transform, cerradura));

        var control = ArmarControlEnergia(raiz.transform, lucesCuarto, conEnergia, sinEnergia);
        ArmarTablero(raiz.transform, control);

        AjustarAmbienteEditor();
        AsegurarInventario();
        ActualizarPuzzleData();

        // Para que Ctrl+Z deshaga la construcción entera
        foreach (Transform hijo in raiz.transform)
            Undo.RegisterCreatedObjectUndo(hijo.gameObject, "Construir Cuarto 2");

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(raiz.scene);
        Selection.activeGameObject = raiz;

        Debug.Log("Cuarto 2 construido. Primero se sube la llave del tablero, despues se ponen " +
                  "en hora los relojes A=7, B=1, C=3 y E=9 (el D es el senuelo). " +
                  "Leidos en el orden de la bitacora (C-A-B-E) dan la clave 3719.");
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
             new Vector3(MURO, ALTO, FONDO), mPared, true);
        Cubo("Pared_Este", paredes.transform, new Vector3(ANCHO + MURO / 2f, ALTO / 2f, FONDO / 2f),
             new Vector3(MURO, ALTO, FONDO), mPared, true);

        // Pared Norte (la de los relojes) partida por el vano de entrada, en verde petróleo
        Muro(paredes.transform, "Pared_Norte_Izq", -MURO, ENTRADA_X0, -MURO / 2f, 0f, ALTO, mParedRelojes);
        Muro(paredes.transform, "Pared_Norte_Der", ENTRADA_X1, ANCHO + MURO, -MURO / 2f, 0f, ALTO, mParedRelojes);
        Muro(paredes.transform, "Dintel_Entrada", ENTRADA_X0, ENTRADA_X1, -MURO / 2f, ALTO_PUERTA, ALTO, mParedRelojes);

        // Pared Sur (la de la puerta de salida) en terracota
        Muro(paredes.transform, "Pared_Sur_Izq", -MURO, SALIDA_X0, FONDO + MURO / 2f, 0f, ALTO, mParedPuerta);
        Muro(paredes.transform, "Pared_Sur_Der", SALIDA_X1, ANCHO + MURO, FONDO + MURO / 2f, 0f, ALTO, mParedPuerta);
        Muro(paredes.transform, "Dintel_Salida", SALIDA_X0, SALIDA_X1, FONDO + MURO / 2f, ALTO_PUERTA, ALTO, mParedPuerta);

        Cubo("Techo", p, new Vector3(ANCHO / 2f, ALTO + MURO / 2f, FONDO / 2f),
             new Vector3(ANCHO + MURO * 2f, MURO, FONDO + MURO * 2f), mTecho, true);

        // Zócalo negro finito, como en las oficinas modernas
        var zocalos = Grupo("Zocalos", p);
        Zocalo(zocalos.transform, "Zocalo_Oeste", 0.008f, FONDO / 2f, 0.016f, FONDO);
        Zocalo(zocalos.transform, "Zocalo_Este", ANCHO - 0.008f, FONDO / 2f, 0.016f, FONDO);
        Zocalo(zocalos.transform, "Zocalo_Norte_Izq", ENTRADA_X0 / 2f, 0.008f, ENTRADA_X0, 0.016f);
        Zocalo(zocalos.transform, "Zocalo_Norte_Der", (ENTRADA_X1 + ANCHO) / 2f, 0.008f, ANCHO - ENTRADA_X1, 0.016f);
        Zocalo(zocalos.transform, "Zocalo_Sur_Izq", SALIDA_X0 / 2f, FONDO - 0.008f, SALIDA_X0, 0.016f);
        Zocalo(zocalos.transform, "Zocalo_Sur_Der", (SALIDA_X1 + ANCHO) / 2f, FONDO - 0.008f, ANCHO - SALIDA_X1, 0.016f);

        // Dos luminarias lineales en el techo: la carcasa se ve siempre, el difusor
        // encendido y la luz solo cuando hay energía
        int n = 1;
        foreach (float z in new[] { 2.3f, 4.9f })
        {
            var lum = Grupo("Luminaria_" + n, p);
            lum.transform.localPosition = new Vector3(ANCHO / 2f, ALTO - 0.03f, z);
            Cubo("Carcasa", lum.transform, Vector3.zero, new Vector3(2.6f, 0.05f, 0.16f), mNegro);

            var difusor = Cubo("Difusor", lum.transform, new Vector3(0f, -0.028f, 0f),
                               new Vector3(2.5f, 0.01f, 0.1f), mLuzTecho);
            difusor.SetActive(false);
            conEnergia.Add(difusor);

            lucesCuarto.Add(LuzPunto("Luz", lum.transform, new Vector3(0f, -0.3f, 0f),
                                     new Color(1f, 0.96f, 0.9f), 3.2f, 10f));
            n++;
        }
    }

    // Tramo de pared entre dos X, a una altura determinada
    static void Muro(Transform p, string nombre, float x0, float x1, float z, float yBase, float yTope,
                     Material material = null)
    {
        float ancho = x1 - x0;
        float alto = yTope - yBase;
        if (ancho <= 0f || alto <= 0f) return;
        Cubo(nombre, p, new Vector3((x0 + x1) / 2f, yBase + alto / 2f, z),
             new Vector3(ancho, alto, MURO), material != null ? material : mPared, true);
    }

    static void Zocalo(Transform p, string nombre, float x, float z, float largoX, float largoZ)
        => Cubo(nombre, p, new Vector3(x, 0.04f, z), new Vector3(largoX, 0.08f, largoZ), mNegro);

    // ------------------------------------------------------------------ panel de objetivos

    // Cartel colgado al lado de la entrada: dice qué hay que hacer ahora y se va
    // actualizando solo. Es lo primero que se ve al entrar al cuarto.
    static void ArmarPanelObjetivo(Transform raiz)
    {
        var g = Grupo("Panel_Objetivo", raiz);
        g.transform.localPosition = new Vector3(0.1f, 1.55f, 1.15f);
        g.transform.localEulerAngles = new Vector3(0f, 90f, 0f);

        Cubo("Marco", g.transform, Vector3.zero, new Vector3(1.1f, 0.7f, 0.04f), mNegro, true);
        Cubo("Tablero", g.transform, new Vector3(0f, 0f, 0.022f), new Vector3(1.04f, 0.64f, 0.01f), mBlanco);
        Texto("Titulo", g.transform, new Vector3(0f, 0.25f, 0.03f), Vector3.zero,
              "DIRECCION", 0.26f, new Color(0.1f, 0.1f, 0.1f), 0.9f, 0.1f);
        Cubo("Linea", g.transform, new Vector3(0f, 0.19f, 0.028f), new Vector3(0.9f, 0.004f, 0.002f), mNegro);

        var texto = Texto("Texto_Objetivo", g.transform, new Vector3(0f, -0.06f, 0.03f), Vector3.zero,
                          "", 0.27f, new Color(0.15f, 0.15f, 0.15f), 0.98f, 0.46f);
        texto.lineSpacing = -12f;

        // Una luz chica para que el cartel se lea aunque el cuarto esté a oscuras
        LuzPunto("Luz_Panel", g.transform, new Vector3(0f, 0.1f, 0.5f), new Color(1f, 0.95f, 0.85f), 1.4f, 1.6f);

        panelObjetivo = g.AddComponent<PanelObjetivo>();
        panelObjetivo.texto = texto;
        panelObjetivo.pasos = new[]
        {
            "SIN ENERGIA\n\nSube la llave del tablero, junto a la puerta.\nEn el escritorio hay una computadora:\nel boton verde muestra que hay que hacer.",
            "VOLVIO LA LUZ\n\nLos cinco relojes quedaron parados\na las 4:40, la hora del apagon.\nBusca la bitacora sobre el aparador.",
            "HAY QUE PONERLOS EN HORA\n\nLa bitacora dice a que hora suena\ncada campana. La placa del cuadro dice\nque reloj es cada una. Gira las manecillas.",
            "BIEN\n\nCada reloj en hora muestra su numero.\nSegui con los demas y marca el codigo\nen el orden que dice la bitacora.",
            "CODIGO ACEPTADO\n\nAhora falta la llave.\nEl cuaderno del estante dice donde esta.\nDespues: encajala en la cerradura y gira la perilla.",
            "PUERTA ABIERTA\n\nLlevate el medallon del cajon del escritorio\ny sali: la puerta se cierra sola."
        };
        texto.text = panelObjetivo.pasos[0];   // así ya se ve en el editor, sin darle Play
        EditorUtility.SetDirty(panelObjetivo);
    }

    // Conecta un evento para que el cartel pase a mostrar un paso determinado
    static void AvisarPanel(UnityEventBase evento, int paso)
    {
        if (panelObjetivo == null) return;
        UnityEventTools.AddIntPersistentListener(evento, new UnityAction<int>(panelObjetivo.MostrarPaso), paso);
    }

    // ------------------------------------------------------------------ tablero eléctrico

    // El primer paso del cuarto: sin esta llave no hay luz, ni relojes, ni teclado.
    static void ArmarTablero(Transform raiz, ControlEnergia control)
    {
        var g = Grupo("Tablero_Electrico", raiz);
        g.transform.localPosition = new Vector3(0.42f, 1.35f, 0.07f);

        Cubo("Caja", g.transform, Vector3.zero, new Vector3(0.34f, 0.44f, 0.1f), mBlanco, true);
        Cubo("Frente", g.transform, new Vector3(0f, 0f, 0.052f), new Vector3(0.29f, 0.39f, 0.01f), mNegro);
        Texto("Titulo", g.transform, new Vector3(0f, 0.15f, 0.06f), Vector3.zero,
              "TABLERO", 0.16f, new Color(0.85f, 0.85f, 0.8f), 0.28f, 0.08f);

        // El piloto rojo es emisivo, así se lo ve aunque el cuarto esté a oscuras
        var pilotoRojo = Esfera("Piloto_Sin_Energia", g.transform, new Vector3(0.09f, 0.04f, 0.055f),
                                new Vector3(0.045f, 0.045f, 0.045f), mRojo);
        var pilotoVerde = Esfera("Piloto_Con_Energia", g.transform, new Vector3(-0.09f, 0.04f, 0.055f),
                                 new Vector3(0.045f, 0.045f, 0.045f), mVerde);
        pilotoVerde.SetActive(false);

        var audio = AudioEn("Audio_Llave", g.transform);

        var llave = Cubo("Llave_General", g.transform, new Vector3(0f, -0.1f, 0.075f),
                         new Vector3(0.08f, 0.15f, 0.06f), mBronce, true);
        llave.AddComponent<XRSimpleInteractable>();
        var boton = llave.AddComponent<PressableButton>();
        boton.parteMovil = llave.transform;
        boton.recorrido = 0.03f;
        Resaltar(llave, llave.GetComponent<Renderer>());

        // Al subir la llave vuelve la energía: de eso se encarga ControlEnergia
        UnityEventTools.AddVoidPersistentListener(boton.alPresionar, new UnityAction(control.Encender));
        UnityEventTools.AddBoolPersistentListener(boton.alPresionar, new UnityAction<bool>(pilotoVerde.SetActive), true);
        UnityEventTools.AddBoolPersistentListener(boton.alPresionar, new UnityAction<bool>(pilotoRojo.SetActive), false);
        UnityEventTools.AddVoidPersistentListener(boton.alPresionar, new UnityAction(audio.Play));
        AvisarPanel(boton.alPresionar, 1);
        EditorUtility.SetDirty(boton);
    }

    // ------------------------------------------------------------------ escritorio

    static void ArmarEscritorio(Transform p, List<Light> lucesCuarto)
    {
        var g = Grupo("Escritorio", p);
        g.transform.localPosition = new Vector3(1.5f, 0f, 2.6f);

        // Tablero de nogal sobre patas de acero negro en forma de U
        Cubo("Tablero", g.transform, new Vector3(0f, 0.74f, 0f), new Vector3(1.8f, 0.04f, 0.85f), mMadera, true);
        foreach (float x in new[] { -0.84f, 0.84f })
        {
            Cubo("Pata_Frente", g.transform, new Vector3(x, 0.36f, -0.37f), new Vector3(0.04f, 0.72f, 0.04f), mNegro);
            Cubo("Pata_Fondo", g.transform, new Vector3(x, 0.36f, 0.37f), new Vector3(0.04f, 0.72f, 0.04f), mNegro);
            Cubo("Travesano", g.transform, new Vector3(x, 0.7f, 0f), new Vector3(0.04f, 0.03f, 0.78f), mNegro);
            Cubo("Pie", g.transform, new Vector3(x, 0.015f, 0f), new Vector3(0.05f, 0.03f, 0.8f), mNegro);
        }

        // Cajonera colgada debajo del tablero, con hueco para que entre el cajón
        var cajonera = Grupo("Cajonera", g.transform);
        cajonera.transform.localPosition = new Vector3(0.5f, 0f, 0f);
        Cubo("Lado_Izq", cajonera.transform, new Vector3(-0.31f, 0.61f, 0f), new Vector3(0.02f, 0.22f, 0.84f), mNegro);
        Cubo("Lado_Der", cajonera.transform, new Vector3(0.31f, 0.61f, 0f), new Vector3(0.02f, 0.22f, 0.84f), mNegro);
        Cubo("Fondo", cajonera.transform, new Vector3(0f, 0.61f, 0.41f), new Vector3(0.62f, 0.22f, 0.02f), mNegro);
        Cubo("Base", cajonera.transform, new Vector3(0f, 0.5f, 0f), new Vector3(0.62f, 0.02f, 0.84f), mNegro);

        // El cajón mira al Norte (hacia donde entra el jugador), por eso va rotado 180
        var cajon = Grupo("Cajon", g.transform);
        cajon.transform.localPosition = new Vector3(0.5f, 0.61f, -0.41f);
        cajon.transform.localEulerAngles = new Vector3(0f, 180f, 0f);

        Cubo("Frente", cajon.transform, Vector3.zero, new Vector3(0.6f, 0.2f, 0.03f), mMadera, true);
        Cubo("Piso_Cajon", cajon.transform, new Vector3(0f, -0.08f, -0.38f), new Vector3(0.54f, 0.02f, 0.72f), mNegro);
        Cubo("Lado_A", cajon.transform, new Vector3(-0.27f, -0.02f, -0.38f), new Vector3(0.02f, 0.13f, 0.72f), mNegro);
        Cubo("Lado_B", cajon.transform, new Vector3(0.27f, -0.02f, -0.38f), new Vector3(0.02f, 0.13f, 0.72f), mNegro);
        Cubo("Tirador", cajon.transform, new Vector3(0f, 0.06f, 0.025f), new Vector3(0.4f, 0.012f, 0.02f), mNegro);

        var inter = cajon.AddComponent<XRSimpleInteractable>();
        var drawer = cajon.AddComponent<Drawer>();
        drawer.aperturaMaxima = 0.45f;
        Resaltar(cajon, cajon.transform.Find("Frente").GetComponent<Renderer>());
        EditorUtility.SetDirty(inter);

        // El medallón que hay que llevarse al Cuarto 4
        var medallon = Cilindro("Medallon", cajon.transform, new Vector3(0f, -0.055f, -0.3f),
                                new Vector3(0.1f, 0.008f, 0.1f), mBronce, true);
        medallon.transform.localEulerAngles = new Vector3(90f, 0f, 0f);
        var grab = medallon.AddComponent<XRGrabInteractable>();
        var rb = medallon.GetComponent<Rigidbody>();
        if (rb != null) { rb.isKinematic = true; rb.useGravity = false; }
        var pick = medallon.AddComponent<PickableItem>();
        pick.datos = BuscarAsset<ItemData>("ItemData_ObjetoEspecial");
        Resaltar(medallon, medallon.GetComponent<Renderer>());
        EditorUtility.SetDirty(grab);
        EditorUtility.SetDirty(pick);

        ArmarComputadora(g.transform);

        // Lo que va arriba del escritorio (de Poly Haven, si está)
        Modelo("desk_lamp_arm_01", g.transform, new Vector3(0.6f, 0.76f, 0.22f), 200f, 0.88f, false);
        Modelo("potted_plant_04", g.transform, new Vector3(-0.78f, 0.76f, 0.28f), 0f, 0.27f, false);

        // Sillón del director, detrás del escritorio
        if (Modelo("mid_century_lounge_chair", p, new Vector3(1.5f, 0f, 3.6f), 180f, 1.17f, true) == null)
            SillaSimple(p, new Vector3(1.5f, 0f, 3.5f));

        // Lámpara colgante sobre el escritorio: su luz es una de las del cuarto
        if (Modelo("modern_ceiling_lamp_01", p, new Vector3(1.5f, ALTO - 0.95f, 2.6f), 0f, 0.95f, false) == null)
        {
            var colgante = Grupo("Lampara_Colgante", p);
            colgante.transform.localPosition = new Vector3(1.5f, 0f, 2.6f);
            Cubo("Cable", colgante.transform, new Vector3(0f, ALTO - 0.45f, 0f), new Vector3(0.01f, 0.9f, 0.01f), mNegro);
            Cilindro("Pantalla", colgante.transform, new Vector3(0f, ALTO - 0.95f, 0f), new Vector3(0.38f, 0.08f, 0.38f), mNegro);
        }
        lucesCuarto.Add(LuzPunto("Luz_Colgante", p, new Vector3(1.5f, ALTO - 1.05f, 2.6f),
                                 new Color(1f, 0.85f, 0.65f), 2.2f, 5f));
    }

    // Computadora del escritorio. El botón verde, más grande que el resto de los
    // botones del cuarto, cambia la pantalla entre el aviso y las instrucciones.
    static void ArmarComputadora(Transform escritorio)
    {
        var pc = Grupo("Computadora", escritorio);
        pc.transform.localPosition = new Vector3(0.28f, 0.768f, 0.16f);
        pc.transform.localEulerAngles = new Vector3(-6f, 0f, 0f);

        Cubo("Base", pc.transform, new Vector3(0f, 0.008f, 0f), new Vector3(0.24f, 0.016f, 0.15f), mNegro, true);
        Cubo("Cuello", pc.transform, new Vector3(0f, 0.09f, 0.01f), new Vector3(0.05f, 0.18f, 0.03f), mNegro);
        Cubo("Marco", pc.transform, new Vector3(0f, 0.3f, 0.005f), new Vector3(0.58f, 0.36f, 0.02f), mNegro, true);
        Cubo("Pantalla", pc.transform, new Vector3(0f, 0.3f, -0.007f), new Vector3(0.54f, 0.32f, 0.004f), mPantallaPC);

        var espera = Texto("Texto_Espera", pc.transform, new Vector3(0f, 0.3f, -0.012f), new Vector3(0f, 180f, 0f),
                           "DIRECCION\n\nPULSA EL BOTON VERDE\npara ver que hay que hacer",
                           0.3f, new Color(0.55f, 0.95f, 1f), 0.52f, 0.3f);
        espera.lineSpacing = -14f;

        var instrucciones = Texto("Texto_Instrucciones", pc.transform, new Vector3(0f, 0.3f, -0.012f),
                                  new Vector3(0f, 180f, 0f),
                                  "LOS RELOJES DEL PASILLO\n\n" +
                                  "Los cinco se pararon a las 4:40, cuando se\n" +
                                  "corto la luz. Hay que volver a ponerlos en hora.\n\n" +
                                  "- La bitacora, sobre el mueble largo, dice a que\n" +
                                  "   hora suena cada campana del colegio.\n" +
                                  "- La placa debajo del cuadro dice que reloj es\n" +
                                  "   cada campana.\n" +
                                  "- Agarra la aguja corta y girala hasta esa hora:\n" +
                                  "   el reloj se enciende y muestra un numero.\n" +
                                  "- Un reloj no figura en la placa. NO lo toques.\n\n" +
                                  "Los numeros, en el orden de la bitacora, son el\n" +
                                  "codigo del teclado de la salida.\n" +
                                  "Despues hace falta la llave: el cuaderno del\n" +
                                  "estante dice donde esta.",
                                  0.165f, new Color(0.75f, 0.98f, 1f), 0.52f, 0.3f);
        instrucciones.alignment = TextAlignmentOptions.TopLeft;
        instrucciones.lineSpacing = -16f;
        instrucciones.gameObject.SetActive(false);

        // Teclado de la computadora, con sus teclas chiquitas
        var teclado = Grupo("Teclado_PC", escritorio);
        teclado.transform.localPosition = new Vector3(0.28f, 0.772f, -0.12f);
        Cubo("Base", teclado.transform, Vector3.zero, new Vector3(0.44f, 0.015f, 0.18f), mNegro);
        for (int fila = 0; fila < 3; fila++)
            for (int col = 0; col < 12; col++)
                Cubo("Tecla", teclado.transform,
                     new Vector3(-0.19f + col * 0.0345f, 0.011f, -0.05f + fila * 0.032f),
                     new Vector3(0.028f, 0.006f, 0.026f), mMetal);

        // El botón de las pistas: va en el teclado, mucho más grande que las teclas
        var boton = Cilindro("Boton_Pistas", teclado.transform, new Vector3(0.14f, 0.014f, 0.055f),
                             new Vector3(0.08f, 0.012f, 0.08f), mVerde, true);
        Texto("Etiqueta", teclado.transform, new Vector3(-0.04f, 0.012f, 0.06f), new Vector3(90f, 0f, 0f),
              "PISTAS >", 0.09f, new Color(0.8f, 0.85f, 0.85f), 0.2f, 0.03f);

        boton.AddComponent<XRSimpleInteractable>();
        var pulsador = boton.AddComponent<PressableButton>();
        pulsador.parteMovil = boton.transform;
        pulsador.recorrido = 0.008f;
        Resaltar(boton, boton.GetComponent<Renderer>());

        var alternar = pc.AddComponent<AlternarObjetos>();
        alternar.objetos = new[] { espera.gameObject, instrucciones.gameObject };
        UnityEventTools.AddVoidPersistentListener(pulsador.alPresionar, new UnityAction(alternar.Alternar));
        EditorUtility.SetDirty(pulsador);
        EditorUtility.SetDirty(alternar);
    }

    static void SillaSimple(Transform p, Vector3 pos)
    {
        var s = Grupo("Silla", p);
        s.transform.localPosition = pos;
        Cubo("Asiento", s.transform, new Vector3(0f, 0.46f, 0f), new Vector3(0.52f, 0.06f, 0.5f), mTela, true);
        Cubo("Respaldo", s.transform, new Vector3(0f, 0.78f, 0.23f), new Vector3(0.52f, 0.58f, 0.06f), mTela);
        Cubo("Base", s.transform, new Vector3(0f, 0.22f, 0f), new Vector3(0.05f, 0.44f, 0.05f), mNegro);
        Cubo("Pie", s.transform, new Vector3(0f, 0.02f, 0f), new Vector3(0.55f, 0.03f, 0.55f), mNegro);
    }

    // ------------------------------------------------------------------ aparador y cuadro

    // Mueble largo contra la pared Oeste. Arriba de él están las dos pistas:
    // la bitácora apoyada encima y el cuadro con su placa en la pared.
    static void ArmarAparador(Transform p)
    {
        const float Z = 5f;          // centro del mueble a lo largo de la pared
        const float ALTO_MUEBLE = 0.68f;

        // Panel de listones de madera clara, de piso a techo, detrás del aparador:
        // es la pared de acento del cuarto
        var listones = Grupo("Listones", p);
        for (float z = Z - 1.7f; z <= Z + 1.7f; z += 0.075f)
            Cubo("Liston", listones.transform, new Vector3(0.0125f, ALTO / 2f, z),
                 new Vector3(0.025f, ALTO - 0.02f, 0.045f), mMaderaClara);

        if (Modelo("modern_wooden_cabinet", p, new Vector3(0.3f, 0f, Z), 90f, ALTO_MUEBLE, true) == null)
        {
            var a = Grupo("Aparador", p);
            a.transform.localPosition = new Vector3(0.3f, 0f, Z);
            Cubo("Cuerpo", a.transform, new Vector3(0f, 0.38f, 0f), new Vector3(0.5f, 0.6f, 2.4f), mMadera, true);
            foreach (float z in new[] { -1.1f, 1.1f })
                Cubo("Pata", a.transform, new Vector3(0f, 0.04f, z), new Vector3(0.4f, 0.08f, 0.04f), mNegro);
        }

        Modelo("ceramic_vase_01", p, new Vector3(0.3f, ALTO_MUEBLE, Z + 0.95f), 0f, 0.4f, false);
        // La escultura de la ballena: la pieza llamativa del cuarto
        Modelo("bronze_whale_statue", p, new Vector3(0.3f, ALTO_MUEBLE, Z + 0.35f), -110f, 0.42f, false);

        // La bitácora, abierta sobre el aparador y un poco inclinada para leerla
        var bitacora = Grupo("Bitacora", p);
        bitacora.transform.localPosition = new Vector3(0.33f, ALTO_MUEBLE + 0.02f, Z - 0.7f);
        bitacora.transform.localEulerAngles = new Vector3(20f, 90f, 0f);
        Cubo("Tapa", bitacora.transform, Vector3.zero, new Vector3(0.38f, 0.03f, 0.3f), mLibroA, true);
        Cubo("Hojas", bitacora.transform, new Vector3(0f, 0.02f, 0f), new Vector3(0.36f, 0.02f, 0.28f), mPapel);
        var txt = Texto("Texto_Bitacora", bitacora.transform, new Vector3(0f, 0.035f, 0f), new Vector3(-90f, 0f, 0f),
                        "HORARIO DE CAMPANAS\n\n" +
                        "Entrada . . . 7:00\n" +
                        "Recreo  . . . 1:00\n" +
                        "Salida  . . . 3:00\n" +
                        "Cierre  . . . 9:00\n\n" +
                        "Orden: C - A - B - E",
                        0.155f, new Color(0.15f, 0.12f, 0.1f), 0.35f, 0.28f);
        txt.lineSpacing = -12f;

        // Tocar la bitácora cuenta como haberla leído: el cartel pasa al paso siguiente
        var interBitacora = bitacora.AddComponent<XRSimpleInteractable>();
        Resaltar(bitacora, bitacora.transform.Find("Tapa").GetComponent<Renderer>());
        AvisarPanel(interBitacora.selectEntered, 2);
        EditorUtility.SetDirty(interBitacora);

        // El cuadro colgado sobre el aparador, y debajo la placa con la otra pista
        if (Modelo("hanging_picture_frame_01", p, new Vector3(0.06f, 1.3f, Z), 90f, 0.84f, false) == null)
        {
            var c = Grupo("Cuadro", p);
            c.transform.localPosition = new Vector3(0.05f, 1.6f, Z);
            c.transform.localEulerAngles = new Vector3(0f, 90f, 0f);
            Cubo("Marco", c.transform, Vector3.zero, new Vector3(0.8f, 0.52f, 0.03f), mNegro);
            Cubo("Lamina", c.transform, new Vector3(0f, 0f, 0.016f), new Vector3(0.72f, 0.44f, 0.005f), mPapel);
        }

        var placa = Grupo("Placa_Cuadro", p);
        placa.transform.localPosition = new Vector3(0.035f, 1.1f, Z);
        placa.transform.localEulerAngles = new Vector3(0f, 90f, 0f);
        Cubo("Chapa", placa.transform, Vector3.zero, new Vector3(0.66f, 0.2f, 0.01f), mNegro);
        var textoPlaca = Texto("Texto_Placa", placa.transform, new Vector3(0f, 0f, 0.008f), Vector3.zero,
                               "RELOJES DEL PASILLO\n" +
                               "A - Entrada      B - Recreo\n" +
                               "C - Salida        E - Cierre",
                               0.12f, new Color(0.92f, 0.92f, 0.9f), 0.64f, 0.19f);
        textoPlaca.lineSpacing = -14f;
    }

    // ------------------------------------------------------------------ estantería

    static void ArmarEstanteria(Transform p)
    {
        // Estantería de cubos contra la pared Este, con cosas encima y en los huecos.
        // Las alturas de los huecos salen de medir el propio modelo (ver AlturasDeEstantes).
        var mueble = Modelo("wooden_display_shelves_01", p, new Vector3(5.62f, 0f, 1.7f), -90f, 1.556f, true);
        if (mueble != null)
        {
            float alto = 1.556f;
            Modelo("book_encyclopedia_set_01", p, new Vector3(5.62f, alto, 1.45f), -90f, 0.2f, false);
            Modelo("ceramic_vase_03", p, new Vector3(5.62f, alto, 2.05f), 0f, 0.41f, false);
            Modelo("wooden_bowl_01", p, new Vector3(5.62f, alto, 1.78f), 0f, 0.09f, false);
            Modelo("brass_vase_01", p, new Vector3(5.62f, 1.03f, 2.08f), 0f, 0.4f, false);
            Modelo("potted_plant_04", p, new Vector3(5.62f, 1.03f, 1.34f), 0f, 0.27f, false);
            ArmarCuaderno(p, new Vector3(5.6f, 1.575f, 1.68f));
            return;
        }

        // Versión simple: marco de acero negro con estantes de nogal y algunos libros
        var g = Grupo("Estanteria", p);
        g.transform.localPosition = new Vector3(5.75f, 0f, 1.45f);
        foreach (float z in new[] { -0.28f, 0.28f })
            foreach (float x in new[] { -0.2f, 0.2f })
                Cubo("Parante", g.transform, new Vector3(x, 1.05f, z), new Vector3(0.03f, 2.1f, 0.03f), mNegro);

        Material[] colores = { mLibroA, mLibroB, mLibroC };
        float[] alturas = { 0.3f, 0.8f, 1.3f, 1.8f };
        for (int i = 0; i < alturas.Length; i++)
        {
            Cubo("Estante_" + (i + 1), g.transform, new Vector3(0f, alturas[i], 0f), new Vector3(0.44f, 0.03f, 0.6f), mMadera, true);
            if (i == alturas.Length - 1) continue;
            for (int k = 0; k < 5; k++)
            {
                float alto = 0.22f + (k % 3) * 0.03f;
                Cubo("Libro", g.transform, new Vector3(0.02f, alturas[i] + 0.015f + alto / 2f, -0.2f + k * 0.07f),
                     new Vector3(0.22f, alto, 0.05f), colores[(i + k) % colores.Length]);
            }
        }
    }

    // Cuaderno que se agarra del estante: trae el acertijo que lleva hasta la llave
    static void ArmarCuaderno(Transform p, Vector3 pos)
    {
        var g = Grupo("Cuaderno_Acertijo", p);
        g.transform.localPosition = pos;
        g.transform.localEulerAngles = new Vector3(0f, 200f, 0f);

        Cubo("Tapa", g.transform, Vector3.zero, new Vector3(0.24f, 0.025f, 0.32f), mLibroB, true);
        Cubo("Hojas", g.transform, new Vector3(0f, 0.016f, 0f), new Vector3(0.22f, 0.008f, 0.3f), mPapel);
        var texto = Texto("Texto_Acertijo", g.transform, new Vector3(0f, 0.022f, 0f), new Vector3(-90f, 0f, 0f),
                          "ACERTIJO\n\n" +
                          "No tengo llave,\npero guardo una.\n\n" +
                          "Nadie me mira,\ntodos me usan.\n\n" +
                          "Blando por fuera,\nhueco por dentro:\n\n" +
                          "levanta lo que cubre\nmi asiento.",
                          0.1f, new Color(0.15f, 0.12f, 0.1f), 0.2f, 0.29f);
        texto.lineSpacing = -16f;

        var grab = g.AddComponent<XRGrabInteractable>();
        var rb = g.GetComponent<Rigidbody>();
        if (rb != null) { rb.isKinematic = true; rb.useGravity = false; }
        Resaltar(g, g.transform.Find("Tapa").GetComponent<Renderer>());
        EditorUtility.SetDirty(grab);
    }

    // ------------------------------------------------------------------ sala de estar

    // Rincón con sofá y mesa ratona, del lado Este: le da vida al cuarto
    static void ArmarSala(Transform p)
    {
        Cubo("Alfombra", p, new Vector3(4.75f, 0.006f, 4.6f), new Vector3(2.4f, 0.012f, 2.4f), mAlfombra);

        if (Modelo("sofa_02", p, new Vector3(5.5f, 0f, 4.6f), -90f, 0.71f, true) == null)
        {
            var s = Grupo("Sofa", p);
            s.transform.localPosition = new Vector3(5.5f, 0f, 4.6f);
            Cubo("Base", s.transform, new Vector3(0f, 0.22f, 0f), new Vector3(0.85f, 0.4f, 1.8f), mTela, true);
            Cubo("Respaldo", s.transform, new Vector3(0.33f, 0.58f, 0f), new Vector3(0.2f, 0.36f, 1.8f), mTela);
            Cubo("Brazo_1", s.transform, new Vector3(0f, 0.5f, -0.85f), new Vector3(0.85f, 0.18f, 0.12f), mTela);
            Cubo("Brazo_2", s.transform, new Vector3(0f, 0.5f, 0.85f), new Vector3(0.85f, 0.18f, 0.12f), mTela);
        }

        if (Modelo("modern_coffee_table_02", p, new Vector3(4.3f, 0f, 4.6f), 90f, 0.37f, true) == null)
        {
            var m = Grupo("Mesa_Ratona", p);
            m.transform.localPosition = new Vector3(4.3f, 0f, 4.6f);
            Cubo("Tapa", m.transform, new Vector3(0f, 0.37f, 0f), new Vector3(0.6f, 0.03f, 1.2f), mMadera, true);
            foreach (float x in new[] { -0.26f, 0.26f })
                foreach (float z in new[] { -0.55f, 0.55f })
                    Cubo("Pata", m.transform, new Vector3(x, 0.18f, z), new Vector3(0.03f, 0.36f, 0.03f), mNegro);
        }

        Modelo("potted_plant_02", p, new Vector3(5.5f, 0f, 5.95f), 0f, 0.84f, false);

        // La llave de la puerta, escondida debajo del almohadón del sofá
        var llave = Grupo("Llave_Salida", p);
        llave.transform.localPosition = new Vector3(5.45f, 0.42f, 4.55f);
        llave.transform.localEulerAngles = new Vector3(0f, 0f, 90f);
        Cilindro("Cabeza", llave.transform, new Vector3(0f, 0.055f, 0f), new Vector3(0.05f, 0.006f, 0.05f), mBronce, true)
            .transform.localEulerAngles = new Vector3(90f, 0f, 0f);
        Cubo("Vastago", llave.transform, Vector3.zero, new Vector3(0.012f, 0.11f, 0.012f), mBronce);
        Cubo("Diente_1", llave.transform, new Vector3(0.018f, -0.04f, 0f), new Vector3(0.024f, 0.012f, 0.01f), mBronce);
        Cubo("Diente_2", llave.transform, new Vector3(0.016f, -0.062f, 0f), new Vector3(0.02f, 0.012f, 0.01f), mBronce);

        var grabLlave = llave.AddComponent<XRGrabInteractable>();
        var rbLlave = llave.GetComponent<Rigidbody>();
        if (rbLlave != null) { rbLlave.isKinematic = true; rbLlave.useGravity = false; }
        Resaltar(llave, llave.transform.Find("Cabeza").GetComponent<Renderer>());
        EditorUtility.SetDirty(grabLlave);

        // El almohadón que la tapa: hay que agarrarlo y correrlo
        var almohadon = Cubo("Almohadon", p, new Vector3(5.45f, 0.48f, 4.55f),
                             new Vector3(0.46f, 0.12f, 0.46f), mTela, true);
        var grabCojin = almohadon.AddComponent<XRGrabInteractable>();
        var rbCojin = almohadon.GetComponent<Rigidbody>();
        if (rbCojin != null) { rbCojin.isKinematic = true; rbCojin.useGravity = false; }
        Resaltar(almohadon, almohadon.GetComponent<Renderer>());
        EditorUtility.SetDirty(grabCojin);
    }

    // ------------------------------------------------------------------ relojes

    // Devuelve las "caras" de los relojes, que arrancan apagadas hasta que vuelve la energía
    static List<GameObject> ArmarRelojes(Transform p)
    {
        var caras = new List<GameObject>();
        // Los cinco arrancan parados a las 4:40, la hora del apagón, y hay que ponerlos
        // en hora girándoles la manecilla. La bitácora dice a qué hora suena cada
        // campana y el cuadro dice qué reloj es cada campana:
        // A=Entrada 7, B=Recreo 1, C=Salida 3, E=Cierre 9. El D no figura en ninguna
        // de las dos listas: es el señuelo y tocarlo dispara el susto.
        //
        // Las X van de mayor a menor a propósito: los relojes están en la pared Norte
        // y el jugador los mira desde adentro del cuarto, o sea mirando hacia -Z.
        // Desde ahí su derecha es -X, así que el reloj con la X más grande es el que
        // se ve más a la izquierda. Puestos así, se leen A B C D E de izquierda a derecha.
        caras.Add(Reloj(p, "Reloj_A", 4.8f, "A", 7, false));
        caras.Add(Reloj(p, "Reloj_B", 4.2f, "B", 1, false));
        caras.Add(Reloj(p, "Reloj_C", 3.6f, "C", 3, false));
        caras.Add(Reloj(p, "Reloj_D", 3.0f, "D", 0, true));
        caras.Add(Reloj(p, "Reloj_E", 2.4f, "E", 9, false));
        return caras;
    }

    // horaObjetivo es la hora a la que hay que dejarlo (y también el dígito que entrega).
    // En el señuelo va 0: ese no se resuelve, solo dispara el susto.
    static GameObject Reloj(Transform p, string nombre, float x, string letra,
                            int horaObjetivo, bool senuelo)
    {
        const float HORA_APAGON = 4.67f;   // todos arrancan parados a las 4:40
        const float MINUTOS_APAGON = 40f;

        var g = Grupo(nombre, p);
        g.transform.localPosition = new Vector3(x, ALTURA_RELOJ, 0.05f);

        // El cuerpo del reloj se ve siempre, aunque no haya energía. Si está el modelo
        // de Poly Haven se usa ese (trae la esfera con los números); sus agujas se
        // ocultan, porque las que giran son las que se arman más abajo.
        var reloj = Modelo("wall_clock", g.transform, Vector3.zero, 0f, 0.32f, false, false);
        Renderer esferaRenderer = null;
        if (reloj != null)
        {
            foreach (var r in reloj.GetComponentsInChildren<Renderer>())
            {
                if (r.name.ToLower().Contains("hand")) r.gameObject.SetActive(false);
                else if (esferaRenderer == null && !r.name.ToLower().Contains("glass")) esferaRenderer = r;
            }
        }

        // Todo lo que se enciende cuando vuelve la luz
        var cara = Grupo("Cara", g.transform);

        if (reloj == null)
        {
            // Versión simple, sin el modelo: caja negra y esfera con marcas y números
            Cilindro("Caja", g.transform, Vector3.zero, new Vector3(0.34f, 0.035f, 0.34f), mNegro)
                .transform.localEulerAngles = new Vector3(90f, 0f, 0f);
            Cilindro("Esfera_Apagada", g.transform, new Vector3(0f, 0f, 0.034f), new Vector3(0.3f, 0.004f, 0.3f), mNegro)
                .transform.localEulerAngles = new Vector3(90f, 0f, 0f);

            var esfera = Cilindro("Esfera", cara.transform, new Vector3(0f, 0f, 0.036f),
                                  new Vector3(0.3f, 0.004f, 0.3f), mEsfera);
            esfera.transform.localEulerAngles = new Vector3(90f, 0f, 0f);
            esferaRenderer = esfera.GetComponent<Renderer>();

            // Números de las 12 horas
            var numeros = Grupo("Numeros", cara.transform);
            numeros.transform.localPosition = new Vector3(0f, 0f, 0.04f);
            for (int h = 1; h <= 12; h++)
            {
                float ang = h * 30f * Mathf.Deg2Rad;
                // -X es la derecha de quien mira el reloj, ahí van las 3
                Texto("Numero_" + h, numeros.transform,
                      new Vector3(-Mathf.Sin(ang) * 0.108f, Mathf.Cos(ang) * 0.108f, 0f), Vector3.zero,
                      h.ToString(), 0.28f, new Color(0.1f, 0.1f, 0.1f), 0.06f, 0.05f);
            }
        }

        // Los giros van en positivo: el reloj se mira desde su +Z, y desde ahí un giro
        // positivo en Z avanza en el sentido de las agujas (ver RelojManecilla)

        // Manecilla de los minutos: queda clavada en el minuto del apagón
        // Con el modelo las agujas van pegadas a su esfera; sin él, sobre el cilindro
        float zMin = reloj != null ? 0.018f : 0.046f;
        float zHora = reloj != null ? 0.022f : 0.052f;

        var pivMin = Grupo("Manecilla_Minuto", cara.transform);
        pivMin.transform.localPosition = new Vector3(0f, 0f, zMin);
        pivMin.transform.localEulerAngles = new Vector3(0f, 0f, MINUTOS_APAGON * 6f);
        Cubo("Aguja", pivMin.transform, new Vector3(0f, 0.06f, 0f), new Vector3(0.009f, 0.13f, 0.005f), mNegro);

        // Manecilla de la hora: esta es la que el jugador agarra y gira
        var pivHora = Grupo("Manecilla_Hora", cara.transform);
        pivHora.transform.localPosition = new Vector3(0f, 0f, zHora);
        pivHora.transform.localEulerAngles = new Vector3(0f, 0f, HORA_APAGON * 30f);
        var aguja = Cubo("Aguja", pivHora.transform, new Vector3(0f, 0.042f, 0f),
                         new Vector3(0.022f, 0.1f, 0.012f), mNegro, true);

        Esfera("Pin", cara.transform, new Vector3(0f, 0f, zHora + 0.004f), new Vector3(0.022f, 0.022f, 0.022f), mBronce);

        // La letra va en una chapita debajo del reloj, para no taparle los números
        var chapaLetra = Grupo("Letra", g.transform);
        chapaLetra.transform.localPosition = new Vector3(0f, -0.23f, -0.02f);
        Cubo("Chapa", chapaLetra.transform, Vector3.zero, new Vector3(0.1f, 0.08f, 0.01f), mMetal);
        Texto("Texto", chapaLetra.transform, new Vector3(0f, 0f, 0.008f), Vector3.zero,
              letra, 0.5f, new Color(0.1f, 0.1f, 0.1f), 0.1f, 0.08f);

        // Todos los relojes se giran igual: el jugador agarra la manecilla de la hora
        var inter = pivHora.AddComponent<XRSimpleInteractable>();
        var manecilla = pivHora.AddComponent<RelojManecilla>();
        manecilla.horaObjetivo = horaObjetivo;
        Resaltar(pivHora, aguja.GetComponent<Renderer>());
        EditorUtility.SetDirty(inter);
        EditorUtility.SetDirty(manecilla);

        if (senuelo)
        {
            // El reloj que no figura en ninguna lista: al agarrarlo salta el susto
            var luz = LuzApagada("Luz_Susto", cara.transform, new Vector3(0f, 0f, 0.25f),
                                 new Color(1f, 0.25f, 0.2f), 9f, 2.5f, false);
            var decoy = pivHora.AddComponent<RelojDecoy>();
            decoy.vidrio = esferaRenderer;
            decoy.materialResquebrajado = mGrieta;
            decoy.luzSusto = luz.GetComponent<Light>();
            decoy.duracionDestello = 0.8f;
            EditorUtility.SetDirty(decoy);
        }
        else
        {
            // Al quedar en hora se enciende y muestra su dígito
            var chapa = Grupo("Digito", cara.transform);
            chapa.transform.localPosition = new Vector3(0f, -0.26f, 0.02f);
            Cubo("Chapa", chapa.transform, Vector3.zero, new Vector3(0.16f, 0.16f, 0.02f), mDigito);
            Texto("Numero", chapa.transform, new Vector3(0f, 0f, 0.02f), Vector3.zero,
                  horaObjetivo.ToString(), 0.9f, new Color(0.03f, 0.08f, 0.1f), 0.16f, 0.16f);
            chapa.SetActive(false);

            var luzOk = LuzApagada("Luz_EnHora", cara.transform, new Vector3(0f, 0f, 0.22f),
                                   new Color(0.4f, 1f, 0.5f), 2.5f, 0.9f);

            UnityEventTools.AddBoolPersistentListener(manecilla.alPonerEnHora, new UnityAction<bool>(luzOk.SetActive), true);
            UnityEventTools.AddBoolPersistentListener(manecilla.alPonerEnHora, new UnityAction<bool>(chapa.SetActive), true);
            AvisarPanel(manecilla.alPonerEnHora, 3);
        }

        cara.SetActive(false);
        return cara;
    }

    // ------------------------------------------------------------------ teclado

    // Devuelve el grupo que se enciende cuando vuelve la energía
    static GameObject ArmarTeclado(Transform raiz, CerraduraLlave cerradura)
    {
        var g = Grupo("Teclado", raiz);
        g.transform.localPosition = new Vector3(3.85f, 1.2f, FONDO - 0.08f);
        g.transform.localEulerAngles = new Vector3(0f, 180f, 0f);

        Cubo("Marco", g.transform, Vector3.zero, new Vector3(0.38f, 0.56f, 0.06f), mMetal, true);
        Cubo("Panel", g.transform, new Vector3(0f, 0f, 0.032f), new Vector3(0.33f, 0.5f, 0.02f), mNegro);
        Cubo("Fondo_Pantalla", g.transform, new Vector3(0f, 0.18f, 0.044f), new Vector3(0.26f, 0.09f, 0.01f), mPantalla);
        Cubo("Franja", g.transform, new Vector3(0f, 0.115f, 0.044f), new Vector3(0.26f, 0.008f, 0.01f), mVerde);

        // Todo esto arranca apagado: sin energía el teclado no responde
        var activo = Grupo("Activo", g.transform);

        var pantalla = Texto("Pantalla", activo.transform, new Vector3(0f, 0.18f, 0.052f), Vector3.zero,
                             "----", 0.5f, new Color(0.35f, 1f, 0.45f), 0.25f, 0.09f);

        Esfera("Foco_Acierto", activo.transform, new Vector3(-0.1f, 0.28f, 0.04f), new Vector3(0.04f, 0.04f, 0.04f), mVerde);
        Esfera("Foco_Error", activo.transform, new Vector3(0.1f, 0.28f, 0.04f), new Vector3(0.04f, 0.04f, 0.04f), mRojo);

        var luzOk = LuzApagada("Luz_Acierto", activo.transform, new Vector3(-0.1f, 0.28f, 0.12f),
                               new Color(0.35f, 1f, 0.45f), 4f, 1.5f);
        var luzMal = LuzApagada("Luz_Error", activo.transform, new Vector3(0.1f, 0.28f, 0.12f),
                                new Color(1f, 0.3f, 0.25f), 4f, 1.5f);

        var audioOk = AudioEn("Audio_Acierto", activo.transform);
        var audioMal = AudioEn("Audio_Error", activo.transform);

        var keypad = g.AddComponent<KeypadPuzzle>();
        keypad.datos = BuscarAsset<PuzzleData>("PuzzleData_Cuarto2");
        keypad.pantalla = pantalla;

        // Teclas 1-9 en tres columnas y el 0 abajo.
        // Las columnas van de positivo a negativo porque el teclado está rotado 180
        // (mira hacia adentro del cuarto): sin esto las teclas se leerían 3 2 1.
        float[] columnas = { 0.085f, 0f, -0.085f };
        for (int i = 0; i < 10; i++)
        {
            int digito = i < 9 ? i + 1 : 0;
            float px = i < 9 ? columnas[i % 3] : 0f;
            float py = i < 9 ? 0.05f - (i / 3) * 0.08f : -0.19f;

            var tecla = Cubo("Boton_" + digito, activo.transform, new Vector3(px, py, 0.05f),
                             new Vector3(0.068f, 0.068f, 0.028f), mNegro, true);
            Texto("Numero", tecla.transform, new Vector3(0f, 0f, 0.6f), Vector3.zero,
                  digito.ToString(), 6f, new Color(0.92f, 0.92f, 0.9f), 1f, 1f);

            tecla.AddComponent<XRSimpleInteractable>();
            var boton = tecla.AddComponent<PressableButton>();
            boton.parteMovil = tecla.transform;
            boton.recorrido = 0.008f;
            Resaltar(tecla, tecla.GetComponent<Renderer>());

            UnityEventTools.AddIntPersistentListener(boton.alPresionar,
                new UnityAction<int>(keypad.IngresarDigito), digito);
            EditorUtility.SetDirty(boton);
        }

        // Qué pasa al acertar y al errar
        UnityEventTools.AddBoolPersistentListener(keypad.OnSolved, new UnityAction<bool>(luzOk.SetActive), true);
        UnityEventTools.AddBoolPersistentListener(keypad.OnSolved, new UnityAction<bool>(luzMal.SetActive), false);
        UnityEventTools.AddVoidPersistentListener(keypad.OnSolved, new UnityAction(audioOk.Play));
        // El código ya no abre la puerta: habilita la cerradura, que además pide la llave
        if (cerradura != null)
            UnityEventTools.AddVoidPersistentListener(keypad.OnSolved, new UnityAction(cerradura.Habilitar));
        AvisarPanel(keypad.OnSolved, 4);

        UnityEventTools.AddBoolPersistentListener(keypad.alError, new UnityAction<bool>(luzMal.SetActive), true);
        UnityEventTools.AddVoidPersistentListener(keypad.alError, new UnityAction(audioMal.Play));
        EditorUtility.SetDirty(keypad);

        activo.SetActive(false);
        return activo;
    }

    // ------------------------------------------------------------------ puerta

    static CerraduraLlave ArmarPuertaSalida(Transform raiz)
    {
        var g = Grupo("Puerta_Salida", raiz);
        g.transform.localPosition = new Vector3(SALIDA_X0, 0f, FONDO);

        float ancho = SALIDA_X1 - SALIDA_X0;
        Cubo("Jamba_Izq", g.transform, new Vector3(-0.04f, ALTO_PUERTA / 2f, 0f),
             new Vector3(0.08f, ALTO_PUERTA, 0.2f), mNegro, true);
        Cubo("Jamba_Der", g.transform, new Vector3(ancho + 0.04f, ALTO_PUERTA / 2f, 0f),
             new Vector3(0.08f, ALTO_PUERTA, 0.2f), mNegro, true);
        Cubo("Dintel", g.transform, new Vector3(ancho / 2f, ALTO_PUERTA + 0.04f, 0f),
             new Vector3(ancho + 0.16f, 0.08f, 0.2f), mNegro, true);

        Cubo("Cartel", g.transform, new Vector3(ancho / 2f, ALTO_PUERTA + 0.26f, -0.1f),
             new Vector3(0.5f, 0.16f, 0.03f), mNegro);
        Texto("Texto_Cartel", g.transform, new Vector3(ancho / 2f, ALTO_PUERTA + 0.26f, -0.12f),
              new Vector3(0f, 180f, 0f), "SALIDA", 0.14f, new Color(0.4f, 1f, 0.5f), 0.5f, 0.15f);

        var bisagra = Grupo("Bisagra", g.transform);
        var puerta = bisagra.AddComponent<Door>();
        // Ángulo negativo: la hoja gira hacia +Z, o sea hacia afuera del cuarto
        puerta.anguloApertura = -95f;
        puerta.duracion = 1.4f;
        puerta.segundosParaCerrar = 6f;   // se cierra sola después de salir

        // Hoja lisa de nogal con manija de barra larga
        Cubo("Hoja", bisagra.transform, new Vector3(ancho / 2f, ALTO_PUERTA / 2f, 0f),
             new Vector3(ancho, ALTO_PUERTA, 0.05f), mMadera, true);
        Cubo("Manija", bisagra.transform, new Vector3(ancho - 0.12f, 1.05f, -0.07f),
             new Vector3(0.025f, 0.6f, 0.025f), mNegro);
        Cubo("Soporte_1", bisagra.transform, new Vector3(ancho - 0.12f, 1.3f, -0.045f),
             new Vector3(0.02f, 0.02f, 0.04f), mNegro);
        Cubo("Soporte_2", bisagra.transform, new Vector3(ancho - 0.12f, 0.8f, -0.045f),
             new Vector3(0.02f, 0.02f, 0.04f), mNegro);

        // Cerradura: la llave se encaja en la ranura y después se gira la perilla.
        // Solo cede si el teclado ya acepto el codigo.
        var cerradura = Grupo("Cerradura", bisagra.transform);
        cerradura.transform.localPosition = new Vector3(ancho - 0.3f, 1.05f, -0.03f);

        Cubo("Escudo", cerradura.transform, Vector3.zero, new Vector3(0.1f, 0.16f, 0.02f), mMetal, true);
        Cubo("Ojo_Cerradura", cerradura.transform, new Vector3(0f, 0.02f, -0.012f), new Vector3(0.02f, 0.03f, 0.01f), mNegro);

        var ranura = Grupo("Ranura_Llave", cerradura.transform);
        ranura.transform.localPosition = new Vector3(0f, 0.02f, -0.05f);
        var colisionRanura = ranura.AddComponent<SphereCollider>();
        colisionRanura.isTrigger = true;
        colisionRanura.radius = 0.07f;
        var socket = ranura.AddComponent<XRSocketInteractor>();

        var luzLista = LuzPunto("Luz_Lista", cerradura.transform, new Vector3(0f, 0.08f, -0.06f),
                                new Color(0.4f, 1f, 0.5f), 1.2f, 0.5f);
        luzLista.enabled = false;

        var perilla = Cilindro("Perilla", cerradura.transform, new Vector3(0f, -0.05f, -0.035f),
                               new Vector3(0.05f, 0.012f, 0.05f), mBronce, true);
        perilla.transform.localEulerAngles = new Vector3(90f, 0f, 0f);
        Texto("Etiqueta_Girar", cerradura.transform, new Vector3(0f, -0.1f, -0.012f), Vector3.zero,
              "GIRAR", 0.07f, new Color(0.85f, 0.85f, 0.85f), 0.12f, 0.03f);

        var cerraduraLlave = cerradura.AddComponent<CerraduraLlave>();
        cerraduraLlave.ranura = socket;
        cerraduraLlave.luzLista = luzLista;

        perilla.AddComponent<XRSimpleInteractable>();
        var botonGirar = perilla.AddComponent<PressableButton>();
        botonGirar.parteMovil = perilla.transform;
        botonGirar.recorrido = 0.004f;
        Resaltar(perilla, perilla.GetComponent<Renderer>());
        UnityEventTools.AddVoidPersistentListener(botonGirar.alPresionar, new UnityAction(cerraduraLlave.Girar));

        // Al ceder la cerradura, la puerta se abre
        UnityEventTools.AddVoidPersistentListener(cerraduraLlave.alAbrir, new UnityAction(puerta.Abrir));
        AvisarPanel(cerraduraLlave.alAbrir, 5);

        EditorUtility.SetDirty(puerta);
        EditorUtility.SetDirty(botonGirar);
        EditorUtility.SetDirty(cerraduraLlave);
        return cerraduraLlave;
    }

    // ------------------------------------------------------------------ luces y terror

    // Lo único que ilumina mientras no hay energía: luces rojas de emergencia en las
    // cuatro esquinas del techo, dos de ellas parpadeando. Al subir la llave del
    // tablero se apagan todas (ControlEnergia las desactiva) y el cuarto se ve normal.
    static List<GameObject> ArmarTerror(Transform p)
    {
        var rojas = new List<GameObject>();
        float[] xs = { 0.35f, ANCHO - 0.35f };
        float[] zs = { 0.35f, FONDO - 0.35f };
        int n = 1;
        foreach (float x in xs)
            foreach (float z in zs)
            {
                var esquina = Grupo("Luz_Roja_" + n, p);
                esquina.transform.localPosition = new Vector3(x, ALTO - 0.05f, z);
                Cubo("Foco", esquina.transform, Vector3.zero, new Vector3(0.18f, 0.06f, 0.18f), mRojo);

                var luz = LuzPunto("Luz", esquina.transform, new Vector3(0f, -0.25f, 0f),
                                   new Color(1f, 0.07f, 0.05f), 3f, 5.5f);

                // Parpadean las de una diagonal, así no queda todo sincronizado
                if (n == 1 || n == 4)
                {
                    var parpadeo = luz.gameObject.AddComponent<Parpadeo>();
                    parpadeo.intensidadMinima = 0.3f;
                    parpadeo.intensidadMaxima = 3f;
                }

                rojas.Add(esquina);
                n++;
            }
        return rojas;
    }

    // ------------------------------------------------------------------ control de energía

    static ControlEnergia ArmarControlEnergia(Transform raiz, List<Light> lucesCuarto,
                                              List<GameObject> conEnergia, List<GameObject> sinEnergia)
    {
        var g = Grupo("Energia", raiz);
        var control = g.AddComponent<ControlEnergia>();
        control.lucesDelCuarto = lucesCuarto.ToArray();
        control.objetosConEnergia = conEnergia.ToArray();
        control.objetosSinEnergia = sinEnergia.ToArray();
        // Los parpadeos solo corren mientras el cuarto está sin energía
        control.efectosSinEnergia = raiz.GetComponentsInChildren<Parpadeo>();
        EditorUtility.SetDirty(control);
        return control;
    }

    // En el editor el ambiente queda como con luz, para poder trabajar cómodo.
    // El apagón lo arma ControlEnergia recién al darle Play.
    static void AjustarAmbienteEditor()
    {
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.55f, 0.53f, 0.5f);
        RenderSettings.fog = false;
    }

    // ------------------------------------------------------------------ apoyo

    static void AsegurarInventario()
    {
        if (Object.FindAnyObjectByType<Inventory>() != null) return;
        var go = new GameObject("Inventory");
        go.AddComponent<Inventory>();
        Undo.RegisterCreatedObjectUndo(go, "Construir Cuarto 2");
    }

    static void ActualizarPuzzleData()
    {
        var datos = BuscarAsset<PuzzleData>("PuzzleData_Cuarto2");
        if (datos == null)
        {
            Debug.LogWarning("No se encontro PuzzleData_Cuarto2: el teclado va a quedar sin datos.");
            return;
        }

        datos.solucion = "3719";
        datos.pista = "Primero hay que devolver la energia desde el tablero. La bitacora sobre el " +
                      "aparador da la hora de cada campana y el orden; la placa del cuadro dice que " +
                      "reloj es cada campana. Cada reloj se pone en hora girando su manecilla.";
        datos.mensajeAcierto = "La cerradura del teclado hace clic y la puerta se abre";
        datos.mensajeError = "El teclado parpadea en rojo: codigo incorrecto";
        EditorUtility.SetDirty(datos);
    }

    // Pone un modelo de Poly Haven si está descargado en el proyecto y devuelve el
    // objeto que lo contiene; si no está, devuelve null y el que llama arma otra cosa.
    // El modelo se escala a su altura real y se apoya en el piso del punto indicado,
    // así no importa con qué escala o pivote venga el archivo.
    // "apoyar": true deja la base del modelo a la altura del punto (muebles apoyados en
    // el piso o sobre un mueble); false lo centra en el punto (cosas colgadas, como el reloj).
    static GameObject Modelo(string nombrePolyHaven, Transform padre, Vector3 pos, float giroY,
                             float altoReal, bool colisiona, bool apoyar = true)
    {
        var fuente = BuscarModelo(nombrePolyHaven);
        if (fuente == null) return null;

        // El giro va en un objeto contenedor: así no se pisa la rotación que trae el
        // archivo (los FBX que vienen de Blender suelen traer una corrección en X)
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

        // Centrado sobre el punto, apoyado o centrado también en altura
        Vector3 destino = contenedor.transform.position;
        modelo.transform.position += destino - new Vector3(b.center.x, apoyar ? b.min.y : b.center.y, b.center.z);

        if (colisiona)
        {
            Limites(modelo, out b);
            var col = contenedor.AddComponent<BoxCollider>();
            col.center = contenedor.transform.InverseTransformPoint(b.center);
            Vector3 t = b.size;
            // Con el contenedor girado 90 o 270 grados, el ancho y el fondo se intercambian
            if (Mathf.Abs(Mathf.Sin(giroY * Mathf.Deg2Rad)) > 0.7f) t = new Vector3(t.z, t.y, t.x);
            col.size = t;
        }

        return contenedor;
    }

    // Dos cosas que Unity no importa bien de los FBX de Poly Haven:
    // - los vidrios ("_glass") salen opacos y tapan lo que hay detrás (la lámina del cuadro)
    // - el follaje trae la transparencia en una textura aparte ("_alpha"), y sin ella las
    //   hojas se ven como rectángulos blancos
    // Se reemplazan esos materiales por unos armados acá.
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
                    mats[i] = MaterialVidrio();
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

    // Vidrio casi invisible, para que se vea lo que hay detrás
    static Material MaterialVidrio()
    {
        var m = Mat("C2_Vidrio", new Color(1f, 1f, 1f, 0.08f), 0f, 0.95f);
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

    // Material con recorte por transparencia (para hojas): junta el color y la
    // transparencia en una sola textura, que es como la usa Unity
    static Material MaterialRecortado(string nombre, Texture2D diff, Texture2D alfa, Texture2D normal)
    {
        const string CARPETA = "Assets/Materials/Cuarto2/PolyHaven";
        if (!AssetDatabase.IsValidFolder(CARPETA))
            AssetDatabase.CreateFolder("Assets/Materials/Cuarto2", "PolyHaven");

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
        m.SetFloat("_Cull", 0f);   // las hojas se ven de los dos lados
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

    // Carpetas donde se buscan los assets descargados de Poly Haven
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

    // Los normal maps que vienen con los modelos también tienen que estar marcados
    // como "Normal map", si no los muebles se ven con el relieve mal
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

    // Unity necesita que los normal maps estén importados como "Normal map",
    // si no se ven planos o con los relieves invertidos
    static void ConfigurarComoNormal(Texture2D textura)
    {
        string ruta = AssetDatabase.GetAssetPath(textura);
        var importer = AssetImporter.GetAtPath(ruta) as TextureImporter;
        if (importer == null || importer.textureType == TextureImporterType.NormalMap) return;

        importer.textureType = TextureImporterType.NormalMap;
        importer.SaveAndReimport();
    }

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
        // "rot" se piensa como si el texto se leyera desde su +Z, pero TextMeshPro se
        // lee desde su -Z: sin este giro todos los textos se verían en espejo
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

    // Luz que arranca apagada. "porObjeto" define cómo se la prende después:
    // true  -> se prende activando el GameObject (así la prenden los eventos)
    // false -> se prende habilitando el componente Light (así la prende RelojDecoy)
    static GameObject LuzApagada(string nombre, Transform padre, Vector3 pos,
                                 Color color, float intensidad, float rango, bool porObjeto = true)
    {
        var l = LuzPunto(nombre, padre, pos, color, intensidad, rango);
        if (porObjeto) l.gameObject.SetActive(false);
        else l.enabled = false;
        return l.gameObject;
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

    static T BuscarAsset<T>(string nombre) where T : Object
    {
        var guids = AssetDatabase.FindAssets(nombre + " t:" + typeof(T).Name);
        if (guids.Length == 0) return null;
        return AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guids[0]));
    }

    // ------------------------------------------------------------------ materiales

    static void CrearMateriales()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Materials"))
            AssetDatabase.CreateFolder("Assets", "Materials");
        if (!AssetDatabase.IsValidFolder("Assets/Materials/Cuarto2"))
            AssetDatabase.CreateFolder("Assets/Materials", "Cuarto2");

        // Paleta moderna: paredes claras, parquet, nogal y acero negro.
        // Los nombres sueltos ("herringbone_parquet", etc.) son texturas de Poly Haven:
        // si están en el proyecto se usan, y si no queda el color plano.
        // Paredes pintadas de un crema claro. Del yeso de Poly Haven se usa solo el
        // relieve: su color es un gris medio que oscurecería la pintura.
        mPared = Mat("C2_Pared", new Color(0.96f, 0.92f, 0.85f), 0f, 0.1f,
                     default, "plastered_wall_04", 3f, 1.5f, false, true);
        // Dos paredes con color: la de los relojes y la de la puerta de salida
        mParedRelojes = Mat("C2_ParedRelojes", new Color(0.16f, 0.33f, 0.35f), 0f, 0.1f,
                            default, "plastered_wall_04", 3f, 1.5f, false, true);
        mParedPuerta = Mat("C2_ParedPuerta", new Color(0.62f, 0.33f, 0.24f), 0f, 0.1f,
                           default, "plastered_wall_04", 3f, 1.5f, false, true);
        // Madera clara para los listones de la pared del aparador
        mMaderaClara = Mat("C2_MaderaClara", new Color(1f, 0.82f, 0.62f), 0f, 0.3f,
                           default, "american_walnut_veneer", 0.3f, 2f, true);
        // Pantalla de la computadora: encendida, se lee aunque el cuarto esté oscuro
        mPantallaPC = Mat("C2_PantallaPC", new Color(0.04f, 0.09f, 0.13f), 0f, 0.7f,
                          new Color(0.05f, 0.16f, 0.22f));
        mTecho = Mat("C2_Techo", new Color(0.94f, 0.94f, 0.93f), 0f, 0.05f);
        mPiso = Mat("C2_Piso", new Color(0.55f, 0.4f, 0.26f), 0f, 0.35f,
                    default, "herringbone_parquet", 4f, 4.7f);
        // El "nogal" de Poly Haven es una madera grisácea: se tiñe para darle el tono cálido
        mMadera = Mat("C2_Madera", new Color(0.8f, 0.56f, 0.38f), 0f, 0.35f,
                      default, "american_walnut_veneer", 1f, 1f, true);
        mNegro = Mat("C2_Negro", new Color(0.05f, 0.05f, 0.055f), 0.3f, 0.35f);
        mBlanco = Mat("C2_Blanco", new Color(0.93f, 0.93f, 0.92f), 0f, 0.3f);
        mMetal = Mat("C2_Metal", new Color(0.52f, 0.54f, 0.56f), 0.85f, 0.55f);
        mBronce = Mat("C2_Bronce", new Color(0.72f, 0.52f, 0.22f), 0.9f, 0.6f);
        mEsfera = Mat("C2_EsferaReloj", new Color(0.95f, 0.94f, 0.9f), 0f, 0.4f);
        mVerde = Mat("C2_Verde", new Color(0.2f, 0.7f, 0.3f), 0f, 0.4f, new Color(0.15f, 0.8f, 0.3f));
        mRojo = Mat("C2_Rojo", new Color(0.7f, 0.15f, 0.12f), 0f, 0.4f, new Color(0.9f, 0.1f, 0.08f));
        mPantalla = Mat("C2_Pantalla", new Color(0.05f, 0.12f, 0.06f), 0f, 0.6f);
        mResaltado = Mat("C2_Resaltado", new Color(0.55f, 0.85f, 1f), 0f, 0.6f, new Color(0.25f, 0.6f, 0.9f));
        mGrieta = Mat("C2_VidrioRoto", new Color(0.5f, 0.5f, 0.52f), 0.2f, 0.75f);
        mAlfombra = Mat("C2_Alfombra", new Color(0.2f, 0.2f, 0.22f), 0f, 0.05f);
        mPapel = Mat("C2_Papel", new Color(0.92f, 0.9f, 0.84f), 0f, 0.1f);
        mTela = Mat("C2_Tela", new Color(0.24f, 0.25f, 0.27f), 0f, 0.1f);
        mLuzTecho = Mat("C2_LuzTecho", new Color(1f, 1f, 0.97f), 0f, 0.5f, new Color(1.6f, 1.55f, 1.45f));
        mDigito = Mat("C2_Digito", new Color(0.1f, 0.1f, 0.12f), 0f, 0.7f, new Color(0.15f, 0.9f, 1.1f));
        mLibroA = Mat("C2_LibroA", new Color(0.35f, 0.14f, 0.15f), 0f, 0.15f);
        mLibroB = Mat("C2_LibroB", new Color(0.14f, 0.25f, 0.33f), 0f, 0.15f);
        mLibroC = Mat("C2_LibroC", new Color(0.24f, 0.28f, 0.16f), 0f, 0.15f);
    }

    // "polyHaven" es el nombre del asset en Poly Haven (por ejemplo "herringbone_parquet").
    // Si sus texturas están en el proyecto se le ponen al material; si no, queda el color plano.
    // "tenir": si es true, la textura se tiñe con el color; si es false se ve tal cual.
    // "soloRelieve": usa solo el normal map (el relieve) y deja el color liso, como una
    // pared pintada: sirve cuando la textura es más oscura que el color que se quiere.
    static Material Mat(string nombre, Color color, float metalico, float suavidad, Color emision = default,
                        string polyHaven = null, float tileX = 1f, float tileY = 1f,
                        bool tenir = false, bool soloRelieve = false)
    {
        string ruta = "Assets/Materials/Cuarto2/" + nombre + ".mat";
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
                // Con textura, el color la tiñe: si no hay que teñir se pone blanco
                // para que la textura se vea tal cual
                m.SetTexture("_BaseMap", baseMap);
                m.SetTextureScale("_BaseMap", tiling);
                if (!tenir && m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", Color.white);
            }

            // nor_gl es la versión OpenGL del normal map, que es la que usa Unity
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
