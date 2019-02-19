using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System;

namespace RocksmithFontGenerator.Models
{
    public sealed class DropShadowSettings : ReactiveObject
    {
        [Reactive]
        public double BlurRadius { get; set; } = Defaults.ShadowBlurRadius;

        [Reactive]
        public double Direction { get; set; } = Defaults.ShadowDirection;

        [Reactive]
        public double Opacity { get; set; } = Defaults.ShadowOpacity;

        [Reactive]
        public double Depth { get; set; } = Defaults.ShadowDepth;
    }
}
