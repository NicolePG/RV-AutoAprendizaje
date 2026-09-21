using TMPro;
using UnityEngine;
using UnityEngine.Events;

// Acertijo del teclado numérico del Cuarto 2.
// Los botones (PressableButton, uno por dígito 0-9) llaman a IngresarDigito() desde
// su evento "alPresionar" en el Inspector. Al completar tantos dígitos como tiene
// datos.solucion: si coincide, llama a Resolver() (evento alResolverse, heredado de
// PuzzleBase) y con eso se abre la puerta; si no coincide, dispara "alError"
// (luz roja + sonido, se conecta en el Inspector) y reinicia lo ingresado.
public class KeypadPuzzle : PuzzleBase
{
    [Tooltip("Pantallita del teclado donde se ve lo que se va marcando")]
    public TMP_Text pantalla;

    [Tooltip("Qué pasa si el código ingresado es incorrecto (luz roja, sonido grave)")]
    public UnityEvent alError = new UnityEvent();

    string ingresado = "";

    void Start() => MostrarEnPantalla();

    public void IngresarDigito(int digito)
    {
        if (Resuelto) return;

        // Sin datos no sabe cuál es el código: antes fallaba sin decir nada y parecía
        // que el 3719 estaba mal
        if (datos == null)
        {
            Debug.LogError("El teclado no tiene PuzzleData asignado y no sabe cuál es el código. " +
                           "Hay que reconstruir el Cuarto 2 (Escape Room > Construir Cuarto 2).", this);
            return;
        }

        ingresado += digito;
        MostrarEnPantalla();

        if (ingresado.Length < datos.solucion.Length) return;

        if (ingresado == datos.solucion)
        {
            Resolver();
            if (pantalla != null) pantalla.text = "OK";
        }
        else
        {
            alError.Invoke();
            ingresado = "";
            if (pantalla != null) pantalla.text = "ERROR";
        }
    }

    public void Borrar()
    {
        ingresado = "";
        MostrarEnPantalla();
    }

    // Muestra los dígitos marcados y guiones en los que faltan: por ejemplo "37--"
    void MostrarEnPantalla()
    {
        if (pantalla == null || datos == null) return;

        int faltan = datos.solucion.Length - ingresado.Length;
        pantalla.text = ingresado + new string('-', Mathf.Max(0, faltan));
    }
}
