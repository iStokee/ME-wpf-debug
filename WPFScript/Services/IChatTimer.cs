using System;

namespace MESharp.Services
{
    public interface IChatTimer
    {
        event EventHandler Tick;
        void Start();
    }
}
