using System.Reflection;

namespace RealProxy;

internal static class InternalAsyncHelper
{
    public static bool IsAsyncMethod(this MethodInfo method)
    {
        return method.ReturnType == typeof(Task)
               || method.ReturnType.IsGenericType
               && method.ReturnType.GetGenericTypeDefinition() == typeof(Task<>);
    }

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

    public static object CallAwaitTaskWithPostActionAndFinallyAndGetResult(
        Type taskReturnType,
        object actualReturnValue,
        Func<object, Task> action,
        Action<Exception> finalAction
    )
    {
        //AwaitTaskWithPostActionAndFinallyAndGetResult<taskReturnType>(actualReturnValue, action, finalAction);
        return typeof(InternalAsyncHelper)
            .GetMethod(
                "AwaitTaskWithPostActionAndFinallyAndGetResult",
                BindingFlags.Public | BindingFlags.Static
            )
            .MakeGenericMethod(taskReturnType)
            .Invoke(null, new object[] { actualReturnValue, action, finalAction });
    }
}