using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

// Acertijo 4 del Cuarto 3: la consola de acceso al Laboratorio.
//
// Tiene un teclado hexadecimal (0-9 y A-F, como las direcciones de una red) y una tecla
// BORRAR. Cada tecla (PressableButton) llama a Ingresar() con su carácter. Al completar el
// largo de la clave (datos.solucion, el PuzzleData):
//  - si coincide: luz verde, "ACCESO CONCEDIDO" y se dispara "alResolverse" (las luces en
//    cascada hasta la puerta, que se abre);
//  - si no: luz roja, zumbido, "ACCESO DENEGADO" y se borra lo escrito.
// Sin energía (antes de prender las luces) la consola está apagada y no responde.
//
// Es como el teclado del Cuarto 2 (KeypadPuzzle), pero con letras además de números.
//
// En la escena: va en la consola. ConstructorCuarto3 arma las teclas y las conecta.
public class ConsolaAcceso : PuzzleBase
{
    public TMP_Text pantalla;

    [Tooltip("Fondo de la pantalla: apagado sin energía, encendido con energía")]
    public Renderer fondo;
    public Material fondoApagado, fondoEncendido;

    [Tooltip("Luz de estado: roja = bloqueado, verde = acceso concedido")]
    public Renderer luz;
    public Material luzRoja, luzVerde, luzApagada;

    [Tooltip("Qué pasa al poner una clave equivocada")]
    public UnityEvent alError = new UnityEvent();

    [Tooltip("Indicación que se ve debajo de la clave (cómo se arma)")]
    public string indicacion = "Fragmentos ordenados por IP, de menor a mayor";

    string escrito = "";
    bool energia;
    bool comprobando;

    void Start() => Mostrar();

    public void DarEnergia()
    {
        energia = true;
        Mostrar();
    }

    public void Ingresar(string caracter)
    {
        if (!energia || Resuelto || comprobando || escrito.Length >= Largo) return;
        escrito += caracter.ToUpper();
        SonidoSintetico.Tocar(SonidoSintetico.Pitido(1500f, 0.05f), transform.position, 0.5f);
        Mostrar();
        if (escrito.Length == Largo) StartCoroutine(Comprobar());
    }

    public void Borrar()
    {
        if (!energia || Resuelto || comprobando || escrito.Length == 0) return;
        escrito = escrito.Substring(0, escrito.Length - 1);
        SonidoSintetico.Tocar(SonidoSintetico.Pitido(700f, 0.05f), transform.position, 0.5f);
        Mostrar();
    }

    IEnumerator Comprobar()
    {
        comprobando = true;
        yield return new WaitForSeconds(0.35f);

        if (escrito == Solucion)
        {
            Resolver();
        }
        else
        {
            alError.Invoke();
            SonidoSintetico.Tocar(SonidoSintetico.Zumbido(150f, 0.45f), transform.position);
            if (luz != null) luz.sharedMaterial = luzRoja;
            pantalla.text = "<size=70%>ACCESO AL LABORATORIO</size>\n\n" + (datos != null ? datos.mensajeError : "ACCESO DENEGADO");
            pantalla.color = new Color(1f, 0.3f, 0.25f);
            yield return new WaitForSeconds(1.3f);
            escrito = "";
            Mostrar();
        }
        comprobando = false;
    }

    void Mostrar()
    {
        if (fondo != null) fondo.sharedMaterial = energia ? fondoEncendido : fondoApagado;
        if (luz != null) luz.sharedMaterial = energia ? luzRoja : luzApagada;
        if (pantalla == null) return;
        if (!energia)
        {
            pantalla.text = "";
            return;
        }

        // Los casilleros de la clave: lo escrito y rayitas para lo que falta
        string casilleros = "";
        for (int i = 0; i < Largo; i++)
            casilleros += (i < escrito.Length ? escrito[i].ToString() : "_") + (i < Largo - 1 ? "  " : "");
        pantalla.text = "<size=70%>ACCESO AL LABORATORIO</size>\n<size=150%>" + casilleros + "</size>\n<size=50%>" + indicacion + "</size>";
        pantalla.color = new Color(1f, 0.72f, 0.25f);
    }

    protected override void MostrarAcierto()
    {
        if (luz != null) luz.sharedMaterial = luzVerde;
        if (pantalla == null) return;
        pantalla.text = "<size=70%>ACCESO AL LABORATORIO</size>\n\n" + (datos != null ? datos.mensajeAcierto : "ACCESO CONCEDIDO");
        pantalla.color = new Color(0.4f, 1f, 0.55f);
    }

    int Largo => datos != null && !string.IsNullOrEmpty(datos.solucion) ? datos.solucion.Length : 3;
    string Solucion => datos != null && datos.solucion != null ? datos.solucion.ToUpper() : "";
}
