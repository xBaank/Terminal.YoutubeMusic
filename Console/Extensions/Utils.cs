namespace Console.Extensions;

public static class Utils
{
    public static async ValueTask<TResult> PipeAsync<TSource, TResult>(
        this Task<TSource> input,
        Func<TSource, TResult> pipe
    ) => pipe(await input);

    public static async ValueTask<TResult> PipeAsync<TSource, TResult>(
        this ValueTask<TSource> input,
        Func<TSource, TResult> pipe
    ) => pipe(await input);
}
