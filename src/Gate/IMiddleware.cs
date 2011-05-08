namespace Gate
{    
    public interface IMiddleware
    {
        AppDelegate Create(AppDelegate app);
    }

    public interface IMiddleware<in T1>
    {
        AppDelegate Create(AppDelegate app, T1 t1);
    }

    public interface IMiddleware<in T1, in T2>
    {
        AppDelegate Create(AppDelegate app, T1 arg1, T2 arg2);
    }
    
    public interface IMiddleware<in T1, in T2, in T3>
    {
        AppDelegate Create(AppDelegate app, T1 arg1, T2 arg2, T3 arg3);
    }

    public interface IMiddleware<in T1, in T2, in T3, in T4>
    {
        AppDelegate Create(AppDelegate app, T1 arg1, T2 arg2, T3 arg3, T4 arg4);
    }
}
