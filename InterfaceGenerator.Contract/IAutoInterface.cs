namespace InterfaceGenerator
{
    /// <summary>
    /// The base interface for generated public auto interfaces
    /// </summary>
    public interface IAutoInterface
    {
    }

    /// <summary>
    /// The base generic interface for generated public auto interfaces, where T is an implementation type
    /// </summary>
    public interface IAutoInterface<T> : IAutoInterface
    {
    }
}