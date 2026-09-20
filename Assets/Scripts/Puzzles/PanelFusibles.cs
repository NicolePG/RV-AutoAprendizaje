using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

// Panel eléctrico del Cuarto 4 (Laboratorio), primer paso del cuarto.
//
// Tiene tres encajes (A, B, C). En la mesa de trabajo hay seis fusibles y solo tres
// sirven: cada encaje acepta un amperaje distinto, escrito al lado del encaje. Cuando
// los tres están bien puestos vuelve la corriente y se dispara "alCompletar".
//
// En la escena: va en el objeto "Panel_Electrico". En "encajes" van los tres
// XR Socket Interactor en el orden A, B, C, y en "amperajesCorrectos" el amperaje que
// espera cada uno, en ese mismo orden. En "lucesOk" va la lucecita de cada encaje.
public class PanelFusibles : MonoBehaviour
{
    [Tooltip("Los tres encajes del panel, en el orden A, B, C")]
    public XRSocketInteractor[] encajes;

    [Tooltip("El amperaje que espera cada encaje, en el mismo orden")]
    public string[] amperajesCorrectos;

    [Tooltip("La luz verde de cada encaje: se prende cuando el fusible es el correcto")]
    public GameObject[] lucesOk;

    [Tooltip("Sonido corto cada vez que se acierta un encaje")]
    public AudioSource audioAcierto;

    [Tooltip("Qué pasa cuando los tres fusibles están bien puestos")]
    public UnityEvent alCompletar = new UnityEvent();

    bool resuelto;
    int aciertosAnteriores;

    void OnEnable()
    {
        foreach (var encaje in encajes)
        {
            if (encaje == null) continue;
            encaje.selectEntered.AddListener(Entro);
            encaje.selectExited.AddListener(Salio);
        }
        Revisar();
    }

    void OnDisable()
    {
        foreach (var encaje in encajes)
        {
            if (encaje == null) continue;
            encaje.selectEntered.RemoveListener(Entro);
            encaje.selectExited.RemoveListener(Salio);
        }
    }

    // Los dos eventos del socket traen datos distintos, así que hace falta un método
    // para cada uno, pero los dos hacen lo mismo: volver a revisar el panel
    void Entro(SelectEnterEventArgs args) => Revisar();
    void Salio(SelectExitEventArgs args) => Revisar();

    void Revisar()
    {
        if (resuelto) return;

        int aciertos = 0;
        for (int i = 0; i < encajes.Length; i++)
        {
            bool bien = EsCorrecto(i);
            if (bien) aciertos++;
            if (i < lucesOk.Length && lucesOk[i] != null) lucesOk[i].SetActive(bien);
        }

        // Un sonidito cada vez que se acierta uno más, para que se note el avance
        if (aciertos > aciertosAnteriores && audioAcierto != null) audioAcierto.Play();
        aciertosAnteriores = aciertos;

        if (aciertos < encajes.Length) return;

        resuelto = true;
        alCompletar.Invoke();
    }

    // ¿El fusible que está puesto en el encaje "i" es el del amperaje que pide?
    bool EsCorrecto(int i)
    {
        var encaje = encajes[i];
        if (encaje == null || !encaje.hasSelection) return false;
        if (i >= amperajesCorrectos.Length) return false;

        var puesto = encaje.interactablesSelected[0] as Component;
        if (puesto == null) return false;

        var fusible = puesto.GetComponentInParent<Fusible>();
        return fusible != null && fusible.amperaje == amperajesCorrectos[i];
    }
}
