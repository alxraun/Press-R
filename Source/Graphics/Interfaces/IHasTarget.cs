namespace PressR.Graphics.Interfaces
{
    public interface IHasTarget<T>
        where T : class
    {
        T Target { get; set; }
    }
}
