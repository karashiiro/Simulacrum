﻿namespace Simulacrum.Drawing.Common;

public interface IScreen
{
    void Show(IRenderSource source);

    void Play();

    void Pause();
}