using UnityEngine;

namespace PressR.Graphics
{
    public interface IMpbConfigurator
    {
        void Configure(MaterialPropertyBlock mpb, MpbConfigurators.Payload payload);
    }
}
