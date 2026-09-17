using UnityEngine;

// Tornillo que se desatornilla como uno de verdad: hay que apoyar la punta del destornillador y GIRARLO
// (en el Quest girando la muñeca, en el PC moviendo el mouse en círculos). Con cada vuelta del destornillador
// el tornillo gira y sale un poco. Si la punta se aleja antes de terminar, queda a medio salir.
// Mientras la punta lo toca, el tornillo se ilumina en ámbar para avisar que ya se puede girar.
// Al salir del todo: suena, cae al piso con física y avisa a su tapa.
//
// En la escena: va en el tornillo, que necesita un collider marcado como "Is Trigger" (la zona donde
// se detecta la punta). En el Inspector se arrastran el collider de la punta del destornillador y la tapa.
// El eje Y local del tornillo tiene que apuntar hacia afuera (hacia donde sale).
[RequireComponent(typeof(Collider))]
public class Screw : MonoBehaviour
{
    [Tooltip("Collider de la punta del destornillador: solo ese objeto puede quitar el tornillo")]
    public Collider puntaHerramienta;

    [Tooltip("La tapa que sostiene este tornillo")]
    public ScrewedPanel tapa;

    [Tooltip("Sonido al empezar a girar y al salir (opcional)")]
    public AudioClip sonido;

    [Tooltip("Vueltas completas que hay que girar el destornillador para sacar el tornillo")]
    public float vueltasParaSacar = 2f;

    [Tooltip("Cuántos metros sale antes de soltarse")]
    public float recorrido = 0.012f;

    [Tooltip("Radio del collider cuando ya cayó (en escala local): pequeño, del tamaño del tornillo")]
    public float radioAlCaer = 0.5f;

    float progreso;              // 0 = atornillado, 1 = afuera
    bool quitado;
    float ultimoContacto = -1f;  // última vez que la punta tocó el tornillo
    bool resaltado;
    Destornillador herramienta;
    Renderer dibujo;
    MaterialPropertyBlock bloque;
    Vector3 posicionInicial;
    Quaternion rotacionInicial;

    void Awake()
    {
        posicionInicial = transform.localPosition;
        rotacionInicial = transform.localRotation;
        dibujo = GetComponent<Renderer>();
        bloque = new MaterialPropertyBlock();
        if (puntaHerramienta != null) herramienta = puntaHerramienta.GetComponentInParent<Destornillador>();
    }

    // Se llama en cada paso de la física mientras algo está dentro de la zona del tornillo
    void OnTriggerStay(Collider otro)
    {
        if (otro == puntaHerramienta) ultimoContacto = Time.time;
    }

    void Update()
    {
        if (quitado) return;

        bool tocando = Time.time - ultimoContacto < 0.15f;
        Resaltar(tocando);
        if (!tocando || herramienta == null) return;

        float giro = herramienta.GiroEsteCuadro;
        if (giro <= 0f) return;

        if (progreso == 0f && sonido != null) AudioSource.PlayClipAtPoint(sonido, transform.position);
        progreso = Mathf.Min(1f, progreso + giro / (vueltasParaSacar * 360f));
        herramienta.GirarVisual(giro);

        // El tornillo gira lo mismo que el destornillador y avanza hacia afuera por su eje (Y local)
        transform.localRotation = rotacionInicial * Quaternion.Euler(0, -progreso * vueltasParaSacar * 360f, 0);
        transform.localPosition = posicionInicial + rotacionInicial * Vector3.up * (progreso * recorrido);

        if (progreso >= 1f) Soltar();
    }

    // Tono ámbar mientras la punta toca el tornillo: "ya puedes girar"
    void Resaltar(bool encendido)
    {
        if (encendido == resaltado || dibujo == null) return;
        resaltado = encendido;
        if (!encendido)
        {
            dibujo.SetPropertyBlock(null);
            return;
        }
        bloque.Clear();
        bloque.SetColor("_BaseColor", new Color(1f, 0.6f, 0.1f));
        dibujo.SetPropertyBlock(bloque);
    }

    void Soltar()
    {
        quitado = true;
        Resaltar(false);
        if (sonido != null) AudioSource.PlayClipAtPoint(sonido, transform.position);

        // El tornillo se suelta y cae con física. Su zona de detección pasa a ser un collider sólido pequeño.
        transform.SetParent(null, true);
        Collider col = GetComponent<Collider>();
        col.isTrigger = false;
        if (col is SphereCollider esfera) esfera.radius = radioAlCaer;

        Rigidbody rb = gameObject.AddComponent<Rigidbody>();
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic; // es pequeño: evita que atraviese el piso
        rb.AddForce(transform.up * 0.3f, ForceMode.VelocityChange);          // un empujoncito hacia afuera

        if (tapa != null) tapa.QuitarTornillo();
    }
}
