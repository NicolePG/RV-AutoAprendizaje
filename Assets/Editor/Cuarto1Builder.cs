using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Filtering;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;

// Herramienta SOLO de editor: está en la carpeta Editor, así que no entra en el build del Quest.
// Construye el Cuarto 1 (Recepción) dentro de la escena EscapeRoom: estructura, colores, luces,
// pistas y objetos interactivos (botones, cajones, destornillador y tapa atornillada).
//
// Uso: menú  Escape Room > Construir Cuarto 1 (Recepción)
// Se puede ejecutar las veces que quieran: borra el cuarto anterior y lo vuelve a crear.
//
// Todas las medidas están en metros. El centro del piso del cuarto es (0, 0, 0)
// y el norte (+Z) es el muro donde está la puerta hacia el Cuarto 2 (Oficina).
public static class Cuarto1Builder
{
    const string EscenaBase = "Assets/Scenes/BasicScene.unity";
    const string EscenaJuego = "Assets/Scenes/EscapeRoom.unity";
    const string CarpetaMateriales = "Assets/Materials/Cuarto1";
    const string RutaSonidoClic = "Assets/VRTemplateAssets/Audio/Button_22_click.wav";
    const string NombreRaiz = "Cuarto1_Recepcion";

    static readonly Color TintaOscura = new Color(0.12f, 0.12f, 0.12f);
    static readonly Color TintaBlanca = new Color(0.97f, 0.97f, 0.95f);
    static readonly Color TintaPantalla = new Color(0.61f, 0.95f, 0.71f);
    static readonly Color TintaAzul = new Color(0.12f, 0.27f, 0.58f);
    static readonly Color TintaRoja = new Color(0.75f, 0.10f, 0.10f);
    static readonly Color TintaVerde = new Color(0.18f, 0.50f, 0.25f);

    static Material matPiso, matPared, matVerdeAzulado, matTecho, matMadera, matMaderaOscura, matAzul, matMetal;
    static Material matNegro, matPapel, matLaton, matRojo, matAmarillo, matVerde, matTerracota, matCarton;
    static Material matCorcho, matAlfombra, matAgua, matFoco, matPantalla, matLuzRoja, matLuzAmbar;

    static AudioClip sonidoClic;
    static Collider puntaDestornillador; // se crea dentro del cajón y la usan los tornillos de la cerradura

    [MenuItem("Escape Room/Construir Cuarto 1 (Recepción)")]
    static void Construir()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogWarning("Detén el modo Play antes de construir el cuarto.");
            return;
        }
        if (!AbrirEscenaJuego()) return;

        Borrar(NombreRaiz); // versión anterior del cuarto
        Borrar("Plane");    // piso de la plantilla, lo reemplaza el nuestro

        CrearMateriales();
        sonidoClic = AssetDatabase.LoadAssetAtPath<AudioClip>(RutaSonidoClic);
        Transform raiz = new GameObject(NombreRaiz).transform;

        ConstruirEstructura(Grupo("Estructura", raiz));
        ConstruirIluminacion(Grupo("Iluminacion", raiz));
        ConstruirMostrador(Grupo("Mostrador", raiz)); // va antes que la puerta: aquí se crea el destornillador
        ConstruirPuerta(Grupo("Puerta_Oficina", raiz));
        ConstruirPistas(Grupo("Pistas", raiz));
        ConstruirSalaDeEspera(Grupo("SalaDeEspera", raiz));
        ColocarJugador();

        AssetDatabase.SaveAssets();
        Scene escena = SceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(escena);
        EditorSceneManager.SaveScene(escena);
        Selection.activeTransform = raiz;
        Debug.Log("Cuarto 1 (Recepción) construido en " + EscenaJuego);
    }

    // ================================================================ Estructura

    static void ConstruirEstructura(Transform g)
    {
        // Piso de 6 x 7 m. Es el único lugar al que el jugador se puede teletransportar.
        GameObject piso = Caja("Piso", g, new Vector3(0, -0.05f, 0), new Vector3(6, 0.1f, 7), matPiso);
        piso.AddComponent<TeleportationArea>().interactionLayers = InteractionLayerMask.GetMask("Teleport");

        Caja("Techo", g, new Vector3(0, 3.05f, 0), new Vector3(6, 0.1f, 7), matTecho);
        Caja("Pared_Oeste", g, new Vector3(-3.1f, 1.5f, 0), new Vector3(0.2f, 3, 7.4f), matPared);
        Caja("Pared_Este", g, new Vector3(3.1f, 1.5f, 0), new Vector3(0.2f, 3, 7.4f), matPared);
        Caja("Pared_Sur", g, new Vector3(0, 1.5f, -3.6f), new Vector3(6, 3, 0.2f), matPared);

        // Muro norte en 3 piezas para dejar el hueco de la puerta (x de 1.3 a 2.3, 2.1 m de alto)
        Caja("Pared_Norte_Izq", g, new Vector3(-0.85f, 1.5f, 3.6f), new Vector3(4.3f, 3, 0.2f), matPared);
        Caja("Pared_Norte_Der", g, new Vector3(2.65f, 1.5f, 3.6f), new Vector3(0.7f, 3, 0.2f), matPared);
        Caja("Dintel", g, new Vector3(1.8f, 2.55f, 3.6f), new Vector3(1.0f, 0.9f, 0.2f), matPared);

        // Zócalo abajo y franja a media altura: dan color a los muros sin tapar nada
        FranjaEnMuros(g, "Zocalo", 0.06f, 0.12f);
        FranjaEnMuros(g, "Franja", 1.0f, 0.08f);

        // Alfombra SIN collider: si tuviera, el rayo de teletransporte chocaría con ella y no llegaría al piso
        SinCollider(Caja("Alfombra", g, new Vector3(-1.6f, 0.003f, -1.4f), new Vector3(1.6f, 0.006f, 2.2f), matAlfombra));
    }

    static void FranjaEnMuros(Transform g, string nombre, float altura, float alto)
    {
        SinCollider(Caja(nombre + "_Oeste", g, new Vector3(-2.99f, altura, 0), new Vector3(0.02f, alto, 7), matVerdeAzulado));
        SinCollider(Caja(nombre + "_Este", g, new Vector3(2.99f, altura, 0), new Vector3(0.02f, alto, 7), matVerdeAzulado));
        SinCollider(Caja(nombre + "_Sur", g, new Vector3(0, altura, -3.49f), new Vector3(6, alto, 0.02f), matVerdeAzulado));
        SinCollider(Caja(nombre + "_Norte_Izq", g, new Vector3(-0.85f, altura, 3.49f), new Vector3(4.3f, alto, 0.02f), matVerdeAzulado));
        SinCollider(Caja(nombre + "_Norte_Der", g, new Vector3(2.65f, altura, 3.49f), new Vector3(0.7f, alto, 0.02f), matVerdeAzulado));
    }

    // ================================================================ Iluminación

    static void ConstruirIluminacion(Transform g)
    {
        // Foco colgante encendido al centro del cuarto: es la luz principal
        Transform foco = Grupo("Foco_Colgante", g);
        foco.localPosition = new Vector3(0, 2.3f, 0);
        SinCollider(Primitiva(PrimitiveType.Cylinder, "Cable", foco, new Vector3(0, 0.35f, 0), new Vector3(0.01f, 0.35f, 0.01f), matNegro));
        SinCollider(Primitiva(PrimitiveType.Cylinder, "Pantalla", foco, new Vector3(0, 0.1f, 0), new Vector3(0.36f, 0.02f, 0.36f), matVerdeAzulado));
        SinCollider(Primitiva(PrimitiveType.Cylinder, "Portalamparas", foco, new Vector3(0, 0.07f, 0), new Vector3(0.05f, 0.03f, 0.05f), matNegro));
        SinCollider(Primitiva(PrimitiveType.Sphere, "Bombilla", foco, Vector3.zero, Vector3.one * 0.12f, matFoco));

        // Pocas luces a propósito: el Quest tiene un límite de luces por objeto
        Luz("Luz_Foco", g, new Vector3(0, 2.2f, 0), new Color(1f, 0.86f, 0.65f), 3.5f, 9f);
        Luz("Luz_Emergencia", g, new Vector3(1.8f, 2.6f, 3.1f), new Color(1f, 0.63f, 0.2f), 1.2f, 5f);
        Luz("Luz_Cerradura", g, new Vector3(0.95f, 1.45f, 3.35f), new Color(0.9f, 0.22f, 0.2f), 0.6f, 0.6f);

        // La luz de la plantilla se baja y se pone un ambiente suave para que ninguna esquina quede negra
        GameObject sol = GameObject.Find("Directional Light");
        if (sol != null) sol.GetComponent<Light>().intensity = 0.3f;
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.33f, 0.34f, 0.36f);
    }

    // ================================================================ Mostrador

    static void ConstruirMostrador(Transform g)
    {
        // Mostrador de 3 m. Cerrado por el frente (sur) y abierto por detrás (norte), donde están los cajones.
        Caja("Tablero", g, new Vector3(-1f, 1.075f, 1.2f), new Vector3(3, 0.05f, 0.8f), matMadera);
        Caja("Frente", g, new Vector3(-1f, 0.525f, 0.825f), new Vector3(3, 1.05f, 0.05f), matVerdeAzulado);
        Caja("Lado_Izq", g, new Vector3(-2.475f, 0.525f, 1.2f), new Vector3(0.05f, 1.05f, 0.8f), matVerdeAzulado);
        Caja("Lado_Der", g, new Vector3(0.475f, 0.525f, 1.2f), new Vector3(0.05f, 1.05f, 0.8f), matVerdeAzulado);
        Caja("Repisa", g, new Vector3(-1f, 0.35f, 1.2f), new Vector3(2.9f, 0.03f, 0.7f), matMadera);
        Caja("Cajonera_Base", g, new Vector3(-0.5f, 0.74f, 1.225f), new Vector3(1.8f, 0.02f, 0.75f), matMadera);

        // Utilería sobre el tablero
        Caja("Monitor_Base", g, new Vector3(-1.7f, 1.11f, 1.3f), new Vector3(0.2f, 0.02f, 0.15f), matNegro);
        Caja("Monitor", g, new Vector3(-1.7f, 1.28f, 1.3f), new Vector3(0.5f, 0.32f, 0.04f), matNegro);
        Caja("Papeles", g, new Vector3(-2.15f, 1.12f, 1.3f), new Vector3(0.22f, 0.04f, 0.3f), matPapel);

        // Silla del recepcionista, lejos de los cajones para que no estorbe al abrirlos
        Transform silla = Grupo("Silla", g);
        silla.localPosition = new Vector3(-1.9f, 0, 2.4f);
        Caja("Asiento", silla, new Vector3(0, 0.45f, 0), new Vector3(0.45f, 0.05f, 0.45f), matAzul);
        Caja("Respaldo", silla, new Vector3(0, 0.72f, 0.2f), new Vector3(0.45f, 0.5f, 0.05f), matAzul);
        Primitiva(PrimitiveType.Cylinder, "Pata", silla, new Vector3(0, 0.21f, 0), new Vector3(0.08f, 0.21f, 0.08f), matNegro);

        SinCollider(Caja("Letrero_Recepcion_Placa", g, new Vector3(-1f, 2.2f, 3.49f), new Vector3(2.1f, 0.42f, 0.02f), matVerdeAzulado));
        Texto("Letrero_Recepcion", g, "RECEPCIÓN", new Vector3(-1f, 2.2f, 3.477f), Vector3.zero, new Vector2(1.9f, 0.34f), TintaBlanca);

        ConstruirNota(g);
        ConstruirContestador(g);
        ConstruirTimbre(g);
        ConstruirCajones(Grupo("Cajones", g));
    }

    static void ConstruirNota(Transform g)
    {
        // Nota con la primera pista: es lo primero que el jugador puede agarrar
        GameObject nota = Agarrable("Nota_Pista", g, new Vector3(-0.8f, 1.105f, 1.0f), new Vector3(0.15f, 0.01f, 0.21f));
        SinCollider(Caja("Hoja", nota.transform, Vector3.zero, new Vector3(0.15f, 0.01f, 0.21f), matPapel));
        // Rotado 90° en X para que el texto quede acostado sobre la hoja, mirando hacia arriba
        Texto("Texto", nota.transform,
            "TURNO NOCHE\n\nDejé un mensaje en el contestador sobre la llave de la oficina.\n\n- Vigilancia",
            new Vector3(0, 0.006f, 0), new Vector3(90, 0, 0), new Vector2(0.13f, 0.19f), TintaOscura);
    }

    static void ConstruirContestador(Transform g)
    {
        // Contestador mirando al jugador (sur). Al presionar PLAY cambia la pantalla y muestra el mensaje.
        Transform c = Grupo("Contestador", g);
        c.localPosition = new Vector3(-1.3f, 1.1f, 1.0f);
        Caja("Cuerpo", c, new Vector3(0, 0.035f, 0), new Vector3(0.3f, 0.07f, 0.2f), matNegro);

        // Pantalla inclinada 30° hacia el jugador; los textos van 6 mm por delante de ella
        Vector3 inclinacion = new Vector3(30, 0, 0);
        SinCollider(Caja("Pantalla", c, new Vector3(0, 0.14f, 0.06f), new Vector3(0.26f, 0.14f, 0.01f), matPantalla, inclinacion));
        Vector3 posTexto = new Vector3(0, 0.143f, 0.0548f);
        GameObject espera = Texto("Texto_Espera", c, "MENSAJE NUEVO\n<size=60%>Presiona PLAY</size>",
            posTexto, inclinacion, new Vector2(0.24f, 0.12f), TintaPantalla);
        GameObject mensaje = Texto("Texto_Mensaje", c,
            "<size=70%>MENSAJE NUEVO</size>\nLa copia de la llave de la oficina está en el cajón con el símbolo del laboratorio. La tapa de la cerradura sigue atornillada.",
            posTexto, inclinacion, new Vector2(0.24f, 0.12f), TintaPantalla);
        mensaje.SetActive(false);

        // LED rojo = hay un mensaje sin escuchar
        GameObject led = SinCollider(Primitiva(PrimitiveType.Sphere, "LED", c, new Vector3(-0.11f, 0.075f, -0.07f), Vector3.one * 0.015f, matLuzRoja));

        // Botón PLAY. Lo que hace se conecta en el evento "alPresionar" (se ve en el Inspector del botón).
        Transform play = Grupo("Boton_Play", c);
        play.localPosition = new Vector3(0.09f, 0.07f, -0.05f);
        GameObject capuchon = SinCollider(Primitiva(PrimitiveType.Cylinder, "Capuchon", play, new Vector3(0, 0.006f, 0), new Vector3(0.035f, 0.006f, 0.035f), matVerde));
        PressableButton boton = Boton(play.gameObject, capuchon.transform, new Vector3(0, 0.01f, 0), new Vector3(0.045f, 0.03f, 0.045f));
        UnityEventTools.AddBoolPersistentListener(boton.alPresionar, espera.SetActive, false);
        UnityEventTools.AddBoolPersistentListener(boton.alPresionar, mensaje.SetActive, true);
        UnityEventTools.AddBoolPersistentListener(boton.alPresionar, led.SetActive, false);
        Texto("Etiqueta_Play", c, "PLAY", new Vector3(0.09f, 0.0705f, -0.088f), new Vector3(90, 0, 0), new Vector2(0.06f, 0.018f), TintaBlanca);
    }

    static void ConstruirTimbre(Transform g)
    {
        // Timbre de recepción: solo suena. Es un botón "seguro" para aprender a presionar.
        Transform t = Grupo("Timbre", g);
        t.localPosition = new Vector3(0.1f, 1.1f, 1.0f);
        SinCollider(Primitiva(PrimitiveType.Cylinder, "Base", t, new Vector3(0, 0.01f, 0), new Vector3(0.08f, 0.01f, 0.08f), matNegro));
        SinCollider(Primitiva(PrimitiveType.Sphere, "Campana", t, new Vector3(0, 0.035f, 0), new Vector3(0.07f, 0.035f, 0.07f), matMetal));
        GameObject pulsador = SinCollider(Primitiva(PrimitiveType.Cylinder, "Pulsador", t, new Vector3(0, 0.075f, 0), new Vector3(0.012f, 0.008f, 0.012f), matMetal));
        Boton(t.gameObject, pulsador.transform, new Vector3(0, 0.045f, 0), new Vector3(0.08f, 0.07f, 0.08f));
    }

    static void ConstruirCajones(Transform g)
    {
        // Tres cajones bajo el tablero, del lado de recepción. Cada uno tiene un símbolo en el frente:
        // rombo = llave (pista: el logo del laboratorio), cuadrado = destornillador, círculo = señuelo.
        Transform rombo = Cajon("Cajon_Rombo", g, -1.1f);
        SinCollider(Caja("Simbolo", rombo, new Vector3(0, -0.045f, 0.315f), new Vector3(0.036f, 0.036f, 0.004f), matAzul, new Vector3(0, 0, 45)));
        GameObject llave = Agarrable("Llave", rombo, new Vector3(0, -0.06f, 0), new Vector3(0.165f, 0.02f, 0.05f), true);
        llave.GetComponent<BoxCollider>().center = new Vector3(0.005f, 0, 0);
        SinCollider(Caja("Cabeza", llave.transform, new Vector3(-0.05f, 0, 0), new Vector3(0.05f, 0.015f, 0.05f), matLaton));
        SinCollider(Caja("Vastago", llave.transform, new Vector3(0.03f, 0, 0), new Vector3(0.11f, 0.012f, 0.015f), matLaton));
        SinCollider(Caja("Diente", llave.transform, new Vector3(0.07f, 0, 0.015f), new Vector3(0.02f, 0.012f, 0.02f), matLaton));

        Transform cuadrado = Cajon("Cajon_Cuadrado", g, -0.5f);
        SinCollider(Caja("Simbolo", cuadrado, new Vector3(0, -0.045f, 0.315f), new Vector3(0.045f, 0.045f, 0.004f), matVerde));
        ConstruirDestornillador(cuadrado);

        Transform circulo = Cajon("Cajon_Circulo", g, 0.1f);
        SinCollider(Primitiva(PrimitiveType.Cylinder, "Simbolo", circulo, new Vector3(0, -0.045f, 0.316f), new Vector3(0.05f, 0.002f, 0.05f), matRojo, new Vector3(90, 0, 0)));
        SinCollider(Caja("Hojas", circulo, new Vector3(-0.1f, -0.067f, 0), new Vector3(0.2f, 0.006f, 0.28f), matPapel));
        GameObject sello = Agarrable("Sello", circulo, new Vector3(0.12f, -0.03f, 0), new Vector3(0.05f, 0.08f, 0.05f), true);
        SinCollider(Caja("Base", sello.transform, new Vector3(0, -0.03f, 0), new Vector3(0.05f, 0.02f, 0.05f), matNegro));
        SinCollider(Primitiva(PrimitiveType.Cylinder, "Mango", sello.transform, new Vector3(0, 0.01f, 0), new Vector3(0.03f, 0.03f, 0.03f), matRojo));
    }

    // Cajón vacío que se abre jalando. Empieza cerrado: su frente queda en el borde trasero del mostrador (z = 1.6).
    static Transform Cajon(string nombre, Transform padre, float x)
    {
        Transform cajon = Grupo(nombre, padre);
        cajon.localPosition = new Vector3(x, 0.86f, 1.3f);

        Caja("Fondo", cajon, new Vector3(0, -0.08f, 0), new Vector3(0.52f, 0.02f, 0.58f), matMadera);
        Caja("Lado_Izq", cajon, new Vector3(-0.26f, -0.01f, 0), new Vector3(0.02f, 0.14f, 0.58f), matMadera);
        Caja("Lado_Der", cajon, new Vector3(0.26f, -0.01f, 0), new Vector3(0.02f, 0.14f, 0.58f), matMadera);
        Caja("Trasera", cajon, new Vector3(0, -0.01f, -0.28f), new Vector3(0.52f, 0.14f, 0.02f), matMadera);
        GameObject frente = Caja("Frente", cajon, new Vector3(0, 0, 0.3f), new Vector3(0.54f, 0.18f, 0.02f), matMadera);
        GameObject manija = Caja("Manija", cajon, new Vector3(0, 0.03f, 0.325f), new Vector3(0.16f, 0.025f, 0.03f), matMetal);
        SinCollider(Caja("Placa", cajon, new Vector3(0, -0.045f, 0.312f), new Vector3(0.09f, 0.07f, 0.004f), matPapel));

        // Rigidbody kinematic: el cajón lo mueve el script, no la física
        cajon.gameObject.AddComponent<Rigidbody>().isKinematic = true;

        // Solo el frente y la manija sirven para agarrar el cajón (no lo que hay adentro)
        XRSimpleInteractable interactable = cajon.gameObject.AddComponent<XRSimpleInteractable>();
        interactable.colliders.Add(frente.GetComponent<Collider>());
        interactable.colliders.Add(manija.GetComponent<Collider>());
        cajon.gameObject.AddComponent<Drawer>().aperturaMaxima = 0.4f;
        return cajon;
    }

    static void ConstruirDestornillador(Transform cajon)
    {
        // Herramienta. Lo que quita los tornillos es SOLO el collider de la punta.
        GameObject d = Agarrable("Destornillador", cajon, new Vector3(0, -0.054f, 0), new Vector3(0.22f, 0.032f, 0.032f), true);
        SinCollider(Primitiva(PrimitiveType.Cylinder, "Mango", d.transform, new Vector3(-0.06f, 0, 0), new Vector3(0.032f, 0.05f, 0.032f), matAmarillo, new Vector3(0, 0, 90)));
        SinCollider(Primitiva(PrimitiveType.Cylinder, "Varilla", d.transform, new Vector3(0.05f, 0, 0), new Vector3(0.008f, 0.06f, 0.008f), matMetal, new Vector3(0, 0, 90)));

        GameObject punta = new GameObject("Punta");
        punta.transform.SetParent(d.transform, false);
        punta.transform.localPosition = new Vector3(0.115f, 0, 0);
        BoxCollider colPunta = punta.AddComponent<BoxCollider>();
        colPunta.size = Vector3.one * 0.02f;
        puntaDestornillador = colPunta;
    }

    // ================================================================ Puerta y cerradura

    static void ConstruirPuerta(Transform g)
    {
        // La bisagra es un objeto vacío en el borde de la puerta.
        // Door.cs (paso 5) girará este objeto y la puerta girará con él.
        Transform bisagra = Grupo("Puerta_Bisagra", g);
        bisagra.localPosition = new Vector3(1.3f, 0, 3.6f);
        Caja("Puerta", bisagra, new Vector3(0.5f, 1.05f, 0), new Vector3(0.98f, 2.08f, 0.06f), matMaderaOscura);
        Caja("Manija", bisagra, new Vector3(0.85f, 1.0f, -0.06f), new Vector3(0.12f, 0.03f, 0.03f), matMetal);

        SinCollider(Caja("Letrero_Oficina_Placa", g, new Vector3(1.8f, 2.3f, 3.49f), new Vector3(0.9f, 0.22f, 0.02f), matVerdeAzulado));
        Texto("Letrero_Oficina", g, "OFICINA", new Vector3(1.8f, 2.3f, 3.477f), Vector3.zero, new Vector2(0.8f, 0.18f), TintaBlanca);
        SinCollider(Caja("Lampara_Emergencia", g, new Vector3(1.8f, 2.85f, 3.44f), new Vector3(0.4f, 0.12f, 0.12f), matLuzAmbar));

        // Cerradura junto a la puerta, a la altura de la mano
        Caja("Cerradura", g, new Vector3(0.95f, 1.2f, 3.46f), new Vector3(0.25f, 0.35f, 0.08f), matAzul);
        SinCollider(Caja("Ranura", g, new Vector3(0.95f, 1.2f, 3.415f), new Vector3(0.03f, 0.08f, 0.01f), matNegro));

        // Punto donde quedará encajada la llave. El XRSocketInteractor se agrega en el paso 9 (KeyPuzzle).
        Transform socket = Grupo("Socket_Llave", g);
        socket.localPosition = new Vector3(0.95f, 1.2f, 3.38f);

        // Luz roja = cerrado. KeyPuzzle la cambiará a verde al acertar.
        SinCollider(Primitiva(PrimitiveType.Sphere, "Luz_Indicador", g, new Vector3(0.95f, 1.45f, 3.46f), Vector3.one * 0.06f, matLuzRoja));

        // Tapa metálica atornillada encima de la ranura. Empieza kinematic: no se cae hasta quitar los 2 tornillos.
        GameObject tapa = Caja("Tapa_Cerradura", g, new Vector3(0.95f, 1.2f, 3.4125f), new Vector3(0.2f, 0.28f, 0.015f), matMetal);
        tapa.AddComponent<Rigidbody>().isKinematic = true;
        ScrewedPanel panel = tapa.AddComponent<ScrewedPanel>();
        panel.tornillosRestantes = 2;
        Tornillo("Tornillo_Arriba", g, new Vector3(0.95f, 1.3f, 3.4f), panel);
        Tornillo("Tornillo_Abajo", g, new Vector3(0.95f, 1.1f, 3.4f), panel);
    }

    static void Tornillo(string nombre, Transform padre, Vector3 pos, ScrewedPanel tapa)
    {
        GameObject t = SinCollider(Primitiva(PrimitiveType.Cylinder, nombre, padre, pos, new Vector3(0.022f, 0.005f, 0.022f), matMetal, new Vector3(90, 0, 0)));

        // Zona de contacto más grande que el tornillo para que sea fácil acertarle en VR
        SphereCollider zona = t.AddComponent<SphereCollider>();
        zona.isTrigger = true;
        zona.radius = 1.4f; // en escala local: 1.4 x 0.022 m = unos 3 cm de radio

        Screw screw = t.AddComponent<Screw>();
        screw.puntaHerramienta = puntaDestornillador;
        screw.tapa = tapa;
        screw.sonido = sonidoClic;
    }

    // ================================================================ Pistas para mirar

    static void ConstruirPistas(Transform g)
    {
        // Cartelera de corcho en el muro oeste, sobre la banca.
        // Los textos se rotan -90° en Y para que se lean desde dentro del cuarto (mirando al oeste).
        Vector3 haciaEste = new Vector3(0, -90, 0);
        Caja("Cartelera", g, new Vector3(-2.985f, 1.65f, -1.2f), new Vector3(0.03f, 0.9f, 1.4f), matCorcho);

        // Póster con el logo del laboratorio: el rombo azul es la pista del cajón correcto
        SinCollider(Caja("Poster_Logo", g, new Vector3(-2.967f, 1.72f, -1.62f), new Vector3(0.004f, 0.5f, 0.4f), matPapel));
        SinCollider(Caja("Logo_Rombo", g, new Vector3(-2.963f, 1.84f, -1.62f), new Vector3(0.004f, 0.14f, 0.14f), matAzul, new Vector3(45, 0, 0)));
        Texto("Poster_Texto", g, "LABORATORIOS\nALBA\n<size=45%>Investigación y desarrollo</size>",
            new Vector3(-2.958f, 1.62f, -1.62f), haciaEste, new Vector2(0.34f, 0.2f), TintaAzul);

        // Plano de evacuación: muestra los 4 cuartos en orden y dónde está la salida
        SinCollider(Caja("Plano_Evacuacion", g, new Vector3(-2.967f, 1.8f, -1.0f), new Vector3(0.004f, 0.36f, 0.56f), matPapel));
        Texto("Plano_Titulo", g, "PLANO DE EVACUACIÓN", new Vector3(-2.958f, 1.93f, -1.0f), haciaEste, new Vector2(0.5f, 0.05f), TintaOscura);
        string[] cuartos = { "RECEPCIÓN", "OFICINA", "MÁQUINAS", "LABORATORIO" };
        for (int i = 0; i < cuartos.Length; i++)
        {
            float z = -1.2f + i * 0.13f;
            Material color = i == 0 ? matRojo : (i == cuartos.Length - 1 ? matVerde : matMetal);
            SinCollider(Caja("Plano_Cuarto_" + i, g, new Vector3(-2.963f, 1.79f, z), new Vector3(0.004f, 0.09f, 0.11f), color));
            Texto("Plano_Nombre_" + i, g, cuartos[i], new Vector3(-2.958f, 1.715f, z), haciaEste, new Vector2(0.12f, 0.03f), TintaOscura);
        }
        Texto("Plano_UstedEstaAqui", g, "USTED ESTÁ AQUÍ", new Vector3(-2.958f, 1.67f, -1.2f), haciaEste, new Vector2(0.14f, 0.025f), TintaRoja);
        Texto("Plano_Salida", g, "SALIDA", new Vector3(-2.958f, 1.67f, -0.81f), haciaEste, new Vector2(0.12f, 0.025f), TintaVerde);

        // Hoja de turnos: solo ambienta. Sin horas para no confundir con el código del Cuarto 2.
        SinCollider(Caja("Hoja_Turnos", g, new Vector3(-2.967f, 1.38f, -0.95f), new Vector3(0.004f, 0.28f, 0.3f), matPapel));
        Texto("Turnos_Texto", g, "TURNOS DE VIGILANCIA\n<size=70%>Lunes a viernes: R. Díaz\nFin de semana: M. Soto\nNoche: vigilancia externa</size>",
            new Vector3(-2.958f, 1.38f, -0.95f), haciaEste, new Vector2(0.26f, 0.24f), TintaOscura);

        Chincheta(g, new Vector3(-2.955f, 1.95f, -1.76f), matRojo);
        Chincheta(g, new Vector3(-2.955f, 1.96f, -1.0f), matAmarillo);
        Chincheta(g, new Vector3(-2.955f, 1.5f, -0.95f), matVerde);

        // Llavero junto a la puerta: el gancho de OFICINA vacío dice qué llave hay que buscar
        Caja("Llavero", g, new Vector3(0.2f, 1.55f, 3.49f), new Vector3(0.5f, 0.35f, 0.02f), matMadera);
        Texto("Llavero_Titulo", g, "LLAVES", new Vector3(0.2f, 1.685f, 3.477f), Vector3.zero, new Vector2(0.3f, 0.05f), TintaBlanca);
        string[] ganchos = { "RECEP.", "OFICINA", "MÁQUINAS", "LAB." };
        for (int i = 0; i < ganchos.Length; i++)
        {
            float x = 0.02f + i * 0.12f;
            SinCollider(Primitiva(PrimitiveType.Cylinder, "Gancho_" + i, g, new Vector3(x, 1.58f, 3.46f), new Vector3(0.01f, 0.02f, 0.01f), matMetal, new Vector3(90, 0, 0)));
            Texto("Llavero_Etiqueta_" + i, g, ganchos[i], new Vector3(x, 1.47f, 3.477f), Vector3.zero, new Vector2(0.11f, 0.035f), TintaBlanca);
        }

        // Cámara de seguridad en la esquina noroeste, mirando al centro del cuarto
        Transform camara = Grupo("Camara_Seguridad", g);
        camara.localPosition = new Vector3(-2.85f, 2.8f, 3.35f);
        camara.localRotation = Quaternion.Euler(20, 135, 0);
        SinCollider(Caja("Cuerpo", camara, Vector3.zero, new Vector3(0.12f, 0.08f, 0.2f), matNegro));
        SinCollider(Primitiva(PrimitiveType.Sphere, "LED", camara, new Vector3(0.03f, 0.025f, 0.101f), Vector3.one * 0.012f, matLuzRoja));
    }

    // ================================================================ Sala de espera y decoración

    static void ConstruirSalaDeEspera(Transform g)
    {
        Caja("Banca", g, new Vector3(-2.7f, 0.225f, -1.2f), new Vector3(0.5f, 0.45f, 1.6f), matAzul);
        Caja("Banca_Respaldo", g, new Vector3(-2.96f, 0.675f, -1.2f), new Vector3(0.08f, 0.45f, 1.6f), matAzul);
        Primitiva(PrimitiveType.Cylinder, "Maceta", g, new Vector3(-2.6f, 0.2f, -3f), new Vector3(0.4f, 0.2f, 0.4f), matTerracota);
        Primitiva(PrimitiveType.Sphere, "Planta", g, new Vector3(-2.6f, 0.65f, -3f), Vector3.one * 0.6f, matVerde);

        // Entrada tapiada detrás del punto de inicio: no hay vuelta atrás
        Caja("Entrada_Tapiada", g, new Vector3(0, 1.1f, -3.47f), new Vector3(1.2f, 2.2f, 0.06f), matMaderaOscura);
        Caja("Tabla_1", g, new Vector3(0, 1.4f, -3.42f), new Vector3(1.5f, 0.15f, 0.04f), matMadera, new Vector3(0, 0, 25));
        Caja("Tabla_2", g, new Vector3(0, 0.8f, -3.38f), new Vector3(1.5f, 0.15f, 0.04f), matMadera, new Vector3(0, 0, -25));
        SinCollider(Caja("Cinta_1", g, new Vector3(0, 1.1f, -3.345f), new Vector3(1.3f, 0.08f, 0.01f), matAmarillo, new Vector3(0, 0, 35)));
        SinCollider(Caja("Cinta_2", g, new Vector3(0, 1.1f, -3.33f), new Vector3(1.3f, 0.08f, 0.01f), matAmarillo, new Vector3(0, 0, -35)));
        SinCollider(Caja("Letrero_Entrada_Placa", g, new Vector3(0, 2.45f, -3.49f), new Vector3(1.0f, 0.22f, 0.02f), matVerdeAzulado));
        // Rotado 180° en Y para que se lea desde dentro del cuarto
        Texto("Letrero_Entrada", g, "ENTRADA", new Vector3(0, 2.45f, -3.477f), new Vector3(0, 180, 0), new Vector2(0.9f, 0.18f), TintaBlanca);

        // Extintor con su letrero
        Primitiva(PrimitiveType.Cylinder, "Extintor", g, new Vector3(0.9f, 0.25f, -3.35f), new Vector3(0.16f, 0.25f, 0.16f), matRojo);
        SinCollider(Primitiva(PrimitiveType.Cylinder, "Extintor_Valvula", g, new Vector3(0.9f, 0.54f, -3.35f), new Vector3(0.05f, 0.04f, 0.05f), matNegro));
        SinCollider(Caja("Extintor_Placa", g, new Vector3(0.9f, 1.35f, -3.495f), new Vector3(0.2f, 0.26f, 0.01f), matRojo));
        Texto("Extintor_Letrero", g, "EXTINTOR", new Vector3(0.9f, 1.35f, -3.488f), new Vector3(0, 180, 0), new Vector2(0.18f, 0.06f), TintaBlanca);

        // Dispensador de agua en el pasillo este
        Caja("Dispensador_Agua", g, new Vector3(2.75f, 0.5f, 0.2f), new Vector3(0.3f, 1.0f, 0.3f), matPapel);
        Primitiva(PrimitiveType.Cylinder, "Bidon", g, new Vector3(2.75f, 1.14f, 0.2f), new Vector3(0.24f, 0.14f, 0.24f), matAgua);
        SinCollider(Caja("Grifo_Frio", g, new Vector3(2.59f, 0.85f, 0.14f), new Vector3(0.02f, 0.03f, 0.03f), matAzul));
        SinCollider(Caja("Grifo_Caliente", g, new Vector3(2.59f, 0.85f, 0.26f), new Vector3(0.02f, 0.03f, 0.03f), matRojo));

        Caja("Archivador", g, new Vector3(2.7f, 0.65f, -1f), new Vector3(0.6f, 1.3f, 0.5f), matMetal);
        Caja("Caja_1", g, new Vector3(2.65f, 0.2f, -2.3f), new Vector3(0.5f, 0.4f, 0.4f), matCarton);
        Caja("Caja_2", g, new Vector3(2.6f, 0.55f, -2.25f), new Vector3(0.4f, 0.3f, 0.35f), matCarton, new Vector3(0, 15, 0));
    }

    static void ColocarJugador()
    {
        // Punto de inicio: frente a la entrada tapiada, mirando al norte (hacia el mostrador y la puerta)
        GameObject jugador = GameObject.Find("XR Origin (XR Rig)");
        if (jugador == null) return;
        jugador.transform.SetPositionAndRotation(new Vector3(0, 0, -2.3f), Quaternion.identity);
    }

    // ================================================================ Escena y materiales

    static bool AbrirEscenaJuego()
    {
        if (SceneManager.GetActiveScene().path == EscenaJuego) return true;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return false;

        // La primera vez se crea copiando BasicScene (ya trae XR Origin, luz y EventSystem)
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(EscenaJuego) == null &&
            !AssetDatabase.CopyAsset(EscenaBase, EscenaJuego))
        {
            Debug.LogError("No se pudo copiar " + EscenaBase + " a " + EscenaJuego);
            return false;
        }
        EditorSceneManager.OpenScene(EscenaJuego);
        return true;
    }

    static void CrearMateriales()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Materials")) AssetDatabase.CreateFolder("Assets", "Materials");
        if (!AssetDatabase.IsValidFolder(CarpetaMateriales)) AssetDatabase.CreateFolder("Assets/Materials", "Cuarto1");

        // Para cambiar un color: se cambia aquí y se vuelve a construir el cuarto
        matPiso = CrearMaterial("Piso", new Color(0.55f, 0.52f, 0.47f));
        matPared = CrearMaterial("Pared_Menta", new Color(0.74f, 0.86f, 0.82f));
        matVerdeAzulado = CrearMaterial("Verde_Azulado", new Color(0.17f, 0.40f, 0.42f));
        matTecho = CrearMaterial("Techo", new Color(0.93f, 0.92f, 0.89f));
        matMadera = CrearMaterial("Madera", new Color(0.56f, 0.37f, 0.22f));
        matMaderaOscura = CrearMaterial("Madera_Oscura", new Color(0.40f, 0.26f, 0.16f));
        matAzul = CrearMaterial("Azul", new Color(0.16f, 0.34f, 0.66f));
        matMetal = CrearMaterial("Metal", new Color(0.62f, 0.64f, 0.67f));
        matNegro = CrearMaterial("Negro", new Color(0.12f, 0.12f, 0.13f));
        matPapel = CrearMaterial("Papel", new Color(0.96f, 0.96f, 0.93f));
        matLaton = CrearMaterial("Laton", new Color(0.85f, 0.66f, 0.26f));
        matRojo = CrearMaterial("Rojo", new Color(0.78f, 0.12f, 0.12f));
        matAmarillo = CrearMaterial("Amarillo", new Color(0.95f, 0.78f, 0.12f));
        matVerde = CrearMaterial("Verde", new Color(0.25f, 0.55f, 0.30f));
        matTerracota = CrearMaterial("Terracota", new Color(0.70f, 0.38f, 0.25f));
        matCarton = CrearMaterial("Carton", new Color(0.68f, 0.52f, 0.33f));
        matCorcho = CrearMaterial("Corcho", new Color(0.72f, 0.54f, 0.34f));
        matAlfombra = CrearMaterial("Alfombra", new Color(0.45f, 0.14f, 0.18f));
        matAgua = CrearMaterial("Agua", new Color(0.55f, 0.78f, 0.95f));

        // Materiales que brillan (emisivos). El número es cuánto brillan.
        matFoco = CrearMaterial("Foco", new Color(1f, 0.85f, 0.55f), 4f);
        matPantalla = CrearMaterial("Pantalla", new Color(0.05f, 0.22f, 0.15f), 1f);
        matLuzRoja = CrearMaterial("Luz_Roja", new Color(0.90f, 0.22f, 0.20f), 2f);
        matLuzAmbar = CrearMaterial("Luz_Ambar", new Color(1f, 0.63f, 0f), 2f);
    }

    static Material CrearMaterial(string nombre, Color color, float brillo = 0f)
    {
        string ruta = CarpetaMateriales + "/" + nombre + ".mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(ruta);
        if (mat == null)
        {
            mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(mat, ruta);
        }

        // El color se aplica siempre: el script es el que manda
        mat.SetColor("_BaseColor", color);
        if (brillo > 0f)
        {
            mat.EnableKeyword("_EMISSION");
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            mat.SetColor("_EmissionColor", color * brillo);
        }
        EditorUtility.SetDirty(mat);
        return mat;
    }

    // ================================================================ Ayudantes

    static void Borrar(string nombre)
    {
        GameObject go = GameObject.Find(nombre);
        if (go != null) Object.DestroyImmediate(go);
    }

    static Transform Grupo(string nombre, Transform padre)
    {
        Transform t = new GameObject(nombre).transform;
        t.SetParent(padre, false);
        return t;
    }

    static GameObject Caja(string nombre, Transform padre, Vector3 pos, Vector3 tam, Material mat, Vector3 rot = default)
    {
        return Primitiva(PrimitiveType.Cube, nombre, padre, pos, tam, mat, rot);
    }

    static GameObject Primitiva(PrimitiveType tipo, string nombre, Transform padre, Vector3 pos, Vector3 tam, Material mat, Vector3 rot = default)
    {
        GameObject go = GameObject.CreatePrimitive(tipo);
        go.name = nombre;
        go.transform.SetParent(padre, false);
        go.transform.localPosition = pos;
        go.transform.localRotation = Quaternion.Euler(rot);
        go.transform.localScale = tam;
        go.GetComponent<Renderer>().sharedMaterial = mat;
        return go;
    }

    // Quita el collider: para piezas decorativas o piezas visuales de un objeto que ya tiene su collider en el padre
    static GameObject SinCollider(GameObject go)
    {
        Object.DestroyImmediate(go.GetComponent<Collider>());
        return go;
    }

    static void Chincheta(Transform padre, Vector3 pos, Material mat)
    {
        SinCollider(Primitiva(PrimitiveType.Sphere, "Chincheta", padre, pos, Vector3.one * 0.018f, mat));
    }

    // Objeto vacío con collider, Rigidbody y XRGrabInteractable. Las piezas visuales se agregan como hijas.
    static GameObject Agarrable(string nombre, Transform padre, Vector3 pos, Vector3 tamCollider, bool dentroDeCajon = false)
    {
        GameObject go = new GameObject(nombre);
        go.transform.SetParent(padre, false);
        go.transform.localPosition = pos;
        go.AddComponent<BoxCollider>().size = tamCollider;
        go.AddComponent<Rigidbody>();
        XRGrabInteractable grab = go.AddComponent<XRGrabInteractable>();
        grab.useDynamicAttach = true; // se agarra desde donde toca la mano, no desde el centro
        // Si empieza dentro de un cajón: al soltarlo NO vuelve a ser hijo del cajón
        if (dentroDeCajon) grab.retainTransformParent = false;
        return go;
    }

    // Botón que se presiona con el dedo (hacia abajo) o con el gatillo
    static PressableButton Boton(GameObject go, Transform parteMovil, Vector3 centroCollider, Vector3 tamCollider)
    {
        BoxCollider col = go.AddComponent<BoxCollider>();
        col.center = centroCollider;
        col.size = tamCollider;
        go.AddComponent<XRSimpleInteractable>();

        // El filtro de poke permite presionarlo con la punta del dedo, empujando hacia abajo (-Y)
        XRPokeFilter poke = go.AddComponent<XRPokeFilter>();
        poke.pokeConfiguration.Value.pokeDirection = PokeAxis.NegativeY;

        AudioSource audio = go.AddComponent<AudioSource>();
        audio.playOnAwake = false;
        audio.spatialBlend = 1f; // sonido 3D: se oye desde el botón

        PressableButton boton = go.AddComponent<PressableButton>();
        boton.parteMovil = parteMovil;
        boton.sonido = sonidoClic;
        return boton;
    }

    static GameObject Texto(string nombre, Transform padre, string texto, Vector3 pos, Vector3 rot, Vector2 tam, Color color)
    {
        GameObject go = new GameObject(nombre);
        TextMeshPro tmp = go.AddComponent<TextMeshPro>(); // primero el componente: convierte el Transform en RectTransform
        go.transform.SetParent(padre, false);
        go.transform.localPosition = pos;
        go.transform.localRotation = Quaternion.Euler(rot);
        tmp.rectTransform.sizeDelta = tam;
        tmp.text = texto;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = color;
        tmp.enableAutoSizing = true; // el texto se ajusta solo al tamaño del letrero
        tmp.fontSizeMin = 0.01f;
        tmp.fontSizeMax = 5f;
        return go;
    }

    static void Luz(string nombre, Transform padre, Vector3 pos, Color color, float intensidad, float alcance)
    {
        GameObject go = new GameObject(nombre);
        go.transform.SetParent(padre, false);
        go.transform.localPosition = pos;
        Light luz = go.AddComponent<Light>();
        luz.type = LightType.Point;
        luz.color = color;
        luz.intensity = intensidad;
        luz.range = alcance;
        luz.shadows = LightShadows.None;
    }
}
