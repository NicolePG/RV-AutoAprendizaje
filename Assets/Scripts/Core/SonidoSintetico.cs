using System.Collections.Generic;
using UnityEngine;

// Sonidos cortos armados por código: pitidos, el zumbido de error, el arranque de una
// computadora y un chirrido metálico. El proyecto casi no tiene audios, así que en vez de
// buscar archivos se generan acá, como una onda que se escribe muestra por muestra.
// Cada sonido se crea una sola vez y se guarda para reusarlo.
//
// Se usa desde los scripts: SonidoSintetico.Tocar(SonidoSintetico.Pitido(1200f, 0.08f), posicion);
public static class SonidoSintetico
{
    const int MuestrasPorSegundo = 44100;
    static readonly Dictionary<string, AudioClip> guardados = new Dictionary<string, AudioClip>();
    static readonly Dictionary<string, AudioClip> grabados = new Dictionary<string, AudioClip>();

    // Tono puro, como el pitido de una tecla
    public static AudioClip Pitido(float hz, float segundos)
        => Crear("pitido_" + hz + "_" + segundos, segundos, t => Mathf.Sin(2f * Mathf.PI * hz * t));

    // Tono áspero y grave (onda cuadrada): el sonido de "error"
    public static AudioClip Zumbido(float hz, float segundos)
        => Crear("zumbido_" + hz + "_" + segundos, segundos, t => Mathf.Sin(2f * Mathf.PI * hz * t) > 0f ? 0.55f : -0.55f);

    // Tono que sube de desde a hasta: el arranque de una computadora
    public static AudioClip Subida(float desde, float hasta, float segundos)
        => Crear("subida_" + desde + "_" + hasta + "_" + segundos, segundos,
                 t => Mathf.Sin(2f * Mathf.PI * (desde * t + (hasta - desde) * t * t / (2f * segundos))));

    // Chirrido de metal arrastrado: un tono agudo que tiembla, mezclado con ruido
    public static AudioClip Chirrido(float segundos)
    {
        var azar = new System.Random(7);
        return Crear("chirrido_" + segundos, segundos, t =>
        {
            float ruido = (float)azar.NextDouble() * 2f - 1f;
            float hz = 950f + 350f * Mathf.Sin(2f * Mathf.PI * 3f * t) + 120f * ruido;
            return 0.65f * Mathf.Sin(2f * Mathf.PI * hz * t) + 0.35f * ruido;
        });
    }

    // Golpe seco y grave, como un pestillo metálico que se suelta
    public static AudioClip Golpe()
        => Crear("golpe", 0.25f, t => Mathf.Sin(2f * Mathf.PI * 95f * t) * Mathf.Exp(-t * 18f) * 2f);

    // Ruido continuo para usar en loop: el siseo del gas, el rugido de un mechero o el viento
    // de un extractor. "suavidad" le saca lo agudo: 0 = siseo fino, 0.95 = rugido grave.
    public static AudioClip Ruido(float suavidad, float segundos = 2f)
    {
        var azar = new System.Random(11);
        float anterior = 0f;
        return Crear("ruido_" + suavidad + "_" + segundos, segundos, t =>
        {
            float blanco = (float)azar.NextDouble() * 2f - 1f;
            anterior = Mathf.Lerp(blanco, anterior, suavidad);
            return anterior * (1f + suavidad * 3f);   // al filtrarlo baja el volumen: se compensa
        }, true);
    }

    // Roce: ruido filtrado que arranca fuerte y se va apagando, como algo que se
    // arrastra y se frena. Sirve para el cajón, el almohadón del sofá y el tornillo.
    public static AudioClip Roce(float segundos, float suavidad = 0.85f)
    {
        var azar = new System.Random(3);
        float anterior = 0f;
        return Crear("roce_" + segundos + "_" + suavidad, segundos, t =>
        {
            float blanco = (float)azar.NextDouble() * 2f - 1f;
            anterior = Mathf.Lerp(blanco, anterior, suavidad);
            return anterior * (1f + suavidad * 3f) * Mathf.Exp(-t * 2.5f / segundos);
        });
    }

    // ------------------------------------------------------------ sonidos del juego
    //
    // Los scripts piden el sonido por su nombre y no por su frecuencia, así se entiende
    // qué suena en cada lado. Son el RESPALDO: si en el Inspector se le arrastra una
    // grabación de verdad al campo "sonido" del componente, esa le gana a esto.

    // Cada uno busca primero la GRABACIÓN en Assets/Resources/Audio (los .ogg que se
    // bajaron de OpenGameArt, ver el CREDITOS.txt de esa carpeta). Si el archivo no está,
    // usa el sonido armado por código, que es el que había antes: así el juego nunca queda
    // mudo, ni siquiera si alguien borra la carpeta de audio.
    public static AudioClip Clic() => Grabado("clic") ?? Pitido(1400f, 0.05f);        // botón, tecla
    public static AudioClip Tecla() => Grabado("tecla") ?? Pitido(880f, 0.06f);       // teclado numérico
    public static AudioClip Error() => Grabado("error") ?? Zumbido(150f, 0.4f);       // código equivocado
    public static AudioClip Puerta() => Grabado("puerta") ?? Chirrido(1.1f);          // bisagra que gira
    public static AudioClip Pestillo() => Grabado("pestillo") ?? Golpe();             // la hoja que encaja
    public static AudioClip Cajon() => Grabado("cajon") ?? Roce(0.5f, 0.9f);          // cajón que se desliza
    public static AudioClip Cojin() => Grabado("cojin") ?? Roce(0.45f, 0.95f);        // almohadón
    public static AudioClip Tornillo() => Grabado("tornillo") ?? Roce(0.18f, 0.6f);   // tornillo girando
    public static AudioClip Cerradura() => Grabado("cerradura") ?? Golpe();           // la llave que gira
    public static AudioClip Susto() => Grabado("susto") ?? Subida(600f, 55f, 0.8f);   // risa endemoniada
    public static AudioClip Grito() => Grabado("grito") ?? Subida(900f, 300f, 0.6f);  // grito humano
    public static AudioClip Agarrar() => Grabado("agarrar") ?? Roce(0.12f, 0.75f);    // la mano toma algo
    public static AudioClip Soltar() => Grabado("soltar") ?? Roce(0.16f, 0.88f);      // lo apoya
    public static AudioClip Papel() => Grabado("papel") ?? Roce(0.3f, 0.35f);         // hoja o libro
    public static AudioClip Vidrio() => Grabado("vidrio") ?? Roce(0.8f, 0.55f);       // vidrio que corre
    public static AudioClip Encajar() => Grabado("encajar") ?? Pitido(900f, 0.09f);   // entra en su lugar

    // Busca una grabación en Assets/Resources/Audio. Devuelve null si no está, y se acuerda
    // de lo que ya buscó (también de lo que no encontró) para no mirar el disco cada vez.
    public static AudioClip Grabado(string nombre)
    {
        if (grabados.TryGetValue(nombre, out AudioClip guardado)) return guardado;

        AudioClip clip = Resources.Load<AudioClip>("Audio/" + nombre);
        grabados[nombre] = clip;
        return clip;
    }

    // Portazo: el golpe de la hoja contra el marco con el crujido de la madera encima.
    // Es aparte del Golpe porque este tiene que oírse fuerte y desde todo el cuarto.
    public static AudioClip Portazo()
    {
        AudioClip grabacion = Grabado("portazo");
        if (grabacion != null) return grabacion;

        var azar = new System.Random(5);
        return Crear("portazo", 0.45f, t =>
        {
            float ruido = (float)azar.NextDouble() * 2f - 1f;
            float cuerpo = Mathf.Sin(2f * Mathf.PI * 80f * t) * Mathf.Exp(-t * 11f) * 2.2f;
            float madera = ruido * Mathf.Exp(-t * 26f) * 0.8f;
            return cuerpo + madera;
        });
    }

    public static void Tocar(AudioClip clip, Vector3 donde, float volumen = 1f)
    {
        if (clip != null) AudioSource.PlayClipAtPoint(clip, donde, volumen);
    }

    // enLoop: el sonido se repite sin cortes, así que no se le suavizan los bordes
    static AudioClip Crear(string nombre, float segundos, System.Func<float, float> onda, bool enLoop = false)
    {
        if (guardados.TryGetValue(nombre, out AudioClip guardado) && guardado != null) return guardado;

        int cantidad = Mathf.CeilToInt(segundos * MuestrasPorSegundo);
        var muestras = new float[cantidad];
        for (int i = 0; i < cantidad; i++)
        {
            float t = i / (float)MuestrasPorSegundo;
            // Sube y baja el volumen en los bordes: sin esto se oye un "clic" al empezar y al terminar
            float borde = enLoop ? 1f : Mathf.Min(1f, t / 0.005f) * Mathf.Min(1f, (segundos - t) / 0.03f);
            muestras[i] = Mathf.Clamp(onda(t), -1f, 1f) * borde * 0.5f;
        }

        AudioClip clip = AudioClip.Create(nombre, cantidad, 1, MuestrasPorSegundo, false);
        clip.SetData(muestras, 0);
        guardados[nombre] = clip;
        return clip;
    }
}
