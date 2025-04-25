using PressR.Graphics.Shaders;
using UnityEngine;

namespace PressR.Graphics.Interfaces
{
    public interface IMpbConfigurator
    {
        void Configure(MaterialPropertyBlock mpb, MpbConfigurators.Payload payload);
    }
}
