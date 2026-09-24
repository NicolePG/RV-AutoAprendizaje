using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// El menú Escape Room de Unity, todo en un lugar:
//  - Construir los 4 cuartos: arma la escena completa, en orden, y al final hornea la luz.
//  - Llevar jugador al Cuarto 1, 2, 3 o 4: pone al jugador en la entrada de ese cuarto, para
//    dar Play y probarlo sin tener que resolver los anteriores.
//
// Cada cuarto se sigue armando con su propio constructor (Cuarto1Builder, ConstructorCuarto2,
// ConstructorCuarto3 y ConstructorCuarto4): este menú solo los llama en el orden correcto.
public static class MenuEscapeRoom
{
    [MenuItem("Escape Room/Construir los 4 cuartos", false, 0)]
    static void ConstruirTodo()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogWarning("Detén el modo Play antes de construir los cuartos.");
            return;
        }
        if (Lightmapping.isRunning)
        {
            Debug.LogWarning("Todavía se está horneando la luz: espera a que termine la barra de abajo.");
            return;
        }
        // Si Unity no terminó de compilar (o hay errores), el menú correría la versión VIEJA de
        // los constructores y armaría los cuartos como estaban antes de los últimos cambios
        if (EditorApplication.isCompiling || EditorUtility.scriptCompilationFailed)
        {
            Debug.LogWarning("Unity todavía está compilando los scripts (o hay errores en la consola). " +
                             "Espera a que termine la ruedita de abajo a la derecha y vuelve a construir.");
            return;
        }
        if (!Cuarto1Builder.AbrirEscenaJuego()) return;

        try
        {
            Paso("Cuarto 1 (Recepción)", 0.1f);
            Cuarto1Builder.Construir();
            Paso("Cuarto 2 (Dirección)", 0.35f);
            ConstructorCuarto2.Construir();
            Paso("Cuarto 4 (Laboratorio)", 0.6f);
            ConstructorCuarto4.Construir();
            // El 3 va último: ocupa el lugar del pasillo provisional (lo apaga) y deja lista la
            // optimización (el objeto "Optimizacion")
            Paso("Cuarto 3 (Sala de Computación)", 0.85f);
            ConstructorCuarto3.Construir();
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        // El juego empieza en el Cuarto 1
        Cuarto1Builder.ColocarJugador();
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());

        // La luz se hornea una sola vez, con todos los cuartos armados. Al terminar se guarda sola.
        Debug.Log("Los 4 cuartos están armados. Horneando la luz: espera a que termine la barra de abajo antes de dar Play.");
        Cuarto1Builder.HornearLuz();
    }

    static void Paso(string cuarto, float avance) =>
        EditorUtility.DisplayProgressBar("Construyendo los 4 cuartos", cuarto + "...", avance);

    [MenuItem("Escape Room/Llevar jugador al Cuarto 1", false, 20)]
    static void LlevarAlCuarto1()
    {
        Transform jugador = Jugador();
        if (jugador == null) return;
        Undo.RecordObject(jugador, "Llevar jugador al Cuarto 1");
        Cuarto1Builder.ColocarJugador();   // el mismo lugar donde empieza el juego
        Selection.activeGameObject = jugador.gameObject;
    }

    [MenuItem("Escape Room/Llevar jugador al Cuarto 2", false, 21)]
    static void LlevarAlCuarto2()
    {
        var raiz = GameObject.Find("Cuarto2_Oficina");
        if (raiz == null)
        {
            Debug.LogWarning("Primero hay que construir los cuartos (Escape Room > Construir los 4 cuartos).");
            return;
        }
        Transform jugador = Jugador();
        if (jugador == null) return;

        Undo.RecordObject(jugador, "Llevar jugador al Cuarto 2");
        // Frente a la entrada, a 60 cm de la puerta: adentro de la zona que pone el clima del cuarto.
        // TransformPoint y no una suma, porque el cuarto está girado (ver DisposicionCuartos).
        jugador.position = raiz.transform.TransformPoint(new Vector3(1.35f, 0f, 0.6f));
        jugador.rotation = raiz.transform.rotation;
        Selection.activeGameObject = jugador.gameObject;
    }

    [MenuItem("Escape Room/Llevar jugador al Cuarto 3", false, 22)]
    static void LlevarAlCuarto3() => ConstructorCuarto3.LlevarJugador();

    [MenuItem("Escape Room/Llevar jugador al Cuarto 4", false, 23)]
    static void LlevarAlCuarto4() => ConstructorCuarto4.LlevarJugador();

    // El jugador es el objeto de más arriba de todos los que tienen la cámara adentro (el XR Origin)
    static Transform Jugador()
    {
        Camera camara = Camera.main;
        if (camara == null)
        {
            Debug.LogWarning("No se encontró la cámara del jugador en la escena.");
            return null;
        }
        Transform jugador = camara.transform;
        while (jugador.parent != null) jugador = jugador.parent;
        return jugador;
    }
}
