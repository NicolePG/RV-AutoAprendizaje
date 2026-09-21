using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Attachment;
using UnityEngine.XR.Interaction.Toolkit.Filtering;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;

// Herramienta SOLO de editor: está en la carpeta Editor, así que no entra en el build del Quest.
// Construye el Cuarto 1 (Recepción = portería del colegio) dentro de la escena EscapeRoom: estructura,
// texturas, luces, pistas y objetos interactivos (botones, cajones, destornillador y tapa atornillada).
//
// Uso: menú  Escape Room > Construir Cuarto 1 (Recepción)
// Se puede ejecutar las veces que quieran: borra el cuarto anterior y lo vuelve a crear.
// Al final hornea la iluminación (barra de progreso abajo a la derecha): hay que esperar a que termine
// antes de darle Play. Cuando termina, guarda la escena sola.
//
// Medidas del documento de diseño: cuarto de 4 x 4 m con techo a 2.6 m. Todo está en metros.
// El centro del piso es (0, 0, 0). Norte (+Z) = pared de entrada (tapiada, ahí empieza el jugador).
// Sur (-Z) = muro con la puerta hacia el Cuarto 2 (Dirección).
// Para pasar a las coordenadas del plano del documento: X_plano = x + 2,  Y_plano = 2 - z.
public static class Cuarto1Builder
{
    const string EscenaBase = "Assets/Scenes/BasicScene.unity";
    const string EscenaJuego = "Assets/Scenes/EscapeRoom.unity";
    const string CarpetaMateriales = "Assets/Materials/Cuarto1";
    const string RutaSonidoClic = "Assets/VRTemplateAssets/Audio/Button_22_click.wav";
    const string NombreRaiz = "Cuarto1_Recepcion";

    // Arte descargado de polyhaven.com (licencia CC0: uso libre). Resolución 1k para no cargar al Quest.
    // Los modelos .gltf los importa el paquete glTFast.
    const string CarpetaTexturas = "Assets/PolyHaven/Texturas";
    const string CarpetaModelos = "Assets/PolyHaven/Modelos";
    const string RutaAjustesLuz = "Assets/Settings/EscapeRoom_Iluminacion.lighting";

    static readonly Color TintaOscura = new Color(0.12f, 0.12f, 0.12f);
    static readonly Color TintaBlanca = new Color(0.97f, 0.97f, 0.95f);
    static readonly Color TintaAzul = new Color(0.12f, 0.27f, 0.58f);
    static readonly Color TintaRoja = new Color(0.75f, 0.10f, 0.10f);
    static readonly Color TintaVerde = new Color(0.18f, 0.50f, 0.25f);
    static readonly Color TintaGris = new Color(0.6f, 0.65f, 0.7f);
    static readonly Color TintaAmbar = new Color(1f, 0.7f, 0.25f);

    // Rotaciones para que un texto se lea de frente según el muro donde está
    static readonly Vector3 TextoMuroSur = new Vector3(0, 180, 0);  // el jugador mira al sur

    static Material matPiso, matPared, matVerdeAzulado, matTecho, matMadera, matMaderaOscura, matAzul, matMetal;
    static Material matNegro, matPapel, matLaton, matRojo, matAmarillo, matVerde, matTerracota, matCarton;
    static Material matTela, matAlfombra, matFoco, matPantalla, matLuzRoja, matLuzAmbar;
    static Material matFrenteMostrador, matCartel, matAcento, matMetalOscuro, matAluminio, matGrisClaro, matVidrio, matPanel;
    static Material matAcero, matPlastico, matRojoSenal, matBlancoSenal, matVidrioClaro, matPantallaApagada, matLuzVerde;

    static AudioClip sonidoClic;
    static Collider puntaDestornillador; // se crea dentro del cajón y la usan los tornillos de la cerradura

    // Partes del acertijo que se crean en distintos lugares del builder y después se conectan con eventos
    static XRGrabInteractable llaveCuarto1;
    static Door puertaDireccion;
    static Light luzCerradura;
    static GameObject luzPrincipal, bombillaEncendida, luzTablero, pantallaEncendida;
    static AudioClip sonidoPalanca, sonidoAcierto;
    const string RutaSonidoPalanca = "Assets/Samples/XR Interaction Toolkit/3.5.1/Starter Assets/DemoAssets/Audio/Button Pop.wav";
    const string RutaSonidoAcierto = "Assets/Samples/XR Interaction Toolkit/3.5.1/Hands Interaction Demo/DemoAssets/Audio/TeleportSelection.wav";

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
        AvisarSiFaltanLucesAdicionales();
        sonidoClic = AssetDatabase.LoadAssetAtPath<AudioClip>(RutaSonidoClic);
        sonidoPalanca = AssetDatabase.LoadAssetAtPath<AudioClip>(RutaSonidoPalanca);
        sonidoAcierto = AssetDatabase.LoadAssetAtPath<AudioClip>(RutaSonidoAcierto);
        Transform raiz = new GameObject(NombreRaiz).transform;

        ConstruirEstructura(Grupo("Estructura", raiz));
        ConstruirIluminacion(Grupo("Iluminacion", raiz));
        ConstruirMostrador(Grupo("Mostrador", raiz)); // va antes que la puerta: aquí se crea el destornillador
        ConstruirPuerta(Grupo("Puerta_Direccion", raiz));
        ConstruirPistas(Grupo("Pistas", raiz));
        ConstruirSalaDeEspera(Grupo("SalaDeEspera", raiz));
        ColocarJugador();
        MarcarEstaticos(raiz);

        AssetDatabase.SaveAssets();
        Scene escena = SceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(escena);
        EditorSceneManager.SaveScene(escena);
        Selection.activeTransform = raiz;
        Debug.Log("Cuarto 1 (Recepción) construido en " + EscenaJuego + ". Horneando la luz...");
        HornearLuz();
    }

    // ================================================================ Estructura

    static void ConstruirEstructura(Transform g)
    {
        // Piso de 4 x 4 m. Es el único lugar al que el jugador se puede teletransportar.
        GameObject piso = Caja("Piso", g, new Vector3(0, -0.05f, 0), new Vector3(4, 0.1f, 4), matPiso);
        piso.AddComponent<TeleportationArea>().interactionLayers = InteractionLayerMask.GetMask("Teleport");

        // Muros de 20 cm de grueso: su cara interior queda justo en ±2 m. Este y oeste tapan las esquinas.
        Caja("Techo", g, new Vector3(0, 2.65f, 0), new Vector3(4.4f, 0.1f, 4.4f), matTecho);
        Caja("Pared_Oeste", g, new Vector3(-2.1f, 1.3f, 0), new Vector3(0.2f, 2.6f, 4.4f), matPared);
        Caja("Pared_Este", g, new Vector3(2.1f, 1.3f, 0), new Vector3(0.2f, 2.6f, 4.4f), matPared);
        Caja("Pared_Norte", g, new Vector3(0, 1.3f, 2.1f), new Vector3(4, 2.6f, 0.2f), matPared);

        // Muro sur en 3 piezas para dejar el hueco de la puerta (x de 0.7 a 1.7, 2.1 m de alto)
        Caja("Pared_Sur_Izq", g, new Vector3(-0.65f, 1.3f, -2.1f), new Vector3(2.7f, 2.6f, 0.2f), matPared);
        Caja("Pared_Sur_Der", g, new Vector3(1.85f, 1.3f, -2.1f), new Vector3(0.3f, 2.6f, 0.2f), matPared);
        Caja("Dintel", g, new Vector3(1.2f, 2.35f, -2.1f), new Vector3(1.0f, 0.5f, 0.2f), matPared);

        // Zócalo oscuro abajo y pasamanos de madera a media altura, como en los pasillos de colegio
        FranjaEnMuros(g, "Zocalo", 0.06f, 0.12f, 0.02f, matCartel);
        FranjaEnMuros(g, "Pasamanos", 0.95f, 0.06f, 0.03f, matMadera);

        // Tapete de entrada: lana gris carbón en espiga sobre un borde de goma negra que asoma 4 cm.
        // SIN collider: si tuviera, el rayo de teletransporte chocaría con él y no llegaría al piso.
        SinCollider(Caja("Tapete_Borde", g, new Vector3(0, 0.003f, 1.0f), new Vector3(1.4f, 0.006f, 1.0f), matNegro));
        SinCollider(Caja("Tapete", g, new Vector3(0, 0.0045f, 1.0f), new Vector3(1.32f, 0.009f, 0.92f), matAlfombra));
    }

    // Una franja que recorre los 4 muros (sin tapar el hueco de la puerta). grosor = cuánto sobresale del muro.
    static void FranjaEnMuros(Transform g, string nombre, float altura, float alto, float grosor, Material mat)
    {
        float d = 2f - grosor / 2; // centro de la franja: pegada a la cara interior del muro (a 2 m del centro)
        // Las piezas no se enciman en las esquinas ni debajo del marco de la puerta (x de 0.65 a 1.75):
        // dos cajas encimadas en el mismo plano parpadean al moverse.
        float largoLados = 4f - 2 * grosor;
        SinCollider(Caja(nombre + "_Oeste", g, new Vector3(-d, altura, 0), new Vector3(grosor, alto, largoLados), mat));
        SinCollider(Caja(nombre + "_Este", g, new Vector3(d, altura, 0), new Vector3(grosor, alto, largoLados), mat));
        SinCollider(Caja(nombre + "_Norte", g, new Vector3(0, altura, d), new Vector3(4, alto, grosor), mat));
        SinCollider(Caja(nombre + "_Sur_Izq", g, new Vector3(-0.675f, altura, -d), new Vector3(2.65f, alto, grosor), mat));
        SinCollider(Caja(nombre + "_Sur_Der", g, new Vector3(1.875f, altura, -d), new Vector3(0.25f, alto, grosor), mat));
    }

    // ================================================================ Iluminación

    static void ConstruirIluminacion(Transform g)
    {
        // Lámpara colgante moderna sobre el mostrador (no al centro, para que nadie choque la cabeza).
        // El modelo mide 1.17 m desde su origen hasta su tope: se sube para que el tope quede 2 mm debajo del techo
        // (2.6 m; pegado justo al techo parpadearía) y su pantalla queda a 1.65 m, por encima del tablero.
        GameObject lampara = Modelo("modern_ceiling_lamp_01", g, new Vector3(-1f, 2.598f - 1.173f, -0.3f), Vector3.zero);
        if (lampara != null) SinSombra(lampara);
        else
        {
            // Sin el modelo: foco gray box
            Transform foco = Grupo("Foco_Colgante", g);
            foco.localPosition = new Vector3(-1f, 2.2f, -0.3f);
            SinCollider(Primitiva(PrimitiveType.Cylinder, "Cable", foco, new Vector3(0, 0.2f, 0), new Vector3(0.01f, 0.2f, 0.01f), matNegro));
            SinCollider(Primitiva(PrimitiveType.Cylinder, "Pantalla", foco, new Vector3(0, 0.1f, 0), new Vector3(0.36f, 0.02f, 0.36f), matVerdeAzulado));
            SinCollider(Primitiva(PrimitiveType.Sphere, "Bombilla", foco, Vector3.zero, Vector3.one * 0.12f, matFoco));
        }

        // Bombilla encendida dentro de la lámpara: aparece cuando vuelve la energía
        bombillaEncendida = SinCollider(Primitiva(PrimitiveType.Sphere, "Bombilla_Encendida", g, new Vector3(-1f, 1.72f, -0.3f), Vector3.one * 0.09f, matFoco));
        bombillaEncendida.SetActive(false);

        // Pocas luces a propósito: el Quest tiene un límite de luces por objeto.
        // El cuarto empieza sin energía: solo se hornea la luz de emergencia (ámbar).
        // Las luces que cambian durante el juego van en tiempo real:
        //  - la principal empieza APAGADA y la enciende la palanca del tablero
        //  - la roja del tablero llama la atención en la oscuridad y se apaga al volver la energía
        //  - la de la cerradura pasa de roja a verde al abrirla
        luzPrincipal = Luz("Luz_Principal", g, new Vector3(-1f, 1.72f, -0.3f), new Color(0.85f, 0.92f, 1f), 3f, 8f, false);
        // Al encenderse parpadea unas veces antes de quedar fija, junto con la bombilla
        luzPrincipal.AddComponent<EncendidoConParpadeo>().bombilla = bombillaEncendida;
        luzPrincipal.SetActive(false);
        Luz("Luz_Emergencia", g, new Vector3(1.86f, 2.36f, -1.85f), new Color(1f, 0.63f, 0.2f), 1.8f, 5f, true);
        luzTablero = Luz("Luz_Tablero", g, new Vector3(-1.8f, 1.3f, -1.35f), new Color(1f, 0.2f, 0.12f), 0.9f, 1.5f, false);
        luzCerradura = Luz("Luz_Cerradura", g, new Vector3(0.4f, 1.45f, -1.85f), new Color(0.9f, 0.22f, 0.2f), 0.6f, 0.6f, false).GetComponent<Light>();

        // La luz del sol de la plantilla se apaga: el cuarto está cerrado y solo lo aplanaría todo.
        // Queda un ambiente suave para que ninguna esquina quede completamente negra.
        GameObject sol = GameObject.Find("Directional Light");
        if (sol != null) sol.GetComponent<Light>().enabled = false;
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.22f, 0.24f, 0.27f); // un poco azulado: ambiente frío de apagón

        // Sondas de luz: guardan la luz horneada en el aire para iluminar lo que se mueve
        // (cajones, llave, destornillador, puerta), que no puede tener la luz pintada encima.
        LightProbeGroup sondas = new GameObject("Sondas_Luz").AddComponent<LightProbeGroup>();
        sondas.transform.SetParent(g, false);
        var posiciones = new System.Collections.Generic.List<Vector3>();
        float[] horizontales = { -1.6f, -0.55f, 0.55f, 1.6f };
        float[] alturas = { 0.4f, 1.2f, 2.2f };
        foreach (float x in horizontales)
            foreach (float z in horizontales)
                foreach (float y in alturas)
                    posiciones.Add(new Vector3(x, y, z));
        sondas.probePositions = posiciones.ToArray();

        // Sonda de reflejos horneada: le da reflejos del cuarto a lo metálico (destornillador, extintor, estante)
        ReflectionProbe reflejos = new GameObject("Sonda_Reflejos").AddComponent<ReflectionProbe>();
        reflejos.transform.SetParent(g, false);
        reflejos.transform.localPosition = new Vector3(0, 1.3f, 0);
        reflejos.mode = UnityEngine.Rendering.ReflectionProbeMode.Baked;
        reflejos.size = new Vector3(4, 2.6f, 4);
        reflejos.resolution = 128; // baja a propósito, por el Quest
    }

    // ================================================================ Mostrador

    static void ConstruirMostrador(Transform g)
    {
        // Mostrador de 2 m pegado al muro oeste. El frente (norte) mira al jugador cuando entra;
        // por detrás (sur) están los cajones: hay que rodearlo por el lado este para abrirlos.
        Caja("Tablero", g, new Vector3(-1f, 1.075f, -0.3f), new Vector3(2, 0.05f, 0.8f), matMadera);
        Caja("Frente", g, new Vector3(-1f, 0.525f, 0.075f), new Vector3(2, 1.05f, 0.05f), matFrenteMostrador);
        // Los lados terminan justo detrás del frente (no lo atraviesan) para que no compartan cara con él
        Caja("Lado_Izq", g, new Vector3(-1.975f, 0.525f, -0.325f), new Vector3(0.05f, 1.05f, 0.75f), matFrenteMostrador);
        Caja("Lado_Der", g, new Vector3(-0.025f, 0.525f, -0.325f), new Vector3(0.05f, 1.05f, 0.75f), matFrenteMostrador);

        // Relleno invisible: el mostrador es hueco por dentro y un objeto empujado contra el frente podía colarse
        // adentro y "desaparecer". Estas cajas sin dibujo lo vuelven macizo, sin tapar los cajones
        // (que van de 0.84 a 1.02 m de alto y de z -0.71 a -0.11, en x -1.82..-1.28, -1.22..-0.68 y -0.62..-0.08).
        Relleno("Relleno_Debajo_Cajones", g, new Vector3(-1f, 0.415f, -0.325f), new Vector3(1.9f, 0.83f, 0.75f));
        Relleno("Relleno_Detras_Cajones", g, new Vector3(-1f, 0.94f, -0.025f), new Vector3(1.9f, 0.22f, 0.15f));
        Relleno("Relleno_Oeste", g, new Vector3(-1.885f, 0.94f, -0.4f), new Vector3(0.13f, 0.22f, 0.6f));
        Relleno("Relleno_Entre_Cajones_1", g, new Vector3(-1.25f, 0.94f, -0.4f), new Vector3(0.06f, 0.22f, 0.6f));
        Relleno("Relleno_Entre_Cajones_2", g, new Vector3(-0.65f, 0.94f, -0.4f), new Vector3(0.06f, 0.22f, 0.6f));
        Relleno("Relleno_Este", g, new Vector3(-0.065f, 0.94f, -0.4f), new Vector3(0.03f, 0.22f, 0.6f));
        Caja("Repisa", g, new Vector3(-1f, 0.35f, -0.3f), new Vector3(1.9f, 0.03f, 0.7f), matMadera);
        Caja("Cajonera_Base", g, new Vector3(-0.95f, 0.825f, -0.325f), new Vector3(1.8f, 0.02f, 0.75f), matMadera);

        // Televisor viejo en el extremo oeste del tablero. El modelo tiene el origen en su base y la pantalla
        // hacia +Z: queda mirando a la entrada.
        if (Modelo("Television_01", g, new Vector3(-1.68f, 1.1f, -0.42f), Vector3.zero, 1f, true) == null)
        {
            Caja("Monitor_Base", g, new Vector3(-1.7f, 1.11f, -0.45f), new Vector3(0.2f, 0.02f, 0.15f), matNegro);
            Caja("Monitor", g, new Vector3(-1.7f, 1.28f, -0.45f), new Vector3(0.5f, 0.32f, 0.04f), matNegro);
        }

        // Libretas y útiles del conserje. Los modelos traen sets de varias piezas: se toma una pieza de cada uno.
        // Solo decoran (no se pueden agarrar) y van del lado del conserje para no tapar la nota.
        if (ModeloPieza("office_notepads", "office_notepads_yellow_pad", g, new Vector3(-0.85f, 1.1f, -0.5f), new Vector3(0, 15, 0)) == null)
            Caja("Papeles", g, new Vector3(-0.85f, 1.12f, -0.5f), new Vector3(0.22f, 0.04f, 0.3f), matPapel);
        ModeloPieza("office_notepads", "office_notepads_a4_stack", g, new Vector3(-0.4f, 1.1f, -0.48f), new Vector3(0, -8, 0));
        // Los lápices vienen parados: con -90° en X quedan acostados sobre el tablero
        ModeloPieza("stationery_supplies", "stationery_supplies_pen_blue", g, new Vector3(-0.55f, 1.105f, -0.28f), new Vector3(-90, 10, 0));
        ModeloPieza("stationery_supplies", "stationery_supplies_pencil_old", g, new Vector3(-0.2f, 1.105f, -0.33f), new Vector3(-90, 75, 0));

        // Silla del conserje en la esquina, lejos de los cajones para que no estorbe al abrirlos
        Silla("Silla_Conserje", g, new Vector3(-1.55f, 0, -1.6f), 180);

        // Cartel en el muro sur: se ve desde la entrada, por encima del mostrador
        Cartel("Letrero_Conserjeria", g, "CONSERJERÍA", new Vector3(-0.9f, 2.2f, -2f), 180, 1.3f, 0.26f);

        ConstruirNota(g);
        ConstruirContestador(g);
        ConstruirTimbre(g);
        ConstruirCajones(Grupo("Cajones", g));
    }

    static void ConstruirNota(Transform g)
    {
        // Nota con la primera pista: es lo primero que el jugador puede agarrar.
        // Girada 180° para que se lea desde la entrada (norte).
        GameObject nota = Agarrable("Nota_Pista", g, new Vector3(-0.75f, 1.105f, -0.12f), new Vector3(0.15f, 0.01f, 0.21f));
        // Se toma por el borde de abajo y queda parada con el texto mirando al jugador, lista para leer
        PuntoDeAgarre(nota, new Vector3(0, 0, -0.09f), Vector3.down, Vector3.forward);
        nota.transform.localRotation = Quaternion.Euler(0, 180, 0);
        SinCollider(Caja("Hoja", nota.transform, Vector3.zero, new Vector3(0.15f, 0.01f, 0.21f), matPapel));
        // Rotado 90° en X para que el texto quede acostado sobre la hoja, mirando hacia arriba
        Texto("Texto", nota.transform,
            "TURNO NOCHE\n\nTe dejé un mensaje de voz en la computadora de portería.\n\n- Conserje",
            new Vector3(0, 0.006f, 0), new Vector3(90, 0, 0), new Vector2(0.13f, 0.19f), TintaOscura);
    }

    static void ConstruirContestador(Transform g)
    {
        // Computadora de portería con un buzón de voz en pantalla (modelo classic_laptop de Poly Haven).
        // Mira al jugador (norte, +Z). Al presionar la tecla PLAY la pantalla cambia y muestra el mensaje.
        Transform c = Grupo("Contestador", g);
        c.localPosition = new Vector3(-1.2f, 1.1f, -0.2f);

        // El modelo mide 65 cm de ancho (el doble de una laptop real): al 50 % queda de 33 cm.
        // Su pantalla es plana y vertical: al 50 % está a z = -0.1034, mide 22 x 18 cm y su centro está a 16.5 cm de alto.
        // Sin collider: una caja alrededor de la laptop taparía la tecla PLAY y el rayo no la alcanzaría
        GameObject laptop = Modelo("classic_laptop", c, Vector3.zero, Vector3.zero, 0.5f);
        if (laptop != null)
        {
            // La pantalla del modelo trae texto antiguo pintado: se cambia por la pantalla oscura del buzón
            foreach (Renderer r in laptop.GetComponentsInChildren<Renderer>())
            {
                Material[] mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                    if (mats[i] != null && mats[i].name.Contains("screen")) mats[i] = matPantallaApagada;
                r.sharedMaterials = mats;
            }
        }
        else
        {
            // Sin el modelo: base y pantalla gray box en el mismo lugar
            Caja("Cuerpo", c, new Vector3(0, 0.0375f / 2, 0), new Vector3(0.33f, 0.0375f, 0.24f), matNegro);
            SinCollider(Caja("Pantalla", c, new Vector3(0, 0.165f, -0.108f), new Vector3(0.224f, 0.183f, 0.01f), matPantallaApagada));
        }

        // Interfaz del buzón sobre la pantalla. Dentro del grupo el jugador mira hacia -Z, así que los textos
        // van girados 180°. Capas: pantalla en z = -0.1034, líneas 1 mm delante y textos 2 mm delante.
        // Todo lo que se ve con la computadora encendida va en "Pantalla_Encendida", que empieza apagado
        // y lo activa la palanca del tablero. Primero, el fondo iluminado de la pantalla (0.25 mm delante del vidrio).
        const float zLinea = -0.1024f, zTexto = -0.1014f;
        Transform encendida = Grupo("Pantalla_Encendida", c);
        SinCollider(Caja("Fondo_Encendido", encendida, new Vector3(0, 0.165f, -0.1029f), new Vector3(0.224f, 0.183f, 0.0005f), matPantalla));
        Texto("Buzon_Titulo", encendida, "BUZÓN DE VOZ", new Vector3(0, 0.243f, zTexto), TextoMuroSur, new Vector2(0.2f, 0.014f), TintaGris);
        SinCollider(Caja("Buzon_Linea", encendida, new Vector3(0, 0.233f, zLinea), new Vector3(0.2f, 0.002f, 0.001f), matAcento));

        // Estado "mensaje sin escuchar": punto ámbar, aviso grande e indicación
        Transform espera = Grupo("Pantalla_Espera", encendida);
        SinCollider(Primitiva(PrimitiveType.Cylinder, "Punto", espera, new Vector3(-0.085f, 0.19f, zLinea), new Vector3(0.012f, 0.0005f, 0.012f), matAcento, new Vector3(90, 0, 0)));
        Texto("Aviso", espera, "1 MENSAJE NUEVO", new Vector3(0.01f, 0.19f, zTexto), TextoMuroSur, new Vector2(0.17f, 0.028f), TintaBlanca);
        Texto("Indicacion", espera, "Presiona PLAY (arriba del teclado)", new Vector3(0, 0.14f, zTexto), TextoMuroSur, new Vector2(0.19f, 0.016f), TintaAmbar);

        // Estado "mensaje escuchado": quién lo dejó, el mensaje y una barra de reproducción
        Transform mensaje = Grupo("Pantalla_Mensaje", encendida);
        Texto("De", mensaje, "DE: VIGILANCIA  ·  TURNO NOCHE", new Vector3(0, 0.215f, zTexto), TextoMuroSur, new Vector2(0.2f, 0.012f), TintaAmbar);
        Texto("Mensaje", mensaje,
            "La copia de la llave de la dirección está en el cajón con el escudo del colegio. La tapa de la cerradura sigue atornillada.",
            new Vector3(0, 0.155f, zTexto), TextoMuroSur, new Vector2(0.2f, 0.09f), TintaBlanca);
        SinCollider(Caja("Barra_Fondo", mensaje, new Vector3(0, 0.095f, zLinea), new Vector3(0.18f, 0.004f, 0.001f), matGrisClaro));
        SinCollider(Caja("Barra_Avance", mensaje, new Vector3(0.03f, 0.095f, zLinea + 0.0005f), new Vector3(0.12f, 0.004f, 0.001f), matAcento));
        mensaje.gameObject.SetActive(false);

        // Botón PLAY integrado en la franja de botones de la laptop (entre la pantalla y el teclado), como los
        // botones multimedia de una laptop real. La franja es plana: al 50 % queda a 3.73 cm de alto.
        // A su lado, un LED ámbar avisa que hay un mensaje sin escuchar.
        // Lo que hace se conecta en el evento "alPresionar" (se ve en el Inspector del botón).
        Transform play = Grupo("Boton_Play", c);
        play.localPosition = new Vector3(-0.11f, 0.0373f, -0.072f);
        Transform tecla = Grupo("Tecla", play); // la tapa y la palabra PLAY se hunden juntas al presionar
        SinCollider(Caja("Tapa", tecla, new Vector3(0, 0.0015f, 0), new Vector3(0.022f, 0.003f, 0.011f), matPlastico));
        Texto("Etiqueta_Play", tecla, "PLAY", new Vector3(0, 0.0031f, 0), new Vector3(90, 180, 0), new Vector2(0.018f, 0.006f), TintaAmbar);
        // El LED también es parte de lo "encendido": sin energía no brilla
        GameObject led = SinCollider(Caja("LED", encendida, new Vector3(-0.127f, 0.0378f, -0.072f), new Vector3(0.004f, 0.001f, 0.004f), matAcento));
        PressableButton boton = Boton(play.gameObject, tecla, new Vector3(0, 0.008f, 0), new Vector3(0.034f, 0.02f, 0.022f));
        boton.recorrido = 0.002f;
        UnityEventTools.AddBoolPersistentListener(boton.alPresionar, espera.gameObject.SetActive, false);
        UnityEventTools.AddBoolPersistentListener(boton.alPresionar, mensaje.gameObject.SetActive, true);
        UnityEventTools.AddBoolPersistentListener(boton.alPresionar, led.SetActive, false);

        encendida.gameObject.SetActive(false);
        pantallaEncendida = encendida.gameObject;
    }

    static void ConstruirTimbre(Transform g)
    {
        // Timbre de portería: solo suena. Es un botón "seguro" para aprender a presionar.
        Transform t = Grupo("Timbre", g);
        t.localPosition = new Vector3(-0.3f, 1.1f, -0.12f);
        SinCollider(Primitiva(PrimitiveType.Cylinder, "Base", t, new Vector3(0, 0.01f, 0), new Vector3(0.08f, 0.01f, 0.08f), matNegro));
        SinCollider(Primitiva(PrimitiveType.Sphere, "Campana", t, new Vector3(0, 0.035f, 0), new Vector3(0.07f, 0.035f, 0.07f), matMetal));
        GameObject pulsador = SinCollider(Primitiva(PrimitiveType.Cylinder, "Pulsador", t, new Vector3(0, 0.075f, 0), new Vector3(0.012f, 0.008f, 0.012f), matMetal));
        Boton(t.gameObject, pulsador.transform, new Vector3(0, 0.045f, 0), new Vector3(0.08f, 0.07f, 0.08f));
    }

    static void ConstruirCajones(Transform g)
    {
        // Tres cajones bajo el tablero, del lado del conserje. Cada uno tiene un símbolo en el frente:
        // rombo = llave (pista: el escudo del colegio), cuadrado = destornillador, círculo = señuelo.
        // El de la llave va en el extremo este, el más fácil de alcanzar; el señuelo queda junto a la silla.
        Transform rombo = Cajon("Cajon_Rombo", g, -0.35f);
        SinCollider(Caja("Simbolo", rombo, new Vector3(0, -0.045f, 0.315f), new Vector3(0.036f, 0.036f, 0.004f), matAzul, new Vector3(0, 0, 45)));
        GameObject llave = Agarrable("Llave", rombo, new Vector3(0, -0.06f, 0), new Vector3(0.165f, 0.02f, 0.05f), true);
        // Se toma por la cabeza, con la punta hacia adelante y la hoja vertical (como para meterla en la cerradura)
        PuntoDeAgarre(llave, new Vector3(-0.05f, 0, 0), Vector3.right, Vector3.forward);
        llaveCuarto1 = llave.GetComponent<XRGrabInteractable>();
        // Al soltarla junto a la cerradura, viaja suave hasta su lugar en vez de aparecer de golpe
        llaveCuarto1.attachEaseInTime = 0.3f;
        // En el PC: se agarra con un clic y queda derecha, mirando al frente, lista para entrar en la cerradura
        llave.AddComponent<HerramientaEnMano>();
        llave.GetComponent<BoxCollider>().center = new Vector3(0.005f, 0, 0);
        SinCollider(Caja("Cabeza", llave.transform, new Vector3(-0.05f, 0, 0), new Vector3(0.05f, 0.015f, 0.05f), matLaton));
        SinCollider(Caja("Vastago", llave.transform, new Vector3(0.03f, 0, 0), new Vector3(0.11f, 0.012f, 0.015f), matLaton));
        SinCollider(Caja("Diente", llave.transform, new Vector3(0.07f, 0, 0.015f), new Vector3(0.02f, 0.012f, 0.02f), matLaton));

        Transform cuadrado = Cajon("Cajon_Cuadrado", g, -0.95f);
        SinCollider(Caja("Simbolo", cuadrado, new Vector3(0, -0.045f, 0.315f), new Vector3(0.045f, 0.045f, 0.004f), matVerde));
        ConstruirDestornillador(cuadrado);

        Transform circulo = Cajon("Cajon_Circulo", g, -1.55f);
        SinCollider(Primitiva(PrimitiveType.Cylinder, "Simbolo", circulo, new Vector3(0, -0.045f, 0.316f), new Vector3(0.05f, 0.002f, 0.05f), matRojo, new Vector3(90, 0, 0)));
        SinCollider(Caja("Hojas", circulo, new Vector3(-0.1f, -0.067f, 0), new Vector3(0.2f, 0.006f, 0.28f), matPapel));
        GameObject sello = Agarrable("Sello", circulo, new Vector3(0.12f, -0.03f, 0), new Vector3(0.05f, 0.08f, 0.05f), true);
        PuntoDeAgarre(sello, new Vector3(0, 0.01f, 0), Vector3.forward, Vector3.up); // se toma por el mango
        SinCollider(Caja("Base", sello.transform, new Vector3(0, -0.03f, 0), new Vector3(0.05f, 0.02f, 0.05f), matNegro));
        SinCollider(Primitiva(PrimitiveType.Cylinder, "Mango", sello.transform, new Vector3(0, 0.01f, 0), new Vector3(0.03f, 0.03f, 0.03f), matRojo));
    }

    // Cajón vacío que se abre jalando. Empieza cerrado: su frente queda en el borde trasero del mostrador (z = -0.7).
    // Está girado 180° para que su "adelante" (+Z local, hacia donde se abre) apunte al sur, detrás del mostrador.
    static Transform Cajon(string nombre, Transform padre, float x)
    {
        Transform cajon = Grupo(nombre, padre);
        cajon.localPosition = new Vector3(x, 0.93f, -0.4f);
        cajon.localRotation = Quaternion.Euler(0, 180, 0);

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
        cajon.gameObject.AddComponent<ResaltarAlApuntar>();
        return cajon;
    }

    static void ConstruirDestornillador(Transform cajon)
    {
        // Herramienta. Lo que quita los tornillos es SOLO el collider de la punta.
        GameObject d = Agarrable("Destornillador", cajon, new Vector3(0, -0.054f, 0), new Vector3(0.22f, 0.032f, 0.032f), true);
        // Se toma por el mango con la punta hacia adelante: así se apunta a los tornillos como con uno real
        PuntoDeAgarre(d, new Vector3(-0.06f, 0, 0), Vector3.right, Vector3.up);

        // El modelo viene parado con la punta hacia arriba (+Y). Se acuesta con -90° en Z para que la punta
        // quede hacia +X, donde está el collider "Punta", y se corre 3 cm para centrarlo en el collider.
        GameObject modelo = Modelo("screwdriver", d.transform, new Vector3(-0.03f, 0, 0), new Vector3(0, 0, -90));
        if (modelo == null)
        {
            // Sin el modelo: versión gray box, dentro de un grupo para que gire entera
            modelo = Grupo("Visual", d.transform).gameObject;
            SinCollider(Primitiva(PrimitiveType.Cylinder, "Mango", modelo.transform, new Vector3(-0.06f, 0, 0), new Vector3(0.032f, 0.05f, 0.032f), matAmarillo, new Vector3(0, 0, 90)));
            SinCollider(Primitiva(PrimitiveType.Cylinder, "Varilla", modelo.transform, new Vector3(0.05f, 0, 0), new Vector3(0.008f, 0.06f, 0.008f), matMetal, new Vector3(0, 0, 90)));
        }
        // Mide cuánto se gira el destornillador (muñeca en el Quest, mouse en círculos en el PC).
        // Mientras saca un tornillo, el modelo gira sobre su eje; el collider y la punta no se mueven.
        d.AddComponent<Destornillador>().visual = modelo.transform;
        // En el PC: se agarra con un clic y queda derecho, con la punta al frente, apuntando a los tornillos
        d.AddComponent<HerramientaEnMano>();

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
        // La bisagra es un objeto vacío en el borde oeste del hueco de la puerta (muro sur).
        // Door.cs girará este objeto: todo lo que es parte de la puerta va dentro para girar con ella.
        Transform bisagra = Grupo("Puerta_Bisagra", g);
        bisagra.localPosition = new Vector3(0.7f, 0, -2.1f);

        // Puerta lisa de madera clara, la misma del pasamanos y del mostrador: moderna y del color del cuarto
        Caja("Puerta", bisagra, new Vector3(0.5f, 1.05f, 0), new Vector3(0.98f, 2.08f, 0.06f), matMadera);

        // Door.cs la abre sola cuando se resuelve el acertijo de la llave. Con +90° la hoja gira hacia -Z:
        // se abre hacia la Dirección, alejándose del jugador.
        // Primero se suelta el pestillo (se despega 4°), una pausa corta y se abre frenando al final.
        puertaDireccion = bisagra.gameObject.AddComponent<Door>();
        puertaDireccion.anguloAbierto = 90f;
        puertaDireccion.anguloPestillo = 4f;
        puertaDireccion.pausa = 0.35f;
        puertaDireccion.duracion = 2.2f;
        ConstruirPasilloTemporal(g);

        // Ventanilla vertical de vidrio oscuro con marco antracita, del lado de la manija.
        // La cara de la puerta está a 3 cm del centro: el vidrio sobresale 2 mm y el marco 4 mm (así no parpadean).
        SinCollider(Caja("Ventanilla_Vidrio", bisagra, new Vector3(0.72f, 1.5f, 0), new Vector3(0.1f, 0.8f, 0.064f), matVidrio));
        SinCollider(Caja("Ventanilla_Marco_Arriba", bisagra, new Vector3(0.72f, 1.906f, 0), new Vector3(0.124f, 0.012f, 0.068f), matCartel));
        SinCollider(Caja("Ventanilla_Marco_Abajo", bisagra, new Vector3(0.72f, 1.094f, 0), new Vector3(0.124f, 0.012f, 0.068f), matCartel));
        SinCollider(Caja("Ventanilla_Marco_Izq", bisagra, new Vector3(0.664f, 1.5f, 0), new Vector3(0.012f, 0.8f, 0.068f), matCartel));
        SinCollider(Caja("Ventanilla_Marco_Der", bisagra, new Vector3(0.776f, 1.5f, 0), new Vector3(0.012f, 0.8f, 0.068f), matCartel));

        // Manija de barra larga (70 cm) en metal oscuro, del lado del cuarto (+Z), sobre dos soportes redondos
        Primitiva(PrimitiveType.Cylinder, "Manija", bisagra, new Vector3(0.9f, 1.0f, 0.08f), new Vector3(0.028f, 0.35f, 0.028f), matMetalOscuro);
        SinCollider(Primitiva(PrimitiveType.Cylinder, "Manija_Soporte_Arriba", bisagra, new Vector3(0.9f, 1.25f, 0.05f), new Vector3(0.016f, 0.025f, 0.016f), matMetalOscuro, new Vector3(90, 0, 0)));
        SinCollider(Primitiva(PrimitiveType.Cylinder, "Manija_Soporte_Abajo", bisagra, new Vector3(0.9f, 0.75f, 0.05f), new Vector3(0.016f, 0.025f, 0.016f), matMetalOscuro, new Vector3(90, 0, 0)));

        // Marco antracita alrededor del hueco, como el zócalo y los carteles (no se mueve: está fuera de la bisagra).
        // Ninguna cara del marco coincide con una cara del muro, del dintel o del zócalo:
        // los laterales entran 1 mm en el hueco y el travesaño queda 1 cm por debajo del dintel.
        Caja("Marco_Izq", g, new Vector3(0.676f, 1.07f, -2.1f), new Vector3(0.05f, 2.14f, 0.25f), matCartel);
        Caja("Marco_Der", g, new Vector3(1.724f, 1.07f, -2.1f), new Vector3(0.05f, 2.14f, 0.25f), matCartel);
        Caja("Marco_Arriba", g, new Vector3(1.2f, 2.115f, -2.1f), new Vector3(0.998f, 0.05f, 0.25f), matCartel);

        Cartel("Letrero_Direccion", g, "DIRECCIÓN", new Vector3(1.2f, 2.27f, -2f), 180, 0.8f, 0.2f);

        // Lámpara de emergencia enjaulada al lado de la puerta. No cabe encima de la puerta junto al cartel.
        // El modelo tiene la parte trasera en el origen y mira a +Z, que en el muro sur ya es hacia el cuarto:
        // no se gira. Se separa 2 mm del muro: si su trasera quedara justo en la cara del muro, las dos
        // superficies pelearían por dibujarse y la lámpara parpadearía al moverse (z-fighting).
        GameObject lamparaPared = Modelo("industrial_wall_lamp", g, new Vector3(1.86f, 2.36f, -1.998f), Vector3.zero);
        if (lamparaPared != null) SinSombra(lamparaPared);
        else SinCollider(Caja("Lampara_Emergencia", g, new Vector3(1.2f, 2.47f, -1.94f), new Vector3(0.4f, 0.1f, 0.12f), matLuzAmbar));

        ConstruirPanelAcceso(g);
    }

    // Pasillo corto detrás de la puerta, con el mismo piso, paredes y techo del cuarto. Al abrirse la puerta
    // se enciende su luz (con parpadeo): así se ve la hoja de la puerta girar hacia el pasillo, en vez de
    // perderse en un hueco negro. Es TEMPORAL: se reemplaza cuando se construya el Cuarto 2 (Dirección).
    // No se puede entrar (no tiene piso de teletransporte).
    static void ConstruirPasilloTemporal(Transform g)
    {
        Transform p = Grupo("Pasillo_Temporal", g);
        // El piso empieza justo donde termina el del cuarto (z = -2): también cubre el umbral bajo la puerta.
        // Se tocan borde con borde, sin encimarse, así no parpadean.
        Caja("Piso", p, new Vector3(1.2f, -0.05f, -2.7f), new Vector3(1.4f, 0.1f, 1.4f), matPiso);
        Caja("Techo", p, new Vector3(1.2f, 2.35f, -2.8f), new Vector3(1.4f, 0.1f, 1.2f), matTecho);
        Caja("Pared_Oeste", p, new Vector3(0.55f, 1.15f, -2.8f), new Vector3(0.1f, 2.3f, 1.2f), matPared);
        Caja("Pared_Este", p, new Vector3(1.85f, 1.15f, -2.8f), new Vector3(0.1f, 2.3f, 1.2f), matPared);
        Caja("Pared_Sur", p, new Vector3(1.2f, 1.15f, -3.45f), new Vector3(1.4f, 2.3f, 0.1f), matPared);

        // Alcance corto: ilumina el pasillo sin sumar una tercera luz a los objetos del cuarto (el Quest usa 2 por objeto)
        GameObject luz = Luz("Luz_Pasillo", p, new Vector3(1.2f, 2.0f, -2.85f), new Color(0.85f, 0.92f, 1f), 1.6f, 2.2f, false);
        luz.AddComponent<EncendidoConParpadeo>();
        luz.SetActive(false);
        UnityEventTools.AddBoolPersistentListener(puertaDireccion.alAbrirse, luz.SetActive, true);
    }

    // Panel de acceso moderno junto a la puerta, a la altura de la mano (zona cómoda del documento: 0.9 a 1.3 m).
    // Arriba dice ACCESO y muestra que está BLOQUEADO. Abajo, una tapa de acero con la palabra LLAVE y dos
    // tornillos dorados esconde el ojo de la cerradura: es lo que el jugador abre con el destornillador.
    // El panel mide 16 x 40 cm y su frente queda a 3 cm del muro (z = -1.97). Lo de encima va 1 mm por delante.
    static void ConstruirPanelAcceso(Transform g)
    {
        Caja("Cerradura", g, new Vector3(0.4f, 1.25f, -1.985f), new Vector3(0.16f, 0.4f, 0.03f), matPanel);
        Texto("Cerradura_Titulo", g, "ACCESO", new Vector3(0.4f, 1.425f, -1.969f), TextoMuroSur, new Vector2(0.12f, 0.022f), TintaBlanca);
        GameObject tira;
        GameObject estado;

        // Barra de estado: roja = bloqueado. KeyPuzzle la cambiará a verde (y el texto a ABIERTO) al acertar.
        tira = SinCollider(Caja("Luz_Indicador", g, new Vector3(0.4f, 1.4f, -1.969f), new Vector3(0.1f, 0.008f, 0.002f), matLuzRoja));
        estado = Texto("Cerradura_Estado", g, "BLOQUEADO", new Vector3(0.4f, 1.375f, -1.969f), TextoMuroSur, new Vector2(0.12f, 0.018f), TintaRoja);
        // Línea ámbar abajo, igual que en los carteles
        SinCollider(Caja("Cerradura_Linea", g, new Vector3(0.4f, 1.056f, -1.969f), new Vector3(0.16f, 0.012f, 0.002f), matAcento));

        // Ojo de la cerradura (escondido debajo de la tapa): aro de acero, círculo y ranura negros
        SinCollider(Primitiva(PrimitiveType.Cylinder, "Cerradura_Aro", g, new Vector3(0.4f, 1.2f, -1.9685f), new Vector3(0.036f, 0.0015f, 0.036f), matMetal, new Vector3(90, 0, 0)));
        SinCollider(Primitiva(PrimitiveType.Cylinder, "Cerradura_Ojo", g, new Vector3(0.4f, 1.205f, -1.9665f), new Vector3(0.012f, 0.0005f, 0.012f), matNegro, new Vector3(90, 0, 0)));
        SinCollider(Caja("Cerradura_Ranura", g, new Vector3(0.4f, 1.193f, -1.9665f), new Vector3(0.006f, 0.016f, 0.001f), matNegro));

        // Indicador: aro ámbar que brilla alrededor del ojo de la cerradura cuando se cae la tapa ("la llave va aquí").
        // Es un disco de 1 mm detrás del aro de acero: solo se ve el borde. Su frente queda 2 mm delante del panel.
        GameObject indicador = SinCollider(Primitiva(PrimitiveType.Cylinder, "Indicador_Llave", g, new Vector3(0.4f, 1.2f, -1.9685f), new Vector3(0.056f, 0.0005f, 0.056f), matLuzAmbar, new Vector3(90, 0, 0)));
        indicador.SetActive(false);

        // Encaje de la llave (XRSocketInteractor). El encaje coloca el punto de agarre de la llave (su cabeza)
        // aquí, 7 cm delante del panel, y lo gira 180° para que la punta apunte al muro con la hoja vertical.
        // Después KeyPuzzle la mete 3 cm más (la cabeza queda a 4 cm) y la gira.
        // La zona es grande (10 cm): basta con soltar la llave cerca del aro ámbar.
        // Empieza APAGADO: se activa cuando se cae la tapa, así la llave no entra con la tapa puesta.
        GameObject socket = new GameObject("Socket_Llave");
        socket.transform.SetParent(g, false);
        socket.transform.localPosition = new Vector3(0.4f, 1.2f, -1.9f);
        socket.transform.localRotation = Quaternion.Euler(0, 180, 0);
        SphereCollider zonaEncaje = socket.AddComponent<SphereCollider>();
        zonaEncaje.isTrigger = true;
        zonaEncaje.radius = 0.1f;
        socket.AddComponent<XRSocketInteractor>();

        // El acertijo: solo la llave del cajón encaja; al entrar, luz verde, ABIERTO, sonido y se abre la puerta
        KeyPuzzle acertijo = socket.AddComponent<KeyPuzzle>();
        acertijo.datos = DatosAcertijoCuarto1();
        acertijo.llave = llaveCuarto1;
        acertijo.tiraDeLuz = tira.GetComponent<Renderer>();
        acertijo.materialVerde = matLuzVerde;
        acertijo.textoEstado = estado.GetComponent<TMP_Text>();
        acertijo.luzCerradura = luzCerradura;
        acertijo.sonidoAcierto = sonidoAcierto;
        acertijo.sonidoGiro = sonidoClic; // clic de la llave al terminar de girar
        acertijo.indicador = indicador;
        acertijo.profundidadEntrada = 0.03f;
        UnityEventTools.AddVoidPersistentListener(acertijo.alResolverse, puertaDireccion.Abrir);
        socket.SetActive(false);

        // Tapa de acero atornillada encima del ojo. Es un objeto vacío con collider, Rigidbody y ScrewedPanel;
        // la placa y la palabra LLAVE son hijas, así el texto no se deforma con la escala de la placa.
        // Empieza kinematic: no se cae hasta quitar los 2 tornillos. Girada 180° para que su "adelante"
        // apunte al muro: ScrewedPanel la empuja hacia atrás, o sea, hacia el cuarto.
        GameObject tapa = new GameObject("Tapa_Cerradura");
        tapa.transform.SetParent(g, false);
        tapa.transform.localPosition = new Vector3(0.4f, 1.2f, -1.966f);
        tapa.transform.localRotation = Quaternion.Euler(0, 180, 0);
        tapa.AddComponent<BoxCollider>().size = new Vector3(0.12f, 0.16f, 0.008f);
        tapa.AddComponent<Rigidbody>().isKinematic = true;
        SinCollider(Caja("Placa", tapa.transform, Vector3.zero, new Vector3(0.12f, 0.16f, 0.008f), matAluminio));
        // Dentro de la tapa (girada 180°) el cuarto queda hacia -Z: el texto va medio milímetro por delante de la placa
        Texto("Etiqueta", tapa.transform, "LLAVE", new Vector3(0, 0, -0.0045f), Vector3.zero, new Vector2(0.08f, 0.024f), TintaOscura);

        ScrewedPanel panel = tapa.AddComponent<ScrewedPanel>();
        panel.tornillosRestantes = 2;
        UnityEventTools.AddBoolPersistentListener(panel.alSoltarse, socket.SetActive, true); // al caer la tapa, ya se puede meter la llave
        UnityEventTools.AddBoolPersistentListener(panel.alSoltarse, indicador.SetActive, true); // y brilla el aro ámbar
        Tornillo("Tornillo_Arriba", g, new Vector3(0.4f, 1.255f, -1.9585f), panel);
        Tornillo("Tornillo_Abajo", g, new Vector3(0.4f, 1.145f, -1.9585f), panel);
    }

    // Asset con los datos del acertijo del Cuarto 1 (ScriptableObject). Si ya existe no se toca,
    // así lo que el equipo cambie en el Inspector no se pierde al reconstruir.
    static PuzzleData DatosAcertijoCuarto1()
    {
        const string carpeta = "Assets/ScriptableObjects/Puzzles";
        const string ruta = carpeta + "/Cuarto1_Llave.asset";
        if (!AssetDatabase.IsValidFolder("Assets/ScriptableObjects")) AssetDatabase.CreateFolder("Assets", "ScriptableObjects");
        if (!AssetDatabase.IsValidFolder(carpeta)) AssetDatabase.CreateFolder("Assets/ScriptableObjects", "Puzzles");

        PuzzleData datos = AssetDatabase.LoadAssetAtPath<PuzzleData>(ruta);
        if (datos != null) return datos;

        datos = ScriptableObject.CreateInstance<PuzzleData>();
        datos.id = "cuarto1_llave";
        datos.nombreCuarto = "Recepción";
        datos.pista = "La llave está en el cajón con el escudo del colegio (rombo azul). La tapa de la cerradura está atornillada: el destornillador está en el cajón del cuadrado.";
        datos.tipo = TipoAcertijo.Llave;
        datos.solucion = "llave_direccion";
        datos.mensajeAcierto = "ABIERTO";
        datos.mensajeError = "BLOQUEADO";
        AssetDatabase.CreateAsset(datos, ruta);
        return datos;
    }

    static void Tornillo(string nombre, Transform padre, Vector3 pos, ScrewedPanel tapa)
    {
        // Cabeza dorada de 2.4 cm: resalta sobre el acero de la tapa
        GameObject t = SinCollider(Primitiva(PrimitiveType.Cylinder, nombre, padre, pos, new Vector3(0.024f, 0.003f, 0.024f), matLaton, new Vector3(90, 0, 0)));

        // Ranura negra de la cabeza. Es hija del tornillo (se cae con él), así que su escala se multiplica
        // por la del tornillo: 0.75 x 2.4 cm = 1.8 cm de largo. El eje Y del cilindro apunta hacia el cuarto.
        SinCollider(Primitiva(PrimitiveType.Cube, "Ranura", t.transform, new Vector3(0, 0.5f, 0), new Vector3(0.75f, 1.5f, 0.15f), matNegro));

        // Zona de contacto más grande que el tornillo para que sea fácil acertarle en VR
        SphereCollider zona = t.AddComponent<SphereCollider>();
        zona.isTrigger = true;
        zona.radius = 2f; // en escala local: 2 x 0.024 m = 4.8 cm de radio (los tornillos están a 11 cm: no se pisan)

        Screw screw = t.AddComponent<Screw>();
        screw.puntaHerramienta = puntaDestornillador;
        screw.tapa = tapa;
        screw.sonido = sonidoClic;
    }

    // ================================================================ Pistas para mirar

    static void ConstruirPistas(Transform g)
    {
        ConstruirCartelera(g);

        // Llavero en el muro sur, junto a la cerradura: el gancho de DIRECCIÓN dice qué llave hay que buscar.
        // Mirando al sur, la izquierda del jugador es +X: por eso los ganchos van de +X a -X.
        Caja("Llavero", g, new Vector3(-0.35f, 1.55f, -1.99f), new Vector3(0.5f, 0.35f, 0.02f), matMetalOscuro);
        Texto("Llavero_Titulo", g, "LLAVES", new Vector3(-0.35f, 1.685f, -1.977f), TextoMuroSur, new Vector2(0.3f, 0.05f), TintaBlanca);
        string[] ganchos = { "PORT.", "DIRECC.", "COMPUT.", "LAB." };
        for (int i = 0; i < ganchos.Length; i++)
        {
            float x = -0.17f - i * 0.12f;
            SinCollider(Primitiva(PrimitiveType.Cylinder, "Gancho_" + i, g, new Vector3(x, 1.58f, -1.96f), new Vector3(0.01f, 0.02f, 0.01f), matMetal, new Vector3(90, 0, 0)));
            Texto("Llavero_Etiqueta_" + i, g, ganchos[i], new Vector3(x, 1.47f, -1.977f), TextoMuroSur, new Vector2(0.11f, 0.035f), TintaBlanca);
        }

        // Cámara de seguridad en la esquina suroeste (detrás del mostrador), vigilando la entrada.
        // El modelo mira hacia +Z y su soporte queda atrás, pegado a la esquina.
        Transform camara = Grupo("Camara_Seguridad", g);
        camara.localPosition = new Vector3(-1.78f, 2.25f, -1.78f);
        camara.localRotation = Quaternion.Euler(15, 45, 0);
        if (Modelo("security_camera_01", camara, Vector3.zero, Vector3.zero) == null)
        {
            // Sin el modelo: versión gray box
            SinCollider(Caja("Cuerpo", camara, Vector3.zero, new Vector3(0.12f, 0.08f, 0.2f), matNegro));
            SinCollider(Primitiva(PrimitiveType.Sphere, "LED", camara, new Vector3(0.03f, 0.025f, 0.101f), Vector3.one * 0.012f, matLuzRoja));
        }
    }

    // ================================================================ Cartelera

    // Panel de anuncios de tela con marco de aluminio en el muro oeste, sobre las sillas de espera.
    // Se arma dentro de un grupo girado hacia el cuarto. Dentro del grupo: X = derecha, Y = arriba,
    // el muro está en z = 0 y lo que sobresale va hacia -Z (hacia el jugador).
    static void ConstruirCartelera(Transform padre)
    {
        Transform c = Grupo("Cartelera", padre);
        c.localPosition = new Vector3(-2f, 1.65f, 0.9f);
        c.localRotation = Quaternion.Euler(0, -90, 0); // se lee mirando al oeste

        // Tablero de tela de 1.4 x 0.9 m y marco de aluminio de 3 cm que sobresale un poco más que la tela
        Caja("Tablero", c, new Vector3(0, 0, -0.015f), new Vector3(1.4f, 0.9f, 0.03f), matTela);
        SinCollider(Caja("Marco_Arriba", c, new Vector3(0, 0.465f, -0.02f), new Vector3(1.46f, 0.03f, 0.04f), matAluminio));
        SinCollider(Caja("Marco_Abajo", c, new Vector3(0, -0.465f, -0.02f), new Vector3(1.46f, 0.03f, 0.04f), matAluminio));
        SinCollider(Caja("Marco_Izq", c, new Vector3(-0.715f, 0, -0.02f), new Vector3(0.03f, 0.9f, 0.04f), matAluminio));
        SinCollider(Caja("Marco_Der", c, new Vector3(0.715f, 0, -0.02f), new Vector3(0.03f, 0.9f, 0.04f), matAluminio));

        // Póster del colegio: el rombo azul del escudo es la pista del cajón correcto
        Transform poster = Hoja("Poster", c, new Vector2(-0.42f, 0.05f), 0.4f, 0.52f, -2f);
        Impreso(poster, "Encabezado", 0, 0.21f, 0.4f, 0.1f, matCartel);
        Texto("Titulo", poster, "COLEGIO ALBA", new Vector3(0, 0.21f, -0.004f), Vector3.zero, new Vector2(0.36f, 0.06f), TintaBlanca);
        Impreso(poster, "Escudo_Rombo", 0, 0.02f, 0.13f, 0.13f, matAzul, 45);
        Texto("Lema", poster, "Fundado en 1978", new Vector3(0, -0.15f, -0.004f), Vector3.zero, new Vector2(0.34f, 0.04f), TintaAzul);
        Impreso(poster, "Pie", 0, -0.245f, 0.4f, 0.03f, matAzul);
        Chincheta(poster, new Vector3(0, 0.235f, -0.008f), matRojo);

        // Plano de evacuación: los 4 cuartos en orden unidos por el pasillo, dónde está el jugador y la salida
        Transform plano = Hoja("Plano_Evacuacion", c, new Vector2(0.22f, 0.17f), 0.58f, 0.36f, 1.5f);
        Impreso(plano, "Encabezado", 0, 0.145f, 0.58f, 0.07f, matVerde);
        Texto("Titulo", plano, "PLANO DE EVACUACIÓN", new Vector3(0, 0.145f, -0.004f), Vector3.zero, new Vector2(0.52f, 0.05f), TintaBlanca);
        Impreso(plano, "Pasillo", 0, 0, 0.46f, 0.008f, matNegro);
        string[] cuartos = { "PORTERÍA", "DIRECCIÓN", "COMPUTACIÓN", "LABORATORIO" };
        for (int i = 0; i < cuartos.Length; i++)
        {
            float x = -0.2f + i * 0.133f;
            Material color = i == 0 ? matRojo : (i == cuartos.Length - 1 ? matVerde : matGrisClaro);
            // Los cuadros van una capa más adelante que el pasillo, que pasa por detrás
            Impreso(plano, "Cuarto_" + i, x, 0, 0.1f, 0.075f, color, 0, 0.0035f);
            Texto("Nombre_" + i, plano, cuartos[i], new Vector3(x, -0.06f, -0.005f), Vector3.zero, new Vector2(0.12f, 0.025f), TintaOscura);
        }
        Texto("UstedEstaAqui", plano, "USTED ESTÁ AQUÍ", new Vector3(-0.2f, -0.11f, -0.005f), Vector3.zero, new Vector2(0.14f, 0.025f), TintaRoja);
        Texto("Salida", plano, "SALIDA  >", new Vector3(0.2f, -0.11f, -0.005f), Vector3.zero, new Vector2(0.14f, 0.025f), TintaVerde);
        Chincheta(plano, new Vector3(0, 0.165f, -0.008f), matAmarillo);

        // Hoja de turnos: solo ambienta. Sin horas para no confundir con el código del Cuarto 2.
        Transform turnos = Hoja("Turnos", c, new Vector2(0.3f, -0.27f), 0.32f, 0.28f, -3f);
        Impreso(turnos, "Encabezado", 0, 0.11f, 0.32f, 0.06f, matAmarillo);
        Texto("Titulo", turnos, "TURNOS DE PORTERÍA", new Vector3(0, 0.11f, -0.004f), Vector3.zero, new Vector2(0.29f, 0.04f), TintaOscura);
        string[] filas = { "Mañana  ·  R. Díaz", "Tarde  ·  M. Soto", "Noche  ·  Vigilancia" };
        for (int i = 0; i < filas.Length; i++)
        {
            float y = 0.045f - i * 0.065f;
            Texto("Turno_" + i, turnos, filas[i], new Vector3(0, y, -0.004f), Vector3.zero, new Vector2(0.28f, 0.035f), TintaOscura);
            if (i > 0) Impreso(turnos, "Linea_" + i, 0, y + 0.0325f, 0.28f, 0.002f, matGrisClaro);
        }
        Chincheta(turnos, new Vector3(0, 0.125f, -0.008f), matVerde);
    }

    // Hoja de papel clavada en la cartelera, un poco torcida (angulo en grados) para que se vea natural.
    // Devuelve el grupo de la hoja: la cara del papel está en z = 0 y lo impreso encima va hacia -Z.
    static Transform Hoja(string nombre, Transform cartelera, Vector2 centro, float ancho, float alto, float angulo)
    {
        Transform h = Grupo(nombre, cartelera);
        h.localPosition = new Vector3(centro.x, centro.y, -0.033f); // 1 mm por delante de la tela
        h.localRotation = Quaternion.Euler(0, 0, angulo);
        SinCollider(Caja("Papel", h, new Vector3(0, 0, 0.001f), new Vector3(ancho, alto, 0.002f), matPapel));
        return h;
    }

    // Rectángulo de color impreso sobre una hoja (encabezados, escudo, cuadros del plano).
    // giro = grados sobre la hoja. capa = distancia delante del papel: cada capa debe ir a 1 mm de la otra
    // para que no parpadeen (el papel está en 0, lo impreso en 2.5 mm y los textos en 4 mm).
    static void Impreso(Transform hoja, string nombre, float x, float y, float ancho, float alto, Material mat, float giro = 0, float capa = 0.0025f)
    {
        SinCollider(Caja(nombre, hoja, new Vector3(x, y, -capa), new Vector3(ancho, alto, 0.001f), mat, new Vector3(0, 0, giro)));
    }

    // Bebedero moderno de acero inoxidable: arriba una estación para llenar botellas con su pantalla
    // (apagada por el corte de energía) y abajo la fuente para tomar agua.
    // Poly Haven no tiene dispensadores de agua, así que se arma con acero y vidrio negro.
    // Dentro del grupo: X = derecha, Y = altura desde el piso, el muro en z = 0 y lo que sobresale va hacia -Z.
    static void ConstruirBebedero(Transform padre)
    {
        Transform b = Grupo("Bebedero", padre);
        b.localPosition = new Vector3(2f, 0, 1.2f);
        b.localRotation = Quaternion.Euler(0, 90, 0); // se usa mirando al este

        // Placa trasera de 3.5 cm: tapa el pasamanos, que sobresale 3 cm del muro
        Caja("Placa_Trasera", b, new Vector3(0, 1.12f, -0.0175f), new Vector3(0.5f, 1.15f, 0.035f), matMetalOscuro);

        // Estación de llenado de botellas: hueco negro con el pico arriba y una rejilla abajo
        Caja("Estacion", b, new Vector3(0, 1.33f, -0.095f), new Vector3(0.34f, 0.62f, 0.12f), matAcero);
        SinCollider(Caja("Estacion_Hueco", b, new Vector3(0, 1.28f, -0.1565f), new Vector3(0.24f, 0.34f, 0.003f), matPanel));
        SinCollider(Primitiva(PrimitiveType.Cylinder, "Estacion_Pico", b, new Vector3(0, 1.43f, -0.168f), new Vector3(0.02f, 0.015f, 0.02f), matAcero));
        SinCollider(Caja("Estacion_Rejilla", b, new Vector3(0, 1.12f, -0.18f), new Vector3(0.22f, 0.012f, 0.05f), matAcero));
        SinCollider(Caja("Pantalla", b, new Vector3(0, 1.57f, -0.1565f), new Vector3(0.2f, 0.06f, 0.003f), matPanel));
        Texto("Pantalla_Titulo", b, "AGUA FILTRADA", new Vector3(0, 1.585f, -0.159f), Vector3.zero, new Vector2(0.18f, 0.015f), TintaGris);
        Texto("Pantalla_Estado", b, "SIN ENERGÍA", new Vector3(0, 1.557f, -0.159f), Vector3.zero, new Vector2(0.18f, 0.02f), TintaAmbar);

        // Fuente para tomar agua: lavabo, fondo, surtidor, botón al frente y cubierta de abajo
        Caja("Fuente_Lavabo", b, new Vector3(0, 0.9f, -0.185f), new Vector3(0.46f, 0.08f, 0.3f), matAcero);
        SinCollider(Caja("Fuente_Fondo", b, new Vector3(0, 0.9405f, -0.2f), new Vector3(0.38f, 0.001f, 0.22f), matMetalOscuro));
        SinCollider(Primitiva(PrimitiveType.Cylinder, "Fuente_Surtidor", b, new Vector3(0.1f, 0.96f, -0.22f), new Vector3(0.015f, 0.02f, 0.015f), matAcero));
        SinCollider(Primitiva(PrimitiveType.Cylinder, "Fuente_Boton", b, new Vector3(0.15f, 0.9f, -0.336f), new Vector3(0.03f, 0.001f, 0.03f), matMetalOscuro, new Vector3(90, 0, 0)));
        Caja("Fuente_Cubierta", b, new Vector3(0, 0.71f, -0.135f), new Vector3(0.3f, 0.3f, 0.2f), matMetalOscuro);
    }

    // Cartel de seguridad moderno del extintor: placa roja con borde blanco, pictograma y texto.
    // Mismas reglas que Cartel(): pos = punto de la cara del muro; rotY = hacia dónde mira el jugador al leerlo.
    static void CartelExtintor(string nombre, Transform padre, Vector3 pos, float rotY)
    {
        Transform c = Grupo(nombre, padre);
        c.localPosition = pos;
        c.localRotation = Quaternion.Euler(0, rotY, 0);
        SinCollider(Caja("Placa", c, new Vector3(0, 0, -0.006f), new Vector3(0.2f, 0.28f, 0.012f), matRojoSenal));

        // Todo lo blanco va 1 mm por delante de la placa
        const float z = -0.0125f;
        SinCollider(Caja("Borde_Arriba", c, new Vector3(0, 0.125f, z), new Vector3(0.176f, 0.006f, 0.001f), matBlancoSenal));
        SinCollider(Caja("Borde_Abajo", c, new Vector3(0, -0.125f, z), new Vector3(0.176f, 0.006f, 0.001f), matBlancoSenal));
        SinCollider(Caja("Borde_Izq", c, new Vector3(-0.085f, 0, z), new Vector3(0.006f, 0.256f, 0.001f), matBlancoSenal));
        SinCollider(Caja("Borde_Der", c, new Vector3(0.085f, 0, z), new Vector3(0.006f, 0.256f, 0.001f), matBlancoSenal));

        // Pictograma: cuerpo del extintor, tapa redonda, cuello y boquilla
        SinCollider(Caja("Icono_Cuerpo", c, new Vector3(0, 0.02f, z), new Vector3(0.04f, 0.085f, 0.001f), matBlancoSenal));
        SinCollider(Primitiva(PrimitiveType.Cylinder, "Icono_Tapa", c, new Vector3(0, 0.0625f, z), new Vector3(0.04f, 0.0005f, 0.04f), matBlancoSenal, new Vector3(90, 0, 0)));
        SinCollider(Caja("Icono_Cuello", c, new Vector3(0, 0.09f, z), new Vector3(0.012f, 0.02f, 0.001f), matBlancoSenal));
        SinCollider(Caja("Icono_Boquilla", c, new Vector3(-0.014f, 0.098f, z), new Vector3(0.03f, 0.008f, 0.001f), matBlancoSenal));
        Texto("Texto", c, "EXTINTOR", new Vector3(0, -0.075f, -0.0135f), Vector3.zero, new Vector2(0.15f, 0.035f), TintaBlanca);
    }

    // Interruptor general debajo del tablero eléctrico: caja con luz roja (sin energía) y una palanca.
    // Paso 1 del acertijo: al subir la palanca vuelve la energía, se enciende la luz principal y la computadora,
    // y la luz del interruptor pasa a verde.
    // Dentro del grupo: X = derecha, Y = arriba, el muro en z = 0 y lo que sobresale va hacia -Z (hacia el jugador).
    static void ConstruirInterruptorGeneral(Transform padre)
    {
        Transform c = Grupo("Interruptor_General", padre);
        c.localPosition = new Vector3(-2f, 1.12f, -1.35f); // a la altura de la mano, debajo del tablero
        c.localRotation = Quaternion.Euler(0, -90, 0);     // se usa mirando al oeste

        Caja("Caja", c, new Vector3(0, 0, -0.02f), new Vector3(0.16f, 0.2f, 0.04f), matMetalOscuro);
        Texto("Titulo", c, "ENERGÍA", new Vector3(0.01f, 0.082f, -0.041f), Vector3.zero, new Vector2(0.1f, 0.018f), TintaBlanca);
        Texto("On", c, "ON", new Vector3(0.05f, 0.045f, -0.041f), Vector3.zero, new Vector2(0.04f, 0.016f), new Color(0.3f, 0.9f, 0.4f));
        Texto("Off", c, "OFF", new Vector3(0.05f, -0.06f, -0.041f), Vector3.zero, new Vector2(0.04f, 0.016f), new Color(1f, 0.35f, 0.3f));
        GameObject ledRojo = SinCollider(Caja("LED_Sin_Energia", c, new Vector3(-0.058f, 0.082f, -0.0415f), new Vector3(0.012f, 0.012f, 0.003f), matLuzRoja));
        GameObject ledVerde = SinCollider(Caja("LED_Con_Energia", c, new Vector3(-0.058f, 0.082f, -0.0415f), new Vector3(0.012f, 0.012f, 0.003f), matLuzVerde));
        ledVerde.SetActive(false);

        // Palanca: el pivote está en el frente de la caja y la manija apunta a su +Z.
        // Girado 180° para que la manija salga hacia el cuarto, y 45° hacia abajo para empezar apagada.
        Transform pivote = Grupo("Palanca", c);
        pivote.localPosition = new Vector3(-0.02f, -0.01f, -0.04f);
        pivote.localRotation = Quaternion.Euler(0, 180, 0) * Quaternion.Euler(45, 0, 0);
        SinCollider(Caja("Base", pivote, new Vector3(0, 0, 0.01f), new Vector3(0.05f, 0.03f, 0.02f), matNegro));
        SinCollider(Caja("Brazo", pivote, new Vector3(0, 0, 0.06f), new Vector3(0.018f, 0.018f, 0.1f), matAcero));
        SinCollider(Primitiva(PrimitiveType.Sphere, "Perilla", pivote, new Vector3(0, 0, 0.115f), Vector3.one * 0.032f, matRojo));
        BoxCollider agarre = pivote.gameObject.AddComponent<BoxCollider>();
        agarre.center = new Vector3(0, 0, 0.07f);
        agarre.size = new Vector3(0.06f, 0.06f, 0.14f);
        pivote.gameObject.AddComponent<Rigidbody>().isKinematic = true; // la mueve el script, no la física
        pivote.gameObject.AddComponent<XRSimpleInteractable>();
        Lever palanca = pivote.gameObject.AddComponent<Lever>();
        pivote.gameObject.AddComponent<ResaltarAlApuntar>();
        palanca.sonido = sonidoPalanca;

        // Lo que pasa al subirla (se ve en el evento "alActivar" del Inspector de la palanca)
        UnityEventTools.AddBoolPersistentListener(palanca.alActivar, luzPrincipal.SetActive, true);
        UnityEventTools.AddBoolPersistentListener(palanca.alActivar, bombillaEncendida.SetActive, true);
        UnityEventTools.AddBoolPersistentListener(palanca.alActivar, pantallaEncendida.SetActive, true);
        UnityEventTools.AddBoolPersistentListener(palanca.alActivar, luzTablero.SetActive, false);
        UnityEventTools.AddBoolPersistentListener(palanca.alActivar, ledRojo.SetActive, false);
        UnityEventTools.AddBoolPersistentListener(palanca.alActivar, ledVerde.SetActive, true);
    }

    // Busca un objeto por nombre dentro de otro (en cualquier nivel)
    static Transform BuscarHijo(Transform padre, string nombre)
    {
        foreach (Transform t in padre.GetComponentsInChildren<Transform>(true))
            if (t.name == nombre) return t;
        return null;
    }

    // ================================================================ Sala de espera y decoración

    static void ConstruirSalaDeEspera(Transform g)
    {
        // Sillas de espera contra el muro oeste, mirando al este (hacia el centro del cuarto)
        Silla("Silla_Espera_1", g, new Vector3(-1.64f, 0, 0.35f), -90);
        Silla("Silla_Espera_2", g, new Vector3(-1.64f, 0, 0.95f), -90);

        // Planta decorativa en la esquina noroeste. El modelo es una planta de escritorio de 27 cm:
        // al 280 % queda de 75 cm, del tamaño de una planta de piso.
        if (Modelo("potted_plant_04", g, new Vector3(-1.65f, 0, 1.65f), Vector3.zero, 2.8f, true) == null)
        {
            Primitiva(PrimitiveType.Cylinder, "Maceta", g, new Vector3(-1.65f, 0.2f, 1.65f), new Vector3(0.4f, 0.2f, 0.4f), matTerracota);
            Primitiva(PrimitiveType.Sphere, "Planta", g, new Vector3(-1.65f, 0.65f, 1.65f), Vector3.one * 0.6f, matVerde);
        }

        // Cuadro en el muro norte, al lado de la entrada. Su parte trasera está en el origen: se gira 180° para mirar al cuarto.
        Modelo("hanging_picture_frame_02", g, new Vector3(-1.3f, 1.75f, 1.998f), new Vector3(0, 180, 0));

        // Bolsas de basura junto a la entrada tapiada: el colegio lleva tiempo abandonado
        Modelo("trashbag", g, new Vector3(-1.05f, 0, 1.7f), new Vector3(0, 30, 0), 0.9f);

        // Tablero eléctrico abierto en el muro oeste, detrás del mostrador (la historia del apagón).
        // El modelo tiene el frente hacia +Z con la puerta abierta: se gira 90° para mirar al este.
        // Su trasera está a 4.7 cm del origen: en x = -1.951 queda a 2 mm del muro (sin parpadeo).
        Modelo("power_box_01", g, new Vector3(-1.951f, 1.5f, -1.35f), new Vector3(0, 90, 0));
        ConstruirInterruptorGeneral(g);

        // Entrada tapiada en el muro norte, detrás del punto de inicio: no hay vuelta atrás
        Caja("Entrada_Tapiada", g, new Vector3(0, 1.05f, 1.97f), new Vector3(1.2f, 2.1f, 0.06f), matMaderaOscura);
        Caja("Tabla_1", g, new Vector3(0, 1.4f, 1.92f), new Vector3(1.5f, 0.15f, 0.04f), matMadera, new Vector3(0, 0, 25));
        Caja("Tabla_2", g, new Vector3(0, 0.8f, 1.88f), new Vector3(1.5f, 0.15f, 0.04f), matMadera, new Vector3(0, 0, -25));
        SinCollider(Caja("Cinta_1", g, new Vector3(0, 1.1f, 1.845f), new Vector3(1.3f, 0.08f, 0.01f), matAmarillo, new Vector3(0, 0, 35)));
        SinCollider(Caja("Cinta_2", g, new Vector3(0, 1.1f, 1.83f), new Vector3(1.3f, 0.08f, 0.01f), matAmarillo, new Vector3(0, 0, -35)));
        Cartel("Letrero_Entrada", g, "ENTRADA", new Vector3(0, 2.38f, 2f), 0, 0.9f, 0.2f);

        // Extintor con su letrero en el muro norte. El modelo tiene el origen en el piso y el frente hacia +Z:
        // se gira 180° para que mire al cuarto.
        if (Modelo("korean_fire_extinguisher_01", g, new Vector3(1.2f, 0, 1.87f), new Vector3(0, 180, 0)) == null)
        {
            // Sin el modelo: versión gray box
            Primitiva(PrimitiveType.Cylinder, "Extintor", g, new Vector3(1.2f, 0.25f, 1.85f), new Vector3(0.16f, 0.25f, 0.16f), matRojo);
            SinCollider(Primitiva(PrimitiveType.Cylinder, "Extintor_Valvula", g, new Vector3(1.2f, 0.54f, 1.85f), new Vector3(0.05f, 0.04f, 0.05f), matNegro));
        }
        CartelExtintor("Cartel_Extintor", g, new Vector3(1.2f, 1.35f, 2f), 0);

        // Alarma de incendio en el muro este, junto a la puerta. Solo decora (no hay versión gray box).
        // Su parte trasera está en el origen: se pega al muro y se gira -90° para que mire al oeste (al cuarto).
        Modelo("fire_alarm", g, new Vector3(1.998f, 1.25f, -1.55f), new Vector3(0, -90, 0));

        // Bebedero moderno en el muro este, con un reloj encima y un cartel de piso mojado al lado
        ConstruirBebedero(g);

        // Reloj de pared que marca la hora real. Trasera en el origen, a 2 mm del muro; girado para mirar al oeste.
        GameObject reloj = Modelo("wall_clock", g, new Vector3(1.998f, 1.9f, 1.2f), new Vector3(0, -90, 0));
        if (reloj != null)
        {
            Transform horario = BuscarHijo(reloj.transform, "wall_clock_hours_hand");
            Transform minutero = BuscarHijo(reloj.transform, "wall_clock_minute_hand");
            Transform segundero = BuscarHijo(reloj.transform, "wall_clock_second_hand");
            if (horario != null && minutero != null && segundero != null)
            {
                RelojDePared agujas = reloj.AddComponent<RelojDePared>();
                agujas.horario = horario;
                agujas.minutero = minutero;
                agujas.segundero = segundero;
                // Hacia dónde apuntan las agujas en el modelo (medido en su geometría: marca las 10:09:28)
                agujas.anguloHorario = 306.8f;
                agujas.anguloMinutero = 57f;
                agujas.anguloSegundero = 170.5f;
            }
            else Debug.LogWarning("No se encontraron las agujas del reloj: queda detenido.");
        }
        Modelo("WetFloorSign_01", g, new Vector3(1.25f, 0, 1.0f), new Vector3(0, -60, 0));

        // Papelera oxidada en la esquina noreste. El modelo trae dos papeleras: se usa solo la oxidada, al 60 %.
        ModeloPieza("metal_trash_can", "metal_trash_can_rust", g, new Vector3(1.7f, 0, 1.7f), new Vector3(0, 200, 0), 0.6f);

        // Estante metálico y caja de madera en el muro este. Dejan libre el paso para rodear el mostrador.
        // El estante viene en centímetros (mide 21 m): al 8 % queda de 1.7 m de alto. Girado 90° para ir a lo largo del muro.
        if (Modelo("steel_frame_shelves_01", g, new Vector3(1.8f, 0, 0.2f), new Vector3(0, 90, 0), 0.08f, true) == null)
            Caja("Archivador", g, new Vector3(1.7f, 0.65f, 0.2f), new Vector3(0.6f, 1.3f, 0.5f), matMetal);
        // La caja tiene el pestillo hacia +Z: con -90° queda mirando al cuarto (oeste)
        if (Modelo("wooden_crate_01", g, new Vector3(1.79f, 0, -0.9f), new Vector3(0, -90, 0), 1f, true) == null)
        {
            Caja("Caja_1", g, new Vector3(1.75f, 0.2f, -0.9f), new Vector3(0.5f, 0.4f, 0.4f), matCarton);
            Caja("Caja_2", g, new Vector3(1.7f, 0.55f, -0.85f), new Vector3(0.4f, 0.3f, 0.35f), matCarton, new Vector3(0, 15, 0));
        }
    }

    static void ColocarJugador()
    {
        // Punto de inicio: frente a la entrada tapiada, mirando al sur (hacia el mostrador y la puerta)
        GameObject jugador = GameObject.Find("XR Origin (XR Rig)");
        if (jugador == null) return;
        jugador.transform.SetPositionAndRotation(new Vector3(0, 0, 1.2f), Quaternion.Euler(0, 180, 0));

        // La cabeza no atraviesa paredes ni muebles, y la altura de los ojos queda entre 0.8 y 1.85 m
        if (jugador.GetComponent<LimitesDelJugador>() == null) jugador.AddComponent<LimitesDelJugador>();
        // Para probar en el PC: C agacharse/levantarse y SHIFT correr (en el Quest no hace nada)
        if (jugador.GetComponent<ControlesDePC>() == null) jugador.AddComponent<ControlesDePC>();

        // La vista de escena del editor queda mirando desde los ojos del jugador (dentro del cuarto, no afuera)
        Camera ojos = jugador.GetComponentInChildren<Camera>();
        if (ojos != null && SceneView.lastActiveSceneView != null)
            SceneView.lastActiveSceneView.AlignViewToObject(ojos.transform);
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

        // Para cambiar un color: se cambia aquí y se vuelve a construir el cuarto.
        // En los materiales con textura el color funciona como tinte: blanco = la textura tal cual.
        // Estilo "moderno abandonado": terrazo, yeso liso gris, madera clara, metal oscuro y detalles ámbar.
        matPiso = CrearMaterial("Piso", new Color(0.92f, 0.92f, 0.92f));
        AplicarTextura(matPiso, "terrazzo_tiles", 2f, 2f);
        matPared = CrearMaterial("Pared", new Color(0.82f, 0.84f, 0.85f));
        AplicarTextura(matPared, "plastered_wall_04", 3.2f, 3.2f);
        matTecho = CrearMaterial("Techo", new Color(0.75f, 0.76f, 0.78f));
        AplicarTextura(matTecho, "white_stucco", 2f, 2f);
        matMadera = CrearMaterial("Madera", Color.white);
        AplicarTextura(matMadera, "kitchen_wood", 0.6f, 0.6f);
        matMaderaOscura = CrearMaterial("Madera_Oscura", new Color(0.8f, 0.8f, 0.8f));
        AplicarTextura(matMaderaOscura, "wood_table_001", 1.5f, 1.5f);
        matFrenteMostrador = CrearMaterial("Frente_Mostrador", new Color(0.35f, 0.37f, 0.4f));
        AplicarTextura(matFrenteMostrador, "painted_metal_shutter", 2f, 2f); // metal acanalado oscuro

        // Colores lisos
        matVerdeAzulado = CrearMaterial("Verde_Azulado", new Color(0.17f, 0.40f, 0.42f));
        matCartel = CrearMaterial("Cartel", new Color(0.14f, 0.15f, 0.16f));      // placas de carteles y zócalo
        matAcento = CrearMaterial("Acento_Ambar", new Color(1f, 0.62f, 0.12f), 0.6f); // línea de los carteles
        matAzul = CrearMaterial("Azul", new Color(0.16f, 0.34f, 0.66f));

        // Metales: con la sonda de reflejos se ven pulidos. Metalico(material, cuánto metal, cuánto brillo)
        matMetal = CrearMaterial("Metal", new Color(0.7f, 0.72f, 0.75f));
        Metalico(matMetal, 1f, 0.6f);
        matMetalOscuro = CrearMaterial("Metal_Oscuro", new Color(0.18f, 0.19f, 0.2f));
        Metalico(matMetalOscuro, 0.8f, 0.45f);
        matNegro = CrearMaterial("Negro", new Color(0.12f, 0.12f, 0.13f));
        matPapel = CrearMaterial("Papel", new Color(0.96f, 0.96f, 0.93f));
        matLaton = CrearMaterial("Laton", new Color(0.85f, 0.66f, 0.26f));
        matRojo = CrearMaterial("Rojo", new Color(0.78f, 0.12f, 0.12f));
        matAmarillo = CrearMaterial("Amarillo", new Color(0.95f, 0.78f, 0.12f));
        matVerde = CrearMaterial("Verde", new Color(0.25f, 0.55f, 0.30f));
        matTerracota = CrearMaterial("Terracota", new Color(0.70f, 0.38f, 0.25f));
        matCarton = CrearMaterial("Carton", new Color(0.68f, 0.52f, 0.33f));
        // Tela de arpillera teñida de gris azulado para la cartelera (Poly Haven no tiene corcho)
        matTela = CrearMaterial("Tela_Cartelera", new Color(0.42f, 0.5f, 0.58f));
        AplicarTextura(matTela, "hessian_230", 0.27f, 0.27f);
        matGrisClaro = CrearMaterial("Gris_Claro", new Color(0.8f, 0.82f, 0.84f));
        matAluminio = CrearMaterial("Aluminio", new Color(0.75f, 0.77f, 0.8f));
        Metalico(matAluminio, 0.6f, 0.5f); // menos metálico que matMetal: en un cuarto oscuro el metal puro se ve negro
        matVidrio = CrearMaterial("Vidrio_Esmerilado", new Color(0.1f, 0.12f, 0.14f));
        Metalico(matVidrio, 0f, 0.95f); // oscuro y muy brillante: parece vidrio polarizado sin dejar ver el otro cuarto
        matPanel = CrearMaterial("Panel_Acceso", new Color(0.06f, 0.065f, 0.07f));
        Metalico(matPanel, 0.3f, 0.8f); // negro brillante, como el frente de vidrio de un lector de acceso
        matAcero = CrearMaterial("Acero_Inoxidable", new Color(0.72f, 0.74f, 0.76f));
        Metalico(matAcero, 0.85f, 0.55f);
        matPlastico = CrearMaterial("Plastico_Oscuro", new Color(0.28f, 0.29f, 0.3f));
        Metalico(matPlastico, 0f, 0.3f);
        // Los colores de los carteles de seguridad brillan un poco, como los fotoluminiscentes
        matRojoSenal = CrearMaterial("Rojo_Senal", new Color(0.8f, 0.1f, 0.1f), 0.15f);
        matBlancoSenal = CrearMaterial("Blanco_Senal", new Color(0.95f, 0.95f, 0.95f), 0.2f);
        matVidrioClaro = CrearMaterialTransparente("Vidrio_Transparente", new Color(0.9f, 0.95f, 1f, 0.08f));
        matAlfombra = CrearMaterial("Alfombra", new Color(0.35f, 0.36f, 0.38f));
        AplicarTextura(matAlfombra, "poly_wool_herringbone", 0.27f, 0.27f); // lana en espiga

        // Materiales que brillan (emisivos). El número es cuánto brillan.
        matFoco = CrearMaterial("Foco", new Color(1f, 0.85f, 0.55f), 4f);
        matPantalla = CrearMaterial("Pantalla", new Color(0.02f, 0.035f, 0.06f), 1f); // azul noche con brillo suave
        matPantallaApagada = CrearMaterial("Pantalla_Apagada", new Color(0.01f, 0.01f, 0.012f));
        Metalico(matPantallaApagada, 0f, 0.9f); // vidrio negro de una pantalla sin energía
        matLuzVerde = CrearMaterial("Luz_Verde", new Color(0.2f, 0.9f, 0.35f), 2f);
        Metalico(matPantalla, 0f, 0.85f); // vidrio de pantalla
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

    // La luz principal y las luces chicas del cuarto son "luces adicionales" de URP en tiempo real.
    // Si el perfil de URP activo las tiene desactivadas, se encienden pero no iluminan nada.
    static void AvisarSiFaltanLucesAdicionales()
    {
        var perfil = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline;
        if (perfil == null) return;
        SerializedProperty modo = new SerializedObject(perfil).FindProperty("m_AdditionalLightsRenderingMode");
        if (modo != null && modo.intValue == 0)
            Debug.LogWarning("El perfil de URP '" + perfil.name + "' tiene las luces adicionales desactivadas: la luz principal "
                + "no va a iluminar. Actívalas en ese asset: Lighting > Additional Lights = Per Pixel.");
    }

    // Pone una textura de Poly Haven en un material: la imagen de color y el mapa de relieve (normal map).
    // metrosAncho y metrosAlto = cuánto mide la textura en la vida real (lo dice la página de Poly Haven).
    // Si la textura no está, el material se queda con su color liso.
    static void AplicarTextura(Material mat, string nombre, float metrosAncho, float metrosAlto)
    {
        string ruta = CarpetaTexturas + "/" + nombre + "/" + nombre;
        Texture2D color = AssetDatabase.LoadAssetAtPath<Texture2D>(ruta + "_diff_1k.jpg");
        if (color == null)
        {
            Debug.LogWarning("No se encontró la textura " + nombre + ": el material queda de color liso.");
            return;
        }
        mat.SetTexture("_BaseMap", color);
        // Las cajas tienen sus UVs en metros (ver UVsEnMetros), así que la repetición es 1 / tamaño real
        mat.SetTextureScale("_BaseMap", new Vector2(1f / metrosAncho, 1f / metrosAlto));
        mat.SetFloat("_Smoothness", 0.15f); // superficies mate, sin brillo de plástico

        Texture2D relieve = CargarMapaNormal(ruta + "_nor_gl_1k.jpg");
        if (relieve != null)
        {
            mat.SetTexture("_BumpMap", relieve);
            mat.EnableKeyword("_NORMALMAP");
        }
    }

    // Material transparente (lo mismo que elegir "Surface Type: Transparent" en el Inspector).
    // El alfa del color dice cuánto se ve: 0 = invisible, 1 = opaco.
    static Material CrearMaterialTransparente(string nombre, Color color)
    {
        Material mat = CrearMaterial(nombre, color);
        mat.SetFloat("_Surface", 1);
        mat.SetFloat("_Blend", 0);
        mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetFloat("_SrcBlendAlpha", (float)UnityEngine.Rendering.BlendMode.One);
        mat.SetFloat("_DstBlendAlpha", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetFloat("_ZWrite", 0);
        mat.SetOverrideTag("RenderType", "Transparent");
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        Metalico(mat, 0f, 0.95f);
        return mat;
    }

    // metal: 0 = no metálico, 1 = metal. brillo: 0 = mate, 1 = espejo.
    static void Metalico(Material mat, float metal, float brillo)
    {
        mat.SetFloat("_Metallic", metal);
        mat.SetFloat("_Smoothness", brillo);
    }

    // Unity solo usa bien un mapa de relieve si está marcado como "Normal map" en su importador
    static Texture2D CargarMapaNormal(string ruta)
    {
        TextureImporter importador = AssetImporter.GetAtPath(ruta) as TextureImporter;
        if (importador == null) return null;
        if (importador.textureType != TextureImporterType.NormalMap)
        {
            importador.textureType = TextureImporterType.NormalMap;
            importador.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Texture2D>(ruta);
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
        if (tipo == PrimitiveType.Cube && mat.GetTexture("_BaseMap") != null) UVsEnMetros(go);
        return go;
    }

    // El cubo de Unity pone la textura completa en cada cara sin importar su tamaño: en un muro de 4 m
    // se vería estirada. Esto le da al cubo una copia de su malla donde 1 unidad de UV = 1 metro,
    // así la textura se repite a su tamaño real en cualquier caja.
    static void UVsEnMetros(GameObject caja)
    {
        MeshFilter filtro = caja.GetComponent<MeshFilter>();
        Mesh malla = Object.Instantiate(filtro.sharedMesh);
        malla.name = "Cubo_UV_" + caja.name;

        Vector3 tam = caja.transform.localScale;
        Vector3[] vertices = malla.vertices;
        Vector3[] normales = malla.normals;
        Vector2[] uvs = new Vector2[vertices.Length];
        for (int i = 0; i < vertices.Length; i++)
        {
            Vector3 p = Vector3.Scale(vertices[i], tam); // posición del vértice en metros
            Vector3 n = normales[i];
            if (Mathf.Abs(n.x) > 0.5f) uvs[i] = new Vector2(p.z, p.y);      // caras este y oeste
            else if (Mathf.Abs(n.y) > 0.5f) uvs[i] = new Vector2(p.x, p.z); // caras de arriba y abajo
            else uvs[i] = new Vector2(p.x, p.y);                            // caras norte y sur
        }
        malla.uv = uvs;
        malla.RecalculateTangents(); // el relieve necesita tangentes que sigan a las UVs nuevas
        filtro.sharedMesh = malla;
    }

    // Coloca un modelo 3D de Poly Haven. escala = tamaño (1 = tamaño real).
    // conCollider = le pone una caja de colisión que lo envuelve (para muebles: que los objetos no lo atraviesen).
    // Si no lo encuentra (por ejemplo, falta instalar glTFast) avisa y devuelve null,
    // y quien lo llama construye la versión gray box.
    static GameObject Modelo(string nombre, Transform padre, Vector3 pos, Vector3 rot, float escala = 1f, bool conCollider = false)
    {
        string ruta = CarpetaModelos + "/" + nombre + "/" + nombre + "_1k.gltf";
        GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(ruta);
        if (asset == null)
        {
            Debug.LogWarning("No se encontró el modelo " + ruta + " (¿está instalado glTFast?). Se usa gray box.");
            return null;
        }
        GameObject go = PrefabUtility.InstantiatePrefab(asset, padre) as GameObject;
        if (go == null) go = Object.Instantiate(asset, padre);
        go.name = "Modelo_" + nombre;
        go.transform.localPosition = pos;
        go.transform.localRotation = Quaternion.Euler(rot);
        go.transform.localScale = Vector3.one * escala;

        // Los modelos reciben la luz horneada desde las sondas de luz, no desde un mapa de luz propio
        // (para eso necesitarían UVs extra que Poly Haven no trae). Igual hacen sombra sobre el cuarto.
        foreach (MeshRenderer r in go.GetComponentsInChildren<MeshRenderer>())
            r.receiveGI = ReceiveGI.LightProbes;

        // Los vidrios de Poly Haven usan un efecto de "transmisión" que Unity no dibuja en el Quest: se ven como
        // una capa blanca (así quedaba tapada la cara del reloj). Se cambian por un vidrio transparente propio.
        foreach (Renderer r in go.GetComponentsInChildren<Renderer>())
        {
            Material[] mats = r.sharedMaterials;
            bool cambio = false;
            for (int i = 0; i < mats.Length; i++)
            {
                bool esVidrio = mats[i] != null && mats[i].name.Contains("glass") && mats[i].renderQueue >= 3000;
                if (esVidrio) { mats[i] = matVidrioClaro; cambio = true; }
            }
            if (cambio) r.sharedMaterials = mats;
        }

        if (conCollider) ColliderQueEnvuelve(go);
        return go;
    }

    // Algunos modelos traen varias piezas juntas (un set de papeles, dos papeleras).
    // Esto deja solo las piezas cuyo nombre empieza con "pieza" y las centra en "pos".
    static GameObject ModeloPieza(string nombre, string pieza, Transform padre, Vector3 pos, Vector3 rot, float escala = 1f)
    {
        GameObject go = Modelo(nombre, padre, pos, rot, escala);
        if (go == null) return null;
        go.name = "Modelo_" + pieza;

        // Para borrar partes de un modelo primero hay que "desempaquetarlo" (dejarlo como objetos normales)
        PrefabUtility.UnpackPrefabInstance(go, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        foreach (MeshRenderer parte in go.GetComponentsInChildren<MeshRenderer>())
        {
            if (parte != null && !parte.name.StartsWith(pieza)) Object.DestroyImmediate(parte.gameObject);
        }

        // Las piezas no están en el centro del modelo: se corre todo en horizontal para que queden justo en "pos"
        Renderer[] quedan = go.GetComponentsInChildren<Renderer>();
        if (quedan.Length == 0)
        {
            Debug.LogWarning("El modelo " + nombre + " no tiene piezas que empiecen con " + pieza);
            return go;
        }
        Bounds limites = quedan[0].bounds;
        foreach (Renderer r in quedan) limites.Encapsulate(r.bounds);
        Vector3 desfase = limites.center - go.transform.position;
        desfase.y = 0;
        go.transform.position -= desfase;
        return go;
    }

    // Para lámparas: la luz va dentro de la lámpara, y si la lámpara hiciera sombra se taparía su propia luz
    static void SinSombra(GameObject go)
    {
        foreach (Renderer r in go.GetComponentsInChildren<Renderer>())
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    }

    // Cartel moderno: placa oscura, una línea ámbar abajo y el texto blanco con letras separadas.
    // pos = punto de la cara del muro donde va el centro del cartel.
    // rotY = hacia dónde mira el jugador para leerlo: 0 = al norte, 180 = al sur, -90 = al oeste.
    static void Cartel(string nombre, Transform padre, string texto, Vector3 pos, float rotY, float ancho, float alto)
    {
        Transform c = Grupo(nombre, padre);
        c.localPosition = pos;
        c.localRotation = Quaternion.Euler(0, rotY, 0);

        // Dentro del grupo el muro está en z = 0 y el cartel sobresale hacia -Z (hacia el jugador)
        SinCollider(Caja("Placa", c, new Vector3(0, 0, -0.01f), new Vector3(ancho, alto, 0.02f), matCartel));
        SinCollider(Caja("Linea", c, new Vector3(0, -alto / 2 + 0.01f, -0.021f), new Vector3(ancho, 0.02f, 0.004f), matAcento));
        GameObject t = Texto("Texto", c, texto, new Vector3(0, 0.008f, -0.025f), Vector3.zero, new Vector2(ancho - 0.1f, alto - 0.08f), TintaBlanca);
        t.GetComponent<TextMeshPro>().characterSpacing = 6;
    }

    // Caja de colisión del tamaño del modelo (sirve bien con giros de 0, 90, 180 o 270 grados)
    static void ColliderQueEnvuelve(GameObject go)
    {
        Renderer[] partes = go.GetComponentsInChildren<Renderer>();
        if (partes.Length == 0) return;
        Bounds limites = partes[0].bounds;
        foreach (Renderer r in partes) limites.Encapsulate(r.bounds);

        BoxCollider col = go.AddComponent<BoxCollider>();
        col.center = go.transform.InverseTransformPoint(limites.center);
        Vector3 tam = go.transform.InverseTransformVector(limites.size);
        col.size = new Vector3(Mathf.Abs(tam.x), Mathf.Abs(tam.y), Mathf.Abs(tam.z));
    }

    // Silla de colegio. Con rotY = 0 la persona sentada mira al sur (-Z).
    static void Silla(string nombre, Transform padre, Vector3 pos, float rotY)
    {
        Transform silla = Grupo(nombre, padre);
        silla.localPosition = pos;
        silla.localRotation = Quaternion.Euler(0, rotY, 0);
        // El modelo tiene el respaldo en -Z (la persona sentada mira a +Z): se gira 180° para seguir la regla de arriba
        if (Modelo("SchoolChair_01", silla, Vector3.zero, new Vector3(0, 180, 0), 1f, true) != null) return;

        // Sin el modelo: silla gray box
        Caja("Asiento", silla, new Vector3(0, 0.45f, 0), new Vector3(0.45f, 0.05f, 0.45f), matAzul);
        Caja("Respaldo", silla, new Vector3(0, 0.72f, 0.2f), new Vector3(0.45f, 0.5f, 0.05f), matAzul);
        Primitiva(PrimitiveType.Cylinder, "Pata", silla, new Vector3(0, 0.21f, 0), new Vector3(0.08f, 0.21f, 0.08f), matNegro);
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

        Rigidbody cuerpo = go.AddComponent<Rigidbody>();
        // Detección de choques continua: un objeto chico que cae rápido no atraviesa la mesa ni el piso
        cuerpo.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        cuerpo.interpolation = RigidbodyInterpolation.Interpolate; // movimiento suave, sin saltitos
        // Dentro de un cajón empieza quieto (sin física) y se mueve con el cajón hasta que lo agarran
        cuerpo.isKinematic = dentroDeCajon;

        XRGrabInteractable grab = go.AddComponent<XRGrabInteractable>();
        // Agarre firme, como una mano que toma bien una herramienta:
        //  - punto de agarre fijo (PuntoDeAgarre): siempre queda en la misma posición en la mano
        //  - Instantaneous: sigue a la mano sin retraso ni temblor, así se puede apuntar con precisión
        //  - al agarrarlo con el rayo, el objeto viene a la mano en vez de quedar flotando lejos
        // Que no quede metido en la mesa al soltarlo lo resuelve ObjetoAgarrable.
        grab.useDynamicAttach = false;
        grab.movementType = XRBaseInteractable.MovementType.Instantaneous;
        grab.farAttachMode = InteractableFarAttachMode.Near;
        // Si empieza dentro de un cajón: al soltarlo NO vuelve a ser hijo del cajón
        if (dentroDeCajon) grab.retainTransformParent = false;

        // Quieto en el cajón hasta agarrarlo; al soltarlo sale de la mesa; si se pierde aparece sobre el mostrador
        ObjetoAgarrable objeto = go.AddComponent<ObjetoAgarrable>();
        objeto.puntoDeRescate = new Vector3(-0.45f, 1.2f, -0.1f);
        objeto.zonaPermitida = new Bounds(new Vector3(0, 1.3f, 0), new Vector3(4f, 2.6f, 4f));
        go.AddComponent<ResaltarAlApuntar>();  // se tiñe de ámbar al apuntarlo
        return go;
    }

    // Punto de agarre fijo de un objeto: dónde lo toma la mano y cómo queda orientado.
    // pos = lugar del objeto que queda en la mano. adelante = hacia dónde apunta esa parte del objeto
    // (hacia adelante de la mano). arriba = qué lado del objeto queda hacia arriba. Todo en coordenadas del objeto.
    static void PuntoDeAgarre(GameObject objeto, Vector3 pos, Vector3 adelante, Vector3 arriba)
    {
        Transform punto = Grupo("Punto_Agarre", objeto.transform);
        punto.localPosition = pos;
        punto.localRotation = Quaternion.LookRotation(adelante, arriba);
        objeto.GetComponent<XRGrabInteractable>().attachTransform = punto;
    }

    // Colisión invisible (sin dibujo): vuelve macizo un mueble hueco para que nada se cuele adentro
    static void Relleno(string nombre, Transform padre, Vector3 pos, Vector3 tam)
    {
        GameObject go = Caja(nombre, padre, pos, tam, matNegro);
        Object.DestroyImmediate(go.GetComponent<MeshRenderer>());
        Object.DestroyImmediate(go.GetComponent<MeshFilter>());
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
        go.AddComponent<ResaltarAlApuntar>();
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

    // horneada = true: la luz y sus sombras se calculan en el editor y quedan pintadas (gratis en el Quest).
    // horneada = false: luz en tiempo real sin sombras, para luces que cambian durante el juego.
    static GameObject Luz(string nombre, Transform padre, Vector3 pos, Color color, float intensidad, float alcance, bool horneada)
    {
        GameObject go = new GameObject(nombre);
        go.transform.SetParent(padre, false);
        go.transform.localPosition = pos;
        Light luz = go.AddComponent<Light>();
        luz.type = LightType.Point;
        luz.color = color;
        luz.intensity = intensidad;
        luz.range = alcance;
        if (horneada)
        {
            luz.lightmapBakeType = LightmapBakeType.Baked;
            luz.shadows = LightShadows.Soft;
            luz.shapeRadius = 0.08f; // tamaño de la bombilla: más grande = sombras más suaves
        }
        else
        {
            luz.lightmapBakeType = LightmapBakeType.Realtime;
            luz.shadows = LightShadows.None;
        }
        return go;
    }

    // ================================================================ Iluminación horneada

    // Marca como "estático" (que nunca se mueve) todo lo que puede recibir luz horneada.
    // Quedan fuera los objetos que se mueven o cambian en el juego: cajones y lo que tienen adentro,
    // agarrables, botones, tornillos y la puerta. Esos se iluminan con las sondas de luz.
    // También los textos, porque TextMeshPro usa los UVs extra para otra cosa.
    static void MarcarEstaticos(Transform raiz)
    {
        foreach (Transform t in raiz.GetComponentsInChildren<Transform>(true))
        {
            bool seMueve = t.GetComponentInParent<Rigidbody>(true) != null
                || t.GetComponentInParent<XRBaseInteractable>(true) != null
                || t.GetComponentInParent<Screw>(true) != null
                || EstaDentroDe(t, "Puerta_Bisagra");
            bool esTexto = t.GetComponent<TMP_Text>() != null;
            bool empiezaApagado = !t.gameObject.activeInHierarchy; // se enciende durante el juego
            if (seMueve || esTexto || empiezaApagado) continue;
            GameObjectUtility.SetStaticEditorFlags(t.gameObject, StaticEditorFlags.ContributeGI | StaticEditorFlags.ReflectionProbeStatic);

            // Las piezas chicas (menos de 25 cm) toman la luz de las sondas: en el mapa de luz serían
            // puntitos borrosos y harían más lento el horneado. Igual hacen sombra sobre lo demás.
            MeshRenderer r = t.GetComponent<MeshRenderer>();
            if (r != null && r.bounds.size.magnitude < 0.25f) r.receiveGI = ReceiveGI.LightProbes;
        }
    }

    static bool EstaDentroDe(Transform t, string nombrePadre)
    {
        for (Transform p = t; p != null; p = p.parent)
            if (p.name == nombrePadre) return true;
        return false;
    }

    // Configura y lanza el horneado de luz. Corre en segundo plano (barra abajo a la derecha).
    static void HornearLuz()
    {
        LightingSettings ajustes = AssetDatabase.LoadAssetAtPath<LightingSettings>(RutaAjustesLuz);
        if (ajustes == null)
        {
            ajustes = new LightingSettings();
            AssetDatabase.CreateAsset(ajustes, RutaAjustesLuz);
        }
        ajustes.bakedGI = true;
        ajustes.realtimeGI = false;
        ajustes.lightmapper = LightingSettings.Lightmapper.ProgressiveGPU; // si la tarjeta no puede, Unity usa la CPU
        ajustes.lightmapResolution = 30;                                  // puntos de luz por metro
        ajustes.lightmapMaxSize = 1024;
        ajustes.directionalityMode = LightmapsMode.NonDirectional;         // más liviano para el Quest
        ajustes.lightmapCompression = LightmapCompression.NormalQuality;
        ajustes.ao = true;            // oscurece rincones y uniones (debajo del mostrador, esquinas)
        ajustes.aoMaxDistance = 0.5f;
        EditorUtility.SetDirty(ajustes);
        Lightmapping.lightingSettings = ajustes;

        Lightmapping.bakeCompleted -= AlTerminarHorneado; // por si quedó conectado de un horneado anterior
        Lightmapping.bakeCompleted += AlTerminarHorneado;
        Lightmapping.BakeAsync();
    }

    static void AlTerminarHorneado()
    {
        Lightmapping.bakeCompleted -= AlTerminarHorneado;
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        Debug.Log("Luz del Cuarto 1 horneada y escena guardada. Ya se puede dar Play.");
    }
}
