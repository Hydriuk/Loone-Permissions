using Rocket.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LoonePermissions.API
{
    public interface ILooneCommand
    {
        string Help { get; }
        void Excecute(IRocketPlayer caller, string[] args);
    }

    public enum EGroupProperty
    {
        NAME,
        PARENT,
        PREFIX,
        SUFFIX,
        COLOR,
        PRIORITY
    }
}