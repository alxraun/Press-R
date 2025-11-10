namespace Microtools.Graphics
{
    public interface IHasTarget<T>
        where T : class
    {
        T Target { get; set; }
    }
}
