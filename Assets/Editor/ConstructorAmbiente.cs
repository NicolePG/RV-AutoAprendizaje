using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

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
        public float gravedad;
        public float volumenSinLuz;
        public float volumenConLuz;
        public Vector2 cadaCuanto;
    }

    static readonly Ajuste[] AJUSTES =
    {
        // Recepción: el viento que entra por la entrada tapiada, y la madera del mostrador
        new Ajuste { cuarto = "Cuarto1_Recepcion", gravedad = 0.93f, volumenSinLuz = 0.30f,
                     volumenConLuz = 0.14f, cadaCuanto = new Vector2(18f, 40f) },
        // Dirección: zumbido de la instalación eléctrica, más cerrado
        new Ajuste { cuarto = "Cuarto2_Oficina", gravedad = 0.88f, volumenSinLuz = 0.32f,
                     volumenConLuz = 0.15f, cadaCuanto = new Vector2(16f, 36f) },
        // Sala de computación: de noche, con los equipos y sus ventiladores
        new Ajuste { cuarto = "Cuarto3_Computacion", gravedad = 0.80f, volumenSinLuz = 0.30f,
                     volumenConLuz = 0.20f, cadaCuanto = new Vector2(20f, 44f) },
        // Laboratorio: el más grave y el más incómodo, que es donde está el último susto
        new Ajuste { cuarto = "Cuarto4_Laboratorio", gravedad = 0.95f, volumenSinLuz = 0.38f,
                     volumenConLuz = 0.18f, cadaCuanto = new Vector2(12f, 30f) },
    };

    [MenuItem("Escape Room/Armar ambiente sonoro", false, 30)]
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

            if (!Medir(cuarto.transform, out Vector3 centro, out float radio)) continue;

            var go = new GameObject("Ambiente_" + ajuste.cuarto);
            go.transform.SetParent(raiz.transform, false);
            // A la altura de la cabeza, en el medio del cuarto
            go.transform.position = new Vector3(centro.x, 1.6f, centro.z);

            var ambiente = go.AddComponent<AmbienteCuarto>();
            ambiente.gravedad = ajuste.gravedad;
            ambiente.volumenSinLuz = ajuste.volumenSinLuz;
            ambiente.volumenConLuz = ajuste.volumenConLuz;
            ambiente.cadaCuanto = ajuste.cadaCuanto;
            // Un poco más que el cuarto, para que no se corte de golpe al cruzar la puerta
            ambiente.alcance = radio + 3f;
            ambiente.dispersion = radio * 0.6f;
            EditorUtility.SetDirty(ambiente);
            puestos++;
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("Ambiente sonoro puesto en " + puestos + " cuartos. Suena un ruido de fondo " +
                  "por cuarto (más fuerte a oscuras) y crujidos sueltos cada tanto.");
    }

    // El lugar que ocupa un cuarto, medido por lo que se dibuja adentro
    static bool Medir(Transform cuarto, out Vector3 centro, out float radio)
    {
        centro = cuarto.position;
        radio = 6f;

        var renderers = cuarto.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return false;

        Bounds caja = renderers[0].bounds;
        foreach (Renderer r in renderers) caja.Encapsulate(r.bounds);

        centro = caja.center;
        // El radio sale del lado más largo en el piso: lo que tiene que cubrir el sonido
        radio = Mathf.Max(caja.size.x, caja.size.z) * 0.5f;
        return true;
    }
}
