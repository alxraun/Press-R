using System;

namespace PressR.Graphics.Controllers
{
    public interface IGraphicsController<TContext>
        where TContext : struct
    {
        void Update(TContext context);

        void Clear();
    }
}
