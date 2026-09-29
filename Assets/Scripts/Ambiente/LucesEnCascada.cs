using System.Collections;
using UnityEngine;
using UnityEngine.Events;

// Prende una fila de luces de a una, como una cascada, y al terminar suena el golpe del
// pestillo y avisa ("alTerminar"). En el Cuarto 3 marca en el piso el camino de la consola
// a la puerta cuando se da el acceso, y al final se abre la puerta (GDD: "las luces se
// encienden en cascada y el pestillo cede con un golpe metálico").
//
// En la escena: en "luces" van las piezas en orden (empiezan apagadas). ConstructorCuarto3
// lo arma solo.
public class LucesEnCascada : MonoBehaviour
{
    [Tooltip("Las luces en el orden en que se prenden (empiezan apagadas)")]
    public GameObject[] luces;

    [Tooltip("Segundos entre una luz y la siguiente")]
    public float intervalo = 0.18f;

    [Tooltip("Qué pasa al terminar (por ejemplo, abrir la puerta)")]
    public UnityEvent alTerminar = new UnityEvent();

    public void Encender() => StartCoroutine(Cascada());

    IEnumerator Cascada()
    {
        foreach (GameObject luz in luces)
        {
            if (luz == null) continue;
            luz.SetActive(true);
            SonidoSintetico.Tocar(SonidoSintetico.Pitido(900f, 0.06f), luz.transform.position, 0.4f);
            yield return new WaitForSeconds(intervalo);
        }

        Vector3 ultimo = luces.Length > 0 && luces[luces.Length - 1] != null ? luces[luces.Length - 1].transform.position : transform.position;
        SonidoSintetico.Tocar(SonidoSintetico.Golpe(), ultimo);
        alTerminar.Invoke();
    }
}
