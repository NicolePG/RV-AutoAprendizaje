using System.Collections;
using TMPro;
using UnityEngine;

// Acertijo 4 del Cuarto 4: el ensayo a la llama y las palancas de la salida.
//
// El cierre de la salida de emergencia tiene tres palancas de cuchilla, cada una con el
// símbolo y el nombre de un metal (Na sodio, Li litio, Cu cobre). El cartel dice que se bajan
// en el orden de las muestras 1, 2 y 3 de la práctica. Para saber qué metal es cada muestra
// hay que hacer el ensayo a la llama en los mecheros (la llama se tiñe) y leer la tabla de
// colores: muestra 1 = verde = cobre, 2 = amarillo = sodio, 3 = rojo = litio.
//
// Cada palanca bajada le avisa acá (Marcar). Si era la que tocaba, queda abajo; si no, suena
// la alarma, se prende la luz roja y suben todas: hay que empezar de nuevo. Con las tres en
// orden se dispara "alResolverse" (se abre el casillero del docente, con la tarjeta de la salida).
//
// Las palancas están trabadas hasta que llega el gas (DarEnergia), porque sin mecheros no
// se puede hacer el ensayo.
//
// En la escena: va en el panel de las palancas. ConstructorCuarto4 lo arma y lo conecta.
public class SecuenciaPalancas : PuzzleBase
{
    public PalancaCuchilla[] palancas;

    [Tooltip("La palanca del metal de cada muestra, en orden: orden[0] es la de la muestra 1, etc.")]
    public int[] orden = { 3, 1, 2 };

    public Renderer luz;
    public Material luzVerde, luzRoja, luzApagada;
    public TMP_Text visor;

    int paso;
    bool reiniciando;   // mientras suena la alarma y suben las palancas

    void Start() => Mostrar("SIN GAS", luzApagada);

    public void DarEnergia()
    {
        foreach (PalancaCuchilla p in palancas) p.bloqueada = false;
        Mostrar(Pedido(), luzApagada);
    }

    // El visor dice qué muestra toca ahora: así queda claro que el orden es el de las muestras
    // (1, 2, 3) y no el de las palancas en la pared
    string Pedido() => "BAJAR EL METAL\nDE LA MUESTRA " + (paso + 1);

    public void Marcar(PalancaCuchilla palanca)
    {
        if (Resuelto) return;
        if (reiniciando) { palanca.Reiniciar(); return; }   // durante la alarma no cuenta: vuelve a subir
        if (paso < orden.Length && palanca.numero == orden[paso])
        {
            paso++;
            SonidoSintetico.Tocar(SonidoSintetico.Pitido(1000f + paso * 200f, 0.1f), transform.position, 0.6f);
            if (paso == orden.Length) Resolver();
            else Mostrar("<size=80%>MUESTRA " + paso + " OK</size>\n" + Pedido(), luzVerde);
            return;
        }
        StartCoroutine(Error());
    }

    IEnumerator Error()
    {
        reiniciando = true;
        paso = 0;
        Mostrar("NO ERA ESE METAL\n<size=70%>SE EMPIEZA DE NUEVO</size>", luzRoja);
        SonidoSintetico.Tocar(SonidoSintetico.Zumbido(140f, 0.6f), transform.position);
        yield return new WaitForSeconds(0.7f);
        foreach (PalancaCuchilla p in palancas) p.Reiniciar();
        yield return new WaitForSeconds(0.8f);
        reiniciando = false;
        if (!Resuelto) Mostrar(Pedido(), luzApagada);
    }

    protected override void MostrarAcierto()
    {
        Mostrar("CIERRE\nLIBERADO", luzVerde);
    }

    void Mostrar(string texto, Material materialLuz)
    {
        if (luz != null && materialLuz != null) luz.sharedMaterial = materialLuz;
        if (visor != null) visor.text = texto;
    }
}
