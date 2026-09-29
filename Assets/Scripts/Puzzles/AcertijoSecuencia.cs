using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

// Tercer paso del laboratorio: la secuencia de las palancas.
//
// Mientras no hay gas no se ve nada. Apenas el gas llena el cuarto de niebla, los haces
// de luz que bajan del techo se hacen visibles y empiezan a encenderse de a uno, en un
// orden fijo. Ese es el orden en el que hay que accionar las palancas.
//
// La secuencia se repite sola cada tanto, así que el jugador puede mirarla de nuevo si
// se le mezcló. Una palanca fuera de orden enciende la luz roja, suena grave y todas
// vuelven arriba para empezar de nuevo.
//
// OJO con el orden: cada palanca tiene que aparecer UNA SOLA VEZ. Una palanca que ya
// quedó abajo no se puede volver a tocar (así es como el jugador sabe que esa ya está),
// así que si un número se repitiera, el acertijo no se podría terminar nunca.
//
// En la escena: va en un objeto vacío llamado "Acertijo_Palancas". En "haces" van los
// tres haces de luz en el orden de las palancas 1, 2 y 3, y cada Palanca tiene que
// apuntar acá y saber su número.
public class AcertijoSecuencia : MonoBehaviour
{
    [Tooltip("El orden correcto, con los números de las palancas")]
    public int[] secuencia = { 2, 3, 1 };

    [Tooltip("Los haces de luz de cada palanca, en orden: el 1, el 2 y el 3")]
    public GameObject[] haces;

    [Tooltip("Las tres palancas: hacen falta para volver a subirlas todas si se falla")]
    public Palanca[] palancas;

    [Tooltip("Cuánto queda encendido cada haz, en segundos")]
    public float segundosEncendido = 0.7f;

    [Tooltip("Cuánto pasa entre un haz y el siguiente")]
    public float segundosEntreHaces = 0.35f;

    [Tooltip("Cuánto espera antes de repetir toda la secuencia")]
    public float segundosEntreRepeticiones = 4f;

    [Header("Avisos")]
    [Tooltip("Cartel que va diciendo cuántas palancas van bien")]
    public TMP_Text cartel;
    public GameObject luzOk;
    public GameObject luzError;
    public AudioSource sonidoOk;
    public AudioSource sonidoError;

    [Tooltip("Qué pasa cuando la secuencia se completa bien")]
    public UnityEvent OnSolved = new UnityEvent();

    public bool Activo { get; private set; }
    public bool Resuelto { get; private set; }

    int paso;

    void Start() => Avisar();

    // Lo llama el sistema de gas cuando la niebla deja ver los haces
    public void Activar()
    {
        if (Activo || Resuelto) return;
        Activo = true;
        Avisar();
        StartCoroutine(MostrarSecuencia());
    }

    // Lo llama cada palanca al accionarse. Devuelve true si era la que tocaba: con eso
    // la palanca sabe si quedarse abajo y prender su luz verde, o volver a subir.
    public bool Marcar(int numero)
    {
        if (Resuelto) return false;

        // Sin gas todavía no se ve el orden: accionar palancas no sirve de nada
        if (!Activo) { Fallo(); return false; }

        if (secuencia == null || secuencia.Length == 0) return false;
        if (numero != secuencia[paso]) { Fallo(); return false; }

        paso++;
        Avisar();
        if (paso < secuencia.Length)
        {
            if (sonidoOk != null) sonidoOk.Play();
            return true;
        }

        Resuelto = true;
        Activo = false;
        StopAllCoroutines();
        ApagarHaces();

        if (luzError != null) luzError.SetActive(false);
        if (luzOk != null) luzOk.SetActive(true);
        if (sonidoOk != null) sonidoOk.Play();
        Avisar();
        OnSolved.Invoke();
        return true;
    }

    void Fallo()
    {
        paso = 0;
        Avisar();
        if (sonidoError != null) sonidoError.Play();
        if (luzError != null) StartCoroutine(Parpadear(luzError, 0.8f));

        // Todas las palancas vuelven arriba y se les apaga la luz: la secuencia
        // empieza de cero y se ve que empieza de cero
        if (palancas == null) return;
        foreach (var palanca in palancas)
            if (palanca != null) palanca.Reiniciar();
    }

    // El cartel de las palancas va contando cuántas van bien. Sirve para que el jugador
    // sepa que la última que tocó contó, y para darse cuenta enseguida si algo no anda.
    void Avisar()
    {
        if (cartel == null) return;

        int total = secuencia != null ? secuencia.Length : 0;
        if (Resuelto) cartel.text = "Secuencia completa";
        else if (!Activo) cartel.text = "Todavia no hay gas: las luces no se ven";
        else cartel.text = "Van " + paso + " de " + total + " - mira el orden de las luces";
    }

    IEnumerator Parpadear(GameObject luz, float segundos)
    {
        luz.SetActive(true);
        yield return new WaitForSeconds(segundos);
        luz.SetActive(false);
    }

    IEnumerator MostrarSecuencia()
    {
        while (Activo)
        {
            foreach (int numero in secuencia)
            {
                var haz = Haz(numero);
                if (haz != null) haz.SetActive(true);
                yield return new WaitForSeconds(segundosEncendido);
                if (haz != null) haz.SetActive(false);
                yield return new WaitForSeconds(segundosEntreHaces);
            }

            // Entre repetición y repetición quedan los tres prendidos flojito, para que
            // se vea dónde están, y el jugador tenga tiempo de accionar las palancas
            PrenderHaces();
            yield return new WaitForSeconds(segundosEntreRepeticiones);
            ApagarHaces();
            yield return new WaitForSeconds(0.6f);
        }
    }

    GameObject Haz(int numero)
    {
        if (haces == null) return null;
        int i = numero - 1;
        return i >= 0 && i < haces.Length ? haces[i] : null;
    }

    void PrenderHaces()
    {
        if (haces == null) return;
        foreach (var haz in haces) if (haz != null) haz.SetActive(true);
    }

    void ApagarHaces()
    {
        if (haces == null) return;
        foreach (var haz in haces) if (haz != null) haz.SetActive(false);
    }
}
