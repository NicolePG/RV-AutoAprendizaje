using UnityEngine;

// Dónde va cada cuarto de la escena para que queden empalmados uno detrás del otro.
//
// El Cuarto 1 (Recepción, de Otoniel) está centrado en (0, 0, 0) y su salida está en el
// muro sur. Detrás de esa puerta tiene un pasillo corto que llega hasta z = -3.4 y está
// centrado en x = 1.2. Ahí tiene que arrancar el Cuarto 2.
//
// Los Cuartos 2 y 4 y el pasillo provisional se arman mirando hacia +Z (la entrada en su
// z = 0 y el fondo más adelante). Para que sigan el recorrido del Cuarto 1, que avanza
// hacia el sur, se los gira 180 grados y se los corre hasta que la entrada del Cuarto 2
// cae justo al final de ese pasillo. Los tres llevan el mismo giro y el mismo corrimiento,
// así entre ellos siguen conectados igual que antes y ninguno cambia de tamaño.
//
// Las cuentas: con el giro, un punto (x, z) del armado queda en (2.55 - x, 0.48 - z).
// El centro de la entrada del Cuarto 2 (x = 1.35, z = 4 en el armado original) cae en
// (1.2, -3.52): centrado con el pasillo del Cuarto 1 y pegado a su final.
//
// El Cuarto 3 ocupa el hueco que había entre el Cuarto 2 y el Cuarto 4 (donde estaba el
// pasillo provisional). Medido con las cuentas del pasillo: la pared del fondo del Cuarto 2
// termina en z = 11.12 y la de la entrada del Cuarto 4 empieza en z = 18.88. El Cuarto 3
// pone sus propias paredes de 12 cm adentro de ese hueco, así que su piso va de 11.24 a
// 18.76 (7.52 m de fondo) y su z = 0 cae en el mundo en 0.48 - 11.24 = -10.76.
public static class DisposicionCuartos
{
    public static readonly Quaternion Giro = Quaternion.Euler(0f, 180f, 0f);

    public static readonly Vector3 Cuarto2 = new Vector3(2.55f, 0f, -3.52f);
    public static readonly Vector3 Pasillo = new Vector3(2.55f, 0f, 0.48f);
    public static readonly Vector3 Cuarto3 = new Vector3(2.55f, 0f, -10.76f);
    public static readonly Vector3 Cuarto4 = new Vector3(2.55f, 0f, -18.52f);
}
