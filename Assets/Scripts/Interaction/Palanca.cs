using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

// Palanca del laboratorio. Al tocarla baja y le avisa al acertijo de la secuencia qué
// número se accionó.
//
// Si era la que tocaba, la palanca SE QUEDA ABAJO y se le prende la luz verde, así el
// jugador ve de una que esa ya está hecha y no la vuelve a tocar. Si se equivocó, todas
// vuelven a subir y se apagan, y hay que empezar la secuencia de nuevo.
//
// En la escena: va en el brazo de la palanca, junto a un XR Simple Interactable. En
// "acertijo" va el objeto con el AcertijoSecuencia y en "numero" cuál palanca es.
[RequireComponent(typeof(XRSimpleInteractable))]
public class Palanca : MonoBehaviour
{
    [Tooltip("Qué palanca es: 1, 2 o 3")]
    public int numero = 1;

    [Tooltip("La parte que baja al accionarla")]
    public Transform brazo;

    [Tooltip("Cuántos grados baja")]
    public float anguloBajada = 55f;

    [Tooltip("Cuánto tarda en bajar o subir, en segundos")]
    public float duracion = 0.25f;

    [Tooltip("Luz verde de esta palanca: se prende cuando quedó bien puesta")]
    public GameObject luzOk;

    [Tooltip("El acertijo que lleva la cuenta de la secuencia")]
    public AcertijoSecuencia acertijo;

    [Tooltip("Sonido del golpe de la palanca")]
    public AudioSource sonido;

    public bool Abajo { get; private set; }

    XRSimpleInteractable interactable;
    Quaternion arriba;
    bool moviendose;

    void Awake()
    {
        interactable = GetComponent<XRSimpleInteractable>();
        if (brazo != null) arriba = brazo.localRotation;
        if (luzOk != null) luzOk.SetActive(false);
    }

    void OnEnable() => interactable.selectEntered.AddListener(Accionar);
    void OnDisable() => interactable.selectEntered.RemoveListener(Accionar);

    void Accionar(SelectEnterEventArgs args)
    {
        // Ya está hecha: no se vuelve a tocar
        if (Abajo || moviendose) return;

        if (sonido != null) sonido.Play();

        // El acertijo dice si era la que tocaba
        bool acierto = acertijo != null && acertijo.Marcar(numero);

        if (acierto)
        {
            Abajo = true;
            if (luzOk != null) luzOk.SetActive(true);
            StartCoroutine(Girar(Abajo));
            return;
        }

        // Si se equivocó, esta baja y sube sola; las demás las reinicia el acertijo
        StartCoroutine(Rebotar());
    }

    // Lo llama el acertijo cuando alguien se equivoca: todas vuelven arriba
    public void Reiniciar()
    {
        if (!Abajo) return;
        Abajo = false;
        if (luzOk != null) luzOk.SetActive(false);
        StartCoroutine(Girar(false));
    }

    IEnumerator Rebotar()
    {
        yield return Girar(true);
        yield return new WaitForSeconds(0.2f);
        yield return Girar(false);
    }

    IEnumerator Girar(bool baja)
    {
        if (brazo == null) yield break;

        moviendose = true;
        Quaternion desde = brazo.localRotation;
        Quaternion hasta = baja ? arriba * Quaternion.Euler(anguloBajada, 0f, 0f) : arriba;

        float t = 0f;
        while (t < duracion)
        {
            t += Time.deltaTime;
            brazo.localRotation = Quaternion.Slerp(desde, hasta, t / duracion);
            yield return null;
        }
        brazo.localRotation = hasta;
        moviendose = false;
    }
}
