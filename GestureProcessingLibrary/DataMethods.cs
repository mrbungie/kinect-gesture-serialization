using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Globalization;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace GestureProcessingLibrary
{
    /// <summary>
    /// This class contains Methods for processing point clouds collected with the Kinect.
    /// 
    /// </summary>
    public class DataMethods
    {
        /// <summary>
        /// This static method loads gesture position array from a CSV file.
        /// </summary>
        /// <param name="GestureName">This is the name of the gesture to be collected</param>
        /// <returns>The jagged array representation of a given gesture</returns>
        public static double[][][] LoadPositionData(string GestureName)
        {

            string RootPath = AppDomain.CurrentDomain.BaseDirectory + "Observaciones\\";

            var xCsvStream = new StreamReader(RootPath + GestureName + "_x.csv");
            var yCsvStream = new StreamReader(RootPath + GestureName + "_y.csv");
            var zCsvStream = new StreamReader(RootPath + GestureName + "_z.csv");
            string[] xStringArray, yStringArray, zStringArray;

            int frameNumber = File.ReadLines(RootPath + GestureName + "_x.csv").Count();
            int sampleNumber = GetSampleNumber(xCsvStream);
            xCsvStream = new StreamReader(RootPath + GestureName + "_x.csv");

            double[][][] samples = new double[sampleNumber][][];

            for (int i = 0; i < sampleNumber; i++)
            {
                samples[i] = new double[frameNumber][];
                for (int j = 0; j < frameNumber; j++)
                {
                    samples[i][j] = new double[3];
                }
            }

            for (int i = 0; i < frameNumber; i++)
            {
                xStringArray = xCsvStream.ReadLine().Split(',');
                yStringArray = yCsvStream.ReadLine().Split(',');
                zStringArray = zCsvStream.ReadLine().Split(',');

                for (int j = 0; j < sampleNumber; j++)
                {
                    samples[j][i][0] = double.Parse(xStringArray[j], new CultureInfo("en-US"));
                    samples[j][i][1] = double.Parse(yStringArray[j], new CultureInfo("en-US"));
                    samples[j][i][2] = double.Parse(zStringArray[j], new CultureInfo("en-US"));
                }
            }
            return samples;
        }

        /// <summary>
        /// Gets the number of samples based on a CSV file.
        /// </summary>
        /// <param name="CsvStream">StreamReader object to be counted</param>
        /// <returns>Number of samples</returns>
        private static int GetSampleNumber(StreamReader CsvStream)
        {
            var StringArray = CsvStream.ReadLine().Split(',');
            return StringArray.Count();
        }

        /// <summary>
        /// Calculates the mean of an array of movement position data.
        /// </summary>
        /// <param name="data">Array of position data</param>
        /// <returns>The mean of an movement position data</returns>
        public static double[][] Mean(double[][][] data)
        {
            int sampleDimensions = data[0][0].Count();
            int frameNumber = data[0].Count();
            int sampleNumber = data.Count();
            double[][] meanData = new double[frameNumber][];


            for (int i = 0; i < sampleNumber; i++)
            {
                for (int j = 0; j < frameNumber; j++)
                {
                    if (i == 0) meanData[j] = new double[sampleDimensions];
                    for (int k = 0; k < sampleDimensions; k++)
                    {
                        if (i == 0 && j == 0) meanData[j][k] = 0;
                        meanData[j][k] += data[i][j][k] / sampleNumber;
                    }
                }
            }


            return meanData;
        }

        /// <summary>
        /// Serializes an gesture object to an .ges file
        /// </summary>
        /// <param name="gesture">Gesture object to be serialized</param>
        public static void SaveGesture(Gesture gesture)
        {
            string RootPath = AppDomain.CurrentDomain.BaseDirectory + "Gestures\\";

            Stream stream = File.Open(RootPath + gesture.GetName() + ".ges", FileMode.Create);
            BinaryFormatter bFormatter = new BinaryFormatter();

            bFormatter.Serialize(stream, gesture);
            stream.Close();
        }

        /// <summary>
        /// Deserializes an .ges file to an gesture object
        /// </summary>
        /// <param name="path">Location of the .ges file</param>
        /// <returns>A gesture</returns>
        public static Gesture LoadGesture(String path)
        {
            Stream stream = File.Open(path, FileMode.Open);
            BinaryFormatter bFormatter = new BinaryFormatter();

            Gesture gesture = (Gesture)bFormatter.Deserialize(stream);
            stream.Close();

            return gesture;
        }

        /// <summary>
        /// Loads all the .ges files in a folder to a list of gestures
        /// </summary>
        /// <returns>List of gestures</returns>
        public static List<Gesture> LoadAllGestures()
        {
            List<Gesture> AllGestures = new List<Gesture>();

            string rootPath = AppDomain.CurrentDomain.BaseDirectory + "Gestures\\";
            string[] filePaths = Directory.GetFiles(@rootPath, "*.ges");

            foreach(string filePath in filePaths){
                AllGestures.Add(LoadGesture(filePath));
            }

            return AllGestures;
        }
    }
}