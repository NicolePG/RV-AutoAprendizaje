using UnityEngine;

// Tornillo que se quita tocándolo con la punta del destornillador.
// Al quitarse: suena, se cae al piso (se le agrega física) y avisa a su tapa.
//
// En la escena: va en el tornillo, que necesita un collider marcado como "Is Trigger".
// En el Inspector se arrastra el collider de la punta del destornillador y la tapa (ScrewedPanel).
[RequireComponent(typeof(Collider))]
public class Screw : MonoBehaviour
{
    [Tooltip("Collider de la punta del destornillador: solo ese objeto puede quitar el tornillo")]
    public Collider puntaHerramienta;

    [Tooltip("La tapa que sostiene este tornillo")]
    public ScrewedPanel tapa;

    [Tooltip("Sonido al quitarse (opcional)")]
    public AudioClip sonido;

    bool quitado;

    void OnTriggerEnter(Collider otro)
    {
        if (quitado || otro != puntaHerramienta) return;
        quitado = true;

        if (sonido != null) AudioSource.PlayClipAtPoint(sonido, transform.position);

        // El tornillo se suelta de la tapa y cae con física
        transform.SetParent(null, true);
        GetComponent<Collider>().isTrigger = false;
        Rigidbody rb = gameObject.AddComponent<Rigidbody>();
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic; // es pequeño: evita que atraviese el piso

        if (tapa != null) tapa.QuitarTornillo();
    }
}
