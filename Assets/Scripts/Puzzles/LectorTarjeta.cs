using System.Collections;
using TMPro;
using UnityEngine;

// Acertijo 5 del Cuarto 4 (el último del juego): el lector de tarjetas de la salida de emergencia.
//
// Es un lector sin contacto: se acerca la tarjeta y la lee. Solo abre la tarjeta del docente
// ("tarjetaValida"), que está en su casillero y se libera al validar el análisis. La del alumno,
// que quedó sobre la mesada, da "ACCESO DENEGADO": solo el personal docente puede autorizar
// la evacuación del laboratorio.
// Con la correcta: luz verde, pitido, "ACCESO CONCEDIDO" y se dispara "alResolverse" (se abre
// la puerta de emergencia).
//
// En la escena: va en el lector, junto a un collider "Is Trigger" delante de él (la zona donde
// se lee la tarjeta). ConstructorCuarto4 lo arma y lo conecta con la puerta.
[RequireComponent(typeof(Collider))]
public class LectorTarjeta : PuzzleBase
{
    [Tooltip("La única tarjeta que abre la salida (su ItemData)")]
    public ItemData tarjetaValida;

    public TMP_Text pantalla;
    public Renderer luz;
    public Material luzVerde, luzRoja, luzEspera;

    const string EN_ESPERA = "ACERQUE SU\nTARJETA";
    float proximaLectura;

    void Start() => Mostrar(EN_ESPERA, luzEspera);

    void OnTriggerEnter(Collider otro)
    {
        if (Resuelto || Time.time < proximaLectura) return;
        var tarjeta = otro.GetComponentInParent<TarjetaAcceso>();
        if (tarjeta == null) return;
        proximaLectura = Time.time + 1.2f;   // una lectura por pasada, como un lector real

        if (tarjetaValida != null && tarjeta.datos == tarjetaValida)
        {
            SonidoSintetico.Tocar(SonidoSintetico.Pitido(1760f, 0.15f), transform.position, 0.7f);
            Resolver();
            return;
        }
        StartCoroutine(Denegar(tarjeta));
    }

    IEnumerator Denegar(TarjetaAcceso tarjeta)
    {
        SonidoSintetico.Tocar(SonidoSintetico.Zumbido(160f, 0.4f), transform.position);
        string quien = tarjeta.datos != null ? tarjeta.datos.nombre.ToUpper() : "TARJETA";
        Mostrar("ACCESO DENEGADO\n<size=55%>" + quien + ": SIN PERMISO</size>", luzRoja);
        yield return new WaitForSeconds(2.2f);
        if (!Resuelto) Mostrar(EN_ESPERA, luzEspera);
    }

    protected override void MostrarAcierto() => Mostrar("ACCESO\nCONCEDIDO", luzVerde);

    void Mostrar(string texto, Material materialLuz)
    {
        if (luz != null && materialLuz != null) luz.sharedMaterial = materialLuz;
        if (pantalla != null) pantalla.text = texto;
    }
}
