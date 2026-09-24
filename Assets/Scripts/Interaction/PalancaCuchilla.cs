using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

// Una palanca de cuchilla (interruptor antiguo) del cierre de la salida del Cuarto 4, acertijo 4.
//
// Se agarra la manija y se BAJA con la mano: bajar la mano baja la palanca (media vuelta,
// de arriba hacia abajo, pasando por delante). Al llegar abajo queda trabada y le avisa a la
// secuencia (SecuenciaPalancas) qué palanca se bajó. Si se suelta antes, un resorte la
// devuelve arriba. Si la secuencia dice que no era la que tocaba, Reiniciar() la sube.
//
// El modelo (Sketchfab) mueve la manija con un hueso: este script gira ese hueso sobre su
// eje X, igual que la animación que trae el modelo.
//
// En la escena: va en el hueso de la manija, junto a un XRSimpleInteractable y un collider.
// ConstructorCuarto4 lo arma.
[RequireComponent(typeof(XRSimpleInteractable))]
public class PalancaCuchilla : MonoBehaviour
{
    [Tooltip("Qué palanca es (1, 2 o 3)")]
    public int numero = 1;

    [Tooltip("Cuántos grados gira de arriba hasta abajo")]
    public float anguloBajada = 180f;

    [Tooltip("Cuántos grados gira por cada metro que baja la mano")]
    public float gradosPorMetro = 450f;

    [Tooltip("Grados por segundo con los que vuelve arriba si la sueltan")]
    public float velocidadResorte = 360f;

    public SecuenciaPalancas secuencia;

    // Bloqueada no se mueve (hasta que la secuencia tiene energía)
    public bool bloqueada = true;

    public bool Abajo { get; private set; }

    XRSimpleInteractable interactable;
    Quaternion rotacionArriba;
    float angulo;
    float anguloAlAgarrar;
    float alturaManoAlAgarrar;
    bool volviendo;

    void Awake()
    {
        interactable = GetComponent<XRSimpleInteractable>();
        rotacionArriba = transform.localRotation;
    }

    void OnEnable() => interactable.selectEntered.AddListener(AlAgarrar);
    void OnDisable() => interactable.selectEntered.RemoveListener(AlAgarrar);

    void AlAgarrar(SelectEnterEventArgs args)
    {
        anguloAlAgarrar = angulo;
        alturaManoAlAgarrar = args.interactorObject.GetAttachTransform(interactable).position.y;
        // Trabada por falta de energía: suena seca y no se mueve
        if (bloqueada) SonidoSintetico.Tocar(SonidoSintetico.Pitido(220f, 0.05f), transform.position, 0.6f);
    }

    void Update()
    {
        if (Abajo || volviendo) return;

        if (!interactable.isSelected || bloqueada)
        {
            // El resorte la devuelve arriba
            if (angulo > 0f) Girar(Mathf.MoveTowards(angulo, 0f, velocidadResorte * Time.deltaTime));
            return;
        }

        // Cuánto bajó la mano desde que la agarró
        float alturaMano = interactable.interactorsSelecting[0].GetAttachTransform(interactable).position.y;
        Girar(Mathf.Clamp(anguloAlAgarrar + (alturaManoAlAgarrar - alturaMano) * gradosPorMetro, 0f, anguloBajada));

        if (angulo >= anguloBajada - 6f) Bajar();
    }

    void Bajar()
    {
        Abajo = true;
        Girar(anguloBajada);
        SonidoSintetico.Tocar(SonidoSintetico.Golpe(), transform.position, 0.8f);
        if (secuencia != null) secuencia.Marcar(this);
    }

    // La sube de nuevo (se equivocó de orden)
    public void Reiniciar()
    {
        if (!Abajo || volviendo) return;
        StartCoroutine(Subir());
    }

    IEnumerator Subir()
    {
        volviendo = true;
        while (angulo > 0f)
        {
            Girar(Mathf.MoveTowards(angulo, 0f, velocidadResorte * Time.deltaTime));
            yield return null;
        }
        Abajo = false;
        volviendo = false;

        // Si el jugador la sigue agarrando, se cuenta desde acá: si no, como la mano quedó
        // abajo, la palanca se volvería a bajar sola
        if (interactable.isSelected)
        {
            anguloAlAgarrar = 0f;
            alturaManoAlAgarrar = interactable.interactorsSelecting[0].GetAttachTransform(interactable).position.y;
        }
    }

    void Girar(float grados)
    {
        angulo = grados;
        transform.localRotation = rotacionArriba * Quaternion.AngleAxis(angulo, Vector3.right);
    }
}
