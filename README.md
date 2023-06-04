# Simulacrum

## Developing

Simulacrum relies on FFmpeg for AV-decoding. If you are modifying the native libraries, Make sure you have the FFmpeg development files installed. Always build FFmpeg as a set of static libraries, to avoid runtime issues related to shadow-copying assemblies on load.
