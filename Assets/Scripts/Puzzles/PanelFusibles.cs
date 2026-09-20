using UnityEngine;
using UnityEngine.Events;

// Panel eléctrico del Cuarto 4 (Laboratorio), primer paso del cuarto.
//
// Tiene tres huecos (A, B y C). En la mesa de trabajo hay fusibles y solo tres sirven:
// cada hueco acepta un amperaje distinto, que sale de la hoja de los circuitos. Cuando
// los tres están bien puestos vuelve la corriente y se dispara "alCompletar".
//
// El jugador toca un fusible para llevarlo en la mano y después toca el hueco donde lo
// quiere poner (ver ObjetoLlevable y Encaje). Si se equivocó, toca el hueco con la mano
// vacía y el fusible vuelve a la mano.
//
// En la escena: va en el objeto "Panel_Electrico". En "encajes" van los tres Encaje en
// el orden A, B, C, y en "amperajesCorrectos" el amperaje que espera cada uno.
public class PanelFusibles : MonoBehaviour
{
    [Tooltip("Los tres huecos del panel, en el orden A, B, C")]
    public Encaje[] encajes;

    [Tooltip("El amperaje que espera cada hueco, en el mismo orden")]
    public string[] amperajesCorrectos;

    [Tooltip("La luz verde de cada hueco: se prende cuando el fusible es el correcto")]
    public GameObject[] lucesOk;

    [Tooltip("La luz roja de cada hueco: se prende cuando hay un fusible pero no es el que va")]
    public GameObject[] lucesMal;

    [Tooltip("Sonido corto cada vez que se acierta un hueco")]
    public AudioSource audioAcierto;

    [Tooltip("Qué pasa cuando los tres fusibles están bien puestos")]
    public UnityEvent alCompletar = new UnityEvent();

    bool resuelto;
    int aciertosAnteriores;

    void OnEnable()
    {
        if (encajes == null) return;
        foreach (var encaje in encajes)
            if (encaje != null) encaje.alCambiar.AddListener(Revisar);

        Revisar();
    }

    void OnDisable()
    {
        if (encajes == null) return;
        foreach (var encaje in encajes)
            if (encaje != null) encaje.alCambiar.RemoveListener(Revisar);
    }

    void Revisar()
    {
        if (resuelto) return;

        if (encajes == null) return;

        int aciertos = 0;
        for (int i = 0; i < encajes.Length; i++)
        {
            bool ocupado = encajes[i] != null && encajes[i].Contenido != null;
            bool bien = EsCorrecto(i);
            if (bien) aciertos++;

            // Verde: el fusible que va. Rojo: hay uno puesto pero no es ese.
            if (lucesOk != null && i < lucesOk.Length && lucesOk[i] != null) lucesOk[i].SetActive(bien);
            if (lucesMal != null && i < lucesMal.Length && lucesMal[i] != null) lucesMal[i].SetActive(ocupado && !bien);
        }

        // Un sonidito cada vez que se acierta uno más, para que se note el avance
        if (aciertos > aciertosAnteriores && audioAcierto != null) audioAcierto.Play();
        aciertosAnteriores = aciertos;

        if (aciertos < encajes.Length) return;

        resuelto = true;
        alCompletar.Invoke();
    }

    // ¿El fusible que está puesto en el hueco "i" es el del amperaje que pide?
    bool EsCorrecto(int i)
    {
        var encaje = encajes[i];
        if (encaje == null || encaje.Contenido == null) return false;
        if (amperajesCorrectos == null || i >= amperajesCorrectos.Length) return false;

        var fusible = encaje.Contenido.GetComponentInParent<Fusible>();
        return fusible != null && fusible.amperaje == amperajesCorrectos[i];
    }
}
