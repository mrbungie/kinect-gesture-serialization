using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Accord;
using Accord.MachineLearning;
using Accord.Statistics;
using Accord.Statistics.Models;
using Accord.Statistics.Models.Markov;
using Accord.Statistics.Models.Markov.Learning;
using Accord.Statistics.Models.Markov.Topology;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace GestureProcessingLibrary
{
    /// <summary>
    /// Implements a GestureProcessingLibrary.Gesture that models a skeletal gesture.
    /// </summary>
    /// 
    [Serializable()]
    public class Gesture : ISerializable
    {
        private string name;
        private double[][][] trainingData;
        private int sampleDimensionsCount, frameCount, trainingSampleCount, alphabetCount, statesCount;
        private KMeans trainingDataKMeans;
        private double recognitionThreshold = 0;
        private HiddenMarkovModel model;

        /// <summary>
        /// Initializes a new instance of GestureProcessingLibrary.Gesture for the specified gesture name.
        /// </summary>
        /// <param name="name">The name of the gesture to be modeled</param>
        /// <param name="alphabetCount">The number of alphabet signs in the model</param>
        /// <param name="statesCount">The number of hidden states in the model</param>
        public Gesture(string name, int alphabetCount = 8, int statesCount = 12)
        {
            this.name = name;
            this.trainingData = DataMethods.LoadPositionData(this.name);

            this.sampleDimensionsCount = trainingData[0][0].Count();
            this.frameCount = trainingData[0].Count();
            this.trainingSampleCount = trainingData.Count();
            this.statesCount = statesCount;

            this.alphabetCount = alphabetCount;
            this.trainingDataKMeans = new KMeans(alphabetCount);

            this.TrainModel();
        }

        /// <summary>
        /// Utilizes the KMeans algorithm to calculate centroids and then it clusters the training
        /// position data.
        /// </summary>
        /// <returns>The training data clusters</returns>
        private int[][] DataKMeans()
        {
            double[][] meanData = DataMethods.Mean(trainingData);
            int[] meanCluster = this.trainingDataKMeans.Compute(meanData);

            int[][] trainingClusters = new int[trainingSampleCount][];
            for (int i = 0; i < trainingSampleCount; i++)
            {
                trainingClusters[i] = trainingDataKMeans.Clusters.Compute(trainingData[i]);
            }

            return trainingClusters;
        }

        /// <summary>
        /// Trains the model based on the given position data.
        /// </summary>
        private void TrainModel()
        {
            double trainingLikelihood;
            double factor = this.trainingSampleCount;

            int[][] trainingLabels = DataKMeans();

            Forward modelTopology = new Forward(statesCount, 2);
            this.model = new HiddenMarkovModel(modelTopology, alphabetCount);
            var baumWelchTeacher = new BaumWelchLearning(model);
            baumWelchTeacher.Run(trainingLabels);
            
            for (int i = 0; i < this.trainingSampleCount; i++)
            {
                trainingLikelihood = model.Evaluate(trainingLabels[i]);
                this.recognitionThreshold += trainingLikelihood;
            }

            this.recognitionThreshold *= (2 / factor);
        }


        /// <summary>
        /// Evaluates a new movement based on the trained model
        /// </summary>
        /// <param name="newGestureData">The position data from the new movement</param>
        /// <returns>The evaluation based on the model</returns>
        public bool EvaluateMovement(double[][] newGestureData)
        {
            int[] newGestureLabels = trainingDataKMeans.Clusters.Compute(newGestureData);
            double newGestureLikelihood = model.Evaluate(newGestureLabels);

            bool wasDetected = (newGestureLikelihood >= recognitionThreshold) ? true : false;

            return wasDetected;
        }

        public double LikelihoodMovement(double[][] newGestureData)
        {
            int[] newGestureLabels = trainingDataKMeans.Clusters.Compute(newGestureData);
            double newGestureLikelihood = model.Evaluate(newGestureLabels);

            return newGestureLikelihood;
        }

        /// <summary>
        /// Gets the gesture name
        /// </summary>
        /// <returns>The gesture name</returns>
        public string GetName()
        {
            return name;
        }

        /// <summary>
        ///  Deserialization
        /// </summary>
        public Gesture(SerializationInfo info, StreamingContext ctxt)
        {
            //Get the values from info and assign them to the appropriate properties
            this.name = (String)info.GetValue("name", typeof(string));
            this.sampleDimensionsCount = (int)info.GetValue("sampleDimensionsCount", typeof(int));
            this.frameCount = (int)info.GetValue("frameCount", typeof(int));
            this.trainingSampleCount = (int)info.GetValue("trainingSampleCount", typeof(int));
            this.alphabetCount = (int)info.GetValue("alphabetCount", typeof(int));
            this.statesCount = (int)info.GetValue("statesCount", typeof(int));
            this.trainingDataKMeans = (KMeans)info.GetValue("trainingDataKMeans", typeof(KMeans));
            this.recognitionThreshold = (double)info.GetValue("recognitionThreshold", typeof(double));
            this.model = (HiddenMarkovModel)info.GetValue("model", typeof(HiddenMarkovModel));
        }

        /// <summary>
        /// Serialization function
        /// </summary>
        /// <param name="info"></param>
        /// <param name="ctxt"></param>
        public void GetObjectData(SerializationInfo info, StreamingContext ctxt)
        {
            info.AddValue("name", name);
            info.AddValue("sampleDimensionsCount", sampleDimensionsCount);
            info.AddValue("frameCount", frameCount);
            info.AddValue("trainingSampleCount", trainingSampleCount);
            info.AddValue("alphabetCount", alphabetCount);
            info.AddValue("statesCount", statesCount);
            info.AddValue("trainingDataKMeans", trainingDataKMeans);
            info.AddValue("recognitionThreshold", recognitionThreshold);
            info.AddValue("model", model);
        }
    }
}
