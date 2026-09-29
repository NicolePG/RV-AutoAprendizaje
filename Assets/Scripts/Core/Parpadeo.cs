using UnityEngine;

// Hace parpadear una luz, como un tubo a punto de quemarse.
// Se usa mientras el cuarto está sin energía, para el clima de terror.
// ControlEnergia lo desactiva cuando vuelve la corriente.
[RequireComponent(typeof(Light))]
public class Parpadeo : MonoBehaviour
{
    public float intensidadMinima = 0.15f;
    public float intensidadMaxima = 1.8f;

    [Tooltip("Qué tan rápido cambia entre apagada y encendida")]
    public float velocidad = 14f;

    Light luz;
    float objetivo;
    float proximoCambio;

    void Awake()
    {
        luz = GetComponent<Light>();
        objetivo = intensidadMaxima;
    }

    void Update()
    {
        if (Time.time >= proximoCambio)
        {
            // La mayor parte del tiempo está encendida: el parpadeo es el sobresalto
            objetivo = Random.value < 0.25f ? intensidadMinima : intensidadMaxima;
            proximoCambio = Time.time + Random.Range(0.04f, 0.6f);
        }

        luz.intensity = Mathf.Lerp(luz.intensity, objetivo, Time.deltaTime * velocidad);
    }
}
