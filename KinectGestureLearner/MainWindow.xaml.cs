using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Timers;
using Microsoft.Kinect;
using GestureProcessingLibrary;

namespace KinectGestureLearner
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        KinectSensor _sensor;        
        CollectedGestureData proceso;
        Skeleton[] skeletons;
        Gesture gesture;
        int count, limite;
        double x, y, z;
        bool observando;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Closing_1(object sender, System.ComponentModel.CancelEventArgs e)
        {
            //Parar sensor cuando se cierra ventana
            _sensor.Stop();
        }

        private void Window_Loaded_1(object sender, RoutedEventArgs e)
        {
            //Parte en modo de NO observacion
            observando = false;
            // Revisa si hay Kinects
            if (KinectSensor.KinectSensors.Count > 0)
            {
                //Inicializa _sensor apuntando al primer kinect
                _sensor = KinectSensor.KinectSensors.FirstOrDefault();

                //Revisa si esta conectado
                if (_sensor.Status == KinectStatus.Connected)
                {
                    //Actualizacion de UI
                    kinectstatus.Foreground = Brushes.ForestGreen;
                    kinectstatus.Content = "Conectado";

                    //Habilitando Flujos de datos (Color, Esqueleto y Profundidad)
                    _sensor.ColorStream.Enable();
                    _sensor.SkeletonStream.Enable();
                    _sensor.DepthStream.Enable();

                    //Se sincronizan
                    _sensor.AllFramesReady += _sensor_AllFramesReady;
                    //Parte
                    _sensor.Start();
                }
            }
        }

        //Obtiene el primer skeleto a mano
        Skeleton GetFirstSkeleton(AllFramesReadyEventArgs e)
        {
            using (SkeletonFrame skeletonFrameData = e.OpenSkeletonFrame())
            {
                //Instanciar nuevo skeleton
                skeletons = new Skeleton[skeletonFrameData.SkeletonArrayLength];
                //Prevencion contra framedrop (perdida de informacion)
                if (skeletonFrameData == null)
                {
                    return null;
                }

                //Copia datos a skeletos
                skeletonFrameData.CopySkeletonDataTo(skeletons);

                //Elige el primer skeleto de todos
                Skeleton first = (from s in skeletons where s.TrackingState == SkeletonTrackingState.Tracked select s).FirstOrDefault();
                //Devuelve skeleton
                return first;
            }
        }

        /// <summary>
        /// Envia todos 
        /// </summary>
        /// <param name="sender">Objeto que envia el evento (no sirve de mucho aqui)</param>
        /// <param name="e">Argumentos con los que se envia el evento, en este caso, informacion acerca del conjunto de frames
        ///     (COLOR), (SKELETON), (DEPTH)</param>
        void _sensor_AllFramesReady(object sender, AllFramesReadyEventArgs e)
        {
            // Envio de datos de camara a caja de Imagen en UI
            using (ColorImageFrame colorFrame = e.OpenColorImageFrame())
            {
                //Prevencion contra framedrop
                if (colorFrame == null)
                {
                    return;
                }

                //Arreglo de pixeles
                byte[] pixels = new byte[colorFrame.PixelDataLength];
                //Copiando datos de frame de color al arreglo de pixeles
                colorFrame.CopyPixelDataTo(pixels);

                //Una barrida de 4 bits por columna (RGB + alpha)
                int stride = colorFrame.Width * 4;
                kinectcolor.Source = BitmapSource.Create(colorFrame.Width, colorFrame.Height, 96, 96, PixelFormats.Bgr32, null, pixels, stride);
            }

            //Obtiene el primer skeleton
            Skeleton first = GetFirstSkeleton(e);

            if (first != null)
            {
                
                //Se puede agregar muestra
                agregar.IsEnabled = true;

                //Posiciones de la mano izquierda
                x = first.Joints[JointType.HandLeft].Position.X;
                y = first.Joints[JointType.HandLeft].Position.Y;
                z = first.Joints[JointType.HandLeft].Position.Z;

                //Se llenan los ayudadores
                x2.Content = Math.Truncate(x * 1000) / 1000;
                y2.Content = Math.Truncate(y * 1000) / 1000;
                z2.Content = Math.Truncate(z * 1000) / 1000;

                //Codigo que se realiza si se esta obteniendo una muestra
                if (observando)
                {
                    //Si esta dentro del limite (60 para 2 segs, 90 para 3 segs, 120 para 4... etc)
                    if (count < limite)
                    {
                        //Por cada frame se aumenta el contador
                        count++;
                        //Se agregar punto a la nube de puntos de la muestra
                        proceso.agregarPosicion((float)x*10, (float)y*10, (float)z*10);
                        ayuda.Content = count.ToString();
                    }
                    //Si supero el limite
                    else
                    {
                        //Contador de frames
                        ayuda.Content = "-";
                        //Se agrega la muestra
                        proceso.agregarMuestra();
                        //Se deja de observar
                        observando = false;
                    }
                }
            }
            //Si no hay skeleton
            else
            {
                //Ayudadores en 0
                x2.Content = 0;
                y2.Content = 0;
                z2.Content = 0;
                //No se puede agregar muestra
                agregar.IsEnabled = false;
            }
        }

        //Cambios una vez que se comienza el modo de recolección (CLICK AL BOTON RECOLECTAR)
        private void startcollecting_Click(object sender, RoutedEventArgs e)
        {
            // Actualizacion UI
            progstat.Foreground = Brushes.Blue; //Cambia color progstat
            progstat.Content = "Esperando nuevos movimientos"; //Cambia texto progstat
            ctdmov.Content = 0; // Se reinicia el contador de muestras
            //Habilitacion y deshabilitacion de botones correspondientes
            stopcollecting.IsEnabled = true;
            startcollecting.IsEnabled = false;
            agregar.IsEnabled = true;
            name.IsEnabled = false;
            
            //Inicialización de objeto de posiciones
            proceso = new CollectedGestureData(textname.Text);
            
        }

        //Sucesos que pasan cuando se agrega una nueva muestra
        private void agregar_Click(object sender, RoutedEventArgs e)
        {
            //Se reinicia el contador de frames grabados
            count = 0;
            //Se agrega uno al contador de muestras
            ctdmov.Content = int.Parse(ctdmov.Content.ToString()) + 1;
            //Se establece limite de frames segun segundos establecidos
            limite = 30*int.Parse(seconds.Text);
            //Se activa modo grabacion
            observando = true;
        }

        //Boton subir angulo
        private void up_Click(object sender, RoutedEventArgs e)
        {
            //Se sube el angulo en 3 grados
            _sensor.ElevationAngle += 3;
        }

        //Boton bajar angulo
        private void down_Click(object sender, RoutedEventArgs e)
        {
            //Se baja el angulo en 3 grados
            _sensor.ElevationAngle -= 3;
        }

        //Parar recoleccion y comenzar procesamiento de los movimientos
        private void stopcollecting_Click(object sender, RoutedEventArgs e)
        {
            //Se guardan los csv de nubes de puntos
            proceso.escribirArchivos();
            //Se instancia un nuevo objeto gesture y se cargan los csv en el
            gesture = new Gesture(textname.Text);
            textname.Text = "";

            //Se serializa el objeto gesture (guarda en un archivo ges)
            progstat.Content = "Modelando";
            DataMethods.SaveGesture(gesture);
            progstat.Content = "Guardado";

            //Se cambian la habilitacion de los botones
            stopcollecting.IsEnabled = false;
            startcollecting.IsEnabled = true;
            agregar.IsEnabled = false;
        }
    }
}
