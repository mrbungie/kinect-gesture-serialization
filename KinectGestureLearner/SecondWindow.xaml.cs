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
    /// Interaction logic for SecondWindow.xaml
    /// </summary>
    public partial class SecondWindow : Window
    {
        KinectSensor _sensor;        
        Skeleton[] skeletons;
        IList<String> list_string;
        List<Gesture> listaMovimientos;
        Queue<double[]> observacionActual;
        double[][] arrayAnalisis;
        double x, y, z;

        public SecondWindow()
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
            //Lista de strings para mostrar en el databind
            list_string = new List<String>();
            //Cargar todos los movimientos
            listaMovimientos = DataMethods.LoadAllGestures();
            //Instanciar cola (Primero entra primero sale) de coordenadas
            observacionActual = new Queue<double[]>(60);
            // Revisa si hay Kinects
            if (KinectSensor.KinectSensors.Count > 0)
            {
                //Inicializa _sensor apuntando al primer kinect
                _sensor = KinectSensor.KinectSensors.FirstOrDefault();

                //Revisa si esta conectado
                if (_sensor.Status == KinectStatus.Connected)
                {
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
                return first;
            }
        }

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

            //Primer esqueleto
            Skeleton first = GetFirstSkeleton(e);

            //Si el esqueleto no es nulo
            if (first != null)
            {
                //Una vez que se llena la cola con datos
                if (observacionActual.Count == 60)
                {                
                    //Cola a Array
                    double[][] arrayAnalisis = observacionActual.ToArray();
                    //Se corre la cola en 1
                    observacionActual.Dequeue();

                    //Establece el minimo como el valor más negativo posible en los double (no existe nada menor)
                    double minimo = double.MinValue;
                    //Establece el nombre del gesto como nulo.
                    string gestureName = null;

                    //Se revisa si la cola posee algun movimiento
                    foreach (Gesture gesture in listaMovimientos)
                    {
                        //Se revisa si es posible que sea parte de un movimiento y se ve cual es el mejor
                        //Se compara con respecto a los demas, y se ve cual es el más probable (con más likelihood)
                        if (gesture.EvaluateMovement(arrayAnalisis) && gesture.LikelihoodMovement(arrayAnalisis) > minimo)
                        {
                            //Se reestablece el likelihood como el nuevo minimo
                            minimo = gesture.LikelihoodMovement(arrayAnalisis);
                            //Se pone el nombre correspondiente al nuevo
                            gestureName = gesture.GetName();
                        }
                    }

                    //Si es que hay gesto (su nombre no es nulo) se agrega al registro
                    if (gestureName != null)
                    {
                        //Se agrega a la lista de strings a mostrar en el registro
                        list_string.Add("Se ha detectado (" + gestureName + ") en " + DateTime.Now.ToShortTimeString());
                        //Se vuelve a cargar el datagrid con la lista
                        datagrid.ItemsSource = list_string.Select(temp => new { Value = temp }).ToList();
                    }

                    //Borrar la cola
                    observacionActual = new Queue<double[]>();
                }

                //Obtener posiciones

                /*
                 * Posiciones relativas TEST
                x = first.Joints[JointType.HandLeft].Position.X - first.Joints[JointType.HipCenter].Position.X;
                y = first.Joints[JointType.HandLeft].Position.Y - first.Joints[JointType.HipCenter].Position.Y;
                z = first.Joints[JointType.HandLeft].Position.Z - first.Joints[JointType.HipCenter].Position.Z;
                */

                //Posiciones absolutas mano derecha
                x = first.Joints[JointType.HandLeft].Position.X;
                y = first.Joints[JointType.HandLeft].Position.Y;
                z = first.Joints[JointType.HandLeft].Position.Z;

                //Se agrega la medicion a la cola
                observacionActual.Enqueue(new double[3] { x*10, y*10, z*10 });
                
                //Se truncan los numeros (son muy largos) y se mandan a los labels x2, y2 y z2.
                x2.Content = Math.Truncate(x * 1000) / 1000;
                y2.Content = Math.Truncate(y * 1000) / 1000;
                z2.Content = Math.Truncate(z * 1000) / 1000;

            }
            //Si no hay esqueleto se ponen 0 en los labels x2, y2, z3 (ayudadores)
            else
            {
                x2.Content = 0;
                y2.Content = 0;
                z2.Content = 0;
            }
        }
    }
}
