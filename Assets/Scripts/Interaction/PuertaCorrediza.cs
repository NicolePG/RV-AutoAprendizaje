using System.Collections;
using UnityEngine;

// Puerta corrediza automática de vidrio, como la entrada de un edificio moderno: cuando se llama
// a Abrir(), las dos hojas se corren hacia los costados (arrancan y frenan suave) con el siseo
// del motor. Se usa en la puerta principal del colegio, al final del pasillo de salida: se abre
// sola cuando termina la cascada de luces del pasillo.
//
// En la escena: va en el objeto de la puerta. "hojaIzquierda" se corre hacia el -X de ese objeto y
// "hojaDerecha" hacia el +X. Las hojas empiezan cerradas. ConstructorCuarto4 la arma sola.
public class PuertaCorrediza : MonoBehaviour
{
    [Tooltip("La hoja que se corre hacia el -X de la puerta")]
    public Transform hojaIzquierda;

    [Tooltip("La hoja que se corre hacia el +X de la puerta")]
    public Transform hojaDerecha;

    [Tooltip("Cuántos metros se corre cada hoja")]
    public float recorrido = 0.9f;

    [Tooltip("Segundos que tarda en abrirse")]
    public float duracion = 1.8f;

    public bool Abierta { get; private set; }

    public void Abrir()
    {
        if (Abierta) return;
        Abierta = true;
        StartCoroutine(Animar());
    }

    IEnumerator Animar()
    {
        // El motor: un siseo grave y corto
        SonidoSintetico.Tocar(SonidoSintetico.Ruido(0.9f, duracion), transform.position, 0.5f);

        Vector3 izquierda = hojaIzquierda.localPosition;
        Vector3 derecha = hojaDerecha.localPosition;
        for (float t = 0f; t < 1f; t += Time.deltaTime / duracion)
        {
            float d = Mathf.SmoothStep(0f, recorrido, t);
            hojaIzquierda.localPosition = izquierda + Vector3.left * d;
            hojaDerecha.localPosition = derecha + Vector3.right * d;
            yield return null;
        }
        hojaIzquierda.localPosition = izquierda + Vector3.left * recorrido;
        hojaDerecha.localPosition = derecha + Vector3.right * recorrido;
    }
}
