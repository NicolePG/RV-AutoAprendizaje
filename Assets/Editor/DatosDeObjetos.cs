using UnityEditor;
using UnityEngine;

// Ayuda para los constructores de los cuartos: une un objeto agarrable de la escena con su asset
// ItemData (Assets/ScriptableObjects/Items). El asset se crea la primera vez; después se respeta lo
// que se cambie en el Inspector. El objeto recibe un PickableItem: al agarrarlo por primera vez
// queda en el inventario, suena un clic y vibra el control. El objeto no desaparece (se usa en la mano).
public static class DatosDeObjetos
{
    const string CARPETA = "Assets/ScriptableObjects/Items";

    public static ItemData Obtener(string archivo, string id, string nombre, string descripcion, TipoItem tipo)
    {
        if (!AssetDatabase.IsValidFolder("Assets/ScriptableObjects")) AssetDatabase.CreateFolder("Assets", "ScriptableObjects");
        if (!AssetDatabase.IsValidFolder(CARPETA)) AssetDatabase.CreateFolder("Assets/ScriptableObjects", "Items");

        string ruta = CARPETA + "/" + archivo + ".asset";
        var item = AssetDatabase.LoadAssetAtPath<ItemData>(ruta);
        if (item != null) return item;

        item = ScriptableObject.CreateInstance<ItemData>();
        item.id = id;
        item.nombre = nombre;
        item.descripcion = descripcion;
        item.tipo = tipo;
        AssetDatabase.CreateAsset(item, ruta);
        return item;
    }

    // El objeto ya tiene que tener su XRGrabInteractable
    public static void Guardable(GameObject objeto, ItemData datos)
    {
        var pick = objeto.GetComponent<PickableItem>();
        if (pick == null) pick = objeto.AddComponent<PickableItem>();
        pick.datos = datos;
        pick.ocultarAlAgarrar = false;
        EditorUtility.SetDirty(pick);
    }
}
