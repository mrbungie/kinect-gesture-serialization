using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Globalization;

namespace KinectGestureLearner
{
    //Clase para guardar nube de puntos
    class CollectedGestureData
    {
        private string nombre;
        private List<List<float>> observacionesX, observacionesY, observacionesZ;
        private List<float> muestraX, muestraY, muestraZ;
        private string x, y, z;

        //Constructor para inicializar cada instancia
        public CollectedGestureData(string nombre)
        {
            //Establece el nombre del gesto
            this.nombre = nombre;
            //Instancia listas de listas de numeros flotantes.
            observacionesX = new List<List<float>>();
            observacionesY = new List<List<float>>();
            observacionesZ = new List<List<float>>();
            //Instancia listas de numeros flotantes (placeholders)
            muestraX = new List<float>();
            muestraY = new List<float>();
            muestraZ = new List<float>();
        }

        //Agregar medición de posición de 1 FPS
        public void agregarPosicion(float x, float y, float z)
        {
            //Agrega a la lista la medicion de cada coordenada
            muestraX.Add(x);
            muestraY.Add(y);
            muestraZ.Add(z);
        }

        //Agregar una colección de X FPS, en este caso 60.
        public void agregarMuestra()
        {
            //Agrega la muestra a la observacion completa
            observacionesX.Add(muestraX);
            observacionesY.Add(muestraY);
            observacionesZ.Add(muestraZ);
            //Reinstancia listas de muestras, para volver a llenarlas posteriormente
            muestraX = new List<float>();
            muestraY = new List<float>();
            muestraZ = new List<float>();
        }

        //Escribir los archivos de posicion _x, _y, _z
        public void escribirArchivos()
        {
            string directorio = AppDomain.CurrentDomain.BaseDirectory + "Observaciones\\";

            //Define tamaño de la muestra (60 en 2 segs por ejemplo)
            int tamañoMuestra = observacionesX.FirstOrDefault().Count;
            int cantidadObs = observacionesX.Count;

            //Guarda cada dimension
            for(int i = 0; i < tamañoMuestra; i++){
                //Guarda cada observacion
                for (int j = 0; j < cantidadObs; j++)
                {
                    //Agrega los valores de las coordenadas
                    x += observacionesX[j][i].ToString(CultureInfo.CreateSpecificCulture("en-US"));
                    y += observacionesY[j][i].ToString(CultureInfo.CreateSpecificCulture("en-US"));
                    z += observacionesZ[j][i].ToString(CultureInfo.CreateSpecificCulture("en-US"));
                    if (j != cantidadObs - 1)
                    {
                        //Agrega comas
                        x += ',';
                        y += ',';
                        z += ',';
                    }
                }
                //Finales de linea (ENTERs)
                x += '\r';
                x += '\n';
                y += '\r';
                y += '\n';
                z += '\r';
                z += '\n';
            }

            //Se guarda el csv de las Xs.
            StreamWriter writer = new StreamWriter(directorio + nombre + "_x.csv", true);
            //Escribe
            writer.Write(x);
            //Close
            writer.Close();

            //Se guarda el csv de las Ys.
            writer = new StreamWriter(directorio + nombre + "_y.csv", true);
            //Escribe
            writer.Write(y);
            //Close
            writer.Close();

            //Se guarda el csv de las Zs.
            writer = new StreamWriter(directorio + nombre + "_z.csv", true);
            //Escribe
            writer.Write(z);
            //Cierra
            writer.Close();
        }
    }
}
