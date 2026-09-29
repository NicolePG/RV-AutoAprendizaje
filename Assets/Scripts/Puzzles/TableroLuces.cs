using UnityEngine;

// Acertijo 1 del Cuarto 3: prender las luces de la sala, de noche.
//
// La sala está a oscuras. Junto a la entrada hay un tablero con 4 interruptores, pero no
// están conectados como uno esperaría: cada uno prende o apaga VARIAS lámparas a la vez
// (las de su circuito), y los circuitos se pisan entre sí. Hay que encontrar la combinación
// que deja las 6 lámparas prendidas (hay una sola; uno de los interruptores sobra).
// El tablero tiene un mapa chico con una lucecita por lámpara, así se ve qué hace cada
// interruptor sin tener que mirar el techo.
//
// Mientras tanto la sala se va aclarando según cuántas lámparas estén prendidas. Con las 6:
// vuelve la luz del todo, los interruptores se traban y se dispara "alResolverse" (energía
// para el panel de red, las computadoras y la consola; el cartel pasa al paso siguiente).
//
// En la escena: va en el tablero. ConstructorCuarto3 arma las lámparas, los circuitos y las
// conexiones.
public class TableroLuces : PuzzleBase
{
    [System.Serializable]
    public class Lampara
    {
        [Tooltip("Los tubos de la lámpara (cambian de material al prenderse)")]
        public Renderer[] tubos;

        [Tooltip("Lucecita de esta lámpara en el mapa del tablero")]
        public Renderer indicador;

        [Tooltip("0 = lado izquierdo de la sala (luz del techo A), 1 = lado derecho (luz B)")]
        public int lado;
    }

    [System.Serializable]
    public class Circuito
    {
        [Tooltip("Qué lámparas cambia este interruptor (sus números en la lista de lámparas)")]
        public int[] lamparas;
    }

    [Tooltip("Los interruptores del tablero, en orden")]
    public Interruptor[] interruptores;

    [Tooltip("Un circuito por interruptor, en el mismo orden")]
    public Circuito[] circuitos;

    public Lampara[] lamparas;

    [Tooltip("Luz real de cada lado de la sala: [0] izquierda, [1] derecha. Brilla según cuántas lámparas de su lado estén prendidas")]
    public Light[] lucesPorLado;
    public float intensidadMaxima = 2f;

    public Material tuboPrendido, tuboApagado, indicadorPrendido, indicadorApagado;

    [Tooltip("Clima de la sala: se va aclarando con cada lámpara")]
    public ClimaCuarto clima;

    [Tooltip("Sonda de reflejos: se vuelve a sacar la foto de la sala cuando hay luz")]
    public ReflectionProbe sondaReflejos;

    [Tooltip("Lo que se enciende recién al resolverlo (por ejemplo, el parpadeo de una lámpara)")]
    public Behaviour[] activarAlResolver;

    [Tooltip("Lo que se apaga al resolverlo (por ejemplo, la luz de la luna)")]
    public GameObject[] apagarAlResolver;

    bool[] prendida;

    void Awake()
    {
        prendida = new bool[lamparas.Length];
        for (int i = 0; i < interruptores.Length; i++)
        {
            int numero = i;   // copia para que cada interruptor recuerde el suyo
            interruptores[i].alAccionar.AddListener(() => Accionar(numero));
        }
    }

    // Al empezar el juego el jugador está en el Cuarto 1: acá solo se preparan las lámparas.
    // El clima NO se aplica (es de toda la escena y oscurecería el Cuarto 1): lo pone la zona
    // de la entrada de la sala cuando el jugador llega.
    void Start()
    {
        Actualizar(false);
        if (sondaReflejos != null) sondaReflejos.RenderProbe();
    }

    void Accionar(int interruptor)
    {
        if (Resuelto || interruptor >= circuitos.Length) return;

        foreach (int l in circuitos[interruptor].lamparas)
            if (l >= 0 && l < prendida.Length) prendida[l] = !prendida[l];

        Actualizar(true);
        if (System.Array.TrueForAll(prendida, p => p)) Resolver();
    }

    // Pone cada lámpara, su lucecita, las luces reales y el clima según lo que esté prendido
    void Actualizar(bool aplicarClima)
    {
        int[] prendidasPorLado = new int[2];
        int[] totalPorLado = new int[2];
        int prendidas = 0;

        for (int i = 0; i < lamparas.Length; i++)
        {
            Lampara l = lamparas[i];
            foreach (Renderer tubo in l.tubos)
                if (tubo != null) tubo.sharedMaterial = prendida[i] ? tuboPrendido : tuboApagado;
            if (l.indicador != null) l.indicador.sharedMaterial = prendida[i] ? indicadorPrendido : indicadorApagado;

            int lado = Mathf.Clamp(l.lado, 0, 1);
            totalPorLado[lado]++;
            if (prendida[i]) { prendidasPorLado[lado]++; prendidas++; }
        }

        for (int lado = 0; lado < lucesPorLado.Length && lado < 2; lado++)
        {
            Light luz = lucesPorLado[lado];
            if (luz == null || totalPorLado[lado] == 0) continue;
            luz.intensity = intensidadMaxima * prendidasPorLado[lado] / totalPorLado[lado];
            luz.enabled = prendidasPorLado[lado] > 0;
        }

        if (clima != null && lamparas.Length > 0)
        {
            float nivel = (float)prendidas / lamparas.Length;
            if (aplicarClima) clima.Mezclar(nivel);
            else clima.nivel = nivel;
        }
    }

    protected override void MostrarAcierto()
    {
        foreach (Interruptor i in interruptores) i.bloqueado = true;
        foreach (Behaviour b in activarAlResolver) if (b != null) b.enabled = true;
        foreach (GameObject g in apagarAlResolver) if (g != null) g.SetActive(false);
        if (sondaReflejos != null) sondaReflejos.RenderProbe();
    }
}
