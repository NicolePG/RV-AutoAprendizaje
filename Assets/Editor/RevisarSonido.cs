using System.Text;
using UnityEditor;
using UnityEngine;

// Revisa por qué no se oye el juego y arregla lo que se pueda arreglar solo.
//
// Cuando NO SE OYE NADA (ni los menús, ni los botones, ni el ambiente) el problema casi nunca
// está en el sonido que falta: está en algo que apaga todo de una vez. Esto los revisa en
// orden y lo dice en la Consola, en vez de andar probando a ciegas:
//
//  1. El botón de silencio del EDITOR. Es el altavoz tachado de la barra de abajo y el botón
//     "Mute Audio" de la pestaña Game. Silencia TODO el juego y no deja ni un error, así que
//     es el que más confunde. Si está puesto, esto lo saca.
//  2. El volumen que eligió el jugador en el menú (Opciones). Se guarda en el visor y se
//     aplica cada vez que arranca el juego, así que si alguna vez quedó en 0 %, el juego
//     arranca mudo para siempre. Si está en 0, esto lo vuelve a 100 %.
//  3. Que haya un AudioListener (el "oído" del jugador) y que esté prendido.
//  4. Que estén los archivos de sonido en Resources/Audio.
//  5. Cómo abrió Unity la placa de sonido.
//
// Se corre solo al final de "Escape Room > Construir los 4 cuartos" (MenuEscapeRoom): no tiene
// menú propio. El informe queda en la Consola.
public static class RevisarSonido
{
    static readonly string[] ESPERADOS =
    {
        "menu_terror", "ambiente_cuarto1", "ambiente_cuarto2", "ambiente_cuarto3", "ambiente_cuarto4",
        "crujido1", "crujido2", "crujido3", "crujido4", "puerta", "portazo", "pestillo", "clic",
        "tecla", "cajon", "tornillo", "cerradura", "encajar", "susto", "agarrar", "soltar",
        "papel", "vidrio",
    };

    public static void Revisar()
    {
        var informe = new StringBuilder("REVISIÓN DEL SONIDO\n\n");
        int arreglados = 0;
        int problemas = 0;

        // 1. El silencio del editor
        if (EditorUtility.audioMasterMute)
        {
            EditorUtility.audioMasterMute = false;
            informe.AppendLine("ARREGLADO  El sonido del editor estaba SILENCIADO (el altavoz tachado de");
            informe.AppendLine("           la barra de abajo). Se sacó: por eso no se oía absolutamente nada.");
            arreglados++;
        }
        else
        {
            informe.AppendLine("ok         El sonido del editor no está silenciado.");
        }

        // 2. El volumen guardado del menú
        float guardado = PlayerPrefs.GetFloat("volumen", 1f);
        if (guardado <= 0.001f)
        {
            PlayerPrefs.SetFloat("volumen", 1f);
            PlayerPrefs.Save();
            AudioListener.volume = 1f;
            informe.AppendLine("ARREGLADO  El volumen del juego estaba guardado en 0 % (se baja desde el menú,");
            informe.AppendLine("           en Opciones). Se volvió a 100 %.");
            arreglados++;
        }
        else
        {
            informe.AppendLine("ok         Volumen guardado del juego: " + Mathf.RoundToInt(guardado * 100f) + " %.");
        }

        // 3. El oído del jugador
        var oidos = Object.FindObjectsByType<AudioListener>(FindObjectsInactive.Include);
        if (oidos.Length == 0)
        {
            informe.AppendLine("PROBLEMA   No hay ningún AudioListener en la escena: el jugador no tiene oídos.");
            informe.AppendLine("           Normalmente viene en la cámara del XR Origin.");
            problemas++;
        }
        else
        {
            int prendidos = 0;
            foreach (AudioListener o in oidos) if (o.enabled && o.gameObject.activeInHierarchy) prendidos++;

            if (prendidos == 0)
            {
                informe.AppendLine("PROBLEMA   Hay " + oidos.Length + " AudioListener pero ninguno está prendido.");
                problemas++;
            }
            else if (oidos.Length > 1)
            {
                informe.AppendLine("AVISO      Hay " + oidos.Length + " AudioListener (debería haber uno solo).");
                informe.AppendLine("           Unity usa uno y avisa por consola, pero el sonido se oye igual.");
            }
            else
            {
                informe.AppendLine("ok         El AudioListener está en \"" + oidos[0].name + "\" y está prendido.");
            }
        }

        // 4. Los archivos de sonido
        int faltan = 0;
        var nombresFaltantes = new StringBuilder();
        foreach (string nombre in ESPERADOS)
        {
            if (Resources.Load<AudioClip>("Audio/" + nombre) != null) continue;
            faltan++;
            nombresFaltantes.Append(nombre).Append(' ');
        }
        if (faltan > 0)
        {
            informe.AppendLine("AVISO      Faltan " + faltan + " de " + ESPERADOS.Length + " sonidos en Resources/Audio: " + nombresFaltantes);
            informe.AppendLine("           Esos usan el sonido armado por código, así que igual se oye algo.");
        }
        else
        {
            informe.AppendLine("ok         Están los " + ESPERADOS.Length + " sonidos en Resources/Audio.");
        }

        // 5. La placa de sonido
        AudioConfiguration config = AudioSettings.GetConfiguration();
        informe.AppendLine("ok         Placa de sonido: " + config.sampleRate + " Hz, " + config.speakerMode +
                           ", buffer de " + config.dspBufferSize + " muestras.");
        if (AudioSettings.driverCapabilities == AudioSpeakerMode.Mono)
        {
            informe.AppendLine("AVISO      Windows dice que la salida es MONO. En VR conviene que sea estéreo.");
        }

        informe.AppendLine();
        if (arreglados > 0)
            informe.AppendLine("Se arreglaron " + arreglados + " cosas. Dale Play de nuevo y probá.");
        else if (problemas > 0)
            informe.AppendLine("Quedan " + problemas + " problemas que hay que mirar a mano (arriba dice cuáles).");
        else
            informe.AppendLine("No se encontró nada apagado. Si igual no se oye, revisá el volumen de Windows y");

        if (arreglados == 0 && problemas == 0)
            informe.AppendLine("qué dispositivo de salida está usando (con el visor por Link, Windows suele");

        if (arreglados == 0 && problemas == 0)
            informe.AppendLine("cambiar la salida a los auriculares del Quest).");

        Debug.Log(informe.ToString());
    }
}
