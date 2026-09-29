using UnityEngine;

// Un fusible del laboratorio. Lo único que guarda es su amperaje: el panel eléctrico
// lo lee para saber si el fusible que le encajaron es el que iba en ese encaje.
// Sobre la mesa de trabajo hay más fusibles de los que hacen falta y solo tres sirven.
//
// En la escena: va en cada fusible, junto a su XR Grab Interactable.
public class Fusible : MonoBehaviour
{
    [Tooltip("El amperaje escrito en el fusible, por ejemplo 10")]
    public string amperaje = "10";
}
