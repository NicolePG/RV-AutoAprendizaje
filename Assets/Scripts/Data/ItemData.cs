using UnityEngine;

// Qué tipo de objeto es. Los valores nuevos van siempre al final: los assets guardan el número
public enum TipoItem { ObjetoEspecial, Tarjeta, Llave, Herramienta }

// Datos de un objeto que se puede guardar en el inventario, como asset reusable sin tocar código.
// Un asset por objeto, en Assets/ScriptableObjects/Items: la llave y el destornillador del Cuarto 1,
// la linterna del Cuarto 3 y las dos tarjetas del Cuarto 4.
//
// Por qué un ScriptableObject: el objeto es un asset y no un texto. El lector de la salida del
// Cuarto 4 compara el asset de la tarjeta (la del docente abre, la del alumno no), el inventario
// guarda assets, y un objeto nuevo se crea como asset sin tocar código. PickableItem une cada
// objeto de la escena con su asset: al agarrarlo, queda en el inventario y vibra el control.
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
