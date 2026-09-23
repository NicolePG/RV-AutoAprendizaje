using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Attachment;
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
// El acertijo, paso a paso:
// 1) El cuarto entra sin energía, a oscuras y con clima de terror. Hay que subir la
//    llave del tablero eléctrico, junto a la entrada. Ahí se prende todo, incluido el
//    televisor de la pared, que muestra el paso siguiente: "VE AL ESCRITORIO".
// 2) El escritorio está dado vuelta, con la computadora mirando al sillón del director.
//    En el teclado hay una tecla verde, igual de grande que las demás: al apretarla se
//    ilumina y recién ahí se prende el monitor con las instrucciones de los relojes.
// 3) Los cinco relojes están parados a las 4:40. La bitácora (sobre el aparador) da
//    la hora de cada campana y el orden; la placa del cuadro dice qué reloj es cada
//    campana. Se gira la manecilla de cada reloj hasta su hora y muestra su dígito.
//    El reloj D no figura en ninguna lista: es el señuelo y dispara el susto.
// 4) Con los cuatro relojes en hora, el televisor muestra el código: 3719.
// 5) El código destraba la cerradura, pero falta la llave: está debajo de uno de los dos
//    almohadones del sofá, que se corren de costado con un clic. Se la agarra con un clic
//    (queda en el mando, como en la mano), se va hasta la puerta, aparece la sombra de la
//    llave en la cerradura, se le hace clic y la llave entra. Girándola se abre la puerta.
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

    static Material mPared, mParedRelojes, mParedPuerta, mMaderaClara, mPantallaPC, mOro, mTecho, mPiso, mMadera, mNegro, mBlanco, mMetal, mBronce,
                    mEsfera, mVerde, mRojo, mPantalla, mResaltado, mGrieta, mAlfombra,
                    mPapel, mTela, mLuzTecho, mDigito, mLibroA, mLibroB, mLibroC,
                    mAluminio, mRojoMate, mVidrioApagado, mPantallaTV, mTecla, mTeclaVerde,
                    mTeclaVerdeOk, mFantasma;

    // Televisor del cuarto: cada paso del acertijo le avisa para que cambie el mensaje
    static PanelObjetivo panelObjetivo;

    // Sombra de la llave, que se arma con la puerta y se conecta con la llave del sofa
    static GameObject fantasmaLlave;

    [MenuItem("Escape Room/Construir Cuarto 2")]
    public static void Construir()
    {
        var raiz = GameObject.Find("Cuarto2_Oficina");
        if (raiz == null)
        {
            raiz = new GameObject("Cuarto2_Oficina");
            Undo.RegisterCreatedObjectUndo(raiz, "Construir Cuarto 2");
        }
        // Siempre en su lugar, empalmado con la salida del Cuarto 1 (ver DisposicionCuartos)
        raiz.transform.SetPositionAndRotation(DisposicionCuartos.Cuarto2, DisposicionCuartos.Giro);

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
        ArmarTelevisor(raiz.transform, conEnergia);
        ArmarEscritorio(mobiliario.transform, lucesCuarto);
        ArmarAparador(mobiliario.transform);
        ArmarEstanteria(mobiliario.transform);
        ArmarDecoracion(mobiliario.transform);
        LlaveAutomatica llave = ArmarSala(mobiliario.transform);

        conEnergia.AddRange(ArmarRelojes(relojes.transform));
        List<GameObject> sinEnergia = ArmarTerror(luces.transform);

        ArmarRejaEntrada(raiz.transform);
        CerraduraLlave cerradura = ArmarPuertaSalida(raiz.transform);
        ConectarLlave(llave, cerradura);
        conEnergia.Add(ArmarTeclado(raiz.transform, cerradura));

        var control = ArmarControlEnergia(raiz.transform, lucesCuarto, conEnergia, sinEnergia);
        ArmarTablero(raiz.transform, control);
        ArmarZonaClima(raiz.transform, control);
        AbrirPasilloDelCuarto1();

        AjustarAmbienteEditor();
        AsegurarInventario();

        // Para que Ctrl+Z deshaga la construcción entera
        foreach (Transform hijo in raiz.transform)
            Undo.RegisterCreatedObjectUndo(hijo.gameObject, "Construir Cuarto 2");

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(raiz.scene);
        Selection.activeGameObject = raiz;

        Debug.Log("Cuarto 2 construido. Se sube la llave del tablero (se prende la luz y el " +
                  "televisor), se aprieta la tecla verde del teclado de la PC (se prende el " +
                  "monitor), se ponen en hora los relojes A=7, B=1, C=3 y E=9 (el D es el " +
                  "senuelo) y el televisor muestra la clave 3719. La llave esta debajo del " +
                  "segundo almohadon del sofa: se la lleva en el mando hasta la puerta y se le " +
                  "hace clic a la sombra de la llave para encajarla.");
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

    // Caja de colisión invisible. Se usa cuando el collider automático de un modelo no
    // sirve: el del sofá, por ejemplo, es una caja que envuelve el mueble entero y tapa
    // lo que está apoyado en el asiento.
    static void Colision(Transform p, string nombre, Vector3 centro, Vector3 tamano)
    {
        var go = Grupo(nombre, p);
        go.transform.localPosition = centro;
        go.AddComponent<BoxCollider>().size = tamano;
    }

    // ------------------------------------------------------------------ televisor

    // Televisor de pantalla plana colgado en la pared, al lado del tablero eléctrico
    // (antes ahí había un cartel de corcho). Arranca apagado: se prende junto con la luz
    // del cuarto y desde ahí va diciendo qué hacer. Cuando los cuatro relojes quedan en
    // hora, muestra el código de la puerta.
    static void ArmarTelevisor(Transform raiz, List<GameObject> conEnergia)
    {
        var g = Grupo("Televisor", raiz);
        g.transform.localPosition = new Vector3(0.1f, 1.62f, 1.15f);
        g.transform.localEulerAngles = new Vector3(0f, 90f, 0f);   // colgado en la pared Oeste

        // Cuerpo: caja negra fina y marco de aluminio, como una pantalla plana de ahora
        Cubo("Caja", g.transform, new Vector3(0f, 0f, -0.03f), new Vector3(0.94f, 0.54f, 0.05f), mNegro, true);
        Cubo("Marco", g.transform, Vector3.zero, new Vector3(1.04f, 0.63f, 0.025f), mAluminio, true);
        Cubo("Soporte_Pared", g.transform, new Vector3(0f, 0f, -0.055f), new Vector3(0.3f, 0.3f, 0.02f), mNegro);
        // Vidrio apagado: se ve la pantalla negra aunque no haya energía
        Cubo("Vidrio_Apagado", g.transform, new Vector3(0f, 0f, 0.014f), new Vector3(0.99f, 0.58f, 0.006f), mVidrioApagado);
        // Piloto de encendido: rojo mientras el televisor está apagado
        Esfera("Piloto", g.transform, new Vector3(0.47f, -0.29f, 0.016f),
                              new Vector3(0.014f, 0.014f, 0.014f), mRojo);

        // Todo lo que se prende cuando vuelve la energía
        var encendida = Grupo("Encendida", g.transform);
        Cubo("Pantalla", encendida.transform, new Vector3(0f, 0f, 0.018f), new Vector3(0.97f, 0.57f, 0.006f), mPantallaTV);
        Cubo("Barra", encendida.transform, new Vector3(0f, 0.2f, 0.023f), new Vector3(0.9f, 0.008f, 0.002f), mVerde);
        Texto("Titulo", encendida.transform, new Vector3(0f, 0.24f, 0.023f), Vector3.zero,
              "DIRECCION", 0.28f, new Color(0.5f, 0.92f, 1f), 0.9f, 0.07f);
        // El piloto verde se ve por encima del rojo cuando el televisor está prendido
        Esfera("Piloto_Ok", encendida.transform, new Vector3(0.47f, -0.29f, 0.014f),
               new Vector3(0.016f, 0.016f, 0.016f), mVerde);

        var texto = Texto("Texto_TV", encendida.transform, new Vector3(0f, -0.05f, 0.023f), Vector3.zero,
                          "", 0.55f, new Color(0.88f, 0.97f, 1f), 0.94f, 0.4f);
        texto.lineSpacing = -12f;

        // Un poco de luz propia, como la que tira un televisor encendido
        LuzPunto("Luz_TV", encendida.transform, new Vector3(0f, 0f, 0.45f),
                 new Color(0.6f, 0.85f, 1f), 1.5f, 2.4f);

        encendida.SetActive(false);
        conEnergia.Add(encendida);

        panelObjetivo = g.AddComponent<PanelObjetivo>();
        panelObjetivo.texto = texto;
        panelObjetivo.pasos = new[]
        {
            "",
            // Los mensajes son cortos a propósito: el televisor dice A DÓNDE ir, no qué
            // hacer. Lo que hay que hacer se descubre tocando las cosas del cuarto.
            "VE AL ESCRITORIO",
            "<size=60%>CODIGO DE LA PUERTA</size>\n\n<size=210%>3 7 1 9</size>",
            "CODIGO ACEPTADO",
            // Este es el único que además avisa algo: sin el medallón del cajón no se
            // puede terminar el Cuarto 4, y si el jugador sale sin él ya no puede volver.
            "PUERTA ABIERTA\n\n<size=70%>No te olvides del cajon</size>"
        };
        texto.text = panelObjetivo.pasos[1];   // así se ve algo en el editor, sin darle Play
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
    // Es un tablero moderno: gabinete embutido de chapa blanca, frente de aluminio, la
    // fila de térmicas y abajo la llave general, que es la única que se toca.
    //
    // Las térmicas van CHATAS, sin palanquitas. Antes tenían una palanquita negra cada
    // una y parecían seis interruptores para subir: el jugador les hacía clic a esas (que
    // solo decoran y ni collider tienen) y creía que el tablero estaba roto. Ahora lo único
    // que parece un interruptor es la llave general, que además brilla en rojo y tiene el
    // cartel justo debajo.
    static void ArmarTablero(Transform raiz, ControlEnergia control)
    {
        var g = Grupo("Tablero_Electrico", raiz);
        g.transform.localPosition = new Vector3(0.42f, 1.33f, 0.07f);

        Cubo("Gabinete", g.transform, Vector3.zero, new Vector3(0.44f, 0.56f, 0.1f), mBlanco, true);
        Cubo("Frente", g.transform, new Vector3(0f, 0f, 0.052f), new Vector3(0.4f, 0.52f, 0.008f), mAluminio);

        // Franja con el nombre, arriba de todo
        Cubo("Franja", g.transform, new Vector3(0f, 0.22f, 0.057f), new Vector3(0.4f, 0.05f, 0.003f), mNegro);
        Texto("Titulo", g.transform, new Vector3(0f, 0.22f, 0.06f), Vector3.zero,
              "TABLERO GENERAL", 0.13f, new Color(0.88f, 0.88f, 0.86f), 0.38f, 0.045f);

        // Fila de térmicas sobre su riel. Solo decoran: son chatas para que no se confundan
        // con la llave general.
        Cubo("Riel", g.transform, new Vector3(0f, 0.1f, 0.056f), new Vector3(0.34f, 0.13f, 0.004f), mNegro);
        for (int i = 0; i < 6; i++)
            Cubo("Termica_" + (i + 1), g.transform, new Vector3(-0.125f + i * 0.05f, 0.1f, 0.06f),
                 new Vector3(0.042f, 0.11f, 0.008f), mMetal);

        // Cartel del cuarto. Con el televisor apagado es lo único que se lee a oscuras,
        // así que va en ámbar emisivo: se ve solo, sin que le pegue ninguna luz.
        Cubo("Aviso", g.transform, new Vector3(0f, 0f, 0.056f), new Vector3(0.36f, 0.05f, 0.003f), mNegro);
        Texto("Texto_Aviso", g.transform, new Vector3(0f, 0f, 0.059f), Vector3.zero,
              "SIN ENERGIA", 0.1f, new Color(1f, 0.72f, 0.15f), 0.36f, 0.045f);

        // Los pilotos son emisivos, así se los ve aunque el cuarto esté a oscuras
        var pilotoRojo = Esfera("Piloto_Sin_Energia", g.transform, new Vector3(0.155f, -0.14f, 0.058f),
                                new Vector3(0.036f, 0.036f, 0.036f), mRojo);
        var pilotoVerde = Esfera("Piloto_Con_Energia", g.transform, new Vector3(-0.155f, -0.14f, 0.058f),
                                 new Vector3(0.036f, 0.036f, 0.036f), mVerde);
        pilotoVerde.SetActive(false);

        var audio = AudioEn("Audio_Llave", g.transform);

        // La llave general: chapa negra de fondo y la palanca roja, que es lo que se toca.
        // Es grande a propósito, para que el rayo del control le pegue sin puntería fina.
        Cubo("Base_Llave", g.transform, new Vector3(0f, -0.14f, 0.058f), new Vector3(0.21f, 0.21f, 0.01f), mNegro);
        var llave = Cubo("Llave_General", g.transform, new Vector3(0f, -0.13f, 0.088f),
                         new Vector3(0.115f, 0.17f, 0.06f), mRojoMate, true);
        llave.AddComponent<XRSimpleInteractable>();
        var boton = llave.AddComponent<PressableButton>();
        boton.parteMovil = llave.transform;
        boton.recorrido = 0.03f;
        Resaltar(llave, llave.GetComponent<Renderer>());

        // El cartel que dice qué hacer, justo debajo de la llave y en ámbar emisivo
        Texto("Texto_Instruccion", g.transform, new Vector3(0f, -0.235f, 0.059f), Vector3.zero,
              "SUBE ESTA LLAVE", 0.095f, new Color(1f, 0.72f, 0.15f), 0.38f, 0.04f);

        // Al subir la llave vuelve la energía: de eso se encarga ControlEnergia
        UnityEventTools.AddVoidPersistentListener(boton.alPresionar, new UnityAction(control.Encender));
        UnityEventTools.AddBoolPersistentListener(boton.alPresionar, new UnityAction<bool>(pilotoVerde.SetActive), true);
        UnityEventTools.AddBoolPersistentListener(boton.alPresionar, new UnityAction<bool>(pilotoRojo.SetActive), false);
        UnityEventTools.AddVoidPersistentListener(boton.alPresionar, new UnityAction(audio.Play));
        // El televisor se prende con la energía y muestra el primer mensaje
        AvisarPanel(boton.alPresionar, 1);
        EditorUtility.SetDirty(boton);
    }

    // ------------------------------------------------------------------ escritorio

    static void ArmarEscritorio(Transform p, List<Light> lucesCuarto)
    {
        var g = Grupo("Escritorio", p);
        g.transform.localPosition = new Vector3(1.5f, 0f, 2.6f);
        // Dado vuelta: la computadora, el teclado y el cajon quedan del lado del sillon
        // del director, como en una oficina de verdad. El jugador tiene que pasar detras
        // del escritorio para usarlos. Todo lo que cuelga del grupo se da vuelta con el.
        g.transform.localEulerAngles = new Vector3(0f, 180f, 0f);

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
        drawer.abrirDeUnToque = true;   // con el control, jalar de lejos es incómodo
        drawer.bloqueado = true;        // hasta que se caiga la chapa atornillada
        Resaltar(cajon, cajon.transform.Find("Frente").GetComponent<Renderer>());
        EditorUtility.SetDirty(inter);
        EditorUtility.SetDirty(drawer);

        // El cajón está tapado por una chapa atornillada: hay que sacarle los tres
        // tornillos. Cuando la chapa se suelta, desbloquea el cajón.
        ArmarTapaAtornillada(g.transform, new Vector3(0.5f, 0.61f, -0.45f), drawer);

        // El medallón que hay que llevarse al Cuarto 4. Va adentro de un grupo para que
        // el disco pueda estar girado sin que se deforme lo que cuelga de él.
        var medallon = Grupo("Medallon", cajon.transform);
        medallon.transform.localPosition = new Vector3(0f, -0.055f, -0.3f);
        Cilindro("Disco", medallon.transform, Vector3.zero, new Vector3(0.11f, 0.007f, 0.11f), mOro)
            .transform.localEulerAngles = new Vector3(90f, 0f, 0f);
        Cilindro("Aro", medallon.transform, new Vector3(0f, 0f, 0.001f), new Vector3(0.13f, 0.004f, 0.13f), mBronce)
            .transform.localEulerAngles = new Vector3(90f, 0f, 0f);

        var colisionMedallon = medallon.AddComponent<BoxCollider>();
        colisionMedallon.size = new Vector3(0.16f, 0.16f, 0.06f);

        // Se lleva de un toque, igual que la llave y los fusibles, y queda a la vista
        // todo el camino hasta el laboratorio
        medallon.AddComponent<XRSimpleInteractable>();
        var llevable = medallon.AddComponent<ObjetoLlevable>();
        // Se queda con el jugador hasta el Cuarto 4: agarrar un fusible no lo suelta
        llevable.quedaConElJugador = true;
        // Bien adelante y apenas a la izquierda: si va muy al costado, en la pantalla
        // del simulador queda fuera de cuadro y parece que se perdió
        llevable.distancia = 0.5f;
        llevable.bajar = 0.14f;
        EditorUtility.SetDirty(llevable);
        var pick = medallon.AddComponent<PickableItem>();
        pick.datos = BuscarAsset<ItemData>("ItemData_ObjetoEspecial");
        pick.ocultarAlAgarrar = false;   // no desaparece: el jugador lo lleva en la mano
        Resaltar(medallon, medallon.transform.Find("Disco").GetComponent<Renderer>());
        EditorUtility.SetDirty(pick);

        ArmarComputadora(g.transform);

        // Lo que va arriba del escritorio (de Poly Haven, si está)
        Modelo("desk_lamp_arm_01", g.transform, new Vector3(0.6f, 0.76f, 0.22f), 200f, 0.88f, false);
        Modelo("potted_plant_04", g.transform, new Vector3(-0.78f, 0.76f, 0.28f), 0f, 0.27f, false);

        // Sillon del director, detras del escritorio y mirandolo. Se corrio un poco mas
        // atras que antes para que quede lugar donde pararse entre el escritorio y el.
        // Primero la silla de oficina con rueditas de Sketchfab (la que trajo el Cuarto 3),
        // que es la que mejor pega en una direccion; si no esta, la butaca de Poly Haven.
        if (ModeloSketchfab("office_chair_modern", p, new Vector3(1.5f, 0f, 4.05f), 180f, 1.1f, true) == null &&
            Modelo("mid_century_lounge_chair", p, new Vector3(1.5f, 0f, 4.05f), 180f, 1.17f, true) == null)
            SillaSimple(p, new Vector3(1.5f, 0f, 4.05f));

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

    // Computadora del escritorio, mirando al sillon del director.
    //
    // La pantalla arranca APAGADA: hay que encontrar la tecla verde del teclado, que es
    // igual de grande que todas las demas, y apretarla. Ahi la tecla queda iluminada de
    // verde y recien entonces se prende el monitor con las instrucciones de los relojes.
    static void ArmarComputadora(Transform escritorio)
    {
        var pc = Grupo("Computadora", escritorio);
        pc.transform.localPosition = new Vector3(0.28f, 0.768f, 0.16f);
        pc.transform.localEulerAngles = new Vector3(6f, 0f, 0f);   // apenas echada hacia atras

        // Monitor de marco finito sobre pie de aluminio, como los de ahora
        Cubo("Pie", pc.transform, new Vector3(0f, 0.008f, 0f), new Vector3(0.26f, 0.016f, 0.16f), mAluminio, true);
        Cubo("Cuello", pc.transform, new Vector3(0f, 0.09f, 0.012f), new Vector3(0.045f, 0.18f, 0.025f), mAluminio);
        Cubo("Marco", pc.transform, new Vector3(0f, 0.34f, 0.006f), new Vector3(0.72f, 0.44f, 0.018f), mNegro, true);
        // Vidrio apagado: se ve el monitor negro hasta que alguien lo prende
        Cubo("Vidrio_Apagado", pc.transform, new Vector3(0f, 0.34f, -0.005f), new Vector3(0.69f, 0.41f, 0.004f), mVidrioApagado);

        // Todo lo que se ve con la computadora prendida
        var encendida = Grupo("Encendida", pc.transform);
        Cubo("Pantalla", encendida.transform, new Vector3(0f, 0.34f, -0.008f), new Vector3(0.68f, 0.4f, 0.004f), mPantallaPC);

        // Las instrucciones de los relojes: pocas lineas y letra grande. Ya no dice a que
        // hora quedaron parados; eso se ve en los relojes mismos.
        // Letra grande y casi blanca: antes era más chica y celeste, y con el reflejo de
        // las luces del techo no se leía. Las líneas se cortaron más cortas para que entre
        // el tamaño nuevo sin que el texto se parta solo.
        var pistas = Texto("Texto_Pistas", encendida.transform, new Vector3(0f, 0.34f, -0.012f), new Vector3(0f, 180f, 0f),
                           "Move la aguja corta\n" +
                           "de cada reloj\n" +
                           "hasta su hora.\n\n" +
                           "Los numeros que salen\n" +
                           "son el codigo.",
                           0.46f, new Color(0.93f, 0.99f, 1f), 0.66f, 0.38f);
        pistas.lineSpacing = -14f;

        // Luz de la pantalla prendida, para que se note desde lejos que algo cambio
        LuzPunto("Luz_Pantalla", encendida.transform, new Vector3(0f, 0.34f, -0.3f),
                 new Color(0.6f, 0.85f, 1f), 1.1f, 1.6f);
        encendida.SetActive(false);

        ArmarTecladoPC(escritorio, encendida);
    }

    // Teclado de la computadora: bandeja de aluminio y teclas chatas separadas, como los
    // teclados de ahora. Una de las teclas es verde y es la que prende el monitor: mide
    // exactamente lo mismo que las demas, asi que hay que buscarla con la vista.
    static void ArmarTecladoPC(Transform escritorio, GameObject pantallaEncendida)
    {
        const int FILAS = 5, COLUMNAS = 14;
        const float PASO_X = 0.0315f, PASO_Z = 0.026f;
        const float TECLA_X = 0.026f, TECLA_Z = 0.021f, TECLA_Y = 0.007f;

        var teclado = Grupo("Teclado_PC", escritorio);
        teclado.transform.localPosition = new Vector3(0.28f, 0.772f, -0.14f);
        teclado.transform.localEulerAngles = new Vector3(-3f, 0f, 0f);   // apenas levantado atras

        Cubo("Bandeja", teclado.transform, Vector3.zero, new Vector3(0.47f, 0.014f, 0.17f), mAluminio, true);
        Cubo("Canto", teclado.transform, new Vector3(0f, -0.006f, 0f), new Vector3(0.48f, 0.008f, 0.18f), mNegro);
        Cubo("Hueco", teclado.transform, new Vector3(0f, 0.0075f, -0.004f), new Vector3(0.45f, 0.002f, 0.145f), mNegro);

        // La tecla verde va arriba a la derecha del que escribe. El jugador mira el
        // escritorio desde el lado del sillon, asi que su derecha es el +X del teclado.
        const int FILA_VERDE = 4, COLUMNA_VERDE = 13;

        float x0 = -(COLUMNAS - 1) * PASO_X / 2f;
        float z0 = -(FILAS - 1) * PASO_Z / 2f;
        GameObject teclaVerde = null;

        for (int fila = 0; fila < FILAS; fila++)
            for (int col = 0; col < COLUMNAS; col++)
            {
                bool esLaVerde = fila == FILA_VERDE && col == COLUMNA_VERDE;
                var tecla = Cubo(esLaVerde ? "Tecla_Verde" : "Tecla", teclado.transform,
                                 new Vector3(x0 + col * PASO_X, 0.0105f, z0 + fila * PASO_Z),
                                 new Vector3(TECLA_X, TECLA_Y, TECLA_Z),
                                 esLaVerde ? mTeclaVerde : mTecla, esLaVerde);
                if (esLaVerde) teclaVerde = tecla;
            }

        // Barra espaciadora, para que se vea como un teclado y no como una grilla
        Cubo("Barra_Espacio", teclado.transform, new Vector3(-0.01f, 0.0105f, z0 - PASO_Z),
             new Vector3(PASO_X * 5f, TECLA_Y, TECLA_Z), mTecla);

        // La misma tecla pero iluminada: arranca escondida y aparece al apretarla
        var teclaEncendida = Cubo("Tecla_Verde_Encendida", teclado.transform,
                                  teclaVerde.transform.localPosition,
                                  new Vector3(TECLA_X, TECLA_Y, TECLA_Z), mTeclaVerdeOk);
        teclaEncendida.SetActive(false);
        var luzTecla = LuzApagada("Luz_Tecla", teclado.transform,
                                  teclaVerde.transform.localPosition + new Vector3(0f, 0.05f, 0f),
                                  new Color(0.35f, 1f, 0.45f), 1.2f, 0.35f);

        var audio = AudioEn("Audio_Tecla", teclado.transform);

        teclaVerde.AddComponent<XRSimpleInteractable>();
        var boton = teclaVerde.AddComponent<PressableButton>();
        boton.parteMovil = teclaVerde.transform;
        boton.recorrido = 0.004f;
        Resaltar(teclaVerde, teclaVerde.GetComponent<Renderer>());

        // Apretar la tecla correcta: se ilumina de verde y se prende el monitor
        UnityEventTools.AddBoolPersistentListener(boton.alPresionar, new UnityAction<bool>(teclaEncendida.SetActive), true);
        UnityEventTools.AddBoolPersistentListener(boton.alPresionar, new UnityAction<bool>(luzTecla.SetActive), true);
        UnityEventTools.AddBoolPersistentListener(boton.alPresionar, new UnityAction<bool>(pantallaEncendida.SetActive), true);
        UnityEventTools.AddVoidPersistentListener(boton.alPresionar, new UnityAction(audio.Play));
        EditorUtility.SetDirty(boton);
    }

    // Chapa atornillada que tapa el cajón del escritorio. Cada tornillo se saca con un
    // toque; cuando sale el tercero, la chapa se suelta y se cae al piso, y recién ahí
    // se puede abrir el cajón y sacar el medallón.
    static void ArmarTapaAtornillada(Transform p, Vector3 pos, Drawer cajon)
    {
        // La chapa va adentro de un grupo: si los tornillos fueran hijos del cubo,
        // heredarían su escala y saldrían aplastados
        var g = Grupo("Tapa_Cajon", p);
        g.transform.localPosition = pos;

        Cubo("Chapa", g.transform, Vector3.zero, new Vector3(0.68f, 0.28f, 0.02f), mMetal, true);

        var rb = g.AddComponent<Rigidbody>();
        rb.isKinematic = true;   // se queda quieta hasta que se sueltan los tornillos
        var panel = g.AddComponent<ScrewedPanel>();
        panel.tornillosRestantes = 3;

        // Recién con la chapa en el piso se puede abrir el cajón
        if (cajon != null)
            UnityEventTools.AddVoidPersistentListener(panel.alSoltarse, new UnityAction(cajon.Desbloquear));

        Texto("Texto_Tapa", g.transform, new Vector3(0f, 0.025f, -0.013f), new Vector3(0f, 180f, 0f),
              "ARCHIVO\nDIRECCION", 0.5f, new Color(0.12f, 0.12f, 0.12f), 0.62f, 0.16f);

        // Los tres tornillos, hijos de la chapa para que se caigan con ella si queda alguno
        Vector3[] puntos =
        {
            new Vector3(-0.28f, 0.1f, -0.02f),
            new Vector3(0.28f, 0.1f, -0.02f),
            new Vector3(0f, -0.1f, -0.02f)
        };

        for (int i = 0; i < puntos.Length; i++)
        {
            var t = Cilindro("Tornillo_" + (i + 1), g.transform, puntos[i],
                             new Vector3(0.05f, 0.02f, 0.05f), mBronce, true);
            t.transform.localEulerAngles = new Vector3(90f, 0f, 0f);

            t.AddComponent<XRSimpleInteractable>();
            var tornillo = t.AddComponent<Screw>();
            tornillo.tapa = panel;
            tornillo.sacarConUnToque = true;   // se saca tocándolo, sin destornillador
            Resaltar(t, t.GetComponent<Renderer>());
            EditorUtility.SetDirty(tornillo);
        }

        EditorUtility.SetDirty(panel);
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
                        "Cierre  . . . 9:00",
                        0.155f, new Color(0.15f, 0.12f, 0.1f), 0.35f, 0.28f);
        txt.lineSpacing = -12f;

        // Se la puede tocar para que se resalte, pero ya no cambia el televisor: el
        // televisor pasa al codigo recien cuando los cuatro relojes estan en hora
        var interBitacora = bitacora.AddComponent<XRSimpleInteractable>();
        Resaltar(bitacora, bitacora.transform.Find("Tapa").GetComponent<Renderer>());
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
        // Apenas torcido, como dejado ahí. El lado largo va sobre el ancho del estante.
        g.transform.localEulerAngles = new Vector3(0f, 6f, 0f);

        Cubo("Tapa", g.transform, Vector3.zero, new Vector3(0.26f, 0.025f, 0.4f), mLibroB, true);
        Cubo("Hojas", g.transform, new Vector3(0f, 0.016f, 0f), new Vector3(0.24f, 0.008f, 0.38f), mPapel);

        // El acertijo va impreso en la tapa: al tocar el libro se para de frente y se lee.
        // No nombra al sillón a propósito: el jugador tiene que darse cuenta solo.
        var texto = Texto("Texto_Acertijo", g.transform, new Vector3(0f, 0.022f, 0f), new Vector3(-90f, 0f, 0f),
                          "ACERTIJO\n\n" +
                          "Tengo brazos\n" +
                          "y no abrazo,\n" +
                          "tengo patas\n" +
                          "y no camino.\n\n" +
                          "Levanta lo blando\n" +
                          "y tendras la llave",
                          0.24f, new Color(0.15f, 0.12f, 0.1f), 0.24f, 0.38f);
        texto.lineSpacing = -18f;

        g.AddComponent<XRSimpleInteractable>();
        var dePie = g.AddComponent<LibroDePie>();
        dePie.alturaExtra = 0.12f;   // cuánto sube al pararse
        dePie.acercar = 0.18f;      // cuánto se acerca al jugador
        EditorUtility.SetDirty(dePie);
        Resaltar(g, g.transform.Find("Tapa").GetComponent<Renderer>());
    }

    // ------------------------------------------------------------------ decoración

    // Lo que termina de darle cara de oficina de director: cuadros en las paredes, una
    // alfombra bajo el escritorio, la papelera, plantas y las cosas de seguridad del
    // colegio. Nada de esto se toca ni entra en el acertijo, solo decora.
    static void ArmarDecoracion(Transform p)
    {
        // Alfombra grande debajo del escritorio y del sillón
        Cubo("Alfombra_Escritorio", p, new Vector3(1.85f, 0.005f, 3.1f),
             new Vector3(2.5f, 0.01f, 2.4f), mAlfombra);

        // Cuadros. Los de la pared Este miran hacia adentro (-X, o sea giro -90) y el de
        // la pared Sur mira hacia adentro del cuarto (-Z, giro 180).
        //
        // El cuadro grande es el óleo de Sketchfab (Assets/Sketchfab/cuadro_oleo). Mide
        // 1,14 x 0,84 m y viene parado, con el frente hacia su +Z, igual que los de Poly
        // Haven. Va con apoyar en false para que el 1,72 sea el CENTRO del cuadro y no su
        // borde de abajo: si no, queda colgado casi contra el techo. Si el modelo no está,
        // queda el marco de Poly Haven de siempre.
        if (ModeloSketchfab("cuadro_oleo", p, new Vector3(5.92f, 1.72f, 3.15f), -90f, 0.62f, false, false) == null)
            Modelo("hanging_picture_frame_02", p, new Vector3(5.92f, 1.72f, 3.15f), -90f, 0.62f, false, false);

        // Los otros dos son marcos de Poly Haven. A propósito no se repite el mismo óleo
        // tres veces: quedaría de mala calidad que los tres cuadros sean idénticos.
        Modelo("hanging_picture_frame_02", p, new Vector3(1.15f, 1.7f, 6.88f), 180f, 0.55f, false, false);
        Modelo("hanging_picture_frame_01", p, new Vector3(5.92f, 1.7f, 6.1f), -90f, 0.5f, false, false);

        // Globo terráqueo en el estante, ese toque de oficina de director
        ModeloSketchfab("globo_terraqueo", p, new Vector3(5.62f, 1.03f, 1.72f), 0f, 0.3f, false);

        // Papelera moderna al lado del escritorio: cilindro de acero cepillado con el aro
        // de arriba negro. Antes iba el tacho de chapa abollada de Poly Haven, que es de
        // taller y desentonaba en una dirección.
        var papelera = Grupo("Papelera", p);
        papelera.transform.localPosition = new Vector3(2.78f, 0f, 3.3f);
        Cilindro("Cuerpo", papelera.transform, new Vector3(0f, 0.17f, 0f), new Vector3(0.27f, 0.17f, 0.27f), mAluminio, true);
        Cilindro("Aro", papelera.transform, new Vector3(0f, 0.345f, 0f), new Vector3(0.29f, 0.012f, 0.29f), mNegro);
        Cilindro("Base", papelera.transform, new Vector3(0f, 0.008f, 0f), new Vector3(0.26f, 0.008f, 0.26f), mNegro);
        Modelo("potted_plant_02", p, new Vector3(5.4f, 0f, 0.75f), 0f, 0.95f, true);
        Modelo("ceramic_vase_01", p, new Vector3(4.3f, 0.37f, 4.6f), 0f, 0.26f, false);

        // Cosas de seguridad del colegio: extintor y alarma cerca de la salida, y la
        // cámara en la esquina de arriba, mirando al cuarto
        Modelo("korean_fire_extinguisher_01", p, new Vector3(2.15f, 0f, 6.72f), 180f, 0.55f, false);
        // Al otro extremo de la pared Norte: en el medio están los cinco relojes
        Modelo("fire_alarm", p, new Vector3(5.55f, 1.85f, 0.09f), 0f, 0.17f, false);
        Modelo("security_camera_01", p, new Vector3(5.75f, 2.78f, 0.35f), 215f, 0.18f, false);

        // Impresora sobre el aparador, en el hueco que queda entre la bitácora y la ballena
        ModeloSketchfab("printer_low_poly", p, new Vector3(0.32f, 0.68f, 4.8f), 90f, 0.26f, false);
    }

    // ------------------------------------------------------------------ sala de estar

    // Rincón con sofá y mesa ratona, del lado Este: le da vida al cuarto
    static LlaveAutomatica ArmarSala(Transform p)
    {
        Cubo("Alfombra", p, new Vector3(4.75f, 0.006f, 4.6f), new Vector3(2.4f, 0.012f, 2.4f), mAlfombra);

        // Ojo con el collider del sofá: el automático es una caja que envuelve TODO el
        // mueble, incluido el asiento, y se tragaba la llave. Por eso el modelo va sin
        // collider y las dos cajas se ponen a mano, dejando el asiento libre desde 0.42
        // para arriba, que es donde está apoyada la llave.
        if (Modelo("sofa_02", p, new Vector3(5.5f, 0f, 4.6f), -90f, 0.71f, false) == null)
        {
            var s = Grupo("Sofa", p);
            s.transform.localPosition = new Vector3(5.5f, 0f, 4.6f);
            Cubo("Base", s.transform, new Vector3(0f, 0.22f, 0f), new Vector3(0.85f, 0.4f, 1.8f), mTela, true);
            Cubo("Respaldo", s.transform, new Vector3(0.33f, 0.58f, 0f), new Vector3(0.2f, 0.36f, 1.8f), mTela);
            Cubo("Brazo_1", s.transform, new Vector3(0f, 0.5f, -0.85f), new Vector3(0.85f, 0.18f, 0.12f), mTela);
            Cubo("Brazo_2", s.transform, new Vector3(0f, 0.5f, 0.85f), new Vector3(0.85f, 0.18f, 0.12f), mTela);
        }

        Colision(p, "Colision_Sofa_Asiento", new Vector3(5.45f, 0.21f, 4.6f), new Vector3(0.95f, 0.42f, 1.95f));
        Colision(p, "Colision_Sofa_Respaldo", new Vector3(5.86f, 0.52f, 4.6f), new Vector3(0.3f, 0.62f, 1.95f));

        if (Modelo("modern_coffee_table_02", p, new Vector3(4.3f, 0f, 4.6f), 90f, 0.37f, true) == null)
        {
            var m = Grupo("Mesa_Ratona", p);
            m.transform.localPosition = new Vector3(4.3f, 0f, 4.6f);
            Cubo("Tapa", m.transform, new Vector3(0f, 0.37f, 0f), new Vector3(0.6f, 0.03f, 1.2f), mMadera, true);
            foreach (float x in new[] { -0.26f, 0.26f })
                foreach (float z in new[] { -0.55f, 0.55f })
                    Cubo("Pata", m.transform, new Vector3(x, 0.18f, z), new Vector3(0.03f, 0.36f, 0.03f), mNegro);
        }

        // Con collider, para que el jugador no la atraviese ni se teletransporte encima
        Modelo("potted_plant_02", p, new Vector3(5.5f, 0f, 5.95f), 0f, 0.84f, true);

        // La llave de la puerta, acostada en el asiento y tapada por un almohadón.
        // Va apenas por encima del collider del sofá, así el rayo del control la toca
        // a ella y no al mueble: ese era el motivo por el que no se la podía agarrar.
        var llave = Grupo("Llave_Salida", p);
        llave.transform.localPosition = new Vector3(5.45f, 0.44f, 4.85f);
        // Cruzada al sofá (y no a lo largo): así ocupa 16 cm de fondo en vez de 28 y el
        // almohadón la tapa entera, pero con correrse un poco ya queda destapada
        llave.transform.localEulerAngles = new Vector3(0f, 100f, 0f);

        // Acostada: la cabeza es un disco plano y el vástago va a lo largo de la Z
        Cilindro("Cabeza", llave.transform, new Vector3(0f, 0f, -0.075f),
                 new Vector3(0.08f, 0.005f, 0.08f), mOro);
        Cubo("Vastago", llave.transform, Vector3.zero, new Vector3(0.016f, 0.01f, 0.17f), mOro);
        Cubo("Diente_1", llave.transform, new Vector3(0.026f, 0f, 0.05f), new Vector3(0.036f, 0.01f, 0.016f), mOro);
        Cubo("Diente_2", llave.transform, new Vector3(0.024f, 0f, 0.082f), new Vector3(0.03f, 0.01f, 0.016f), mOro);

        // Un solo collider bien generoso para toda la llave: se agarra de un clic
        var colisionLlave = llave.AddComponent<BoxCollider>();
        colisionLlave.center = new Vector3(0.008f, 0.01f, 0f);
        colisionLlave.size = new Vector3(0.12f, 0.09f, 0.28f);

        // La llave no se agarra con la mano: con un clic se queda adelante de la vista
        // y al acercarse a la puerta se va sola a la cerradura. Sosteniéndola con el
        // gatillo todo el camino se caía a cada rato.
        llave.AddComponent<XRSimpleInteractable>();
        var llaveAutomatica = llave.AddComponent<LlaveAutomatica>();
        Resaltar(llave, llave.transform.Find("Cabeza").GetComponent<Renderer>());
        EditorUtility.SetDirty(llaveAutomatica);

        // Dos almohadones grandes, uno al lado del otro: debajo del segundo esta la llave.
        // Al hacerles clic se DESLIZAN de costado sobre el asiento, cada uno hacia su punta
        // del sofa. Antes eran agarrables y volaban hacia el jugador, que no era la idea.
        // El primero no tapa nada: la gracia es que haya que probar los dos.
        // Las medidas están calculadas para que, corrido y todo, el almohadón nunca se
        // pase del asiento. Tomando el caso peor (un sofá de 1,50 m, o sea de z 3.85 a
        // 5.35): el almohadón mide 34 cm de fondo y se corre 24 cm, así que el de atrás
        // termina en 5.31 y el de adelante en 3.94, los dos adentro del asiento.
        // La llave, cruzada, ocupa de 4.77 a 4.93: el segundo almohadón la tapa entera
        // (4.73 a 5.07) y al correrse queda de 4.97 para allá, o sea destapada del todo.
        float[] zAlmohadones = { 4.35f, 4.9f };
        for (int i = 0; i < zAlmohadones.Length; i++)
        {
            // Sin collider automatico: el de la esfera es una bola del tamano del lado mas
            // largo y se comia media llave. Se le pone una caja a medida.
            var almohadon = Esfera("Almohadon_" + (i + 1), p, new Vector3(5.45f, 0.53f, zAlmohadones[i]),
                                   new Vector3(0.52f, 0.22f, 0.34f), mTela);
            almohadon.transform.localEulerAngles = new Vector3(0f, 0f, i == 0 ? 4f : -5f);
            almohadon.AddComponent<BoxCollider>().size = Vector3.one;

            almohadon.AddComponent<XRSimpleInteractable>();
            var deslizar = almohadon.AddComponent<CojinDeslizante>();
            // Se corren a lo largo del sofa, cada uno para su lado, sin caerse del asiento
            deslizar.direccion = new Vector3(0f, 0f, i == 0 ? -1f : 1f);
            deslizar.distancia = 0.24f;
            Resaltar(almohadon, almohadon.GetComponent<Renderer>());
            EditorUtility.SetDirty(deslizar);
        }

        return llaveAutomatica;
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
        // Cuenta los cuatro relojes buenos. Recien cuando estan los cuatro en hora el
        // televisor muestra el codigo: si avisara con el primero, se saltearia el acertijo.
        var contador = p.gameObject.AddComponent<ContadorPasos>();
        contador.total = 4;
        AvisarPanel(contador.alCompletar, 2);
        EditorUtility.SetDirty(contador);

        caras.Add(Reloj(p, "Reloj_A", 4.8f, "A", 7, false, contador));
        caras.Add(Reloj(p, "Reloj_B", 4.2f, "B", 1, false, contador));
        caras.Add(Reloj(p, "Reloj_C", 3.6f, "C", 3, false, contador));
        caras.Add(Reloj(p, "Reloj_D", 3.0f, "D", 0, true, null));
        caras.Add(Reloj(p, "Reloj_E", 2.4f, "E", 9, false, contador));
        return caras;
    }

    // horaObjetivo es la hora a la que hay que dejarlo (y también el dígito que entrega).
    // En el señuelo va 0: ese no se resuelve, solo dispara el susto.
    //
    // Es un reloj minimalista: aro fino de grafito, esfera blanca sin vidrio, números
    // finitos y agujas tipo bastón. Antes se usaba el modelo wall_clock de Poly Haven,
    // que es un reloj de pared viejo de escuela y desentonaba con el resto del cuarto;
    // armado con piezas queda moderno y además se le puede dar el aspecto que se quiera.
    static GameObject Reloj(Transform p, string nombre, float x, string letra,
                            int horaObjetivo, bool senuelo, ContadorPasos contador)
    {
        const float HORA_APAGON = 4.67f;   // todos arrancan parados a las 4:40
        const float MINUTOS_APAGON = 40f;
        const float RADIO = 0.155f;        // de la esfera, para ubicar números y marcas

        var g = Grupo(nombre, p);
        g.transform.localPosition = new Vector3(x, ALTURA_RELOJ, 0.05f);

        // El cuerpo se ve siempre, aunque no haya energía: aro fino y esfera apagada
        Cilindro("Aro", g.transform, Vector3.zero, new Vector3(0.35f, 0.022f, 0.35f), mNegro)
            .transform.localEulerAngles = new Vector3(90f, 0f, 0f);
        Cilindro("Esfera_Apagada", g.transform, new Vector3(0f, 0f, 0.023f),
                 new Vector3(0.335f, 0.004f, 0.335f), mNegro)
            .transform.localEulerAngles = new Vector3(90f, 0f, 0f);

        // Todo lo que se enciende cuando vuelve la luz
        var cara = Grupo("Cara", g.transform);

        var esfera = Cilindro("Esfera", cara.transform, new Vector3(0f, 0f, 0.025f),
                              new Vector3(0.335f, 0.004f, 0.335f), mEsfera);
        esfera.transform.localEulerAngles = new Vector3(90f, 0f, 0f);
        Renderer esferaRenderer = esfera.GetComponent<Renderer>();

        // Marcas de las horas: doce rayitas finas pegadas al borde. Solo las doce y no
        // las sesenta de los minutos: son cinco relojes, y trescientas rayitas de más en
        // la pared le cuestan caro al Quest sin que se noten.
        var marcas = Grupo("Marcas", cara.transform);
        marcas.transform.localPosition = new Vector3(0f, 0f, 0.028f);
        for (int i = 0; i < 12; i++)
        {
            float ang = i * 30f * Mathf.Deg2Rad;
            float radio = RADIO - 0.011f;
            var marca = Cubo("Marca", marcas.transform,
                             new Vector3(-Mathf.Sin(ang) * radio, Mathf.Cos(ang) * radio, 0f),
                             new Vector3(0.005f, 0.022f, 0.002f), mNegro);
            marca.transform.localEulerAngles = new Vector3(0f, 0f, -i * 30f);
        }

        // Números de las 12 horas, finitos y un poco adentro de las marcas
        var numeros = Grupo("Numeros", cara.transform);
        numeros.transform.localPosition = new Vector3(0f, 0f, 0.028f);
        for (int h = 1; h <= 12; h++)
        {
            float ang = h * 30f * Mathf.Deg2Rad;
            // -X es la derecha de quien mira el reloj, ahí van las 3
            Texto("Numero_" + h, numeros.transform,
                  new Vector3(-Mathf.Sin(ang) * 0.113f, Mathf.Cos(ang) * 0.113f, 0f), Vector3.zero,
                  h.ToString(), 0.26f, new Color(0.12f, 0.12f, 0.14f), 0.06f, 0.05f);
        }

        // Los giros van en positivo: el reloj se mira desde su +Z, y desde ahí un giro
        // positivo en Z avanza en el sentido de las agujas (ver RelojManecilla)

        // Manecilla de los minutos: queda clavada en el minuto del apagón
        var pivMin = Grupo("Manecilla_Minuto", cara.transform);
        pivMin.transform.localPosition = new Vector3(0f, 0f, 0.031f);
        pivMin.transform.localEulerAngles = new Vector3(0f, 0f, MINUTOS_APAGON * 6f);
        Cubo("Aguja", pivMin.transform, new Vector3(0f, 0.058f, 0f), new Vector3(0.005f, 0.135f, 0.003f), mNegro);

        // Manecilla de la hora: esta es la que el jugador agarra y gira
        var pivHora = Grupo("Manecilla_Hora", cara.transform);
        pivHora.transform.localPosition = new Vector3(0f, 0f, 0.034f);
        pivHora.transform.localEulerAngles = new Vector3(0f, 0f, HORA_APAGON * 30f);
        var aguja = Cubo("Aguja", pivHora.transform, new Vector3(0f, 0.042f, 0f),
                         new Vector3(0.011f, 0.095f, 0.004f), mNegro);

        // La aguja se dibuja finita para que quede linda, pero el jugador tiene que poder
        // agarrarla sin puntería de cirujano: esta caja invisible es la que se toca
        Colision(pivHora.transform, "Zona_Aguja", new Vector3(0f, 0.045f, 0f), new Vector3(0.055f, 0.13f, 0.03f));

        Esfera("Pin", cara.transform, new Vector3(0f, 0f, 0.037f), new Vector3(0.018f, 0.018f, 0.018f), mBronce);

        // La letra va en una chapita debajo del reloj, para no taparle los números
        var chapaLetra = Grupo("Letra", g.transform);
        // Arriba del reloj: abajo aparece la chapa del dígito y antes se tapaban
        chapaLetra.transform.localPosition = new Vector3(0f, 0.235f, -0.02f);
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
            chapa.transform.localPosition = new Vector3(0f, -0.265f, 0.02f);
            Cubo("Chapa", chapa.transform, Vector3.zero, new Vector3(0.16f, 0.16f, 0.02f), mDigito);
            Texto("Numero", chapa.transform, new Vector3(0f, 0f, 0.02f), Vector3.zero,
                  horaObjetivo.ToString(), 0.9f, new Color(0.03f, 0.08f, 0.1f), 0.16f, 0.16f);
            chapa.SetActive(false);

            var luzOk = LuzApagada("Luz_EnHora", cara.transform, new Vector3(0f, 0f, 0.22f),
                                   new Color(0.4f, 1f, 0.5f), 2.5f, 0.9f);

            UnityEventTools.AddBoolPersistentListener(manecilla.alPonerEnHora, new UnityAction<bool>(luzOk.SetActive), true);
            UnityEventTools.AddBoolPersistentListener(manecilla.alPonerEnHora, new UnityAction<bool>(chapa.SetActive), true);
            // Le avisa al contador: cuando estén los cuatro, el televisor muestra el código
            if (contador != null)
                UnityEventTools.AddVoidPersistentListener(manecilla.alPonerEnHora, new UnityAction(contador.Contar));
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
        keypad.datos = DatosDelTeclado();
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
        UnityEventTools.AddBoolPersistentListener(keypad.alResolverse, new UnityAction<bool>(luzOk.SetActive), true);
        UnityEventTools.AddBoolPersistentListener(keypad.alResolverse, new UnityAction<bool>(luzMal.SetActive), false);
        UnityEventTools.AddVoidPersistentListener(keypad.alResolverse, new UnityAction(audioOk.Play));
        // El código ya no abre la puerta: habilita la cerradura, que además pide la llave
        if (cerradura != null)
            UnityEventTools.AddVoidPersistentListener(keypad.alResolverse, new UnityAction(cerradura.Habilitar));
        AvisarPanel(keypad.alResolverse, 3);

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

        // Aviso al costado de la puerta: avisa que ademas del codigo hace falta la llave.
        // Es un cuadrito chico y corrido hacia la esquina: el teclado del codigo esta del
        // otro lado del vano y antes se chocaban. Ahora quedan a mas de medio metro.
        var aviso = Grupo("Aviso_Llave", g.transform);
        aviso.transform.localPosition = new Vector3(-1.55f, 1.42f, -0.08f);
        aviso.transform.localEulerAngles = new Vector3(0f, 180f, 0f);
        Cubo("Marco", aviso.transform, Vector3.zero, new Vector3(0.46f, 0.29f, 0.025f), mNegro, true);
        // El cartel esta girado 180, asi que lo que va "adelante" (hacia el cuarto) lleva
        // z positivo. Con z negativo el papel y el texto quedaban detras del marco y no se veian.
        Cubo("Papel", aviso.transform, new Vector3(0f, 0f, 0.015f), new Vector3(0.42f, 0.25f, 0.004f), mPapel);
        var textoAviso = Texto("Texto_Aviso", aviso.transform, new Vector3(0f, 0f, 0.02f), Vector3.zero,
                               "FALTA LA LLAVE\n\nVe al estante",
                               0.29f, new Color(0.2f, 0.17f, 0.15f), 0.42f, 0.24f);
        textoAviso.lineSpacing = -12f;

        var bisagra = Grupo("Bisagra", g.transform);
        var puerta = bisagra.AddComponent<Door>();
        // Ángulo negativo: la hoja gira hacia +Z, o sea hacia afuera del cuarto
        puerta.anguloAbierto = -95f;
        puerta.duracion = 1.4f;
        puerta.segundosParaCerrar = 30f;   // red de seguridad si el jugador no sale
        puerta.esperaAlPasar = 3f;         // no le cierra la puerta encima al jugador

        // Zona del otro lado del vano, ya bien metida en el pasillo: recién cuando el
        // jugador llega hasta ahí la puerta se cierra atrás suyo, y con tres segundos
        // más de espera. Así no se cierra apenas cruza el vano.
        var salida = Grupo("Zona_Salida", g.transform);
        salida.transform.localPosition = new Vector3(ancho / 2f, 1.1f, 2f);
        var colisionSalida = salida.AddComponent<BoxCollider>();
        colisionSalida.isTrigger = true;
        colisionSalida.size = new Vector3(ancho + 0.6f, 2.2f, 1.4f);
        var disparadorSalida = salida.AddComponent<DisparadorJugador>();
        UnityEventTools.AddVoidPersistentListener(disparadorSalida.alEntrar, new UnityAction(puerta.Cerrar));
        EditorUtility.SetDirty(disparadorSalida);

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

        // El punto donde entra la llave. No tiene collider ni nada: la llave se viene
        // sola hasta acá cuando el jugador se acerca, y queda colgada de este objeto,
        // que es el que gira cuando la cerradura cede.
        var ranura = Grupo("Ranura_Llave", cerradura.transform);
        ranura.transform.localPosition = new Vector3(0f, 0.02f, -0.06f);

        // Sombra de la llave: una copia transparente, en el lugar exacto donde va la llave.
        // Arranca escondida y aparece sola cuando el jugador llega con la llave en el mando.
        // Al hacerle clic, la llave entra. Las medidas son las mismas que las de la llave
        // de verdad (ver ArmarSala), asi que la sombra calza justo con ella.
        fantasmaLlave = Grupo("Llave_Fantasma", ranura.transform);
        Cilindro("Cabeza", fantasmaLlave.transform, new Vector3(0f, 0f, -0.075f),
                 new Vector3(0.08f, 0.005f, 0.08f), mFantasma);
        Cubo("Vastago", fantasmaLlave.transform, Vector3.zero, new Vector3(0.016f, 0.01f, 0.17f), mFantasma);
        Cubo("Diente_1", fantasmaLlave.transform, new Vector3(0.026f, 0f, 0.05f), new Vector3(0.036f, 0.01f, 0.016f), mFantasma);
        Cubo("Diente_2", fantasmaLlave.transform, new Vector3(0.024f, 0f, 0.082f), new Vector3(0.03f, 0.01f, 0.016f), mFantasma);

        var colisionFantasma = fantasmaLlave.AddComponent<BoxCollider>();
        colisionFantasma.size = new Vector3(0.14f, 0.12f, 0.3f);
        fantasmaLlave.AddComponent<XRSimpleInteractable>();
        Resaltar(fantasmaLlave, fantasmaLlave.transform.Find("Vastago").GetComponent<Renderer>());
        fantasmaLlave.SetActive(false);

        var luzLista = LuzPunto("Luz_Lista", cerradura.transform, new Vector3(0f, 0.08f, -0.06f),
                                new Color(0.4f, 1f, 0.5f), 1.2f, 0.5f);
        luzLista.enabled = false;

        var perilla = Cilindro("Perilla", cerradura.transform, new Vector3(0f, -0.05f, -0.035f),
                               new Vector3(0.05f, 0.012f, 0.05f), mBronce, true);
        perilla.transform.localEulerAngles = new Vector3(90f, 0f, 0f);
        // El jugador mira la puerta desde -Z: la etiqueta se lee girada 180
        Texto("Etiqueta_Girar", cerradura.transform, new Vector3(0f, -0.1f, -0.012f), new Vector3(0f, 180f, 0f),
              "GIRAR", 0.07f, new Color(0.85f, 0.85f, 0.85f), 0.12f, 0.03f);

        var cerraduraLlave = cerradura.AddComponent<CerraduraLlave>();
        cerraduraLlave.ranura = ranura.transform;
        cerraduraLlave.luzLista = luzLista;

        perilla.AddComponent<XRSimpleInteractable>();
        var botonGirar = perilla.AddComponent<PressableButton>();
        botonGirar.parteMovil = perilla.transform;
        botonGirar.recorrido = 0.004f;
        Resaltar(perilla, perilla.GetComponent<Renderer>());
        UnityEventTools.AddVoidPersistentListener(botonGirar.alPresionar, new UnityAction(cerraduraLlave.Girar));

        // Al ceder la cerradura, la puerta se abre
        UnityEventTools.AddVoidPersistentListener(cerraduraLlave.alAbrir, new UnityAction(puerta.Abrir));
        AvisarPanel(cerraduraLlave.alAbrir, 4);

        EditorUtility.SetDirty(puerta);
        EditorUtility.SetDirty(botonGirar);
        EditorUtility.SetDirty(cerraduraLlave);
        return cerraduraLlave;
    }

    // Zona invisible apenas pasando la entrada. Cuando el jugador la pisa, el cuarto pone
    // su clima: oscuro y con niebla hasta que se sube el tablero. Se hace acá y no al darle
    // Play porque la luz ambiental y la niebla son de toda la escena, y al arrancar el
    // jugador está en el Cuarto 1.
    static void ArmarZonaClima(Transform raiz, ControlEnergia control)
    {
        var zona = Grupo("Zona_Clima", raiz);
        zona.transform.localPosition = new Vector3((ENTRADA_X0 + ENTRADA_X1) / 2f, 1f, 0.45f);

        var colision = zona.AddComponent<BoxCollider>();
        colision.isTrigger = true;
        colision.size = new Vector3(ENTRADA_X1 - ENTRADA_X0 + 0.8f, 2f, 0.7f);

        var disparador = zona.AddComponent<DisparadorJugador>();
        UnityEventTools.AddVoidPersistentListener(disparador.alEntrar, new UnityAction(control.Reaplicar));
        EditorUtility.SetDirty(disparador);
    }

    // El Cuarto 1 tiene detrás de su puerta un pasillo corto que termina en una pared,
    // porque cuando se armó todavía no había Cuarto 2. Ahora ese pasillo desemboca en la
    // entrada de este cuarto, así que la pared del fondo sobra: tapaba la entrada.
    //
    // Solo se apaga esa pared, sin tocar nada más del Cuarto 1. Si se vuelve a construir
    // el Cuarto 1, la pared vuelve a aparecer: por eso conviene construir el 1 antes que el 2.
    static void AbrirPasilloDelCuarto1()
    {
        var cuarto1 = GameObject.Find("Cuarto1_Recepcion");
        if (cuarto1 == null) return;

        // El pasillo no cuelga directo del cuarto sino de uno de sus grupos: se lo busca
        // entre todos los hijos
        Transform pared = null;
        foreach (var t in cuarto1.GetComponentsInChildren<Transform>(true))
            if (t.name == "Pared_Sur" && t.parent != null && t.parent.name == "Pasillo_Temporal")
                pared = t;
        if (pared == null) return;

        Undo.RecordObject(pared.gameObject, "Abrir pasillo del Cuarto 1");
        pared.gameObject.SetActive(false);
        EditorUtility.SetDirty(pared.gameObject);
    }

    // La llave y la cerradura se arman por separado (una en el sofá y la otra en la
    // puerta), así que acá se las presenta: la llave sabe a dónde tiene que ir y la
    // cerradura se entera cuando la llave llegó.
    static void ConectarLlave(LlaveAutomatica llave, CerraduraLlave cerradura)
    {
        if (llave == null || cerradura == null) return;

        llave.ranura = cerradura.ranura;
        llave.fantasma = fantasmaLlave;

        // Hacerle clic a la sombra es lo que mete la llave en la cerradura
        if (fantasmaLlave != null)
        {
            var tocarFantasma = fantasmaLlave.GetComponent<XRSimpleInteractable>();
            UnityEventTools.AddVoidPersistentListener(tocarFantasma.selectEntered, new UnityAction(llave.Encajar));
            EditorUtility.SetDirty(tocarFantasma);
        }

        UnityEventTools.AddVoidPersistentListener(llave.alEncajar, new UnityAction(cerradura.PonerLlave));
        // Tocar la llave ya puesta es girarla: eso lo hace el jugador, no se abre sola
        UnityEventTools.AddVoidPersistentListener(llave.alGirar, new UnityAction(cerradura.Girar));
        EditorUtility.SetDirty(llave);
    }

    // Reja que cae sobre la entrada apenas el jugador pisa el cuarto: desde ahí no se
    // puede volver al Cuarto 1, ni caminando ni teletransportándose (corta el rayo).
    static void ArmarRejaEntrada(Transform raiz)
    {
        var reja = Grupo("Reja_Entrada", raiz);
        reja.transform.localPosition = new Vector3((ENTRADA_X0 + ENTRADA_X1) / 2f, 0f, 0.02f);

        float ancho = ENTRADA_X1 - ENTRADA_X0;
        Cubo("Marco_Arriba", reja.transform, new Vector3(0f, ALTO_PUERTA - 0.04f, 0f),
             new Vector3(ancho, 0.08f, 0.06f), mMetal, true);
        Cubo("Marco_Abajo", reja.transform, new Vector3(0f, 0.04f, 0f),
             new Vector3(ancho, 0.08f, 0.06f), mMetal, true);
        for (int i = 0; i < 8; i++)
            Cubo("Barrote", reja.transform,
                 new Vector3(-ancho / 2f + 0.08f + i * (ancho - 0.16f) / 7f, ALTO_PUERTA / 2f, 0f),
                 new Vector3(0.035f, ALTO_PUERTA, 0.035f), mMetal, true);

        var audio = AudioEn("Audio_Reja", reja.transform);
        reja.SetActive(false);

        // Zona que cubre casi todo el cuarto: apenas el jugador está adentro, cae la reja
        var zona = Grupo("Zona_Entrada", raiz);
        zona.transform.localPosition = new Vector3(ANCHO / 2f, 1.2f, FONDO / 2f);
        var colision = zona.AddComponent<BoxCollider>();
        colision.isTrigger = true;
        colision.size = new Vector3(ANCHO - 0.8f, 2.4f, FONDO - 0.8f);

        var disparador = zona.AddComponent<DisparadorJugador>();
        UnityEventTools.AddBoolPersistentListener(disparador.alEntrar, new UnityAction<bool>(reja.SetActive), true);
        UnityEventTools.AddVoidPersistentListener(disparador.alEntrar, new UnityAction(audio.Play));
        EditorUtility.SetDirty(disparador);
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

    // Los datos del acertijo del teclado (el asset PuzzleData_Cuarto2), siempre al día.
    //
    // Si el asset no está o Unity no lo puede leer, se crea de nuevo en vez de dejar el
    // teclado sin código. Pasó una vez al unir las ramas: el asset quedó apuntando a una
    // versión de PuzzleData.cs que ya no existía, el teclado se quedó sin datos y
    // rechazaba el 3719 aunque estuviera bien puesto.
    static PuzzleData DatosDelTeclado()
    {
        const string RUTA = "Assets/ScriptableObjects/Puzzles/PuzzleData_Cuarto2.asset";

        var datos = BuscarAsset<PuzzleData>("PuzzleData_Cuarto2");
        if (datos == null)
        {
            Debug.LogWarning("PuzzleData_Cuarto2 no se pudo leer: se crea de nuevo.");
            AssetDatabase.DeleteAsset(RUTA);   // por si quedó un archivo roto en esa ruta
            datos = ScriptableObject.CreateInstance<PuzzleData>();
            AssetDatabase.CreateAsset(datos, RUTA);
        }

        datos.id = "cuarto2_codigo";
        datos.nombreCuarto = "Dirección";
        datos.tipo = TipoAcertijo.Codigo;
        datos.solucion = "3719";
        datos.pista = "Primero hay que devolver la energia desde el tablero. La bitacora sobre el " +
                      "aparador da la hora de cada campana y el orden; la placa del cuadro dice que " +
                      "reloj es cada campana. Cada reloj se pone en hora girando su manecilla.";
        datos.mensajeAcierto = "La cerradura del teclado hace clic y la puerta se abre";
        datos.mensajeError = "El teclado parpadea en rojo: codigo incorrecto";
        EditorUtility.SetDirty(datos);
        return datos;
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
        return ColocarModelo(fuente, nombrePolyHaven, padre, pos, giroY, altoReal, colisiona, apoyar);
    }

    // Igual que Modelo, pero con un modelo de Sketchfab (carpeta Assets/Sketchfab).
    // Tienen licencia CC BY y sus autores están anotados en Assets/Sketchfab/CREDITOS.txt.
    //
    // Sketchfab deja bajar dos formatos y acá se aceptan los dos:
    //  - GLB: un solo archivo, "Assets/Sketchfab/<nombre>.glb" (así están los del Cuarto 3).
    //  - glTF: una carpeta "Assets/Sketchfab/<nombre>" con scene.gltf adentro, el .bin y las
    //    texturas sueltas (así están el cuadro y el globo del Cuarto 2).
    // Si no está en ninguno de los dos devuelve null, igual que Modelo, y el cuarto se arma
    // con la versión de Poly Haven.
    static GameObject ModeloSketchfab(string archivo, Transform padre, Vector3 pos, float giroY,
                                      float altoReal, bool colisiona, bool apoyar = true)
    {
        const string CARPETA = "Assets/Sketchfab/";
        var fuente = AssetDatabase.LoadAssetAtPath<GameObject>(CARPETA + archivo + ".glb");
        if (fuente == null)
            fuente = AssetDatabase.LoadAssetAtPath<GameObject>(CARPETA + archivo + "/scene.gltf");
        if (fuente == null) return null;
        return ColocarModelo(fuente, archivo, padre, pos, giroY, altoReal, colisiona, apoyar);
    }

    static GameObject ColocarModelo(GameObject fuente, string nombre, Transform padre, Vector3 pos,
                                    float giroY, float altoReal, bool colisiona, bool apoyar)
    {
        // El giro va en un objeto contenedor: así no se pisa la rotación que trae el
        // archivo (los FBX que vienen de Blender suelen traer una corrección en X)
        var contenedor = Grupo(nombre, padre);
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
        // Pantallas encendidas. La suavidad va casi en cero a propósito: con brillo alto
        // reflejan las luces del techo y el reflejo tapa las letras. Mates, se leen igual
        // de lejos y desde cualquier ángulo.
        mPantallaPC = Mat("C2_PantallaPC", new Color(0.02f, 0.05f, 0.08f), 0f, 0.03f,
                          new Color(0.03f, 0.1f, 0.15f));
        mPantallaTV = Mat("C2_PantallaTV", new Color(0.02f, 0.05f, 0.09f), 0f, 0.03f,
                          new Color(0.04f, 0.13f, 0.2f));
        // Vidrio de una pantalla apagada: negro espejado, no se lee nada
        mVidrioApagado = Mat("C2_VidrioApagado", new Color(0.02f, 0.02f, 0.025f), 0.2f, 0.92f);
        // Aluminio cepillado: frente del tablero, marco del televisor y bandeja del teclado
        mAluminio = Mat("C2_Aluminio", new Color(0.62f, 0.63f, 0.65f), 0.7f, 0.65f);
        // Rojo de la llave general. Es emisivo a propósito: el cuarto entra a oscuras y
        // una llave de rojo mate no se ve, así que el jugador no sabe qué tocar.
        mRojoMate = Mat("C2_RojoMate", new Color(0.85f, 0.15f, 0.1f), 0f, 0.35f,
                        new Color(0.7f, 0.1f, 0.05f));
        // Teclas de la computadora: las comunes, la verde apagada y la verde encendida
        mTecla = Mat("C2_Tecla", new Color(0.13f, 0.13f, 0.14f), 0.1f, 0.25f);
        mTeclaVerde = Mat("C2_TeclaVerde", new Color(0.13f, 0.45f, 0.2f), 0f, 0.3f);
        mTeclaVerdeOk = Mat("C2_TeclaVerdeOk", new Color(0.25f, 0.95f, 0.4f), 0f, 0.5f,
                            new Color(0.2f, 1.1f, 0.35f));
        // Llave de sombra: se ve a través de ella, así se entiende que todavía no está puesta
        mFantasma = MatFantasma("C2_Fantasma", new Color(1f, 0.82f, 0.25f, 0.4f),
                                new Color(0.55f, 0.4f, 0.06f));
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
        // Dorado bien visible, para que la llave se note apenas se levanta el almohadón
        mOro = Mat("C2_Oro", new Color(1f, 0.78f, 0.15f), 0.9f, 0.75f, new Color(0.35f, 0.25f, 0.03f));
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
    // Material que se ve a través: lo usa la llave de sombra de la cerradura.
    // En URP la transparencia no alcanza con bajarle el alfa al color: hay que pasar el
    // material a modo Transparent a mano, que es lo que hacen estas líneas.
    static Material MatFantasma(string nombre, Color color, Color emision)
    {
        var m = Mat(nombre, color, 0f, 0.6f, emision);
        m.SetFloat("_Surface", 1f);                 // 1 = Transparent
        m.SetFloat("_Blend", 0f);                   // mezcla por alfa
        m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        m.SetFloat("_ZWrite", 0f);
        m.SetFloat("_AlphaClip", 0f);
        m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        m.DisableKeyword("_ALPHATEST_ON");
        m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        // Mat() pisa el color con el del Mat, pero sin alfa: se lo vuelve a poner
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", color);
        if (m.HasProperty("_Color")) m.SetColor("_Color", color);
        EditorUtility.SetDirty(m);
        return m;
    }

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
