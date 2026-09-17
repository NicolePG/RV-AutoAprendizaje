using UnityEngine;

// Se agregan más valores acá a medida que se arman los objetos de los otros cuartos
public enum TipoItem { ObjetoEspecial }

// Datos de un objeto que se puede guardar en el inventario, como asset reusable sin tocar código.
// Un asset por objeto (por ejemplo "ItemData_ObjetoEspecial").
[CreateAssetMenu(fileName = "ItemData_", menuName = "EscapeRoom/Item Data")]
public class ItemData : ScriptableObject
{
    public string id;
    public string nombre;
    public Sprite icono;

    [TextArea]
    public string descripcion;

    public TipoItem tipo;
}
