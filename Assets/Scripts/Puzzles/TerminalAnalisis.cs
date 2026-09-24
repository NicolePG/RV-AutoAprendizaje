using System.Collections;
using TMPro;
using UnityEngine;

// Acertijo 4 del Cuarto 4: el registro del ensayo a la llama.
//
// Es la terminal del laboratorio (una pantalla con botones) que está junto a los mecheros.
// Muestra la tabla de colores (qué metal tiñe la llama de qué color) y, para cada muestra
// (1, 2 y 3), cuatro botones con los metales posibles: Li, Na, K y Cu.
// El jugador mete la punta de cada muestra en la llama, mira el color, lo busca en la tabla
// y marca el metal. Con las tres marcadas aprieta VALIDAR:
//  - si están bien: "ANÁLISIS VALIDADO" y se dispara "alResolverse" (se abre el casillero del
//    docente, donde está la tarjeta de la salida);
//  - si no: "RESULTADO INCORRECTO", zumbido y se borran las marcas. No dice cuál está mal: hay
//    que volver a mirar las llamas.
//
// Mientras no hay gas (mecheros apagados) la terminal espera y no deja marcar.
//
// En la escena: va en la terminal. ConstructorCuarto4 arma los botones y los conecta: cada botón
// de metal llama a Marcar() con un número = muestra * 10 + metal (21 = muestra 2, Na) y el botón
// grande llama a Validar().
public class TerminalAnalisis : PuzzleBase
{
    [Tooltip("Los metales de los botones de cada fila, en orden")]
    public string[] metales = { "Li", "Na", "K", "Cu" };

    [Tooltip("El metal correcto de cada muestra (su lugar en 'metales'): 1 = Cu, 2 = Na, 3 = Li")]
    public int[] correctos = { 3, 1, 0 };

    [Tooltip("La tapa de cada botón de metal, fila por fila (muestra 1: Li Na K Cu, muestra 2: ...)")]
    public Renderer[] botones;
    public Material botonNormal, botonMarcado;

    [Tooltip("Línea de estado, abajo de la pantalla")]
    public TMP_Text estado;

    public Renderer luz;
    public Material luzVerde, luzRoja, luzApagada;

    readonly int[] marcados = { -1, -1, -1 };   // el metal marcado en cada muestra (-1 = ninguno)
    bool habilitada, comprobando;

    void Start()
    {
        Pintar();
        Mostrar("<color=#94a3b8>Esperando gas para los mecheros...</color>", luzApagada);
    }

    // La llama la línea de gas cuando se encienden los mecheros
    public void Habilitar()
    {
        habilitada = true;
        Mostrar("Marcá el metal de cada muestra y apretá VALIDAR", luzApagada);
    }

    public void Marcar(int codigo)
    {
        if (!habilitada || Resuelto || comprobando) return;
        int muestra = codigo / 10 - 1, metal = codigo % 10;
        if (muestra < 0 || muestra >= marcados.Length || metal >= metales.Length) return;

        marcados[muestra] = metal;
        SonidoSintetico.Tocar(SonidoSintetico.Pitido(1400f, 0.04f), transform.position, 0.4f);
        Pintar();
    }

    public void Validar()
    {
        if (!habilitada || Resuelto || comprobando) return;
        for (int i = 0; i < marcados.Length; i++)
        {
            if (marcados[i] >= 0) continue;
            SonidoSintetico.Tocar(SonidoSintetico.Pitido(500f, 0.12f), transform.position, 0.5f);
            Mostrar("<color=#fbbf24>Falta marcar la MUESTRA " + (i + 1) + "</color>", luzApagada);
            return;
        }
        StartCoroutine(Comprobar());
    }

    IEnumerator Comprobar()
    {
        comprobando = true;
        Mostrar("Validando análisis...", luzApagada);
        yield return new WaitForSeconds(0.7f);
        comprobando = false;

        bool bien = true;
        for (int i = 0; i < marcados.Length; i++)
            if (marcados[i] != correctos[i]) bien = false;
        if (bien)
        {
            Resolver();
            yield break;
        }

        SonidoSintetico.Tocar(SonidoSintetico.Zumbido(150f, 0.45f), transform.position);
        Mostrar("<color=#f87171>RESULTADO INCORRECTO</color>  Volvé a mirar el color de cada llama", luzRoja);
        for (int i = 0; i < marcados.Length; i++) marcados[i] = -1;
        Pintar();
    }

    protected override void MostrarAcierto()
    {
        Mostrar("<color=#4ade80>ANÁLISIS VALIDADO</color>  Casillero del docente liberado", luzVerde);
    }

    // Resalta el botón marcado de cada fila
    void Pintar()
    {
        for (int i = 0; i < botones.Length; i++)
        {
            if (botones[i] == null) continue;
            bool marcado = marcados[i / metales.Length] == i % metales.Length;
            botones[i].sharedMaterial = marcado ? botonMarcado : botonNormal;
        }
    }

    void Mostrar(string texto, Material materialLuz)
    {
        if (luz != null && materialLuz != null) luz.sharedMaterial = materialLuz;
        if (estado != null) estado.text = texto;
    }
}
