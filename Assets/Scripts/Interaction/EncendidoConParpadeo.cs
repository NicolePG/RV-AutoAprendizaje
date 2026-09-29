using System.Collections;
using UnityEngine;

// Al encenderse, la luz parpadea unas veces antes de quedar fija, como un tubo o una lámpara vieja
// cuando vuelve la energía. La bombilla visible parpadea junto con la luz.
//
// En la escena: va en el objeto de la luz (que empieza apagado). Se activa solo cuando algo prende
// ese objeto, por ejemplo la palanca del tablero. Cuarto1Builder lo agrega solo.
[RequireComponent(typeof(Light))]
public class EncendidoConParpadeo : MonoBehaviour
{
    [Tooltip("Objeto que brilla junto con la luz (la bombilla de la lámpara)")]
    public GameObject bombilla;

    [Tooltip("Segundos de cada tramo: prendida, apagada, prendida, apagada... Al final queda prendida")]
    public float[] tramos = { 0.06f, 0.1f, 0.05f, 0.2f, 0.08f, 0.06f };

    Light luz;

    void Awake()
    {
        luz = GetComponent<Light>();
    }

    // OnEnable se ejecuta cada vez que se activa el objeto de la luz
    void OnEnable()
    {
        StartCoroutine(Parpadear());
    }

    IEnumerator Parpadear()
    {
        bool prendida = true;
        foreach (float duracion in tramos)
        {
            Aplicar(prendida);
            yield return new WaitForSeconds(duracion);
            prendida = !prendida;
        }
        Aplicar(true);
    }

    void Aplicar(bool prendida)
    {
        luz.enabled = prendida;
        if (bombilla != null) bombilla.SetActive(prendida);
    }
}
