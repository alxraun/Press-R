namespace PressR.Graphics.GraphicObjects
{
    public interface IHasTarget<T>
        where T : class
    {
        T Target { get; set; }
    }
}
