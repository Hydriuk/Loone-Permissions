using Rocket.API;

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