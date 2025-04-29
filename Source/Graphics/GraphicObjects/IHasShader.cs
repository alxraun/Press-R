using UnityEngine;

namespace PressR.Graphics.GraphicObjects
{
    public interface IHasShader
    {
        Shader Shader { get; set; }
    }
}
