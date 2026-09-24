using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

// Un asa de muestra para el ensayo a la llama (Cuarto 4, acertijo 4).
//
// Es una varilla con un aro en la punta cargado con una sal metálica, como las de las
// prácticas de química. A simple vista las tres muestras son iguales (polvo blanco): solo se
// distinguen metiéndolas en la llama de un mechero, que se tiñe del color del metal
// (litio = rojo, sodio = amarillo, cobre = verde). Ese color lo pinta el Mechero.
//
// En la escena: va en el asa, junto a su XRGrabInteractable. ConstructorCuarto4 la arma.
[RequireComponent(typeof(XRGrabInteractable))]
public class MuestraLlama : MonoBehaviour
{
    [Tooltip("Color que toma la llama con esta muestra")]
    public Color colorLlama = Color.green;
}
