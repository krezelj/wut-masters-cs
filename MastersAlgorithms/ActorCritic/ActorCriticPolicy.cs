namespace MastersAlgorithms.ActorCritic
{
    public class ActorCriticPolicy
    {
        public Model Actor;
        public Model Critic;

        public ActorCriticPolicy(string modelDirectory)
        {
            string actor_path = Path.Join(modelDirectory, "actor.onnx");
            string critic_path = Path.Join(modelDirectory, "critic.onnx");

            Actor = new Model(actor_path);
            Critic = new Model(critic_path);
        }

        public float[] GetLogits(float[] input, int batchCount = 1)
        {
            return Actor.Forward(input, batchCount);
        }

        public float[] GetProbs(float[] input, int batchCount = 1)
        {
            return Utils.Softmax(GetLogits(input, batchCount), batchCount);
        }

        public float[] GetMaskedProbs(float[] input, bool[] mask, int batchCount = 1)
        {
            return Utils.MaskedSoftmax(GetLogits(input, batchCount), mask, batchCount);
        }

        public float[] GetValue(float[] input, int batchCount = 1)
        {
            return Critic.Forward(input, batchCount);
        }
    }
}