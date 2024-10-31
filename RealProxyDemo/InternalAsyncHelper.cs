namespace RealProxy;

/// <summary>
/// 提供异步方法的帮助程序。
/// </summary>
internal static class InternalAsyncHelper
{
    /// <summary>
    /// 判断指定方法是否是异步方法。
    /// </summary>
    /// <param name="method">需要检查的 <see cref="MethodInfo"/> 对象。</param>
    /// <returns>如果是异步方法，则返回 true；否则返回 false。</returns>
    public static bool IsAsyncMethod(this MethodInfo method)
    {
        return method.ReturnType == typeof(Task)
               || method.ReturnType.IsGenericType
               && method.ReturnType.GetGenericTypeDefinition() == typeof(Task<>);
    }

    /// <summary>
    /// 等待给定的任务并在完成后执行后续操作，最后执行最终操作。
    /// </summary>
    /// <param name="actualReturnValue">实际返回的任务。</param>
    /// <param name="postAction">完成后的操作。</param>
    /// <param name="finalAction">最终操作，包含异常信息（如果有）。</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public static async Task AwaitTaskWithPostActionAndFinally(
        Task actualReturnValue,
        Func<Task> postAction,
        Action<Exception> finalAction
    )
    {
        Exception exception = null;

        try
        {
            await actualReturnValue;
            await postAction();
        }
        catch (Exception ex)
        {
            exception = ex;
        }
        finally
        {
            finalAction(exception);
        }
    }

    /// <summary>
    /// 等待给定的任务并在完成后执行后续操作，返回结果并执行最终操作。
    /// </summary>
    /// <typeparam name="T">任务返回值的类型。</typeparam>
    /// <param name="actualReturnValue">实际返回的任务。</param>
    /// <param name="postAction">完成后的操作，接受任务返回的结果。</param>
    /// <param name="finalAction">最终操作，包含异常信息（如果有）。</param>
    /// <returns>任务的结果。</returns>
    public static async Task<T> AwaitTaskWithPostActionAndFinallyAndGetResult<T>(
        Task<T> actualReturnValue,
        Func<object, Task> postAction,
        Action<Exception> finalAction
    )
    {
        Exception exception = null;
        try
        {
            var result = await actualReturnValue;
            await postAction(result);
            return result;
        }
        catch (Exception ex)
        {
            exception = ex;
            throw;
        }
        finally
        {
            finalAction(exception);
        }
    }

    /// <summary>
    /// 调用指定类型的任务，并在完成后执行后续操作，最终操作返回结果。
    /// </summary>
    /// <param name="taskReturnType">任务返回值的类型。</param>
    /// <param name="actualReturnValue">实际返回的任务。</param>
    /// <param name="action">执行后续操作。</param>
    /// <param name="finalAction">最终操作，包含异常信息（如果有）。</param>
    /// <returns>任务的结果。</returns>
    public static object CallAwaitTaskWithPostActionAndFinallyAndGetResult(
        Type taskReturnType,
        object actualReturnValue,
        Func<object, Task> action,
        Action<Exception> finalAction
    )
    {
        return typeof(InternalAsyncHelper)
            .GetMethod(
                "AwaitTaskWithPostActionAndFinallyAndGetResult",
                BindingFlags.Public | BindingFlags.Static
            )
            .MakeGenericMethod(taskReturnType)
            .Invoke(null, new object[] { actualReturnValue, action, finalAction });
    }
}
