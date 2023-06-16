# Simulacrum

## Developing

Simulacrum relies on FFmpeg for AV-decoding. If you are modifying the native libraries, make sure you have the FFmpeg development files installed. Always build FFmpeg as a set of static libraries, to avoid runtime issues related to shadow-copying assemblies on load. 

If you use `vcpkg`, the dependencies can be installed with `vcpkg install ffmpeg[all] --triplet x64-windows`.

The plugin should always be built for the x64 platform.

