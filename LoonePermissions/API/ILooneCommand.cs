using Rocket.API;

namespace ChubbyQuokka.LoonePermissions.API
{
    public interface ILooneCommand
    {
        string Help { get; }
        void Excecute(IRocketPlayer caller, string[] args);
    }

    internal enum EGroupProperty
    {
        NAME,
        PARENT,
        PREFIX,
        SUFFIX,
        COLOR,
        PRIORITY
    }
}