using UnityEngine;

// Acertijo 3 del Cuarto 3: prender las computadoras y reunir la clave.
//
// Junta las 5 computadoras de la sala: les pasa la energía (cuando se prenden las luces). La
// red le llega a cada una por su propio cable (RedSala). Se resuelve cuando las 3 computadoras
// que tienen clave la están mostrando, y ahí el cartel pasa al último paso (la consola).
// Además prende el resplandor de las pantallas en cuanto se enciende la primera.
//
// En la escena: va en el grupo de los puestos. ConstructorCuarto3 lo arma solo.
public class ControlComputadoras : PuzzleBase
{
    public ComputadoraSala[] computadoras;

    [Tooltip("Luz del resplandor de las pantallas sobre la mesada")]
    public Light resplandor;

    void Awake()
    {
        foreach (ComputadoraSala c in computadoras)
        {
            c.alMostrarClave.AddListener(Revisar);
            c.alPrender.AddListener(PrenderResplandor);
        }
    }

    void Start()
    {
        if (resplandor != null) resplandor.enabled = false;
    }

    public void DarEnergia()
    {
        foreach (ComputadoraSala c in computadoras) c.DarEnergia();
    }

    void PrenderResplandor()
    {
        if (resplandor != null) resplandor.enabled = true;
    }

    void Revisar()
    {
        if (Resuelto) return;
        foreach (ComputadoraSala c in computadoras)
            if (c.tipo == ComputadoraSala.Tipo.Clave && !c.MuestraClave) return;
        Resolver();
    }
}
