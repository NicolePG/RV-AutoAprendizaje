using UnityEngine;

// Acertijo 2 del Cuarto 3: conectar las computadoras a la red.
//
// Con luz, las computadoras prenden pero dicen "SIN CONEXIÓN A LA RED": sus cables están
// desenchufados. De la torre de cada computadora que funciona (C2, C3 y C5) sale su cable,
// con la ficha suelta sobre la mesada. En la pared, al final de la mesada, está el panel de
// red con 6 tomas (R1 a R6) y al lado el "Mapa de la red", que dice la toma de cada equipo.
// Hay que llevar cada ficha a su toma (se agarra con la mano y se suelta cerca de la toma).
//
// Cada toma tiene una luz: verde = esa ficha va ahí, roja = no va ahí (y suena un error).
// Cada computadora tiene red solo mientras SU ficha esté en SU toma: si la ficha va a otra
// toma, esa computadora sigue sin red. El panel no tiene energía hasta que se prenden las
// luces (acertijo 1). Con las 3 bien conectadas se dispara "alResolverse" (cartel).
//
// En la escena: va en el panel de red. ConstructorCuarto3 arma las tomas, las fichas y los cables.
public class RedSala : PuzzleBase
{
    [Tooltip("Las tomas del panel (R1 a R6)")]
    public TomaDeRed[] tomas;

    [Tooltip("Las fichas de las computadoras que funcionan")]
    public FichaRed[] fichas;

    public Material luzVerde, luzRoja, luzApagada;

    bool energia;
    int rojasAntes;

    void Awake()
    {
        foreach (TomaDeRed toma in tomas) toma.alCambiar.AddListener(Revisar);
    }

    void Start() => Revisar();

    // Lo llama el tablero de luces al resolverse: sin energía el panel está apagado
    public void DarEnergia()
    {
        energia = true;
        Revisar();
    }

    void Revisar()
    {
        // La luz de cada toma
        int rojas = 0;
        foreach (TomaDeRed toma in tomas)
        {
            FichaRed ficha = toma.Conectada;
            bool bien = ficha != null && ficha.tomaCorrecta == toma;
            Material luz = luzApagada;
            if (energia && ficha != null)
            {
                luz = bien ? luzVerde : luzRoja;
                if (!bien) rojas++;
            }
            if (toma.luz != null) toma.luz.sharedMaterial = luz;
        }
        // Una ficha recién puesta en la toma equivocada: zumbido de error
        if (rojas > rojasAntes) SonidoSintetico.Tocar(SonidoSintetico.Zumbido(180f, 0.3f), transform.position);
        rojasAntes = rojas;

        // La red de cada computadora
        bool todas = true;
        foreach (FichaRed ficha in fichas)
        {
            bool conectada = energia && ficha.tomaCorrecta != null && ficha.tomaCorrecta.Conectada == ficha;
            if (ficha.computadora != null) ficha.computadora.CambiarRed(conectada);
            if (!conectada) todas = false;
        }

        if (todas && !Resuelto) Resolver();
    }
}
