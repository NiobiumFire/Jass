using BelotWebApp.BelotClasses.Cards;

namespace BelotWebApp.BelotClasses.Agents
{
    public class AgentAdvanced
    {
        public AgentAdvanced(int inputs, int neurons, int outputs)
        {
            Brain = new(inputs, neurons, outputs);
            CallVariation = [];
        }

        public AgentAdvanced(AgentAdvanced master)
        {
            Brain = new Brain(master.Brain);
            CallVariation = new List<int>();
        }

        public Brain Brain { get; set; }
        public float Fitness { get; set; }
        public int Errors { get; set; }
        public List<int> CallVariation { get; set; }
        public int GamesPlayed { get; set; }

        public static Random rnd = new Random();

        public int CallSuit(float[] inputs) // keep calling and playing separate for now. Can maybe let the brain process call and play inputs together, and take the max of the subrange of outputs depending on the required action
        {
            float[] choices = Brain.Forward(inputs);
            //double[] calls = choices.ToList().GetRange(8, 9).ToArray();
            //return Array.IndexOf(calls, calls.Max());
            int choice = Array.IndexOf(choices, choices.Max());
            return choice;
        }

        public float[] PlayCard(BelotGame game)
        {
            var input = BuildNNInputVector(game);
            float[] choices = Brain.Forward(input);
            return choices.ToList().GetRange(0, 8).ToArray();
        }

        public void ModifyFitness(float score)
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
                    if (rnd.Next(100) + 1 <= chance) Brain.Hidden1.Biases[i] = (float)rnd.NextDouble() * 2 - 1;
                    for (int j = 0; j < Brain.Hidden1.Weights.GetLength(1); j++)
                    {
                        if (rnd.Next(100) + 1 <= chance) Brain.Hidden1.Weights[i, j] = (float)rnd.NextDouble() * 2 - 1;
                    }
                }
            }

            for (int i = 0; i < Brain.Hidden2.Weights.GetLength(0); i++)
            {
                lock (rnd)
                {
                    if (rnd.Next(100) + 1 <= chance) Brain.Hidden2.Biases[i] = (float)rnd.NextDouble() * 2 - 1;
                    for (int j = 0; j < Brain.Hidden2.Weights.GetLength(1); j++)
                    {
                        if (rnd.Next(100) + 1 <= chance) Brain.Hidden2.Weights[i, j] = (float)rnd.NextDouble() * 2 - 1;
                    }
                }
            }

            for (int i = 0; i < Brain.Output.Weights.GetLength(0); i++)
            {
                lock (rnd)
                {
                    if (rnd.Next(100) + 1 <= chance) Brain.Output.Biases[i] = (float)rnd.NextDouble() * 2 - 1;
                    for (int j = 0; j < Brain.Output.Weights.GetLength(1); j++)
                    {
                        if (rnd.Next(100) + 1 <= chance) Brain.Output.Weights[i, j] = (float)rnd.NextDouble() * 2 - 1;
                    }
                }
            }
        }

        public static float[] BuildNNInputVector(BelotGame game)
        {
            List<float> input = [];

            input.AddRange(EncodeHand(game.Hand?[game.Turn]));
            input.AddRange(EncodeTableCards(game.TableCards));
            input.AddRange(EncodeCall(game.RoundCall));
            input.AddRange(EncodeSuit(game.TrickSuit));
            input.AddRange(EncodeTurn(game.Turn));

            return [.. input];
        }

        private static float[] EncodeHand(List<Card>? hand) // which card in which hand slot -> agent must output 0-7 for choice of card to play
        {
            float[] cardsInHand = new float[32 * 8];

            if (hand != null)
            {
                for (int i = 0; i < hand.Count; i++)
                {
                    int cardNum = BelotHelpers.GetCardIndex(hand[i]);
                    cardsInHand[i * 32 + cardNum] = 1f;
                }
            }

            return cardsInHand;
        }

        private static float[] EncodeTableCards(Card[]? tableCards)
        {
            float[] cardsOnTable = new float[32];

            if (tableCards != null)
            {
                foreach (var card in tableCards)
                {
                    if (!card.IsNull())
                    {
                        cardsOnTable[BelotHelpers.GetCardIndex(card)] = 1f;
                    }
                }
            }

            return cardsOnTable;
        }

        private static float[] EncodeCall(Call call)
        {
            float[] vector = new float[6]; // C, D, H, S, NT, AT

            if (call > 0)
            {
                vector[(int)call - 1] = 1f; // Clubs = 1, Can't be Pass = 0 when playing a card
            }

            return vector;
        }

        private static float[] EncodeSuit(Suit? suit)
        {
            float[] vector = new float[4];

            if (suit.HasValue)
            {
                vector[(int)suit.Value - 1] = 1f; // Clubs = 1
            }

            return vector;
        }

        private static float[] EncodeTurn(int turn)
        {
            float[] vector = new float[4];
            vector[turn] = 1f;
            return vector;
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
        public float[] Forward(float[] inputs)
        {
            ReLU activation = new();
            float[] outputs = Hidden1.Forward(inputs);
            outputs = activation.Forward(outputs);
            outputs = Hidden2.Forward(outputs);
            outputs = activation.Forward(outputs);
            outputs = Output.Forward(outputs);
            return Normalize(outputs);
        }

        private float[] Normalize(float[] outputs)
        {
            float min = outputs.Min();
            if (min < 0f)
            {
                outputs = outputs.Select(o => o - min).ToArray();
            }

            float sum = outputs.Sum();
            if (sum == 0f)
            {
                return outputs; // avoid div by 0
            }

            return outputs.Select(o => o / sum).ToArray();
        }
    }

    public class Layer
    {
        public Layer(int inputs, int neurons)
        {
            var biases = new float[neurons];
            var weights = new float[neurons, inputs];
            for (int i = 0; i < neurons; i++)
            {
                lock (rnd) biases[i] = (float)rnd.NextDouble() * 2 - 1;
                for (int j = 0; j < inputs; j++)
                {
                    lock (rnd) weights[i, j] = (float)rnd.NextDouble() * 2 - 1;
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
        public float[,] Weights { get; set; }
        public float[] Biases { get; set; }

        public static Random rnd = new Random();

        public float[] Forward(float[] inputs)
        {
            var outputs = new float[Biases.Length];
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
        public float[] Forward(float[] inputs)
        {
            var outputs = new float[inputs.Length];
            for (int i = 0; i < inputs.Length; i++)
            {
                outputs[i] = Math.Max(0, inputs[i]);
            }
            return outputs;
        }
    }
}