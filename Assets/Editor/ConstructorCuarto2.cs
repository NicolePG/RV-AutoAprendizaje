using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;

// Arma el Cuarto 2 (Dirección) completo adentro del objeto "Cuarto2_Oficina":
// estructura, muebles, tablero eléctrico, los 5 relojes, el teclado y la puerta,
// todo ya conectado entre sí. Se corre desde el menú: Escape Room > Construir Cuarto 2.
// Borra el contenido anterior del cuarto y lo rehace, así siempre queda igual.
//
// El acertijo que arma tiene tres pasos:
// 1) El cuarto entra sin energía: los relojes están muertos y el teclado apagado.
//    Hay que encontrar el tablero eléctrico y subir la llave.
// 2) Con luz, cada reloj que se toca muestra su dígito. La bitácora del estante dice
//    en qué orden leerlos (C, A, B, E) y el reloj que NO está en esa lista (el D)
//    es el que dispara el susto.
// 3) El reloj E está parado: hay que girarle la manecilla con la mano hasta la hora
//    que dice la placa del cuadro (las 9). Recién ahí muestra su dígito.
//    Los cuatro dígitos en ese orden dan 3719, que es lo que abre la puerta.
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
    const int HORA_RELOJ_E = 9;  // la hora que hay que marcarle al reloj parado

    static Material mMadera, mMaderaOsc, mPared, mZocalo, mPiso, mBaldosaA, mBaldosaB, mMetal,
                    mBronce, mEsfera, mNegro, mVerde, mRojo, mPantalla, mResaltado,
                    mGrieta, mAlfombra, mPapel, mLibroA, mLibroB, mLibroC, mLampara,
                    mLedFuerte, mLedSuave, mDigito;

    // Si están las texturas de Poly Haven en Assets/Textures/Cuarto2, el piso usa
    // la textura ajedrezada; si no, se arma el ajedrez con baldosas de colores.
    static bool hayTexturaPiso;

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

        var estructura = Grupo("Estructura", raiz.transform);
        var mobiliario = Grupo("Mobiliario", raiz.transform);
        var relojes = Grupo("Relojes", raiz.transform);
        var luces = Grupo("Luces", raiz.transform);

        ArmarEstructura(estructura.transform);
        ArmarPanelObjetivo(raiz.transform);
        ArmarEscritorio(mobiliario.transform);
        ArmarEstanteria(mobiliario.transform);
        ArmarCuadro(mobiliario.transform);
        GameObject luzLampara = ArmarLampara(mobiliario.transform);
        ArmarDecoracion(mobiliario.transform);

        List<GameObject> caras = ArmarRelojes(relojes.transform);
        GameObject luzSala = ArmarLuces(luces.transform);
        GameObject leds = ArmarLedsTecho(luces.transform);

        Door puerta = ArmarPuertaSalida(raiz.transform);
        GameObject tecladoActivo = ArmarTeclado(raiz.transform, puerta);

        var control = ArmarControlEnergia(raiz.transform, leds, luzSala, luzLampara, caras, tecladoActivo);
        ArmarTablero(raiz.transform, control);

        OscurecerEscena();
        AsegurarInventario();
        ActualizarPuzzleData();

        // Para que Ctrl+Z deshaga la construcción entera
        foreach (Transform hijo in raiz.transform)
            Undo.RegisterCreatedObjectUndo(hijo.gameObject, "Construir Cuarto 2");

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(raiz.scene);
        Selection.activeGameObject = raiz;

        Debug.Log("Cuarto 2 construido. Primero hay que subir la llave del tablero electrico. " +
                  "Clave del teclado: 3719 (reloj C=3, A=7, B=1, y E=9 despues de ponerlo en hora).");
    }

    // ------------------------------------------------------------------ estructura

    static void ArmarEstructura(Transform p)
    {
        // La losa del piso es la que hace de zona de teletransporte
        var losa = Cubo("Piso", p, new Vector3(ANCHO / 2f, -0.1f, FONDO / 2f),
                        new Vector3(ANCHO, 0.2f, FONDO), hayTexturaPiso ? mPiso : mBaldosaB, true);
        var area = losa.AddComponent<TeleportationArea>();
        int capaTeleport = InteractionLayerMask.GetMask("Teleport");
        if (capaTeleport == 0) capaTeleport = 1 << 31;
        area.interactionLayers = capaTeleport;

        // Sin la textura ajedrezada de Poly Haven, el ajedrez se arma con baldosas sueltas.
        // Van sin collider para no tapar el teletransporte del piso.
        if (!hayTexturaPiso)
        {
            var baldosas = Grupo("Baldosas", p);
            for (int x = 0; x < (int)ANCHO; x++)
                for (int z = 0; z < (int)FONDO; z++)
                    Cubo($"Baldosa_{x}_{z}", baldosas.transform,
                         new Vector3(x + 0.5f, 0.005f, z + 0.5f),
                         new Vector3(0.96f, 0.02f, 0.96f),
                         (x + z) % 2 == 0 ? mBaldosaA : mBaldosaB);
        }

        var paredes = Grupo("Paredes", p);
        Cubo("Pared_Oeste", paredes.transform, new Vector3(-MURO / 2f, ALTO / 2f, FONDO / 2f),
             new Vector3(MURO, ALTO, FONDO), mPared, true);
        Cubo("Pared_Este", paredes.transform, new Vector3(ANCHO + MURO / 2f, ALTO / 2f, FONDO / 2f),
             new Vector3(MURO, ALTO, FONDO), mPared, true);

        // Pared Norte partida por el vano de entrada
        Muro(paredes.transform, "Pared_Norte_Izq", -MURO, ENTRADA_X0, -MURO / 2f, 0f, ALTO);
        Muro(paredes.transform, "Pared_Norte_Der", ENTRADA_X1, ANCHO + MURO, -MURO / 2f, 0f, ALTO);
        Muro(paredes.transform, "Dintel_Entrada", ENTRADA_X0, ENTRADA_X1, -MURO / 2f, ALTO_PUERTA, ALTO);

        // Pared Sur partida por el vano de salida
        Muro(paredes.transform, "Pared_Sur_Izq", -MURO, SALIDA_X0, FONDO + MURO / 2f, 0f, ALTO);
        Muro(paredes.transform, "Pared_Sur_Der", SALIDA_X1, ANCHO + MURO, FONDO + MURO / 2f, 0f, ALTO);
        Muro(paredes.transform, "Dintel_Salida", SALIDA_X0, SALIDA_X1, FONDO + MURO / 2f, ALTO_PUERTA, ALTO);

        Cubo("Techo", p, new Vector3(ANCHO / 2f, ALTO + MURO / 2f, FONDO / 2f),
             new Vector3(ANCHO + MURO * 2f, MURO, FONDO + MURO * 2f), mPared, true);

        // Plafón del techo: de acá sale la luz de sala cuando vuelve la energía.
        // Si está el modelo de Poly Haven se usa ese en lugar de los cilindros.
        if (Modelo("mounted_fluorescent_lights", p, new Vector3(ANCHO / 2f, ALTO - 0.05f, FONDO / 2f), 0f, 1f) == null)
        {
            var plafon = Grupo("Plafon", p);
            plafon.transform.localPosition = new Vector3(ANCHO / 2f, ALTO - 0.06f, FONDO / 2f);
            Cilindro("Aro", plafon.transform, Vector3.zero, new Vector3(0.5f, 0.03f, 0.5f), mMetal);
            Cilindro("Vidrio", plafon.transform, new Vector3(0f, -0.04f, 0f), new Vector3(0.42f, 0.02f, 0.42f), mEsfera);
        }

        // Friso de madera en la mitad baja de la pared (con su riel arriba)
        var friso = Grupo("Friso", p);
        Friso(friso.transform, "Friso_Oeste", 0.03f, FONDO / 2f, 0.06f, FONDO);
        Friso(friso.transform, "Friso_Este", ANCHO - 0.03f, FONDO / 2f, 0.06f, FONDO);
        Friso(friso.transform, "Friso_Norte_Izq", ENTRADA_X0 / 2f, 0.03f, ENTRADA_X0, 0.06f);
        Friso(friso.transform, "Friso_Norte_Der", (ENTRADA_X1 + ANCHO) / 2f, 0.03f, ANCHO - ENTRADA_X1, 0.06f);
        Friso(friso.transform, "Friso_Sur_Izq", SALIDA_X0 / 2f, FONDO - 0.03f, SALIDA_X0, 0.06f);

        // Moldura contra el techo, que cierra el ambiente
        var moldura = Grupo("Moldura", p);
        Cubo("Moldura_Oeste", moldura.transform, new Vector3(0.05f, ALTO - 0.07f, FONDO / 2f),
             new Vector3(0.1f, 0.14f, FONDO), mZocalo);
        Cubo("Moldura_Este", moldura.transform, new Vector3(ANCHO - 0.05f, ALTO - 0.07f, FONDO / 2f),
             new Vector3(0.1f, 0.14f, FONDO), mZocalo);
        Cubo("Moldura_Norte", moldura.transform, new Vector3(ANCHO / 2f, ALTO - 0.07f, 0.05f),
             new Vector3(ANCHO, 0.14f, 0.1f), mZocalo);
        Cubo("Moldura_Sur", moldura.transform, new Vector3(ANCHO / 2f, ALTO - 0.07f, FONDO - 0.05f),
             new Vector3(ANCHO, 0.14f, 0.1f), mZocalo);
    }

    // Panel de madera contra la pared, con el riel que lo remata arriba
    static void Friso(Transform p, string nombre, float x, float z, float largoX, float largoZ)
    {
        const float alto = 0.95f;
        Cubo(nombre, p, new Vector3(x, alto / 2f, z), new Vector3(largoX, alto, largoZ), mMadera);
        Cubo(nombre + "_Riel", p, new Vector3(x, alto + 0.03f, z),
             new Vector3(largoX + 0.04f, 0.06f, largoZ + 0.04f), mZocalo);
    }

    // Tramo de pared entre dos X, a una altura determinada
    static void Muro(Transform p, string nombre, float x0, float x1, float z, float yBase, float yTope)
    {
        float ancho = x1 - x0;
        float alto = yTope - yBase;
        if (ancho <= 0f || alto <= 0f) return;
        Cubo(nombre, p, new Vector3((x0 + x1) / 2f, yBase + alto / 2f, z),
             new Vector3(ancho, alto, MURO), mPared, true);
    }

    // ------------------------------------------------------------------ tiras LED

    // Tiras moradas por todo el borde del techo y por las esquinas verticales.
    // Son lo único que ilumina mientras el cuarto está sin energía.
    static GameObject ArmarLedsTecho(Transform p)
    {
        var g = Grupo("Leds", p);
        float y = ALTO - 0.17f;

        Cubo("Tira_Norte", g.transform, new Vector3(ANCHO / 2f, y, 0.06f),
             new Vector3(ANCHO, 0.05f, 0.05f), mLedFuerte);
        Cubo("Tira_Sur", g.transform, new Vector3(ANCHO / 2f, y, FONDO - 0.06f),
             new Vector3(ANCHO, 0.05f, 0.05f), mLedFuerte);
        Cubo("Tira_Oeste", g.transform, new Vector3(0.06f, y, FONDO / 2f),
             new Vector3(0.05f, 0.05f, FONDO), mLedFuerte);
        Cubo("Tira_Este", g.transform, new Vector3(ANCHO - 0.06f, y, FONDO / 2f),
             new Vector3(0.05f, 0.05f, FONDO), mLedFuerte);

        // Esquinas verticales, de piso a techo
        float[] esquinasX = { 0.06f, ANCHO - 0.06f };
        float[] esquinasZ = { 0.06f, FONDO - 0.06f };
        int n = 1;
        foreach (float ex in esquinasX)
            foreach (float ez in esquinasZ)
                Cubo("Tira_Esquina_" + n++, g.transform, new Vector3(ex, ALTO / 2f, ez),
                     new Vector3(0.05f, ALTO - 0.3f, 0.05f), mLedFuerte);

        // Focos morados que tiñen el cuarto de verdad (las tiras solo brillan)
        int f = 1;
        foreach (float ex in new[] { 1.2f, ANCHO - 1.2f })
            foreach (float ez in new[] { 1.4f, FONDO - 1.4f })
            {
                var luz = new GameObject("Luz_Led_" + f++);
                luz.transform.SetParent(g.transform, false);
                luz.transform.localPosition = new Vector3(ex, ALTO - 0.35f, ez);
                var l = luz.AddComponent<Light>();
                l.type = LightType.Point;
                l.color = new Color(0.65f, 0.25f, 1f);
                l.intensity = 5f;
                l.range = 7f;
            }

        return g;
    }

    // ------------------------------------------------------------------ control de energía

    static ControlEnergia ArmarControlEnergia(Transform raiz, GameObject leds, GameObject luzSala,
                                              GameObject luzLampara, List<GameObject> caras,
                                              GameObject tecladoActivo)
    {
        var g = Grupo("Energia", raiz);
        var control = g.AddComponent<ControlEnergia>();

        control.lucesDelCuarto = new[] { luzSala.GetComponent<Light>(), luzLampara.GetComponent<Light>() };
        control.lucesLed = leds.GetComponentsInChildren<Light>();
        control.tirasLed = leds.GetComponentsInChildren<Renderer>();
        control.materialLedFuerte = mLedFuerte;
        control.materialLedSuave = mLedSuave;

        var despiertan = new List<GameObject>(caras);
        if (tecladoActivo != null) despiertan.Add(tecladoActivo);
        control.objetosConEnergia = despiertan.ToArray();

        EditorUtility.SetDirty(control);
        return control;
    }

    // ------------------------------------------------------------------ panel de objetivos

    // Tablero de avisos colgado al lado de la entrada: dice qué hay que hacer ahora
    // y se va actualizando solo. Es lo primero que se ve al entrar al cuarto.
    static void ArmarPanelObjetivo(Transform raiz)
    {
        var g = Grupo("Panel_Objetivo", raiz);
        g.transform.localPosition = new Vector3(0.1f, 1.55f, 1.15f);
        g.transform.localEulerAngles = new Vector3(0f, 90f, 0f);

        Cubo("Marco", g.transform, Vector3.zero, new Vector3(1.1f, 0.7f, 0.05f), mMaderaOsc, true);
        Cubo("Corcho", g.transform, new Vector3(0f, 0f, 0.032f), new Vector3(1f, 0.6f, 0.01f), mPapel);
        Cubo("Chapa_Titulo", g.transform, new Vector3(0f, 0.26f, 0.04f), new Vector3(0.6f, 0.11f, 0.01f), mBronce);
        Texto("Titulo", g.transform, new Vector3(0f, 0.26f, 0.05f), Vector3.zero,
              "DIRECCION", 0.16f, new Color(0.15f, 0.12f, 0.05f), 0.6f, 0.12f);

        var texto = Texto("Texto_Objetivo", g.transform, new Vector3(0f, -0.06f, 0.045f), Vector3.zero,
                          "", 0.2f, new Color(0.12f, 0.1f, 0.08f), 0.95f, 0.42f);
        texto.lineSpacing = -12f;

        // Una luz chica para que el cartel se lea aunque el cuarto esté sin energía
        var luz = new GameObject("Luz_Panel");
        luz.transform.SetParent(g.transform, false);
        luz.transform.localPosition = new Vector3(0f, 0.1f, 0.5f);
        var l = luz.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = new Color(1f, 0.95f, 0.8f);
        l.intensity = 1.4f;
        l.range = 1.6f;

        panelObjetivo = g.AddComponent<PanelObjetivo>();
        panelObjetivo.texto = texto;
        panelObjetivo.pasos = new[]
        {
            "SIN ENERGIA\n\nEl cuarto esta sin luz.\nBusca el tablero electrico\njunto a la puerta y sube la llave.",
            "VOLVIO LA LUZ\n\nLos relojes despertaron.\nTocalos para ver su numero.\nRevisa el estante y el cajon del escritorio.",
            "ORDEN: C - A - B - E\n\nUn reloj quedo parado y no tiene numero.\nMira la placa del cuadro para saber\na que hora hay que ponerlo.",
            "YA TENES LOS 4 NUMEROS\n\nMarcalos en el teclado de la puerta,\nen el orden de la bitacora.",
            "PUERTA ABIERTA\n\nLlevate el medallon del cajon\ny segui al proximo cuarto."
        };
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

        Cubo("Caja", g.transform, Vector3.zero, new Vector3(0.34f, 0.44f, 0.1f), mMetal, true);
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
                         new Vector3(0.07f, 0.14f, 0.05f), mBronce, true);
        llave.AddComponent<XRSimpleInteractable>();
        var boton = llave.AddComponent<PressableButton>();
        boton.parteMovil = llave.transform;
        boton.recorrido = 0.03f;
        Resaltar(llave, llave.GetComponent<Renderer>());

        // Al subir la llave vuelve la energía: de eso se encarga ControlEnergia,
        // que prende las luces, sube la luz ambiental, baja los LED y despierta
        // los relojes y el teclado de una sola vez
        UnityEventTools.AddVoidPersistentListener(boton.alPresionar, new UnityAction(control.Encender));
        UnityEventTools.AddBoolPersistentListener(boton.alPresionar, new UnityAction<bool>(pilotoVerde.SetActive), true);
        UnityEventTools.AddBoolPersistentListener(boton.alPresionar, new UnityAction<bool>(pilotoRojo.SetActive), false);
        UnityEventTools.AddVoidPersistentListener(boton.alPresionar, new UnityAction(audio.Play));

        AvisarPanel(boton.alPresionar, 1);
        EditorUtility.SetDirty(boton);
    }

    // ------------------------------------------------------------------ escritorio

    static void ArmarEscritorio(Transform p)
    {
        var g = Grupo("Escritorio", p);
        g.transform.localPosition = new Vector3(1.5f, 0f, 2.6f);

        Cubo("Tablero", g.transform, new Vector3(0f, 0.75f, 0f), new Vector3(1.8f, 0.06f, 0.9f), mMadera, true);
        Cubo("Canto", g.transform, new Vector3(0f, 0.71f, 0f), new Vector3(1.84f, 0.03f, 0.94f), mMaderaOsc);
        Cubo("Faldon", g.transform, new Vector3(0f, 0.58f, -0.42f), new Vector3(1.7f, 0.24f, 0.04f), mMadera);

        Cubo("Pata_Izq_Frente", g.transform, new Vector3(-0.82f, 0.36f, -0.38f), new Vector3(0.08f, 0.72f, 0.08f), mMaderaOsc);
        Cubo("Pata_Izq_Fondo", g.transform, new Vector3(-0.82f, 0.36f, 0.38f), new Vector3(0.08f, 0.72f, 0.08f), mMaderaOsc);

        // Cajonera: se arma con paneles para que el cajón tenga hueco donde entrar
        var cajonera = Grupo("Cajonera", g.transform);
        cajonera.transform.localPosition = new Vector3(0.52f, 0f, 0f);
        Cubo("Lado_Izq", cajonera.transform, new Vector3(-0.31f, 0.36f, 0f), new Vector3(0.03f, 0.72f, 0.88f), mMadera);
        Cubo("Lado_Der", cajonera.transform, new Vector3(0.31f, 0.36f, 0f), new Vector3(0.03f, 0.72f, 0.88f), mMadera);
        Cubo("Fondo", cajonera.transform, new Vector3(0f, 0.36f, 0.43f), new Vector3(0.62f, 0.72f, 0.03f), mMadera);
        Cubo("Base", cajonera.transform, new Vector3(0f, 0.02f, 0f), new Vector3(0.62f, 0.04f, 0.88f), mMaderaOsc);
        Cubo("Division", cajonera.transform, new Vector3(0f, 0.40f, 0f), new Vector3(0.6f, 0.02f, 0.86f), mMadera);

        // El cajón mira al Norte (hacia donde entra el jugador), por eso va rotado 180
        var cajon = Grupo("Cajon", g.transform);
        cajon.transform.localPosition = new Vector3(0.52f, 0.22f, -0.42f);
        cajon.transform.localEulerAngles = new Vector3(0f, 180f, 0f);

        Cubo("Frente", cajon.transform, Vector3.zero, new Vector3(0.6f, 0.3f, 0.04f), mMadera, true);
        Cubo("Piso_Cajon", cajon.transform, new Vector3(0f, -0.12f, -0.38f), new Vector3(0.54f, 0.03f, 0.72f), mMaderaOsc);
        Cubo("Lado_A", cajon.transform, new Vector3(-0.27f, -0.02f, -0.38f), new Vector3(0.03f, 0.22f, 0.72f), mMaderaOsc);
        Cubo("Lado_B", cajon.transform, new Vector3(0.27f, -0.02f, -0.38f), new Vector3(0.03f, 0.22f, 0.72f), mMaderaOsc);
        Cubo("Tirador", cajon.transform, new Vector3(0f, 0f, 0.04f), new Vector3(0.24f, 0.035f, 0.05f), mBronce);

        var inter = cajon.AddComponent<XRSimpleInteractable>();
        var drawer = cajon.AddComponent<Drawer>();
        drawer.aperturaMaxima = 0.45f;
        Resaltar(cajon, cajon.transform.Find("Frente").GetComponent<Renderer>());
        EditorUtility.SetDirty(inter);

        // El medallón que hay que llevarse al Cuarto 4
        var medallon = Cilindro("Medallon", cajon.transform, new Vector3(0f, -0.07f, -0.3f),
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

        Cilindro("Portalapices", g.transform, new Vector3(-0.72f, 0.83f, -0.25f), new Vector3(0.08f, 0.05f, 0.08f), mMetal);
        Modelo("desk_lamp_arm_01", g.transform, new Vector3(0.65f, 0.78f, 0.25f), 200f, 1f);

        // Hoja de pistas: se toca y aparece el cartel con todo lo que hay que hacer
        var hoja = Grupo("Hoja_Pistas", g.transform);
        hoja.transform.localPosition = new Vector3(-0.45f, 0.79f, 0.02f);
        hoja.transform.localEulerAngles = new Vector3(0f, 12f, 0f);
        Cubo("Papel", hoja.transform, Vector3.zero, new Vector3(0.32f, 0.01f, 0.44f), mPapel, true);
        Texto("Titulo_Hoja", hoja.transform, new Vector3(0f, 0.008f, 0f), new Vector3(-90f, 0f, 0f),
              "NOTAS\n\n(tocar para leer)", 0.1f, new Color(0.2f, 0.17f, 0.15f), 0.3f, 0.4f);

        var panelPistas = Grupo("Panel_Pistas", g.transform);
        panelPistas.transform.localPosition = new Vector3(-0.45f, 1.45f, 0.1f);
        Cubo("Fondo", panelPistas.transform, Vector3.zero, new Vector3(0.95f, 0.72f, 0.02f), mNegro);
        Cubo("Borde", panelPistas.transform, new Vector3(0f, 0f, 0.012f), new Vector3(0.99f, 0.76f, 0.01f), mLedSuave);
        var textoPistas = Texto("Texto_Pistas", panelPistas.transform, new Vector3(0f, 0f, -0.02f),
                                new Vector3(0f, 180f, 0f),
                                "QUE HAY QUE HACER\n\n" +
                                "1 - Subir la llave del tablero, junto a la puerta\n" +
                                "2 - Tocar los relojes: cada uno muestra su numero\n" +
                                "3 - Leer la bitacora del estante: da el orden\n" +
                                "4 - El reloj parado se gira con la mano hasta la\n" +
                                "     hora que dice la placa del cuadro\n" +
                                "5 - Marcar los 4 numeros en el teclado\n" +
                                "6 - Llevarse el medallon del cajon",
                                0.115f, new Color(0.75f, 0.95f, 1f), 0.92f, 0.68f);
        textoPistas.lineSpacing = -14f;
        panelPistas.SetActive(false);

        var interHoja = hoja.AddComponent<XRSimpleInteractable>();
        var pistas = hoja.AddComponent<MostrarPistas>();
        pistas.panel = panelPistas;
        Resaltar(hoja, hoja.transform.Find("Papel").GetComponent<Renderer>());
        UnityEventTools.AddVoidPersistentListener(interHoja.selectEntered, new UnityAction(pistas.Alternar));
        EditorUtility.SetDirty(interHoja);
        EditorUtility.SetDirty(pistas);

        // Silla del director, detrás del escritorio.
        // Si está el modelo de Poly Haven se usa ese; si no, se arma con cubos.
        if (Modelo("painted_wooden_chair_01", p, new Vector3(1.5f, 0f, 3.45f), 180f, 1f) != null) return;

        var silla = Grupo("Silla", p);
        silla.transform.localPosition = new Vector3(1.5f, 0f, 3.45f);
        Cubo("Asiento", silla.transform, new Vector3(0f, 0.45f, 0f), new Vector3(0.5f, 0.07f, 0.5f), mMaderaOsc, true);
        Cubo("Respaldo", silla.transform, new Vector3(0f, 0.75f, 0.23f), new Vector3(0.5f, 0.55f, 0.06f), mMaderaOsc);
        Cubo("Pata_1", silla.transform, new Vector3(-0.21f, 0.22f, -0.21f), new Vector3(0.05f, 0.44f, 0.05f), mMaderaOsc);
        Cubo("Pata_2", silla.transform, new Vector3(0.21f, 0.22f, -0.21f), new Vector3(0.05f, 0.44f, 0.05f), mMaderaOsc);
        Cubo("Pata_3", silla.transform, new Vector3(-0.21f, 0.22f, 0.21f), new Vector3(0.05f, 0.44f, 0.05f), mMaderaOsc);
        Cubo("Pata_4", silla.transform, new Vector3(0.21f, 0.22f, 0.21f), new Vector3(0.05f, 0.44f, 0.05f), mMaderaOsc);
    }

    // ------------------------------------------------------------------ estantería

    static void ArmarEstanteria(Transform p)
    {
        var g = Grupo("Estanteria", p);
        g.transform.localPosition = new Vector3(5.7f, 0f, 2.6f);

        Cubo("Lado_Norte", g.transform, new Vector3(0f, 0.95f, -0.85f), new Vector3(0.4f, 1.9f, 0.04f), mMadera);
        Cubo("Lado_Sur", g.transform, new Vector3(0f, 0.95f, 0.85f), new Vector3(0.4f, 1.9f, 0.04f), mMadera);
        Cubo("Fondo", g.transform, new Vector3(0.18f, 0.95f, 0f), new Vector3(0.04f, 1.9f, 1.7f), mMaderaOsc);

        float[] alturas = { 0.35f, 0.85f, 1.35f, 1.85f };
        for (int i = 0; i < alturas.Length; i++)
            Cubo("Balda_" + (i + 1), g.transform, new Vector3(0f, alturas[i], 0f),
                 new Vector3(0.4f, 0.04f, 1.74f), mMadera, true);

        // Libros de relleno
        var libros = Grupo("Libros", g.transform);
        Material[] colores = { mLibroA, mLibroB, mLibroC, mMaderaOsc };
        int n = 0;
        for (int balda = 0; balda < 3; balda++)
        {
            float z = -0.75f;
            for (int i = 0; i < 7; i++)
            {
                float grosor = 0.05f + (n % 3) * 0.015f;
                float alto = 0.24f + (n % 4) * 0.025f;
                z += grosor / 2f + 0.01f;
                Cubo("Libro_" + n, libros.transform,
                     new Vector3(-0.02f, alturas[balda] + 0.02f + alto / 2f, z),
                     new Vector3(0.24f, alto, grosor), colores[n % colores.Length]);
                z += grosor / 2f;
                n++;
            }
        }

        // La bitácora: apoyada abierta sobre una balda, con el orden de los relojes
        var bitacora = Grupo("Bitacora", g.transform);
        bitacora.transform.localPosition = new Vector3(-0.05f, 0.93f, 0.45f);
        bitacora.transform.localEulerAngles = new Vector3(-25f, 90f, 0f);
        Cubo("Tapa", bitacora.transform, Vector3.zero, new Vector3(0.38f, 0.03f, 0.3f), mLibroA, true);
        Cubo("Hojas", bitacora.transform, new Vector3(0f, 0.02f, 0f), new Vector3(0.36f, 0.02f, 0.28f), mPapel);
        var txt = Texto("Texto_Bitacora", bitacora.transform, new Vector3(0f, 0.035f, 0f),
                        new Vector3(-90f, 0f, 0f),
                        "BITACORA\n\nCampanas:\nC - A - B - E\n\nEl reloj E se paro",
                        0.22f, new Color(0.15f, 0.12f, 0.1f), 0.35f, 0.27f);
        txt.lineSpacing = -10f;

        // Tocar la bitácora cuenta como haberla leído: el cartel pasa al paso siguiente
        var interBitacora = bitacora.AddComponent<XRSimpleInteractable>();
        Resaltar(bitacora, bitacora.transform.Find("Tapa").GetComponent<Renderer>());
        AvisarPanel(interBitacora.selectEntered, 2);
        EditorUtility.SetDirty(interBitacora);
    }

    // ------------------------------------------------------------------ cuadro

    static void ArmarCuadro(Transform p)
    {
        var g = Grupo("Cuadro", p);
        g.transform.localPosition = new Vector3(0.09f, 1.6f, 3.3f);
        g.transform.localEulerAngles = new Vector3(0f, 90f, 0f);

        Cubo("Marco", g.transform, Vector3.zero, new Vector3(0.8f, 1f, 0.06f), mBronce, true);
        Cubo("Lienzo", g.transform, new Vector3(0f, 0f, 0.035f), new Vector3(0.7f, 0.9f, 0.01f), mMaderaOsc);
        Cubo("Retrato", g.transform, new Vector3(0f, 0.08f, 0.042f), new Vector3(0.45f, 0.55f, 0.01f), mPapel);

        Cubo("Placa", g.transform, new Vector3(0f, -0.36f, 0.045f), new Vector3(0.6f, 0.16f, 0.01f), mBronce);
        Texto("Texto_Placa", g.transform, new Vector3(0f, -0.36f, 0.055f), Vector3.zero,
              "DIRECCION\nEl colegio cerro a las 9:00", 0.15f, new Color(0.16f, 0.12f, 0.05f), 0.58f, 0.15f);
    }

    // ------------------------------------------------------------------ lámpara

    // Devuelve la luz de la lámpara, que arranca apagada hasta que vuelve la energía
    static GameObject ArmarLampara(Transform p)
    {
        var g = Grupo("Lampara_Pie", p);
        g.transform.localPosition = new Vector3(0.75f, 0f, 1.5f);

        Cilindro("Base", g.transform, new Vector3(0f, 0.03f, 0f), new Vector3(0.34f, 0.03f, 0.34f), mMetal, true);
        Cilindro("Tubo", g.transform, new Vector3(0f, 0.75f, 0f), new Vector3(0.05f, 0.72f, 0.05f), mMetal);
        Cilindro("Pantalla", g.transform, new Vector3(0f, 1.55f, 0f), new Vector3(0.36f, 0.16f, 0.36f), mLampara);

        // Queda encendida acá: la apaga ControlEnergia mientras no hay energía
        var luz = new GameObject("Luz_Lampara");
        luz.transform.SetParent(g.transform, false);
        luz.transform.localPosition = new Vector3(0f, 1.42f, 0f);
        var l = luz.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = new Color(1f, 0.88f, 0.65f);
        l.intensity = 3.2f;
        l.range = 7f;
        l.shadows = LightShadows.Soft;
        return luz;
    }

    // ------------------------------------------------------------------ decoración

    static void ArmarDecoracion(Transform p)
    {
        var g = Grupo("Decoracion", p);

        // Diplomas colgados en la pared Oeste
        for (int i = 0; i < 3; i++)
        {
            var d = Grupo("Diploma_" + (i + 1), g.transform);
            d.transform.localPosition = new Vector3(0.08f, 1.75f - i * 0.05f, 1.5f + i * 0.62f);
            d.transform.localEulerAngles = new Vector3(0f, 90f, i == 1 ? 4f : -3f);
            Cubo("Marco", d.transform, Vector3.zero, new Vector3(0.42f, 0.32f, 0.04f), mMaderaOsc);
            Cubo("Papel", d.transform, new Vector3(0f, 0f, 0.026f), new Vector3(0.36f, 0.26f, 0.01f), mPapel);
        }

        // Alfombra bajo el escritorio
        Cubo("Alfombra", g.transform, new Vector3(1.5f, 0.02f, 2.7f), new Vector3(2.4f, 0.02f, 2f), mAlfombra);

        // Papelera y una planta seca: si están los modelos de Poly Haven se usan esos
        if (Modelo("metal_trash_can", g.transform, new Vector3(2.55f, 0f, 3.1f), 0f, 1f) == null)
            Cilindro("Papelera", g.transform, new Vector3(2.55f, 0.16f, 3.1f), new Vector3(0.26f, 0.16f, 0.26f), mMetal, true);

        // Estos son decorativos: si el modelo de Poly Haven está, aparece; si no, no pasa nada
        Modelo("potted_plant_01", g.transform, new Vector3(5.4f, 0f, 0.6f), 0f, 1f);
        Modelo("cardboard_box_01", g.transform, new Vector3(0.7f, 0f, 6.4f), 25f, 1f);
        Modelo("vintage_cabinet_01", g.transform, new Vector3(3.2f, 0f, 6.7f), 180f, 1f);

        // Ventana tapiada en la pared Este
        var v = Grupo("Ventana_Tapiada", g.transform);
        v.transform.localPosition = new Vector3(ANCHO - 0.06f, 1.7f, 5.2f);
        Cubo("Hueco", v.transform, Vector3.zero, new Vector3(0.06f, 1.1f, 1.3f), mNegro);
        for (int i = 0; i < 3; i++)
        {
            var tabla = Cubo("Tabla_" + (i + 1), v.transform, new Vector3(-0.05f, -0.3f + i * 0.32f, 0f),
                             new Vector3(0.04f, 0.2f, 1.45f), mMadera);
            tabla.transform.localEulerAngles = new Vector3(i % 2 == 0 ? 6f : -5f, 0f, 0f);
        }

        // Papel tapiz despegado en una esquina
        var tapiz = Cubo("Tapiz_Despegado", g.transform, new Vector3(0.06f, 2.2f, 6.6f),
                         new Vector3(0.02f, 0.9f, 0.5f), mPapel);
        tapiz.transform.localEulerAngles = new Vector3(0f, 0f, 12f);
    }

    // ------------------------------------------------------------------ relojes

    // Devuelve las "caras" de los relojes, que arrancan apagadas hasta que vuelve la energía
    static List<GameObject> ArmarRelojes(Transform p)
    {
        var caras = new List<GameObject>();
        // A=7, B=1, C=3 se leen tocándolos. D es el señuelo (marca la hora del apagón).
        // E está parado en las 12: hay que girarle la manecilla hasta las 9.
        //
        // Las X van de mayor a menor a propósito: los relojes están en la pared Norte
        // y el jugador los mira desde adentro del cuarto, o sea mirando hacia -Z.
        // Desde ahí su derecha es -X, así que el reloj con la X más grande es el que
        // se ve más a la izquierda. Puestos así, se leen A B C D E de izquierda a derecha.
        caras.Add(Reloj(p, "Reloj_A", 4.8f, "A", 7f, 0f, 7, false, false));
        caras.Add(Reloj(p, "Reloj_B", 4.2f, "B", 1f, 0f, 1, false, false));
        caras.Add(Reloj(p, "Reloj_C", 3.6f, "C", 3f, 0f, 3, false, false));
        caras.Add(Reloj(p, "Reloj_D", 3.0f, "D", 4.67f, 40f, 0, true, false));
        caras.Add(Reloj(p, "Reloj_E", 2.4f, "E", 12f, 0f, HORA_RELOJ_E, false, true));
        return caras;
    }

    static GameObject Reloj(Transform p, string nombre, float x, string letra,
                            float hora, float minutos, int digito, bool senuelo, bool ajustable)
    {
        var g = Grupo(nombre, p);
        g.transform.localPosition = new Vector3(x, ALTURA_RELOJ, 0.05f);

        // La caja y la esfera muerta se ven siempre, aunque no haya energía
        var caja = Cilindro("Caja", g.transform, Vector3.zero, new Vector3(0.34f, 0.035f, 0.34f), mMetal);
        caja.transform.localEulerAngles = new Vector3(90f, 0f, 0f);

        var apagada = Cilindro("Esfera_Apagada", g.transform, new Vector3(0f, 0f, 0.034f),
                               new Vector3(0.3f, 0.004f, 0.3f), mNegro);
        apagada.transform.localEulerAngles = new Vector3(90f, 0f, 0f);

        // Todo lo que se enciende cuando vuelve la luz
        var cara = Grupo("Cara", g.transform);

        var esfera = Cilindro("Esfera", cara.transform, new Vector3(0f, 0f, 0.036f),
                              new Vector3(0.3f, 0.004f, 0.3f), mEsfera);
        esfera.transform.localEulerAngles = new Vector3(90f, 0f, 0f);

        // Marcas de las 12 horas
        var marcas = Grupo("Marcas", cara.transform);
        marcas.transform.localPosition = new Vector3(0f, 0f, 0.04f);
        for (int h = 0; h < 12; h++)
        {
            float ang = h * 30f * Mathf.Deg2Rad;
            bool grande = h % 3 == 0;
            var m = Cubo("Marca_" + (h == 0 ? 12 : h), marcas.transform,
                         new Vector3(Mathf.Sin(ang) * 0.117f, Mathf.Cos(ang) * 0.117f, 0f),
                         new Vector3(grande ? 0.014f : 0.008f, grande ? 0.04f : 0.022f, 0.004f), mNegro);
            m.transform.localEulerAngles = new Vector3(0f, 0f, -h * 30f);
        }

        // Manecilla de los minutos
        var pivMin = Grupo("Manecilla_Minuto", cara.transform);
        pivMin.transform.localPosition = new Vector3(0f, 0f, 0.046f);
        pivMin.transform.localEulerAngles = new Vector3(0f, 0f, -minutos * 6f);
        Cubo("Aguja", pivMin.transform, new Vector3(0f, 0.06f, 0f), new Vector3(0.009f, 0.13f, 0.005f), mNegro);

        // Manecilla de la hora (es la que se agarra si el reloj es ajustable)
        var pivHora = Grupo("Manecilla_Hora", cara.transform);
        pivHora.transform.localPosition = new Vector3(0f, 0f, 0.052f);
        pivHora.transform.localEulerAngles = new Vector3(0f, 0f, -hora * 30f);
        var aguja = Cubo("Aguja", pivHora.transform, new Vector3(0f, 0.042f, 0f),
                         new Vector3(0.018f, 0.095f, 0.006f), mNegro, ajustable);

        Esfera("Pin", cara.transform, new Vector3(0f, 0f, 0.056f), new Vector3(0.022f, 0.022f, 0.022f), mBronce);

        Texto("Letra", cara.transform, new Vector3(0f, -0.085f, 0.042f), Vector3.zero,
              letra, 0.55f, new Color(0.18f, 0.15f, 0.12f), 0.2f, 0.12f);

        // Chapita con el dígito, oculta hasta que el jugador se lo gana
        GameObject chapa = null;
        if (digito > 0)
        {
            chapa = Grupo("Digito", cara.transform);
            chapa.transform.localPosition = new Vector3(0f, -0.26f, 0.02f);
            Cubo("Chapa", chapa.transform, Vector3.zero, new Vector3(0.16f, 0.16f, 0.02f), mDigito);
            Texto("Numero", chapa.transform, new Vector3(0f, 0f, 0.02f), Vector3.zero,
                  digito.ToString(), 0.9f, new Color(0.03f, 0.08f, 0.1f), 0.16f, 0.16f);
            chapa.SetActive(false);
        }

        if (ajustable)
        {
            // El reloj parado: se le gira la manecilla hasta la hora que dice el cuadro
            var inter = pivHora.AddComponent<XRSimpleInteractable>();
            var manecilla = pivHora.AddComponent<RelojManecilla>();
            manecilla.horaObjetivo = HORA_RELOJ_E;
            Resaltar(pivHora, aguja.GetComponent<Renderer>());

            var luzOk = LuzApagada("Luz_EnHora", cara.transform, new Vector3(0f, 0f, 0.22f),
                                   new Color(0.4f, 1f, 0.5f), 2.5f, 0.9f);

            UnityEventTools.AddBoolPersistentListener(manecilla.alPonerEnHora, new UnityAction<bool>(luzOk.SetActive), true);
            if (chapa != null)
                UnityEventTools.AddBoolPersistentListener(manecilla.alPonerEnHora, new UnityAction<bool>(chapa.SetActive), true);
            AvisarPanel(manecilla.alPonerEnHora, 3);

            EditorUtility.SetDirty(inter);
            EditorUtility.SetDirty(manecilla);
        }
        else
        {
            // Zona invisible para poder tocar el reloj sin que moleste la caja
            var zona = Cubo("Zona_Tactil", cara.transform, new Vector3(0f, 0f, 0.03f),
                            new Vector3(0.3f, 0.3f, 0.06f), null, true);
            Object.DestroyImmediate(zona.GetComponent<MeshRenderer>());

            var inter = cara.AddComponent<XRSimpleInteractable>();
            // En el señuelo se resalta la caja y no la esfera: la esfera se la queda
            // el RelojDecoy para mostrar el vidrio resquebrajado del susto
            Resaltar(cara, senuelo ? caja.GetComponent<Renderer>() : esfera.GetComponent<Renderer>());
            EditorUtility.SetDirty(inter);

            if (senuelo)
            {
                var luz = LuzApagada("Luz_Susto", cara.transform, new Vector3(0f, 0f, 0.25f),
                                     new Color(1f, 0.25f, 0.2f), 9f, 2.5f, false);
                var decoy = cara.AddComponent<RelojDecoy>();
                decoy.vidrio = esfera.GetComponent<Renderer>();
                decoy.materialResquebrajado = mGrieta;
                decoy.luzSusto = luz.GetComponent<Light>();
                decoy.duracionDestello = 0.8f;
                EditorUtility.SetDirty(decoy);
            }
            else if (chapa != null)
            {
                // Tocar un reloj de la lista muestra su dígito
                UnityEventTools.AddBoolPersistentListener(inter.selectEntered, new UnityAction<bool>(chapa.SetActive), true);
            }
        }

        cara.SetActive(false);
        return cara;
    }

    // ------------------------------------------------------------------ teclado

    // Devuelve el grupo que se enciende cuando vuelve la energía
    static GameObject ArmarTeclado(Transform raiz, Door puerta)
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
        if (puerta != null)
            UnityEventTools.AddVoidPersistentListener(keypad.OnSolved, new UnityAction(puerta.Abrir));

        AvisarPanel(keypad.OnSolved, 4);

        UnityEventTools.AddBoolPersistentListener(keypad.alError, new UnityAction<bool>(luzMal.SetActive), true);
        UnityEventTools.AddVoidPersistentListener(keypad.alError, new UnityAction(audioMal.Play));
        EditorUtility.SetDirty(keypad);

        activo.SetActive(false);
        return activo;
    }

    // ------------------------------------------------------------------ puerta

    static Door ArmarPuertaSalida(Transform raiz)
    {
        var g = Grupo("Puerta_Salida", raiz);
        g.transform.localPosition = new Vector3(SALIDA_X0, 0f, FONDO);

        float ancho = SALIDA_X1 - SALIDA_X0;
        Cubo("Jamba_Izq", g.transform, new Vector3(-0.07f, ALTO_PUERTA / 2f, 0f),
             new Vector3(0.14f, ALTO_PUERTA, 0.24f), mMaderaOsc, true);
        Cubo("Jamba_Der", g.transform, new Vector3(ancho + 0.07f, ALTO_PUERTA / 2f, 0f),
             new Vector3(0.14f, ALTO_PUERTA, 0.24f), mMaderaOsc, true);
        Cubo("Dintel", g.transform, new Vector3(ancho / 2f, ALTO_PUERTA + 0.07f, 0f),
             new Vector3(ancho + 0.28f, 0.14f, 0.24f), mMaderaOsc, true);

        Cubo("Cartel", g.transform, new Vector3(ancho / 2f, ALTO_PUERTA + 0.28f, -0.1f),
             new Vector3(0.5f, 0.16f, 0.03f), mMetal);
        Texto("Texto_Cartel", g.transform, new Vector3(ancho / 2f, ALTO_PUERTA + 0.28f, -0.13f),
              new Vector3(0f, 180f, 0f), "SALIDA", 0.14f, new Color(0.4f, 1f, 0.5f), 0.5f, 0.15f);

        var bisagra = Grupo("Bisagra", g.transform);
        var puerta = bisagra.AddComponent<Door>();
        // Ángulo negativo: la hoja gira hacia +Z, o sea hacia afuera del cuarto
        puerta.anguloApertura = -95f;
        puerta.duracion = 1.4f;

        Cubo("Hoja", bisagra.transform, new Vector3(ancho / 2f, ALTO_PUERTA / 2f, 0f),
             new Vector3(ancho, ALTO_PUERTA, 0.06f), mMadera, true);
        Cubo("Tablero_Alto", bisagra.transform, new Vector3(ancho / 2f, ALTO_PUERTA * 0.72f, -0.035f),
             new Vector3(ancho - 0.3f, 0.6f, 0.02f), mMaderaOsc);
        Cubo("Tablero_Bajo", bisagra.transform, new Vector3(ancho / 2f, ALTO_PUERTA * 0.3f, -0.035f),
             new Vector3(ancho - 0.3f, 0.7f, 0.02f), mMaderaOsc);
        Esfera("Manija", bisagra.transform, new Vector3(ancho - 0.14f, 1.05f, -0.06f),
               new Vector3(0.07f, 0.07f, 0.07f), mBronce);

        EditorUtility.SetDirty(puerta);
        return puerta;
    }

    // ------------------------------------------------------------------ luces

    // Devuelve la luz de sala. La apaga ControlEnergia al arrancar, no se apaga acá,
    // porque el que decide si hay energía o no es ese script.
    static GameObject ArmarLuces(Transform p)
    {
        // Luz verde de emergencia sobre la puerta de salida: es la única que
        // sobrevive al apagón, así que queda encendida siempre
        var emer = new GameObject("Luz_Emergencia");
        emer.transform.SetParent(p, false);
        emer.transform.localPosition = new Vector3((SALIDA_X0 + SALIDA_X1) / 2f, ALTO_PUERTA + 0.3f, FONDO - 0.4f);
        var le = emer.AddComponent<Light>();
        le.type = LightType.Point;
        le.color = new Color(0.4f, 1f, 0.55f);
        le.intensity = 1.6f;
        le.range = 3.5f;

        var sala = new GameObject("Luz_Sala");
        sala.transform.SetParent(p, false);
        sala.transform.localPosition = new Vector3(ANCHO / 2f, ALTO - 0.35f, FONDO / 2f);
        var ls = sala.AddComponent<Light>();
        ls.type = LightType.Point;
        ls.color = new Color(1f, 0.96f, 0.88f);
        ls.intensity = 4.5f;
        ls.range = 18f;
        ls.shadows = LightShadows.Soft;
        return sala;
    }

    // ------------------------------------------------------------------ apoyo

    // La escena venía con la luz ambiental del skybox al máximo, así que todo se veía
    // igual de iluminado aunque las luces estuvieran apagadas y el apagón no se notaba.
    // Con ambiente casi negro, lo único que ilumina son las luces reales del cuarto.
    // OJO: esto es un ajuste de toda la escena, también afecta al Cuarto 1.
    static void OscurecerEscena()
    {
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.04f, 0.045f, 0.06f);
    }

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
        datos.pista = "Primero hay que devolver la energia desde el tablero. " +
                      "La bitacora del estante da el orden de los relojes y el cuadro dice " +
                      "a que hora hay que poner el reloj que quedo parado.";
        datos.mensajeAcierto = "La cerradura del teclado hace clic y la puerta se abre";
        datos.mensajeError = "El teclado parpadea en rojo: codigo incorrecto";
        EditorUtility.SetDirty(datos);
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

        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(ancho, alto);

        var t = go.GetComponent<TextMeshPro>();
        t.text = texto;
        t.fontSize = tamano;
        t.color = color;
        t.alignment = TextAlignmentOptions.Center;
        return t;
    }

    // Luz que arranca apagada. "porObjeto" define cómo se la prende después:
    // true  -> se prende activando el GameObject (así la prenden los eventos)
    // false -> se prende habilitando el componente Light (así la prende RelojDecoy)
    static GameObject LuzApagada(string nombre, Transform padre, Vector3 pos,
                                 Color color, float intensidad, float rango, bool porObjeto = true)
    {
        var go = new GameObject(nombre);
        go.transform.SetParent(padre, false);
        go.transform.localPosition = pos;
        var l = go.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = color;
        l.intensity = intensidad;
        l.range = rango;

        if (porObjeto) go.SetActive(false);
        else l.enabled = false;

        return go;
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

        // Los nombres sueltos ("american_walnut_veneer", etc.) son los de Poly Haven:
        // si esas texturas están en el proyecto se usan, y si no queda el color plano.
        mMadera = Mat("C2_Madera", new Color(0.36f, 0.23f, 0.14f), 0f, 0.25f,
                      default, "american_walnut_veneer", 1f, 1f);
        mMaderaOsc = Mat("C2_MaderaOscura", new Color(0.19f, 0.12f, 0.07f), 0f, 0.2f,
                         default, "dark_wooden_planks", 1f, 1f);
        mPared = Mat("C2_Pared", new Color(0.55f, 0.56f, 0.48f), 0f, 0.05f,
                     default, "decrepit_wallpaper", 3f, 1.5f);
        mZocalo = Mat("C2_Zocalo", new Color(0.23f, 0.16f, 0.1f), 0f, 0.2f,
                      default, "dark_wooden_planks", 4f, 1f);
        mPiso = Mat("C2_Piso", new Color(0.5f, 0.49f, 0.45f), 0f, 0.35f,
                    default, "checkered_pavement_tiles", 3f, 3.5f);
        mBaldosaA = Mat("C2_BaldosaClara", new Color(0.66f, 0.64f, 0.58f), 0f, 0.35f);
        mBaldosaB = Mat("C2_BaldosaOscura", new Color(0.16f, 0.16f, 0.15f), 0f, 0.35f);
        mMetal = Mat("C2_Metal", new Color(0.52f, 0.54f, 0.56f), 0.85f, 0.55f,
                     default, "metal_plate", 1f, 1f);

        hayTexturaPiso = BuscarTextura("checkered_pavement_tiles_diff") != null;
        mBronce = Mat("C2_Bronce", new Color(0.72f, 0.52f, 0.22f), 0.9f, 0.6f);
        mEsfera = Mat("C2_EsferaReloj", new Color(0.92f, 0.9f, 0.83f), 0f, 0.3f);
        mNegro = Mat("C2_Negro", new Color(0.07f, 0.07f, 0.08f), 0.1f, 0.3f);
        mVerde = Mat("C2_Verde", new Color(0.2f, 0.7f, 0.3f), 0f, 0.4f, new Color(0.15f, 0.8f, 0.3f));
        mRojo = Mat("C2_Rojo", new Color(0.7f, 0.15f, 0.12f), 0f, 0.4f, new Color(0.9f, 0.1f, 0.08f));
        mPantalla = Mat("C2_Pantalla", new Color(0.05f, 0.12f, 0.06f), 0f, 0.6f);
        mResaltado = Mat("C2_Resaltado", new Color(0.55f, 0.85f, 1f), 0f, 0.6f, new Color(0.25f, 0.6f, 0.9f));
        mGrieta = Mat("C2_VidrioRoto", new Color(0.5f, 0.5f, 0.52f), 0.2f, 0.75f);
        mAlfombra = Mat("C2_Alfombra", new Color(0.32f, 0.11f, 0.12f), 0f, 0.05f,
                        default, "dirty_carpet", 2f, 2f);
        mPapel = Mat("C2_Papel", new Color(0.85f, 0.82f, 0.72f), 0f, 0.1f);
        mLibroA = Mat("C2_LibroA", new Color(0.35f, 0.14f, 0.15f), 0f, 0.15f);
        mLibroB = Mat("C2_LibroB", new Color(0.14f, 0.25f, 0.33f), 0f, 0.15f);
        mLibroC = Mat("C2_LibroC", new Color(0.24f, 0.28f, 0.16f), 0f, 0.15f);
        mLampara = Mat("C2_PantallaLampara", new Color(0.16f, 0.42f, 0.24f), 0f, 0.3f);

        // Tiras LED: fuerte mientras no hay energía, suave cuando vuelve la luz
        mLedFuerte = Mat("C2_LedFuerte", new Color(0.45f, 0.15f, 0.85f), 0f, 0.8f,
                         new Color(1.7f, 0.5f, 3f));
        mLedSuave = Mat("C2_LedSuave", new Color(0.35f, 0.15f, 0.6f), 0f, 0.8f,
                        new Color(0.35f, 0.1f, 0.6f));
        mDigito = Mat("C2_Digito", new Color(0.1f, 0.1f, 0.12f), 0f, 0.7f,
                      new Color(0.15f, 0.9f, 1.1f));
    }

    // "polyHaven" es el nombre del asset en Poly Haven (por ejemplo "dark_wooden_planks").
    // Si sus texturas están en el proyecto se le ponen al material; si no, queda el color plano.
    static Material Mat(string nombre, Color color, float metalico, float suavidad, Color emision = default,
                        string polyHaven = null, float tileX = 1f, float tileY = 1f)
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

            var baseMap = BuscarTextura(polyHaven + "_diff");
            if (baseMap != null && m.HasProperty("_BaseMap"))
            {
                // Con textura, el color tiñe: se pone blanco para ver la textura tal cual
                m.SetTexture("_BaseMap", baseMap);
                m.SetTextureScale("_BaseMap", tiling);
                if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", Color.white);
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

    // Pone un modelo de Poly Haven si está descargado en el proyecto.
    // Devuelve null si no está, y ahí el que llama arma la versión con cubos.
    static GameObject Modelo(string nombrePolyHaven, Transform padre, Vector3 pos, float giroY, float escala)
    {
        var guids = AssetDatabase.FindAssets(nombrePolyHaven + " t:GameObject");
        if (guids.Length == 0) return null;

        var fuente = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guids[0]));
        if (fuente == null) return null;

        var go = (GameObject)PrefabUtility.InstantiatePrefab(fuente, padre);
        go.name = nombrePolyHaven;
        go.transform.localPosition = pos;
        go.transform.localEulerAngles = new Vector3(0f, giroY, 0f);
        go.transform.localScale = Vector3.one * escala;
        return go;
    }

    static Texture2D BuscarTextura(string prefijo)
    {
        var guids = AssetDatabase.FindAssets(prefijo + " t:Texture2D");
        if (guids.Length == 0) return null;
        return AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GUIDToAssetPath(guids[0]));
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
}
