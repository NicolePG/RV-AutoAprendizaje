using UnityEditor;
using UnityEngine;

// Prende y apaga el simulador de XR, que es el que permite probar el juego en la PC sin
// visor: al dar Play crea un casco y unos mandos de mentira que se manejan con el teclado
// y el mouse.
//
// Por qué hace falta poder apagarlo: cuando el Quest está conectado por Link, ese casco de
// mentira se mezcla con el de verdad y le gana. El resultado es que la vista queda
// congelada (no sigue la cabeza), los mandos reales no aparecen y todo el juego responde
// solo al teclado, como si no hubiera visor. Apagándolo, el Quest toma el control.
//
// No hace falta apagarlo para el APK que se instala en el visor: ahí el simulador nunca se
// crea, porque está marcado como "solo en el editor".
//
// Menú: Escape Room > Simulador de PC. El tilde muestra si está prendido.
// Es lo mismo que el tilde "Use XR Interaction Simulator in scenes" de
// Project Settings > XR Interaction Toolkit, pero a mano y sin buscarlo.
public static class SimuladorDePC
{
    const string RUTA = "Assets/XRI/Settings/Resources/XRDeviceSimulatorSettings.asset";
    const string MENU = "Escape Room/Simulador de PC (apagalo para probar con el visor)";

    [MenuItem(MENU, false, 40)]
    static void Alternar()
    {
        SerializedProperty prop = Propiedad(out SerializedObject datos);
        if (prop == null)
        {
            Debug.LogWarning("No se encontró " + RUTA + ". El simulador se prende y apaga " +
                             "desde Project Settings > XR Interaction Toolkit.");
            return;
        }

        prop.boolValue = !prop.boolValue;
        datos.ApplyModifiedProperties();
        AssetDatabase.SaveAssets();

        Debug.Log(prop.boolValue
            ? "Simulador de PC PRENDIDO: se prueba con teclado y mouse, sin visor."
            : "Simulador de PC APAGADO: se prueba con el Quest conectado por Link.");
    }

    // Le pone el tilde al menú cuando el simulador está prendido
    [MenuItem(MENU, true)]
    static bool Validar()
    {
        SerializedProperty prop = Propiedad(out _);
        Menu.SetChecked(MENU, prop != null && prop.boolValue);
        return prop != null;
    }

    static SerializedProperty Propiedad(out SerializedObject datos)
    {
        datos = null;
        Object asset = AssetDatabase.LoadAssetAtPath<Object>(RUTA);
        if (asset == null) return null;

        datos = new SerializedObject(asset);
        return datos.FindProperty("m_AutomaticallyInstantiateSimulatorPrefab");
    }
}
