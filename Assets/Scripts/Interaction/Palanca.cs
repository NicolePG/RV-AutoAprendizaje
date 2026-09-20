using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

// Palanca del laboratorio. Al tocarla, el brazo baja, vuelve a subir y le avisa al
// acertijo de la secuencia qué número se accionó.
//
// En la escena: va en la palanca, junto a un XR Simple Interactable. En "brazo" va la
// parte que gira, en "acertijo" el objeto que tiene el AcertijoSecuencia y en "numero"
// el número de esta palanca (1, 2 o 3).
[RequireComponent(typeof(XRSimpleInteractable))]
public class Palanca : MonoBehaviour
{
    [Tooltip("Qué palanca es: 1, 2 o 3")]
    public int numero = 1;

    [Tooltip("La parte que baja al accionarla")]
    public Transform brazo;

    [Tooltip("Cuántos grados baja")]
    public float anguloBajada = 55f;

    [Tooltip("Cuánto tarda en bajar y volver, en segundos")]
    public float duracion = 0.25f;

    [Tooltip("El acertijo que lleva la cuenta de la secuencia")]
    public AcertijoSecuencia acertijo;

    [Tooltip("Sonido del golpe de la palanca")]
    public AudioSource sonido;

    XRSimpleInteractable interactable;
    bool moviendose;

    void Awake() => interactable = GetComponent<XRSimpleInteractable>();

    void OnEnable() => interactable.selectEntered.AddListener(Accionar);
    void OnDisable() => interactable.selectEntered.RemoveListener(Accionar);

    void Accionar(SelectEnterEventArgs args)
    {
        if (moviendose) return;
        StartCoroutine(Mover());

        if (sonido != null) sonido.Play();
        if (acertijo != null) acertijo.Marcar(numero);
    }

    IEnumerator Mover()
    {
        moviendose = true;

        Quaternion arriba = brazo != null ? brazo.localRotation : Quaternion.identity;
        Quaternion abajo = arriba * Quaternion.Euler(anguloBajada, 0f, 0f);

        yield return Girar(arriba, abajo);
        yield return new WaitForSeconds(0.15f);
        yield return Girar(abajo, arriba);

        moviendose = false;
    }

    IEnumerator Girar(Quaternion desde, Quaternion hasta)
    {
        if (brazo == null) yield break;

        float t = 0f;
        while (t < duracion)
        {
            t += Time.deltaTime;
            brazo.localRotation = Quaternion.Slerp(desde, hasta, t / duracion);
            yield return null;
        }
        brazo.localRotation = hasta;
    }
}
