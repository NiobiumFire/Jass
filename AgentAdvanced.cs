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
            CallVariation = new List<int>();
        }

        public AgentAdvanced(AgentAdvanced master)
        {
            Brain = new Brain(master.Brain);
            CallVariation = new List<int>();
        }

        public Brain Brain { get; set; }
        public double Fitness { get; set; }
        public int Errors { get; set; }
        public List<int> CallVariation { get; set; }
        public int GamesPlayed { get; set; }

        public static Random rnd = new Random();

        public int CallSuit(double[] inputs) // keep calling and playing separate for now. Can maybe let the brain process call and play inputs together, and take the max of the subrange of outputs depending on the required action
        {
            double[] choices = Brain.Forward(inputs);
            //double[] calls = choices.ToList().GetRange(8, 9).ToArray();
            //return Array.IndexOf(calls, calls.Max());
            int choice = Array.IndexOf(choices, choices.Max());
            return choice;
        }

        public int PlayCard(double[] inputs)
        {
            double[] choices = Brain.Forward(inputs);
            double[] cards = choices.ToList().GetRange(0, 8).ToArray();
            return Array.IndexOf(cards, cards.Max());
        }

        public void ModifyFitness(double score)
        {
            Fitness += score;
        }

        public void CrossOver(AgentAdvanced partner, int chance)
        {
            for (int i = 0; i < Brain.Hidden1.Weights.GetLength(0); i++)
            {
                lock (rnd)
                {
                    if (rnd.Next(100) + 1 <= chance) Brain.Hidden1.Biases[i] = partner.Brain.Hidden1.Biases[i];
                    for (int j = 0; j < Brain.Hidden1.Weights.GetLength(1); j++)
                    {
                        if (rnd.Next(100) + 1 <= chance) Brain.Hidden1.Weights[i, j] = partner.Brain.Hidden1.Weights[i, j];
                    }
                }
            }

            for (int i = 0; i < Brain.Hidden2.Weights.GetLength(0); i++)
            {
                lock (rnd)
                {
                    if (rnd.Next(100) + 1 <= chance) Brain.Hidden2.Biases[i] = partner.Brain.Hidden2.Biases[i];
                    for (int j = 0; j < Brain.Hidden2.Weights.GetLength(1); j++)
                    {
                        if (rnd.Next(100) + 1 <= chance) Brain.Hidden2.Weights[i, j] = partner.Brain.Hidden2.Weights[i, j];
                    }
                }
            }

            for (int i = 0; i < Brain.Output.Weights.GetLength(0); i++)
            {
                lock (rnd)
                {
                    if (rnd.Next(100) + 1 <= chance) Brain.Output.Biases[i] = partner.Brain.Output.Biases[i];
                    for (int j = 0; j < Brain.Output.Weights.GetLength(1); j++)
                    {
                        if (rnd.Next(100) + 1 <= chance) Brain.Output.Weights[i, j] = partner.Brain.Output.Weights[i, j];
                    }
                }
            }
        }

        public void Mutate(int chance)
        {
            for (int i = 0; i < Brain.Hidden1.Weights.GetLength(0); i++)
            {
                lock (rnd)
                {
                    if (rnd.Next(100) + 1 <= chance) Brain.Hidden1.Biases[i] = rnd.NextDouble() * 2 - 1;
                    for (int j = 0; j < Brain.Hidden1.Weights.GetLength(1); j++)
                    {
                        if (rnd.Next(100) + 1 <= chance) Brain.Hidden1.Weights[i, j] = rnd.NextDouble() * 2 - 1;
                    }
                }
            }

            for (int i = 0; i < Brain.Hidden2.Weights.GetLength(0); i++)
            {
                lock (rnd)
                {
                    if (rnd.Next(100) + 1 <= chance) Brain.Hidden2.Biases[i] = rnd.NextDouble() * 2 - 1;
                    for (int j = 0; j < Brain.Hidden2.Weights.GetLength(1); j++)
                    {
                        if (rnd.Next(100) + 1 <= chance) Brain.Hidden2.Weights[i, j] = rnd.NextDouble() * 2 - 1;
                    }
                }
            }

            for (int i = 0; i < Brain.Output.Weights.GetLength(0); i++)
            {
                lock (rnd)
                {
                    if (rnd.Next(100) + 1 <= chance) Brain.Output.Biases[i] = rnd.NextDouble() * 2 - 1;
                    for (int j = 0; j < Brain.Output.Weights.GetLength(1); j++)
                    {
                        if (rnd.Next(100) + 1 <= chance) Brain.Output.Weights[i, j] = rnd.NextDouble() * 2 - 1;
                    }
                }
            }
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
        public Brain(Brain master)
        {
            Hidden1 = new Layer(master.Hidden1);
            Hidden2 = new Layer(master.Hidden2);
            Output = new Layer(master.Output);
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
            outputs = Output.Forward(outputs);
            return outputs;
        }
    }

    public class Layer
    {
        public Layer(int inputs, int neurons)
        {
            double[] biases = new double[neurons];
            double[,] weights = new double[neurons, inputs];
            for (int i = 0; i < neurons; i++)
            {
                lock (rnd) biases[i] = rnd.NextDouble() * 2 - 1;
                for (int j = 0; j < inputs; j++)
                {
                    lock (rnd) weights[i, j] = rnd.NextDouble() * 2 - 1;
                }
            }

            Weights = weights;
            Biases = biases;
        }

        public Layer(Layer master)
        {
            Weights = master.Weights;
            Biases = master.Biases;
        }

        public double[,] Weights { get; set; }

        public double[] Biases { get; set; }

        public static Random rnd = new Random();

        public double[] Forward(double[] inputs)
        {
            double[] outputs = new double[Biases.Length];
            for (int i = 0; i < Biases.Length; i++) // for each neuron
            {
                for (int j = 0; j < inputs.Length; j++) // for each input
                {
                    outputs[i] += inputs[j] * Weights[i, j];
                }
                outputs[i] += Biases[i];
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