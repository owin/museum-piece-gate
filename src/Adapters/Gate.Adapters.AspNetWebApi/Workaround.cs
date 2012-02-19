using Owin;

namespace Gate.Adapters.AspNetWebApi
{
    static class Workaround
    {
#pragma warning disable 169
        static ResultDelegate _resultDelegate;
#pragma warning restore 169
    }
}
