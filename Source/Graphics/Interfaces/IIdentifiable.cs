namespace PressR.Graphics.Interfaces
{
    public interface IIdentifiable<TKey>
    {
        TKey Key { get; }
    }
}
