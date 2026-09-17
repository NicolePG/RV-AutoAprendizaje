using System.Collections.Generic;
using UnityEngine;

// Guarda qué objetos (ItemData) tiene el jugador. Sin interfaz visual todavía:
// solo responde "¿el jugador tiene tal objeto?", que es lo único que necesita
// el Cuarto 2 (para guardar el objeto especial) y lo que va a preguntar el
// Cuarto 4 más adelante (para habilitar la ranura).
//
// En la escena: un solo objeto vacío llamado "Inventory" en la raíz, con este script.
public class Inventory : MonoBehaviour
{
    public static Inventory Instancia { get; private set; }

    readonly List<ItemData> items = new List<ItemData>();

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
