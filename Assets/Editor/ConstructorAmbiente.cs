using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;

// Pone el ruido de fondo de cada cuarto (ver AmbienteCuarto).
//
// Los objetos van colgados de un grupo "Ambiente" que está afuera de los cuartos, y no
// adentro de cada uno, a propósito: así no se borran cuando se reconstruye un cuarto suelto.
//
// El centro y el tamaño de cada cuarto se miden solos, a partir de lo que ocupa en la
// escena, así que si un cuarto cambia de tamaño esto se acomoda sin tocar nada.
//
// Lo llama el menú Escape Room > Construir los 4 cuartos, y también se puede correr suelto
// desde Escape Room > Armar ambiente sonoro.
public static class ConstructorAmbiente
{
    // Cada cuarto tiene su carácter: el colegio vacío suena distinto que el laboratorio.
    // gravedad: 0 = siseo fino, 0.95 = rugido grave.
    struct Ajuste
    {
        public string cuarto;
        public string fondo;
        public string entrada;
        public float gravedad;
        public float volumenSinLuz;
        public float volumenConLuz;
        public Vector2 cadaCuanto;
    }

    static readonly Ajuste[] AJUSTES =
    {
        // Recepción: el viento que entra por la entrada tapiada, y la madera del mostrador
        new Ajuste { cuarto = "Cuarto1_Recepcion", fondo = "ambiente_cuarto1", gravedad = 0.93f, volumenSinLuz = 0.30f,
                     volumenConLuz = 0.14f, cadaCuanto = new Vector2(18f, 40f) },
        // Dirección: zumbido de la instalación eléctrica, más cerrado
        new Ajuste { cuarto = "Cuarto2_Oficina", fondo = "ambiente_cuarto2", gravedad = 0.88f, volumenSinLuz = 0.32f,
                     volumenConLuz = 0.15f, cadaCuanto = new Vector2(16f, 36f) },
        // Sala de computación: de noche, con los equipos y sus ventiladores
        new Ajuste { cuarto = "Cuarto3_Computacion", fondo = "ambiente_cuarto3", gravedad = 0.80f, volumenSinLuz = 0.30f,
                     volumenConLuz = 0.20f, cadaCuanto = new Vector2(20f, 44f) },
        // Laboratorio: el más grave y el más incómodo, que es donde está el último susto
        new Ajuste { cuarto = "Cuarto4_Laboratorio", fondo = "ambiente_cuarto4", entrada = "entrada_cuarto4",
                     gravedad = 0.95f, volumenSinLuz = 0.38f,
                     volumenConLuz = 0.18f, cadaCuanto = new Vector2(12f, 30f) },
    };

    // Lo llama "Escape Room > Construir los 4 cuartos" (MenuEscapeRoom): no tiene menú propio
    public static void Construir()
    {
        var raiz = GameObject.Find("Ambiente");
        if (raiz == null)
        {
            raiz = new GameObject("Ambiente");
            Undo.RegisterCreatedObjectUndo(raiz, "Armar ambiente sonoro");
        }

        for (int i = raiz.transform.childCount - 1; i >= 0; i--)
            Undo.DestroyObjectImmediate(raiz.transform.GetChild(i).gameObject);

        int puestos = 0;
        foreach (Ajuste ajuste in AJUSTES)
        {
            GameObject cuarto = GameObject.Find(ajuste.cuarto);
            if (cuarto == null)
            {
                Debug.LogWarning("Ambiente: no está " + ajuste.cuarto + ", se saltea.");
                continue;
            }

            if (!Medir(cuarto.transform, out Vector3 centro, out Vector3 mitad)) continue;

            var go = new GameObject("Ambiente_" + ajuste.cuarto);
            go.transform.SetParent(raiz.transform, false);
            // A la altura de la cabeza, en el medio del cuarto
            go.transform.position = new Vector3(centro.x, 1.6f, centro.z);

            var ambiente = go.AddComponent<AmbienteCuarto>();
            ambiente.mitadDelCuarto = mitad;
            ambiente.nombreDelFondo = ajuste.fondo;
            ambiente.nombreDeLaEntrada = ajuste.entrada;
            ambiente.gravedad = ajuste.gravedad;
            ambiente.volumenSinLuz = ajuste.volumenSinLuz;
            ambiente.volumenConLuz = ajuste.volumenConLuz;
            ambiente.cadaCuanto = ajuste.cadaCuanto;
            // Justo el cuarto y nada más: si el alcance se pasa, se oye el de al lado
            ambiente.alcance = Mathf.Max(mitad.x, mitad.z) + 1f;
            ambiente.dispersion = Mathf.Min(mitad.x, mitad.z) * 0.7f;
            EditorUtility.SetDirty(ambiente);
            puestos++;
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("Ambiente sonoro puesto en " + puestos + " cuartos. Suena un ruido de fondo " +
                  "por cuarto (más fuerte a oscuras) y crujidos sueltos cada tanto.");
    }

    // El lugar que ocupa un cuarto. Se mide por su PISO, no por todo lo que se dibuja
    // adentro: el piso es exactamente la planta del cuarto, mientras que "todo lo que se
    // dibuja" incluye cosas que se salen (un pasillo, una puerta abierta, el cielo de la
    // ventana). Midiendo así, el ambiente del Cuarto 3 quedaba 6 metros corrido y se metía
    // adentro del Cuarto 2: se oían los dos mezclados y parecía que tuvieran el mismo sonido.
    //
    // El piso es el objeto que lleva la zona de teletransporte, que es justo por donde el
    // jugador puede caminar. Si no lo encuentra, vuelve a medir por lo que se dibuja.
    static bool Medir(Transform cuarto, out Vector3 centro, out Vector3 mitad)
    {
        centro = cuarto.position;
        mitad = new Vector3(6f, 3f, 6f);

        Bounds caja = default;
        bool hay = false;

        foreach (var piso in cuarto.GetComponentsInChildren<TeleportationArea>())
        {
            var r = piso.GetComponent<Renderer>();
            if (r == null) continue;
            if (!hay) { caja = r.bounds; hay = true; }
            else caja.Encapsulate(r.bounds);
        }

        if (!hay)
        {
            var renderers = cuarto.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return false;
            caja = renderers[0].bounds;
            foreach (Renderer r in renderers) caja.Encapsulate(r.bounds);
            Debug.LogWarning("Ambiente: " + cuarto.name + " no tiene piso con zona de " +
                             "teletransporte, se mide por lo que se dibuja (menos exacto).");
        }

        centro = caja.center;
        mitad = new Vector3(caja.size.x * 0.5f, Mathf.Max(caja.size.y * 0.5f, 2f), caja.size.z * 0.5f);
        return true;
    }
}
