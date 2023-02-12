using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ChatWebApp
{
    public class AgentAdvanced
    {
        public AgentAdvanced(int inputs, int neurons, int outputs)
        {
            Brain = new Brain(inputs, neurons, outputs);
        }

        public Brain Brain { get; set; }
        public int Fitness { get; set; }

        public int CallSuit() // keep calling and playing separate for now. Can maybe let the brain process call and play inputs together, and take the max of the subrange of outputs depending on the required action
        {
            return 0;
        }

        public int PlayCard(double[] inputs)
        {
            double[] choices = Brain.Forward(inputs);
            return Array.IndexOf(choices, choices.Max());
        }


    }

    public class Brain
    {
        public Brain(int inputs, int neurons, int outputs)
        {
            Hidden1 = new Layer(inputs, neurons);
            Hidden2 = new Layer(neurons, neurons);
            Output = new Layer(neurons, outputs);
        }

        public Layer Hidden1 { get; set; }
        public Layer Hidden2 { get; set; }
        public Layer Output { get; set; }

        public double[] Forward(double[] inputs)
        {
            ReLU activation = new ReLU();
            double[] outputs = Hidden1.Forward(inputs);
            outputs = activation.Forward(outputs);
            outputs = Hidden2.Forward(outputs);
            outputs = activation.Forward(outputs);
            return outputs;
        }
    }

    public class Layer
    {
        public Layer(int inputs, int neurons)
        {
            double[] biases = new double[neurons];
            double[,] weights = new double[neurons, inputs];
            Random rnd = new Random();
            for (int i = 0; i < neurons; i++)
            {
                biases[i] = rnd.Next(-1000000000, 1000000000) / 1000000000.0;
                for (int j = 0; j < inputs; j++)
                {
                    weights[i, j] = rnd.Next(-1000000000, 1000000000) / 1000000000.0;
                }
            }

            Weights = weights;
            Biases = biases;
        }

        public double[,] Weights { get; set; }

        public double[] Biases { get; set; }

        public double[] Forward(double[] inputs)
        {
            double[] outputs = Biases;
            for (int i = 0; i < Biases.Length; i++) // for each neuron
            {
                for (int j = 0; j < inputs.Length; j++) // for each input
                {
                    outputs[i] += inputs[j] * Weights[i, j];
                }
            }

            return outputs;
        }
    }

    public class ReLU // rectified linear activation function
    {
        public double[] Forward(double[] inputs)
        {
            double[] outputs = new double[inputs.Length];
            for (int i = 0; i < inputs.Length; i++)
            {
                outputs[i] = Math.Max(0, inputs[i]);
            }
            return outputs;
        }
    }
}