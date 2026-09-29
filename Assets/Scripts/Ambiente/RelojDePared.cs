using System;
using UnityEngine;

// Reloj de pared que marca la hora real (la del computador o del Quest).
// Gira las tres agujas del modelo alrededor del centro del reloj, sobre su eje Z.
//
// En la escena: va en el reloj. Se arrastran sus tres agujas y se anota hacia dónde apunta cada una
// en el modelo original (en grados desde las 12, en sentido horario), porque el modelo ya viene
// marcando una hora. Cuarto1Builder lo configura solo.
public class RelojDePared : MonoBehaviour
{
    public Transform horario;
    public Transform minutero;
    public Transform segundero;

    [Tooltip("Ángulo al que apunta cada aguja en el modelo original (grados desde las 12, sentido horario)")]
    public float anguloHorario;
    public float anguloMinutero;
    public float anguloSegundero;

    Quaternion rotacionHorario, rotacionMinutero, rotacionSegundero;

    void Awake()
    {
        // Rotación original de cada aguja: el giro se suma sobre esta
        rotacionHorario = horario.localRotation;
        rotacionMinutero = minutero.localRotation;
        rotacionSegundero = segundero.localRotation;
    }

    void Update()
    {
        DateTime ahora = DateTime.Now;
        float segundos = ahora.Second;                    // salta de a un segundo, como un reloj real
        float minutos = ahora.Minute + segundos / 60f;
        float horas = ahora.Hour % 12 + minutos / 60f;

        // Una vuelta = 360°: la hora avanza 30° por hora y el minutero y el segundero 6° por unidad.
        // Visto de frente, girar en +Z es girar en sentido horario.
        horario.localRotation = rotacionHorario * Quaternion.Euler(0, 0, horas * 30f - anguloHorario);
        minutero.localRotation = rotacionMinutero * Quaternion.Euler(0, 0, minutos * 6f - anguloMinutero);
        segundero.localRotation = rotacionSegundero * Quaternion.Euler(0, 0, segundos * 6f - anguloSegundero);
    }
}
