using System.Collections;
using UnityEngine;

// El susto del laboratorio, el más fuerte de los tres (GDD, sección 6).
//
// Salta mientras el jugador gira la segunda llave de gas: tiene la mano ocupada y la mesa de
// disección, con un cuerpo tapado por una sábana, le queda a la espalda. En dos segundos:
//  1. Un golpe seco y se corta la luz del laboratorio.
//  2. A oscuras suenan las ruedas: la sábana cae al piso y la camilla se corre y gira sola.
//  3. La luz vuelve parpadeando... y la mesa está VACÍA. El cuerpo ya no está.
// No hay ningún muñeco que se mueva (se veía falso): el miedo lo da lo que no se ve.
//
// Es la versión segura del GDD: nada va hacia el jugador ni lo toca. La camilla rueda hacia el
// fondo, sin acercarse a la pared donde está él. Pasa una sola vez por partida.
//
// En la escena: va en el objeto "Camilla". ConstructorCuarto4 lo arma y lo conecta a la llave V2.
public class SustoCamilla : MonoBehaviour
{
    [Tooltip("Las luces del cuarto que se cortan durante el apagón")]
    public Light[] luces;

    [Tooltip("Lo que se ve encendido y se apaga con las luces (los tubos de las lámparas)")]
    public GameObject[] encendidos;

    [Tooltip("La sábana con la forma del cuerpo")]
    public GameObject sabana;

    [Tooltip("La sábana tirada en el piso: arranca apagada")]
    public GameObject sabanaCaida;

    [Tooltip("La parte que rueda: la mesa con la sábana")]
    public Transform camilla;

    [Tooltip("Cuánto se corre la camilla (en los ejes del cuarto) y cuánto gira")]
    public Vector3 corrimiento = new Vector3(0f, 0f, 0.6f);
    public float giro = 10f;

    [Tooltip("Chirrido de las ruedas y golpe (si no hay audio, se arma uno por código)")]
    public AudioSource ruido;

    public bool Disparado { get; private set; }

    void Awake()
    {
        if (sabanaCaida != null) sabanaCaida.SetActive(false);
    }

    // Lo llama la segunda llave de gas cuando va por la mitad del giro
    public void Disparar()
    {
        if (Disparado) return;
        Disparado = true;
        StartCoroutine(Asustar());
    }

    IEnumerator Asustar()
    {
        // 1. Golpe y apagón (también la luz de ambiente, que es la que ilumina parejo el cuarto)
        Color ambiente = RenderSettings.ambientLight;
        float reflejos = RenderSettings.reflectionIntensity;
        bool[] prendidas = new bool[luces.Length];
        for (int i = 0; i < luces.Length; i++) prendidas[i] = luces[i] != null && luces[i].enabled;

        SonidoSintetico.Tocar(SonidoSintetico.Golpe(), transform.position, 1f);
        Luz(false, prendidas, ambiente, reflejos);
        yield return new WaitForSeconds(0.35f);

        // 2. A oscuras: se cae la sábana y la camilla rueda sola, con chirrido
        if (ruido != null)
        {
            if (ruido.clip == null) ruido.clip = SonidoSintetico.Chirrido(0.9f);
            ruido.Play();
        }
        if (sabana != null) sabana.SetActive(false);
        if (sabanaCaida != null) sabanaCaida.SetActive(true);
        if (camilla != null)
        {
            Vector3 desde = camilla.localPosition;
            Quaternion giroDesde = camilla.localRotation;
            Vector3 hasta = desde + corrimiento;
            Quaternion giroHasta = giroDesde * Quaternion.Euler(0f, giro, 0f);
            for (float t = 0f; t < 1f; t += Time.deltaTime / 0.8f)
            {
                float s = t * t * (3f - 2f * t);
                camilla.localPosition = Vector3.Lerp(desde, hasta, s);
                camilla.localRotation = Quaternion.Slerp(giroDesde, giroHasta, s);
                yield return null;
            }
            camilla.localPosition = hasta;
            camilla.localRotation = giroHasta;
        }
        yield return new WaitForSeconds(0.6f);

        // 3. La luz vuelve parpadeando, como un tubo que arranca
        float[] esperas = { 0.06f, 0.14f, 0.05f, 0.25f, 0.08f };
        for (int i = 0; i < esperas.Length; i++)
        {
            Luz(i % 2 == 0, prendidas, ambiente, reflejos);
            yield return new WaitForSeconds(esperas[i]);
        }
        Luz(true, prendidas, ambiente, reflejos);
    }

    // Prende o apaga las luces que estaban prendidas antes del susto, los tubos y el ambiente
    void Luz(bool prender, bool[] prendidas, Color ambiente, float reflejos)
    {
        for (int i = 0; i < luces.Length; i++)
            if (luces[i] != null && prendidas[i]) luces[i].enabled = prender;
        foreach (GameObject objeto in encendidos)
            if (objeto != null) objeto.SetActive(prender);
        RenderSettings.ambientLight = prender ? ambiente : ambiente * 0.04f;
        RenderSettings.reflectionIntensity = prender ? reflejos : 0.05f;
    }
}
