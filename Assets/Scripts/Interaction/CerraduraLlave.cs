using System.Collections;
using UnityEngine;
using UnityEngine.Events;

// Cerradura de la puerta de salida. Para que abra tienen que pasar dos cosas, en
// cualquier orden:
// 1) el teclado acepta el código (alguien llama a Habilitar()),
// 2) la llave queda encajada en la ranura (LlaveAutomatica llama a PonerLlave()).
//
// Cuando se cumplen las dos, el jugador gira la llave: tocando la propia llave o
// apretando la perilla. Las dos cosas llaman a Girar(), y ahí la puerta se abre.
//
// En la escena: va en la cerradura de la puerta, con "ranura" apuntando al objeto
// vacío donde entra la llave (ese objeto es el que gira, y la llave cuelga de él).
public class CerraduraLlave : MonoBehaviour
{
    [Tooltip("El punto donde se encaja la llave: al abrir, gira y la llave gira con él")]
    public Transform ranura;

    [Tooltip("Luz que avisa que el código ya fue aceptado y falta la llave")]
    public Light luzLista;

    [Tooltip("Sonido del pestillo al girar (opcional)")]
    public AudioClip sonidoGiro;

    [Tooltip("Qué pasa cuando la cerradura cede")]
    public UnityEvent alAbrir = new UnityEvent();

    public bool CodigoAceptado { get; private set; }
    public bool LlavePuesta { get; private set; }

    bool abierta;

    void Awake()
    {
        if (luzLista != null) luzLista.enabled = false;
    }

    // Lo llama el teclado cuando el código es correcto
    public void Habilitar()
    {
        if (CodigoAceptado) return;
        CodigoAceptado = true;
        if (luzLista != null) luzLista.enabled = true;

    }

    // Lo llama la llave cuando termina de encajarse en la ranura
    public void PonerLlave()
    {
        if (LlavePuesta) return;
        LlavePuesta = true;

        // No gira sola: el jugador tiene que tocar la llave o la perilla para girarla
    }

    // Lo llama la perilla al presionarla
    public void Girar()
    {
        if (abierta || !CodigoAceptado || !LlavePuesta) return;

        abierta = true;
        if (sonidoGiro != null) AudioSource.PlayClipAtPoint(sonidoGiro, transform.position);
        StartCoroutine(GirarLlave());
    }

    // La llave está colgada de la ranura, así que girando la ranura gira la llave
    IEnumerator GirarLlave()
    {
        if (ranura == null)
        {
            alAbrir.Invoke();
            yield break;
        }

        Quaternion inicial = ranura.localRotation;
        Quaternion final = inicial * Quaternion.Euler(0f, 0f, -90f);

        float t = 0f;
        while (t < 0.4f)
        {
            t += Time.deltaTime;
            ranura.localRotation = Quaternion.Slerp(inicial, final, t / 0.4f);
            yield return null;
        }
        ranura.localRotation = final;

        alAbrir.Invoke();
    }
}
