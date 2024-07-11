namespace Console.Extensions
{
    public static class QueueExtensions
    {
        public static T? TryGet<T>(this Queue<T> queue)
        {
            var isSome = queue.TryDequeue(out var result);
            return isSome ? result : default;
        }
    }
}
