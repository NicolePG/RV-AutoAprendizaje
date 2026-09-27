using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

// Segunda parte de ConstructorSistema: el MENÚ DE INICIO (MainMenu y EfectosMenuInicio). Se arma
// con el mismo menú: Escape Room > Construir los 4 cuartos (o "Construir menús, reloj y guardado").
//
// Es una pantalla grande de terror (casi 2 x 1.1 m, a 1.35 m del jugador), bien distinta del menú
// de pausa:
//  - de fondo, la foto de un aula abandonada (Assets/Menu, de Unsplash: ver CREDITOS.txt), oscura,
//    casi sin color y teñida de rojo, con líneas de monitor y una línea de interferencia que baja;
//  - el título "ESCAPE BACKROOM" (BACKROOM en rojo, con brillo, parpadea) y "Protocolo Apagón";
//  - arriba, la línea de alerta con su LED ("ALERTA // CORTE DE ENERGÍA") y la batería de respaldo;
//  - a la derecha, sobre un degradado negro, las opciones: número, LED rojo, nombre y flecha. Con
//    el rayo encima la placa se pone roja, el LED se prende y la opción se corre a la derecha;
//  - un marco de acero oscuro con tornillos y una baliza roja arriba;
//  - tipografías de Google Fonts (licencia OFL, en Assets/Fuentes): Bebas Neue para títulos y
//    opciones, Barlow Condensed para los textos y Share Tech Mono para las líneas "de sistema".
// Las tres páginas (principal, opciones y cómo jugar) comparten la pantalla; opciones y cómo jugar
// la tapan con un velo oscuro. Mientras está abierto: "BACKROOM" parpadea, la imagen tiene fallas
// de señal, la recepción late en rojo y se oye un latido grave (EfectosMenuInicio).
//
// Las medidas son de la pantalla SIN agrandar (1.24 x 0.7): todo cuelga del grupo "Panel", que la
// agranda ESCALA veces. El centro es (0, 0), +X a la derecha del jugador y el frente mira hacia -Z.
public static partial class ConstructorSistema
{
    const string CARPETA_FUENTES = "Assets/Fuentes";
    const string RUTA_FOTO_MENU = "Assets/Menu/aula_abandonada_menu.jpg";
    const float ESCALA = 1.55f;
    const float PANTALLA_ANCHO = 1.24f, PANTALLA_ALTO = 0.7f;
    const float BORDE = 0.025f;
    const float PANEL_ANCHO = PANTALLA_ANCHO + BORDE * 2f, PANEL_ALTO = PANTALLA_ALTO + BORDE * 2f;

    // Los caracteres que llevan las tipografías (el español incluido)
    const string CARACTERES = " !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~" +
                              "ÁÉÍÓÚÑÜáéíóúñü¿¡·–—°";

    static readonly Color Hueso = new Color(0.93f, 0.9f, 0.85f);
    static readonly Color HuesoTenue = new Color(0.66f, 0.62f, 0.58f);
    static readonly Color RojoTexto = new Color(0.95f, 0.27f, 0.24f);
    static readonly Color RojoTenue = new Color(0.68f, 0.24f, 0.22f);
    static readonly Color RojoTitulo = new Color(0.9f, 0.1f, 0.1f);

    static TMP_FontAsset fTitulo, fTexto, fSistema;
    static Material mMarco, mBisel, mTornillo, mFoto, mDegradeDerecha, mDegradeAbajo, mVelo, mLineas, mInterferencia,
                    mPlaca, mRojoPlaca, mRojoOscuro, mRojoLinea;

    static MainMenu ArmarMenuInicio(Transform sistema)
    {
        PrepararEstiloEmergencia();

        Transform raiz = Grupo("Menu_Inicio", sistema, Vector3.zero);
        var panel = raiz.gameObject.AddComponent<PanelFlotante>();
        var menu = raiz.gameObject.AddComponent<MainMenu>();
        var efectos = raiz.gameObject.AddComponent<EfectosMenuInicio>();
        Transform c = Grupo("Contenido", raiz, Vector3.zero);
        panel.contenido = c;
        panel.anchoPanel = PANEL_ANCHO * ESCALA;
        panel.altoPanel = PANEL_ALTO * ESCALA;   // así revisa también el mostrador y las lámparas
        panel.distancia = 1.35f;
        panel.oscurecedor = Oscurecedor(sistema, "Oscurecer_Inicio");
        panel.oscuridad = 0.5f;
        panel.colorOscuro = new Color(0.14f, 0f, 0.01f);   // el cuarto se ve rojo muy oscuro

        // Todo cuelga de "Panel", agrandado: así las medidas de abajo son las de la pantalla chica
        Transform g = Grupo("Panel", c, Vector3.zero);
        g.localScale = Vector3.one * ESCALA;

        ArmarPantallaDeTerror(g, efectos);

        // --- Página principal: abajo a la izquierda el título; a la derecha las cinco opciones
        Transform p = Grupo("Pagina_Principal", g, Vector3.zero);
        const float XI = -0.285f;   // centro de la columna izquierda (va de -0.58 a 0.01)
        TextoMenu("Sobretitulo", p, new Vector3(XI, 0.078f, -0.006f), new Vector2(0.59f, 0.022f), "PROTOCOLO APAGÓN  //  UN COLEGIO SIN LUZ",
                  RojoTexto, fSistema, TextAlignmentOptions.Left, 3f);
        TextoMenu("Titulo_1", p, new Vector3(XI, 0.005f, -0.006f), new Vector2(0.59f, 0.11f), "ESCAPE", Hueso, fTitulo,
                  TextAlignmentOptions.Left, 8f);
        TextMeshPro backroom = TextoMenu("Titulo_2", p, new Vector3(XI, -0.108f, -0.006f), new Vector2(0.59f, 0.13f), "BACKROOM", RojoTitulo,
                                         fTitulo, TextAlignmentOptions.Left, 8f);
        Material brillo = MaterialTituloBrillante();
        if (brillo != null) backroom.fontSharedMaterial = brillo;
        Caja("Raya", p, new Vector3(-0.545f, -0.185f, -0.005f), new Vector3(0.07f, 0.004f, 0.001f), mRojoPlaca);
        TextoMenu("Premisa", p, new Vector3(XI, -0.222f, -0.006f), new Vector2(0.59f, 0.042f),
                  "Un corte de energía te dejó encerrado en el colegio. Tenés 15 minutos para salir.",
                  new Color(0.85f, 0.82f, 0.78f), fTexto, TextAlignmentOptions.Left);
        menu.textoPartida = TextoMenu("Partida", p, new Vector3(XI, -0.265f, -0.006f), new Vector2(0.59f, 0.032f),
                                      "Todavía no hay una partida guardada", HuesoTenue, fSistema, TextAlignmentOptions.Left, 1f);

        const float XD = 0.345f, ANCHO_OPCION = 0.46f, ALTO_OPCION = 0.078f;
        TextoMenu("Titulo_Opciones", p, new Vector3(XD, 0.255f, -0.006f), new Vector2(ANCHO_OPCION, 0.02f), "SELECCIONÁ UNA OPCIÓN",
                  RojoTenue, fSistema, TextAlignmentOptions.Left, 3f);
        menu.botonNuevaPartida = BotonEmergencia("Boton_Nueva_Partida", p, new Vector3(XD, 0.2f, 0f), ANCHO_OPCION, ALTO_OPCION, "01",
                                                 "NUEVA PARTIDA", "Desde el Cuarto 1  ·  15 minutos", true);
        menu.botonContinuar = BotonEmergencia("Boton_Continuar", p, new Vector3(XD, 0.105f, 0f), ANCHO_OPCION, ALTO_OPCION, "02",
                                              "CONTINUAR", "No hay partida guardada", false);
        menu.botonOpciones = BotonEmergencia("Boton_Opciones", p, new Vector3(XD, 0.01f, 0f), ANCHO_OPCION, ALTO_OPCION, "03",
                                             "OPCIONES", "Volumen", false);
        menu.botonControles = BotonEmergencia("Boton_Como_Jugar", p, new Vector3(XD, -0.085f, 0f), ANCHO_OPCION, ALTO_OPCION, "04",
                                              "CÓMO JUGAR", "Controles del visor y del PC", false);
        menu.botonSalir = BotonEmergencia("Boton_Salir", p, new Vector3(XD, -0.18f, 0f), ANCHO_OPCION, ALTO_OPCION, "05",
                                          "SALIR", "Cerrar el juego", false);

        // --- Opciones: el volumen, con "-", "+" y una barra de 10 rayitas
        Transform o = Grupo("Pagina_Opciones", g, Vector3.zero);
        Velo(o);
        TituloDeSeccion(o, "CONFIGURACIÓN  //  SE GUARDA EN EL VISOR", "OPCIONES");
        Caja("Tarjeta", o, new Vector3(0f, -0.005f, -0.004f), new Vector3(0.9f, 0.2f, 0.003f), mPlaca);
        Caja("Tarjeta_Barra", o, new Vector3(0f, 0.0935f, -0.0058f), new Vector3(0.9f, 0.003f, 0.001f), mRojoPlaca);
        TextoMenu("Etiqueta", o, new Vector3(0f, 0.065f, -0.006f), new Vector2(0.5f, 0.022f), "VOLUMEN GENERAL", RojoTexto, fSistema,
                  TextAlignmentOptions.Center, 3f);
        menu.botonMenosVolumen = BotonSimbolo("Boton_Menos", o, new Vector3(-0.37f, -0.02f, 0f), "-");
        menu.botonMasVolumen = BotonSimbolo("Boton_Mas", o, new Vector3(0.37f, -0.02f, 0f), "+");
        menu.barraVolumen = new Renderer[10];
        for (int i = 0; i < 10; i++)
            menu.barraVolumen[i] = Caja("Rayita_" + (i + 1), o, new Vector3(-0.2475f + i * 0.055f, -0.02f, -0.0065f),
                                        new Vector3(0.045f, 0.06f, 0.001f), mRojoOscuro).GetComponent<Renderer>();
        menu.textoVolumen = TextoMenu("Valor", o, new Vector3(0f, -0.078f, -0.006f), new Vector2(0.3f, 0.042f), "100%", Hueso, fTitulo,
                                      TextAlignmentOptions.Center, 4f);
        menu.botonVolverOpciones = BotonEmergencia("Boton_Volver", o, new Vector3(0f, -0.2f, 0f), 0.42f, 0.075f, null,
                                                   "VOLVER", "Al menú principal", false);

        // --- Cómo jugar: los controles del visor (lo que pide el GDD: teletransporte y manos) y los
        // del simulador en el PC. Sin pistas de los acertijos.
        Transform k = Grupo("Pagina_Como_Jugar", g, Vector3.zero);
        Velo(k);
        TituloDeSeccion(k, "MANUAL  //  CONTROLES", "CÓMO JUGAR");
        TextoMenu("Subtitulo", k, new Vector3(0f, 0.125f, -0.006f), new Vector2(1.16f, 0.028f),
                  "Las pistas de cada acertijo están en el cuarto: mirá bien a tu alrededor.", HuesoTenue, fTexto, TextAlignmentOptions.Left);
        const string R = "<color=#E8453C>", F = "</color>   ";
        TarjetaDeControles(k, -0.2975f, "VISOR  //  META QUEST",
            R + "MOVERTE" + F + "joystick derecho adelante y soltá\n" +
            R + "GIRAR" + F + "joystick derecho a los costados\n" +
            R + "AGARRAR" + F + "mantené el botón de agarre\n" +
            R + "BOTONES" + F + "tocalos, o apuntá y apretá agarre\n" +
            R + "AGACHARTE" + F + "agachate de verdad\n" +
            R + "PAUSA" + F + "menú del control izquierdo");
        TarjetaDeControles(k, 0.2975f, "PC  //  SIMULADOR XR",
            R + "MOVERTE" + F + "W A S D\n" +
            R + "MIRAR" + F + "clic derecho y mové el mouse\n" +
            R + "AGARRAR" + F + "G  (herramientas: un toque)\n" +
            R + "CAMBIAR DE MANO" + F + "Tab\n" +
            R + "AGACHARTE" + F + "C      " + R + "CORRER" + F + "SHIFT\n" +
            R + "PAUSA" + F + "Esc");
        menu.botonVolverControles = BotonEmergencia("Boton_Volver", k, new Vector3(0f, -0.215f, 0f), 0.42f, 0.07f, null,
                                                    "VOLVER", "Al menú principal", false);

        // La luz de la alarma: arriba, entre el jugador y el panel. Tiñe de rojo la recepción.
        var luz = new GameObject("Luz_Alarma").AddComponent<Light>();
        luz.transform.SetParent(raiz, false);
        luz.transform.localPosition = new Vector3(0f, 1f, -0.6f);
        luz.type = LightType.Point;
        luz.color = new Color(1f, 0.08f, 0.05f);
        luz.range = 7f;
        luz.intensity = 0f;
        luz.shadows = LightShadows.None;
        luz.lightmapBakeType = LightmapBakeType.Realtime;

        // El zumbido grave de fondo (2D, en loop; el sonido lo arma EfectosMenuInicio al empezar)
        var zumbido = raiz.gameObject.AddComponent<AudioSource>();
        zumbido.playOnAwake = false;
        zumbido.spatialBlend = 0f;
        zumbido.volume = 0.2f;

        menu.panel = panel;
        menu.audioMenu = audioUI;
        menu.paginaPrincipal = p.gameObject;
        menu.paginaOpciones = o.gameObject;
        menu.paginaControles = k.gameObject;
        efectos.panel = panel;
        efectos.tituloParpadeante = backroom;
        efectos.luzAlarma = luz;
        efectos.zumbido = zumbido;
        efectos.audioMenu = audioUI;

        o.gameObject.SetActive(false);
        k.gameObject.SetActive(false);
        c.gameObject.SetActive(false);   // se prende al abrir el menú
        EditorUtility.SetDirty(panel);
        EditorUtility.SetDirty(menu);
        EditorUtility.SetDirty(efectos);
        return menu;
    }

    // Lo que comparten las tres páginas: el marco de acero con tornillos, la baliza, la foto con
    // sus degradados y sus líneas de monitor, la línea de interferencia, la línea de alerta de arriba
    // y la línea de ayuda de abajo
    static void ArmarPantallaDeTerror(Transform g, EfectosMenuInicio efectos)
    {
        Caja("Marco", g, new Vector3(0f, 0f, 0.02f), new Vector3(PANEL_ANCHO, PANEL_ALTO, 0.04f), mMarco, true);
        Caja("Bisel", g, new Vector3(0f, 0f, -0.0005f), new Vector3(PANTALLA_ANCHO + 0.012f, PANTALLA_ALTO + 0.012f, 0.003f), mBisel);
        foreach (float x in new[] { -1f, 1f })
            foreach (float y in new[] { -1f, 1f })
                Tornillo(g, new Vector3(x * (PANEL_ANCHO / 2f - BORDE / 2f), y * (PANEL_ALTO / 2f - BORDE / 2f), -0.001f));

        // La baliza roja arriba del panel (la hace latir EfectosMenuInicio)
        Caja("Baliza_Base", g, new Vector3(0f, PANEL_ALTO / 2f + 0.008f, 0.02f), new Vector3(0.16f, 0.016f, 0.04f), mMarco);
        efectos.baliza = Caja("Baliza", g, new Vector3(0f, PANEL_ALTO / 2f + 0.026f, 0.02f), new Vector3(0.11f, 0.022f, 0.03f),
                              mRojoOscuro).GetComponent<Renderer>();

        // La foto y lo que va encima de ella, de atrás hacia adelante
        efectos.fondo = Caja("Foto", g, new Vector3(0f, 0f, -0.002f), new Vector3(PANTALLA_ANCHO, PANTALLA_ALTO, 0.001f), mFoto)
                        .GetComponent<Renderer>();
        // Dos degradados de pantalla completa (sin bordes que se noten): oscurecen la derecha, donde van
        // las opciones, y la parte de abajo, donde va el título
        Caja("Degrade_Derecha", g, new Vector3(0f, 0f, -0.0028f), new Vector3(PANTALLA_ANCHO, PANTALLA_ALTO, 0.0005f), mDegradeDerecha);
        Caja("Degrade_Abajo", g, new Vector3(0f, 0f, -0.0029f), new Vector3(PANTALLA_ANCHO, PANTALLA_ALTO, 0.0005f), mDegradeAbajo);
        Caja("Franja_Alerta", g, new Vector3(0f, 0.315f, -0.003f), new Vector3(PANTALLA_ANCHO, 0.07f, 0.0005f), mVelo);
        Caja("Lineas_Monitor", g, new Vector3(0f, 0f, -0.0032f), new Vector3(PANTALLA_ANCHO, PANTALLA_ALTO, 0.0005f), mLineas);
        efectos.lineaInterferencia = Caja("Interferencia", g, new Vector3(0f, 0f, -0.0034f), new Vector3(PANTALLA_ANCHO, 0.014f, 0.0005f),
                                          mInterferencia).transform;
        efectos.altoPantalla = PANTALLA_ALTO;

        // La línea de alerta, arriba, con su LED que titila
        const float Y_ALERTA = 0.315f;
        efectos.ledAlerta = Caja("LED_Alerta", g, new Vector3(-0.592f, Y_ALERTA, -0.0045f), new Vector3(0.012f, 0.012f, 0.001f),
                                 mRojoOscuro).GetComponent<Renderer>();
        TextoMenu("Alerta", g, new Vector3(-0.265f, Y_ALERTA, -0.006f), new Vector2(0.62f, 0.024f),
                  "ALERTA  //  CORTE DE ENERGÍA EN TODO EL EDIFICIO", RojoTexto, fSistema, TextAlignmentOptions.Left, 2f);
        TextoMenu("Respaldo", g, new Vector3(0.43f, Y_ALERTA, -0.006f), new Vector2(0.3f, 0.022f), "BATERÍA DE RESPALDO  12%",
                  HuesoTenue, fSistema, TextAlignmentOptions.Right, 2f);
        Caja("Linea_Alerta", g, new Vector3(0f, 0.28f, -0.004f), new Vector3(1.18f, 0.0015f, 0.001f), mRojoLinea);

        TextoMenu("Ayuda", g, new Vector3(0.345f, -0.318f, -0.006f), new Vector2(0.46f, 0.02f),
                  "APUNTÁ Y APRETÁ  [ AGARRE ]    ·    EN EL PC  [ G ]", HuesoTenue, fSistema, TextAlignmentOptions.Left, 2f);
    }

    // Un velo oscuro sobre la foto, para las páginas con mucho texto (opciones y cómo jugar)
    static void Velo(Transform pagina) =>
        Caja("Velo", pagina, new Vector3(0f, -0.035f, -0.0036f), new Vector3(PANTALLA_ANCHO, PANTALLA_ALTO - 0.07f, 0.0005f), mVelo);

    // El encabezado de las páginas "Opciones" y "Cómo jugar": la línea "de sistema" y el título
    static void TituloDeSeccion(Transform pagina, string linea, string titulo)
    {
        TextoMenu("Sobretitulo", pagina, new Vector3(-0.285f, 0.25f, -0.006f), new Vector2(0.59f, 0.022f), linea, RojoTenue, fSistema,
                  TextAlignmentOptions.Left, 3f);
        TextoMenu("Titulo", pagina, new Vector3(-0.285f, 0.19f, -0.006f), new Vector2(0.59f, 0.08f), titulo, Hueso, fTitulo,
                  TextAlignmentOptions.Left, 6f);
    }

    // Una tarjeta de "Cómo jugar": placa con una raya roja arriba, el título y la lista de controles
    static void TarjetaDeControles(Transform pagina, float x, string titulo, string lista)
    {
        const float Y = -0.035f, ANCHO_T = 0.575f, ALTO_T = 0.25f;
        Caja("Tarjeta", pagina, new Vector3(x, Y, -0.004f), new Vector3(ANCHO_T, ALTO_T, 0.003f), mPlaca);
        Caja("Tarjeta_Barra", pagina, new Vector3(x, Y + ALTO_T / 2f - 0.0015f, -0.0058f), new Vector3(ANCHO_T, 0.003f, 0.001f), mRojoPlaca);
        TextoMenu("Titulo_Tarjeta", pagina, new Vector3(x, Y + 0.1f, -0.006f), new Vector2(ANCHO_T - 0.045f, 0.02f), titulo, RojoTexto,
                  fSistema, TextAlignmentOptions.Left, 3f);
        TextoMenu("Controles", pagina, new Vector3(x, Y - 0.025f, -0.006f), new Vector2(ANCHO_T - 0.045f, 0.19f), lista, Hueso, fTexto,
                  TextAlignmentOptions.TopLeft);
    }

    // Una opción del menú: placa oscura (deja ver un poco la foto) con un LED rojo a la izquierda,
    // el número (si tiene), el nombre, la aclaración y una flecha. Con el rayo encima la placa se
    // pone roja, el LED se prende y la vista se corre a la derecha (BotonMenu). "principal" = placa
    // roja (NUEVA PARTIDA).
    static BotonMenu BotonEmergencia(string nombre, Transform padre, Vector3 pos, float ancho, float alto,
                                     string numero, string texto, string detalle, bool principal)
    {
        BotonMenu boton = RaizDeBoton(nombre, padre, pos, ancho, alto, out Transform vista);
        GameObject fondo = Caja("Fondo", vista, Vector3.zero, new Vector3(ancho, alto, 0.008f), principal ? mRojoPlaca : mPlaca);
        float izquierda = -ancho / 2f;
        GameObject led = Caja("LED", vista, new Vector3(izquierda + 0.012f, 0f, -0.0045f), new Vector3(0.007f, alto * 0.55f, 0.001f), mRojoOscuro);

        float xTexto = izquierda + 0.032f;
        if (!string.IsNullOrEmpty(numero))
        {
            TextoMenu("Numero", vista, new Vector3(izquierda + 0.047f, 0.003f, -0.0055f), new Vector2(0.034f, alto * 0.36f), numero,
                      principal ? Hueso : RojoTenue, fSistema, TextAlignmentOptions.Center);
            xTexto = izquierda + 0.078f;
        }
        float anchoTexto = ancho / 2f - 0.045f - xTexto;
        float xCentro = xTexto + anchoTexto / 2f;
        boton.texto = TextoMenu("Texto", vista, new Vector3(xCentro, alto * 0.14f, -0.0055f), new Vector2(anchoTexto, alto * 0.46f), texto,
                                Hueso, fTitulo, TextAlignmentOptions.Left, 4f);
        boton.detalle = TextoMenu("Detalle", vista, new Vector3(xCentro, -alto * 0.24f, -0.0055f), new Vector2(anchoTexto, alto * 0.3f),
                                  detalle, principal ? new Color(1f, 0.86f, 0.82f, 0.85f) : HuesoTenue, fTexto, TextAlignmentOptions.Left);
        TextoMenu("Flecha", vista, new Vector3(ancho / 2f - 0.025f, 0f, -0.0055f), new Vector2(0.03f, alto * 0.45f), ">",
                  principal ? Hueso : RojoTenue, fTitulo, TextAlignmentOptions.Center);

        boton.fondo = fondo.GetComponent<Renderer>();
        boton.indicador = led.GetComponent<Renderer>();
        boton.colorIndicador = principal ? new Color(1f, 0.72f, 0.66f) : new Color(0.3f, 0.04f, 0.05f);
        boton.colorIndicadorEncima = principal ? Color.white : new Color(1f, 0.22f, 0.2f);
        boton.colorEncima = principal ? new Color(1f, 0.22f, 0.2f) : new Color(0.62f, 0.06f, 0.08f, 0.92f);
        boton.desplazarAlApuntar = 0.012f;
        return boton;
    }

    // Botón cuadrado con un símbolo ("-" y "+" del volumen)
    static BotonMenu BotonSimbolo(string nombre, Transform padre, Vector3 pos, string simbolo)
    {
        const float LADO = 0.09f;
        BotonMenu boton = RaizDeBoton(nombre, padre, pos, LADO, LADO, out Transform vista);
        boton.fondo = Caja("Fondo", vista, Vector3.zero, new Vector3(LADO, LADO, 0.008f), mRojoOscuro).GetComponent<Renderer>();
        boton.texto = TextoMenu("Texto", vista, new Vector3(0f, 0.004f, -0.0055f), new Vector2(LADO * 0.6f, LADO * 0.6f), simbolo, Hueso,
                                fTitulo, TextAlignmentOptions.Center);
        boton.colorEncima = new Color(0.85f, 0.1f, 0.12f);
        return boton;
    }

    // Lo común de los botones: collider, XRSimpleInteractable en la capa de los menús, BotonMenu y la
    // "vista" (lo que se anima)
    static BotonMenu RaizDeBoton(string nombre, Transform padre, Vector3 pos, float ancho, float alto, out Transform vista)
    {
        Transform raiz = Grupo(nombre, padre, pos);
        var colision = raiz.gameObject.AddComponent<BoxCollider>();
        colision.center = new Vector3(0f, 0f, -0.015f);
        colision.size = new Vector3(ancho, alto, 0.03f);
        var interactuable = raiz.gameObject.AddComponent<XRSimpleInteractable>();
        interactuable.interactionLayers = GameManager.MascaraMenu;
        var boton = raiz.gameObject.AddComponent<BotonMenu>();
        vista = Grupo("Vista", raiz, new Vector3(0f, 0f, -0.012f));
        boton.vista = vista;
        boton.audioMenu = audioUI;
        EditorUtility.SetDirty(interactuable);
        EditorUtility.SetDirty(boton);
        return boton;
    }

    // Un tornillo del marco: cabeza redonda con la ranura
    static void Tornillo(Transform padre, Vector3 pos)
    {
        var cabeza = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        cabeza.name = "Tornillo";
        Object.DestroyImmediate(cabeza.GetComponent<Collider>());
        cabeza.transform.SetParent(padre, false);
        cabeza.transform.localPosition = pos;
        cabeza.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        cabeza.transform.localScale = new Vector3(0.015f, 0.002f, 0.015f);
        var r = cabeza.GetComponent<Renderer>();
        r.sharedMaterial = mTornillo;
        SinSombras(r);
        Caja("Ranura", padre, pos + new Vector3(0f, 0f, -0.0022f), new Vector3(0.01f, 0.002f, 0.0005f), mMarco).transform.localRotation =
            Quaternion.Euler(0f, 0f, 35f);
    }

    // Un texto del menú de inicio, con su tipografía (sin negrita falsa: el peso lo da la fuente)
    static TextMeshPro TextoMenu(string nombre, Transform padre, Vector3 pos, Vector2 caja, string texto, Color color,
                                 TMP_FontAsset fuente, TextAlignmentOptions alineacion, float espaciado = 0f)
    {
        TextMeshPro t = Rotulo(nombre, padre, pos, caja, texto, color, alineacion, false);
        if (fuente != null) t.font = fuente;
        t.characterSpacing = espaciado;
        return t;
    }

    // ------------------------------------------------------------------ estilo: fuentes, texturas y materiales

    static void PrepararEstiloEmergencia()
    {
        fTitulo = Fuente("BebasNeue-Regular.ttf", "Bebas Neue");
        fTexto = Fuente("BarlowCondensed-Medium.ttf", "Barlow Condensed");
        fSistema = Fuente("ShareTechMono-Regular.ttf", "Share Tech Mono");

        mMarco = MaterialUI("UIE_Marco", new Color(0.06f, 0.06f, 0.065f), -5, true, false);
        mBisel = MaterialUI("UIE_Bisel", new Color(0.2f, 0.19f, 0.2f), -5, true, false);
        mTornillo = MaterialUI("UIE_Tornillo", new Color(0.36f, 0.35f, 0.36f), -4, true, false);
        mFoto = ConTextura(MaterialUI("UIE_Foto", Color.white, -4, true, false), FotoDelMenu(), Vector2.one);

        // Lo que va sobre la foto: transparente y sin escribir profundidad
        mDegradeDerecha = ConTextura(MaterialUI("UIE_DegradeDerecha", new Color(0f, 0f, 0f, 0.82f), -3, false, false),
                                     TexturaDegrade("UIE_DegradeDerecha.png", true), Vector2.one);
        mDegradeAbajo = ConTextura(MaterialUI("UIE_DegradeAbajo", new Color(0f, 0f, 0f, 0.8f), -3, false, false),
                                   TexturaDegrade("UIE_DegradeAbajo.png", false), Vector2.one);
        mVelo = MaterialUI("UIE_Velo", new Color(0.02f, 0.01f, 0.012f, 0.78f), -3, false, false);
        mLineas = ConTextura(MaterialUI("UIE_Lineas", Color.white, -2, false, false), TexturaLineas(), new Vector2(1f, 170f));
        mInterferencia = MaterialUI("UIE_Interferencia", new Color(1f, 0.35f, 0.3f, 0.07f), -2, false, false);

        mPlaca = MaterialUI("UIE_Placa", new Color(0.06f, 0.045f, 0.05f, 0.82f), -1, true, false);
        mRojoPlaca = MaterialUI("UIE_RojoPlaca", new Color(0.7f, 0.05f, 0.08f, 0.95f), -1, true, false);
        mRojoOscuro = MaterialUI("UIE_RojoOscuro", new Color(0.28f, 0.03f, 0.04f), -1, true, false);
        mRojoLinea = MaterialUI("UIE_RojoLinea", new Color(0.5f, 0.08f, 0.09f), -1, true, false);
        AssetDatabase.SaveAssets();
    }

    static Material ConTextura(Material m, Texture2D textura, Vector2 repeticion)
    {
        m.SetTexture("_BaseMap", textura);
        m.SetTextureScale("_BaseMap", repeticion);
        EditorUtility.SetDirty(m);
        return m;
    }

    // La foto del aula abandonada, ya tratada (oscura, roja y con viñeta). A 2k como máximo, con mipmaps.
    static Texture2D FotoDelMenu()
    {
        var importador = AssetImporter.GetAtPath(RUTA_FOTO_MENU) as TextureImporter;
        if (importador == null)
        {
            Debug.LogWarning("Falta la foto del menú " + RUTA_FOTO_MENU + ": la pantalla queda negra.");
            return null;
        }
        if (importador.maxTextureSize != 2048 || importador.wrapMode != TextureWrapMode.Clamp)
        {
            importador.maxTextureSize = 2048;
            importador.wrapMode = TextureWrapMode.Clamp;
            importador.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Texture2D>(RUTA_FOTO_MENU);
    }

    // Crea la tipografía de TextMesh Pro a partir del .ttf (una sola vez), con los caracteres del
    // español ya dibujados en su textura (modo estático: en el visor no se agregan letras nuevas)
    static TMP_FontAsset Fuente(string archivo, string nombre)
    {
        string ruta = CARPETA_FUENTES + "/" + nombre + " SDF.asset";
        var fuente = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(ruta);
        if (fuente != null) return fuente;

        var ttf = AssetDatabase.LoadAssetAtPath<Font>(CARPETA_FUENTES + "/" + archivo);
        if (ttf == null)
        {
            Debug.LogWarning("Falta la tipografía " + CARPETA_FUENTES + "/" + archivo + ": el menú de inicio usa la de siempre.");
            return null;
        }
        fuente = TMP_FontAsset.CreateFontAsset(ttf, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, false);
        fuente.name = nombre + " SDF";
        fuente.TryAddCharacters(CARACTERES);
        fuente.atlasPopulationMode = AtlasPopulationMode.Static;
        AssetDatabase.CreateAsset(fuente, ruta);
        fuente.atlasTextures[0].name = nombre + " Atlas";
        AssetDatabase.AddObjectToAsset(fuente.atlasTextures[0], fuente);
        fuente.material.name = nombre + " Material";
        AssetDatabase.AddObjectToAsset(fuente.material, fuente);
        EditorUtility.SetDirty(fuente);
        AssetDatabase.SaveAssets();
        return fuente;
    }

    // El material del título rojo: la misma tipografía con un brillo rojo alrededor, como un cartel encendido
    static Material MaterialTituloBrillante()
    {
        if (fTitulo == null) return null;
        string ruta = CARPETA_FUENTES + "/Bebas Neue SDF Brillo Rojo.mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(ruta);
        if (m == null)
        {
            m = new Material(fTitulo.material);
            AssetDatabase.CreateAsset(m, ruta);
        }
        m.SetTexture("_MainTex", fTitulo.atlasTexture);
        m.EnableKeyword("GLOW_ON");
        m.SetColor("_GlowColor", new Color(1f, 0.06f, 0.04f, 0.8f));
        m.SetFloat("_GlowOffset", 0.1f);
        m.SetFloat("_GlowInner", 0.1f);
        m.SetFloat("_GlowOuter", 0.6f);
        m.SetFloat("_GlowPower", 0.7f);
        ShaderUtilities.UpdateShaderRatios(m);
        EditorUtility.SetDirty(m);
        return m;
    }

    // Degradado de transparente a opaco (el color lo pone el material). "horizontal": transparente
    // hasta un poco antes de la mitad y opaco a la derecha. Si no, opaco abajo y transparente desde
    // la mitad para arriba.
    static Texture2D TexturaDegrade(string archivo, bool horizontal) =>
        TexturaGenerada(CARPETA + "/" + archivo, horizontal ? 256 : 4, horizontal ? 4 : 256, TextureWrapMode.Clamp, (x, y) =>
        {
            float a = horizontal ? Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.4f, 0.78f, x / 255f))
                                 : 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0f, 0.5f, y / 255f));
            return new Color(1f, 1f, 1f, a);
        });

    // Líneas de monitor: una fila oscura cada cuatro (se repite a lo alto)
    static Texture2D TexturaLineas() => TexturaGenerada(CARPETA + "/UIE_Lineas.png", 4, 4, TextureWrapMode.Repeat,
        (x, y) => y == 0 ? new Color(0f, 0f, 0f, 0.45f) : new Color(0f, 0f, 0f, 0f));

    // Una textura hecha por código y guardada como PNG en Materials/Sistema (se crea una sola vez;
    // para rehacerla, se borra el PNG)
    static Texture2D TexturaGenerada(string ruta, int ancho, int alto, TextureWrapMode envoltura, System.Func<int, int, Color> pixel)
    {
        var existente = AssetDatabase.LoadAssetAtPath<Texture2D>(ruta);
        if (existente != null) return existente;

        var tex = new Texture2D(ancho, alto, TextureFormat.RGBA32, false);
        for (int y = 0; y < alto; y++)
            for (int x = 0; x < ancho; x++)
                tex.SetPixel(x, y, pixel(x, y));
        tex.Apply();
        File.WriteAllBytes(ruta, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(ruta);
        var importador = (TextureImporter)AssetImporter.GetAtPath(ruta);
        importador.wrapMode = envoltura;
        importador.alphaIsTransparency = true;
        importador.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Texture2D>(ruta);
    }
}
