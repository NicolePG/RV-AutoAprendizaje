using UnityEngine;

// Prende y apaga objetos con un solo llamado. Se usa en el botón verde de la
// computadora: al presionarlo la pantalla pasa del aviso a las pistas, y al
// presionarlo de nuevo vuelve al aviso.
public class AlternarObjetos : MonoBehaviour
{
    [Tooltip("Los objetos que se prenden y apagan al alternar")]
    public GameObject[] objetos;

    public void Alternar()
    {
        foreach (var objeto in objetos)
            if (objeto != null) objeto.SetActive(!objeto.activeSelf);
    }
}
