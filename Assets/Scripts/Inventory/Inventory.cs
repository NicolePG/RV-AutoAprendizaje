using System.Collections.Generic;
using UnityEngine;

// Guarda qué objetos (ItemData) agarró el jugador. No tiene interfaz visual: responde
// "¿el jugador tiene tal objeto?" y el aviso de que algo se guardó es la vibración del
// control (la dispara PickableItem).
//
// Lo llenan los cinco objetos que tienen PickableItem: la llave y el destornillador del
// Cuarto 1, la linterna del Cuarto 3 y las dos tarjetas del Cuarto 4 (la del docente y la
// del alumno). Cada uno trae su asset ItemData en Assets/ScriptableObjects/Items.
//
// En la escena: un solo objeto vacío llamado "Inventory" en la raíz, con este script.
public class Inventory : MonoBehaviour
{
    public static Inventory Instancia { get; private set; }

    // Se ve en el Inspector mientras el juego corre: es la forma de comprobar, en una
    // prueba, que el objeto se guardó de verdad. Todavía no hay interfaz en el visor.
    [SerializeField]
    [Tooltip("Lo que el jugador lleva encima. Se llena solo al agarrar los objetos")]
    List<ItemData> items = new List<ItemData>();

    void Awake()
    {
        Instancia = this;
    }

    public void Agregar(ItemData item)
    {
        if (item != null && !items.Contains(item)) items.Add(item);
    }

    public bool Tiene(ItemData item)
    {
        return items.Contains(item);
    }
}
