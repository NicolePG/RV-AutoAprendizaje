using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Deja en la escena el objeto "Optimizacion" con los scripts que ayudan a mantener los FPS
// en el Quest:
//  - GestorDeCuartos: dibuja solo los cuartos que el jugador puede ver.
//  - FoveacionQuest: en el visor, dibuja los bordes de la vista con menos detalle.
// Si el objeto ya existe, solo le agrega lo que le falte. Lo llama el constructor del
// Cuarto 3 al terminar, dentro de Escape Room > Construir los 4 cuartos.
public static class OptimizarEscena
{
    public static void Preparar()
    {
        GameObject objeto = GameObject.Find("Optimizacion");
        if (objeto == null)
        {
            objeto = new GameObject("Optimizacion");
            Undo.RegisterCreatedObjectUndo(objeto, "Optimizar escena");
        }
        if (objeto.GetComponent<GestorDeCuartos>() == null) objeto.AddComponent<GestorDeCuartos>();
        if (objeto.GetComponent<FoveacionQuest>() == null) objeto.AddComponent<FoveacionQuest>();

        EditorSceneManager.MarkSceneDirty(objeto.scene);
        Debug.Log("Optimización lista: el objeto 'Optimizacion' dibuja solo los cuartos que el jugador puede ver. Guarda la escena (Ctrl+S).");
    }
}
