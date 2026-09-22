using System.Collections.Generic;
using UnityEngine;

// Optimización: dibuja solo los cuartos que el jugador puede llegar a ver.
//
// Los 4 cuartos están en una sola escena (así el reloj y el inventario no se pierden al pasar
// de uno a otro). El problema: Unity dibuja todo lo que queda delante de la cámara, aunque esté
// detrás de una pared. Parado en el Cuarto 1 y mirando hacia el sur, el Quest dibujaba los 4
// cuartos enteros (más de un millón y medio de triángulos y decenas de luces), y ahí caían los FPS.
//
// Este script se fija en qué cuarto está el jugador (mira qué piso tiene debajo) y deja
// dibujados solo:
//  - el cuarto donde está y los dos de al lado (por su puerta se los puede ver);
//  - un cuarto más lejano, solo si todas las puertas entre el jugador y ese cuarto están abiertas.
// A los demás les apaga el dibujo (Renderer.forceRenderingOff) y sus luces. No los desactiva:
// sus scripts, colliders y acertijos siguen funcionando igual; solo no se dibujan.
//
// Nunca apaga las puertas que unen los cuartos (desde el otro lado se vería un hueco) ni lo que
// salió de su cuarto (por ejemplo, un objeto del Cuarto 2 que el jugador llevó al Cuarto 4).
//
// En la escena: va en el objeto "Optimizacion". Lo crea el menú Escape Room > Optimizar escena
// (también lo hace el constructor del Cuarto 3). El orden de "cuartos" es el del recorrido.
public class GestorDeCuartos : MonoBehaviour
{
    [Tooltip("Nombres de los cuartos en la escena, en el orden en que se recorren")]
    public string[] cuartos = { "Cuarto1_Recepcion", "Cuarto2_Oficina", "Cuarto3_Computacion", "Cuarto4_Laboratorio" };

    [Tooltip("Cada cuántos segundos revisa en qué cuarto está el jugador")]
    public float intervalo = 0.25f;

    class Cuarto
    {
        public Transform raiz;
        public Bounds zona;           // el lugar que ocupa el cuarto en el mundo
        public Door puertaSiguiente;  // la puerta que lo une con el cuarto que sigue
        public bool visible = true;   // al empezar se ve todo
        public readonly List<Light> lucesApagadas = new List<Light>();
    }

    readonly List<Cuarto> lista = new List<Cuarto>();
    readonly List<Transform> puertasDeUnion = new List<Transform>();
    readonly RaycastHit[] golpes = new RaycastHit[8];
    int actual = -1;                  // cuarto del jugador (-1 = todavía no se sabe)
    bool preparado;
    float proximaRevision;

    // Se prepara en el primer Update y no en Start: así los demás scripts ya hicieron su Start
    // (por ejemplo, el que apaga las luces de un cuarto sin energía) y no se pisan
    void Update()
    {
        if (!preparado) Preparar();
        if (Time.time < proximaRevision) return;
        proximaRevision = Time.time + intervalo;

        actual = CuartoDelJugador();
        for (int i = 0; i < lista.Count; i++)
        {
            bool ver = DebeVerse(i);
            if (ver != lista[i].visible) Mostrar(lista[i], ver);
        }
    }

    void Preparar()
    {
        preparado = true;
        foreach (string nombre in cuartos)
        {
            GameObject raiz = GameObject.Find(nombre);   // una sola vez, al empezar
            if (raiz != null) lista.Add(new Cuarto { raiz = raiz.transform, zona = Zona(raiz.transform) });
        }

        // La puerta que une cada cuarto con el siguiente: la de ese cuarto más cercana al siguiente
        for (int i = 0; i + 1 < lista.Count; i++)
        {
            float masCerca = float.MaxValue;
            foreach (Door puerta in lista[i].raiz.GetComponentsInChildren<Door>(true))
            {
                float distancia = lista[i + 1].zona.SqrDistance(puerta.transform.position);
                if (distancia >= masCerca) continue;
                masCerca = distancia;
                lista[i].puertaSiguiente = puerta;
            }
            // Lo que nunca se apaga: la puerta entera (hoja, marco, cerradura), no solo la bisagra
            Door union = lista[i].puertaSiguiente;
            if (union != null)
                puertasDeUnion.Add(union.transform.parent != lista[i].raiz ? union.transform.parent : union.transform);
        }
    }

    // El lugar que ocupa un cuarto: la caja que envuelve todo lo que se ve y todo lo que choca
    static Bounds Zona(Transform raiz)
    {
        bool hay = false;
        var zona = new Bounds(raiz.position, Vector3.zero);
        foreach (Renderer r in raiz.GetComponentsInChildren<Renderer>())
        {
            if (!r.enabled) continue;
            if (!hay) { zona = r.bounds; hay = true; }
            else zona.Encapsulate(r.bounds);
        }
        foreach (Collider c in raiz.GetComponentsInChildren<Collider>())
            if (!c.isTrigger) zona.Encapsulate(c.bounds);
        return zona;
    }

    // En qué cuarto está el jugador: el del piso (o mueble) que tiene justo debajo de la cabeza.
    // No cuenta lo que se mueve (objetos con Rigidbody). Si no encuentra nada, sigue en el mismo.
    int CuartoDelJugador()
    {
        Camera camara = Camera.main;
        if (camara == null) return actual;

        int cantidad = Physics.RaycastNonAlloc(camara.transform.position, Vector3.down, golpes, 4f, ~0, QueryTriggerInteraction.Ignore);
        int encontrado = actual;
        float masCerca = float.MaxValue;
        for (int i = 0; i < cantidad; i++)
        {
            if (golpes[i].distance >= masCerca || golpes[i].collider.attachedRigidbody != null) continue;
            int indice = IndiceDe(golpes[i].collider.transform);
            if (indice < 0) continue;
            masCerca = golpes[i].distance;
            encontrado = indice;
        }
        return encontrado;
    }

    int IndiceDe(Transform t)
    {
        for (int i = 0; i < lista.Count; i++)
            if (t.IsChildOf(lista[i].raiz)) return i;
        return -1;
    }

    bool DebeVerse(int i)
    {
        if (actual < 0) return true;                  // todavía no se sabe dónde está: se ve todo
        if (Mathf.Abs(i - actual) <= 1) return true;  // su cuarto y los de al lado

        // Más lejos: solo si todas las puertas entre el jugador y ese cuarto están abiertas
        int paso = i > actual ? 1 : -1;
        for (int k = actual; k != i; k += paso)
        {
            Door puerta = lista[Mathf.Min(k, k + paso)].puertaSiguiente;
            if (puerta == null || puerta.CerradaDelTodo) return false;
        }
        return true;
    }

    void Mostrar(Cuarto cuarto, bool ver)
    {
        cuarto.visible = ver;

        // Lo que se dibuja. forceRenderingOff es solo para esto: ningún otro script lo usa, así
        // que prenderlo y apagarlo no se mezcla con lo que hacen los acertijos.
        foreach (Renderer r in cuarto.raiz.GetComponentsInChildren<Renderer>(true))
        {
            if (ver) r.forceRenderingOff = false;
            else if (SigueEnSuCuarto(cuarto, r.transform)) r.forceRenderingOff = true;
        }

        // Las luces: se apagan las que estaban prendidas y, al volver, se prenden esas mismas
        if (ver)
        {
            foreach (Light luz in cuarto.lucesApagadas)
                if (luz != null) luz.enabled = true;
            cuarto.lucesApagadas.Clear();
        }
        else
        {
            foreach (Light luz in cuarto.raiz.GetComponentsInChildren<Light>())
            {
                if (!luz.enabled || !SigueEnSuCuarto(cuarto, luz.transform)) continue;
                luz.enabled = false;
                cuarto.lucesApagadas.Add(luz);
            }
        }
    }

    // true si el objeto está dentro de su cuarto y no es parte de una puerta entre cuartos
    bool SigueEnSuCuarto(Cuarto cuarto, Transform t)
    {
        if (!cuarto.zona.Contains(t.position)) return false;
        foreach (Transform puerta in puertasDeUnion)
            if (t.IsChildOf(puerta)) return false;
        return true;
    }
}
