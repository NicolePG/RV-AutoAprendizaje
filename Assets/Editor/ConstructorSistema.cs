using System.Collections.Generic;
using TMPro;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Comfort;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;

// Arma lo que no es de ningún cuarto: el reloj de la partida, el guardado, el reloj de la muñeca,
// el menú de inicio, el menú de pausa y la pantalla de tiempo agotado. Queda todo dentro de un objeto "Sistema" en la
// raíz de la escena (si ya existía, se rearma de cero).
//
// Además conecta las puertas: al abrirse la salida de cada cuarto, GameManager.LlegarAlCuarto
// guarda la partida sola; al abrirse la salida del Cuarto 4, GameManager.SalidaAbierta detiene el
// reloj. Por eso va DESPUÉS de construir los cuatro cuartos (lo llama "Construir los 4 cuartos").
//
// El menú de inicio (un panel de emergencia, con otro estilo) está en ConstructorMenuInicio.cs.
//
// Diseño de los menús: paneles planos oscuros con borde, una franja de color arriba (ámbar en la
// pausa, rojo en el tiempo agotado), textos claros y botones grandes que se iluminan al apuntarlos.
// Cada material tiene su lugar en el orden de dibujado (_QueueOffset): primero la esfera que
// oscurece el cuarto, después el borde, el fondo, las tarjetas, las barras y los botones, y al
// final los textos. Así nada tapa a nada aunque estén casi pegados, y el rayo del control se ve
// encima del menú.
public static partial class ConstructorSistema
{
    const string CARPETA = "Assets/Materials/Sistema";
    const string AJUSTES_CAPAS = "Assets/XRI/Settings/Resources/InteractionLayerSettings.asset";

    static readonly Color Texto = new Color(0.94f, 0.96f, 0.98f);
    static readonly Color Suave = new Color(0.58f, 0.64f, 0.72f);
    static readonly Color Oscuro = new Color(0.07f, 0.09f, 0.13f);

    static Material mOscurecer, mBorde, mFondo, mHUD, mTarjeta, mGris, mAmbar, mSecundario, mPeligro;
    static AudioSource audioUI;

    // Lo llama el menú Escape Room > Construir los 4 cuartos (MenuEscapeRoom), después de los
    // cuartos: no tiene menú propio, todo se construye junto
    public static void Construir()
    {
        var viejo = GameObject.Find("Sistema");
        if (viejo != null) Undo.DestroyObjectImmediate(viejo);

        CrearMateriales();
        NombrarCapaMenu();

        var sistema = new GameObject("Sistema");
        Undo.RegisterCreatedObjectUndo(sistema, "Construir sistema");
        var reloj = sistema.AddComponent<TimerController>();
        var guardado = sistema.AddComponent<SaveManager>();
        var juego = sistema.AddComponent<GameManager>();
        juego.reloj = reloj;
        juego.guardado = guardado;

        // Los sonidos de los menús: 2D (se oyen igual en los dos oídos) y fuera de la pausa del audio
        var audio = new GameObject("Audio_UI");
        audio.transform.SetParent(sistema.transform, false);
        audioUI = audio.AddComponent<AudioSource>();
        audioUI.playOnAwake = false;
        audioUI.spatialBlend = 0f;

        juego.puntosDeInicio = ArmarPuntosDeInicio(sistema.transform);
        juego.cuartos = ArmarDatosDeCuartos();
        juego.hud = ArmarHUD(sistema.transform, reloj);
        juego.menuInicio = ArmarMenuInicio(sistema.transform);
        ArmarMenuPausa(sistema.transform);
        juego.pantallaTiempoAgotado = ArmarTiempoAgotado(sistema.transform);
        ConectarPuertas(juego);
        PrepararMovimiento();

        EditorUtility.SetDirty(juego);
    }

    // ------------------------------------------------------------------ datos de los cuartos

    // Los cuatro assets RoomData (Assets/ScriptableObjects/Rooms), en el orden del recorrido. Si un
    // asset no existe se crea con su nombre, su descripción y su tiempo sugerido (entre los cuatro
    // suman los 15 minutos); si ya existe, se respeta lo que se haya cambiado en el Inspector. Los
    // acertijos se cargan siempre de nuevo: son los PuzzleData de los acertijos de cada cuarto.
    static RoomData[] ArmarDatosDeCuartos()
    {
        const string CARPETA_CUARTOS = "Assets/ScriptableObjects/Rooms";
        if (!AssetDatabase.IsValidFolder("Assets/ScriptableObjects")) AssetDatabase.CreateFolder("Assets", "ScriptableObjects");
        if (!AssetDatabase.IsValidFolder(CARPETA_CUARTOS)) AssetDatabase.CreateFolder("Assets/ScriptableObjects", "Rooms");

        var cuartos = new[]
        {
            DatosDeCuarto(CARPETA_CUARTOS, 1, "Cuarto1_Recepcion", "Recepción", 3f,
                "Portería del colegio y tutorial: palanca de la luz, computadora, cajones, destornillador y llave."),
            DatosDeCuarto(CARPETA_CUARTOS, 2, "Cuarto2_Oficina", "Dirección", 4f,
                "Oficina del director: el reloj real entre cinco, el libro y el cuadro dan el código del teclado."),
            DatosDeCuarto(CARPETA_CUARTOS, 3, "Cuarto3_Computacion", "Sala de Computación", 4f,
                "De noche: luces, red, computadoras y la clave de la consola."),
            DatosDeCuarto(CARPETA_CUARTOS, 4, "Cuarto4_Laboratorio", "Laboratorio", 4f,
                "Fusibles, campana, gas, ensayo a la llama y la tarjeta del docente para salir.")
        };
        AssetDatabase.SaveAssets();
        return cuartos;
    }

    static RoomData DatosDeCuarto(string carpeta, int numero, string raiz, string nombre, float minutos, string descripcion)
    {
        string ruta = carpeta + "/" + raiz + ".asset";
        var datos = AssetDatabase.LoadAssetAtPath<RoomData>(ruta);
        if (datos == null)
        {
            datos = ScriptableObject.CreateInstance<RoomData>();
            datos.numeroCuarto = numero;
            datos.nombre = nombre;
            datos.tiempoSugerido = minutos;
            datos.descripcion = descripcion;
            AssetDatabase.CreateAsset(datos, ruta);
        }

        // Los acertijos que están armados en ese cuarto, en el orden de la escena
        datos.acertijos.Clear();
        var cuarto = GameObject.Find(raiz);
        if (cuarto == null)
        {
            Debug.LogWarning("ConstructorSistema: no está " + raiz + ": el RoomData del Cuarto " + numero + " queda sin acertijos.");
        }
        else
        {
            foreach (PuzzleBase acertijo in cuarto.GetComponentsInChildren<PuzzleBase>(true))
                if (acertijo.datos != null && !datos.acertijos.Contains(acertijo.datos)) datos.acertijos.Add(acertijo.datos);
        }
        EditorUtility.SetDirty(datos);
        return datos;
    }

    // ------------------------------------------------------------------ movimiento del jugador

    const string RUTA_VINETA = "Assets/Samples/XR Interaction Toolkit/3.5.1/Starter Assets/TunnelingVignette/TunnelingVignette.prefab";

    // Le pone al XR Origin el ModoDeMovimiento (por defecto solo teletransporte; caminar con el
    // joystick izquierdo se activa en Opciones) y, debajo de la cámara, la viñeta de confort del XR
    // Interaction Toolkit: oscurece los bordes de la vista solo mientras se camina con el joystick.
    static void PrepararMovimiento()
    {
        var origen = Object.FindAnyObjectByType<XROrigin>();
        if (origen == null || origen.Camera == null)
        {
            Debug.LogWarning("ConstructorSistema: no está el XR Origin: no se configuró el movimiento.");
            return;
        }

        var modo = origen.GetComponent<ModoDeMovimiento>();
        if (modo == null) modo = Undo.AddComponent<ModoDeMovimiento>(origen.gameObject);
        foreach (var mano in origen.GetComponentsInChildren<ControllerInputActionManager>(true))
        {
            if (mano.name.Contains("Left")) modo.manoIzquierda = mano;
            if (mano.name.Contains("Right")) modo.manoDerecha = mano;   // para el giro fluido
        }
        if (modo.manoIzquierda == null || modo.manoDerecha == null)
            Debug.LogWarning("ConstructorSistema: no encontré los dos controles del XR Origin (se buscan solos al dar Play).");
        EditorUtility.SetDirty(modo);

        var caminar = origen.GetComponentInChildren<ContinuousMoveProvider>(true);
        var vineta = origen.Camera.GetComponentInChildren<TunnelingVignetteController>(true);
        if (vineta == null)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RUTA_VINETA);
            if (prefab == null)
            {
                Debug.LogWarning("ConstructorSistema: falta " + RUTA_VINETA + ": caminar funciona, pero sin la viñeta de confort.");
                return;
            }
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, origen.Camera.transform);
            Undo.RegisterCreatedObjectUndo(go, "Viñeta de confort");
            vineta = go.GetComponent<TunnelingVignetteController>();
        }
        vineta.locomotionVignetteProviders = new List<LocomotionVignetteProvider>
        {
            new LocomotionVignetteProvider { locomotionProvider = caminar, enabled = caminar != null }
        };
        EditorUtility.SetDirty(vineta);
    }

    // ------------------------------------------------------------------ puertas y puntos de inicio

    // Dónde aparece el jugador al cargar cada cuarto: en su entrada, mirando hacia adentro (los
    // mismos lugares que usa el menú "Llevar jugador al Cuarto N"). El del Cuarto 1 es donde
    // empieza el juego: ahí llevan "Volver a jugar" y "Reiniciar juego".
    static Transform[] ArmarPuntosDeInicio(Transform sistema)
    {
        Transform g = Grupo("Puntos_De_Inicio", sistema, Vector3.zero);
        Transform inicio1 = Grupo("Inicio_Cuarto1", g, Vector3.zero);
        inicio1.SetPositionAndRotation(Cuarto1Builder.InicioJugador, Cuarto1Builder.GiroInicioJugador);
        return new[]
        {
            inicio1,
            PuntoDeInicio(g, "Inicio_Cuarto2", "Cuarto2_Oficina", new Vector3(1.35f, 0f, 0.6f)),
            PuntoDeInicio(g, "Inicio_Cuarto3", "Cuarto3_Computacion", new Vector3(5f, 0f, 0.6f)),
            PuntoDeInicio(g, "Inicio_Cuarto4", "Cuarto4_Laboratorio", new Vector3(1.35f, 0f, 1f))
        };
    }

    static Transform PuntoDeInicio(Transform padre, string nombre, string cuarto, Vector3 enElCuarto)
    {
        var raiz = GameObject.Find(cuarto);
        if (raiz == null)
        {
            Debug.LogError("ConstructorSistema: no está " + cuarto + ". Construye los cuartos primero.");
            return null;
        }
        Transform punto = Grupo(nombre, padre, Vector3.zero);
        // TransformPoint y no una suma: los cuartos están girados (ver DisposicionCuartos)
        punto.SetPositionAndRotation(raiz.transform.TransformPoint(enElCuarto), raiz.transform.rotation);
        return punto;
    }

    static void ConectarPuertas(GameManager juego)
    {
        ConectarPuerta("Cuarto1_Recepcion/Puerta_Direccion/Puerta_Bisagra", juego, 2);
        ConectarPuerta("Cuarto2_Oficina/Puerta_Salida/Bisagra", juego, 3);
        ConectarPuerta("Cuarto3_Computacion/Puerta_Salida/Puerta_Bisagra", juego, 4);

        Door salida = BuscarPuerta("Cuarto4_Laboratorio/Puerta_Salida/Bisagra");
        if (salida == null) return;
        LimpiarConexiones(salida);
        UnityEventTools.AddVoidPersistentListener(salida.alAbrirse, new UnityAction(juego.SalidaAbierta));
        EditorUtility.SetDirty(salida);
    }

    static void ConectarPuerta(string ruta, GameManager juego, int cuartoSiguiente)
    {
        Door puerta = BuscarPuerta(ruta);
        if (puerta == null) return;
        LimpiarConexiones(puerta);
        UnityEventTools.AddIntPersistentListener(puerta.alAbrirse, new UnityAction<int>(juego.LlegarAlCuarto), cuartoSiguiente);
        EditorUtility.SetDirty(puerta);
    }

    static Door BuscarPuerta(string ruta)
    {
        var objeto = GameObject.Find(ruta);
        Door puerta = objeto != null ? objeto.GetComponent<Door>() : null;
        if (puerta == null) Debug.LogError("ConstructorSistema: no encontré la puerta " + ruta + " (¿se construyeron los cuartos?).");
        return puerta;
    }

    // Si se rearma el sistema sin rearmar los cuartos, las puertas conservan las conexiones al
    // GameManager anterior (ya borrado): se sacan antes de conectar el nuevo
    static void LimpiarConexiones(Door puerta)
    {
        for (int i = puerta.alAbrirse.GetPersistentEventCount() - 1; i >= 0; i--)
        {
            Object destino = puerta.alAbrirse.GetPersistentTarget(i);
            if (destino == null || destino is GameManager)
                UnityEventTools.RemovePersistentListener(puerta.alAbrirse, i);
        }
    }

    // ------------------------------------------------------------------ reloj en pantalla

    // Tarjeta de 13 x 5 cm en la esquina de arriba a la izquierda de la vista (HUDReloj la cuelga
    // de la cámara). Fondo oscuro semitransparente con una franja de color a la izquierda. A la
    // izquierda "TIEMPO" y el tiempo grande; a la derecha "CUARTO", "1/4" y una rayita por cuarto;
    // abajo, la barra que se vacía. Debajo de la tarjeta aparecen los avisos.
    static HUDReloj ArmarHUD(Transform sistema, TimerController reloj)
    {
        Transform raiz = Grupo("HUD_Reloj", sistema, Vector3.zero);
        var hud = raiz.gameObject.AddComponent<HUDReloj>();
        hud.reloj = reloj;

        Transform t = Grupo("Tarjeta", raiz, Vector3.zero);
        Caja("Fondo", t, Vector3.zero, new Vector3(0.13f, 0.05f, 0.002f), mHUD);
        GameObject acento = Caja("Acento", t, new Vector3(-0.0635f, 0f, -0.0012f), new Vector3(0.003f, 0.05f, 0.001f), mAmbar);
        Rotulo("Etiqueta", t, new Vector3(-0.03f, 0.0155f, -0.0013f), new Vector2(0.056f, 0.008f), "TIEMPO", Suave,
               TextAlignmentOptions.Left, true).characterSpacing = 4f;
        TextMeshPro tiempo = Rotulo("Tiempo", t, new Vector3(-0.03f, -0.001f, -0.0013f), new Vector2(0.058f, 0.024f), "15:00", Texto,
                                    TextAlignmentOptions.Left, true);
        Caja("Divisor", t, new Vector3(0.009f, 0.001f, -0.0012f), new Vector3(0.0008f, 0.034f, 0.0005f), mGris);
        Rotulo("Etiqueta_Cuarto", t, new Vector3(0.036f, 0.0155f, -0.0013f), new Vector2(0.044f, 0.008f), "CUARTO", Suave,
               TextAlignmentOptions.Center, true).characterSpacing = 4f;
        TextMeshPro cuarto = Rotulo("Cuarto", t, new Vector3(0.036f, 0.0015f, -0.0013f), new Vector2(0.044f, 0.016f), "1/4", Texto,
                                    TextAlignmentOptions.Center, true);
        hud.fichasCuartos = new Renderer[GameManager.TOTAL_CUARTOS];
        for (int i = 0; i < GameManager.TOTAL_CUARTOS; i++)
            hud.fichasCuartos[i] = Caja("Ficha_" + (i + 1), t, new Vector3(0.036f + (i - 1.5f) * 0.0105f, -0.0105f, -0.0012f),
                                        new Vector3(0.0085f, 0.0022f, 0.0005f), mGris).GetComponent<Renderer>();
        Caja("Barra_Fondo", t, new Vector3(0.0015f, -0.0205f, -0.0012f), new Vector3(0.122f, 0.0018f, 0.0005f), mGris);
        Transform barra = Grupo("Barra", t, new Vector3(-0.0595f, -0.0205f, -0.0014f));
        GameObject relleno = Caja("Relleno", barra, new Vector3(0.061f, 0f, 0f), new Vector3(0.122f, 0.0018f, 0.0005f), mAmbar);

        Transform aviso = Grupo("Aviso", raiz, new Vector3(0f, -0.042f, 0f));
        Caja("Fondo", aviso, Vector3.zero, new Vector3(0.13f, 0.024f, 0.002f), mHUD);
        GameObject franja = Caja("Franja", aviso, new Vector3(-0.0635f, 0f, -0.0012f), new Vector3(0.003f, 0.024f, 0.001f), mAmbar);
        TextMeshPro textoAviso = Rotulo("Texto", aviso, new Vector3(0.002f, 0f, -0.0013f), new Vector2(0.118f, 0.019f),
                                        "PROGRESO GUARDADO", Texto, TextAlignmentOptions.Center, true);
        textoAviso.textWrappingMode = TextWrappingModes.Normal;

        hud.tarjeta = t.gameObject;
        hud.textoTiempo = tiempo;
        hud.textoCuarto = cuarto;
        hud.acento = acento.GetComponent<Renderer>();
        hud.barra = barra;
        hud.rellenoBarra = relleno.GetComponent<Renderer>();
        hud.aviso = aviso.gameObject;
        hud.textoAviso = textoAviso;
        hud.franjaAviso = franja.GetComponent<Renderer>();
        EditorUtility.SetDirty(hud);
        return hud;
    }

    // ------------------------------------------------------------------ menú de pausa

    // 1.04 x 0.66 m a 1.1 m de la cabeza. Arriba: "PAUSA" y cómo cerrarlo. A la izquierda, una
    // tarjeta con la partida (tiempo, cuarto con sus cuatro fichas, acertijos). A la derecha, los
    // botones (los dos de abajo, lado a lado). Abajo, la línea de estado (la partida guardada o lo
    // que acaba de pasar).
    static PauseMenu ArmarMenuPausa(Transform sistema)
    {
        Transform raiz = Grupo("Menu_Pausa", sistema, Vector3.zero);
        var panel = raiz.gameObject.AddComponent<PanelFlotante>();
        var menu = raiz.gameObject.AddComponent<PauseMenu>();
        Transform c = Grupo("Contenido", raiz, Vector3.zero);
        panel.contenido = c;
        panel.anchoPanel = 1.04f;
        panel.oscurecedor = Oscurecedor(sistema, "Oscurecer_Pausa");
        panel.oscuridad = 0.62f;

        Placa(c, 1.04f, 0.66f, mAmbar);
        Rotulo("Titulo", c, new Vector3(-0.26f, 0.262f, -0.008f), new Vector2(0.44f, 0.07f), "PAUSA", Texto, TextAlignmentOptions.Left, true);
        TextMeshPro sub = Rotulo("Subtitulo", c, new Vector3(-0.26f, 0.214f, -0.008f), new Vector2(0.44f, 0.024f),
                                 "ESCAPE ROOM  ·  EL COLEGIO", Suave, TextAlignmentOptions.Left, false);
        sub.characterSpacing = 6f;
        Rotulo("Ayuda", c, new Vector3(0.26f, 0.25f, -0.008f), new Vector2(0.44f, 0.024f),
               "Para volver: botón de menú o Y  ·  Esc", Suave, TextAlignmentOptions.Right, false);
        Caja("Separador", c, new Vector3(0f, 0.19f, -0.007f), new Vector3(0.96f, 0.002f, 0.002f), mTarjeta);

        // La tarjeta de la partida
        const float X = -0.26f;
        Caja("Tarjeta", c, new Vector3(X, -0.03f, -0.008f), new Vector3(0.46f, 0.38f, 0.004f), mTarjeta);
        Rotulo("Etiqueta_Tiempo", c, new Vector3(X, 0.125f, -0.011f), new Vector2(0.4f, 0.022f), "TIEMPO RESTANTE", Suave,
               TextAlignmentOptions.Center, true).characterSpacing = 4f;
        menu.textoTiempo = Rotulo("Tiempo", c, new Vector3(X, 0.055f, -0.011f), new Vector2(0.4f, 0.1f), "15:00", Texto,
                                  TextAlignmentOptions.Center, true);
        Caja("Divisor", c, new Vector3(X, -0.012f, -0.011f), new Vector3(0.38f, 0.002f, 0.001f), mGris);
        menu.textoCuarto = Rotulo("Cuarto", c, new Vector3(X, -0.045f, -0.011f), new Vector2(0.4f, 0.022f), "CUARTO 1 DE 4",
                                  Suave, TextAlignmentOptions.Center, true);
        menu.textoNombreCuarto = Rotulo("Nombre_Cuarto", c, new Vector3(X, -0.083f, -0.011f), new Vector2(0.4f, 0.038f),
                                        "Recepción", Texto, TextAlignmentOptions.Center, false);
        menu.fichasCuartos = new Renderer[GameManager.TOTAL_CUARTOS];
        for (int i = 0; i < GameManager.TOTAL_CUARTOS; i++)
        {
            float x = X + (i - 1.5f) * 0.097f;
            menu.fichasCuartos[i] = Caja("Ficha_" + (i + 1), c, new Vector3(x, -0.13f, -0.011f),
                                         new Vector3(0.085f, 0.012f, 0.001f), mGris).GetComponent<Renderer>();
        }
        menu.textoAcertijos = Rotulo("Acertijos", c, new Vector3(X, -0.178f, -0.011f), new Vector2(0.4f, 0.024f),
                                     "0 de 11 acertijos resueltos", Suave, TextAlignmentOptions.Center, false);

        // Los botones, a la derecha
        const float XB = 0.26f, ANCHO = 0.46f, ALTO = 0.078f;
        menu.botonReanudar = Boton("Boton_Reanudar", c, new Vector3(XB, 0.12f, 0f), ANCHO, ALTO, mAmbar, Oscuro,
                                   "REANUDAR", "Volver al juego");
        menu.botonGuardar = Boton("Boton_Guardar", c, new Vector3(XB, 0.025f, 0f), ANCHO, ALTO, mSecundario, Texto,
                                  "GUARDAR PARTIDA", "Cuarto 1 · 15:00");
        menu.botonCargar = Boton("Boton_Cargar", c, new Vector3(XB, -0.07f, 0f), ANCHO, ALTO, mSecundario, Texto,
                                 "CARGAR PARTIDA", "No hay partida guardada");
        // Abajo, lado a lado: reiniciar y volver al menú de inicio
        const float MEDIO = (ANCHO - 0.01f) / 2f;
        menu.botonReiniciar = Boton("Boton_Reiniciar", c, new Vector3(XB - (MEDIO + 0.01f) / 2f, -0.165f, 0f), MEDIO, ALTO, mSecundario, Texto,
                                    "REINICIAR", "Desde el Cuarto 1");
        menu.botonMenuPrincipal = Boton("Boton_Menu_Principal", c, new Vector3(XB + (MEDIO + 0.01f) / 2f, -0.165f, 0f), MEDIO, ALTO, mSecundario, Texto,
                                        "MENÚ PRINCIPAL", "Pantalla de inicio");

        Caja("Separador_Abajo", c, new Vector3(0f, -0.24f, -0.007f), new Vector3(0.96f, 0.002f, 0.002f), mTarjeta);
        menu.textoEstado = Rotulo("Estado", c, new Vector3(0f, -0.278f, -0.008f), new Vector2(0.96f, 0.028f),
                                  "Todavía no hay una partida guardada", Suave, TextAlignmentOptions.Center, false);

        menu.panel = panel;
        menu.audioMenu = audioUI;
        c.gameObject.SetActive(false);   // se prende al abrir el menú
        EditorUtility.SetDirty(panel);
        EditorUtility.SetDirty(menu);
        return menu;
    }

    // ------------------------------------------------------------------ tiempo agotado

    // 0.94 x 0.6 m, con franja roja. Arriba "00:00" en rojo y "TIEMPO AGOTADO"; en el medio hasta
    // dónde llegó; abajo los tres botones lado a lado.
    static PantallaTiempoAgotado ArmarTiempoAgotado(Transform sistema)
    {
        Transform raiz = Grupo("Pantalla_Tiempo_Agotado", sistema, Vector3.zero);
        var panel = raiz.gameObject.AddComponent<PanelFlotante>();
        var pantalla = raiz.gameObject.AddComponent<PantallaTiempoAgotado>();
        Transform c = Grupo("Contenido", raiz, Vector3.zero);
        panel.contenido = c;
        panel.anchoPanel = 0.94f;
        panel.oscurecedor = Oscurecedor(sistema, "Oscurecer_TiempoAgotado");
        panel.oscuridad = 0.85f;

        Color rojo = new Color(0.94f, 0.3f, 0.32f);
        Placa(c, 0.94f, 0.6f, mPeligro);
        Rotulo("Reloj", c, new Vector3(0f, 0.19f, -0.008f), new Vector2(0.4f, 0.1f), "00:00", rojo, TextAlignmentOptions.Center, true);
        Rotulo("Titulo", c, new Vector3(0f, 0.095f, -0.008f), new Vector2(0.8f, 0.068f), "TIEMPO AGOTADO", Texto,
               TextAlignmentOptions.Center, true).characterSpacing = 6f;
        Rotulo("Subtitulo", c, new Vector3(0f, 0.035f, -0.008f), new Vector2(0.8f, 0.03f),
               "Se cumplieron los 15 minutos y el colegio quedó cerrado.", Suave, TextAlignmentOptions.Center, false);

        Caja("Tarjeta", c, new Vector3(0f, -0.035f, -0.008f), new Vector3(0.84f, 0.07f, 0.004f), mTarjeta);
        pantalla.textoProgreso = Rotulo("Progreso", c, new Vector3(0f, -0.035f, -0.011f), new Vector2(0.8f, 0.03f),
                                        "Llegaste al Cuarto 1 de 4", Texto, TextAlignmentOptions.Center, false);

        pantalla.botonCargar = Boton("Boton_Cargar", c, new Vector3(-0.285f, -0.155f, 0f), 0.27f, 0.1f, mAmbar, Oscuro,
                                     "CARGAR PARTIDA", "No hay partida guardada");
        pantalla.botonReiniciar = Boton("Boton_Reiniciar", c, new Vector3(0f, -0.155f, 0f), 0.27f, 0.1f, mSecundario, Texto,
                                        "EMPEZAR DE NUEVO", "Desde el Cuarto 1");
        pantalla.botonMenuPrincipal = Boton("Boton_Menu_Principal", c, new Vector3(0.285f, -0.155f, 0f), 0.27f, 0.1f, mSecundario, Texto,
                                            "MENÚ PRINCIPAL", "Pantalla de inicio");
        Rotulo("Ayuda", c, new Vector3(0f, -0.255f, -0.008f), new Vector2(0.84f, 0.024f),
               "Apuntá con el rayo del control y apretá el botón de agarre", Suave, TextAlignmentOptions.Center, false);

        pantalla.panel = panel;
        pantalla.audioMenu = audioUI;
        c.gameObject.SetActive(false);   // se prende cuando se acaba el tiempo
        EditorUtility.SetDirty(panel);
        EditorUtility.SetDirty(pantalla);
        return pantalla;
    }

    // ------------------------------------------------------------------ piezas

    // El fondo de un panel: borde, placa y la franja de color de arriba
    static void Placa(Transform c, float ancho, float alto, Material franja)
    {
        Caja("Borde", c, new Vector3(0f, 0f, 0.003f), new Vector3(ancho + 0.012f, alto + 0.012f, 0.008f), mBorde, true);
        Caja("Fondo", c, Vector3.zero, new Vector3(ancho, alto, 0.012f), mFondo);
        Caja("Franja", c, new Vector3(0f, alto / 2f - 0.004f, -0.007f), new Vector3(ancho, 0.008f, 0.002f), franja);
    }

    // Un botón: la raíz tiene el collider y el XRSimpleInteractable (en la capa de los menús);
    // la "Vista" tiene el fondo y los dos textos, y es lo que se anima.
    static BotonMenu Boton(string nombre, Transform padre, Vector3 pos, float ancho, float alto, Material material,
                           Color colorTexto, string texto, string detalle)
    {
        Transform raiz = Grupo(nombre, padre, pos);
        var colision = raiz.gameObject.AddComponent<BoxCollider>();
        colision.center = new Vector3(0f, 0f, -0.015f);
        colision.size = new Vector3(ancho, alto, 0.03f);
        var interactuable = raiz.gameObject.AddComponent<XRSimpleInteractable>();
        interactuable.interactionLayers = GameManager.MascaraMenu;
        var boton = raiz.gameObject.AddComponent<BotonMenu>();

        Transform vista = Grupo("Vista", raiz, new Vector3(0f, 0f, -0.012f));
        GameObject fondo = Caja("Fondo", vista, Vector3.zero, new Vector3(ancho, alto, 0.008f), material);
        Color colorDetalle = new Color(colorTexto.r, colorTexto.g, colorTexto.b, 0.72f);
        boton.texto = Rotulo("Texto", vista, new Vector3(0f, alto * 0.16f, -0.0055f), new Vector2(ancho - 0.05f, alto * 0.38f),
                             texto, colorTexto, TextAlignmentOptions.Center, true);
        boton.texto.characterSpacing = 3f;
        boton.detalle = Rotulo("Detalle", vista, new Vector3(0f, -alto * 0.24f, -0.0055f), new Vector2(ancho - 0.05f, alto * 0.25f),
                               detalle, colorDetalle, TextAlignmentOptions.Center, false);

        boton.vista = vista;
        boton.fondo = fondo.GetComponent<Renderer>();
        boton.audioMenu = audioUI;
        EditorUtility.SetDirty(interactuable);
        EditorUtility.SetDirty(boton);
        return boton;
    }

    // La esfera que oscurece el cuarto detrás del menú: rodea la cabeza, se ve desde adentro y no
    // tiene collider (no le corta el rayo al control)
    static Renderer Oscurecedor(Transform sistema, string nombre)
    {
        var esfera = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        esfera.name = nombre;
        Object.DestroyImmediate(esfera.GetComponent<Collider>());
        esfera.transform.SetParent(sistema, false);
        esfera.transform.localScale = Vector3.one * 0.7f;
        var r = esfera.GetComponent<Renderer>();
        r.sharedMaterial = mOscurecer;
        SinSombras(r);
        esfera.SetActive(false);   // la prende su panel
        return r;
    }

    static Transform Grupo(string nombre, Transform padre, Vector3 pos)
    {
        var go = new GameObject(nombre);
        go.transform.SetParent(padre, false);
        go.transform.localPosition = pos;
        return go.transform;
    }

    // Un cubo de la interfaz. Sin collider (salvo que se pida): no tiene que cortarle el rayo a
    // nada ni chocar con la cabeza del jugador.
    static GameObject Caja(string nombre, Transform padre, Vector3 pos, Vector3 tam, Material mat, bool colision = false)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = nombre;
        go.transform.SetParent(padre, false);
        go.transform.localPosition = pos;
        go.transform.localScale = tam;
        if (!colision) Object.DestroyImmediate(go.GetComponent<Collider>());
        var r = go.GetComponent<Renderer>();
        r.sharedMaterial = mat;
        SinSombras(r);
        return go;
    }

    static void SinSombras(Renderer r)
    {
        r.shadowCastingMode = ShadowCastingMode.Off;
        r.receiveShadows = false;
        r.lightProbeUsage = LightProbeUsage.Off;
        r.reflectionProbeUsage = ReflectionProbeUsage.Off;
    }

    // Un texto de la interfaz, de frente al jugador (TextMeshPro se lee desde su -Z). El tamaño
    // de la letra se ajusta solo a la caja: la caja es la que define qué tan grande se ve.
    static TextMeshPro Rotulo(string nombre, Transform padre, Vector3 pos, Vector2 caja, string texto, Color color,
                              TextAlignmentOptions alineacion, bool negrita)
    {
        var go = new GameObject(nombre);
        var t = go.AddComponent<TextMeshPro>();
        go.transform.SetParent(padre, false);
        go.transform.localPosition = pos;
        t.rectTransform.sizeDelta = caja;
        t.text = texto;
        t.color = color;
        t.alignment = alineacion;
        t.textWrappingMode = TextWrappingModes.NoWrap;
        t.enableAutoSizing = true;
        t.fontSizeMin = 0.01f;
        t.fontSizeMax = 12f;
        t.fontStyle = negrita ? FontStyles.Bold : FontStyles.Normal;
        t.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
        return t;
    }

    // ------------------------------------------------------------------ materiales y capa

    // Todos son "Unlit" (no les afecta la luz del cuarto: se leen igual a oscuras) y transparentes,
    // cada uno con su lugar en el orden de dibujado. Los de las piezas escriben profundidad, así
    // el rayo del control y los textos quedan bien delante o detrás de ellos.
    static void CrearMateriales()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Materials")) AssetDatabase.CreateFolder("Assets", "Materials");
        if (!AssetDatabase.IsValidFolder(CARPETA)) AssetDatabase.CreateFolder("Assets/Materials", "Sistema");

        mOscurecer = MaterialUI("UI_Oscurecer", new Color(0f, 0f, 0f, 1f), -6, false, true);
        mBorde = MaterialUI("UI_Borde", new Color(0.17f, 0.23f, 0.32f), -5, true, false);
        mFondo = MaterialUI("UI_Fondo", new Color(0.043f, 0.071f, 0.125f), -4, true, false);
        mHUD = MaterialUI("UI_FondoHUD", new Color(0.043f, 0.071f, 0.125f, 0.8f), -4, true, false);   // semitransparente
        mTarjeta = MaterialUI("UI_Tarjeta", new Color(0.106f, 0.145f, 0.212f), -3, true, false);
        mGris = MaterialUI("UI_Gris", new Color(0.23f, 0.28f, 0.36f), -2, true, false);
        mAmbar = MaterialUI("UI_Ambar", new Color(0.96f, 0.65f, 0.14f), -1, true, false);
        mSecundario = MaterialUI("UI_Secundario", new Color(0.17f, 0.22f, 0.31f), -1, true, false);
        mPeligro = MaterialUI("UI_Peligro", new Color(0.85f, 0.26f, 0.29f), -1, true, false);
        AssetDatabase.SaveAssets();
    }

    static Material MaterialUI(string nombre, Color color, int desplazamientoCola, bool escribeProfundidad, bool dobleCara)
    {
        string ruta = CARPETA + "/" + nombre + ".mat";
        // Shader propio (Assets/Shaders/Interfaz.shader): URP le apagaba la profundidad a sus
        // materiales transparentes y los carteles de atrás se veían a través del reloj
        var shader = Shader.Find("EscapeRoom/Interfaz");
        if (shader == null)
        {
            Debug.LogError("Falta el shader EscapeRoom/Interfaz (Assets/Shaders/Interfaz.shader). Espera a que Unity lo importe.");
            shader = Shader.Find("Universal Render Pipeline/Unlit");
        }
        var m = AssetDatabase.LoadAssetAtPath<Material>(ruta);
        if (m == null)
        {
            m = new Material(shader);
            AssetDatabase.CreateAsset(m, ruta);
        }
        else if (m.shader != shader)
        {
            m.shader = shader;
        }
        m.SetColor("_BaseColor", color);
        m.SetFloat("_ZWrite", escribeProfundidad ? 1f : 0f);
        m.SetFloat("_Cull", dobleCara ? (float)CullMode.Off : (float)CullMode.Back);
        m.renderQueue = (int)RenderQueue.Transparent + desplazamientoCola;
        EditorUtility.SetDirty(m);
        return m;
    }

    // Le pone nombre a la capa de interacción de los menús en los ajustes de XRI (para que se vea
    // como "Menu" en el Inspector). Si ese lugar ya lo usa otra capa, solo avisa.
    static void NombrarCapaMenu()
    {
        var ajustes = AssetDatabase.LoadAssetAtPath<ScriptableObject>(AJUSTES_CAPAS);
        if (ajustes == null) return;
        var so = new SerializedObject(ajustes);
        var nombres = so.FindProperty("m_LayerNames");
        if (nombres == null || nombres.arraySize <= GameManager.CAPA_MENU) return;
        var capa = nombres.GetArrayElementAtIndex(GameManager.CAPA_MENU);
        if (string.IsNullOrEmpty(capa.stringValue))
        {
            capa.stringValue = "Menu";
            so.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();
        }
        else if (capa.stringValue != "Menu")
        {
            Debug.LogWarning("La capa de interacción " + GameManager.CAPA_MENU + " se llama \"" + capa.stringValue +
                             "\": los botones de los menús la usan igual.");
        }
    }
}
