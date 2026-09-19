using UnityEngine;

// Prende y apaga objetos con un solo llamado. Se usa en el botón de la computadora
// del escritorio: al presionarlo cambia la pantalla de espera por las instrucciones,
// y al presionarlo de nuevo vuelve atrás.
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
