namespace PressR.Graphics
{
    public interface IIdentifiable<TKey>
    {
        TKey Key { get; }
    }
}
