using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using TMPro;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;

// Pasillo provisional entre el Cuarto 2 y el Cuarto 4.
//
// El Cuarto 3 lo está haciendo el compañero, así que por ahora no hay nada en el medio
// y no se puede llegar caminando al laboratorio. Esto arma un pasillo en forma de L que
// sale por la puerta del Cuarto 2 y termina en la entrada del Cuarto 4, con piso
// teletransportable, paredes y luces.
//
// Es temporal: cuando esté el Cuarto 3 se borra el objeto "Pasillo_Provisional" de la
// escena y se borra este script.
//
// Ya no está en el menú: el Cuarto 3 ocupa su lugar y su constructor apaga el pasillo.
public static class ConstructorPasillo
{
    // El Cuarto 2 termina en Z = 11 y su puerta está entre X 4.4 y 5.6.
    // El Cuarto 4 empieza en Z = 19 y su entrada está entre X 0.8 y 1.9.
    const float Z_CUARTO2 = 11f, Z_CUARTO4 = 19f;
    const float TRAMO_X0 = 4.2f, TRAMO_X1 = 5.8f;   // tramo que sale de la puerta
    const float CODO_Z = 17.4f;                      // donde dobla hacia el Oeste
    const float CODO_X0 = 0.6f;                      // hasta dónde llega el tramo ancho
    const float ENTRADA_X0 = 0.8f, ENTRADA_X1 = 1.9f;
    const float ALTO = 2.6f, MURO = 0.12f;

    static Material mPiso, mPared, mFranja, mNegro, mCartel;

    public static void Construir()
    {
        var raiz = GameObject.Find("Pasillo_Provisional");
        if (raiz == null)
        {
            raiz = new GameObject("Pasillo_Provisional");
            Undo.RegisterCreatedObjectUndo(raiz, "Construir pasillo");
        }
        // Con el mismo giro y corrimiento que los Cuartos 2 y 4, así sigue uniéndolos
        // (ver DisposicionCuartos). Las medidas de abajo quedan igual que antes.
        raiz.transform.SetPositionAndRotation(DisposicionCuartos.Pasillo, DisposicionCuartos.Giro);

        for (int i = raiz.transform.childCount - 1; i >= 0; i--)
            Undo.DestroyObjectImmediate(raiz.transform.GetChild(i).gameObject);

        CrearMateriales();

        var piso = Grupo("Piso", raiz.transform);
        // Tramo largo: sale derecho de la puerta del Cuarto 2 hacia el Norte
        Losa(piso.transform, "Piso_Tramo", TRAMO_X0, TRAMO_X1, Z_CUARTO2, CODO_Z);
        // Tramo ancho: cruza hasta la entrada del Cuarto 4
        Losa(piso.transform, "Piso_Codo", CODO_X0, TRAMO_X1, CODO_Z, Z_CUARTO4);

        var paredes = Grupo("Paredes", raiz.transform);
        // Los dos lados del tramo largo
        ParedZ(paredes.transform, "Pared_Oeste_Tramo", TRAMO_X0, Z_CUARTO2, CODO_Z);
        ParedZ(paredes.transform, "Pared_Este", TRAMO_X1, Z_CUARTO2, Z_CUARTO4);
        // El codo
        ParedX(paredes.transform, "Pared_Sur_Codo", CODO_Z, CODO_X0, TRAMO_X0);
        ParedZ(paredes.transform, "Pared_Oeste_Codo", CODO_X0, CODO_Z, Z_CUARTO4);
        // El fondo no lleva pared: la pared de la entrada del Cuarto 4 ya cierra ahí,
        // y si se pone otra encima quedan las dos parpadeando una contra la otra

        // Franja amarilla pintada en el piso, como en las obras: marca el camino
        var franja = Grupo("Franja", raiz.transform);
        Cubo("Franja_Tramo", franja.transform,
             new Vector3((TRAMO_X0 + TRAMO_X1) / 2f, 0.011f, (Z_CUARTO2 + CODO_Z) / 2f),
             new Vector3(0.12f, 0.01f, CODO_Z - Z_CUARTO2), mFranja);
        Cubo("Franja_Codo", franja.transform,
             new Vector3((CODO_X0 + TRAMO_X1) / 2f, 0.011f, CODO_Z + 0.55f),
             new Vector3(TRAMO_X1 - CODO_X0, 0.01f, 0.12f), mFranja);
        Cubo("Franja_Entrada", franja.transform,
             new Vector3((ENTRADA_X0 + ENTRADA_X1) / 2f, 0.011f, (CODO_Z + Z_CUARTO4) / 2f),
             new Vector3(0.12f, 0.01f, Z_CUARTO4 - CODO_Z), mFranja);

        ArmarCartel(raiz.transform);

        // Luces colgadas de obra, una cada tres metros
        var luces = Grupo("Luces", raiz.transform);
        Foco(luces.transform, new Vector3(5f, ALTO - 0.2f, 12.5f));
        Foco(luces.transform, new Vector3(5f, ALTO - 0.2f, 15.5f));
        Foco(luces.transform, new Vector3(3.2f, ALTO - 0.2f, 18f));
        Foco(luces.transform, new Vector3(1.35f, ALTO - 0.2f, 18.4f));

        foreach (Transform hijo in raiz.transform)
            Undo.RegisterCreatedObjectUndo(hijo.gameObject, "Construir pasillo");

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(raiz.scene);
        Selection.activeGameObject = raiz;
        Debug.Log("Pasillo provisional armado. Acordate de guardar la escena con Ctrl+S.");
    }

    // Losa de piso con zona de teletransporte, para poder caminarla
    static void Losa(Transform p, string nombre, float x0, float x1, float z0, float z1)
    {
        var losa = Cubo(nombre, p, new Vector3((x0 + x1) / 2f, -0.1f, (z0 + z1) / 2f),
                        new Vector3(x1 - x0, 0.2f, z1 - z0), mPiso, true);

        var area = losa.AddComponent<TeleportationArea>();
        int capaTeleport = InteractionLayerMask.GetMask("Teleport");
        if (capaTeleport == 0) capaTeleport = 1 << 31;
        area.interactionLayers = capaTeleport;
    }

    // Pared a lo largo del eje Z (mira al Este o al Oeste)
    static void ParedZ(Transform p, string nombre, float x, float z0, float z1)
    {
        if (z1 <= z0) return;
        Cubo(nombre, p, new Vector3(x, ALTO / 2f, (z0 + z1) / 2f),
             new Vector3(MURO, ALTO, z1 - z0), mPared, true);
        Cubo(nombre + "_Zocalo", p, new Vector3(x, 0.05f, (z0 + z1) / 2f),
             new Vector3(MURO + 0.02f, 0.1f, z1 - z0), mNegro);
    }

    // Pared a lo ancho, sobre el eje X
    static void ParedX(Transform p, string nombre, float z, float x0, float x1)
    {
        if (x1 <= x0) return;
        Cubo(nombre, p, new Vector3((x0 + x1) / 2f, ALTO / 2f, z),
             new Vector3(x1 - x0, ALTO, MURO), mPared, true);
        Cubo(nombre + "_Zocalo", p, new Vector3((x0 + x1) / 2f, 0.05f, z),
             new Vector3(x1 - x0, 0.1f, MURO + 0.02f), mNegro);
    }

    // Cartel de obra al salir del Cuarto 2, para que se entienda que esto es provisional
    static void ArmarCartel(Transform raiz)
    {
        var g = Grupo("Cartel_Obra", raiz);
        g.transform.localPosition = new Vector3(TRAMO_X0 + 0.09f, 1.7f, 12.6f);
        g.transform.localEulerAngles = new Vector3(0f, 90f, 0f);

        Cubo("Marco", g.transform, Vector3.zero, new Vector3(1.2f, 0.7f, 0.04f), mNegro, true);
        Cubo("Chapa", g.transform, new Vector3(0f, 0f, 0.025f), new Vector3(1.12f, 0.62f, 0.01f), mCartel);

        var texto = Texto("Texto", g.transform, new Vector3(0f, 0f, 0.035f), Vector3.zero,
                          "CUARTO 3\nEN CONSTRUCCION\n\nSEGUI EL PASILLO\nHASTA EL LABORATORIO",
                          0.42f, new Color(0.1f, 0.1f, 0.1f), 1.08f, 0.58f);
        texto.lineSpacing = -14f;

        LuzPunto(g.transform, new Vector3(0f, 0.1f, 0.6f), new Color(1f, 0.97f, 0.85f), 1.6f, 2.5f);
    }

    static void Foco(Transform p, Vector3 pos)
    {
        var g = Grupo("Foco", p);
        g.transform.localPosition = pos;
        Cilindro("Pantalla", g.transform, Vector3.zero, new Vector3(0.24f, 0.05f, 0.24f), mNegro);
        Cilindro("Bombilla", g.transform, new Vector3(0f, -0.06f, 0f), new Vector3(0.14f, 0.01f, 0.14f), mFranja);
        LuzPunto(g.transform, new Vector3(0f, -0.2f, 0f), new Color(1f, 0.96f, 0.88f), 2.2f, 6f);
    }

    // ------------------------------------------------------------------ piezas básicas

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
        // TextMeshPro se lee desde su -Z, así que se lo gira para que "rot" se piense
        // como si se leyera desde su +Z, igual que en los constructores de los cuartos
        go.transform.Rotate(0f, 180f, 0f, Space.Self);

        go.GetComponent<RectTransform>().sizeDelta = new Vector2(ancho, alto);

        var t = go.GetComponent<TextMeshPro>();
        t.text = texto;
        t.fontSize = tamano;
        t.color = color;
        t.alignment = TextAlignmentOptions.Center;
        return t;
    }

    static void LuzPunto(Transform padre, Vector3 pos, Color color, float intensidad, float rango)
    {
        var go = new GameObject("Luz");
        go.transform.SetParent(padre, false);
        go.transform.localPosition = pos;
        var l = go.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = color;
        l.intensity = intensidad;
        l.range = rango;
    }

    static void CrearMateriales()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Materials"))
            AssetDatabase.CreateFolder("Assets", "Materials");
        if (!AssetDatabase.IsValidFolder("Assets/Materials/Pasillo"))
            AssetDatabase.CreateFolder("Assets/Materials", "Pasillo");

        mPiso = Mat("P_Piso", new Color(0.42f, 0.42f, 0.44f), 0.05f);
        mPared = Mat("P_Pared", new Color(0.66f, 0.66f, 0.63f), 0.05f);
        mNegro = Mat("P_Negro", new Color(0.08f, 0.08f, 0.09f), 0.2f);
        mFranja = Mat("P_Franja", new Color(0.95f, 0.78f, 0.1f), 0.3f, new Color(0.5f, 0.4f, 0.05f));
        mCartel = Mat("P_Cartel", new Color(0.95f, 0.82f, 0.2f), 0.2f);
    }

    static Material Mat(string nombre, Color color, float suavidad, Color emision = default)
    {
        string ruta = "Assets/Materials/Pasillo/" + nombre + ".mat";
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
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", suavidad);

        if (emision != default(Color))
        {
            m.EnableKeyword("_EMISSION");
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            if (m.HasProperty("_EmissionColor")) m.SetColor("_EmissionColor", emision);
        }

        EditorUtility.SetDirty(m);
        return m;
    }
}
