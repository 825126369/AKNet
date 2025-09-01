using AKNet.Common;

namespace AKNet.Test
{
    internal static class TaskExample
    {
        static ResettableValueTaskSource mTaskSource = new ResettableValueTaskSource();
        public static async void Do()
        {
            while (true)
            {
                if(mTaskSource.TryGetValueTask(out ValueTask mTask, this, default))
                {
                    throw new Exception();
                }
                await mTask.ConfigureAwait(false);
            }
        }

      
    }
}
