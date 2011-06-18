namespace Gate
{    
    public interface IApplication
    {
        AppDelegate Create();
    }

    public interface IApplication<in T1>
    {
        AppDelegate Create(T1 t1);
    }

    public interface IApplication<in T1, in T2>
    {
        AppDelegate Create(T1 arg1, T2 arg2);
    }
    
    public interface IApplication<in T1, in T2, in T3>
    {
        AppDelegate Create(T1 arg1, T2 arg2, T3 arg3);
    }

    public interface IApplication<in T1, in T2, in T3, in T4>
    {
        AppDelegate Create(T1 arg1, T2 arg2, T3 arg3, T4 arg4);
    }
}
